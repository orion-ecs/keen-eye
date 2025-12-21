using System.Numerics;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Integration tests for <see cref="InputBinding"/> with <see cref="IInputContext"/>.
/// </summary>
public class InputBindingIntegrationTests
{
    #region IsActive Tests - Key Bindings

    [Fact]
    public void IsActive_KeyBinding_WhenKeyPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.Space);
        var binding = InputBinding.FromKey(Key.Space);

        Assert.True(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_KeyBinding_WhenKeyNotPressed_ReturnsFalse()
    {
        var context = new MockInputContext();
        var binding = InputBinding.FromKey(Key.Space);

        Assert.False(binding.IsActive(context));
    }

    #endregion

    #region IsActive Tests - Mouse Bindings

    [Fact]
    public void IsActive_MouseButtonBinding_WhenButtonPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.Mouse.PressButton(MouseButton.Left);
        var binding = InputBinding.FromMouseButton(MouseButton.Left);

        Assert.True(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_MouseButtonBinding_WhenButtonNotPressed_ReturnsFalse()
    {
        var context = new MockInputContext();
        var binding = InputBinding.FromMouseButton(MouseButton.Right);

        Assert.False(binding.IsActive(context));
    }

    #endregion

    #region IsActive Tests - Gamepad Button Bindings

    [Fact]
    public void IsActive_GamepadButtonBinding_WhenButtonPressed_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).PressButton(GamepadButton.South);
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.True(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_GamepadButtonBinding_WhenButtonNotPressed_ReturnsFalse()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.False(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_GamepadButtonBinding_WithSpecificIndex_ChecksOnlyThatGamepad()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.ConnectGamepad(1);
        context.GetGamepad(1).PressButton(GamepadButton.South);
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.False(binding.IsActive(context, gamepadIndex: 0));
        Assert.True(binding.IsActive(context, gamepadIndex: 1));
    }

    [Fact]
    public void IsActive_GamepadButtonBinding_WithAnyIndex_ChecksAllGamepads()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.ConnectGamepad(1);
        context.GetGamepad(1).PressButton(GamepadButton.South);
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.True(binding.IsActive(context, gamepadIndex: -1));
    }

    [Fact]
    public void IsActive_GamepadButtonBinding_DisconnectedGamepad_ReturnsFalse()
    {
        var context = new MockInputContext();
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.False(binding.IsActive(context, gamepadIndex: 0));
    }

    #endregion

    #region IsActive Tests - Gamepad Axis Bindings

    [Fact]
    public void IsActive_GamepadAxisBinding_PositiveAxis_AboveThreshold_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.RightTrigger, 0.8f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.RightTrigger, 0.5f, isPositive: true);

        Assert.True(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_GamepadAxisBinding_PositiveAxis_BelowThreshold_ReturnsFalse()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.RightTrigger, 0.3f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.RightTrigger, 0.5f, isPositive: true);

        Assert.False(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_GamepadAxisBinding_NegativeAxis_BelowNegativeThreshold_ReturnsTrue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, -0.8f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.5f, isPositive: false);

        Assert.True(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_GamepadAxisBinding_NegativeAxis_AboveNegativeThreshold_ReturnsFalse()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, -0.3f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.5f, isPositive: false);

        Assert.False(binding.IsActive(context));
    }

    [Fact]
    public void IsActive_GamepadAxisBinding_WithSpecificIndex_ChecksOnlyThatGamepad()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.ConnectGamepad(1);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, 0.2f);
        context.GetGamepad(1).SetAxis(GamepadAxis.LeftStickX, 0.8f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.5f);

        Assert.False(binding.IsActive(context, gamepadIndex: 0));
        Assert.True(binding.IsActive(context, gamepadIndex: 1));
    }

    #endregion

    #region GetValue Tests - Key Bindings

    [Fact]
    public void GetValue_KeyBinding_WhenKeyPressed_ReturnsOne()
    {
        var context = new MockInputContext();
        context.Keyboard.PressKey(Key.W);
        var binding = InputBinding.FromKey(Key.W);

        Assert.Equal(1f, binding.GetValue(context));
    }

    [Fact]
    public void GetValue_KeyBinding_WhenKeyNotPressed_ReturnsZero()
    {
        var context = new MockInputContext();
        var binding = InputBinding.FromKey(Key.W);

        Assert.Equal(0f, binding.GetValue(context));
    }

    #endregion

    #region GetValue Tests - Mouse Bindings

    [Fact]
    public void GetValue_MouseButtonBinding_WhenButtonPressed_ReturnsOne()
    {
        var context = new MockInputContext();
        context.Mouse.PressButton(MouseButton.Left);
        var binding = InputBinding.FromMouseButton(MouseButton.Left);

        Assert.Equal(1f, binding.GetValue(context));
    }

    [Fact]
    public void GetValue_MouseButtonBinding_WhenButtonNotPressed_ReturnsZero()
    {
        var context = new MockInputContext();
        var binding = InputBinding.FromMouseButton(MouseButton.Left);

        Assert.Equal(0f, binding.GetValue(context));
    }

    #endregion

    #region GetValue Tests - Gamepad Button Bindings

    [Fact]
    public void GetValue_GamepadButtonBinding_WhenButtonPressed_ReturnsOne()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).PressButton(GamepadButton.South);
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.Equal(1f, binding.GetValue(context));
    }

    [Fact]
    public void GetValue_GamepadButtonBinding_WhenButtonNotPressed_ReturnsZero()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.Equal(0f, binding.GetValue(context));
    }

    #endregion

    #region GetValue Tests - Gamepad Axis Bindings

    [Fact]
    public void GetValue_GamepadAxisBinding_ReturnsAxisValue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, 0.75f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.Equal(0.75f, binding.GetValue(context), 0.001f);
    }

    [Fact]
    public void GetValue_GamepadAxisBinding_NoGamepadConnected_ReturnsZero()
    {
        var context = new MockInputContext();
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.Equal(0f, binding.GetValue(context));
    }

    [Fact]
    public void GetValue_GamepadAxisBinding_WithSpecificIndex_ReturnsValueFromThatGamepad()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.ConnectGamepad(1);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, 0.3f);
        context.GetGamepad(1).SetAxis(GamepadAxis.LeftStickX, 0.7f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.Equal(0.3f, binding.GetValue(context, gamepadIndex: 0), 0.001f);
        Assert.Equal(0.7f, binding.GetValue(context, gamepadIndex: 1), 0.001f);
    }

    [Fact]
    public void GetValue_GamepadAxisBinding_WithAnyIndex_ReturnsLargestAbsoluteValue()
    {
        var context = new MockInputContext();
        context.ConnectGamepad(0);
        context.ConnectGamepad(1);
        context.GetGamepad(0).SetAxis(GamepadAxis.LeftStickX, 0.3f);
        context.GetGamepad(1).SetAxis(GamepadAxis.LeftStickX, -0.8f);
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.Equal(-0.8f, binding.GetValue(context, gamepadIndex: -1), 0.001f);
    }

    [Fact]
    public void GetValue_GamepadAxisBinding_InvalidIndex_ReturnsZero()
    {
        var context = new MockInputContext();
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.Equal(0f, binding.GetValue(context, gamepadIndex: 10));
    }

    #endregion
}
