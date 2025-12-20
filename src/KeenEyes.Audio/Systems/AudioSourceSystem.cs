using System.Numerics;
using KeenEyes.Audio.Abstractions;
using KeenEyes.Common;

namespace KeenEyes.Audio;

/// <summary>
/// System that synchronizes <see cref="AudioSource"/> components with backend audio sources.
/// </summary>
/// <remarks>
/// <para>
/// This system handles:
/// </para>
/// <list type="bullet">
/// <item>Creating backend sources when AudioSource components need to play</item>
/// <item>Updating 3D positions from Transform3D each frame</item>
/// <item>Managing playback state (play/pause/stop)</item>
/// <item>Detecting when non-looping sounds finish</item>
/// </list>
/// <para>
/// Backend sources are allocated on-demand when playback is requested and cleaned up
/// when the component is removed or the entity is destroyed.
/// </para>
/// </remarks>
public sealed class AudioSourceSystem : ISystem
{
    private IWorld? world;
    private IAudioContext? audioContext;
    private readonly Dictionary<Entity, uint> entitySources = [];
    private int nextSoundId = 1;

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

        foreach (var entity in world.Query<AudioSource>())
        {
            ref var source = ref world.Get<AudioSource>(entity);

            // Handle PlayOnAwake
            if (source.PlayOnAwake && source.Clip.IsValid)
            {
                source.PlayOnAwake = false;
                source.State = AudioPlayState.Playing;
            }

            // Check if we need a backend source
            bool needsSource = source.State == AudioPlayState.Playing && source.Clip.IsValid;

            if (!entitySources.TryGetValue(entity, out var backendId))
            {
                if (!needsSource)
                {
                    continue;
                }

                // Create backend source
                backendId = device.CreateSource();
                entitySources[entity] = backendId;
                source.BackendSourceId = (int)backendId;

                // Assign a sound handle for tracking
                source.CurrentSound = new SoundHandle(nextSoundId++);

                // Configure the source
                ConfigureSource(device, backendId, ref source);

                // Start playback
                device.PlaySource(backendId);
            }
            else
            {
                // Source exists, sync state
                var actualState = device.GetSourceState(backendId);

                // Handle state transitions
                if (source.State == AudioPlayState.Playing && actualState == AudioPlayState.Stopped)
                {
                    if (!source.Loop)
                    {
                        // Non-looping sound finished naturally
                        source.State = AudioPlayState.Stopped;
                        source.CurrentSound = SoundHandle.Invalid;
                    }
                    else
                    {
                        // Looping sound stopped unexpectedly, restart
                        device.PlaySource(backendId);
                    }
                }
                else if (source.State == AudioPlayState.Stopped && actualState == AudioPlayState.Playing)
                {
                    // User requested stop
                    device.StopSource(backendId);
                    source.CurrentSound = SoundHandle.Invalid;
                }
                else if (source.State == AudioPlayState.Paused && actualState == AudioPlayState.Playing)
                {
                    // User requested pause
                    device.PauseSource(backendId);
                }
                else if (source.State == AudioPlayState.Playing && actualState == AudioPlayState.Paused)
                {
                    // User requested resume
                    device.PlaySource(backendId);
                }

                // Update volume and pitch (they might have changed)
                device.SetSourceGain(backendId, source.Volume);
                device.SetSourcePitch(backendId, source.Pitch);
            }

            // Update 3D position if spatial
            if (source.Spatial && world.Has<Transform3D>(entity))
            {
                ref readonly var transform = ref world.Get<Transform3D>(entity);
                device.SetSourcePosition(backendId, transform.Position);

                // Update velocity for Doppler if available
                if (world.Has<Velocity3D>(entity))
                {
                    ref readonly var velocity = ref world.Get<Velocity3D>(entity);
                    device.SetSourceVelocity(backendId, velocity.Value);
                }
            }
        }
    }

    private void ConfigureSource(IAudioDevice device, uint sourceId, ref AudioSource source)
    {
        // Set initial properties
        device.SetSourceGain(sourceId, source.Volume);
        device.SetSourcePitch(sourceId, source.Pitch);
        device.SetSourceLooping(sourceId, source.Loop);
        device.SetSourceMinDistance(sourceId, source.MinDistance);
        device.SetSourceMaxDistance(sourceId, source.MaxDistance);
        device.SetSourceRolloff(sourceId, 1f); // Default rolloff factor

        // Get buffer ID and attach to source
        var bufferId = audioContext?.GetBufferId(source.Clip) ?? 0;
        if (bufferId != 0)
        {
            device.SetSourceBuffer(sourceId, bufferId);
        }
    }

    /// <summary>
    /// Called when an AudioSource component is removed from an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    internal void OnAudioSourceRemoved(Entity entity)
    {
        if (entitySources.Remove(entity, out var sourceId))
        {
            audioContext?.Device?.StopSource(sourceId);
            audioContext?.Device?.DeleteSource(sourceId);
        }
    }

    /// <summary>
    /// Called when an entity is destroyed.
    /// </summary>
    /// <param name="entity">The entity.</param>
    internal void OnEntityDestroyed(Entity entity)
    {
        OnAudioSourceRemoved(entity);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Clean up all tracked sources
        if (audioContext?.Device is not null)
        {
            foreach (var (_, sourceId) in entitySources)
            {
                audioContext.Device.StopSource(sourceId);
                audioContext.Device.DeleteSource(sourceId);
            }
        }
        entitySources.Clear();
    }
}
