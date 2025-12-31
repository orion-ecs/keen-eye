namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for CacheStats struct.
/// </summary>
public class CacheStatsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_AllParameters_SetsPropertiesCorrectly()
    {
        var stats = new CacheStats(
            TotalAssets: 100,
            LoadedAssets: 50,
            PendingAssets: 10,
            FailedAssets: 5,
            TotalSizeBytes: 1024 * 1024,
            MaxSizeBytes: 10 * 1024 * 1024,
            CacheHits: 500,
            CacheMisses: 100);

        Assert.Equal(100, stats.TotalAssets);
        Assert.Equal(50, stats.LoadedAssets);
        Assert.Equal(10, stats.PendingAssets);
        Assert.Equal(5, stats.FailedAssets);
        Assert.Equal(1024 * 1024, stats.TotalSizeBytes);
        Assert.Equal(10 * 1024 * 1024, stats.MaxSizeBytes);
        Assert.Equal(500, stats.CacheHits);
        Assert.Equal(100, stats.CacheMisses);
    }

    [Fact]
    public void Constructor_ZeroValues_AreValid()
    {
        var stats = new CacheStats(
            TotalAssets: 0,
            LoadedAssets: 0,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 0,
            MaxSizeBytes: 0,
            CacheHits: 0,
            CacheMisses: 0);

        Assert.Equal(0, stats.TotalAssets);
        Assert.Equal(0, stats.LoadedAssets);
        Assert.Equal(0, stats.TotalSizeBytes);
        Assert.Equal(0, stats.CacheHits);
    }

    #endregion

    #region HitRatio Tests

    [Fact]
    public void HitRatio_NoHitsOrMisses_ReturnsZero()
    {
        var stats = new CacheStats(
            TotalAssets: 0,
            LoadedAssets: 0,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 0,
            MaxSizeBytes: 0,
            CacheHits: 0,
            CacheMisses: 0);

        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public void HitRatio_OnlyHits_ReturnsOne()
    {
        var stats = new CacheStats(
            TotalAssets: 10,
            LoadedAssets: 10,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 1000,
            MaxSizeBytes: 10000,
            CacheHits: 100,
            CacheMisses: 0);

        Assert.Equal(1.0, stats.HitRatio);
    }

    [Fact]
    public void HitRatio_OnlyMisses_ReturnsZero()
    {
        var stats = new CacheStats(
            TotalAssets: 10,
            LoadedAssets: 10,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 1000,
            MaxSizeBytes: 10000,
            CacheHits: 0,
            CacheMisses: 100);

        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public void HitRatio_FiftyPercent_ReturnsPointFive()
    {
        var stats = new CacheStats(
            TotalAssets: 10,
            LoadedAssets: 10,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 1000,
            MaxSizeBytes: 10000,
            CacheHits: 50,
            CacheMisses: 50);

        Assert.Equal(0.5, stats.HitRatio, 0.001);
    }

    [Fact]
    public void HitRatio_SeventyFivePercent_ReturnsCorrectValue()
    {
        var stats = new CacheStats(
            TotalAssets: 10,
            LoadedAssets: 10,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 1000,
            MaxSizeBytes: 10000,
            CacheHits: 75,
            CacheMisses: 25);

        Assert.Equal(0.75, stats.HitRatio, 0.001);
    }

    #endregion

    #region UtilizationRatio Tests

    [Fact]
    public void UtilizationRatio_ZeroMaxSize_ReturnsZero()
    {
        var stats = new CacheStats(
            TotalAssets: 10,
            LoadedAssets: 10,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 1000,
            MaxSizeBytes: 0,
            CacheHits: 100,
            CacheMisses: 0);

        Assert.Equal(0.0, stats.UtilizationRatio);
    }

    [Fact]
    public void UtilizationRatio_EmptyCache_ReturnsZero()
    {
        var stats = new CacheStats(
            TotalAssets: 0,
            LoadedAssets: 0,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 0,
            MaxSizeBytes: 10000,
            CacheHits: 0,
            CacheMisses: 0);

        Assert.Equal(0.0, stats.UtilizationRatio);
    }

    [Fact]
    public void UtilizationRatio_FullCache_ReturnsOne()
    {
        var stats = new CacheStats(
            TotalAssets: 10,
            LoadedAssets: 10,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 10000,
            MaxSizeBytes: 10000,
            CacheHits: 100,
            CacheMisses: 0);

        Assert.Equal(1.0, stats.UtilizationRatio);
    }

    [Fact]
    public void UtilizationRatio_HalfFull_ReturnsPointFive()
    {
        var stats = new CacheStats(
            TotalAssets: 5,
            LoadedAssets: 5,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 5000,
            MaxSizeBytes: 10000,
            CacheHits: 50,
            CacheMisses: 0);

        Assert.Equal(0.5, stats.UtilizationRatio, 0.001);
    }

    [Fact]
    public void UtilizationRatio_TenPercent_ReturnsCorrectValue()
    {
        var stats = new CacheStats(
            TotalAssets: 1,
            LoadedAssets: 1,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 1024,
            MaxSizeBytes: 10240,
            CacheHits: 10,
            CacheMisses: 0);

        Assert.Equal(0.1, stats.UtilizationRatio, 0.001);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var stats1 = new CacheStats(10, 5, 2, 1, 1000, 10000, 100, 10);
        var stats2 = new CacheStats(10, 5, 2, 1, 1000, 10000, 100, 10);

        Assert.Equal(stats1, stats2);
        Assert.True(stats1 == stats2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var stats1 = new CacheStats(10, 5, 2, 1, 1000, 10000, 100, 10);
        var stats2 = new CacheStats(10, 5, 2, 1, 1000, 10000, 200, 10); // Different CacheHits

        Assert.NotEqual(stats1, stats2);
        Assert.True(stats1 != stats2);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHashCode()
    {
        var stats1 = new CacheStats(10, 5, 2, 1, 1000, 10000, 100, 10);
        var stats2 = new CacheStats(10, 5, 2, 1, 1000, 10000, 100, 10);

        Assert.Equal(stats1.GetHashCode(), stats2.GetHashCode());
    }

    #endregion
}
