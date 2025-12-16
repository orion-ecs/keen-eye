namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Keyboard modifier key flags indicating which modifier keys are pressed.
/// </summary>
/// <remarks>
/// This flags enum allows combining multiple modifiers, such as Ctrl+Shift.
/// </remarks>
[Flags]
public enum KeyModifiers
{
    /// <summary>No modifier keys are pressed.</summary>
    None = 0,

    /// <summary>Either Shift key is pressed.</summary>
    Shift = 1,

    /// <summary>Either Control key is pressed.</summary>
    Control = 2,

    /// <summary>Either Alt key is pressed.</summary>
    Alt = 4,

    /// <summary>Either Super/Windows/Command key is pressed.</summary>
    Super = 8,

    /// <summary>Caps Lock is active.</summary>
    CapsLock = 16,

    /// <summary>Num Lock is active.</summary>
    NumLock = 32
}
