namespace KeenEyes.TestBridge.Snapshot;

/// <summary>
/// Controller interface for creating, restoring, and comparing world state snapshots.
/// </summary>
/// <remarks>
/// <para>
/// The snapshot controller enables save/restore functionality for debugging sessions.
/// Snapshots capture the complete world state including all entities, components, and singletons.
/// </para>
/// <para>
/// Snapshots are stored in memory and can be persisted to files for later restoration.
/// Use <see cref="DiffAsync"/> and <see cref="DiffCurrentAsync"/> to compare snapshots
/// and identify state changes.
/// </para>
/// </remarks>
public interface ISnapshotController
{
    /// <summary>
    /// Creates a named in-memory snapshot of the current world state.
    /// </summary>
    /// <param name="name">The name to assign to the snapshot.</param>
    /// <returns>A result indicating success or failure with snapshot metadata.</returns>
    /// <remarks>
    /// <para>
    /// Snapshot names must be unique. Creating a snapshot with an existing name
    /// will overwrite the previous snapshot.
    /// </para>
    /// </remarks>
    Task<SnapshotResult> CreateAsync(string name);

    /// <summary>
    /// Restores the world state from a named snapshot.
    /// </summary>
    /// <param name="name">The name of the snapshot to restore.</param>
    /// <returns>A result indicating success or failure.</returns>
    /// <remarks>
    /// <para>
    /// Restoration clears the current world state and recreates all entities
    /// and components from the snapshot. Entity IDs may differ from the original.
    /// </para>
    /// </remarks>
    Task<SnapshotResult> RestoreAsync(string name);

    /// <summary>
    /// Deletes a named snapshot from memory.
    /// </summary>
    /// <param name="name">The name of the snapshot to delete.</param>
    /// <returns>True if the snapshot was deleted; false if it didn't exist.</returns>
    Task<bool> DeleteAsync(string name);

    /// <summary>
    /// Lists all available snapshots.
    /// </summary>
    /// <returns>Metadata for all stored snapshots.</returns>
    Task<IReadOnlyList<SnapshotInfo>> ListAsync();

    /// <summary>
    /// Gets metadata for a specific snapshot.
    /// </summary>
    /// <param name="name">The name of the snapshot.</param>
    /// <returns>The snapshot metadata, or null if not found.</returns>
    Task<SnapshotInfo?> GetInfoAsync(string name);

    /// <summary>
    /// Compares two named snapshots and returns the differences.
    /// </summary>
    /// <param name="name1">The first (baseline) snapshot name.</param>
    /// <param name="name2">The second snapshot name to compare.</param>
    /// <returns>A diff containing all differences between the snapshots.</returns>
    Task<SnapshotDiff> DiffAsync(string name1, string name2);

    /// <summary>
    /// Compares a named snapshot with the current world state.
    /// </summary>
    /// <param name="name">The snapshot name to compare against current state.</param>
    /// <returns>A diff containing all differences between the snapshot and current state.</returns>
    Task<SnapshotDiff> DiffCurrentAsync(string name);

    /// <summary>
    /// Saves a snapshot to a file.
    /// </summary>
    /// <param name="name">The name of the snapshot to save.</param>
    /// <param name="path">The file path to save to.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<SnapshotResult> SaveToFileAsync(string name, string path);

    /// <summary>
    /// Loads a snapshot from a file.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <param name="name">Optional name for the loaded snapshot. If null, uses the filename.</param>
    /// <returns>A result indicating success or failure with snapshot metadata.</returns>
    Task<SnapshotResult> LoadFromFileAsync(string path, string? name = null);

    /// <summary>
    /// Exports a snapshot as JSON.
    /// </summary>
    /// <param name="name">The name of the snapshot to export.</param>
    /// <returns>The snapshot data as a JSON string.</returns>
    Task<string> ExportJsonAsync(string name);

    /// <summary>
    /// Imports a snapshot from JSON.
    /// </summary>
    /// <param name="json">The JSON string containing snapshot data.</param>
    /// <param name="name">The name to assign to the imported snapshot.</param>
    /// <returns>A result indicating success or failure with snapshot metadata.</returns>
    Task<SnapshotResult> ImportJsonAsync(string json, string name);

    /// <summary>
    /// Creates a quicksave snapshot with a reserved name.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    /// <remarks>
    /// <para>
    /// The quicksave slot is a single snapshot that can be rapidly saved and restored.
    /// Only one quicksave can exist at a time.
    /// </para>
    /// </remarks>
    Task<SnapshotResult> QuickSaveAsync();

    /// <summary>
    /// Restores from the quicksave snapshot.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Task<SnapshotResult> QuickLoadAsync();
}
