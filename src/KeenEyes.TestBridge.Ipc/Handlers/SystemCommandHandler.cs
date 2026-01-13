using System.Text.Json;
using KeenEyes.TestBridge.Systems;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles system control commands.
/// </summary>
internal sealed class SystemCommandHandler(ISystemController systemController) : ICommandHandler
{
    public string Prefix => "system";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "list" => await systemController.GetSystemsAsync(),
            "getCount" => await systemController.GetCountAsync(),
            "get" => await HandleGetAsync(args),
            "enable" => await HandleEnableAsync(args),
            "disable" => await HandleDisableAsync(args),
            "toggle" => await HandleToggleAsync(args),
            "getByPhase" => await HandleGetByPhaseAsync(args),
            "getEnabled" => await systemController.GetEnabledSystemsAsync(),
            "getDisabled" => await systemController.GetDisabledSystemsAsync(),
            _ => throw new InvalidOperationException($"Unknown system command: {command}")
        };
    }

    private async Task<SystemSnapshot?> HandleGetAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await systemController.GetSystemAsync(name);
    }

    private async Task<SystemSnapshot> HandleEnableAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await systemController.EnableSystemAsync(name);
    }

    private async Task<SystemSnapshot> HandleDisableAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await systemController.DisableSystemAsync(name);
    }

    private async Task<SystemSnapshot> HandleToggleAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await systemController.ToggleSystemAsync(name);
    }

    private async Task<IReadOnlyList<SystemSnapshot>> HandleGetByPhaseAsync(JsonElement? args)
    {
        var phase = GetRequiredString(args, "phase");
        return await systemController.GetSystemsByPhaseAsync(phase);
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

    #endregion
}
