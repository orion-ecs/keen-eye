using System.Collections.Immutable;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Testing.Input;

/// <summary>
/// A mock keyboard implementation for testing input-dependent systems.
/// </summary>
/// <remarks>
/// <para>
/// MockKeyboard provides two categories of methods:
/// </para>
/// <list type="bullet">
/// <item><b>State methods</b> (SetKeyDown, SetKeyUp): Change state without firing events.
/// Use these for polling-based input tests.</item>
/// <item><b>Simulate methods</b> (SimulateKeyDown, SimulateKeyUp): Change state AND fire events.
/// Use these for event-driven input tests.</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var keyboard = new MockKeyboard();
///
/// // For polling-based tests
/// keyboard.SetKeyDown(Key.W);
/// Assert.True(keyboard.IsKeyDown(Key.W));
///
/// // For event-driven tests
/// bool eventFired = false;
/// keyboard.OnKeyDown += args => eventFired = true;
/// keyboard.SimulateKeyDown(Key.Space);
/// Assert.True(eventFired);
/// </code>
/// </example>
public sealed class MockKeyboard : IKeyboard
{
    private readonly HashSet<Key> keysDown = [];
    private KeyModifiers modifiers = KeyModifiers.None;

    #region State Control Methods

    /// <summary>
    /// Sets a key as pressed without firing events.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <remarks>
    /// Use this for polling-based input tests where you want to set state
    /// without triggering event handlers.
    /// </remarks>
    public void SetKeyDown(Key key)
    {
        keysDown.Add(key);
        UpdateModifiersFromKey(key, true);
    }

    /// <summary>
    /// Sets a key as released without firing events.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <remarks>
    /// Use this for polling-based input tests where you want to set state
    /// without triggering event handlers.
    /// </remarks>
    public void SetKeyUp(Key key)
    {
        keysDown.Remove(key);
        UpdateModifiersFromKey(key, false);
    }

    /// <summary>
    /// Sets the modifier key state directly.
    /// </summary>
    /// <param name="modifiers">The new modifier state.</param>
    public void SetModifiers(KeyModifiers modifiers)
    {
        this.modifiers = modifiers;
    }

    /// <summary>
    /// Releases all keys and clears modifier state.
    /// </summary>
    public void ClearAllKeys()
    {
        keysDown.Clear();
        modifiers = KeyModifiers.None;
    }

    #endregion

    #region Event Simulation Methods

    /// <summary>
    /// Simulates pressing a key, updating state and firing the OnKeyDown event.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">Optional modifier keys held during the press.</param>
    /// <param name="isRepeat">Whether this is a key repeat event.</param>
    public void SimulateKeyDown(Key key, KeyModifiers modifiers = KeyModifiers.None, bool isRepeat = false)
    {
        keysDown.Add(key);
        this.modifiers = modifiers;
        UpdateModifiersFromKey(key, true);
        OnKeyDown?.Invoke(new KeyEventArgs(key, this.modifiers, isRepeat));
    }

    /// <summary>
    /// Simulates releasing a key, updating state and firing the OnKeyUp event.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <param name="modifiers">Optional modifier keys held during the release.</param>
    public void SimulateKeyUp(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        keysDown.Remove(key);
        this.modifiers = modifiers;
        UpdateModifiersFromKey(key, false);
        OnKeyUp?.Invoke(new KeyEventArgs(key, this.modifiers, IsRepeat: false));
    }

    /// <summary>
    /// Simulates text input, firing the OnTextInput event.
    /// </summary>
    /// <param name="character">The character that was typed.</param>
    /// <remarks>
    /// Use this for testing text input fields. The character represents
    /// the final Unicode character after keyboard layout translation.
    /// </remarks>
    public void SimulateTextInput(char character)
    {
        OnTextInput?.Invoke(character);
    }

    /// <summary>
    /// Simulates typing a string of text, firing OnTextInput for each character.
    /// </summary>
    /// <param name="text">The text to type.</param>
    public void SimulateTextInput(string text)
    {
        foreach (var character in text)
        {
            SimulateTextInput(character);
        }
    }

    #endregion

    #region IKeyboard Implementation

    /// <inheritdoc />
    public KeyboardState GetState() => new([.. keysDown], modifiers);

    /// <inheritdoc />
    public bool IsKeyDown(Key key) => keysDown.Contains(key);

    /// <inheritdoc />
    public bool IsKeyUp(Key key) => !keysDown.Contains(key);

    /// <inheritdoc />
    public KeyModifiers Modifiers => modifiers;

    /// <inheritdoc />
    public event Action<KeyEventArgs>? OnKeyDown;

    /// <inheritdoc />
    public event Action<KeyEventArgs>? OnKeyUp;

    /// <inheritdoc />
    public event Action<char>? OnTextInput;

    #endregion

    #region Private Helpers

    private void UpdateModifiersFromKey(Key key, bool isDown)
    {
        var modifier = key switch
        {
            Key.LeftShift or Key.RightShift => KeyModifiers.Shift,
            Key.LeftControl or Key.RightControl => KeyModifiers.Control,
            Key.LeftAlt or Key.RightAlt => KeyModifiers.Alt,
            Key.LeftSuper or Key.RightSuper => KeyModifiers.Super,
            Key.CapsLock => KeyModifiers.CapsLock,
            Key.NumLock => KeyModifiers.NumLock,
            _ => KeyModifiers.None
        };

        if (modifier != KeyModifiers.None)
        {
            if (isDown)
            {
                modifiers |= modifier;
            }
            else
            {
                modifiers &= ~modifier;
            }
        }
    }

    #endregion
}
