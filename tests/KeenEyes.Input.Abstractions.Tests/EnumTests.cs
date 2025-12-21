namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for input enumeration types to ensure all enum values are valid.
/// </summary>
public class EnumTests
{
    #region Key Enum Tests

    [Theory]
    [InlineData(Key.Unknown, 0)]
    [InlineData(Key.A, 1)]
    [InlineData(Key.Z, 26)]
    [InlineData(Key.Number0, 27)]
    [InlineData(Key.Number9, 36)]
    [InlineData(Key.F1, 37)]
    [InlineData(Key.F12, 48)]
    [InlineData(Key.Space, 83)]
    [InlineData(Key.Enter, 84)]
    [InlineData(Key.Escape, 85)]
    public void Key_HasCorrectValue(Key key, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)key);
    }

    [Fact]
    public void Key_AllValues_AreUnique()
    {
        var values = Enum.GetValues<Key>();
        var distinctValues = values.Distinct().ToArray();

        Assert.Equal(values.Length, distinctValues.Length);
    }

    [Fact]
    public void Key_CanConvertToString()
    {
        Assert.Equal("Space", Key.Space.ToString());
        Assert.Equal("Enter", Key.Enter.ToString());
        Assert.Equal("Unknown", Key.Unknown.ToString());
    }

    #endregion

    #region GamepadButton Enum Tests

    [Theory]
    [InlineData(GamepadButton.Unknown, 0)]
    [InlineData(GamepadButton.South, 1)]
    [InlineData(GamepadButton.East, 2)]
    [InlineData(GamepadButton.West, 3)]
    [InlineData(GamepadButton.North, 4)]
    [InlineData(GamepadButton.LeftShoulder, 5)]
    [InlineData(GamepadButton.RightShoulder, 6)]
    [InlineData(GamepadButton.Start, 15)]
    [InlineData(GamepadButton.Guide, 17)]
    public void GamepadButton_HasCorrectValue(GamepadButton button, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)button);
    }

    [Fact]
    public void GamepadButton_AllValues_AreUnique()
    {
        var values = Enum.GetValues<GamepadButton>();
        var distinctValues = values.Distinct().ToArray();

        Assert.Equal(values.Length, distinctValues.Length);
    }

    [Fact]
    public void GamepadButton_CanConvertToString()
    {
        Assert.Equal("South", GamepadButton.South.ToString());
        Assert.Equal("Start", GamepadButton.Start.ToString());
        Assert.Equal("Unknown", GamepadButton.Unknown.ToString());
    }

    #endregion

    #region GamepadAxis Enum Tests

    [Theory]
    [InlineData(GamepadAxis.Unknown, 0)]
    [InlineData(GamepadAxis.LeftStickX, 1)]
    [InlineData(GamepadAxis.LeftStickY, 2)]
    [InlineData(GamepadAxis.RightStickX, 3)]
    [InlineData(GamepadAxis.RightStickY, 4)]
    [InlineData(GamepadAxis.LeftTrigger, 5)]
    [InlineData(GamepadAxis.RightTrigger, 6)]
    public void GamepadAxis_HasCorrectValue(GamepadAxis axis, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)axis);
    }

    [Fact]
    public void GamepadAxis_AllValues_AreUnique()
    {
        var values = Enum.GetValues<GamepadAxis>();
        var distinctValues = values.Distinct().ToArray();

        Assert.Equal(values.Length, distinctValues.Length);
    }

    [Fact]
    public void GamepadAxis_CanConvertToString()
    {
        Assert.Equal("LeftStickX", GamepadAxis.LeftStickX.ToString());
        Assert.Equal("RightTrigger", GamepadAxis.RightTrigger.ToString());
        Assert.Equal("Unknown", GamepadAxis.Unknown.ToString());
    }

    #endregion

    #region MouseButton Enum Tests

    [Theory]
    [InlineData(MouseButton.Unknown, 0)]
    [InlineData(MouseButton.Left, 1)]
    [InlineData(MouseButton.Right, 2)]
    [InlineData(MouseButton.Middle, 3)]
    [InlineData(MouseButton.Button4, 4)]
    [InlineData(MouseButton.Button5, 5)]
    [InlineData(MouseButton.Button6, 6)]
    [InlineData(MouseButton.Button7, 7)]
    [InlineData(MouseButton.Button8, 8)]
    public void MouseButton_HasCorrectValue(MouseButton button, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)button);
    }

    [Fact]
    public void MouseButton_AllValues_AreUnique()
    {
        var values = Enum.GetValues<MouseButton>();
        var distinctValues = values.Distinct().ToArray();

        Assert.Equal(values.Length, distinctValues.Length);
    }

    [Fact]
    public void MouseButton_CanConvertToString()
    {
        Assert.Equal("Left", MouseButton.Left.ToString());
        Assert.Equal("Right", MouseButton.Right.ToString());
        Assert.Equal("Unknown", MouseButton.Unknown.ToString());
    }

    #endregion

    #region KeyModifiers Enum Tests

    [Theory]
    [InlineData(KeyModifiers.None, 0)]
    [InlineData(KeyModifiers.Shift, 1)]
    [InlineData(KeyModifiers.Control, 2)]
    [InlineData(KeyModifiers.Alt, 4)]
    [InlineData(KeyModifiers.Super, 8)]
    [InlineData(KeyModifiers.CapsLock, 16)]
    [InlineData(KeyModifiers.NumLock, 32)]
    public void KeyModifiers_HasCorrectValue(KeyModifiers modifier, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)modifier);
    }

    [Fact]
    public void KeyModifiers_CanCombineFlags()
    {
        var combined = KeyModifiers.Shift | KeyModifiers.Control;

        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Control, combined);
        Assert.True((combined & KeyModifiers.Shift) != 0);
        Assert.True((combined & KeyModifiers.Control) != 0);
        Assert.False((combined & KeyModifiers.Alt) != 0);
    }

    [Fact]
    public void KeyModifiers_HasFlagsAttribute()
    {
        var type = typeof(KeyModifiers);
        var hasFlagsAttribute = type.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;

        Assert.True(hasFlagsAttribute);
    }

    #endregion

    #region InputBindingType Enum Tests

    [Theory]
    [InlineData(InputBindingType.Key, 0)]
    [InlineData(InputBindingType.MouseButton, 1)]
    [InlineData(InputBindingType.GamepadButton, 2)]
    [InlineData(InputBindingType.GamepadAxis, 3)]
    public void InputBindingType_HasCorrectValue(InputBindingType type, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)type);
    }

    [Fact]
    public void InputBindingType_AllValues_AreUnique()
    {
        var values = Enum.GetValues<InputBindingType>();
        var distinctValues = values.Distinct().ToArray();

        Assert.Equal(values.Length, distinctValues.Length);
    }

    [Fact]
    public void InputBindingType_CanConvertToString()
    {
        Assert.Equal("Key", InputBindingType.Key.ToString());
        Assert.Equal("MouseButton", InputBindingType.MouseButton.ToString());
        Assert.Equal("GamepadButton", InputBindingType.GamepadButton.ToString());
        Assert.Equal("GamepadAxis", InputBindingType.GamepadAxis.ToString());
    }

    #endregion

    #region MouseButtons Enum Tests

    [Theory]
    [InlineData(MouseButtons.None, 0)]
    [InlineData(MouseButtons.Left, 1)]
    [InlineData(MouseButtons.Right, 2)]
    [InlineData(MouseButtons.Middle, 4)]
    [InlineData(MouseButtons.Button4, 8)]
    [InlineData(MouseButtons.Button5, 16)]
    public void MouseButtons_HasCorrectValue(MouseButtons buttons, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)buttons);
    }

    [Fact]
    public void MouseButtons_CanCombineFlags()
    {
        var combined = MouseButtons.Left | MouseButtons.Right;

        Assert.Equal(MouseButtons.Left | MouseButtons.Right, combined);
        Assert.True((combined & MouseButtons.Left) != 0);
        Assert.True((combined & MouseButtons.Right) != 0);
        Assert.False((combined & MouseButtons.Middle) != 0);
    }

    [Fact]
    public void MouseButtons_HasFlagsAttribute()
    {
        var type = typeof(MouseButtons);
        var hasFlagsAttribute = type.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;

        Assert.True(hasFlagsAttribute);
    }

    #endregion

    #region GamepadButtons Enum Tests

    [Theory]
    [InlineData(GamepadButtons.None, 0u)]
    [InlineData(GamepadButtons.South, 1u)]
    [InlineData(GamepadButtons.East, 2u)]
    [InlineData(GamepadButtons.West, 4u)]
    [InlineData(GamepadButtons.North, 8u)]
    [InlineData(GamepadButtons.LeftShoulder, 16u)]
    [InlineData(GamepadButtons.RightShoulder, 32u)]
    [InlineData(GamepadButtons.Start, 16384u)]
    [InlineData(GamepadButtons.Guide, 65536u)]
    public void GamepadButtons_HasCorrectValue(GamepadButtons buttons, uint expectedValue)
    {
        Assert.Equal(expectedValue, (uint)buttons);
    }

    [Fact]
    public void GamepadButtons_CanCombineFlags()
    {
        var combined = GamepadButtons.South | GamepadButtons.East;

        Assert.Equal(GamepadButtons.South | GamepadButtons.East, combined);
        Assert.True((combined & GamepadButtons.South) != 0);
        Assert.True((combined & GamepadButtons.East) != 0);
        Assert.False((combined & GamepadButtons.West) != 0);
    }

    [Fact]
    public void GamepadButtons_HasFlagsAttribute()
    {
        var type = typeof(GamepadButtons);
        var hasFlagsAttribute = type.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;

        Assert.True(hasFlagsAttribute);
    }

    #endregion
}
