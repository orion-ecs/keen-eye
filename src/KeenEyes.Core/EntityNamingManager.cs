namespace KeenEyes;

/// <summary>
/// Manages entity naming and lookup.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all entity naming operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Entity names are unique within a world. Named entities can be retrieved
/// by name for debugging, editor integration, and scenarios where entities
/// need human-readable identifiers.
/// </para>
/// </remarks>
internal sealed class EntityNamingManager
{
    // Entity ID -> Name
    private readonly Dictionary<int, string> entityNames = [];

    // Name -> Entity ID (for O(1) lookups by name)
    private readonly Dictionary<string, int> namesToEntityIds = [];

    /// <summary>
    /// Validates that a name is available for use.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the name is already assigned to another entity.
    /// </exception>
    internal void ValidateName(string? name)
    {
        if (name is not null && namesToEntityIds.ContainsKey(name))
        {
            throw new ArgumentException(
                $"An entity with the name '{name}' already exists in this world.", nameof(name));
        }
    }

    /// <summary>
    /// Registers a name for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to register the name for.</param>
    /// <param name="name">The name to register. If null, no registration occurs.</param>
    /// <remarks>
    /// <para>
    /// Call <see cref="ValidateName"/> before this method to ensure the name is available.
    /// </para>
    /// </remarks>
    internal void RegisterName(int entityId, string? name)
    {
        if (name is null)
        {
            return;
        }

        entityNames[entityId] = name;
        namesToEntityIds[name] = entityId;
    }

    /// <summary>
    /// Unregisters the name for an entity if it has one.
    /// </summary>
    /// <param name="entityId">The entity ID to unregister the name for.</param>
    internal void UnregisterName(int entityId)
    {
        if (entityNames.TryGetValue(entityId, out var name))
        {
            entityNames.Remove(entityId);
            namesToEntityIds.Remove(name);
        }
    }

    /// <summary>
    /// Gets the name assigned to an entity, if any.
    /// </summary>
    /// <param name="entityId">The entity ID to get the name for.</param>
    /// <returns>
    /// The name assigned to the entity, or <c>null</c> if the entity has no name.
    /// </returns>
    internal string? GetName(int entityId)
    {
        return entityNames.TryGetValue(entityId, out var name) ? name : null;
    }

    /// <summary>
    /// Finds an entity ID by its assigned name.
    /// </summary>
    /// <param name="name">The name to search for.</param>
    /// <param name="entityId">
    /// When this method returns <c>true</c>, contains the entity ID with the specified name.
    /// When this method returns <c>false</c>, contains -1.
    /// </param>
    /// <returns>
    /// <c>true</c> if an entity with the specified name exists; <c>false</c> otherwise.
    /// </returns>
    internal bool TryGetEntityIdByName(string name, out int entityId)
    {
        return namesToEntityIds.TryGetValue(name, out entityId);
    }

    /// <summary>
    /// Clears all name mappings.
    /// </summary>
    internal void Clear()
    {
        entityNames.Clear();
        namesToEntityIds.Clear();
    }
}
