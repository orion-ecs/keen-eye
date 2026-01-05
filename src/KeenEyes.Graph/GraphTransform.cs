using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// Provides coordinate transformation utilities for graph canvases.
/// </summary>
/// <remarks>
/// <para>
/// The graph uses a canvas coordinate system where (0,0) is the initial center.
/// Pan and zoom transform between canvas and screen coordinates.
/// </para>
/// <para>
/// Coordinate formulas:
/// <list type="bullet">
/// <item>Screen = (Canvas - Pan) * Zoom + Origin</item>
/// <item>Canvas = (Screen - Origin) / Zoom + Pan</item>
/// </list>
/// </para>
/// </remarks>
public static class GraphTransform
{
    /// <summary>
    /// Converts screen coordinates to canvas coordinates.
    /// </summary>
    /// <param name="screenPos">The position in screen coordinates.</param>
    /// <param name="pan">The current pan offset.</param>
    /// <param name="zoom">The current zoom level (1.0 = 100%).</param>
    /// <param name="origin">The screen position of the canvas origin.</param>
    /// <returns>The position in canvas coordinates.</returns>
    public static Vector2 ScreenToCanvas(Vector2 screenPos, Vector2 pan, float zoom, Vector2 origin)
    {
        return (screenPos - origin) / zoom + pan;
    }

    /// <summary>
    /// Converts canvas coordinates to screen coordinates.
    /// </summary>
    /// <param name="canvasPos">The position in canvas coordinates.</param>
    /// <param name="pan">The current pan offset.</param>
    /// <param name="zoom">The current zoom level (1.0 = 100%).</param>
    /// <param name="origin">The screen position of the canvas origin.</param>
    /// <returns>The position in screen coordinates.</returns>
    public static Vector2 CanvasToScreen(Vector2 canvasPos, Vector2 pan, float zoom, Vector2 origin)
    {
        return (canvasPos - pan) * zoom + origin;
    }

    /// <summary>
    /// Converts a canvas rectangle to screen coordinates.
    /// </summary>
    /// <param name="canvasRect">The rectangle in canvas coordinates.</param>
    /// <param name="pan">The current pan offset.</param>
    /// <param name="zoom">The current zoom level.</param>
    /// <param name="origin">The screen position of the canvas origin.</param>
    /// <returns>The rectangle in screen coordinates.</returns>
    public static Rectangle CanvasToScreen(in Rectangle canvasRect, Vector2 pan, float zoom, Vector2 origin)
    {
        var screenTopLeft = CanvasToScreen(new Vector2(canvasRect.X, canvasRect.Y), pan, zoom, origin);
        return new Rectangle(
            screenTopLeft.X,
            screenTopLeft.Y,
            canvasRect.Width * zoom,
            canvasRect.Height * zoom
        );
    }

    /// <summary>
    /// Gets the visible canvas area given the screen bounds and transform.
    /// </summary>
    /// <param name="screenBounds">The screen rectangle of the canvas area.</param>
    /// <param name="pan">The current pan offset.</param>
    /// <param name="zoom">The current zoom level.</param>
    /// <returns>The visible area in canvas coordinates.</returns>
    public static Rectangle GetVisibleArea(in Rectangle screenBounds, Vector2 pan, float zoom)
    {
        var origin = new Vector2(screenBounds.X, screenBounds.Y);
        var topLeft = ScreenToCanvas(origin, pan, zoom, origin);
        var bottomRight = ScreenToCanvas(origin + new Vector2(screenBounds.Width, screenBounds.Height), pan, zoom, origin);

        return new Rectangle(
            topLeft.X,
            topLeft.Y,
            bottomRight.X - topLeft.X,
            bottomRight.Y - topLeft.Y
        );
    }

    /// <summary>
    /// Calculates the zoom centered on a screen point.
    /// </summary>
    /// <param name="currentPan">The current pan offset.</param>
    /// <param name="currentZoom">The current zoom level.</param>
    /// <param name="newZoom">The new zoom level.</param>
    /// <param name="screenFocus">The screen point to zoom towards.</param>
    /// <param name="origin">The screen position of the canvas origin.</param>
    /// <returns>The new pan offset to keep the focus point stationary.</returns>
    public static Vector2 ZoomToPoint(Vector2 currentPan, float currentZoom, float newZoom, Vector2 screenFocus, Vector2 origin)
    {
        // Get the canvas position under the mouse before zoom
        var canvasPos = ScreenToCanvas(screenFocus, currentPan, currentZoom, origin);

        // After zoom, this canvas position should still be at screenFocus
        // screenFocus = (canvasPos - newPan) * newZoom + origin
        // newPan = canvasPos - (screenFocus - origin) / newZoom
        return canvasPos - (screenFocus - origin) / newZoom;
    }

    /// <summary>
    /// Snaps a position to the nearest grid point.
    /// </summary>
    /// <param name="position">The position to snap.</param>
    /// <param name="gridSize">The grid spacing.</param>
    /// <returns>The snapped position.</returns>
    public static Vector2 SnapToGrid(Vector2 position, float gridSize)
    {
        return new Vector2(
            MathF.Round(position.X / gridSize) * gridSize,
            MathF.Round(position.Y / gridSize) * gridSize
        );
    }

    /// <summary>
    /// Checks if a screen point is inside a canvas rectangle.
    /// </summary>
    /// <param name="screenPoint">The screen position to test.</param>
    /// <param name="canvasRect">The rectangle in canvas coordinates.</param>
    /// <param name="pan">The current pan offset.</param>
    /// <param name="zoom">The current zoom level.</param>
    /// <param name="origin">The screen position of the canvas origin.</param>
    /// <returns>True if the point is inside the rectangle.</returns>
    public static bool HitTest(Vector2 screenPoint, in Rectangle canvasRect, Vector2 pan, float zoom, Vector2 origin)
    {
        var screenRect = CanvasToScreen(canvasRect, pan, zoom, origin);
        return screenRect.Contains(screenPoint);
    }

    /// <summary>
    /// Creates a selection box from two screen points.
    /// </summary>
    /// <param name="start">The starting screen point.</param>
    /// <param name="end">The ending screen point.</param>
    /// <returns>A normalized rectangle (positive width/height) in screen coordinates.</returns>
    public static Rectangle CreateSelectionBox(Vector2 start, Vector2 end)
    {
        var minX = MathF.Min(start.X, end.X);
        var minY = MathF.Min(start.Y, end.Y);
        var maxX = MathF.Max(start.X, end.X);
        var maxY = MathF.Max(start.Y, end.Y);

        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }
}
