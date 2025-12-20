using System.Numerics;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Testing.Input;

/// <summary>
/// Records input events from an <see cref="IInputContext"/> for later playback.
/// </summary>
/// <remarks>
/// <para>
/// InputRecorder attaches to an input context and captures all keyboard, mouse, and
/// gamepad events with precise timestamps. When used with a <see cref="TestClock"/>,
/// timestamps are synchronized with simulation time for deterministic replay.
/// </para>
/// <para>
/// The recorder supports starting and stopping recording, allowing capture of specific
/// sequences of user input.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var testWorld = new TestWorldBuilder()
///     .WithManualTime()
///     .WithMockInput()
///     .Build();
///
/// var recorder = new InputRecorder(testWorld.MockInput!, testWorld.Clock);
///
/// recorder.StartRecording();
///
/// // Simulate input
/// testWorld.MockInput!.SimulateKeyDown(Key.W);
/// testWorld.Step();
/// testWorld.MockInput.SimulateKeyUp(Key.W);
///
/// var recording = recorder.StopRecording();
/// recording.ToJson(); // Save for later
/// </code>
/// </example>
public sealed class InputRecorder : IDisposable
{
    private readonly IInputContext inputContext;
    private readonly TestClock? clock;
    private readonly List<Action> unsubscribeActions = [];
    private InputRecording? currentRecording;
    private bool isRecording;
    private bool disposed;

    /// <summary>
    /// Creates a new input recorder for the specified input context.
    /// </summary>
    /// <param name="inputContext">The input context to record from.</param>
    /// <param name="clock">Optional clock for timestamp synchronization. If null, uses 0 for all timestamps.</param>
    public InputRecorder(IInputContext inputContext, TestClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(inputContext);

        this.inputContext = inputContext;
        this.clock = clock;

        SubscribeToEvents();
    }

    /// <summary>
    /// Gets whether the recorder is currently recording.
    /// </summary>
    public bool IsRecording => isRecording;

    /// <summary>
    /// Gets the current recording in progress, or null if not recording.
    /// </summary>
    public InputRecording? CurrentRecording => currentRecording;

    /// <summary>
    /// Starts a new recording session.
    /// </summary>
    /// <param name="name">Optional name for the recording.</param>
    /// <param name="description">Optional description for the recording.</param>
    /// <exception cref="InvalidOperationException">Thrown if already recording.</exception>
    public void StartRecording(string? name = null, string? description = null)
    {
        if (isRecording)
        {
            throw new InvalidOperationException("Already recording. Call StopRecording() first.");
        }

        currentRecording = new InputRecording
        {
            Metadata = new InputRecordingMetadata
            {
                Name = name,
                Description = description,
                RecordedAt = DateTime.UtcNow
            }
        };

        isRecording = true;
    }

    /// <summary>
    /// Stops the current recording session and returns the recording.
    /// </summary>
    /// <returns>The completed recording.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not currently recording.</exception>
    public InputRecording StopRecording()
    {
        if (!isRecording || currentRecording is null)
        {
            throw new InvalidOperationException("Not currently recording. Call StartRecording() first.");
        }

        var recording = currentRecording;
        currentRecording = null;
        isRecording = false;

        return recording;
    }

    /// <summary>
    /// Cancels the current recording session and discards the recording.
    /// </summary>
    public void CancelRecording()
    {
        currentRecording = null;
        isRecording = false;
    }

    /// <summary>
    /// Disposes the recorder and unsubscribes from all events.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var unsubscribe in unsubscribeActions)
        {
            unsubscribe();
        }

        unsubscribeActions.Clear();
    }

    private float GetTimestamp()
    {
        return clock?.CurrentTime ?? 0f;
    }

    private void RecordEvent(RecordedInputEvent evt)
    {
        if (isRecording && currentRecording is not null)
        {
            currentRecording.Add(evt);
        }
    }

    private void SubscribeToEvents()
    {
        // Keyboard events
        inputContext.OnKeyDown += OnKeyDown;
        unsubscribeActions.Add(() => inputContext.OnKeyDown -= OnKeyDown);

        inputContext.OnKeyUp += OnKeyUp;
        unsubscribeActions.Add(() => inputContext.OnKeyUp -= OnKeyUp);

        inputContext.OnTextInput += OnTextInput;
        unsubscribeActions.Add(() => inputContext.OnTextInput -= OnTextInput);

        // Mouse events
        inputContext.OnMouseMove += OnMouseMove;
        unsubscribeActions.Add(() => inputContext.OnMouseMove -= OnMouseMove);

        inputContext.OnMouseButtonDown += OnMouseButtonDown;
        unsubscribeActions.Add(() => inputContext.OnMouseButtonDown -= OnMouseButtonDown);

        inputContext.OnMouseButtonUp += OnMouseButtonUp;
        unsubscribeActions.Add(() => inputContext.OnMouseButtonUp -= OnMouseButtonUp);

        inputContext.OnMouseScroll += OnMouseScroll;
        unsubscribeActions.Add(() => inputContext.OnMouseScroll -= OnMouseScroll);

        // Gamepad events
        inputContext.OnGamepadButtonDown += OnGamepadButtonDown;
        unsubscribeActions.Add(() => inputContext.OnGamepadButtonDown -= OnGamepadButtonDown);

        inputContext.OnGamepadButtonUp += OnGamepadButtonUp;
        unsubscribeActions.Add(() => inputContext.OnGamepadButtonUp -= OnGamepadButtonUp);
    }

    private void OnKeyDown(IKeyboard keyboard, KeyEventArgs args)
    {
        RecordEvent(new RecordedKeyDownEvent(GetTimestamp(), args.Key, args.Modifiers, args.IsRepeat));
    }

    private void OnKeyUp(IKeyboard keyboard, KeyEventArgs args)
    {
        RecordEvent(new RecordedKeyUpEvent(GetTimestamp(), args.Key, args.Modifiers));
    }

    private void OnTextInput(IKeyboard keyboard, char character)
    {
        RecordEvent(new RecordedTextInputEvent(GetTimestamp(), character));
    }

    private void OnMouseMove(IMouse mouse, MouseMoveEventArgs args)
    {
        RecordEvent(new RecordedMouseMoveEvent(GetTimestamp(), args.Position.X, args.Position.Y, args.Delta.X, args.Delta.Y));
    }

    private void OnMouseButtonDown(IMouse mouse, MouseButtonEventArgs args)
    {
        RecordEvent(new RecordedMouseButtonDownEvent(GetTimestamp(), args.Button, args.Position.X, args.Position.Y, args.Modifiers));
    }

    private void OnMouseButtonUp(IMouse mouse, MouseButtonEventArgs args)
    {
        RecordEvent(new RecordedMouseButtonUpEvent(GetTimestamp(), args.Button, args.Position.X, args.Position.Y, args.Modifiers));
    }

    private void OnMouseScroll(IMouse mouse, MouseScrollEventArgs args)
    {
        RecordEvent(new RecordedMouseScrollEvent(GetTimestamp(), args.Delta.X, args.Delta.Y, args.Position.X, args.Position.Y));
    }

    private void OnGamepadButtonDown(IGamepad gamepad, GamepadButtonEventArgs args)
    {
        RecordEvent(new RecordedGamepadButtonDownEvent(GetTimestamp(), args.GamepadIndex, args.Button));
    }

    private void OnGamepadButtonUp(IGamepad gamepad, GamepadButtonEventArgs args)
    {
        RecordEvent(new RecordedGamepadButtonUpEvent(GetTimestamp(), args.GamepadIndex, args.Button));
    }
}
