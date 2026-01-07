using KeenEyes.TestBridge.Capture;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of ICaptureController for testing.
/// </summary>
internal sealed class MockCaptureController : ICaptureController
{
    public bool IsAvailable { get; set; } = true;
    public bool IsRecording { get; private set; }
    public int RecordedFrameCount { get; private set; }
    public (int Width, int Height) FrameSize { get; set; } = (1920, 1080);
    public byte[] ScreenshotData { get; set; } = [];

    private readonly List<FrameCapture> recordedFrames = [];

    public Task<FrameCapture> CaptureFrameAsync()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Capture is not available");
        }

        var frame = new FrameCapture
        {
            Width = FrameSize.Width,
            Height = FrameSize.Height,
            Pixels = new byte[FrameSize.Width * FrameSize.Height * 4],
            FrameNumber = 1,
            Timestamp = TimeSpan.FromSeconds(1)
        };

        return Task.FromResult(frame);
    }

    public Task<string> SaveScreenshotAsync(string filePath, ImageFormat format = ImageFormat.Png)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Capture is not available");
        }

        // In a real implementation this would save to disk
        return Task.FromResult(filePath);
    }

    public Task<byte[]> GetScreenshotBytesAsync(ImageFormat format = ImageFormat.Png)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Capture is not available");
        }

        if (ScreenshotData.Length > 0)
        {
            return Task.FromResult(ScreenshotData);
        }

        // Return a minimal PNG stub (PNG header + empty IDAT)
        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return Task.FromResult(pngHeader);
    }

    public Task<(int Width, int Height)> GetFrameSizeAsync()
    {
        return Task.FromResult(FrameSize);
    }

    public Task StartRecordingAsync(int maxFrames = 300)
    {
        IsRecording = true;
        RecordedFrameCount = 0;
        recordedFrames.Clear();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FrameCapture>> StopRecordingAsync()
    {
        IsRecording = false;
        return Task.FromResult<IReadOnlyList<FrameCapture>>(recordedFrames);
    }
}
