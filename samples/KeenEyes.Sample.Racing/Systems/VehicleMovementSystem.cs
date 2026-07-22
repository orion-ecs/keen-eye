using System;
using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// Drives each car along the parametric racing line based on its throttle input.
/// </summary>
/// <remarks>
/// <para>
/// Each frame the system eases the car's current speed toward its throttle
/// target, advances its distance along the track, and writes the resulting
/// world position and heading into the car's <see cref="Transform3D"/>. The
/// <see cref="Transform3D"/> is what the replay recorder captures and what the
/// ghost pipeline later reads back, so keeping it accurate every frame is what
/// makes smooth ghosts possible.
/// </para>
/// </remarks>
public sealed class VehicleMovementSystem : SystemBase
{
    private readonly Track track;

    /// <summary>
    /// Initializes a new instance of the <see cref="VehicleMovementSystem"/> class.
    /// </summary>
    /// <param name="track">The track whose racing line the cars follow.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="track"/> is null.</exception>
    public VehicleMovementSystem(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        this.track = track;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Vehicle, TrackPosition, Transform3D>())
        {
            ref var vehicle = ref World.Get<Vehicle>(entity);
            ref var trackPosition = ref World.Get<TrackPosition>(entity);
            ref var transform = ref World.Get<Transform3D>(entity);

            // Ease the current speed toward the throttle target. Using a tolerance
            // comparison (never == on floats) avoids jitter once the target is reached.
            var targetSpeed = Math.Clamp(vehicle.Throttle, 0f, 1f) * vehicle.MaxSpeed;
            if (!vehicle.CurrentSpeed.ApproximatelyEquals(targetSpeed))
            {
                var step = vehicle.Acceleration * deltaTime;
                if (vehicle.CurrentSpeed < targetSpeed)
                {
                    vehicle.CurrentSpeed = MathF.Min(vehicle.CurrentSpeed + step, targetSpeed);
                }
                else
                {
                    vehicle.CurrentSpeed = MathF.Max(vehicle.CurrentSpeed - step, targetSpeed);
                }
            }

            // Advance along the track and sample the racing line for the new pose.
            trackPosition.Distance += vehicle.CurrentSpeed * deltaTime;

            vehicle.Steering = track.HeadingAt(trackPosition.Distance);
            transform.Position = track.PositionAt(trackPosition.Distance);
            transform.Rotation = Quaternion.CreateFromYawPitchRoll(vehicle.Steering, 0f, 0f);
        }
    }
}
