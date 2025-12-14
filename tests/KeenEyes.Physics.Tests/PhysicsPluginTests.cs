using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests for the PhysicsPlugin integration with the World.
/// </summary>
public class PhysicsPluginTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Installation Tests

    [Fact]
    public void Install_WithDefaultConfig_Succeeds()
    {
        world = new World();

        // Should not throw
        world.InstallPlugin(new PhysicsPlugin());

        // Should have the extension
        Assert.True(world.TryGetExtension<PhysicsWorld>(out _));
    }

    [Fact]
    public void Install_WithCustomConfig_Succeeds()
    {
        world = new World();

        var config = new PhysicsConfig
        {
            FixedTimestep = 1f / 120f,
            Gravity = new Vector3(0, -20f, 0),
            MaxStepsPerFrame = 5
        };

        world.InstallPlugin(new PhysicsPlugin(config));

        Assert.True(world.TryGetExtension<PhysicsWorld>(out var physics));
        Assert.NotNull(physics);
        Assert.Equal(1f / 120f, physics.Config.FixedTimestep);
    }

    [Fact]
    public void Install_WithInvalidConfig_ThrowsArgumentException()
    {
        world = new World();

        var config = new PhysicsConfig
        {
            FixedTimestep = -1f // Invalid
        };

        var ex = Assert.Throws<ArgumentException>(() => world.InstallPlugin(new PhysicsPlugin(config)));
        Assert.Contains("Invalid PhysicsConfig", ex.Message);
    }

    #endregion

    #region Uninstallation Tests

    [Fact]
    public void Uninstall_RemovesExtension()
    {
        world = new World();
        var plugin = new PhysicsPlugin();

        world.InstallPlugin(plugin);
        Assert.True(world.TryGetExtension<PhysicsWorld>(out _));

        world.UninstallPlugin<PhysicsPlugin>();
        Assert.False(world.TryGetExtension<PhysicsWorld>(out _));
    }

    #endregion

    #region Body Creation Tests

    [Fact]
    public void AddRigidBody_CreatesPhysicsBody()
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
        Assert.Equal(1, physics.BodyCount);
    }

    [Fact]
    public void AddStaticBody_CreatesStaticPhysicsBody()
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
        Assert.Equal(1, physics.StaticCount);
    }

    [Fact]
    public void RemoveRigidBody_RemovesPhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.Equal(1, physics.BodyCount);

        world.Remove<RigidBody>(entity);

        Assert.False(physics.HasPhysicsBody(entity));
        Assert.Equal(0, physics.BodyCount);
    }

    [Fact]
    public void DespawnEntity_RemovesPhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.Equal(1, physics.BodyCount);

        world.Despawn(entity);

        Assert.Equal(0, physics.BodyCount);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void PhysicsConfig_Validate_ValidConfig_ReturnsNull()
    {
        var config = new PhysicsConfig
        {
            FixedTimestep = 1f / 60f,
            MaxStepsPerFrame = 3,
            VelocityIterations = 8
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void PhysicsConfig_Validate_NegativeTimestep_ReturnsError()
    {
        var config = new PhysicsConfig
        {
            FixedTimestep = -1f
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("FixedTimestep", error);
    }

    [Fact]
    public void PhysicsConfig_Validate_TooLargeTimestep_ReturnsError()
    {
        var config = new PhysicsConfig
        {
            FixedTimestep = 0.5f // Too large
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("FixedTimestep", error);
    }

    [Fact]
    public void PhysicsConfig_Validate_ZeroMaxSteps_ReturnsError()
    {
        var config = new PhysicsConfig
        {
            MaxStepsPerFrame = 0
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxStepsPerFrame", error);
    }

    #endregion

    #region Component Tests

    [Fact]
    public void RigidBody_Dynamic_CreatesCorrectType()
    {
        var body = RigidBody.Dynamic(5f);

        Assert.Equal(RigidBodyType.Dynamic, body.BodyType);
        Assert.Equal(5f, body.Mass);
    }

    [Fact]
    public void RigidBody_Static_CreatesCorrectType()
    {
        var body = RigidBody.Static();

        Assert.Equal(RigidBodyType.Static, body.BodyType);
    }

    [Fact]
    public void RigidBody_Kinematic_CreatesCorrectType()
    {
        var body = RigidBody.Kinematic();

        Assert.Equal(RigidBodyType.Kinematic, body.BodyType);
    }

    [Fact]
    public void PhysicsShape_Sphere_CreatesCorrectShape()
    {
        var shape = PhysicsShape.Sphere(2.5f);

        Assert.Equal(ShapeType.Sphere, shape.Type);
        Assert.Equal(2.5f, shape.Size.X);
    }

    [Fact]
    public void PhysicsShape_Box_CreatesCorrectShape()
    {
        var shape = PhysicsShape.Box(2f, 4f, 6f);

        Assert.Equal(ShapeType.Box, shape.Type);
        Assert.Equal(1f, shape.Size.X); // Half extents
        Assert.Equal(2f, shape.Size.Y);
        Assert.Equal(3f, shape.Size.Z);
    }

    [Fact]
    public void PhysicsShape_Capsule_CreatesCorrectShape()
    {
        var shape = PhysicsShape.Capsule(1f, 4f);

        Assert.Equal(ShapeType.Capsule, shape.Type);
        Assert.Equal(1f, shape.Size.X); // Radius
    }

    [Fact]
    public void PhysicsMaterial_Default_HasReasonableValues()
    {
        var material = PhysicsMaterial.Default;

        Assert.InRange(material.Friction, 0f, 1f);
        Assert.InRange(material.Restitution, 0f, 1f);
    }

    [Fact]
    public void PhysicsMaterial_Rubber_HasHighBounce()
    {
        var rubber = PhysicsMaterial.Rubber;
        var ice = PhysicsMaterial.Ice;

        Assert.True(rubber.Restitution > ice.Restitution);
        Assert.True(rubber.Friction > ice.Friction);
    }

    #endregion

    #region Shape Tests

    [Fact]
    public void AllShapeTypes_CreateValidBodies()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Sphere
        world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Box
        world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Capsule
        world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Capsule(0.5f, 2f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Cylinder
        world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Cylinder(0.5f, 2f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.Equal(4, physics.BodyCount);
    }

    #endregion
}
