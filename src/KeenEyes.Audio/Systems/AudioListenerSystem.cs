using System.Numerics;
using KeenEyes.Audio.Abstractions;
using KeenEyes.Common;

namespace KeenEyes.Audio;

/// <summary>
/// System that synchronizes the <see cref="AudioListener"/> component with the backend listener.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the Update phase and updates the backend audio listener
/// position, orientation, and velocity based on the entity's <see cref="Transform3D"/>.
/// </para>
/// <para>
/// Only one active listener is supported. If multiple entities have active
/// <see cref="AudioListener"/> components, the first one found is used.
/// </para>
/// </remarks>
public sealed class AudioListenerSystem : ISystem
{
    private IWorld? world;
    private IAudioContext? audioContext;
    private Vector3 previousPosition;

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Lazy initialization
        audioContext ??= world?.TryGetExtension<IAudioContext>(out var ctx) == true ? ctx : null;

        if (audioContext?.Device is null || world is null)
        {
            return;
        }

        var device = audioContext.Device;

        foreach (var entity in world.Query<AudioListener, Transform3D>())
        {
            ref readonly var listener = ref world.Get<AudioListener>(entity);
            if (!listener.IsActive)
            {
                continue;
            }

            ref readonly var transform = ref world.Get<Transform3D>(entity);

            // Update listener position
            device.SetListenerPosition(transform.Position);

            // Compute velocity from position delta for Doppler effect
            if (deltaTime > 0)
            {
                var velocity = (transform.Position - previousPosition) / deltaTime;
                device.SetListenerVelocity(velocity);
            }
            previousPosition = transform.Position;

            // Update orientation from rotation quaternion
            var forward = Vector3.Transform(-Vector3.UnitZ, transform.Rotation);
            var up = Vector3.Transform(Vector3.UnitY, transform.Rotation);
            device.SetListenerOrientation(forward, up);

            // Apply volume multiplier
            device.SetListenerGain(listener.VolumeMultiplier);

            // Configure global Doppler settings
            device.SetDopplerFactor(listener.DopplerFactor);
            device.SetSpeedOfSound(listener.SpeedOfSound);

            // Only process first active listener
            break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
