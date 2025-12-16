using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Interface for 2D primitive rendering operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides high-level 2D rendering primitives for:
/// <list type="bullet">
///   <item><description>Rectangles (filled and outlined)</description></item>
///   <item><description>Lines and line strips</description></item>
///   <item><description>Circles and ellipses</description></item>
///   <item><description>Textured quads and sprites</description></item>
///   <item><description>Texture atlases for efficient batch rendering</description></item>
/// </list>
/// </para>
/// <para>
/// The renderer supports automatic batching of draw calls for performance.
/// Use <see cref="Begin()"/> to start a batch and <see cref="End"/> to flush it.
/// </para>
/// <para>
/// Coordinate system: By default, (0,0) is at the top-left corner with Y increasing downward,
/// which is standard for 2D UI/game rendering.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// renderer.Begin();
///
/// // Draw filled rectangle
/// renderer.FillRect(10, 10, 100, 50, new Vector4(1, 0, 0, 1));
///
/// // Draw outlined rectangle
/// renderer.DrawRect(10, 10, 100, 50, new Vector4(1, 1, 1, 1), 2f);
///
/// // Draw line
/// renderer.DrawLine(0, 0, 100, 100, new Vector4(0, 1, 0, 1));
///
/// // Draw textured quad
/// renderer.DrawTexture(texture, 200, 100);
///
/// renderer.End();
/// </code>
/// </example>
public interface I2DRenderer : IDisposable
{
    #region Batch Control

    /// <summary>
    /// Begins a new render batch.
    /// </summary>
    /// <remarks>
    /// All draw calls between <see cref="Begin()"/> and <see cref="End"/> are batched
    /// for efficient rendering. Nested Begin/End calls are not supported.
    /// </remarks>
    void Begin();

    /// <summary>
    /// Begins a new render batch with a custom projection matrix.
    /// </summary>
    /// <param name="projection">The projection matrix to use.</param>
    void Begin(in Matrix4x4 projection);

    /// <summary>
    /// Ends the current batch and flushes all queued draw calls to the GPU.
    /// </summary>
    void End();

    /// <summary>
    /// Forces the current batch to be flushed without ending it.
    /// </summary>
    /// <remarks>
    /// Use this when you need to change render state mid-batch.
    /// </remarks>
    void Flush();

    #endregion

    #region Rectangles

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="color">The fill color (RGBA).</param>
    void FillRect(float x, float y, float width, float height, Vector4 color);

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    /// <param name="rect">The rectangle bounds.</param>
    /// <param name="color">The fill color (RGBA).</param>
    void FillRect(in Rectangle rect, Vector4 color);

    /// <summary>
    /// Draws an outlined rectangle.
    /// </summary>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="color">The outline color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    void DrawRect(float x, float y, float width, float height, Vector4 color, float thickness = 1f);

    /// <summary>
    /// Draws an outlined rectangle.
    /// </summary>
    /// <param name="rect">The rectangle bounds.</param>
    /// <param name="color">The outline color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    void DrawRect(in Rectangle rect, Vector4 color, float thickness = 1f);

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// </summary>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="radius">The corner radius.</param>
    /// <param name="color">The fill color (RGBA).</param>
    void FillRoundedRect(float x, float y, float width, float height, float radius, Vector4 color);

    /// <summary>
    /// Draws an outlined rounded rectangle.
    /// </summary>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="radius">The corner radius.</param>
    /// <param name="color">The outline color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    void DrawRoundedRect(float x, float y, float width, float height, float radius, Vector4 color, float thickness = 1f);

    #endregion

    #region Lines

    /// <summary>
    /// Draws a line segment.
    /// </summary>
    /// <param name="x1">The X coordinate of the start point.</param>
    /// <param name="y1">The Y coordinate of the start point.</param>
    /// <param name="x2">The X coordinate of the end point.</param>
    /// <param name="y2">The Y coordinate of the end point.</param>
    /// <param name="color">The line color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    void DrawLine(float x1, float y1, float x2, float y2, Vector4 color, float thickness = 1f);

    /// <summary>
    /// Draws a line segment.
    /// </summary>
    /// <param name="start">The start point.</param>
    /// <param name="end">The end point.</param>
    /// <param name="color">The line color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    void DrawLine(Vector2 start, Vector2 end, Vector4 color, float thickness = 1f);

    /// <summary>
    /// Draws a connected series of line segments.
    /// </summary>
    /// <param name="points">The points defining the line strip.</param>
    /// <param name="color">The line color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    void DrawLineStrip(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f);

    /// <summary>
    /// Draws a closed polygon outline.
    /// </summary>
    /// <param name="points">The vertices of the polygon.</param>
    /// <param name="color">The outline color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    void DrawPolygon(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f);

    #endregion

    #region Circles and Ellipses

    /// <summary>
    /// Draws a filled circle.
    /// </summary>
    /// <param name="centerX">The X coordinate of the center.</param>
    /// <param name="centerY">The Y coordinate of the center.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The fill color (RGBA).</param>
    /// <param name="segments">The number of segments (higher = smoother).</param>
    void FillCircle(float centerX, float centerY, float radius, Vector4 color, int segments = 32);

    /// <summary>
    /// Draws an outlined circle.
    /// </summary>
    /// <param name="centerX">The X coordinate of the center.</param>
    /// <param name="centerY">The Y coordinate of the center.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The outline color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    /// <param name="segments">The number of segments (higher = smoother).</param>
    void DrawCircle(float centerX, float centerY, float radius, Vector4 color, float thickness = 1f, int segments = 32);

    /// <summary>
    /// Draws a filled ellipse.
    /// </summary>
    /// <param name="centerX">The X coordinate of the center.</param>
    /// <param name="centerY">The Y coordinate of the center.</param>
    /// <param name="radiusX">The horizontal radius.</param>
    /// <param name="radiusY">The vertical radius.</param>
    /// <param name="color">The fill color (RGBA).</param>
    /// <param name="segments">The number of segments (higher = smoother).</param>
    void FillEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, int segments = 32);

    /// <summary>
    /// Draws an outlined ellipse.
    /// </summary>
    /// <param name="centerX">The X coordinate of the center.</param>
    /// <param name="centerY">The Y coordinate of the center.</param>
    /// <param name="radiusX">The horizontal radius.</param>
    /// <param name="radiusY">The vertical radius.</param>
    /// <param name="color">The outline color (RGBA).</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    /// <param name="segments">The number of segments (higher = smoother).</param>
    void DrawEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, float thickness = 1f, int segments = 32);

    #endregion

    #region Textured Quads

    /// <summary>
    /// Draws a textured quad at the specified position.
    /// </summary>
    /// <param name="texture">The texture handle.</param>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="tint">Optional color tint (default: white/no tint).</param>
    void DrawTexture(TextureHandle texture, float x, float y, Vector4? tint = null);

    /// <summary>
    /// Draws a textured quad with explicit size.
    /// </summary>
    /// <param name="texture">The texture handle.</param>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="width">The width to draw.</param>
    /// <param name="height">The height to draw.</param>
    /// <param name="tint">Optional color tint (default: white/no tint).</param>
    void DrawTexture(TextureHandle texture, float x, float y, float width, float height, Vector4? tint = null);

    /// <summary>
    /// Draws a region of a texture (for texture atlases/sprite sheets).
    /// </summary>
    /// <param name="texture">The texture handle.</param>
    /// <param name="destRect">The destination rectangle on screen.</param>
    /// <param name="sourceRect">The source rectangle in texture coordinates (0-1 range).</param>
    /// <param name="tint">Optional color tint (default: white/no tint).</param>
    void DrawTextureRegion(TextureHandle texture, in Rectangle destRect, in Rectangle sourceRect, Vector4? tint = null);

    /// <summary>
    /// Draws a textured quad with rotation.
    /// </summary>
    /// <param name="texture">The texture handle.</param>
    /// <param name="destRect">The destination rectangle on screen.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="origin">The rotation origin (relative to destRect, 0-1 range).</param>
    /// <param name="tint">Optional color tint (default: white/no tint).</param>
    void DrawTextureRotated(TextureHandle texture, in Rectangle destRect, float rotation, Vector2 origin, Vector4? tint = null);

    #endregion

    #region Clipping

    /// <summary>
    /// Pushes a clipping rectangle onto the clip stack.
    /// </summary>
    /// <param name="rect">The clipping rectangle in screen coordinates.</param>
    /// <remarks>
    /// Nested clips are intersected. Call <see cref="PopClip"/> to restore the previous clip.
    /// </remarks>
    void PushClip(in Rectangle rect);

    /// <summary>
    /// Pops the most recent clipping rectangle from the clip stack.
    /// </summary>
    void PopClip();

    /// <summary>
    /// Clears all clipping rectangles.
    /// </summary>
    void ClearClip();

    #endregion

    #region Batching Hints

    /// <summary>
    /// Provides a hint to optimize for a specific number of sprites.
    /// </summary>
    /// <param name="count">The expected number of sprites to render.</param>
    /// <remarks>
    /// This can help the renderer pre-allocate appropriate buffer sizes.
    /// </remarks>
    void SetBatchHint(int count);

    #endregion
}

/// <summary>
/// A rectangle defined by position and size.
/// </summary>
/// <param name="X">The X coordinate of the top-left corner.</param>
/// <param name="Y">The Y coordinate of the top-left corner.</param>
/// <param name="Width">The width of the rectangle.</param>
/// <param name="Height">The height of the rectangle.</param>
public readonly record struct Rectangle(float X, float Y, float Width, float Height)
{
    /// <summary>An empty rectangle at the origin.</summary>
    public static readonly Rectangle Empty = new(0, 0, 0, 0);

    /// <summary>Gets the left edge (X coordinate).</summary>
    public float Left => X;

    /// <summary>Gets the top edge (Y coordinate).</summary>
    public float Top => Y;

    /// <summary>Gets the right edge.</summary>
    public float Right => X + Width;

    /// <summary>Gets the bottom edge.</summary>
    public float Bottom => Y + Height;

    /// <summary>Gets the center point.</summary>
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>Gets the size as a vector.</summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>Gets the top-left corner.</summary>
    public Vector2 TopLeft => new(X, Y);

    /// <summary>Gets the top-right corner.</summary>
    public Vector2 TopRight => new(Right, Y);

    /// <summary>Gets the bottom-left corner.</summary>
    public Vector2 BottomLeft => new(X, Bottom);

    /// <summary>Gets the bottom-right corner.</summary>
    public Vector2 BottomRight => new(Right, Bottom);

    /// <summary>
    /// Checks if this rectangle contains a point.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the point is inside the rectangle.</returns>
    public bool Contains(Vector2 point) =>
        point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    /// <summary>
    /// Checks if this rectangle intersects another rectangle.
    /// </summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>True if the rectangles overlap.</returns>
    public bool Intersects(in Rectangle other) =>
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    /// <summary>
    /// Computes the intersection of two rectangles.
    /// </summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The intersection rectangle, or Empty if no intersection.</returns>
    public Rectangle Intersection(in Rectangle other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        if (right <= x || bottom <= y)
        {
            return Empty;
        }

        return new Rectangle(x, y, right - x, bottom - y);
    }

    /// <summary>
    /// Creates a rectangle from left, top, right, bottom coordinates.
    /// </summary>
    /// <param name="left">The left edge.</param>
    /// <param name="top">The top edge.</param>
    /// <param name="right">The right edge.</param>
    /// <param name="bottom">The bottom edge.</param>
    /// <returns>A rectangle with the specified bounds.</returns>
    public static Rectangle FromLTRB(float left, float top, float right, float bottom) =>
        new(left, top, right - left, bottom - top);
}
