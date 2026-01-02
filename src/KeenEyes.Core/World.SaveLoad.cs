using KeenEyes.Serialization;

namespace KeenEyes;

/// <summary>
/// Provides save/load functionality for the world.
/// </summary>
public sealed partial class World
{
    /// <summary>
    /// Gets or sets the directory where save files are stored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to a "saves" subdirectory in the current working directory.
    /// </para>
    /// <para>
    /// The directory is created automatically when the first save is performed.
    /// </para>
    /// </remarks>
    public string SaveDirectory
    {
        get => GetSaveManager().SaveDirectory;
        set => GetSaveManager().SaveDirectory = value;
    }

    /// <summary>
    /// Saves the current world state to a slot.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements both <see cref="IComponentSerializer"/>
    /// and <see cref="IBinaryComponentSerializer"/>.
    /// </typeparam>
    /// <param name="slotName">
    /// The name of the save slot. Must be a valid filename (alphanumeric, underscores, hyphens).
    /// </param>
    /// <param name="serializer">
    /// The component serializer for AOT-compatible serialization. Pass the generated
    /// ComponentSerializer from your project.
    /// </param>
    /// <param name="options">
    /// Optional save options for compression, checksum, and metadata. Uses default if not specified.
    /// </param>
    /// <returns>The save slot info with updated metadata including sizes and checksum.</returns>
    /// <exception cref="ArgumentException">Thrown when slotName is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when serializer is null.</exception>
    /// <example>
    /// <code>
    /// // Basic save
    /// var info = world.SaveToSlot("slot1", serializer);
    ///
    /// // Save with options
    /// var options = new SaveSlotOptions
    /// {
    ///     DisplayName = "Chapter 3 - The Forest",
    ///     PlayTime = TimeSpan.FromHours(2.5),
    ///     Compression = CompressionMode.Brotli
    /// };
    /// var info = world.SaveToSlot("slot1", serializer, options);
    /// </code>
    /// </example>
    public SaveSlotInfo SaveToSlot<TSerializer>(
        string slotName,
        TSerializer serializer,
        SaveSlotOptions? options = null)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        return GetSaveManager().Save(slotName, serializer, options);
    }

    /// <summary>
    /// Loads a world state from a save slot.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements both <see cref="IComponentSerializer"/>
    /// and <see cref="IBinaryComponentSerializer"/>.
    /// </typeparam>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="serializer">
    /// The component serializer for AOT-compatible deserialization. Pass the generated
    /// ComponentSerializer from your project.
    /// </param>
    /// <param name="validateChecksum">
    /// Whether to validate the checksum if present. Defaults to true.
    /// </param>
    /// <returns>
    /// A tuple containing the save slot info and a mapping from original entity IDs to new entities.
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown when the save slot doesn't exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the save file is corrupted.</exception>
    /// <remarks>
    /// <para>
    /// Loading a slot clears the current world state before restoring the saved state.
    /// Entity IDs in the restored world may differ from the original saved IDs.
    /// Use the returned entity map to translate original IDs to new entities.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var (info, entityMap) = world.LoadFromSlot("slot1", serializer);
    ///
    /// // Use entityMap to find restored entities by their original IDs
    /// if (entityMap.TryGetValue(originalPlayerId, out var player))
    /// {
    ///     // player is the restored entity
    /// }
    /// </code>
    /// </example>
    public (SaveSlotInfo SlotInfo, Dictionary<int, Entity> EntityMap) LoadFromSlot<TSerializer>(
        string slotName,
        TSerializer serializer,
        bool validateChecksum = true)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        return GetSaveManager().Load(slotName, serializer, validateChecksum);
    }

    /// <summary>
    /// Gets information about a save slot without loading it.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <returns>The save slot info, or null if the slot doesn't exist.</returns>
    /// <remarks>
    /// This is useful for displaying save slot information in a UI without
    /// the overhead of loading the full world snapshot.
    /// </remarks>
    /// <example>
    /// <code>
    /// var info = world.GetSaveSlotInfo("slot1");
    /// if (info != null)
    /// {
    ///     Console.WriteLine($"Last saved: {info.ModifiedAt}");
    ///     Console.WriteLine($"Play time: {info.PlayTime}");
    ///     Console.WriteLine($"Entities: {info.EntityCount}");
    /// }
    /// </code>
    /// </example>
    public SaveSlotInfo? GetSaveSlotInfo(string slotName)
    {
        return GetSaveManager().TryGetSlotInfo(slotName);
    }

    /// <summary>
    /// Lists all available save slots.
    /// </summary>
    /// <returns>An enumerable of save slot info for all valid save files.</returns>
    /// <remarks>
    /// Invalid or corrupted save files are silently skipped.
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var slot in world.ListSaveSlots())
    /// {
    ///     Console.WriteLine($"{slot.SlotName}: {slot.DisplayName ?? slot.SlotName}");
    ///     Console.WriteLine($"  Modified: {slot.ModifiedAt}");
    ///     Console.WriteLine($"  Play time: {slot.PlayTime}");
    /// }
    /// </code>
    /// </example>
    public IEnumerable<SaveSlotInfo> ListSaveSlots()
    {
        return GetSaveManager().ListSlots();
    }

    /// <summary>
    /// Checks if a save slot exists.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <returns>True if the slot exists and is valid.</returns>
    public bool SaveSlotExists(string slotName)
    {
        return GetSaveManager().SlotExists(slotName);
    }

    /// <summary>
    /// Deletes a save slot.
    /// </summary>
    /// <param name="slotName">The name of the save slot to delete.</param>
    /// <returns>True if the slot was deleted; false if it didn't exist.</returns>
    public bool DeleteSaveSlot(string slotName)
    {
        return GetSaveManager().DeleteSlot(slotName);
    }

    /// <summary>
    /// Copies a save slot to a new name.
    /// </summary>
    /// <param name="sourceSlotName">The source slot name.</param>
    /// <param name="destinationSlotName">The destination slot name.</param>
    /// <param name="overwrite">Whether to overwrite if destination exists.</param>
    /// <returns>The save slot info for the copied slot.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the source slot doesn't exist.</exception>
    /// <exception cref="IOException">Thrown when destination exists and overwrite is false.</exception>
    public SaveSlotInfo CopySaveSlot(string sourceSlotName, string destinationSlotName, bool overwrite = false)
    {
        return GetSaveManager().CopySlot(sourceSlotName, destinationSlotName, overwrite);
    }

    /// <summary>
    /// Renames a save slot.
    /// </summary>
    /// <param name="oldSlotName">The current slot name.</param>
    /// <param name="newSlotName">The new slot name.</param>
    /// <returns>The save slot info with the new name.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the source slot doesn't exist.</exception>
    /// <exception cref="IOException">Thrown when the new name already exists.</exception>
    public SaveSlotInfo RenameSaveSlot(string oldSlotName, string newSlotName)
    {
        return GetSaveManager().RenameSlot(oldSlotName, newSlotName);
    }

    /// <summary>
    /// Validates a save slot's integrity.
    /// </summary>
    /// <param name="slotName">The name of the save slot to validate.</param>
    /// <returns>
    /// The validation result with any errors. Returns null if the file doesn't exist or is completely invalid.
    /// Check <see cref="SaveSlotInfo.IsValid"/> and <see cref="SaveSlotInfo.ValidationError"/> for details.
    /// </returns>
    public SaveSlotInfo? ValidateSaveSlot(string slotName)
    {
        return GetSaveManager().ValidateSlot(slotName);
    }

    /// <summary>
    /// Gets the file path for a save slot.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <returns>The full file path where the save would be stored.</returns>
    public string GetSaveSlotPath(string slotName)
    {
        return GetSaveManager().GetSlotFilePath(slotName);
    }

    /// <summary>
    /// Gets or creates the save manager for this world.
    /// </summary>
    private SaveManager GetSaveManager()
    {
        return saveManager ??= new SaveManager(this);
    }

    #region Delta Save Operations

    /// <summary>
    /// Saves a delta snapshot to a slot.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements both <see cref="IComponentSerializer"/>
    /// and <see cref="IBinaryComponentSerializer"/>.
    /// </typeparam>
    /// <param name="slotName">
    /// The name of the save slot. Must be a valid filename.
    /// </param>
    /// <param name="delta">The delta snapshot to save.</param>
    /// <param name="serializer">
    /// The component serializer for AOT-compatible serialization.
    /// </param>
    /// <param name="options">
    /// Optional save options. Uses default if not specified.
    /// </param>
    /// <returns>The save slot info with updated metadata.</returns>
    /// <remarks>
    /// <para>
    /// Delta saves contain only the changes since a baseline snapshot, making them
    /// significantly smaller than full saves when few entities have changed.
    /// </para>
    /// <para>
    /// To restore from a delta save, first load the baseline, then apply
    /// each delta in sequence using <see cref="LoadDeltaFromSlot"/>.
    /// </para>
    /// </remarks>
    /// <inheritdoc />
    public SaveSlotInfo SaveDeltaToSlot<TSerializer>(
        string slotName,
        DeltaSnapshot delta,
        TSerializer serializer,
        SaveSlotOptions? options = null)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        return GetSaveManager().SaveDelta(slotName, delta, serializer, options);
    }

    /// <summary>
    /// Loads a delta snapshot from a slot.
    /// </summary>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>The delta snapshot.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the save slot doesn't exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the save file is corrupted or not a delta.</exception>
    /// <remarks>
    /// <para>
    /// After loading a delta, apply it to a world that has been restored from
    /// the baseline using <see cref="DeltaRestorer.ApplyDelta"/>.
    /// </para>
    /// </remarks>
    internal DeltaSnapshot LoadDeltaFromSlot(string slotName, bool validateChecksum = true)
    {
        return GetSaveManager().LoadDelta(slotName, validateChecksum);
    }

    #endregion

    #region Snapshot Operations

    /// <summary>
    /// Creates a snapshot of the current world state.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements <see cref="IComponentSerializer"/>.
    /// </typeparam>
    /// <param name="serializer">The component serializer for AOT-compatible serialization.</param>
    /// <returns>A snapshot containing all entities, components, hierarchy, and singletons.</returns>
    public WorldSnapshot CreateSnapshot<TSerializer>(TSerializer serializer)
        where TSerializer : IComponentSerializer
    {
        return SnapshotManager.CreateSnapshot(this, serializer);
    }

    /// <summary>
    /// Creates a delta snapshot by comparing the current world state to a baseline.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements <see cref="IComponentSerializer"/>.
    /// </typeparam>
    /// <param name="baseline">The baseline snapshot to compare against.</param>
    /// <param name="serializer">The component serializer.</param>
    /// <param name="baselineSlotName">The slot name of the baseline snapshot.</param>
    /// <param name="sequenceNumber">The sequence number for this delta.</param>
    /// <returns>A delta snapshot containing only the changes since the baseline.</returns>
    public DeltaSnapshot CreateDelta<TSerializer>(
        WorldSnapshot baseline,
        TSerializer serializer,
        string baselineSlotName,
        int sequenceNumber)
        where TSerializer : IComponentSerializer
    {
        return DeltaDiffer.CreateDelta(this, baseline, serializer, baselineSlotName, sequenceNumber);
    }

    #endregion

    #region Async Save/Load Operations

    /// <summary>
    /// Saves the current world state to a slot asynchronously.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements both <see cref="IComponentSerializer"/>
    /// and <see cref="IBinaryComponentSerializer"/>.
    /// </typeparam>
    /// <param name="slotName">
    /// The name of the save slot. Must be a valid filename.
    /// </param>
    /// <param name="serializer">
    /// The component serializer for AOT-compatible serialization.
    /// </param>
    /// <param name="options">
    /// Optional save options. Uses default if not specified.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The save slot info with updated metadata.</returns>
    /// <remarks>
    /// <para>
    /// This async version performs file I/O without blocking the calling thread,
    /// making it suitable for use in game loops where blocking is unacceptable.
    /// </para>
    /// <para>
    /// Note that snapshot creation and serialization are still synchronous CPU-bound operations.
    /// Only the file I/O is performed asynchronously.
    /// </para>
    /// </remarks>
    public Task<SaveSlotInfo> SaveToSlotAsync<TSerializer>(
        string slotName,
        TSerializer serializer,
        SaveSlotOptions? options = null,
        CancellationToken cancellationToken = default)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        return GetSaveManager().SaveAsync(slotName, serializer, options, cancellationToken);
    }

    /// <summary>
    /// Loads a world state from a save slot asynchronously.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements both <see cref="IComponentSerializer"/>
    /// and <see cref="IBinaryComponentSerializer"/>.
    /// </typeparam>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="serializer">
    /// The component serializer for AOT-compatible deserialization.
    /// </param>
    /// <param name="validateChecksum">
    /// Whether to validate the checksum if present. Defaults to true.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A tuple containing the save slot info and a mapping from original entity IDs to new entities.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This async version performs file I/O without blocking the calling thread.
    /// </para>
    /// <para>
    /// Note that snapshot deserialization and world restoration are still synchronous CPU-bound operations.
    /// Only the file I/O is performed asynchronously.
    /// </para>
    /// </remarks>
    public Task<(SaveSlotInfo SlotInfo, Dictionary<int, Entity> EntityMap)> LoadFromSlotAsync<TSerializer>(
        string slotName,
        TSerializer serializer,
        bool validateChecksum = true,
        CancellationToken cancellationToken = default)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        return GetSaveManager().LoadAsync(slotName, serializer, validateChecksum, cancellationToken);
    }

    /// <summary>
    /// Gets information about a save slot without loading it asynchronously.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The save slot info, or null if the slot doesn't exist.</returns>
    public Task<SaveSlotInfo?> GetSaveSlotInfoAsync(string slotName, CancellationToken cancellationToken = default)
    {
        return GetSaveManager().TryGetSlotInfoAsync(slotName, cancellationToken);
    }

    /// <summary>
    /// Lists all available save slots asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of save slot info for all valid save files.</returns>
    public Task<IReadOnlyList<SaveSlotInfo>> ListSaveSlotsAsync(CancellationToken cancellationToken = default)
    {
        return GetSaveManager().ListSlotsAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a save slot asynchronously.
    /// </summary>
    /// <param name="slotName">The name of the save slot to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>True if the slot was deleted; false if it didn't exist.</returns>
    public Task<bool> DeleteSaveSlotAsync(string slotName, CancellationToken cancellationToken = default)
    {
        return GetSaveManager().DeleteSlotAsync(slotName, cancellationToken);
    }

    /// <summary>
    /// Copies a save slot to a new name asynchronously.
    /// </summary>
    /// <param name="sourceSlotName">The source slot name.</param>
    /// <param name="destinationSlotName">The destination slot name.</param>
    /// <param name="overwrite">Whether to overwrite if destination exists.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The save slot info for the copied slot.</returns>
    public Task<SaveSlotInfo> CopySaveSlotAsync(
        string sourceSlotName,
        string destinationSlotName,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        return GetSaveManager().CopySlotAsync(sourceSlotName, destinationSlotName, overwrite, cancellationToken);
    }

    /// <summary>
    /// Validates a save slot's integrity asynchronously.
    /// </summary>
    /// <param name="slotName">The name of the save slot to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// The validation result with any errors. Returns null if the file doesn't exist.
    /// </returns>
    public Task<SaveSlotInfo?> ValidateSaveSlotAsync(string slotName, CancellationToken cancellationToken = default)
    {
        return GetSaveManager().ValidateSlotAsync(slotName, cancellationToken);
    }

    #endregion
}
