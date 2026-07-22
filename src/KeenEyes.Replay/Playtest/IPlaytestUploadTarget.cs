namespace KeenEyes.Replay.Playtest;

/// <summary>
/// Defines a pluggable destination that a completed playtest bundle can be uploaded to.
/// </summary>
/// <remarks>
/// <para>
/// KeenEyes ships one implementation, <see cref="DirectoryUploadTarget"/>, which copies the
/// bundle to a target directory (for example a shared network drive). Studios can implement
/// this interface to deliver bundles to their own backend (an HTTP endpoint, object storage,
/// an issue tracker, and so on) without KeenEyes shipping or hosting any such service.
/// </para>
/// </remarks>
public interface IPlaytestUploadTarget
{
    /// <summary>
    /// Uploads the bundle at the specified path to this target.
    /// </summary>
    /// <param name="bundlePath">The path to the local bundle archive to upload.</param>
    /// <param name="manifest">The manifest describing the bundle contents.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the upload has finished.</returns>
    Task UploadAsync(string bundlePath, PlaytestManifest manifest, CancellationToken cancellationToken);
}
