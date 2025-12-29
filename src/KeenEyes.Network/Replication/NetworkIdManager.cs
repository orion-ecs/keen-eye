using KeenEyes.Network.Components;

namespace KeenEyes.Network.Replication;

/// <summary>
/// Manages network entity ID assignment and mapping.
/// </summary>
/// <remarks>
/// <para>
/// The server assigns unique network IDs to entities. Clients maintain a mapping
/// from network IDs to their local entity IDs, since local entity IDs may differ
/// between client and server.
/// </para>
/// </remarks>
public sealed class NetworkIdManager(bool isServer)
{
    private readonly Dictionary<uint, Entity> networkToLocal = [];
    private readonly Dictionary<int, uint> localToNetwork = [];
    private uint nextNetworkId = 1;

    /// <summary>
    /// Gets the number of registered network entities.
    /// </summary>
    public int Count => networkToLocal.Count;

    /// <summary>
    /// Assigns a new network ID to an entity (server only).
    /// </summary>
    /// <param name="entity">The entity to assign an ID to.</param>
    /// <returns>The assigned network ID.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called on a client or if the entity already has a network ID.
    /// </exception>
    public NetworkId AssignNetworkId(Entity entity)
    {
        if (!isServer)
        {
            throw new InvalidOperationException("Only the server can assign network IDs.");
        }

        if (localToNetwork.ContainsKey(entity.Id))
        {
            throw new InvalidOperationException($"Entity {entity.Id} already has a network ID.");
        }

        var networkId = new NetworkId { Value = nextNetworkId++ };
        RegisterMapping(networkId, entity);
        return networkId;
    }

    /// <summary>
    /// Registers a network ID to local entity mapping (client receives from server).
    /// </summary>
    /// <param name="networkId">The network ID from the server.</param>
    /// <param name="localEntity">The local entity.</param>
    public void RegisterMapping(NetworkId networkId, Entity localEntity)
    {
        networkToLocal[networkId.Value] = localEntity;
        localToNetwork[localEntity.Id] = networkId.Value;
    }

    /// <summary>
    /// Removes the mapping for an entity.
    /// </summary>
    /// <param name="entity">The entity to unregister.</param>
    /// <returns>True if the entity was registered; false otherwise.</returns>
    public bool UnregisterEntity(Entity entity)
    {
        if (!localToNetwork.TryGetValue(entity.Id, out var networkId))
        {
            return false;
        }

        localToNetwork.Remove(entity.Id);
        networkToLocal.Remove(networkId);
        return true;
    }

    /// <summary>
    /// Removes the mapping for a network ID.
    /// </summary>
    /// <param name="networkId">The network ID to unregister.</param>
    /// <returns>True if the network ID was registered; false otherwise.</returns>
    public bool UnregisterNetworkId(NetworkId networkId)
    {
        if (!networkToLocal.TryGetValue(networkId.Value, out var entity))
        {
            return false;
        }

        networkToLocal.Remove(networkId.Value);
        localToNetwork.Remove(entity.Id);
        return true;
    }

    /// <summary>
    /// Gets the local entity for a network ID.
    /// </summary>
    /// <param name="networkId">The network ID.</param>
    /// <param name="entity">The local entity if found.</param>
    /// <returns>True if the network ID is registered; false otherwise.</returns>
    public bool TryGetLocalEntity(NetworkId networkId, out Entity entity)
    {
        return networkToLocal.TryGetValue(networkId.Value, out entity);
    }

    /// <summary>
    /// Gets the local entity for a network ID.
    /// </summary>
    /// <param name="networkIdValue">The network ID value.</param>
    /// <param name="entity">The local entity if found.</param>
    /// <returns>True if the network ID is registered; false otherwise.</returns>
    public bool TryGetLocalEntity(uint networkIdValue, out Entity entity)
    {
        return networkToLocal.TryGetValue(networkIdValue, out entity);
    }

    /// <summary>
    /// Gets the network ID for a local entity.
    /// </summary>
    /// <param name="entity">The local entity.</param>
    /// <param name="networkId">The network ID if found.</param>
    /// <returns>True if the entity is registered; false otherwise.</returns>
    public bool TryGetNetworkId(Entity entity, out NetworkId networkId)
    {
        if (localToNetwork.TryGetValue(entity.Id, out var value))
        {
            networkId = new NetworkId { Value = value };
            return true;
        }

        networkId = NetworkId.Invalid;
        return false;
    }

    /// <summary>
    /// Checks if an entity has a network ID.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has a network ID; false otherwise.</returns>
    public bool HasNetworkId(Entity entity)
    {
        return localToNetwork.ContainsKey(entity.Id);
    }

    /// <summary>
    /// Checks if a network ID is registered.
    /// </summary>
    /// <param name="networkId">The network ID to check.</param>
    /// <returns>True if the network ID is registered; false otherwise.</returns>
    public bool IsRegistered(NetworkId networkId)
    {
        return networkToLocal.ContainsKey(networkId.Value);
    }

    /// <summary>
    /// Gets all registered network IDs and their local entities.
    /// </summary>
    /// <returns>An enumerable of (NetworkId, Entity) pairs.</returns>
    public IEnumerable<(NetworkId NetworkId, Entity Entity)> GetAllMappings()
    {
        foreach (var (networkIdValue, entity) in networkToLocal)
        {
            yield return (new NetworkId { Value = networkIdValue }, entity);
        }
    }

    /// <summary>
    /// Clears all mappings.
    /// </summary>
    public void Clear()
    {
        networkToLocal.Clear();
        localToNetwork.Clear();
    }
}
