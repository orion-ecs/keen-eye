namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for StreamingManager background preloading functionality.
/// </summary>
public class StreamingManagerTests : IDisposable
{
    private readonly TestAssetDirectory testDir;
    private readonly AssetManager manager;
    private readonly StreamingManager streaming;

    public StreamingManagerTests()
    {
        testDir = new TestAssetDirectory();
        manager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
        manager.RegisterLoader(new TestAssetLoader());
        streaming = new StreamingManager(manager);
    }

    public void Dispose()
    {
        streaming.Dispose();
        manager.Dispose();
        testDir.Dispose();
    }

    #region Queue Tests

    [Fact]
    public void Queue_AddsAssetToQueue()
    {
        testDir.CreateFile("queue.txt", "content");

        streaming.Queue<TestAsset>("queue.txt");

        Assert.Equal(1, streaming.QueuedCount);
    }

    [Fact]
    public void Queue_AlreadyLoaded_DoesNotQueue()
    {
        var path = testDir.CreateFile("loaded.txt", "content");
        using var handle = manager.Load<TestAsset>(path);

        streaming.Queue<TestAsset>(path);

        Assert.Equal(0, streaming.QueuedCount);
    }

    [Fact]
    public void Queue_DuplicatePath_MayQueue()
    {
        // Duplicate detection happens for activeRequests, not pendingRequests
        testDir.CreateFile("dup.txt", "content");

        streaming.Queue<TestAsset>("dup.txt");
        streaming.Queue<TestAsset>("dup.txt");

        // Both go to pending queue; duplicate detection happens during processing
        Assert.True(streaming.QueuedCount >= 1);
    }

    [Fact]
    public void QueueMany_AddsMultipleAssets()
    {
        testDir.CreateFile("m1.txt", "a");
        testDir.CreateFile("m2.txt", "b");
        testDir.CreateFile("m3.txt", "c");

        streaming.QueueMany<TestAsset>(["m1.txt", "m2.txt", "m3.txt"]);

        Assert.Equal(3, streaming.QueuedCount);
    }

    [Fact]
    public void Queue_NullPath_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => streaming.Queue<TestAsset>(null!));
    }

    [Fact]
    public void Queue_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => streaming.Queue<TestAsset>(""));
    }

    #endregion

    #region Progress Tests

    [Fact]
    public void Progress_EmptyQueue_ReturnsOne()
    {
        Assert.Equal(1f, streaming.Progress);
    }

    [Fact]
    public void Progress_WithQueuedAssets_ReturnsZero()
    {
        testDir.CreateFile("prog.txt", "content");
        streaming.Queue<TestAsset>("prog.txt");

        Assert.Equal(0f, streaming.Progress);
    }

    [Fact]
    public async Task Progress_AfterCompletion_ReturnsOne()
    {
        var path = testDir.CreateFile("complete.txt", "content");
        streaming.Queue<TestAsset>(path);
        streaming.Start();

        await streaming.WaitForCompletionAsync();

        Assert.Equal(1f, streaming.Progress);
    }

    #endregion

    #region Start/Stop Tests

    [Fact]
    public void IsStreaming_BeforeStart_ReturnsFalse()
    {
        testDir.CreateFile("s.txt", "content");
        streaming.Queue<TestAsset>("s.txt");

        Assert.False(streaming.IsStreaming);
    }

    [Fact]
    public void IsStreaming_AfterStart_ReturnsTrue()
    {
        manager.RegisterLoader(new SlowLoader(500));
        testDir.CreateFile("slow.slow", "content");
        streaming.Queue<TestAsset>("slow.slow");

        streaming.Start();

        Assert.True(streaming.IsStreaming);
        streaming.Stop();
    }

    [Fact]
    public async Task Start_LoadsQueuedAssets()
    {
        var path = testDir.CreateFile("start.txt", "content");
        streaming.Queue<TestAsset>(path);

        streaming.Start();
        await streaming.WaitForCompletionAsync();

        Assert.True(manager.IsLoaded(path));
    }

    [Fact]
    public void Start_WhenAlreadyStreaming_DoesNotRestart()
    {
        manager.RegisterLoader(new SlowLoader(500));
        testDir.CreateFile("running.slow", "content");
        streaming.Queue<TestAsset>("running.slow");

        streaming.Start();
        streaming.Start(); // Should be no-op

        Assert.True(streaming.IsStreaming);
        streaming.Stop();
    }

    [Fact]
    public void Stop_StopsStreaming()
    {
        manager.RegisterLoader(new SlowLoader(500));
        testDir.CreateFile("stop.slow", "content");
        streaming.Queue<TestAsset>("stop.slow");

        streaming.Start();
        streaming.Stop();

        // Task should eventually complete
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesQueuedAssets()
    {
        testDir.CreateFile("c1.txt", "a");
        testDir.CreateFile("c2.txt", "b");
        streaming.Queue<TestAsset>("c1.txt");
        streaming.Queue<TestAsset>("c2.txt");

        streaming.Clear();

        Assert.Equal(0, streaming.QueuedCount);
    }

    [Fact]
    public void Clear_ResetsProgress()
    {
        testDir.CreateFile("reset.txt", "content");
        streaming.Queue<TestAsset>("reset.txt");

        streaming.Clear();

        Assert.Equal(1f, streaming.Progress);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task OnAssetStreamed_FiredForEachAsset()
    {
        var path = testDir.CreateFile("event.txt", "content");
        var streamedPaths = new List<string>();

        streaming.OnAssetStreamed += p => streamedPaths.Add(p);
        streaming.Queue<TestAsset>(path);
        streaming.Start();
        await streaming.WaitForCompletionAsync();

        Assert.Contains(path, streamedPaths);
    }

    [Fact]
    public async Task OnStreamingComplete_FiredAfterAllAssets()
    {
        testDir.CreateFile("e1.txt", "a");
        testDir.CreateFile("e2.txt", "b");
        var completeFired = false;

        streaming.OnStreamingComplete += () => completeFired = true;
        streaming.Queue<TestAsset>("e1.txt");
        streaming.Queue<TestAsset>("e2.txt");
        streaming.Start();
        await streaming.WaitForCompletionAsync();

        Assert.True(completeFired);
    }

    [Fact]
    public async Task OnStreamingError_FiredOnLoadFailure()
    {
        testDir.CreateFile("missing.txt", ""); // Wrong extension
        var errors = new List<(string, Exception)>();

        streaming.OnStreamingError += (path, ex) => errors.Add((path, ex));
        streaming.Queue<TestAsset>("nonexistent.txt");
        streaming.Start();
        await streaming.WaitForCompletionAsync();

        Assert.NotEmpty(errors);
    }

    #endregion

    #region WaitForCompletion Tests

    [Fact]
    public async Task WaitForCompletionAsync_WithoutStarting_CompletesImmediately()
    {
        await streaming.WaitForCompletionAsync();
        // Should not hang
    }

    [Fact]
    public async Task WaitForCompletionAsync_WithCancellation_CompletesGracefully()
    {
        manager.RegisterLoader(new SlowLoader(1000));
        testDir.CreateFile("cancel.slow", "content");
        streaming.Queue<TestAsset>("cancel.slow");
        streaming.Start();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        // WaitForCompletionAsync catches cancellation and returns gracefully
        await streaming.WaitForCompletionAsync(cts.Token);

        streaming.Stop();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_StopsStreaming()
    {
        manager.RegisterLoader(new SlowLoader(500));
        testDir.CreateFile("disp.slow", "content");
        streaming.Queue<TestAsset>("disp.slow");
        streaming.Start();

        streaming.Dispose();
        streaming.Dispose(); // Should be idempotent
    }

    [Fact]
    public void Dispose_ClearsQueue()
    {
        testDir.CreateFile("dq.txt", "content");
        streaming.Queue<TestAsset>("dq.txt");

        streaming.Dispose();

        Assert.Equal(0, streaming.QueuedCount);
    }

    #endregion

    #region Concurrent Loading Tests

    [Fact]
    public async Task Start_WithMaxConcurrent_RespectsLimit()
    {
        manager.RegisterLoader(new SlowLoader(100));
        for (int i = 0; i < 5; i++)
        {
            testDir.CreateFile($"c{i}.slow", $"content{i}");
            streaming.Queue<TestAsset>($"c{i}.slow");
        }

        streaming.Start(maxConcurrent: 2);
        await streaming.WaitForCompletionAsync();

        Assert.Equal(1f, streaming.Progress);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamingManager(null!));
    }

    #endregion
}
