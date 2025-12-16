using KeenEyes.Input.Abstractions;
using SilkInput = Silk.NET.Input;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Maps between KeenEyes gamepad buttons and Silk.NET gamepad buttons.
/// </summary>
internal static class GamepadButtonMapper
{
    /// <summary>
    /// Converts a KeenEyes gamepad button to a Silk.NET button name.
    /// </summary>
    public static SilkInput.ButtonName? ToSilkButton(GamepadButton button)
    {
        return button switch
        {
            GamepadButton.South => SilkInput.ButtonName.A,
            GamepadButton.East => SilkInput.ButtonName.B,
            GamepadButton.West => SilkInput.ButtonName.X,
            GamepadButton.North => SilkInput.ButtonName.Y,
            GamepadButton.LeftShoulder => SilkInput.ButtonName.LeftBumper,
            GamepadButton.RightShoulder => SilkInput.ButtonName.RightBumper,
            GamepadButton.Back => SilkInput.ButtonName.Back,
            GamepadButton.Start => SilkInput.ButtonName.Start,
            GamepadButton.Guide => SilkInput.ButtonName.Home,
            GamepadButton.LeftStick => SilkInput.ButtonName.LeftStick,
            GamepadButton.RightStick => SilkInput.ButtonName.RightStick,
            GamepadButton.DPadUp => SilkInput.ButtonName.DPadUp,
            GamepadButton.DPadDown => SilkInput.ButtonName.DPadDown,
            GamepadButton.DPadLeft => SilkInput.ButtonName.DPadLeft,
            GamepadButton.DPadRight => SilkInput.ButtonName.DPadRight,
            _ => null
        };
    }

    /// <summary>
    /// Converts a Silk.NET button name to a KeenEyes gamepad button.
    /// </summary>
    public static GamepadButton? FromSilkButton(SilkInput.ButtonName silkButton)
    {
        return silkButton switch
        {
            SilkInput.ButtonName.A => GamepadButton.South,
            SilkInput.ButtonName.B => GamepadButton.East,
            SilkInput.ButtonName.X => GamepadButton.West,
            SilkInput.ButtonName.Y => GamepadButton.North,
            SilkInput.ButtonName.LeftBumper => GamepadButton.LeftShoulder,
            SilkInput.ButtonName.RightBumper => GamepadButton.RightShoulder,
            SilkInput.ButtonName.Back => GamepadButton.Back,
            SilkInput.ButtonName.Start => GamepadButton.Start,
            SilkInput.ButtonName.Home => GamepadButton.Guide,
            SilkInput.ButtonName.LeftStick => GamepadButton.LeftStick,
            SilkInput.ButtonName.RightStick => GamepadButton.RightStick,
            SilkInput.ButtonName.DPadUp => GamepadButton.DPadUp,
            SilkInput.ButtonName.DPadDown => GamepadButton.DPadDown,
            SilkInput.ButtonName.DPadLeft => GamepadButton.DPadLeft,
            SilkInput.ButtonName.DPadRight => GamepadButton.DPadRight,
            _ => null
        };
    }
}
