using System.Collections.Immutable;
using System.Numerics;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Integration tests for <see cref="InputAction"/> with <see cref="IInputContext"/>.
/// </summary>
public class InputActionIntegrationTests
{
    #region IsPressed Tests

    [Fact]
    public void IsPressed_WithKeyBinding_WhenKeyPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.Space);

        var action = new InputAction("Jump", InputBinding.FromKey(Key.Space));

        Assert.True(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WithKeyBinding_WhenKeyNotPressed_ReturnsFalse()
    {
        var context = new MockInputContext();
        var action = new InputAction("Jump", InputBinding.FromKey(Key.Space));

        Assert.False(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WithMouseBinding_WhenButtonPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.Mouse.PressButton(MouseButton.Left);

        var action = new InputAction("Fire", InputBinding.FromMouseButton(MouseButton.Left));

        Assert.True(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WithGamepadBinding_WhenButtonPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).PressButton(GamepadButton.South);

        var action = new InputAction("Jump", InputBinding.FromGamepadButton(GamepadButton.South));

        Assert.True(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WhenDisabled_ReturnsFalse()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.Space);

        var action = new InputAction("Jump", InputBinding.FromKey(Key.Space)) { Enabled = false };

        Assert.False(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WithMultipleBindings_AnyPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.W);

        var action = new InputAction("MoveForward",
            InputBinding.FromKey(Key.W),
            InputBinding.FromKey(Key.Up));

        Assert.True(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WithGamepadAxis_AboveThreshold_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.RightTrigger, 0.8f);

        var action = new InputAction("Accelerate", InputBinding.FromGamepadAxis(GamepadAxis.RightTrigger, 0.5f));

        Assert.True(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WithGamepadAxis_BelowThreshold_ReturnsFalse()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.RightTrigger, 0.3f);

        var action = new InputAction("Accelerate", InputBinding.FromGamepadAxis(GamepadAxis.RightTrigger, 0.5f));

        Assert.False(action.IsPressed(context));
    }

    #endregion

    #region IsReleased Tests

    [Fact]
    public void IsReleased_WhenPressed_ReturnsFalse()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.Space);

        var action = new InputAction("Jump", InputBinding.FromKey(Key.Space));

        Assert.False(action.IsReleased(context));
    }

    [Fact]
    public void IsReleased_WhenNotPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        var action = new InputAction("Jump", InputBinding.FromKey(Key.Space));

        Assert.True(action.IsReleased(context));
    }

    [Fact]
    public void IsReleased_WhenDisabled_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.Space);

        var action = new InputAction("Jump", InputBinding.FromKey(Key.Space)) { Enabled = false };

        Assert.True(action.IsReleased(context));
    }

    #endregion

    #region GetValue Tests

    [Fact]
    public void GetValue_WithKeyPressed_ReturnsOne()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.W);

        var action = new InputAction("MoveForward", InputBinding.FromKey(Key.W));

        Assert.Equal(1f, action.GetValue(context));
    }

    [Fact]
    public void GetValue_WithKeyNotPressed_ReturnsZero()
    {
        var context = new MockInputContext();
        var action = new InputAction("MoveForward", InputBinding.FromKey(Key.W));

        Assert.Equal(0f, action.GetValue(context));
    }

    [Fact]
    public void GetValue_WhenDisabled_ReturnsZero()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.W);

        var action = new InputAction("MoveForward", InputBinding.FromKey(Key.W)) { Enabled = false };

        Assert.Equal(0f, action.GetValue(context));
    }

    [Fact]
    public void GetValue_WithGamepadAxis_ReturnsAxisValue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, 0.75f);

        var action = new InputAction("Horizontal", InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.1f));

        Assert.Equal(0.75f, action.GetValue(context), 0.001f);
    }

    [Fact]
    public void GetValue_WithMultipleBindings_ReturnsLargestValue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, 0.5f);
        context.GetGamepad(0).SetAxis(GamepadAxis.RightStickX, 0.8f);

        var action = new InputAction("Horizontal",
            InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX),
            InputBinding.FromGamepadAxis(GamepadAxis.RightStickX));

        Assert.Equal(0.8f, action.GetValue(context), 0.001f);
    }

    #endregion

    #region GamepadIndex Tests

    [Fact]
    public void IsPressed_WithSpecificGamepadIndex_OnlyChecksSpecifiedGamepad()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.ConnectGamepad(1);
        context.GetGamepad(1).PressButton(GamepadButton.South);

        var action = new InputAction("Jump", InputBinding.FromGamepadButton(GamepadButton.South)) { GamepadIndex = 0 };

        Assert.False(action.IsPressed(context));
    }

    [Fact]
    public void IsPressed_WithAnyGamepadIndex_ChecksAllGamepads()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.ConnectGamepad(1);
        context.GetGamepad(1).PressButton(GamepadButton.South);

        var action = new InputAction("Jump", InputBinding.FromGamepadButton(GamepadButton.South)) { GamepadIndex = -1 };

        Assert.True(action.IsPressed(context));
    }

    #endregion
}
