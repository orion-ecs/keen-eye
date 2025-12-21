namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for AssetsConfig configuration options.
/// </summary>
public class AssetsConfigTests
{
    #region Default Values Tests

    [Fact]
    public void Default_RootPath_IsAssets()
    {
        var config = AssetsConfig.Default;

        Assert.Equal("Assets", config.RootPath);
    }

    [Fact]
    public void Default_CachePolicy_IsLRU()
    {
        var config = AssetsConfig.Default;

        Assert.Equal(CachePolicy.LRU, config.CachePolicy);
    }

    [Fact]
    public void Default_MaxCacheBytes_Is512MB()
    {
        var config = AssetsConfig.Default;

        Assert.Equal(512 * 1024 * 1024, config.MaxCacheBytes);
    }

    [Fact]
    public void Default_MaxConcurrentLoads_Is4()
    {
        var config = AssetsConfig.Default;

        Assert.Equal(4, config.MaxConcurrentLoads);
    }

    [Fact]
    public void Default_EnableHotReload_IsFalse()
    {
        var config = AssetsConfig.Default;

        Assert.False(config.EnableHotReload);
    }

    [Fact]
    public void Default_DefaultPriority_IsNormal()
    {
        var config = AssetsConfig.Default;

        Assert.Equal(LoadPriority.Normal, config.DefaultPriority);
    }

    [Fact]
    public void Default_Services_IsNull()
    {
        var config = AssetsConfig.Default;

        Assert.Null(config.Services);
    }

    [Fact]
    public void Default_OnLoadError_IsNull()
    {
        var config = AssetsConfig.Default;

        Assert.Null(config.OnLoadError);
    }

    #endregion

    #region Development Preset Tests

    [Fact]
    public void Development_EnableHotReload_IsTrue()
    {
        var config = AssetsConfig.Development;

        Assert.True(config.EnableHotReload);
    }

    [Fact]
    public void Development_CachePolicy_IsAggressive()
    {
        var config = AssetsConfig.Development;

        Assert.Equal(CachePolicy.Aggressive, config.CachePolicy);
    }

    #endregion

    #region Custom Values Tests

    [Fact]
    public void WithInit_RootPath_SetsValue()
    {
        var config = new AssetsConfig { RootPath = "CustomPath" };

        Assert.Equal("CustomPath", config.RootPath);
    }

    [Fact]
    public void WithInit_MaxCacheBytes_SetsValue()
    {
        var config = new AssetsConfig { MaxCacheBytes = 1024 * 1024 * 1024 };

        Assert.Equal(1024 * 1024 * 1024, config.MaxCacheBytes);
    }

    [Fact]
    public void WithInit_CachePolicy_SetsValue()
    {
        var config = new AssetsConfig { CachePolicy = CachePolicy.Manual };

        Assert.Equal(CachePolicy.Manual, config.CachePolicy);
    }

    [Fact]
    public void WithInit_MaxConcurrentLoads_SetsValue()
    {
        var config = new AssetsConfig { MaxConcurrentLoads = 8 };

        Assert.Equal(8, config.MaxConcurrentLoads);
    }

    [Fact]
    public void WithInit_EnableHotReload_SetsValue()
    {
        var config = new AssetsConfig { EnableHotReload = true };

        Assert.True(config.EnableHotReload);
    }

    [Fact]
    public void WithInit_DefaultPriority_SetsValue()
    {
        var config = new AssetsConfig { DefaultPriority = LoadPriority.High };

        Assert.Equal(LoadPriority.High, config.DefaultPriority);
    }

    [Fact]
    public void WithInit_OnLoadError_SetsCallback()
    {
        var errors = new List<(string, Exception)>();
        var config = new AssetsConfig
        {
            OnLoadError = (path, ex) => errors.Add((path, ex))
        };

        Assert.NotNull(config.OnLoadError);
    }

    #endregion
}
