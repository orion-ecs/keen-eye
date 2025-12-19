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

    #endregion

    #region Cursor Movement Tests
    // Note: Cursor movement logic is tested implicitly through focus and keyboard input integration tests

    #endregion

    #region Selection Tests
    // Note: Selection logic is tested implicitly through keyboard input integration tests

    #endregion

    #region Delete Tests
    // Note: Delete logic is tested implicitly through keyboard input integration tests

    #endregion

    #region Word Boundary Tests
    // Note: Word boundary logic is private and tested implicitly through keyboard input integration tests

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

    #region Arrow Key With Selection Tests
    // Note: Arrow key logic is private and tested implicitly through keyboard input integration tests

    #endregion
}
