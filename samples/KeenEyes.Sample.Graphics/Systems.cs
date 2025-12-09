using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Sample.Graphics;

/// <summary>
/// System that rotates entities with the Spin component.
/// </summary>
public class SpinSystem : SystemBase
{
    /// <summary>
    /// Updates the rotation of spinning entities.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update.</param>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Spin>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var spin = ref World.Get<Spin>(entity);

            // Create rotation quaternion from euler angles
            var rotation = Quaternion.CreateFromYawPitchRoll(
                spin.Speed.Y * deltaTime,
                spin.Speed.X * deltaTime,
                spin.Speed.Z * deltaTime);

            // Apply rotation
            transform.Rotation = Quaternion.Normalize(transform.Rotation * rotation);
        }
    }
}

/// <summary>
/// System that bobs entities up and down.
/// </summary>
public class BobSystem : SystemBase
{
    /// <summary>
    /// Updates the position of bobbing entities.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update.</param>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Bob>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var bob = ref World.Get<Bob>(entity);

            // Update phase
            bob.Phase += bob.Frequency * deltaTime * MathF.PI * 2f;

            // Calculate new Y position
            var newY = bob.OriginY + MathF.Sin(bob.Phase) * bob.Amplitude;

            // Update transform
            transform.Position = new Vector3(
                transform.Position.X,
                newY,
                transform.Position.Z);
        }
    }
}
