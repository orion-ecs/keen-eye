// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Tools;

namespace KeenEyes.Editor.Tests.Tools;

public class RotateToolTests : IDisposable
{
    private readonly World world;

    public RotateToolTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Property Tests

    [Fact]
    public void DisplayName_ReturnsRotate()
    {
        var tool = new RotateTool();

        Assert.Equal("Rotate", tool.DisplayName);
    }

    [Fact]
    public void Icon_ReturnsRotate()
    {
        var tool = new RotateTool();

        Assert.Equal("rotate", tool.Icon);
    }

    [Fact]
    public void Category_ReturnsTransform()
    {
        var tool = new RotateTool();

        Assert.Equal(ToolCategories.Transform, tool.Category);
    }

    [Fact]
    public void Tooltip_ReturnsExpectedValue()
    {
        var tool = new RotateTool();

        Assert.Equal("Rotate selected entities (E)", tool.Tooltip);
    }

    [Fact]
    public void Shortcut_ReturnsE()
    {
        var tool = new RotateTool();

        Assert.Equal("E", tool.Shortcut);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue()
    {
        var tool = new RotateTool();

        Assert.True(tool.IsEnabled);
    }

    #endregion

    #region Mouse Down Tests

    [Fact]
    public void OnMouseDown_WithLeftButton_NoSelection_ReturnsFalse()
    {
        var tool = new RotateTool();
        var context = CreateToolContext([]);

        var result = tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseDown_WithRightButton_ReturnsFalse()
    {
        var tool = new RotateTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        var result = tool.OnMouseDown(context, MouseButton.Right, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseDown_WithMiddleButton_ReturnsFalse()
    {
        var tool = new RotateTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        var result = tool.OnMouseDown(context, MouseButton.Middle, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    #endregion

    #region Mouse Up Tests

    [Fact]
    public void OnMouseUp_WithoutPriorMouseDown_ReturnsFalse()
    {
        var tool = new RotateTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseUp_WithRightButton_ReturnsFalse()
    {
        var tool = new RotateTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        var result = tool.OnMouseUp(context, MouseButton.Right, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    #endregion

    #region Mouse Move Tests

    [Fact]
    public void OnMouseMove_WithoutDragging_ReturnsFalse()
    {
        var tool = new RotateTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        var result = tool.OnMouseMove(context, new Vector2(0.5f, 0.5f), new Vector2(0.1f, 0.1f));

        Assert.False(result);
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void OnActivate_ResetsDragState()
    {
        var tool = new RotateTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        tool.OnActivate(context);

        // After activation, mouse move should return false (not dragging)
        var result = tool.OnMouseMove(context, new Vector2(0.6f, 0.6f), new Vector2(0.1f, 0.1f));
        Assert.False(result);
    }

    [Fact]
    public void OnDeactivate_CancelsDragging()
    {
        var tool = new RotateTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        tool.OnDeactivate(context);

        // After deactivation, mouse up should return false
        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        Assert.False(result);
    }

    #endregion

    private Entity CreateEntityWithTransform()
    {
        return world.Spawn("TestEntity")
            .With(new Transform3D { Position = Vector3.Zero, Rotation = Quaternion.Identity, Scale = Vector3.One })
            .Build();
    }

    private ToolContext CreateToolContext(Entity[] selectedEntities)
    {
        return new ToolContext
        {
            EditorContext = new MockEditorContext(),
            SceneWorld = world,
            SelectedEntities = selectedEntities,
            ViewportBounds = new ViewportBounds { X = 0, Y = 0, Width = 800, Height = 600 },
            ViewMatrix = Matrix4x4.Identity,
            ProjectionMatrix = Matrix4x4.Identity,
            CameraPosition = new Vector3(0, 0, 10),
            CameraForward = -Vector3.UnitZ
        };
    }
}
