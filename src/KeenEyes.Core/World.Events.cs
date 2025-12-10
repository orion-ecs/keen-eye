namespace KeenEyes;

public sealed partial class World
{
    #region Events

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is added to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for additions.</typeparam>
    /// <param name="handler">
    /// The handler to invoke when the component is added. Receives the entity
    /// and the component value that was added.
    /// </param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires after the component has been successfully added to the entity.
    /// The entity is guaranteed to have the component when the handler is invoked.
    /// </para>
    /// <para>
    /// Component additions occur when calling <see cref="Add{T}(Entity, in T)"/> at runtime
    /// or when building an entity with <see cref="EntityBuilder.With{T}(T)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnComponentAdded&lt;Health&gt;((entity, health) =>
    /// {
    ///     Console.WriteLine($"Entity {entity} now has {health.Current}/{health.Max} health");
    /// });
    ///
    /// // Later, unsubscribe
    /// subscription.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="OnComponentRemoved{T}(Action{Entity})"/>
    /// <seealso cref="OnComponentChanged{T}(Action{Entity, T, T})"/>
    public EventSubscription OnComponentAdded<T>(Action<Entity, T> handler) where T : struct, IComponent
        => eventManager.OnComponentAdded(handler);

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is removed from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for removals.</typeparam>
    /// <param name="handler">The handler to invoke when the component is removed.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires before the component is fully removed. The handler receives only
    /// the entity, not the component value, because the component data may be in the
    /// process of being overwritten.
    /// </para>
    /// <para>
    /// Component removals occur when calling <see cref="Remove{T}(Entity)"/> or when
    /// despawning an entity.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnComponentRemoved&lt;Health&gt;(entity =>
    /// {
    ///     Console.WriteLine($"Entity {entity} lost its Health component");
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnComponentAdded{T}(Action{Entity, T})"/>
    public EventSubscription OnComponentRemoved<T>(Action<Entity> handler) where T : struct, IComponent
        => eventManager.OnComponentRemoved<T>(handler);

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is changed on an entity via <see cref="Set{T}(Entity, in T)"/>.
    /// </summary>
    /// <typeparam name="T">The component type to watch for changes.</typeparam>
    /// <param name="handler">
    /// The handler to invoke when the component is changed. Receives the entity,
    /// the old component value, and the new component value.
    /// </param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event is only fired when using <see cref="Set{T}(Entity, in T)"/>.
    /// Direct modifications via <see cref="Get{T}(Entity)"/> references do not
    /// trigger this event since there is no way to detect when a reference is modified.
    /// </para>
    /// <para>
    /// This is useful for implementing reactive patterns where systems need to respond
    /// to specific component value changes, such as health dropping to zero.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnComponentChanged&lt;Health&gt;((entity, oldHealth, newHealth) =>
    /// {
    ///     if (newHealth.Current &lt;= 0 &amp;&amp; oldHealth.Current &gt; 0)
    ///     {
    ///         Console.WriteLine($"Entity {entity} just died!");
    ///     }
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnComponentAdded{T}(Action{Entity, T})"/>
    /// <seealso cref="OnComponentRemoved{T}(Action{Entity})"/>
    public EventSubscription OnComponentChanged<T>(Action<Entity, T, T> handler) where T : struct, IComponent
        => eventManager.OnComponentChanged(handler);

    /// <summary>
    /// Registers a handler to be called when an entity is created.
    /// </summary>
    /// <param name="handler">
    /// The handler to invoke when an entity is created. Receives the entity
    /// and its optional name (null if unnamed).
    /// </param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires after the entity has been fully created and added to the world,
    /// including all initial components from the entity builder.
    /// </para>
    /// <para>
    /// Entity creation events occur when <see cref="EntityBuilder.Build"/> is called.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnEntityCreated((entity, name) =>
    /// {
    ///     if (name is not null)
    ///     {
    ///         Console.WriteLine($"Named entity created: {name}");
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine($"Anonymous entity created: {entity}");
    ///     }
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnEntityDestroyed(Action{Entity})"/>
    public EventSubscription OnEntityCreated(Action<Entity, string?> handler)
        => eventManager.OnEntityCreated(handler);

    /// <summary>
    /// Registers a handler to be called when an entity is destroyed.
    /// </summary>
    /// <param name="handler">The handler to invoke when an entity is destroyed.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires at the start of the despawn process, before the entity is removed
    /// from the world. The entity handle is still valid during the callback and can be
    /// used to query components.
    /// </para>
    /// <para>
    /// Entity destruction events occur when <see cref="Despawn(Entity)"/> or
    /// <see cref="DespawnRecursive(Entity)"/> is called.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnEntityDestroyed(entity =>
    /// {
    ///     var name = world.GetName(entity);
    ///     Console.WriteLine($"Entity destroyed: {name ?? entity.ToString()}");
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnEntityCreated(Action{Entity, string?})"/>
    public EventSubscription OnEntityDestroyed(Action<Entity> handler)
        => eventManager.OnEntityDestroyed(handler);

    #endregion
}
