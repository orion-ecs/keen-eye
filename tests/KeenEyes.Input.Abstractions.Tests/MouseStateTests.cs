using System.Numerics;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="MouseState"/> struct.
/// </summary>
public class MouseStateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsPosition()
    {
        var position = new Vector2(150, 250);
        var state = new MouseState(position, MouseButtons.None, Vector2.Zero);

        Assert.Equal(position, state.Position);
        Assert.Equal(150, state.X);
        Assert.Equal(250, state.Y);
    }

    [Fact]
    public void Constructor_SetsPressedButtons()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left | MouseButtons.Right, Vector2.Zero);

        Assert.Equal(MouseButtons.Left | MouseButtons.Right, state.PressedButtons);
    }

    [Fact]
    public void Constructor_SetsScrollDelta()
    {
        var scrollDelta = new Vector2(1, -2);
        var state = new MouseState(Vector2.Zero, MouseButtons.None, scrollDelta);

        Assert.Equal(scrollDelta, state.ScrollDelta);
        Assert.Equal(1, state.ScrollX);
        Assert.Equal(-2, state.ScrollY);
    }

    #endregion

    #region Empty State Tests

    [Fact]
    public void Empty_HasZeroPosition()
    {
        Assert.Equal(Vector2.Zero, MouseState.Empty.Position);
        Assert.Equal(0, MouseState.Empty.X);
        Assert.Equal(0, MouseState.Empty.Y);
    }

    [Fact]
    public void Empty_HasNoPressedButtons()
    {
        Assert.Equal(MouseButtons.None, MouseState.Empty.PressedButtons);
    }

    [Fact]
    public void Empty_HasZeroScrollDelta()
    {
        Assert.Equal(Vector2.Zero, MouseState.Empty.ScrollDelta);
        Assert.Equal(0, MouseState.Empty.ScrollX);
        Assert.Equal(0, MouseState.Empty.ScrollY);
    }

    #endregion

    #region IsButtonDown/IsButtonUp Tests

    [Fact]
    public void IsButtonDown_WithLeftButton_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.True(state.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void IsButtonDown_WithRightButton_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Right, Vector2.Zero);

        Assert.True(state.IsButtonDown(MouseButton.Right));
    }

    [Fact]
    public void IsButtonDown_WithMiddleButton_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Middle, Vector2.Zero);

        Assert.True(state.IsButtonDown(MouseButton.Middle));
    }

    [Fact]
    public void IsButtonDown_WithButton4_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Button4, Vector2.Zero);

        Assert.True(state.IsButtonDown(MouseButton.Button4));
    }

    [Fact]
    public void IsButtonDown_WithButton5_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Button5, Vector2.Zero);

        Assert.True(state.IsButtonDown(MouseButton.Button5));
    }

    [Fact]
    public void IsButtonDown_WithUnpressedButton_ReturnsFalse()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.False(state.IsButtonDown(MouseButton.Right));
    }

    [Fact]
    public void IsButtonDown_WithUnknownButton_ReturnsFalse()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.False(state.IsButtonDown(MouseButton.Unknown));
    }

    [Fact]
    public void IsButtonUp_WithPressedButton_ReturnsFalse()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.False(state.IsButtonUp(MouseButton.Left));
    }

    [Fact]
    public void IsButtonUp_WithUnpressedButton_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.True(state.IsButtonUp(MouseButton.Right));
    }

    [Fact]
    public void IsButtonDown_WithMultipleButtons_WorksForAll()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left | MouseButtons.Right | MouseButtons.Middle, Vector2.Zero);

        Assert.True(state.IsButtonDown(MouseButton.Left));
        Assert.True(state.IsButtonDown(MouseButton.Right));
        Assert.True(state.IsButtonDown(MouseButton.Middle));
        Assert.False(state.IsButtonDown(MouseButton.Button4));
    }

    #endregion

    #region Convenience Property Tests

    [Fact]
    public void IsLeftButtonDown_WithLeftButton_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.True(state.IsLeftButtonDown);
    }

    [Fact]
    public void IsLeftButtonDown_WithoutLeftButton_ReturnsFalse()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Right, Vector2.Zero);

        Assert.False(state.IsLeftButtonDown);
    }

    [Fact]
    public void IsRightButtonDown_WithRightButton_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Right, Vector2.Zero);

        Assert.True(state.IsRightButtonDown);
    }

    [Fact]
    public void IsRightButtonDown_WithoutRightButton_ReturnsFalse()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.False(state.IsRightButtonDown);
    }

    [Fact]
    public void IsMiddleButtonDown_WithMiddleButton_ReturnsTrue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Middle, Vector2.Zero);

        Assert.True(state.IsMiddleButtonDown);
    }

    [Fact]
    public void IsMiddleButtonDown_WithoutMiddleButton_ReturnsFalse()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);

        Assert.False(state.IsMiddleButtonDown);
    }

    #endregion

    #region Position Property Tests

    [Fact]
    public void X_ReturnsCorrectValue()
    {
        var state = new MouseState(new Vector2(123.5f, 456.7f), MouseButtons.None, Vector2.Zero);

        Assert.Equal(123.5f, state.X);
    }

    [Fact]
    public void Y_ReturnsCorrectValue()
    {
        var state = new MouseState(new Vector2(123.5f, 456.7f), MouseButtons.None, Vector2.Zero);

        Assert.Equal(456.7f, state.Y);
    }

    #endregion

    #region Scroll Property Tests

    [Fact]
    public void ScrollX_ReturnsCorrectValue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.None, new Vector2(2.5f, -1.5f));

        Assert.Equal(2.5f, state.ScrollX);
    }

    [Fact]
    public void ScrollY_ReturnsCorrectValue()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.None, new Vector2(2.5f, -1.5f));

        Assert.Equal(-1.5f, state.ScrollY);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesPositionAndButtons()
    {
        var state = new MouseState(new Vector2(10, 20), MouseButtons.Left, Vector2.Zero);

        var result = state.ToString();

        Assert.Contains("10", result);
        Assert.Contains("20", result);
        Assert.Contains("Left", result);
    }

    [Fact]
    public void ToString_WithMultipleButtons_IncludesAll()
    {
        var state = new MouseState(Vector2.Zero, MouseButtons.Left | MouseButtons.Right, Vector2.Zero);

        var result = state.ToString();

        Assert.Contains("Left", result);
        Assert.Contains("Right", result);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var state1 = new MouseState(new Vector2(10, 20), MouseButtons.Left, new Vector2(1, 2));
        var state2 = new MouseState(new Vector2(10, 20), MouseButtons.Left, new Vector2(1, 2));

        Assert.Equal(state1, state2);
    }

    [Fact]
    public void Equals_DifferentPosition_ReturnsFalse()
    {
        var state1 = new MouseState(new Vector2(10, 20), MouseButtons.None, Vector2.Zero);
        var state2 = new MouseState(new Vector2(11, 20), MouseButtons.None, Vector2.Zero);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void Equals_DifferentButtons_ReturnsFalse()
    {
        var state1 = new MouseState(Vector2.Zero, MouseButtons.Left, Vector2.Zero);
        var state2 = new MouseState(Vector2.Zero, MouseButtons.Right, Vector2.Zero);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void Equals_DifferentScrollDelta_ReturnsFalse()
    {
        var state1 = new MouseState(Vector2.Zero, MouseButtons.None, new Vector2(1, 2));
        var state2 = new MouseState(Vector2.Zero, MouseButtons.None, new Vector2(1, 3));

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var state1 = new MouseState(new Vector2(10, 20), MouseButtons.Left, Vector2.Zero);
        var state2 = new MouseState(new Vector2(10, 20), MouseButtons.Left, Vector2.Zero);

        Assert.Equal(state1.GetHashCode(), state2.GetHashCode());
    }

    #endregion
}
