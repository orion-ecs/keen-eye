namespace KeenEyes.Localization.Tests;

public class LocalizedAssetTests
{
    #region Create Tests

    [Fact]
    public void Create_SetsAssetKey()
    {
        var asset = LocalizedAsset.Create("textures/logo");

        asset.AssetKey.ShouldBe("textures/logo");
    }

    [Fact]
    public void Create_ResolvedPathIsNull()
    {
        var asset = LocalizedAsset.Create("textures/logo");

        asset.ResolvedPath.ShouldBeNull();
    }

    [Fact]
    public void Create_IsResolvedIsFalse()
    {
        var asset = LocalizedAsset.Create("textures/logo");

        asset.IsResolved.ShouldBeFalse();
    }

    #endregion

    #region IsResolved Tests

    [Fact]
    public void IsResolved_WithResolvedPath_ReturnsTrue()
    {
        var asset = new LocalizedAsset
        {
            AssetKey = "textures/logo",
            ResolvedPath = "textures/logo.en-US.png"
        };

        asset.IsResolved.ShouldBeTrue();
    }

    [Fact]
    public void IsResolved_WithEmptyResolvedPath_ReturnsFalse()
    {
        var asset = new LocalizedAsset
        {
            AssetKey = "textures/logo",
            ResolvedPath = ""
        };

        asset.IsResolved.ShouldBeFalse();
    }

    [Fact]
    public void IsResolved_WithNullResolvedPath_ReturnsFalse()
    {
        var asset = new LocalizedAsset
        {
            AssetKey = "textures/logo",
            ResolvedPath = null
        };

        asset.IsResolved.ShouldBeFalse();
    }

    #endregion

    #region Invalidate Tests

    [Fact]
    public void Invalidate_ClearsResolvedPath()
    {
        var asset = new LocalizedAsset
        {
            AssetKey = "textures/logo",
            ResolvedPath = "textures/logo.en-US.png"
        };

        asset.Invalidate();

        asset.ResolvedPath.ShouldBeNull();
    }

    [Fact]
    public void Invalidate_PreservesAssetKey()
    {
        var asset = new LocalizedAsset
        {
            AssetKey = "textures/logo",
            ResolvedPath = "textures/logo.en-US.png"
        };

        asset.Invalidate();

        asset.AssetKey.ShouldBe("textures/logo");
    }

    [Fact]
    public void Invalidate_SetsIsResolvedToFalse()
    {
        var asset = new LocalizedAsset
        {
            AssetKey = "textures/logo",
            ResolvedPath = "textures/logo.en-US.png"
        };
        asset.IsResolved.ShouldBeTrue();

        asset.Invalidate();

        asset.IsResolved.ShouldBeFalse();
    }

    #endregion
}
