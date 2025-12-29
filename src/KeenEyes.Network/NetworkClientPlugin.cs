using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Replication;
using KeenEyes.Network.Systems;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network;

/// <summary>
/// Client-side network plugin for receiving replicated state.
/// </summary>
/// <remarks>
/// <para>
/// The client plugin receives state updates from the server and applies them
/// to the local world. For the local player entity, it can optionally run
/// client-side prediction for responsive controls.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var transport = new LocalTransport();
///
/// var config = new ClientNetworkConfig
/// {
///     ServerAddress = "127.0.0.1",
///     ServerPort = 7777,
///     EnablePrediction = true
/// };
///
/// var plugin = new NetworkClientPlugin(transport, config);
/// world.InstallPlugin(plugin);
///
/// await plugin.ConnectAsync();
/// </code>
/// </example>
public sealed class NetworkClientPlugin(INetworkTransport transport, ClientNetworkConfig? config = null) : INetworkPlugin
{
    private readonly INetworkTransport transport = transport;
    private readonly ClientNetworkConfig config = config ?? new ClientNetworkConfig();
    private readonly NetworkIdManager networkIdManager = new(isServer: false);

    private IPluginContext? context;
    private uint currentTick;
    private uint lastReceivedTick;
    private int localClientId;
    private bool isConnected;

    /// <inheritdoc/>
    public string Name => "NetworkClient";

    /// <inheritdoc/>
    public INetworkTransport Transport => transport;

    /// <inheritdoc/>
    public bool IsServer => false;

    /// <inheritdoc/>
    public bool IsClient => true;

    /// <inheritdoc/>
    public uint CurrentTick => currentTick;

    /// <inheritdoc/>
    public int LocalClientId => localClientId;

    /// <summary>
    /// Gets the network ID manager.
    /// </summary>
    public NetworkIdManager NetworkIds => networkIdManager;

    /// <summary>
    /// Gets the client configuration.
    /// </summary>
    public ClientNetworkConfig Config => config;

    /// <summary>
    /// Gets whether the client is connected to a server.
    /// </summary>
    public bool IsConnected => isConnected;

    /// <summary>
    /// Gets the last received server tick.
    /// </summary>
    public uint LastReceivedTick => lastReceivedTick;

    /// <summary>
    /// Raised when connected to the server.
    /// </summary>
    public event Action? Connected;

    /// <summary>
    /// Raised when disconnected from the server.
    /// </summary>
    public event Action? Disconnected;

    /// <summary>
    /// Raised when connection is rejected.
    /// </summary>
    public event Action<string>? ConnectionRejected;

    /// <inheritdoc/>
    public void Install(IPluginContext ctx)
    {
        context = ctx;

        // Subscribe to transport events
        transport.StateChanged += OnStateChanged;
        transport.DataReceived += OnDataReceived;

        // Register systems
        ctx.AddSystem(new NetworkClientReceiveSystem(this), SystemPhase.EarlyUpdate);
        ctx.AddSystem(new NetworkClientSendSystem(this), SystemPhase.LateUpdate);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext ctx)
    {
        // Unsubscribe from transport events
        transport.StateChanged -= OnStateChanged;
        transport.DataReceived -= OnDataReceived;

        context = null;
    }

    /// <summary>
    /// Connects to the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await transport.ConnectAsync(config.ServerAddress, config.ServerPort, cancellationToken);
    }

    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    public void Disconnect()
    {
        transport.Disconnect();
    }

    /// <summary>
    /// Sends data to the server.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="mode">The delivery mode.</param>
    public void SendToServer(ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        transport.Send(0, data, mode);
    }

    /// <summary>
    /// Acknowledges a received tick.
    /// </summary>
    /// <param name="tick">The tick to acknowledge.</param>
    public void AcknowledgeTick(uint tick)
    {
        Span<byte> buffer = stackalloc byte[8];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ClientAck, tick);
        transport.Send(0, writer.GetWrittenSpan(), DeliveryMode.UnreliableSequenced);
    }

    /// <summary>
    /// Spawns a local entity to represent a networked entity.
    /// </summary>
    /// <param name="networkId">The network ID from the server.</param>
    /// <param name="ownerId">The owner client ID.</param>
    /// <returns>The created local entity.</returns>
    public Entity SpawnNetworkedEntity(uint networkId, int ownerId)
    {
        if (context is null)
        {
            throw new InvalidOperationException("Plugin is not installed.");
        }

        var entity = context.World.Spawn().Build();

        var netId = new NetworkId { Value = networkId };
        networkIdManager.RegisterMapping(netId, entity);

        // Add network components
        context.World.Add(entity, netId);
        context.World.Add(entity, new NetworkOwner { ClientId = ownerId });
        context.World.Add(entity, new NetworkState());
        context.World.Add(entity, default(Networked));

        // Mark as locally or remotely owned
        if (ownerId == localClientId)
        {
            context.World.Add(entity, default(LocallyOwned));
            if (config.EnablePrediction)
            {
                context.World.Add(entity, default(Predicted));
                context.World.Add(entity, new PredictionState());
            }
        }
        else
        {
            context.World.Add(entity, default(RemotelyOwned));
            context.World.Add(entity, default(Interpolated));
            context.World.Add(entity, new InterpolationState());
        }

        return entity;
    }

    /// <summary>
    /// Despawns a networked entity.
    /// </summary>
    /// <param name="networkId">The network ID of the entity to despawn.</param>
    public void DespawnNetworkedEntity(uint networkId)
    {
        if (context is null)
        {
            return;
        }

        if (networkIdManager.TryGetLocalEntity(networkId, out var entity))
        {
            networkIdManager.UnregisterNetworkId(new NetworkId { Value = networkId });
            context.World.Despawn(entity);
        }
    }

    /// <summary>
    /// Updates the current tick from server.
    /// </summary>
    /// <param name="serverTick">The server tick.</param>
    public void UpdateTick(uint serverTick)
    {
        lastReceivedTick = serverTick;
        currentTick = serverTick;
    }

    private void OnStateChanged(ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Connected:
                // Wait for server to send ConnectionAccepted with our client ID
                break;

            case ConnectionState.Disconnected:
                isConnected = false;
                localClientId = 0;
                networkIdManager.Clear();
                Disconnected?.Invoke();
                break;
        }
    }

    private void OnDataReceived(int connectionId, ReadOnlySpan<byte> data)
    {
        var reader = new NetworkMessageReader(data);
        reader.ReadHeader(out var messageType, out var tick);

        UpdateTick(tick);

        switch (messageType)
        {
            case MessageType.ConnectionAccepted:
                localClientId = reader.ReadSignedBits(16);
                isConnected = true;
                Connected?.Invoke();
                break;

            case MessageType.ConnectionRejected:
                // TODO: Read rejection reason
                ConnectionRejected?.Invoke("Connection rejected by server");
                break;

            case MessageType.EntitySpawn:
                reader.ReadEntitySpawn(out var spawnNetworkId, out var ownerId);
                SpawnNetworkedEntity(spawnNetworkId, ownerId);
                break;

            case MessageType.EntityDespawn:
                reader.ReadEntityDespawn(out var despawnNetworkId);
                DespawnNetworkedEntity(despawnNetworkId);
                break;

            case MessageType.FullSnapshot:
                // TODO: Handle full snapshot
                break;

            case MessageType.DeltaSnapshot:
                // TODO: Handle delta snapshot
                break;

            case MessageType.ComponentUpdate:
                // TODO: Handle component update
                break;

            case MessageType.Pong:
                // TODO: Calculate RTT
                break;

            case MessageType.OwnershipTransfer:
                // TODO: Handle ownership transfer
                break;
        }

        // Acknowledge received tick
        AcknowledgeTick(tick);
    }
}
