using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests for PhysicsPlugin handling of incomplete entities.
/// These tests cover the null check paths in OnRigidBodyAdded where
/// entities are missing required components (Transform3D or PhysicsShape).
/// </summary>
public class PhysicsPluginIncompleteEntityTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Missing Transform3D Tests

    [Fact]
    public void OnRigidBodyAdded_WithoutTransform3D_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with RigidBody and PhysicsShape but NO Transform3D
        var entity = world.Spawn()
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Physics body should NOT be created (missing Transform3D)
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void OnRigidBodyAdded_WithoutTransform3D_StaticBody_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create static entity without Transform3D
        var entity = world.Spawn()
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        // Physics body should NOT be created
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.StaticCount);
    }

    [Fact]
    public void OnRigidBodyAdded_WithoutTransform3D_KinematicBody_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create kinematic entity without Transform3D
        var entity = world.Spawn()
            .With(PhysicsShape.Capsule(0.5f, 2f))
            .With(RigidBody.Kinematic())
            .Build();

        // Physics body should NOT be created
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void OnRigidBodyAdded_AddingTransform3DLater_DoesNotAutomaticallyCreateBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with RigidBody and PhysicsShape but no Transform3D
        var entity = world.Spawn()
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.False(physics.HasPhysicsBody(entity));

        // Add Transform3D later
        world.Add(entity, new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One));

        // Body is still not created (RigidBody wasn't re-added)
        Assert.False(physics.HasPhysicsBody(entity));
    }

    #endregion

    #region Missing PhysicsShape Tests

    [Fact]
    public void OnRigidBodyAdded_WithoutPhysicsShape_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with Transform3D and RigidBody but NO PhysicsShape
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Physics body should NOT be created (missing PhysicsShape)
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void OnRigidBodyAdded_WithoutPhysicsShape_StaticBody_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create static entity without PhysicsShape
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(RigidBody.Static())
            .Build();

        // Physics body should NOT be created
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.StaticCount);
    }

    [Fact]
    public void OnRigidBodyAdded_WithoutPhysicsShape_KinematicBody_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create kinematic entity without PhysicsShape
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(RigidBody.Kinematic())
            .Build();

        // Physics body should NOT be created
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void OnRigidBodyAdded_AddingPhysicsShapeLater_DoesNotAutomaticallyCreateBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with Transform3D and RigidBody but no PhysicsShape
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.False(physics.HasPhysicsBody(entity));

        // Add PhysicsShape later
        world.Add(entity, PhysicsShape.Sphere(1f));

        // Body is still not created (RigidBody wasn't re-added)
        Assert.False(physics.HasPhysicsBody(entity));
    }

    #endregion

    #region Missing Both Components Tests

    [Fact]
    public void OnRigidBodyAdded_WithoutTransform3DAndPhysicsShape_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with only RigidBody
        var entity = world.Spawn()
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Physics body should NOT be created
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void OnRigidBodyAdded_WithAllComponentsEventually_RequiresRigidBodyReAdd()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with only RigidBody
        var entity = world.Spawn()
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.False(physics.HasPhysicsBody(entity));

        // Add Transform3D
        world.Add(entity, new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One));
        Assert.False(physics.HasPhysicsBody(entity));

        // Add PhysicsShape
        world.Add(entity, PhysicsShape.Sphere(1f));
        Assert.False(physics.HasPhysicsBody(entity));

        // Remove and re-add RigidBody to trigger OnRigidBodyAdded
        var rigidBody = world.Get<RigidBody>(entity);
        world.Remove<RigidBody>(entity);
        world.Add(entity, rigidBody);

        // NOW the physics body should be created
        Assert.True(physics.HasPhysicsBody(entity));
        Assert.Equal(1, physics.BodyCount);
    }

    #endregion

    #region Multiple Incomplete Entities

    [Fact]
    public void OnRigidBodyAdded_MultipleIncompleteEntities_DoesNotCreateAnyBodies()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Entity 1: Missing Transform3D
        world.Spawn()
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Entity 2: Missing PhysicsShape
        world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Entity 3: Missing both
        world.Spawn()
            .With(RigidBody.Dynamic(1f))
            .Build();

        // No physics bodies should be created
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void OnRigidBodyAdded_MixedCompleteAndIncomplete_CreatesOnlyValidBodies()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Complete entity
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Incomplete entity (missing Transform3D)
        world.Spawn()
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Another complete entity
        world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Capsule(0.5f, 2f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Incomplete entity (missing PhysicsShape)
        world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Only 2 complete entities should have physics bodies
        Assert.Equal(2, physics.BodyCount);
    }

    #endregion

    #region With PhysicsMaterial Tests

    [Fact]
    public void OnRigidBodyAdded_WithoutTransform3D_ButWithMaterial_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with PhysicsShape, RigidBody, and PhysicsMaterial but NO Transform3D
        var entity = world.Spawn()
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Rubber)
            .Build();

        // Physics body should NOT be created (missing Transform3D)
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void OnRigidBodyAdded_WithoutPhysicsShape_ButWithMaterial_DoesNotCreatePhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with Transform3D, RigidBody, and PhysicsMaterial but NO PhysicsShape
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Ice)
            .Build();

        // Physics body should NOT be created (missing PhysicsShape)
        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    #endregion

    #region Edge Case: Null PhysicsWorld Tests

    [Fact]
    public void OnRigidBodyAdded_AfterUninstall_DoesNotCrash()
    {
        world = new World();
        var plugin = new PhysicsPlugin();
        world.InstallPlugin(plugin);

        // Uninstall the plugin - this nulls out physicsWorld
        world.UninstallPlugin<PhysicsPlugin>();

        // Now try to add a RigidBody component
        // This should trigger OnRigidBodyAdded with physicsWorld == null
        // The event handler should gracefully return without creating a body
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Should not crash - just verify we can access the entity
        Assert.True(world.IsAlive(entity));
        Assert.True(world.Has<RigidBody>(entity));
    }

    #endregion
}
