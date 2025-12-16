using KeenEyes.Input.Abstractions;
using SilkInput = Silk.NET.Input;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Maps between KeenEyes key codes and Silk.NET key codes.
/// </summary>
internal static class KeyMapper
{
    /// <summary>
    /// Converts a KeenEyes key to a Silk.NET key.
    /// </summary>
    public static SilkInput.Key ToSilkKey(Key key)
    {
        return key switch
        {
            // Letters
            Key.A => SilkInput.Key.A,
            Key.B => SilkInput.Key.B,
            Key.C => SilkInput.Key.C,
            Key.D => SilkInput.Key.D,
            Key.E => SilkInput.Key.E,
            Key.F => SilkInput.Key.F,
            Key.G => SilkInput.Key.G,
            Key.H => SilkInput.Key.H,
            Key.I => SilkInput.Key.I,
            Key.J => SilkInput.Key.J,
            Key.K => SilkInput.Key.K,
            Key.L => SilkInput.Key.L,
            Key.M => SilkInput.Key.M,
            Key.N => SilkInput.Key.N,
            Key.O => SilkInput.Key.O,
            Key.P => SilkInput.Key.P,
            Key.Q => SilkInput.Key.Q,
            Key.R => SilkInput.Key.R,
            Key.S => SilkInput.Key.S,
            Key.T => SilkInput.Key.T,
            Key.U => SilkInput.Key.U,
            Key.V => SilkInput.Key.V,
            Key.W => SilkInput.Key.W,
            Key.X => SilkInput.Key.X,
            Key.Y => SilkInput.Key.Y,
            Key.Z => SilkInput.Key.Z,

            // Numbers
            Key.Number0 => SilkInput.Key.Number0,
            Key.Number1 => SilkInput.Key.Number1,
            Key.Number2 => SilkInput.Key.Number2,
            Key.Number3 => SilkInput.Key.Number3,
            Key.Number4 => SilkInput.Key.Number4,
            Key.Number5 => SilkInput.Key.Number5,
            Key.Number6 => SilkInput.Key.Number6,
            Key.Number7 => SilkInput.Key.Number7,
            Key.Number8 => SilkInput.Key.Number8,
            Key.Number9 => SilkInput.Key.Number9,

            // Function keys
            Key.F1 => SilkInput.Key.F1,
            Key.F2 => SilkInput.Key.F2,
            Key.F3 => SilkInput.Key.F3,
            Key.F4 => SilkInput.Key.F4,
            Key.F5 => SilkInput.Key.F5,
            Key.F6 => SilkInput.Key.F6,
            Key.F7 => SilkInput.Key.F7,
            Key.F8 => SilkInput.Key.F8,
            Key.F9 => SilkInput.Key.F9,
            Key.F10 => SilkInput.Key.F10,
            Key.F11 => SilkInput.Key.F11,
            Key.F12 => SilkInput.Key.F12,

            // Arrow keys
            Key.Up => SilkInput.Key.Up,
            Key.Down => SilkInput.Key.Down,
            Key.Left => SilkInput.Key.Left,
            Key.Right => SilkInput.Key.Right,

            // Modifiers
            Key.LeftShift => SilkInput.Key.ShiftLeft,
            Key.RightShift => SilkInput.Key.ShiftRight,
            Key.LeftControl => SilkInput.Key.ControlLeft,
            Key.RightControl => SilkInput.Key.ControlRight,
            Key.LeftAlt => SilkInput.Key.AltLeft,
            Key.RightAlt => SilkInput.Key.AltRight,
            Key.LeftSuper => SilkInput.Key.SuperLeft,
            Key.RightSuper => SilkInput.Key.SuperRight,

            // Special keys
            Key.Space => SilkInput.Key.Space,
            Key.Enter => SilkInput.Key.Enter,
            Key.Escape => SilkInput.Key.Escape,
            Key.Tab => SilkInput.Key.Tab,
            Key.Backspace => SilkInput.Key.Backspace,
            Key.Delete => SilkInput.Key.Delete,
            Key.Insert => SilkInput.Key.Insert,
            Key.Home => SilkInput.Key.Home,
            Key.End => SilkInput.Key.End,
            Key.PageUp => SilkInput.Key.PageUp,
            Key.PageDown => SilkInput.Key.PageDown,
            Key.CapsLock => SilkInput.Key.CapsLock,

            // Punctuation
            Key.Comma => SilkInput.Key.Comma,
            Key.Period => SilkInput.Key.Period,
            Key.Slash => SilkInput.Key.Slash,
            Key.Semicolon => SilkInput.Key.Semicolon,
            Key.Apostrophe => SilkInput.Key.Apostrophe,
            Key.LeftBracket => SilkInput.Key.LeftBracket,
            Key.RightBracket => SilkInput.Key.RightBracket,
            Key.Backslash => SilkInput.Key.BackSlash,
            Key.Minus => SilkInput.Key.Minus,
            Key.Equal => SilkInput.Key.Equal,
            Key.GraveAccent => SilkInput.Key.GraveAccent,

            // Numpad
            Key.Keypad0 => SilkInput.Key.Keypad0,
            Key.Keypad1 => SilkInput.Key.Keypad1,
            Key.Keypad2 => SilkInput.Key.Keypad2,
            Key.Keypad3 => SilkInput.Key.Keypad3,
            Key.Keypad4 => SilkInput.Key.Keypad4,
            Key.Keypad5 => SilkInput.Key.Keypad5,
            Key.Keypad6 => SilkInput.Key.Keypad6,
            Key.Keypad7 => SilkInput.Key.Keypad7,
            Key.Keypad8 => SilkInput.Key.Keypad8,
            Key.Keypad9 => SilkInput.Key.Keypad9,
            Key.KeypadDecimal => SilkInput.Key.KeypadDecimal,
            Key.KeypadEnter => SilkInput.Key.KeypadEnter,
            Key.KeypadAdd => SilkInput.Key.KeypadAdd,
            Key.KeypadSubtract => SilkInput.Key.KeypadSubtract,
            Key.KeypadMultiply => SilkInput.Key.KeypadMultiply,
            Key.KeypadDivide => SilkInput.Key.KeypadDivide,

            _ => SilkInput.Key.Unknown
        };
    }

    /// <summary>
    /// Converts a Silk.NET key to a KeenEyes key.
    /// </summary>
    public static Key? FromSilkKey(SilkInput.Key silkKey)
    {
        return silkKey switch
        {
            // Letters
            SilkInput.Key.A => Key.A,
            SilkInput.Key.B => Key.B,
            SilkInput.Key.C => Key.C,
            SilkInput.Key.D => Key.D,
            SilkInput.Key.E => Key.E,
            SilkInput.Key.F => Key.F,
            SilkInput.Key.G => Key.G,
            SilkInput.Key.H => Key.H,
            SilkInput.Key.I => Key.I,
            SilkInput.Key.J => Key.J,
            SilkInput.Key.K => Key.K,
            SilkInput.Key.L => Key.L,
            SilkInput.Key.M => Key.M,
            SilkInput.Key.N => Key.N,
            SilkInput.Key.O => Key.O,
            SilkInput.Key.P => Key.P,
            SilkInput.Key.Q => Key.Q,
            SilkInput.Key.R => Key.R,
            SilkInput.Key.S => Key.S,
            SilkInput.Key.T => Key.T,
            SilkInput.Key.U => Key.U,
            SilkInput.Key.V => Key.V,
            SilkInput.Key.W => Key.W,
            SilkInput.Key.X => Key.X,
            SilkInput.Key.Y => Key.Y,
            SilkInput.Key.Z => Key.Z,

            // Numbers
            SilkInput.Key.Number0 => Key.Number0,
            SilkInput.Key.Number1 => Key.Number1,
            SilkInput.Key.Number2 => Key.Number2,
            SilkInput.Key.Number3 => Key.Number3,
            SilkInput.Key.Number4 => Key.Number4,
            SilkInput.Key.Number5 => Key.Number5,
            SilkInput.Key.Number6 => Key.Number6,
            SilkInput.Key.Number7 => Key.Number7,
            SilkInput.Key.Number8 => Key.Number8,
            SilkInput.Key.Number9 => Key.Number9,

            // Function keys
            SilkInput.Key.F1 => Key.F1,
            SilkInput.Key.F2 => Key.F2,
            SilkInput.Key.F3 => Key.F3,
            SilkInput.Key.F4 => Key.F4,
            SilkInput.Key.F5 => Key.F5,
            SilkInput.Key.F6 => Key.F6,
            SilkInput.Key.F7 => Key.F7,
            SilkInput.Key.F8 => Key.F8,
            SilkInput.Key.F9 => Key.F9,
            SilkInput.Key.F10 => Key.F10,
            SilkInput.Key.F11 => Key.F11,
            SilkInput.Key.F12 => Key.F12,

            // Arrow keys
            SilkInput.Key.Up => Key.Up,
            SilkInput.Key.Down => Key.Down,
            SilkInput.Key.Left => Key.Left,
            SilkInput.Key.Right => Key.Right,

            // Modifiers
            SilkInput.Key.ShiftLeft => Key.LeftShift,
            SilkInput.Key.ShiftRight => Key.RightShift,
            SilkInput.Key.ControlLeft => Key.LeftControl,
            SilkInput.Key.ControlRight => Key.RightControl,
            SilkInput.Key.AltLeft => Key.LeftAlt,
            SilkInput.Key.AltRight => Key.RightAlt,
            SilkInput.Key.SuperLeft => Key.LeftSuper,
            SilkInput.Key.SuperRight => Key.RightSuper,

            // Special keys
            SilkInput.Key.Space => Key.Space,
            SilkInput.Key.Enter => Key.Enter,
            SilkInput.Key.Escape => Key.Escape,
            SilkInput.Key.Tab => Key.Tab,
            SilkInput.Key.Backspace => Key.Backspace,
            SilkInput.Key.Delete => Key.Delete,
            SilkInput.Key.Insert => Key.Insert,
            SilkInput.Key.Home => Key.Home,
            SilkInput.Key.End => Key.End,
            SilkInput.Key.PageUp => Key.PageUp,
            SilkInput.Key.PageDown => Key.PageDown,
            SilkInput.Key.CapsLock => Key.CapsLock,

            // Punctuation
            SilkInput.Key.Comma => Key.Comma,
            SilkInput.Key.Period => Key.Period,
            SilkInput.Key.Slash => Key.Slash,
            SilkInput.Key.Semicolon => Key.Semicolon,
            SilkInput.Key.Apostrophe => Key.Apostrophe,
            SilkInput.Key.LeftBracket => Key.LeftBracket,
            SilkInput.Key.RightBracket => Key.RightBracket,
            SilkInput.Key.BackSlash => Key.Backslash,
            SilkInput.Key.Minus => Key.Minus,
            SilkInput.Key.Equal => Key.Equal,
            SilkInput.Key.GraveAccent => Key.GraveAccent,

            // Numpad
            SilkInput.Key.Keypad0 => Key.Keypad0,
            SilkInput.Key.Keypad1 => Key.Keypad1,
            SilkInput.Key.Keypad2 => Key.Keypad2,
            SilkInput.Key.Keypad3 => Key.Keypad3,
            SilkInput.Key.Keypad4 => Key.Keypad4,
            SilkInput.Key.Keypad5 => Key.Keypad5,
            SilkInput.Key.Keypad6 => Key.Keypad6,
            SilkInput.Key.Keypad7 => Key.Keypad7,
            SilkInput.Key.Keypad8 => Key.Keypad8,
            SilkInput.Key.Keypad9 => Key.Keypad9,
            SilkInput.Key.KeypadDecimal => Key.KeypadDecimal,
            SilkInput.Key.KeypadEnter => Key.KeypadEnter,
            SilkInput.Key.KeypadAdd => Key.KeypadAdd,
            SilkInput.Key.KeypadSubtract => Key.KeypadSubtract,
            SilkInput.Key.KeypadMultiply => Key.KeypadMultiply,
            SilkInput.Key.KeypadDivide => Key.KeypadDivide,

            _ => null
        };
    }
}
