namespace KeenEyes.Replay.Playtest;

/// <summary>
/// Describes the contents and provenance of a playtest bundle.
/// </summary>
/// <remarks>
/// <para>
/// The manifest is serialized as <c>manifest.json</c> at the root of a playtest bundle
/// archive. It records who produced the session, when it ran, the engine and game
/// versions in effect, and an inventory of the other entries in the archive so tools can
/// discover the bundle contents without unpacking every file.
/// </para>
/// </remarks>
public sealed record PlaytestManifest
{
    /// <summary>
    /// Gets the unique identifier assigned to this playtest session.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Gets the identifier of the playtester who produced this session.
    /// </summary>
    public required string PlaytesterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the session started.
    /// </summary>
    public required DateTimeOffset StartedUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the session ended or was captured.
    /// </summary>
    public required DateTimeOffset EndedUtc { get; init; }

    /// <summary>
    /// Gets the KeenEyes engine version that produced this bundle, if available.
    /// </summary>
    public string? EngineVersion { get; init; }

    /// <summary>
    /// Gets the host game/application version that produced this bundle, if available.
    /// </summary>
    public string? GameVersion { get; init; }

    /// <summary>
    /// Gets a value indicating whether this bundle was produced by a crash capture.
    /// </summary>
    public bool HasCrash { get; init; }

    /// <summary>
    /// Gets the caller-supplied metadata associated with the session, if any.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets the inventory of entry names contained in the bundle archive, including
    /// <c>manifest.json</c> itself.
    /// </summary>
    public required IReadOnlyList<string> Entries { get; init; }
}
