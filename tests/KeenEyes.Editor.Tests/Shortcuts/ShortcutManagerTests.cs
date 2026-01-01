using KeenEyes.Editor.Shortcuts;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Tests.Shortcuts;

public class ShortcutManagerTests
{
    #region Registration Tests

    [Fact]
    public void Register_ValidShortcut_AddsBinding()
    {
        var manager = new ShortcutManager();

        var binding = manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        Assert.NotNull(binding);
        Assert.Equal("test.action", binding.ActionId);
        Assert.Equal("Test Action", binding.DisplayName);
        Assert.Equal("Test", binding.Category);
        Assert.Equal(new KeyCombination(Key.S, KeyModifiers.Control), binding.CurrentShortcut);
    }

    [Fact]
    public void Register_DuplicateActionId_ThrowsArgumentException()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        Assert.Throws<ArgumentException>(() =>
            manager.Register("test.action", "Another Action", "Test", "Ctrl+D", () => { }));
    }

    [Fact]
    public void Register_NullActionId_ThrowsArgumentNullException()
    {
        var manager = new ShortcutManager();

        Assert.Throws<ArgumentNullException>(() =>
            manager.Register(null!, "Test Action", "Test", "Ctrl+S", () => { }));
    }

    [Fact]
    public void Register_EmptyActionId_ThrowsArgumentException()
    {
        var manager = new ShortcutManager();

        Assert.Throws<ArgumentException>(() =>
            manager.Register("", "Test Action", "Test", "Ctrl+S", () => { }));
    }

    [Fact]
    public void Register_NullAction_ThrowsArgumentNullException()
    {
        var manager = new ShortcutManager();

        Assert.Throws<ArgumentNullException>(() =>
            manager.Register("test.action", "Test Action", "Test", "Ctrl+S", null!));
    }

    #endregion

    #region GetBinding Tests

    [Fact]
    public void GetBinding_ExistingAction_ReturnsBinding()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        var binding = manager.GetBinding("test.action");

        Assert.NotNull(binding);
        Assert.Equal("test.action", binding.ActionId);
    }

    [Fact]
    public void GetBinding_NonExistingAction_ReturnsNull()
    {
        var manager = new ShortcutManager();

        var binding = manager.GetBinding("nonexistent");

        Assert.Null(binding);
    }

    [Fact]
    public void GetBinding_CaseInsensitive()
    {
        var manager = new ShortcutManager();
        manager.Register("Test.Action", "Test Action", "Test", "Ctrl+S", () => { });

        var binding = manager.GetBinding("TEST.ACTION");

        Assert.NotNull(binding);
    }

    #endregion

    #region GetShortcut Tests

    [Fact]
    public void GetShortcut_ExistingAction_ReturnsKeyCombination()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+Shift+S", () => { });

        var shortcut = manager.GetShortcut("test.action");

        Assert.Equal(Key.S, shortcut.Key);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, shortcut.Modifiers);
    }

    [Fact]
    public void GetShortcut_NonExistingAction_ReturnsNone()
    {
        var manager = new ShortcutManager();

        var shortcut = manager.GetShortcut("nonexistent");

        Assert.Equal(KeyCombination.None, shortcut);
    }

    [Fact]
    public void GetShortcutString_ExistingAction_ReturnsFormattedString()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+Shift+S", () => { });

        var shortcutStr = manager.GetShortcutString("test.action");

        Assert.Equal("Ctrl+Shift+S", shortcutStr);
    }

    [Fact]
    public void GetShortcutString_NonExistingAction_ReturnsNull()
    {
        var manager = new ShortcutManager();

        var shortcutStr = manager.GetShortcutString("nonexistent");

        Assert.Null(shortcutStr);
    }

    #endregion

    #region ProcessKeyDown Tests

    [Fact]
    public void ProcessKeyDown_MatchingShortcut_ExecutesAction()
    {
        var manager = new ShortcutManager();
        var called = false;
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => called = true);

        var handled = manager.ProcessKeyDown(Key.S, KeyModifiers.Control);

        Assert.True(handled);
        Assert.True(called);
    }

    [Fact]
    public void ProcessKeyDown_NonMatchingKey_DoesNotExecuteAction()
    {
        var manager = new ShortcutManager();
        var called = false;
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => called = true);

        var handled = manager.ProcessKeyDown(Key.D, KeyModifiers.Control);

        Assert.False(handled);
        Assert.False(called);
    }

    [Fact]
    public void ProcessKeyDown_NonMatchingModifiers_DoesNotExecuteAction()
    {
        var manager = new ShortcutManager();
        var called = false;
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => called = true);

        var handled = manager.ProcessKeyDown(Key.S, KeyModifiers.None);

        Assert.False(handled);
        Assert.False(called);
    }

    [Fact]
    public void ProcessKeyDown_ModifierKeyAlone_ReturnsFalse()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        var handled = manager.ProcessKeyDown(Key.LeftControl, KeyModifiers.Control);

        Assert.False(handled);
    }

    [Fact]
    public void ProcessKeyDown_DisabledShortcut_DoesNotExecuteAction()
    {
        var manager = new ShortcutManager();
        var called = false;
        var binding = manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => called = true);
        binding.IsEnabled = false;

        var handled = manager.ProcessKeyDown(Key.S, KeyModifiers.Control);

        Assert.False(handled);
        Assert.False(called);
    }

    [Fact]
    public void ProcessKeyDown_IgnoresLocksInModifiers()
    {
        var manager = new ShortcutManager();
        var called = false;
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => called = true);

        // Even with CapsLock active, should match
        var handled = manager.ProcessKeyDown(Key.S, KeyModifiers.Control | KeyModifiers.CapsLock);

        Assert.True(handled);
        Assert.True(called);
    }

    #endregion

    #region Rebind Tests

    [Fact]
    public void Rebind_ValidNewShortcut_UpdatesBinding()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        var result = manager.Rebind("test.action", "Ctrl+D");

        Assert.True(result);
        Assert.Equal(new KeyCombination(Key.D, KeyModifiers.Control), manager.GetShortcut("test.action"));
    }

    [Fact]
    public void Rebind_AfterRebind_NewShortcutTriggersAction()
    {
        var manager = new ShortcutManager();
        var called = false;
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => called = true);
        manager.Rebind("test.action", "Ctrl+D");

        // Old shortcut should not work
        manager.ProcessKeyDown(Key.S, KeyModifiers.Control);
        Assert.False(called);

        // New shortcut should work
        manager.ProcessKeyDown(Key.D, KeyModifiers.Control);
        Assert.True(called);
    }

    [Fact]
    public void Rebind_ConflictingShortcut_ReturnsFalse()
    {
        var manager = new ShortcutManager();
        manager.Register("action1", "Action 1", "Test", "Ctrl+S", () => { });
        manager.Register("action2", "Action 2", "Test", "Ctrl+D", () => { });

        var result = manager.Rebind("action2", "Ctrl+S");

        Assert.False(result);
        // Original binding should be preserved
        Assert.Equal(new KeyCombination(Key.D, KeyModifiers.Control), manager.GetShortcut("action2"));
    }

    [Fact]
    public void Rebind_ConflictingShortcut_RaisesConflictEvent()
    {
        var manager = new ShortcutManager();
        manager.Register("action1", "Action 1", "Test", "Ctrl+S", () => { });
        manager.Register("action2", "Action 2", "Test", "Ctrl+D", () => { });

        ShortcutConflictEventArgs? eventArgs = null;
        manager.ConflictDetected += (_, e) => eventArgs = e;

        manager.Rebind("action2", "Ctrl+S");

        Assert.NotNull(eventArgs);
        Assert.Equal("action2", eventArgs.Binding.ActionId);
        Assert.Equal("action1", eventArgs.ConflictingBinding.ActionId);
    }

    [Fact]
    public void Rebind_RaisesShortcutChangedEvent()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        ShortcutChangedEventArgs? eventArgs = null;
        manager.ShortcutChanged += (_, e) => eventArgs = e;

        manager.Rebind("test.action", "Ctrl+D");

        Assert.NotNull(eventArgs);
        Assert.Equal("test.action", eventArgs.Binding.ActionId);
        Assert.Equal(new KeyCombination(Key.S, KeyModifiers.Control), eventArgs.OldShortcut);
        Assert.Equal(new KeyCombination(Key.D, KeyModifiers.Control), eventArgs.NewShortcut);
    }

    [Fact]
    public void Rebind_NonExistingAction_ReturnsFalse()
    {
        var manager = new ShortcutManager();

        var result = manager.Rebind("nonexistent", "Ctrl+S");

        Assert.False(result);
    }

    [Fact]
    public void Rebind_SameShortcut_Succeeds()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        var result = manager.Rebind("test.action", "Ctrl+S");

        Assert.True(result);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void ResetToDefault_RestoresDefaultShortcut()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });
        manager.Rebind("test.action", "Ctrl+D");

        manager.ResetToDefault("test.action");

        Assert.Equal(new KeyCombination(Key.S, KeyModifiers.Control), manager.GetShortcut("test.action"));
    }

    [Fact]
    public void ResetAllToDefaults_RestoresAllShortcuts()
    {
        var manager = new ShortcutManager();
        manager.Register("action1", "Action 1", "Test", "Ctrl+S", () => { });
        manager.Register("action2", "Action 2", "Test", "Ctrl+D", () => { });
        manager.Rebind("action1", "Ctrl+A");
        manager.Rebind("action2", "Ctrl+B");

        manager.ResetAllToDefaults();

        Assert.Equal(new KeyCombination(Key.S, KeyModifiers.Control), manager.GetShortcut("action1"));
        Assert.Equal(new KeyCombination(Key.D, KeyModifiers.Control), manager.GetShortcut("action2"));
    }

    [Fact]
    public void IsCustomized_AfterRebind_ReturnsTrue()
    {
        var manager = new ShortcutManager();
        var binding = manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });
        manager.Rebind("test.action", "Ctrl+D");

        Assert.True(binding.IsCustomized);
    }

    [Fact]
    public void IsCustomized_AfterReset_ReturnsFalse()
    {
        var manager = new ShortcutManager();
        var binding = manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });
        manager.Rebind("test.action", "Ctrl+D");
        manager.ResetToDefault("test.action");

        Assert.False(binding.IsCustomized);
    }

    #endregion

    #region Category Tests

    [Fact]
    public void Categories_ReturnsDistinctCategories()
    {
        var manager = new ShortcutManager();
        manager.Register("action1", "Action 1", "File", "Ctrl+S", () => { });
        manager.Register("action2", "Action 2", "Edit", "Ctrl+Z", () => { });
        manager.Register("action3", "Action 3", "File", "Ctrl+O", () => { });

        var categories = manager.Categories.ToList();

        Assert.Equal(2, categories.Count);
        Assert.Contains("Edit", categories);
        Assert.Contains("File", categories);
    }

    [Fact]
    public void GetBindingsForCategory_ReturnsCorrectBindings()
    {
        var manager = new ShortcutManager();
        manager.Register("action1", "Action 1", "File", "Ctrl+S", () => { });
        manager.Register("action2", "Action 2", "Edit", "Ctrl+Z", () => { });
        manager.Register("action3", "Action 3", "File", "Ctrl+O", () => { });

        var fileBindings = manager.GetBindingsForCategory("File").ToList();

        Assert.Equal(2, fileBindings.Count);
        Assert.All(fileBindings, b => Assert.Equal("File", b.Category));
    }

    #endregion

    #region FindActionForShortcut Tests

    [Fact]
    public void FindActionForShortcut_ExistingShortcut_ReturnsActionId()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        var actionId = manager.FindActionForShortcut(new KeyCombination(Key.S, KeyModifiers.Control));

        Assert.Equal("test.action", actionId);
    }

    [Fact]
    public void FindActionForShortcut_NonExistingShortcut_ReturnsNull()
    {
        var manager = new ShortcutManager();
        manager.Register("test.action", "Test Action", "Test", "Ctrl+S", () => { });

        var actionId = manager.FindActionForShortcut(new KeyCombination(Key.D, KeyModifiers.Control));

        Assert.Null(actionId);
    }

    #endregion
}
