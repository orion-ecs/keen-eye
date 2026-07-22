namespace KeenEyes.Replay.Playtest;

/// <summary>
/// An <see cref="IPlaytestUploadTarget"/> that copies playtest bundles into a target
/// directory, such as a shared network drive.
/// </summary>
/// <remarks>
/// <para>
/// The target directory is not created automatically: the copy fails if the directory does
/// not exist, so a misconfigured drop location surfaces as an error rather than silently
/// creating a stray folder. The bundle file name is preserved in the destination.
/// </para>
/// </remarks>
public sealed class DirectoryUploadTarget : IPlaytestUploadTarget
{
    private readonly string targetDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryUploadTarget"/> class.
    /// </summary>
    /// <param name="targetDirectory">The directory that bundles are copied into.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="targetDirectory"/> is null or whitespace.
    /// </exception>
    public DirectoryUploadTarget(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        this.targetDirectory = targetDirectory;
    }

    /// <inheritdoc />
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the configured target directory does not exist.
    /// </exception>
    public Task UploadAsync(string bundlePath, PlaytestManifest manifest, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundlePath);
        ArgumentNullException.ThrowIfNull(manifest);

        cancellationToken.ThrowIfCancellationRequested();

        var destination = Path.Combine(targetDirectory, Path.GetFileName(bundlePath));
        File.Copy(bundlePath, destination, overwrite: true);

        return Task.CompletedTask;
    }
}
