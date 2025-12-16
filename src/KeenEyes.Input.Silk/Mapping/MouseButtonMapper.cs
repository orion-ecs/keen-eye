using KeenEyes.Input.Abstractions;
using SilkInput = Silk.NET.Input;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Maps between KeenEyes mouse buttons and Silk.NET mouse buttons.
/// </summary>
internal static class MouseButtonMapper
{
    /// <summary>
    /// Converts a KeenEyes mouse button to a Silk.NET mouse button.
    /// </summary>
    public static SilkInput.MouseButton ToSilkButton(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => SilkInput.MouseButton.Left,
            MouseButton.Right => SilkInput.MouseButton.Right,
            MouseButton.Middle => SilkInput.MouseButton.Middle,
            MouseButton.Button4 => SilkInput.MouseButton.Button4,
            MouseButton.Button5 => SilkInput.MouseButton.Button5,
            _ => SilkInput.MouseButton.Unknown
        };
    }

    /// <summary>
    /// Converts a Silk.NET mouse button to a KeenEyes mouse button.
    /// </summary>
    public static MouseButton? FromSilkButton(SilkInput.MouseButton silkButton)
    {
        return silkButton switch
        {
            SilkInput.MouseButton.Left => MouseButton.Left,
            SilkInput.MouseButton.Right => MouseButton.Right,
            SilkInput.MouseButton.Middle => MouseButton.Middle,
            SilkInput.MouseButton.Button4 => MouseButton.Button4,
            SilkInput.MouseButton.Button5 => MouseButton.Button5,
            _ => null
        };
    }
}
