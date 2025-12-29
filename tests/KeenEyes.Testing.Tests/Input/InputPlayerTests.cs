using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Testing.Tests.Input;

public class InputPlayerTests
{
    #region Construction

    [Fact]
    public void Constructor_CreatesPlayer()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);

        Assert.False(player.IsPlaying);
        Assert.False(player.IsPaused);
        Assert.Equal(0f, player.PlaybackPosition);
        Assert.Null(player.CurrentRecording);
    }

    [Fact]
    public void Constructor_WithNullInputContext_ThrowsArgumentNullException()
    {
        var clock = new TestClock();

        Assert.Throws<ArgumentNullException>(() => new InputPlayer(null!, clock));
    }

    [Fact]
    public void Constructor_WithNullClock_ThrowsArgumentNullException()
    {
        using var input = new MockInputContext();

        Assert.Throws<ArgumentNullException>(() => new InputPlayer(input, null!));
    }

    #endregion

    #region LoadRecording

    [Fact]
    public void LoadRecording_LoadsRecordingSuccessfully()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);

        Assert.Equal(recording, player.CurrentRecording);
    }

    [Fact]
    public void LoadRecording_WithNullRecording_ThrowsArgumentNullException()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);

        Assert.Throws<ArgumentNullException>(() => player.LoadRecording(null!));
    }

    [Fact]
    public void LoadRecording_WhilePlaying_ThrowsInvalidOperationException()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);
        player.Play();

        var newRecording = new InputRecording();
        var ex = Assert.Throws<InvalidOperationException>(() => player.LoadRecording(newRecording));
        Assert.Equal("Cannot load a recording while playing. Call Stop() first.", ex.Message);
    }

    #endregion

    #region Play, Pause, Stop

    [Fact]
    public void Play_StartsPlayback()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);
        player.Play();

        Assert.True(player.IsPlaying);
        Assert.False(player.IsPaused);
    }

    [Fact]
    public void Play_WithoutRecording_ThrowsInvalidOperationException()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);

        var ex = Assert.Throws<InvalidOperationException>(() => player.Play());
        Assert.Equal("No recording loaded. Call LoadRecording() first.", ex.Message);
    }

    [Fact]
    public void Pause_PausesPlayback()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);
        player.Play();
        player.Pause();

        Assert.True(player.IsPaused);
        Assert.False(player.IsPlaying);
    }

    [Fact]
    public void Play_AfterPause_ResumesPlayback()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);
        player.Play();
        player.Pause();
        player.Play();

        Assert.True(player.IsPlaying);
        Assert.False(player.IsPaused);
    }

    [Fact]
    public void Stop_StopsPlayback()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);
        player.Play();
        player.Stop();

        Assert.False(player.IsPlaying);
        Assert.False(player.IsPaused);
    }

    #endregion

    #region Update and Event Playback

    [Fact]
    public void Update_PlaysEventsAtCorrectTime()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));

        bool keyDownFired = false;
        bool keyUpFired = false;
        input.OnKeyDown += (_, _) => keyDownFired = true;
        input.OnKeyUp += (_, _) => keyUpFired = true;

        player.LoadRecording(recording);
        player.Play();

        // At time 0, no events should fire
        player.Update();
        Assert.False(keyDownFired);
        Assert.False(keyUpFired);

        // Advance to 100ms - key down should fire
        clock.StepByTime(100f);
        player.Update();
        Assert.True(keyDownFired);
        Assert.False(keyUpFired);

        // Advance to 200ms - key up should fire
        clock.StepByTime(100f);
        player.Update();
        Assert.True(keyUpFired);
    }

    [Fact]
    public void Update_WhenNotPlaying_DoesNothing()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        bool keyDownFired = false;
        input.OnKeyDown += (_, _) => keyDownFired = true;

        player.LoadRecording(recording);
        clock.StepByTime(100f);
        player.Update();

        Assert.False(keyDownFired);
    }

    [Fact]
    public void Update_WhenPaused_DoesNotPlayEvents()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        bool keyDownFired = false;
        input.OnKeyDown += (_, _) => keyDownFired = true;

        player.LoadRecording(recording);
        player.Play();
        player.Pause();

        clock.StepByTime(100f);
        player.Update();

        Assert.False(keyDownFired);
    }

    [Fact]
    public void Update_FiresOnPlaybackCompleteWhenFinished()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        bool completeFired = false;
        player.OnPlaybackComplete += () => completeFired = true;

        player.LoadRecording(recording);
        player.Play();

        clock.StepByTime(100f);
        player.Update();

        Assert.True(completeFired);
        Assert.False(player.IsPlaying);
    }

    #endregion

    #region Seek

    [Fact]
    public void Seek_ChangesPlaybackPosition()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));

        player.LoadRecording(recording);
        player.Play();
        player.Seek(150f);

        // PlaybackPosition should be 150 now
        Assert.Equal(150f, player.PlaybackPosition, 1f);
    }

    [Fact]
    public void Seek_ClampsToRecordingDuration()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);
        player.Play();
        player.Seek(500f); // Beyond recording duration

        Assert.Equal(100f, player.PlaybackPosition, 1f);
    }

    [Fact]
    public void Seek_WithNegativePosition_ClampsToZero()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        player.LoadRecording(recording);
        player.Play();
        clock.StepByTime(50f);
        player.Seek(-10f);

        Assert.Equal(0f, player.PlaybackPosition, 1f);
    }

    #endregion

    #region Progress

    [Fact]
    public void Progress_ReturnsCorrectValue()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));

        player.LoadRecording(recording);
        player.Play();

        clock.StepByTime(100f);
        player.Update();

        Assert.Equal(0.5f, player.Progress, 0.01f);
    }

    [Fact]
    public void Progress_WithNoRecording_ReturnsZero()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);

        Assert.Equal(0f, player.Progress);
    }

    #endregion

    #region All Event Type Playback

    [Fact]
    public void Update_PlaysMouseMoveEvents()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedMouseMoveEvent(100f, 150f, 250f, 10f, 20f));

        bool fired = false;
        input.OnMouseMove += (_, _) => fired = true;

        player.LoadRecording(recording);
        player.Play();
        clock.StepByTime(100f);
        player.Update();

        Assert.True(fired);
    }

    [Fact]
    public void Update_PlaysMouseButtonEvents()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedMouseButtonDownEvent(100f, MouseButton.Left, 100f, 100f, KeyModifiers.None));
        recording.Add(new RecordedMouseButtonUpEvent(200f, MouseButton.Left, 100f, 100f, KeyModifiers.None));

        int downCount = 0;
        int upCount = 0;
        input.OnMouseButtonDown += (_, _) => downCount++;
        input.OnMouseButtonUp += (_, _) => upCount++;

        player.LoadRecording(recording);
        player.Play();
        clock.StepByTime(100f);
        player.Update();
        Assert.Equal(1, downCount);

        clock.StepByTime(100f);
        player.Update();
        Assert.Equal(1, upCount);
    }

    [Fact]
    public void Update_PlaysMouseScrollEvents()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedMouseScrollEvent(100f, 1f, -1f, 100f, 100f));

        bool fired = false;
        input.OnMouseScroll += (_, _) => fired = true;

        player.LoadRecording(recording);
        player.Play();
        clock.StepByTime(100f);
        player.Update();

        Assert.True(fired);
    }

    [Fact]
    public void Update_PlaysGamepadButtonEvents()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedGamepadButtonDownEvent(100f, 0, GamepadButton.South));
        recording.Add(new RecordedGamepadButtonUpEvent(200f, 0, GamepadButton.South));

        int downCount = 0;
        int upCount = 0;
        input.OnGamepadButtonDown += (_, _) => downCount++;
        input.OnGamepadButtonUp += (_, _) => upCount++;

        player.LoadRecording(recording);
        player.Play();
        clock.StepByTime(100f);
        player.Update();
        Assert.Equal(1, downCount);

        clock.StepByTime(100f);
        player.Update();
        Assert.Equal(1, upCount);
    }

    [Fact]
    public void Update_PlaysGamepadAxisEvents()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedGamepadAxisEvent(100f, 0, GamepadAxis.LeftStickX, 0.5f, 0.0f));

        player.LoadRecording(recording);
        player.Play();
        clock.StepByTime(100f);
        player.Update();

        // Check that the axis value was set
        var gamepad = input.GetMockGamepad(0);
        Assert.Equal(0.5f, gamepad.GetAxis(GamepadAxis.LeftStickX));
    }

    [Fact]
    public void Update_PlaysTextInputEvents()
    {
        using var input = new MockInputContext();
        var clock = new TestClock();
        var player = new InputPlayer(input, clock);
        var recording = new InputRecording();
        recording.Add(new RecordedTextInputEvent(100f, 'a'));

        char receivedChar = '\0';
        input.OnTextInput += (_, c) => receivedChar = c;

        player.LoadRecording(recording);
        player.Play();
        clock.StepByTime(100f);
        player.Update();

        Assert.Equal('a', receivedChar);
    }

    #endregion
}
