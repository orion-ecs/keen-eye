using KeenEyes.Particles.Components;
using KeenEyes.Particles.Systems;

namespace KeenEyes.Particles;

/// <summary>
/// Plugin that adds particle system support to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides a high-performance particle system for visual effects
/// like fire, smoke, explosions, and magic effects. Particles are pooled data
/// (not individual entities) for optimal performance with thousands of particles.
/// </para>
/// <para>
/// After installation, access the particle manager through the world extension:
/// <code>
/// var particles = world.GetExtension&lt;ParticleManager&gt;();
/// int totalActive = particles.TotalActiveParticles;
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install with default configuration
/// world.InstallPlugin(new ParticlesPlugin());
///
/// // Or with custom configuration
/// world.InstallPlugin(new ParticlesPlugin(new ParticlesConfig
/// {
///     MaxParticlesPerEmitter = 5000,
///     MaxEmitters = 50
/// }));
///
/// // Create a particle emitter
/// var fire = world.Spawn()
///     .With(new Transform2D(new Vector2(400, 300), 0, Vector2.One))
///     .With(ParticleEmitter.Default with
///     {
///         EmissionRate = 100,
///         StartColor = new Vector4(1, 0.5f, 0, 1),
///         BlendMode = BlendMode.Additive
///     })
///     .With(ParticleEmitterModifiers.WithFadeOut(new Vector4(1, 0.3f, 0, 1)))
///     .Build();
/// </code>
/// </example>
public sealed class ParticlesPlugin : IWorldPlugin
{
    private readonly ParticlesConfig config;
    private ParticleManager? particleManager;
    private EventSubscription? emitterAddedSubscription;
    private EventSubscription? emitterRemovedSubscription;
    private EventSubscription? entityDestroyedSubscription;

    /// <summary>
    /// Creates a new particle plugin with default configuration.
    /// </summary>
    public ParticlesPlugin()
        : this(ParticlesConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new particle plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The particle configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public ParticlesPlugin(ParticlesConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid ParticlesConfig: {error}", nameof(config));
        }

        this.config = config;
    }

    /// <inheritdoc/>
    public string Name => "Particles";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register components
        context.RegisterComponent<ParticleEmitter>();
        context.RegisterComponent<ParticleEmitterModifiers>();

        // Create and expose the particle manager
        particleManager = new ParticleManager(context.World, config);
        context.SetExtension(particleManager);

        // Register systems
        context.AddSystem<ParticleSpawnSystem>(
            SystemPhase.Update,
            order: 100);

        context.AddSystem<ParticleUpdateSystem>(
            SystemPhase.Update,
            order: 101);

        context.AddSystem<ParticleRenderSystem>(
            SystemPhase.Render,
            order: 100);

        // Subscribe to component lifecycle events
        emitterAddedSubscription = context.World.OnComponentAdded<ParticleEmitter>(OnEmitterAdded);
        emitterRemovedSubscription = context.World.OnComponentRemoved<ParticleEmitter>(OnEmitterRemoved);
        entityDestroyedSubscription = context.World.OnEntityDestroyed(OnEntityDestroyed);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Unsubscribe from events
        emitterAddedSubscription?.Dispose();
        emitterRemovedSubscription?.Dispose();
        entityDestroyedSubscription?.Dispose();

        emitterAddedSubscription = null;
        emitterRemovedSubscription = null;
        entityDestroyedSubscription = null;

        // Remove the extension
        context.RemoveExtension<ParticleManager>();

        // Dispose the particle manager
        particleManager?.Dispose();
        particleManager = null;

        // Systems are automatically cleaned up by PluginManager
    }

    private void OnEmitterAdded(Entity entity, ParticleEmitter emitter)
    {
        particleManager?.RegisterEmitter(entity, in emitter);
    }

    private void OnEmitterRemoved(Entity entity)
    {
        particleManager?.UnregisterEmitter(entity);
    }

    private void OnEntityDestroyed(Entity entity)
    {
        // Clean up particle pool when entity is destroyed
        particleManager?.UnregisterEmitter(entity);
    }
}
