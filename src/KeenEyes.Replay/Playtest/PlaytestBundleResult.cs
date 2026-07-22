namespace KeenEyes.Replay.Playtest;

/// <summary>
/// The outcome of ending a playtest session and producing its bundle.
/// </summary>
/// <remarks>
/// <para>
/// The bundle is always written to disk before any upload is attempted, so
/// <see cref="BundlePath"/> is valid regardless of whether an upload target was configured
/// or the upload succeeded. When an upload fails, the local bundle is preserved and the
/// failure is surfaced via <see cref="UploadError"/> rather than thrown.
/// </para>
/// </remarks>
public sealed record PlaytestBundleResult
{
    /// <summary>
    /// Gets the path to the local bundle archive on disk.
    /// </summary>
    public required string BundlePath { get; init; }

    /// <summary>
    /// Gets the manifest that describes the bundle contents.
    /// </summary>
    public required PlaytestManifest Manifest { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bundle was successfully uploaded.
    /// </summary>
    /// <remarks>
    /// Always <see langword="false"/> when no upload target was configured.
    /// </remarks>
    public bool Uploaded { get; init; }

    /// <summary>
    /// Gets the exception that caused an upload to fail, if any.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when no upload was attempted or the upload succeeded.
    /// </remarks>
    public Exception? UploadError { get; init; }
}
