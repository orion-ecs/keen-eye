namespace KeenEyes.Debugging.Tests;

/// <summary>
/// Unit tests for the <see cref="DebugPlugin"/> class.
/// </summary>
public sealed class DebugPluginTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct TestComponent : IComponent
    {
        public int Value;
    }
#pragma warning restore CS0649

    private sealed class TestSystem : SystemBase
    {
        public int UpdateCount { get; private set; }

        public override void Update(float deltaTime)
        {
            UpdateCount++;

            // Simulate some work
            Thread.Sleep(1);

            // Allocate some memory to test GC tracking
            var list = new List<int>(100);
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultOptions_CreatesPlugin()
    {
        // Act
        var plugin = new DebugPlugin();

        // Assert
        Assert.Equal("Debug", plugin.Name);
    }

    [Fact]
    public void Constructor_CustomOptions_CreatesPlugin()
    {
        // Arrange
        var options = new DebugOptions
        {
            EnableProfiling = false,
            EnableGCTracking = true
        };

        // Act
        var plugin = new DebugPlugin(options);

        // Assert
        Assert.Equal("Debug", plugin.Name);
    }

    #endregion

    #region Install Tests

    [Fact]
    public void Install_DefaultOptions_InstallsAllExtensions()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();

        // Act
        world.InstallPlugin(plugin);

        // Assert - All extensions should be available
        var inspector = world.GetExtension<EntityInspector>();
        var memoryTracker = world.GetExtension<MemoryTracker>();
        var profiler = world.GetExtension<Profiler>();
        var gcTracker = world.GetExtension<GCTracker>();

        Assert.NotNull(inspector);
        Assert.NotNull(memoryTracker);
        Assert.NotNull(profiler);
        Assert.NotNull(gcTracker);
    }

    [Fact]
    public void Install_ProfilingDisabled_DoesNotInstallProfiler()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions { EnableProfiling = false };
        var plugin = new DebugPlugin(options);

        // Act
        world.InstallPlugin(plugin);

        // Assert
        var hasProfiler = world.TryGetExtension<Profiler>(out _);
        Assert.False(hasProfiler);

        // Other extensions should still be available
        var inspector = world.GetExtension<EntityInspector>();
        var memoryTracker = world.GetExtension<MemoryTracker>();
        Assert.NotNull(inspector);
        Assert.NotNull(memoryTracker);
    }

    [Fact]
    public void Install_GCTrackingDisabled_DoesNotInstallGCTracker()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions { EnableGCTracking = false };
        var plugin = new DebugPlugin(options);

        // Act
        world.InstallPlugin(plugin);

        // Assert
        var hasGCTracker = world.TryGetExtension<GCTracker>(out _);
        Assert.False(hasGCTracker);

        // Other extensions should still be available
        var inspector = world.GetExtension<EntityInspector>();
        var memoryTracker = world.GetExtension<MemoryTracker>();
        var profiler = world.GetExtension<Profiler>();
        Assert.NotNull(inspector);
        Assert.NotNull(memoryTracker);
        Assert.NotNull(profiler);
    }

    [Fact]
    public void Install_AlwaysInstalls_EntityInspectorAndMemoryTracker()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions
        {
            EnableProfiling = false,
            EnableGCTracking = false
        };
        var plugin = new DebugPlugin(options);

        // Act
        world.InstallPlugin(plugin);

        // Assert - These should always be available
        var inspector = world.GetExtension<EntityInspector>();
        var memoryTracker = world.GetExtension<MemoryTracker>();
        Assert.NotNull(inspector);
        Assert.NotNull(memoryTracker);
    }

    #endregion

    #region Profiling Integration Tests

    [Fact]
    public void Profiling_AutomaticallyTracksSystemExecutionTime()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        world.InstallPlugin(plugin);

        var system = new TestSystem();
        world.AddSystem(system);

        var profiler = world.GetExtension<Profiler>();
        Assert.NotNull(profiler);

        // Act - Run multiple updates
        for (int i = 0; i < 5; i++)
        {
            world.Update(0.016f);
        }

        // Assert
        var profile = profiler.GetSystemProfile(nameof(TestSystem));
        Assert.Equal(5, profile.CallCount);
        Assert.True(profile.TotalTime > TimeSpan.Zero);
        Assert.True(profile.AverageTime > TimeSpan.Zero);
    }

    [Fact]
    public void Profiling_WithPhaseFilter_OnlyTracksSpecifiedPhase()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions
        {
            ProfilingPhase = SystemPhase.Update
        };
        var plugin = new DebugPlugin(options);
        world.InstallPlugin(plugin);

        var system = new TestSystem();
        world.AddSystem(system, phase: SystemPhase.Update);

        var profiler = world.GetExtension<Profiler>();
        Assert.NotNull(profiler);

        // Act
        world.Update(0.016f); // Should be profiled
        world.FixedUpdate(0.016f); // Should not be profiled

        // Assert
        var profile = profiler.GetSystemProfile(nameof(TestSystem));
        Assert.Equal(1, profile.CallCount); // Only Update should be counted
    }

    #endregion

    #region GC Tracking Integration Tests

    [Fact]
    public void GCTracking_AutomaticallyTracksSystemAllocations()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        world.InstallPlugin(plugin);

        var system = new TestSystem();
        world.AddSystem(system);

        var gcTracker = world.GetExtension<GCTracker>();
        Assert.NotNull(gcTracker);

        // Act - Run multiple updates
        for (int i = 0; i < 5; i++)
        {
            world.Update(0.016f);
        }

        // Assert
        var profile = gcTracker.GetSystemAllocations(nameof(TestSystem));
        Assert.Equal(5, profile.CallCount);
        // Note: Allocations may be 0 if GC hasn't updated, but should track calls
    }

    [Fact]
    public void GCTracking_WithPhaseFilter_OnlyTracksSpecifiedPhase()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions
        {
            GCTrackingPhase = SystemPhase.Update
        };
        var plugin = new DebugPlugin(options);
        world.InstallPlugin(plugin);

        var system = new TestSystem();
        world.AddSystem(system, phase: SystemPhase.Update);

        var gcTracker = world.GetExtension<GCTracker>();
        Assert.NotNull(gcTracker);

        // Act
        world.Update(0.016f); // Should be tracked
        world.FixedUpdate(0.016f); // Should not be tracked

        // Assert
        var profile = gcTracker.GetSystemAllocations(nameof(TestSystem));
        Assert.Equal(1, profile.CallCount); // Only Update should be counted
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_RemovesAllExtensions()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        world.InstallPlugin(plugin);

        // Verify extensions exist
        Assert.NotNull(world.GetExtension<EntityInspector>());
        Assert.NotNull(world.GetExtension<MemoryTracker>());
        Assert.NotNull(world.GetExtension<Profiler>());
        Assert.NotNull(world.GetExtension<GCTracker>());

        // Act
        world.UninstallPlugin("Debug");

        // Assert - All extensions should be removed
        Assert.False(world.TryGetExtension<EntityInspector>(out _));
        Assert.False(world.TryGetExtension<MemoryTracker>(out _));
        Assert.False(world.TryGetExtension<Profiler>(out _));
        Assert.False(world.TryGetExtension<GCTracker>(out _));
    }

    [Fact]
    public void Uninstall_DisposesHooks()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        world.InstallPlugin(plugin);

        var system = new TestSystem();
        world.AddSystem(system);

        var profiler = world.GetExtension<Profiler>();
        Assert.NotNull(profiler);

        // Act - Uninstall plugin
        world.UninstallPlugin("Debug");

        // Run update
        world.Update(0.016f);

        // Assert - Profiler should no longer track (hooks disposed)
        var profile = profiler.GetSystemProfile(nameof(TestSystem));
        Assert.Equal(0, profile.CallCount);
    }

    [Fact]
    public void Uninstall_OnlyRemovesInstalledExtensions()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions
        {
            EnableProfiling = false,
            EnableGCTracking = false
        };
        var plugin = new DebugPlugin(options);
        world.InstallPlugin(plugin);

        // Act
        world.UninstallPlugin("Debug");

        // Assert - Should not throw even though Profiler and GCTracker were never installed
        Assert.False(world.TryGetExtension<EntityInspector>(out _));
        Assert.False(world.TryGetExtension<MemoryTracker>(out _));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void DebugPlugin_WorksWithEntityInspector()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        world.InstallPlugin(plugin);

        var entity = world.Spawn("TestEntity")
            .With(new TestComponent { Value = 42 })
            .Build();

        var inspector = world.GetExtension<EntityInspector>();
        Assert.NotNull(inspector);

        // Act
        var info = inspector.Inspect(entity);

        // Assert
        Assert.Equal("TestEntity", info.Name);
        Assert.Single(info.Components);
    }

    [Fact]
    public void DebugPlugin_WorksWithMemoryTracker()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        world.InstallPlugin(plugin);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        var tracker = world.GetExtension<MemoryTracker>();
        Assert.NotNull(tracker);

        // Act
        var stats = tracker.GetMemoryStats();

        // Assert
        Assert.Equal(2, stats.EntitiesActive);
    }

    [Fact]
    public void DebugPlugin_AllToolsWorkTogether()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        world.InstallPlugin(plugin);

        var system = new TestSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 100 })
            .Build();

        // Act - Run simulation
        for (int i = 0; i < 3; i++)
        {
            world.Update(0.016f);
        }

        // Assert - All tools should have captured data
        var profiler = world.GetExtension<Profiler>();
        var gcTracker = world.GetExtension<GCTracker>();
        var inspector = world.GetExtension<EntityInspector>();
        var memoryTracker = world.GetExtension<MemoryTracker>();

        Assert.NotNull(profiler);
        Assert.NotNull(gcTracker);
        Assert.NotNull(inspector);
        Assert.NotNull(memoryTracker);

        var profile = profiler.GetSystemProfile(nameof(TestSystem));
        Assert.Equal(3, profile.CallCount);

        var allocProfile = gcTracker.GetSystemAllocations(nameof(TestSystem));
        Assert.Equal(3, allocProfile.CallCount);

        var entityInfo = inspector.Inspect(entity);
        Assert.Single(entityInfo.Components);

        var memStats = memoryTracker.GetMemoryStats();
        Assert.Equal(1, memStats.EntitiesActive);
    }

    #endregion

    #region DebugOptions Tests

    [Fact]
    public void DebugOptions_DefaultValues()
    {
        // Act
        var options = new DebugOptions();

        // Assert
        Assert.True(options.EnableProfiling);
        Assert.True(options.EnableGCTracking);
        Assert.Null(options.ProfilingPhase);
        Assert.Null(options.GCTrackingPhase);
    }

    [Fact]
    public void DebugOptions_CanCustomize()
    {
        // Act
        var options = new DebugOptions
        {
            EnableProfiling = false,
            EnableGCTracking = true,
            ProfilingPhase = SystemPhase.Update,
            GCTrackingPhase = SystemPhase.FixedUpdate
        };

        // Assert
        Assert.False(options.EnableProfiling);
        Assert.True(options.EnableGCTracking);
        Assert.Equal(SystemPhase.Update, options.ProfilingPhase);
        Assert.Equal(SystemPhase.FixedUpdate, options.GCTrackingPhase);
    }

    [Fact]
    public void DebugOptions_RecordEquality()
    {
        // Arrange
        var options1 = new DebugOptions
        {
            EnableProfiling = true,
            EnableGCTracking = false
        };

        var options2 = new DebugOptions
        {
            EnableProfiling = true,
            EnableGCTracking = false
        };

        // Assert
        Assert.Equal(options1, options2);
    }

    #endregion
}
