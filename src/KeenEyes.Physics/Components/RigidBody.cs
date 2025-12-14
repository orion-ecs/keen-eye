using KeenEyes.Common;

namespace KeenEyes.Physics.Components;

/// <summary>
/// Defines the type of a rigid body in the physics simulation.
/// </summary>
public enum RigidBodyType
{
    /// <summary>
    /// A dynamic body that responds to forces and collisions.
    /// </summary>
    Dynamic,

    /// <summary>
    /// A kinematic body that can move but is not affected by forces or collisions.
    /// Kinematic bodies can push dynamic bodies but cannot be pushed.
    /// </summary>
    Kinematic,

    /// <summary>
    /// A static body that never moves. Optimized for scenery and level geometry.
    /// </summary>
    Static
}

/// <summary>
/// Describes the activity state preferences for a rigid body.
/// </summary>
/// <remarks>
/// BepuPhysics uses activity states to optimize performance by sleeping inactive bodies.
/// This struct allows configuring when a body should sleep and when it should stay awake.
/// </remarks>
/// <param name="sleepThreshold">
/// Velocity magnitude threshold below which the body becomes a candidate for sleeping.
/// Higher values cause bodies to sleep sooner. Default is 0.01f.
/// </param>
/// <param name="minimumTimestepsBeforeSleep">
/// Number of consecutive timesteps the body must be below the sleep threshold before sleeping.
/// Higher values make bodies harder to sleep. Default is 32.
/// </param>
public readonly struct ActivityDescription(float sleepThreshold = 0.01f, byte minimumTimestepsBeforeSleep = 32)
{
    /// <summary>
    /// Velocity magnitude threshold below which the body becomes a candidate for sleeping.
    /// </summary>
    public readonly float SleepThreshold = sleepThreshold;

    /// <summary>
    /// Number of consecutive timesteps the body must be below the sleep threshold before sleeping.
    /// </summary>
    public readonly byte MinimumTimestepsBeforeSleep = minimumTimestepsBeforeSleep;

    /// <summary>
    /// Default activity settings optimized for general use.
    /// </summary>
    public static ActivityDescription Default => new();

    /// <summary>
    /// Settings for bodies that should never sleep (always active).
    /// </summary>
    public static ActivityDescription NeverSleep => new(float.MaxValue, byte.MaxValue);
}

/// <summary>
/// Component that marks an entity as a physics-enabled rigid body.
/// </summary>
/// <remarks>
/// <para>
/// Adding this component to an entity will cause the physics system to create a
/// corresponding body in the BepuPhysics simulation. The entity must also have
/// a <see cref="PhysicsShape"/> component to define its collision shape.
/// </para>
/// <para>
/// The physics system uses the entity's <see cref="KeenEyes.Common.Transform3D"/>
/// and <see cref="KeenEyes.Common.Velocity3D"/> components for position and velocity
/// synchronization with the simulation.
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new dynamic rigid body component.
/// </remarks>
/// <param name="mass">The mass of the body in kilograms.</param>
/// <param name="activity">Optional activity settings for sleep behavior.</param>
public struct RigidBody(float mass, ActivityDescription activity = default) : IComponent
{
    /// <summary>
    /// The mass of the body in kilograms. Only used for dynamic bodies.
    /// </summary>
    /// <remarks>
    /// For static and kinematic bodies, this value is ignored.
    /// </remarks>
    public float Mass = mass;

    /// <summary>
    /// The type of rigid body (dynamic, kinematic, or static).
    /// </summary>
    public RigidBodyType BodyType = RigidBodyType.Dynamic;

    /// <summary>
    /// Activity settings controlling when the body sleeps.
    /// </summary>
    public ActivityDescription Activity = activity.SleepThreshold.IsApproximatelyZero() ? ActivityDescription.Default : activity;

    /// <summary>
    /// Creates a dynamic rigid body with the specified mass.
    /// </summary>
    /// <param name="mass">The mass in kilograms.</param>
    /// <returns>A new dynamic RigidBody component.</returns>
    public static RigidBody Dynamic(float mass) => new(mass);

    /// <summary>
    /// Creates a kinematic rigid body.
    /// </summary>
    /// <returns>A new kinematic RigidBody component.</returns>
    public static RigidBody Kinematic() => new() { BodyType = RigidBodyType.Kinematic };

    /// <summary>
    /// Creates a static rigid body.
    /// </summary>
    /// <returns>A new static RigidBody component.</returns>
    public static RigidBody Static() => new() { BodyType = RigidBodyType.Static };
}
