namespace KeenEyes.Replay;

/// <summary>
/// Represents the type of an input event in the replay system.
/// </summary>
/// <remarks>
/// <para>
/// Input event types categorize user input for recording and playback.
/// This enables deterministic replay of gameplay by capturing all user
/// interactions including keyboard, mouse, and gamepad inputs.
/// </para>
/// <para>
/// For application-specific input types not covered by the built-in types,
/// use <see cref="Custom"/> and specify the custom type name in
/// <see cref="InputEvent.CustomType"/>.
/// </para>
/// </remarks>
public enum InputEventType
{
    /// <summary>
    /// A keyboard key was pressed down.
    /// </summary>
    /// <remarks>
    /// The <see cref="InputEvent.Key"/> property contains the key identifier.
    /// </remarks>
    KeyDown = 0,

    /// <summary>
    /// A keyboard key was released.
    /// </summary>
    /// <remarks>
    /// The <see cref="InputEvent.Key"/> property contains the key identifier.
    /// </remarks>
    KeyUp = 1,

    /// <summary>
    /// The mouse cursor moved.
    /// </summary>
    /// <remarks>
    /// The <see cref="InputEvent.Position"/> property contains the new cursor position.
    /// </remarks>
    MouseMove = 2,

    /// <summary>
    /// A mouse button was pressed down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="InputEvent.Key"/> property contains the button identifier
    /// (e.g., "Left", "Right", "Middle").
    /// </para>
    /// <para>
    /// The <see cref="InputEvent.Position"/> property contains the click position.
    /// </para>
    /// </remarks>
    MouseButtonDown = 3,

    /// <summary>
    /// A mouse button was released.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="InputEvent.Key"/> property contains the button identifier.
    /// </para>
    /// <para>
    /// The <see cref="InputEvent.Position"/> property contains the release position.
    /// </para>
    /// </remarks>
    MouseButtonUp = 4,

    /// <summary>
    /// The mouse wheel was scrolled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="InputEvent.Value"/> property contains the scroll delta.
    /// Positive values indicate scrolling up/forward, negative values indicate
    /// scrolling down/backward.
    /// </para>
    /// <para>
    /// The <see cref="InputEvent.Position"/> property contains the cursor position.
    /// </para>
    /// </remarks>
    MouseWheel = 5,

    /// <summary>
    /// A gamepad button was pressed or released.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="InputEvent.Key"/> property contains the button identifier.
    /// </para>
    /// <para>
    /// The <see cref="InputEvent.Value"/> property is 1.0 for pressed, 0.0 for released.
    /// </para>
    /// </remarks>
    GamepadButton = 6,

    /// <summary>
    /// A gamepad analog axis changed value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="InputEvent.Key"/> property contains the axis identifier
    /// (e.g., "LeftStickX", "RightTrigger").
    /// </para>
    /// <para>
    /// The <see cref="InputEvent.Value"/> property contains the axis value,
    /// typically in the range [-1.0, 1.0] for sticks or [0.0, 1.0] for triggers.
    /// </para>
    /// </remarks>
    GamepadAxis = 7,

    /// <summary>
    /// A custom input event type defined by the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this type for application-specific inputs not covered by the built-in types,
    /// such as touch gestures, voice commands, or VR controller inputs.
    /// </para>
    /// <para>
    /// The <see cref="InputEvent.CustomType"/> property should contain the custom type name,
    /// and <see cref="InputEvent.CustomData"/> can store event-specific data.
    /// </para>
    /// </remarks>
    Custom = 8,
}
