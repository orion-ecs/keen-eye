using KeenEyes.Capabilities;
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
/// <para>
/// This system requires the world to implement <see cref="ISaveLoadCapability"/>.
/// The standard <see cref="World"/> class provides this capability.
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
        if (World is ISaveLoadCapability saveLoad && saveLoad.SaveSlotExists(config.BaselineSlotName))
        {
            try
            {
                var info = saveLoad.GetSaveSlotInfo(config.BaselineSlotName);
                if (info is not null)
                {
                    // Load the baseline snapshot for delta comparison
                    saveLoad.LoadFromSlot(config.BaselineSlotName, serializer);
                    baselineSnapshot = saveLoad.CreateSnapshot(serializer);

                    // Find the highest existing delta sequence
                    currentDeltaSequence = FindHighestDeltaSequence(saveLoad);
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
        if (!config.Enabled || !isInitialized || World is not ISaveLoadCapability saveLoad)
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
            PerformAutoSave(saveLoad);
        }
    }

    /// <summary>
    /// Manually triggers an auto-save immediately.
    /// </summary>
    /// <returns>The save slot info, or null if save failed.</returns>
    public SaveSlotInfo? SaveNow()
    {
        if (!isInitialized || World is not ISaveLoadCapability saveLoad)
        {
            return null;
        }

        return PerformAutoSave(saveLoad);
    }

    /// <summary>
    /// Creates a new baseline snapshot, resetting the delta chain.
    /// </summary>
    /// <returns>The save slot info for the new baseline.</returns>
    public SaveSlotInfo? CreateNewBaseline()
    {
        if (!isInitialized || World is not ISaveLoadCapability saveLoad)
        {
            return null;
        }

        return SaveBaseline(saveLoad);
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

    private SaveSlotInfo? PerformAutoSave(ISaveLoadCapability saveLoad)
    {
        try
        {
            SaveSlotInfo info;

            if (config.UseDeltaSaves && baselineSnapshot is not null)
            {
                // Check if we should create a new baseline
                if (currentDeltaSequence >= config.MaxDeltasBeforeBaseline)
                {
                    info = SaveBaseline(saveLoad);
                }
                else
                {
                    info = SaveDelta(saveLoad);
                }
            }
            else
            {
                // Create baseline (either first save or full saves mode)
                info = SaveBaseline(saveLoad);
            }

            // Reset timer
            timeSinceLastSave = 0;

            // Clear dirty flags if configured
            if (config.ClearDirtyFlagsAfterSave)
            {
                saveLoad.ClearAllDirtyFlags();
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

    private SaveSlotInfo SaveBaseline(ISaveLoadCapability saveLoad)
    {
        // Create snapshot
        var snapshot = saveLoad.CreateSnapshot(serializer);

        // Save as baseline
        var info = saveLoad.SaveToSlot(config.BaselineSlotName, serializer, config.SaveOptions);

        // Update state
        baselineSnapshot = snapshot;
        currentDeltaSequence = 0;

        // Clean up old deltas
        CleanupOldDeltas(saveLoad);

        return info;
    }

    private SaveSlotInfo SaveDelta(ISaveLoadCapability saveLoad)
    {
        // Increment sequence
        currentDeltaSequence++;

        // Create true delta by comparing current state to baseline
        var delta = saveLoad.CreateDelta(
            baselineSnapshot!,
            serializer,
            config.BaselineSlotName,
            currentDeltaSequence);

        // If delta is empty, skip saving
        if (delta.IsEmpty)
        {
            currentDeltaSequence--;
            // Return a placeholder info for the baseline
            return saveLoad.GetSaveSlotInfo(config.BaselineSlotName)
                ?? throw new InvalidOperationException("Baseline slot not found");
        }

        // Save delta to slot
        var slotName = config.GetDeltaSlotName(currentDeltaSequence);
        var info = saveLoad.SaveDeltaToSlot(slotName, delta, serializer, config.SaveOptions with
        {
            DisplayName = $"Auto-save (Delta #{currentDeltaSequence})"
        });

        return info;
    }

    private void CleanupOldDeltas(ISaveLoadCapability saveLoad)
    {
        // Remove old delta files when a new baseline is created
        for (int i = 1; i <= config.MaxDeltasBeforeBaseline + 5; i++)
        {
            var slotName = config.GetDeltaSlotName(i);
            if (saveLoad.SaveSlotExists(slotName))
            {
                saveLoad.DeleteSaveSlot(slotName);
            }
        }
    }

    private int FindHighestDeltaSequence(ISaveLoadCapability saveLoad)
    {
        int highest = 0;
        for (int i = 1; i <= config.MaxDeltasBeforeBaseline + 5; i++)
        {
            if (saveLoad.SaveSlotExists(config.GetDeltaSlotName(i)))
            {
                highest = i;
            }
        }
        return highest;
    }
}
