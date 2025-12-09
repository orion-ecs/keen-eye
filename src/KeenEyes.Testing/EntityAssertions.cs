namespace KeenEyes.Testing;

/// <summary>
/// Fluent assertion extensions for <see cref="Entity"/> validation in tests.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a fluent, readable syntax for asserting entity state
/// in unit tests. They follow FluentAssertions-style patterns and throw
/// descriptive exceptions on failure.
/// </para>
/// <para>
/// The assertions accept <see cref="IWorld"/> for maximum flexibility, allowing
/// them to work with any world implementation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entity = world.Spawn().With(new Position { X = 10 }).Build();
///
/// entity.ShouldBeAlive(world);
/// entity.ShouldHaveComponent&lt;Position&gt;(world);
/// entity.ShouldNotHaveComponent&lt;Velocity&gt;(world);
/// </code>
/// </example>
public static class EntityAssertions
{
    /// <summary>
    /// Asserts that the entity is alive in the specified world.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity is not alive.</exception>
    public static Entity ShouldBeAlive(this Entity entity, IWorld world, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.IsAlive(entity))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity (Id={entity.Id}, Version={entity.Version}) to be alive{reason}, but it was not.");
        }

        return entity;
    }

    /// <summary>
    /// Asserts that the entity is alive in the specified test world.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="testWorld">The test world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity is not alive.</exception>
    public static Entity ShouldBeAlive(this Entity entity, TestWorld testWorld, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        return entity.ShouldBeAlive(testWorld.World, because);
    }

    /// <summary>
    /// Asserts that the entity is dead (not alive) in the specified world.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="world">The world to check against.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity is still alive.</exception>
    public static Entity ShouldBeDead(this Entity entity, IWorld world, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.IsAlive(entity))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity (Id={entity.Id}, Version={entity.Version}) to be dead{reason}, but it was alive.");
        }

        return entity;
    }

    /// <summary>
    /// Asserts that the entity is dead (not alive) in the specified test world.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="testWorld">The test world to check against.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity is still alive.</exception>
    public static Entity ShouldBeDead(this Entity entity, TestWorld testWorld, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        return entity.ShouldBeDead(testWorld.World, because);
    }

    /// <summary>
    /// Asserts that the entity has a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity does not have the component.</exception>
    public static Entity ShouldHaveComponent<T>(this Entity entity, IWorld world, string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Has<T>(entity))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity (Id={entity.Id}) to have component {typeof(T).Name}{reason}, but it did not.");
        }

        return entity;
    }

    /// <summary>
    /// Asserts that the entity has a component of the specified type in the test world.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="testWorld">The test world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity does not have the component.</exception>
    public static Entity ShouldHaveComponent<T>(this Entity entity, TestWorld testWorld, string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        return entity.ShouldHaveComponent<T>(testWorld.World, because);
    }

    /// <summary>
    /// Asserts that the entity does not have a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity has the component.</exception>
    public static Entity ShouldNotHaveComponent<T>(this Entity entity, IWorld world, string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Has<T>(entity))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity (Id={entity.Id}) to not have component {typeof(T).Name}{reason}, but it did.");
        }

        return entity;
    }

    /// <summary>
    /// Asserts that the entity does not have a component of the specified type in the test world.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="testWorld">The test world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity has the component.</exception>
    public static Entity ShouldNotHaveComponent<T>(this Entity entity, TestWorld testWorld, string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        return entity.ShouldNotHaveComponent<T>(testWorld.World, because);
    }

    /// <summary>
    /// Asserts that the entity has a tag component of the specified type.
    /// </summary>
    /// <typeparam name="T">The tag component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity does not have the tag.</exception>
    public static Entity ShouldHaveTag<T>(this Entity entity, IWorld world, string? because = null)
        where T : struct, ITagComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        // ITagComponent extends IComponent, so we use Has<T>
        if (!world.Has<T>(entity))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity (Id={entity.Id}) to have tag {typeof(T).Name}{reason}, but it did not.");
        }

        return entity;
    }

    /// <summary>
    /// Asserts that the entity has a tag component of the specified type in the test world.
    /// </summary>
    /// <typeparam name="T">The tag component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="testWorld">The test world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity does not have the tag.</exception>
    public static Entity ShouldHaveTag<T>(this Entity entity, TestWorld testWorld, string? because = null)
        where T : struct, ITagComponent
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        return entity.ShouldHaveTag<T>(testWorld.World, because);
    }

    /// <summary>
    /// Asserts that the entity does not have a tag component of the specified type.
    /// </summary>
    /// <typeparam name="T">The tag component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity has the tag.</exception>
    public static Entity ShouldNotHaveTag<T>(this Entity entity, IWorld world, string? because = null)
        where T : struct, ITagComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        // ITagComponent extends IComponent, so we use Has<T>
        if (world.Has<T>(entity))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity (Id={entity.Id}) to not have tag {typeof(T).Name}{reason}, but it did.");
        }

        return entity;
    }

    /// <summary>
    /// Asserts that the entity does not have a tag component of the specified type in the test world.
    /// </summary>
    /// <typeparam name="T">The tag component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="testWorld">The test world containing the entity.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity has the tag.</exception>
    public static Entity ShouldNotHaveTag<T>(this Entity entity, TestWorld testWorld, string? because = null)
        where T : struct, ITagComponent
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        return entity.ShouldNotHaveTag<T>(testWorld.World, because);
    }

    /// <summary>
    /// Asserts that the entity has a component matching the specified predicate.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="predicate">The predicate to match against the component value.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the component doesn't match the predicate.</exception>
    public static Entity ShouldHaveComponentMatching<T>(
        this Entity entity,
        IWorld world,
        Func<T, bool> predicate,
        string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(predicate);

        entity.ShouldHaveComponent<T>(world);

        ref readonly var component = ref world.Get<T>(entity);
        if (!predicate(component))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity (Id={entity.Id}) to have component {typeof(T).Name} matching predicate{reason}, but it did not.");
        }

        return entity;
    }

    /// <summary>
    /// Asserts that the entity has a component matching the specified predicate in the test world.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="testWorld">The test world containing the entity.</param>
    /// <param name="predicate">The predicate to match against the component value.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the component doesn't match the predicate.</exception>
    public static Entity ShouldHaveComponentMatching<T>(
        this Entity entity,
        TestWorld testWorld,
        Func<T, bool> predicate,
        string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        return entity.ShouldHaveComponentMatching(testWorld.World, predicate, because);
    }
}
