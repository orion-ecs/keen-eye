using System.Diagnostics.CodeAnalysis;

using FontStashSharp.Interfaces;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Text;

/// <summary>
/// Texture manager for FontStashSharp that creates OpenGL textures.
/// </summary>
/// <remarks>
/// This class implements the ITexture2DManager interface required by FontStashSharp
/// to create texture atlases for font glyphs.
/// </remarks>
/// <param name="device">The graphics device for texture creation.</param>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
internal sealed class FontStashTextureManager(IGraphicsDevice device) : ITexture2DManager
{
    private readonly IGraphicsDevice device = device ?? throw new ArgumentNullException(nameof(device));

    /// <inheritdoc />
    public object CreateTexture(int width, int height)
    {
        return new FontStashTexture(device, width, height);
    }

    /// <inheritdoc />
    public System.Drawing.Point GetTextureSize(object texture)
    {
        var fsTexture = (FontStashTexture)texture;
        return new System.Drawing.Point(fsTexture.Width, fsTexture.Height);
    }

    /// <inheritdoc />
    public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
    {
        var fsTexture = (FontStashTexture)texture;
        fsTexture.SetData(bounds, data);
    }
}
