using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for UI component types and their factory methods.
/// </summary>
public class UIComponentTests
{
    #region UIElement Tests

    [Fact]
    public void UIElement_Default_IsVisibleAndRaycastTarget()
    {
        var element = UIElement.Default;

        Assert.True(element.Visible);
        Assert.True(element.RaycastTarget);
    }

    [Fact]
    public void UIElement_NonInteractive_IsVisibleButNotRaycastTarget()
    {
        var element = UIElement.NonInteractive;

        Assert.True(element.Visible);
        Assert.False(element.RaycastTarget);
    }

    #endregion

    #region UIRect Tests

    [Fact]
    public void UIRect_Stretch_FillsParent()
    {
        var rect = UIRect.Stretch();

        Assert.Equal(Vector2.Zero, rect.AnchorMin);
        Assert.Equal(Vector2.One, rect.AnchorMax);
        Assert.Equal(new Vector2(0.5f, 0.5f), rect.Pivot);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void UIRect_Centered_HasCorrectAnchorsAndSize()
    {
        var rect = UIRect.Centered(200, 100);

        Assert.Equal(new Vector2(0.5f, 0.5f), rect.AnchorMin);
        Assert.Equal(new Vector2(0.5f, 0.5f), rect.AnchorMax);
        Assert.Equal(new Vector2(0.5f, 0.5f), rect.Pivot);
        Assert.Equal(new Vector2(200, 100), rect.Size);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void UIRect_Fixed_HasCorrectOffsetAndSize()
    {
        var rect = UIRect.Fixed(10, 20, 150, 75);

        Assert.Equal(Vector2.Zero, rect.AnchorMin);
        Assert.Equal(Vector2.Zero, rect.AnchorMax);
        Assert.Equal(Vector2.Zero, rect.Pivot);
        Assert.Equal(10, rect.Offset.Left);
        Assert.Equal(20, rect.Offset.Top);
        Assert.Equal(new Vector2(150, 75), rect.Size);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    #endregion

    #region UIInteractable Tests

    [Fact]
    public void UIInteractable_Clickable_HasCorrectSettings()
    {
        var interactable = UIInteractable.Clickable();

        Assert.False(interactable.CanFocus);
        Assert.True(interactable.CanClick);
        Assert.False(interactable.CanDrag);
        Assert.Equal(0, interactable.TabIndex);
    }

    [Fact]
    public void UIInteractable_Button_HasCorrectSettings()
    {
        var interactable = UIInteractable.Button(5);

        Assert.True(interactable.CanFocus);
        Assert.True(interactable.CanClick);
        Assert.False(interactable.CanDrag);
        Assert.Equal(5, interactable.TabIndex);
    }

    [Fact]
    public void UIInteractable_Draggable_HasCorrectSettings()
    {
        var interactable = UIInteractable.Draggable();

        Assert.False(interactable.CanFocus);
        Assert.False(interactable.CanClick);
        Assert.True(interactable.CanDrag);
    }

    [Fact]
    public void UIInteractable_HasEvent_DetectsPendingEvents()
    {
        var interactable = new UIInteractable
        {
            PendingEvents = UIEventFlags.Click | UIEventFlags.PointerEnter
        };

        Assert.True(interactable.HasEvent(UIEventFlags.Click));
        Assert.True(interactable.HasEvent(UIEventFlags.PointerEnter));
        Assert.False(interactable.HasEvent(UIEventFlags.PointerExit));
    }

    [Fact]
    public void UIInteractable_StateProperties_ReflectState()
    {
        var interactable = new UIInteractable
        {
            State = UIInteractionState.Hovered | UIInteractionState.Focused
        };

        Assert.True(interactable.IsHovered);
        Assert.True(interactable.IsFocused);
        Assert.False(interactable.IsPressed);
        Assert.False(interactable.IsDragging);
    }

    #endregion

    #region UILayout Tests

    [Fact]
    public void UILayout_Horizontal_HasCorrectSettings()
    {
        var layout = UILayout.Horizontal(10);

        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
        Assert.Equal(LayoutAlign.Start, layout.MainAxisAlign);
        Assert.Equal(LayoutAlign.Start, layout.CrossAxisAlign);
        Assert.Equal(10f, layout.Spacing);
        Assert.False(layout.Wrap);
        Assert.False(layout.ReverseOrder);
    }

    [Fact]
    public void UILayout_Vertical_HasCorrectSettings()
    {
        var layout = UILayout.Vertical(5);

        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
        Assert.Equal(LayoutAlign.Start, layout.MainAxisAlign);
        Assert.Equal(LayoutAlign.Start, layout.CrossAxisAlign);
        Assert.Equal(5f, layout.Spacing);
        Assert.False(layout.Wrap);
        Assert.False(layout.ReverseOrder);
    }

    [Fact]
    public void UILayout_HorizontalCentered_HasCenteredAlignment()
    {
        var layout = UILayout.HorizontalCentered(8);

        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
        Assert.Equal(LayoutAlign.Center, layout.MainAxisAlign);
        Assert.Equal(LayoutAlign.Center, layout.CrossAxisAlign);
        Assert.Equal(8f, layout.Spacing);
    }

    [Fact]
    public void UILayout_VerticalCentered_HasCenteredAlignment()
    {
        var layout = UILayout.VerticalCentered(12);

        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
        Assert.Equal(LayoutAlign.Center, layout.MainAxisAlign);
        Assert.Equal(LayoutAlign.Center, layout.CrossAxisAlign);
        Assert.Equal(12f, layout.Spacing);
    }

    #endregion

    #region UIEdges Tests

    [Fact]
    public void UIEdges_Zero_HasAllZeroValues()
    {
        var edges = UIEdges.Zero;

        Assert.Equal(0, edges.Left);
        Assert.Equal(0, edges.Top);
        Assert.Equal(0, edges.Right);
        Assert.Equal(0, edges.Bottom);
    }

    [Fact]
    public void UIEdges_Constructor_SetsAllValues()
    {
        var edges = new UIEdges(10, 20, 30, 40);

        Assert.Equal(10, edges.Left);
        Assert.Equal(20, edges.Top);
        Assert.Equal(30, edges.Right);
        Assert.Equal(40, edges.Bottom);
    }

    [Fact]
    public void UIEdges_HorizontalSize_ReturnsSumOfLeftAndRight()
    {
        var edges = new UIEdges(10, 5, 15, 5);

        Assert.Equal(25, edges.HorizontalSize);
    }

    [Fact]
    public void UIEdges_VerticalSize_ReturnsSumOfTopAndBottom()
    {
        var edges = new UIEdges(5, 10, 5, 20);

        Assert.Equal(30, edges.VerticalSize);
    }

    #endregion
}
