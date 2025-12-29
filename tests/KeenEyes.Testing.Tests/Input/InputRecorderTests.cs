using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Testing.Tests.Input;

public class InputRecorderTests
{
    #region Construction

    [Fact]
    public void Constructor_CreatesRecorderWithInputContext()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);

        Assert.False(recorder.IsRecording);
        Assert.Null(recorder.CurrentRecording);
    }

    [Fact]
    public void Constructor_WithClock_CreatesRecorder()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        using var recorder = new InputRecorder(input, clock);

        Assert.False(recorder.IsRecording);
        Assert.Null(recorder.CurrentRecording);
    }

    [Fact]
    public void Constructor_WithNullInputContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InputRecorder(null!));
    }

    #endregion

    #region Start and StopRecording

    [Fact]
    public void StartRecording_BeginRecording()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);

        recorder.StartRecording();

        Assert.True(recorder.IsRecording);
        Assert.NotNull(recorder.CurrentRecording);
    }

    [Fact]
    public void StartRecording_WithNameAndDescription_SetsMetadata()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);

        recorder.StartRecording("Test Recording", "A test");

        Assert.Equal("Test Recording", recorder.CurrentRecording!.Metadata.Name);
        Assert.Equal("A test", recorder.CurrentRecording!.Metadata.Description);
        Assert.NotNull(recorder.CurrentRecording!.Metadata.RecordedAt);
    }

    [Fact]
    public void StartRecording_WhenAlreadyRecording_ThrowsInvalidOperationException()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        var ex = Assert.Throws<InvalidOperationException>(() => recorder.StartRecording());
        Assert.Equal("Already recording. Call StopRecording() first.", ex.Message);
    }

    [Fact]
    public void StopRecording_ReturnsRecording()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        var recording = recorder.StopRecording();

        Assert.NotNull(recording);
        Assert.False(recorder.IsRecording);
        Assert.Null(recorder.CurrentRecording);
    }

    [Fact]
    public void StopRecording_WhenNotRecording_ThrowsInvalidOperationException()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);

        var ex = Assert.Throws<InvalidOperationException>(() => recorder.StopRecording());
        Assert.Equal("Not currently recording. Call StartRecording() first.", ex.Message);
    }

    [Fact]
    public void CancelRecording_DiscardsRecording()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        recorder.CancelRecording();

        Assert.False(recorder.IsRecording);
        Assert.Null(recorder.CurrentRecording);
    }

    #endregion

    #region Keyboard Event Recording

    [Fact]
    public void Recorder_CapturesKeyDownEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.MockKeyboard.SimulateKeyDown(Key.W, KeyModifiers.Control, false);

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedKeyDownEvent>(recording.Events[0]);
        Assert.Equal(Key.W, evt.Key);
        Assert.Equal(KeyModifiers.Control, evt.Modifiers);
        Assert.False(evt.IsRepeat);
    }

    [Fact]
    public void Recorder_CapturesKeyUpEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.MockKeyboard.SimulateKeyUp(Key.W, KeyModifiers.Shift);

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedKeyUpEvent>(recording.Events[0]);
        Assert.Equal(Key.W, evt.Key);
        Assert.Equal(KeyModifiers.Shift, evt.Modifiers);
    }

    [Fact]
    public void Recorder_CapturesTextInputEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.MockKeyboard.SimulateTextInput('a');

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedTextInputEvent>(recording.Events[0]);
        Assert.Equal('a', evt.Character);
    }

    #endregion

    #region Mouse Event Recording

    [Fact]
    public void Recorder_CapturesMouseMoveEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.MockMouse.SimulateMove(new System.Numerics.Vector2(100f, 200f));

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedMouseMoveEvent>(recording.Events[0]);
        Assert.Equal(100f, evt.PositionX);
        Assert.Equal(200f, evt.PositionY);
    }

    [Fact]
    public void Recorder_CapturesMouseButtonDownEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.MockMouse.SetPosition(150f, 250f);
        input.MockMouse.SimulateButtonDown(MouseButton.Left, KeyModifiers.Alt);

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedMouseButtonDownEvent>(recording.Events[0]);
        Assert.Equal(MouseButton.Left, evt.Button);
        Assert.Equal(150f, evt.PositionX);
        Assert.Equal(250f, evt.PositionY);
        Assert.Equal(KeyModifiers.Alt, evt.Modifiers);
    }

    [Fact]
    public void Recorder_CapturesMouseButtonUpEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.MockMouse.SetPosition(150f, 250f);
        input.MockMouse.SimulateButtonUp(MouseButton.Right, KeyModifiers.None);

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedMouseButtonUpEvent>(recording.Events[0]);
        Assert.Equal(MouseButton.Right, evt.Button);
        Assert.Equal(150f, evt.PositionX);
        Assert.Equal(250f, evt.PositionY);
    }

    [Fact]
    public void Recorder_CapturesMouseScrollEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.MockMouse.SetPosition(100f, 100f);
        input.MockMouse.SimulateScroll(1f, -1f);

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedMouseScrollEvent>(recording.Events[0]);
        Assert.Equal(1f, evt.DeltaX);
        Assert.Equal(-1f, evt.DeltaY);
        Assert.Equal(100f, evt.PositionX);
        Assert.Equal(100f, evt.PositionY);
    }

    #endregion

    #region Gamepad Event Recording

    [Fact]
    public void Recorder_CapturesGamepadButtonDownEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.GetMockGamepad(0).SimulateButtonDown(GamepadButton.South);

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedGamepadButtonDownEvent>(recording.Events[0]);
        Assert.Equal(0, evt.GamepadIndex);
        Assert.Equal(GamepadButton.South, evt.Button);
    }

    [Fact]
    public void Recorder_CapturesGamepadButtonUpEvents()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);
        recorder.StartRecording();

        input.GetMockGamepad(0).SimulateButtonUp(GamepadButton.East);

        var recording = recorder.StopRecording();
        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedGamepadButtonUpEvent>(recording.Events[0]);
        Assert.Equal(0, evt.GamepadIndex);
        Assert.Equal(GamepadButton.East, evt.Button);
    }

    #endregion

    #region Timestamp Synchronization

    [Fact]
    public void Recorder_WithClock_UsesClockTimeForTimestamps()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        using var recorder = new InputRecorder(input, clock);
        recorder.StartRecording();

        clock.StepByTime(100f);
        input.MockKeyboard.SimulateKeyDown(Key.W);

        clock.StepByTime(100f);
        input.MockKeyboard.SimulateKeyUp(Key.W);

        var recording = recorder.StopRecording();
        Assert.Equal(2, recording.Count);
        Assert.Equal(100f, recording.Events[0].Timestamp);
        Assert.Equal(200f, recording.Events[1].Timestamp);
    }

    [Fact]
    public void Recorder_WithoutClock_UsesZeroForTimestamps()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input, null);
        recorder.StartRecording();

        input.MockKeyboard.SimulateKeyDown(Key.W);
        input.MockKeyboard.SimulateKeyUp(Key.W);

        var recording = recorder.StopRecording();
        Assert.Equal(2, recording.Count);
        Assert.Equal(0f, recording.Events[0].Timestamp);
        Assert.Equal(0f, recording.Events[1].Timestamp);
    }

    #endregion

    #region Event Capture Control

    [Fact]
    public void Recorder_OnlyCapturesEventsWhileRecording()
    {
        using var input = new MockInputContext();
        using var recorder = new InputRecorder(input);

        // Before recording
        input.MockKeyboard.SimulateKeyDown(Key.W);

        recorder.StartRecording();

        // During recording
        input.MockKeyboard.SimulateKeyDown(Key.A);

        var recording = recorder.StopRecording();

        // After recording
        input.MockKeyboard.SimulateKeyDown(Key.S);

        Assert.Single(recording.Events);
        var evt = Assert.IsType<RecordedKeyDownEvent>(recording.Events[0]);
        Assert.Equal(Key.A, evt.Key);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        using var input = new MockInputContext();
        var recorder = new InputRecorder(input);
        recorder.StartRecording();

        var initialCount = recorder.CurrentRecording!.Count;
        recorder.Dispose();

        // Events should not be recorded after dispose
        input.MockKeyboard.SimulateKeyDown(Key.W);

        // CurrentRecording may or may not be null after dispose, but it should not have recorded the event
        if (recorder.CurrentRecording != null)
        {
            Assert.Equal(initialCount, recorder.CurrentRecording.Count);
        }
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        using var input = new MockInputContext();
        var recorder = new InputRecorder(input);

        recorder.Dispose();
        recorder.Dispose();

        // Should not throw
    }

    #endregion
}
