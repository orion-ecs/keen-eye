using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Systems;

namespace KeenEyes.Physics;

/// <summary>
/// Plugin that adds BepuPhysics v2 integration to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides a complete 3D physics simulation using BepuPhysics v2.
/// It handles rigid body dynamics, collision detection, and constraint solving
/// with high performance suitable for games and simulations.
/// </para>
/// <para>
/// The plugin automatically creates physics bodies when <see cref="RigidBody"/>
/// components are added to entities and removes them when the components are removed.
/// Entities must also have <see cref="PhysicsShape"/> and <see cref="Transform3D"/>
/// components for physics to work properly.
/// </para>
/// <para>
/// After installation, access the physics API through the <see cref="PhysicsWorld"/>
/// extension:
/// <code>
/// var physics = world.GetExtension&lt;PhysicsWorld&gt;();
/// physics.ApplyForce(entity, new Vector3(0, 100, 0));
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install the plugin with default configuration
/// world.InstallPlugin(new PhysicsPlugin());
///
/// // Or with custom configuration
/// world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
/// {
///     FixedTimestep = 1f / 120f,  // 120 Hz physics
///     Gravity = new Vector3(0, -20f, 0)
/// }));
///
/// // Create a dynamic physics entity
/// var ball = world.Spawn()
///     .With(new Transform3D(new Vector3(0, 10, 0), Quaternion.Identity, Vector3.One))
///     .With(new Velocity3D(0, 0, 0))
///     .With(RigidBody.Dynamic(1.0f))
///     .With(PhysicsShape.Sphere(0.5f))
///     .With(PhysicsMaterial.Rubber)
///     .Build();
///
/// // Create a static ground plane
/// var ground = world.Spawn()
///     .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
///     .With(RigidBody.Static())
///     .With(PhysicsShape.Box(50, 1, 50))
///     .Build();
///
/// // Run simulation
/// world.Update(SystemPhase.FixedUpdate, deltaTime);
/// </code>
/// </example>
public sealed class PhysicsPlugin : IWorldPlugin
{
    private readonly PhysicsConfig config;
    private PhysicsWorld? physicsWorld;
    private EventSubscription? rigidBodyAddedSubscription;
    private EventSubscription? rigidBodyRemovedSubscription;
    private EventSubscription? entityDestroyedSubscription;

    /// <summary>
    /// Creates a new physics plugin with default configuration.
    /// </summary>
    public PhysicsPlugin()
        : this(PhysicsConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new physics plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The physics configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public PhysicsPlugin(PhysicsConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid PhysicsConfig: {error}", nameof(config));
        }

        this.config = config;
    }

    /// <inheritdoc/>
    public string Name => "Physics";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register components
        context.RegisterComponent<RigidBody>();
        context.RegisterComponent<PhysicsShape>();
        context.RegisterComponent<PhysicsMaterial>();
        context.RegisterComponent<CollisionFilter>();

        // Create and expose the physics world API
        physicsWorld = new PhysicsWorld(context.World, config);
        context.SetExtension(physicsWorld);

        // Register systems
        context.AddSystem<PhysicsStepSystem>(
            SystemPhase.FixedUpdate,
            order: 0);

        if (config.EnableInterpolation)
        {
            context.AddSystem<PhysicsSyncSystem>(
                SystemPhase.LateUpdate,
                order: 0);
        }

        // Subscribe to component lifecycle events
        rigidBodyAddedSubscription = context.World.OnComponentAdded<RigidBody>(OnRigidBodyAdded);
        rigidBodyRemovedSubscription = context.World.OnComponentRemoved<RigidBody>(OnRigidBodyRemoved);
        entityDestroyedSubscription = context.World.OnEntityDestroyed(OnEntityDestroyed);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Unsubscribe from events
        rigidBodyAddedSubscription?.Dispose();
        rigidBodyRemovedSubscription?.Dispose();
        entityDestroyedSubscription?.Dispose();

        rigidBodyAddedSubscription = null;
        rigidBodyRemovedSubscription = null;
        entityDestroyedSubscription = null;

        // Remove the extension
        context.RemoveExtension<PhysicsWorld>();

        // Dispose the physics world
        physicsWorld?.Dispose();
        physicsWorld = null;

        // Systems are automatically cleaned up by PluginManager
    }

    private void OnRigidBodyAdded(Entity entity, RigidBody rigidBody)
    {
        if (physicsWorld == null)
        {
            return;
        }

        var ecsWorld = physicsWorld.EcsWorld;

        // Need Transform3D and PhysicsShape to create a body
        if (!ecsWorld.Has<Transform3D>(entity))
        {
            return;
        }

        if (!ecsWorld.Has<PhysicsShape>(entity))
        {
            return;
        }

        ref readonly var transform = ref ecsWorld.Get<Transform3D>(entity);
        ref readonly var shape = ref ecsWorld.Get<PhysicsShape>(entity);

        PhysicsMaterial? material = ecsWorld.Has<PhysicsMaterial>(entity)
            ? ecsWorld.Get<PhysicsMaterial>(entity)
            : null;

        switch (rigidBody.BodyType)
        {
            case RigidBodyType.Dynamic:
                physicsWorld.CreateDynamicBody(entity, in transform, in shape, in rigidBody, material);
                break;

            case RigidBodyType.Kinematic:
                physicsWorld.CreateKinematicBody(entity, in transform, in shape);
                break;

            case RigidBodyType.Static:
                physicsWorld.CreateStaticBody(entity, in transform, in shape);
                break;
        }
    }

    private void OnRigidBodyRemoved(Entity entity)
    {
        physicsWorld?.RemoveBody(entity);
    }

    private void OnEntityDestroyed(Entity entity)
    {
        // Clean up physics body when entity is destroyed
        // This handles the case where the entity is despawned without explicitly removing components
        physicsWorld?.RemoveBody(entity);
    }
}
