namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for AssetResolutionSystem automatic asset loading.
/// </summary>
public class AssetResolutionSystemTests : IDisposable
{
    private readonly TestAssetDirectory testDir;
    private readonly World world;
    private readonly AssetManager assetManager;
    private readonly AssetResolutionSystem system;

    public AssetResolutionSystemTests()
    {
        testDir = new TestAssetDirectory();
        world = new World();

        assetManager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
        assetManager.RegisterLoader(new TestAssetLoader());
        world.SetExtension(assetManager);

        system = new AssetResolutionSystem();
        system.Initialize(world);
    }

    public void Dispose()
    {
        system.Dispose();
        world.Dispose();
        assetManager.Dispose();
        testDir.Dispose();
    }

    #region Initialize Tests

    [Fact]
    public void Initialize_WithAssetManager_EnablesSystem()
    {
        Assert.True(system.Enabled);
    }

    [Fact]
    public void Initialize_WithoutAssetManager_DisablesSystem()
    {
        using var worldWithoutAssets = new World();
        var sys = new AssetResolutionSystem();
        sys.Initialize(worldWithoutAssets);

        Assert.False(sys.Enabled);

        sys.Dispose();
    }

    #endregion

    #region Enabled Property Tests

    [Fact]
    public void Enabled_CanBeSetToFalse()
    {
        system.Enabled = false;

        Assert.False(system.Enabled);
    }

    [Fact]
    public void Enabled_WhenFalse_UpdateDoesNothing()
    {
        var path = testDir.CreateFile("disabled.txt", "content");
        var entity = world.Spawn()
            .With(new AssetRef<TestAsset> { Path = path })
            .Build();

        system.Enabled = false;
        system.Update(0.016f);

        // Asset should not be loaded
        Assert.False(assetManager.IsLoaded(path));
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithUnresolvedAssetRef_DoesNotThrow()
    {
        var path = testDir.CreateFile("resolve.txt", "content");
        world.Spawn()
            .With(new AssetRef<TestAsset> { Path = path })
            .Build();

        // Run update - should not throw
        system.Update(0.016f);
    }

    [Fact]
    public void Update_WithAlreadyLoadedAsset_DoesNotThrow()
    {
        var path = testDir.CreateFile("loaded.txt", "content");
        using var preload = assetManager.Load<TestAsset>(path);

        world.Spawn()
            .With(new AssetRef<TestAsset> { Path = path })
            .Build();

        // Update should not throw
        system.Update(0.016f);
    }

    [Fact]
    public void Update_WithNoPath_DoesNothing()
    {
        var entity = world.Spawn()
            .With(new AssetRef<TestAsset> { Path = null! })
            .Build();

        system.Update(0.016f);

        ref var assetRef = ref world.Get<AssetRef<TestAsset>>(entity);
        Assert.False(assetRef.IsResolved);
    }

    [Fact]
    public void Update_WithEmptyPath_DoesNothing()
    {
        var entity = world.Spawn()
            .With(new AssetRef<TestAsset> { Path = "" })
            .Build();

        system.Update(0.016f);

        ref var assetRef = ref world.Get<AssetRef<TestAsset>>(entity);
        Assert.False(assetRef.IsResolved);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WaitsForPendingLoads()
    {
        assetManager.RegisterLoader(new SlowLoader(100));
        var path = testDir.CreateFile("pending.slow", "content");
        var entity = world.Spawn()
            .With(new AssetRef<TestAsset> { Path = path })
            .Build();

        system.Update(0.016f);
        system.Dispose(); // Should wait for pending loads

        // No exception should be thrown
    }

    [Fact]
    public void Dispose_MultipleCallsAreIdempotent()
    {
        system.Dispose();
        system.Dispose(); // Should not throw
    }

    #endregion

    #region Async Loading Tests

    [Fact]
    public void Update_WithNullWorld_DoesNothing()
    {
        var sys = new AssetResolutionSystem();
        // Don't call Initialize - world will be null

        sys.Update(0.016f); // Should not throw

        sys.Dispose();
    }

    #endregion

    #region RawAsset Resolution Tests

    [Fact]
    public async Task Update_WithRawAssetRef_StartsAsyncLoad()
    {
        assetManager.RegisterLoader(new RawLoader());
        var path = testDir.CreateFile("data.bin", new byte[] { 1, 2, 3 });

        world.Spawn()
            .With(new AssetRef<RawAsset> { Path = path })
            .Build();

        // First update starts async load
        system.Update(0.016f);

        // Wait for async load to complete
        await Task.Delay(100);

        // Second update processes completed loads
        system.Update(0.016f);

        Assert.True(assetManager.IsLoaded(path));
    }

    [Fact]
    public void Update_RawAssetPendingLoad_SkipsSecondLoad()
    {
        assetManager.RegisterLoader(new RawLoader());
        var path = testDir.CreateFile("pending.bin", new byte[] { 1, 2, 3 });

        world.Spawn()
            .With(new AssetRef<RawAsset> { Path = path })
            .Build();

        // First update starts the load
        system.Update(0.016f);

        // Second update should skip since load is pending
        system.Update(0.016f); // Should not throw or start another load
    }

    [Fact]
    public void Update_RawAssetAlreadyLoaded_ResolvesImmediately()
    {
        assetManager.RegisterLoader(new RawLoader());
        var path = testDir.CreateFile("preloaded.bin", new byte[] { 1, 2, 3 });

        // Pre-load the asset
        using var preloaded = assetManager.Load<RawAsset>(path);

        var entity = world.Spawn()
            .With(new AssetRef<RawAsset> { Path = path })
            .Build();

        // Update should resolve immediately since asset is loaded
        system.Update(0.016f);

        ref var assetRef = ref world.Get<AssetRef<RawAsset>>(entity);
        Assert.True(assetRef.IsResolved);
    }

    [Fact]
    public void Update_RawAssetAlreadyResolved_SkipsResolution()
    {
        assetManager.RegisterLoader(new RawLoader());
        var path = testDir.CreateFile("resolved.bin", new byte[] { 1, 2, 3 });

        // Pre-load the asset
        using var preloaded = assetManager.Load<RawAsset>(path);

        var entity = world.Spawn()
            .With(new AssetRef<RawAsset> { Path = path, HandleId = preloaded.Id })
            .Build();

        // Update should skip already resolved refs
        system.Update(0.016f);

        // Asset should still be loaded (no duplicate load)
        Assert.True(assetManager.IsLoaded(path));
    }

    #endregion
}
