using System.Text.Json;
using KeenEyes;
using KeenEyes.Capabilities;
using KeenEyes.Editor.PlayMode;
using KeenEyes.Replay;
using KeenEyes.Serialization;

namespace KeenEyes.Editor.Tests.PlayMode;

public class ReplayPlaybackModeTests : IDisposable
{
    private readonly TestComponentSerializer serializer;
    private readonly ReplayPlaybackMode playbackMode;
    private readonly ReplayData testReplayData;

    public ReplayPlaybackModeTests()
    {
        serializer = new TestComponentSerializer();
        playbackMode = new ReplayPlaybackMode(serializer);
        testReplayData = CreateTestReplayData();
    }

    public void Dispose()
    {
        playbackMode.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullSerializer()
    {
        Assert.Throws<ArgumentNullException>(() => new ReplayPlaybackMode(null!));
    }

    [Fact]
    public void Constructor_InitializesToNotLoaded()
    {
        Assert.False(playbackMode.IsLoaded);
        Assert.Equal(-1, playbackMode.CurrentFrame);
        Assert.Equal(0, playbackMode.TotalFrames);
        Assert.Equal(PlaybackState.Stopped, playbackMode.State);
    }

    #endregion

    #region LoadReplay Tests

    [Fact]
    public void LoadReplay_WithValidData_LoadsSuccessfully()
    {
        playbackMode.LoadReplay(testReplayData);

        Assert.True(playbackMode.IsLoaded);
        Assert.Equal(0, playbackMode.CurrentFrame);
        Assert.Equal(testReplayData.FrameCount, playbackMode.TotalFrames);
    }

    [Fact]
    public void LoadReplay_ThrowsOnNullData()
    {
        Assert.Throws<ArgumentNullException>(() => playbackMode.LoadReplay((ReplayData)null!));
    }

    [Fact]
    public void LoadReplay_RaisesFrameChangedEvent()
    {
        FrameChangedEventArgs? eventArgs = null;
        playbackMode.FrameChanged += (_, e) => eventArgs = e;

        playbackMode.LoadReplay(testReplayData);

        Assert.NotNull(eventArgs);
        Assert.Equal(FrameChangeReason.Load, eventArgs.Reason);
        Assert.Equal(0, eventArgs.CurrentFrame);
    }

    [Fact]
    public void LoadReplay_CreatesPlaybackWorld()
    {
        playbackMode.LoadReplay(testReplayData);

        Assert.NotNull(playbackMode.PlaybackWorld);
    }

    #endregion

    #region Unload Tests

    [Fact]
    public void Unload_DisposesPlaybackWorld()
    {
        playbackMode.LoadReplay(testReplayData);
        Assert.NotNull(playbackMode.PlaybackWorld);

        playbackMode.Unload();

        Assert.Null(playbackMode.PlaybackWorld);
        Assert.False(playbackMode.IsLoaded);
    }

    #endregion

    #region Play Tests

    [Fact]
    public void Play_WithLoadedReplay_TransitionsToPlaying()
    {
        playbackMode.LoadReplay(testReplayData);

        playbackMode.Play();

        Assert.Equal(PlaybackState.Playing, playbackMode.State);
    }

    [Fact]
    public void Play_WithoutLoadedReplay_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => playbackMode.Play());
    }

    [Fact]
    public void Play_RaisesStateChangedEvent()
    {
        playbackMode.LoadReplay(testReplayData);
        PlaybackStateChangedEventArgs? eventArgs = null;
        playbackMode.StateChanged += (_, e) => eventArgs = e;

        playbackMode.Play();

        Assert.NotNull(eventArgs);
        Assert.Equal(PlaybackState.Stopped, eventArgs.PreviousState);
        Assert.Equal(PlaybackState.Playing, eventArgs.NewState);
    }

    #endregion

    #region Pause Tests

    [Fact]
    public void Pause_FromPlaying_TransitionsToPaused()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();

        playbackMode.Pause();

        Assert.Equal(PlaybackState.Paused, playbackMode.State);
    }

    [Fact]
    public void Pause_RaisesStateChangedEvent()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();
        PlaybackStateChangedEventArgs? eventArgs = null;
        playbackMode.StateChanged += (_, e) => eventArgs = e;

        playbackMode.Pause();

        Assert.NotNull(eventArgs);
        Assert.Equal(PlaybackState.Playing, eventArgs.PreviousState);
        Assert.Equal(PlaybackState.Paused, eventArgs.NewState);
    }

    #endregion

    #region Stop Tests

    [Fact]
    public void Stop_ResetsToBeginning()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();
        playbackMode.StepFrame();

        playbackMode.Stop();

        Assert.Equal(PlaybackState.Stopped, playbackMode.State);
        Assert.Equal(0, playbackMode.CurrentFrame);
    }

    [Fact]
    public void Stop_RaisesFrameChangedEvent_WhenFrameChanged()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.StepFrame();
        FrameChangedEventArgs? eventArgs = null;
        playbackMode.FrameChanged += (_, e) => eventArgs = e;

        playbackMode.Stop();

        Assert.NotNull(eventArgs);
        Assert.Equal(FrameChangeReason.Stop, eventArgs.Reason);
    }

    #endregion

    #region StepFrame Tests

    [Fact]
    public void StepFrame_AdvancesOneFrame()
    {
        playbackMode.LoadReplay(testReplayData);

        playbackMode.StepFrame();

        Assert.Equal(1, playbackMode.CurrentFrame);
    }

    [Fact]
    public void StepFrame_PausesIfPlaying()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();

        playbackMode.StepFrame();

        Assert.Equal(PlaybackState.Paused, playbackMode.State);
    }

    [Fact]
    public void StepFrame_RaisesFrameChangedEvent()
    {
        playbackMode.LoadReplay(testReplayData);
        FrameChangedEventArgs? eventArgs = null;
        playbackMode.FrameChanged += (_, e) => eventArgs = e;

        playbackMode.StepFrame();

        Assert.NotNull(eventArgs);
        Assert.Equal(FrameChangeReason.StepForward, eventArgs.Reason);
        Assert.Equal(0, eventArgs.PreviousFrame);
        Assert.Equal(1, eventArgs.CurrentFrame);
    }

    [Fact]
    public void StepFrame_ClampsAtEnd()
    {
        playbackMode.LoadReplay(testReplayData);

        // Step to the end
        for (int i = 0; i < testReplayData.FrameCount + 10; i++)
        {
            playbackMode.StepFrame();
        }

        Assert.Equal(testReplayData.FrameCount - 1, playbackMode.CurrentFrame);
    }

    #endregion

    #region StepFrameBack Tests

    [Fact]
    public void StepFrameBack_MovesBackOneFrame()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.StepFrame();
        playbackMode.StepFrame();

        playbackMode.StepFrameBack();

        Assert.Equal(1, playbackMode.CurrentFrame);
    }

    [Fact]
    public void StepFrameBack_RaisesFrameChangedEvent()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.StepFrame();
        playbackMode.StepFrame();
        FrameChangedEventArgs? eventArgs = null;
        playbackMode.FrameChanged += (_, e) => eventArgs = e;

        playbackMode.StepFrameBack();

        Assert.NotNull(eventArgs);
        Assert.Equal(FrameChangeReason.StepBackward, eventArgs.Reason);
        Assert.Equal(2, eventArgs.PreviousFrame);
        Assert.Equal(1, eventArgs.CurrentFrame);
    }

    [Fact]
    public void StepFrameBack_ClampsAtBeginning()
    {
        playbackMode.LoadReplay(testReplayData);

        playbackMode.StepFrameBack();

        Assert.Equal(0, playbackMode.CurrentFrame);
    }

    #endregion

    #region SeekToFrame Tests

    [Fact]
    public void SeekToFrame_JumpsToSpecifiedFrame()
    {
        playbackMode.LoadReplay(testReplayData);

        playbackMode.SeekToFrame(5);

        Assert.Equal(5, playbackMode.CurrentFrame);
    }

    [Fact]
    public void SeekToFrame_PausesIfPlaying()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();

        playbackMode.SeekToFrame(5);

        Assert.Equal(PlaybackState.Paused, playbackMode.State);
    }

    [Fact]
    public void SeekToFrame_RaisesFrameChangedEvent()
    {
        playbackMode.LoadReplay(testReplayData);
        FrameChangedEventArgs? eventArgs = null;
        playbackMode.FrameChanged += (_, e) => eventArgs = e;

        playbackMode.SeekToFrame(5);

        Assert.NotNull(eventArgs);
        Assert.Equal(FrameChangeReason.Seek, eventArgs.Reason);
        Assert.Equal(5, eventArgs.CurrentFrame);
    }

    [Fact]
    public void SeekToFrame_ThrowsOnNegativeFrame()
    {
        playbackMode.LoadReplay(testReplayData);

        Assert.Throws<ArgumentOutOfRangeException>(() => playbackMode.SeekToFrame(-1));
    }

    [Fact]
    public void SeekToFrame_ThrowsOnFrameBeyondTotal()
    {
        playbackMode.LoadReplay(testReplayData);

        Assert.Throws<ArgumentOutOfRangeException>(() => playbackMode.SeekToFrame(testReplayData.FrameCount + 1));
    }

    #endregion

    #region TogglePlayPause Tests

    [Fact]
    public void TogglePlayPause_FromStopped_TransitionsToPlaying()
    {
        playbackMode.LoadReplay(testReplayData);

        playbackMode.TogglePlayPause();

        Assert.Equal(PlaybackState.Playing, playbackMode.State);
    }

    [Fact]
    public void TogglePlayPause_FromPlaying_TransitionsToPaused()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();

        playbackMode.TogglePlayPause();

        Assert.Equal(PlaybackState.Paused, playbackMode.State);
    }

    [Fact]
    public void TogglePlayPause_FromPaused_TransitionsToPlaying()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();
        playbackMode.Pause();

        playbackMode.TogglePlayPause();

        Assert.Equal(PlaybackState.Playing, playbackMode.State);
    }

    #endregion

    #region PlaybackSpeed Tests

    [Fact]
    public void PlaybackSpeed_DefaultIsOne()
    {
        Assert.Equal(1.0f, playbackMode.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_CanBeSet()
    {
        playbackMode.PlaybackSpeed = 0.5f;

        Assert.Equal(0.5f, playbackMode.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_ClampsToMinimum()
    {
        playbackMode.PlaybackSpeed = 0.01f;

        Assert.Equal(0.1f, playbackMode.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_ClampsToMaximum()
    {
        playbackMode.PlaybackSpeed = 20.0f;

        Assert.Equal(10.0f, playbackMode.PlaybackSpeed);
    }

    #endregion

    #region GetCurrentFrameData Tests

    [Fact]
    public void GetCurrentFrameData_WithLoadedReplay_ReturnsFrameInspectionData()
    {
        playbackMode.LoadReplay(testReplayData);

        var frameData = playbackMode.GetCurrentFrameData();

        Assert.NotNull(frameData);
        Assert.Equal(0, frameData.FrameNumber);
    }

    [Fact]
    public void GetCurrentFrameData_WithoutLoadedReplay_ReturnsNull()
    {
        var frameData = playbackMode.GetCurrentFrameData();

        Assert.Null(frameData);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WhenNotPlaying_ReturnsFalse()
    {
        playbackMode.LoadReplay(testReplayData);

        var result = playbackMode.Update(0.016f);

        Assert.False(result);
    }

    [Fact]
    public void Update_WhenPlaying_ReturnsTrue_WhenFrameAdvances()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Play();

        // Accumulate enough time to advance a frame
        var result = playbackMode.Update(1.0f);

        Assert.True(result);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        playbackMode.LoadReplay(testReplayData);

        playbackMode.Dispose();
        var exception = Record.Exception(() => playbackMode.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_DisposesPlaybackWorld()
    {
        playbackMode.LoadReplay(testReplayData);
        var world = playbackMode.PlaybackWorld;

        playbackMode.Dispose();

        // After disposal, the playback world should be disposed
        Assert.NotNull(world);
    }

    [Fact]
    public void MethodsThrow_AfterDispose()
    {
        playbackMode.LoadReplay(testReplayData);
        playbackMode.Dispose();

        Assert.Throws<ObjectDisposedException>(() => playbackMode.Play());
        Assert.Throws<ObjectDisposedException>(() => playbackMode.Pause());
        Assert.Throws<ObjectDisposedException>(() => playbackMode.Stop());
        Assert.Throws<ObjectDisposedException>(() => playbackMode.StepFrame());
        Assert.Throws<ObjectDisposedException>(() => playbackMode.StepFrameBack());
    }

    #endregion

    #region Test Helpers

    private static ReplayData CreateTestReplayData()
    {
        var frames = new List<ReplayFrame>();

        for (int i = 0; i < 10; i++)
        {
            frames.Add(new ReplayFrame
            {
                FrameNumber = i,
                DeltaTime = TimeSpan.FromMilliseconds(16.67),
                ElapsedTime = TimeSpan.FromMilliseconds(16.67 * i),
                Events = CreateTestEvents(i)
            });
        }

        return new ReplayData
        {
            Name = "Test Replay",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(16.67 * 10),
            FrameCount = 10,
            Frames = frames,
            Snapshots = []
        };
    }

    private static IReadOnlyList<ReplayEvent> CreateTestEvents(int frameNumber)
    {
        List<ReplayEvent> events =
        [
            new ReplayEvent
            {
                Type = ReplayEventType.FrameStart,
                Timestamp = TimeSpan.Zero
            }
        ];

        if (frameNumber == 0)
        {
            events.Add(new ReplayEvent
            {
                Type = ReplayEventType.EntityCreated,
                EntityId = 1,
                Timestamp = TimeSpan.FromMilliseconds(1)
            });
        }

        events.Add(new ReplayEvent
        {
            Type = ReplayEventType.FrameEnd,
            Timestamp = TimeSpan.FromMilliseconds(16.67)
        });

        return events;
    }

    /// <summary>
    /// A minimal test implementation of IComponentSerializer for testing.
    /// </summary>
    private sealed class TestComponentSerializer : IComponentSerializer
    {
        public bool IsSerializable(Type type) => false;
        public bool IsSerializable(string typeName) => false;
        public object? Deserialize(string typeName, JsonElement json) => null;
        public JsonElement? Serialize(Type type, object value) => null;
        public Type? GetType(string typeName) => null;
        public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag) => null;
        public bool SetSingleton(ISerializationCapability serialization, string typeName, object value) => false;
        public object? CreateDefault(string typeName) => null;
        public int GetVersion(string typeName) => 1;
        public int GetVersion(Type type) => 1;
    }

    #endregion
}
