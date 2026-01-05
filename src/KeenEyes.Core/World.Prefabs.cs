#pragma warning disable CS0618 // Type or member is obsolete - internal usage of deprecated PrefabManager

namespace KeenEyes;

public sealed partial class World
{
    #region Prefabs

    /// <summary>
    /// Registers a prefab with the given name for later instantiation.
    /// </summary>
    /// <param name="name">The unique name for the prefab.</param>
    /// <param name="prefab">The prefab definition to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="prefab"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a prefab with the given name is already registered.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Prefabs are reusable entity templates that define a set of components. Once registered,
    /// entities can be created from the prefab using <see cref="SpawnFromPrefab(string)"/>.
    /// </para>
    /// <para>
    /// Prefabs support inheritance through <see cref="EntityPrefab.Extends(string)"/>. When
    /// spawning from a derived prefab, the inheritance chain is resolved and components are
    /// merged, with derived prefabs overriding base prefab components of the same type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define a base enemy prefab
    /// var enemyPrefab = new EntityPrefab()
    ///     .With(new Health { Current = 100, Max = 100 })
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .WithTag&lt;EnemyTag&gt;();
    ///
    /// world.RegisterPrefab("Enemy", enemyPrefab);
    ///
    /// // Create entities from the prefab
    /// var enemy1 = world.SpawnFromPrefab("Enemy").Build();
    /// var enemy2 = world.SpawnFromPrefab("Enemy").Build();
    /// </code>
    /// </example>
    /// <seealso cref="SpawnFromPrefab(string)"/>
    /// <seealso cref="SpawnFromPrefab(string, string?)"/>
    /// <seealso cref="HasPrefab(string)"/>
    [Obsolete("Runtime prefabs are deprecated. Use .keprefab files with source-generated spawn methods instead. See docs/prefabs.md for migration guidance.")]
    public void RegisterPrefab(string name, EntityPrefab prefab)
        => prefabManager.Register(name, prefab);

    /// <summary>
    /// Spawns an entity from a registered prefab.
    /// </summary>
    /// <param name="name">The name of the prefab to spawn from.</param>
    /// <returns>
    /// An entity builder pre-configured with the prefab's components.
    /// Call <see cref="IEntityBuilder.Build"/> to create the entity.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no prefab with the given name is registered, or when the prefab has
    /// a circular inheritance chain.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The returned builder can be used to add additional components or apply overrides
    /// before creating the entity. This allows customizing individual instances while
    /// still using the prefab as a base template.
    /// </para>
    /// <para>
    /// If the prefab has a base prefab (via <see cref="EntityPrefab.Extends(string)"/>),
    /// the inheritance chain is resolved and all inherited components are included.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Spawn with default prefab values
    /// var enemy1 = world.SpawnFromPrefab("Enemy").Build();
    ///
    /// // Spawn with overridden position
    /// var enemy2 = world.SpawnFromPrefab("Enemy")
    ///     .With(new Position { X = 100, Y = 50 })
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="RegisterPrefab(string, EntityPrefab)"/>
    /// <seealso cref="SpawnFromPrefab(string, string?)"/>
    [Obsolete("Runtime prefabs are deprecated. Use .keprefab files with source-generated spawn methods instead. See docs/prefabs.md for migration guidance.")]
    public IEntityBuilder SpawnFromPrefab(string name)
        => prefabManager.SpawnFromPrefab(name);

    /// <summary>
    /// Spawns a named entity from a registered prefab.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to spawn from.</param>
    /// <param name="entityName">
    /// The name for the spawned entity, or <c>null</c> for an unnamed entity.
    /// Must be unique within this world if provided.
    /// </param>
    /// <returns>
    /// An entity builder pre-configured with the prefab's components.
    /// Call <see cref="IEntityBuilder.Build"/> to create the entity.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefabName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no prefab with the given name is registered, or when the prefab has
    /// a circular inheritance chain.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="entityName"/> is already assigned to another entity.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Named entities can be retrieved later using <see cref="GetEntityByName(string)"/>.
    /// This is useful for scenarios where entities need human-readable identifiers,
    /// such as debugging or editor tooling.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Spawn a named entity from a prefab
    /// var player = world.SpawnFromPrefab("Player", "MainPlayer").Build();
    ///
    /// // Later, retrieve by name
    /// var foundPlayer = world.GetEntityByName("MainPlayer");
    /// </code>
    /// </example>
    /// <seealso cref="RegisterPrefab(string, EntityPrefab)"/>
    /// <seealso cref="SpawnFromPrefab(string)"/>
    /// <seealso cref="GetEntityByName(string)"/>
    [Obsolete("Runtime prefabs are deprecated. Use .keprefab files with source-generated spawn methods instead. See docs/prefabs.md for migration guidance.")]
    public IEntityBuilder SpawnFromPrefab(string prefabName, string? entityName)
        => prefabManager.SpawnFromPrefab(prefabName, entityName);

    /// <summary>
    /// Checks if a prefab with the given name is registered.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns><c>true</c> if a prefab with the name exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <example>
    /// <code>
    /// if (world.HasPrefab("Enemy"))
    /// {
    ///     var enemy = world.SpawnFromPrefab("Enemy").Build();
    /// }
    /// </code>
    /// </example>
    [Obsolete("Runtime prefabs are deprecated. Use .keprefab files with source-generated spawn methods instead. See docs/prefabs.md for migration guidance.")]
    public bool HasPrefab(string name)
        => prefabManager.HasPrefab(name);

    /// <summary>
    /// Unregisters a prefab by name.
    /// </summary>
    /// <param name="name">The name of the prefab to remove.</param>
    /// <returns><c>true</c> if the prefab was removed; <c>false</c> if it wasn't registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <remarks>
    /// Unregistering a prefab does not affect entities that were already spawned from it.
    /// Those entities continue to exist with their components intact.
    /// </remarks>
    [Obsolete("Runtime prefabs are deprecated. Use .keprefab files with source-generated spawn methods instead. See docs/prefabs.md for migration guidance.")]
    public bool UnregisterPrefab(string name)
        => prefabManager.Unregister(name);

    /// <summary>
    /// Gets all registered prefab names.
    /// </summary>
    /// <returns>An enumerable of all registered prefab names.</returns>
    /// <remarks>
    /// The returned names are in no particular order.
    /// </remarks>
    [Obsolete("Runtime prefabs are deprecated. Use .keprefab files with source-generated spawn methods instead. See docs/prefabs.md for migration guidance.")]
    public IEnumerable<string> GetAllPrefabNames()
        => prefabManager.GetAllPrefabNames();

    #endregion
}
