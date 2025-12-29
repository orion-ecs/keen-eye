using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="ConnectionState"/> enum.
/// </summary>
public class ConnectionStateTests
{
    [Fact]
    public void Disconnected_HasExpectedValue()
    {
        Assert.Equal(0, (int)ConnectionState.Disconnected);
    }

    [Fact]
    public void Connecting_HasExpectedValue()
    {
        Assert.Equal(1, (int)ConnectionState.Connecting);
    }

    [Fact]
    public void Connected_HasExpectedValue()
    {
        Assert.Equal(2, (int)ConnectionState.Connected);
    }

    [Fact]
    public void Disconnecting_HasExpectedValue()
    {
        Assert.Equal(3, (int)ConnectionState.Disconnecting);
    }
}
