namespace KeenEyes.Testing;

/// <summary>
/// Fluent assertion extensions for <see cref="World"/> and <see cref="IWorld"/> validation in tests.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a fluent, readable syntax for asserting world state
/// in unit tests. They follow FluentAssertions-style patterns and throw
/// descriptive exceptions on failure.
/// </para>
/// <para>
/// Query-based assertions accept <see cref="IWorld"/> for maximum flexibility.
/// Methods that require World-specific features (like plugin checks) accept
/// the concrete <see cref="World"/> type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
/// world.Spawn().Build();
/// world.Spawn().Build();
///
/// world.ShouldHaveEntityCount(2);
/// </code>
/// </example>
public static class WorldAssertions
{
    /// <summary>
    /// Asserts that the world has exactly the specified number of entities.
    /// </summary>
    /// <param name="world">The world to check.</param>
    /// <param name="expectedCount">The expected entity count.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity count doesn't match.</exception>
    public static World ShouldHaveEntityCount(this World world, int expectedCount, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        var actualCount = world.GetAllEntities().Count();
        if (actualCount != expectedCount)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to have {expectedCount} entities{reason}, but found {actualCount}.");
        }

        return world;
    }

    /// <summary>
    /// Asserts that the test world has exactly the specified number of entities.
    /// </summary>
    /// <param name="testWorld">The test world to check.</param>
    /// <param name="expectedCount">The expected entity count.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The test world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity count doesn't match.</exception>
    public static TestWorld ShouldHaveEntityCount(this TestWorld testWorld, int expectedCount, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        testWorld.World.ShouldHaveEntityCount(expectedCount, because);
        return testWorld;
    }

    /// <summary>
    /// Asserts that the world has no entities.
    /// </summary>
    /// <param name="world">The world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when entities exist.</exception>
    public static World ShouldBeEmpty(this World world, string? because = null)
    {
        return world.ShouldHaveEntityCount(0, because);
    }

    /// <summary>
    /// Asserts that the test world has no entities.
    /// </summary>
    /// <param name="testWorld">The test world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The test world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when entities exist.</exception>
    public static TestWorld ShouldBeEmpty(this TestWorld testWorld, string? because = null)
    {
        return testWorld.ShouldHaveEntityCount(0, because);
    }

    /// <summary>
    /// Asserts that the world has at least one entity.
    /// </summary>
    /// <param name="world">The world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no entities exist.</exception>
    public static World ShouldNotBeEmpty(this World world, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        var count = world.GetAllEntities().Count();
        if (count == 0)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to have at least one entity{reason}, but it was empty.");
        }

        return world;
    }

    /// <summary>
    /// Asserts that the test world has at least one entity.
    /// </summary>
    /// <param name="testWorld">The test world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The test world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no entities exist.</exception>
    public static TestWorld ShouldNotBeEmpty(this TestWorld testWorld, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        testWorld.World.ShouldNotBeEmpty(because);
        return testWorld;
    }

    /// <summary>
    /// Asserts that the world has a plugin of the specified type installed.
    /// </summary>
    /// <typeparam name="T">The plugin type to check for.</typeparam>
    /// <param name="world">The world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the plugin is not installed.</exception>
    public static World ShouldHavePlugin<T>(this World world, string? because = null)
        where T : IWorldPlugin
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.HasPlugin<T>())
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to have plugin {typeof(T).Name}{reason}, but it was not installed.");
        }

        return world;
    }

    /// <summary>
    /// Asserts that the test world has a plugin of the specified type installed.
    /// </summary>
    /// <typeparam name="T">The plugin type to check for.</typeparam>
    /// <param name="testWorld">The test world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The test world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the plugin is not installed.</exception>
    public static TestWorld ShouldHavePlugin<T>(this TestWorld testWorld, string? because = null)
        where T : IWorldPlugin
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        testWorld.World.ShouldHavePlugin<T>(because);
        return testWorld;
    }

    /// <summary>
    /// Asserts that the world does not have a plugin of the specified type installed.
    /// </summary>
    /// <typeparam name="T">The plugin type to check for.</typeparam>
    /// <param name="world">The world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the plugin is installed.</exception>
    public static World ShouldNotHavePlugin<T>(this World world, string? because = null)
        where T : IWorldPlugin
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.HasPlugin<T>())
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to not have plugin {typeof(T).Name}{reason}, but it was installed.");
        }

        return world;
    }

    /// <summary>
    /// Asserts that the test world does not have a plugin of the specified type installed.
    /// </summary>
    /// <typeparam name="T">The plugin type to check for.</typeparam>
    /// <param name="testWorld">The test world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The test world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the plugin is installed.</exception>
    public static TestWorld ShouldNotHavePlugin<T>(this TestWorld testWorld, string? because = null)
        where T : IWorldPlugin
    {
        ArgumentNullException.ThrowIfNull(testWorld);
        testWorld.World.ShouldNotHavePlugin<T>(because);
        return testWorld;
    }

    /// <summary>
    /// Asserts that the world contains at least one entity matching the query.
    /// </summary>
    /// <typeparam name="T1">The component type to query.</typeparam>
    /// <param name="world">The world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching entities exist.</exception>
    public static IWorld ShouldContainEntitiesWith<T1>(this IWorld world, string? because = null)
        where T1 : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Query<T1>().Any())
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to contain at least one entity with {typeof(T1).Name}{reason}, but found none.");
        }

        return world;
    }

    /// <summary>
    /// Asserts that the world contains at least one entity matching the query.
    /// </summary>
    /// <typeparam name="T1">The first component type to query.</typeparam>
    /// <typeparam name="T2">The second component type to query.</typeparam>
    /// <param name="world">The world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching entities exist.</exception>
    public static IWorld ShouldContainEntitiesWith<T1, T2>(this IWorld world, string? because = null)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Query<T1, T2>().Any())
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to contain at least one entity with ({typeof(T1).Name}, {typeof(T2).Name}){reason}, but found none.");
        }

        return world;
    }

    /// <summary>
    /// Asserts that the world does not contain any entities matching the query.
    /// </summary>
    /// <typeparam name="T1">The component type to query.</typeparam>
    /// <param name="world">The world to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when matching entities exist.</exception>
    public static IWorld ShouldNotContainEntitiesWith<T1>(this IWorld world, string? because = null)
        where T1 : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        var count = world.Query<T1>().Count();
        if (count > 0)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to not contain entities with {typeof(T1).Name}{reason}, but found {count}.");
        }

        return world;
    }

    /// <summary>
    /// Asserts that the world contains exactly the specified number of entities matching the query.
    /// </summary>
    /// <typeparam name="T1">The component type to query.</typeparam>
    /// <param name="world">The world to check.</param>
    /// <param name="expectedCount">The expected count of matching entities.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The world for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static IWorld ShouldContainExactlyWith<T1>(this IWorld world, int expectedCount, string? because = null)
        where T1 : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(world);

        var actualCount = world.Query<T1>().Count();
        if (actualCount != expectedCount)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected world to contain exactly {expectedCount} entities with {typeof(T1).Name}{reason}, but found {actualCount}.");
        }

        return world;
    }
}
