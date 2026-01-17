using System.Text.Json;
using KeenEyes.TestBridge.Replay;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles replay recording and playback commands.
/// </summary>
internal sealed class ReplayCommandHandler(IReplayController replayController) : ICommandHandler
{
    public string Prefix => "replay";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Recording control
            "startRecording" => await HandleStartRecordingAsync(args),
            "stopRecording" => await replayController.StopRecordingAsync(),
            "cancelRecording" => await replayController.CancelRecordingAsync(),
            "isRecording" => await replayController.IsRecordingAsync(),
            "getRecordingInfo" => await replayController.GetRecordingInfoAsync(),
            "forceSnapshot" => await replayController.ForceSnapshotAsync(),

            // Recording management
            "save" => await HandleSaveAsync(args),
            "load" => await HandleLoadAsync(args),
            "list" => await HandleListAsync(args),
            "delete" => await HandleDeleteAsync(args),
            "getMetadata" => await HandleGetMetadataAsync(args),

            // Playback control
            "play" => await replayController.PlayAsync(),
            "pause" => await replayController.PauseAsync(),
            "stopPlayback" => await replayController.StopPlaybackAsync(),
            "getPlaybackState" => await replayController.GetPlaybackStateAsync(),
            "setSpeed" => await HandleSetSpeedAsync(args),

            // Playback navigation
            "seekFrame" => await HandleSeekFrameAsync(args),
            "seekTime" => await HandleSeekTimeAsync(args),
            "stepForward" => await HandleStepForwardAsync(args),
            "stepBackward" => await HandleStepBackwardAsync(args),

            // Frame inspection
            "getFrame" => await HandleGetFrameAsync(args),
            "getFrameRange" => await HandleGetFrameRangeAsync(args),
            "getInputs" => await HandleGetInputsAsync(args),
            "getEvents" => await HandleGetEventsAsync(args),
            "getSnapshots" => await replayController.GetSnapshotsAsync(),

            // Validation
            "validate" => await HandleValidateAsync(args),
            "checkDeterminism" => await replayController.CheckDeterminismAsync(),

            _ => throw new InvalidOperationException($"Unknown replay command: {command}")
        };
    }

    #region Recording Control Handlers

    private async Task<ReplayOperationResult> HandleStartRecordingAsync(JsonElement? args)
    {
        var name = GetOptionalString(args, "name");
        var maxFrames = GetOptionalInt(args, "maxFrames") ?? 36000;
        var snapshotIntervalMs = GetOptionalInt(args, "snapshotIntervalMs") ?? 5000;

        return await replayController.StartRecordingAsync(name, maxFrames, snapshotIntervalMs);
    }

    #endregion

    #region Recording Management Handlers

    private async Task<ReplayOperationResult> HandleSaveAsync(JsonElement? args)
    {
        var path = GetRequiredString(args, "path");
        return await replayController.SaveAsync(path);
    }

    private async Task<ReplayOperationResult> HandleLoadAsync(JsonElement? args)
    {
        var path = GetRequiredString(args, "path");
        return await replayController.LoadAsync(path);
    }

    private async Task<IReadOnlyList<ReplayFileSnapshot>> HandleListAsync(JsonElement? args)
    {
        var directory = GetOptionalString(args, "directory");
        return await replayController.ListAsync(directory);
    }

    private async Task<bool> HandleDeleteAsync(JsonElement? args)
    {
        var path = GetRequiredString(args, "path");
        return await replayController.DeleteAsync(path);
    }

    private async Task<ReplayMetadataSnapshot?> HandleGetMetadataAsync(JsonElement? args)
    {
        var path = GetRequiredString(args, "path");
        return await replayController.GetMetadataAsync(path);
    }

    #endregion

    #region Playback Control Handlers

    private async Task<ReplayOperationResult> HandleSetSpeedAsync(JsonElement? args)
    {
        var speed = GetRequiredFloat(args, "speed");
        return await replayController.SetSpeedAsync(speed);
    }

    #endregion

    #region Playback Navigation Handlers

    private async Task<ReplayOperationResult> HandleSeekFrameAsync(JsonElement? args)
    {
        var frame = GetRequiredInt(args, "frame");
        return await replayController.SeekFrameAsync(frame);
    }

    private async Task<ReplayOperationResult> HandleSeekTimeAsync(JsonElement? args)
    {
        var seconds = GetRequiredFloat(args, "seconds");
        return await replayController.SeekTimeAsync(seconds);
    }

    private async Task<ReplayOperationResult> HandleStepForwardAsync(JsonElement? args)
    {
        var frames = GetOptionalInt(args, "frames") ?? 1;
        return await replayController.StepForwardAsync(frames);
    }

    private async Task<ReplayOperationResult> HandleStepBackwardAsync(JsonElement? args)
    {
        var frames = GetOptionalInt(args, "frames") ?? 1;
        return await replayController.StepBackwardAsync(frames);
    }

    #endregion

    #region Frame Inspection Handlers

    private async Task<ReplayFrameSnapshot?> HandleGetFrameAsync(JsonElement? args)
    {
        var frame = GetRequiredInt(args, "frame");
        return await replayController.GetFrameAsync(frame);
    }

    private async Task<IReadOnlyList<ReplayFrameSnapshot>> HandleGetFrameRangeAsync(JsonElement? args)
    {
        var startFrame = GetRequiredInt(args, "startFrame");
        var count = GetRequiredInt(args, "count");
        return await replayController.GetFrameRangeAsync(startFrame, count);
    }

    private async Task<IReadOnlyList<InputEventSnapshot>> HandleGetInputsAsync(JsonElement? args)
    {
        var startFrame = GetRequiredInt(args, "startFrame");
        var endFrame = GetRequiredInt(args, "endFrame");
        return await replayController.GetInputsAsync(startFrame, endFrame);
    }

    private async Task<IReadOnlyList<ReplayEventSnapshot>> HandleGetEventsAsync(JsonElement? args)
    {
        var startFrame = GetRequiredInt(args, "startFrame");
        var endFrame = GetRequiredInt(args, "endFrame");
        return await replayController.GetEventsAsync(startFrame, endFrame);
    }

    #endregion

    #region Validation Handlers

    private async Task<ValidationResultSnapshot> HandleValidateAsync(JsonElement? args)
    {
        var path = GetRequiredString(args, "path");
        return await replayController.ValidateAsync(path);
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Argument '{name}' cannot be null");
    }

    private static string? GetOptionalString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetString();
    }

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static int? GetOptionalInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return prop.GetInt32();
    }

    private static float GetRequiredFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetSingle();
    }

    #endregion
}
