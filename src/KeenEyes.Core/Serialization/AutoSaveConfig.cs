namespace KeenEyes.Serialization;

/// <summary>
/// Configuration options for the auto-save system.
/// </summary>
/// <remarks>
/// <para>
/// Auto-save can be triggered by time intervals, change thresholds, or both.
/// When using delta saves, a new baseline is created after a configurable
/// number of deltas to prevent long restoration chains.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var config = new AutoSaveConfig
/// {
///     AutoSaveIntervalSeconds = 300, // 5 minutes
///     UseDeltaSaves = true,
///     MaxDeltasBeforeBaseline = 10
/// };
/// world.InstallAutoSave(serializer, config);
/// </code>
/// </example>
public sealed record AutoSaveConfig
{
    /// <summary>
    /// Gets or sets the interval in seconds between auto-saves.
    /// </summary>
    /// <remarks>
    /// Set to 0 or negative to disable time-based auto-save.
    /// Default is 300 seconds (5 minutes).
    /// </remarks>
    public float AutoSaveIntervalSeconds { get; init; } = 300f;

    /// <summary>
    /// Gets or sets the number of entity changes that trigger an auto-save.
    /// </summary>
    /// <remarks>
    /// Set to 0 or negative to disable change-based auto-save.
    /// Default is 1000 changes.
    /// </remarks>
    public int ChangeThreshold { get; init; } = 1000;

    /// <summary>
    /// Gets or sets whether to use delta (incremental) saves.
    /// </summary>
    /// <remarks>
    /// When true, only changes since the last save are stored.
    /// When false, a full snapshot is saved each time.
    /// Default is true.
    /// </remarks>
    public bool UseDeltaSaves { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum number of delta saves before creating a new baseline.
    /// </summary>
    /// <remarks>
    /// After this many deltas, a full baseline snapshot is created to prevent
    /// long restoration chains. Default is 10.
    /// </remarks>
    public int MaxDeltasBeforeBaseline { get; init; } = 10;

    /// <summary>
    /// Gets or sets the base slot name for auto-saves.
    /// </summary>
    /// <remarks>
    /// The baseline will be saved as "{BaseSlotName}_baseline" and deltas as
    /// "{BaseSlotName}_delta_{N}". Default is "autosave".
    /// </remarks>
    public string BaseSlotName { get; init; } = "autosave";

    /// <summary>
    /// Gets or sets whether auto-save is enabled.
    /// </summary>
    /// <remarks>
    /// Can be toggled at runtime to pause/resume auto-saving.
    /// Default is true.
    /// </remarks>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets options for the save operation.
    /// </summary>
    /// <remarks>
    /// Controls compression, checksums, and other save options.
    /// Uses <see cref="SaveSlotOptions.Fast"/> by default for auto-saves.
    /// </remarks>
    public SaveSlotOptions SaveOptions { get; init; } = SaveSlotOptions.Fast;

    /// <summary>
    /// Gets or sets whether to clear dirty flags after saving.
    /// </summary>
    /// <remarks>
    /// When true, dirty flags are cleared after each save so the next delta
    /// only captures changes since the last save. Default is true.
    /// </remarks>
    public bool ClearDirtyFlagsAfterSave { get; init; } = true;

    /// <summary>
    /// Gets the slot name for the baseline snapshot.
    /// </summary>
    public string BaselineSlotName => $"{BaseSlotName}_baseline";

    /// <summary>
    /// Gets the slot name for a delta snapshot at the given sequence number.
    /// </summary>
    /// <param name="sequenceNumber">The delta sequence number.</param>
    /// <returns>The slot name for the delta.</returns>
    public string GetDeltaSlotName(int sequenceNumber) => $"{BaseSlotName}_delta_{sequenceNumber}";

    /// <summary>
    /// Gets the default configuration optimized for most games.
    /// </summary>
    public static AutoSaveConfig Default { get; } = new();

    /// <summary>
    /// Gets configuration for frequent saves with minimal overhead.
    /// </summary>
    public static AutoSaveConfig Frequent { get; } = new()
    {
        AutoSaveIntervalSeconds = 60f,
        ChangeThreshold = 500,
        UseDeltaSaves = true,
        MaxDeltasBeforeBaseline = 20
    };

    /// <summary>
    /// Gets configuration for rare saves with full snapshots.
    /// </summary>
    public static AutoSaveConfig Infrequent { get; } = new()
    {
        AutoSaveIntervalSeconds = 600f,
        ChangeThreshold = -1,
        UseDeltaSaves = false
    };
}
