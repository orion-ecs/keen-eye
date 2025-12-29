using KeenEyes.Network.Components;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="NetworkState"/> component.
/// </summary>
public class NetworkStateTests
{
    [Fact]
    public void NeedsFullSync_DefaultsToFalse()
    {
        var state = new NetworkState();

        Assert.False(state.NeedsFullSync);
    }

    [Fact]
    public void LastSentTick_DefaultsToZero()
    {
        var state = new NetworkState();

        Assert.Equal(0u, state.LastSentTick);
    }

    [Fact]
    public void LastReceivedTick_DefaultsToZero()
    {
        var state = new NetworkState();

        Assert.Equal(0u, state.LastReceivedTick);
    }

    [Fact]
    public void NeedsFullSync_CanBeSetToTrue()
    {
        var state = new NetworkState { NeedsFullSync = true };

        Assert.True(state.NeedsFullSync);
    }

    [Fact]
    public void LastSentTick_CanBeSet()
    {
        var state = new NetworkState { LastSentTick = 100 };

        Assert.Equal(100u, state.LastSentTick);
    }

    [Fact]
    public void LastReceivedTick_CanBeSet()
    {
        var state = new NetworkState { LastReceivedTick = 200 };

        Assert.Equal(200u, state.LastReceivedTick);
    }
}
