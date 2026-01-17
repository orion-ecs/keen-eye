using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Snapshot;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for world state snapshot management.
/// </summary>
/// <remarks>
/// <para>
/// These tools allow creating, restoring, and comparing world state snapshots
/// for debugging and testing. Snapshots capture the complete state of all
/// entities and components at a point in time.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class SnapshotTools(BridgeConnectionManager connection)
{
    #region Create/Restore

    [McpServerTool(Name = "snapshot_create")]
    [Description("Create a named in-memory snapshot of the current world state.")]
    public async Task<SnapshotOperationResult> Create(
        [Description("Unique name for this snapshot")]
        string name)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Snapshot.CreateAsync(name);
        return SnapshotOperationResult.FromResult(result);
    }

    [McpServerTool(Name = "snapshot_restore")]
    [Description("Restore the world state from a named snapshot. This clears the current world and recreates all entities.")]
    public async Task<SnapshotOperationResult> Restore(
        [Description("Name of the snapshot to restore")]
        string name)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Snapshot.RestoreAsync(name);
        return SnapshotOperationResult.FromResult(result);
    }

    [McpServerTool(Name = "snapshot_delete")]
    [Description("Delete a named snapshot from memory.")]
    public async Task<SnapshotDeleteResult> Delete(
        [Description("Name of the snapshot to delete")]
        string name)
    {
        var bridge = connection.GetBridge();
        var deleted = await bridge.Snapshot.DeleteAsync(name);
        return new SnapshotDeleteResult
        {
            Success = deleted,
            SnapshotName = name,
            Message = deleted ? $"Snapshot '{name}' deleted" : $"Snapshot '{name}' not found"
        };
    }

    #endregion

    #region List/Info

    [McpServerTool(Name = "snapshot_list")]
    [Description("List all available snapshots with their metadata.")]
    public async Task<SnapshotListResult> List()
    {
        var bridge = connection.GetBridge();
        var snapshots = await bridge.Snapshot.ListAsync();
        return new SnapshotListResult
        {
            Count = snapshots.Count,
            Snapshots = snapshots.Select(SnapshotInfoResult.FromInfo).ToList()
        };
    }

    [McpServerTool(Name = "snapshot_get_info")]
    [Description("Get detailed information about a specific snapshot.")]
    public async Task<SnapshotInfoResult?> GetInfo(
        [Description("Name of the snapshot")]
        string name)
    {
        var bridge = connection.GetBridge();
        var info = await bridge.Snapshot.GetInfoAsync(name);
        return info != null ? SnapshotInfoResult.FromInfo(info) : null;
    }

    #endregion

    #region Diff

    [McpServerTool(Name = "snapshot_diff")]
    [Description("Compare two named snapshots and show the differences.")]
    public async Task<SnapshotDiffResult> Diff(
        [Description("Name of the first (baseline) snapshot")]
        string name1,
        [Description("Name of the second snapshot to compare")]
        string name2)
    {
        var bridge = connection.GetBridge();
        var diff = await bridge.Snapshot.DiffAsync(name1, name2);
        return SnapshotDiffResult.FromDiff(diff);
    }

    [McpServerTool(Name = "snapshot_diff_current")]
    [Description("Compare a snapshot with the current world state.")]
    public async Task<SnapshotDiffResult> DiffCurrent(
        [Description("Name of the snapshot to compare against current state")]
        string name)
    {
        var bridge = connection.GetBridge();
        var diff = await bridge.Snapshot.DiffCurrentAsync(name);
        return SnapshotDiffResult.FromDiff(diff);
    }

    #endregion

    #region File Operations

    [McpServerTool(Name = "snapshot_save_file")]
    [Description("Save a snapshot to a file on disk.")]
    public async Task<SnapshotOperationResult> SaveToFile(
        [Description("Name of the snapshot to save")]
        string name,
        [Description("File path to save the snapshot to")]
        string path)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Snapshot.SaveToFileAsync(name, path);
        return SnapshotOperationResult.FromResult(result);
    }

    [McpServerTool(Name = "snapshot_load_file")]
    [Description("Load a snapshot from a file on disk.")]
    public async Task<SnapshotOperationResult> LoadFromFile(
        [Description("File path to load the snapshot from")]
        string path,
        [Description("Optional name for the loaded snapshot (defaults to filename)")]
        string? name = null)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Snapshot.LoadFromFileAsync(path, name);
        return SnapshotOperationResult.FromResult(result);
    }

    #endregion

    #region Quick Save/Load

    [McpServerTool(Name = "quicksave")]
    [Description("Create a quicksave snapshot for rapid iteration. Only one quicksave can exist at a time.")]
    public async Task<SnapshotOperationResult> QuickSave()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Snapshot.QuickSaveAsync();
        return SnapshotOperationResult.FromResult(result);
    }

    [McpServerTool(Name = "quickload")]
    [Description("Restore from the quicksave snapshot.")]
    public async Task<SnapshotOperationResult> QuickLoad()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Snapshot.QuickLoadAsync();
        return SnapshotOperationResult.FromResult(result);
    }

    #endregion

    #region Export/Import

    [McpServerTool(Name = "snapshot_export_json")]
    [Description("Export a snapshot as a JSON string.")]
    public async Task<SnapshotExportResult> ExportJson(
        [Description("Name of the snapshot to export")]
        string name)
    {
        var bridge = connection.GetBridge();
        var json = await bridge.Snapshot.ExportJsonAsync(name);
        return new SnapshotExportResult
        {
            Success = !string.IsNullOrEmpty(json),
            SnapshotName = name,
            Json = json,
            Error = string.IsNullOrEmpty(json) ? $"Snapshot '{name}' not found or empty" : null
        };
    }

    [McpServerTool(Name = "snapshot_import_json")]
    [Description("Import a snapshot from a JSON string.")]
    public async Task<SnapshotOperationResult> ImportJson(
        [Description("JSON string containing snapshot data")]
        string json,
        [Description("Name to assign to the imported snapshot")]
        string name)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Snapshot.ImportJsonAsync(json, name);
        return SnapshotOperationResult.FromResult(result);
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result of a snapshot operation (create, restore, etc.).
/// </summary>
public sealed record SnapshotOperationResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the name of the snapshot involved.
    /// </summary>
    public string? SnapshotName { get; init; }

    /// <summary>
    /// Gets snapshot metadata if available.
    /// </summary>
    public SnapshotInfoResult? Info { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a result from a SnapshotResult.
    /// </summary>
    public static SnapshotOperationResult FromResult(SnapshotResult result)
    {
        return new SnapshotOperationResult
        {
            Success = result.Success,
            SnapshotName = result.SnapshotName,
            Info = result.Info != null ? SnapshotInfoResult.FromInfo(result.Info) : null,
            Error = result.Error
        };
    }
}

/// <summary>
/// Result of a snapshot delete operation.
/// </summary>
public sealed record SnapshotDeleteResult
{
    /// <summary>
    /// Gets whether the delete was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the name of the snapshot that was deleted.
    /// </summary>
    public required string SnapshotName { get; init; }

    /// <summary>
    /// Gets a message describing the result.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Result of a snapshot list operation.
/// </summary>
public sealed record SnapshotListResult
{
    /// <summary>
    /// Gets the number of snapshots available.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets metadata for all available snapshots.
    /// </summary>
    public required List<SnapshotInfoResult> Snapshots { get; init; }
}

/// <summary>
/// Snapshot metadata for MCP results.
/// </summary>
public sealed record SnapshotInfoResult
{
    /// <summary>
    /// Gets the snapshot name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets when the snapshot was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the number of entities in the snapshot.
    /// </summary>
    public required int EntityCount { get; init; }

    /// <summary>
    /// Gets the total number of components.
    /// </summary>
    public required int ComponentCount { get; init; }

    /// <summary>
    /// Gets the approximate size in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Gets whether this is the quicksave slot.
    /// </summary>
    public bool IsQuickSave { get; init; }

    /// <summary>
    /// Creates from a SnapshotInfo.
    /// </summary>
    public static SnapshotInfoResult FromInfo(SnapshotInfo info)
    {
        return new SnapshotInfoResult
        {
            Name = info.Name,
            CreatedAt = info.CreatedAt,
            EntityCount = info.EntityCount,
            ComponentCount = info.ComponentCount,
            SizeBytes = info.SizeBytes,
            IsQuickSave = info.IsQuickSave
        };
    }
}

/// <summary>
/// Result of a snapshot diff operation.
/// </summary>
public sealed record SnapshotDiffResult
{
    /// <summary>
    /// Gets the first snapshot name.
    /// </summary>
    public required string Snapshot1 { get; init; }

    /// <summary>
    /// Gets the second snapshot name.
    /// </summary>
    public required string Snapshot2 { get; init; }

    /// <summary>
    /// Gets whether the snapshots are identical.
    /// </summary>
    public required bool AreEqual { get; init; }

    /// <summary>
    /// Gets the total number of changes.
    /// </summary>
    public required int TotalChanges { get; init; }

    /// <summary>
    /// Gets the number of entities added.
    /// </summary>
    public required int AddedCount { get; init; }

    /// <summary>
    /// Gets the number of entities removed.
    /// </summary>
    public required int RemovedCount { get; init; }

    /// <summary>
    /// Gets the number of entities modified.
    /// </summary>
    public required int ModifiedCount { get; init; }

    /// <summary>
    /// Gets entity diff details for added entities.
    /// </summary>
    public required List<EntityDiffResult> AddedEntities { get; init; }

    /// <summary>
    /// Gets entity diff details for removed entities.
    /// </summary>
    public required List<EntityDiffResult> RemovedEntities { get; init; }

    /// <summary>
    /// Gets entity diff details for modified entities.
    /// </summary>
    public required List<EntityDiffResult> ModifiedEntities { get; init; }

    /// <summary>
    /// Creates from a SnapshotDiff.
    /// </summary>
    public static SnapshotDiffResult FromDiff(SnapshotDiff diff)
    {
        return new SnapshotDiffResult
        {
            Snapshot1 = diff.Snapshot1,
            Snapshot2 = diff.Snapshot2,
            AreEqual = diff.AreEqual,
            TotalChanges = diff.TotalChanges,
            AddedCount = diff.AddedEntities.Count,
            RemovedCount = diff.RemovedEntities.Count,
            ModifiedCount = diff.ModifiedEntities.Count,
            AddedEntities = diff.AddedEntities.Select(EntityDiffResult.FromDiff).ToList(),
            RemovedEntities = diff.RemovedEntities.Select(EntityDiffResult.FromDiff).ToList(),
            ModifiedEntities = diff.ModifiedEntities.Select(EntityDiffResult.FromDiff).ToList()
        };
    }
}

/// <summary>
/// Entity diff details for MCP results.
/// </summary>
public sealed record EntityDiffResult
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the entity name, if any.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the type of change (Added, Removed, Modified).
    /// </summary>
    public required string ChangeType { get; init; }

    /// <summary>
    /// Gets components that were added.
    /// </summary>
    public List<string>? AddedComponents { get; init; }

    /// <summary>
    /// Gets components that were removed.
    /// </summary>
    public List<string>? RemovedComponents { get; init; }

    /// <summary>
    /// Gets components that were modified.
    /// </summary>
    public List<ComponentDiffResult>? ModifiedComponents { get; init; }

    /// <summary>
    /// Creates from an EntityDiff.
    /// </summary>
    public static EntityDiffResult FromDiff(EntityDiff diff)
    {
        return new EntityDiffResult
        {
            EntityId = diff.EntityId,
            Name = diff.Name,
            ChangeType = diff.ChangeType,
            AddedComponents = diff.AddedComponents?.ToList(),
            RemovedComponents = diff.RemovedComponents?.ToList(),
            ModifiedComponents = diff.ModifiedComponents?.Select(ComponentDiffResult.FromDiff).ToList()
        };
    }
}

/// <summary>
/// Component diff details for MCP results.
/// </summary>
public sealed record ComponentDiffResult
{
    /// <summary>
    /// Gets the component type name.
    /// </summary>
    public required string ComponentType { get; init; }

    /// <summary>
    /// Gets the field changes.
    /// </summary>
    public required List<FieldDiffResult> Fields { get; init; }

    /// <summary>
    /// Creates from a ComponentDiff.
    /// </summary>
    public static ComponentDiffResult FromDiff(ComponentDiff diff)
    {
        return new ComponentDiffResult
        {
            ComponentType = diff.ComponentType,
            Fields = diff.Fields.Select(FieldDiffResult.FromDiff).ToList()
        };
    }
}

/// <summary>
/// Field diff details for MCP results.
/// </summary>
public sealed record FieldDiffResult
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Gets the old value.
    /// </summary>
    public required string OldValue { get; init; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public required string NewValue { get; init; }

    /// <summary>
    /// Creates from a FieldDiff.
    /// </summary>
    public static FieldDiffResult FromDiff(FieldDiff diff)
    {
        return new FieldDiffResult
        {
            FieldName = diff.FieldName,
            OldValue = diff.OldValue,
            NewValue = diff.NewValue
        };
    }
}

/// <summary>
/// Result of a snapshot export operation.
/// </summary>
public sealed record SnapshotExportResult
{
    /// <summary>
    /// Gets whether the export was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the snapshot name that was exported.
    /// </summary>
    public required string SnapshotName { get; init; }

    /// <summary>
    /// Gets the exported JSON string.
    /// </summary>
    public string? Json { get; init; }

    /// <summary>
    /// Gets an error message if the export failed.
    /// </summary>
    public string? Error { get; init; }
}

#endregion
