namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for gamepad event argument structs.
/// </summary>
public class GamepadEventArgsTests
{
    #region GamepadButtonEventArgs Tests

    [Fact]
    public void GamepadButtonEventArgs_Constructor_SetsGamepadIndex()
    {
        var args = new GamepadButtonEventArgs(2, GamepadButton.South);

        Assert.Equal(2, args.GamepadIndex);
    }

    [Fact]
    public void GamepadButtonEventArgs_Constructor_SetsButton()
    {
        var args = new GamepadButtonEventArgs(0, GamepadButton.Start);

        Assert.Equal(GamepadButton.Start, args.Button);
    }

    [Fact]
    public void GamepadButtonEventArgs_ToString_IncludesGamepadAndButton()
    {
        var args = new GamepadButtonEventArgs(1, GamepadButton.North);

        var result = args.ToString();

        Assert.Contains("1", result);
        Assert.Contains("North", result);
    }

    [Fact]
    public void GamepadButtonEventArgs_Equality_SameValues_ReturnsTrue()
    {
        var args1 = new GamepadButtonEventArgs(0, GamepadButton.East);
        var args2 = new GamepadButtonEventArgs(0, GamepadButton.East);

        Assert.Equal(args1, args2);
    }

    [Fact]
    public void GamepadButtonEventArgs_Equality_DifferentGamepadIndex_ReturnsFalse()
    {
        var args1 = new GamepadButtonEventArgs(0, GamepadButton.South);
        var args2 = new GamepadButtonEventArgs(1, GamepadButton.South);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void GamepadButtonEventArgs_Equality_DifferentButton_ReturnsFalse()
    {
        var args1 = new GamepadButtonEventArgs(0, GamepadButton.South);
        var args2 = new GamepadButtonEventArgs(0, GamepadButton.East);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void GamepadButtonEventArgs_GetHashCode_SameValues_ReturnsSameHash()
    {
        var args1 = new GamepadButtonEventArgs(2, GamepadButton.West);
        var args2 = new GamepadButtonEventArgs(2, GamepadButton.West);

        Assert.Equal(args1.GetHashCode(), args2.GetHashCode());
    }

    #endregion

    #region GamepadAxisEventArgs Tests

    [Fact]
    public void GamepadAxisEventArgs_Constructor_SetsGamepadIndex()
    {
        var args = new GamepadAxisEventArgs(3, GamepadAxis.LeftStickX, 0.5f, 0.0f);

        Assert.Equal(3, args.GamepadIndex);
    }

    [Fact]
    public void GamepadAxisEventArgs_Constructor_SetsAxis()
    {
        var args = new GamepadAxisEventArgs(0, GamepadAxis.RightTrigger, 0.75f, 0.5f);

        Assert.Equal(GamepadAxis.RightTrigger, args.Axis);
    }

    [Fact]
    public void GamepadAxisEventArgs_Constructor_SetsValue()
    {
        var args = new GamepadAxisEventArgs(0, GamepadAxis.LeftStickY, 0.8f, 0.0f);

        Assert.Equal(0.8f, args.Value);
    }

    [Fact]
    public void GamepadAxisEventArgs_Constructor_SetsPreviousValue()
    {
        var args = new GamepadAxisEventArgs(0, GamepadAxis.RightStickX, 0.5f, 0.3f);

        Assert.Equal(0.3f, args.PreviousValue);
    }

    [Fact]
    public void GamepadAxisEventArgs_Delta_CalculatesCorrectly()
    {
        var args = new GamepadAxisEventArgs(0, GamepadAxis.LeftTrigger, 0.8f, 0.3f);

        Assert.Equal(0.5f, args.Delta, 0.0001f);
    }

    [Fact]
    public void GamepadAxisEventArgs_Delta_NegativeChange_CalculatesCorrectly()
    {
        var args = new GamepadAxisEventArgs(0, GamepadAxis.RightStickY, -0.5f, 0.2f);

        Assert.Equal(-0.7f, args.Delta, 0.0001f);
    }

    [Fact]
    public void GamepadAxisEventArgs_Delta_NoChange_ReturnsZero()
    {
        var args = new GamepadAxisEventArgs(0, GamepadAxis.LeftStickX, 0.5f, 0.5f);

        Assert.Equal(0.0f, args.Delta, 0.0001f);
    }

    [Fact]
    public void GamepadAxisEventArgs_ToString_IncludesGamepadAxisAndValue()
    {
        var args = new GamepadAxisEventArgs(1, GamepadAxis.LeftStickX, 0.756f, 0.0f);

        var result = args.ToString();

        Assert.Contains("1", result);
        Assert.Contains("LeftStickX", result);
        Assert.Contains("0.75", result);
    }

    [Fact]
    public void GamepadAxisEventArgs_Equality_SameValues_ReturnsTrue()
    {
        var args1 = new GamepadAxisEventArgs(0, GamepadAxis.LeftTrigger, 0.5f, 0.2f);
        var args2 = new GamepadAxisEventArgs(0, GamepadAxis.LeftTrigger, 0.5f, 0.2f);

        Assert.Equal(args1, args2);
    }

    [Fact]
    public void GamepadAxisEventArgs_Equality_DifferentGamepadIndex_ReturnsFalse()
    {
        var args1 = new GamepadAxisEventArgs(0, GamepadAxis.LeftStickX, 0.5f, 0.0f);
        var args2 = new GamepadAxisEventArgs(1, GamepadAxis.LeftStickX, 0.5f, 0.0f);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void GamepadAxisEventArgs_Equality_DifferentAxis_ReturnsFalse()
    {
        var args1 = new GamepadAxisEventArgs(0, GamepadAxis.LeftStickX, 0.5f, 0.0f);
        var args2 = new GamepadAxisEventArgs(0, GamepadAxis.RightStickX, 0.5f, 0.0f);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void GamepadAxisEventArgs_Equality_DifferentValue_ReturnsFalse()
    {
        var args1 = new GamepadAxisEventArgs(0, GamepadAxis.LeftStickX, 0.5f, 0.0f);
        var args2 = new GamepadAxisEventArgs(0, GamepadAxis.LeftStickX, 0.6f, 0.0f);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void GamepadAxisEventArgs_GetHashCode_SameValues_ReturnsSameHash()
    {
        var args1 = new GamepadAxisEventArgs(2, GamepadAxis.RightTrigger, 0.8f, 0.5f);
        var args2 = new GamepadAxisEventArgs(2, GamepadAxis.RightTrigger, 0.8f, 0.5f);

        Assert.Equal(args1.GetHashCode(), args2.GetHashCode());
    }

    #endregion
}
