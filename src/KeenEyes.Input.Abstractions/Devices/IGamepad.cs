using System.Numerics;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Represents a gamepad/controller input device.
/// </summary>
/// <remarks>
/// <para>
/// Provides both polling-based state queries and event-based input notification.
/// Polling is ideal for continuous input (movement, camera), while events are better
/// for discrete actions (menu selection).
/// </para>
/// <para>
/// Stick values range from -1.0 to 1.0, with (0, 0) being the center/neutral position.
/// Trigger values range from 0.0 (released) to 1.0 (fully pressed).
/// </para>
/// </remarks>
public interface IGamepad
{
    /// <summary>
    /// Gets the gamepad index (0-based).
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets whether this gamepad is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the display name of the gamepad (e.g., "Xbox Controller").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the current gamepad state snapshot.
    /// </summary>
    /// <returns>A snapshot of the current gamepad state.</returns>
    GamepadState GetState();

    /// <summary>
    /// Gets whether the specified button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is pressed; otherwise, <c>false</c>.</returns>
    bool IsButtonDown(GamepadButton button);

    /// <summary>
    /// Gets whether the specified button is currently released.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is released; otherwise, <c>false</c>.</returns>
    bool IsButtonUp(GamepadButton button);

    /// <summary>
    /// Gets the current value of the specified axis.
    /// </summary>
    /// <param name="axis">The axis to query.</param>
    /// <returns>The axis value (-1.0 to 1.0 for sticks, 0.0 to 1.0 for triggers).</returns>
    float GetAxis(GamepadAxis axis);

    /// <summary>
    /// Gets the left analog stick position.
    /// </summary>
    Vector2 LeftStick { get; }

    /// <summary>
    /// Gets the right analog stick position.
    /// </summary>
    Vector2 RightStick { get; }

    /// <summary>
    /// Gets the left trigger value (0.0 to 1.0).
    /// </summary>
    float LeftTrigger { get; }

    /// <summary>
    /// Gets the right trigger value (0.0 to 1.0).
    /// </summary>
    float RightTrigger { get; }

    /// <summary>
    /// Sets the vibration intensity for the gamepad motors.
    /// </summary>
    /// <param name="leftMotor">Left motor intensity (0.0 to 1.0).</param>
    /// <param name="rightMotor">Right motor intensity (0.0 to 1.0).</param>
    /// <remarks>
    /// The left motor is typically the low-frequency rumble motor,
    /// while the right motor is the high-frequency motor.
    /// </remarks>
    void SetVibration(float leftMotor, float rightMotor);

    /// <summary>
    /// Stops all vibration.
    /// </summary>
    void StopVibration();

    #region Events

    /// <summary>
    /// Raised when a button is pressed.
    /// </summary>
    event Action<GamepadButtonEventArgs>? OnButtonDown;

    /// <summary>
    /// Raised when a button is released.
    /// </summary>
    event Action<GamepadButtonEventArgs>? OnButtonUp;

    /// <summary>
    /// Raised when an axis value changes significantly.
    /// </summary>
    /// <remarks>
    /// Implementations may apply a deadzone filter to reduce noise.
    /// </remarks>
    event Action<GamepadAxisEventArgs>? OnAxisChanged;

    /// <summary>
    /// Raised when the gamepad is connected.
    /// </summary>
    event Action<IGamepad>? OnConnected;

    /// <summary>
    /// Raised when the gamepad is disconnected.
    /// </summary>
    event Action<IGamepad>? OnDisconnected;

    #endregion
}
