namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="InputActionMap"/> class.
/// </summary>
public class InputActionMapTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsName()
    {
        var map = new InputActionMap("Gameplay");

        Assert.Equal("Gameplay", map.Name);
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => new InputActionMap(null!));
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new InputActionMap(""));
    }

    [Fact]
    public void Constructor_WhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new InputActionMap("   "));
    }

    [Fact]
    public void Constructor_StartsWithEmptyActions()
    {
        var map = new InputActionMap("Gameplay");

        Assert.Empty(map.Actions);
    }

    #endregion

    #region AddAction Tests

    [Fact]
    public void AddAction_ReturnsCreatedAction()
    {
        var map = new InputActionMap("Gameplay");

        var action = map.AddAction("Jump");

        Assert.NotNull(action);
        Assert.Equal("Jump", action.Name);
    }

    [Fact]
    public void AddAction_WithBindings_SetsBindingsOnAction()
    {
        var map = new InputActionMap("Gameplay");
        var binding = InputBinding.FromKey(Key.Space);

        var action = map.AddAction("Jump", binding);

        Assert.Single(action.Bindings);
        Assert.Contains(binding, action.Bindings);
    }

    [Fact]
    public void AddAction_AddsToActionsDictionary()
    {
        var map = new InputActionMap("Gameplay");

        map.AddAction("Jump");

        Assert.True(map.Actions.ContainsKey("Jump"));
    }

    [Fact]
    public void AddAction_DuplicateName_ThrowsArgumentException()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        Assert.Throws<ArgumentException>(() => map.AddAction("Jump"));
    }

    [Fact]
    public void AddAction_DuplicateName_CaseInsensitive_ThrowsArgumentException()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        Assert.Throws<ArgumentException>(() => map.AddAction("JUMP"));
    }

    [Fact]
    public void AddAction_WhenMapEnabled_ActionIsEnabled()
    {
        var map = new InputActionMap("Gameplay") { Enabled = true };

        var action = map.AddAction("Jump");

        Assert.True(action.Enabled);
    }

    [Fact]
    public void AddAction_WhenMapDisabled_ActionIsDisabled()
    {
        var map = new InputActionMap("Gameplay") { Enabled = false };

        var action = map.AddAction("Jump");

        Assert.False(action.Enabled);
    }

    #endregion

    #region GetAction Tests

    [Fact]
    public void GetAction_ExistingAction_ReturnsAction()
    {
        var map = new InputActionMap("Gameplay");
        var expected = map.AddAction("Jump");

        var actual = map.GetAction("Jump");

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetAction_NonExistentAction_ReturnsNull()
    {
        var map = new InputActionMap("Gameplay");

        var action = map.GetAction("Jump");

        Assert.Null(action);
    }

    [Fact]
    public void GetAction_CaseInsensitive()
    {
        var map = new InputActionMap("Gameplay");
        var expected = map.AddAction("Jump");

        var actual = map.GetAction("JUMP");

        Assert.Same(expected, actual);
    }

    #endregion

    #region TryGetAction Tests

    [Fact]
    public void TryGetAction_ExistingAction_ReturnsTrue()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        bool result = map.TryGetAction("Jump", out _);

        Assert.True(result);
    }

    [Fact]
    public void TryGetAction_ExistingAction_OutputsAction()
    {
        var map = new InputActionMap("Gameplay");
        var expected = map.AddAction("Jump");

        map.TryGetAction("Jump", out var actual);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void TryGetAction_NonExistentAction_ReturnsFalse()
    {
        var map = new InputActionMap("Gameplay");

        bool result = map.TryGetAction("Jump", out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetAction_NonExistentAction_OutputsNull()
    {
        var map = new InputActionMap("Gameplay");

        map.TryGetAction("Jump", out var action);

        Assert.Null(action);
    }

    #endregion

    #region RemoveAction Tests

    [Fact]
    public void RemoveAction_ExistingAction_ReturnsTrue()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        bool result = map.RemoveAction("Jump");

        Assert.True(result);
    }

    [Fact]
    public void RemoveAction_ExistingAction_RemovesFromDictionary()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        map.RemoveAction("Jump");

        Assert.False(map.Actions.ContainsKey("Jump"));
    }

    [Fact]
    public void RemoveAction_NonExistentAction_ReturnsFalse()
    {
        var map = new InputActionMap("Gameplay");

        bool result = map.RemoveAction("Jump");

        Assert.False(result);
    }

    [Fact]
    public void RemoveAction_CaseInsensitive()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        bool result = map.RemoveAction("JUMP");

        Assert.True(result);
        Assert.False(map.Actions.ContainsKey("Jump"));
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllActions()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");
        map.AddAction("Fire");
        map.AddAction("Move");

        map.Clear();

        Assert.Empty(map.Actions);
    }

    #endregion

    #region ContainsAction Tests

    [Fact]
    public void ContainsAction_ExistingAction_ReturnsTrue()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        bool result = map.ContainsAction("Jump");

        Assert.True(result);
    }

    [Fact]
    public void ContainsAction_NonExistentAction_ReturnsFalse()
    {
        var map = new InputActionMap("Gameplay");

        bool result = map.ContainsAction("Jump");

        Assert.False(result);
    }

    [Fact]
    public void ContainsAction_CaseInsensitive()
    {
        var map = new InputActionMap("Gameplay");
        map.AddAction("Jump");

        bool result = map.ContainsAction("JUMP");

        Assert.True(result);
    }

    #endregion

    #region Enabled Property Tests

    [Fact]
    public void Enabled_DefaultsToTrue()
    {
        var map = new InputActionMap("Gameplay");

        Assert.True(map.Enabled);
    }

    [Fact]
    public void Enabled_SetToFalse_DisablesAllActions()
    {
        var map = new InputActionMap("Gameplay");
        var action1 = map.AddAction("Jump");
        var action2 = map.AddAction("Fire");

        map.Enabled = false;

        Assert.False(action1.Enabled);
        Assert.False(action2.Enabled);
    }

    [Fact]
    public void Enabled_SetToTrue_EnablesAllActions()
    {
        var map = new InputActionMap("Gameplay") { Enabled = false };
        var action1 = map.AddAction("Jump");
        var action2 = map.AddAction("Fire");

        map.Enabled = true;

        Assert.True(action1.Enabled);
        Assert.True(action2.Enabled);
    }

    #endregion
}
