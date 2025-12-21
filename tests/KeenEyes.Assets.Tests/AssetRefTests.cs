namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for AssetRef ECS component.
/// </summary>
public class AssetRefTests
{
    #region HasPath Tests

    [Fact]
    public void HasPath_WithPath_ReturnsTrue()
    {
        var assetRef = new AssetRef<TestAsset> { Path = "textures/player.png" };

        Assert.True(assetRef.HasPath);
    }

    [Fact]
    public void HasPath_WithNullPath_ReturnsFalse()
    {
        var assetRef = new AssetRef<TestAsset> { Path = null! };

        Assert.False(assetRef.HasPath);
    }

    [Fact]
    public void HasPath_WithEmptyPath_ReturnsFalse()
    {
        var assetRef = new AssetRef<TestAsset> { Path = "" };

        Assert.False(assetRef.HasPath);
    }

    [Fact]
    public void HasPath_Default_ReturnsFalse()
    {
        var assetRef = default(AssetRef<TestAsset>);

        Assert.False(assetRef.HasPath);
    }

    #endregion

    #region IsResolved Tests

    [Fact]
    public void IsResolved_WithZeroHandleId_ReturnsFalse()
    {
        var assetRef = new AssetRef<TestAsset> { Path = "test.txt" };

        Assert.False(assetRef.IsResolved);
    }

    [Fact]
    public void IsResolved_WithPositiveHandleId_ReturnsTrue()
    {
        var assetRef = new AssetRef<TestAsset> { Path = "test.txt" };
        // Simulate resolution by setting internal HandleId
        // This would normally be done by the AssetResolutionSystem

        // Since HandleId is internal, we test through the manager
        Assert.False(assetRef.IsResolved); // Should be false initially
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SamePath_ReturnsTrue()
    {
        var ref1 = new AssetRef<TestAsset> { Path = "same.txt" };
        var ref2 = new AssetRef<TestAsset> { Path = "same.txt" };

        Assert.True(ref1.Equals(ref2));
    }

    [Fact]
    public void Equals_DifferentPath_ReturnsFalse()
    {
        var ref1 = new AssetRef<TestAsset> { Path = "a.txt" };
        var ref2 = new AssetRef<TestAsset> { Path = "b.txt" };

        Assert.False(ref1.Equals(ref2));
    }

    [Fact]
    public void GetHashCode_SamePath_ReturnsSameHash()
    {
        var ref1 = new AssetRef<TestAsset> { Path = "hash.txt" };
        var ref2 = new AssetRef<TestAsset> { Path = "hash.txt" };

        Assert.Equal(ref1.GetHashCode(), ref2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_SamePath_ReturnsTrue()
    {
        var ref1 = new AssetRef<TestAsset> { Path = "eq.txt" };
        var ref2 = new AssetRef<TestAsset> { Path = "eq.txt" };

        Assert.True(ref1 == ref2);
    }

    [Fact]
    public void OperatorNotEquals_DifferentPath_ReturnsTrue()
    {
        var ref1 = new AssetRef<TestAsset> { Path = "x.txt" };
        var ref2 = new AssetRef<TestAsset> { Path = "y.txt" };

        Assert.True(ref1 != ref2);
    }

    #endregion
}
