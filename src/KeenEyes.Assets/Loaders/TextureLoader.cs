using KeenEyes.Graphics.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StbImageSharp;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for image texture assets using StbImageSharp and ImageSharp.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TextureLoader"/> loads common image formats (PNG, JPG, BMP, TGA, etc.)
/// using the StbImageSharp library, a pure C# port of stb_image.h. WebP images are
/// loaded using SixLabors.ImageSharp which provides pure C# WebP decoding.
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
    public IReadOnlyList<string> Extensions => [".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd", ".hdr", ".webp"];

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
        var extension = Path.GetExtension(context.Path).ToLowerInvariant();

        // WebP files use ImageSharp for decoding
        if (extension == ".webp")
        {
            return LoadWebP(stream);
        }

        // Other formats use StbImageSharp
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

    private TextureAsset LoadWebP(Stream stream)
    {
        using var image = Image.Load<Rgba32>(stream);

        // Copy pixel data to byte array
        var pixelData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixelData);

        // Create GPU texture
        var handle = graphics.CreateTexture(image.Width, image.Height, pixelData);

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
