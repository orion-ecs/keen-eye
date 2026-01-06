using System.Numerics;

namespace KeenEyes.Replay;

/// <summary>
/// Interface for recording input events during gameplay for replay playback.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface capture user input events and store them
/// for later replay. The recorded inputs can be synchronized with ECS frame
/// updates to enable deterministic playback.
/// </para>
/// <para>
/// The interface provides both low-level event recording (via <see cref="RecordInput"/>)
/// and convenience methods for common input types (keyboard, mouse, gamepad).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get the recorder from the world extension
/// var recorder = world.GetExtension&lt;ReplayRecorder&gt;();
///
/// // Record keyboard input
/// recorder.RecordKeyDown("Space");
///
/// // Record mouse movement
/// recorder.RecordMouseMove(new Vector2(100, 200));
///
/// // Record gamepad axis
/// recorder.RecordGamepadAxis("LeftStickX", 0.75f);
/// </code>
/// </example>
public interface IInputRecorder
{
    /// <summary>
    /// Gets a value indicating whether input recording is currently active.
    /// </summary>
    bool IsRecordingInputs { get; }

    /// <summary>
    /// Gets the current frame number for input recording.
    /// </summary>
    /// <remarks>
    /// Returns -1 if recording is not active.
    /// </remarks>
    int CurrentInputFrame { get; }

    /// <summary>
    /// Records a raw input event.
    /// </summary>
    /// <param name="inputEvent">The input event to record.</param>
    /// <remarks>
    /// <para>
    /// This is the low-level recording method that stores the event directly.
    /// The <see cref="InputEvent.Frame"/> property should be set to the current
    /// frame number, or it will be automatically set by the recorder.
    /// </para>
    /// <para>
    /// This method does nothing if recording is not active.
    /// </para>
    /// </remarks>
    void RecordInput(InputEvent inputEvent);

    /// <summary>
    /// Records a key press event.
    /// </summary>
    /// <param name="key">The key identifier (e.g., "A", "Space", "Escape").</param>
    /// <remarks>
    /// Records an <see cref="InputEventType.KeyDown"/> event for the specified key.
    /// </remarks>
    void RecordKeyDown(string key);

    /// <summary>
    /// Records a key release event.
    /// </summary>
    /// <param name="key">The key identifier.</param>
    /// <remarks>
    /// Records an <see cref="InputEventType.KeyUp"/> event for the specified key.
    /// </remarks>
    void RecordKeyUp(string key);

    /// <summary>
    /// Records a mouse movement event.
    /// </summary>
    /// <param name="position">The new cursor position.</param>
    /// <remarks>
    /// Records an <see cref="InputEventType.MouseMove"/> event with the specified position.
    /// </remarks>
    void RecordMouseMove(Vector2 position);

    /// <summary>
    /// Records a mouse button press event.
    /// </summary>
    /// <param name="button">The button identifier (e.g., "Left", "Right", "Middle").</param>
    /// <param name="position">The cursor position when the button was pressed.</param>
    /// <remarks>
    /// Records an <see cref="InputEventType.MouseButtonDown"/> event.
    /// </remarks>
    void RecordMouseButtonDown(string button, Vector2 position);

    /// <summary>
    /// Records a mouse button release event.
    /// </summary>
    /// <param name="button">The button identifier.</param>
    /// <param name="position">The cursor position when the button was released.</param>
    /// <remarks>
    /// Records an <see cref="InputEventType.MouseButtonUp"/> event.
    /// </remarks>
    void RecordMouseButtonUp(string button, Vector2 position);

    /// <summary>
    /// Records a mouse wheel scroll event.
    /// </summary>
    /// <param name="delta">The scroll delta (positive=up, negative=down).</param>
    /// <param name="position">The cursor position when scrolling.</param>
    /// <remarks>
    /// Records an <see cref="InputEventType.MouseWheel"/> event.
    /// </remarks>
    void RecordMouseWheel(float delta, Vector2 position);

    /// <summary>
    /// Records a gamepad button event.
    /// </summary>
    /// <param name="button">The button identifier (e.g., "A", "B", "Start").</param>
    /// <param name="pressed">True if the button was pressed, false if released.</param>
    /// <remarks>
    /// Records a <see cref="InputEventType.GamepadButton"/> event with
    /// <see cref="InputEvent.Value"/> set to 1.0 if pressed, 0.0 if released.
    /// </remarks>
    void RecordGamepadButton(string button, bool pressed);

    /// <summary>
    /// Records a gamepad axis value change.
    /// </summary>
    /// <param name="axis">The axis identifier (e.g., "LeftStickX", "RightTrigger").</param>
    /// <param name="value">The axis value, typically in range [-1.0, 1.0] or [0.0, 1.0].</param>
    /// <remarks>
    /// Records a <see cref="InputEventType.GamepadAxis"/> event.
    /// </remarks>
    void RecordGamepadAxis(string axis, float value);

    /// <summary>
    /// Records a custom input event.
    /// </summary>
    /// <param name="customType">The custom event type name.</param>
    /// <param name="customData">Optional event-specific data.</param>
    /// <remarks>
    /// <para>
    /// Records an <see cref="InputEventType.Custom"/> event for application-specific
    /// input types not covered by the built-in types.
    /// </para>
    /// <para>
    /// The <paramref name="customData"/> must be serializable by the replay system.
    /// </para>
    /// </remarks>
    void RecordCustomInput(string customType, object? customData = null);

    /// <summary>
    /// Records a custom input event with typed data.
    /// </summary>
    /// <typeparam name="T">The type of the custom data.</typeparam>
    /// <param name="customType">The custom event type name.</param>
    /// <param name="customData">The event-specific data.</param>
    /// <remarks>
    /// <para>
    /// This generic overload provides type safety for custom input data.
    /// The type <typeparamref name="T"/> must be serializable by the replay system.
    /// </para>
    /// </remarks>
    void RecordCustomInput<T>(string customType, T customData);
}
