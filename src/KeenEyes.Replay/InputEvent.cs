using System.Numerics;

namespace KeenEyes.Replay;

/// <summary>
/// Represents a recorded input event for replay playback.
/// </summary>
/// <remarks>
/// <para>
/// Input events capture user interactions such as keyboard presses, mouse movements,
/// and gamepad inputs. These events are stored per-frame in the replay data and
/// can be replayed to reproduce the original gameplay.
/// </para>
/// <para>
/// The struct is immutable with init-only properties to ensure thread safety and
/// prevent accidental modification after creation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a keyboard event
/// var keyEvent = new InputEvent
/// {
///     Type = InputEventType.KeyDown,
///     Frame = 42,
///     Key = "Space"
/// };
///
/// // Create a mouse move event
/// var mouseEvent = new InputEvent
/// {
///     Type = InputEventType.MouseMove,
///     Frame = 42,
///     Position = new Vector2(100f, 200f)
/// };
///
/// // Create a gamepad axis event
/// var gamepadEvent = new InputEvent
/// {
///     Type = InputEventType.GamepadAxis,
///     Frame = 42,
///     Key = "LeftStickX",
///     Value = 0.75f
/// };
/// </code>
/// </example>
public readonly struct InputEvent
{
    /// <summary>
    /// Gets the type of this input event.
    /// </summary>
    /// <remarks>
    /// Determines how other properties should be interpreted. For example,
    /// <see cref="InputEventType.MouseMove"/> uses <see cref="Position"/>,
    /// while <see cref="InputEventType.GamepadAxis"/> uses <see cref="Key"/>
    /// and <see cref="Value"/>.
    /// </remarks>
    public InputEventType Type { get; init; }

    /// <summary>
    /// Gets the frame number when this input event occurred.
    /// </summary>
    /// <remarks>
    /// Frame numbers correspond to <see cref="ReplayFrame.FrameNumber"/> and
    /// enable synchronization between input events and world state during playback.
    /// </remarks>
    public int Frame { get; init; }

    /// <summary>
    /// Gets the key or button identifier for this event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interpretation depends on <see cref="Type"/>:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="InputEventType.KeyDown"/>/<see cref="InputEventType.KeyUp"/>:
    /// Keyboard key name (e.g., "A", "Space", "Escape", "F1").
    /// </description></item>
    /// <item><description>
    /// <see cref="InputEventType.MouseButtonDown"/>/<see cref="InputEventType.MouseButtonUp"/>:
    /// Mouse button name (e.g., "Left", "Right", "Middle", "X1", "X2").
    /// </description></item>
    /// <item><description>
    /// <see cref="InputEventType.GamepadButton"/>:
    /// Gamepad button name (e.g., "A", "B", "LeftBumper", "Start").
    /// </description></item>
    /// <item><description>
    /// <see cref="InputEventType.GamepadAxis"/>:
    /// Axis name (e.g., "LeftStickX", "LeftStickY", "RightTrigger").
    /// </description></item>
    /// </list>
    /// <para>
    /// For other event types, this property may be null.
    /// </para>
    /// </remarks>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the numeric value associated with this event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interpretation depends on <see cref="Type"/>:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="InputEventType.MouseWheel"/>: Scroll delta (positive=up, negative=down).
    /// </description></item>
    /// <item><description>
    /// <see cref="InputEventType.GamepadButton"/>: 1.0 for pressed, 0.0 for released.
    /// </description></item>
    /// <item><description>
    /// <see cref="InputEventType.GamepadAxis"/>: Axis value, typically [-1.0, 1.0]
    /// for sticks or [0.0, 1.0] for triggers.
    /// </description></item>
    /// </list>
    /// </remarks>
    public float Value { get; init; }

    /// <summary>
    /// Gets the position associated with this event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used for mouse events to record cursor position:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="InputEventType.MouseMove"/>: New cursor position.
    /// </description></item>
    /// <item><description>
    /// <see cref="InputEventType.MouseButtonDown"/>/<see cref="InputEventType.MouseButtonUp"/>:
    /// Position where the button was pressed/released.
    /// </description></item>
    /// <item><description>
    /// <see cref="InputEventType.MouseWheel"/>: Cursor position when scrolling.
    /// </description></item>
    /// </list>
    /// <para>
    /// For non-mouse events, this defaults to <see cref="Vector2.Zero"/>.
    /// </para>
    /// </remarks>
    public Vector2 Position { get; init; }

    /// <summary>
    /// Gets the custom event type name for <see cref="InputEventType.Custom"/> events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property identifies application-specific input types not covered by
    /// the built-in <see cref="InputEventType"/> values. Use this for custom
    /// input sources like touch gestures, VR controllers, or voice commands.
    /// </para>
    /// <para>
    /// Only relevant when <see cref="Type"/> is <see cref="InputEventType.Custom"/>.
    /// </para>
    /// </remarks>
    public string? CustomType { get; init; }

    /// <summary>
    /// Gets custom data associated with this event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <see cref="InputEventType.Custom"/> events, this can store any
    /// serializable data specific to the custom input type. The data must be
    /// serializable by the replay system.
    /// </para>
    /// <para>
    /// Common uses include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Touch gesture parameters (pinch scale, rotation angle)</description></item>
    /// <item><description>VR controller tracking data</description></item>
    /// <item><description>Voice command transcription</description></item>
    /// <item><description>Multi-touch point arrays</description></item>
    /// </list>
    /// </remarks>
    public object? CustomData { get; init; }

    /// <summary>
    /// Gets the timestamp of this event within the frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Represents the offset from the start of the frame when this input occurred.
    /// This enables accurate sub-frame input timing during playback when multiple
    /// inputs occur within the same frame.
    /// </para>
    /// <para>
    /// Default is <see cref="TimeSpan.Zero"/>, indicating the input occurred at
    /// the start of the frame.
    /// </para>
    /// </remarks>
    public TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Returns a string representation of this input event.
    /// </summary>
    /// <returns>A string describing the event type, frame, and relevant data.</returns>
    public override string ToString()
    {
        return Type switch
        {
            InputEventType.KeyDown or InputEventType.KeyUp =>
                $"{Type}(Frame={Frame}, Key={Key})",
            InputEventType.MouseMove =>
                $"{Type}(Frame={Frame}, Position={Position})",
            InputEventType.MouseButtonDown or InputEventType.MouseButtonUp =>
                $"{Type}(Frame={Frame}, Button={Key}, Position={Position})",
            InputEventType.MouseWheel =>
                $"{Type}(Frame={Frame}, Delta={Value}, Position={Position})",
            InputEventType.GamepadButton =>
                $"{Type}(Frame={Frame}, Button={Key}, Pressed={Value > 0.5f})",
            InputEventType.GamepadAxis =>
                $"{Type}(Frame={Frame}, Axis={Key}, Value={Value})",
            InputEventType.Custom =>
                $"{Type}(Frame={Frame}, CustomType={CustomType})",
            _ => $"{Type}(Frame={Frame})"
        };
    }
}
