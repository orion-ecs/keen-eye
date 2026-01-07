using KeenEyes.Input.Abstractions;

namespace KeenEyes.TestBridge.Input;

/// <summary>
/// Controller for injecting input events into the application.
/// </summary>
/// <remarks>
/// <para>
/// The input controller provides methods for simulating keyboard, mouse, and gamepad input.
/// Input events are injected into the application's input context and processed normally
/// by input systems.
/// </para>
/// <para>
/// Methods come in two flavors:
/// </para>
/// <list type="bullet">
/// <item><description>Set methods: Update state without firing events (instant state change)</description></item>
/// <item><description>Simulate methods: Update state and fire events (realistic user input)</description></item>
/// </list>
/// </remarks>
public interface IInputController
{
    #region Keyboard

    /// <summary>
    /// Simulates pressing a key down.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">Modifier keys held during the press.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task KeyDownAsync(Key key, KeyModifiers modifiers = KeyModifiers.None);

    /// <summary>
    /// Simulates releasing a key.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <param name="modifiers">Modifier keys held during the release.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task KeyUpAsync(Key key, KeyModifiers modifiers = KeyModifiers.None);

    /// <summary>
    /// Simulates a complete key press (down and up).
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">Modifier keys held during the press.</param>
    /// <param name="holdDuration">How long to hold the key. Defaults to one frame.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task KeyPressAsync(Key key, KeyModifiers modifiers = KeyModifiers.None, TimeSpan? holdDuration = null);

    /// <summary>
    /// Types a string of text as text input events.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="delayBetweenChars">Delay between each character. Defaults to no delay.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This fires text input events (like IME input), not individual key events.
    /// For key events, use <see cref="KeyPressAsync"/> for each character.
    /// </remarks>
    Task TypeTextAsync(string text, TimeSpan? delayBetweenChars = null);

    /// <summary>
    /// Gets whether a key is currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is pressed; otherwise, false.</returns>
    bool IsKeyDown(Key key);

    #endregion

    #region Mouse

    /// <summary>
    /// Moves the mouse to absolute coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MouseMoveAsync(float x, float y);

    /// <summary>
    /// Moves the mouse by a relative delta.
    /// </summary>
    /// <param name="deltaX">The X delta.</param>
    /// <param name="deltaY">The Y delta.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MouseMoveRelativeAsync(float deltaX, float deltaY);

    /// <summary>
    /// Simulates a mouse button down.
    /// </summary>
    /// <param name="button">The button to press.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MouseDownAsync(MouseButton button);

    /// <summary>
    /// Simulates a mouse button up.
    /// </summary>
    /// <param name="button">The button to release.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MouseUpAsync(MouseButton button);

    /// <summary>
    /// Simulates a complete mouse click at the specified position.
    /// </summary>
    /// <param name="x">The X coordinate to click at.</param>
    /// <param name="y">The Y coordinate to click at.</param>
    /// <param name="button">The button to click. Defaults to left button.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClickAsync(float x, float y, MouseButton button = MouseButton.Left);

    /// <summary>
    /// Simulates a double-click at the specified position.
    /// </summary>
    /// <param name="x">The X coordinate to click at.</param>
    /// <param name="y">The Y coordinate to click at.</param>
    /// <param name="button">The button to click. Defaults to left button.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DoubleClickAsync(float x, float y, MouseButton button = MouseButton.Left);

    /// <summary>
    /// Simulates a drag operation from one position to another.
    /// </summary>
    /// <param name="startX">The starting X coordinate.</param>
    /// <param name="startY">The starting Y coordinate.</param>
    /// <param name="endX">The ending X coordinate.</param>
    /// <param name="endY">The ending Y coordinate.</param>
    /// <param name="button">The button to hold during drag. Defaults to left button.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DragAsync(float startX, float startY, float endX, float endY, MouseButton button = MouseButton.Left);

    /// <summary>
    /// Simulates mouse wheel scrolling.
    /// </summary>
    /// <param name="deltaX">The horizontal scroll delta.</param>
    /// <param name="deltaY">The vertical scroll delta.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ScrollAsync(float deltaX, float deltaY);

    /// <summary>
    /// Gets the current mouse position.
    /// </summary>
    /// <returns>The mouse position as (X, Y) coordinates.</returns>
    (float X, float Y) GetMousePosition();

    /// <summary>
    /// Gets whether a mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is pressed; otherwise, false.</returns>
    bool IsMouseButtonDown(MouseButton button);

    #endregion

    #region Gamepad

    /// <summary>
    /// Simulates a gamepad button down.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index (0-based).</param>
    /// <param name="button">The button to press.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GamepadButtonDownAsync(int gamepadIndex, GamepadButton button);

    /// <summary>
    /// Simulates a gamepad button up.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index (0-based).</param>
    /// <param name="button">The button to release.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GamepadButtonUpAsync(int gamepadIndex, GamepadButton button);

    /// <summary>
    /// Sets the left analog stick position.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index (0-based).</param>
    /// <param name="x">X position (-1 to 1).</param>
    /// <param name="y">Y position (-1 to 1).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetLeftStickAsync(int gamepadIndex, float x, float y);

    /// <summary>
    /// Sets the right analog stick position.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index (0-based).</param>
    /// <param name="x">X position (-1 to 1).</param>
    /// <param name="y">Y position (-1 to 1).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetRightStickAsync(int gamepadIndex, float x, float y);

    /// <summary>
    /// Sets a trigger value.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index (0-based).</param>
    /// <param name="isLeft">True for left trigger, false for right trigger.</param>
    /// <param name="value">Trigger value (0 to 1).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetTriggerAsync(int gamepadIndex, bool isLeft, float value);

    /// <summary>
    /// Sets the connection state of a gamepad.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index (0-based).</param>
    /// <param name="connected">Whether the gamepad should be connected.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetGamepadConnectedAsync(int gamepadIndex, bool connected);

    /// <summary>
    /// Gets whether a gamepad button is currently pressed.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index (0-based).</param>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is pressed; otherwise, false.</returns>
    bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button);

    /// <summary>
    /// Gets the number of gamepad slots available.
    /// </summary>
    int GamepadCount { get; }

    #endregion

    #region InputAction System

    /// <summary>
    /// Triggers an input action by name, regardless of bindings.
    /// </summary>
    /// <param name="actionName">The name of the action to trigger.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This bypasses normal input binding evaluation and directly triggers the action.
    /// Useful for testing action-based logic without setting up specific input states.
    /// </remarks>
    Task TriggerActionAsync(string actionName);

    /// <summary>
    /// Sets an axis-based action value.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <param name="value">The axis value (-1 to 1 for most axes, 0 to 1 for triggers).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetActionValueAsync(string actionName, float value);

    /// <summary>
    /// Sets a 2D axis action value.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <param name="x">The X axis value.</param>
    /// <param name="y">The Y axis value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetActionVector2Async(string actionName, float x, float y);

    #endregion

    #region State Reset

    /// <summary>
    /// Resets all input state to default (all keys up, mouse at origin, sticks centered).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAllAsync();

    #endregion
}
