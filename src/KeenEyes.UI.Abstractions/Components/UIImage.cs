using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that renders an image within a UI element.
/// </summary>
/// <remarks>
/// <para>
/// Images are rendered using textures loaded through the graphics context.
/// The <see cref="ScaleMode"/> controls how the image is fitted within the element bounds.
/// </para>
/// <para>
/// Use <see cref="SourceRect"/> to render only a portion of the texture (sprite atlas).
/// </para>
/// </remarks>
public struct UIImage : IComponent
{
    /// <summary>
    /// The texture to render.
    /// </summary>
    public TextureHandle Texture;

    /// <summary>
    /// Color tint applied to the image (multiplied with texture colors).
    /// </summary>
    public Vector4 Tint;

    /// <summary>
    /// How the image is scaled to fit the element bounds.
    /// </summary>
    public ImageScaleMode ScaleMode;

    /// <summary>
    /// The source rectangle within the texture (for sprite atlases).
    /// Use <see cref="Rectangle.Empty"/> for the entire texture.
    /// </summary>
    public Rectangle SourceRect;

    /// <summary>
    /// Whether to preserve the image aspect ratio when scaling.
    /// </summary>
    public bool PreserveAspect;

    /// <summary>
    /// Border sizes for 9-slice rendering (in source texture pixels).
    /// Only used when <see cref="ScaleMode"/> is <see cref="ImageScaleMode.NineSlice"/>.
    /// </summary>
    public UIEdges SliceBorder;

    /// <summary>
    /// How to fill the center region when 9-slice rendering.
    /// </summary>
    public SlicedFillMode CenterFillMode;

    /// <summary>
    /// How to fill the edge regions when 9-slice rendering.
    /// </summary>
    public SlicedFillMode EdgeFillMode;

    /// <summary>
    /// Creates an image component with no tint (original colors).
    /// </summary>
    /// <param name="texture">The texture to render.</param>
    public static UIImage Create(TextureHandle texture) => new()
    {
        Texture = texture,
        Tint = Vector4.One, // No tint (multiply by 1)
        ScaleMode = ImageScaleMode.ScaleToFit,
        SourceRect = Rectangle.Empty,
        PreserveAspect = true
    };

    /// <summary>
    /// Creates an image component that stretches to fill the element.
    /// </summary>
    /// <param name="texture">The texture to render.</param>
    public static UIImage Stretch(TextureHandle texture) => new()
    {
        Texture = texture,
        Tint = Vector4.One,
        ScaleMode = ImageScaleMode.Stretch,
        SourceRect = Rectangle.Empty,
        PreserveAspect = false
    };

    /// <summary>
    /// Creates an image component from a sprite atlas region.
    /// </summary>
    /// <param name="texture">The atlas texture.</param>
    /// <param name="sourceRect">The source rectangle within the atlas.</param>
    public static UIImage FromAtlas(TextureHandle texture, Rectangle sourceRect) => new()
    {
        Texture = texture,
        Tint = Vector4.One,
        ScaleMode = ImageScaleMode.ScaleToFit,
        SourceRect = sourceRect,
        PreserveAspect = true
    };

    /// <summary>
    /// Creates an image component with 9-slice scaling.
    /// </summary>
    /// <param name="texture">The texture to render.</param>
    /// <param name="border">The border sizes in source texture pixels.</param>
    /// <param name="centerFill">How to fill the center region (default: Stretch).</param>
    /// <param name="edgeFill">How to fill the edge regions (default: Stretch).</param>
    public static UIImage NineSlice(
        TextureHandle texture,
        UIEdges border,
        SlicedFillMode centerFill = SlicedFillMode.Stretch,
        SlicedFillMode edgeFill = SlicedFillMode.Stretch) => new()
        {
            Texture = texture,
            Tint = Vector4.One,
            ScaleMode = ImageScaleMode.NineSlice,
            SourceRect = Rectangle.Empty,
            PreserveAspect = false,
            SliceBorder = border,
            CenterFillMode = centerFill,
            EdgeFillMode = edgeFill
        };

    /// <summary>
    /// Creates an image component that tiles the texture.
    /// </summary>
    /// <param name="texture">The texture to render.</param>
    public static UIImage Tiled(TextureHandle texture) => new()
    {
        Texture = texture,
        Tint = Vector4.One,
        ScaleMode = ImageScaleMode.Tile,
        SourceRect = Rectangle.Empty,
        PreserveAspect = false
    };
}
