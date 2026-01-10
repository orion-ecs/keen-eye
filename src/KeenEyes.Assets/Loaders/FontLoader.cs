using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for TrueType (.ttf) and OpenType (.otf) font files.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FontLoader"/> loads font files and creates <see cref="FontAsset"/>
/// instances that can be used for text rendering. Fonts are loaded with a default
/// size but can be resized using <see cref="FontAsset.GetSized"/>.
/// </para>
/// <para>
/// The loader caches font data in memory to support creating size variants
/// efficiently without reloading from disk.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example font loading
/// var handle = assetManager.Load&lt;FontAsset&gt;("fonts/OpenSans-Regular.ttf");
/// var font = handle.Asset;
///
/// // Get different sizes
/// var smallFont = font.GetSized(12f);
/// var largeFont = font.GetSized(32f);
/// </code>
/// </example>
public sealed class FontLoader : IAssetLoader<FontAsset>
{
    private const float DefaultFontSize = 16f;

    private readonly IFontManager fontManager;

    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".ttf", ".otf"];

    /// <summary>
    /// Creates a new font loader.
    /// </summary>
    /// <param name="fontManager">The font manager for creating font resources.</param>
    /// <exception cref="ArgumentNullException">Thrown when fontManager is null.</exception>
    public FontLoader(IFontManager fontManager)
    {
        ArgumentNullException.ThrowIfNull(fontManager);
        this.fontManager = fontManager;
    }

    /// <inheritdoc />
    public FontAsset Load(Stream stream, AssetLoadContext context)
    {
        // Read font data into memory
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var fontData = memoryStream.ToArray();

        // Create font with default size
        var handle = fontManager.LoadFontFromMemory(fontData, DefaultFontSize, context.Path);

        // Extract family name from path
        var familyName = Path.GetFileNameWithoutExtension(context.Path);

        return new FontAsset(handle, DefaultFontSize, familyName, fontData, fontManager);
    }

    /// <inheritdoc />
    public async Task<FontAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // Font loading is I/O bound, read asynchronously
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var fontData = memoryStream.ToArray();

        // Font creation is CPU-bound but fast
        var handle = fontManager.LoadFontFromMemory(fontData, DefaultFontSize, context.Path);
        var familyName = Path.GetFileNameWithoutExtension(context.Path);

        return new FontAsset(handle, DefaultFontSize, familyName, fontData, fontManager);
    }

    /// <inheritdoc />
    public long EstimateSize(FontAsset asset) => asset.SizeBytes;
}
