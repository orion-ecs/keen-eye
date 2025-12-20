namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// An opaque handle to a playing sound instance.
/// </summary>
/// <remarks>
/// <para>
/// Sound handles reference active playback instances, allowing control over
/// playing sounds (stop, pause, volume changes). Unlike <see cref="AudioClipHandle"/>
/// which references loaded audio data, <see cref="SoundHandle"/> references a
/// runtime playback state.
/// </para>
/// <para>
/// A single <see cref="AudioClipHandle"/> can have multiple concurrent
/// <see cref="SoundHandle"/> instances playing simultaneously.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var clip = audio.LoadClip("explosion.wav");
/// var sound = audio.Play(clip, volume: 0.8f);
///
/// // Later, stop the specific sound instance
/// audio.Stop(sound);
/// </code>
/// </example>
/// <param name="Id">The internal identifier for this sound instance.</param>
public readonly record struct SoundHandle(int Id)
{
    /// <summary>
    /// An invalid sound handle representing no sound.
    /// </summary>
    public static readonly SoundHandle Invalid = new(-1);

    /// <summary>
    /// Gets whether this handle refers to a valid sound instance.
    /// </summary>
    public bool IsValid => Id >= 0;

    /// <inheritdoc/>
    public override string ToString() => IsValid ? $"Sound({Id})" : "Sound(Invalid)";
}
