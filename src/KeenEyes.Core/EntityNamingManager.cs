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
/// <para>
/// Each operation on this class acquires an internal lock, so individual calls are safe
/// from multiple threads, and name uniqueness is checked and enforced atomically within
/// <see cref="RegisterName"/> and <see cref="SetName"/>. Sequences of separate calls are
/// not atomic as a whole; per the threading model documented on <see cref="World"/>,
/// world operations are expected to run on a single thread.
/// </para>
/// </remarks>
internal sealed class EntityNamingManager
{
    private readonly Lock syncRoot = new();

    // Entity ID -> Name
    private readonly Dictionary<int, string> entityNames = [];

    // Name -> Entity ID (for O(1) lookups by name)
    private readonly Dictionary<string, int> namesToEntityIds = [];

    /// <summary>
    /// Validates name availability and registers the name for an entity in a single
    /// atomic operation.
    /// </summary>
    /// <param name="entityId">The entity ID to register the name for.</param>
    /// <param name="name">The name to register. If null, no registration occurs.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the name is already assigned to another entity.
    /// </exception>
    internal void RegisterName(int entityId, string? name)
    {
        if (name is null)
        {
            return;
        }

        lock (syncRoot)
        {
            if (namesToEntityIds.ContainsKey(name))
            {
                throw new ArgumentException(
                    $"An entity with the name '{name}' already exists in this world.", nameof(name));
            }

            entityNames[entityId] = name;
            namesToEntityIds[name] = entityId;
        }
    }

    /// <summary>
    /// Unregisters the name for an entity if it has one.
    /// </summary>
    /// <param name="entityId">The entity ID to unregister the name for.</param>
    internal void UnregisterName(int entityId)
    {
        lock (syncRoot)
        {
            if (entityNames.TryGetValue(entityId, out var name))
            {
                entityNames.Remove(entityId);
                namesToEntityIds.Remove(name);
            }
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
        lock (syncRoot)
        {
            return entityNames.TryGetValue(entityId, out var name) ? name : null;
        }
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
        lock (syncRoot)
        {
            return namesToEntityIds.TryGetValue(name, out entityId);
        }
    }

    /// <summary>
    /// Clears all name mappings.
    /// </summary>
    internal void Clear()
    {
        lock (syncRoot)
        {
            entityNames.Clear();
            namesToEntityIds.Clear();
        }
    }

    /// <summary>
    /// Changes the name of an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to rename.</param>
    /// <param name="newName">The new name, or null to remove the name.</param>
    /// <exception cref="ArgumentException">Thrown when the new name is already in use.</exception>
    internal void SetName(int entityId, string? newName)
    {
        lock (syncRoot)
        {
            // Validate new name if not null
            if (newName is not null && namesToEntityIds.TryGetValue(newName, out var existingId) && existingId != entityId)
            {
                throw new ArgumentException($"An entity with the name '{newName}' already exists in this world.");
            }

            // Remove old name
            UnregisterNameNoLock(entityId);

            // Register new name
            RegisterNameNoLock(entityId, newName);
        }
    }

    private void UnregisterNameNoLock(int entityId)
    {
        if (entityNames.TryGetValue(entityId, out var name))
        {
            entityNames.Remove(entityId);
            namesToEntityIds.Remove(name);
        }
    }

    private void RegisterNameNoLock(int entityId, string? name)
    {
        if (name is null)
        {
            return;
        }

        entityNames[entityId] = name;
        namesToEntityIds[name] = entityId;
    }
}
