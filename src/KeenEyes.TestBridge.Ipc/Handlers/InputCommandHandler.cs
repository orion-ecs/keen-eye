using System.Text.Json;
using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.Input;
using KeenEyes.TestBridge.Ipc.Protocol;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles input injection commands.
/// </summary>
internal sealed class InputCommandHandler(IInputController inputController) : ICommandHandler
{
    public string Prefix => "input";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "keyDown" => await HandleKeyDownAsync(args),
            "keyUp" => await HandleKeyUpAsync(args),
            "keyPress" => await HandleKeyPressAsync(args),
            "typeText" => await HandleTypeTextAsync(args),
            "mouseMove" => await HandleMouseMoveAsync(args),
            "mouseMoveRelative" => await HandleMouseMoveRelativeAsync(args),
            "mouseDown" => await HandleMouseDownAsync(args),
            "mouseUp" => await HandleMouseUpAsync(args),
            "click" => await HandleClickAsync(args),
            "doubleClick" => await HandleDoubleClickAsync(args),
            "drag" => await HandleDragAsync(args),
            "scroll" => await HandleScrollAsync(args),
            "gamepadButtonDown" => await HandleGamepadButtonDownAsync(args),
            "gamepadButtonUp" => await HandleGamepadButtonUpAsync(args),
            "setLeftStick" => await HandleSetLeftStickAsync(args),
            "setRightStick" => await HandleSetRightStickAsync(args),
            "setTrigger" => await HandleSetTriggerAsync(args),
            "setGamepadConnected" => await HandleSetGamepadConnectedAsync(args),
            "triggerAction" => await HandleTriggerActionAsync(args),
            "setActionValue" => await HandleSetActionValueAsync(args),
            "setActionVector2" => await HandleSetActionVector2Async(args),
            "resetAll" => await HandleResetAllAsync(),
            "isKeyDown" => HandleIsKeyDown(args),
            "isMouseButtonDown" => HandleIsMouseButtonDown(args),
            "isGamepadButtonDown" => HandleIsGamepadButtonDown(args),
            "getMousePosition" => HandleGetMousePosition(),
            "gamepadCount" => HandleGamepadCount(),
            _ => throw new InvalidOperationException($"Unknown input command: {command}")
        };
    }

    private async Task<object?> HandleKeyDownAsync(JsonElement? args)
    {
        var key = GetRequiredEnum<Key>(args, "key");
        var modifiers = GetOptionalEnum<KeyModifiers>(args, "modifiers") ?? KeyModifiers.None;
        await inputController.KeyDownAsync(key, modifiers);
        return null;
    }

    private async Task<object?> HandleKeyUpAsync(JsonElement? args)
    {
        var key = GetRequiredEnum<Key>(args, "key");
        var modifiers = GetOptionalEnum<KeyModifiers>(args, "modifiers") ?? KeyModifiers.None;
        await inputController.KeyUpAsync(key, modifiers);
        return null;
    }

    private async Task<object?> HandleKeyPressAsync(JsonElement? args)
    {
        var key = GetRequiredEnum<Key>(args, "key");
        var modifiers = GetOptionalEnum<KeyModifiers>(args, "modifiers") ?? KeyModifiers.None;
        var holdDurationMs = GetOptionalFloat(args, "holdDurationMs");
        var holdDuration = holdDurationMs.HasValue ? TimeSpan.FromMilliseconds(holdDurationMs.Value) : (TimeSpan?)null;
        await inputController.KeyPressAsync(key, modifiers, holdDuration);
        return null;
    }

    private async Task<object?> HandleTypeTextAsync(JsonElement? args)
    {
        var text = GetRequiredString(args, "text");
        var delayMs = GetOptionalFloat(args, "delayBetweenCharsMs");
        var delay = delayMs.HasValue ? TimeSpan.FromMilliseconds(delayMs.Value) : (TimeSpan?)null;
        await inputController.TypeTextAsync(text, delay);
        return null;
    }

    private async Task<object?> HandleMouseMoveAsync(JsonElement? args)
    {
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        await inputController.MouseMoveAsync(x, y);
        return null;
    }

    private async Task<object?> HandleMouseMoveRelativeAsync(JsonElement? args)
    {
        var deltaX = GetRequiredFloat(args, "deltaX");
        var deltaY = GetRequiredFloat(args, "deltaY");
        await inputController.MouseMoveRelativeAsync(deltaX, deltaY);
        return null;
    }

    private async Task<object?> HandleMouseDownAsync(JsonElement? args)
    {
        var button = GetOptionalEnum<MouseButton>(args, "button") ?? MouseButton.Left;
        await inputController.MouseDownAsync(button);
        return null;
    }

    private async Task<object?> HandleMouseUpAsync(JsonElement? args)
    {
        var button = GetOptionalEnum<MouseButton>(args, "button") ?? MouseButton.Left;
        await inputController.MouseUpAsync(button);
        return null;
    }

    private async Task<object?> HandleClickAsync(JsonElement? args)
    {
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        var button = GetOptionalEnum<MouseButton>(args, "button") ?? MouseButton.Left;
        await inputController.ClickAsync(x, y, button);
        return null;
    }

    private async Task<object?> HandleDoubleClickAsync(JsonElement? args)
    {
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        var button = GetOptionalEnum<MouseButton>(args, "button") ?? MouseButton.Left;
        await inputController.DoubleClickAsync(x, y, button);
        return null;
    }

    private async Task<object?> HandleDragAsync(JsonElement? args)
    {
        var startX = GetRequiredFloat(args, "startX");
        var startY = GetRequiredFloat(args, "startY");
        var endX = GetRequiredFloat(args, "endX");
        var endY = GetRequiredFloat(args, "endY");
        var button = GetOptionalEnum<MouseButton>(args, "button") ?? MouseButton.Left;
        await inputController.DragAsync(startX, startY, endX, endY, button);
        return null;
    }

    private async Task<object?> HandleScrollAsync(JsonElement? args)
    {
        var deltaX = GetOptionalFloat(args, "deltaX") ?? 0f;
        var deltaY = GetOptionalFloat(args, "deltaY") ?? 0f;
        await inputController.ScrollAsync(deltaX, deltaY);
        return null;
    }

    private async Task<object?> HandleGamepadButtonDownAsync(JsonElement? args)
    {
        var gamepadIndex = GetRequiredInt(args, "gamepadIndex");
        var button = GetRequiredEnum<GamepadButton>(args, "button");
        await inputController.GamepadButtonDownAsync(gamepadIndex, button);
        return null;
    }

    private async Task<object?> HandleGamepadButtonUpAsync(JsonElement? args)
    {
        var gamepadIndex = GetRequiredInt(args, "gamepadIndex");
        var button = GetRequiredEnum<GamepadButton>(args, "button");
        await inputController.GamepadButtonUpAsync(gamepadIndex, button);
        return null;
    }

    private async Task<object?> HandleSetLeftStickAsync(JsonElement? args)
    {
        var gamepadIndex = GetRequiredInt(args, "gamepadIndex");
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        await inputController.SetLeftStickAsync(gamepadIndex, x, y);
        return null;
    }

    private async Task<object?> HandleSetRightStickAsync(JsonElement? args)
    {
        var gamepadIndex = GetRequiredInt(args, "gamepadIndex");
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        await inputController.SetRightStickAsync(gamepadIndex, x, y);
        return null;
    }

    private async Task<object?> HandleSetTriggerAsync(JsonElement? args)
    {
        var gamepadIndex = GetRequiredInt(args, "gamepadIndex");
        var isLeft = GetRequiredBool(args, "isLeft");
        var value = GetRequiredFloat(args, "value");
        await inputController.SetTriggerAsync(gamepadIndex, isLeft, value);
        return null;
    }

    private async Task<object?> HandleSetGamepadConnectedAsync(JsonElement? args)
    {
        var gamepadIndex = GetRequiredInt(args, "gamepadIndex");
        var connected = GetRequiredBool(args, "connected");
        await inputController.SetGamepadConnectedAsync(gamepadIndex, connected);
        return null;
    }

    private async Task<object?> HandleTriggerActionAsync(JsonElement? args)
    {
        var actionName = GetRequiredString(args, "actionName");
        await inputController.TriggerActionAsync(actionName);
        return null;
    }

    private async Task<object?> HandleSetActionValueAsync(JsonElement? args)
    {
        var actionName = GetRequiredString(args, "actionName");
        var value = GetRequiredFloat(args, "value");
        await inputController.SetActionValueAsync(actionName, value);
        return null;
    }

    private async Task<object?> HandleSetActionVector2Async(JsonElement? args)
    {
        var actionName = GetRequiredString(args, "actionName");
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        await inputController.SetActionVector2Async(actionName, x, y);
        return null;
    }

    private async Task<object?> HandleResetAllAsync()
    {
        await inputController.ResetAllAsync();
        return null;
    }

    private object HandleIsKeyDown(JsonElement? args)
    {
        var key = GetRequiredEnum<Key>(args, "key");
        return inputController.IsKeyDown(key);
    }

    private object HandleIsMouseButtonDown(JsonElement? args)
    {
        var button = GetRequiredEnum<MouseButton>(args, "button");
        return inputController.IsMouseButtonDown(button);
    }

    private object HandleIsGamepadButtonDown(JsonElement? args)
    {
        var gamepadIndex = GetRequiredInt(args, "gamepadIndex");
        var button = GetRequiredEnum<GamepadButton>(args, "button");
        return inputController.IsGamepadButtonDown(gamepadIndex, button);
    }

    private object HandleGetMousePosition()
    {
        var (x, y) = inputController.GetMousePosition();
        return new MousePositionResult { X = x, Y = y };
    }

    private object HandleGamepadCount()
    {
        return inputController.GamepadCount;
    }

    #region Typed Argument Helpers (AOT-compatible)

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static float GetRequiredFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetSingle();
    }

    private static bool GetRequiredBool(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetBoolean();
    }

    private static T GetRequiredEnum<T>(JsonElement? args, string name) where T : struct, Enum
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        var str = prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
        return Enum.Parse<T>(str, ignoreCase: true);
    }

    private static float? GetOptionalFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetSingle();
    }

    private static T? GetOptionalEnum<T>(JsonElement? args, string name) where T : struct, Enum
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var str = prop.GetString();
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        return Enum.Parse<T>(str, ignoreCase: true);
    }

    #endregion
}
