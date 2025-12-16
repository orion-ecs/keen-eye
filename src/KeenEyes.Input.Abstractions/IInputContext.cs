using System.Collections.Immutable;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// The main entry point for input handling in KeenEyes.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IInputContext"/> provides a unified interface for accessing all input devices
/// and supports both polling-based state queries and event-based input notification.
/// </para>
/// <para>
/// <strong>Polling vs Events:</strong>
/// <list type="bullet">
/// <item>Use polling (<c>Keyboard.IsKeyDown</c>, <c>Mouse.Position</c>) for continuous input
/// that's checked every frame, such as character movement or camera control.</item>
/// <item>Use events (<c>OnKeyDown</c>, <c>OnMouseButtonDown</c>) for discrete actions that
/// shouldn't be missed, such as menu selection or ability activation.</item>
/// </list>
/// </para>
/// <para>
/// Call <see cref="Update"/> once per frame (typically at the start) to refresh device
/// states and process any pending input events.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Polling-based movement
/// if (input.Keyboard.IsKeyDown(Key.W))
///     MoveForward(deltaTime);
///
/// // Event-based action
/// input.Keyboard.OnKeyDown += args =>
/// {
///     if (args.Key == Key.Space)
///         Jump();
/// };
/// </code>
/// </example>
public interface IInputContext : IDisposable
{
    /// <summary>
    /// Gets the primary keyboard device.
    /// </summary>
    /// <remarks>
    /// Returns the first available keyboard. For systems with multiple keyboards,
    /// use <see cref="Keyboards"/> to access all connected devices.
    /// </remarks>
    IKeyboard Keyboard { get; }

    /// <summary>
    /// Gets the primary mouse device.
    /// </summary>
    /// <remarks>
    /// Returns the first available mouse. For systems with multiple mice,
    /// use <see cref="Mice"/> to access all connected devices.
    /// </remarks>
    IMouse Mouse { get; }

    /// <summary>
    /// Gets the primary gamepad device.
    /// </summary>
    /// <remarks>
    /// Returns the first connected gamepad (index 0), or a disconnected placeholder
    /// if no gamepad is connected. Check <see cref="IGamepad.IsConnected"/> before use.
    /// </remarks>
    IGamepad Gamepad { get; }

    /// <summary>
    /// Gets all connected keyboard devices.
    /// </summary>
    ImmutableArray<IKeyboard> Keyboards { get; }

    /// <summary>
    /// Gets all connected mouse devices.
    /// </summary>
    ImmutableArray<IMouse> Mice { get; }

    /// <summary>
    /// Gets all gamepad slots (connected and disconnected).
    /// </summary>
    /// <remarks>
    /// The array is typically fixed-size (e.g., 4 slots). Each slot maintains
    /// consistent index across connects/disconnects. Check <see cref="IGamepad.IsConnected"/>
    /// to determine if a gamepad is currently plugged in.
    /// </remarks>
    ImmutableArray<IGamepad> Gamepads { get; }

    /// <summary>
    /// Gets the number of currently connected gamepads.
    /// </summary>
    int ConnectedGamepadCount { get; }

    /// <summary>
    /// Updates all input device states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this once per frame, typically at the beginning of the update loop.
    /// This method polls hardware for new input and dispatches queued events.
    /// </para>
    /// <para>
    /// Event handlers registered on devices are invoked during this call.
    /// Ensure event handlers are registered before calling <see cref="Update"/>.
    /// </para>
    /// </remarks>
    void Update();

    #region Global Events

    /// <summary>
    /// Raised when any keyboard key is pressed.
    /// </summary>
    event Action<IKeyboard, KeyEventArgs>? OnKeyDown;

    /// <summary>
    /// Raised when any keyboard key is released.
    /// </summary>
    event Action<IKeyboard, KeyEventArgs>? OnKeyUp;

    /// <summary>
    /// Raised when text is typed on any keyboard.
    /// </summary>
    event Action<IKeyboard, char>? OnTextInput;

    /// <summary>
    /// Raised when any mouse button is pressed.
    /// </summary>
    event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonDown;

    /// <summary>
    /// Raised when any mouse button is released.
    /// </summary>
    event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonUp;

    /// <summary>
    /// Raised when any mouse moves.
    /// </summary>
    event Action<IMouse, MouseMoveEventArgs>? OnMouseMove;

    /// <summary>
    /// Raised when any mouse scrolls.
    /// </summary>
    event Action<IMouse, MouseScrollEventArgs>? OnMouseScroll;

    /// <summary>
    /// Raised when any gamepad button is pressed.
    /// </summary>
    event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonDown;

    /// <summary>
    /// Raised when any gamepad button is released.
    /// </summary>
    event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonUp;

    /// <summary>
    /// Raised when a gamepad connects.
    /// </summary>
    event Action<IGamepad>? OnGamepadConnected;

    /// <summary>
    /// Raised when a gamepad disconnects.
    /// </summary>
    event Action<IGamepad>? OnGamepadDisconnected;

    #endregion
}
