#pragma warning disable CS0618 // Type or member is obsolete - mock for deprecated prefab API

using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Mock implementation of <see cref="IPrefabCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock tracks prefab registrations and spawn operations for testing
/// without requiring a real World.
/// </para>
/// <para>
/// Note: Since EntityBuilder and EntityPrefab are Core types, this mock
/// requires a World instance to create EntityBuilders. If no World is provided,
/// spawn operations will throw.
/// </para>
/// </remarks>
public sealed class MockPrefabCapability : IPrefabCapability
{
    private readonly Dictionary<string, EntityPrefab> prefabs = [];
    private readonly List<string> registrationOrder = [];
    private readonly List<(string PrefabName, string? EntityName)> spawnLog = [];
    private readonly World? world;

    /// <summary>
    /// Creates a mock prefab capability without a World.
    /// Spawn operations will throw unless a World is provided.
    /// </summary>
    public MockPrefabCapability()
    {
    }

    /// <summary>
    /// Creates a mock prefab capability with a World for spawn operations.
    /// </summary>
    /// <param name="world">The world to use for creating EntityBuilders.</param>
    public MockPrefabCapability(World world)
    {
        this.world = world;
    }

    /// <summary>
    /// Gets the log of spawn operations.
    /// </summary>
    public IReadOnlyList<(string PrefabName, string? EntityName)> SpawnLog => spawnLog;

    /// <summary>
    /// Gets the order in which prefabs were registered.
    /// </summary>
    public IReadOnlyList<string> RegistrationOrder => registrationOrder;

    /// <inheritdoc />
    public void RegisterPrefab(string name, EntityPrefab prefab)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(prefab);

        if (prefabs.ContainsKey(name))
        {
            throw new ArgumentException($"A prefab with name '{name}' is already registered.", nameof(name));
        }

        prefabs[name] = prefab;
        registrationOrder.Add(name);
    }

    /// <inheritdoc />
    public IEntityBuilder SpawnFromPrefab(string name)
    {
        return SpawnFromPrefab(name, null);
    }

    /// <inheritdoc />
    public IEntityBuilder SpawnFromPrefab(string prefabName, string? entityName)
    {
        ArgumentNullException.ThrowIfNull(prefabName);

        if (!prefabs.ContainsKey(prefabName))
        {
            throw new InvalidOperationException($"No prefab with name '{prefabName}' is registered.");
        }

        spawnLog.Add((prefabName, entityName));

        if (world is null)
        {
            throw new InvalidOperationException(
                "MockPrefabCapability requires a World to spawn entities. " +
                "Provide a World in the constructor.");
        }

        // Use the actual World's prefab manager if it has the prefab registered
        if (world.HasPrefab(prefabName))
        {
            return world.SpawnFromPrefab(prefabName, entityName);
        }

        // Otherwise just spawn a new entity (the prefab data in the mock is for verification only)
        return entityName is not null ? world.Spawn(entityName) : world.Spawn();
    }

    /// <inheritdoc />
    public bool HasPrefab(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return prefabs.ContainsKey(name);
    }

    /// <inheritdoc />
    public bool UnregisterPrefab(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return prefabs.Remove(name);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllPrefabNames()
    {
        return prefabs.Keys;
    }

    /// <summary>
    /// Gets the prefab registered with the given name.
    /// </summary>
    public EntityPrefab? GetPrefab(string name)
    {
        return prefabs.TryGetValue(name, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// Clears all registered prefabs and logs.
    /// </summary>
    public void Clear()
    {
        prefabs.Clear();
        registrationOrder.Clear();
        spawnLog.Clear();
    }
}
