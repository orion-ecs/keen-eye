using System.Text.Json;
using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Ipc.Protocol;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles capture commands.
/// </summary>
internal sealed class CaptureCommandHandler(ICaptureController captureController) : ICommandHandler
{
    public string Prefix => "capture";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "isAvailable" => captureController.IsAvailable,
            "captureFrame" => await captureController.CaptureFrameAsync(),
            "saveScreenshot" => await HandleSaveScreenshotAsync(args),
            "getScreenshotBytes" => await HandleGetScreenshotBytesAsync(args),
            "getFrameSize" => await HandleGetFrameSizeAsync(),
            "startRecording" => await HandleStartRecordingAsync(args),
            "stopRecording" => await captureController.StopRecordingAsync(),
            "isRecording" => captureController.IsRecording,
            "recordedFrameCount" => captureController.RecordedFrameCount,
            _ => throw new InvalidOperationException($"Unknown capture command: {command}")
        };
    }

    private async Task<object?> HandleSaveScreenshotAsync(JsonElement? args)
    {
        var filePath = GetRequiredString(args, "filePath");
        var format = GetOptionalEnum<ImageFormat>(args, "format") ?? ImageFormat.Png;
        return await captureController.SaveScreenshotAsync(filePath, format);
    }

    private async Task<object?> HandleGetScreenshotBytesAsync(JsonElement? args)
    {
        var format = GetOptionalEnum<ImageFormat>(args, "format") ?? ImageFormat.Png;
        var bytes = await captureController.GetScreenshotBytesAsync(format);
        // Return as base64 for JSON transport
        return Convert.ToBase64String(bytes);
    }

    private async Task<object?> HandleGetFrameSizeAsync()
    {
        var (width, height) = await captureController.GetFrameSizeAsync();
        return new FrameSizeResult { Width = width, Height = height };
    }

    private async Task<object?> HandleStartRecordingAsync(JsonElement? args)
    {
        var maxFrames = GetOptionalInt(args, "maxFrames") ?? 300;
        await captureController.StartRecordingAsync(maxFrames);
        return null;
    }

    #region Typed Argument Helpers (AOT-compatible)

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    private static int? GetOptionalInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetInt32();
    }

    private static T? GetOptionalEnum<T>(JsonElement? args, string name) where T : struct, Enum
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var str = prop.GetString();
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        return Enum.Parse<T>(str, ignoreCase: true);
    }

    #endregion
}
