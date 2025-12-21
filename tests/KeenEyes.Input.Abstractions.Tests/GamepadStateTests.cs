using System.Numerics;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="GamepadState"/> struct.
/// </summary>
public class GamepadStateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsIndex()
    {
        var state = new GamepadState(2, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.Equal(2, state.Index);
    }

    [Fact]
    public void Constructor_SetsIsConnected()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsConnected);
    }

    [Fact]
    public void Constructor_SetsPressedButtons()
    {
        var buttons = GamepadButtons.South | GamepadButtons.East;
        var state = new GamepadState(0, true, buttons, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.Equal(buttons, state.PressedButtons);
    }

    [Fact]
    public void Constructor_SetsLeftStick()
    {
        var leftStick = new Vector2(0.5f, -0.3f);
        var state = new GamepadState(0, true, GamepadButtons.None, leftStick, Vector2.Zero, 0f, 0f);

        Assert.Equal(leftStick, state.LeftStick);
        Assert.Equal(0.5f, state.LeftStickX);
        Assert.Equal(-0.3f, state.LeftStickY);
    }

    [Fact]
    public void Constructor_SetsRightStick()
    {
        var rightStick = new Vector2(-0.2f, 0.8f);
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, rightStick, 0f, 0f);

        Assert.Equal(rightStick, state.RightStick);
        Assert.Equal(-0.2f, state.RightStickX);
        Assert.Equal(0.8f, state.RightStickY);
    }

    [Fact]
    public void Constructor_SetsLeftTrigger()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0.75f, 0f);

        Assert.Equal(0.75f, state.LeftTrigger);
    }

    [Fact]
    public void Constructor_SetsRightTrigger()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0.9f);

        Assert.Equal(0.9f, state.RightTrigger);
    }

    #endregion

    #region Disconnected State Tests

    [Fact]
    public void Disconnected_IsNotConnected()
    {
        Assert.False(GamepadState.Disconnected.IsConnected);
    }

    [Fact]
    public void Disconnected_HasNegativeIndex()
    {
        Assert.Equal(-1, GamepadState.Disconnected.Index);
    }

    [Fact]
    public void Disconnected_HasNoPressedButtons()
    {
        Assert.Equal(GamepadButtons.None, GamepadState.Disconnected.PressedButtons);
    }

    [Fact]
    public void Disconnected_HasZeroStickValues()
    {
        Assert.Equal(Vector2.Zero, GamepadState.Disconnected.LeftStick);
        Assert.Equal(Vector2.Zero, GamepadState.Disconnected.RightStick);
    }

    [Fact]
    public void Disconnected_HasZeroTriggerValues()
    {
        Assert.Equal(0f, GamepadState.Disconnected.LeftTrigger);
        Assert.Equal(0f, GamepadState.Disconnected.RightTrigger);
    }

    #endregion

    #region IsButtonDown/IsButtonUp Tests

    [Fact]
    public void IsButtonDown_WithSouth_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.South));
    }

    [Fact]
    public void IsButtonDown_WithEast_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.East, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.East));
    }

    [Fact]
    public void IsButtonDown_WithWest_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.West, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.West));
    }

    [Fact]
    public void IsButtonDown_WithNorth_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.North, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.North));
    }

    [Fact]
    public void IsButtonDown_WithLeftShoulder_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.LeftShoulder, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.LeftShoulder));
    }

    [Fact]
    public void IsButtonDown_WithRightShoulder_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.RightShoulder, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.RightShoulder));
    }

    [Fact]
    public void IsButtonDown_WithLeftTrigger_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.LeftTrigger, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.LeftTrigger));
    }

    [Fact]
    public void IsButtonDown_WithRightTrigger_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.RightTrigger, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.RightTrigger));
    }

    [Fact]
    public void IsButtonDown_WithDPadButtons_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.DPadUp | GamepadButtons.DPadLeft, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.DPadUp));
        Assert.True(state.IsButtonDown(GamepadButton.DPadLeft));
        Assert.False(state.IsButtonDown(GamepadButton.DPadDown));
        Assert.False(state.IsButtonDown(GamepadButton.DPadRight));
    }

    [Fact]
    public void IsButtonDown_WithStickButtons_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.LeftStick | GamepadButtons.RightStick, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.LeftStick));
        Assert.True(state.IsButtonDown(GamepadButton.RightStick));
    }

    [Fact]
    public void IsButtonDown_WithMenuButtons_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.Start | GamepadButtons.Back | GamepadButtons.Guide, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonDown(GamepadButton.Start));
        Assert.True(state.IsButtonDown(GamepadButton.Back));
        Assert.True(state.IsButtonDown(GamepadButton.Guide));
    }

    [Fact]
    public void IsButtonDown_WithUnpressedButton_ReturnsFalse()
    {
        var state = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.False(state.IsButtonDown(GamepadButton.North));
    }

    [Fact]
    public void IsButtonDown_WithUnknownButton_ReturnsFalse()
    {
        var state = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.False(state.IsButtonDown(GamepadButton.Unknown));
    }

    [Fact]
    public void IsButtonUp_WithPressedButton_ReturnsFalse()
    {
        var state = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.False(state.IsButtonUp(GamepadButton.South));
    }

    [Fact]
    public void IsButtonUp_WithUnpressedButton_ReturnsTrue()
    {
        var state = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.True(state.IsButtonUp(GamepadButton.North));
    }

    #endregion

    #region GetAxis Tests

    [Fact]
    public void GetAxis_LeftStickX_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, new Vector2(0.7f, 0.2f), Vector2.Zero, 0f, 0f);

        Assert.Equal(0.7f, state.GetAxis(GamepadAxis.LeftStickX));
    }

    [Fact]
    public void GetAxis_LeftStickY_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, new Vector2(0.7f, 0.2f), Vector2.Zero, 0f, 0f);

        Assert.Equal(0.2f, state.GetAxis(GamepadAxis.LeftStickY));
    }

    [Fact]
    public void GetAxis_RightStickX_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, new Vector2(-0.5f, 0.8f), 0f, 0f);

        Assert.Equal(-0.5f, state.GetAxis(GamepadAxis.RightStickX));
    }

    [Fact]
    public void GetAxis_RightStickY_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, new Vector2(-0.5f, 0.8f), 0f, 0f);

        Assert.Equal(0.8f, state.GetAxis(GamepadAxis.RightStickY));
    }

    [Fact]
    public void GetAxis_LeftTrigger_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0.6f, 0f);

        Assert.Equal(0.6f, state.GetAxis(GamepadAxis.LeftTrigger));
    }

    [Fact]
    public void GetAxis_RightTrigger_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0.9f);

        Assert.Equal(0.9f, state.GetAxis(GamepadAxis.RightTrigger));
    }

    [Fact]
    public void GetAxis_UnknownAxis_ReturnsZero()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.Equal(0f, state.GetAxis(GamepadAxis.Unknown));
    }

    #endregion

    #region Stick Property Tests

    [Fact]
    public void LeftStickX_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, new Vector2(0.5f, -0.3f), Vector2.Zero, 0f, 0f);

        Assert.Equal(0.5f, state.LeftStickX);
    }

    [Fact]
    public void LeftStickY_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, new Vector2(0.5f, -0.3f), Vector2.Zero, 0f, 0f);

        Assert.Equal(-0.3f, state.LeftStickY);
    }

    [Fact]
    public void RightStickX_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, new Vector2(-0.2f, 0.8f), 0f, 0f);

        Assert.Equal(-0.2f, state.RightStickX);
    }

    [Fact]
    public void RightStickY_ReturnsCorrectValue()
    {
        var state = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, new Vector2(-0.2f, 0.8f), 0f, 0f);

        Assert.Equal(0.8f, state.RightStickY);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WhenConnected_IncludesIndexAndButtons()
    {
        var state = new GamepadState(2, true, GamepadButtons.South | GamepadButtons.East, Vector2.Zero, Vector2.Zero, 0f, 0f);

        var result = state.ToString();

        Assert.Contains("2", result);
        Assert.Contains("South", result);
        Assert.Contains("East", result);
    }

    [Fact]
    public void ToString_WhenDisconnected_IndicatesDisconnected()
    {
        var state = new GamepadState(0, false, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);

        var result = state.ToString();

        Assert.Contains("Disconnected", result);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var state1 = new GamepadState(0, true, GamepadButtons.South, new Vector2(0.5f, 0.3f), new Vector2(-0.2f, 0.8f), 0.6f, 0.7f);
        var state2 = new GamepadState(0, true, GamepadButtons.South, new Vector2(0.5f, 0.3f), new Vector2(-0.2f, 0.8f), 0.6f, 0.7f);

        Assert.Equal(state1, state2);
    }

    [Fact]
    public void Equals_DifferentIndex_ReturnsFalse()
    {
        var state1 = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);
        var state2 = new GamepadState(1, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void Equals_DifferentConnected_ReturnsFalse()
    {
        var state1 = new GamepadState(0, true, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);
        var state2 = new GamepadState(0, false, GamepadButtons.None, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void Equals_DifferentButtons_ReturnsFalse()
    {
        var state1 = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);
        var state2 = new GamepadState(0, true, GamepadButtons.East, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void Equals_DifferentLeftStick_ReturnsFalse()
    {
        var state1 = new GamepadState(0, true, GamepadButtons.None, new Vector2(0.5f, 0.3f), Vector2.Zero, 0f, 0f);
        var state2 = new GamepadState(0, true, GamepadButtons.None, new Vector2(0.6f, 0.3f), Vector2.Zero, 0f, 0f);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var state1 = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);
        var state2 = new GamepadState(0, true, GamepadButtons.South, Vector2.Zero, Vector2.Zero, 0f, 0f);

        Assert.Equal(state1.GetHashCode(), state2.GetHashCode());
    }

    #endregion
}
