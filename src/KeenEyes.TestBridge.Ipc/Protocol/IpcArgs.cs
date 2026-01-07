using KeenEyes.Input.Abstractions;

namespace KeenEyes.TestBridge.Ipc.Protocol;

#region Input Command Arguments

/// <summary>
/// Arguments for key down/up commands.
/// </summary>
public sealed record KeyActionArgs
{
    /// <summary>Gets the key.</summary>
    public Key Key { get; init; }

    /// <summary>Gets the modifiers.</summary>
    public KeyModifiers Modifiers { get; init; }
}

/// <summary>
/// Arguments for key press command.
/// </summary>
public sealed record KeyPressArgs
{
    /// <summary>Gets the key.</summary>
    public Key Key { get; init; }

    /// <summary>Gets the modifiers.</summary>
    public KeyModifiers Modifiers { get; init; }

    /// <summary>Gets the hold duration in milliseconds.</summary>
    public double? HoldDurationMs { get; init; }
}

/// <summary>
/// Arguments for type text command.
/// </summary>
public sealed record TypeTextArgs
{
    /// <summary>Gets the text to type.</summary>
    public string Text { get; init; } = "";

    /// <summary>Gets the delay between characters in milliseconds.</summary>
    public double? DelayBetweenCharsMs { get; init; }
}

/// <summary>
/// Arguments for single key query.
/// </summary>
public sealed record SingleKeyArgs
{
    /// <summary>Gets the key.</summary>
    public Key Key { get; init; }
}

/// <summary>
/// Arguments for mouse move command.
/// </summary>
public sealed record MouseMoveArgs
{
    /// <summary>Gets the X coordinate.</summary>
    public float X { get; init; }

    /// <summary>Gets the Y coordinate.</summary>
    public float Y { get; init; }
}

/// <summary>
/// Arguments for relative mouse move command.
/// </summary>
public sealed record MouseRelativeArgs
{
    /// <summary>Gets the delta X.</summary>
    public float DeltaX { get; init; }

    /// <summary>Gets the delta Y.</summary>
    public float DeltaY { get; init; }
}

/// <summary>
/// Arguments for mouse button commands.
/// </summary>
public sealed record MouseButtonArgs
{
    /// <summary>Gets the button.</summary>
    public MouseButton Button { get; init; }
}

/// <summary>
/// Arguments for click commands.
/// </summary>
public sealed record ClickArgs
{
    /// <summary>Gets the X coordinate.</summary>
    public float X { get; init; }

    /// <summary>Gets the Y coordinate.</summary>
    public float Y { get; init; }

    /// <summary>Gets the button.</summary>
    public MouseButton Button { get; init; }
}

/// <summary>
/// Arguments for drag command.
/// </summary>
public sealed record DragArgs
{
    /// <summary>Gets the start X coordinate.</summary>
    public float StartX { get; init; }

    /// <summary>Gets the start Y coordinate.</summary>
    public float StartY { get; init; }

    /// <summary>Gets the end X coordinate.</summary>
    public float EndX { get; init; }

    /// <summary>Gets the end Y coordinate.</summary>
    public float EndY { get; init; }

    /// <summary>Gets the button.</summary>
    public MouseButton Button { get; init; }
}

/// <summary>
/// Arguments for scroll command.
/// </summary>
public sealed record ScrollArgs
{
    /// <summary>Gets the delta X.</summary>
    public float DeltaX { get; init; }

    /// <summary>Gets the delta Y.</summary>
    public float DeltaY { get; init; }
}

/// <summary>
/// Arguments for gamepad button commands.
/// </summary>
public sealed record GamepadButtonArgs
{
    /// <summary>Gets the gamepad index.</summary>
    public int GamepadIndex { get; init; }

    /// <summary>Gets the button.</summary>
    public GamepadButton Button { get; init; }
}

/// <summary>
/// Arguments for stick commands.
/// </summary>
public sealed record StickArgs
{
    /// <summary>Gets the gamepad index.</summary>
    public int GamepadIndex { get; init; }

    /// <summary>Gets the X value.</summary>
    public float X { get; init; }

    /// <summary>Gets the Y value.</summary>
    public float Y { get; init; }
}

/// <summary>
/// Arguments for trigger command.
/// </summary>
public sealed record TriggerArgs
{
    /// <summary>Gets the gamepad index.</summary>
    public int GamepadIndex { get; init; }

    /// <summary>Gets whether this is the left trigger.</summary>
    public bool IsLeft { get; init; }

    /// <summary>Gets the trigger value.</summary>
    public float Value { get; init; }
}

/// <summary>
/// Arguments for gamepad connection command.
/// </summary>
public sealed record GamepadConnectionArgs
{
    /// <summary>Gets the gamepad index.</summary>
    public int GamepadIndex { get; init; }

    /// <summary>Gets whether the gamepad is connected.</summary>
    public bool Connected { get; init; }
}

/// <summary>
/// Arguments for action trigger command.
/// </summary>
public sealed record ActionNameArgs
{
    /// <summary>Gets the action name.</summary>
    public string ActionName { get; init; } = "";
}

/// <summary>
/// Arguments for action value command.
/// </summary>
public sealed record ActionValueArgs
{
    /// <summary>Gets the action name.</summary>
    public string ActionName { get; init; } = "";

    /// <summary>Gets the value.</summary>
    public float Value { get; init; }
}

/// <summary>
/// Arguments for action vector2 command.
/// </summary>
public sealed record ActionVector2Args
{
    /// <summary>Gets the action name.</summary>
    public string ActionName { get; init; } = "";

    /// <summary>Gets the X value.</summary>
    public float X { get; init; }

    /// <summary>Gets the Y value.</summary>
    public float Y { get; init; }
}

#endregion

#region State Command Arguments

/// <summary>
/// Arguments for entity ID queries.
/// </summary>
public sealed record EntityIdArgs
{
    /// <summary>Gets the entity ID.</summary>
    public int EntityId { get; init; }
}

/// <summary>
/// Arguments for name queries.
/// </summary>
public sealed record NameArgs
{
    /// <summary>Gets the name.</summary>
    public string Name { get; init; } = "";
}

/// <summary>
/// Arguments for component queries.
/// </summary>
public sealed record ComponentArgs
{
    /// <summary>Gets the entity ID.</summary>
    public int EntityId { get; init; }

    /// <summary>Gets the component type name.</summary>
    public string ComponentTypeName { get; init; } = "";
}

/// <summary>
/// Arguments for type name queries.
/// </summary>
public sealed record TypeNameArgs
{
    /// <summary>Gets the type name.</summary>
    public string TypeName { get; init; } = "";
}

/// <summary>
/// Arguments for performance metrics queries.
/// </summary>
public sealed record FrameCountArgs
{
    /// <summary>Gets the frame count.</summary>
    public int FrameCount { get; init; }
}

/// <summary>
/// Arguments for tag queries.
/// </summary>
public sealed record TagArgs
{
    /// <summary>Gets the tag.</summary>
    public string Tag { get; init; } = "";
}

/// <summary>
/// Arguments for parent ID queries.
/// </summary>
public sealed record ParentIdArgs
{
    /// <summary>Gets the parent ID.</summary>
    public int ParentId { get; init; }
}

#endregion
