using System.Text.Json;
using KeenEyes.TestBridge.Time;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles time control commands.
/// </summary>
internal sealed class TimeCommandHandler(ITimeController timeController) : ICommandHandler
{
    public string Prefix => "time";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "getState" => await timeController.GetTimeStateAsync(),
            "pause" => await timeController.PauseAsync(),
            "resume" => await timeController.ResumeAsync(),
            "togglePause" => await timeController.TogglePauseAsync(),
            "setScale" => await HandleSetScaleAsync(args),
            "stepFrame" => await HandleStepFrameAsync(args),
            _ => throw new InvalidOperationException($"Unknown time command: {command}")
        };
    }

    private async Task<TimeStateSnapshot> HandleSetScaleAsync(JsonElement? args)
    {
        var scale = GetRequiredFloat(args, "scale");
        return await timeController.SetTimeScaleAsync(scale);
    }

    private async Task<TimeStateSnapshot> HandleStepFrameAsync(JsonElement? args)
    {
        var frames = GetOptionalInt(args, "frames") ?? 1;
        return await timeController.StepFrameAsync(frames);
    }

    #region Typed Argument Helpers (AOT-compatible)

    private static float GetRequiredFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetSingle();
    }

    private static int? GetOptionalInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetInt32();
    }

    #endregion
}
