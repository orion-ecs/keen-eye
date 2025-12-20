using KeenEyes.Testing.Encryption;
using KeenEyes.Testing.Events;
using KeenEyes.Testing.Graphics;
using KeenEyes.Testing.Input;
using KeenEyes.Testing.Logging;
using KeenEyes.Testing.Network;
using KeenEyes.Testing.Platform;

namespace KeenEyes.Testing;

/// <summary>
/// A test-friendly wrapper around <see cref="World"/> that provides
/// deterministic behavior and manual time control for testing.
/// </summary>
/// <remarks>
/// <para>
/// TestWorld wraps a standard World instance and provides additional
/// testing utilities such as deterministic entity IDs, manual time control,
/// mock input support, and event recording.
/// It implements <see cref="IDisposable"/> to properly clean up the underlying world.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var testWorld = new TestWorldBuilder()
///     .WithDeterministicIds()
///     .WithManualTime()
///     .WithMockInput()
///     .WithEventRecording&lt;DamageEvent&gt;()
///     .Build();
///
/// var entity = testWorld.World.Spawn().Build();
/// testWorld.MockInput!.SetKeyDown(Key.W);
/// testWorld.Step(); // Advance one frame
/// testWorld.GetEventRecorder&lt;DamageEvent&gt;()?.ShouldNotHaveFired();
/// </code>
/// </example>
public sealed class TestWorld : IDisposable
{
    private readonly Dictionary<Type, object> eventRecorders;
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
    /// Gets the mock input context for simulating input in tests.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockInput"/>.
    /// </remarks>
    public MockInputContext? MockInput { get; }

    /// <summary>
    /// Gets whether mock input mode is enabled.
    /// </summary>
    public bool HasMockInput => MockInput != null;

    /// <summary>
    /// Gets the mock loop provider for step-through game loop testing.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockLoopProvider"/>.
    /// </remarks>
    public MockLoopProvider? MockLoopProvider { get; }

    /// <summary>
    /// Gets the mock window for headless window lifecycle testing.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockWindow"/>.
    /// </remarks>
    public MockWindow? MockWindow { get; }

    /// <summary>
    /// Gets the mock graphics context for GPU operation testing.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockGraphics"/>.
    /// </remarks>
    public MockGraphicsContext? MockGraphicsContext { get; }

    /// <summary>
    /// Gets the mock graphics device for low-level GPU testing.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockGraphics"/>.
    /// </remarks>
    public MockGraphicsDevice? MockGraphicsDevice { get; }

    /// <summary>
    /// Gets the mock 2D renderer for testing 2D rendering code.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMock2DRenderer"/>.
    /// </remarks>
    public Mock2DRenderer? Mock2DRenderer { get; }

    /// <summary>
    /// Gets the mock text renderer for testing text rendering code.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockTextRenderer"/>.
    /// </remarks>
    public MockTextRenderer? MockTextRenderer { get; }

    /// <summary>
    /// Gets the mock font manager for testing text layout without real fonts.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockFontManager"/>.
    /// </remarks>
    public MockFontManager? MockFontManager { get; }

    /// <summary>
    /// Gets the mock log provider for capturing and verifying log output.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockLogging"/>.
    /// </remarks>
    public MockLogProvider? MockLogProvider { get; }

    /// <summary>
    /// Gets the mock encryption provider for testing encryption operations.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockEncryption"/>.
    /// </remarks>
    public MockEncryptionProvider? MockEncryption { get; }

    /// <summary>
    /// Gets the mock network context for testing network code.
    /// </summary>
    /// <remarks>
    /// This is only available if the world was built with <see cref="TestWorldBuilder.WithMockNetwork"/>.
    /// </remarks>
    public MockNetworkContext? MockNetwork { get; }

    /// <summary>
    /// Creates a new TestWorld instance.
    /// </summary>
    /// <param name="world">The underlying world.</param>
    /// <param name="clock">Optional test clock for manual time control.</param>
    /// <param name="deterministicIds">Whether deterministic ID mode is enabled.</param>
    /// <param name="mockInput">Optional mock input context.</param>
    /// <param name="eventRecorders">Dictionary of event recorders by event type.</param>
    /// <param name="mockLoopProvider">Optional mock loop provider.</param>
    /// <param name="mockWindow">Optional mock window.</param>
    /// <param name="mockGraphicsContext">Optional mock graphics context.</param>
    /// <param name="mockGraphicsDevice">Optional mock graphics device.</param>
    /// <param name="mock2DRenderer">Optional mock 2D renderer.</param>
    /// <param name="mockTextRenderer">Optional mock text renderer.</param>
    /// <param name="mockFontManager">Optional mock font manager.</param>
    /// <param name="mockLogProvider">Optional mock log provider.</param>
    /// <param name="mockEncryption">Optional mock encryption provider.</param>
    /// <param name="mockNetwork">Optional mock network context.</param>
    internal TestWorld(
        World world,
        TestClock? clock,
        bool deterministicIds,
        MockInputContext? mockInput = null,
        Dictionary<Type, object>? eventRecorders = null,
        MockLoopProvider? mockLoopProvider = null,
        MockWindow? mockWindow = null,
        MockGraphicsContext? mockGraphicsContext = null,
        MockGraphicsDevice? mockGraphicsDevice = null,
        Mock2DRenderer? mock2DRenderer = null,
        MockTextRenderer? mockTextRenderer = null,
        MockFontManager? mockFontManager = null,
        MockLogProvider? mockLogProvider = null,
        MockEncryptionProvider? mockEncryption = null,
        MockNetworkContext? mockNetwork = null)
    {
        World = world;
        Clock = clock;
        HasDeterministicIds = deterministicIds;
        MockInput = mockInput;
        MockLoopProvider = mockLoopProvider;
        MockWindow = mockWindow;
        MockGraphicsContext = mockGraphicsContext;
        MockGraphicsDevice = mockGraphicsDevice;
        Mock2DRenderer = mock2DRenderer;
        MockTextRenderer = mockTextRenderer;
        MockFontManager = mockFontManager;
        MockLogProvider = mockLogProvider;
        MockEncryption = mockEncryption;
        MockNetwork = mockNetwork;
        this.eventRecorders = eventRecorders ?? [];
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
    /// Gets the event recorder for the specified event type.
    /// </summary>
    /// <typeparam name="T">The event type to get the recorder for.</typeparam>
    /// <returns>The event recorder, or null if event recording was not enabled for this type.</returns>
    /// <remarks>
    /// <para>
    /// Event recording must be enabled during world construction using
    /// <see cref="TestWorldBuilder.WithEventRecording{T}"/> for the recorder to be available.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var testWorld = new TestWorldBuilder()
    ///     .WithEventRecording&lt;DamageEvent&gt;()
    ///     .Build();
    ///
    /// // Fire events...
    /// world.Events.Publish(new DamageEvent(entity, 50));
    ///
    /// // Verify events
    /// testWorld.GetEventRecorder&lt;DamageEvent&gt;()!
    ///     .ShouldHaveFired()
    ///     .ShouldHaveFiredMatching(e => e.Amount == 50);
    /// </code>
    /// </example>
    public EventRecorder<T>? GetEventRecorder<T>()
    {
        if (eventRecorders.TryGetValue(typeof(T), out var recorder))
        {
            return (EventRecorder<T>)recorder;
        }

        return null;
    }

    /// <summary>
    /// Checks if event recording is enabled for the specified event type.
    /// </summary>
    /// <typeparam name="T">The event type to check.</typeparam>
    /// <returns>True if event recording is enabled for this type; otherwise, false.</returns>
    public bool HasEventRecording<T>()
    {
        return eventRecorders.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Disposes the underlying world and all associated resources.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            // Dispose event recorders
            foreach (var recorder in eventRecorders.Values)
            {
                if (recorder is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // Dispose mock instances
            MockInput?.Dispose();
            MockLoopProvider?.Dispose();
            MockWindow?.Dispose();
            MockGraphicsContext?.Dispose();
            MockGraphicsDevice?.Dispose();
            Mock2DRenderer?.Dispose();
            MockTextRenderer?.Dispose();
            MockFontManager?.Dispose();
            MockLogProvider?.Dispose();
            MockNetwork?.Dispose();

            // Dispose the world
            World.Dispose();
            disposed = true;
        }
    }
}
