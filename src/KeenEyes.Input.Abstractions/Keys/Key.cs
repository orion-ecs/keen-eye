namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Keyboard key codes representing physical keys on a standard keyboard.
/// </summary>
/// <remarks>
/// Values are designed to be backend-agnostic and map to common key layouts.
/// Backend implementations should translate platform-specific keycodes to these values.
/// </remarks>
public enum Key
{
    /// <summary>Unknown or unsupported key.</summary>
    Unknown = 0,

    // Letters (A-Z)
    /// <summary>The A key.</summary>
    A = 1,
    /// <summary>The B key.</summary>
    B = 2,
    /// <summary>The C key.</summary>
    C = 3,
    /// <summary>The D key.</summary>
    D = 4,
    /// <summary>The E key.</summary>
    E = 5,
    /// <summary>The F key.</summary>
    F = 6,
    /// <summary>The G key.</summary>
    G = 7,
    /// <summary>The H key.</summary>
    H = 8,
    /// <summary>The I key.</summary>
    I = 9,
    /// <summary>The J key.</summary>
    J = 10,
    /// <summary>The K key.</summary>
    K = 11,
    /// <summary>The L key.</summary>
    L = 12,
    /// <summary>The M key.</summary>
    M = 13,
    /// <summary>The N key.</summary>
    N = 14,
    /// <summary>The O key.</summary>
    O = 15,
    /// <summary>The P key.</summary>
    P = 16,
    /// <summary>The Q key.</summary>
    Q = 17,
    /// <summary>The R key.</summary>
    R = 18,
    /// <summary>The S key.</summary>
    S = 19,
    /// <summary>The T key.</summary>
    T = 20,
    /// <summary>The U key.</summary>
    U = 21,
    /// <summary>The V key.</summary>
    V = 22,
    /// <summary>The W key.</summary>
    W = 23,
    /// <summary>The X key.</summary>
    X = 24,
    /// <summary>The Y key.</summary>
    Y = 25,
    /// <summary>The Z key.</summary>
    Z = 26,

    // Numbers (0-9 on main keyboard)
    /// <summary>The 0 key on the main keyboard.</summary>
    Number0 = 27,
    /// <summary>The 1 key on the main keyboard.</summary>
    Number1 = 28,
    /// <summary>The 2 key on the main keyboard.</summary>
    Number2 = 29,
    /// <summary>The 3 key on the main keyboard.</summary>
    Number3 = 30,
    /// <summary>The 4 key on the main keyboard.</summary>
    Number4 = 31,
    /// <summary>The 5 key on the main keyboard.</summary>
    Number5 = 32,
    /// <summary>The 6 key on the main keyboard.</summary>
    Number6 = 33,
    /// <summary>The 7 key on the main keyboard.</summary>
    Number7 = 34,
    /// <summary>The 8 key on the main keyboard.</summary>
    Number8 = 35,
    /// <summary>The 9 key on the main keyboard.</summary>
    Number9 = 36,

    // Function keys (F1-F12)
    /// <summary>The F1 function key.</summary>
    F1 = 37,
    /// <summary>The F2 function key.</summary>
    F2 = 38,
    /// <summary>The F3 function key.</summary>
    F3 = 39,
    /// <summary>The F4 function key.</summary>
    F4 = 40,
    /// <summary>The F5 function key.</summary>
    F5 = 41,
    /// <summary>The F6 function key.</summary>
    F6 = 42,
    /// <summary>The F7 function key.</summary>
    F7 = 43,
    /// <summary>The F8 function key.</summary>
    F8 = 44,
    /// <summary>The F9 function key.</summary>
    F9 = 45,
    /// <summary>The F10 function key.</summary>
    F10 = 46,
    /// <summary>The F11 function key.</summary>
    F11 = 47,
    /// <summary>The F12 function key.</summary>
    F12 = 48,

    // Numpad keys
    /// <summary>The 0 key on the numeric keypad.</summary>
    Keypad0 = 49,
    /// <summary>The 1 key on the numeric keypad.</summary>
    Keypad1 = 50,
    /// <summary>The 2 key on the numeric keypad.</summary>
    Keypad2 = 51,
    /// <summary>The 3 key on the numeric keypad.</summary>
    Keypad3 = 52,
    /// <summary>The 4 key on the numeric keypad.</summary>
    Keypad4 = 53,
    /// <summary>The 5 key on the numeric keypad.</summary>
    Keypad5 = 54,
    /// <summary>The 6 key on the numeric keypad.</summary>
    Keypad6 = 55,
    /// <summary>The 7 key on the numeric keypad.</summary>
    Keypad7 = 56,
    /// <summary>The 8 key on the numeric keypad.</summary>
    Keypad8 = 57,
    /// <summary>The 9 key on the numeric keypad.</summary>
    Keypad9 = 58,
    /// <summary>The decimal point key on the numeric keypad.</summary>
    KeypadDecimal = 59,
    /// <summary>The divide (/) key on the numeric keypad.</summary>
    KeypadDivide = 60,
    /// <summary>The multiply (*) key on the numeric keypad.</summary>
    KeypadMultiply = 61,
    /// <summary>The subtract (-) key on the numeric keypad.</summary>
    KeypadSubtract = 62,
    /// <summary>The add (+) key on the numeric keypad.</summary>
    KeypadAdd = 63,
    /// <summary>The Enter key on the numeric keypad.</summary>
    KeypadEnter = 64,

    // Modifier keys
    /// <summary>The left Shift key.</summary>
    LeftShift = 65,
    /// <summary>The right Shift key.</summary>
    RightShift = 66,
    /// <summary>The left Control key.</summary>
    LeftControl = 67,
    /// <summary>The right Control key.</summary>
    RightControl = 68,
    /// <summary>The left Alt key.</summary>
    LeftAlt = 69,
    /// <summary>The right Alt key.</summary>
    RightAlt = 70,
    /// <summary>The left Super/Windows/Command key.</summary>
    LeftSuper = 71,
    /// <summary>The right Super/Windows/Command key.</summary>
    RightSuper = 72,

    // Navigation keys
    /// <summary>The up arrow key.</summary>
    Up = 73,
    /// <summary>The down arrow key.</summary>
    Down = 74,
    /// <summary>The left arrow key.</summary>
    Left = 75,
    /// <summary>The right arrow key.</summary>
    Right = 76,
    /// <summary>The Home key.</summary>
    Home = 77,
    /// <summary>The End key.</summary>
    End = 78,
    /// <summary>The Page Up key.</summary>
    PageUp = 79,
    /// <summary>The Page Down key.</summary>
    PageDown = 80,
    /// <summary>The Insert key.</summary>
    Insert = 81,
    /// <summary>The Delete key.</summary>
    Delete = 82,

    // Common keys
    /// <summary>The Space bar.</summary>
    Space = 83,
    /// <summary>The Enter/Return key.</summary>
    Enter = 84,
    /// <summary>The Escape key.</summary>
    Escape = 85,
    /// <summary>The Tab key.</summary>
    Tab = 86,
    /// <summary>The Backspace key.</summary>
    Backspace = 87,
    /// <summary>The Caps Lock key.</summary>
    CapsLock = 88,
    /// <summary>The Num Lock key.</summary>
    NumLock = 89,
    /// <summary>The Scroll Lock key.</summary>
    ScrollLock = 90,
    /// <summary>The Print Screen key.</summary>
    PrintScreen = 91,
    /// <summary>The Pause/Break key.</summary>
    Pause = 92,
    /// <summary>The Menu/Application key.</summary>
    Menu = 93,

    // Punctuation and symbols
    /// <summary>The grave accent/backtick (`) key.</summary>
    GraveAccent = 94,
    /// <summary>The minus (-) key on the main keyboard.</summary>
    Minus = 95,
    /// <summary>The equals (=) key on the main keyboard.</summary>
    Equal = 96,
    /// <summary>The left bracket ([) key.</summary>
    LeftBracket = 97,
    /// <summary>The right bracket (]) key.</summary>
    RightBracket = 98,
    /// <summary>The backslash (\) key.</summary>
    Backslash = 99,
    /// <summary>The semicolon (;) key.</summary>
    Semicolon = 100,
    /// <summary>The apostrophe (') key.</summary>
    Apostrophe = 101,
    /// <summary>The comma (,) key.</summary>
    Comma = 102,
    /// <summary>The period (.) key.</summary>
    Period = 103,
    /// <summary>The forward slash (/) key.</summary>
    Slash = 104
}
