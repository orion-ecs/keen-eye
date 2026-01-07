using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.Input;
using KeenEyes.TestBridge.Ipc.Protocol;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IInputController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteInputController(TestBridgeClient client) : IInputController
{
    /// <inheritdoc />
    public int GamepadCount => client.SendRequestAsync<int>("input.gamepadCount", null, CancellationToken.None)
        .GetAwaiter().GetResult();

    #region Keyboard

    /// <inheritdoc />
    public async Task KeyDownAsync(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        await client.SendRequestAsync("input.keyDown", new KeyActionArgs { Key = key, Modifiers = modifiers }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task KeyUpAsync(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        await client.SendRequestAsync("input.keyUp", new KeyActionArgs { Key = key, Modifiers = modifiers }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task KeyPressAsync(Key key, KeyModifiers modifiers = KeyModifiers.None, TimeSpan? holdDuration = null)
    {
        await client.SendRequestAsync("input.keyPress", new KeyPressArgs
        {
            Key = key,
            Modifiers = modifiers,
            HoldDurationMs = holdDuration?.TotalMilliseconds
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task TypeTextAsync(string text, TimeSpan? delayBetweenChars = null)
    {
        await client.SendRequestAsync("input.typeText", new TypeTextArgs
        {
            Text = text,
            DelayBetweenCharsMs = delayBetweenChars?.TotalMilliseconds
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public bool IsKeyDown(Key key)
    {
        return client.SendRequestAsync<bool>("input.isKeyDown", new SingleKeyArgs { Key = key }, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    #endregion

    #region Mouse

    /// <inheritdoc />
    public async Task MouseMoveAsync(float x, float y)
    {
        await client.SendRequestAsync("input.mouseMove", new MouseMoveArgs { X = x, Y = y }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task MouseMoveRelativeAsync(float deltaX, float deltaY)
    {
        await client.SendRequestAsync("input.mouseMoveRelative", new MouseRelativeArgs { DeltaX = deltaX, DeltaY = deltaY }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task MouseDownAsync(MouseButton button)
    {
        await client.SendRequestAsync("input.mouseDown", new MouseButtonArgs { Button = button }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task MouseUpAsync(MouseButton button)
    {
        await client.SendRequestAsync("input.mouseUp", new MouseButtonArgs { Button = button }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task ClickAsync(float x, float y, MouseButton button = MouseButton.Left)
    {
        await client.SendRequestAsync("input.click", new ClickArgs { X = x, Y = y, Button = button }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task DoubleClickAsync(float x, float y, MouseButton button = MouseButton.Left)
    {
        await client.SendRequestAsync("input.doubleClick", new ClickArgs { X = x, Y = y, Button = button }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task DragAsync(float startX, float startY, float endX, float endY, MouseButton button = MouseButton.Left)
    {
        await client.SendRequestAsync("input.drag", new DragArgs { StartX = startX, StartY = startY, EndX = endX, EndY = endY, Button = button }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task ScrollAsync(float deltaX, float deltaY)
    {
        await client.SendRequestAsync("input.scroll", new ScrollArgs { DeltaX = deltaX, DeltaY = deltaY }, CancellationToken.None);
    }

    /// <inheritdoc />
    public (float X, float Y) GetMousePosition()
    {
        var result = client.SendRequestAsync<MousePositionResult>("input.getMousePosition", null, CancellationToken.None)
            .GetAwaiter().GetResult();
        return result != null ? (result.X, result.Y) : (0, 0);
    }

    /// <inheritdoc />
    public bool IsMouseButtonDown(MouseButton button)
    {
        return client.SendRequestAsync<bool>("input.isMouseButtonDown", new MouseButtonArgs { Button = button }, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    #endregion

    #region Gamepad

    /// <inheritdoc />
    public async Task GamepadButtonDownAsync(int gamepadIndex, GamepadButton button)
    {
        await client.SendRequestAsync("input.gamepadButtonDown", new GamepadButtonArgs { GamepadIndex = gamepadIndex, Button = button }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task GamepadButtonUpAsync(int gamepadIndex, GamepadButton button)
    {
        await client.SendRequestAsync("input.gamepadButtonUp", new GamepadButtonArgs { GamepadIndex = gamepadIndex, Button = button }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task SetLeftStickAsync(int gamepadIndex, float x, float y)
    {
        await client.SendRequestAsync("input.setLeftStick", new StickArgs { GamepadIndex = gamepadIndex, X = x, Y = y }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task SetRightStickAsync(int gamepadIndex, float x, float y)
    {
        await client.SendRequestAsync("input.setRightStick", new StickArgs { GamepadIndex = gamepadIndex, X = x, Y = y }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task SetTriggerAsync(int gamepadIndex, bool isLeft, float value)
    {
        await client.SendRequestAsync("input.setTrigger", new TriggerArgs { GamepadIndex = gamepadIndex, IsLeft = isLeft, Value = value }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task SetGamepadConnectedAsync(int gamepadIndex, bool connected)
    {
        await client.SendRequestAsync("input.setGamepadConnected", new GamepadConnectionArgs { GamepadIndex = gamepadIndex, Connected = connected }, CancellationToken.None);
    }

    /// <inheritdoc />
    public bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button)
    {
        return client.SendRequestAsync<bool>("input.isGamepadButtonDown", new GamepadButtonArgs { GamepadIndex = gamepadIndex, Button = button }, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    #endregion

    #region InputAction System

    /// <inheritdoc />
    public async Task TriggerActionAsync(string actionName)
    {
        await client.SendRequestAsync("input.triggerAction", new ActionNameArgs { ActionName = actionName }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task SetActionValueAsync(string actionName, float value)
    {
        await client.SendRequestAsync("input.setActionValue", new ActionValueArgs { ActionName = actionName, Value = value }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task SetActionVector2Async(string actionName, float x, float y)
    {
        await client.SendRequestAsync("input.setActionVector2", new ActionVector2Args { ActionName = actionName, X = x, Y = y }, CancellationToken.None);
    }

    #endregion

    #region State Reset

    /// <inheritdoc />
    public async Task ResetAllAsync()
    {
        await client.SendRequestAsync("input.resetAll", null, CancellationToken.None);
    }

    #endregion
}
