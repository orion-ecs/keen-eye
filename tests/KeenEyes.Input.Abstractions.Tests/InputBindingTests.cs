namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="InputBinding"/> struct.
/// </summary>
public class InputBindingTests
{
    #region Factory Method Tests

    [Fact]
    public void FromKey_SetsCorrectType()
    {
        var binding = InputBinding.FromKey(Key.Space);

        Assert.Equal(InputBindingType.Key, binding.Type);
    }

    [Fact]
    public void FromKey_SetsKeyValue()
    {
        var binding = InputBinding.FromKey(Key.W);

        Assert.Equal(Key.W, binding.Key);
    }

    [Fact]
    public void FromMouseButton_SetsCorrectType()
    {
        var binding = InputBinding.FromMouseButton(MouseButton.Left);

        Assert.Equal(InputBindingType.MouseButton, binding.Type);
    }

    [Fact]
    public void FromMouseButton_SetsButtonValue()
    {
        var binding = InputBinding.FromMouseButton(MouseButton.Right);

        Assert.Equal(MouseButton.Right, binding.MouseButton);
    }

    [Fact]
    public void FromGamepadButton_SetsCorrectType()
    {
        var binding = InputBinding.FromGamepadButton(GamepadButton.South);

        Assert.Equal(InputBindingType.GamepadButton, binding.Type);
    }

    [Fact]
    public void FromGamepadButton_SetsButtonValue()
    {
        var binding = InputBinding.FromGamepadButton(GamepadButton.North);

        Assert.Equal(GamepadButton.North, binding.GamepadButton);
    }

    [Fact]
    public void FromGamepadAxis_SetsCorrectType()
    {
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.Equal(InputBindingType.GamepadAxis, binding.Type);
    }

    [Fact]
    public void FromGamepadAxis_SetsAxisValue()
    {
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.RightTrigger);

        Assert.Equal(GamepadAxis.RightTrigger, binding.GamepadAxis);
    }

    [Fact]
    public void FromGamepadAxis_DefaultThreshold_Is05()
    {
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.Equal(0.5f, binding.AxisThreshold);
    }

    [Fact]
    public void FromGamepadAxis_CustomThreshold_IsSet()
    {
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.3f);

        Assert.Equal(0.3f, binding.AxisThreshold);
    }

    [Fact]
    public void FromGamepadAxis_DefaultIsPositive_IsTrue()
    {
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX);

        Assert.True(binding.IsPositiveAxis);
    }

    [Fact]
    public void FromGamepadAxis_IsPositiveFalse_IsSet()
    {
        var binding = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.5f, isPositive: false);

        Assert.False(binding.IsPositiveAxis);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameKeyBindings_ReturnsTrue()
    {
        var binding1 = InputBinding.FromKey(Key.Space);
        var binding2 = InputBinding.FromKey(Key.Space);

        Assert.Equal(binding1, binding2);
    }

    [Fact]
    public void Equals_DifferentKeyBindings_ReturnsFalse()
    {
        var binding1 = InputBinding.FromKey(Key.Space);
        var binding2 = InputBinding.FromKey(Key.Enter);

        Assert.NotEqual(binding1, binding2);
    }

    [Fact]
    public void Equals_DifferentTypes_ReturnsFalse()
    {
        var keyBinding = InputBinding.FromKey(Key.Space);
        var buttonBinding = InputBinding.FromMouseButton(MouseButton.Left);

        Assert.NotEqual(keyBinding, buttonBinding);
    }

    [Fact]
    public void Equals_SameAxisBindings_WithDifferentThresholds_ReturnsFalse()
    {
        var binding1 = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.3f);
        var binding2 = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, 0.5f);

        Assert.NotEqual(binding1, binding2);
    }

    [Fact]
    public void GetHashCode_SameBindings_ReturnsSameHash()
    {
        var binding1 = InputBinding.FromKey(Key.W);
        var binding2 = InputBinding.FromKey(Key.W);

        Assert.Equal(binding1.GetHashCode(), binding2.GetHashCode());
    }

    #endregion
}
