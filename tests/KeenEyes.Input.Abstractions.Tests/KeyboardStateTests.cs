using System.Collections.Immutable;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="KeyboardState"/> struct.
/// </summary>
public class KeyboardStateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsPressedKeys()
    {
        var keys = ImmutableHashSet.Create(Key.W, Key.A, Key.S, Key.D);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.Equal(keys, state.PressedKeys);
    }

    [Fact]
    public void Constructor_SetsModifiers()
    {
        var keys = ImmutableHashSet<Key>.Empty;
        var state = new KeyboardState(keys, KeyModifiers.Shift | KeyModifiers.Control);

        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Control, state.Modifiers);
    }

    #endregion

    #region Empty State Tests

    [Fact]
    public void Empty_HasNoPressedKeys()
    {
        Assert.Empty(KeyboardState.Empty.PressedKeys);
    }

    [Fact]
    public void Empty_HasNoModifiers()
    {
        Assert.Equal(KeyModifiers.None, KeyboardState.Empty.Modifiers);
    }

    [Fact]
    public void Empty_PressedKeyCount_IsZero()
    {
        Assert.Equal(0, KeyboardState.Empty.PressedKeyCount);
    }

    #endregion

    #region IsKeyDown/IsKeyUp Tests

    [Fact]
    public void IsKeyDown_WithPressedKey_ReturnsTrue()
    {
        var keys = ImmutableHashSet.Create(Key.Space);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.True(state.IsKeyDown(Key.Space));
    }

    [Fact]
    public void IsKeyDown_WithUnpressedKey_ReturnsFalse()
    {
        var keys = ImmutableHashSet.Create(Key.W);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.False(state.IsKeyDown(Key.S));
    }

    [Fact]
    public void IsKeyUp_WithPressedKey_ReturnsFalse()
    {
        var keys = ImmutableHashSet.Create(Key.Enter);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.False(state.IsKeyUp(Key.Enter));
    }

    [Fact]
    public void IsKeyUp_WithUnpressedKey_ReturnsTrue()
    {
        var keys = ImmutableHashSet.Create(Key.A);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.True(state.IsKeyUp(Key.B));
    }

    [Fact]
    public void IsKeyDown_WithMultiplePressedKeys_WorksForAll()
    {
        var keys = ImmutableHashSet.Create(Key.W, Key.A, Key.S, Key.D);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.True(state.IsKeyDown(Key.W));
        Assert.True(state.IsKeyDown(Key.A));
        Assert.True(state.IsKeyDown(Key.S));
        Assert.True(state.IsKeyDown(Key.D));
        Assert.False(state.IsKeyDown(Key.Space));
    }

    #endregion

    #region Modifier Property Tests

    [Fact]
    public void IsShiftDown_WithShift_ReturnsTrue()
    {
        var state = new KeyboardState([], KeyModifiers.Shift);

        Assert.True(state.IsShiftDown);
    }

    [Fact]
    public void IsShiftDown_WithoutShift_ReturnsFalse()
    {
        var state = new KeyboardState([], KeyModifiers.Control);

        Assert.False(state.IsShiftDown);
    }

    [Fact]
    public void IsControlDown_WithControl_ReturnsTrue()
    {
        var state = new KeyboardState([], KeyModifiers.Control);

        Assert.True(state.IsControlDown);
    }

    [Fact]
    public void IsControlDown_WithoutControl_ReturnsFalse()
    {
        var state = new KeyboardState([], KeyModifiers.Shift);

        Assert.False(state.IsControlDown);
    }

    [Fact]
    public void IsAltDown_WithAlt_ReturnsTrue()
    {
        var state = new KeyboardState([], KeyModifiers.Alt);

        Assert.True(state.IsAltDown);
    }

    [Fact]
    public void IsAltDown_WithoutAlt_ReturnsFalse()
    {
        var state = new KeyboardState([], KeyModifiers.None);

        Assert.False(state.IsAltDown);
    }

    [Fact]
    public void IsSuperDown_WithSuper_ReturnsTrue()
    {
        var state = new KeyboardState([], KeyModifiers.Super);

        Assert.True(state.IsSuperDown);
    }

    [Fact]
    public void IsSuperDown_WithoutSuper_ReturnsFalse()
    {
        var state = new KeyboardState([], KeyModifiers.None);

        Assert.False(state.IsSuperDown);
    }

    [Fact]
    public void IsCapsLockOn_WithCapsLock_ReturnsTrue()
    {
        var state = new KeyboardState([], KeyModifiers.CapsLock);

        Assert.True(state.IsCapsLockOn);
    }

    [Fact]
    public void IsCapsLockOn_WithoutCapsLock_ReturnsFalse()
    {
        var state = new KeyboardState([], KeyModifiers.None);

        Assert.False(state.IsCapsLockOn);
    }

    [Fact]
    public void IsNumLockOn_WithNumLock_ReturnsTrue()
    {
        var state = new KeyboardState([], KeyModifiers.NumLock);

        Assert.True(state.IsNumLockOn);
    }

    [Fact]
    public void IsNumLockOn_WithoutNumLock_ReturnsFalse()
    {
        var state = new KeyboardState([], KeyModifiers.None);

        Assert.False(state.IsNumLockOn);
    }

    [Fact]
    public void ModifierProperties_WithCombinedModifiers_WorkCorrectly()
    {
        var state = new KeyboardState([], KeyModifiers.Shift | KeyModifiers.Control | KeyModifiers.CapsLock);

        Assert.True(state.IsShiftDown);
        Assert.True(state.IsControlDown);
        Assert.True(state.IsCapsLockOn);
        Assert.False(state.IsAltDown);
        Assert.False(state.IsSuperDown);
        Assert.False(state.IsNumLockOn);
    }

    #endregion

    #region PressedKeyCount Tests

    [Fact]
    public void PressedKeyCount_WithNoPressedKeys_ReturnsZero()
    {
        var state = new KeyboardState([], KeyModifiers.None);

        Assert.Equal(0, state.PressedKeyCount);
    }

    [Fact]
    public void PressedKeyCount_WithOnePressedKey_ReturnsOne()
    {
        var keys = ImmutableHashSet.Create(Key.Space);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.Equal(1, state.PressedKeyCount);
    }

    [Fact]
    public void PressedKeyCount_WithMultiplePressedKeys_ReturnsCorrectCount()
    {
        var keys = ImmutableHashSet.Create(Key.W, Key.A, Key.S, Key.D, Key.Space);
        var state = new KeyboardState(keys, KeyModifiers.None);

        Assert.Equal(5, state.PressedKeyCount);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithNoPressedKeys_ReturnsEmptyIndicator()
    {
        var state = new KeyboardState([], KeyModifiers.None);

        var result = state.ToString();

        Assert.Contains("Empty", result);
    }

    [Fact]
    public void ToString_WithPressedKeys_IncludesCountAndModifiers()
    {
        var keys = ImmutableHashSet.Create(Key.W, Key.A);
        var state = new KeyboardState(keys, KeyModifiers.Shift);

        var result = state.ToString();

        Assert.Contains("2", result);
        Assert.Contains("Shift", result);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameKeysAndModifiers_ReturnsTrue()
    {
        var keys = ImmutableHashSet.Create(Key.W, Key.A);
        var state1 = new KeyboardState(keys, KeyModifiers.Shift);
        var state2 = new KeyboardState(keys, KeyModifiers.Shift);

        Assert.Equal(state1, state2);
    }

    [Fact]
    public void Equals_DifferentKeys_ReturnsFalse()
    {
        var keys1 = ImmutableHashSet.Create(Key.W);
        var keys2 = ImmutableHashSet.Create(Key.S);
        var state1 = new KeyboardState(keys1, KeyModifiers.None);
        var state2 = new KeyboardState(keys2, KeyModifiers.None);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void Equals_DifferentModifiers_ReturnsFalse()
    {
        var keys = ImmutableHashSet.Create(Key.A);
        var state1 = new KeyboardState(keys, KeyModifiers.Shift);
        var state2 = new KeyboardState(keys, KeyModifiers.Control);

        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var keys = ImmutableHashSet.Create(Key.Space);
        var state1 = new KeyboardState(keys, KeyModifiers.None);
        var state2 = new KeyboardState(keys, KeyModifiers.None);

        Assert.Equal(state1.GetHashCode(), state2.GetHashCode());
    }

    #endregion
}
