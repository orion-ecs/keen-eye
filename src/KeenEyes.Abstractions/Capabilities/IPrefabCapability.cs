namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for prefab (entity template) management.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides access to prefab registration and instantiation.
/// Prefabs are reusable entity templates that define a set of components,
/// enabling efficient creation of similar entities.
/// </para>
/// <para>
/// Plugins that need to register or spawn from prefabs should request this
/// capability via <see cref="IPluginContext.GetCapability{T}"/> rather than
/// casting to the concrete World type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (context.TryGetCapability&lt;IPrefabCapability&gt;(out var prefabs))
///     {
///         // Register a prefab
///         var enemyPrefab = new EntityPrefab()
///             .With(new Health { Current = 100, Max = 100 })
///             .With(new Position { X = 0, Y = 0 });
///
///         prefabs.RegisterPrefab("Enemy", enemyPrefab);
///
///         // Spawn from prefab
///         var enemy = prefabs.SpawnFromPrefab("Enemy").Build();
///     }
/// }
/// </code>
/// </example>
public interface IPrefabCapability
{
    /// <summary>
    /// Registers a prefab with the given name for later instantiation.
    /// </summary>
    /// <param name="name">The unique name for the prefab.</param>
    /// <param name="prefab">The prefab definition to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when name or prefab is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a prefab with the given name is already registered.
    /// </exception>
    void RegisterPrefab(string name, EntityPrefab prefab);

    /// <summary>
    /// Spawns an entity from a registered prefab.
    /// </summary>
    /// <param name="name">The name of the prefab to spawn from.</param>
    /// <returns>
    /// An entity builder pre-configured with the prefab's components.
    /// Call <see cref="IEntityBuilder.Build"/> to create the entity.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no prefab with the given name is registered, or when the prefab has
    /// a circular inheritance chain.
    /// </exception>
    IEntityBuilder SpawnFromPrefab(string name);

    /// <summary>
    /// Spawns a named entity from a registered prefab.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to spawn from.</param>
    /// <param name="entityName">The name for the spawned entity, or null for unnamed.</param>
    /// <returns>
    /// An entity builder pre-configured with the prefab's components.
    /// Call <see cref="IEntityBuilder.Build"/> to create the entity.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when prefabName is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no prefab with the given name is registered.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when entityName is already assigned to another entity.
    /// </exception>
    IEntityBuilder SpawnFromPrefab(string prefabName, string? entityName);

    /// <summary>
    /// Checks if a prefab with the given name is registered.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if a prefab with the name exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    bool HasPrefab(string name);

    /// <summary>
    /// Unregisters a prefab by name.
    /// </summary>
    /// <param name="name">The name of the prefab to remove.</param>
    /// <returns>True if the prefab was removed; false if it wasn't registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    bool UnregisterPrefab(string name);

    /// <summary>
    /// Gets all registered prefab names.
    /// </summary>
    /// <returns>An enumerable of all registered prefab names.</returns>
    IEnumerable<string> GetAllPrefabNames();
}
