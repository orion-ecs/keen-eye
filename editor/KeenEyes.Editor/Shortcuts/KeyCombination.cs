using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Shortcuts;

/// <summary>
/// Represents a keyboard shortcut as a key plus optional modifier keys.
/// </summary>
/// <param name="Key">The primary key for this combination.</param>
/// <param name="Modifiers">The modifier keys that must be held.</param>
public readonly record struct KeyCombination(Key Key, KeyModifiers Modifiers = KeyModifiers.None)
{
    /// <summary>
    /// An empty key combination representing no shortcut.
    /// </summary>
    public static readonly KeyCombination None = new(Key.Unknown, KeyModifiers.None);

    /// <summary>
    /// Gets whether this is a valid key combination.
    /// </summary>
    public bool IsValid => Key != Key.Unknown;

    /// <summary>
    /// Parses a string representation of a key combination (e.g., "Ctrl+S", "Ctrl+Shift+N").
    /// </summary>
    /// <param name="shortcut">The string to parse.</param>
    /// <returns>The parsed key combination, or <see cref="None"/> if parsing fails.</returns>
    public static KeyCombination Parse(string shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
        {
            return None;
        }

        var parts = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return None;
        }

        var modifiers = KeyModifiers.None;
        Key key = Key.Unknown;

        foreach (var part in parts)
        {
            var upperPart = part.ToUpperInvariant();
            switch (upperPart)
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "SHIFT":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "ALT":
                    modifiers |= KeyModifiers.Alt;
                    break;
                case "SUPER":
                case "WIN":
                case "CMD":
                case "COMMAND":
                    modifiers |= KeyModifiers.Super;
                    break;
                default:
                    // Try to parse as a key
                    key = ParseKey(upperPart);
                    break;
            }
        }

        return new KeyCombination(key, modifiers);
    }

    private static Key ParseKey(string keyName)
    {
        // Handle special cases
        return keyName switch
        {
            "DEL" or "DELETE" => Key.Delete,
            "INS" or "INSERT" => Key.Insert,
            "ESC" or "ESCAPE" => Key.Escape,
            "ENTER" or "RETURN" => Key.Enter,
            "SPACE" or "SPACEBAR" => Key.Space,
            "TAB" => Key.Tab,
            "BACKSPACE" or "BACK" => Key.Backspace,
            "HOME" => Key.Home,
            "END" => Key.End,
            "PAGEUP" or "PGUP" => Key.PageUp,
            "PAGEDOWN" or "PGDN" => Key.PageDown,
            "UP" => Key.Up,
            "DOWN" => Key.Down,
            "LEFT" => Key.Left,
            "RIGHT" => Key.Right,

            // Function keys
            "F1" => Key.F1,
            "F2" => Key.F2,
            "F3" => Key.F3,
            "F4" => Key.F4,
            "F5" => Key.F5,
            "F6" => Key.F6,
            "F7" => Key.F7,
            "F8" => Key.F8,
            "F9" => Key.F9,
            "F10" => Key.F10,
            "F11" => Key.F11,
            "F12" => Key.F12,

            // Letters
            "A" => Key.A,
            "B" => Key.B,
            "C" => Key.C,
            "D" => Key.D,
            "E" => Key.E,
            "F" => Key.F,
            "G" => Key.G,
            "H" => Key.H,
            "I" => Key.I,
            "J" => Key.J,
            "K" => Key.K,
            "L" => Key.L,
            "M" => Key.M,
            "N" => Key.N,
            "O" => Key.O,
            "P" => Key.P,
            "Q" => Key.Q,
            "R" => Key.R,
            "S" => Key.S,
            "T" => Key.T,
            "U" => Key.U,
            "V" => Key.V,
            "W" => Key.W,
            "X" => Key.X,
            "Y" => Key.Y,
            "Z" => Key.Z,

            // Numbers
            "0" => Key.Number0,
            "1" => Key.Number1,
            "2" => Key.Number2,
            "3" => Key.Number3,
            "4" => Key.Number4,
            "5" => Key.Number5,
            "6" => Key.Number6,
            "7" => Key.Number7,
            "8" => Key.Number8,
            "9" => Key.Number9,

            // Numpad
            "NUMPAD0" or "KP0" => Key.Keypad0,
            "NUMPAD1" or "KP1" => Key.Keypad1,
            "NUMPAD2" or "KP2" => Key.Keypad2,
            "NUMPAD3" or "KP3" => Key.Keypad3,
            "NUMPAD4" or "KP4" => Key.Keypad4,
            "NUMPAD5" or "KP5" => Key.Keypad5,
            "NUMPAD6" or "KP6" => Key.Keypad6,
            "NUMPAD7" or "KP7" => Key.Keypad7,
            "NUMPAD8" or "KP8" => Key.Keypad8,
            "NUMPAD9" or "KP9" => Key.Keypad9,

            _ => Key.Unknown
        };
    }

    /// <summary>
    /// Checks if this key combination matches the given key event.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">The active modifiers.</param>
    /// <returns>True if this combination matches.</returns>
    public bool Matches(Key key, KeyModifiers modifiers)
    {
        if (Key != key)
        {
            return false;
        }

        // Only check the modifiers we care about (Ctrl, Shift, Alt, Super)
        var relevantModifiers = modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super);
        return relevantModifiers == Modifiers;
    }

    /// <summary>
    /// Returns a string representation of this key combination.
    /// </summary>
    public override string ToString()
    {
        if (!IsValid)
        {
            return string.Empty;
        }

        var parts = new List<string>(4);

        if (Modifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(KeyModifiers.Super))
        {
            parts.Add("Super");
        }

        parts.Add(KeyToString(Key));

        return string.Join("+", parts);
    }

    private static string KeyToString(Key key)
    {
        return key switch
        {
            Key.Delete => "Del",
            Key.Insert => "Ins",
            Key.Escape => "Esc",
            Key.Enter => "Enter",
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Backspace => "Backspace",
            Key.Number0 => "0",
            Key.Number1 => "1",
            Key.Number2 => "2",
            Key.Number3 => "3",
            Key.Number4 => "4",
            Key.Number5 => "5",
            Key.Number6 => "6",
            Key.Number7 => "7",
            Key.Number8 => "8",
            Key.Number9 => "9",
            Key.Keypad0 => "Numpad0",
            Key.Keypad1 => "Numpad1",
            Key.Keypad2 => "Numpad2",
            Key.Keypad3 => "Numpad3",
            Key.Keypad4 => "Numpad4",
            Key.Keypad5 => "Numpad5",
            Key.Keypad6 => "Numpad6",
            Key.Keypad7 => "Numpad7",
            Key.Keypad8 => "Numpad8",
            Key.Keypad9 => "Numpad9",
            _ => key.ToString()
        };
    }
}
