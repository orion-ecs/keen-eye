using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests;

public class EcsLoggingPluginTests
{
    #region Test Components

    private struct TestPosition : IComponent
    {
        public float X;
        public float Y;
    }

    private struct TestVelocity : IComponent
    {
        public float X;
        public float Y;
    }

    private sealed class TestSystem : SystemBase
    {
        public int UpdateCount { get; private set; }

        public override void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogManager_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new EcsLoggingPlugin(null!));
    }

    [Fact]
    public void Constructor_WithValidLogManager_SetsProperties()
    {
        using var logManager = new LogManager();
        var plugin = new EcsLoggingPlugin(logManager);

        plugin.Name.ShouldBe("EcsLogging");
        plugin.Logger.ShouldNotBeNull();
        plugin.Logger.LogManager.ShouldBe(logManager);
    }

    #endregion

    #region Installation Tests

    [Fact]
    public void Install_ExposesLoggerAsExtension()
    {
        using var logManager = new LogManager();
        var plugin = new EcsLoggingPlugin(logManager);
        using var world = new World();

        world.InstallPlugin(plugin);

        var logger = world.GetExtension<EcsLogger>();
        logger.ShouldBe(plugin.Logger);
    }

    [Fact]
    public void Uninstall_RemovesExtension()
    {
        using var logManager = new LogManager();
        var plugin = new EcsLoggingPlugin(logManager);
        using var world = new World();
        world.InstallPlugin(plugin);

        world.UninstallPlugin("EcsLogging");

        world.TryGetExtension<EcsLogger>(out var logger).ShouldBeFalse();
    }

    #endregion

    #region Entity Lifecycle Logging Tests

    [Fact]
    public void EntityCreated_LogsCreation()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        provider.Clear(); // Clear any installation logs

        var entity = world.Spawn("TestEntity").Build();

        provider.ContainsCategory("ECS.Entity").ShouldBeTrue();
        provider.ContainsMessage("created").ShouldBeTrue();
        provider.ContainsMessage("TestEntity").ShouldBeTrue();
    }

    [Fact]
    public void EntityDestroyed_LogsDestruction()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        var entity = world.Spawn().Build();
        provider.Clear();

        world.Despawn(entity);

        provider.ContainsCategory("ECS.Entity").ShouldBeTrue();
        provider.ContainsMessage("destroyed").ShouldBeTrue();
    }

    #endregion

    #region System Execution Logging Tests

    [Fact]
    public void SystemExecution_LogsStartAndComplete()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        world.AddSystem(new TestSystem());
        provider.Clear();

        world.Update(0.016f);

        var systemLogs = provider.GetByCategory("ECS.System");
        systemLogs.Count.ShouldBeGreaterThanOrEqualTo(2);
        systemLogs.ShouldContain(l => l.Message.Contains("started"));
        systemLogs.ShouldContain(l => l.Message.Contains("completed"));
    }

    [Fact]
    public void SystemExecution_WhenSystemCategoryDisabled_DoesNotLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        // Disable system logging at category level
        plugin.Logger.SetCategoryLevel(EcsLogCategory.System, LogLevel.Fatal);

        using var world = new World();
        world.InstallPlugin(plugin);
        world.AddSystem(new TestSystem());
        provider.Clear();

        world.Update(0.016f);

        var systemLogs = provider.GetByCategory("ECS.System");
        systemLogs.Count.ShouldBe(0);
    }

    #endregion

    #region Component Logging Tests

    [Fact]
    public void EnableComponentLogging_WithoutInstall_ThrowsInvalidOperationException()
    {
        using var logManager = new LogManager();
        var plugin = new EcsLoggingPlugin(logManager);

        Should.Throw<InvalidOperationException>(() => plugin.EnableComponentLogging<TestPosition>());
    }

    [Fact]
    public void EnableComponentLogging_LogsComponentAddition()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        plugin.EnableComponentLogging<TestPosition>();
        provider.Clear();

        var entity = world.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();

        var componentLogs = provider.GetByCategory("ECS.Component");
        componentLogs.Count.ShouldBeGreaterThanOrEqualTo(1);
        componentLogs.ShouldContain(l => l.Message.Contains("added"));
        componentLogs.ShouldContain(l => l.Message.Contains("TestPosition"));
    }

    [Fact]
    public void EnableComponentLogging_LogsComponentRemoval()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        plugin.EnableComponentLogging<TestPosition>();
        var entity = world.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();
        provider.Clear();

        world.Remove<TestPosition>(entity);

        var componentLogs = provider.GetByCategory("ECS.Component");
        componentLogs.ShouldContain(l => l.Message.Contains("removed"));
    }

    [Fact]
    public void EnableComponentLogging_LogsComponentChange()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        plugin.EnableComponentLogging<TestPosition>();
        var entity = world.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();
        provider.Clear();

        world.Set(entity, new TestPosition { X = 10, Y = 20 });

        var componentLogs = provider.GetByCategory("ECS.Component");
        componentLogs.ShouldContain(l => l.Message.Contains("changed"));
    }

    [Fact]
    public void ComponentNotEnabled_DoesNotLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        // Note: Not calling EnableComponentLogging<TestPosition>()
        provider.Clear();

        var entity = world.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();

        var componentLogs = provider.GetByCategory("ECS.Component");
        componentLogs.Count.ShouldBe(0);
    }

    [Fact]
    public void MultipleComponentTypes_LogsAll()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        plugin.EnableComponentLogging<TestPosition>();
        plugin.EnableComponentLogging<TestVelocity>();
        provider.Clear();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1, Y = 2 })
            .With(new TestVelocity { X = 3, Y = 4 })
            .Build();

        var componentLogs = provider.GetByCategory("ECS.Component");
        componentLogs.ShouldContain(l => l.Message.Contains("TestPosition"));
        componentLogs.ShouldContain(l => l.Message.Contains("TestVelocity"));
    }

    #endregion

    #region Query Stats Logging Tests

    [Fact]
    public void LogQueryStats_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        using var world = new World();
        world.InstallPlugin(plugin);
        provider.Clear();

        plugin.LogQueryStats(5, 100, 10, 90.9);

        provider.ContainsCategory("ECS.Query").ShouldBeTrue();
        provider.ContainsMessage("90.9%").ShouldBeTrue();
    }

    #endregion

    #region Verbosity Configuration Tests

    [Fact]
    public void VerbosityConfiguration_BeforeInstall_TakesEffect()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        // Configure verbosity BEFORE installing
        plugin.Logger.SetCategoryLevel(EcsLogCategory.Entity, LogLevel.Warning);

        using var world = new World();
        world.InstallPlugin(plugin);
        provider.Clear();

        // This should NOT be logged (Debug < Warning)
        var entity = world.Spawn().Build();

        var entityLogs = provider.GetByCategory("ECS.Entity");
        entityLogs.Count.ShouldBe(0);
    }

    [Fact]
    public void GlobalDisable_PreventsAllLogging()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var plugin = new EcsLoggingPlugin(logManager);

        plugin.Logger.IsEnabled = false;

        using var world = new World();
        world.InstallPlugin(plugin);
        world.AddSystem(new TestSystem());
        provider.Clear();

        var entity = world.Spawn().Build();
        world.Update(0.016f);
        world.Despawn(entity);

        provider.MessageCount.ShouldBe(0);
    }

    #endregion
}
