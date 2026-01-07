using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.Input;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IInputController for testing.
/// </summary>
internal sealed class MockInputController : IInputController
{
    private readonly HashSet<Key> pressedKeys = [];
    private readonly HashSet<MouseButton> pressedButtons = [];
    private readonly Dictionary<int, HashSet<GamepadButton>> pressedGamepadButtons = [];

    public List<string> RecordedActions { get; } = [];
    public (float X, float Y) MousePosition { get; set; }
    public Dictionary<int, (float X, float Y)> LeftStickPositions { get; } = [];
    public Dictionary<int, (float X, float Y)> RightStickPositions { get; } = [];
    public Dictionary<int, (float Left, float Right)> TriggerValues { get; } = [];
    public HashSet<int> ConnectedGamepads { get; } = [0];
    public int GamepadCount => 4;

    #region Keyboard

    public Task KeyDownAsync(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        pressedKeys.Add(key);
        RecordedActions.Add($"KeyDown:{key}:{modifiers}");
        return Task.CompletedTask;
    }

    public Task KeyUpAsync(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        pressedKeys.Remove(key);
        RecordedActions.Add($"KeyUp:{key}:{modifiers}");
        return Task.CompletedTask;
    }

    public Task KeyPressAsync(Key key, KeyModifiers modifiers = KeyModifiers.None, TimeSpan? holdDuration = null)
    {
        RecordedActions.Add($"KeyPress:{key}:{modifiers}:{holdDuration?.TotalMilliseconds ?? 0}");
        return Task.CompletedTask;
    }

    public Task TypeTextAsync(string text, TimeSpan? delayBetweenChars = null)
    {
        RecordedActions.Add($"TypeText:{text}");
        return Task.CompletedTask;
    }

    public bool IsKeyDown(Key key) => pressedKeys.Contains(key);

    #endregion

    #region Mouse

    public Task MouseMoveAsync(float x, float y)
    {
        MousePosition = (x, y);
        RecordedActions.Add($"MouseMove:{x}:{y}");
        return Task.CompletedTask;
    }

    public Task MouseMoveRelativeAsync(float deltaX, float deltaY)
    {
        MousePosition = (MousePosition.X + deltaX, MousePosition.Y + deltaY);
        RecordedActions.Add($"MouseMoveRelative:{deltaX}:{deltaY}");
        return Task.CompletedTask;
    }

    public Task MouseDownAsync(MouseButton button)
    {
        pressedButtons.Add(button);
        RecordedActions.Add($"MouseDown:{button}");
        return Task.CompletedTask;
    }

    public Task MouseUpAsync(MouseButton button)
    {
        pressedButtons.Remove(button);
        RecordedActions.Add($"MouseUp:{button}");
        return Task.CompletedTask;
    }

    public Task ClickAsync(float x, float y, MouseButton button = MouseButton.Left)
    {
        MousePosition = (x, y);
        RecordedActions.Add($"Click:{x}:{y}:{button}");
        return Task.CompletedTask;
    }

    public Task DoubleClickAsync(float x, float y, MouseButton button = MouseButton.Left)
    {
        MousePosition = (x, y);
        RecordedActions.Add($"DoubleClick:{x}:{y}:{button}");
        return Task.CompletedTask;
    }

    public Task DragAsync(float startX, float startY, float endX, float endY, MouseButton button = MouseButton.Left)
    {
        MousePosition = (endX, endY);
        RecordedActions.Add($"Drag:{startX}:{startY}:{endX}:{endY}:{button}");
        return Task.CompletedTask;
    }

    public Task ScrollAsync(float deltaX, float deltaY)
    {
        RecordedActions.Add($"Scroll:{deltaX}:{deltaY}");
        return Task.CompletedTask;
    }

    public (float X, float Y) GetMousePosition() => MousePosition;

    public bool IsMouseButtonDown(MouseButton button) => pressedButtons.Contains(button);

    #endregion

    #region Gamepad

    public Task GamepadButtonDownAsync(int gamepadIndex, GamepadButton button)
    {
        if (!pressedGamepadButtons.TryGetValue(gamepadIndex, out var buttons))
        {
            buttons = [];
            pressedGamepadButtons[gamepadIndex] = buttons;
        }

        buttons.Add(button);
        RecordedActions.Add($"GamepadButtonDown:{gamepadIndex}:{button}");
        return Task.CompletedTask;
    }

    public Task GamepadButtonUpAsync(int gamepadIndex, GamepadButton button)
    {
        if (pressedGamepadButtons.TryGetValue(gamepadIndex, out var buttons))
        {
            buttons.Remove(button);
        }

        RecordedActions.Add($"GamepadButtonUp:{gamepadIndex}:{button}");
        return Task.CompletedTask;
    }

    public Task SetLeftStickAsync(int gamepadIndex, float x, float y)
    {
        LeftStickPositions[gamepadIndex] = (x, y);
        RecordedActions.Add($"SetLeftStick:{gamepadIndex}:{x}:{y}");
        return Task.CompletedTask;
    }

    public Task SetRightStickAsync(int gamepadIndex, float x, float y)
    {
        RightStickPositions[gamepadIndex] = (x, y);
        RecordedActions.Add($"SetRightStick:{gamepadIndex}:{x}:{y}");
        return Task.CompletedTask;
    }

    public Task SetTriggerAsync(int gamepadIndex, bool isLeft, float value)
    {
        if (!TriggerValues.TryGetValue(gamepadIndex, out var triggers))
        {
            triggers = (0f, 0f);
        }

        TriggerValues[gamepadIndex] = isLeft ? (value, triggers.Right) : (triggers.Left, value);
        RecordedActions.Add($"SetTrigger:{gamepadIndex}:{(isLeft ? "Left" : "Right")}:{value}");
        return Task.CompletedTask;
    }

    public Task SetGamepadConnectedAsync(int gamepadIndex, bool connected)
    {
        if (connected)
        {
            ConnectedGamepads.Add(gamepadIndex);
        }
        else
        {
            ConnectedGamepads.Remove(gamepadIndex);
        }

        RecordedActions.Add($"SetGamepadConnected:{gamepadIndex}:{connected}");
        return Task.CompletedTask;
    }

    public bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button)
    {
        return pressedGamepadButtons.TryGetValue(gamepadIndex, out var buttons) && buttons.Contains(button);
    }

    #endregion

    #region Actions

    public Task TriggerActionAsync(string actionName)
    {
        RecordedActions.Add($"TriggerAction:{actionName}");
        return Task.CompletedTask;
    }

    public Task SetActionValueAsync(string actionName, float value)
    {
        RecordedActions.Add($"SetActionValue:{actionName}:{value}");
        return Task.CompletedTask;
    }

    public Task SetActionVector2Async(string actionName, float x, float y)
    {
        RecordedActions.Add($"SetActionVector2:{actionName}:{x}:{y}");
        return Task.CompletedTask;
    }

    public Task ResetAllAsync()
    {
        pressedKeys.Clear();
        pressedButtons.Clear();
        pressedGamepadButtons.Clear();
        MousePosition = (0, 0);
        LeftStickPositions.Clear();
        RightStickPositions.Clear();
        TriggerValues.Clear();
        RecordedActions.Add("ResetAll");
        return Task.CompletedTask;
    }

    #endregion
}
