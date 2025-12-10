namespace KeenEyes;

/// <summary>
/// Queues entity operations for deferred execution, enabling safe modification during system iteration.
/// </summary>
/// <remarks>
/// <para>
/// CommandBuffer is the solution to iterator invalidation when modifying entities during queries.
/// Instead of directly spawning, despawning, or modifying components during iteration,
/// operations are queued and executed atomically after iteration completes.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> CommandBuffer is not thread-safe. Each system should use its own buffer,
/// or access should be synchronized externally.
/// </para>
/// <para>
/// <strong>Performance:</strong> The buffer maintains O(N) performance where N is the number of commands.
/// Commands are stored in a list and executed in order during <see cref="Flush"/>.
/// </para>
/// <para>
/// <strong>Execution Order:</strong> Commands are executed in the order they were queued.
/// Spawn commands create placeholder-to-real entity mappings, allowing subsequent commands
/// to reference newly created entities.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // During system iteration
/// var buffer = new CommandBuffer();
/// foreach (var entity in world.Query&lt;Health&gt;())
/// {
///     ref var health = ref world.Get&lt;Health&gt;(entity);
///     if (health.Current &lt;= 0)
///     {
///         buffer.Despawn(entity);  // Queue for later, don't invalidate iterator
///     }
/// }
///
/// // After iteration
/// buffer.Flush(world);  // Execute all queued commands
/// </code>
/// </example>
public interface ICommandBuffer
{
    /// <summary>
    /// Gets the number of commands currently queued in the buffer.
    /// </summary>
    int Count { get; }


    /// <summary>
    /// Queues a command to add a component to an existing entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component value.</param>
    /// <remarks>
    /// The component is not added until <see cref="Flush"/> is called.
    /// If the entity is not alive or already has the component at flush time,
    /// the behavior matches <see cref="IWorld.Add{T}"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Add a power-up component to entities that collected a power-up
    /// buffer.AddComponent(entity, new PowerUp { Type = PowerUpType.Speed, Duration = 10f });
    /// </code>
    /// </example>
    void AddComponent<T>(Entity entity, T component) where T : struct, IComponent;


    /// <summary>
    /// Queues a command to add a component to an entity referenced by placeholder ID.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="placeholderId">The placeholder ID from a previous Spawn call.</param>
    /// <param name="component">The component value.</param>
    void AddComponent<T>(int placeholderId, T component) where T : struct, IComponent;

    /// <summary>
    /// Clears all queued commands without executing them.
    /// </summary>
    /// <remarks>
    /// Use this to abandon queued commands. The buffer can be reused after clearing.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Queues a command to despawn an existing entity.
    /// </summary>
    /// <param name="entity">The entity to despawn.</param>
    /// <remarks>
    /// The entity is not destroyed until <see cref="Flush"/> is called.
    /// If the entity is not alive at flush time, the command is silently ignored.
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var entity in world.Query&lt;Health&gt;())
    /// {
    ///     ref var health = ref world.Get&lt;Health&gt;(entity);
    ///     if (health.Current &lt;= 0)
    ///     {
    ///         buffer.Despawn(entity);
    ///     }
    /// }
    /// </code>
    /// </example>
    void Despawn(Entity entity);

    /// <summary>
    /// Queues a command to despawn an entity referenced by placeholder ID.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID from a previous Spawn call.</param>
    /// <remarks>
    /// This allows despawning entities that were spawned in the same command buffer
    /// before <see cref="Flush"/> is called.
    /// </remarks>
    void Despawn(int placeholderId);

    /// <summary>
    /// Executes all queued commands on the specified world and clears the buffer.
    /// </summary>
    /// <param name="world">The world to execute commands on.</param>
    /// <returns>
    /// A dictionary mapping placeholder entity IDs to the real entities created.
    /// This allows callers to track which entities were spawned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Commands are executed in the order they were queued. Spawn commands are processed
    /// first in sequence, creating the placeholder-to-entity mapping that subsequent
    /// commands can use.
    /// </para>
    /// <para>
    /// After execution, the buffer is cleared and ready for reuse.
    /// </para>
    /// <para>
    /// <strong>Exception Handling:</strong> If a command throws an exception, subsequent
    /// commands are not executed. The buffer is still cleared to prevent duplicate execution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var cmd1 = buffer.Spawn().With(new Position { X = 0, Y = 0 });
    /// var cmd2 = buffer.Spawn().With(new Position { X = 10, Y = 10 });
    ///
    /// var entityMap = buffer.Flush(world);
    ///
    /// var entity1 = entityMap[cmd1.PlaceholderId];  // Get the real entity
    /// var entity2 = entityMap[cmd2.PlaceholderId];
    /// </code>
    /// </example>
    Dictionary<int, Entity> Flush(IWorld world);

    /// <summary>
    /// Queues a command to remove a component from an existing entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <remarks>
    /// The component is not removed until <see cref="Flush"/> is called.
    /// If the entity is not alive or does not have the component at flush time,
    /// the command is silently ignored (matches <see cref="IWorld.Remove{T}"/> behavior).
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove frozen status from entities that thaw
    /// buffer.RemoveComponent&lt;FrozenTag&gt;(entity);
    /// </code>
    /// </example>
    void RemoveComponent<T>(Entity entity) where T : struct, IComponent;

    /// <summary>
    /// Queues a command to remove a component from an entity referenced by placeholder ID.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="placeholderId">The placeholder ID from a previous Spawn call.</param>
    void RemoveComponent<T>(int placeholderId) where T : struct, IComponent;

    /// <summary>
    /// Queues a command to set (replace) a component value on an existing entity.
    /// </summary>
    /// <typeparam name="T">The component type to set.</typeparam>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="component">The new component value.</param>
    /// <remarks>
    /// The component is not updated until <see cref="Flush"/> is called.
    /// If the entity is not alive or does not have the component at flush time,
    /// the behavior matches <see cref="IWorld.Set{T}"/>.
    /// Use <see cref="AddComponent{T}(Entity, T)"/> to add a new component.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Update position after calculating new location
    /// buffer.SetComponent(entity, new Position { X = newX, Y = newY });
    /// </code>
    /// </example>
    void SetComponent<T>(Entity entity, T component) where T : struct, IComponent;

    /// <summary>
    /// Queues a command to set (replace) a component value on an entity referenced by placeholder ID.
    /// </summary>
    /// <typeparam name="T">The component type to set.</typeparam>
    /// <param name="placeholderId">The placeholder ID from a previous Spawn call.</param>
    /// <param name="component">The new component value.</param>
    void SetComponent<T>(int placeholderId, T component) where T : struct, IComponent;

    /// <summary>
    /// Queues a spawn command and returns a fluent builder for adding components.
    /// </summary>
    /// <returns>An <see cref="EntityCommands"/> builder for configuring the new entity.</returns>
    /// <remarks>
    /// <para>
    /// The entity is not created until <see cref="Flush"/> is called.
    /// Use the returned builder to add components to the entity.
    /// </para>
    /// <para>
    /// Each call to Spawn generates a unique placeholder ID (negative value)
    /// that can be used to reference the entity in subsequent commands.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var entityCmd = buffer.Spawn()
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .With(new Velocity { X = 1, Y = 0 });
    ///
    /// // Use placeholder to add more components later
    /// buffer.AddComponent(entityCmd.PlaceholderId, new Health { Current = 100, Max = 100 });
    /// </code>
    /// </example>
    EntityCommands Spawn();

    /// <summary>
    /// Queues a spawn command with an optional name and returns a fluent builder for adding components.
    /// </summary>
    /// <param name="name">
    /// The optional name for the entity. If provided, must be unique within the world at flush time.
    /// </param>
    /// <returns>An <see cref="EntityCommands"/> builder for configuring the new entity.</returns>
    /// <remarks>
    /// <para>
    /// Named entities can be retrieved later using <c>world.GetEntityByName()</c>.
    /// This is useful for debugging, editor tooling, and scenarios where entities need
    /// human-readable identifiers.
    /// </para>
    /// <para>
    /// The entity is not created until <see cref="Flush"/> is called. If the name
    /// is already in use at flush time, an exception will be thrown.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var playerCmd = buffer.Spawn("Player")
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .With(new Health { Current = 100, Max = 100 });
    ///
    /// buffer.Flush(world);
    ///
    /// // Later, retrieve by name
    /// var player = world.GetEntityByName("Player");
    /// </code>
    /// </example>
    EntityCommands Spawn(string? name);
}
