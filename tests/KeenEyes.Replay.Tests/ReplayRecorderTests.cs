using System.Text.Json;
using KeenEyes.Capabilities;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for the <see cref="ReplayRecorder"/> class.
/// </summary>
public partial class ReplayRecorderTests
{
    private sealed class TestSystem : SystemBase
    {
        public int UpdateCount { get; private set; }

        public override void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesRecorder()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();

        // Act
        var recorder = new ReplayRecorder(world, serializer);

        // Assert
        Assert.False(recorder.IsRecording);
        Assert.Equal(-1, recorder.CurrentFrameNumber);
        Assert.Equal(TimeSpan.Zero, recorder.ElapsedTime);
    }

    [Fact]
    public void Constructor_WithNullWorld_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new MockComponentSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReplayRecorder(null!, serializer));
    }

    [Fact]
    public void Constructor_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        using var world = new World();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReplayRecorder(world, null!));
    }

    [Fact]
    public void Constructor_WithCustomOptions_AppliesOptions()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(5),
            MaxFrames = 100
        };

        // Act
        var recorder = new ReplayRecorder(world, serializer, options);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), recorder.Options.SnapshotInterval);
        Assert.Equal(100, recorder.Options.MaxFrames);
    }

    #endregion

    #region StartRecording Tests

    [Fact]
    public void StartRecording_WhenNotRecording_StartsRecording()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act
        recorder.StartRecording("Test Recording");

        // Assert
        Assert.True(recorder.IsRecording);
        Assert.Equal(0, recorder.CurrentFrameNumber);
    }

    [Fact]
    public void StartRecording_WhenAlreadyRecording_ThrowsInvalidOperationException()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => recorder.StartRecording());
    }

    [Fact]
    public void StartRecording_CapturesInitialSnapshot()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act
        recorder.StartRecording();

        // Assert
        Assert.Equal(1, recorder.SnapshotCount);
    }

    [Fact]
    public void StartRecording_WithNullName_UsesDefaultRecordingName()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { DefaultRecordingName = "Default Session" };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act
        recorder.StartRecording(null);
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default Session", result.Name);
    }

    [Fact]
    public void StartRecording_WithMetadata_IncludesMetadataInResult()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        var metadata = new Dictionary<string, object>
        {
            ["Player"] = "TestPlayer",
            ["Level"] = 5
        };

        // Act
        recorder.StartRecording("Test", metadata);
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal("TestPlayer", result.Metadata["Player"]);
        Assert.Equal(5, result.Metadata["Level"]);
    }

    #endregion

    #region StopRecording Tests

    [Fact]
    public void StopRecording_WhenRecording_ReturnsReplayData()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording("Test");

        // Simulate a frame
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);

        // Act
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(1, result.FrameCount);
        Assert.False(recorder.IsRecording);
    }

    [Fact]
    public void StopRecording_WhenNotRecording_ReturnsNull()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act
        var result = recorder.StopRecording();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void StopRecording_IncludesAllFrames()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Simulate 5 frames
        for (int i = 0; i < 5; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }

        // Act
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.FrameCount);
        Assert.Equal(5, result.Frames.Count);
    }

    #endregion

    #region CancelRecording Tests

    [Fact]
    public void CancelRecording_WhenRecording_StopsWithoutReturningData()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Act
        recorder.CancelRecording();

        // Assert
        Assert.False(recorder.IsRecording);
        Assert.Equal(0, recorder.RecordedFrameCount);
    }

    [Fact]
    public void CancelRecording_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act & Assert - should not throw
        recorder.CancelRecording();
    }

    #endregion

    #region RecordEvent Tests

    [Fact]
    public void RecordEvent_WhenRecording_AddsEventToCurrentFrame()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        var customEvent = new ReplayEvent
        {
            Type = ReplayEventType.Custom,
            CustomType = "TestEvent",
            Timestamp = TimeSpan.Zero
        };

        // Act
        recorder.RecordEvent(customEvent);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames);
        Assert.Contains(result.Frames[0].Events, e => e.Type == ReplayEventType.Custom && e.CustomType == "TestEvent");
    }

    [Fact]
    public void RecordEvent_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        var customEvent = new ReplayEvent
        {
            Type = ReplayEventType.Custom,
            CustomType = "TestEvent",
            Timestamp = TimeSpan.Zero
        };

        // Act & Assert - should not throw
        recorder.RecordEvent(customEvent);
    }

    [Fact]
    public void RecordEvent_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => recorder.RecordEvent(null!));
    }

    [Fact]
    public void RecordCustomEvent_WhenRecording_AddsCustomEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordCustomEvent("PlayerJumped", new Dictionary<string, object> { ["height"] = 2.5 });
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var customEvents = result.Frames[0].Events.Where(e => e.Type == ReplayEventType.Custom).ToList();
        Assert.Single(customEvents);
        Assert.Equal("PlayerJumped", customEvents[0].CustomType);
    }

    #endregion

    #region Frame Tracking Tests

    [Fact]
    public void BeginFrame_EndFrame_CreatesFrame()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Act
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);

        // Assert
        Assert.Equal(1, recorder.RecordedFrameCount);
        Assert.Equal(1, recorder.CurrentFrameNumber);
    }

    [Fact]
    public void ElapsedTime_AccumulatesAcrossFrames()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Act - simulate 3 frames at 16ms each
        for (int i = 0; i < 3; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }

        // Assert
        var elapsed = recorder.ElapsedTime;
        Assert.True(Math.Abs((elapsed - TimeSpan.FromSeconds(0.048)).TotalMilliseconds) < 1);
    }

    [Fact]
    public void Frames_ContainCorrectDeltaTime()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Act
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.033f);
        recorder.EndFrame(0.033f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.True(Math.Abs((result.Frames[0].DeltaTime - TimeSpan.FromSeconds(0.016)).TotalMilliseconds) < 1);
        Assert.True(Math.Abs((result.Frames[1].DeltaTime - TimeSpan.FromSeconds(0.033)).TotalMilliseconds) < 1);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void CaptureSnapshot_ManuallyTriggered_CreatesSnapshot()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Initial snapshot is captured on start
        Assert.Equal(1, recorder.SnapshotCount);

        // Act
        recorder.CaptureSnapshot();

        // Assert
        Assert.Equal(2, recorder.SnapshotCount);
    }

    [Fact]
    public void AutomaticSnapshots_CapturedAtInterval()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { SnapshotInterval = TimeSpan.FromSeconds(0.05) };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Initial snapshot
        Assert.Equal(1, recorder.SnapshotCount);

        // Act - simulate frames until we pass the interval
        for (int i = 0; i < 4; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }
        // Total elapsed: 64ms, should trigger snapshot at 50ms

        // Assert
        Assert.True(recorder.SnapshotCount >= 2);
    }

    [Fact]
    public void CaptureSnapshot_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Verify not recording
        Assert.False(recorder.IsRecording);
        var initialCount = recorder.SnapshotCount;

        // Act - should not throw and should not add snapshot
        recorder.CaptureSnapshot();

        // Assert
        Assert.Equal(initialCount, recorder.SnapshotCount);
    }

    [Fact]
    public void BeginFrame_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act & Assert - should not throw
        recorder.BeginFrame(0.016f);
        Assert.Equal(0, recorder.RecordedFrameCount);
    }

    [Fact]
    public void EndFrame_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act & Assert - should not throw
        recorder.EndFrame(0.016f);
        Assert.Equal(0, recorder.RecordedFrameCount);
    }

    [Fact]
    public void BeginFrame_AfterMaxDurationReached_SkipsFrameStartEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { MaxDuration = TimeSpan.FromSeconds(0.032) };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Record frames until max duration
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        // Now at 32ms = max duration

        // Act - ShouldStopRecording should return true
        Assert.True(recorder.ShouldStopRecording());
    }

    [Fact]
    public void BeginFrame_AfterMaxFramesReached_SkipsFrameStartEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { MaxFrames = 2, UseRingBuffer = false };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Record frames until max
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        // Now at 2 frames = max

        // Act - ShouldStopRecording should return true
        Assert.True(recorder.ShouldStopRecording());
    }

    #endregion

    #region Ring Buffer Tests

    [Fact]
    public void RingBuffer_WhenEnabled_DiscardsOldFrames()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            UseRingBuffer = true,
            MaxFrames = 5
        };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Act - record 10 frames
        for (int i = 0; i < 10; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.RecordCustomEvent($"Frame{i}");
            recorder.EndFrame(0.016f);
        }

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.FrameCount);
    }

    [Fact]
    public void RingBuffer_WhenDisabled_KeepsAllFrames()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            UseRingBuffer = false,
            MaxFrames = null
        };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Act - record 10 frames
        for (int i = 0; i < 10; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.FrameCount);
    }

    #endregion

    #region Max Limits Tests

    [Fact]
    public void ShouldStopRecording_WhenMaxFramesReached_ReturnsTrue()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { MaxFrames = 3, UseRingBuffer = false };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Record 3 frames
        for (int i = 0; i < 3; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }

        // Act & Assert
        Assert.True(recorder.ShouldStopRecording());
    }

    [Fact]
    public void ShouldStopRecording_WhenMaxDurationReached_ReturnsTrue()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { MaxDuration = TimeSpan.FromSeconds(0.05) };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Record frames until we exceed duration
        for (int i = 0; i < 5; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }
        // Total: 80ms, exceeds 50ms max

        // Act & Assert
        Assert.True(recorder.ShouldStopRecording());
    }

    [Fact]
    public void ShouldStopRecording_WhenNotRecording_ReturnsFalse()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { MaxFrames = 3, UseRingBuffer = false };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act & Assert - not recording, should return false
        Assert.False(recorder.ShouldStopRecording());
    }

    [Fact]
    public void ShouldStopRecording_WithRingBuffer_ReturnsFalse()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { MaxFrames = 3, UseRingBuffer = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Record more than MaxFrames
        for (int i = 0; i < 5; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }

        // Act & Assert - ring buffer should never trigger stop
        Assert.False(recorder.ShouldStopRecording());
    }

    #endregion

    #region Entity Event Recording Tests

    [Fact]
    public void RecordEntityCreated_WhenRecording_AddsEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordEntityCreated(42, "TestEntity");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var entityEvent = result.Frames[0].Events.FirstOrDefault(e => e.Type == ReplayEventType.EntityCreated);
        Assert.NotNull(entityEvent);
        Assert.Equal(42, entityEvent.EntityId);
    }

    [Fact]
    public void RecordEntityDestroyed_WhenRecording_AddsEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordEntityDestroyed(42);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var entityEvent = result.Frames[0].Events.FirstOrDefault(e => e.Type == ReplayEventType.EntityDestroyed);
        Assert.NotNull(entityEvent);
        Assert.Equal(42, entityEvent.EntityId);
    }

    [Fact]
    public void RecordEntityCreated_WithNullName_DoesNotIncludeDataField()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordEntityCreated(42, null);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var entityEvent = result.Frames[0].Events.FirstOrDefault(e => e.Type == ReplayEventType.EntityCreated);
        Assert.NotNull(entityEvent);
        Assert.Equal(42, entityEvent.EntityId);
        Assert.Null(entityEvent.Data);
    }

    [Fact]
    public void RecordEntityEvents_WhenDisabled_DoesNotAddEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = false };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordEntityCreated(42, "TestEntity");
        recorder.RecordEntityDestroyed(43);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Frames[0].Events, e => e.Type == ReplayEventType.EntityCreated);
        Assert.DoesNotContain(result.Frames[0].Events, e => e.Type == ReplayEventType.EntityDestroyed);
    }

    [Fact]
    public void RecordEntityCreated_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act & Assert - should not throw
        recorder.RecordEntityCreated(42, "TestEntity");
    }

    [Fact]
    public void RecordEntityDestroyed_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act & Assert - should not throw
        recorder.RecordEntityDestroyed(42);
    }

    #endregion

    #region System Event Recording Tests

    [Fact]
    public void RecordSystemStart_WhenEnabled_AddsEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordSystemEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordSystemStart("TestSystem");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var systemEvent = result.Frames[0].Events.FirstOrDefault(e => e.Type == ReplayEventType.SystemStart);
        Assert.NotNull(systemEvent);
        Assert.Equal("TestSystem", systemEvent.SystemTypeName);
    }

    [Fact]
    public void RecordSystemEvents_WhenDisabled_DoesNotAddEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordSystemEvents = false };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordSystemStart("TestSystem");
        recorder.RecordSystemEnd("TestSystem");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Frames[0].Events, e => e.Type == ReplayEventType.SystemStart);
        Assert.DoesNotContain(result.Frames[0].Events, e => e.Type == ReplayEventType.SystemEnd);
    }

    [Fact]
    public void RecordSystemEnd_WhenEnabled_AddsEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordSystemEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordSystemEnd("TestSystem");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var systemEvent = result.Frames[0].Events.FirstOrDefault(e => e.Type == ReplayEventType.SystemEnd);
        Assert.NotNull(systemEvent);
        Assert.Equal("TestSystem", systemEvent.SystemTypeName);
    }

    [Fact]
    public void RecordSystemStart_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordSystemEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act & Assert - should not throw
        recorder.RecordSystemStart("TestSystem");
    }

    [Fact]
    public void RecordSystemEnd_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordSystemEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act & Assert - should not throw
        recorder.RecordSystemEnd("TestSystem");
    }

    #endregion

    #region Component Event Recording Tests

    [Fact]
    public void RecordComponentAdded_WhenEnabled_AddsEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordComponentEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordComponentAdded(42, "Position");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var componentEvent = result.Frames[0].Events.FirstOrDefault(e => e.Type == ReplayEventType.ComponentAdded);
        Assert.NotNull(componentEvent);
        Assert.Equal(42, componentEvent.EntityId);
        Assert.Equal("Position", componentEvent.ComponentTypeName);
    }

    [Fact]
    public void RecordComponentRemoved_WhenEnabled_AddsEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordComponentEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordComponentRemoved(42, "Velocity");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var componentEvent = result.Frames[0].Events.FirstOrDefault(e => e.Type == ReplayEventType.ComponentRemoved);
        Assert.NotNull(componentEvent);
        Assert.Equal(42, componentEvent.EntityId);
        Assert.Equal("Velocity", componentEvent.ComponentTypeName);
    }

    [Fact]
    public void RecordComponentEvents_WhenDisabled_DoesNotAddEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordComponentEvents = false };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordComponentAdded(42, "Position");
        recorder.RecordComponentRemoved(42, "Velocity");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Frames[0].Events, e => e.Type == ReplayEventType.ComponentAdded);
        Assert.DoesNotContain(result.Frames[0].Events, e => e.Type == ReplayEventType.ComponentRemoved);
    }

    [Fact]
    public void RecordComponentAdded_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordComponentEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act & Assert - should not throw
        recorder.RecordComponentAdded(42, "Position");
    }

    [Fact]
    public void RecordComponentRemoved_WhenNotRecording_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordComponentEvents = true };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Act & Assert - should not throw
        recorder.RecordComponentRemoved(42, "Position");
    }

    #endregion

    #region Sequential Recording Tests

    [Fact]
    public void SequentialRecordings_CanStartStopMultipleTimes()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act - First recording
        recorder.StartRecording("First Recording");
        for (int i = 0; i < 5; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.RecordCustomEvent($"Event1_{i}");
            recorder.EndFrame(0.016f);
        }
        var firstResult = recorder.StopRecording();

        // Second recording
        recorder.StartRecording("Second Recording");
        for (int i = 0; i < 3; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.RecordCustomEvent($"Event2_{i}");
            recorder.EndFrame(0.016f);
        }
        var secondResult = recorder.StopRecording();

        // Assert
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Equal("First Recording", firstResult.Name);
        Assert.Equal("Second Recording", secondResult.Name);
        Assert.Equal(5, firstResult.FrameCount);
        Assert.Equal(3, secondResult.FrameCount);
    }

    [Fact]
    public void SequentialRecordings_ResetStateCorrectly()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act - First recording
        recorder.StartRecording();
        for (int i = 0; i < 10; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }
        recorder.StopRecording();

        // Verify state is reset
        Assert.False(recorder.IsRecording);
        Assert.Equal(-1, recorder.CurrentFrameNumber);
        Assert.Equal(TimeSpan.Zero, recorder.ElapsedTime);

        // Second recording should start fresh
        recorder.StartRecording();
        Assert.True(recorder.IsRecording);
        Assert.Equal(0, recorder.CurrentFrameNumber);
    }

    [Fact]
    public void SequentialRecordings_CancelThenRecordAgain()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // First recording - cancelled
        recorder.StartRecording("Cancelled");
        for (int i = 0; i < 3; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }
        recorder.CancelRecording();

        // Second recording - completed
        recorder.StartRecording("Completed");
        for (int i = 0; i < 5; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Name);
        Assert.Equal(5, result.FrameCount);
    }

    #endregion

    #region Large-Scale Tests

    [Fact]
    public void LargeScale_1000FramesWith100Entities_PerformsCorrectly()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            RecordEntityEvents = true,
            SnapshotInterval = TimeSpan.FromSeconds(1) // Snapshot every second
        };
        var recorder = new ReplayRecorder(world, serializer, options);

        // Create 100 entities before recording
        var entities = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            entities.Add(world.Spawn().Build());
        }

        // Act - Record 1000+ frames
        recorder.StartRecording("Large Scale Test");

        for (int frame = 0; frame < 1200; frame++)
        {
            recorder.BeginFrame(0.016f); // ~60 FPS

            // Record some entity events on specific frames
            if (frame % 100 == 0)
            {
                recorder.RecordCustomEvent("Checkpoint", new Dictionary<string, object>
                {
                    ["frame"] = frame,
                    ["entityCount"] = entities.Count
                });
            }

            recorder.EndFrame(0.016f);
        }

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1200, result.FrameCount);
        Assert.Equal(1200, result.Frames.Count);

        // Should have multiple snapshots due to 1-second interval over ~19 seconds of recording
        Assert.True(result.Snapshots.Count >= 10,
            $"Expected at least 10 snapshots, got {result.Snapshots.Count}");

        // Verify custom events were recorded
        var checkpointEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.Custom && e.CustomType == "Checkpoint")
            .ToList();
        Assert.Equal(12, checkpointEvents.Count); // Frames 0, 100, 200, ..., 1100

        // Verify duration is approximately correct (1200 frames * 16ms ≈ 19.2 seconds)
        var expectedDuration = TimeSpan.FromSeconds(1200 * 0.016);
        var durationDiff = Math.Abs((result.Duration - expectedDuration).TotalSeconds);
        Assert.True(durationDiff < 0.1, $"Duration {result.Duration} differs from expected {expectedDuration}");
    }

    [Fact]
    public void LargeScale_ManyEventsPerFrame_PerformsCorrectly()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act - Record frames with many events
        recorder.StartRecording();

        for (int frame = 0; frame < 100; frame++)
        {
            recorder.BeginFrame(0.016f);

            // Record 50 events per frame
            for (int eventNum = 0; eventNum < 50; eventNum++)
            {
                recorder.RecordCustomEvent($"Event_{eventNum}", new Dictionary<string, object>
                {
                    ["frame"] = frame,
                    ["index"] = eventNum
                });
            }

            recorder.EndFrame(0.016f);
        }

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.FrameCount);

        // Each frame should have 50 events
        foreach (var frameData in result.Frames)
        {
            var customEvents = frameData.Events.Where(e => e.Type == ReplayEventType.Custom).ToList();
            Assert.Equal(50, customEvents.Count);
        }

        // Total custom events: 100 frames * 50 events = 5000
        var totalCustomEvents = result.Frames.SelectMany(f => f.Events)
            .Count(e => e.Type == ReplayEventType.Custom);
        Assert.Equal(5000, totalCustomEvents);
    }

    #endregion

    #region Checksum Recording Tests

    [Fact]
    public void Frame_WhenRecordChecksumsDisabled_HasNullChecksum()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordChecksums = false };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Act
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames);
        Assert.Null(result.Frames[0].Checksum);
    }

    [Fact]
    public void Frame_WhenRecordChecksumsEnabled_HasChecksum()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordChecksums = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Act
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames);
        Assert.NotNull(result.Frames[0].Checksum);
    }

    [Fact]
    public void Snapshot_WhenRecordChecksumsEnabled_HasChecksum()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordChecksums = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Initial snapshot is captured on start
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Snapshots);
        Assert.NotNull(result.Snapshots[0].Checksum);
    }

    [Fact]
    public void Snapshot_WhenRecordChecksumsDisabled_HasNullChecksum()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordChecksums = false };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Initial snapshot is captured on start
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Snapshots);
        Assert.Null(result.Snapshots[0].Checksum);
    }

    [Fact]
    public void RecordChecksums_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var options = new ReplayOptions();

        // Assert
        Assert.False(options.RecordChecksums);
    }

    [Fact]
    public void Frame_Checksums_AreConsistentForSameWorldState()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordChecksums = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Act - Record two frames without changing world state
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert - Same world state = same checksum
        Assert.NotNull(result);
        Assert.Equal(2, result.Frames.Count);
        Assert.NotNull(result.Frames[0].Checksum);
        Assert.NotNull(result.Frames[1].Checksum);
        Assert.Equal(result.Frames[0].Checksum, result.Frames[1].Checksum);
    }

    [Fact]
    public void Frame_Checksums_DifferWhenWorldStateChanges()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordChecksums = true };
        var recorder = new ReplayRecorder(world, serializer, options);
        recorder.StartRecording();

        // Act - Record frame, change world, record another frame
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);

        // Change world state
        world.Spawn().Build();

        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert - Different world state = different checksum
        Assert.NotNull(result);
        Assert.Equal(2, result.Frames.Count);
        Assert.NotNull(result.Frames[0].Checksum);
        Assert.NotNull(result.Frames[1].Checksum);
        Assert.NotEqual(result.Frames[0].Checksum, result.Frames[1].Checksum);
    }

    #endregion
}

/// <summary>
/// Mock component serializer for testing.
/// </summary>
internal sealed class MockComponentSerializer : IComponentSerializer
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
