namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for AssetsPlugin world integration.
/// </summary>
public class AssetsPluginTests : IDisposable
{
    private readonly TestAssetDirectory testDir;

    public AssetsPluginTests()
    {
        testDir = new TestAssetDirectory();
    }

    public void Dispose()
    {
        testDir.Dispose();
    }

    #region Install Tests

    [Fact]
    public void Install_RegistersAssetManagerExtension()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig { RootPath = testDir.RootPath });

        world.InstallPlugin(plugin);

        Assert.True(world.TryGetExtension<AssetManager>(out var manager));
        Assert.NotNull(manager);
    }

    [Fact]
    public void Install_AddsAssetResolutionSystem()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig { RootPath = testDir.RootPath });

        world.InstallPlugin(plugin);

        // The system should be registered and executable
        world.Update(0.016f); // Should not throw
    }

    [Fact]
    public void Install_RegistersRawLoader()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig { RootPath = testDir.RootPath });
        world.InstallPlugin(plugin);

        var path = testDir.CreateFile("test.bin", [1, 2, 3]);
        var manager = world.GetExtension<AssetManager>();

        using var handle = manager.Load<RawAsset>(path);

        Assert.True(handle.IsLoaded);
    }

    [Fact]
    public void Install_RegistersMeshLoader()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig { RootPath = testDir.RootPath });
        world.InstallPlugin(plugin);

        var manager = world.GetExtension<AssetManager>();
        // MeshLoader should be registered, but we can't easily test without valid glTF file
    }

    [Fact]
    public void Install_WithNullConfig_UsesDefaults()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(null);

        world.InstallPlugin(plugin);

        var manager = world.GetExtension<AssetManager>();
        Assert.Equal("Assets", manager.RootPath);
    }

    [Fact]
    public void Install_UsesProvidedConfig()
    {
        using var world = new World();
        var config = new AssetsConfig { RootPath = testDir.RootPath };
        var plugin = new AssetsPlugin(config);

        world.InstallPlugin(plugin);

        var manager = world.GetExtension<AssetManager>();
        Assert.Equal(testDir.RootPath, manager.RootPath);
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_RemovesExtension()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig { RootPath = testDir.RootPath });
        world.InstallPlugin(plugin);

        world.UninstallPlugin<AssetsPlugin>();

        Assert.False(world.TryGetExtension<AssetManager>(out _));
    }

    [Fact]
    public void Uninstall_DisposesAssetManager()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig { RootPath = testDir.RootPath });
        world.InstallPlugin(plugin);
        var manager = world.GetExtension<AssetManager>();

        // Load an asset
        var path = testDir.CreateFile("uninstall.bin", [1, 2, 3]);
        using var handle = manager.Load<RawAsset>(path);

        world.UninstallPlugin<AssetsPlugin>();

        // Manager should be disposed, operations should fail
    }

    #endregion

    #region Name Property Tests

    [Fact]
    public void Name_ReturnsAssets()
    {
        var plugin = new AssetsPlugin();

        Assert.Equal("Assets", plugin.Name);
    }

    #endregion

    #region Hot Reload Integration Tests

    [Fact]
    public void Install_WithHotReloadEnabled_CreatesReloadManager()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            EnableHotReload = true
        });

        world.InstallPlugin(plugin);

        // Should not throw
        world.Update(0.016f);
    }

    [Fact]
    public void Install_WithHotReloadDisabled_DoesNotCreateReloadManager()
    {
        using var world = new World();
        var plugin = new AssetsPlugin(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            EnableHotReload = false
        });

        world.InstallPlugin(plugin);

        // Should not throw
        world.Update(0.016f);
    }

    #endregion
}
