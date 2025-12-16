namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="InputAction"/> class.
/// </summary>
public class InputActionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsName()
    {
        var action = new InputAction("Jump");

        Assert.Equal("Jump", action.Name);
    }

    [Fact]
    public void Constructor_WithBindings_SetsBindings()
    {
        var binding1 = InputBinding.FromKey(Key.Space);
        var binding2 = InputBinding.FromGamepadButton(GamepadButton.South);

        var action = new InputAction("Jump", binding1, binding2);

        Assert.Equal(2, action.Bindings.Count);
        Assert.Contains(binding1, action.Bindings);
        Assert.Contains(binding2, action.Bindings);
    }

    [Fact]
    public void Constructor_NoBindings_HasEmptyBindingsList()
    {
        var action = new InputAction("Jump");

        Assert.Empty(action.Bindings);
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => new InputAction(null!));
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new InputAction(""));
    }

    [Fact]
    public void Constructor_WhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new InputAction("   "));
    }

    #endregion

    #region Binding Management Tests

    [Fact]
    public void AddBinding_IncreasesBindingCount()
    {
        var action = new InputAction("Jump");

        action.AddBinding(InputBinding.FromKey(Key.Space));

        Assert.Single(action.Bindings);
    }

    [Fact]
    public void AddBinding_AddsCorrectBinding()
    {
        var action = new InputAction("Jump");
        var binding = InputBinding.FromKey(Key.W);

        action.AddBinding(binding);

        Assert.Contains(binding, action.Bindings);
    }

    [Fact]
    public void RemoveBinding_ExistingBinding_ReturnsTrue()
    {
        var binding = InputBinding.FromKey(Key.Space);
        var action = new InputAction("Jump", binding);

        bool result = action.RemoveBinding(binding);

        Assert.True(result);
    }

    [Fact]
    public void RemoveBinding_ExistingBinding_RemovesFromList()
    {
        var binding = InputBinding.FromKey(Key.Space);
        var action = new InputAction("Jump", binding);

        action.RemoveBinding(binding);

        Assert.DoesNotContain(binding, action.Bindings);
    }

    [Fact]
    public void RemoveBinding_NonExistentBinding_ReturnsFalse()
    {
        var action = new InputAction("Jump");
        var binding = InputBinding.FromKey(Key.Space);

        bool result = action.RemoveBinding(binding);

        Assert.False(result);
    }

    [Fact]
    public void ClearBindings_RemovesAllBindings()
    {
        var action = new InputAction("Jump",
            InputBinding.FromKey(Key.Space),
            InputBinding.FromGamepadButton(GamepadButton.South));

        action.ClearBindings();

        Assert.Empty(action.Bindings);
    }

    [Fact]
    public void SetBindings_ReplacesExistingBindings()
    {
        var oldBinding = InputBinding.FromKey(Key.Space);
        var action = new InputAction("Jump", oldBinding);

        var newBinding = InputBinding.FromKey(Key.W);
        action.SetBindings(newBinding);

        Assert.Single(action.Bindings);
        Assert.Contains(newBinding, action.Bindings);
        Assert.DoesNotContain(oldBinding, action.Bindings);
    }

    [Fact]
    public void SetBindings_MultipleBindings_SetsAll()
    {
        var action = new InputAction("Jump");

        var binding1 = InputBinding.FromKey(Key.Space);
        var binding2 = InputBinding.FromKey(Key.W);
        action.SetBindings(binding1, binding2);

        Assert.Equal(2, action.Bindings.Count);
    }

    #endregion

    #region Enabled Property Tests

    [Fact]
    public void Enabled_DefaultsToTrue()
    {
        var action = new InputAction("Jump");

        Assert.True(action.Enabled);
    }

    [Fact]
    public void Enabled_CanBeSetToFalse()
    {
        var action = new InputAction("Jump") { Enabled = false };

        Assert.False(action.Enabled);
    }

    #endregion

    #region GamepadIndex Property Tests

    [Fact]
    public void GamepadIndex_DefaultsToNegativeOne()
    {
        var action = new InputAction("Jump");

        Assert.Equal(-1, action.GamepadIndex);
    }

    [Fact]
    public void GamepadIndex_CanBeSetToSpecificIndex()
    {
        var action = new InputAction("Jump") { GamepadIndex = 2 };

        Assert.Equal(2, action.GamepadIndex);
    }

    #endregion
}
