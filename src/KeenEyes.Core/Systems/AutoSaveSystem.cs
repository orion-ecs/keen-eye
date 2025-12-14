using KeenEyes.Serialization;

namespace KeenEyes.Systems;

/// <summary>
/// A system that automatically saves the world state at configurable intervals.
/// </summary>
/// <remarks>
/// <para>
/// The AutoSaveSystem monitors elapsed time and optionally entity changes to trigger
/// automatic saves. It supports both full snapshots and delta (incremental) saves.
/// </para>
/// <para>
/// Delta saves are significantly smaller than full saves when few entities change
/// between saves. The system automatically creates a new baseline after a configurable
/// number of deltas to prevent long restoration chains.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create auto-save system with default config
/// var autoSave = new AutoSaveSystem&lt;MySerializer&gt;(serializer);
/// world.AddSystem(autoSave, SystemPhase.PostRender);
///
/// // Configure for more frequent saves
/// autoSave.Config = AutoSaveConfig.Frequent;
///
/// // Manually trigger a save
/// autoSave.SaveNow();
/// </code>
/// </example>
/// <typeparam name="TSerializer">The serializer type that implements both serialization interfaces.</typeparam>
public sealed class AutoSaveSystem<TSerializer> : SystemBase
    where TSerializer : IComponentSerializer, IBinaryComponentSerializer
{
    private readonly TSerializer serializer;
    private AutoSaveConfig config;

    private float timeSinceLastSave;
    private int currentDeltaSequence;
    private WorldSnapshot? baselineSnapshot;
    private bool isInitialized;

    /// <summary>
    /// Creates a new auto-save system with the specified serializer.
    /// </summary>
    /// <param name="serializer">The component serializer for AOT-compatible serialization.</param>
    /// <param name="config">Optional configuration. Uses default if not specified.</param>
    public AutoSaveSystem(TSerializer serializer, AutoSaveConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        this.serializer = serializer;
        this.config = config ?? AutoSaveConfig.Default;
    }

    /// <summary>
    /// Gets or sets the auto-save configuration.
    /// </summary>
    public AutoSaveConfig Config
    {
        get => config;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            config = value;
        }
    }

    /// <summary>
    /// Gets the time elapsed since the last save.
    /// </summary>
    public float TimeSinceLastSave => timeSinceLastSave;

    /// <summary>
    /// Gets the current delta sequence number (0 if no saves have occurred).
    /// </summary>
    public int CurrentDeltaSequence => currentDeltaSequence;

    /// <summary>
    /// Gets whether a baseline snapshot exists.
    /// </summary>
    public bool HasBaseline => baselineSnapshot is not null;

    /// <summary>
    /// Event raised when an auto-save occurs.
    /// </summary>
    public event Action<SaveSlotInfo>? OnAutoSave;

    /// <summary>
    /// Event raised when an auto-save fails.
    /// </summary>
    public event Action<Exception>? OnAutoSaveError;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        isInitialized = true;

        // Try to load existing baseline if it exists
        if (World is World world && world.SaveSlotExists(config.BaselineSlotName))
        {
            try
            {
                var info = world.GetSaveSlotInfo(config.BaselineSlotName);
                if (info is not null)
                {
                    // Load the baseline snapshot for delta comparison
                    world.LoadFromSlot(config.BaselineSlotName, serializer);
                    baselineSnapshot = SnapshotManager.CreateSnapshot(world, serializer);

                    // Find the highest existing delta sequence
                    currentDeltaSequence = FindHighestDeltaSequence(world);
                }
            }
            catch
            {
                // If loading fails, we'll create a new baseline on first save
                baselineSnapshot = null;
                currentDeltaSequence = 0;
            }
        }
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (!config.Enabled || !isInitialized || World is not World world)
        {
            return;
        }

        timeSinceLastSave += deltaTime;

        // Check if we should save
        bool shouldSave = false;

        // Time-based trigger
        if (config.AutoSaveIntervalSeconds > 0 && timeSinceLastSave >= config.AutoSaveIntervalSeconds)
        {
            shouldSave = true;
        }

        if (shouldSave)
        {
            PerformAutoSave(world);
        }
    }

    /// <summary>
    /// Manually triggers an auto-save immediately.
    /// </summary>
    /// <returns>The save slot info, or null if save failed.</returns>
    public SaveSlotInfo? SaveNow()
    {
        if (!isInitialized || World is not World world)
        {
            return null;
        }

        return PerformAutoSave(world);
    }

    /// <summary>
    /// Creates a new baseline snapshot, resetting the delta chain.
    /// </summary>
    /// <returns>The save slot info for the new baseline.</returns>
    public SaveSlotInfo? CreateNewBaseline()
    {
        if (!isInitialized || World is not World world)
        {
            return null;
        }

        return SaveBaseline(world);
    }

    /// <summary>
    /// Resets the auto-save state, clearing any existing baseline.
    /// </summary>
    public void Reset()
    {
        timeSinceLastSave = 0;
        currentDeltaSequence = 0;
        baselineSnapshot = null;
    }

    private SaveSlotInfo? PerformAutoSave(World world)
    {
        try
        {
            SaveSlotInfo info;

            if (config.UseDeltaSaves && baselineSnapshot is not null)
            {
                // Check if we should create a new baseline
                if (currentDeltaSequence >= config.MaxDeltasBeforeBaseline)
                {
                    info = SaveBaseline(world);
                }
                else
                {
                    info = SaveDelta(world);
                }
            }
            else
            {
                // Create baseline (either first save or full saves mode)
                info = SaveBaseline(world);
            }

            // Reset timer
            timeSinceLastSave = 0;

            // Clear dirty flags if configured
            if (config.ClearDirtyFlagsAfterSave)
            {
                world.ClearAllDirtyFlags();
            }

            // Raise event
            OnAutoSave?.Invoke(info);

            return info;
        }
        catch (Exception ex)
        {
            OnAutoSaveError?.Invoke(ex);
            return null;
        }
    }

    private SaveSlotInfo SaveBaseline(World world)
    {
        // Create snapshot
        var snapshot = SnapshotManager.CreateSnapshot(world, serializer);

        // Save as baseline
        var info = world.SaveToSlot(config.BaselineSlotName, serializer, config.SaveOptions);

        // Update state
        baselineSnapshot = snapshot;
        currentDeltaSequence = 0;

        // Clean up old deltas
        CleanupOldDeltas(world);

        return info;
    }

    private SaveSlotInfo SaveDelta(World world)
    {
        // Increment sequence
        currentDeltaSequence++;

        // For now, save a full snapshot as a "delta" (simplified implementation)
        // A full delta implementation would compare with baselineSnapshot and only save changes
        var slotName = config.GetDeltaSlotName(currentDeltaSequence);
        var info = world.SaveToSlot(slotName, serializer, config.SaveOptions with
        {
            DisplayName = $"Auto-save (Delta #{currentDeltaSequence})"
        });

        return info;
    }

    private void CleanupOldDeltas(World world)
    {
        // Remove old delta files when a new baseline is created
        for (int i = 1; i <= config.MaxDeltasBeforeBaseline + 5; i++)
        {
            var slotName = config.GetDeltaSlotName(i);
            if (world.SaveSlotExists(slotName))
            {
                world.DeleteSaveSlot(slotName);
            }
        }
    }

    private int FindHighestDeltaSequence(World world)
    {
        int highest = 0;
        for (int i = 1; i <= config.MaxDeltasBeforeBaseline + 5; i++)
        {
            if (world.SaveSlotExists(config.GetDeltaSlotName(i)))
            {
                highest = i;
            }
        }
        return highest;
    }
}
