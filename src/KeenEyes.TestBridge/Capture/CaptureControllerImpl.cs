using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.TestBridge.Capture;

/// <summary>
/// In-process implementation of <see cref="ICaptureController"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation captures screenshots directly from the GPU framebuffer
/// when a graphics context is available. Falls back gracefully when no graphics
/// context is present (e.g., headless testing mode).
/// </para>
/// <para>
/// When an <see cref="ILoopProvider"/> is available, OpenGL operations are marshalled
/// to the render thread to ensure the GL context is current.
/// </para>
/// </remarks>
/// <param name="graphicsContext">Optional graphics context for GPU capture.</param>
/// <param name="loopProvider">Optional loop provider for render thread marshalling.</param>
internal sealed class CaptureControllerImpl(IGraphicsContext? graphicsContext = null, ILoopProvider? loopProvider = null) : ICaptureController
{
    private readonly List<FrameCapture> recordedFrames = [];
    private bool isRecording;
    private int maxRecordingFrames = 300;
    private long currentFrameNumber;

    /// <inheritdoc />
    public bool IsAvailable => graphicsContext?.IsInitialized == true && graphicsContext.Device != null;

    /// <inheritdoc />
    public bool IsRecording => isRecording;

    /// <inheritdoc />
    public int RecordedFrameCount => recordedFrames.Count;

    /// <summary>
    /// Increments the frame counter for timestamp tracking.
    /// </summary>
    internal void OnFrameComplete()
    {
        currentFrameNumber++;
    }

    /// <inheritdoc />
    public async Task<FrameCapture> CaptureFrameAsync()
    {
        ThrowIfNotAvailable();

        // If we have a loop provider, marshal to render thread for GL context
        if (loopProvider != null)
        {
            var capture = await loopProvider.InvokeOnRenderThreadAsync(() => CaptureFrameCore());

            if (isRecording)
            {
                RecordFrame(capture);
            }

            return capture;
        }

        // No loop provider - call directly (assumes we're on the right thread)
        var directCapture = CaptureFrameCore();

        if (isRecording)
        {
            RecordFrame(directCapture);
        }

        return directCapture;
    }

    /// <summary>
    /// Core frame capture logic that must run on the render thread.
    /// </summary>
    private FrameCapture CaptureFrameCore()
    {
        var width = graphicsContext!.Width;
        var height = graphicsContext.Height;
        var pixels = new byte[width * height * 4];

        // Read pixels from GPU framebuffer
        graphicsContext.Device!.ReadFramebuffer(0, 0, width, height, PixelFormat.RGBA, pixels);

        // OpenGL reads pixels from bottom-left origin, flip vertically for standard top-left origin
        FlipVertically(pixels, width, height);

        return new FrameCapture
        {
            Width = width,
            Height = height,
            Pixels = pixels,
            FrameNumber = currentFrameNumber,
            Timestamp = TimeSpan.FromSeconds(currentFrameNumber / 60.0)
        };
    }

    /// <inheritdoc />
    public async Task<string> SaveScreenshotAsync(string filePath, ImageFormat format = ImageFormat.Png)
    {
        var bytes = await GetScreenshotBytesAsync(format);
        var fullPath = Path.GetFullPath(filePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(fullPath, bytes);
        return fullPath;
    }

    /// <inheritdoc />
    public async Task<byte[]> GetScreenshotBytesAsync(ImageFormat format = ImageFormat.Png)
    {
        var capture = await CaptureFrameAsync();
        return ImageEncoder.Encode(capture.Pixels, capture.Width, capture.Height, format);
    }

    /// <inheritdoc />
    public Task<(int Width, int Height)> GetFrameSizeAsync()
    {
        if (!IsAvailable)
        {
            return Task.FromResult((0, 0));
        }

        return Task.FromResult((graphicsContext!.Width, graphicsContext.Height));
    }

    /// <inheritdoc />
    public async Task<FrameCapture> CaptureRegionAsync(int x, int y, int width, int height)
    {
        ThrowIfNotAvailable();

        // If we have a loop provider, marshal to render thread for GL context
        if (loopProvider != null)
        {
            return await loopProvider.InvokeOnRenderThreadAsync(() => CaptureRegionCore(x, y, width, height));
        }

        // No loop provider - call directly (assumes we're on the right thread)
        return CaptureRegionCore(x, y, width, height);
    }

    /// <summary>
    /// Core region capture logic that must run on the render thread.
    /// </summary>
    private FrameCapture CaptureRegionCore(int x, int y, int width, int height)
    {
        var screenWidth = graphicsContext!.Width;
        var screenHeight = graphicsContext.Height;

        // Validate bounds
        if (x < 0 || y < 0 || width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Region dimensions must be positive and coordinates non-negative.");
        }

        if (x + width > screenWidth || y + height > screenHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(width),
                $"Region ({x}, {y}, {width}x{height}) extends outside screen bounds ({screenWidth}x{screenHeight}).");
        }

        var pixels = new byte[width * height * 4];

        // Convert screen coordinates (top-left origin) to OpenGL (bottom-left origin)
        var glY = screenHeight - y - height;

        graphicsContext.Device!.ReadFramebuffer(x, glY, width, height, PixelFormat.RGBA, pixels);

        // OpenGL reads pixels from bottom-left origin, flip vertically for standard top-left origin
        FlipVertically(pixels, width, height);

        return new FrameCapture
        {
            Width = width,
            Height = height,
            Pixels = pixels,
            FrameNumber = currentFrameNumber,
            Timestamp = TimeSpan.FromSeconds(currentFrameNumber / 60.0)
        };
    }

    /// <inheritdoc />
    public async Task<byte[]> GetRegionScreenshotBytesAsync(int x, int y, int width, int height, ImageFormat format = ImageFormat.Png)
    {
        var capture = await CaptureRegionAsync(x, y, width, height);
        return ImageEncoder.Encode(capture.Pixels, capture.Width, capture.Height, format);
    }

    /// <inheritdoc />
    public async Task<string> SaveRegionScreenshotAsync(int x, int y, int width, int height, string filePath, ImageFormat format = ImageFormat.Png)
    {
        var bytes = await GetRegionScreenshotBytesAsync(x, y, width, height, format);
        var fullPath = Path.GetFullPath(filePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(fullPath, bytes);
        return fullPath;
    }

    /// <inheritdoc />
    public Task StartRecordingAsync(int maxFrames = 300)
    {
        if (isRecording)
        {
            throw new InvalidOperationException("Recording is already in progress.");
        }

        isRecording = true;
        maxRecordingFrames = maxFrames;
        recordedFrames.Clear();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FrameCapture>> StopRecordingAsync()
    {
        if (!isRecording)
        {
            throw new InvalidOperationException("Recording is not in progress.");
        }

        isRecording = false;
        var result = recordedFrames.ToList();
        recordedFrames.Clear();

        return Task.FromResult<IReadOnlyList<FrameCapture>>(result);
    }

    private void RecordFrame(FrameCapture capture)
    {
        recordedFrames.Add(capture);

        // Trim to max size (FIFO)
        while (recordedFrames.Count > maxRecordingFrames)
        {
            recordedFrames.RemoveAt(0);
        }
    }

    /// <summary>
    /// Flips pixel data vertically (bottom-to-top becomes top-to-bottom).
    /// </summary>
    /// <param name="pixels">RGBA pixel data.</param>
    /// <param name="width">Image width.</param>
    /// <param name="height">Image height.</param>
    private static void FlipVertically(byte[] pixels, int width, int height)
    {
        var rowSize = width * 4;
        var tempRow = new byte[rowSize];

        for (var y = 0; y < height / 2; y++)
        {
            var topRowStart = y * rowSize;
            var bottomRowStart = (height - 1 - y) * rowSize;

            // Swap rows
            Array.Copy(pixels, topRowStart, tempRow, 0, rowSize);
            Array.Copy(pixels, bottomRowStart, pixels, topRowStart, rowSize);
            Array.Copy(tempRow, 0, pixels, bottomRowStart, rowSize);
        }
    }

    private void ThrowIfNotAvailable()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "Capture is not available. Ensure a graphics context is configured and initialized.");
        }
    }
}
