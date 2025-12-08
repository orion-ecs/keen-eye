using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests.Providers;

public class TestLogProviderTests
{
    [Fact]
    public void Name_ReturnsTest()
    {
        using var provider = new TestLogProvider();

        provider.Name.ShouldBe("Test");
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        using var provider = new TestLogProvider();

        provider.MinimumLevel.ShouldBe(LogLevel.Trace);
    }

    [Fact]
    public void Messages_InitiallyEmpty()
    {
        using var provider = new TestLogProvider();

        provider.Messages.ShouldBeEmpty();
        provider.MessageCount.ShouldBe(0);
    }

    [Fact]
    public void Log_CapturesMessage()
    {
        using var provider = new TestLogProvider();

        provider.Log(LogLevel.Info, "TestCategory", "Test message", null);

        provider.MessageCount.ShouldBe(1);
        provider.Messages[0].Level.ShouldBe(LogLevel.Info);
        provider.Messages[0].Category.ShouldBe("TestCategory");
        provider.Messages[0].Message.ShouldBe("Test message");
    }

    [Fact]
    public void Log_CapturesTimestamp()
    {
        using var provider = new TestLogProvider();
        var before = DateTime.Now;

        provider.Log(LogLevel.Info, "Test", "Message", null);

        var after = DateTime.Now;
        provider.Messages[0].Timestamp.ShouldBeInRange(before, after);
    }

    [Fact]
    public void Log_CapturesProperties()
    {
        using var provider = new TestLogProvider();
        var properties = new Dictionary<string, object?>
        {
            ["String"] = "Value",
            ["Int"] = 42,
            ["Null"] = null
        };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var capturedProps = provider.Messages[0].Properties;
        capturedProps.ShouldNotBeNull();
        capturedProps["String"].ShouldBe("Value");
        capturedProps["Int"].ShouldBe(42);
        capturedProps["Null"].ShouldBeNull();
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotCapture()
    {
        using var provider = new TestLogProvider { MinimumLevel = LogLevel.Warning };

        provider.Log(LogLevel.Debug, "Test", "Debug message", null);

        provider.MessageCount.ShouldBe(0);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message 1", null);
        provider.Log(LogLevel.Info, "Test", "Message 2", null);

        provider.Clear();

        provider.MessageCount.ShouldBe(0);
        provider.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void ContainsMessage_WithMatchingText_ReturnsTrue()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "Test", "Hello World", null);

        provider.ContainsMessage("World").ShouldBeTrue();
    }

    [Fact]
    public void ContainsMessage_WithNonMatchingText_ReturnsFalse()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "Test", "Hello World", null);

        provider.ContainsMessage("Goodbye").ShouldBeFalse();
    }

    [Fact]
    public void ContainsLevel_WithMatchingLevel_ReturnsTrue()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Warning, "Test", "Warning message", null);

        provider.ContainsLevel(LogLevel.Warning).ShouldBeTrue();
    }

    [Fact]
    public void ContainsLevel_WithNonMatchingLevel_ReturnsFalse()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "Test", "Info message", null);

        provider.ContainsLevel(LogLevel.Error).ShouldBeFalse();
    }

    [Fact]
    public void ContainsCategory_WithMatchingCategory_ReturnsTrue()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "MyCategory", "Message", null);

        provider.ContainsCategory("MyCategory").ShouldBeTrue();
    }

    [Fact]
    public void ContainsCategory_WithNonMatchingCategory_ReturnsFalse()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "MyCategory", "Message", null);

        provider.ContainsCategory("OtherCategory").ShouldBeFalse();
    }

    [Fact]
    public void GetByLevel_ReturnsMatchingMessages()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "Test", "Info 1", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Info, "Test", "Info 2", null);

        var infoMessages = provider.GetByLevel(LogLevel.Info);

        infoMessages.Count.ShouldBe(2);
        infoMessages.ShouldAllBe(m => m.Level == LogLevel.Info);
    }

    [Fact]
    public void GetByCategory_ReturnsMatchingMessages()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "CategoryA", "Message A1", null);
        provider.Log(LogLevel.Info, "CategoryB", "Message B", null);
        provider.Log(LogLevel.Info, "CategoryA", "Message A2", null);

        var categoryAMessages = provider.GetByCategory("CategoryA");

        categoryAMessages.Count.ShouldBe(2);
        categoryAMessages.ShouldAllBe(m => m.Category == "CategoryA");
    }

    [Fact]
    public void Messages_ReturnsCopy()
    {
        using var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message", null);

        var messages1 = provider.Messages;
        var messages2 = provider.Messages;

        messages1.ShouldNotBeSameAs(messages2);
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var provider = new TestLogProvider();

        Should.NotThrow(() => provider.Flush());
    }

    [Fact]
    public void Dispose_ClearsMessages()
    {
        var provider = new TestLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message", null);

        provider.Dispose();

        provider.MessageCount.ShouldBe(0);
    }

    [Fact]
    public void Log_FromMultipleThreads_ThreadSafe()
    {
        using var provider = new TestLogProvider();

        Parallel.For(0, 100, i =>
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        });

        provider.MessageCount.ShouldBe(100);
    }
}
