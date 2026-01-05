using System.Numerics;
using KeenEyes.Graph;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Tests;

public class GraphTransformTests
{
    [Fact]
    public void ScreenToCanvas_WithNoPanOrZoom_ReturnsRelativeToOrigin()
    {
        var screenPos = new Vector2(100, 100);
        var pan = Vector2.Zero;
        var zoom = 1f;
        var origin = new Vector2(50, 50);

        var canvasPos = GraphTransform.ScreenToCanvas(screenPos, pan, zoom, origin);

        Assert.Equal(50f, canvasPos.X);
        Assert.Equal(50f, canvasPos.Y);
    }

    [Fact]
    public void CanvasToScreen_WithNoPanOrZoom_ReturnsRelativeToOrigin()
    {
        var canvasPos = new Vector2(50, 50);
        var pan = Vector2.Zero;
        var zoom = 1f;
        var origin = new Vector2(50, 50);

        var screenPos = GraphTransform.CanvasToScreen(canvasPos, pan, zoom, origin);

        Assert.Equal(100f, screenPos.X);
        Assert.Equal(100f, screenPos.Y);
    }

    [Fact]
    public void ScreenToCanvas_AndBack_ReturnsOriginalPosition()
    {
        var screenPos = new Vector2(200, 300);
        var pan = new Vector2(10, 20);
        var zoom = 1.5f;
        var origin = new Vector2(100, 100);

        var canvasPos = GraphTransform.ScreenToCanvas(screenPos, pan, zoom, origin);
        var backToScreen = GraphTransform.CanvasToScreen(canvasPos, pan, zoom, origin);

        Assert.Equal(screenPos.X, backToScreen.X, precision: 4);
        Assert.Equal(screenPos.Y, backToScreen.Y, precision: 4);
    }

    [Fact]
    public void CanvasToScreen_WithZoom_ScalesPosition()
    {
        var canvasPos = new Vector2(100, 100);
        var pan = Vector2.Zero;
        var zoom = 2f;
        var origin = Vector2.Zero;

        var screenPos = GraphTransform.CanvasToScreen(canvasPos, pan, zoom, origin);

        Assert.Equal(200f, screenPos.X);
        Assert.Equal(200f, screenPos.Y);
    }

    [Fact]
    public void ScreenToCanvas_WithZoom_UnscalesPosition()
    {
        var screenPos = new Vector2(200, 200);
        var pan = Vector2.Zero;
        var zoom = 2f;
        var origin = Vector2.Zero;

        var canvasPos = GraphTransform.ScreenToCanvas(screenPos, pan, zoom, origin);

        Assert.Equal(100f, canvasPos.X);
        Assert.Equal(100f, canvasPos.Y);
    }

    [Fact]
    public void CanvasToScreen_WithPan_OffsetsByPan()
    {
        var canvasPos = new Vector2(100, 100);
        var pan = new Vector2(50, 50);
        var zoom = 1f;
        var origin = Vector2.Zero;

        var screenPos = GraphTransform.CanvasToScreen(canvasPos, pan, zoom, origin);

        Assert.Equal(50f, screenPos.X);
        Assert.Equal(50f, screenPos.Y);
    }

    [Fact]
    public void GetVisibleArea_ReturnsCorrectCanvasArea()
    {
        var screenBounds = new Rectangle(0, 0, 800, 600);
        var pan = Vector2.Zero;
        var zoom = 1f;

        var visible = GraphTransform.GetVisibleArea(screenBounds, pan, zoom);

        Assert.Equal(800f, visible.Width);
        Assert.Equal(600f, visible.Height);
    }

    [Fact]
    public void GetVisibleArea_WithZoom_ScalesArea()
    {
        var screenBounds = new Rectangle(0, 0, 800, 600);
        var pan = Vector2.Zero;
        var zoom = 2f;

        var visible = GraphTransform.GetVisibleArea(screenBounds, pan, zoom);

        Assert.Equal(400f, visible.Width);
        Assert.Equal(300f, visible.Height);
    }

    [Fact]
    public void ZoomToPoint_KeepsFocusPointStationary()
    {
        var currentPan = Vector2.Zero;
        var currentZoom = 1f;
        var newZoom = 2f;
        var screenFocus = new Vector2(400, 300);
        var origin = Vector2.Zero;

        // Get the canvas position under focus before zoom
        var canvasFocus = GraphTransform.ScreenToCanvas(screenFocus, currentPan, currentZoom, origin);

        // Calculate new pan
        var newPan = GraphTransform.ZoomToPoint(currentPan, currentZoom, newZoom, screenFocus, origin);

        // Get the screen position of the same canvas point after zoom
        var screenAfter = GraphTransform.CanvasToScreen(canvasFocus, newPan, newZoom, origin);

        Assert.Equal(screenFocus.X, screenAfter.X, precision: 4);
        Assert.Equal(screenFocus.Y, screenAfter.Y, precision: 4);
    }

    [Fact]
    public void SnapToGrid_SnapsToNearestGridPoint()
    {
        var position = new Vector2(33, 47);
        var gridSize = 20f;

        var snapped = GraphTransform.SnapToGrid(position, gridSize);

        Assert.Equal(40f, snapped.X);
        Assert.Equal(40f, snapped.Y);
    }

    [Fact]
    public void SnapToGrid_WithExactGridPoint_ReturnsUnchanged()
    {
        var position = new Vector2(40, 60);
        var gridSize = 20f;

        var snapped = GraphTransform.SnapToGrid(position, gridSize);

        Assert.Equal(40f, snapped.X);
        Assert.Equal(60f, snapped.Y);
    }

    [Fact]
    public void HitTest_WithPointInsideRect_ReturnsTrue()
    {
        var screenPoint = new Vector2(150, 150);
        var canvasRect = new Rectangle(100, 100, 100, 100);
        var pan = Vector2.Zero;
        var zoom = 1f;
        var origin = Vector2.Zero;

        var hit = GraphTransform.HitTest(screenPoint, canvasRect, pan, zoom, origin);

        Assert.True(hit);
    }

    [Fact]
    public void HitTest_WithPointOutsideRect_ReturnsFalse()
    {
        var screenPoint = new Vector2(50, 50);
        var canvasRect = new Rectangle(100, 100, 100, 100);
        var pan = Vector2.Zero;
        var zoom = 1f;
        var origin = Vector2.Zero;

        var hit = GraphTransform.HitTest(screenPoint, canvasRect, pan, zoom, origin);

        Assert.False(hit);
    }

    [Fact]
    public void CreateSelectionBox_NormalizesNegativeSize()
    {
        var start = new Vector2(200, 200);
        var end = new Vector2(100, 100);

        var box = GraphTransform.CreateSelectionBox(start, end);

        Assert.Equal(100f, box.X);
        Assert.Equal(100f, box.Y);
        Assert.Equal(100f, box.Width);
        Assert.Equal(100f, box.Height);
    }

    [Fact]
    public void CanvasToScreen_Rectangle_TransformsCorrectly()
    {
        var canvasRect = new Rectangle(100, 100, 50, 50);
        var pan = Vector2.Zero;
        var zoom = 2f;
        var origin = Vector2.Zero;

        var screenRect = GraphTransform.CanvasToScreen(canvasRect, pan, zoom, origin);

        Assert.Equal(200f, screenRect.X);
        Assert.Equal(200f, screenRect.Y);
        Assert.Equal(100f, screenRect.Width);
        Assert.Equal(100f, screenRect.Height);
    }
}
