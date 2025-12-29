using KeenEyes.Network;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="ReplicatedAttribute"/> class.
/// </summary>
public class ReplicatedAttributeTests
{
    [Fact]
    public void Strategy_DefaultsToAuthoritative()
    {
        var attr = new ReplicatedAttribute();

        Assert.Equal(SyncStrategy.Authoritative, attr.Strategy);
    }

    [Fact]
    public void Priority_DefaultsTo128()
    {
        var attr = new ReplicatedAttribute();

        Assert.Equal(128, attr.Priority);
    }

    [Fact]
    public void Strategy_CanBeSet()
    {
        var attr = new ReplicatedAttribute { Strategy = SyncStrategy.Interpolated };

        Assert.Equal(SyncStrategy.Interpolated, attr.Strategy);
    }

    [Fact]
    public void Priority_CanBeSet()
    {
        var attr = new ReplicatedAttribute { Priority = 200 };

        Assert.Equal(200, attr.Priority);
    }
}
