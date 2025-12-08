namespace KeenEyes;

/// <summary>
/// Interface for ECS world operations used by systems and plugins.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the core operations that systems need to interact with
/// the ECS world. It enables plugin authors to write systems that depend only on
/// the abstractions package rather than the full Core implementation.
/// </para>
/// <para>
/// For advanced operations not covered by this interface, systems can cast to
/// the concrete <c>World</c> type when necessary.
/// </para>
/// </remarks>
public interface IWorld : IDisposable
{
    #region Entity Operations

    /// <summary>
    /// Checks if an entity is alive (not despawned).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity exists and is alive; false otherwise.</returns>
    bool IsAlive(Entity entity);

    /// <summary>
    /// Despawns an entity, removing it and all its components from the world.
    /// </summary>
    /// <param name="entity">The entity to despawn.</param>
    /// <returns>True if the entity was despawned; false if it wasn't alive.</returns>
    bool Despawn(Entity entity);

    #endregion

    #region Component Operations

    /// <summary>
    /// Gets a reference to a component on an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>A reference to the component data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity doesn't have the component.</exception>
    ref T Get<T>(Entity entity) where T : struct, IComponent;

    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component; false otherwise.</returns>
    bool Has<T>(Entity entity) where T : struct, IComponent;

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>True if the component was removed; false if the entity didn't have it.</returns>
    bool Remove<T>(Entity entity) where T : struct, IComponent;

    #endregion

    #region Query Operations

    /// <summary>
    /// Creates a query for entities with a single component type.
    /// </summary>
    /// <typeparam name="T1">The required component type.</typeparam>
    /// <returns>An enumerable of entities matching the query.</returns>
    IEnumerable<Entity> Query<T1>() where T1 : struct, IComponent;

    /// <summary>
    /// Creates a query for entities with two component types.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <returns>An enumerable of entities matching the query.</returns>
    IEnumerable<Entity> Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent;

    /// <summary>
    /// Creates a query for entities with three component types.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <typeparam name="T3">The third required component type.</typeparam>
    /// <returns>An enumerable of entities matching the query.</returns>
    IEnumerable<Entity> Query<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent;

    /// <summary>
    /// Creates a query for entities with four component types.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <typeparam name="T3">The third required component type.</typeparam>
    /// <typeparam name="T4">The fourth required component type.</typeparam>
    /// <returns>An enumerable of entities matching the query.</returns>
    IEnumerable<Entity> Query<T1, T2, T3, T4>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent;

    #endregion

    #region Extension Operations

    /// <summary>
    /// Gets an extension registered with the world.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>The extension instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the extension is not registered.</exception>
    T GetExtension<T>() where T : class;

    /// <summary>
    /// Tries to get an extension registered with the world.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found.</param>
    /// <returns>True if the extension is registered; false otherwise.</returns>
    bool TryGetExtension<T>(out T? extension) where T : class;

    /// <summary>
    /// Checks if an extension of the specified type is registered.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>True if the extension is registered; false otherwise.</returns>
    bool HasExtension<T>() where T : class;

    #endregion
}
