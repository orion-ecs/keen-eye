namespace KeenEyes;

/// <summary>
/// Manages entity prefabs (reusable entity templates) within a world.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles prefab registration, inheritance resolution,
/// and entity spawning from prefabs. The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Prefab registration is O(1). Spawning from a prefab is O(C * D) where C is the total number
/// of components (including inherited) and D is the inheritance depth.
/// </para>
/// <para>
/// This class is thread-safe: prefab registration, lookup, and spawning can be called
/// concurrently from multiple threads.
/// </para>
/// </remarks>
[Obsolete("Runtime prefabs are deprecated. Use .keprefab files with source-generated spawn methods instead. See docs/prefabs.md for migration guidance.")]
internal sealed class PrefabManager
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<string, EntityPrefab> prefabs = [];
    private readonly World world;

    /// <summary>
    /// Creates a new prefab manager for the specified world.
    /// </summary>
    /// <param name="world">The world that owns this prefab manager.</param>
    internal PrefabManager(World world)
    {
        this.world = world;
    }

    /// <summary>
    /// Registers a prefab with the given name.
    /// </summary>
    /// <param name="name">The unique name for the prefab.</param>
    /// <param name="prefab">The prefab definition to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="prefab"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a prefab with the given name is already registered.
    /// </exception>
    internal void Register(string name, EntityPrefab prefab)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(prefab);

        lock (syncRoot)
        {
            if (prefabs.ContainsKey(name))
            {
                throw new ArgumentException($"A prefab with the name '{name}' is already registered.", nameof(name));
            }

            prefabs[name] = prefab;
        }
    }

    /// <summary>
    /// Checks if a prefab with the given name is registered.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns><c>true</c> if the prefab exists; otherwise, <c>false</c>.</returns>
    internal bool HasPrefab(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (syncRoot)
        {
            return prefabs.ContainsKey(name);
        }
    }

    /// <summary>
    /// Unregisters a prefab by name.
    /// </summary>
    /// <param name="name">The name of the prefab to remove.</param>
    /// <returns><c>true</c> if the prefab was removed; <c>false</c> if it wasn't registered.</returns>
    internal bool Unregister(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (syncRoot)
        {
            return prefabs.Remove(name);
        }
    }

    /// <summary>
    /// Spawns an entity from a registered prefab.
    /// </summary>
    /// <param name="name">The name of the prefab to spawn from.</param>
    /// <returns>An entity builder pre-configured with the prefab's components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the prefab is not registered, or when there is a circular inheritance chain.
    /// </exception>
    internal EntityBuilder SpawnFromPrefab(string name)
    {
        return SpawnFromPrefab(name, entityName: null);
    }

    /// <summary>
    /// Spawns an entity from a registered prefab with a specific entity name.
    /// </summary>
    /// <param name="name">The name of the prefab to spawn from.</param>
    /// <param name="entityName">The name for the spawned entity, or <c>null</c> for unnamed.</param>
    /// <returns>An entity builder pre-configured with the prefab's components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the prefab is not registered, or when there is a circular inheritance chain.
    /// </exception>
    internal EntityBuilder SpawnFromPrefab(string name, string? entityName)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Resolve inheritance chain and get merged components under lock
        List<ComponentDefinition> resolvedComponents;
        lock (syncRoot)
        {
            if (!prefabs.TryGetValue(name, out var prefab))
            {
                throw new InvalidOperationException($"No prefab registered with name '{name}'.");
            }

            resolvedComponents = ResolveInheritanceNoLock(prefab, name);
        }

        // Get a fresh builder from the world (outside lock)
        var builder = world.Spawn(entityName);

        // Apply all resolved components to the builder (outside lock)
        foreach (var component in resolvedComponents)
        {
            ApplyComponentToBuilder(builder, component);
        }

        return builder;
    }

    /// <summary>
    /// Resolves the inheritance chain for a prefab and returns merged component definitions.
    /// Must be called while holding syncRoot.
    /// </summary>
    /// <param name="prefab">The prefab to resolve.</param>
    /// <param name="originalName">The name of the original prefab (for error messages).</param>
    /// <returns>A list of merged component definitions.</returns>
    private List<ComponentDefinition> ResolveInheritanceNoLock(EntityPrefab prefab, string originalName)
    {
        // Track visited prefabs to detect circular inheritance
        var visited = new HashSet<string>();
        var inheritanceChain = new List<EntityPrefab>();

        // Walk up the inheritance chain
        var current = prefab;
        string? currentName = originalName;

        while (current != null)
        {
            if (currentName != null && !visited.Add(currentName))
            {
                throw new InvalidOperationException(
                    $"Circular inheritance detected in prefab '{originalName}'. " +
                    $"Prefab '{currentName}' appears multiple times in the inheritance chain.");
            }

            inheritanceChain.Add(current);

            if (current.BasePrefab == null)
            {
                break;
            }

            currentName = current.BasePrefab;
            if (!prefabs.TryGetValue(currentName, out current))
            {
                throw new InvalidOperationException(
                    $"Base prefab '{currentName}' not found for prefab '{originalName}'.");
            }
        }

        // Reverse to process base prefabs first (so derived prefabs override)
        inheritanceChain.Reverse();

        // Merge components - later components override earlier ones by type
        var mergedComponents = new Dictionary<Type, ComponentDefinition>();

        foreach (var p in inheritanceChain)
        {
            foreach (var component in p.Components)
            {
                mergedComponents[component.Type] = component;
            }
        }

        return [.. mergedComponents.Values];
    }

    /// <summary>
    /// Applies a component definition to an entity builder using AOT-compatible delegates.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="ComponentInfo.ApplyToBuilder"/> and <see cref="ComponentInfo.ApplyTagToBuilder"/>
    /// delegates stored during component registration for AOT compatibility.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the component type is not registered in the world's component registry.
    /// Register all component types before spawning from prefabs using
    /// <c>world.Components.Register&lt;T&gt;()</c>.
    /// </exception>
    private void ApplyComponentToBuilder(EntityBuilder builder, ComponentDefinition component)
    {
        // Get the ComponentInfo from the registry - component must be pre-registered
        var info = world.Components.Get(component.Type);
        if (info is null)
        {
            throw new InvalidOperationException(
                $"Component type '{component.Type.Name}' is not registered. " +
                $"Register all component types before spawning from prefabs using world.Components.Register<{component.Type.Name}>().");
        }

        if (component.IsTag)
        {
            // Use the pre-stored tag applicator delegate (AOT-compatible)
            if (info.ApplyTagToBuilder is null)
            {
                throw new InvalidOperationException(
                    $"Component type '{component.Type.Name}' was not registered as a tag component.");
            }
            info.ApplyTagToBuilder(builder);
        }
        else
        {
            // Use the pre-stored applicator delegate (AOT-compatible)
            if (info.ApplyToBuilder is null)
            {
                throw new InvalidOperationException(
                    $"Component type '{component.Type.Name}' does not have an applicator delegate.");
            }
            info.ApplyToBuilder(builder, component.Data!);
        }
    }

    /// <summary>
    /// Gets all registered prefab names.
    /// </summary>
    /// <returns>An enumerable of all registered prefab names.</returns>
    /// <remarks>
    /// Returns a snapshot to allow safe iteration while other threads may register prefabs.
    /// </remarks>
    internal IEnumerable<string> GetAllPrefabNames()
    {
        lock (syncRoot)
        {
            return [.. prefabs.Keys];
        }
    }
}
