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
}
