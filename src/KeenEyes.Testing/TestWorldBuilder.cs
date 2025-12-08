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
///     .WithPlugin&lt;PhysicsPlugin&gt;()
///     .WithSystem&lt;MovementSystem&gt;()
///     .Build();
///
/// var entity = testWorld.World.Spawn().Build();
/// Assert.Equal(0, entity.Id); // Deterministic!
///
/// testWorld.Step(5); // Advance 5 frames
/// </code>
/// </example>
public sealed class TestWorldBuilder
{
    private readonly List<PluginRegistration> plugins = [];
    private readonly List<SystemRegistration> systems = [];
    private bool deterministicIds;
    private bool manualTime;
    private float targetFps = 60f;

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

        return new TestWorld(world, clock, deterministicIds);
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
