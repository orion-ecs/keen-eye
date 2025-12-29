using KeenEyes.Network.Components;
using KeenEyes.Network.Replication;

namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for the <see cref="NetworkIdManager"/> class.
/// </summary>
public class NetworkIdManagerTests
{
    [Fact]
    public void Count_InitiallyZero()
    {
        var manager = new NetworkIdManager(isServer: true);

        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void AssignNetworkId_ReturnsUniqueId()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity1 = new Entity(1, 0);
        var entity2 = new Entity(2, 0);

        var id1 = manager.AssignNetworkId(entity1);
        var id2 = manager.AssignNetworkId(entity2);

        Assert.NotEqual(id1.Value, id2.Value);
        Assert.True(id1.IsValid);
        Assert.True(id2.IsValid);
    }

    [Fact]
    public void AssignNetworkId_OnClient_ThrowsException()
    {
        var manager = new NetworkIdManager(isServer: false);
        var entity = new Entity(1, 0);

        Assert.Throws<InvalidOperationException>(() => manager.AssignNetworkId(entity));
    }

    [Fact]
    public void AssignNetworkId_DuplicateEntity_ThrowsException()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity = new Entity(1, 0);

        manager.AssignNetworkId(entity);

        Assert.Throws<InvalidOperationException>(() => manager.AssignNetworkId(entity));
    }

    [Fact]
    public void RegisterMapping_CanBeQueriedByNetworkId()
    {
        var manager = new NetworkIdManager(isServer: false);
        var networkId = new NetworkId { Value = 42 };
        var entity = new Entity(1, 0);

        manager.RegisterMapping(networkId, entity);

        Assert.True(manager.TryGetLocalEntity(networkId, out var result));
        Assert.Equal(entity, result);
    }

    [Fact]
    public void RegisterMapping_CanBeQueriedByEntity()
    {
        var manager = new NetworkIdManager(isServer: false);
        var networkId = new NetworkId { Value = 42 };
        var entity = new Entity(1, 0);

        manager.RegisterMapping(networkId, entity);

        Assert.True(manager.TryGetNetworkId(entity, out var result));
        Assert.Equal(networkId, result);
    }

    [Fact]
    public void UnregisterEntity_RemovesMapping()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity = new Entity(1, 0);
        manager.AssignNetworkId(entity);

        var removed = manager.UnregisterEntity(entity);

        Assert.True(removed);
        Assert.False(manager.HasNetworkId(entity));
    }

    [Fact]
    public void UnregisterNetworkId_RemovesMapping()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity = new Entity(1, 0);
        var networkId = manager.AssignNetworkId(entity);

        var removed = manager.UnregisterNetworkId(networkId);

        Assert.True(removed);
        Assert.False(manager.IsRegistered(networkId));
    }

    [Fact]
    public void HasNetworkId_WithRegisteredEntity_ReturnsTrue()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity = new Entity(1, 0);
        manager.AssignNetworkId(entity);

        Assert.True(manager.HasNetworkId(entity));
    }

    [Fact]
    public void HasNetworkId_WithUnregisteredEntity_ReturnsFalse()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity = new Entity(1, 0);

        Assert.False(manager.HasNetworkId(entity));
    }

    [Fact]
    public void IsRegistered_WithRegisteredId_ReturnsTrue()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity = new Entity(1, 0);
        var networkId = manager.AssignNetworkId(entity);

        Assert.True(manager.IsRegistered(networkId));
    }

    [Fact]
    public void IsRegistered_WithUnregisteredId_ReturnsFalse()
    {
        var manager = new NetworkIdManager(isServer: true);
        var networkId = new NetworkId { Value = 999 };

        Assert.False(manager.IsRegistered(networkId));
    }

    [Fact]
    public void GetAllMappings_ReturnsAllRegisteredMappings()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity1 = new Entity(1, 0);
        var entity2 = new Entity(2, 0);
        manager.AssignNetworkId(entity1);
        manager.AssignNetworkId(entity2);

        var mappings = manager.GetAllMappings().ToList();

        Assert.Equal(2, mappings.Count);
    }

    [Fact]
    public void Clear_RemovesAllMappings()
    {
        var manager = new NetworkIdManager(isServer: true);
        var entity = new Entity(1, 0);
        manager.AssignNetworkId(entity);

        manager.Clear();

        Assert.Equal(0, manager.Count);
    }
}
