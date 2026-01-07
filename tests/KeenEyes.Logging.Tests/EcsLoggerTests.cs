using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests;

public class EcsLoggerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogManager_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new EcsLogger(null!));
    }

    [Fact]
    public void Constructor_WithValidLogManager_SetsDefaults()
    {
        using var logManager = new LogManager();
        var logger = new EcsLogger(logManager);

        logger.IsEnabled.ShouldBeTrue();
        logger.LogManager.ShouldBe(logManager);
    }

    #endregion

    #region Category Level Tests

    [Fact]
    public void SetCategoryLevel_UpdatesLevel()
    {
        using var logManager = new LogManager();
        var logger = new EcsLogger(logManager);

        logger.SetCategoryLevel(EcsLogCategory.System, LogLevel.Warning);

        logger.GetCategoryLevel(EcsLogCategory.System).ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void GetCategoryLevel_DefaultsToTrace()
    {
        using var logManager = new LogManager();
        var logger = new EcsLogger(logManager);

        logger.GetCategoryLevel(EcsLogCategory.System).ShouldBe(LogLevel.Trace);
        logger.GetCategoryLevel(EcsLogCategory.Entity).ShouldBe(LogLevel.Trace);
        logger.GetCategoryLevel(EcsLogCategory.Component).ShouldBe(LogLevel.Trace);
        logger.GetCategoryLevel(EcsLogCategory.Query).ShouldBe(LogLevel.Trace);
    }

    [Fact]
    public void IsLevelEnabled_RespectsGlobalEnabled()
    {
        using var logManager = new LogManager();
        logManager.AddProvider(new TestLogProvider());
        var logger = new EcsLogger(logManager) { IsEnabled = false };

        logger.IsLevelEnabled(EcsLogCategory.System, LogLevel.Info).ShouldBeFalse();
    }

    [Fact]
    public void IsLevelEnabled_RespectsCategoryLevel()
    {
        using var logManager = new LogManager();
        logManager.AddProvider(new TestLogProvider());
        var logger = new EcsLogger(logManager);

        logger.SetCategoryLevel(EcsLogCategory.System, LogLevel.Warning);

        logger.IsLevelEnabled(EcsLogCategory.System, LogLevel.Debug).ShouldBeFalse();
        logger.IsLevelEnabled(EcsLogCategory.System, LogLevel.Warning).ShouldBeTrue();
        logger.IsLevelEnabled(EcsLogCategory.System, LogLevel.Error).ShouldBeTrue();
    }

    #endregion

    #region System Logging Tests

    [Fact]
    public void LogSystemRegistered_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogSystemRegistered("MovementSystem", "Update", 10);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Category.ShouldBe("ECS.System");
        provider.Messages[0].Level.ShouldBe(LogLevel.Info);
        provider.Messages[0].Message.ShouldContain("MovementSystem");
        provider.Messages[0].Message.ShouldContain("Update");
        provider.Messages[0].Properties.ShouldNotBeNull();
        provider.Messages[0].Properties!["SystemType"].ShouldBe("MovementSystem");
        provider.Messages[0].Properties!["Phase"].ShouldBe("Update");
        provider.Messages[0].Properties!["Order"].ShouldBe(10);
    }

    [Fact]
    public void LogSystemStarted_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogSystemStarted("MovementSystem", 0.016f);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Category.ShouldBe("ECS.System");
        provider.Messages[0].Level.ShouldBe(LogLevel.Trace);
        provider.Messages[0].Message.ShouldContain("MovementSystem");
        provider.Messages[0].Properties!["DeltaTime"].ShouldBe(0.016f);
    }

    [Fact]
    public void LogSystemCompleted_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogSystemStarted("MovementSystem", 0.016f);
        Thread.Sleep(5); // Ensure some time passes
        logger.LogSystemCompleted("MovementSystem", 0.016f);

        provider.MessageCount.ShouldBe(2);
        provider.Messages[1].Level.ShouldBe(LogLevel.Debug);
        provider.Messages[1].Message.ShouldContain("completed");
        provider.Messages[1].Properties!["ElapsedMs"].ShouldBeOfType<double>();
    }

    [Fact]
    public void LogSystemEnabledChanged_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogSystemEnabledChanged("MovementSystem", false);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Level.ShouldBe(LogLevel.Info);
        provider.Messages[0].Message.ShouldContain("disabled");
        provider.Messages[0].Properties!["Enabled"].ShouldBe(false);
    }

    [Fact]
    public void LogSystemRegistered_WhenBelowLevel_DoesNotLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.SetCategoryLevel(EcsLogCategory.System, LogLevel.Warning);
        logger.LogSystemRegistered("MovementSystem", "Update", 10);

        provider.MessageCount.ShouldBe(0);
    }

    #endregion

    #region Entity Logging Tests

    [Fact]
    public void LogEntityCreated_WithName_IncludesName()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogEntityCreated(42, 1, "Player");

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Category.ShouldBe("ECS.Entity");
        provider.Messages[0].Level.ShouldBe(LogLevel.Debug);
        provider.Messages[0].Message.ShouldContain("42");
        provider.Messages[0].Message.ShouldContain("Player");
        provider.Messages[0].Properties!["EntityName"].ShouldBe("Player");
    }

    [Fact]
    public void LogEntityCreated_WithoutName_OmitsName()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogEntityCreated(42, 1, null);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Message.ShouldNotContain("'");
        provider.Messages[0].Properties!["EntityName"].ShouldBeNull();
    }

    [Fact]
    public void LogEntityDestroyed_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogEntityDestroyed(42, 1);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Category.ShouldBe("ECS.Entity");
        provider.Messages[0].Level.ShouldBe(LogLevel.Debug);
        provider.Messages[0].Message.ShouldContain("destroyed");
        provider.Messages[0].Properties!["EntityId"].ShouldBe(42);
        provider.Messages[0].Properties!["EntityVersion"].ShouldBe(1);
    }

    [Fact]
    public void LogEntityParentChanged_WithParent_LogsParenting()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogEntityParentChanged(42, 100);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Level.ShouldBe(LogLevel.Trace);
        provider.Messages[0].Message.ShouldContain("parented to");
        provider.Messages[0].Properties!["ParentId"].ShouldBe(100);
    }

    [Fact]
    public void LogEntityParentChanged_WithoutParent_LogsUnparenting()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogEntityParentChanged(42, null);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Message.ShouldContain("unparented");
    }

    #endregion

    #region Component Logging Tests

    [Fact]
    public void LogComponentAdded_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogComponentAdded(42, "Position");

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Category.ShouldBe("ECS.Component");
        provider.Messages[0].Level.ShouldBe(LogLevel.Trace);
        provider.Messages[0].Message.ShouldContain("added");
        provider.Messages[0].Properties!["ComponentType"].ShouldBe("Position");
    }

    [Fact]
    public void LogComponentRemoved_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogComponentRemoved(42, "Position");

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Message.ShouldContain("removed");
    }

    [Fact]
    public void LogComponentChanged_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogComponentChanged(42, "Position");

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Message.ShouldContain("changed");
    }

    #endregion

    #region Query Logging Tests

    [Fact]
    public void LogQueryExecuted_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogQueryExecuted(["Position", "Velocity"], 5, true);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Category.ShouldBe("ECS.Query");
        provider.Messages[0].Level.ShouldBe(LogLevel.Trace);
        provider.Messages[0].Message.ShouldContain("Position");
        provider.Messages[0].Message.ShouldContain("Velocity");
        provider.Messages[0].Message.ShouldContain("5 archetypes");
        provider.Messages[0].Message.ShouldContain("cache hit");
        provider.Messages[0].Properties!["CacheHit"].ShouldBe(true);
    }

    [Fact]
    public void LogQueryCacheStats_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogQueryCacheStats(10, 1000, 50, 95.2);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Level.ShouldBe(LogLevel.Info);
        provider.Messages[0].Message.ShouldContain("10 queries");
        provider.Messages[0].Message.ShouldContain("95.2%");
        provider.Messages[0].Properties!["HitRate"].ShouldBe(95.2);
    }

    [Fact]
    public void LogQueryCacheInvalidated_WritesToLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager);

        logger.LogQueryCacheInvalidated("New archetype created");

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Level.ShouldBe(LogLevel.Debug);
        provider.Messages[0].Message.ShouldContain("invalidated");
        provider.Messages[0].Properties!["Reason"].ShouldBe("New archetype created");
    }

    #endregion

    #region Disabled Logging Tests

    [Fact]
    public void AllLoggingMethods_WhenDisabled_DoNotLog()
    {
        using var logManager = new LogManager();
        var provider = new TestLogProvider();
        logManager.AddProvider(provider);
        var logger = new EcsLogger(logManager) { IsEnabled = false };

        logger.LogSystemRegistered("Test", "Update", 0);
        logger.LogSystemStarted("Test", 0.016f);
        logger.LogSystemCompleted("Test", 0.016f);
        logger.LogEntityCreated(1, 1, "Test");
        logger.LogEntityDestroyed(1, 1);
        logger.LogComponentAdded(1, "Test");
        logger.LogComponentRemoved(1, "Test");
        logger.LogComponentChanged(1, "Test");
        logger.LogQueryExecuted(["Test"], 1, true);
        logger.LogQueryCacheStats(1, 1, 1, 50.0);
        logger.LogQueryCacheInvalidated("Test");

        provider.MessageCount.ShouldBe(0);
    }

    #endregion
}
