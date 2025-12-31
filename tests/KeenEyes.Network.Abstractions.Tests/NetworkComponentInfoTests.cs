using KeenEyes.Network;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="NetworkComponentInfo"/> struct.
/// </summary>
public class NetworkComponentInfoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var info = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 42,
            Strategy = SyncStrategy.Interpolated,
            Frequency = 30,
            Priority = 200,
            SupportsInterpolation = true,
            SupportsPrediction = true,
            SupportsDelta = true
        };

        Assert.Equal(typeof(TestComponent), info.Type);
        Assert.Equal(42, info.NetworkTypeId);
        Assert.Equal(SyncStrategy.Interpolated, info.Strategy);
        Assert.Equal(30, info.Frequency);
        Assert.Equal(200, info.Priority);
        Assert.True(info.SupportsInterpolation);
        Assert.True(info.SupportsPrediction);
        Assert.True(info.SupportsDelta);
    }

    [Fact]
    public void Constructor_WithMinimalValues_SetsDefaults()
    {
        var info = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 1,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = false,
            SupportsPrediction = false,
            SupportsDelta = false
        };

        Assert.Equal(typeof(TestComponent), info.Type);
        Assert.Equal(1, info.NetworkTypeId);
        Assert.Equal(SyncStrategy.Authoritative, info.Strategy);
        Assert.Equal(0, info.Frequency);
        Assert.Equal(128, info.Priority);
        Assert.False(info.SupportsInterpolation);
        Assert.False(info.SupportsPrediction);
        Assert.False(info.SupportsDelta);
    }

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var info1 = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 10,
            Strategy = SyncStrategy.Predicted,
            Frequency = 60,
            Priority = 100,
            SupportsInterpolation = true,
            SupportsPrediction = false,
            SupportsDelta = true
        };

        var info2 = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 10,
            Strategy = SyncStrategy.Predicted,
            Frequency = 60,
            Priority = 100,
            SupportsInterpolation = true,
            SupportsPrediction = false,
            SupportsDelta = true
        };

        Assert.Equal(info1, info2);
    }

    [Fact]
    public void Equals_DifferentNetworkTypeId_AreNotEqual()
    {
        var info1 = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 10,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = false,
            SupportsPrediction = false,
            SupportsDelta = false
        };

        var info2 = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 20,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = false,
            SupportsPrediction = false,
            SupportsDelta = false
        };

        Assert.NotEqual(info1, info2);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var info1 = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 5,
            Strategy = SyncStrategy.Interpolated,
            Frequency = 30,
            Priority = 150,
            SupportsInterpolation = true,
            SupportsPrediction = true,
            SupportsDelta = false
        };

        var info2 = new NetworkComponentInfo
        {
            Type = typeof(TestComponent),
            NetworkTypeId = 5,
            Strategy = SyncStrategy.Interpolated,
            Frequency = 30,
            Priority = 150,
            SupportsInterpolation = true,
            SupportsPrediction = true,
            SupportsDelta = false
        };

        Assert.Equal(info1.GetHashCode(), info2.GetHashCode());
    }

    private struct TestComponent : IComponent;
}
