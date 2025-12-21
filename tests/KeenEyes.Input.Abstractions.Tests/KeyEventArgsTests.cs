using System.Numerics;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="KeyEventArgs"/> struct.
/// </summary>
public class KeyEventArgsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsKey()
    {
        var args = new KeyEventArgs(Key.Space, KeyModifiers.None, false);

        Assert.Equal(Key.Space, args.Key);
    }

    [Fact]
    public void Constructor_SetsModifiers()
    {
        var args = new KeyEventArgs(Key.A, KeyModifiers.Shift, false);

        Assert.Equal(KeyModifiers.Shift, args.Modifiers);
    }

    [Fact]
    public void Constructor_SetsIsRepeat()
    {
        var args = new KeyEventArgs(Key.W, KeyModifiers.None, true);

        Assert.True(args.IsRepeat);
    }

    #endregion

    #region Modifier Property Tests

    [Fact]
    public void IsShiftDown_WithShift_ReturnsTrue()
    {
        var args = new KeyEventArgs(Key.A, KeyModifiers.Shift, false);

        Assert.True(args.IsShiftDown);
    }

    [Fact]
    public void IsShiftDown_WithoutShift_ReturnsFalse()
    {
        var args = new KeyEventArgs(Key.A, KeyModifiers.Control, false);

        Assert.False(args.IsShiftDown);
    }

    [Fact]
    public void IsControlDown_WithControl_ReturnsTrue()
    {
        var args = new KeyEventArgs(Key.C, KeyModifiers.Control, false);

        Assert.True(args.IsControlDown);
    }

    [Fact]
    public void IsControlDown_WithoutControl_ReturnsFalse()
    {
        var args = new KeyEventArgs(Key.C, KeyModifiers.Shift, false);

        Assert.False(args.IsControlDown);
    }

    [Fact]
    public void IsAltDown_WithAlt_ReturnsTrue()
    {
        var args = new KeyEventArgs(Key.F4, KeyModifiers.Alt, false);

        Assert.True(args.IsAltDown);
    }

    [Fact]
    public void IsAltDown_WithoutAlt_ReturnsFalse()
    {
        var args = new KeyEventArgs(Key.F4, KeyModifiers.None, false);

        Assert.False(args.IsAltDown);
    }

    [Fact]
    public void IsSuperDown_WithSuper_ReturnsTrue()
    {
        var args = new KeyEventArgs(Key.L, KeyModifiers.Super, false);

        Assert.True(args.IsSuperDown);
    }

    [Fact]
    public void IsSuperDown_WithoutSuper_ReturnsFalse()
    {
        var args = new KeyEventArgs(Key.L, KeyModifiers.None, false);

        Assert.False(args.IsSuperDown);
    }

    [Fact]
    public void ModifierProperties_WithCombinedModifiers_WorkCorrectly()
    {
        var args = new KeyEventArgs(Key.V, KeyModifiers.Control | KeyModifiers.Shift, false);

        Assert.True(args.IsControlDown);
        Assert.True(args.IsShiftDown);
        Assert.False(args.IsAltDown);
        Assert.False(args.IsSuperDown);
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Create_SetsKeyAndModifiers()
    {
        var args = KeyEventArgs.Create(Key.Enter, KeyModifiers.None);

        Assert.Equal(Key.Enter, args.Key);
        Assert.Equal(KeyModifiers.None, args.Modifiers);
    }

    [Fact]
    public void Create_SetsIsRepeatToFalse()
    {
        var args = KeyEventArgs.Create(Key.Space, KeyModifiers.None);

        Assert.False(args.IsRepeat);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithoutRepeat_IncludesKeyAndModifiers()
    {
        var args = new KeyEventArgs(Key.A, KeyModifiers.Shift, false);

        var result = args.ToString();

        Assert.Contains("A", result);
        Assert.Contains("Shift", result);
        Assert.DoesNotContain("Repeat", result);
    }

    [Fact]
    public void ToString_WithRepeat_IncludesRepeatIndicator()
    {
        var args = new KeyEventArgs(Key.W, KeyModifiers.None, true);

        var result = args.ToString();

        Assert.Contains("W", result);
        Assert.Contains("Repeat", result);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var args1 = new KeyEventArgs(Key.Space, KeyModifiers.None, false);
        var args2 = new KeyEventArgs(Key.Space, KeyModifiers.None, false);

        Assert.Equal(args1, args2);
    }

    [Fact]
    public void Equals_DifferentKey_ReturnsFalse()
    {
        var args1 = new KeyEventArgs(Key.Space, KeyModifiers.None, false);
        var args2 = new KeyEventArgs(Key.Enter, KeyModifiers.None, false);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void Equals_DifferentModifiers_ReturnsFalse()
    {
        var args1 = new KeyEventArgs(Key.A, KeyModifiers.Shift, false);
        var args2 = new KeyEventArgs(Key.A, KeyModifiers.Control, false);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void Equals_DifferentIsRepeat_ReturnsFalse()
    {
        var args1 = new KeyEventArgs(Key.W, KeyModifiers.None, false);
        var args2 = new KeyEventArgs(Key.W, KeyModifiers.None, true);

        Assert.NotEqual(args1, args2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var args1 = new KeyEventArgs(Key.Space, KeyModifiers.Shift, false);
        var args2 = new KeyEventArgs(Key.Space, KeyModifiers.Shift, false);

        Assert.Equal(args1.GetHashCode(), args2.GetHashCode());
    }

    #endregion
}
