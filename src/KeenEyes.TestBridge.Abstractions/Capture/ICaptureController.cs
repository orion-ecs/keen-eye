namespace KeenEyes.TestBridge.Capture;

/// <summary>
/// Controller for capturing screenshots and frames.
/// </summary>
/// <remarks>
/// <para>
/// The capture controller provides methods for taking screenshots and recording frames
/// from running applications. This is useful for visual regression testing and debugging.
/// </para>
/// <para>
/// Capture uses a priority-based provider system:
/// </para>
/// <list type="bullet">
/// <item><description>GPU framebuffer capture (highest priority) - Most accurate, requires graphics context</description></item>
/// <item><description>Platform window capture (fallback) - Works for any windowed application</description></item>
/// </list>
/// </remarks>
public interface ICaptureController
{
    /// <summary>
    /// Gets whether capture is available.
    /// </summary>
    /// <remarks>
    /// Capture may be unavailable if no graphics context is present (e.g., headless mode)
    /// and no platform fallback is supported.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>
    /// Captures the current frame from the GPU framebuffer.
    /// </summary>
    /// <returns>Raw RGBA pixel data and dimensions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when capture is not available.</exception>
    Task<FrameCapture> CaptureFrameAsync();

    /// <summary>
    /// Captures a screenshot and saves it to a file.
    /// </summary>
    /// <param name="filePath">The path to save the screenshot to.</param>
    /// <param name="format">The image format. Defaults to PNG.</param>
    /// <returns>The full path to the saved file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when capture is not available.</exception>
    Task<string> SaveScreenshotAsync(string filePath, ImageFormat format = ImageFormat.Png);

    /// <summary>
    /// Captures a screenshot and returns it as bytes.
    /// </summary>
    /// <param name="format">The image format. Defaults to PNG.</param>
    /// <returns>The screenshot as encoded image bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when capture is not available.</exception>
    Task<byte[]> GetScreenshotBytesAsync(ImageFormat format = ImageFormat.Png);

    /// <summary>
    /// Gets the current frame dimensions.
    /// </summary>
    /// <returns>The frame dimensions as (Width, Height).</returns>
    Task<(int Width, int Height)> GetFrameSizeAsync();

    /// <summary>
    /// Starts recording frames for later retrieval.
    /// </summary>
    /// <param name="maxFrames">Maximum number of frames to record. Older frames are discarded.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Recording captures frames at the application's frame rate. Use this for
    /// creating test replays or debugging visual issues.
    /// </remarks>
    Task StartRecordingAsync(int maxFrames = 300);

    /// <summary>
    /// Gets whether recording is currently active.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Stops recording and returns the captured frames.
    /// </summary>
    /// <returns>The list of captured frames in chronological order.</returns>
    Task<IReadOnlyList<FrameCapture>> StopRecordingAsync();

    /// <summary>
    /// Gets the number of frames currently recorded.
    /// </summary>
    int RecordedFrameCount { get; }
}

/// <summary>
/// Represents a captured frame.
/// </summary>
/// <remarks>
/// Frame data is in RGBA format with 8 bits per channel (32 bits per pixel).
/// Pixel data is stored in row-major order, starting from the top-left corner.
/// </remarks>
public readonly record struct FrameCapture
{
    /// <summary>
    /// Gets the frame width in pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Gets the frame height in pixels.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Gets the raw pixel data in RGBA format.
    /// </summary>
    /// <remarks>
    /// Array length is Width * Height * 4 bytes.
    /// </remarks>
    public required byte[] Pixels { get; init; }

    /// <summary>
    /// Gets the frame number when this was captured.
    /// </summary>
    public required long FrameNumber { get; init; }

    /// <summary>
    /// Gets the timestamp when this frame was captured.
    /// </summary>
    public required TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Gets the size of the pixel data in bytes.
    /// </summary>
    public int ByteSize => Width * Height * 4;

    /// <summary>
    /// Gets the pixel at the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>The pixel as (R, G, B, A) values.</returns>
    public (byte R, byte G, byte B, byte A) GetPixel(int x, int y)
    {
        var offset = (y * Width + x) * 4;
        return (Pixels[offset], Pixels[offset + 1], Pixels[offset + 2], Pixels[offset + 3]);
    }
}

/// <summary>
/// Image format for screenshot encoding.
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// PNG format (lossless compression).
    /// </summary>
    Png,

    /// <summary>
    /// JPEG format (lossy compression).
    /// </summary>
    Jpeg,

    /// <summary>
    /// BMP format (uncompressed).
    /// </summary>
    Bmp
}
