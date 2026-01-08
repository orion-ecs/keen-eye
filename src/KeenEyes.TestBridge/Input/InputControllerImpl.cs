using System.Numerics;
using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.Input;
using KeenEyes.Testing.Input;

namespace KeenEyes.TestBridge.Input;

/// <summary>
/// In-process implementation of <see cref="IInputController"/> using <see cref="MockInputContext"/>.
/// </summary>
internal sealed class InputControllerImpl(MockInputContext inputContext) : IInputController
{
    /// <summary>
    /// Default hold duration for key presses (50ms, roughly 3 frames at 60fps).
    /// This ensures the key press spans at least one frame and is detected by polling-based systems.
    /// </summary>
    private static readonly TimeSpan defaultKeyHoldDuration = TimeSpan.FromMilliseconds(50);
    private readonly Dictionary<string, bool> actionStates = [];
    private readonly Dictionary<string, float> actionValues = [];
    private readonly Dictionary<string, (float X, float Y)> actionVectors = [];

    #region Keyboard

    public Task KeyDownAsync(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        inputContext.SimulateKeyDown(key, modifiers);
        return Task.CompletedTask;
    }

    public Task KeyUpAsync(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        inputContext.SimulateKeyUp(key, modifiers);
        return Task.CompletedTask;
    }

    public async Task KeyPressAsync(Key key, KeyModifiers modifiers = KeyModifiers.None, TimeSpan? holdDuration = null)
    {
        var actualDuration = holdDuration ?? defaultKeyHoldDuration;

        await KeyDownAsync(key, modifiers);
        await Task.Delay(actualDuration);
        await KeyUpAsync(key, modifiers);
    }

    public Task TypeTextAsync(string text, TimeSpan? delayBetweenChars = null)
    {
        if (delayBetweenChars.HasValue && delayBetweenChars.Value > TimeSpan.Zero)
        {
            return TypeTextWithDelayAsync(text, delayBetweenChars.Value);
        }

        inputContext.MockKeyboard.SimulateTextInput(text);
        return Task.CompletedTask;
    }

    private async Task TypeTextWithDelayAsync(string text, TimeSpan delay)
    {
        foreach (var c in text)
        {
            inputContext.MockKeyboard.SimulateTextInput(c);
            await Task.Delay(delay);
        }
    }

    public bool IsKeyDown(Key key)
    {
        return inputContext.Keyboard.IsKeyDown(key);
    }

    #endregion

    #region Mouse

    public Task MouseMoveAsync(float x, float y)
    {
        inputContext.MockMouse.SimulateMove(new Vector2(x, y));
        return Task.CompletedTask;
    }

    public Task MouseMoveRelativeAsync(float deltaX, float deltaY)
    {
        inputContext.MockMouse.SimulateMoveBy(new Vector2(deltaX, deltaY));
        return Task.CompletedTask;
    }

    public Task MouseDownAsync(MouseButton button)
    {
        inputContext.MockMouse.SimulateButtonDown(button);
        return Task.CompletedTask;
    }

    public Task MouseUpAsync(MouseButton button)
    {
        inputContext.MockMouse.SimulateButtonUp(button);
        return Task.CompletedTask;
    }

    public async Task ClickAsync(float x, float y, MouseButton button = MouseButton.Left)
    {
        await MouseMoveAsync(x, y);
        inputContext.MockMouse.SimulateClick(button);
    }

    public async Task DoubleClickAsync(float x, float y, MouseButton button = MouseButton.Left)
    {
        await MouseMoveAsync(x, y);
        inputContext.MockMouse.SimulateDoubleClick(button);
    }

    public Task DragAsync(float startX, float startY, float endX, float endY, MouseButton button = MouseButton.Left)
    {
        inputContext.MockMouse.SimulateDrag(new Vector2(startX, startY), new Vector2(endX, endY), button);
        return Task.CompletedTask;
    }

    public Task ScrollAsync(float deltaX, float deltaY)
    {
        inputContext.MockMouse.SimulateScroll(deltaX, deltaY);
        return Task.CompletedTask;
    }

    public (float X, float Y) GetMousePosition()
    {
        var pos = inputContext.Mouse.Position;
        return (pos.X, pos.Y);
    }

    public bool IsMouseButtonDown(MouseButton button)
    {
        return inputContext.Mouse.IsButtonDown(button);
    }

    #endregion

    #region Gamepad

    public Task GamepadButtonDownAsync(int gamepadIndex, GamepadButton button)
    {
        var gamepad = inputContext.GetMockGamepad(gamepadIndex);
        gamepad.SimulateButtonDown(button);
        return Task.CompletedTask;
    }

    public Task GamepadButtonUpAsync(int gamepadIndex, GamepadButton button)
    {
        var gamepad = inputContext.GetMockGamepad(gamepadIndex);
        gamepad.SimulateButtonUp(button);
        return Task.CompletedTask;
    }

    public Task SetLeftStickAsync(int gamepadIndex, float x, float y)
    {
        inputContext.SetGamepadStick(gamepadIndex, isLeft: true, x, y);
        return Task.CompletedTask;
    }

    public Task SetRightStickAsync(int gamepadIndex, float x, float y)
    {
        inputContext.SetGamepadStick(gamepadIndex, isLeft: false, x, y);
        return Task.CompletedTask;
    }

    public Task SetTriggerAsync(int gamepadIndex, bool isLeft, float value)
    {
        inputContext.SetGamepadTrigger(gamepadIndex, isLeft, value);
        return Task.CompletedTask;
    }

    public Task SetGamepadConnectedAsync(int gamepadIndex, bool connected)
    {
        inputContext.SetGamepadConnected(gamepadIndex, connected);
        return Task.CompletedTask;
    }

    public bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button)
    {
        if (gamepadIndex < 0 || gamepadIndex >= inputContext.Gamepads.Length)
        {
            return false;
        }

        return inputContext.Gamepads[gamepadIndex].IsButtonDown(button);
    }

    public int GamepadCount => inputContext.Gamepads.Length;

    #endregion

    #region InputAction System

    public Task TriggerActionAsync(string actionName)
    {
        actionStates[actionName] = true;
        return Task.CompletedTask;
    }

    public Task SetActionValueAsync(string actionName, float value)
    {
        actionValues[actionName] = value;
        return Task.CompletedTask;
    }

    public Task SetActionVector2Async(string actionName, float x, float y)
    {
        actionVectors[actionName] = (x, y);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets whether an action has been triggered.
    /// </summary>
    /// <param name="actionName">The action name.</param>
    /// <returns>True if the action was triggered.</returns>
    public bool IsActionTriggered(string actionName)
    {
        return actionStates.GetValueOrDefault(actionName, false);
    }

    /// <summary>
    /// Gets the current value of an axis action.
    /// </summary>
    /// <param name="actionName">The action name.</param>
    /// <returns>The axis value, or 0 if not set.</returns>
    public float GetActionValue(string actionName)
    {
        return actionValues.GetValueOrDefault(actionName, 0f);
    }

    /// <summary>
    /// Gets the current value of a 2D axis action.
    /// </summary>
    /// <param name="actionName">The action name.</param>
    /// <returns>The 2D axis value, or (0, 0) if not set.</returns>
    public (float X, float Y) GetActionVector2(string actionName)
    {
        return actionVectors.GetValueOrDefault(actionName, (0f, 0f));
    }

    #endregion

    #region State Reset

    public Task ResetAllAsync()
    {
        // Reset keyboard
        inputContext.MockKeyboard.ClearAllKeys();

        // Reset mouse
        inputContext.MockMouse.Reset();

        // Reset gamepads
        for (int i = 0; i < inputContext.Gamepads.Length; i++)
        {
            var gamepad = inputContext.GetMockGamepad(i);
            gamepad.SetLeftStick(0, 0);
            gamepad.SetRightStick(0, 0);
            gamepad.SetLeftTrigger(0);
            gamepad.SetRightTrigger(0);
            // Clear any pressed buttons
            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                if (gamepad.IsButtonDown(button))
                {
                    gamepad.SetButtonUp(button);
                }
            }
        }

        // Reset actions
        actionStates.Clear();
        actionValues.Clear();
        actionVectors.Clear();

        return Task.CompletedTask;
    }

    #endregion
}
