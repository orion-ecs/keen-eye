using KeenEyes.Graphics.Abstractions;
using StbImageSharp;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for image texture assets using StbImageSharp.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TextureLoader"/> loads common image formats (PNG, JPG, BMP, TGA, etc.)
/// using the StbImageSharp library, a pure C# port of stb_image.h.
/// </para>
/// <para>
/// Loaded images are uploaded to the GPU via <see cref="IGraphicsContext"/> and wrapped
/// in a <see cref="TextureAsset"/> containing the GPU handle and metadata.
/// </para>
/// </remarks>
public sealed class TextureLoader : IAssetLoader<TextureAsset>
{
    private readonly IGraphicsContext graphics;

    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd", ".hdr"];

    /// <summary>
    /// Creates a new texture loader.
    /// </summary>
    /// <param name="graphics">The graphics context for GPU texture creation.</param>
    /// <exception cref="ArgumentNullException">Graphics context is null.</exception>
    public TextureLoader(IGraphicsContext graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        this.graphics = graphics;
    }

    /// <inheritdoc />
    public TextureAsset Load(Stream stream, AssetLoadContext context)
    {
        // Load image data using StbImageSharp
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        // Create GPU texture
        var handle = graphics.CreateTexture(image.Width, image.Height, image.Data);

        return new TextureAsset(
            handle,
            image.Width,
            image.Height,
            TextureFormat.Rgba8,
            graphics);
    }

    /// <inheritdoc />
    public async Task<TextureAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // StbImageSharp doesn't have async methods, so run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(TextureAsset asset)
        => asset.SizeBytes;
}
