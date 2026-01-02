using KeenEyes.Serialization;

namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for world save/load operations.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides access to the complete save/load functionality
/// including save slots, snapshots, and delta saves. Systems that need to
/// perform save operations should use this interface rather than casting
/// to the concrete World type.
/// </para>
/// <para>
/// This interface extends <see cref="IPersistenceCapability"/> which provides
/// basic persistence configuration such as the save directory.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// if (World is ISaveLoadCapability saveLoad)
/// {
///     if (saveLoad.SaveSlotExists("autosave"))
///     {
///         var info = saveLoad.GetSaveSlotInfo("autosave");
///         // Work with save slot info
///     }
/// }
/// </code>
/// </example>
public interface ISaveLoadCapability : IPersistenceCapability
{
    #region Save Slot Operations

    /// <summary>
    /// Checks if a save slot exists.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <returns>True if the slot exists and is valid.</returns>
    bool SaveSlotExists(string slotName);

    /// <summary>
    /// Gets information about a save slot without loading it.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <returns>The save slot info, or null if the slot doesn't exist.</returns>
    SaveSlotInfo? GetSaveSlotInfo(string slotName);

    /// <summary>
    /// Lists all available save slots.
    /// </summary>
    /// <returns>An enumerable of save slot info for all valid save files.</returns>
    IEnumerable<SaveSlotInfo> ListSaveSlots();

    /// <summary>
    /// Deletes a save slot.
    /// </summary>
    /// <param name="slotName">The name of the save slot to delete.</param>
    /// <returns>True if the slot was deleted; false if it didn't exist.</returns>
    bool DeleteSaveSlot(string slotName);

    #endregion

    #region Save Operations

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
    /// The component serializer for AOT-compatible serialization.
    /// </param>
    /// <param name="options">
    /// Optional save options for compression, checksum, and metadata.
    /// </param>
    /// <returns>The save slot info with updated metadata.</returns>
    SaveSlotInfo SaveToSlot<TSerializer>(
        string slotName,
        TSerializer serializer,
        SaveSlotOptions? options = null)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer;

    /// <summary>
    /// Saves a delta snapshot to a slot.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements both <see cref="IComponentSerializer"/>
    /// and <see cref="IBinaryComponentSerializer"/>.
    /// </typeparam>
    /// <param name="slotName">The name of the save slot.</param>
    /// <param name="delta">The delta snapshot to save.</param>
    /// <param name="serializer">The component serializer.</param>
    /// <param name="options">Optional save options.</param>
    /// <returns>The save slot info with updated metadata.</returns>
    SaveSlotInfo SaveDeltaToSlot<TSerializer>(
        string slotName,
        DeltaSnapshot delta,
        TSerializer serializer,
        SaveSlotOptions? options = null)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer;

    #endregion

    #region Load Operations

    /// <summary>
    /// Loads a world state from a save slot.
    /// </summary>
    /// <typeparam name="TSerializer">
    /// The serializer type that implements both <see cref="IComponentSerializer"/>
    /// and <see cref="IBinaryComponentSerializer"/>.
    /// </typeparam>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="serializer">The component serializer.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>
    /// A tuple containing the save slot info and a mapping from original entity IDs to new entities.
    /// </returns>
    (SaveSlotInfo SlotInfo, Dictionary<int, Entity> EntityMap) LoadFromSlot<TSerializer>(
        string slotName,
        TSerializer serializer,
        bool validateChecksum = true)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer;

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
    WorldSnapshot CreateSnapshot<TSerializer>(TSerializer serializer)
        where TSerializer : IComponentSerializer;

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
    DeltaSnapshot CreateDelta<TSerializer>(
        WorldSnapshot baseline,
        TSerializer serializer,
        string baselineSlotName,
        int sequenceNumber)
        where TSerializer : IComponentSerializer;

    #endregion

    #region Change Tracking

    /// <summary>
    /// Clears dirty flags for all entities and component types.
    /// </summary>
    void ClearAllDirtyFlags();

    #endregion
}
