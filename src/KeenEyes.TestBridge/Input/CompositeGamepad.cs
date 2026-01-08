using System.Numerics;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.TestBridge.Input;

/// <summary>
/// A gamepad implementation that merges input from real and virtual sources.
/// </summary>
/// <remarks>
/// <para>
/// CompositeGamepad allows both real hardware input and virtual (TestBridge-injected)
/// input to work together. Button states return true if EITHER source has the button pressed.
/// Analog values use the greater magnitude between real and virtual input.
/// Events are forwarded from BOTH sources.
/// </para>
/// <para>
/// This enables hybrid testing scenarios where real user input and automated test
/// input can coexist.
/// </para>
/// </remarks>
internal sealed class CompositeGamepad : IGamepad
{
    private readonly IGamepad real;
    private readonly IGamepad virtual_;

    /// <summary>
    /// Creates a new composite gamepad merging real and virtual input.
    /// </summary>
    /// <param name="real">The real hardware gamepad.</param>
    /// <param name="virtual_">The virtual (mock) gamepad for TestBridge injection.</param>
    public CompositeGamepad(IGamepad real, IGamepad virtual_)
    {
        this.real = real;
        this.virtual_ = virtual_;

        // Forward events from both sources
        real.OnButtonDown += args => OnButtonDown?.Invoke(args);
        real.OnButtonUp += args => OnButtonUp?.Invoke(args);
        real.OnAxisChanged += args => OnAxisChanged?.Invoke(args);
        real.OnConnected += g => OnConnected?.Invoke(this);
        real.OnDisconnected += g => OnDisconnected?.Invoke(this);

        virtual_.OnButtonDown += args => OnButtonDown?.Invoke(args);
        virtual_.OnButtonUp += args => OnButtonUp?.Invoke(args);
        virtual_.OnAxisChanged += args => OnAxisChanged?.Invoke(args);
        virtual_.OnConnected += g => OnConnected?.Invoke(this);
        virtual_.OnDisconnected += g => OnDisconnected?.Invoke(this);
    }

    /// <inheritdoc />
    public int Index => real.Index;

    /// <inheritdoc />
    public bool IsConnected => real.IsConnected || virtual_.IsConnected;

    /// <inheritdoc />
    public string Name => real.Name;

    /// <inheritdoc />
    public GamepadState GetState()
    {
        var realState = real.GetState();
        var virtualState = virtual_.GetState();

        // Merge button states (OR them together)
        var mergedButtons = realState.PressedButtons | virtualState.PressedButtons;

        // Merge sticks - use the one with greater magnitude
        var leftStick = MergeSticks(realState.LeftStick, virtualState.LeftStick);
        var rightStick = MergeSticks(realState.RightStick, virtualState.RightStick);

        // Merge triggers - use the higher value
        var leftTrigger = Math.Max(realState.LeftTrigger, virtualState.LeftTrigger);
        var rightTrigger = Math.Max(realState.RightTrigger, virtualState.RightTrigger);

        return new GamepadState(
            Index,
            IsConnected,
            mergedButtons,
            leftStick,
            rightStick,
            leftTrigger,
            rightTrigger);
    }

    /// <inheritdoc />
    public bool IsButtonDown(GamepadButton button) => real.IsButtonDown(button) || virtual_.IsButtonDown(button);

    /// <inheritdoc />
    public bool IsButtonUp(GamepadButton button) => real.IsButtonUp(button) && virtual_.IsButtonUp(button);

    /// <inheritdoc />
    public float GetAxis(GamepadAxis axis)
    {
        var realValue = real.GetAxis(axis);
        var virtualValue = virtual_.GetAxis(axis);

        // For triggers, use max value
        if (axis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger)
        {
            return Math.Max(realValue, virtualValue);
        }

        // For sticks, use the one with greater magnitude
        return Math.Abs(virtualValue) > Math.Abs(realValue) ? virtualValue : realValue;
    }

    /// <inheritdoc />
    public Vector2 LeftStick => MergeSticks(real.LeftStick, virtual_.LeftStick);

    /// <inheritdoc />
    public Vector2 RightStick => MergeSticks(real.RightStick, virtual_.RightStick);

    /// <inheritdoc />
    public float LeftTrigger => Math.Max(real.LeftTrigger, virtual_.LeftTrigger);

    /// <inheritdoc />
    public float RightTrigger => Math.Max(real.RightTrigger, virtual_.RightTrigger);

    /// <inheritdoc />
    public void SetVibration(float leftMotor, float rightMotor)
    {
        // Vibration goes to real hardware only
        real.SetVibration(leftMotor, rightMotor);
    }

    /// <inheritdoc />
    public void StopVibration()
    {
        real.StopVibration();
    }

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

    private static Vector2 MergeSticks(Vector2 real, Vector2 virtual_)
    {
        // Use the stick with greater magnitude
        var realMag = real.LengthSquared();
        var virtualMag = virtual_.LengthSquared();
        return virtualMag > realMag ? virtual_ : real;
    }
}
