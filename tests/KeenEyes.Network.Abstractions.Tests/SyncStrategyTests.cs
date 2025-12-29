using KeenEyes.Network;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="SyncStrategy"/> enum.
/// </summary>
public class SyncStrategyTests
{
    [Fact]
    public void Authoritative_HasExpectedValue()
    {
        Assert.Equal(0, (int)SyncStrategy.Authoritative);
    }

    [Fact]
    public void Interpolated_HasExpectedValue()
    {
        Assert.Equal(1, (int)SyncStrategy.Interpolated);
    }

    [Fact]
    public void Predicted_HasExpectedValue()
    {
        Assert.Equal(2, (int)SyncStrategy.Predicted);
    }

    [Fact]
    public void OwnerAuthoritative_HasExpectedValue()
    {
        Assert.Equal(3, (int)SyncStrategy.OwnerAuthoritative);
    }
}
