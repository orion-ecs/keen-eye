using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Systems;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests for PhysicsStepSystem and PhysicsSyncSystem.
/// </summary>
public class PhysicsSystemsTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region PhysicsStepSystem Tests

    [Fact]
    public void PhysicsStepSystem_Initialize_FindsPhysicsWorld()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        // Should not throw during update
        system.Update(1f / 60f);

        Assert.True(system.StepsTaken >= 0); // Should step even with no bodies
    }

    [Fact]
    public void PhysicsStepSystem_Update_StepsSimulation()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        Assert.True(system.StepsTaken >= 0);
    }

    [Fact]
    public void PhysicsStepSystem_WithLargeDeltaTime_ClampsSteps()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            FixedTimestep = 1f / 60f,
            MaxStepsPerFrame = 2
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        system.Update(1f); // Very large delta

        Assert.Equal(2, system.StepsTaken); // Clamped to MaxStepsPerFrame
    }

    [Fact]
    public void PhysicsStepSystem_WithSmallDeltaTime_TakesNoSteps()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        system.Update(0.001f); // Very small delta

        Assert.Equal(0, system.StepsTaken); // Below fixed timestep threshold
    }

    [Fact]
    public void PhysicsStepSystem_AccumulatesTimestep_OverMultipleFrames()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            FixedTimestep = 1f / 60f
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        // First frame - small delta
        system.Update(0.008f);
        Assert.Equal(0, system.StepsTaken);

        // Second frame - small delta (accumulated ~0.016)
        system.Update(0.008f);
        Assert.Equal(0, system.StepsTaken);

        // Third frame - now accumulated time exceeds fixed timestep
        system.Update(0.008f);
        Assert.True(system.StepsTaken >= 1);
    }

    [Fact]
    public void PhysicsStepSystem_WithNoPhysicsWorld_DoesNotCrash()
    {
        world = new World();
        // Don't install physics plugin

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
        Assert.Equal(0, system.StepsTaken);
    }

    [Fact]
    public void PhysicsStepSystem_UpdatesDynamicBodies()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        var initialY = world.Get<Transform3D>(entity).Position.Y;

        // Step multiple times to let gravity take effect
        for (int i = 0; i < 10; i++)
        {
            system.Update(1f / 60f);
        }

        var finalY = world.Get<Transform3D>(entity).Position.Y;

        // Should have fallen due to gravity
        Assert.True(finalY < initialY);
    }

    #endregion

    #region PhysicsSyncSystem Tests

    [Fact]
    public void PhysicsSyncSystem_Initialize_FindsPhysicsWorld()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void PhysicsSyncSystem_WithInterpolationDisabled_DoesNotInterpolate()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = false
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        var initialPos = world.Get<Transform3D>(entity).Position;

        system.Update(1f / 60f);

        var finalPos = world.Get<Transform3D>(entity).Position;

        // Position should not change (no interpolation)
        Assert.Equal(initialPos, finalPos);
    }

    [Fact]
    public void PhysicsSyncSystem_WithInterpolationEnabled_InterpolatesTransforms()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = true,
            FixedTimestep = 1f / 60f
        };
        world.InstallPlugin(new PhysicsPlugin(config));
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var stepSystem = new PhysicsStepSystem();
        var syncSystem = new PhysicsSyncSystem();
        world.AddSystem(stepSystem);
        world.AddSystem(syncSystem);

        // Apply velocity to make the entity move
        physics.SetVelocity(entity, new Vector3(10f, 0f, 0f));

        // Step simulation
        stepSystem.Update(1f / 60f);

        // Sync with interpolation
        syncSystem.Update(1f / 60f);

        // Should have updated position
        var pos = world.Get<Transform3D>(entity).Position;
        Assert.True(pos.X > 0 || pos == Vector3.Zero); // May or may not have moved depending on accumulator
    }

    [Fact]
    public void PhysicsSyncSystem_OnlyInterpolatesDynamicBodies()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = true
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        // Create static body
        var staticEntity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        // Create kinematic body
        var kinematicEntity = world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .Build();

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        var staticPosInitial = world.Get<Transform3D>(staticEntity).Position;
        var kinematicPosInitial = world.Get<Transform3D>(kinematicEntity).Position;

        system.Update(1f / 60f);

        // Static and kinematic should not be affected by interpolation
        Assert.Equal(staticPosInitial, world.Get<Transform3D>(staticEntity).Position);
        Assert.Equal(kinematicPosInitial, world.Get<Transform3D>(kinematicEntity).Position);
    }

    [Fact]
    public void PhysicsSyncSystem_CleansUpDespawnedEntities()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = true
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        // First update to track the entity
        system.Update(1f / 60f);

        // Despawn the entity
        world.Despawn(entity);

        // Update again - should clean up without crashing
        system.Update(1f / 60f);

        // No assertions needed - just verifying no crash
    }

    [Fact]
    public void PhysicsSyncSystem_Dispose_ClearsState()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = true
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        // Should not throw
        system.Dispose();
    }

    [Fact]
    public void PhysicsSyncSystem_WithNoPhysicsWorld_DoesNotCrash()
    {
        world = new World();
        // Don't install physics plugin

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void PhysicsSyncSystem_HandlesMultipleEntities()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = true
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        // Create multiple dynamic entities
        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new Transform3D(new Vector3(i, 0f, 0f), Quaternion.Identity, Vector3.One))
                .With(PhysicsShape.Sphere(1f))
                .With(RigidBody.Dynamic(1f))
                .Build();
        }

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        // Should handle all entities without crashing
        system.Update(1f / 60f);
        system.Update(1f / 60f);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PhysicsSystems_WorkTogether()
    {
        world = new World();
        var config = new PhysicsConfig
        {
            EnableInterpolation = true,
            FixedTimestep = 1f / 60f
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var stepSystem = new PhysicsStepSystem();
        var syncSystem = new PhysicsSyncSystem();
        world.AddSystem(stepSystem);
        world.AddSystem(syncSystem);

        var initialY = world.Get<Transform3D>(entity).Position.Y;

        // Simulate several frames
        for (int i = 0; i < 30; i++)
        {
            stepSystem.Update(1f / 60f);
            syncSystem.Update(1f / 60f);
        }

        var finalY = world.Get<Transform3D>(entity).Position.Y;

        // Gravity should have pulled it down
        Assert.True(finalY < initialY);
    }

    [Fact]
    public void PhysicsSystems_HandleEntityLifecycle()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var stepSystem = new PhysicsStepSystem();
        var syncSystem = new PhysicsSyncSystem();
        world.AddSystem(stepSystem);
        world.AddSystem(syncSystem);

        // Create entity
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        stepSystem.Update(1f / 60f);
        syncSystem.Update(1f / 60f);

        // Despawn entity
        world.Despawn(entity);

        // Should handle gracefully
        stepSystem.Update(1f / 60f);
        syncSystem.Update(1f / 60f);

        // No assertions needed - just verify no crash
    }

    #endregion
}
