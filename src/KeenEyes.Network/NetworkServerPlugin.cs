using KeenEyes.Capabilities;
using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Replication;
using KeenEyes.Network.Systems;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network;

/// <summary>
/// Server-side network plugin for entity replication.
/// </summary>
/// <remarks>
/// <para>
/// The server plugin is authoritative: it owns the game state and replicates
/// changes to connected clients. Clients cannot directly modify server state;
/// they send inputs which the server processes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var transport = new LocalTransport();
/// await transport.ListenAsync(7777);
///
/// var config = new ServerNetworkConfig { Port = 7777, MaxClients = 16 };
/// var plugin = new NetworkServerPlugin(transport, config);
///
/// world.InstallPlugin(plugin);
/// </code>
/// </example>
public sealed class NetworkServerPlugin(INetworkTransport transport, ServerNetworkConfig? config = null) : INetworkPlugin
{
    private readonly INetworkTransport transport = transport;
    private readonly ServerNetworkConfig config = config ?? new ServerNetworkConfig();
    private readonly NetworkIdManager networkIdManager = new(isServer: true);
    private readonly Dictionary<int, ClientState> clients = [];

    private IPluginContext? context;
    private uint currentTick;
    private float tickAccumulator;
    private EventSubscription? entityCreatedSubscription;
    private EventSubscription? entityDestroyedSubscription;

    /// <inheritdoc/>
    public string Name => "NetworkServer";

    /// <inheritdoc/>
    public INetworkTransport Transport => transport;

    /// <inheritdoc/>
    public bool IsServer => true;

    /// <inheritdoc/>
    public bool IsClient => false;

    /// <inheritdoc/>
    public uint CurrentTick => currentTick;

    /// <inheritdoc/>
    public int LocalClientId => NetworkOwner.ServerClientId;

    /// <summary>
    /// Gets the network ID manager.
    /// </summary>
    public NetworkIdManager NetworkIds => networkIdManager;

    /// <summary>
    /// Gets the connected client count.
    /// </summary>
    public int ClientCount => clients.Count;

    /// <summary>
    /// Gets the server configuration.
    /// </summary>
    public ServerNetworkConfig Config => config;

    /// <summary>
    /// Raised when a client input is received.
    /// </summary>
    /// <remarks>
    /// The parameters are (clientId, inputTick, rawInputData).
    /// The input data span is only valid during the event callback.
    /// </remarks>
    public event Action<int, uint, ReadOnlySpan<byte>>? ClientInputReceived;

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        this.context = context;

        // Subscribe to transport events
        transport.ClientConnected += OnClientConnected;
        transport.ClientDisconnected += OnClientDisconnected;
        transport.DataReceived += OnDataReceived;

        // Register systems
        context.AddSystem(new NetworkServerReceiveSystem(this), SystemPhase.EarlyUpdate);
        context.AddSystem(new NetworkServerSendSystem(this), SystemPhase.LateUpdate);

        // Subscribe to entity events for replication
        entityCreatedSubscription = context.World.OnEntityCreated(OnEntityCreated);
        entityDestroyedSubscription = context.World.OnEntityDestroyed(OnEntityDestroyed);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Unsubscribe from transport events
        transport.ClientConnected -= OnClientConnected;
        transport.ClientDisconnected -= OnClientDisconnected;
        transport.DataReceived -= OnDataReceived;

        // Unsubscribe from entity events
        entityCreatedSubscription?.Dispose();
        entityDestroyedSubscription?.Dispose();

        this.context = null;
    }

    /// <summary>
    /// Advances the network tick.
    /// </summary>
    /// <param name="deltaTime">The time since last update.</param>
    /// <returns>True if a tick occurred; false otherwise.</returns>
    public bool Tick(float deltaTime)
    {
        var tickInterval = 1f / config.TickRate;
        tickAccumulator += deltaTime;

        if (tickAccumulator >= tickInterval)
        {
            tickAccumulator -= tickInterval;
            currentTick++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Registers an entity for network replication.
    /// </summary>
    /// <param name="entity">The entity to replicate.</param>
    /// <param name="ownerId">The owning client ID (0 for server).</param>
    /// <returns>The assigned network ID.</returns>
    public NetworkId RegisterNetworkedEntity(Entity entity, int ownerId = 0)
    {
        if (context is null)
        {
            throw new InvalidOperationException("Plugin is not installed.");
        }

        var networkId = networkIdManager.AssignNetworkId(entity);

        // Add network components to entity
        context.World.Add(entity, networkId);
        context.World.Add(entity, new NetworkOwner { ClientId = ownerId });
        context.World.Add(entity, new NetworkState { NeedsFullSync = true });
        context.World.Add(entity, default(Networked));

        return networkId;
    }

    /// <summary>
    /// Sends data to a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="mode">The delivery mode.</param>
    public void SendToClient(int clientId, ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        transport.Send(clientId, data, mode);
    }

    /// <summary>
    /// Sends data to all connected clients.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="mode">The delivery mode.</param>
    public void SendToAll(ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        transport.SendToAll(data, mode);
    }

    /// <summary>
    /// Sends data to all clients except one.
    /// </summary>
    /// <param name="excludeClientId">The client ID to exclude.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="mode">The delivery mode.</param>
    public void SendToAllExcept(int excludeClientId, ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        transport.SendToAllExcept(excludeClientId, data, mode);
    }

    /// <summary>
    /// Gets client state for a connected client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="state">The client state if found.</param>
    /// <returns>True if the client is connected; false otherwise.</returns>
    public bool TryGetClientState(int clientId, out ClientState? state)
    {
        return clients.TryGetValue(clientId, out state);
    }

    /// <summary>
    /// Gets all connected clients.
    /// </summary>
    /// <returns>An enumerable of client states.</returns>
    public IEnumerable<ClientState> GetConnectedClients()
    {
        return clients.Values;
    }

    /// <summary>
    /// Sends a full world snapshot to a client.
    /// </summary>
    /// <param name="clientId">The client to send to.</param>
    /// <remarks>
    /// <para>
    /// A full snapshot contains all networked entities and their current state.
    /// This is used for late joiners to sync up with the world state.
    /// </para>
    /// </remarks>
    public void SendFullSnapshot(int clientId)
    {
        if (context is null)
        {
            return;
        }

        var serializer = config.Serializer;
        if (serializer is null)
        {
            return;
        }

        // Collect all networked entities
        var entities = new List<(Entity entity, NetworkId netId, int ownerId)>();
        foreach (var entity in context.World.Query<NetworkId, NetworkOwner>())
        {
            ref readonly var netId = ref context.World.Get<NetworkId>(entity);
            ref readonly var owner = ref context.World.Get<NetworkOwner>(entity);
            entities.Add((entity, netId, owner.ClientId));
        }

        // Use a large buffer for the snapshot
        var buffer = new byte[64 * 1024]; // 64KB buffer
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.FullSnapshot, currentTick);
        writer.WriteEntityCount((ushort)entities.Count);

        foreach (var (entity, netId, ownerId) in entities)
        {
            writer.WriteEntitySpawn(netId.Value, ownerId);

            // Write all replicated components
            var toSend = new List<(Type, object)>();
            if (context.World is ISnapshotCapability snapshot)
            {
                foreach (var (type, value) in snapshot.GetComponents(entity))
                {
                    if (serializer.IsNetworkSerializable(type))
                    {
                        toSend.Add((type, value));
                    }
                }
            }

            writer.WriteComponentCount((byte)toSend.Count);
            foreach (var (type, value) in toSend)
            {
                writer.WriteComponent(serializer, type, value);
            }
        }

        transport.Send(clientId, writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);

        // Send hierarchy relationships
        SendHierarchySnapshot(clientId);
    }

    private void SendHierarchySnapshot(int clientId)
    {
        if (context is null)
        {
            return;
        }

        // Collect entities with parents
        var hierarchyChanges = new List<(uint childNetId, uint parentNetId)>();
        foreach (var entity in context.World.Query<NetworkId>())
        {
            var parent = context.World.GetParent(entity);
            if (parent != Entity.Null && context.World.IsAlive(parent) && context.World.Has<NetworkId>(parent))
            {
                ref readonly var childNetId = ref context.World.Get<NetworkId>(entity);
                ref readonly var parentNetId = ref context.World.Get<NetworkId>(parent);
                hierarchyChanges.Add((childNetId.Value, parentNetId.Value));
            }
        }

        // Send hierarchy changes
        Span<byte> hierBuffer = stackalloc byte[24];
        foreach (var (childNetId, parentNetId) in hierarchyChanges)
        {
            var writer = new NetworkMessageWriter(hierBuffer);
            writer.WriteHeader(MessageType.HierarchyChange, currentTick);
            writer.WriteHierarchyChange(childNetId, parentNetId);
            transport.Send(clientId, writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        }
    }

    /// <summary>
    /// Sends a hierarchy change to all connected clients.
    /// </summary>
    /// <param name="childEntity">The child entity.</param>
    /// <param name="parentEntity">The parent entity, or Entity.Null for no parent.</param>
    public void SendHierarchyChange(Entity childEntity, Entity parentEntity)
    {
        if (context is null)
        {
            return;
        }

        if (!context.World.Has<NetworkId>(childEntity))
        {
            return;
        }

        uint parentNetId = 0;
        if (parentEntity != Entity.Null && context.World.Has<NetworkId>(parentEntity))
        {
            parentNetId = context.World.Get<NetworkId>(parentEntity).Value;
        }

        ref readonly var childNetId = ref context.World.Get<NetworkId>(childEntity);

        Span<byte> buffer = stackalloc byte[24];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.HierarchyChange, currentTick);
        writer.WriteHierarchyChange(childNetId.Value, parentNetId);
        SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
    }

    private void OnClientConnected(int clientId)
    {
        if (clients.Count >= config.MaxClients)
        {
            // Reject - too many clients
            transport.Disconnect(clientId);
            return;
        }

        clients[clientId] = new ClientState
        {
            ClientId = clientId,
            LastAckedTick = 0,
            NeedsFullSnapshot = true
        };

        // Send connection accepted
        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ConnectionAccepted, currentTick);
        writer.WriteSignedBits(clientId, 16);
        transport.Send(clientId, writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
    }

    private void OnClientDisconnected(int clientId)
    {
        clients.Remove(clientId);

        // Despawn entities owned by this client
        if (context is not null)
        {
            var toRemove = new List<Entity>();
            foreach (var entity in context.World.Query<NetworkOwner>())
            {
                ref readonly var owner = ref context.World.Get<NetworkOwner>(entity);
                if (owner.ClientId == clientId)
                {
                    toRemove.Add(entity);
                }
            }

            foreach (var entity in toRemove)
            {
                context.World.Despawn(entity);
            }
        }
    }

    private void OnDataReceived(int clientId, ReadOnlySpan<byte> data)
    {
        if (!clients.TryGetValue(clientId, out var clientState))
        {
            return;
        }

        var reader = new NetworkMessageReader(data);
        reader.ReadHeader(out var messageType, out var tick);

        switch (messageType)
        {
            case MessageType.ClientAck:
                clientState.LastAckedTick = tick;
                break;

            case MessageType.ClientInput:
                // Notify listeners with remaining data as input payload
                // The remaining bytes in the message after the header contain the input
                var inputPayload = data[5..]; // Skip header (1 byte type + 4 bytes tick)
                ClientInputReceived?.Invoke(clientId, tick, inputPayload);
                break;

            case MessageType.Ping:
                // Respond with pong
                Span<byte> pongBuffer = stackalloc byte[8];
                var pongWriter = new NetworkMessageWriter(pongBuffer);
                pongWriter.WriteHeader(MessageType.Pong, tick);
                transport.Send(clientId, pongWriter.GetWrittenSpan(), DeliveryMode.Unreliable);
                break;
        }
    }

    private void OnEntityCreated(Entity entity, string? name)
    {
        // Auto-register entities with Networked tag
        if (context is not null && context.World.Has<Networked>(entity) && !networkIdManager.HasNetworkId(entity))
        {
            RegisterNetworkedEntity(entity);
        }
    }

    private void OnEntityDestroyed(Entity entity)
    {
        // Remove from network tracking
        if (networkIdManager.TryGetNetworkId(entity, out var networkId))
        {
            // Notify clients of despawn
            Span<byte> buffer = stackalloc byte[16];
            var writer = new NetworkMessageWriter(buffer);
            writer.WriteHeader(MessageType.EntityDespawn, currentTick);
            writer.WriteEntityDespawn(networkId.Value);
            transport.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);

            networkIdManager.UnregisterEntity(entity);
        }
    }
}

/// <summary>
/// Tracks state for a connected client.
/// </summary>
public sealed class ClientState
{
    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets or sets the last acknowledged server tick.
    /// </summary>
    public uint LastAckedTick { get; set; }

    /// <summary>
    /// Gets or sets whether the client needs a full snapshot.
    /// </summary>
    public bool NeedsFullSnapshot { get; set; }

    /// <summary>
    /// Gets or sets the round-trip time in milliseconds.
    /// </summary>
    public float RoundTripTimeMs { get; set; }
}
