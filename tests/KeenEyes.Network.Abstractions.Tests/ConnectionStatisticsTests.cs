using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for ConnectionStatistics struct.
/// </summary>
public class ConnectionStatisticsTests
{
    [Fact]
    public void ConnectionStatistics_RequiredProperties_MustBeSet()
    {
        var stats = new ConnectionStatistics
        {
            RoundTripTimeMs = 50f,
            PacketLossPercent = 0.5f,
            BytesSent = 1024,
            BytesReceived = 2048,
            PacketsSent = 100,
            PacketsReceived = 200,
            PacketsLost = 5
        };

        Assert.Equal(50f, stats.RoundTripTimeMs);
        Assert.Equal(0.5f, stats.PacketLossPercent);
        Assert.Equal(1024, stats.BytesSent);
        Assert.Equal(2048, stats.BytesReceived);
        Assert.Equal(100, stats.PacketsSent);
        Assert.Equal(200, stats.PacketsReceived);
        Assert.Equal(5, stats.PacketsLost);
    }

    [Fact]
    public void ConnectionStatistics_ZeroValues_AreValid()
    {
        var stats = new ConnectionStatistics
        {
            RoundTripTimeMs = 0f,
            PacketLossPercent = 0f,
            BytesSent = 0,
            BytesReceived = 0,
            PacketsSent = 0,
            PacketsReceived = 0,
            PacketsLost = 0
        };

        Assert.Equal(0f, stats.RoundTripTimeMs);
        Assert.Equal(0f, stats.PacketLossPercent);
        Assert.Equal(0, stats.BytesSent);
        Assert.Equal(0, stats.BytesReceived);
        Assert.Equal(0, stats.PacketsSent);
        Assert.Equal(0, stats.PacketsReceived);
        Assert.Equal(0, stats.PacketsLost);
    }

    [Fact]
    public void ConnectionStatistics_LargeValues_AreValid()
    {
        var stats = new ConnectionStatistics
        {
            RoundTripTimeMs = 1000f,
            PacketLossPercent = 100f,
            BytesSent = long.MaxValue,
            BytesReceived = long.MaxValue,
            PacketsSent = long.MaxValue,
            PacketsReceived = long.MaxValue,
            PacketsLost = long.MaxValue
        };

        Assert.Equal(1000f, stats.RoundTripTimeMs);
        Assert.Equal(100f, stats.PacketLossPercent);
        Assert.Equal(long.MaxValue, stats.BytesSent);
        Assert.Equal(long.MaxValue, stats.BytesReceived);
        Assert.Equal(long.MaxValue, stats.PacketsSent);
        Assert.Equal(long.MaxValue, stats.PacketsReceived);
        Assert.Equal(long.MaxValue, stats.PacketsLost);
    }

    [Fact]
    public void ConnectionStatistics_Equality_SameValues_AreEqual()
    {
        var stats1 = new ConnectionStatistics
        {
            RoundTripTimeMs = 50f,
            PacketLossPercent = 1f,
            BytesSent = 1000,
            BytesReceived = 2000,
            PacketsSent = 10,
            PacketsReceived = 20,
            PacketsLost = 1
        };

        var stats2 = new ConnectionStatistics
        {
            RoundTripTimeMs = 50f,
            PacketLossPercent = 1f,
            BytesSent = 1000,
            BytesReceived = 2000,
            PacketsSent = 10,
            PacketsReceived = 20,
            PacketsLost = 1
        };

        Assert.Equal(stats1, stats2);
        Assert.True(stats1 == stats2);
    }

    [Fact]
    public void ConnectionStatistics_Equality_DifferentValues_AreNotEqual()
    {
        var stats1 = new ConnectionStatistics
        {
            RoundTripTimeMs = 50f,
            PacketLossPercent = 1f,
            BytesSent = 1000,
            BytesReceived = 2000,
            PacketsSent = 10,
            PacketsReceived = 20,
            PacketsLost = 1
        };

        var stats2 = new ConnectionStatistics
        {
            RoundTripTimeMs = 100f, // Different
            PacketLossPercent = 1f,
            BytesSent = 1000,
            BytesReceived = 2000,
            PacketsSent = 10,
            PacketsReceived = 20,
            PacketsLost = 1
        };

        Assert.NotEqual(stats1, stats2);
        Assert.True(stats1 != stats2);
    }

    [Fact]
    public void ConnectionStatistics_GetHashCode_SameValues_SameHashCode()
    {
        var stats1 = new ConnectionStatistics
        {
            RoundTripTimeMs = 50f,
            PacketLossPercent = 1f,
            BytesSent = 1000,
            BytesReceived = 2000,
            PacketsSent = 10,
            PacketsReceived = 20,
            PacketsLost = 1
        };

        var stats2 = new ConnectionStatistics
        {
            RoundTripTimeMs = 50f,
            PacketLossPercent = 1f,
            BytesSent = 1000,
            BytesReceived = 2000,
            PacketsSent = 10,
            PacketsReceived = 20,
            PacketsLost = 1
        };

        Assert.Equal(stats1.GetHashCode(), stats2.GetHashCode());
    }

    [Fact]
    public void ConnectionStatistics_ToString_ContainsRelevantInfo()
    {
        var stats = new ConnectionStatistics
        {
            RoundTripTimeMs = 50f,
            PacketLossPercent = 1.5f,
            BytesSent = 1024,
            BytesReceived = 2048,
            PacketsSent = 100,
            PacketsReceived = 200,
            PacketsLost = 5
        };

        var str = stats.ToString();

        // Record structs auto-generate ToString with property values
        Assert.Contains("RoundTripTimeMs", str);
        Assert.Contains("50", str);
        Assert.Contains("PacketLossPercent", str);
        Assert.Contains("BytesSent", str);
        Assert.Contains("1024", str);
    }
}
