using KeenEyes.TestBridge.Capture;

namespace KeenEyes.TestBridge.Capture;

/// <summary>
/// In-process implementation of <see cref="ICaptureController"/>.
/// </summary>
/// <remarks>
/// This is a stub implementation for Phase 1. Full capture functionality
/// will be implemented in Phase 3 with GPU framebuffer and platform fallbacks.
/// </remarks>
internal sealed class CaptureControllerImpl : ICaptureController
{
    private readonly List<FrameCapture> recordedFrames = [];
    private bool isRecording;
    private int maxRecordingFrames = 300;
    private long currentFrameNumber;

    /// <inheritdoc />
    public bool IsAvailable => false; // Will be true when capture providers are implemented

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
    public Task<FrameCapture> CaptureFrameAsync()
    {
        ThrowIfNotAvailable();

        // Stub: Return empty frame
        var capture = new FrameCapture
        {
            Width = 0,
            Height = 0,
            Pixels = [],
            FrameNumber = currentFrameNumber,
            Timestamp = TimeSpan.FromSeconds(currentFrameNumber / 60.0)
        };

        if (isRecording)
        {
            RecordFrame(capture);
        }

        return Task.FromResult(capture);
    }

    /// <inheritdoc />
    public Task<string> SaveScreenshotAsync(string filePath, ImageFormat format = ImageFormat.Png)
    {
        ThrowIfNotAvailable();

        // Stub: Would capture and save to file
        return Task.FromResult(Path.GetFullPath(filePath));
    }

    /// <inheritdoc />
    public Task<byte[]> GetScreenshotBytesAsync(ImageFormat format = ImageFormat.Png)
    {
        ThrowIfNotAvailable();

        // Stub: Would capture and encode
        return Task.FromResult(Array.Empty<byte>());
    }

    /// <inheritdoc />
    public Task<(int Width, int Height)> GetFrameSizeAsync()
    {
        // Stub: Return default size
        return Task.FromResult((0, 0));
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

    private void ThrowIfNotAvailable()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "Capture is not available. Ensure a graphics context is configured and capture providers are implemented.");
        }
    }
}
