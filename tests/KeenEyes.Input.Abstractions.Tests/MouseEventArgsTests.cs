using System.Numerics;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for mouse event argument structs.
/// </summary>
public class MouseEventArgsTests
{
    #region MouseButtonEventArgs Tests

    [Fact]
    public void MouseButtonEventArgs_Constructor_SetsButton()
    {
        var args = new MouseButtonEventArgs(MouseButton.Left, Vector2.Zero, KeyModifiers.None);

        Assert.Equal(MouseButton.Left, args.Button);
    }

    [Fact]
    public void MouseButtonEventArgs_Constructor_SetsPosition()
    {
        var position = new Vector2(100, 200);
        var args = new MouseButtonEventArgs(MouseButton.Left, position, KeyModifiers.None);

        Assert.Equal(position, args.Position);
        Assert.Equal(100, args.X);
        Assert.Equal(200, args.Y);
    }

    [Fact]
    public void MouseButtonEventArgs_Constructor_SetsModifiers()
    {
        var args = new MouseButtonEventArgs(MouseButton.Right, Vector2.Zero, KeyModifiers.Shift);

        Assert.Equal(KeyModifiers.Shift, args.Modifiers);
    }

    [Fact]
    public void MouseButtonEventArgs_IsShiftDown_WithShift_ReturnsTrue()
    {
        var args = new MouseButtonEventArgs(MouseButton.Left, Vector2.Zero, KeyModifiers.Shift);

        Assert.True(args.IsShiftDown);
    }

    [Fact]
    public void MouseButtonEventArgs_IsControlDown_WithControl_ReturnsTrue()
    {
        var args = new MouseButtonEventArgs(MouseButton.Left, Vector2.Zero, KeyModifiers.Control);

        Assert.True(args.IsControlDown);
    }

    [Fact]
    public void MouseButtonEventArgs_IsAltDown_WithAlt_ReturnsTrue()
    {
        var args = new MouseButtonEventArgs(MouseButton.Left, Vector2.Zero, KeyModifiers.Alt);

        Assert.True(args.IsAltDown);
    }

    [Fact]
    public void MouseButtonEventArgs_Create_SetsButtonAndPosition()
    {
        var position = new Vector2(50, 75);
        var args = MouseButtonEventArgs.Create(MouseButton.Middle, position);

        Assert.Equal(MouseButton.Middle, args.Button);
        Assert.Equal(position, args.Position);
    }

    [Fact]
    public void MouseButtonEventArgs_Create_SetsModifiersToNone()
    {
        var args = MouseButtonEventArgs.Create(MouseButton.Left, Vector2.Zero);

        Assert.Equal(KeyModifiers.None, args.Modifiers);
    }

    [Fact]
    public void MouseButtonEventArgs_ToString_IncludesButtonAndPosition()
    {
        var args = new MouseButtonEventArgs(MouseButton.Right, new Vector2(10, 20), KeyModifiers.None);

        var result = args.ToString();

        Assert.Contains("Right", result);
        Assert.Contains("10", result);
        Assert.Contains("20", result);
    }

    [Fact]
    public void MouseButtonEventArgs_Equality_SameValues_ReturnsTrue()
    {
        var args1 = new MouseButtonEventArgs(MouseButton.Left, new Vector2(10, 20), KeyModifiers.Shift);
        var args2 = new MouseButtonEventArgs(MouseButton.Left, new Vector2(10, 20), KeyModifiers.Shift);

        Assert.Equal(args1, args2);
    }

    [Fact]
    public void MouseButtonEventArgs_Equality_DifferentButton_ReturnsFalse()
    {
        var args1 = new MouseButtonEventArgs(MouseButton.Left, Vector2.Zero, KeyModifiers.None);
        var args2 = new MouseButtonEventArgs(MouseButton.Right, Vector2.Zero, KeyModifiers.None);

        Assert.NotEqual(args1, args2);
    }

    #endregion

    #region MouseMoveEventArgs Tests

    [Fact]
    public void MouseMoveEventArgs_Constructor_SetsPosition()
    {
        var position = new Vector2(150, 250);
        var args = new MouseMoveEventArgs(position, Vector2.Zero);

        Assert.Equal(position, args.Position);
        Assert.Equal(150, args.X);
        Assert.Equal(250, args.Y);
    }

    [Fact]
    public void MouseMoveEventArgs_Constructor_SetsDelta()
    {
        var delta = new Vector2(5, -10);
        var args = new MouseMoveEventArgs(Vector2.Zero, delta);

        Assert.Equal(delta, args.Delta);
        Assert.Equal(5, args.DeltaX);
        Assert.Equal(-10, args.DeltaY);
    }

    [Fact]
    public void MouseMoveEventArgs_Create_SetsAllValues()
    {
        var args = MouseMoveEventArgs.Create(100, 200, 10, -5);

        Assert.Equal(100, args.X);
        Assert.Equal(200, args.Y);
        Assert.Equal(10, args.DeltaX);
        Assert.Equal(-5, args.DeltaY);
    }

    [Fact]
    public void MouseMoveEventArgs_ToString_IncludesPositionAndDelta()
    {
        var args = new MouseMoveEventArgs(new Vector2(50, 60), new Vector2(2, -3));

        var result = args.ToString();

        Assert.Contains("50", result);
        Assert.Contains("60", result);
        Assert.Contains("2", result);
    }

    [Fact]
    public void MouseMoveEventArgs_Equality_SameValues_ReturnsTrue()
    {
        var args1 = new MouseMoveEventArgs(new Vector2(10, 20), new Vector2(1, 2));
        var args2 = new MouseMoveEventArgs(new Vector2(10, 20), new Vector2(1, 2));

        Assert.Equal(args1, args2);
    }

    [Fact]
    public void MouseMoveEventArgs_Equality_DifferentPosition_ReturnsFalse()
    {
        var args1 = new MouseMoveEventArgs(new Vector2(10, 20), Vector2.Zero);
        var args2 = new MouseMoveEventArgs(new Vector2(11, 20), Vector2.Zero);

        Assert.NotEqual(args1, args2);
    }

    #endregion

    #region MouseScrollEventArgs Tests

    [Fact]
    public void MouseScrollEventArgs_Constructor_SetsDelta()
    {
        var delta = new Vector2(1, -2);
        var args = new MouseScrollEventArgs(delta, Vector2.Zero);

        Assert.Equal(delta, args.Delta);
        Assert.Equal(1, args.DeltaX);
        Assert.Equal(-2, args.DeltaY);
    }

    [Fact]
    public void MouseScrollEventArgs_Constructor_SetsPosition()
    {
        var position = new Vector2(300, 400);
        var args = new MouseScrollEventArgs(Vector2.Zero, position);

        Assert.Equal(position, args.Position);
        Assert.Equal(300, args.X);
        Assert.Equal(400, args.Y);
    }

    [Fact]
    public void MouseScrollEventArgs_Vertical_SetsOnlyYDelta()
    {
        var position = new Vector2(50, 60);
        var args = MouseScrollEventArgs.Vertical(3, position);

        Assert.Equal(0, args.DeltaX);
        Assert.Equal(3, args.DeltaY);
        Assert.Equal(position, args.Position);
    }

    [Fact]
    public void MouseScrollEventArgs_Horizontal_SetsOnlyXDelta()
    {
        var position = new Vector2(70, 80);
        var args = MouseScrollEventArgs.Horizontal(2, position);

        Assert.Equal(2, args.DeltaX);
        Assert.Equal(0, args.DeltaY);
        Assert.Equal(position, args.Position);
    }

    [Fact]
    public void MouseScrollEventArgs_ToString_IncludesDelta()
    {
        var args = new MouseScrollEventArgs(new Vector2(1.5f, -2.5f), Vector2.Zero);

        var result = args.ToString();

        Assert.Contains("1", result);
        Assert.Contains("2", result);
    }

    [Fact]
    public void MouseScrollEventArgs_Equality_SameValues_ReturnsTrue()
    {
        var args1 = new MouseScrollEventArgs(new Vector2(1, 2), new Vector2(10, 20));
        var args2 = new MouseScrollEventArgs(new Vector2(1, 2), new Vector2(10, 20));

        Assert.Equal(args1, args2);
    }

    [Fact]
    public void MouseScrollEventArgs_Equality_DifferentDelta_ReturnsFalse()
    {
        var args1 = new MouseScrollEventArgs(new Vector2(1, 2), Vector2.Zero);
        var args2 = new MouseScrollEventArgs(new Vector2(1, 3), Vector2.Zero);

        Assert.NotEqual(args1, args2);
    }

    #endregion
}
