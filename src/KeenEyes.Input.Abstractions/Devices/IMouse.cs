using System.Numerics;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Represents a mouse input device.
/// </summary>
/// <remarks>
/// <para>
/// Provides both polling-based state queries and event-based input notification.
/// Polling is ideal for continuous input (camera look), while events are better
/// for discrete actions (button clicks).
/// </para>
/// <para>
/// Position coordinates are in window-relative pixels, with (0, 0) at the top-left.
/// </para>
/// </remarks>
public interface IMouse
{
    /// <summary>
    /// Gets the current mouse state snapshot.
    /// </summary>
    /// <returns>A snapshot of the current mouse state.</returns>
    MouseState GetState();

    /// <summary>
    /// Gets the current mouse position in window coordinates.
    /// </summary>
    Vector2 Position { get; }

    /// <summary>
    /// Gets whether the specified button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is pressed; otherwise, <c>false</c>.</returns>
    bool IsButtonDown(MouseButton button);

    /// <summary>
    /// Gets whether the specified button is currently released.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is released; otherwise, <c>false</c>.</returns>
    bool IsButtonUp(MouseButton button);

    /// <summary>
    /// Gets or sets whether the mouse cursor is visible.
    /// </summary>
    /// <remarks>
    /// Set to <c>false</c> to hide the cursor for first-person camera control.
    /// </remarks>
    bool IsCursorVisible { get; set; }

    /// <summary>
    /// Gets or sets whether the mouse cursor is captured/grabbed.
    /// </summary>
    /// <remarks>
    /// When captured, the cursor cannot leave the window and raw mouse
    /// motion is typically used for input (ideal for FPS camera control).
    /// </remarks>
    bool IsCursorCaptured { get; set; }

    /// <summary>
    /// Sets the mouse cursor position in window coordinates.
    /// </summary>
    /// <param name="position">The new cursor position.</param>
    void SetPosition(Vector2 position);

    #region Events

    /// <summary>
    /// Raised when a mouse button is pressed.
    /// </summary>
    event Action<MouseButtonEventArgs>? OnButtonDown;

    /// <summary>
    /// Raised when a mouse button is released.
    /// </summary>
    event Action<MouseButtonEventArgs>? OnButtonUp;

    /// <summary>
    /// Raised when the mouse cursor moves.
    /// </summary>
    /// <remarks>
    /// The event provides the new position and the delta from the previous position.
    /// </remarks>
    event Action<MouseMoveEventArgs>? OnMove;

    /// <summary>
    /// Raised when the scroll wheel moves.
    /// </summary>
    /// <remarks>
    /// The delta is typically in "notches" where positive Y is scroll up.
    /// Some mice also support horizontal scrolling (positive X is scroll right).
    /// </remarks>
    event Action<MouseScrollEventArgs>? OnScroll;

    /// <summary>
    /// Raised when the mouse cursor enters the window.
    /// </summary>
    event Action? OnEnter;

    /// <summary>
    /// Raised when the mouse cursor leaves the window.
    /// </summary>
    event Action? OnLeave;

    #endregion
}
