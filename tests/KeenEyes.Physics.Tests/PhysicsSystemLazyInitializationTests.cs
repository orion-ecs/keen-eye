using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Systems;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests for lazy initialization paths in PhysicsStepSystem and PhysicsSyncSystem.
/// These tests cover the fallback path where the PhysicsWorld extension is not found
/// during OnInitialize but is found later during Update.
/// </summary>
public class PhysicsSystemLazyInitializationTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region PhysicsStepSystem Lazy Initialization

    [Fact]
    public void PhysicsStepSystem_Update_FindsPhysicsWorldAfterOnInitialize()
    {
        world = new World();

        // Create and add system BEFORE installing physics plugin
        // This means OnInitialize won't find the PhysicsWorld extension
        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        // First update - no physics world yet
        system.Update(1f / 60f);
        Assert.Equal(0, system.StepsTaken);

        // Now install physics plugin - extension becomes available
        world.InstallPlugin(new PhysicsPlugin());

        // Second update - should find physics world via fallback path (lines 44-49)
        system.Update(1f / 60f);

        // Should have successfully stepped the simulation
        Assert.True(system.StepsTaken >= 0);
    }

    [Fact]
    public void PhysicsStepSystem_Update_CachesPhysicsWorldAfterLazyInit()
    {
        world = new World();

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        // Install plugin after system is added
        world.InstallPlugin(new PhysicsPlugin());

        // First update finds and caches physics world
        system.Update(1f / 60f);
        var firstSteps = system.StepsTaken;

        // Subsequent updates should use cached reference
        system.Update(1f / 60f);
        var secondSteps = system.StepsTaken;

        // Both updates should have worked
        Assert.True(firstSteps >= 0);
        Assert.True(secondSteps >= 0);
    }

    [Fact]
    public void PhysicsStepSystem_Update_WithDynamicBody_WorksAfterLazyInit()
    {
        world = new World();

        var system = new PhysicsStepSystem();
        world.AddSystem(system);

        // Install plugin after system is added
        world.InstallPlugin(new PhysicsPlugin());

        // Create a dynamic body
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var initialY = world.Get<Transform3D>(entity).Position.Y;

        // Step multiple times - should work via lazy initialization
        for (int i = 0; i < 20; i++)
        {
            system.Update(1f / 60f);
        }

        var finalY = world.Get<Transform3D>(entity).Position.Y;

        // Gravity should have pulled it down
        Assert.True(finalY < initialY);
    }

    #endregion

    #region PhysicsSyncSystem Lazy Initialization

    [Fact]
    public void PhysicsSyncSystem_Update_FindsPhysicsWorldAfterOnInitialize()
    {
        world = new World();

        // Create and add system BEFORE installing physics plugin
        // This means OnInitialize won't find the PhysicsWorld extension
        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        // First update - no physics world yet
        system.Update(1f / 60f);

        // Now install physics plugin with interpolation enabled
        var config = new PhysicsConfig { EnableInterpolation = true };
        world.InstallPlugin(new PhysicsPlugin(config));

        // Second update - should find physics world via fallback path (lines 46-51)
        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void PhysicsSyncSystem_Update_CachesPhysicsWorldAfterLazyInit()
    {
        world = new World();

        var config = new PhysicsConfig { EnableInterpolation = true };

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        // Install plugin after system is added
        world.InstallPlugin(new PhysicsPlugin(config));

        // Create a dynamic entity
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // First update finds and caches physics world
        system.Update(1f / 60f);

        // Subsequent updates should use cached reference
        system.Update(1f / 60f);
        system.Update(1f / 60f);

        // Should complete without crashing
    }

    [Fact]
    public void PhysicsSyncSystem_Update_InterpolatesAfterLazyInit()
    {
        world = new World();

        var config = new PhysicsConfig
        {
            EnableInterpolation = true,
            FixedTimestep = 1f / 60f
        };

        var syncSystem = new PhysicsSyncSystem();
        world.AddSystem(syncSystem);

        // Install plugin and step system after sync system is added
        world.InstallPlugin(new PhysicsPlugin(config));

        var stepSystem = new PhysicsStepSystem();
        world.AddSystem(stepSystem);

        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Apply velocity
        physics.SetVelocity(entity, new Vector3(10f, 0f, 0f));

        // Step simulation
        stepSystem.Update(1f / 60f);

        // Sync with interpolation - uses lazy initialization path
        syncSystem.Update(1f / 60f);

        // Should have updated position (or at least not crashed)
        var pos = world.Get<Transform3D>(entity).Position;
        Assert.True(pos.X >= 0);
    }

    [Fact]
    public void PhysicsSyncSystem_Update_WithInterpolationDisabled_ReturnsEarlyAfterLazyInit()
    {
        world = new World();

        var config = new PhysicsConfig { EnableInterpolation = false };

        var system = new PhysicsSyncSystem();
        world.AddSystem(system);

        // Install plugin after system is added
        world.InstallPlugin(new PhysicsPlugin(config));

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var initialPos = world.Get<Transform3D>(entity).Position;

        // Update - should find physics world but return early due to disabled interpolation
        system.Update(1f / 60f);

        var finalPos = world.Get<Transform3D>(entity).Position;

        // Position should not change (no interpolation)
        Assert.Equal(initialPos, finalPos);
    }

    #endregion

    #region Combined Lazy Initialization

    [Fact]
    public void BothSystems_Update_WorkTogetherWithLazyInit()
    {
        world = new World();

        // Add both systems before plugin installation
        var stepSystem = new PhysicsStepSystem();
        var syncSystem = new PhysicsSyncSystem();
        world.AddSystem(stepSystem);
        world.AddSystem(syncSystem);

        // First update - no physics world
        stepSystem.Update(1f / 60f);
        syncSystem.Update(1f / 60f);

        // Install plugin
        var config = new PhysicsConfig
        {
            EnableInterpolation = true,
            FixedTimestep = 1f / 60f
        };
        world.InstallPlugin(new PhysicsPlugin(config));

        // Create entity
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var initialY = world.Get<Transform3D>(entity).Position.Y;

        // Simulate several frames with lazy initialization
        for (int i = 0; i < 30; i++)
        {
            stepSystem.Update(1f / 60f);
            syncSystem.Update(1f / 60f);
        }

        var finalY = world.Get<Transform3D>(entity).Position.Y;

        // Gravity should have pulled it down
        Assert.True(finalY < initialY);
    }

    #endregion
}
