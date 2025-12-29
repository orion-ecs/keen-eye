using KeenEyes.Network.Components;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Replication;
using KeenEyes.Network.Serialization;
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
    private readonly Dictionary<Entity, SnapshotBuffer> snapshotBuffers = [];
    private readonly Dictionary<Entity, object> inputBuffers = []; // Stores InputBuffer<T> instances

    private IPluginContext? context;
    private ClientPredictionSystem? predictionSystem;
    private uint currentTick;
    private uint lastReceivedTick;
    private int localClientId;
    private bool isConnected;
    private long lastPingSentTimestamp;
    private float roundTripTimeMs;

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
    /// Gets the prediction system, if enabled.
    /// </summary>
    public ClientPredictionSystem? PredictionSystem => predictionSystem;

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

    /// <summary>
    /// Raised when ownership of an entity is transferred.
    /// </summary>
    /// <remarks>
    /// The parameters are (entity, oldOwnerId, newOwnerId).
    /// </remarks>
    public event Action<Entity, int, int>? OwnershipChanged;

    /// <summary>
    /// Gets the round-trip time to the server in milliseconds.
    /// </summary>
    public float RoundTripTimeMs => roundTripTimeMs;

    /// <summary>
    /// Gets the snapshot buffer for an entity, if it exists.
    /// </summary>
    /// <param name="entity">The entity to get the snapshot buffer for.</param>
    /// <returns>The snapshot buffer, or null if the entity has no buffer.</returns>
    public SnapshotBuffer? GetSnapshotBuffer(Entity entity)
    {
        return snapshotBuffers.TryGetValue(entity, out var buffer) ? buffer : null;
    }

    /// <summary>
    /// Gets the input buffer for an entity, if it exists.
    /// </summary>
    /// <param name="entity">The entity to get the input buffer for.</param>
    /// <returns>The input buffer (as object), or null if the entity has no buffer.</returns>
    internal object? GetInputBuffer(Entity entity)
    {
        return inputBuffers.TryGetValue(entity, out var buffer) ? buffer : null;
    }

    /// <summary>
    /// Gets or creates a typed input buffer for an entity.
    /// </summary>
    /// <typeparam name="T">The input type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The input buffer.</returns>
    public InputBuffer<T> GetOrCreateInputBuffer<T>(Entity entity) where T : struct, INetworkInput
    {
        if (inputBuffers.TryGetValue(entity, out var existing) && existing is InputBuffer<T> typed)
        {
            return typed;
        }

        var buffer = new InputBuffer<T>(config.InputBufferSize);
        inputBuffers[entity] = buffer;
        return buffer;
    }

    /// <summary>
    /// Records an input for prediction and sends it to the server.
    /// </summary>
    /// <typeparam name="T">The input type.</typeparam>
    /// <param name="entity">The entity the input is for.</param>
    /// <param name="input">The input to record.</param>
    public void RecordInput<T>(Entity entity, T input) where T : struct, INetworkInput
    {
        // Set the tick on the input
        input.Tick = currentTick;

        // Store in input buffer
        var buffer = GetOrCreateInputBuffer<T>(entity);
        buffer.Add(input);

        // The input will be sent by NetworkClientSendSystem
    }

    /// <inheritdoc/>
    public void Install(IPluginContext ctx)
    {
        context = ctx;

        // Subscribe to transport events
        transport.StateChanged += OnStateChanged;
        transport.DataReceived += OnDataReceived;

        // Register systems
        ctx.AddSystem(new NetworkClientReceiveSystem(this), SystemPhase.EarlyUpdate);

        // Add prediction system if enabled
        if (config.EnablePrediction)
        {
            predictionSystem = new ClientPredictionSystem(
                this,
                config.Interpolator,
                config.InputApplicator);
            ctx.AddSystem(predictionSystem, SystemPhase.Update);
        }

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
    /// Sends a ping to the server to measure round-trip time.
    /// </summary>
    public void SendPing()
    {
        lastPingSentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<byte> buffer = stackalloc byte[8];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.Ping, currentTick);
        transport.Send(0, writer.GetWrittenSpan(), DeliveryMode.Unreliable);
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
            // Create snapshot buffer for interpolation
            snapshotBuffers[entity] = new SnapshotBuffer();
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
            snapshotBuffers.Remove(entity); // Clean up snapshot buffer
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

        // Update server time estimate based on tick (assuming fixed tick rate)
        var tickInterval = 1.0 / config.TickRate;
        serverTime = serverTick * tickInterval;
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
                HandleConnectionRejected(ref reader);
                break;

            case MessageType.EntitySpawn:
                reader.ReadEntitySpawn(out var spawnNetworkId, out var ownerId);
                var spawnedEntity = SpawnNetworkedEntity(spawnNetworkId, ownerId);
                // Read and apply initial components
                ApplyComponentUpdates(spawnedEntity, ref reader);
                break;

            case MessageType.EntityDespawn:
                reader.ReadEntityDespawn(out var despawnNetworkId);
                DespawnNetworkedEntity(despawnNetworkId);
                break;

            case MessageType.FullSnapshot:
                HandleFullSnapshot(ref reader);
                break;

            case MessageType.DeltaSnapshot:
                HandleDeltaSnapshot(ref reader);
                break;

            case MessageType.ComponentUpdate:
                HandleComponentUpdate(ref reader);
                break;

            case MessageType.Pong:
                HandlePong();
                break;

            case MessageType.OwnershipTransfer:
                HandleOwnershipTransfer(ref reader);
                break;
        }

        // Acknowledge received tick
        AcknowledgeTick(tick);
    }

    private void HandleFullSnapshot(ref NetworkMessageReader reader)
    {
        if (context is null)
        {
            return;
        }

        var entityCount = reader.ReadEntityCount();
        for (int i = 0; i < entityCount; i++)
        {
            reader.ReadEntitySpawn(out var networkId, out var ownerId);

            // Check if entity already exists
            Entity entity;
            if (networkIdManager.TryGetLocalEntity(networkId, out var existingEntity))
            {
                entity = existingEntity;
            }
            else
            {
                entity = SpawnNetworkedEntity(networkId, ownerId);
            }

            // Apply all components
            ApplyComponentUpdates(entity, ref reader);
        }
    }

    private void HandleDeltaSnapshot(ref NetworkMessageReader reader)
    {
        if (context is null)
        {
            return;
        }

        var entityCount = reader.ReadEntityCount();
        for (int i = 0; i < entityCount; i++)
        {
            var networkId = reader.ReadNetworkId();

            if (!networkIdManager.TryGetLocalEntity(networkId, out var entity))
            {
                // Entity not found - we cannot safely skip its components
                // without knowing component sizes. In production, components would
                // be length-prefixed or we'd track sizes in the serializer.
                // For now, skip reading components for unknown entities.
                _ = reader.ReadComponentCount();
                continue;
            }

            ApplyComponentUpdates(entity, ref reader);
        }
    }

    private void HandleComponentUpdate(ref NetworkMessageReader reader)
    {
        if (context is null)
        {
            return;
        }

        var networkId = reader.ReadNetworkId();
        if (!networkIdManager.TryGetLocalEntity(networkId, out var entity))
        {
            // Entity not found, skip the component data
            return;
        }

        ApplyComponentUpdates(entity, ref reader);
    }

    private void ApplyComponentUpdates(Entity entity, ref NetworkMessageReader reader)
    {
        if (context is null)
        {
            return;
        }

        var serializer = config.Serializer;
        if (serializer is null)
        {
            // No serializer, skip component data by reading count
            _ = reader.ReadComponentCount();
            // Cannot skip actual data without knowing sizes, so just return
            return;
        }

        var interpolator = config.Interpolator;
        snapshotBuffers.TryGetValue(entity, out var snapshotBuffer);

        var componentCount = reader.ReadComponentCount();
        for (int i = 0; i < componentCount; i++)
        {
            var component = reader.ReadComponent(serializer, out var componentType);
            if (component is not null && componentType is not null)
            {
                // If interpolation is enabled for this entity and component,
                // push to snapshot buffer instead of applying directly
                if (snapshotBuffer is not null &&
                    interpolator is not null &&
                    interpolator.IsInterpolatable(componentType))
                {
                    snapshotBuffer.PushSnapshot(componentType, component);

                    // Update interpolation timestamps
                    if (context.World.Has<InterpolationState>(entity))
                    {
                        ref var interpState = ref context.World.Get<InterpolationState>(entity);
                        interpState.FromTime = interpState.ToTime;
                        interpState.ToTime = serverTime;
                        interpState.Factor = 0f; // Reset factor for new interpolation window
                    }
                }
                else
                {
                    // No interpolation, apply directly
                    context.World.SetComponent(entity, componentType, component);
                }
            }
        }
    }

    private double serverTime; // Track server time for interpolation timestamps

    private void HandleConnectionRejected(ref NetworkMessageReader reader)
    {
        // Read rejection reason code (1 byte)
        var reasonCode = reader.ReadByte();
        var reason = reasonCode switch
        {
            1 => "Server is full",
            2 => "Authentication failed",
            3 => "Version mismatch",
            4 => "Banned",
            _ => $"Connection rejected (code: {reasonCode})"
        };
        ConnectionRejected?.Invoke(reason);
    }

    private void HandlePong()
    {
        // Calculate RTT from ping timestamp
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        roundTripTimeMs = now - lastPingSentTimestamp;
    }

    private void HandleOwnershipTransfer(ref NetworkMessageReader reader)
    {
        if (context is null)
        {
            return;
        }

        var networkId = reader.ReadNetworkId();
        var newOwnerId = reader.ReadSignedBits(16);

        if (!networkIdManager.TryGetLocalEntity(networkId, out var entity))
        {
            return;
        }

        // Get old owner before updating
        var oldOwnerId = 0;
        if (context.World.Has<NetworkOwner>(entity))
        {
            oldOwnerId = context.World.Get<NetworkOwner>(entity).ClientId;
        }

        // Update owner component
        context.World.Set(entity, new NetworkOwner { ClientId = newOwnerId });

        // Update ownership tags
        var wasLocal = context.World.Has<LocallyOwned>(entity);
        var willBeLocal = newOwnerId == localClientId;

        if (wasLocal && !willBeLocal)
        {
            // Transfer from local to remote
            context.World.Remove<LocallyOwned>(entity);
            if (context.World.Has<Predicted>(entity))
            {
                context.World.Remove<Predicted>(entity);
            }
            if (context.World.Has<PredictionState>(entity))
            {
                context.World.Remove<PredictionState>(entity);
            }
            context.World.Add(entity, default(RemotelyOwned));
            context.World.Add(entity, default(Interpolated));
            context.World.Add(entity, new InterpolationState());
            snapshotBuffers[entity] = new SnapshotBuffer();
        }
        else if (!wasLocal && willBeLocal)
        {
            // Transfer from remote to local
            context.World.Remove<RemotelyOwned>(entity);
            if (context.World.Has<Interpolated>(entity))
            {
                context.World.Remove<Interpolated>(entity);
            }
            if (context.World.Has<InterpolationState>(entity))
            {
                context.World.Remove<InterpolationState>(entity);
            }
            snapshotBuffers.Remove(entity);
            context.World.Add(entity, default(LocallyOwned));
            if (config.EnablePrediction)
            {
                context.World.Add(entity, default(Predicted));
                context.World.Add(entity, new PredictionState());
            }
        }

        // Notify listeners
        OwnershipChanged?.Invoke(entity, oldOwnerId, newOwnerId);
    }
}
