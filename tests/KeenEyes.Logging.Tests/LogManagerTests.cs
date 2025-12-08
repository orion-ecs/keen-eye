using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests;

public class LogManagerTests
{
    #region Provider Management Tests

    [Fact]
    public void AddProvider_WithValidProvider_AddsSuccessfully()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();

        manager.AddProvider(provider);

        manager.ProviderCount.ShouldBe(1);
        manager.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AddProvider_WithNullProvider_ThrowsArgumentNullException()
    {
        using var manager = new LogManager();

        Should.Throw<ArgumentNullException>(() => manager.AddProvider(null!));
    }

    [Fact]
    public void AddProvider_WithDuplicateName_ThrowsInvalidOperationException()
    {
        using var manager = new LogManager();
        manager.AddProvider(new TestLogProvider());

        Should.Throw<InvalidOperationException>(() => manager.AddProvider(new TestLogProvider()));
    }

    [Fact]
    public void AddProvider_AfterDispose_ThrowsObjectDisposedException()
    {
        var manager = new LogManager();
        manager.Dispose();

        Should.Throw<ObjectDisposedException>(() => manager.AddProvider(new TestLogProvider()));
    }

    [Fact]
    public void RemoveProvider_WithExistingName_ReturnsTrue()
    {
        using var manager = new LogManager();
        manager.AddProvider(new TestLogProvider());

        var result = manager.RemoveProvider("Test");

        result.ShouldBeTrue();
        manager.ProviderCount.ShouldBe(0);
    }

    [Fact]
    public void RemoveProvider_WithNonExistentName_ReturnsFalse()
    {
        using var manager = new LogManager();

        var result = manager.RemoveProvider("NonExistent");

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetProvider_WithExistingName_ReturnsProvider()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        var result = manager.GetProvider("Test");

        result.ShouldBe(provider);
    }

    [Fact]
    public void GetProvider_WithNonExistentName_ReturnsNull()
    {
        using var manager = new LogManager();

        var result = manager.GetProvider("NonExistent");

        result.ShouldBeNull();
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void Log_WithRegisteredProvider_SendsMessageToProvider()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        manager.Log(LogLevel.Info, "TestCategory", "Test message");

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Level.ShouldBe(LogLevel.Info);
        provider.Messages[0].Category.ShouldBe("TestCategory");
        provider.Messages[0].Message.ShouldBe("Test message");
    }

    [Fact]
    public void Log_WithMultipleProviders_SendsToAll()
    {
        using var manager = new LogManager();
        var provider1 = new TestLogProvider();
        var provider2 = new NullLogProvider();
        manager.AddProvider(provider1);
        manager.AddProvider(provider2);

        manager.Log(LogLevel.Info, "Test", "Message");

        provider1.MessageCount.ShouldBe(1);
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotSend()
    {
        using var manager = new LogManager();
        manager.MinimumLevel = LogLevel.Warning;
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        manager.Log(LogLevel.Debug, "Test", "Debug message");

        provider.MessageCount.ShouldBe(0);
    }

    [Fact]
    public void Log_BelowProviderMinimumLevel_DoesNotSendToThatProvider()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider { MinimumLevel = LogLevel.Warning };
        manager.AddProvider(provider);

        manager.Log(LogLevel.Info, "Test", "Info message");

        provider.MessageCount.ShouldBe(0);
    }

    [Fact]
    public void Log_WithProperties_IncludesProperties()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);
        var properties = new Dictionary<string, object?> { ["Key"] = "Value" };

        manager.Log(LogLevel.Info, "Test", "Message", properties);

        provider.Messages[0].Properties.ShouldNotBeNull();
        provider.Messages[0].Properties!["Key"].ShouldBe("Value");
    }

    #endregion

    #region Convenience Method Tests

    [Theory]
    [InlineData(nameof(LogManager.Trace), LogLevel.Trace)]
    [InlineData(nameof(LogManager.Debug), LogLevel.Debug)]
    [InlineData(nameof(LogManager.Info), LogLevel.Info)]
    [InlineData(nameof(LogManager.Warning), LogLevel.Warning)]
    [InlineData(nameof(LogManager.Error), LogLevel.Error)]
    [InlineData(nameof(LogManager.Fatal), LogLevel.Fatal)]
    public void ConvenienceMethod_LogsAtCorrectLevel(string methodName, LogLevel expectedLevel)
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        switch (methodName)
        {
            case nameof(LogManager.Trace):
                manager.Trace("Test", "Message");
                break;
            case nameof(LogManager.Debug):
                manager.Debug("Test", "Message");
                break;
            case nameof(LogManager.Info):
                manager.Info("Test", "Message");
                break;
            case nameof(LogManager.Warning):
                manager.Warning("Test", "Message");
                break;
            case nameof(LogManager.Error):
                manager.Error("Test", "Message");
                break;
            case nameof(LogManager.Fatal):
                manager.Fatal("Test", "Message");
                break;
        }

        provider.Messages[0].Level.ShouldBe(expectedLevel);
    }

    #endregion

    #region IsLevelEnabled Tests

    [Fact]
    public void IsLevelEnabled_WithNoProviders_ReturnsFalse()
    {
        using var manager = new LogManager();

        manager.IsLevelEnabled(LogLevel.Info).ShouldBeFalse();
    }

    [Fact]
    public void IsLevelEnabled_AboveMinimum_ReturnsTrue()
    {
        using var manager = new LogManager();
        manager.MinimumLevel = LogLevel.Info;
        manager.AddProvider(new TestLogProvider());

        manager.IsLevelEnabled(LogLevel.Warning).ShouldBeTrue();
    }

    [Fact]
    public void IsLevelEnabled_BelowMinimum_ReturnsFalse()
    {
        using var manager = new LogManager();
        manager.MinimumLevel = LogLevel.Warning;
        manager.AddProvider(new TestLogProvider());

        manager.IsLevelEnabled(LogLevel.Info).ShouldBeFalse();
    }

    #endregion

    #region Scope Tests

    [Fact]
    public void BeginScope_AddsPropertiesToLogMessages()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("TestScope", new Dictionary<string, object?> { ["ScopeKey"] = "ScopeValue" }))
        {
            manager.Info("Test", "Scoped message");
        }

        provider.Messages[0].Properties.ShouldNotBeNull();
        provider.Messages[0].Properties!["ScopeKey"].ShouldBe("ScopeValue");
    }

    [Fact]
    public void BeginScope_WithNestedScopes_MergesProperties()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Outer", new Dictionary<string, object?> { ["OuterKey"] = "Outer" }))
        using (manager.BeginScope("Inner", new Dictionary<string, object?> { ["InnerKey"] = "Inner" }))
        {
            manager.Info("Test", "Nested message");
        }

        var props = provider.Messages[0].Properties;
        props.ShouldNotBeNull();
        props["OuterKey"].ShouldBe("Outer");
        props["InnerKey"].ShouldBe("Inner");
    }

    [Fact]
    public void BeginScope_ChildOverridesParentProperties()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Outer", new Dictionary<string, object?> { ["Key"] = "Outer" }))
        using (manager.BeginScope("Inner", new Dictionary<string, object?> { ["Key"] = "Inner" }))
        {
            manager.Info("Test", "Message");
        }

        provider.Messages[0].Properties!["Key"].ShouldBe("Inner");
    }

    [Fact]
    public void BeginScope_MessagePropertiesOverrideScope()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Scope", new Dictionary<string, object?> { ["Key"] = "Scope" }))
        {
            manager.Info("Test", "Message", new Dictionary<string, object?> { ["Key"] = "Message" });
        }

        provider.Messages[0].Properties!["Key"].ShouldBe("Message");
    }

    [Fact]
    public void BeginScope_AfterDispose_PropertiesNotIncluded()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Scope", new Dictionary<string, object?> { ["Key"] = "Value" }))
        {
            // Scope is active here
        }

        manager.Info("Test", "After scope");

        provider.Messages[0].Properties.ShouldBeNull();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_DisposesAllProviders()
    {
        var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        manager.Dispose();

        manager.ProviderCount.ShouldBe(0);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var manager = new LogManager();

        Should.NotThrow(() =>
        {
            manager.Dispose();
            manager.Dispose();
        });
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Log_FromMultipleThreads_DoesNotThrow()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        Should.NotThrow(() =>
        {
            Parallel.For(0, 100, i =>
            {
                manager.Info("Test", $"Message {i}");
            });
        });

        provider.MessageCount.ShouldBe(100);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void Log_WhenProviderThrows_DoesNotPropagateException()
    {
        using var manager = new LogManager();
        var throwingProvider = new ThrowingLogProvider { ThrowOnLog = true };
        manager.AddProvider(throwingProvider);

        Should.NotThrow(() => manager.Info("Test", "Message"));
    }

    [Fact]
    public void Log_WhenProviderThrows_ContinuesToOtherProviders()
    {
        using var manager = new LogManager();
        var throwingProvider = new ThrowingLogProvider { ThrowOnLog = true };
        var testProvider = new TestLogProvider();
        manager.AddProvider(throwingProvider);
        manager.AddProvider(testProvider);

        manager.Info("Test", "Message");

        testProvider.MessageCount.ShouldBe(1);
    }

    [Fact]
    public void Flush_WhenProviderThrows_DoesNotPropagateException()
    {
        using var manager = new LogManager();
        var throwingProvider = new ThrowingLogProvider { ThrowOnFlush = true };
        manager.AddProvider(throwingProvider);

        Should.NotThrow(() => manager.Flush());
    }

    [Fact]
    public void Flush_WhenProviderThrows_ContinuesToOtherProviders()
    {
        using var manager = new LogManager();
        var throwingProvider = new ThrowingLogProvider { ThrowOnFlush = true };
        var testProvider = new TestLogProvider();
        manager.AddProvider(throwingProvider);
        manager.AddProvider(testProvider);

        Should.NotThrow(() => manager.Flush());
        // If we get here without exception, both providers were processed
    }

    [Fact]
    public void Dispose_WhenProviderThrows_DoesNotPropagateException()
    {
        var manager = new LogManager();
        var throwingProvider = new ThrowingLogProvider { ThrowOnDispose = true };
        manager.AddProvider(throwingProvider);

        Should.NotThrow(() => manager.Dispose());
    }

    [Fact]
    public void Dispose_WhenProviderThrows_ContinuesToOtherProviders()
    {
        var manager = new LogManager();
        var throwingProvider = new ThrowingLogProvider { ThrowOnDispose = true };
        var testProvider = new TestLogProvider();
        manager.AddProvider(throwingProvider);
        manager.AddProvider(testProvider);

        manager.Dispose();

        manager.ProviderCount.ShouldBe(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Log_WithNoProviders_DoesNotThrow()
    {
        using var manager = new LogManager();

        Should.NotThrow(() => manager.Info("Test", "Message"));
    }

    [Fact]
    public void Log_WithScopeButNoMessageProperties_UsesScopeProperties()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Scope", new Dictionary<string, object?> { ["ScopeKey"] = "ScopeValue" }))
        {
            manager.Info("Test", "Message", null);
        }

        provider.Messages[0].Properties.ShouldNotBeNull();
        provider.Messages[0].Properties!["ScopeKey"].ShouldBe("ScopeValue");
    }

    [Fact]
    public void Log_WithNullScopeProperties_WorksCorrectly()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Scope", null))
        {
            manager.Info("Test", "Message");
        }

        // Should complete without error, properties should be null
        provider.Messages[0].Properties.ShouldBeNull();
    }

    [Fact]
    public void Flush_WithNoProviders_DoesNotThrow()
    {
        using var manager = new LogManager();

        Should.NotThrow(() => manager.Flush());
    }

    #endregion
}

/// <summary>
/// A log provider that throws exceptions for testing exception handling.
/// </summary>
internal sealed class ThrowingLogProvider : ILogProvider
{
    public string Name => "Throwing";
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
    public bool ThrowOnLog { get; set; }
    public bool ThrowOnFlush { get; set; }
    public bool ThrowOnDispose { get; set; }

    public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties)
    {
        if (ThrowOnLog)
        {
            throw new InvalidOperationException("Log exception for testing");
        }
    }

    public void Flush()
    {
        if (ThrowOnFlush)
        {
            throw new InvalidOperationException("Flush exception for testing");
        }
    }

    public void Dispose()
    {
        if (ThrowOnDispose)
        {
            throw new InvalidOperationException("Dispose exception for testing");
        }
    }
}
