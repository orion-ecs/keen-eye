using KeenEyes.Testing.Encryption;
using KeenEyes.Testing.Events;
using KeenEyes.Testing.Graphics;
using KeenEyes.Testing.Input;
using KeenEyes.Testing.Logging;
using KeenEyes.Testing.Network;
using KeenEyes.Testing.Platform;

namespace KeenEyes.Testing;

/// <summary>
/// Fluent builder for creating test-friendly <see cref="TestWorld"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// TestWorldBuilder provides a convenient way to configure worlds for testing
/// with features like deterministic entity IDs and manual time control.
/// </para>
/// <para>
/// The builder can be reused to create multiple test worlds with the same configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var testWorld = new TestWorldBuilder()
///     .WithDeterministicIds()
///     .WithManualTime(fps: 60)
///     .WithMockInput()
///     .WithPlugin&lt;PhysicsPlugin&gt;()
///     .WithSystem&lt;MovementSystem&gt;()
///     .Build();
///
/// var entity = testWorld.World.Spawn().Build();
/// Assert.Equal(0, entity.Id); // Deterministic!
///
/// testWorld.MockInput!.SetKeyDown(Key.W);
/// testWorld.Step(5); // Advance 5 frames
/// </code>
/// </example>
public sealed class TestWorldBuilder
{
    private readonly List<PluginRegistration> plugins = [];
    private readonly List<SystemRegistration> systems = [];
    private readonly List<Type> eventRecorderTypes = [];
    private bool deterministicIds;
    private bool manualTime;
    private float targetFps = 60f;
    private bool useMockInput;
    private int gamepadCount = 4;

    // New mock flags
    private bool useMockLoopProvider;
    private bool useMockWindow;
    private int mockWindowWidth = 800;
    private int mockWindowHeight = 600;
    private bool useMockGraphics;
    private bool useMock2DRenderer;
    private bool useMockTextRenderer;
    private bool useMockFontManager;
    private bool useMockLogging;
    private bool useMockEncryption;
    private bool useMockNetwork;
    private NetworkOptions? mockNetworkOptions;

    /// <summary>
    /// Enables deterministic entity ID assignment.
    /// </summary>
    /// <remarks>
    /// When enabled, entity IDs are assigned sequentially starting from 0.
    /// This makes test assertions predictable and reproducible.
    /// </remarks>
    /// <returns>This builder for method chaining.</returns>
    public TestWorldBuilder WithDeterministicIds()
    {
        deterministicIds = true;
        return this;
    }

    /// <summary>
    /// Enables manual time control with a test clock.
    /// </summary>
    /// <param name="fps">Target frames per second for the test clock. Defaults to 60.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="TestClock"/>
    /// that allows precise control over time progression.
    /// </remarks>
    public TestWorldBuilder WithManualTime(float fps = 60f)
    {
        manualTime = true;
        targetFps = fps;
        return this;
    }

    /// <summary>
    /// Enables mock input support with a <see cref="MockInputContext"/>.
    /// </summary>
    /// <param name="gamepadCount">The number of gamepads to simulate. Defaults to 4.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockInputContext"/>
    /// accessible via the <see cref="TestWorld.MockInput"/> property.
    /// </para>
    /// <para>
    /// Use this for testing input-dependent systems and UI interactions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var testWorld = new TestWorldBuilder()
    ///     .WithMockInput()
    ///     .Build();
    ///
    /// testWorld.MockInput!.SetKeyDown(Key.W);
    /// testWorld.MockInput.SimulateMouseClick(MouseButton.Left);
    /// </code>
    /// </example>
    public TestWorldBuilder WithMockInput(int gamepadCount = 4)
    {
        useMockInput = true;
        this.gamepadCount = gamepadCount;
        return this;
    }

    /// <summary>
    /// Enables mock loop provider for step-through game loop testing.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockLoopProvider"/>
    /// accessible via the <see cref="TestWorld.MockLoopProvider"/> property.
    /// </remarks>
    public TestWorldBuilder WithMockLoopProvider()
    {
        useMockLoopProvider = true;
        return this;
    }

    /// <summary>
    /// Enables mock window for headless window lifecycle testing.
    /// </summary>
    /// <param name="width">The mock window width. Defaults to 800.</param>
    /// <param name="height">The mock window height. Defaults to 600.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockWindow"/>
    /// accessible via the <see cref="TestWorld.MockWindow"/> property.
    /// </remarks>
    public TestWorldBuilder WithMockWindow(int width = 800, int height = 600)
    {
        useMockWindow = true;
        mockWindowWidth = width;
        mockWindowHeight = height;
        return this;
    }

    /// <summary>
    /// Enables mock graphics context and device for GPU testing without real hardware.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have <see cref="MockGraphicsContext"/>
    /// and <see cref="MockGraphicsDevice"/> accessible via their respective properties.
    /// </remarks>
    public TestWorldBuilder WithMockGraphics()
    {
        useMockGraphics = true;
        return this;
    }

    /// <summary>
    /// Enables mock 2D renderer for testing 2D rendering code.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="Mock2DRenderer"/>
    /// accessible via the <see cref="TestWorld.Mock2DRenderer"/> property.
    /// </remarks>
    public TestWorldBuilder WithMock2DRenderer()
    {
        useMock2DRenderer = true;
        return this;
    }

    /// <summary>
    /// Enables mock text renderer for testing text rendering code.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockTextRenderer"/>
    /// accessible via the <see cref="TestWorld.MockTextRenderer"/> property.
    /// </remarks>
    public TestWorldBuilder WithMockTextRenderer()
    {
        useMockTextRenderer = true;
        return this;
    }

    /// <summary>
    /// Enables mock font manager for testing text layout without real fonts.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockFontManager"/>
    /// accessible via the <see cref="TestWorld.MockFontManager"/> property.
    /// </remarks>
    public TestWorldBuilder WithMockFontManager()
    {
        useMockFontManager = true;
        return this;
    }

    /// <summary>
    /// Enables mock logging for capturing and verifying log output.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockLogProvider"/>
    /// accessible via the <see cref="TestWorld.MockLogProvider"/> property.
    /// </remarks>
    public TestWorldBuilder WithMockLogging()
    {
        useMockLogging = true;
        return this;
    }

    /// <summary>
    /// Enables mock encryption for testing encryption operations.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockEncryptionProvider"/>
    /// accessible via the <see cref="TestWorld.MockEncryption"/> property.
    /// </remarks>
    public TestWorldBuilder WithMockEncryption()
    {
        useMockEncryption = true;
        return this;
    }

    /// <summary>
    /// Enables mock network context for testing network code.
    /// </summary>
    /// <param name="options">Optional network simulation options.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// When enabled, the built <see cref="TestWorld"/> will have a <see cref="MockNetworkContext"/>
    /// accessible via the <see cref="TestWorld.MockNetwork"/> property.
    /// </remarks>
    public TestWorldBuilder WithMockNetwork(NetworkOptions? options = null)
    {
        useMockNetwork = true;
        mockNetworkOptions = options;
        return this;
    }

    /// <summary>
    /// Enables event recording for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type to record.</typeparam>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// When enabled, the built <see cref="TestWorld"/> will automatically create an
    /// <see cref="EventRecorder{T}"/> for the specified event type. Retrieve it using
    /// <see cref="TestWorld.GetEventRecorder{T}"/>.
    /// </para>
    /// <para>
    /// Event recorders are synchronized with the <see cref="TestClock"/> when manual time is enabled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var testWorld = new TestWorldBuilder()
    ///     .WithEventRecording&lt;DamageEvent&gt;()
    ///     .Build();
    ///
    /// // Fire some events...
    ///
    /// testWorld.GetEventRecorder&lt;DamageEvent&gt;()!
    ///     .ShouldHaveFired()
    ///     .ShouldHaveFiredTimes(2);
    /// </code>
    /// </example>
    public TestWorldBuilder WithEventRecording<T>()
    {
        eventRecorderTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a plugin to be installed when the world is built.
    /// </summary>
    /// <typeparam name="T">The plugin type to install.</typeparam>
    /// <returns>This builder for method chaining.</returns>
    public TestWorldBuilder WithPlugin<T>() where T : IWorldPlugin, new()
    {
        plugins.Add(new PluginRegistration(typeof(T), null));
        return this;
    }

    /// <summary>
    /// Adds a plugin instance to be installed when the world is built.
    /// </summary>
    /// <param name="plugin">The plugin instance to install.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestWorldBuilder WithPlugin(IWorldPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        plugins.Add(new PluginRegistration(null, plugin));
        return this;
    }

    /// <summary>
    /// Adds a system to be registered when the world is built.
    /// </summary>
    /// <typeparam name="T">The system type to register.</typeparam>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestWorldBuilder WithSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0)
        where T : ISystem, new()
    {
        systems.Add(new SystemRegistration(typeof(T), null, phase, order));
        return this;
    }

    /// <summary>
    /// Adds a system instance to be registered when the world is built.
    /// </summary>
    /// <param name="system">The system instance to register.</param>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestWorldBuilder WithSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(system);
        systems.Add(new SystemRegistration(null, system, phase, order));
        return this;
    }

    /// <summary>
    /// Builds and returns a new configured <see cref="TestWorld"/> instance.
    /// </summary>
    /// <returns>A new test world with all configured plugins, systems, and settings.</returns>
    public TestWorld Build()
    {
        var world = new World();

        // Install all plugins first
        foreach (var registration in plugins)
        {
            var plugin = registration.Instance ?? (IWorldPlugin)Activator.CreateInstance(registration.Type!)!;
            world.InstallPlugin(plugin);
        }

        // Register all systems
        foreach (var registration in systems)
        {
            var system = registration.Instance ?? (ISystem)Activator.CreateInstance(registration.Type!)!;
            world.AddSystem(system, registration.Phase, registration.Order);
        }

        // Create test clock if manual time is enabled
        TestClock? clock = manualTime ? new TestClock(targetFps) : null;

        // Create mock input context if enabled
        MockInputContext? mockInput = useMockInput ? new MockInputContext(gamepadCount) : null;

        // Create new mock instances
        MockLoopProvider? mockLoopProvider = useMockLoopProvider ? new MockLoopProvider() : null;
        MockWindow? mockWindow = useMockWindow ? new MockWindow(mockWindowWidth, mockWindowHeight) : null;
        MockGraphicsContext? mockGraphicsContext = useMockGraphics ? new MockGraphicsContext() : null;
        MockGraphicsDevice? mockGraphicsDevice = useMockGraphics ? new MockGraphicsDevice() : null;
        Mock2DRenderer? mock2DRenderer = useMock2DRenderer ? new Mock2DRenderer() : null;
        MockTextRenderer? mockTextRenderer = useMockTextRenderer ? new MockTextRenderer() : null;
        MockFontManager? mockFontManager = useMockFontManager ? new MockFontManager() : null;
        MockLogProvider? mockLogProvider = useMockLogging ? new MockLogProvider() : null;
        MockEncryptionProvider? mockEncryption = useMockEncryption ? new MockEncryptionProvider() : null;
        MockNetworkContext? mockNetwork = useMockNetwork ? new MockNetworkContext() : null;

        if (mockNetwork != null && mockNetworkOptions != null)
        {
            mockNetwork.Options = mockNetworkOptions;
        }

        // Create event recorders
        var eventRecorders = new Dictionary<Type, object>();
        foreach (var eventType in eventRecorderTypes)
        {
            var recorderType = typeof(EventRecorder<>).MakeGenericType(eventType);
            var recorder = Activator.CreateInstance(recorderType, world.Events, clock);
            eventRecorders[eventType] = recorder!;
        }

        return new TestWorld(
            world,
            clock,
            deterministicIds,
            mockInput,
            eventRecorders,
            mockLoopProvider,
            mockWindow,
            mockGraphicsContext,
            mockGraphicsDevice,
            mock2DRenderer,
            mockTextRenderer,
            mockFontManager,
            mockLogProvider,
            mockEncryption,
            mockNetwork);
    }

    /// <summary>
    /// Internal record for tracking plugin registrations.
    /// </summary>
    private sealed record PluginRegistration(Type? Type, IWorldPlugin? Instance);

    /// <summary>
    /// Internal record for tracking system registrations.
    /// </summary>
    private sealed record SystemRegistration(Type? Type, ISystem? Instance, SystemPhase Phase, int Order);
}
