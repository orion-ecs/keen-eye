using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests.Providers;

public class RingBufferLogProviderTests
{
    #region Constructor and Properties

    [Fact]
    public void Name_ReturnsRingBuffer()
    {
        using var provider = new RingBufferLogProvider();

        provider.Name.ShouldBe("RingBuffer");
    }

    [Fact]
    public void Capacity_DefaultsToDefaultCapacity()
    {
        using var provider = new RingBufferLogProvider();

        provider.Capacity.ShouldBe(RingBufferLogProvider.DefaultCapacity);
    }

    [Fact]
    public void Capacity_WithCustomValue_ReturnsCustomValue()
    {
        using var provider = new RingBufferLogProvider(500);

        provider.Capacity.ShouldBe(500);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new RingBufferLogProvider(0));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new RingBufferLogProvider(-1));
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        using var provider = new RingBufferLogProvider();

        provider.MinimumLevel.ShouldBe(LogLevel.Trace);
    }

    [Fact]
    public void EntryCount_InitiallyZero()
    {
        using var provider = new RingBufferLogProvider();

        provider.EntryCount.ShouldBe(0);
    }

    #endregion

    #region Log Method

    [Fact]
    public void Log_CapturesEntry()
    {
        using var provider = new RingBufferLogProvider();

        provider.Log(LogLevel.Info, "TestCategory", "Test message", null);

        provider.EntryCount.ShouldBe(1);
        var entries = provider.GetEntries();
        entries[0].Level.ShouldBe(LogLevel.Info);
        entries[0].Category.ShouldBe("TestCategory");
        entries[0].Message.ShouldBe("Test message");
    }

    [Fact]
    public void Log_CapturesTimestamp()
    {
        using var provider = new RingBufferLogProvider();
        var before = DateTime.Now;

        provider.Log(LogLevel.Info, "Test", "Message", null);

        var after = DateTime.Now;
        var entries = provider.GetEntries();
        entries[0].Timestamp.ShouldBeInRange(before, after);
    }

    [Fact]
    public void Log_CapturesProperties()
    {
        using var provider = new RingBufferLogProvider();
        var properties = new Dictionary<string, object?>
        {
            ["String"] = "Value",
            ["Int"] = 42,
            ["Null"] = null
        };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var capturedProps = provider.GetEntries()[0].Properties;
        capturedProps.ShouldNotBeNull();
        capturedProps["String"].ShouldBe("Value");
        capturedProps["Int"].ShouldBe(42);
        capturedProps["Null"].ShouldBeNull();
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotCapture()
    {
        using var provider = new RingBufferLogProvider { MinimumLevel = LogLevel.Warning };

        provider.Log(LogLevel.Debug, "Test", "Debug message", null);

        provider.EntryCount.ShouldBe(0);
    }

    [Fact]
    public void Log_RaisesLogAddedEvent()
    {
        using var provider = new RingBufferLogProvider();
        LogEntry? capturedEntry = null;
        provider.LogAdded += entry => capturedEntry = entry;

        provider.Log(LogLevel.Info, "Test", "Message", null);

        capturedEntry.ShouldNotBeNull();
        capturedEntry.Message.ShouldBe("Message");
    }

    #endregion

    #region Ring Buffer Behavior

    [Fact]
    public void Log_AtCapacity_EvictsOldestEntry()
    {
        using var provider = new RingBufferLogProvider(3);

        provider.Log(LogLevel.Info, "Test", "Message 1", null);
        provider.Log(LogLevel.Info, "Test", "Message 2", null);
        provider.Log(LogLevel.Info, "Test", "Message 3", null);
        provider.Log(LogLevel.Info, "Test", "Message 4", null);

        provider.EntryCount.ShouldBe(3);
        var entries = provider.GetEntries();
        entries[0].Message.ShouldBe("Message 2");
        entries[1].Message.ShouldBe("Message 3");
        entries[2].Message.ShouldBe("Message 4");
    }

    [Fact]
    public void Log_OverCapacity_MaintainsCapacityLimit()
    {
        using var provider = new RingBufferLogProvider(5);

        for (int i = 0; i < 100; i++)
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        }

        provider.EntryCount.ShouldBe(5);
        var entries = provider.GetEntries();
        entries[0].Message.ShouldBe("Message 95");
        entries[4].Message.ShouldBe("Message 99");
    }

    #endregion

    #region Clear Method

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message 1", null);
        provider.Log(LogLevel.Info, "Test", "Message 2", null);

        provider.Clear();

        provider.EntryCount.ShouldBe(0);
        provider.GetEntries().ShouldBeEmpty();
    }

    [Fact]
    public void Clear_RaisesLogsClearedEvent()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message", null);
        var eventRaised = false;
        provider.LogsCleared += () => eventRaised = true;

        provider.Clear();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Clear_ResetsStatistics()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Error, "Test", "Error", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);

        provider.Clear();

        var stats = provider.GetStats();
        stats.TotalCount.ShouldBe(0);
        stats.ErrorCount.ShouldBe(0);
        stats.WarningCount.ShouldBe(0);
    }

    #endregion

    #region GetStats Method

    [Fact]
    public void GetStats_ReturnsCorrectCounts()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Trace, "Test", "Trace", null);
        provider.Log(LogLevel.Debug, "Test", "Debug", null);
        provider.Log(LogLevel.Info, "Test", "Info 1", null);
        provider.Log(LogLevel.Info, "Test", "Info 2", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);
        provider.Log(LogLevel.Fatal, "Test", "Fatal", null);

        var stats = provider.GetStats();

        stats.TotalCount.ShouldBe(7);
        stats.TraceCount.ShouldBe(1);
        stats.DebugCount.ShouldBe(1);
        stats.InfoCount.ShouldBe(2);
        stats.WarningCount.ShouldBe(1);
        stats.ErrorCount.ShouldBe(1);
        stats.FatalCount.ShouldBe(1);
    }

    [Fact]
    public void GetStats_ReturnsTimestamps()
    {
        using var provider = new RingBufferLogProvider();
        var before = DateTime.Now;
        provider.Log(LogLevel.Info, "Test", "First", null);
        Thread.Sleep(10); // Small delay to ensure different timestamps
        provider.Log(LogLevel.Info, "Test", "Last", null);
        var after = DateTime.Now;

        var stats = provider.GetStats();

        stats.OldestTimestamp.ShouldNotBeNull();
        stats.NewestTimestamp.ShouldNotBeNull();
        stats.OldestTimestamp.Value.ShouldBeInRange(before, after);
        stats.NewestTimestamp.Value.ShouldBeInRange(before, after);
        stats.OldestTimestamp.Value.ShouldBeLessThanOrEqualTo(stats.NewestTimestamp.Value);
    }

    [Fact]
    public void GetStats_EmptyProvider_ReturnsNullTimestamps()
    {
        using var provider = new RingBufferLogProvider();

        var stats = provider.GetStats();

        stats.OldestTimestamp.ShouldBeNull();
        stats.NewestTimestamp.ShouldBeNull();
    }

    [Fact]
    public void GetStats_ReturnsCapacity()
    {
        using var provider = new RingBufferLogProvider(500);

        var stats = provider.GetStats();

        stats.Capacity.ShouldBe(500);
    }

    [Fact]
    public void GetStats_WithEviction_UpdatesCounts()
    {
        using var provider = new RingBufferLogProvider(2);
        provider.Log(LogLevel.Error, "Test", "Error 1", null);
        provider.Log(LogLevel.Error, "Test", "Error 2", null);
        provider.Log(LogLevel.Info, "Test", "Info", null); // Evicts Error 1

        var stats = provider.GetStats();

        stats.TotalCount.ShouldBe(2);
        stats.ErrorCount.ShouldBe(1);
        stats.InfoCount.ShouldBe(1);
    }

    #endregion

    #region Query Method

    [Fact]
    public void Query_NullQuery_ThrowsArgumentNullException()
    {
        using var provider = new RingBufferLogProvider();

        Should.Throw<ArgumentNullException>(() => provider.Query(null!));
    }

    [Fact]
    public void Query_EmptyQuery_ReturnsAllEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message 1", null);
        provider.Log(LogLevel.Info, "Test", "Message 2", null);

        var results = provider.Query(new LogQuery());

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void Query_ByMinLevel_ReturnsMatchingEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Debug, "Test", "Debug", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);

        var results = provider.Query(new LogQuery { MinLevel = LogLevel.Warning });

        results.Count.ShouldBe(2);
        results.ShouldAllBe(e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public void Query_ByMaxLevel_ReturnsMatchingEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Debug, "Test", "Debug", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);

        var results = provider.Query(new LogQuery { MaxLevel = LogLevel.Warning });

        results.Count.ShouldBe(2);
        results.ShouldAllBe(e => e.Level <= LogLevel.Warning);
    }

    [Fact]
    public void Query_ByLevelRange_ReturnsMatchingEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Debug, "Test", "Debug", null);
        provider.Log(LogLevel.Info, "Test", "Info", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);

        var results = provider.Query(new LogQuery { MinLevel = LogLevel.Info, MaxLevel = LogLevel.Warning });

        results.Count.ShouldBe(2);
        results.ShouldAllBe(e => e.Level >= LogLevel.Info && e.Level <= LogLevel.Warning);
    }

    [Fact]
    public void Query_ByCategoryPattern_Exact_ReturnsMatchingEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "CategoryA", "Message A", null);
        provider.Log(LogLevel.Info, "CategoryB", "Message B", null);

        var results = provider.Query(new LogQuery { CategoryPattern = "CategoryA" });

        results.Count.ShouldBe(1);
        results[0].Category.ShouldBe("CategoryA");
    }

    [Fact]
    public void Query_ByCategoryPattern_Wildcard_ReturnsMatchingEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "KeenEyes.Core", "Core message", null);
        provider.Log(LogLevel.Info, "KeenEyes.Physics", "Physics message", null);
        provider.Log(LogLevel.Info, "Other.System", "Other message", null);

        var results = provider.Query(new LogQuery { CategoryPattern = "KeenEyes.*" });

        results.Count.ShouldBe(2);
        results.ShouldAllBe(e => e.Category.StartsWith("KeenEyes"));
    }

    [Fact]
    public void Query_ByMessageContains_ReturnsMatchingEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "Hello World", null);
        provider.Log(LogLevel.Info, "Test", "Goodbye World", null);
        provider.Log(LogLevel.Info, "Test", "Something else", null);

        var results = provider.Query(new LogQuery { MessageContains = "World" });

        results.Count.ShouldBe(2);
        results.ShouldAllBe(e => e.Message.Contains("World"));
    }

    [Fact]
    public void Query_ByMessageContains_IsCaseInsensitive()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "Hello WORLD", null);
        provider.Log(LogLevel.Info, "Test", "hello world", null);

        var results = provider.Query(new LogQuery { MessageContains = "World" });

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void Query_ByTimeRange_ReturnsMatchingEntries()
    {
        using var provider = new RingBufferLogProvider();
        var baseTime = DateTime.Now;

        provider.Log(LogLevel.Info, "Test", "Message 1", null);
        Thread.Sleep(50);
        var afterFirst = DateTime.Now;
        provider.Log(LogLevel.Info, "Test", "Message 2", null);
        Thread.Sleep(50);
        var afterSecond = DateTime.Now;
        provider.Log(LogLevel.Info, "Test", "Message 3", null);

        var results = provider.Query(new LogQuery { After = afterFirst, Before = afterSecond });

        results.Count.ShouldBe(1);
        results[0].Message.ShouldBe("Message 2");
    }

    [Fact]
    public void Query_NewestFirst_ReturnsReverseOrder()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "First", null);
        provider.Log(LogLevel.Info, "Test", "Second", null);
        provider.Log(LogLevel.Info, "Test", "Third", null);

        var results = provider.Query(new LogQuery { NewestFirst = true });

        results[0].Message.ShouldBe("Third");
        results[1].Message.ShouldBe("Second");
        results[2].Message.ShouldBe("First");
    }

    [Fact]
    public void Query_OldestFirst_ReturnsChronologicalOrder()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "First", null);
        provider.Log(LogLevel.Info, "Test", "Second", null);
        provider.Log(LogLevel.Info, "Test", "Third", null);

        var results = provider.Query(new LogQuery { NewestFirst = false });

        results[0].Message.ShouldBe("First");
        results[1].Message.ShouldBe("Second");
        results[2].Message.ShouldBe("Third");
    }

    [Fact]
    public void Query_WithMaxResults_LimitsResults()
    {
        using var provider = new RingBufferLogProvider();
        for (int i = 0; i < 10; i++)
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        }

        var results = provider.Query(new LogQuery { MaxResults = 3 });

        results.Count.ShouldBe(3);
    }

    [Fact]
    public void Query_WithSkip_SkipsEntries()
    {
        using var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "Skip this", null);
        provider.Log(LogLevel.Info, "Test", "Skip this too", null);
        provider.Log(LogLevel.Info, "Test", "Keep this", null);

        var results = provider.Query(new LogQuery { Skip = 2, NewestFirst = false });

        results.Count.ShouldBe(1);
        results[0].Message.ShouldBe("Keep this");
    }

    [Fact]
    public void Query_WithPagination_ReturnsCorrectPage()
    {
        using var provider = new RingBufferLogProvider();
        for (int i = 0; i < 10; i++)
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        }

        // Get second page (skip 3, take 3), newest first
        var results = provider.Query(new LogQuery { Skip = 3, MaxResults = 3, NewestFirst = true });

        results.Count.ShouldBe(3);
        results[0].Message.ShouldBe("Message 6"); // 9,8,7 skipped, so 6,5,4
        results[1].Message.ShouldBe("Message 5");
        results[2].Message.ShouldBe("Message 4");
    }

    #endregion

    #region Thread Safety

    [Fact]
    public void Log_FromMultipleThreads_IsThreadSafe()
    {
        using var provider = new RingBufferLogProvider(1000);

        Parallel.For(0, 100, i =>
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        });

        provider.EntryCount.ShouldBe(100);
    }

    [Fact]
    public async Task Query_WhileLogging_DoesNotThrow()
    {
        using var provider = new RingBufferLogProvider();
        using var cts = new CancellationTokenSource();

        // Start background logging
        var loggingTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                provider.Log(LogLevel.Info, "Test", "Message", null);
                await Task.Delay(1, CancellationToken.None);
            }
        }, CancellationToken.None);

        // Query while logging
        for (int i = 0; i < 50; i++)
        {
            Should.NotThrow(() => provider.Query(new LogQuery { MinLevel = LogLevel.Info }));
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        await cts.CancelAsync();
        await loggingTask;
    }

    [Fact]
    public async Task GetStats_WhileLogging_DoesNotThrow()
    {
        using var provider = new RingBufferLogProvider();
        using var cts = new CancellationTokenSource();

        // Start background logging
        var loggingTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                provider.Log(LogLevel.Info, "Test", "Message", null);
                await Task.Delay(1, CancellationToken.None);
            }
        }, CancellationToken.None);

        // Get stats while logging
        for (int i = 0; i < 50; i++)
        {
            Should.NotThrow(() => provider.GetStats());
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        await cts.CancelAsync();
        await loggingTask;
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_ClearsEntries()
    {
        var provider = new RingBufferLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message", null);

        provider.Dispose();

        provider.EntryCount.ShouldBe(0);
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var provider = new RingBufferLogProvider();

        Should.NotThrow(() => provider.Flush());
    }

    #endregion
}
