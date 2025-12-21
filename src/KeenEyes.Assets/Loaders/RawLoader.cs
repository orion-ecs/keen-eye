namespace KeenEyes.Assets;

/// <summary>
/// Loader for raw binary asset files.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RawLoader"/> loads any file as raw binary data. This is useful for
/// custom file formats or binary configuration files that need application-specific parsing.
/// </para>
/// <para>
/// This loader handles all file extensions that don't have a specialized loader registered.
/// </para>
/// </remarks>
public sealed class RawLoader : IAssetLoader<RawAsset>
{
    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".bin", ".dat", ".raw", ".bytes"];

    /// <inheritdoc />
    public RawAsset Load(Stream stream, AssetLoadContext context)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return new RawAsset(memoryStream.ToArray());
    }

    /// <inheritdoc />
    public async Task<RawAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return new RawAsset(memoryStream.ToArray());
    }

    /// <inheritdoc />
    public long EstimateSize(RawAsset asset)
        => asset.SizeBytes;
}
