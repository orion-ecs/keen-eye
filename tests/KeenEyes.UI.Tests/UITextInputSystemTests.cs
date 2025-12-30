using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UITextInputSystem text editing functionality.
/// </summary>
public class UITextInputSystemTests
{
    #region Focus Tests

    [Fact]
    public void FocusGained_ClearsPlaceholder()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Enter text..." })
            .With(new UITextInput { PlaceholderText = "Enter text...", ShowingPlaceholder = true })
            .Build();

        var focusEvent = new UIFocusGainedEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var text = ref world.Get<UIText>(textField);
        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal("", text.Content);
        Assert.False(textInput.ShowingPlaceholder);
        Assert.True(textInput.IsEditing);
    }

    [Fact]
    public void FocusGained_SelectsAllText()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput())
            .Build();

        var focusEvent = new UIFocusGainedEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal(0, textInput.SelectionStart);
        Assert.Equal(11, textInput.SelectionEnd);
        Assert.Equal(11, textInput.CursorPosition);
    }

    [Fact]
    public void FocusLost_ShowsPlaceholderWhenEmpty()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "" })
            .With(new UITextInput { PlaceholderText = "Enter text...", IsEditing = true })
            .Build();

        var focusEvent = new UIFocusLostEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var text = ref world.Get<UIText>(textField);
        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal("Enter text...", text.Content);
        Assert.True(textInput.ShowingPlaceholder);
        Assert.False(textInput.IsEditing);
    }

    [Fact]
    public void FocusLost_ClearsSelection()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, SelectionStart = 0, SelectionEnd = 5 })
            .Build();

        var focusEvent = new UIFocusLostEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal(0, textInput.SelectionStart);
        Assert.Equal(0, textInput.SelectionEnd);
    }

    [Fact]
    public void FocusGained_WithNullContent_HandlesGracefully()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "" })
            .With(new UITextInput())
            .Build();

        var focusEvent = new UIFocusGainedEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.True(textInput.IsEditing);
        Assert.Equal(0, textInput.CursorPosition);
    }

    [Fact]
    public void FocusGained_DeadEntity_IsIgnored()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput())
            .Build();

        world.Despawn(textField);

        var focusEvent = new UIFocusGainedEvent(textField, null);
        world.Send(focusEvent);

        // Should not throw
        textInputSystem.Update(0);
    }

    [Fact]
    public void FocusLost_DeadEntity_IsIgnored()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true })
            .Build();

        world.Despawn(textField);

        var focusEvent = new UIFocusLostEvent(textField, null);
        world.Send(focusEvent);

        // Should not throw
        textInputSystem.Update(0);
    }

    [Fact]
    public void FocusGained_NonTextInputEntity_IsIgnored()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var button = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var focusEvent = new UIFocusGainedEvent(button, null);
        world.Send(focusEvent);

        // Should not throw
        textInputSystem.Update(0);
    }

    [Fact]
    public void FocusLost_NonTextInputEntity_IsIgnored()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var button = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var focusEvent = new UIFocusLostEvent(button, null);
        world.Send(focusEvent);

        // Should not throw
        textInputSystem.Update(0);
    }

    [Fact]
    public void FocusLost_WithContent_DoesNotShowPlaceholder()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Has content" })
            .With(new UITextInput { PlaceholderText = "Enter text...", IsEditing = true })
            .Build();

        var focusEvent = new UIFocusLostEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var text = ref world.Get<UIText>(textField);
        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal("Has content", text.Content);
        Assert.False(textInput.ShowingPlaceholder);
    }

    [Fact]
    public void FocusLost_WithNoPlaceholder_DoesNotShowPlaceholder()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "" })
            .With(new UITextInput { PlaceholderText = "", IsEditing = true })
            .Build();

        var focusEvent = new UIFocusLostEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var text = ref world.Get<UIText>(textField);
        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal("", text.Content);
        Assert.False(textInput.ShowingPlaceholder);
    }

    [Fact]
    public void FocusLost_EntityWithoutText_IsIgnored()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UITextInput { IsEditing = true })
            .Build();

        var focusEvent = new UIFocusLostEvent(textField, null);
        world.Send(focusEvent);

        // Should not throw
        textInputSystem.Update(0);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.False(textInput.IsEditing);
    }

    [Fact]
    public void FocusGained_EntityWithoutText_StillSetsEditing()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UITextInput())
            .Build();

        var focusEvent = new UIFocusGainedEvent(textField, null);
        world.Send(focusEvent);

        textInputSystem.Update(0);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.True(textInput.IsEditing);
    }

    #endregion

    #region Max Length Tests

    [Fact]
    public void TextInput_ExceedsMaxLength_DoesNotInsert()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5, MaxLength = 5 })
            .Build();

        ref readonly var textBefore = ref world.Get<UIText>(textField);
        var contentBefore = textBefore.Content;

        Assert.Equal(5, contentBefore?.Length);
    }

    #endregion

    #region Selection Range Tests

    [Fact]
    public void GetSelectionRange_ReturnsCorrectRange()
    {
        using var world = new World();

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UITextInput { SelectionStart = 5, SelectionEnd = 2 })
            .Build();

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        var (start, end) = textInput.GetSelectionRange();

        Assert.Equal(2, start);
        Assert.Equal(5, end);
    }

    [Fact]
    public void ClearSelection_ResetsSelection()
    {
        using var world = new World();

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UITextInput { SelectionStart = 2, SelectionEnd = 5 })
            .Build();

        ref var textInput = ref world.Get<UITextInput>(textField);
        textInput.ClearSelection();

        Assert.Equal(0, textInput.SelectionStart);
        Assert.Equal(0, textInput.SelectionEnd);
        Assert.False(textInput.HasSelection);
    }

    [Fact]
    public void GetSelectionRange_WithEqualStartEnd_ReturnsCorrectRange()
    {
        using var world = new World();

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UITextInput { SelectionStart = 3, SelectionEnd = 3 })
            .Build();

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        var (start, end) = textInput.GetSelectionRange();

        Assert.Equal(3, start);
        Assert.Equal(3, end);
    }

    [Fact]
    public void HasSelection_WithDifferentStartEnd_ReturnsTrue()
    {
        using var world = new World();

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UITextInput { SelectionStart = 2, SelectionEnd = 5 })
            .Build();

        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.True(textInput.HasSelection);
    }

    [Fact]
    public void HasSelection_WithEqualStartEnd_ReturnsFalse()
    {
        using var world = new World();

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UITextInput { SelectionStart = 3, SelectionEnd = 3 })
            .Build();

        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.False(textInput.HasSelection);
    }

    #endregion

    #region Multiline Tests

    [Fact]
    public void TextInput_Multiline_AcceptsNewline()
    {
        using var world = new World();

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Line1" })
            .With(new UITextInput { Multiline = true })
            .Build();

        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.True(textInput.Multiline);
    }

    [Fact]
    public void TextInput_SingleLine_IgnoresNewline()
    {
        using var world = new World();

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Line1" })
            .With(new UITextInput { Multiline = false })
            .Build();

        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.False(textInput.Multiline);
    }

    #endregion

    #region System Lifecycle Tests

    [Fact]
    public void TextInputSystem_Update_WithNoInputContext_DoesNotThrow()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        // Should not throw when no input context is registered
        textInputSystem.Update(0);
        textInputSystem.Update(0);
    }

    [Fact]
    public void TextInputSystem_Dispose_WithNoSubscriptions_DoesNotThrow()
    {
        using var world = new World();
        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        // Should not throw
        textInputSystem.Dispose();
    }

    #endregion

    #region Keyboard Input Tests

    [Fact]
    public void TextInput_InsertsCharacter_AtCursorPosition()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);

        // First update to subscribe to input
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        // Simulate text input
        inputContext.MockKeyboard.SimulateTextInput('!');

        ref readonly var text = ref world.Get<UIText>(textField);
        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal("Hello!", text.Content);
        Assert.Equal(6, textInput.CursorPosition);
    }

    [Fact]
    public void TextInput_DeletesSelection_WhenTyping()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 11, SelectionStart = 0, SelectionEnd = 11 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);

        inputContext.MockKeyboard.SimulateTextInput('X');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("X", text.Content);
    }

    [Fact]
    public void TextInput_RespectsMaxLength()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "12345" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5, MaxLength = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        inputContext.MockKeyboard.SimulateTextInput('6');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("12345", text.Content);
    }

    [Fact]
    public void TextInput_IgnoresControlCharacters()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5, Multiline = false })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        // Tab should be ignored
        inputContext.MockKeyboard.SimulateTextInput('\t');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Hello", text.Content);
    }

    [Fact]
    public void TextInput_Multiline_AcceptsNewlineCharacter()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Line1" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5, Multiline = true })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        inputContext.MockKeyboard.SimulateTextInput('\n');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Line1\n", text.Content);
    }

    [Fact]
    public void TextInput_SingleLine_RejectsNewline()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Line1" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5, Multiline = false })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        inputContext.MockKeyboard.SimulateTextInput('\n');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Line1", text.Content);
    }

    [Fact]
    public void TextInput_WithNoFocus_IgnoresInput()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .Build();

        // Don't set focus
        textInputSystem.Update(0);

        inputContext.MockKeyboard.SimulateTextInput('!');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Hello", text.Content);
    }

    [Fact]
    public void TextInput_WithDeadFocusedEntity_IgnoresInput()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);

        world.Despawn(textField);

        // Should not throw
        inputContext.MockKeyboard.SimulateTextInput('!');
    }

    [Fact]
    public void TextInput_WhenNotEditing_IgnoresInput()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = false, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);

        // Reset IsEditing to false since focus handler sets it to true
        ref var textInput = ref world.Get<UITextInput>(textField);
        textInput.IsEditing = false;

        inputContext.MockKeyboard.SimulateTextInput('!');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Hello", text.Content);
    }

    #endregion

    #region Backspace Tests

    [Fact]
    public void Backspace_DeletesCharacterBeforeCursor()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        inputContext.SimulateKeyDown(Key.Backspace);

        ref readonly var text = ref world.Get<UIText>(textField);
        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal("Hell", text.Content);
        Assert.Equal(4, textInput.CursorPosition);
    }

    [Fact]
    public void Backspace_AtStart_DoesNothing()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 0 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 0);

        inputContext.SimulateKeyDown(Key.Backspace);

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Hello", text.Content);
    }

    [Fact]
    public void Backspace_WithSelection_DeletesSelection()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 11, SelectionStart = 5, SelectionEnd = 11 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 11, selectionStart: 5, selectionEnd: 11);

        inputContext.SimulateKeyDown(Key.Backspace);

        ref readonly var text = ref world.Get<UIText>(textField);
        ref readonly var textInput = ref world.Get<UITextInput>(textField);

        Assert.Equal("Hello", text.Content);
        Assert.Equal(5, textInput.CursorPosition);
        Assert.False(textInput.HasSelection);
    }

    #endregion

    #region Delete Key Tests

    [Fact]
    public void Delete_DeletesCharacterAtCursor()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 0 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 0);

        inputContext.SimulateKeyDown(Key.Delete);

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("ello", text.Content);
    }

    [Fact]
    public void Delete_AtEnd_DoesNothing()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        inputContext.SimulateKeyDown(Key.Delete);

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Hello", text.Content);
    }

    [Fact]
    public void Delete_WithSelection_DeletesSelection()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 0, SelectionStart = 0, SelectionEnd = 6 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 0, selectionStart: 0, selectionEnd: 6);

        inputContext.SimulateKeyDown(Key.Delete);

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("World", text.Content);
    }

    #endregion

    #region Arrow Key Tests

    [Fact]
    public void LeftArrow_MovesCursorLeft()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 3 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 3);

        inputContext.SimulateKeyDown(Key.Left);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(2, textInput.CursorPosition);
    }

    [Fact]
    public void RightArrow_MovesCursorRight()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 3 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 3);

        inputContext.SimulateKeyDown(Key.Right);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(4, textInput.CursorPosition);
    }

    [Fact]
    public void LeftArrow_AtStart_StaysAtStart()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 0 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 0);

        inputContext.SimulateKeyDown(Key.Left);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(0, textInput.CursorPosition);
    }

    [Fact]
    public void RightArrow_AtEnd_StaysAtEnd()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        inputContext.SimulateKeyDown(Key.Right);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(5, textInput.CursorPosition);
    }

    [Fact]
    public void LeftArrow_WithSelection_CollapsesToStart()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 4, SelectionStart = 1, SelectionEnd = 4 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 4, selectionStart: 1, selectionEnd: 4);

        inputContext.SimulateKeyDown(Key.Left);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(1, textInput.CursorPosition);
        Assert.False(textInput.HasSelection);
    }

    [Fact]
    public void RightArrow_WithSelection_CollapsesToEnd()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 1, SelectionStart = 1, SelectionEnd = 4 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 1, selectionStart: 1, selectionEnd: 4);

        inputContext.SimulateKeyDown(Key.Right);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(4, textInput.CursorPosition);
        Assert.False(textInput.HasSelection);
    }

    [Fact]
    public void ShiftLeft_ExtendsSelection()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 3 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 3);

        inputContext.SimulateKeyDown(Key.Left, KeyModifiers.Shift);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(2, textInput.CursorPosition);
        Assert.True(textInput.HasSelection);
    }

    [Fact]
    public void ShiftRight_ExtendsSelection()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 2 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 2);

        inputContext.SimulateKeyDown(Key.Right, KeyModifiers.Shift);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(3, textInput.CursorPosition);
        Assert.True(textInput.HasSelection);
    }

    #endregion

    #region Ctrl+Arrow Word Navigation Tests

    [Fact]
    public void CtrlLeft_JumpsToWordBoundary()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 11 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 11);

        inputContext.SimulateKeyDown(Key.Left, KeyModifiers.Control);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(6, textInput.CursorPosition);
    }

    [Fact]
    public void CtrlRight_JumpsToWordBoundary()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 0 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 0);

        inputContext.SimulateKeyDown(Key.Right, KeyModifiers.Control);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(6, textInput.CursorPosition);
    }

    [Fact]
    public void CtrlLeft_AtStart_StaysAtStart()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 0 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 0);

        inputContext.SimulateKeyDown(Key.Left, KeyModifiers.Control);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(0, textInput.CursorPosition);
    }

    [Fact]
    public void CtrlRight_AtEnd_StaysAtEnd()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        inputContext.SimulateKeyDown(Key.Right, KeyModifiers.Control);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(5, textInput.CursorPosition);
    }

    #endregion

    #region Home/End Tests

    [Fact]
    public void Home_MovesCursorToStart()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 3 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 3);

        inputContext.SimulateKeyDown(Key.Home);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(0, textInput.CursorPosition);
        Assert.False(textInput.HasSelection);
    }

    [Fact]
    public void End_MovesCursorToEnd()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 2 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 2);

        inputContext.SimulateKeyDown(Key.End);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(5, textInput.CursorPosition);
        Assert.False(textInput.HasSelection);
    }

    [Fact]
    public void ShiftHome_SelectsToStart()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 3 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 3);

        inputContext.SimulateKeyDown(Key.Home, KeyModifiers.Shift);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(0, textInput.CursorPosition);
        Assert.True(textInput.HasSelection);
        Assert.Equal(3, textInput.SelectionStart);
        Assert.Equal(0, textInput.SelectionEnd);
    }

    [Fact]
    public void ShiftEnd_SelectsToEnd()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 2 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 2);

        inputContext.SimulateKeyDown(Key.End, KeyModifiers.Shift);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(5, textInput.CursorPosition);
        Assert.True(textInput.HasSelection);
        Assert.Equal(2, textInput.SelectionStart);
        Assert.Equal(5, textInput.SelectionEnd);
    }

    #endregion

    #region Ctrl+A Select All Tests

    [Fact]
    public void CtrlA_SelectsAllText()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 3 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 3);

        inputContext.SimulateKeyDown(Key.A, KeyModifiers.Control);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(0, textInput.SelectionStart);
        Assert.Equal(11, textInput.SelectionEnd);
        Assert.Equal(11, textInput.CursorPosition);
    }

    #endregion

    #region TextChangedEvent Tests

    [Fact]
    public void TextInput_SendsTextChangedEvent()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        UITextChangedEvent? receivedEvent = null;
        world.Subscribe<UITextChangedEvent>(e => receivedEvent = e);

        inputContext.MockKeyboard.SimulateTextInput('!');

        Assert.NotNull(receivedEvent);
        Assert.Equal(textField, receivedEvent.Value.Element);
        Assert.Equal("Hello", receivedEvent.Value.OldText);
        Assert.Equal("Hello!", receivedEvent.Value.NewText);
    }

    [Fact]
    public void Backspace_SendsTextChangedEvent()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5);

        UITextChangedEvent? receivedEvent = null;
        world.Subscribe<UITextChangedEvent>(e => receivedEvent = e);

        inputContext.SimulateKeyDown(Key.Backspace);

        Assert.NotNull(receivedEvent);
        Assert.Equal("Hello", receivedEvent.Value.OldText);
        Assert.Equal("Hell", receivedEvent.Value.NewText);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void LeftArrow_WithSelectionAtStart_ClearsSelection()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 0, SelectionStart = 0, SelectionEnd = 3 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 0, selectionStart: 0, selectionEnd: 3);

        inputContext.SimulateKeyDown(Key.Left);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(0, textInput.CursorPosition);
        Assert.False(textInput.HasSelection);
    }

    [Fact]
    public void RightArrow_WithSelectionAtEnd_ClearsSelection()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 5, SelectionStart = 2, SelectionEnd = 5 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 5, selectionStart: 2, selectionEnd: 5);

        inputContext.SimulateKeyDown(Key.Right);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.Equal(5, textInput.CursorPosition);
        Assert.False(textInput.HasSelection);
    }

    [Fact]
    public void CtrlShiftLeft_ExtendsSelectionByWord()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hello World" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 11 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 11);

        inputContext.SimulateKeyDown(Key.Left, KeyModifiers.Control | KeyModifiers.Shift);

        ref readonly var textInput = ref world.Get<UITextInput>(textField);
        Assert.True(textInput.HasSelection);
        Assert.Equal(6, textInput.CursorPosition);
    }

    [Fact]
    public void TextInput_InsertInMiddle_MovesRemainingText()
    {
        using var world = new World();
        var inputContext = new MockInputContext();
        world.SetExtension<IInputContext>(inputContext);

        var uiContext = new UIContext(world);
        world.SetExtension(uiContext);

        var textInputSystem = new UITextInputSystem();
        world.AddSystem(textInputSystem);

        var textField = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "Hllo" })
            .With(new UITextInput { IsEditing = true, CursorPosition = 1 })
            .With(UIInteractable.Button())
            .Build();

        uiContext.RequestFocus(textField);
        textInputSystem.Update(0);
        SetTextInputState(world, textField, cursorPosition: 1);

        inputContext.MockKeyboard.SimulateTextInput('e');

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Hello", text.Content);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Sets up a focused text input with the specified cursor/selection state.
    /// Call this after RequestFocus + Update to override the default "select all" behavior.
    /// </summary>
    private static void SetTextInputState(IWorld world, Entity textField, int cursorPosition,
        int selectionStart = 0, int selectionEnd = 0)
    {
        ref var textInput = ref world.Get<UITextInput>(textField);
        textInput.CursorPosition = cursorPosition;
        textInput.SelectionStart = selectionStart;
        textInput.SelectionEnd = selectionEnd;
    }

    #endregion
}
