using KeenEyes.Network.Components;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="NetworkOwner"/> component.
/// </summary>
public class NetworkOwnerTests
{
    [Fact]
    public void ServerClientId_IsZero()
    {
        Assert.Equal(0, NetworkOwner.ServerClientId);
    }

    [Fact]
    public void Server_HasServerClientId()
    {
        Assert.Equal(NetworkOwner.ServerClientId, NetworkOwner.Server.ClientId);
    }

    [Fact]
    public void IsServerOwned_WithServerClientId_ReturnsTrue()
    {
        var owner = new NetworkOwner { ClientId = NetworkOwner.ServerClientId };

        Assert.True(owner.IsServerOwned);
    }

    [Fact]
    public void IsServerOwned_WithClientId_ReturnsFalse()
    {
        var owner = new NetworkOwner { ClientId = 1 };

        Assert.False(owner.IsServerOwned);
    }

    [Fact]
    public void Equals_SameClientId_ReturnsTrue()
    {
        var owner1 = new NetworkOwner { ClientId = 5 };
        var owner2 = new NetworkOwner { ClientId = 5 };

        Assert.Equal(owner1, owner2);
    }

    [Fact]
    public void Equals_DifferentClientId_ReturnsFalse()
    {
        var owner1 = new NetworkOwner { ClientId = 5 };
        var owner2 = new NetworkOwner { ClientId = 6 };

        Assert.NotEqual(owner1, owner2);
    }

    [Fact]
    public void Client_CreatesOwnerWithSpecifiedId()
    {
        var owner = NetworkOwner.Client(42);

        Assert.Equal(42, owner.ClientId);
        Assert.False(owner.IsServerOwned);
    }

    [Fact]
    public void ToString_ServerOwned_ReturnsServerString()
    {
        var owner = NetworkOwner.Server;

        Assert.Equal("NetworkOwner(Server)", owner.ToString());
    }

    [Fact]
    public void ToString_ClientOwned_ReturnsClientString()
    {
        var owner = NetworkOwner.Client(5);

        Assert.Equal("NetworkOwner(Client:5)", owner.ToString());
    }

    [Fact]
    public void GetHashCode_SameClientId_SameHash()
    {
        var owner1 = new NetworkOwner { ClientId = 10 };
        var owner2 = new NetworkOwner { ClientId = 10 };

        Assert.Equal(owner1.GetHashCode(), owner2.GetHashCode());
    }
}
