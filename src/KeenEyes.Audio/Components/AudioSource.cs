using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio;

/// <summary>
/// ECS component for audio emitters. Attach to entities to play sounds.
/// </summary>
/// <remarks>
/// <para>
/// The audio system synchronizes this component with a backend audio source.
/// For 3D spatialization, ensure the entity also has a <c>Transform3D</c> component
/// from <c>KeenEyes.Common</c>.
/// </para>
/// <para>
/// Use <see cref="Default"/> to create an audio source with sensible defaults,
/// then set the <see cref="Clip"/> property to the audio clip to play.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var clip = audio.LoadClip("explosion.wav");
/// var entity = world.Spawn()
///     .With(new Transform3D(...))
///     .With(AudioSource.Default with { Clip = clip, Spatial = true, PlayOnAwake = true })
///     .Build();
/// </code>
/// </example>
public struct AudioSource : IComponent
{
    /// <summary>
    /// The audio clip to play.
    /// </summary>
    public AudioClipHandle Clip;

    /// <summary>
    /// Playback volume (0.0 to 1.0, can exceed 1.0 for amplification).
    /// </summary>
    public float Volume;

    /// <summary>
    /// Playback pitch multiplier (0.5 to 2.0 typical range).
    /// </summary>
    /// <remarks>
    /// Values below 1.0 lower the pitch, values above 1.0 raise the pitch.
    /// A value of 0.5 is one octave down, 2.0 is one octave up.
    /// </remarks>
    public float Pitch;

    /// <summary>
    /// Whether to loop the audio when it reaches the end.
    /// </summary>
    public bool Loop;

    /// <summary>
    /// Whether to start playing immediately when the component is added.
    /// </summary>
    /// <remarks>
    /// This flag is consumed (set to false) after the first play.
    /// </remarks>
    public bool PlayOnAwake;

    /// <summary>
    /// The mixer channel for volume grouping.
    /// </summary>
    public AudioChannel Channel;

    /// <summary>
    /// Whether this is a 3D spatial source.
    /// </summary>
    /// <remarks>
    /// When true, the audio position is taken from the entity's <c>Transform3D</c> component.
    /// When false, the audio plays at full volume regardless of listener position.
    /// </remarks>
    public bool Spatial;

    /// <summary>
    /// Distance at which sound is at full volume (for spatial audio).
    /// </summary>
    public float MinDistance;

    /// <summary>
    /// Distance at which sound becomes inaudible (for spatial audio).
    /// </summary>
    public float MaxDistance;

    /// <summary>
    /// Distance attenuation curve (for spatial audio).
    /// </summary>
    public AudioRolloffMode RolloffMode;

    /// <summary>
    /// Current playback state (managed by the audio system).
    /// </summary>
    /// <remarks>
    /// Set this to <see cref="AudioPlayState.Playing"/> to start playback,
    /// or <see cref="AudioPlayState.Stopped"/> to stop. The system updates
    /// this value to reflect actual playback state.
    /// </remarks>
    public AudioPlayState State;

    /// <summary>
    /// Handle to the currently playing sound (managed by the audio system).
    /// </summary>
    /// <remarks>
    /// This handle can be used with <see cref="IAudioContext"/> methods to control
    /// the playing sound (e.g., adjust volume, pitch, or position dynamically).
    /// Returns <see cref="SoundHandle.Invalid"/> when no sound is playing.
    /// </remarks>
    public SoundHandle CurrentSound;

    /// <summary>
    /// Internal backend source handle (managed by the audio system).
    /// </summary>
    internal int BackendSourceId;

    /// <summary>
    /// Creates a new audio source with sensible default settings.
    /// </summary>
    /// <remarks>
    /// Default values:
    /// <list type="bullet">
    /// <item>Volume: 1.0</item>
    /// <item>Pitch: 1.0</item>
    /// <item>Loop: false</item>
    /// <item>Channel: SFX</item>
    /// <item>Spatial: false</item>
    /// <item>MinDistance: 1.0</item>
    /// <item>MaxDistance: 100.0</item>
    /// <item>RolloffMode: Logarithmic</item>
    /// </list>
    /// </remarks>
    public static AudioSource Default => new()
    {
        Clip = AudioClipHandle.Invalid,
        Volume = 1f,
        Pitch = 1f,
        Loop = false,
        PlayOnAwake = false,
        Channel = AudioChannel.SFX,
        Spatial = false,
        MinDistance = 1f,
        MaxDistance = 100f,
        RolloffMode = AudioRolloffMode.Logarithmic,
        State = AudioPlayState.Stopped,
        CurrentSound = SoundHandle.Invalid,
        BackendSourceId = -1
    };
}
