using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// A loaded font asset containing a font resource for text rendering.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FontAsset"/> wraps a font loaded from TTF or OTF files and provides
/// access to the font handle for text rendering. The asset supports creating
/// size variants from the same font data.
/// </para>
/// <para>
/// Font assets are reference counted through the asset system, ensuring proper
/// lifecycle management and enabling hot-reload during development.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load a font
/// var fontHandle = assetManager.Load&lt;FontAsset&gt;("fonts/Roboto.ttf");
/// var font = fontHandle.Asset;
///
/// // Use for text rendering
/// var lineHeight = fontManager.GetLineHeight(font.Handle);
///
/// // Create a larger variant
/// var largeFont = font.GetSized(24f);
/// </code>
/// </example>
public sealed class FontAsset : IDisposable
{
    private readonly IFontManager fontManager;
    private readonly byte[] fontData;
    private bool disposed;

    /// <summary>
    /// Gets the font handle for rendering.
    /// </summary>
    public FontHandle Handle { get; }

    /// <summary>
    /// Gets the default font size in pixels.
    /// </summary>
    public float DefaultSize { get; }

    /// <summary>
    /// Gets the font family name.
    /// </summary>
    public string FamilyName { get; }

    /// <summary>
    /// Gets the line height for the default size.
    /// </summary>
    public float LineHeight => fontManager.GetLineHeight(Handle);

    /// <summary>
    /// Gets the estimated size of this asset in bytes.
    /// </summary>
    public long SizeBytes => fontData.Length + 128;

    /// <summary>
    /// Creates a new font asset.
    /// </summary>
    /// <param name="handle">The font handle.</param>
    /// <param name="defaultSize">The default font size.</param>
    /// <param name="familyName">The font family name.</param>
    /// <param name="fontData">The raw font file data.</param>
    /// <param name="fontManager">The font manager for resource management.</param>
    internal FontAsset(
        FontHandle handle,
        float defaultSize,
        string familyName,
        byte[] fontData,
        IFontManager fontManager)
    {
        Handle = handle;
        DefaultSize = defaultSize;
        FamilyName = familyName;
        this.fontData = fontData;
        this.fontManager = fontManager;
    }

    /// <summary>
    /// Creates a font variant with a different size.
    /// </summary>
    /// <param name="size">The desired font size in pixels.</param>
    /// <returns>A font handle for the sized variant.</returns>
    /// <remarks>
    /// The sized font shares the same underlying font data but renders
    /// at the specified size. The returned handle is managed by the
    /// font manager and does not need separate disposal.
    /// </remarks>
    public FontHandle GetSized(float size)
    {
        return fontManager.CreateSizedFont(Handle, size);
    }

    /// <summary>
    /// Releases the font resources.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        fontManager.Release(Handle);
    }
}
