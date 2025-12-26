namespace KeenEyes.Replay.Tests;

/// <summary>
/// Integration tests for the <see cref="ReplayPlugin"/> class.
/// </summary>
public class ReplayPluginTests
{
    private sealed class TestMoveSystem : SystemBase
    {
        public int UpdateCount { get; private set; }

        public override void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    #region Installation Tests

    [Fact]
    public void Install_RegistersRecorderExtension()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);

        // Act
        world.InstallPlugin(plugin);

        // Assert
        var recorder = world.GetExtension<ReplayRecorder>();
        Assert.NotNull(recorder);
    }

    [Fact]
    public void Install_WithCustomOptions_AppliesOptions()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(10),
            MaxFrames = 500
        };
        var plugin = new ReplayPlugin(serializer, options);

        // Act
        world.InstallPlugin(plugin);

        // Assert
        var recorder = world.GetExtension<ReplayRecorder>();
        Assert.NotNull(recorder);
        Assert.Equal(TimeSpan.FromSeconds(10), recorder.Options.SnapshotInterval);
        Assert.Equal(500, recorder.Options.MaxFrames);
    }

    [Fact]
    public void Install_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReplayPlugin(null!));
    }

    [Fact]
    public void Uninstall_RemovesRecorderExtension()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        // Act
        world.UninstallPlugin<ReplayPlugin>();

        // Assert
        Assert.Throws<InvalidOperationException>(() => world.GetExtension<ReplayRecorder>());
    }

    [Fact]
    public void Uninstall_CancelsActiveRecording()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();
        Assert.True(recorder.IsRecording);

        // Act
        world.UninstallPlugin<ReplayPlugin>();

        // Assert
        Assert.False(recorder.IsRecording);
    }

    #endregion

    #region Automatic Frame Tracking Tests

    [Fact]
    public void Update_AutomaticallyTracksFrames()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - run a few update cycles
        for (int i = 0; i < 5; i++)
        {
            world.Update(0.016f);
        }

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.FrameCount);
    }

    [Fact]
    public void Update_AccumulatesElapsedTime()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - run update cycles
        world.Update(0.016f);
        world.Update(0.016f);
        world.Update(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.True(Math.Abs((result.Duration - TimeSpan.FromSeconds(0.048)).TotalMilliseconds) < 1);
    }

    [Fact]
    public void Update_WithSystem_RecordsFramesWithSystemExecution()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        var system = new TestMoveSystem();
        world.AddSystem(system, SystemPhase.Update);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act
        world.Update(0.016f);
        world.Update(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.FrameCount);
        Assert.Equal(2, system.UpdateCount);
    }

    #endregion

    #region Entity Event Recording Tests

    [Fact]
    public void EntityEvents_WhenEnabled_RecordsEntityCreation()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = true };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - manually record entity creation within a frame
        recorder.BeginFrame(0.016f);
        var entity = world.Spawn().Build();
        recorder.RecordEntityCreated(entity.Id, null);
        recorder.EndFrame(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var entityCreatedEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.EntityCreated)
            .ToList();

        Assert.NotEmpty(entityCreatedEvents);
    }

    [Fact]
    public void EntityEvents_WhenEnabled_RecordsEntityDestruction()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = true };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        // Create entity before recording
        var entity = world.Spawn().Build();

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - destroy entity and manually record within a frame
        recorder.BeginFrame(0.016f);
        world.Despawn(entity);
        recorder.RecordEntityDestroyed(entity.Id);
        recorder.EndFrame(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var entityDestroyedEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.EntityDestroyed)
            .ToList();

        Assert.NotEmpty(entityDestroyedEvents);
    }

    [Fact]
    public void EntityEvents_WhenDisabled_DoesNotRecordEntityEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordEntityEvents = false };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Need to call Update to start a frame
        world.Update(0.016f);

        // Act - create and destroy entities
        var entity = world.Spawn().Build();
        world.Update(0.016f);

        world.Despawn(entity);
        world.Update(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var entityEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.EntityCreated || e.Type == ReplayEventType.EntityDestroyed)
            .ToList();

        Assert.Empty(entityEvents);
    }

    #endregion

    #region System Event Recording Tests

    [Fact]
    public void SystemEvents_WhenEnabled_RecordsSystemExecution()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordSystemEvents = true };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        var system = new TestMoveSystem();
        world.AddSystem(system, SystemPhase.Update);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act
        world.Update(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var systemEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.SystemStart || e.Type == ReplayEventType.SystemEnd)
            .ToList();

        Assert.NotEmpty(systemEvents);
        Assert.Contains(systemEvents, e => e.SystemTypeName == nameof(TestMoveSystem));
    }

    [Fact]
    public void SystemEvents_WhenDisabled_DoesNotRecordSystemEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { RecordSystemEvents = false };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        var system = new TestMoveSystem();
        world.AddSystem(system, SystemPhase.Update);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act
        world.Update(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var systemEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.SystemStart || e.Type == ReplayEventType.SystemEnd)
            .ToList();

        Assert.Empty(systemEvents);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void AutomaticSnapshots_CapturedAtInterval()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions { SnapshotInterval = TimeSpan.FromSeconds(0.05) };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        // Create an entity
        world.Spawn().Build();

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - run frames until we pass the snapshot interval
        for (int i = 0; i < 5; i++)
        {
            world.Update(0.016f);
        }
        // Total: ~80ms, should trigger additional snapshots

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Snapshots.Count >= 2); // Initial + at least one automatic
    }

    [Fact]
    public void ManualSnapshot_CanBeCaptured()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        var initialSnapshotCount = recorder.SnapshotCount;

        // Act
        recorder.CaptureSnapshot();

        // Assert
        Assert.Equal(initialSnapshotCount + 1, recorder.SnapshotCount);
    }

    #endregion

    #region Custom Event Tests

    [Fact]
    public void CustomEvents_CanBeRecordedDuringPlayback()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - record custom events during a frame (using BeginFrame/EndFrame manually)
        recorder.BeginFrame(0.016f);
        recorder.RecordCustomEvent("PlayerJumped", new Dictionary<string, object>
        {
            ["height"] = 2.5,
            ["duration"] = 0.5
        });
        recorder.EndFrame(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        var customEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.Custom)
            .ToList();

        Assert.NotEmpty(customEvents);
        Assert.Contains(customEvents, e => e.CustomType == "PlayerJumped");
    }

    #endregion

    #region Max Limits Tests

    [Fact]
    public void MaxFrames_StopsRecordingWhenReached()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            MaxFrames = 3,
            UseRingBuffer = false
        };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - run more frames than the limit
        for (int i = 0; i < 5; i++)
        {
            world.Update(0.016f);
        }

        // Assert - should have stopped after 3 frames
        Assert.True(recorder.ShouldStopRecording());
    }

    [Fact]
    public void RingBuffer_KeepsLastNFrames()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            MaxFrames = 3,
            UseRingBuffer = true
        };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording();

        // Act - run more frames than the buffer size
        for (int i = 0; i < 10; i++)
        {
            world.Update(0.016f);
        }

        var result = recorder.StopRecording();

        // Assert - should only have the last 3 frames
        Assert.NotNull(result);
        Assert.Equal(3, result.FrameCount);
    }

    #endregion

    #region Full Integration Tests

    [Fact]
    public void FullRecordingCycle_WorksEndToEnd()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var options = new ReplayOptions
        {
            RecordEntityEvents = true,
            RecordSystemEvents = true,
            SnapshotInterval = TimeSpan.FromSeconds(0.05)
        };
        var plugin = new ReplayPlugin(serializer, options);
        world.InstallPlugin(plugin);

        var system = new TestMoveSystem();
        world.AddSystem(system, SystemPhase.Update);

        var recorder = world.GetExtension<ReplayRecorder>();

        // Act - full recording cycle
        recorder.StartRecording("Integration Test");

        // Run several frames (no entity creation before first Update due to timing issue)
        for (int i = 0; i < 20; i++)
        {
            world.Update(0.016f);
        }

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Integration Test", result.Name);
        Assert.Equal(20, result.FrameCount);
        Assert.True(result.Duration.TotalSeconds > 0);
        Assert.NotEmpty(result.Snapshots);

        // Verify system events were recorded
        var systemStartEvents = result.Frames
            .SelectMany(f => f.Events)
            .Where(e => e.Type == ReplayEventType.SystemStart)
            .ToList();
        Assert.NotEmpty(systemStartEvents);
    }

    [Fact]
    public void RecordingCycle_CanBeSavedAndLoaded()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var plugin = new ReplayPlugin(serializer);
        world.InstallPlugin(plugin);

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording("Save/Load Test");

        // Record some frames
        for (int i = 0; i < 5; i++)
        {
            world.Update(0.016f);
        }

        var originalResult = recorder.StopRecording();
        Assert.NotNull(originalResult);

        // Act - save to bytes and reload
        var bytes = ReplayFileFormat.Write(originalResult);
        var (fileInfo, loadedData) = ReplayFileFormat.Read(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.NotNull(loadedData);
        Assert.Equal(originalResult.Name, loadedData.Name);
        Assert.Equal(originalResult.FrameCount, loadedData.FrameCount);
        Assert.Equal(originalResult.Frames.Count, loadedData.Frames.Count);
    }

    #endregion
}
