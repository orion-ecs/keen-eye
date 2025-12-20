namespace KeenEyes.Capabilities;

/// <summary>
/// Capability for world snapshot operations including creating and restoring world state.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides the operations needed for serializing and deserializing
/// world state, including accessing all entities, components, and singletons.
/// </para>
/// <para>
/// The World class implements this capability. Plugin authors can
/// use this interface to work with world snapshots without depending on the
/// concrete World type.
/// </para>
/// </remarks>
public interface ISnapshotCapability
{
    /// <summary>
    /// Gets all components on an entity as type/value pairs.
    /// </summary>
    /// <param name="entity">The entity to get components from.</param>
    /// <returns>An enumerable of component type and value pairs.</returns>
    IEnumerable<(Type Type, object Value)> GetComponents(Entity entity);

    /// <summary>
    /// Gets all singletons in the world as type/value pairs.
    /// </summary>
    /// <returns>An enumerable of singleton type and value pairs.</returns>
    IEnumerable<(Type Type, object Value)> GetAllSingletons();

    /// <summary>
    /// Sets a singleton value in the world.
    /// </summary>
    /// <typeparam name="T">The singleton type. Must be a value type.</typeparam>
    /// <param name="value">The singleton value.</param>
    void SetSingleton<T>(in T value) where T : struct;

    /// <summary>
    /// Clears all entities and state from the world.
    /// </summary>
    void Clear();
}
