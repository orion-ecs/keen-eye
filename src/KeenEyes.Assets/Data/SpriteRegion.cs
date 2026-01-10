using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// Represents a single sprite region within a sprite atlas.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SpriteRegion"/> defines a rectangular area within a texture atlas
/// that represents a single sprite. It includes the source rectangle in pixel
/// coordinates, pivot point, and optional rotation flag.
/// </para>
/// <para>
/// For animation frames (from Aseprite exports), the <see cref="Duration"/>
/// property specifies the frame duration in milliseconds.
/// </para>
/// </remarks>
/// <param name="Name">The unique name of this sprite within the atlas.</param>
/// <param name="SourceRect">The source rectangle in pixel coordinates.</param>
/// <param name="OriginalSize">The original size before any trimming was applied.</param>
/// <param name="Pivot">The pivot point in normalized coordinates (0-1).</param>
/// <param name="Rotated">Whether the sprite is rotated 90 degrees clockwise in the atlas.</param>
/// <param name="Duration">Frame duration in milliseconds (for Aseprite animations).</param>
public readonly record struct SpriteRegion(
    string Name,
    Rectangle SourceRect,
    Vector2 OriginalSize,
    Vector2 Pivot,
    bool Rotated = false,
    int Duration = 100
)
{
    /// <summary>
    /// Gets the width of the sprite in pixels.
    /// </summary>
    public float Width => Rotated ? SourceRect.Height : SourceRect.Width;

    /// <summary>
    /// Gets the height of the sprite in pixels.
    /// </summary>
    public float Height => Rotated ? SourceRect.Width : SourceRect.Height;

    /// <summary>
    /// Gets the size of the sprite as a vector.
    /// </summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>
    /// Gets the pivot point in pixel coordinates relative to the sprite.
    /// </summary>
    public Vector2 PivotPixels => new(Width * Pivot.X, Height * Pivot.Y);

    /// <summary>
    /// Computes the UV coordinates for this sprite within the atlas.
    /// </summary>
    /// <param name="atlasWidth">The width of the atlas texture.</param>
    /// <param name="atlasHeight">The height of the atlas texture.</param>
    /// <returns>A rectangle with normalized UV coordinates.</returns>
    public Rectangle GetUVRect(int atlasWidth, int atlasHeight)
    {
        return new Rectangle(
            SourceRect.X / atlasWidth,
            SourceRect.Y / atlasHeight,
            SourceRect.Width / atlasWidth,
            SourceRect.Height / atlasHeight
        );
    }
}
