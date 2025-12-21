using System.Numerics;
using KeenEyes.Physics.Components;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Comprehensive tests for physics component structs.
/// </summary>
public class ComponentTests
{
    #region PhysicsMaterial Tests

    [Fact]
    public void PhysicsMaterial_DefaultConstructor_SetsZeroValues()
    {
        // Default struct constructor bypasses primary constructor parameters
        var material = new PhysicsMaterial();

        Assert.Equal(0f, material.Friction);
        Assert.Equal(0f, material.Restitution);
        Assert.Equal(0f, material.LinearDamping);
        Assert.Equal(0f, material.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_ParameterizedConstructor_SetsAllValues()
    {
        var material = new PhysicsMaterial(friction: 0.8f, restitution: 0.9f, linearDamping: 0.05f, angularDamping: 0.02f);

        Assert.Equal(0.8f, material.Friction);
        Assert.Equal(0.9f, material.Restitution);
        Assert.Equal(0.05f, material.LinearDamping);
        Assert.Equal(0.02f, material.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_Default_HasZeroValues()
    {
        // Default static property uses new() which bypasses primary constructor
        var material = PhysicsMaterial.Default;

        Assert.Equal(0f, material.Friction);
        Assert.Equal(0f, material.Restitution);
        Assert.Equal(0f, material.LinearDamping);
        Assert.Equal(0f, material.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_Rubber_HasHighFrictionAndRestitution()
    {
        var rubber = PhysicsMaterial.Rubber;

        Assert.Equal(0.8f, rubber.Friction);
        Assert.Equal(0.8f, rubber.Restitution);
        Assert.Equal(0.01f, rubber.LinearDamping);
        Assert.Equal(0.01f, rubber.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_Ice_HasLowFrictionAndRestitution()
    {
        var ice = PhysicsMaterial.Ice;

        Assert.Equal(0.05f, ice.Friction);
        Assert.Equal(0.1f, ice.Restitution);
        Assert.Equal(0.01f, ice.LinearDamping);
        Assert.Equal(0.01f, ice.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_Metal_HasModerateProperties()
    {
        var metal = PhysicsMaterial.Metal;

        Assert.Equal(0.4f, metal.Friction);
        Assert.Equal(0.5f, metal.Restitution);
        Assert.Equal(0.01f, metal.LinearDamping);
        Assert.Equal(0.01f, metal.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_Wood_HasModerateFrictionLowRestitution()
    {
        var wood = PhysicsMaterial.Wood;

        Assert.Equal(0.6f, wood.Friction);
        Assert.Equal(0.2f, wood.Restitution);
        Assert.Equal(0.01f, wood.LinearDamping);
        Assert.Equal(0.01f, wood.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_CanBeModified()
    {
        var material = PhysicsMaterial.Default;
        material.Friction = 0.99f;
        material.Restitution = 0.01f;
        material.LinearDamping = 0.5f;
        material.AngularDamping = 0.3f;

        Assert.Equal(0.99f, material.Friction);
        Assert.Equal(0.01f, material.Restitution);
        Assert.Equal(0.5f, material.LinearDamping);
        Assert.Equal(0.3f, material.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_PartialConstructor_UsesDefaults()
    {
        var material = new PhysicsMaterial(friction: 1.0f);

        Assert.Equal(1.0f, material.Friction);
        Assert.Equal(0.3f, material.Restitution); // Default
        Assert.Equal(0.01f, material.LinearDamping); // Default
        Assert.Equal(0.01f, material.AngularDamping); // Default
    }

    [Fact]
    public void PhysicsMaterial_ZeroValues_AreValid()
    {
        var material = new PhysicsMaterial(friction: 0f, restitution: 0f, linearDamping: 0f, angularDamping: 0f);

        Assert.Equal(0f, material.Friction);
        Assert.Equal(0f, material.Restitution);
        Assert.Equal(0f, material.LinearDamping);
        Assert.Equal(0f, material.AngularDamping);
    }

    [Fact]
    public void PhysicsMaterial_MaxValues_AreValid()
    {
        var material = new PhysicsMaterial(friction: 1f, restitution: 1f, linearDamping: 10f, angularDamping: 10f);

        Assert.Equal(1f, material.Friction);
        Assert.Equal(1f, material.Restitution);
        Assert.Equal(10f, material.LinearDamping);
        Assert.Equal(10f, material.AngularDamping);
    }

    #endregion

    #region PhysicsShape Tests

    [Fact]
    public void PhysicsShape_Sphere_SetsCorrectTypeAndSize()
    {
        var shape = PhysicsShape.Sphere(2.5f);

        Assert.Equal(ShapeType.Sphere, shape.Type);
        Assert.Equal(2.5f, shape.Size.X);
        Assert.Equal(0f, shape.Size.Y);
        Assert.Equal(0f, shape.Size.Z);
    }

    [Fact]
    public void PhysicsShape_Sphere_ZeroRadius_IsValid()
    {
        var shape = PhysicsShape.Sphere(0f);

        Assert.Equal(ShapeType.Sphere, shape.Type);
        Assert.Equal(0f, shape.Size.X);
    }

    [Fact]
    public void PhysicsShape_Box_WithVector_SetsCorrectHalfExtents()
    {
        var halfExtents = new Vector3(1f, 2f, 3f);
        var shape = PhysicsShape.Box(halfExtents);

        Assert.Equal(ShapeType.Box, shape.Type);
        Assert.Equal(1f, shape.Size.X);
        Assert.Equal(2f, shape.Size.Y);
        Assert.Equal(3f, shape.Size.Z);
    }

    [Fact]
    public void PhysicsShape_Box_WithDimensions_CalculatesHalfExtents()
    {
        var shape = PhysicsShape.Box(width: 4f, height: 6f, depth: 10f);

        Assert.Equal(ShapeType.Box, shape.Type);
        Assert.Equal(2f, shape.Size.X); // Half of 4
        Assert.Equal(3f, shape.Size.Y); // Half of 6
        Assert.Equal(5f, shape.Size.Z); // Half of 10
    }

    [Fact]
    public void PhysicsShape_Box_ZeroDimensions_IsValid()
    {
        var shape = PhysicsShape.Box(0f, 0f, 0f);

        Assert.Equal(ShapeType.Box, shape.Type);
        Assert.Equal(0f, shape.Size.X);
        Assert.Equal(0f, shape.Size.Y);
        Assert.Equal(0f, shape.Size.Z);
    }

    [Fact]
    public void PhysicsShape_Capsule_CalculatesHalfLength()
    {
        var shape = PhysicsShape.Capsule(radius: 1f, length: 5f);

        Assert.Equal(ShapeType.Capsule, shape.Type);
        Assert.Equal(1f, shape.Size.X); // Radius
        // Half length minus the radius caps: (5 - 2*1) * 0.5 = 1.5
        Assert.Equal(1.5f, shape.Size.Y);
        Assert.Equal(0f, shape.Size.Z);
    }

    [Fact]
    public void PhysicsShape_Capsule_MinimumLength_IsValid()
    {
        // Length equal to diameter (2 * radius) should result in zero half-length
        var shape = PhysicsShape.Capsule(radius: 1f, length: 2f);

        Assert.Equal(ShapeType.Capsule, shape.Type);
        Assert.Equal(1f, shape.Size.X);
        Assert.Equal(0f, shape.Size.Y); // (2 - 2*1) * 0.5 = 0
    }

    [Fact]
    public void PhysicsShape_Cylinder_CalculatesHalfLength()
    {
        var shape = PhysicsShape.Cylinder(radius: 2f, length: 8f);

        Assert.Equal(ShapeType.Cylinder, shape.Type);
        Assert.Equal(2f, shape.Size.X); // Radius
        Assert.Equal(4f, shape.Size.Y); // Half length
        Assert.Equal(0f, shape.Size.Z);
    }

    [Fact]
    public void PhysicsShape_Cylinder_ZeroLength_IsValid()
    {
        var shape = PhysicsShape.Cylinder(radius: 1f, length: 0f);

        Assert.Equal(ShapeType.Cylinder, shape.Type);
        Assert.Equal(1f, shape.Size.X);
        Assert.Equal(0f, shape.Size.Y);
    }

    [Fact]
    public void PhysicsShape_DefaultInstance_HasDefaultValues()
    {
        var shape = new PhysicsShape();

        Assert.Equal(default(ShapeType), shape.Type);
        Assert.Equal(Vector3.Zero, shape.Size);
    }

    #endregion

    #region RigidBody Tests

    [Fact]
    public void RigidBody_Constructor_WithMass_SetsDynamicType()
    {
        var body = new RigidBody(mass: 5f);

        Assert.Equal(5f, body.Mass);
        Assert.Equal(RigidBodyType.Dynamic, body.BodyType);
    }

    [Fact]
    public void RigidBody_Constructor_WithActivity_SetsActivity()
    {
        var activity = new ActivityDescription(sleepThreshold: 0.05f, minimumTimestepsBeforeSleep: 64);
        var body = new RigidBody(mass: 10f, activity: activity);

        Assert.Equal(10f, body.Mass);
        Assert.Equal(0.05f, body.Activity.SleepThreshold);
        Assert.Equal(64, body.Activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void RigidBody_Constructor_WithZeroActivity_UsesDefaultActivity()
    {
        var body = new RigidBody(mass: 1f, activity: default);

        Assert.Equal(ActivityDescription.Default.SleepThreshold, body.Activity.SleepThreshold);
        Assert.Equal(ActivityDescription.Default.MinimumTimestepsBeforeSleep, body.Activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void RigidBody_Dynamic_CreatesCorrectBody()
    {
        var body = RigidBody.Dynamic(mass: 7.5f);

        Assert.Equal(7.5f, body.Mass);
        Assert.Equal(RigidBodyType.Dynamic, body.BodyType);
    }

    [Fact]
    public void RigidBody_Kinematic_CreatesCorrectBody()
    {
        var body = RigidBody.Kinematic();

        Assert.Equal(RigidBodyType.Kinematic, body.BodyType);
    }

    [Fact]
    public void RigidBody_Static_CreatesCorrectBody()
    {
        var body = RigidBody.Static();

        Assert.Equal(RigidBodyType.Static, body.BodyType);
    }

    [Fact]
    public void RigidBody_ZeroMass_IsValid()
    {
        var body = new RigidBody(mass: 0f);

        Assert.Equal(0f, body.Mass);
        Assert.Equal(RigidBodyType.Dynamic, body.BodyType);
    }

    [Fact]
    public void RigidBody_NegativeMass_IsValid()
    {
        // Physics engine should handle validation, not the component
        var body = new RigidBody(mass: -1f);

        Assert.Equal(-1f, body.Mass);
    }

    [Fact]
    public void RigidBody_CanModifyBodyType()
    {
        var body = RigidBody.Dynamic(5f);
        body.BodyType = RigidBodyType.Kinematic;

        Assert.Equal(RigidBodyType.Kinematic, body.BodyType);
        Assert.Equal(5f, body.Mass); // Mass unchanged
    }

    #endregion

    #region ActivityDescription Tests

    [Fact]
    public void ActivityDescription_DefaultConstructor_SetsZeroValues()
    {
        // Default struct constructor bypasses primary constructor parameters
        var activity = new ActivityDescription();

        Assert.Equal(0f, activity.SleepThreshold);
        Assert.Equal(0, activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void ActivityDescription_ParameterizedConstructor_SetsAllValues()
    {
        var activity = new ActivityDescription(sleepThreshold: 0.02f, minimumTimestepsBeforeSleep: 64);

        Assert.Equal(0.02f, activity.SleepThreshold);
        Assert.Equal(64, activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void ActivityDescription_Default_HasZeroValues()
    {
        // Default static property uses new() which bypasses primary constructor
        var activity = ActivityDescription.Default;

        Assert.Equal(0f, activity.SleepThreshold);
        Assert.Equal(0, activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void ActivityDescription_NeverSleep_HasMaxValues()
    {
        var activity = ActivityDescription.NeverSleep;

        Assert.Equal(float.MaxValue, activity.SleepThreshold);
        Assert.Equal(byte.MaxValue, activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void ActivityDescription_ZeroThreshold_IsValid()
    {
        var activity = new ActivityDescription(sleepThreshold: 0f, minimumTimestepsBeforeSleep: 10);

        Assert.Equal(0f, activity.SleepThreshold);
        Assert.Equal(10, activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void ActivityDescription_ZeroTimesteps_IsValid()
    {
        var activity = new ActivityDescription(sleepThreshold: 0.01f, minimumTimestepsBeforeSleep: 0);

        Assert.Equal(0.01f, activity.SleepThreshold);
        Assert.Equal(0, activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void ActivityDescription_MaxTimesteps_IsValid()
    {
        var activity = new ActivityDescription(sleepThreshold: 0.01f, minimumTimestepsBeforeSleep: 255);

        Assert.Equal(0.01f, activity.SleepThreshold);
        Assert.Equal(255, activity.MinimumTimestepsBeforeSleep);
    }

    [Fact]
    public void ActivityDescription_NegativeThreshold_IsValid()
    {
        // Component should allow negative values; physics engine validates
        var activity = new ActivityDescription(sleepThreshold: -1f, minimumTimestepsBeforeSleep: 32);

        Assert.Equal(-1f, activity.SleepThreshold);
    }

    #endregion

    #region ShapeType Enum Tests

    [Fact]
    public void ShapeType_HasExpectedValues()
    {
        Assert.Equal(0, (int)ShapeType.Sphere);
        Assert.Equal(1, (int)ShapeType.Box);
        Assert.Equal(2, (int)ShapeType.Capsule);
        Assert.Equal(3, (int)ShapeType.Cylinder);
    }

    [Fact]
    public void ShapeType_AllValuesDefined()
    {
        var values = Enum.GetValues<ShapeType>();

        Assert.Equal(4, values.Length);
        Assert.Contains(ShapeType.Sphere, values);
        Assert.Contains(ShapeType.Box, values);
        Assert.Contains(ShapeType.Capsule, values);
        Assert.Contains(ShapeType.Cylinder, values);
    }

    #endregion

    #region RigidBodyType Enum Tests

    [Fact]
    public void RigidBodyType_HasExpectedValues()
    {
        Assert.Equal(0, (int)RigidBodyType.Dynamic);
        Assert.Equal(1, (int)RigidBodyType.Kinematic);
        Assert.Equal(2, (int)RigidBodyType.Static);
    }

    [Fact]
    public void RigidBodyType_AllValuesDefined()
    {
        var values = Enum.GetValues<RigidBodyType>();

        Assert.Equal(3, values.Length);
        Assert.Contains(RigidBodyType.Dynamic, values);
        Assert.Contains(RigidBodyType.Kinematic, values);
        Assert.Contains(RigidBodyType.Static, values);
    }

    #endregion
}
