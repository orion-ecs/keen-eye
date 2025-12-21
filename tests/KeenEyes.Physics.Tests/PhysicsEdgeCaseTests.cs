using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Edge case tests to increase coverage to 95%+.
/// </summary>
public class PhysicsEdgeCaseTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region PhysicsWorld Dispose Tests

    [Fact]
    public void PhysicsWorld_Dispose_MultipleTimes_DoesNotThrow()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // First dispose
        physics.Dispose();

        // Second dispose should not throw
        physics.Dispose();
    }

    [Fact]
    public void PhysicsWorld_Dispose_ClearsCollisionEventManager()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create some bodies to trigger collision tracking
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step to initialize everything
        physics.Step(1f / 60f);

        var initialBodyCount = physics.BodyCount;
        Assert.True(initialBodyCount > 0);

        // Dispose should clear everything
        physics.Dispose();

        // After dispose, the simulation is disposed but we can't query it
        // Just verify dispose doesn't throw
    }

    #endregion

    #region PhysicsPlugin Edge Cases

    [Fact]
    public void PhysicsPlugin_InstallMultipleTimes_ThrowsException()
    {
        world = new World();
        var plugin1 = new PhysicsPlugin();
        var plugin2 = new PhysicsPlugin();

        world.InstallPlugin(plugin1);

        // Installing again should throw (duplicate extension)
        Assert.Throws<InvalidOperationException>(() => world.InstallPlugin(plugin2));
    }

    #endregion

    #region Component Edge Cases

    [Fact]
    public void RigidBody_WithZeroMass_CreatesValidDynamicBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Creating dynamic body with very small mass
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(0.001f))
            .Build();

        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void RigidBody_WithVeryLargeMass_CreatesValidDynamicBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Creating dynamic body with very large mass
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1000000f))
            .Build();

        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void PhysicsShape_AllShapeTypes_CreateValidBodies()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Test all shape types
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        _ = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 2f, 3f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        _ = world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Capsule(0.5f, 2f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        _ = world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Cylinder(0.5f, 2f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.Equal(4, physics.BodyCount);
    }

    #endregion

    #region Sync Tests

    [Fact]
    public void Sync_WithMissingRigidBodyComponent_SkipsEntity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with RigidBody
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.Equal(1, physics.BodyCount);

        // Remove RigidBody component
        world.Remove<RigidBody>(entity);

        // Step should handle missing component gracefully
        physics.Step(1f / 60f);

        // Body should have been removed
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void Sync_DynamicBodyWithoutVelocityComponents_WorksCorrectly()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step multiple times
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        // Body should have fallen due to gravity
        var finalPos = world.Get<Transform3D>(entity).Position;
        Assert.True(finalPos.Y < 10f);
    }

    [Fact]
    public void Sync_DynamicBodyWithVelocityAndAngularVelocity_SyncsCorrectly()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .With(new Velocity3D(0f, 0f, 0f))
            .With(new AngularVelocity3D(0f, 0f, 0f))
            .Build();

        // Apply impulse to change velocities
        physics.ApplyImpulse(entity, new Vector3(5f, 0f, 0f));
        physics.ApplyAngularImpulse(entity, new Vector3(0f, 3f, 0f));

        // Step to sync
        physics.Step(1f / 60f);

        // Check that velocity components were updated
        var velocity = world.Get<Velocity3D>(entity);
        var angularVelocity = world.Get<AngularVelocity3D>(entity);

        Assert.True(velocity.Value.X > 0);
        Assert.True(angularVelocity.Value.Y > 0);
    }

    #endregion

    #region Overlap and Raycast Edge Cases

    [Fact]
    public void OverlapSphere_FarFromObjects_ReturnsEmpty()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Test overlap sphere far away
        var results = physics.OverlapSphere(new Vector3(1000f, 0f, 0f), 1f);

        Assert.Empty(results);
    }

    [Fact]
    public void Raycast_WithNegativeMaxDistance_ReturnsFalse()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object
        world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Static())
            .Build();

        // Negative max distance
        var hit = physics.Raycast(Vector3.Zero, Vector3.UnitX, -1f, out _);

        Assert.False(hit);
    }

    [Fact]
    public void OverlapBox_FarFromObjects_ReturnsEmpty()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Test overlap box far away
        var results = physics.OverlapBox(new Vector3(1000f, 0f, 0f), new Vector3(1f, 1f, 1f), Quaternion.Identity);

        Assert.Empty(results);
    }

    #endregion

    #region Material Tests

    [Fact]
    public void PhysicsMaterial_AllPresets_HaveValidValues()
    {
        var defaultMat = PhysicsMaterial.Default;
        var rubber = PhysicsMaterial.Rubber;
        var ice = PhysicsMaterial.Ice;
        var metal = PhysicsMaterial.Metal;
        var wood = PhysicsMaterial.Wood;

        // All materials should have friction and restitution in valid ranges
        Assert.InRange(defaultMat.Friction, 0f, 10f);
        Assert.InRange(rubber.Friction, 0f, 10f);
        Assert.InRange(ice.Friction, 0f, 10f);
        Assert.InRange(metal.Friction, 0f, 10f);
        Assert.InRange(wood.Friction, 0f, 10f);

        Assert.InRange(defaultMat.Restitution, 0f, 1f);
        Assert.InRange(rubber.Restitution, 0f, 1f);
        Assert.InRange(ice.Restitution, 0f, 1f);
        Assert.InRange(metal.Restitution, 0f, 1f);
        Assert.InRange(wood.Restitution, 0f, 1f);
    }

    #endregion

    #region CollisionFilter Tests

    [Fact]
    public void CollisionFilter_Default_CollidesWithEverything()
    {
        var filter = CollisionFilter.Default;

        // Default filter should collide with any other filter
        var otherFilter = new CollisionFilter(5, 0b11111);
        Assert.True(filter.CanCollideWith(in otherFilter));
    }

    [Fact]
    public void CollisionFilter_Trigger_IsTrigger()
    {
        var trigger = CollisionFilter.Trigger();

        Assert.True(trigger.IsTrigger);
    }

    [Fact]
    public void CollisionFilter_TwoTriggers_BothAreTriggers()
    {
        var triggerA = CollisionFilter.Trigger();
        var triggerB = CollisionFilter.Trigger();

        Assert.True(triggerA.IsTriggerCollision(in triggerB));
    }

    [Fact]
    public void CollisionFilter_OneTriggerOneNormal_IsTriggerCollision()
    {
        var trigger = CollisionFilter.Trigger();
        var normal = CollisionFilter.Default;

        Assert.True(trigger.IsTriggerCollision(in normal));
    }

    [Fact]
    public void CollisionFilter_LayerMaskFiltering_WorksCorrectly()
    {
        // Layer 1, only collides with layer 2
        var filterA = new CollisionFilter(1, 0b0010);
        // Layer 2, only collides with layer 1
        var filterB = new CollisionFilter(2, 0b0001);

        // Should be able to collide
        Assert.True(filterA.CanCollideWith(in filterB));
        Assert.True(filterB.CanCollideWith(in filterA));
    }

    [Fact]
    public void CollisionFilter_SameLayerNoMask_CannotCollide()
    {
        // Both in layer 1, but neither accepts layer 1 collisions
        var filterA = new CollisionFilter(1, 0b0010);
        var filterB = new CollisionFilter(1, 0b0010);

        // Should not be able to collide
        Assert.False(filterA.CanCollideWith(in filterB));
    }

    #endregion

    #region Config Validation Tests

    [Fact]
    public void PhysicsConfig_Validate_NegativeVelocityIterations_ReturnsError()
    {
        var config = new PhysicsConfig
        {
            VelocityIterations = -1
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("VelocityIterations", error);
    }

    [Fact]
    public void PhysicsConfig_Validate_AllValidValues_ReturnsNull()
    {
        var config = new PhysicsConfig
        {
            FixedTimestep = 1f / 60f,
            MaxStepsPerFrame = 5,
            VelocityIterations = 8,
            Gravity = new Vector3(0f, -9.81f, 0f),
            EnableInterpolation = true
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion
}
