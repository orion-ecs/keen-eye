// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Tools;

namespace KeenEyes.Editor.Tests.Tools;

public class MoveToolTests : IDisposable
{
    private readonly World world;

    public MoveToolTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Property Tests

    [Fact]
    public void DisplayName_ReturnsMove()
    {
        var tool = new MoveTool();

        Assert.Equal("Move", tool.DisplayName);
    }

    [Fact]
    public void Icon_ReturnsMove()
    {
        var tool = new MoveTool();

        Assert.Equal("move", tool.Icon);
    }

    [Fact]
    public void Category_ReturnsTransform()
    {
        var tool = new MoveTool();

        Assert.Equal(ToolCategories.Transform, tool.Category);
    }

    [Fact]
    public void Tooltip_ReturnsExpectedValue()
    {
        var tool = new MoveTool();

        Assert.Equal("Move selected entities (W)", tool.Tooltip);
    }

    [Fact]
    public void Shortcut_ReturnsW()
    {
        var tool = new MoveTool();

        Assert.Equal("W", tool.Shortcut);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue()
    {
        var tool = new MoveTool();

        Assert.True(tool.IsEnabled);
    }

    #endregion

    #region Mouse Down Tests

    [Fact]
    public void OnMouseDown_WithLeftButton_NoSelection_ReturnsFalse()
    {
        var tool = new MoveTool();
        var context = CreateToolContext([]);

        var result = tool.OnMouseDown(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseDown_WithRightButton_ReturnsFalse()
    {
        var tool = new MoveTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        var result = tool.OnMouseDown(context, MouseButton.Right, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseDown_WithMiddleButton_ReturnsFalse()
    {
        var tool = new MoveTool();
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
        var tool = new MoveTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));

        Assert.False(result);
    }

    [Fact]
    public void OnMouseUp_WithRightButton_ReturnsFalse()
    {
        var tool = new MoveTool();
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
        var tool = new MoveTool();
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
        var tool = new MoveTool();
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
        var tool = new MoveTool();
        var entity = CreateEntityWithTransform();
        var context = CreateToolContext([entity]);

        tool.OnDeactivate(context);

        // After deactivation, mouse up should return false
        var result = tool.OnMouseUp(context, MouseButton.Left, new Vector2(0.5f, 0.5f));
        Assert.False(result);
    }

    #endregion

    #region TransformAxis Enum Tests

    [Fact]
    public void TransformAxis_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.None));
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.X));
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.Y));
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.Z));
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.XY));
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.XZ));
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.YZ));
        Assert.True(Enum.IsDefined(typeof(TransformAxis), TransformAxis.All));
    }

    [Fact]
    public void TransformAxis_HasEightValues()
    {
        var values = Enum.GetValues<TransformAxis>();

        Assert.Equal(8, values.Length);
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
