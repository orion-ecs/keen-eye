namespace KeenEyes.Tests;

/// <summary>
/// Test plugin that registers system hooks for profiling.
/// </summary>
public class ProfilingPlugin : IWorldPlugin
{
    public string Name => "Profiling";
    public Dictionary<string, int> SystemExecutionCounts { get; } = [];
    private EventSubscription? hookSubscription;

    public void Install(IPluginContext context)
    {
        var world = (World)context.World;
        hookSubscription = world.AddSystemHook(
            beforeHook: null,
            afterHook: (system, dt) =>
            {
                var systemName = system.GetType().Name;
                if (!SystemExecutionCounts.ContainsKey(systemName))
                {
                    SystemExecutionCounts[systemName] = 0;
                }
                SystemExecutionCounts[systemName]++;
            }
        );
    }

    public void Uninstall(IPluginContext context)
    {
        hookSubscription?.Dispose();
    }
}

/// <summary>
/// Test plugin that registers system hooks for logging.
/// </summary>
public class LoggingPlugin : IWorldPlugin
{
    public string Name => "Logging";
    public List<string> Logs { get; } = [];
    private EventSubscription? hookSubscription;

    public void Install(IPluginContext context)
    {
        var world = (World)context.World;
        hookSubscription = world.AddSystemHook(
            beforeHook: (system, dt) => Logs.Add($"START: {system.GetType().Name}"),
            afterHook: (system, dt) => Logs.Add($"END: {system.GetType().Name}")
        );
    }

    public void Uninstall(IPluginContext context)
    {
        hookSubscription?.Dispose();
    }
}

/// <summary>
/// Test plugin that conditionally disables debug systems.
/// </summary>
public class DebugControlPlugin : IWorldPlugin
{
    public string Name => "DebugControl";
    public bool DebugMode { get; set; } = true;
    private EventSubscription? hookSubscription;

    public void Install(IPluginContext context)
    {
        var world = (World)context.World;
        hookSubscription = world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                if (system.GetType().Name.Contains("Debug") && !DebugMode)
                {
                    system.Enabled = false;
                }
            },
            afterHook: null
        );
    }

    public void Uninstall(IPluginContext context)
    {
        hookSubscription?.Dispose();
    }
}

/// <summary>
/// Integration tests for system hooks with plugins.
/// </summary>
public class SystemHookPluginIntegrationTests
{
    #region Plugin Hook Registration

    [Fact]
    public void Plugin_CanRegisterSystemHooks()
    {
        using var world = new World();
        var plugin = new ProfilingPlugin();

        world.InstallPlugin(plugin);
        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.Single(plugin.SystemExecutionCounts);
        Assert.Equal(1, plugin.SystemExecutionCounts[nameof(TestHookSystem)]);
    }

    [Fact]
    public void Plugin_Uninstall_UnregistersHooks()
    {
        using var world = new World();
        var plugin = new ProfilingPlugin();

        world.InstallPlugin(plugin);
        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.Equal(1, plugin.SystemExecutionCounts[nameof(TestHookSystem)]);

        world.UninstallPlugin<ProfilingPlugin>();
        world.Update(0.016f);

        // Count should not increase after uninstall
        Assert.Equal(1, plugin.SystemExecutionCounts[nameof(TestHookSystem)]);
    }

    [Fact]
    public void MultiplePlugins_CanRegisterIndependentHooks()
    {
        using var world = new World();
        var profilingPlugin = new ProfilingPlugin();
        var loggingPlugin = new LoggingPlugin();

        world.InstallPlugin(profilingPlugin);
        world.InstallPlugin(loggingPlugin);
        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.Equal(1, profilingPlugin.SystemExecutionCounts[nameof(TestHookSystem)]);
        Assert.Equal(2, loggingPlugin.Logs.Count);
        Assert.Equal("START: TestHookSystem", loggingPlugin.Logs[0]);
        Assert.Equal("END: TestHookSystem", loggingPlugin.Logs[1]);
    }

    [Fact]
    public void Plugin_Uninstall_DoesNotAffectOtherPluginHooks()
    {
        using var world = new World();
        var plugin1 = new ProfilingPlugin();
        var plugin2 = new LoggingPlugin();

        world.InstallPlugin(plugin1);
        world.InstallPlugin(plugin2);
        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        world.UninstallPlugin<ProfilingPlugin>();
        world.Update(0.016f);

        // Plugin1 hook should not execute after uninstall
        Assert.Equal(1, plugin1.SystemExecutionCounts[nameof(TestHookSystem)]);

        // Plugin2 hook should continue executing
        Assert.Equal(4, plugin2.Logs.Count); // 2 from first update + 2 from second
    }

    #endregion

    #region Plugin Use Cases

    [Fact]
    public void ProfilingPlugin_TracksAllSystemExecutions()
    {
        using var world = new World();
        var plugin = new ProfilingPlugin();

        world.InstallPlugin(plugin);
        world.AddSystem<TestHookSystem>();
        world.AddSystem<TestLifecycleSystem>();
        world.Update(0.016f);
        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(2, plugin.SystemExecutionCounts.Count);
        Assert.Equal(3, plugin.SystemExecutionCounts[nameof(TestHookSystem)]);
        Assert.Equal(3, plugin.SystemExecutionCounts[nameof(TestLifecycleSystem)]);
    }

    [Fact]
    public void LoggingPlugin_LogsSystemExecutionOrder()
    {
        using var world = new World();
        var plugin = new LoggingPlugin();

        world.InstallPlugin(plugin);
        world.AddSystem<TestHookSystem>(SystemPhase.Update, order: 0);
        world.AddSystem<TestLifecycleSystem>(SystemPhase.Update, order: 1);
        world.Update(0.016f);

        Assert.Equal(4, plugin.Logs.Count);
        Assert.Equal("START: TestHookSystem", plugin.Logs[0]);
        Assert.Equal("END: TestHookSystem", plugin.Logs[1]);
        Assert.Equal("START: TestLifecycleSystem", plugin.Logs[2]);
        Assert.Equal("END: TestLifecycleSystem", plugin.Logs[3]);
    }

    [Fact]
    public void DebugControlPlugin_DisablesDebugSystemsWhenDebugModeOff()
    {
        using var world = new World();
        var plugin = new DebugControlPlugin { DebugMode = false };

        world.InstallPlugin(plugin);

        // System name doesn't contain "Debug", so it should execute
        var normalSystem = new TestHookSystem();
        world.AddSystem(normalSystem);
        world.Update(0.016f);

        Assert.Equal(1, normalSystem.UpdateCount);
    }

    #endregion

    #region Plugin Lifecycle with Hooks

    [Fact]
    public void Plugin_InstallAfterSystems_HooksStillWork()
    {
        using var world = new World();
        var plugin = new ProfilingPlugin();

        world.AddSystem<TestHookSystem>();
        world.InstallPlugin(plugin); // Install after adding system
        world.Update(0.016f);

        Assert.Equal(1, plugin.SystemExecutionCounts[nameof(TestHookSystem)]);
    }

    [Fact]
    public void Plugin_ReinstallAfterUninstall_RegistersHooksAgain()
    {
        using var world = new World();
        var plugin = new ProfilingPlugin();

        world.InstallPlugin(plugin);
        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        world.UninstallPlugin<ProfilingPlugin>();
        world.Update(0.016f);

        // Re-install
        plugin.SystemExecutionCounts.Clear();
        world.InstallPlugin(plugin);
        world.Update(0.016f);

        Assert.Equal(1, plugin.SystemExecutionCounts[nameof(TestHookSystem)]);
    }

    #endregion

    #region Phase-Filtered Hooks in Plugins

    [Fact]
    public void Plugin_CanRegisterPhaseFilteredHooks()
    {
        using var world = new World();

        var updatePhaseCount = 0;
        var plugin = new TestSimplePlugin();

        world.InstallPlugin(plugin);

        // Register hook for Update phase only
        var hookSub = world.AddSystemHook(
            beforeHook: (system, dt) => updatePhaseCount++,
            afterHook: null,
            phase: SystemPhase.Update
        );

        world.AddSystem<TestHookSystem>(SystemPhase.Update);
        world.AddSystem<TestLifecycleSystem>(SystemPhase.FixedUpdate);
        world.Update(0.016f);

        Assert.Equal(1, updatePhaseCount); // Only Update phase system

        hookSub.Dispose();
    }

    #endregion

    #region Combined Plugin Systems and Hooks

    [Fact]
    public void Plugin_SystemsAndHooks_WorkTogether()
    {
        using var world = new World();
        var loggingPlugin = new LoggingPlugin();

        world.InstallPlugin(loggingPlugin);

        // Add a system registered by a plugin
        var pluginSystem = new TestPluginSystem();
        world.AddSystem(pluginSystem);

        world.Update(0.016f);

        // Hook should track plugin-registered system
        Assert.Contains("START: TestPluginSystem", loggingPlugin.Logs);
        Assert.Contains("END: TestPluginSystem", loggingPlugin.Logs);
        Assert.Equal(1, pluginSystem.UpdateCount);
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Plugin_HookException_DoesNotCrashWorld()
    {
        using var world = new World();
        var exceptionThrown = false;

        // This test verifies that if a hook throws, it propagates
        // (we don't silently swallow exceptions)
        world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                exceptionThrown = true;
                throw new InvalidOperationException("Test exception");
            },
            afterHook: null
        );

        world.AddSystem<TestHookSystem>();

        Assert.Throws<InvalidOperationException>(() => world.Update(0.016f));
        Assert.True(exceptionThrown);
    }

    #endregion
}
