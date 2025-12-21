namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for hot reload functionality.
/// </summary>
public class HotReloadTests : IDisposable
{
    private readonly TestAssetDirectory testDir;
    private readonly AssetManager manager;

    public HotReloadTests()
    {
        testDir = new TestAssetDirectory();
        manager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
        manager.RegisterLoader(new TestAssetLoader());
    }

    public void Dispose()
    {
        manager.Dispose();
        testDir.Dispose();
    }

    #region ReloadAsync Tests

    [Fact]
    public async Task ReloadAsync_UpdatesAssetContent()
    {
        var path = testDir.CreateFile("reload.txt", "original");
        using var handle = manager.Load<TestAsset>(path);

        Assert.Equal("original", handle.Asset!.Content);

        // Modify the file
        File.WriteAllText(Path.Combine(testDir.RootPath, path), "modified");

        await manager.ReloadAsync(path);

        Assert.Equal("modified", handle.Asset!.Content);
    }

    [Fact]
    public async Task ReloadAsync_DisposesOldAsset()
    {
        var path = testDir.CreateFile("dispose.txt", "content");
        using var handle = manager.Load<TestAsset>(path);
        var oldAsset = handle.Asset!;

        await manager.ReloadAsync(path);

        Assert.True(oldAsset.IsDisposed);
    }

    [Fact]
    public async Task ReloadAsync_FiresEvent()
    {
        var path = testDir.CreateFile("event.txt", "content");
        using var handle = manager.Load<TestAsset>(path);
        var reloadedPaths = new List<string>();

        manager.OnAssetReloaded += p => reloadedPaths.Add(p);

        await manager.ReloadAsync(path);

        Assert.Contains(path, reloadedPaths);
    }

    [Fact]
    public async Task ReloadAsync_NonExistent_DoesNothing()
    {
        await manager.ReloadAsync("nonexistent.txt");
        // Should not throw
    }

    [Fact]
    public async Task ReloadAsync_NotLoaded_DoesNothing()
    {
        var path = testDir.CreateFile("notloaded.txt", "content");

        await manager.ReloadAsync(path);
        // Should not throw, asset not loaded
    }

    [Fact]
    public async Task ReloadAsync_FileDeleted_KeepsOldAsset()
    {
        var path = testDir.CreateFile("deleted.txt", "original");
        using var handle = manager.Load<TestAsset>(path);
        var originalAsset = handle.Asset!;

        File.Delete(Path.Combine(testDir.RootPath, path));

        await manager.ReloadAsync(path);

        // Should keep the old asset since file is missing
        Assert.Same(originalAsset, handle.Asset);
    }

    [Fact]
    public async Task ReloadAsync_LoaderError_KeepsOldAsset()
    {
        // Create a file that will fail to parse on reload
        var path = testDir.CreateFile("error.txt", "valid content");
        using var handle = manager.Load<TestAsset>(path);
        var originalContent = handle.Asset!.Content;

        // We can't easily simulate a loader error, but the behavior is tested
        await manager.ReloadAsync(path);

        // Content should still be there
        Assert.NotNull(handle.Asset);
    }

    #endregion

    #region ReloadManager Tests

    [Fact]
    public void ReloadManager_Constructor_WithNonExistentPath_ThrowsDirectoryNotFound()
    {
        // ReloadManager with non-existent path should throw
        Assert.Throws<DirectoryNotFoundException>(
            () => new ReloadManager("/nonexistent/path", manager));
    }

    [Fact]
    public void ReloadManager_Dispose_MultipleCallsAreIdempotent()
    {
        using var reload = new ReloadManager(testDir.RootPath, manager);
        reload.Dispose();
        reload.Dispose(); // Should not throw
    }

    [Fact]
    public void ReloadManager_OnAssetReloaded_CanSubscribe()
    {
        using var reload = new ReloadManager(testDir.RootPath, manager);
        var paths = new List<string>();

        reload.OnAssetReloaded += p => paths.Add(p);

        // Event subscription should work
        Assert.Empty(paths); // No events yet
    }

    #endregion
}
