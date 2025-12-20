using System.Numerics;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Testing.Input;

/// <summary>
/// Plays back recorded input events through a <see cref="MockInputContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// InputPlayer replays recorded input sequences by firing simulated events on
/// a mock input context at the appropriate timestamps. When synchronized with
/// a <see cref="TestClock"/>, playback is deterministic and frame-accurate.
/// </para>
/// <para>
/// Call <see cref="Update"/> each frame (or after each <see cref="TestClock.Step"/>)
/// to process events that have occurred since the last update.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var testWorld = new TestWorldBuilder()
///     .WithManualTime()
///     .WithMockInput()
///     .Build();
///
/// var recording = InputRecording.FromJson(savedJson);
/// var player = new InputPlayer(testWorld.MockInput!, testWorld.Clock!);
///
/// player.LoadRecording(recording);
/// player.Play();
///
/// while (player.IsPlaying)
/// {
///     player.Update();
///     testWorld.Step();
/// }
/// </code>
/// </example>
public sealed class InputPlayer
{
    private readonly MockInputContext inputContext;
    private readonly TestClock clock;
    private InputRecording? currentRecording;
    private int currentEventIndex;
    private float playbackStartTime;
    private bool isPlaying;
    private bool isPaused;

    /// <summary>
    /// Creates a new input player for the specified mock input context.
    /// </summary>
    /// <param name="inputContext">The mock input context to play events through.</param>
    /// <param name="clock">The test clock for timing synchronization.</param>
    public InputPlayer(MockInputContext inputContext, TestClock clock)
    {
        ArgumentNullException.ThrowIfNull(inputContext);
        ArgumentNullException.ThrowIfNull(clock);

        this.inputContext = inputContext;
        this.clock = clock;
    }

    /// <summary>
    /// Gets whether playback is currently active.
    /// </summary>
    public bool IsPlaying => isPlaying && !isPaused;

    /// <summary>
    /// Gets whether playback is paused.
    /// </summary>
    public bool IsPaused => isPaused;

    /// <summary>
    /// Gets the current playback position in milliseconds.
    /// </summary>
    public float PlaybackPosition => isPlaying ? clock.CurrentTime - playbackStartTime : 0f;

    /// <summary>
    /// Gets the currently loaded recording, or null if none is loaded.
    /// </summary>
    public InputRecording? CurrentRecording => currentRecording;

    /// <summary>
    /// Gets the progress through the current recording (0 to 1).
    /// </summary>
    public float Progress
    {
        get
        {
            if (currentRecording is null || currentRecording.Duration <= 0)
            {
                return 0f;
            }

            return Math.Clamp(PlaybackPosition / currentRecording.Duration, 0f, 1f);
        }
    }

    /// <summary>
    /// Occurs when playback completes.
    /// </summary>
    public event Action? OnPlaybackComplete;

    /// <summary>
    /// Loads a recording for playback.
    /// </summary>
    /// <param name="recording">The recording to load.</param>
    /// <exception cref="InvalidOperationException">Thrown if currently playing.</exception>
    public void LoadRecording(InputRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);

        if (isPlaying)
        {
            throw new InvalidOperationException("Cannot load a recording while playing. Call Stop() first.");
        }

        currentRecording = recording;
        currentEventIndex = 0;
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no recording is loaded.</exception>
    public void Play()
    {
        if (currentRecording is null)
        {
            throw new InvalidOperationException("No recording loaded. Call LoadRecording() first.");
        }

        if (isPaused)
        {
            // Resume from pause
            isPaused = false;
        }
        else if (!isPlaying)
        {
            // Start new playback
            isPlaying = true;
            playbackStartTime = clock.CurrentTime;
            currentEventIndex = 0;
        }
    }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    public void Pause()
    {
        if (isPlaying)
        {
            isPaused = true;
        }
    }

    /// <summary>
    /// Stops playback and resets to the beginning.
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        isPaused = false;
        currentEventIndex = 0;
    }

    /// <summary>
    /// Processes input events that should have occurred by the current clock time.
    /// </summary>
    /// <remarks>
    /// Call this method after each clock step to fire input events at the correct times.
    /// </remarks>
    public void Update()
    {
        if (!isPlaying || isPaused || currentRecording is null)
        {
            return;
        }

        var playbackTime = PlaybackPosition;
        var events = currentRecording.Events;

        // Process all events that have occurred by the current time
        while (currentEventIndex < events.Count && events[currentEventIndex].Timestamp <= playbackTime)
        {
            var evt = events[currentEventIndex];
            PlayEvent(evt);
            currentEventIndex++;
        }

        // Check if playback is complete
        if (currentEventIndex >= events.Count)
        {
            isPlaying = false;
            OnPlaybackComplete?.Invoke();
        }
    }

    /// <summary>
    /// Seeks to a specific position in the recording.
    /// </summary>
    /// <param name="positionMs">The position in milliseconds to seek to.</param>
    /// <remarks>
    /// <para>
    /// Seeking does not fire events - it only changes the playback position.
    /// Call <see cref="Update"/> after seeking to start firing events from the new position.
    /// </para>
    /// </remarks>
    public void Seek(float positionMs)
    {
        if (currentRecording is null)
        {
            return;
        }

        positionMs = Math.Clamp(positionMs, 0, currentRecording.Duration);
        playbackStartTime = clock.CurrentTime - positionMs;

        // Find the event index for the new position
        currentEventIndex = 0;
        var events = currentRecording.Events;
        while (currentEventIndex < events.Count && events[currentEventIndex].Timestamp < positionMs)
        {
            currentEventIndex++;
        }
    }

    private void PlayEvent(RecordedInputEvent evt)
    {
        switch (evt)
        {
            case RecordedKeyDownEvent k:
                inputContext.MockKeyboard.SimulateKeyDown(k.Key, k.Modifiers, k.IsRepeat);
                break;

            case RecordedKeyUpEvent k:
                inputContext.MockKeyboard.SimulateKeyUp(k.Key, k.Modifiers);
                break;

            case RecordedTextInputEvent t:
                inputContext.MockKeyboard.SimulateTextInput(t.Character);
                break;

            case RecordedMouseMoveEvent m:
                inputContext.MockMouse.SimulateMove(new Vector2(m.PositionX, m.PositionY));
                break;

            case RecordedMouseButtonDownEvent m:
                inputContext.MockMouse.SetPosition(m.PositionX, m.PositionY);
                inputContext.MockMouse.SimulateButtonDown(m.Button, m.Modifiers);
                break;

            case RecordedMouseButtonUpEvent m:
                inputContext.MockMouse.SetPosition(m.PositionX, m.PositionY);
                inputContext.MockMouse.SimulateButtonUp(m.Button, m.Modifiers);
                break;

            case RecordedMouseScrollEvent m:
                inputContext.MockMouse.SetPosition(m.PositionX, m.PositionY);
                inputContext.MockMouse.SimulateScroll(m.DeltaX, m.DeltaY);
                break;

            case RecordedGamepadButtonDownEvent g:
                inputContext.GetMockGamepad(g.GamepadIndex).SimulateButtonDown(g.Button);
                break;

            case RecordedGamepadButtonUpEvent g:
                inputContext.GetMockGamepad(g.GamepadIndex).SimulateButtonUp(g.Button);
                break;

            case RecordedGamepadAxisEvent g:
                inputContext.GetMockGamepad(g.GamepadIndex).SimulateAxisChange(g.Axis, g.Value);
                break;
        }
    }
}
