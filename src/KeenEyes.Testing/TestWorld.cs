namespace KeenEyes.Testing;

/// <summary>
/// A test-friendly wrapper around <see cref="World"/> that provides
/// deterministic behavior and manual time control for testing.
/// </summary>
/// <remarks>
/// <para>
/// TestWorld wraps a standard World instance and provides additional
/// testing utilities such as deterministic entity IDs and manual time control.
/// It implements <see cref="IDisposable"/> to properly clean up the underlying world.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var testWorld = new TestWorldBuilder()
///     .WithDeterministicIds()
///     .WithManualTime()
///     .Build();
///
/// var entity = testWorld.World.Spawn().Build();
/// testWorld.Step(); // Advance one frame
/// testWorld.World.Update(testWorld.Clock.DeltaSeconds);
/// </code>
/// </example>
public sealed class TestWorld : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets the underlying ECS world.
    /// </summary>
    public World World { get; }

    /// <summary>
    /// Gets the test clock for manual time control.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithManualTime"/>.
    /// </remarks>
    public TestClock? Clock { get; }

    /// <summary>
    /// Gets whether deterministic ID mode is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, entity IDs are assigned sequentially starting from 0
    /// without recycling, making test assertions predictable.
    /// </remarks>
    public bool HasDeterministicIds { get; }

    /// <summary>
    /// Gets whether manual time mode is enabled.
    /// </summary>
    public bool HasManualTime => Clock != null;

    /// <summary>
    /// Creates a new TestWorld instance.
    /// </summary>
    /// <param name="world">The underlying world.</param>
    /// <param name="clock">Optional test clock for manual time control.</param>
    /// <param name="deterministicIds">Whether deterministic ID mode is enabled.</param>
    internal TestWorld(World world, TestClock? clock, bool deterministicIds)
    {
        World = world;
        Clock = clock;
        HasDeterministicIds = deterministicIds;
    }

    /// <summary>
    /// Advances the simulation by the specified number of frames and calls Update on the world.
    /// </summary>
    /// <param name="frames">Number of frames to advance. Defaults to 1.</param>
    /// <returns>The total delta time for all stepped frames in seconds.</returns>
    /// <exception cref="InvalidOperationException">Thrown if manual time is not enabled.</exception>
    public float Step(int frames = 1)
    {
        if (Clock == null)
        {
            throw new InvalidOperationException(
                "Cannot step without manual time. Use TestWorldBuilder.WithManualTime() to enable.");
        }

        var deltaSeconds = Clock.Step(frames);
        World.Update(deltaSeconds);
        return deltaSeconds;
    }

    /// <summary>
    /// Advances the simulation by the specified time in milliseconds and calls Update on the world.
    /// </summary>
    /// <param name="deltaMs">Time to advance in milliseconds.</param>
    /// <returns>The delta time in seconds.</returns>
    /// <exception cref="InvalidOperationException">Thrown if manual time is not enabled.</exception>
    public float StepByTime(float deltaMs)
    {
        if (Clock == null)
        {
            throw new InvalidOperationException(
                "Cannot step without manual time. Use TestWorldBuilder.WithManualTime() to enable.");
        }

        var deltaSeconds = Clock.StepByTime(deltaMs);
        World.Update(deltaSeconds);
        return deltaSeconds;
    }

    /// <summary>
    /// Gets the count of all alive entities in the world.
    /// </summary>
    /// <returns>The number of alive entities.</returns>
    public int GetEntityCount()
    {
        return World.GetAllEntities().Count();
    }

    /// <summary>
    /// Creates a test entity with the specified components.
    /// </summary>
    /// <typeparam name="T1">The component type.</typeparam>
    /// <param name="component1">The component value.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity<T1>(T1 component1)
        where T1 : struct, IComponent
    {
        return World.Spawn().With(component1).Build();
    }

    /// <summary>
    /// Creates a test entity with the specified components.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    /// <param name="component1">The first component value.</param>
    /// <param name="component2">The second component value.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity<T1, T2>(T1 component1, T2 component2)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        return World.Spawn().With(component1).With(component2).Build();
    }

    /// <summary>
    /// Creates a test entity with the specified components.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    /// <typeparam name="T3">The third component type.</typeparam>
    /// <param name="component1">The first component value.</param>
    /// <param name="component2">The second component value.</param>
    /// <param name="component3">The third component value.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity<T1, T2, T3>(T1 component1, T2 component2, T3 component3)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        return World.Spawn().With(component1).With(component2).With(component3).Build();
    }

    /// <summary>
    /// Creates multiple test entities with the same component values.
    /// </summary>
    /// <typeparam name="T1">The component type.</typeparam>
    /// <param name="count">Number of entities to create.</param>
    /// <param name="componentFactory">Factory function to create component values.</param>
    /// <returns>An array of created entities.</returns>
    public Entity[] CreateEntities<T1>(int count, Func<int, T1> componentFactory)
        where T1 : struct, IComponent
    {
        var entities = new Entity[count];
        for (int i = 0; i < count; i++)
        {
            entities[i] = World.Spawn().With(componentFactory(i)).Build();
        }
        return entities;
    }

    /// <summary>
    /// Asserts that the world has no entities and no leaked resources.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the world is not clean.</exception>
    public void AssertClean()
    {
        var entityCount = GetEntityCount();
        if (entityCount > 0)
        {
            throw new InvalidOperationException(
                $"World is not clean: {entityCount} entities still exist.");
        }
    }

    /// <summary>
    /// Disposes the underlying world.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            World.Dispose();
            disposed = true;
        }
    }
}
