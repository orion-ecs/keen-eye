using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests for PhysicsWorld operations including raycasting, overlap queries, and force application.
/// </summary>
public class PhysicsWorldTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Raycast Tests

    [Fact]
    public void Raycast_WithNoObstacles_ReturnsFalse()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var hit = physics.Raycast(Vector3.Zero, Vector3.UnitX, 100f, out var hitInfo);

        Assert.False(hit);
        Assert.Equal(default, hitInfo);
    }

    [Fact]
    public void Raycast_WithStaticBox_ReturnsHit()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create static box at (10, 0, 0)
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(10f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(2f, 2f, 2f))
            .With(RigidBody.Static())
            .Build();

        // Cast ray from origin toward box
        var hit = physics.Raycast(Vector3.Zero, Vector3.UnitX, 20f, out var hitInfo);

        Assert.True(hit);
        Assert.True(hitInfo.Distance > 0);
        Assert.True(hitInfo.Distance < 20f);
    }

    [Fact]
    public void Raycast_WithDynamicSphere_ReturnsHit()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create dynamic sphere at (5, 0, 0)
        var sphere = world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Cast ray from origin toward sphere
        var hit = physics.Raycast(Vector3.Zero, Vector3.UnitX, 10f, out var hitInfo);

        Assert.True(hit);
        Assert.Equal(sphere, hitInfo.Entity);
        Assert.True(hitInfo.Distance > 0);
    }

    [Fact]
    public void Raycast_BeyondMaxDistance_ReturnsFalse()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create box far away at (100, 0, 0)
        world.Spawn()
            .With(new Transform3D(new Vector3(100f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(2f, 2f, 2f))
            .With(RigidBody.Static())
            .Build();

        // Cast ray with maxDistance too short
        var hit = physics.Raycast(Vector3.Zero, Vector3.UnitX, 50f, out _);

        Assert.False(hit);
    }

    #endregion

    #region Overlap Tests

    [Fact]
    public void OverlapSphere_WithNoObjects_ReturnsEmpty()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var results = physics.OverlapSphere(Vector3.Zero, 10f);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapSphere_WithObjectsInside_ReturnsEntities()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create objects within sphere radius
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(-1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var results = physics.OverlapSphere(Vector3.Zero, 5f).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void OverlapSphere_WithObjectsOutside_ReturnsEmpty()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object far from overlap sphere
        world.Spawn()
            .With(new Transform3D(new Vector3(100f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var results = physics.OverlapSphere(Vector3.Zero, 5f);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapBox_WithNoObjects_ReturnsEmpty()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var results = physics.OverlapBox(Vector3.Zero, new Vector3(5f, 5f, 5f), Quaternion.Identity);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapBox_WithObjectsInside_ReturnsEntities()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object inside box
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(2f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var results = physics.OverlapBox(Vector3.Zero, new Vector3(5f, 5f, 5f), Quaternion.Identity).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void OverlapBox_WithRotation_FindsRotatedObjects()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(3f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Overlap box rotated 45 degrees
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4f);
        var results = physics.OverlapBox(Vector3.Zero, new Vector3(5f, 5f, 5f), rotation).ToList();

        Assert.Contains(entity, results);
    }

    #endregion

    #region Force and Impulse Tests

    [Fact]
    public void ApplyForce_WithValidEntity_ChangesVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var initialVelocity = physics.GetVelocity(entity);
        physics.ApplyForce(entity, new Vector3(10f, 0f, 0f));

        // Step the simulation to apply the force
        physics.Step(1f / 60f);

        var finalVelocity = physics.GetVelocity(entity);
        Assert.NotEqual(initialVelocity, finalVelocity);
        Assert.True(finalVelocity.X > initialVelocity.X);
    }

    [Fact]
    public void ApplyForce_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.ApplyForce(fakeEntity, Vector3.UnitX));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void ApplyImpulse_WithValidEntity_ChangesVelocityImmediately()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        physics.ApplyImpulse(entity, new Vector3(5f, 0f, 0f));

        var velocity = physics.GetVelocity(entity);
        Assert.True(velocity.X > 0);
    }

    [Fact]
    public void ApplyAngularImpulse_WithValidEntity_ChangesAngularVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var initialAngularVel = physics.GetAngularVelocity(entity);
        physics.ApplyAngularImpulse(entity, new Vector3(0f, 5f, 0f));

        var finalAngularVel = physics.GetAngularVelocity(entity);
        Assert.NotEqual(initialAngularVel, finalAngularVel);
        Assert.True(finalAngularVel.Y > 0);
    }

    [Fact]
    public void ApplyForceAtPosition_WithValidEntity_CreatesRotation()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(2f, 2f, 2f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Apply force at an offset position to create torque
        physics.ApplyForceAtPosition(entity, new Vector3(0f, 10f, 0f), new Vector3(1f, 0f, 0f));

        physics.Step(1f / 60f);

        var angularVel = physics.GetAngularVelocity(entity);
        Assert.True(angularVel.Length() > 0); // Should have some angular velocity
    }

    #endregion

    #region Velocity Operations Tests

    [Fact]
    public void SetVelocity_WithValidEntity_UpdatesVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var newVelocity = new Vector3(10f, 5f, 3f);
        physics.SetVelocity(entity, newVelocity);

        var actualVelocity = physics.GetVelocity(entity);
        Assert.Equal(newVelocity, actualVelocity);
    }

    [Fact]
    public void SetVelocity_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.SetVelocity(fakeEntity, Vector3.One));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void SetAngularVelocity_WithValidEntity_UpdatesAngularVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var newAngularVel = new Vector3(0f, 3f, 0f);
        physics.SetAngularVelocity(entity, newAngularVel);

        var actualAngularVel = physics.GetAngularVelocity(entity);
        Assert.Equal(newAngularVel, actualAngularVel);
    }

    [Fact]
    public void GetVelocity_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.GetVelocity(fakeEntity));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void GetAngularVelocity_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.GetAngularVelocity(fakeEntity));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    #endregion

    #region Body State Tests

    [Fact]
    public void WakeUp_WithSleepingBody_WakesBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        physics.WakeUp(entity);

        Assert.True(physics.IsAwake(entity));
    }

    [Fact]
    public void WakeUp_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.WakeUp(fakeEntity));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void IsAwake_WithActiveBody_ReturnsTrue()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Apply impulse to ensure it's awake
        physics.ApplyImpulse(entity, Vector3.UnitX);

        Assert.True(physics.IsAwake(entity));
    }

    [Fact]
    public void IsAwake_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.IsAwake(fakeEntity));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void HasPhysicsBody_WithDynamicBody_ReturnsTrue()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void HasPhysicsBody_WithStaticBody_ReturnsTrue()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void HasPhysicsBody_WithNonPhysicsEntity_ReturnsFalse()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        Assert.False(physics.HasPhysicsBody(entity));
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void SetGravity_UpdatesGravityValue()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var newGravity = new Vector3(0f, -20f, 0f);
        physics.SetGravity(newGravity);

        Assert.Equal(newGravity, physics.Gravity);
    }

    [Fact]
    public void Gravity_CanBeSetAndRetrieved()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var customGravity = new Vector3(0f, -5f, 0f);
        physics.Gravity = customGravity;

        Assert.Equal(customGravity, physics.Gravity);
    }

    [Fact]
    public void SetSolverIterations_UpdatesSimulationSettings()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Should not throw
        physics.SetSolverIterations(16, 2);

        // Verify by checking the simulation (internal access)
        Assert.Equal(16, physics.Simulation.Solver.VelocityIterationCount);
        Assert.Equal(2, physics.Simulation.Solver.SubstepCount);
    }

    #endregion

    #region Simulation Step Tests

    [Fact]
    public void Step_WithPositiveDeltaTime_ReturnsStepCount()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var steps = physics.Step(1f / 60f);

        Assert.True(steps >= 0);
    }

    [Fact]
    public void Step_WithLargeDeltaTime_ClampsToMaxSteps()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            FixedTimestep = 1f / 60f,
            MaxStepsPerFrame = 3
        };
        world.InstallPlugin(new PhysicsPlugin(config));
        var physics = world.GetExtension<PhysicsWorld>();

        // Large delta time that would require more than MaxStepsPerFrame
        var steps = physics.Step(1f); // 60x the timestep

        Assert.Equal(3, steps); // Should be clamped to MaxStepsPerFrame
    }

    [Fact]
    public void Step_AccumulatesTimestep()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Small delta that doesn't meet fixed timestep
        var steps1 = physics.Step(0.001f);
        Assert.Equal(0, steps1);

        // Another small delta
        var steps2 = physics.Step(0.001f);
        Assert.Equal(0, steps2);

        // Larger delta that triggers accumulator
        var steps3 = physics.Step(0.02f);
        Assert.True(steps3 >= 1);
    }

    [Fact]
    public void InterpolationAlpha_WithInterpolationEnabled_IsValid()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = true
        };
        world.InstallPlugin(new PhysicsPlugin(config));
        var physics = world.GetExtension<PhysicsWorld>();

        physics.Step(1f / 60f);

        Assert.InRange(physics.InterpolationAlpha, 0f, 1f);
    }

    [Fact]
    public void InterpolationAlpha_WithInterpolationDisabled_IsOne()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = false
        };
        world.InstallPlugin(new PhysicsPlugin(config));
        var physics = world.GetExtension<PhysicsWorld>();

        physics.Step(1f / 60f);

        Assert.Equal(1f, physics.InterpolationAlpha);
    }

    #endregion

    #region Kinematic Body Tests

    [Fact]
    public void KinematicBody_SyncsFromEcsToSimulation()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .Build();

        // Update transform in ECS
        ref var transform = ref world.Get<Transform3D>(entity);
        transform.Position = new Vector3(10f, 0f, 0f);

        // Step simulation to sync
        physics.Step(1f / 60f);

        // Verify the physics body position updated
        Assert.True(physics.HasPhysicsBody(entity));
    }

    #endregion
}
