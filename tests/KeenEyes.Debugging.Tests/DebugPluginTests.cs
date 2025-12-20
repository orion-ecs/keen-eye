using KeenEyes.Capabilities;
using KeenEyes.Testing.Capabilities;
using KeenEyes.Testing.Plugins;

namespace KeenEyes.Debugging.Tests;

/// <summary>
/// Unit tests for the <see cref="DebugPlugin"/> class.
/// </summary>
public partial class DebugPluginTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct TestComponent
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

    #region MockPluginContext Tests

    [Fact]
    public void Install_WithMockContext_RegistersEntityInspectorExtension()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert
        context.ShouldHaveSetExtension<EntityInspector>();
    }

    [Fact]
    public void Install_WithMockContext_RegistersMemoryTrackerExtension()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert
        context.ShouldHaveSetExtension<MemoryTracker>();
    }

    [Fact]
    public void Install_WithMockContext_DefaultOptions_RegistersAllExtensions()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert - default options enable profiling and GC tracking
        context.ShouldHaveSetExtensionCount(4);
        Assert.True(context.WasExtensionSet<EntityInspector>());
        Assert.True(context.WasExtensionSet<MemoryTracker>());
        Assert.True(context.WasExtensionSet<Profiler>());
        Assert.True(context.WasExtensionSet<GCTracker>());
    }

    [Fact]
    public void Install_WithMockContext_ProfilingDisabled_DoesNotRegisterProfiler()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions { EnableProfiling = false };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert
        Assert.False(context.WasExtensionSet<Profiler>());
        Assert.True(context.WasExtensionSet<EntityInspector>());
        Assert.True(context.WasExtensionSet<MemoryTracker>());
        Assert.True(context.WasExtensionSet<GCTracker>());
    }

    [Fact]
    public void Install_WithMockContext_GCTrackingDisabled_DoesNotRegisterGCTracker()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions { EnableGCTracking = false };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert
        Assert.False(context.WasExtensionSet<GCTracker>());
        Assert.True(context.WasExtensionSet<EntityInspector>());
        Assert.True(context.WasExtensionSet<MemoryTracker>());
        Assert.True(context.WasExtensionSet<Profiler>());
    }

    [Fact]
    public void Install_WithMockContext_AllDisabled_RegistersOnlyBasicExtensions()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions
        {
            EnableProfiling = false,
            EnableGCTracking = false
        };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert - only EntityInspector and MemoryTracker are always registered
        context.ShouldHaveSetExtensionCount(2);
        Assert.True(context.WasExtensionSet<EntityInspector>());
        Assert.True(context.WasExtensionSet<MemoryTracker>());
        Assert.False(context.WasExtensionSet<Profiler>());
        Assert.False(context.WasExtensionSet<GCTracker>());
    }

    [Fact]
    public void Install_WithoutWorld_ThrowsInvalidOperationException()
    {
        // Arrange
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin); // No world provided

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => plugin.Install(context));
    }

    [Fact]
    public void Install_RegistersNoSystems()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert - DebugPlugin doesn't register any systems
        Assert.Empty(context.RegisteredSystems);
    }

    [Fact]
    public void Install_RegistersNoComponents()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert - DebugPlugin doesn't register any components
        Assert.Empty(context.RegisteredComponents);
    }

    [Fact]
    public void Install_CreatedExtensions_AreFullyFunctional()
    {
        // Arrange
        using var world = new World();
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert - verify extensions are actually working instances
        var inspector = context.GetSetExtension<EntityInspector>();
        var memoryTracker = context.GetSetExtension<MemoryTracker>();
        var profiler = context.GetSetExtension<Profiler>();
        var gcTracker = context.GetSetExtension<GCTracker>();

        Assert.NotNull(inspector);
        Assert.NotNull(memoryTracker);
        Assert.NotNull(profiler);
        Assert.NotNull(gcTracker);

        // Verify profiler can be used
        profiler.BeginSample("TestSample");
        profiler.EndSample("TestSample");
        var profile = profiler.GetSystemProfile("TestSample");
        Assert.Equal("TestSample", profile.Name);
    }

    [Fact]
    public void Install_WithPhaseFilters_RegistersExtensionsCorrectly()
    {
        // Arrange
        using var world = new World();
        var options = new DebugOptions
        {
            ProfilingPhase = SystemPhase.Update,
            GCTrackingPhase = SystemPhase.FixedUpdate
        };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world);

        // Act
        plugin.Install(context);

        // Assert - phase filters don't affect extension registration
        Assert.True(context.WasExtensionSet<Profiler>());
        Assert.True(context.WasExtensionSet<GCTracker>());
    }

    #endregion

    #region MockSystemHookCapability Tests

    [Fact]
    public void Install_WithMockHookCapability_RegistersProfilingHook()
    {
        // Arrange - Use mock context with mock capability instead of real World
        using var world = new World();
        var mockHooks = new MockSystemHookCapability();
        var plugin = new DebugPlugin();
        var context = new MockPluginContext(plugin, world)
            .SetCapability<ISystemHookCapability>(mockHooks);

        // Act
        plugin.Install(context);

        // Assert - verify hooks were registered via the mock
        Assert.True(mockHooks.WasHookAdded);
        Assert.Equal(2, mockHooks.HookCount); // Profiling + GC tracking hooks
        Assert.True(mockHooks.HasBeforeHook);
        Assert.True(mockHooks.HasAfterHook);
    }

    [Fact]
    public void Install_WithMockHookCapability_ProfilingDisabled_OnlyRegistersGCHook()
    {
        // Arrange
        using var world = new World();
        var mockHooks = new MockSystemHookCapability();
        var options = new DebugOptions { EnableProfiling = false };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world)
            .SetCapability<ISystemHookCapability>(mockHooks);

        // Act
        plugin.Install(context);

        // Assert - only GC tracking hook should be registered
        Assert.Equal(1, mockHooks.HookCount);
    }

    [Fact]
    public void Install_WithMockHookCapability_GCTrackingDisabled_OnlyRegistersProfilingHook()
    {
        // Arrange
        using var world = new World();
        var mockHooks = new MockSystemHookCapability();
        var options = new DebugOptions { EnableGCTracking = false };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world)
            .SetCapability<ISystemHookCapability>(mockHooks);

        // Act
        plugin.Install(context);

        // Assert - only profiling hook should be registered
        Assert.Equal(1, mockHooks.HookCount);
    }

    [Fact]
    public void Install_WithMockHookCapability_AllDisabled_NoHooksRegistered()
    {
        // Arrange
        using var world = new World();
        var mockHooks = new MockSystemHookCapability();
        var options = new DebugOptions
        {
            EnableProfiling = false,
            EnableGCTracking = false
        };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world)
            .SetCapability<ISystemHookCapability>(mockHooks);

        // Act
        plugin.Install(context);

        // Assert - no hooks should be registered
        Assert.False(mockHooks.WasHookAdded);
        Assert.Equal(0, mockHooks.HookCount);
    }

    [Fact]
    public void Install_WithMockHookCapability_ProfilingHookHasPhaseFilter()
    {
        // Arrange
        using var world = new World();
        var mockHooks = new MockSystemHookCapability();
        var options = new DebugOptions
        {
            EnableGCTracking = false,
            ProfilingPhase = SystemPhase.Update
        };
        var plugin = new DebugPlugin(options);
        var context = new MockPluginContext(plugin, world)
            .SetCapability<ISystemHookCapability>(mockHooks);

        // Act
        plugin.Install(context);

        // Assert - verify phase filter was set
        Assert.Single(mockHooks.RegisteredHooks);
        var hook = mockHooks.GetHook(0);
        Assert.Equal(SystemPhase.Update, hook.Phase);
    }

    [Fact]
    public void Install_WithoutHookCapability_WhenHooksNeeded_Throws()
    {
        // Arrange - context without hook capability and without real World
        var mockPlugin = new MockPlugin("MockPlugin");
        var plugin = new DebugPlugin(); // default options need hooks
        var context = new MockPluginContext(mockPlugin); // No world, no capabilities

        // Act & Assert - should throw because hooks are needed but not available
        Assert.Throws<InvalidOperationException>(() => plugin.Install(context));
    }

    [Fact]
    public void Install_WithoutHookCapability_WhenNoHooksNeeded_Succeeds()
    {
        // Arrange - disable profiling and GC tracking so no hooks are needed
        using var world = new World();
        var mockHooks = new MockSystemHookCapability();
        var options = new DebugOptions
        {
            EnableProfiling = false,
            EnableGCTracking = false
        };
        var plugin = new DebugPlugin(options);
        // Note: We don't set ISystemHookCapability - it's not needed
        var context = new MockPluginContext(plugin, world);

        // Act - should not throw
        plugin.Install(context);

        // Assert - extensions should still be registered
        Assert.True(context.WasExtensionSet<EntityInspector>());
        Assert.True(context.WasExtensionSet<MemoryTracker>());
    }

    [Fact]
    public void Install_MockHookCapability_HooksInvokedOnSimulation()
    {
        // Arrange
        using var world = new World();
        var mockHooks = new MockSystemHookCapability();
        var plugin = new DebugPlugin(new DebugOptions { EnableGCTracking = false });
        var context = new MockPluginContext(plugin, world)
            .SetCapability<ISystemHookCapability>(mockHooks);

        plugin.Install(context);

        var profiler = context.GetSetExtension<Profiler>()!;
        var mockSystem = new TestSystem();

        // Act - simulate system execution
        mockHooks.SimulateSystemExecution(mockSystem, 0.016f);
        mockHooks.SimulateSystemExecution(mockSystem, 0.016f);

        // Assert - profiler should have captured the samples
        var profile = profiler.GetSystemProfile(nameof(TestSystem));
        Assert.Equal(2, profile.CallCount);
    }

    #endregion
}
