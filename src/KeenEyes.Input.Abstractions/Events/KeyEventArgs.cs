namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Event arguments for keyboard key events.
/// </summary>
/// <remarks>
/// Provides information about the key that triggered the event and
/// the state of modifier keys at the time of the event.
/// </remarks>
/// <param name="Key">The key that was pressed or released.</param>
/// <param name="Modifiers">The modifier keys that were held during the event.</param>
/// <param name="IsRepeat">Whether this is a repeated key press from holding the key.</param>
public readonly record struct KeyEventArgs(
    Key Key,
    KeyModifiers Modifiers,
    bool IsRepeat)
{
    /// <summary>
    /// Gets whether Shift was held during this event.
    /// </summary>
    public bool IsShiftDown => (Modifiers & KeyModifiers.Shift) != 0;

    /// <summary>
    /// Gets whether Control was held during this event.
    /// </summary>
    public bool IsControlDown => (Modifiers & KeyModifiers.Control) != 0;

    /// <summary>
    /// Gets whether Alt was held during this event.
    /// </summary>
    public bool IsAltDown => (Modifiers & KeyModifiers.Alt) != 0;

    /// <summary>
    /// Gets whether Super/Windows/Command was held during this event.
    /// </summary>
    public bool IsSuperDown => (Modifiers & KeyModifiers.Super) != 0;

    /// <summary>
    /// Creates event arguments for a non-repeating key event.
    /// </summary>
    /// <param name="key">The key that was pressed or released.</param>
    /// <param name="modifiers">The current modifier state.</param>
    /// <returns>A new <see cref="KeyEventArgs"/> instance.</returns>
    public static KeyEventArgs Create(Key key, KeyModifiers modifiers)
        => new(key, modifiers, IsRepeat: false);

    /// <inheritdoc />
    public override string ToString()
        => IsRepeat
            ? $"KeyEvent({Key}, {Modifiers}, Repeat)"
            : $"KeyEvent({Key}, {Modifiers})";
}
