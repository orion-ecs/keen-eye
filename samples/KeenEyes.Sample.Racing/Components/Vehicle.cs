namespace KeenEyes.Sample.Racing;

/// <summary>
/// Dynamic state of a race car as it drives around the track.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure-data component: it holds the car's speed and steering state
/// but contains no logic. All driving behavior lives in
/// <see cref="VehicleMovementSystem"/>.
/// </para>
/// <para>
/// The component is marked <c>Serializable</c> so it is captured in replay
/// snapshots alongside the car's <see cref="KeenEyes.Common.Transform3D"/>.
/// </para>
/// </remarks>
[Component(Serializable = true)]
public partial struct Vehicle
{
    /// <summary>
    /// Throttle input for this frame, from 0 (coasting) to 1 (full power).
    /// </summary>
    /// <remarks>
    /// In this sample the throttle is supplied by a deterministic, scripted
    /// "driver" so the demo runs unattended and reproducibly. In a real game
    /// this would come from player input.
    /// </remarks>
    public float Throttle;

    /// <summary>
    /// Current forward speed in world units per second.
    /// </summary>
    public float CurrentSpeed;

    /// <summary>
    /// Maximum forward speed in world units per second.
    /// </summary>
    public float MaxSpeed;

    /// <summary>
    /// Acceleration in world units per second squared, applied while the
    /// current speed is below the throttle target.
    /// </summary>
    public float Acceleration;

    /// <summary>
    /// Current steering / heading angle in radians (the direction the car
    /// faces), derived each frame from the tangent of the racing line.
    /// </summary>
    public float Steering;
}
