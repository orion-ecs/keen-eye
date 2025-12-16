using System.Numerics;
using KeenEyes.Input.Abstractions;
using SilkInput = Silk.NET.Input;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Silk.NET implementation of <see cref="IGamepad"/>.
/// </summary>
internal sealed class SilkGamepad : IGamepad
{
    private readonly SilkInput.IGamepad gamepad;
    private readonly float deadzone;
    private readonly Dictionary<GamepadAxis, float> previousAxisValues = [];

    /// <inheritdoc />
    public int Index => gamepad.Index;

    /// <inheritdoc />
    public string Name => gamepad.Name;

    /// <inheritdoc />
    public bool IsConnected => gamepad.IsConnected;

    /// <inheritdoc />
    public Vector2 LeftStick => ApplyDeadzone(GetThumbstick(0));

    /// <inheritdoc />
    public Vector2 RightStick => ApplyDeadzone(GetThumbstick(1));

    /// <inheritdoc />
    public float LeftTrigger => GetTriggerValue(0);

    /// <inheritdoc />
    public float RightTrigger => GetTriggerValue(1);

    /// <inheritdoc />
    public event Action<GamepadButtonEventArgs>? OnButtonDown;

    /// <inheritdoc />
    public event Action<GamepadButtonEventArgs>? OnButtonUp;

    /// <inheritdoc />
    public event Action<GamepadAxisEventArgs>? OnAxisChanged;

    /// <inheritdoc />
    public event Action<IGamepad>? OnConnected;

    /// <inheritdoc />
    public event Action<IGamepad>? OnDisconnected;

    internal SilkGamepad(SilkInput.IGamepad gamepad, float deadzone)
    {
        this.gamepad = gamepad;
        this.deadzone = deadzone;

        gamepad.ButtonDown += HandleButtonDown;
        gamepad.ButtonUp += HandleButtonUp;
        gamepad.ThumbstickMoved += HandleThumbstickMoved;
        gamepad.TriggerMoved += HandleTriggerMoved;
    }

    /// <inheritdoc />
    public bool IsButtonDown(GamepadButton button)
    {
        var silkButton = GamepadButtonMapper.ToSilkButton(button);
        return silkButton.HasValue && gamepad.Buttons.Any(b => b.Name == silkButton.Value && b.Pressed);
    }

    /// <inheritdoc />
    public bool IsButtonUp(GamepadButton button)
    {
        return !IsButtonDown(button);
    }

    /// <inheritdoc />
    public float GetAxis(GamepadAxis axis)
    {
        return axis switch
        {
            GamepadAxis.LeftStickX => LeftStick.X,
            GamepadAxis.LeftStickY => LeftStick.Y,
            GamepadAxis.RightStickX => RightStick.X,
            GamepadAxis.RightStickY => RightStick.Y,
            GamepadAxis.LeftTrigger => LeftTrigger,
            GamepadAxis.RightTrigger => RightTrigger,
            _ => 0f
        };
    }

    /// <inheritdoc />
    public GamepadState GetState()
    {
        var pressedButtons = GamepadButtons.None;

        if (IsButtonDown(GamepadButton.South))
        {
            pressedButtons |= GamepadButtons.South;
        }

        if (IsButtonDown(GamepadButton.East))
        {
            pressedButtons |= GamepadButtons.East;
        }

        if (IsButtonDown(GamepadButton.West))
        {
            pressedButtons |= GamepadButtons.West;
        }

        if (IsButtonDown(GamepadButton.North))
        {
            pressedButtons |= GamepadButtons.North;
        }

        if (IsButtonDown(GamepadButton.LeftShoulder))
        {
            pressedButtons |= GamepadButtons.LeftShoulder;
        }

        if (IsButtonDown(GamepadButton.RightShoulder))
        {
            pressedButtons |= GamepadButtons.RightShoulder;
        }

        if (IsButtonDown(GamepadButton.LeftTrigger))
        {
            pressedButtons |= GamepadButtons.LeftTrigger;
        }

        if (IsButtonDown(GamepadButton.RightTrigger))
        {
            pressedButtons |= GamepadButtons.RightTrigger;
        }

        if (IsButtonDown(GamepadButton.DPadUp))
        {
            pressedButtons |= GamepadButtons.DPadUp;
        }

        if (IsButtonDown(GamepadButton.DPadDown))
        {
            pressedButtons |= GamepadButtons.DPadDown;
        }

        if (IsButtonDown(GamepadButton.DPadLeft))
        {
            pressedButtons |= GamepadButtons.DPadLeft;
        }

        if (IsButtonDown(GamepadButton.DPadRight))
        {
            pressedButtons |= GamepadButtons.DPadRight;
        }

        if (IsButtonDown(GamepadButton.LeftStick))
        {
            pressedButtons |= GamepadButtons.LeftStick;
        }

        if (IsButtonDown(GamepadButton.RightStick))
        {
            pressedButtons |= GamepadButtons.RightStick;
        }

        if (IsButtonDown(GamepadButton.Start))
        {
            pressedButtons |= GamepadButtons.Start;
        }

        if (IsButtonDown(GamepadButton.Back))
        {
            pressedButtons |= GamepadButtons.Back;
        }

        if (IsButtonDown(GamepadButton.Guide))
        {
            pressedButtons |= GamepadButtons.Guide;
        }

        return new GamepadState(
            Index,
            IsConnected,
            pressedButtons,
            LeftStick,
            RightStick,
            LeftTrigger,
            RightTrigger);
    }

    /// <inheritdoc />
    public void SetVibration(float leftMotor, float rightMotor)
    {
        foreach (var motor in gamepad.VibrationMotors)
        {
            // Silk.NET doesn't distinguish left/right easily, so we average
            motor.Speed = (leftMotor + rightMotor) / 2f;
        }
    }

    /// <inheritdoc />
    public void StopVibration()
    {
        SetVibration(0f, 0f);
    }

    internal void RaiseConnected()
    {
        OnConnected?.Invoke(this);
    }

    internal void RaiseDisconnected()
    {
        OnDisconnected?.Invoke(this);
    }

    private Vector2 GetThumbstick(int index)
    {
        if (index >= gamepad.Thumbsticks.Count)
        {
            return Vector2.Zero;
        }

        var stick = gamepad.Thumbsticks[index];
        return new Vector2(stick.X, stick.Y);
    }

    private float GetTriggerValue(int index)
    {
        if (index >= gamepad.Triggers.Count)
        {
            return 0f;
        }

        return gamepad.Triggers[index].Position;
    }

    private Vector2 ApplyDeadzone(Vector2 value)
    {
        if (value.LengthSquared() < deadzone * deadzone)
        {
            return Vector2.Zero;
        }

        return value;
    }

    private void HandleButtonDown(SilkInput.IGamepad _, SilkInput.Button button)
    {
        var gamepadButton = GamepadButtonMapper.FromSilkButton(button.Name);
        if (gamepadButton.HasValue)
        {
            OnButtonDown?.Invoke(new GamepadButtonEventArgs(Index, gamepadButton.Value));
        }
    }

    private void HandleButtonUp(SilkInput.IGamepad _, SilkInput.Button button)
    {
        var gamepadButton = GamepadButtonMapper.FromSilkButton(button.Name);
        if (gamepadButton.HasValue)
        {
            OnButtonUp?.Invoke(new GamepadButtonEventArgs(Index, gamepadButton.Value));
        }
    }

    private void HandleThumbstickMoved(SilkInput.IGamepad _, SilkInput.Thumbstick thumbstick)
    {
        var xAxis = thumbstick.Index == 0 ? GamepadAxis.LeftStickX : GamepadAxis.RightStickX;
        var yAxis = thumbstick.Index == 0 ? GamepadAxis.LeftStickY : GamepadAxis.RightStickY;
        var value = ApplyDeadzone(new Vector2(thumbstick.X, thumbstick.Y));

        // Raise X axis event
        var previousX = previousAxisValues.GetValueOrDefault(xAxis, 0f);
        previousAxisValues[xAxis] = value.X;
        OnAxisChanged?.Invoke(new GamepadAxisEventArgs(Index, xAxis, value.X, previousX));

        // Raise Y axis event
        var previousY = previousAxisValues.GetValueOrDefault(yAxis, 0f);
        previousAxisValues[yAxis] = value.Y;
        OnAxisChanged?.Invoke(new GamepadAxisEventArgs(Index, yAxis, value.Y, previousY));
    }

    private void HandleTriggerMoved(SilkInput.IGamepad _, SilkInput.Trigger trigger)
    {
        var axis = trigger.Index == 0 ? GamepadAxis.LeftTrigger : GamepadAxis.RightTrigger;
        var previousValue = previousAxisValues.GetValueOrDefault(axis, 0f);
        previousAxisValues[axis] = trigger.Position;
        OnAxisChanged?.Invoke(new GamepadAxisEventArgs(Index, axis, trigger.Position, previousValue));
    }
}
