using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Core;

/// <summary>
/// Contains information about a raycast hit.
/// </summary>
/// <param name="Entity">The entity that was hit.</param>
/// <param name="Position">The world position of the hit point.</param>
/// <param name="Normal">The surface normal at the hit point.</param>
/// <param name="Distance">The distance from the ray origin to the hit point.</param>
public readonly record struct RayHit(Entity Entity, Vector3 Position, Vector3 Normal, float Distance);

/// <summary>
/// Extension API for physics operations in a world.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a user-friendly API for common physics operations like raycasting,
/// overlap queries, and applying forces. It abstracts away BepuPhysics complexity and
/// uses System.Numerics types for the public interface.
/// </para>
/// <para>
/// Access this API through the world extension system:
/// <code>
/// var physics = world.GetExtension&lt;PhysicsWorld&gt;();
/// if (physics.Raycast(origin, direction, 100f, out var hit))
/// {
///     // Process hit
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class PhysicsWorld : IDisposable
{
    private readonly IWorld world;
    private readonly PhysicsConfig config;
    private readonly BufferPool bufferPool;
    private readonly ThreadDispatcher threadDispatcher;
    private readonly BodyLookup bodyLookup;
    private readonly CollisionEventManager collisionEventManager;
    private Simulation simulation = null!;
    private bool disposed;

    // State for interpolation
    private float accumulator;
    private float interpolationAlpha;

    /// <summary>
    /// Gets the current interpolation alpha (0-1) for rendering.
    /// </summary>
    public float InterpolationAlpha => interpolationAlpha;

    /// <summary>
    /// Gets the physics configuration.
    /// </summary>
    public PhysicsConfig Config => config;

    /// <summary>
    /// Gets or sets the gravity vector.
    /// </summary>
    public Vector3 Gravity { get; set; }

    /// <summary>
    /// Gets the number of physics bodies (dynamic + kinematic).
    /// </summary>
    public int BodyCount => simulation.Bodies.ActiveSet.Count;

    /// <summary>
    /// Gets the number of static bodies.
    /// </summary>
    public int StaticCount => simulation.Statics.Count;

    /// <summary>
    /// Gets the ECS world this physics world is attached to.
    /// </summary>
    internal IWorld EcsWorld => world;

    /// <summary>
    /// Gets the underlying BepuPhysics simulation for advanced use.
    /// </summary>
    internal Simulation Simulation => simulation;

    /// <summary>
    /// Gets the body lookup for entity-body mapping.
    /// </summary>
    internal BodyLookup BodyLookup => bodyLookup;

    /// <summary>
    /// Gets the buffer pool for memory allocation.
    /// </summary>
    internal BufferPool BufferPool => bufferPool;

    internal PhysicsWorld(IWorld world, PhysicsConfig config)
    {
        this.world = world;
        this.config = config;
        Gravity = config.Gravity;
        bodyLookup = new BodyLookup();
        collisionEventManager = new CollisionEventManager(world);
        bufferPool = new BufferPool();
        threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);

        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        var narrowPhaseCallbacks = new NarrowPhaseCallbacks(this);
        var poseIntegratorCallbacks = new PoseIntegratorCallbacks(Gravity);

        simulation = Simulation.Create(
            bufferPool,
            narrowPhaseCallbacks,
            poseIntegratorCallbacks,
            new SolveDescription(config.VelocityIterations, config.SubstepCount));
    }

    #region Raycast and Queries

    /// <summary>
    /// Casts a ray and returns the first hit.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">The direction of the ray (should be normalized).</param>
    /// <param name="maxDistance">Maximum distance to check for hits.</param>
    /// <param name="hit">Information about the hit if one occurred.</param>
    /// <returns>True if something was hit; false otherwise.</returns>
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit)
    {
        var handler = new RayHitHandler(this);
        simulation.RayCast(origin, direction, maxDistance, ref handler);

        if (handler.HasHit)
        {
            hit = handler.Hit;
            return true;
        }

        hit = default;
        return false;
    }

    /// <summary>
    /// Finds all entities within a sphere.
    /// </summary>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns>All entities overlapping the sphere.</returns>
    public IEnumerable<Entity> OverlapSphere(Vector3 center, float radius)
    {
        var results = new List<Entity>();
        var shape = new Sphere(radius);
        var handler = new OverlapHandler(this, results);

        simulation.Sweep(
            shape,
            new RigidPose(center, Quaternion.Identity),
            new BodyVelocity(),
            0f,
            bufferPool,
            ref handler);

        return results;
    }

    /// <summary>
    /// Finds all entities within a box.
    /// </summary>
    /// <param name="center">The center of the box.</param>
    /// <param name="halfExtents">The half-extents of the box.</param>
    /// <param name="rotation">The rotation of the box.</param>
    /// <returns>All entities overlapping the box.</returns>
    public IEnumerable<Entity> OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion rotation = default)
    {
        var results = new List<Entity>();
        var shape = new Box(halfExtents.X * 2, halfExtents.Y * 2, halfExtents.Z * 2);
        var pose = new RigidPose(center, rotation == default ? Quaternion.Identity : rotation);
        var handler = new OverlapHandler(this, results);

        simulation.Sweep(
            shape,
            pose,
            new BodyVelocity(),
            0f,
            bufferPool,
            ref handler);

        return results;
    }

    #endregion

    #region Force and Impulse

    /// <summary>
    /// Applies a force to an entity at its center of mass.
    /// </summary>
    /// <param name="entity">The entity to apply force to.</param>
    /// <param name="force">The force vector in world space.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public void ApplyForce(Entity entity, Vector3 force)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        var bodyRef = simulation.Bodies.GetBodyReference(handle);
        simulation.Awakener.AwakenBody(handle);
        bodyRef.ApplyLinearImpulse(force * config.FixedTimestep);
    }

    /// <summary>
    /// Applies an impulse to an entity at its center of mass.
    /// </summary>
    /// <param name="entity">The entity to apply impulse to.</param>
    /// <param name="impulse">The impulse vector in world space.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public void ApplyImpulse(Entity entity, Vector3 impulse)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        var bodyRef = simulation.Bodies.GetBodyReference(handle);
        simulation.Awakener.AwakenBody(handle);
        bodyRef.ApplyLinearImpulse(impulse);
    }

    /// <summary>
    /// Applies an angular impulse (torque impulse) to an entity.
    /// </summary>
    /// <param name="entity">The entity to apply angular impulse to.</param>
    /// <param name="angularImpulse">The angular impulse vector.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public void ApplyAngularImpulse(Entity entity, Vector3 angularImpulse)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        var bodyRef = simulation.Bodies.GetBodyReference(handle);
        simulation.Awakener.AwakenBody(handle);
        bodyRef.ApplyAngularImpulse(angularImpulse);
    }

    /// <summary>
    /// Applies a force at a specific world position.
    /// </summary>
    /// <param name="entity">The entity to apply force to.</param>
    /// <param name="force">The force vector in world space.</param>
    /// <param name="worldPoint">The world position to apply the force at.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public void ApplyForceAtPosition(Entity entity, Vector3 force, Vector3 worldPoint)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        var bodyRef = simulation.Bodies.GetBodyReference(handle);
        simulation.Awakener.AwakenBody(handle);

        var offset = worldPoint - bodyRef.Pose.Position;
        bodyRef.ApplyLinearImpulse(force * config.FixedTimestep);
        bodyRef.ApplyAngularImpulse(Vector3.Cross(offset, force * config.FixedTimestep));
    }

    #endregion

    #region Velocity

    /// <summary>
    /// Sets the linear velocity of an entity.
    /// </summary>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="velocity">The new velocity.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public void SetVelocity(Entity entity, Vector3 velocity)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        simulation.Awakener.AwakenBody(handle);
        simulation.Bodies[handle].Velocity.Linear = velocity;
    }

    /// <summary>
    /// Sets the angular velocity of an entity.
    /// </summary>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="angularVelocity">The new angular velocity.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public void SetAngularVelocity(Entity entity, Vector3 angularVelocity)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        simulation.Awakener.AwakenBody(handle);
        simulation.Bodies[handle].Velocity.Angular = angularVelocity;
    }

    /// <summary>
    /// Gets the linear velocity of an entity.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <returns>The linear velocity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public Vector3 GetVelocity(Entity entity)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        return simulation.Bodies.GetBodyReference(handle).Velocity.Linear;
    }

    /// <summary>
    /// Gets the angular velocity of an entity.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <returns>The angular velocity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public Vector3 GetAngularVelocity(Entity entity)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        return simulation.Bodies.GetBodyReference(handle).Velocity.Angular;
    }

    #endregion

    #region Body State

    /// <summary>
    /// Wakes up a sleeping body.
    /// </summary>
    /// <param name="entity">The entity to wake.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public void WakeUp(Entity entity)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        simulation.Awakener.AwakenBody(handle);
    }

    /// <summary>
    /// Checks if a body is awake (not sleeping).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if awake; false if sleeping.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity has no physics body.</exception>
    public bool IsAwake(Entity entity)
    {
        if (!bodyLookup.TryGetBody(entity, out var handle))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a physics body.");
        }

        return simulation.Bodies.GetBodyReference(handle).Awake;
    }

    /// <summary>
    /// Checks if an entity has a physics body registered.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has a physics body.</returns>
    public bool HasPhysicsBody(Entity entity)
    {
        return bodyLookup.HasBody(entity) || bodyLookup.HasStatic(entity);
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Sets the gravity vector for the simulation.
    /// </summary>
    /// <param name="newGravity">The new gravity vector.</param>
    public void SetGravity(Vector3 newGravity)
    {
        Gravity = newGravity;
    }

    /// <summary>
    /// Sets the number of solver iterations.
    /// </summary>
    /// <param name="velocityIterations">Number of velocity iterations.</param>
    /// <param name="substepCount">Number of substeps.</param>
    public void SetSolverIterations(int velocityIterations, int substepCount = 1)
    {
        simulation.Solver.VelocityIterationCount = velocityIterations;
        simulation.Solver.SubstepCount = substepCount;
    }

    #endregion

    #region Internal Simulation Control

    /// <summary>
    /// Steps the physics simulation with fixed timestep accumulation.
    /// </summary>
    /// <param name="deltaTime">The frame delta time.</param>
    /// <returns>The number of physics steps taken this frame.</returns>
    internal int Step(float deltaTime)
    {
        accumulator += deltaTime;
        int steps = 0;

        while (accumulator >= config.FixedTimestep && steps < config.MaxStepsPerFrame)
        {
            SyncToSimulation();
            simulation.Timestep(config.FixedTimestep, threadDispatcher);
            SyncFromSimulation();

            // Publish collision events after each physics step
            collisionEventManager.PublishEvents();

            accumulator -= config.FixedTimestep;
            steps++;
        }

        // Calculate interpolation alpha for rendering
        interpolationAlpha = config.EnableInterpolation
            ? accumulator / config.FixedTimestep
            : 1f;

        return steps;
    }

    private void SyncToSimulation()
    {
        // Sync kinematic bodies from ECS to simulation
        foreach (var entity in bodyLookup.DynamicEntities)
        {
            if (!world.Has<RigidBody>(entity))
            {
                continue;
            }

            ref readonly var rigidBody = ref world.Get<RigidBody>(entity);

            if (rigidBody.BodyType == RigidBodyType.Kinematic &&
                world.Has<Transform3D>(entity) && bodyLookup.TryGetBody(entity, out var handle))
            {
                ref readonly var transform = ref world.Get<Transform3D>(entity);
                simulation.Bodies[handle].Pose.Position = transform.Position;
                simulation.Bodies[handle].Pose.Orientation = transform.Rotation;

                if (world.Has<Velocity3D>(entity))
                {
                    ref readonly var velocity = ref world.Get<Velocity3D>(entity);
                    simulation.Bodies[handle].Velocity.Linear = velocity.Value;
                }

                if (world.Has<AngularVelocity3D>(entity))
                {
                    ref readonly var angVel = ref world.Get<AngularVelocity3D>(entity);
                    simulation.Bodies[handle].Velocity.Angular = angVel.Value;
                }
            }
        }
    }

    private void SyncFromSimulation()
    {
        // Sync dynamic bodies from simulation to ECS
        foreach (var entity in bodyLookup.DynamicEntities)
        {
            if (!world.Has<RigidBody>(entity))
            {
                continue;
            }

            ref readonly var rigidBody = ref world.Get<RigidBody>(entity);

            if (rigidBody.BodyType == RigidBodyType.Dynamic && bodyLookup.TryGetBody(entity, out var handle))
            {
                var bodyRef = simulation.Bodies.GetBodyReference(handle);

                if (world.Has<Transform3D>(entity))
                {
                    ref var transform = ref world.Get<Transform3D>(entity);
                    transform.Position = bodyRef.Pose.Position;
                    transform.Rotation = bodyRef.Pose.Orientation;
                }

                if (world.Has<Velocity3D>(entity))
                {
                    ref var velocity = ref world.Get<Velocity3D>(entity);
                    velocity.Value = bodyRef.Velocity.Linear;
                }

                if (world.Has<AngularVelocity3D>(entity))
                {
                    ref var angVel = ref world.Get<AngularVelocity3D>(entity);
                    angVel.Value = bodyRef.Velocity.Angular;
                }
            }
        }
    }

    #endregion

    #region Body Creation (Internal)

    internal BodyHandle CreateDynamicBody(Entity entity, in Transform3D transform, in PhysicsShape shape, in RigidBody rigidBody, PhysicsMaterial? material)
    {
        var shapeIndex = AddShape(shape);
        var inertia = ComputeInertia(shape, rigidBody.Mass);

        var pose = new RigidPose(transform.Position, transform.Rotation);
        var collidable = new CollidableDescription(shapeIndex, 0.1f);

        var activity = new BodyActivityDescription(
            rigidBody.Activity.SleepThreshold,
            rigidBody.Activity.MinimumTimestepsBeforeSleep);

        var bodyDescription = BodyDescription.CreateDynamic(pose, inertia, collidable, activity);
        var handle = simulation.Bodies.Add(bodyDescription);

        bodyLookup.RegisterBody(entity, handle);
        return handle;
    }

    internal BodyHandle CreateKinematicBody(Entity entity, in Transform3D transform, in PhysicsShape shape)
    {
        var shapeIndex = AddShape(shape);

        var pose = new RigidPose(transform.Position, transform.Rotation);
        var collidable = new CollidableDescription(shapeIndex, 0.1f);

        var bodyDescription = BodyDescription.CreateKinematic(pose, collidable, new BodyActivityDescription(-1));
        var handle = simulation.Bodies.Add(bodyDescription);

        bodyLookup.RegisterBody(entity, handle);
        return handle;
    }

    internal StaticHandle CreateStaticBody(Entity entity, in Transform3D transform, in PhysicsShape shape)
    {
        var shapeIndex = AddShape(shape);

        var pose = new RigidPose(transform.Position, transform.Rotation);
        var staticDescription = new StaticDescription(pose, shapeIndex);
        var handle = simulation.Statics.Add(staticDescription);

        bodyLookup.RegisterStatic(entity, handle);
        return handle;
    }

    internal void RemoveBody(Entity entity)
    {
        // Remove from collision tracking first
        collisionEventManager.RemoveEntity(entity);

        if (bodyLookup.TryGetBody(entity, out var bodyHandle))
        {
            simulation.Bodies.Remove(bodyHandle);
            bodyLookup.Unregister(entity);
        }
        else if (bodyLookup.TryGetStatic(entity, out var staticHandle))
        {
            simulation.Statics.Remove(staticHandle);
            bodyLookup.Unregister(entity);
        }
    }

    private TypedIndex AddShape(in PhysicsShape shape)
    {
        return shape.Type switch
        {
            ShapeType.Sphere => simulation.Shapes.Add(new Sphere(shape.Size.X)),
            ShapeType.Box => simulation.Shapes.Add(new Box(shape.Size.X * 2, shape.Size.Y * 2, shape.Size.Z * 2)),
            ShapeType.Capsule => simulation.Shapes.Add(new Capsule(shape.Size.X, shape.Size.Y * 2)),
            ShapeType.Cylinder => simulation.Shapes.Add(new Cylinder(shape.Size.X, shape.Size.Y * 2)),
            _ => throw new ArgumentException($"Unknown shape type: {shape.Type}")
        };
    }

    private static BodyInertia ComputeInertia(in PhysicsShape shape, float mass)
    {
        return shape.Type switch
        {
            ShapeType.Sphere => new Sphere(shape.Size.X).ComputeInertia(mass),
            ShapeType.Box => new Box(shape.Size.X * 2, shape.Size.Y * 2, shape.Size.Z * 2).ComputeInertia(mass),
            ShapeType.Capsule => new Capsule(shape.Size.X, shape.Size.Y * 2).ComputeInertia(mass),
            ShapeType.Cylinder => new Cylinder(shape.Size.X, shape.Size.Y * 2).ComputeInertia(mass),
            _ => throw new ArgumentException($"Unknown shape type: {shape.Type}")
        };
    }

    #endregion

    /// <summary>
    /// Disposes the physics world and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        collisionEventManager.Clear();
        simulation.Dispose();
        threadDispatcher.Dispose();
        bufferPool.Clear();
        bodyLookup.Clear();
    }

    #region Callback Implementations

    private readonly struct NarrowPhaseCallbacks(PhysicsWorld physicsWorld) : INarrowPhaseCallbacks
    {
        private readonly PhysicsWorld physicsWorld = physicsWorld;

        public readonly void Initialize(Simulation simulation) { }

        public readonly bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            // Get entities for both collidables
            if (!TryGetEntityForCollidable(a, out var entityA) || !TryGetEntityForCollidable(b, out var entityB))
            {
                return true; // Allow collision if we can't identify entities
            }

            // Check collision filters
            var filterA = GetCollisionFilter(entityA);
            var filterB = GetCollisionFilter(entityB);

            return filterA.CanCollideWith(in filterB);
        }

        public readonly bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true; // Already filtered at top level
        }

        public readonly bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            // Get entities for both collidables
            bool hasEntityA = TryGetEntityForCollidable(pair.A, out var entityA);
            bool hasEntityB = TryGetEntityForCollidable(pair.B, out var entityB);

            // Get collision filters
            var filterA = hasEntityA ? GetCollisionFilter(entityA) : CollisionFilter.Default;
            var filterB = hasEntityB ? GetCollisionFilter(entityB) : CollisionFilter.Default;

            bool isTrigger = filterA.IsTriggerCollision(in filterB);

            // Get material properties
            var materialA = GetPhysicsMaterial(entityA, hasEntityA);
            var materialB = GetPhysicsMaterial(entityB, hasEntityB);

            // Combine materials (average friction, max restitution)
            float combinedFriction = (materialA.Friction + materialB.Friction) * 0.5f;
            float combinedRestitution = MathF.Max(materialA.Restitution, materialB.Restitution);

            pairMaterial = new PairMaterialProperties
            {
                FrictionCoefficient = combinedFriction,
                MaximumRecoveryVelocity = combinedRestitution * 2f,
                SpringSettings = new SpringSettings(30, 1)
            };

            // Record collision event if we have both entities
            if (hasEntityA && hasEntityB && manifold.Count > 0)
            {
                // Get contact information from the manifold
                // For generic manifolds, extract the first contact's data
                var poseA = GetPoseForCollidable(pair.A);
                var poseB = GetPoseForCollidable(pair.B);

                // Estimate contact point as midpoint between body centers
                // (actual contact details are in concrete manifold types)
                Vector3 contactPoint = (poseA.Position + poseB.Position) * 0.5f;

                // For generic manifolds, compute normal from body positions
                Vector3 direction = poseB.Position - poseA.Position;
                float length = direction.Length();
                Vector3 contactNormal = length > 0.0001f ? direction / length : Vector3.UnitY;

                // Estimate penetration depth (bodies are overlapping if collision occurred)
                float penetrationDepth = 0.01f; // Default small positive value

                physicsWorld.collisionEventManager.RecordCollision(
                    entityA,
                    entityB,
                    contactNormal,
                    contactPoint,
                    penetrationDepth,
                    isTrigger);
            }

            // Return false for triggers to prevent physical response
            return !isTrigger;
        }

        public readonly bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
        {
            // Compound shapes use this overload
            // Get entities for both collidables
            bool hasEntityA = TryGetEntityForCollidable(pair.A, out var entityA);
            bool hasEntityB = TryGetEntityForCollidable(pair.B, out var entityB);

            // Get collision filters
            var filterA = hasEntityA ? GetCollisionFilter(entityA) : CollisionFilter.Default;
            var filterB = hasEntityB ? GetCollisionFilter(entityB) : CollisionFilter.Default;

            bool isTrigger = filterA.IsTriggerCollision(in filterB);

            // Record collision event if we have both entities and contacts
            if (hasEntityA && hasEntityB && manifold.Count > 0)
            {
                float maxDepth = float.MinValue;
                Vector3 contactPoint = Vector3.Zero;

                for (int i = 0; i < manifold.Count; i++)
                {
                    ref var contact = ref Unsafe.Add(ref manifold.Contact0, i);
                    if (contact.Depth > maxDepth)
                    {
                        maxDepth = contact.Depth;
                        contactPoint = contact.Offset;
                    }
                }

                var poseA = GetPoseForCollidable(pair.A);
                contactPoint = poseA.Position + contactPoint;

                physicsWorld.collisionEventManager.RecordCollision(
                    entityA,
                    entityB,
                    manifold.Normal,
                    contactPoint,
                    maxDepth,
                    isTrigger);
            }

            return !isTrigger;
        }

        public readonly void Dispose() { }

        private readonly bool TryGetEntityForCollidable(CollidableReference collidable, out Entity entity)
        {
            if (collidable.Mobility == CollidableMobility.Static)
            {
                return physicsWorld.bodyLookup.TryGetEntity(collidable.StaticHandle, out entity);
            }
            else
            {
                return physicsWorld.bodyLookup.TryGetEntity(collidable.BodyHandle, out entity);
            }
        }

        private readonly CollisionFilter GetCollisionFilter(Entity entity)
        {
            if (physicsWorld.world.Has<CollisionFilter>(entity))
            {
                return physicsWorld.world.Get<CollisionFilter>(entity);
            }
            return CollisionFilter.Default;
        }

        private readonly PhysicsMaterial GetPhysicsMaterial(Entity entity, bool hasEntity)
        {
            if (hasEntity && physicsWorld.world.Has<PhysicsMaterial>(entity))
            {
                return physicsWorld.world.Get<PhysicsMaterial>(entity);
            }
            return PhysicsMaterial.Default;
        }

        private readonly RigidPose GetPoseForCollidable(CollidableReference collidable)
        {
            if (collidable.Mobility == CollidableMobility.Static)
            {
                return physicsWorld.simulation.Statics[collidable.StaticHandle].Pose;
            }
            else
            {
                return physicsWorld.simulation.Bodies[collidable.BodyHandle].Pose;
            }
        }
    }

    private struct PoseIntegratorCallbacks(Vector3 gravity) : IPoseIntegratorCallbacks
    {
        private Vector3 gravityDt;
        private float linearDampingDt;
        private float angularDampingDt;

        public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public readonly bool AllowSubstepsForUnconstrainedBodies => false;
        public readonly bool IntegrateVelocityForKinematics => false;

        public Vector3 Gravity = gravity;

        public readonly void Initialize(Simulation simulation) { }

        public void PrepareForIntegration(float dt)
        {
            gravityDt = Gravity * dt;
            linearDampingDt = MathF.Pow(1f - 0.01f, dt);
            angularDampingDt = MathF.Pow(1f - 0.01f, dt);
        }

        public readonly void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            Vector3Wide.Broadcast(gravityDt, out var gravityWideDt);
            velocity.Linear += gravityWideDt;
            velocity.Linear *= new Vector<float>(linearDampingDt);
            velocity.Angular *= new Vector<float>(angularDampingDt);
        }
    }

    // S927: Parameter names are dictated by BepuPhysics IRayHitHandler interface
#pragma warning disable S927
    private struct RayHitHandler(PhysicsWorld physicsWorld) : IRayHitHandler
    {
        public bool HasHit;
        public RayHit Hit;

        public readonly bool AllowTest(CollidableReference collidable) => true;

        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => true;

        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (t < maximumT)
            {
                maximumT = t;
                HasHit = true;

                Entity entity;
                if (collidable.Mobility == CollidableMobility.Static)
                {
                    physicsWorld.bodyLookup.TryGetEntity(collidable.StaticHandle, out entity);
                }
                else
                {
                    physicsWorld.bodyLookup.TryGetEntity(collidable.BodyHandle, out entity);
                }

                var position = ray.Origin + ray.Direction * t;
                Hit = new RayHit(entity, position, normal, t);
            }
        }
    }

    // S927: Parameter names are dictated by BepuPhysics ISweepHitHandler interface
    private readonly struct OverlapHandler(PhysicsWorld physicsWorld, List<Entity> results) : ISweepHitHandler
    {
        public readonly bool AllowTest(CollidableReference collidable) => true;

        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => true;
#pragma warning restore S927

        public readonly void OnHit(ref float maximumT, float t, in Vector3 hitLocation, in Vector3 hitNormal, CollidableReference collidable)
        {
            Entity entity;
            if (collidable.Mobility == CollidableMobility.Static)
            {
                physicsWorld.bodyLookup.TryGetEntity(collidable.StaticHandle, out entity);
            }
            else
            {
                physicsWorld.bodyLookup.TryGetEntity(collidable.BodyHandle, out entity);
            }

            if (entity != default && !results.Contains(entity))
            {
                results.Add(entity);
            }
        }

        public readonly void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            OnHit(ref maximumT, 0, Vector3.Zero, Vector3.Zero, collidable);
        }
    }

    #endregion
}
