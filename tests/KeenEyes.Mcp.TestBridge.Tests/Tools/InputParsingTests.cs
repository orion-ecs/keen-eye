using System.Reflection;
using KeenEyes.Input.Abstractions;
using KeenEyes.Mcp.TestBridge.Tools;

namespace KeenEyes.Mcp.TestBridge.Tests.Tools;

/// <summary>
/// Tests for InputTools parsing methods.
/// These test the static parsing helpers without requiring a live connection.
/// </summary>
public sealed class InputParsingTests
{
    private static readonly MethodInfo ParseKeyMethod = typeof(InputTools)
        .GetMethod("ParseKey", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ParseModifiersMethod = typeof(InputTools)
        .GetMethod("ParseModifiers", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ParseMouseButtonMethod = typeof(InputTools)
        .GetMethod("ParseMouseButton", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ParseGamepadButtonMethod = typeof(InputTools)
        .GetMethod("ParseGamepadButton", BindingFlags.NonPublic | BindingFlags.Static)!;

    #region ParseKey Tests

    [Theory]
    [InlineData("Space", Key.Space)]
    [InlineData("space", Key.Space)]
    [InlineData("SPACE", Key.Space)]
    [InlineData("Enter", Key.Enter)]
    [InlineData("Escape", Key.Escape)]
    [InlineData("W", Key.W)]
    [InlineData("A", Key.A)]
    [InlineData("S", Key.S)]
    [InlineData("D", Key.D)]
    [InlineData("Up", Key.Up)]
    [InlineData("Down", Key.Down)]
    [InlineData("Left", Key.Left)]
    [InlineData("Right", Key.Right)]
    [InlineData("F1", Key.F1)]
    [InlineData("F12", Key.F12)]
    public void ParseKey_ValidKeys_ReturnsCorrectEnum(string input, Key expected)
    {
        var result = (Key)ParseKeyMethod.Invoke(null, [input])!;
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("InvalidKey")]
    [InlineData("")]
    [InlineData("NotAKey")]
    public void ParseKey_InvalidKeys_ThrowsArgumentException(string input)
    {
        var exception = Should.Throw<TargetInvocationException>(() => ParseKeyMethod.Invoke(null, [input]));
        exception.InnerException.ShouldBeOfType<ArgumentException>();
    }

    #endregion

    #region ParseModifiers Tests

    [Fact]
    public void ParseModifiers_Null_ReturnsNone()
    {
        var result = (KeyModifiers)ParseModifiersMethod.Invoke(null, [null])!;
        result.ShouldBe(KeyModifiers.None);
    }

    [Fact]
    public void ParseModifiers_Empty_ReturnsNone()
    {
        var result = (KeyModifiers)ParseModifiersMethod.Invoke(null, [""])!;
        result.ShouldBe(KeyModifiers.None);
    }

    [Fact]
    public void ParseModifiers_Shift_ReturnsShift()
    {
        var result = (KeyModifiers)ParseModifiersMethod.Invoke(null, ["Shift"])!;
        result.ShouldBe(KeyModifiers.Shift);
    }

    [Theory]
    [InlineData("Ctrl", KeyModifiers.Control)]
    [InlineData("Control", KeyModifiers.Control)]
    public void ParseModifiers_Control_ReturnsControl(string input, KeyModifiers expected)
    {
        var result = (KeyModifiers)ParseModifiersMethod.Invoke(null, [input])!;
        result.ShouldBe(expected);
    }

    [Fact]
    public void ParseModifiers_Alt_ReturnsAlt()
    {
        var result = (KeyModifiers)ParseModifiersMethod.Invoke(null, ["Alt"])!;
        result.ShouldBe(KeyModifiers.Alt);
    }

    [Theory]
    [InlineData("Super")]
    [InlineData("Win")]
    [InlineData("Cmd")]
    [InlineData("Meta")]
    public void ParseModifiers_Super_ReturnsSuper(string input)
    {
        var result = (KeyModifiers)ParseModifiersMethod.Invoke(null, [input])!;
        result.ShouldBe(KeyModifiers.Super);
    }

    [Fact]
    public void ParseModifiers_Multiple_ReturnsCombined()
    {
        var result = (KeyModifiers)ParseModifiersMethod.Invoke(null, ["Shift,Ctrl"])!;
        result.ShouldBe(KeyModifiers.Shift | KeyModifiers.Control);
    }

    [Fact]
    public void ParseModifiers_Invalid_ThrowsArgumentException()
    {
        var exception = Should.Throw<TargetInvocationException>(() =>
            ParseModifiersMethod.Invoke(null, ["InvalidMod"]));
        exception.InnerException.ShouldBeOfType<ArgumentException>();
    }

    #endregion

    #region ParseMouseButton Tests

    [Theory]
    [InlineData(null, MouseButton.Left)]
    [InlineData("", MouseButton.Left)]
    [InlineData("Left", MouseButton.Left)]
    [InlineData("left", MouseButton.Left)]
    [InlineData("Right", MouseButton.Right)]
    [InlineData("Middle", MouseButton.Middle)]
    [InlineData("Button4", MouseButton.Button4)]
    [InlineData("Button5", MouseButton.Button5)]
    public void ParseMouseButton_ValidButtons_ReturnsCorrectEnum(string? input, MouseButton expected)
    {
        var result = (MouseButton)ParseMouseButtonMethod.Invoke(null, [input])!;
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("InvalidButton")]
    [InlineData("Button9")]  // Only Button4-Button8 exist
    public void ParseMouseButton_InvalidButtons_ThrowsArgumentException(string input)
    {
        var exception = Should.Throw<TargetInvocationException>(() =>
            ParseMouseButtonMethod.Invoke(null, [input]));
        exception.InnerException.ShouldBeOfType<ArgumentException>();
    }

    #endregion

    #region ParseGamepadButton Tests

    [Theory]
    [InlineData("South", GamepadButton.South)]
    [InlineData("south", GamepadButton.South)]
    [InlineData("East", GamepadButton.East)]
    [InlineData("West", GamepadButton.West)]
    [InlineData("North", GamepadButton.North)]
    [InlineData("LeftShoulder", GamepadButton.LeftShoulder)]
    [InlineData("RightShoulder", GamepadButton.RightShoulder)]
    [InlineData("Back", GamepadButton.Back)]
    [InlineData("Start", GamepadButton.Start)]
    [InlineData("Guide", GamepadButton.Guide)]
    [InlineData("LeftStick", GamepadButton.LeftStick)]
    [InlineData("RightStick", GamepadButton.RightStick)]
    [InlineData("DPadUp", GamepadButton.DPadUp)]
    [InlineData("DPadDown", GamepadButton.DPadDown)]
    [InlineData("DPadLeft", GamepadButton.DPadLeft)]
    [InlineData("DPadRight", GamepadButton.DPadRight)]
    public void ParseGamepadButton_ValidButtons_ReturnsCorrectEnum(string input, GamepadButton expected)
    {
        var result = (GamepadButton)ParseGamepadButtonMethod.Invoke(null, [input])!;
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("InvalidButton")]
    [InlineData("")]
    [InlineData("A")]  // Note: Xbox naming is documented but not actually supported - enum uses positional names
    [InlineData("B")]
    [InlineData("X")]
    [InlineData("Y")]
    public void ParseGamepadButton_InvalidButtons_ThrowsArgumentException(string input)
    {
        var exception = Should.Throw<TargetInvocationException>(() =>
            ParseGamepadButtonMethod.Invoke(null, [input]));
        exception.InnerException.ShouldBeOfType<ArgumentException>();
    }

    #endregion
}
