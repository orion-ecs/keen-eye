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

    /// <summary>
    /// Checks whether screenshot capture is available on the connected bridge.
    /// </summary>
    /// <returns>A <see cref="CaptureAvailabilityResult"/> indicating whether capture is available.</returns>
    [McpServerTool(Name = "capture_is_available")]
    [Description("Check if screenshot capture is available. Capture may be unavailable in headless mode.")]
    public CaptureAvailabilityResult CaptureIsAvailable()
    {
        var bridge = connection.GetBridge();
        return new CaptureAvailabilityResult { IsAvailable = bridge.Capture.IsAvailable };
    }

    /// <summary>
    /// Captures a screenshot and returns it as base64-encoded image data.
    /// </summary>
    /// <param name="format">The image format: 'png' (default), 'jpeg', or 'bmp'.</param>
    /// <returns>A <see cref="ScreenshotResult"/> containing the encoded image data, or an error if capture failed.</returns>
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

    /// <summary>
    /// Captures a screenshot and saves it to a file.
    /// </summary>
    /// <param name="filePath">The file path to save the screenshot to.</param>
    /// <param name="format">The image format: 'png' (default), 'jpeg', or 'bmp'.</param>
    /// <returns>A <see cref="ScreenshotFileResult"/> containing the full saved file path, or an error if capture failed.</returns>
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

    /// <summary>
    /// Captures a region of the screen and returns it as base64-encoded image data.
    /// </summary>
    /// <param name="x">The left edge X coordinate (0-based).</param>
    /// <param name="y">The top edge Y coordinate (0-based).</param>
    /// <param name="width">The region width in pixels.</param>
    /// <param name="height">The region height in pixels.</param>
    /// <param name="format">The image format: 'png' (default), 'jpeg', or 'bmp'.</param>
    /// <returns>A <see cref="ScreenshotResult"/> containing the encoded image data, or an error if capture failed.</returns>
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

    /// <summary>
    /// Captures a region of the screen and saves it to a file.
    /// </summary>
    /// <param name="x">The left edge X coordinate (0-based).</param>
    /// <param name="y">The top edge Y coordinate (0-based).</param>
    /// <param name="width">The region width in pixels.</param>
    /// <param name="height">The region height in pixels.</param>
    /// <param name="filePath">The file path to save the screenshot to.</param>
    /// <param name="format">The image format: 'png' (default), 'jpeg', or 'bmp'.</param>
    /// <returns>A <see cref="ScreenshotFileResult"/> containing the full saved file path, or an error if capture failed.</returns>
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

    /// <summary>
    /// Starts recording frames, storing them in memory on the game side.
    /// </summary>
    /// <param name="maxFrames">The maximum number of frames to record before the oldest are discarded (default: 300).</param>
    /// <returns>A <see cref="RecordingResult"/> indicating whether recording was started.</returns>
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

    /// <summary>
    /// Stops recording and returns the number of frames captured.
    /// </summary>
    /// <returns>A <see cref="RecordingResult"/> containing the number of recorded frames.</returns>
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

    /// <summary>
    /// Checks whether frame recording is currently active.
    /// </summary>
    /// <returns>A <see cref="RecordingStateResult"/> describing the current recording state.</returns>
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

    /// <summary>
    /// Gets the number of frames recorded so far.
    /// </summary>
    /// <returns>A <see cref="RecordedCountResult"/> containing the recorded frame count and recording state.</returns>
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
    /// <summary>
    /// Gets whether screenshot capture is available.
    /// </summary>
    public required bool IsAvailable { get; init; }
}

/// <summary>
/// Result of a screenshot capture.
/// </summary>
public sealed record ScreenshotResult
{
    /// <summary>
    /// Gets whether the screenshot capture succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the base64-encoded image data, or <c>null</c> if capture failed.
    /// </summary>
    public string? Data { get; init; }

    /// <summary>
    /// Gets the MIME type of the captured image, or <c>null</c> if capture failed.
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Gets the width of the captured image in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets the height of the captured image in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Gets the error message, or <c>null</c> if capture succeeded.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of saving a screenshot to file.
/// </summary>
public sealed record ScreenshotFileResult
{
    /// <summary>
    /// Gets whether the screenshot capture succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the full path the screenshot was saved to, or <c>null</c> if capture failed.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets a human-readable message describing the outcome, or <c>null</c> if capture failed.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the error message, or <c>null</c> if capture succeeded.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of a recording operation.
/// </summary>
public sealed record RecordingResult
{
    /// <summary>
    /// Gets whether the recording operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets a human-readable message describing the outcome.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the number of frames captured, or <c>null</c> if not applicable.
    /// </summary>
    public int? FrameCount { get; init; }
}

/// <summary>
/// Current recording state.
/// </summary>
public sealed record RecordingStateResult
{
    /// <summary>
    /// Gets whether frame recording is currently active.
    /// </summary>
    public required bool IsRecording { get; init; }

    /// <summary>
    /// Gets the number of frames recorded so far.
    /// </summary>
    public required int RecordedFrameCount { get; init; }
}

/// <summary>
/// Recorded frame count result.
/// </summary>
public sealed record RecordedCountResult
{
    /// <summary>
    /// Gets the number of frames recorded so far.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets whether frame recording is currently active.
    /// </summary>
    public required bool IsRecording { get; init; }
}

#endregion
