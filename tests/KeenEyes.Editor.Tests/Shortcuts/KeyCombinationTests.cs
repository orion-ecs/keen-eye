using KeenEyes.Editor.Shortcuts;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Tests.Shortcuts;

public class KeyCombinationTests
{
    #region Parse Tests

    [Theory]
    [InlineData("Ctrl+S", Key.S, KeyModifiers.Control)]
    [InlineData("Ctrl+Shift+S", Key.S, KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("Alt+F4", Key.F4, KeyModifiers.Alt)]
    [InlineData("Ctrl+Shift+Alt+N", Key.N, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt)]
    [InlineData("F1", Key.F1, KeyModifiers.None)]
    [InlineData("Del", Key.Delete, KeyModifiers.None)]
    [InlineData("Delete", Key.Delete, KeyModifiers.None)]
    [InlineData("Escape", Key.Escape, KeyModifiers.None)]
    [InlineData("Esc", Key.Escape, KeyModifiers.None)]
    [InlineData("Enter", Key.Enter, KeyModifiers.None)]
    [InlineData("Space", Key.Space, KeyModifiers.None)]
    public void Parse_ValidShortcut_ReturnsCorrectCombination(string input, Key expectedKey, KeyModifiers expectedModifiers)
    {
        var combination = KeyCombination.Parse(input);

        Assert.Equal(expectedKey, combination.Key);
        Assert.Equal(expectedModifiers, combination.Modifiers);
    }

    [Theory]
    [InlineData("CTRL+S")]
    [InlineData("ctrl+s")]
    [InlineData("Ctrl+s")]
    [InlineData("CTRL+s")]
    public void Parse_CaseInsensitive(string input)
    {
        var combination = KeyCombination.Parse(input);

        Assert.Equal(Key.S, combination.Key);
        Assert.Equal(KeyModifiers.Control, combination.Modifiers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_EmptyOrNull_ReturnsNone(string? input)
    {
        var combination = KeyCombination.Parse(input!);

        Assert.Equal(KeyCombination.None, combination);
    }

    [Fact]
    public void Parse_UnknownKey_ReturnsUnknown()
    {
        var combination = KeyCombination.Parse("Ctrl+Unknown");

        Assert.Equal(Key.Unknown, combination.Key);
    }

    [Theory]
    [InlineData("Control+S", Key.S, KeyModifiers.Control)]
    [InlineData("Win+D", Key.D, KeyModifiers.Super)]
    [InlineData("Cmd+C", Key.C, KeyModifiers.Super)]
    [InlineData("Command+V", Key.V, KeyModifiers.Super)]
    public void Parse_AlternativeModifierNames_Works(string input, Key expectedKey, KeyModifiers expectedModifiers)
    {
        var combination = KeyCombination.Parse(input);

        Assert.Equal(expectedKey, combination.Key);
        Assert.Equal(expectedModifiers, combination.Modifiers);
    }

    [Theory]
    [InlineData("0", Key.Number0)]
    [InlineData("1", Key.Number1)]
    [InlineData("9", Key.Number9)]
    [InlineData("Numpad5", Key.Keypad5)]
    [InlineData("KP7", Key.Keypad7)]
    public void Parse_NumericKeys_Works(string input, Key expectedKey)
    {
        var combination = KeyCombination.Parse(input);

        Assert.Equal(expectedKey, combination.Key);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_SingleKey_ReturnsKeyName()
    {
        var combination = new KeyCombination(Key.F1);

        Assert.Equal("F1", combination.ToString());
    }

    [Fact]
    public void ToString_WithControl_ReturnsCorrectFormat()
    {
        var combination = new KeyCombination(Key.S, KeyModifiers.Control);

        Assert.Equal("Ctrl+S", combination.ToString());
    }

    [Fact]
    public void ToString_MultipleModifiers_ReturnsCorrectOrder()
    {
        var combination = new KeyCombination(Key.N, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt);

        // Should be Ctrl first, then Shift, then Alt
        Assert.Equal("Ctrl+Shift+Alt+N", combination.ToString());
    }

    [Fact]
    public void ToString_None_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, KeyCombination.None.ToString());
    }

    [Fact]
    public void ToString_Delete_ReturnsDel()
    {
        var combination = new KeyCombination(Key.Delete);

        Assert.Equal("Del", combination.ToString());
    }

    [Fact]
    public void ToString_Number_ReturnsDigit()
    {
        var combination = new KeyCombination(Key.Number5);

        Assert.Equal("5", combination.ToString());
    }

    #endregion

    #region Matches Tests

    [Fact]
    public void Matches_ExactMatch_ReturnsTrue()
    {
        var combination = new KeyCombination(Key.S, KeyModifiers.Control);

        Assert.True(combination.Matches(Key.S, KeyModifiers.Control));
    }

    [Fact]
    public void Matches_DifferentKey_ReturnsFalse()
    {
        var combination = new KeyCombination(Key.S, KeyModifiers.Control);

        Assert.False(combination.Matches(Key.D, KeyModifiers.Control));
    }

    [Fact]
    public void Matches_DifferentModifiers_ReturnsFalse()
    {
        var combination = new KeyCombination(Key.S, KeyModifiers.Control);

        Assert.False(combination.Matches(Key.S, KeyModifiers.Shift));
    }

    [Fact]
    public void Matches_ExtraLocksIgnored_ReturnsTrue()
    {
        var combination = new KeyCombination(Key.S, KeyModifiers.Control);

        // Should still match even with CapsLock and NumLock active
        Assert.True(combination.Matches(Key.S, KeyModifiers.Control | KeyModifiers.CapsLock | KeyModifiers.NumLock));
    }

    [Fact]
    public void Matches_NoModifiers_MatchesNoModifiers()
    {
        var combination = new KeyCombination(Key.F1);

        Assert.True(combination.Matches(Key.F1, KeyModifiers.None));
        Assert.False(combination.Matches(Key.F1, KeyModifiers.Control));
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_ValidKey_ReturnsTrue()
    {
        var combination = new KeyCombination(Key.A);

        Assert.True(combination.IsValid);
    }

    [Fact]
    public void IsValid_UnknownKey_ReturnsFalse()
    {
        var combination = new KeyCombination(Key.Unknown);

        Assert.False(combination.IsValid);
    }

    [Fact]
    public void IsValid_None_ReturnsFalse()
    {
        Assert.False(KeyCombination.None.IsValid);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_SameKeyAndModifiers_AreEqual()
    {
        var a = new KeyCombination(Key.S, KeyModifiers.Control);
        var b = new KeyCombination(Key.S, KeyModifiers.Control);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentKey_AreNotEqual()
    {
        var a = new KeyCombination(Key.S, KeyModifiers.Control);
        var b = new KeyCombination(Key.D, KeyModifiers.Control);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Equality_DifferentModifiers_AreNotEqual()
    {
        var a = new KeyCombination(Key.S, KeyModifiers.Control);
        var b = new KeyCombination(Key.S, KeyModifiers.Shift);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_SameCombination_SameHash()
    {
        var a = new KeyCombination(Key.S, KeyModifiers.Control);
        var b = new KeyCombination(Key.S, KeyModifiers.Control);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    #endregion

    #region Roundtrip Tests

    [Theory]
    [InlineData("Ctrl+S")]
    [InlineData("Ctrl+Shift+N")]
    [InlineData("Alt+F4")]
    [InlineData("F1")]
    [InlineData("Del")]
    [InlineData("Enter")]
    public void ParseAndToString_Roundtrips(string input)
    {
        var combination = KeyCombination.Parse(input);
        var output = combination.ToString();
        var reparsed = KeyCombination.Parse(output);

        Assert.Equal(combination, reparsed);
    }

    #endregion
}
