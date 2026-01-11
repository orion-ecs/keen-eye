// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Tools;

namespace KeenEyes.Editor.Tests.Tools;

public class SelectToolTests
{
    #region Property Tests

    [Fact]
    public void DisplayName_ReturnsSelect()
    {
        var tool = new SelectTool();

        Assert.Equal("Select", tool.DisplayName);
    }

    [Fact]
    public void Icon_ReturnsCursor()
    {
        var tool = new SelectTool();

        Assert.Equal("cursor", tool.Icon);
    }

    [Fact]
    public void Category_ReturnsSelection()
    {
        var tool = new SelectTool();

        Assert.Equal(ToolCategories.Selection, tool.Category);
    }

    [Fact]
    public void Tooltip_ReturnsExpectedValue()
    {
        var tool = new SelectTool();

        Assert.Equal("Select entities (Q)", tool.Tooltip);
    }

    [Fact]
    public void Shortcut_ReturnsQ()
    {
        var tool = new SelectTool();

        Assert.Equal("Q", tool.Shortcut);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue()
    {
        var tool = new SelectTool();

        Assert.True(tool.IsEnabled);
    }

    #endregion

    #region Mouse Down Tests

    [Fact]
    public void OnMouseDown_WithLeftButton_ReturnsTrue()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        var result = tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        Assert.True(result);
    }

    [Fact]
    public void OnMouseDown_WithRightButton_ReturnsFalse()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        var result = tool.OnMouseDown(context, MouseButton.Right, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseDown_WithMiddleButton_ReturnsFalse()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        var result = tool.OnMouseDown(context, MouseButton.Middle, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    #endregion

    #region Mouse Up Tests

    [Fact]
    public void OnMouseUp_WithoutPriorMouseDown_ReturnsFalse()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseUp_AfterMouseDown_ReturnsTrue()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        Assert.True(result);
    }

    [Fact]
    public void OnMouseUp_WithRightButton_ReturnsFalse()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        var result = tool.OnMouseUp(context, MouseButton.Right, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    #endregion

    #region Mouse Move Tests

    [Fact]
    public void OnMouseMove_WithoutDragging_ReturnsFalse()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        var result = tool.OnMouseMove(context, new Vector2(0.5f, 0.5f), new Vector2(0.1f, 0.1f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseMove_WhileDragging_ReturnsTrue()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        var result = tool.OnMouseMove(context, new Vector2(0.6f, 0.6f), new Vector2(0.1f, 0.1f));

        Assert.True(result);
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void OnActivate_ResetsDragState()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        // Start dragging
        tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        // Deactivate and reactivate
        tool.OnDeactivate(context);
        tool.OnActivate(context);

        // Should not be dragging anymore
        var result = tool.OnMouseMove(context, new Vector2(0.6f, 0.6f), new Vector2(0.1f, 0.1f));
        Assert.False(result);
    }

    [Fact]
    public void OnDeactivate_StopsDragging()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        tool.OnDeactivate(context);

        // Mouse up should return false since we're no longer dragging
        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        Assert.False(result);
    }

    #endregion

    #region Click vs Drag Detection Tests

    [Fact]
    public void OnMouseUp_WithSmallMovement_IsConsideredClick()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        // Small movement (less than 5 pixels threshold)
        tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.502f, 0.502f));

        // Click behavior - returns true
        Assert.True(result);
    }

    [Fact]
    public void OnMouseUp_WithLargeMovement_IsConsideredDrag()
    {
        var tool = new SelectTool();
        var context = CreateToolContext();

        // Large movement (more than 5 pixels threshold)
        tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.1f, 0.1f));
        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        // Drag behavior - returns true (marquee selection)
        Assert.True(result);
    }

    #endregion

    private static ToolContext CreateToolContext()
    {
        return new ToolContext
        {
            EditorContext = new MockEditorContext(),
            SceneWorld = null,
            SelectedEntities = [],
            ViewportBounds = new ViewportBounds { X = 0, Y = 0, Width = 800, Height = 600 },
            ViewMatrix = Matrix4x4.Identity,
            ProjectionMatrix = Matrix4x4.Identity,
            CameraPosition = Vector3.Zero,
            CameraForward = -Vector3.UnitZ
        };
    }
}
