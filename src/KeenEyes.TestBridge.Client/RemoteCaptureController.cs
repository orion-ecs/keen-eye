using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Ipc.Protocol;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="ICaptureController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteCaptureController(TestBridgeClient client) : ICaptureController
{
    /// <inheritdoc />
    public bool IsAvailable => client.SendRequestAsync<bool>("capture.isAvailable", null, CancellationToken.None)
        .GetAwaiter().GetResult();

    /// <inheritdoc />
    public bool IsRecording => client.SendRequestAsync<bool>("capture.isRecording", null, CancellationToken.None)
        .GetAwaiter().GetResult();

    /// <inheritdoc />
    public int RecordedFrameCount => client.SendRequestAsync<int>("capture.recordedFrameCount", null, CancellationToken.None)
        .GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<FrameCapture> CaptureFrameAsync()
    {
        var result = await client.SendRequestAsync<FrameCapture>("capture.captureFrame", null, CancellationToken.None);
        return result;
    }

    /// <inheritdoc />
    public async Task<string> SaveScreenshotAsync(string filePath, ImageFormat format = ImageFormat.Png)
    {
        var result = await client.SendRequestAsync<string>("capture.saveScreenshot", new { filePath, format }, CancellationToken.None);
        return result ?? throw new InvalidOperationException("Failed to save screenshot");
    }

    /// <inheritdoc />
    public async Task<byte[]> GetScreenshotBytesAsync(ImageFormat format = ImageFormat.Png)
    {
        // Server returns base64-encoded bytes
        var base64 = await client.SendRequestAsync<string>("capture.getScreenshotBytes", new { format }, CancellationToken.None);
        if (string.IsNullOrEmpty(base64))
        {
            throw new InvalidOperationException("Failed to get screenshot bytes");
        }

        return Convert.FromBase64String(base64);
    }

    /// <inheritdoc />
    public async Task<(int Width, int Height)> GetFrameSizeAsync()
    {
        var result = await client.SendRequestAsync<FrameSizeResult>("capture.getFrameSize", null, CancellationToken.None);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to get frame size");
        }

        return (result.Width, result.Height);
    }

    /// <inheritdoc />
    public async Task StartRecordingAsync(int maxFrames = 300)
    {
        await client.SendRequestAsync("capture.startRecording", new { maxFrames }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FrameCapture>> StopRecordingAsync()
    {
        var result = await client.SendRequestAsync<FrameCapture[]>("capture.stopRecording", null, CancellationToken.None);
        return result ?? [];
    }
}
