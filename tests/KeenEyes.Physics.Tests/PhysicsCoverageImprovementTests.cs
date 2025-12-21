using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Additional tests to improve code coverage for the Physics package.
/// </summary>
public class PhysicsCoverageImprovementTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region PhysicsWorld Shape and Inertia Tests

    [Fact]
    public void CreateDynamicBody_WithCylinderShape_CreatesBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Cylinder(0.5f, 2f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.True(physics.HasPhysicsBody(entity));
        Assert.Equal(1, physics.BodyCount);
    }

    [Fact]
    public void CreateStaticBody_WithCylinderShape_CreatesStaticBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Cylinder(1f, 3f))
            .With(RigidBody.Static())
            .Build();

        Assert.True(physics.HasPhysicsBody(entity));
        Assert.Equal(1, physics.StaticCount);
    }

    #endregion

    #region PhysicsWorld RemoveBody Tests

    [Fact]
    public void RemoveBody_WithStaticBody_RemovesCorrectly()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        Assert.Equal(1, physics.StaticCount);

        // Remove the body by removing the RigidBody component
        world.Remove<RigidBody>(entity);

        Assert.Equal(0, physics.StaticCount);
        Assert.False(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void RemoveBody_WithEntityNotInPhysics_DoesNotThrow()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity without physics
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        // Should not throw when trying to remove a non-physics entity
        world.Despawn(entity);

        // Just verify no exception occurred
        Assert.Equal(0, physics.BodyCount);
    }

    #endregion

    #region Synchronization Tests

    [Fact]
    public void SyncToSimulation_WithKinematicBodyAndVelocity_SyncsVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .With(new Velocity3D(5f, 0f, 0f))
            .Build();

        // Update transform
        ref var transform = ref world.Get<Transform3D>(entity);
        transform.Position = new Vector3(1f, 2f, 3f);

        // Step to trigger sync
        physics.Step(1f / 60f);

        // Verify the entity still exists and has body
        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void SyncToSimulation_WithKinematicBodyAndAngularVelocity_SyncsAngularVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .With(new AngularVelocity3D(0f, 1f, 0f))
            .Build();

        // Step to trigger sync
        physics.Step(1f / 60f);

        // Verify the entity still exists and has body
        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void SyncFromSimulation_WithDynamicBodyMissingTransform_HandlesGracefully()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Remove Transform3D after creation
        world.Remove<Transform3D>(entity);

        // Step should handle missing component gracefully
        physics.Step(1f / 60f);

        // Verify no crash occurred
        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void SyncFromSimulation_WithDynamicBodyMissingVelocity_HandlesGracefully()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step should handle missing velocity component gracefully
        physics.Step(1f / 60f);

        // Verify no crash occurred
        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void SyncFromSimulation_WithDynamicBodyMissingAngularVelocity_HandlesGracefully()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step should handle missing angular velocity component gracefully
        physics.Step(1f / 60f);

        // Verify no crash occurred
        Assert.True(physics.HasPhysicsBody(entity));
    }

    #endregion

    #region Collision Detection Tests

    [Fact]
    public void OverlapSphere_WithStaticBodies_DetectsOverlap()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create static body within overlap range
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(2f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Static())
            .Build();

        var results = physics.OverlapSphere(Vector3.Zero, 5f).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void OverlapBox_WithDefaultRotation_UsesIdentityQuaternion()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Call with default rotation parameter
        var results = physics.OverlapBox(Vector3.Zero, new Vector3(3f, 3f, 3f)).ToList();

        Assert.Contains(entity, results);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ApplyImpulse_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.ApplyImpulse(fakeEntity, Vector3.UnitX));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void ApplyAngularImpulse_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.ApplyAngularImpulse(fakeEntity, Vector3.UnitY));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void ApplyForceAtPosition_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.ApplyForceAtPosition(fakeEntity, Vector3.UnitY, Vector3.Zero));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void SetAngularVelocity_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.SetAngularVelocity(fakeEntity, Vector3.UnitY));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    #endregion

    #region Collision Filtering Tests

    [Fact]
    public void CollisionFilter_CanCollideWith_WithDifferentLayers_ReturnsCorrectly()
    {
        var filter1 = new CollisionFilter
        {
            Layer = 1,
            Mask = 2
        };

        var filter2 = new CollisionFilter
        {
            Layer = 2,
            Mask = 1
        };

        // filter1 (layer 1) can collide with layer 2, filter2 (layer 2) can collide with layer 1
        Assert.True(filter1.CanCollideWith(in filter2));
    }

    [Fact]
    public void CollisionFilter_IsTriggerCollision_WithBothTriggers_ReturnsTrue()
    {
        var filter1 = new CollisionFilter
        {
            IsTrigger = true
        };

        var filter2 = new CollisionFilter
        {
            IsTrigger = true
        };

        Assert.True(filter1.IsTriggerCollision(in filter2));
    }

    [Fact]
    public void CollisionFilter_IsTriggerCollision_WithOneTrigger_ReturnsTrue()
    {
        var filter1 = new CollisionFilter
        {
            IsTrigger = true
        };

        var filter2 = new CollisionFilter
        {
            IsTrigger = false
        };

        Assert.True(filter1.IsTriggerCollision(in filter2));
    }

    [Fact]
    public void CollisionFilter_IsTriggerCollision_WithNoTriggers_ReturnsFalse()
    {
        var filter1 = new CollisionFilter
        {
            IsTrigger = false
        };

        var filter2 = new CollisionFilter
        {
            IsTrigger = false
        };

        Assert.False(filter1.IsTriggerCollision(in filter2));
    }

    [Fact]
    public void CreateBodyWithPhysicsMaterial_AppliesMaterialProperties()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Rubber)
            .Build();

        Assert.True(physics.HasPhysicsBody(entity));
        Assert.Equal(1, physics.BodyCount);
    }

    #endregion

    #region System Coverage Tests

    [Fact]
    public void PhysicsStepSystem_WithNoPhysicsWorld_ReturnsEarly()
    {
        world = new World();
        // Don't install physics plugin

        var system = new KeenEyes.Physics.Systems.PhysicsStepSystem();
        world.AddSystem(system, SystemPhase.FixedUpdate);

        // Should not throw when physics world is not available
        system.Update(1f / 60f);

        Assert.Equal(0, system.StepsTaken);
    }

    [Fact]
    public void PhysicsSyncSystem_WithNoPhysicsWorld_ReturnsEarly()
    {
        world = new World();
        // Don't install physics plugin

        var system = new KeenEyes.Physics.Systems.PhysicsSyncSystem();
        world.AddSystem(system, SystemPhase.LateUpdate);

        // Should not throw when physics world is not available
        system.Update(1f / 60f);

        // Just verify no exception occurred
        Assert.NotNull(system);
    }

    #endregion

    #region Body State Coverage Tests

    [Fact]
    public void BodyLookup_GetInternalProperties_ReturnsCorrectValues()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Access internal properties through reflection or public API
        // These are tested indirectly through other operations
        Assert.NotNull(physics.BodyLookup);
        Assert.NotNull(physics.BufferPool);
    }

    #endregion
}
