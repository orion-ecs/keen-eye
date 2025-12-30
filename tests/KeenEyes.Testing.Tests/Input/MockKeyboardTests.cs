using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Testing.Tests.Input;

public class MockKeyboardTests
{
    #region SetKeyDown/SetKeyUp

    [Fact]
    public void SetKeyDown_SetsKeyPressed()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.W);

        Assert.True(keyboard.IsKeyDown(Key.W));
    }

    [Fact]
    public void SetKeyUp_ReleasesKey()
    {
        var keyboard = new MockKeyboard();
        keyboard.SetKeyDown(Key.W);

        keyboard.SetKeyUp(Key.W);

        Assert.False(keyboard.IsKeyDown(Key.W));
    }

    [Fact]
    public void IsKeyUp_WhenNotPressed_ReturnsTrue()
    {
        var keyboard = new MockKeyboard();

        Assert.True(keyboard.IsKeyUp(Key.W));
    }

    [Fact]
    public void IsKeyUp_WhenPressed_ReturnsFalse()
    {
        var keyboard = new MockKeyboard();
        keyboard.SetKeyDown(Key.W);

        Assert.False(keyboard.IsKeyUp(Key.W));
    }

    #endregion

    #region Modifier Keys

    [Fact]
    public void SetKeyDown_LeftShift_SetsShiftModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.LeftShift);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    [Fact]
    public void SetKeyDown_RightShift_SetsShiftModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.RightShift);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    [Fact]
    public void SetKeyUp_LeftShift_ClearsShiftModifier()
    {
        var keyboard = new MockKeyboard();
        keyboard.SetKeyDown(Key.LeftShift);

        keyboard.SetKeyUp(Key.LeftShift);

        Assert.False(keyboard.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    [Fact]
    public void SetKeyDown_LeftControl_SetsControlModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.LeftControl);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Control));
    }

    [Fact]
    public void SetKeyDown_RightControl_SetsControlModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.RightControl);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Control));
    }

    [Fact]
    public void SetKeyDown_LeftAlt_SetsAltModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.LeftAlt);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Alt));
    }

    [Fact]
    public void SetKeyDown_RightAlt_SetsAltModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.RightAlt);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Alt));
    }

    [Fact]
    public void SetKeyDown_LeftSuper_SetsSuperModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.LeftSuper);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Super));
    }

    [Fact]
    public void SetKeyDown_RightSuper_SetsSuperModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.RightSuper);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Super));
    }

    [Fact]
    public void SetKeyDown_CapsLock_SetsCapsLockModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.CapsLock);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.CapsLock));
    }

    [Fact]
    public void SetKeyDown_NumLock_SetsNumLockModifier()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.NumLock);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.NumLock));
    }

    [Fact]
    public void SetKeyDown_NonModifierKey_DoesNotChangeModifiers()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetKeyDown(Key.A);

        Assert.Equal(KeyModifiers.None, keyboard.Modifiers);
    }

    #endregion

    #region SetModifiers

    [Fact]
    public void SetModifiers_SetsModifiersDirectly()
    {
        var keyboard = new MockKeyboard();

        keyboard.SetModifiers(KeyModifiers.Shift | KeyModifiers.Control);

        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Control, keyboard.Modifiers);
    }

    #endregion

    #region ClearAllKeys

    [Fact]
    public void ClearAllKeys_ReleasesAllKeysAndModifiers()
    {
        var keyboard = new MockKeyboard();
        keyboard.SetKeyDown(Key.W);
        keyboard.SetKeyDown(Key.A);
        keyboard.SetKeyDown(Key.LeftShift);

        keyboard.ClearAllKeys();

        Assert.False(keyboard.IsKeyDown(Key.W));
        Assert.False(keyboard.IsKeyDown(Key.A));
        Assert.False(keyboard.IsKeyDown(Key.LeftShift));
        Assert.Equal(KeyModifiers.None, keyboard.Modifiers);
    }

    #endregion

    #region SimulateKeyDown

    [Fact]
    public void SimulateKeyDown_SetsKeyAndFiresEvent()
    {
        var keyboard = new MockKeyboard();
        KeyEventArgs? receivedArgs = null;
        keyboard.OnKeyDown += args => receivedArgs = args;

        keyboard.SimulateKeyDown(Key.Space);

        Assert.True(keyboard.IsKeyDown(Key.Space));
        Assert.NotNull(receivedArgs);
        Assert.Equal(Key.Space, receivedArgs.Value.Key);
        Assert.False(receivedArgs.Value.IsRepeat);
    }

    [Fact]
    public void SimulateKeyDown_WithModifiers_PassesModifiers()
    {
        var keyboard = new MockKeyboard();
        KeyEventArgs? receivedArgs = null;
        keyboard.OnKeyDown += args => receivedArgs = args;

        keyboard.SimulateKeyDown(Key.A, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.NotNull(receivedArgs);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, receivedArgs.Value.Modifiers);
    }

    [Fact]
    public void SimulateKeyDown_WithRepeat_PassesIsRepeat()
    {
        var keyboard = new MockKeyboard();
        KeyEventArgs? receivedArgs = null;
        keyboard.OnKeyDown += args => receivedArgs = args;

        keyboard.SimulateKeyDown(Key.A, isRepeat: true);

        Assert.NotNull(receivedArgs);
        Assert.True(receivedArgs.Value.IsRepeat);
    }

    [Fact]
    public void SimulateKeyDown_ShiftKey_UpdatesModifiers()
    {
        var keyboard = new MockKeyboard();
        keyboard.OnKeyDown += _ => { };

        keyboard.SimulateKeyDown(Key.LeftShift);

        Assert.True(keyboard.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    #endregion

    #region SimulateKeyUp

    [Fact]
    public void SimulateKeyUp_ReleasesKeyAndFiresEvent()
    {
        var keyboard = new MockKeyboard();
        keyboard.SimulateKeyDown(Key.Space);
        KeyEventArgs? receivedArgs = null;
        keyboard.OnKeyUp += args => receivedArgs = args;

        keyboard.SimulateKeyUp(Key.Space);

        Assert.False(keyboard.IsKeyDown(Key.Space));
        Assert.NotNull(receivedArgs);
        Assert.Equal(Key.Space, receivedArgs.Value.Key);
        Assert.False(receivedArgs.Value.IsRepeat);
    }

    [Fact]
    public void SimulateKeyUp_WithModifiers_PassesModifiers()
    {
        var keyboard = new MockKeyboard();
        KeyEventArgs? receivedArgs = null;
        keyboard.OnKeyUp += args => receivedArgs = args;

        keyboard.SimulateKeyUp(Key.A, KeyModifiers.Alt);

        Assert.NotNull(receivedArgs);
        Assert.Equal(KeyModifiers.Alt, receivedArgs.Value.Modifiers);
    }

    [Fact]
    public void SimulateKeyUp_ShiftKey_ClearsModifier()
    {
        var keyboard = new MockKeyboard();
        keyboard.SimulateKeyDown(Key.LeftShift);
        keyboard.OnKeyUp += _ => { };

        keyboard.SimulateKeyUp(Key.LeftShift);

        Assert.False(keyboard.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    #endregion

    #region SimulateTextInput

    [Fact]
    public void SimulateTextInput_Char_FiresOnTextInputEvent()
    {
        var keyboard = new MockKeyboard();
        char? receivedChar = null;
        keyboard.OnTextInput += c => receivedChar = c;

        keyboard.SimulateTextInput('A');

        Assert.Equal('A', receivedChar);
    }

    [Fact]
    public void SimulateTextInput_String_FiresMultipleEvents()
    {
        var keyboard = new MockKeyboard();
        var receivedChars = new List<char>();
        keyboard.OnTextInput += c => receivedChars.Add(c);

        keyboard.SimulateTextInput("Hello");

        Assert.Equal(['H', 'e', 'l', 'l', 'o'], receivedChars);
    }

    #endregion

    #region GetState

    [Fact]
    public void GetState_ReturnsCurrentState()
    {
        var keyboard = new MockKeyboard();
        keyboard.SetKeyDown(Key.W);
        keyboard.SetKeyDown(Key.A);
        keyboard.SetKeyDown(Key.LeftShift);

        var state = keyboard.GetState();

        Assert.Contains(Key.W, state.PressedKeys);
        Assert.Contains(Key.A, state.PressedKeys);
        Assert.Contains(Key.LeftShift, state.PressedKeys);
        Assert.True(state.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    #endregion
}
