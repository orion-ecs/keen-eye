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
}
