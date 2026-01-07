using System.ComponentModel;
using KeenEyes.Input.Abstractions;
using KeenEyes.Mcp.TestBridge.Connection;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for simulating keyboard, mouse, and gamepad input.
/// </summary>
[McpServerToolType]
public sealed class InputTools(BridgeConnectionManager connection)
{
    #region Keyboard

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
    public required bool Success { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Current mouse position.
/// </summary>
public sealed record MousePositionResult
{
    public required float X { get; init; }
    public required float Y { get; init; }
}

/// <summary>
/// Result of checking a key state.
/// </summary>
public sealed record KeyStateResult
{
    public required string Key { get; init; }
    public required bool IsDown { get; init; }
}
