using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Capture;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for screenshot capture and frame recording.
/// </summary>
[McpServerToolType]
public sealed class CaptureTools(BridgeConnectionManager connection)
{
    #region Screenshots

    [McpServerTool(Name = "capture_is_available")]
    [Description("Check if screenshot capture is available. Capture may be unavailable in headless mode.")]
    public CaptureAvailabilityResult CaptureIsAvailable()
    {
        var bridge = connection.GetBridge();
        return new CaptureAvailabilityResult { IsAvailable = bridge.Capture.IsAvailable };
    }

    [McpServerTool(Name = "capture_screenshot")]
    [Description("Capture a screenshot and return it as base64-encoded image data. Formats: 'png' (default), 'jpeg', 'bmp'.")]
    public async Task<ScreenshotResult> CaptureScreenshot(
        [Description("Image format: 'png' (default), 'jpeg', or 'bmp'")]
        string format = "png")
    {
        var bridge = connection.GetBridge();

        if (!bridge.Capture.IsAvailable)
        {
            return new ScreenshotResult
            {
                Success = false,
                Error = "Capture is not available"
            };
        }

        try
        {
            var imageFormat = ParseImageFormat(format);
            var (width, height) = await bridge.Capture.GetFrameSizeAsync();
            var bytes = await bridge.Capture.GetScreenshotBytesAsync(imageFormat);
            var base64 = Convert.ToBase64String(bytes);

            return new ScreenshotResult
            {
                Success = true,
                Data = base64,
                MimeType = GetMimeType(imageFormat),
                Width = width,
                Height = height
            };
        }
        catch (Exception ex)
        {
            return new ScreenshotResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    [McpServerTool(Name = "capture_screenshot_to_file")]
    [Description("Capture a screenshot and save it to a file. Returns the full file path.")]
    public async Task<ScreenshotFileResult> CaptureScreenshotToFile(
        [Description("The file path to save the screenshot to")]
        string filePath,
        [Description("Image format: 'png' (default), 'jpeg', or 'bmp'")]
        string format = "png")
    {
        var bridge = connection.GetBridge();

        if (!bridge.Capture.IsAvailable)
        {
            return new ScreenshotFileResult
            {
                Success = false,
                Error = "Capture is not available"
            };
        }

        try
        {
            var imageFormat = ParseImageFormat(format);
            var savedPath = await bridge.Capture.SaveScreenshotAsync(filePath, imageFormat);

            return new ScreenshotFileResult
            {
                Success = true,
                FilePath = savedPath,
                Message = $"Screenshot saved to {savedPath}"
            };
        }
        catch (Exception ex)
        {
            return new ScreenshotFileResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    [McpServerTool(Name = "capture_screenshot_region")]
    [Description("Capture a region of the screen and return it as base64-encoded image data.")]
    public async Task<ScreenshotResult> CaptureScreenshotRegion(
        [Description("Left edge X coordinate (0-based)")]
        int x,
        [Description("Top edge Y coordinate (0-based)")]
        int y,
        [Description("Region width in pixels")]
        int width,
        [Description("Region height in pixels")]
        int height,
        [Description("Image format: 'png' (default), 'jpeg', or 'bmp'")]
        string format = "png")
    {
        var bridge = connection.GetBridge();

        if (!bridge.Capture.IsAvailable)
        {
            return new ScreenshotResult
            {
                Success = false,
                Error = "Capture is not available"
            };
        }

        try
        {
            var imageFormat = ParseImageFormat(format);
            var bytes = await bridge.Capture.GetRegionScreenshotBytesAsync(x, y, width, height, imageFormat);
            var base64 = Convert.ToBase64String(bytes);

            return new ScreenshotResult
            {
                Success = true,
                Data = base64,
                MimeType = GetMimeType(imageFormat),
                Width = width,
                Height = height
            };
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return new ScreenshotResult
            {
                Success = false,
                Error = $"Invalid region: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ScreenshotResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    [McpServerTool(Name = "capture_screenshot_region_to_file")]
    [Description("Capture a region of the screen and save it to a file.")]
    public async Task<ScreenshotFileResult> CaptureScreenshotRegionToFile(
        [Description("Left edge X coordinate (0-based)")]
        int x,
        [Description("Top edge Y coordinate (0-based)")]
        int y,
        [Description("Region width in pixels")]
        int width,
        [Description("Region height in pixels")]
        int height,
        [Description("The file path to save the screenshot to")]
        string filePath,
        [Description("Image format: 'png' (default), 'jpeg', or 'bmp'")]
        string format = "png")
    {
        var bridge = connection.GetBridge();

        if (!bridge.Capture.IsAvailable)
        {
            return new ScreenshotFileResult
            {
                Success = false,
                Error = "Capture is not available"
            };
        }

        try
        {
            var imageFormat = ParseImageFormat(format);
            var savedPath = await bridge.Capture.SaveRegionScreenshotAsync(x, y, width, height, filePath, imageFormat);

            return new ScreenshotFileResult
            {
                Success = true,
                FilePath = savedPath,
                Message = $"Region screenshot saved to {savedPath}"
            };
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return new ScreenshotFileResult
            {
                Success = false,
                Error = $"Invalid region: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ScreenshotFileResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    #endregion

    #region Recording

    [McpServerTool(Name = "capture_start_recording")]
    [Description("Start recording frames. Frames are stored in memory on the game side.")]
    public async Task<RecordingResult> CaptureStartRecording(
        [Description("Maximum frames to record before oldest are discarded (default: 300)")]
        int maxFrames = 300)
    {
        var bridge = connection.GetBridge();

        if (!bridge.Capture.IsAvailable)
        {
            return new RecordingResult
            {
                Success = false,
                Message = "Capture is not available"
            };
        }

        if (bridge.Capture.IsRecording)
        {
            return new RecordingResult
            {
                Success = false,
                Message = "Recording is already in progress"
            };
        }

        await bridge.Capture.StartRecordingAsync(maxFrames);

        return new RecordingResult
        {
            Success = true,
            Message = $"Started recording (max {maxFrames} frames)"
        };
    }

    [McpServerTool(Name = "capture_stop_recording")]
    [Description("Stop recording and return the number of frames captured.")]
    public async Task<RecordingResult> CaptureStopRecording()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Capture.IsRecording)
        {
            return new RecordingResult
            {
                Success = false,
                Message = "No recording in progress"
            };
        }

        var frames = await bridge.Capture.StopRecordingAsync();

        return new RecordingResult
        {
            Success = true,
            Message = $"Stopped recording, captured {frames.Count} frames",
            FrameCount = frames.Count
        };
    }

    [McpServerTool(Name = "capture_is_recording")]
    [Description("Check if frame recording is currently active.")]
    public RecordingStateResult CaptureIsRecording()
    {
        var bridge = connection.GetBridge();

        return new RecordingStateResult
        {
            IsRecording = bridge.Capture.IsRecording,
            RecordedFrameCount = bridge.Capture.RecordedFrameCount
        };
    }

    [McpServerTool(Name = "capture_get_recorded_count")]
    [Description("Get the number of frames recorded so far.")]
    public RecordedCountResult CaptureGetRecordedCount()
    {
        var bridge = connection.GetBridge();

        return new RecordedCountResult
        {
            Count = bridge.Capture.RecordedFrameCount,
            IsRecording = bridge.Capture.IsRecording
        };
    }

    #endregion

    #region Helpers

    private static ImageFormat ParseImageFormat(string? format)
    {
        return format?.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => ImageFormat.Jpeg,
            "bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Png
        };
    }

    private static string GetMimeType(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Jpeg => "image/jpeg",
            ImageFormat.Bmp => "image/bmp",
            _ => "image/png"
        };
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result of a capture availability check.
/// </summary>
public sealed record CaptureAvailabilityResult
{
    public required bool IsAvailable { get; init; }
}

/// <summary>
/// Result of a screenshot capture.
/// </summary>
public sealed record ScreenshotResult
{
    public required bool Success { get; init; }
    public string? Data { get; init; }
    public string? MimeType { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result of saving a screenshot to file.
/// </summary>
public sealed record ScreenshotFileResult
{
    public required bool Success { get; init; }
    public string? FilePath { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result of a recording operation.
/// </summary>
public sealed record RecordingResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public int? FrameCount { get; init; }
}

/// <summary>
/// Current recording state.
/// </summary>
public sealed record RecordingStateResult
{
    public required bool IsRecording { get; init; }
    public required int RecordedFrameCount { get; init; }
}

/// <summary>
/// Recorded frame count result.
/// </summary>
public sealed record RecordedCountResult
{
    public required int Count { get; init; }
    public required bool IsRecording { get; init; }
}

#endregion
