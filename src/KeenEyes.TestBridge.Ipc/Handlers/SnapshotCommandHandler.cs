using System.Text.Json;
using KeenEyes.TestBridge.Snapshot;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles snapshot control commands.
/// </summary>
internal sealed class SnapshotCommandHandler(ISnapshotController snapshotController) : ICommandHandler
{
    public string Prefix => "snapshot";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "create" => await HandleCreateAsync(args),
            "restore" => await HandleRestoreAsync(args),
            "delete" => await HandleDeleteAsync(args),
            "list" => await snapshotController.ListAsync(),
            "getInfo" => await HandleGetInfoAsync(args),
            "diff" => await HandleDiffAsync(args),
            "diffCurrent" => await HandleDiffCurrentAsync(args),
            "saveToFile" => await HandleSaveToFileAsync(args),
            "loadFromFile" => await HandleLoadFromFileAsync(args),
            "exportJson" => await HandleExportJsonAsync(args),
            "importJson" => await HandleImportJsonAsync(args),
            "quickSave" => await snapshotController.QuickSaveAsync(),
            "quickLoad" => await snapshotController.QuickLoadAsync(),
            _ => throw new InvalidOperationException($"Unknown snapshot command: {command}")
        };
    }

    private async Task<SnapshotResult> HandleCreateAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await snapshotController.CreateAsync(name);
    }

    private async Task<SnapshotResult> HandleRestoreAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await snapshotController.RestoreAsync(name);
    }

    private async Task<bool> HandleDeleteAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await snapshotController.DeleteAsync(name);
    }

    private async Task<SnapshotInfo?> HandleGetInfoAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await snapshotController.GetInfoAsync(name);
    }

    private async Task<SnapshotDiff> HandleDiffAsync(JsonElement? args)
    {
        var name1 = GetRequiredString(args, "name1");
        var name2 = GetRequiredString(args, "name2");
        return await snapshotController.DiffAsync(name1, name2);
    }

    private async Task<SnapshotDiff> HandleDiffCurrentAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await snapshotController.DiffCurrentAsync(name);
    }

    private async Task<SnapshotResult> HandleSaveToFileAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        var path = GetRequiredString(args, "path");
        return await snapshotController.SaveToFileAsync(name, path);
    }

    private async Task<SnapshotResult> HandleLoadFromFileAsync(JsonElement? args)
    {
        var path = GetRequiredString(args, "path");
        var name = GetOptionalString(args, "name");
        return await snapshotController.LoadFromFileAsync(path, name);
    }

    private async Task<string> HandleExportJsonAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await snapshotController.ExportJsonAsync(name);
    }

    private async Task<SnapshotResult> HandleImportJsonAsync(JsonElement? args)
    {
        var json = GetRequiredString(args, "json");
        var name = GetRequiredString(args, "name");
        return await snapshotController.ImportJsonAsync(json, name);
    }

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

    #endregion
}
