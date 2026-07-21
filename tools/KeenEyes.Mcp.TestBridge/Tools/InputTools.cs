using System.ComponentModel;
using KeenEyes.Input.Abstractions;
using KeenEyes.Mcp.TestBridge.Connection;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for simulating keyboard, mouse, and gamepad input.
/// </summary>
/// <param name="connection">The connection manager used to access the active test bridge.</param>
[McpServerToolType]
public sealed class InputTools(BridgeConnectionManager connection)
{
    #region Keyboard

    /// <summary>
    /// Presses and releases a keyboard key, optionally with modifiers and a hold duration.
    /// </summary>
    /// <param name="key">The key to press (e.g., 'Space', 'Enter', 'W', 'Escape').</param>
    /// <param name="modifiers">Modifier keys to hold while pressing, as a comma-separated list: 'Shift', 'Ctrl', 'Alt', 'Super'.</param>
    /// <param name="holdMs">How long to hold the key in milliseconds, defaulting to a single frame.</param>
    /// <returns>The result of the key press operation.</returns>
    [McpServerTool(Name = "input_key_press")]
    [Description("Press and release a keyboard key. Common keys: Space, Enter, Escape, Tab, Backspace, W, A, S, D, Up, Down, Left, Right, F1-F12.")]
    public async Task<InputResult> InputKeyPress(
        [Description("The key to press (e.g., 'Space', 'Enter', 'W', 'Escape')")]
        string key,
        [Description("Modifier keys (comma-separated): 'Shift', 'Ctrl', 'Alt', 'Super'")]
        string? modifiers = null,
        [Description("How long to hold the key in milliseconds (default: one frame)")]
        int? holdMs = null)
    {
        var bridge = connection.GetBridge();
        var keyEnum = ParseKey(key);
        var mods = ParseModifiers(modifiers);
        var duration = holdMs.HasValue ? TimeSpan.FromMilliseconds(holdMs.Value) : (TimeSpan?)null;

        await bridge.Input.KeyPressAsync(keyEnum, mods, duration);

        return new InputResult { Success = true, Message = $"Pressed key: {key}" };
    }

    /// <summary>
    /// Holds a keyboard key down until released with <see cref="InputKeyUp"/>.
    /// </summary>
    /// <param name="key">The key to hold down.</param>
    /// <param name="modifiers">Modifier keys to hold, as a comma-separated list: 'Shift', 'Ctrl', 'Alt', 'Super'.</param>
    /// <returns>The result of the key-down operation.</returns>
    [McpServerTool(Name = "input_key_down")]
    [Description("Hold a keyboard key down. Use input_key_up to release. Common keys: Space, Enter, W, A, S, D, Up, Down, Left, Right.")]
    public async Task<InputResult> InputKeyDown(
        [Description("The key to hold down")]
        string key,
        [Description("Modifier keys (comma-separated): 'Shift', 'Ctrl', 'Alt', 'Super'")]
        string? modifiers = null)
    {
        var bridge = connection.GetBridge();
        var keyEnum = ParseKey(key);
        var mods = ParseModifiers(modifiers);

        await bridge.Input.KeyDownAsync(keyEnum, mods);

        return new InputResult { Success = true, Message = $"Key down: {key}" };
    }

    /// <summary>
    /// Releases a keyboard key previously held down with <see cref="InputKeyDown"/>.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <param name="modifiers">Modifier keys to release, as a comma-separated list: 'Shift', 'Ctrl', 'Alt', 'Super'.</param>
    /// <returns>The result of the key-up operation.</returns>
    [McpServerTool(Name = "input_key_up")]
    [Description("Release a held keyboard key.")]
    public async Task<InputResult> InputKeyUp(
        [Description("The key to release")]
        string key,
        [Description("Modifier keys (comma-separated): 'Shift', 'Ctrl', 'Alt', 'Super'")]
        string? modifiers = null)
    {
        var bridge = connection.GetBridge();
        var keyEnum = ParseKey(key);
        var mods = ParseModifiers(modifiers);

        await bridge.Input.KeyUpAsync(keyEnum, mods);

        return new InputResult { Success = true, Message = $"Key up: {key}" };
    }

    /// <summary>
    /// Types a string of text as text input events, such as entry into a text field.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="delayMs">The delay between characters in milliseconds, defaulting to no delay.</param>
    /// <returns>The result of the type-text operation.</returns>
    [McpServerTool(Name = "input_type_text")]
    [Description("Type a string of text as text input events. Use for text entry like typing in a text field.")]
    public async Task<InputResult> InputTypeText(
        [Description("The text to type")]
        string text,
        [Description("Delay between characters in milliseconds (default: no delay)")]
        int? delayMs = null)
    {
        var bridge = connection.GetBridge();
        var delay = delayMs.HasValue ? TimeSpan.FromMilliseconds(delayMs.Value) : (TimeSpan?)null;

        await bridge.Input.TypeTextAsync(text, delay);

        return new InputResult { Success = true, Message = $"Typed: \"{text}\"" };
    }

    /// <summary>
    /// Checks whether a keyboard key is currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>The current pressed state of the key.</returns>
    [McpServerTool(Name = "input_is_key_down")]
    [Description("Check if a keyboard key is currently pressed.")]
    public KeyStateResult InputIsKeyDown(
        [Description("The key to check")]
        string key)
    {
        var bridge = connection.GetBridge();
        var keyEnum = ParseKey(key);
        var isDown = bridge.Input.IsKeyDown(keyEnum);

        return new KeyStateResult { Key = key, IsDown = isDown };
    }

    #endregion

    #region Mouse

    /// <summary>
    /// Moves the mouse to an absolute screen position.
    /// </summary>
    /// <param name="x">The X coordinate to move to.</param>
    /// <param name="y">The Y coordinate to move to.</param>
    /// <returns>The result of the mouse-move operation.</returns>
    [McpServerTool(Name = "input_mouse_move")]
    [Description("Move the mouse to an absolute screen position.")]
    public async Task<InputResult> InputMouseMove(
        [Description("X coordinate")]
        float x,
        [Description("Y coordinate")]
        float y)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.MouseMoveAsync(x, y);

        return new InputResult { Success = true, Message = $"Moved mouse to ({x}, {y})" };
    }

    /// <summary>
    /// Moves the mouse by a relative delta from its current position.
    /// </summary>
    /// <param name="deltaX">The X delta to move by (positive = right, negative = left).</param>
    /// <param name="deltaY">The Y delta to move by (positive = down, negative = up).</param>
    /// <returns>The result of the relative mouse-move operation.</returns>
    [McpServerTool(Name = "input_mouse_move_relative")]
    [Description("Move the mouse by a relative delta from its current position.")]
    public async Task<InputResult> InputMouseMoveRelative(
        [Description("X delta (positive = right, negative = left)")]
        float deltaX,
        [Description("Y delta (positive = down, negative = up)")]
        float deltaY)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.MouseMoveRelativeAsync(deltaX, deltaY);

        return new InputResult { Success = true, Message = $"Moved mouse by ({deltaX}, {deltaY})" };
    }

    /// <summary>
    /// Clicks the mouse at a position.
    /// </summary>
    /// <param name="x">The X coordinate to click at.</param>
    /// <param name="y">The Y coordinate to click at.</param>
    /// <param name="button">The mouse button to click: 'Left', 'Right', 'Middle' (default: Left).</param>
    /// <returns>The result of the mouse-click operation.</returns>
    [McpServerTool(Name = "input_mouse_click")]
    [Description("Click the mouse at a position. Buttons: Left, Right, Middle, Button4, Button5.")]
    public async Task<InputResult> InputMouseClick(
        [Description("X coordinate to click at")]
        float x,
        [Description("Y coordinate to click at")]
        float y,
        [Description("Mouse button: 'Left', 'Right', 'Middle' (default: Left)")]
        string? button = null)
    {
        var bridge = connection.GetBridge();
        var btn = ParseMouseButton(button);
        await bridge.Input.ClickAsync(x, y, btn);

        return new InputResult { Success = true, Message = $"Clicked {btn} at ({x}, {y})" };
    }

    /// <summary>
    /// Double-clicks the mouse at a position.
    /// </summary>
    /// <param name="x">The X coordinate to click at.</param>
    /// <param name="y">The Y coordinate to click at.</param>
    /// <param name="button">The mouse button to click: 'Left', 'Right', 'Middle' (default: Left).</param>
    /// <returns>The result of the double-click operation.</returns>
    [McpServerTool(Name = "input_mouse_double_click")]
    [Description("Double-click the mouse at a position.")]
    public async Task<InputResult> InputMouseDoubleClick(
        [Description("X coordinate to click at")]
        float x,
        [Description("Y coordinate to click at")]
        float y,
        [Description("Mouse button: 'Left', 'Right', 'Middle' (default: Left)")]
        string? button = null)
    {
        var bridge = connection.GetBridge();
        var btn = ParseMouseButton(button);
        await bridge.Input.DoubleClickAsync(x, y, btn);

        return new InputResult { Success = true, Message = $"Double-clicked {btn} at ({x}, {y})" };
    }

    /// <summary>
    /// Presses a mouse button down until released with <see cref="InputMouseUp"/>.
    /// </summary>
    /// <param name="button">The mouse button to press: 'Left', 'Right', 'Middle' (default: Left).</param>
    /// <returns>The result of the mouse-down operation.</returns>
    [McpServerTool(Name = "input_mouse_down")]
    [Description("Press a mouse button down. Use input_mouse_up to release.")]
    public async Task<InputResult> InputMouseDown(
        [Description("Mouse button: 'Left', 'Right', 'Middle' (default: Left)")]
        string? button = null)
    {
        var bridge = connection.GetBridge();
        var btn = ParseMouseButton(button);
        await bridge.Input.MouseDownAsync(btn);

        return new InputResult { Success = true, Message = $"Mouse button down: {btn}" };
    }

    /// <summary>
    /// Releases a mouse button previously pressed with <see cref="InputMouseDown"/>.
    /// </summary>
    /// <param name="button">The mouse button to release: 'Left', 'Right', 'Middle' (default: Left).</param>
    /// <returns>The result of the mouse-up operation.</returns>
    [McpServerTool(Name = "input_mouse_up")]
    [Description("Release a pressed mouse button.")]
    public async Task<InputResult> InputMouseUp(
        [Description("Mouse button: 'Left', 'Right', 'Middle' (default: Left)")]
        string? button = null)
    {
        var bridge = connection.GetBridge();
        var btn = ParseMouseButton(button);
        await bridge.Input.MouseUpAsync(btn);

        return new InputResult { Success = true, Message = $"Mouse button up: {btn}" };
    }

    /// <summary>
    /// Drags the mouse from one position to another while holding a button.
    /// </summary>
    /// <param name="startX">The starting X coordinate.</param>
    /// <param name="startY">The starting Y coordinate.</param>
    /// <param name="endX">The ending X coordinate.</param>
    /// <param name="endY">The ending Y coordinate.</param>
    /// <param name="button">The mouse button to hold while dragging: 'Left', 'Right', 'Middle' (default: Left).</param>
    /// <returns>The result of the mouse-drag operation.</returns>
    [McpServerTool(Name = "input_mouse_drag")]
    [Description("Drag the mouse from one position to another while holding a button.")]
    public async Task<InputResult> InputMouseDrag(
        [Description("Starting X coordinate")]
        float startX,
        [Description("Starting Y coordinate")]
        float startY,
        [Description("Ending X coordinate")]
        float endX,
        [Description("Ending Y coordinate")]
        float endY,
        [Description("Mouse button: 'Left', 'Right', 'Middle' (default: Left)")]
        string? button = null)
    {
        var bridge = connection.GetBridge();
        var btn = ParseMouseButton(button);
        await bridge.Input.DragAsync(startX, startY, endX, endY, btn);

        return new InputResult { Success = true, Message = $"Dragged from ({startX}, {startY}) to ({endX}, {endY})" };
    }

    /// <summary>
    /// Scrolls the mouse wheel.
    /// </summary>
    /// <param name="deltaY">The vertical scroll amount (positive = up, negative = down).</param>
    /// <param name="deltaX">The horizontal scroll amount (positive = right, negative = left).</param>
    /// <returns>The result of the scroll operation.</returns>
    [McpServerTool(Name = "input_mouse_scroll")]
    [Description("Scroll the mouse wheel. Positive deltaY scrolls up, negative scrolls down.")]
    public async Task<InputResult> InputMouseScroll(
        [Description("Vertical scroll amount (positive = up, negative = down)")]
        float deltaY,
        [Description("Horizontal scroll amount (positive = right, negative = left)")]
        float deltaX = 0)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.ScrollAsync(deltaX, deltaY);

        return new InputResult { Success = true, Message = $"Scrolled ({deltaX}, {deltaY})" };
    }

    /// <summary>
    /// Gets the current mouse position.
    /// </summary>
    /// <returns>The current mouse position.</returns>
    [McpServerTool(Name = "input_get_mouse_position")]
    [Description("Get the current mouse position.")]
    public MousePositionResult InputGetMousePosition()
    {
        var bridge = connection.GetBridge();
        var (x, y) = bridge.Input.GetMousePosition();

        return new MousePositionResult { X = x, Y = y };
    }

    #endregion

    #region Gamepad

    /// <summary>
    /// Presses a gamepad button down until released with <see cref="InputGamepadButtonUp"/>.
    /// </summary>
    /// <param name="button">The gamepad button name.</param>
    /// <param name="playerIndex">The player/gamepad index (default: 0).</param>
    /// <returns>The result of the gamepad button-down operation.</returns>
    [McpServerTool(Name = "input_gamepad_button_down")]
    [Description("Press a gamepad button down. Buttons: A, B, X, Y, LeftBumper, RightBumper, Back, Start, Home, LeftStick, RightStick, DPadUp, DPadDown, DPadLeft, DPadRight.")]
    public async Task<InputResult> InputGamepadButtonDown(
        [Description("Gamepad button name")]
        string button,
        [Description("Player/gamepad index (default: 0)")]
        int playerIndex = 0)
    {
        var bridge = connection.GetBridge();
        var btn = ParseGamepadButton(button);
        await bridge.Input.GamepadButtonDownAsync(playerIndex, btn);

        return new InputResult { Success = true, Message = $"Gamepad {playerIndex} button down: {button}" };
    }

    /// <summary>
    /// Releases a gamepad button previously pressed with <see cref="InputGamepadButtonDown"/>.
    /// </summary>
    /// <param name="button">The gamepad button name.</param>
    /// <param name="playerIndex">The player/gamepad index (default: 0).</param>
    /// <returns>The result of the gamepad button-up operation.</returns>
    [McpServerTool(Name = "input_gamepad_button_up")]
    [Description("Release a pressed gamepad button.")]
    public async Task<InputResult> InputGamepadButtonUp(
        [Description("Gamepad button name")]
        string button,
        [Description("Player/gamepad index (default: 0)")]
        int playerIndex = 0)
    {
        var bridge = connection.GetBridge();
        var btn = ParseGamepadButton(button);
        await bridge.Input.GamepadButtonUpAsync(playerIndex, btn);

        return new InputResult { Success = true, Message = $"Gamepad {playerIndex} button up: {button}" };
    }

    /// <summary>
    /// Presses and releases a gamepad button.
    /// </summary>
    /// <param name="button">The gamepad button name.</param>
    /// <param name="playerIndex">The player/gamepad index (default: 0).</param>
    /// <param name="holdMs">How long to hold the button in milliseconds (default: 50ms).</param>
    /// <returns>The result of the gamepad button-press operation.</returns>
    [McpServerTool(Name = "input_gamepad_button_press")]
    [Description("Press and release a gamepad button.")]
    public async Task<InputResult> InputGamepadButtonPress(
        [Description("Gamepad button name")]
        string button,
        [Description("Player/gamepad index (default: 0)")]
        int playerIndex = 0,
        [Description("How long to hold the button in milliseconds (default: 50ms)")]
        int holdMs = 50)
    {
        var bridge = connection.GetBridge();
        var btn = ParseGamepadButton(button);

        await bridge.Input.GamepadButtonDownAsync(playerIndex, btn);
        await Task.Delay(holdMs);
        await bridge.Input.GamepadButtonUpAsync(playerIndex, btn);

        return new InputResult { Success = true, Message = $"Gamepad {playerIndex} pressed: {button}" };
    }

    /// <summary>
    /// Sets the left analog stick position.
    /// </summary>
    /// <param name="x">The X position (-1 = left, 1 = right).</param>
    /// <param name="y">The Y position (-1 = up, 1 = down).</param>
    /// <param name="playerIndex">The player/gamepad index (default: 0).</param>
    /// <returns>The result of the left-stick operation.</returns>
    [McpServerTool(Name = "input_gamepad_left_stick")]
    [Description("Set the left analog stick position. Values range from -1 to 1 for each axis.")]
    public async Task<InputResult> InputGamepadLeftStick(
        [Description("X position (-1 = left, 1 = right)")]
        float x,
        [Description("Y position (-1 = up, 1 = down)")]
        float y,
        [Description("Player/gamepad index (default: 0)")]
        int playerIndex = 0)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.SetLeftStickAsync(playerIndex, x, y);

        return new InputResult { Success = true, Message = $"Gamepad {playerIndex} left stick: ({x}, {y})" };
    }

    /// <summary>
    /// Sets the right analog stick position.
    /// </summary>
    /// <param name="x">The X position (-1 = left, 1 = right).</param>
    /// <param name="y">The Y position (-1 = up, 1 = down).</param>
    /// <param name="playerIndex">The player/gamepad index (default: 0).</param>
    /// <returns>The result of the right-stick operation.</returns>
    [McpServerTool(Name = "input_gamepad_right_stick")]
    [Description("Set the right analog stick position. Values range from -1 to 1 for each axis.")]
    public async Task<InputResult> InputGamepadRightStick(
        [Description("X position (-1 = left, 1 = right)")]
        float x,
        [Description("Y position (-1 = up, 1 = down)")]
        float y,
        [Description("Player/gamepad index (default: 0)")]
        int playerIndex = 0)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.SetRightStickAsync(playerIndex, x, y);

        return new InputResult { Success = true, Message = $"Gamepad {playerIndex} right stick: ({x}, {y})" };
    }

    /// <summary>
    /// Sets a gamepad trigger value.
    /// </summary>
    /// <param name="trigger">The trigger to set: 'Left' or 'Right'.</param>
    /// <param name="value">The trigger value (0 to 1).</param>
    /// <param name="playerIndex">The player/gamepad index (default: 0).</param>
    /// <returns>The result of the trigger operation.</returns>
    [McpServerTool(Name = "input_gamepad_trigger")]
    [Description("Set a trigger value. Values range from 0 (not pressed) to 1 (fully pressed).")]
    public async Task<InputResult> InputGamepadTrigger(
        [Description("Trigger: 'Left' or 'Right'")]
        string trigger,
        [Description("Trigger value (0 to 1)")]
        float value,
        [Description("Player/gamepad index (default: 0)")]
        int playerIndex = 0)
    {
        var bridge = connection.GetBridge();
        var isLeft = trigger.Equals("Left", StringComparison.OrdinalIgnoreCase);
        await bridge.Input.SetTriggerAsync(playerIndex, isLeft, value);

        return new InputResult { Success = true, Message = $"Gamepad {playerIndex} {trigger} trigger: {value}" };
    }

    /// <summary>
    /// Connects or disconnects a virtual gamepad.
    /// </summary>
    /// <param name="connected">Whether the gamepad should be connected.</param>
    /// <param name="playerIndex">The player/gamepad index (default: 0).</param>
    /// <returns>The result of the connect/disconnect operation.</returns>
    [McpServerTool(Name = "input_gamepad_connect")]
    [Description("Connect or disconnect a virtual gamepad.")]
    public async Task<InputResult> InputGamepadConnect(
        [Description("Whether the gamepad should be connected")]
        bool connected,
        [Description("Player/gamepad index (default: 0)")]
        int playerIndex = 0)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.SetGamepadConnectedAsync(playerIndex, connected);

        var state = connected ? "connected" : "disconnected";
        return new InputResult { Success = true, Message = $"Gamepad {playerIndex} {state}" };
    }

    #endregion

    #region Input Actions

    /// <summary>
    /// Triggers a named input action directly, bypassing normal input bindings.
    /// </summary>
    /// <param name="actionName">The name of the input action to trigger.</param>
    /// <returns>The result of the trigger-action operation.</returns>
    [McpServerTool(Name = "input_trigger_action")]
    [Description("Trigger a named input action directly, bypassing normal input bindings.")]
    public async Task<InputResult> InputTriggerAction(
        [Description("The name of the input action to trigger")]
        string actionName)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.TriggerActionAsync(actionName);

        return new InputResult { Success = true, Message = $"Triggered action: {actionName}" };
    }

    /// <summary>
    /// Sets the value of an axis-based input action.
    /// </summary>
    /// <param name="actionName">The name of the input action.</param>
    /// <param name="value">The axis value (-1 to 1 for most axes, 0 to 1 for triggers).</param>
    /// <returns>The result of the set-action-value operation.</returns>
    [McpServerTool(Name = "input_set_action_value")]
    [Description("Set the value of an axis-based input action.")]
    public async Task<InputResult> InputSetActionValue(
        [Description("The name of the input action")]
        string actionName,
        [Description("The axis value (-1 to 1 for most axes, 0 to 1 for triggers)")]
        float value)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.SetActionValueAsync(actionName, value);

        return new InputResult { Success = true, Message = $"Set action '{actionName}' to {value}" };
    }

    /// <summary>
    /// Sets the value of a 2D axis input action, such as movement.
    /// </summary>
    /// <param name="actionName">The name of the input action.</param>
    /// <param name="x">The X axis value.</param>
    /// <param name="y">The Y axis value.</param>
    /// <returns>The result of the set-action-vector2 operation.</returns>
    [McpServerTool(Name = "input_set_action_vector2")]
    [Description("Set the value of a 2D axis input action (like movement).")]
    public async Task<InputResult> InputSetActionVector2(
        [Description("The name of the input action")]
        string actionName,
        [Description("X axis value")]
        float x,
        [Description("Y axis value")]
        float y)
    {
        var bridge = connection.GetBridge();
        await bridge.Input.SetActionVector2Async(actionName, x, y);

        return new InputResult { Success = true, Message = $"Set action '{actionName}' to ({x}, {y})" };
    }

    /// <summary>
    /// Resets all input state to default (all keys up, mouse at origin, sticks centered).
    /// </summary>
    /// <returns>The result of the reset operation.</returns>
    [McpServerTool(Name = "input_reset")]
    [Description("Reset all input state to default (all keys up, mouse at origin, sticks centered).")]
    public async Task<InputResult> InputReset()
    {
        var bridge = connection.GetBridge();
        await bridge.Input.ResetAllAsync();

        return new InputResult { Success = true, Message = "Reset all input state" };
    }

    #endregion

    #region Helpers

    private static Key ParseKey(string key)
    {
        if (!Enum.TryParse<Key>(key, ignoreCase: true, out var result))
        {
            throw new ArgumentException(
                $"Invalid key: '{key}'. Valid keys include: Space, Enter, Escape, Tab, Backspace, " +
                "A-Z, Number0-Number9, Up, Down, Left, Right, F1-F12.");
        }

        return result;
    }

    private static KeyModifiers ParseModifiers(string? modifiers)
    {
        if (string.IsNullOrWhiteSpace(modifiers))
        {
            return KeyModifiers.None;
        }

        var result = KeyModifiers.None;
        foreach (var mod in modifiers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            result |= mod.ToLowerInvariant() switch
            {
                "shift" => KeyModifiers.Shift,
                "ctrl" or "control" => KeyModifiers.Control,
                "alt" => KeyModifiers.Alt,
                "super" or "win" or "cmd" or "meta" => KeyModifiers.Super,
                _ => throw new ArgumentException($"Invalid modifier: '{mod}'. Valid modifiers: Shift, Ctrl, Alt, Super.")
            };
        }

        return result;
    }

    private static MouseButton ParseMouseButton(string? button)
    {
        if (string.IsNullOrWhiteSpace(button))
        {
            return MouseButton.Left;
        }

        if (!Enum.TryParse<MouseButton>(button, ignoreCase: true, out var result))
        {
            throw new ArgumentException(
                $"Invalid mouse button: '{button}'. Valid buttons: Left, Right, Middle, Button4, Button5.");
        }

        return result;
    }

    private static GamepadButton ParseGamepadButton(string button)
    {
        if (!Enum.TryParse<GamepadButton>(button, ignoreCase: true, out var result))
        {
            throw new ArgumentException(
                $"Invalid gamepad button: '{button}'. Valid buttons: A, B, X, Y, LeftBumper, RightBumper, " +
                "Back, Start, Home, LeftStick, RightStick, DPadUp, DPadDown, DPadLeft, DPadRight.");
        }

        return result;
    }

    #endregion
}

/// <summary>
/// Result of an input operation.
/// </summary>
public sealed record InputResult
{
    /// <summary>
    /// Gets whether the input operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets a message describing the outcome of the input operation.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Current mouse position.
/// </summary>
public sealed record MousePositionResult
{
    /// <summary>
    /// Gets the current mouse X coordinate.
    /// </summary>
    public required float X { get; init; }

    /// <summary>
    /// Gets the current mouse Y coordinate.
    /// </summary>
    public required float Y { get; init; }
}

/// <summary>
/// Result of checking a key state.
/// </summary>
public sealed record KeyStateResult
{
    /// <summary>
    /// Gets the key that was checked.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets whether the key is currently pressed.
    /// </summary>
    public required bool IsDown { get; init; }
}
