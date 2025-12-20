namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// An opaque handle to an audio clip resource.
/// </summary>
/// <remarks>
/// <para>
/// Audio clip handles are returned by <see cref="IAudioContext"/> when loading audio files
/// and must be used to reference the clip in playback operations.
/// </para>
/// <para>
/// Handles are opaque identifiers that avoid exposing backend-specific resource types,
/// enabling portability across different audio APIs (OpenAL, XAudio2, etc.).
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this audio clip resource.</param>
public readonly record struct AudioClipHandle(int Id)
{
    /// <summary>
    /// An invalid audio clip handle representing no clip.
    /// </summary>
    public static readonly AudioClipHandle Invalid = new(-1);

    /// <summary>
    /// Gets whether this handle refers to a valid audio clip resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"AudioClip({Id})" : "AudioClip(Invalid)";
}
