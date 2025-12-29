using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Systems;

/// <summary>
/// Server system that sends state updates to clients.
/// </summary>
/// <remarks>
/// Runs in LateUpdate phase after game logic has executed.
/// </remarks>
public sealed class NetworkServerSendSystem(NetworkServerPlugin plugin) : SystemBase
{
    private readonly byte[] sendBuffer = new byte[4096];

    // Track last sent component values per entity for delta detection
    private readonly Dictionary<Entity, Dictionary<Type, object>> lastSentState = [];

    // Track bytes sent this tick for bandwidth limiting
    private int bytesSentThisTick;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Advance tick
        if (!plugin.Tick(deltaTime))
        {
            return; // Not time for a network tick yet
        }

        // Check for clients that need full snapshots
        foreach (var client in plugin.GetConnectedClients())
        {
            if (client.NeedsFullSnapshot)
            {
                plugin.SendFullSnapshot(client.ClientId);
                client.NeedsFullSnapshot = false;
            }
        }

        var serializer = plugin.Config.Serializer;
        var config = plugin.Config;

        // Calculate bytes per tick budget
        var bytesPerTick = config.EnableBandwidthLimiting
            ? config.MaxBandwidthBytesPerSecond / config.TickRate
            : int.MaxValue;
        bytesSentThisTick = 0;

        // Collect entities that need updates and sort by priority
        var entitiesToUpdate = new List<(Entity entity, float priority, bool needsFullSync)>();

        foreach (var entity in World.Query<NetworkId, NetworkState>())
        {
            ref var networkState = ref World.Get<NetworkState>(entity);

            // Accumulate priority over time
            networkState.AccumulatedPriority += deltaTime;

            if (ShouldSendEntity(entity, ref networkState, serializer))
            {
                entitiesToUpdate.Add((entity, networkState.AccumulatedPriority, networkState.NeedsFullSync));
            }
        }

        // Sort by priority (higher first), with full sync entities always at front
        entitiesToUpdate.Sort((a, b) =>
        {
            // Full sync entities have highest priority
            if (a.needsFullSync != b.needsFullSync)
            {
                return a.needsFullSync ? -1 : 1;
            }
            return b.priority.CompareTo(a.priority);
        });

        // Send entity updates within bandwidth budget
        foreach (var (entity, _, _) in entitiesToUpdate)
        {
            ref var networkState = ref World.Get<NetworkState>(entity);
            ref readonly var networkId = ref World.Get<NetworkId>(entity);

            // Check bandwidth budget
            if (config.EnableBandwidthLimiting && bytesSentThisTick >= bytesPerTick)
            {
                // Don't reset priority for entities we couldn't send
                break;
            }

            var bytesBefore = bytesSentThisTick;
            SendEntityUpdate(entity, networkId, ref networkState);
            networkState.LastSentTick = plugin.CurrentTick;
            networkState.AccumulatedPriority = 0; // Reset priority after sending

            // Stop if we've exceeded budget (but we already sent this message)
            if (config.EnableBandwidthLimiting && bytesSentThisTick > bytesPerTick)
            {
                break;
            }
        }

        // Pump the transport to flush outgoing data
        plugin.Transport.Update();
    }

    private bool ShouldSendEntity(Entity entity, ref NetworkState state, INetworkSerializer? serializer)
    {
        // Always send if needs full sync
        if (state.NeedsFullSync)
        {
            return true;
        }

        // If no serializer, we can only send spawn/despawn
        if (serializer is null)
        {
            return false;
        }

        // Check if any replicated component has changed
        if (!lastSentState.TryGetValue(entity, out var entityState))
        {
            // Never sent this entity - needs update
            return true;
        }

        // Compare current state to last sent state
        foreach (var (type, value) in World.GetComponents(entity))
        {
            if (!serializer.IsNetworkSerializable(type))
            {
                continue;
            }

            if (!entityState.TryGetValue(type, out var lastValue))
            {
                // New component - needs update
                return true;
            }

            // Compare values (simple equality check)
            if (!Equals(lastValue, value))
            {
                return true;
            }
        }

        return false;
    }

    private void SendEntityUpdate(Entity entity, NetworkId networkId, ref NetworkState state)
    {
        var writer = new NetworkMessageWriter(sendBuffer);
        var serializer = plugin.Config.Serializer;

        if (state.NeedsFullSync)
        {
            // Send full entity state
            writer.WriteHeader(MessageType.EntitySpawn, plugin.CurrentTick);

            var owner = World.Has<NetworkOwner>(entity)
                ? World.Get<NetworkOwner>(entity)
                : NetworkOwner.Server;

            writer.WriteEntitySpawn(networkId.Value, owner.ClientId);

            // Write all replicated components
            WriteReplicatedComponents(entity, ref writer, serializer, isDelta: false);

            state.NeedsFullSync = false;
        }
        else
        {
            // Send delta update (only changed components)
            writer.WriteHeader(MessageType.ComponentUpdate, plugin.CurrentTick);
            writer.WriteUInt32(networkId.Value);

            // Write only changed components
            WriteReplicatedComponents(entity, ref writer, serializer, isDelta: true);
        }

        var span = writer.GetWrittenSpan();
        bytesSentThisTick += span.Length;
        plugin.SendToAll(span, DeliveryMode.UnreliableSequenced);

        // Update last sent state for delta tracking
        SaveSentState(entity, serializer);
    }

    private void WriteReplicatedComponents(Entity entity, ref NetworkMessageWriter writer, INetworkSerializer? serializer, bool isDelta)
    {
        if (serializer is null)
        {
            // No serializer configured, write 0 components
            writer.WriteComponentCount(0);
            return;
        }

        // Get last sent state for delta comparison
        lastSentState.TryGetValue(entity, out var entityLastState);

        // Collect components to send
        var toSend = new List<(Type, object)>();
        foreach (var (type, value) in World.GetComponents(entity))
        {
            if (!serializer.IsNetworkSerializable(type))
            {
                continue;
            }

            // For delta updates, only send changed components
            if (isDelta && entityLastState is not null)
            {
                if (entityLastState.TryGetValue(type, out var lastValue) && Equals(lastValue, value))
                {
                    continue; // Component unchanged
                }
            }

            toSend.Add((type, value));
        }

        writer.WriteComponentCount((byte)toSend.Count);

        // Write each component
        foreach (var (type, value) in toSend)
        {
            writer.WriteComponent(serializer, type, value);
        }
    }

    private void SaveSentState(Entity entity, INetworkSerializer? serializer)
    {
        if (serializer is null)
        {
            return;
        }

        if (!lastSentState.TryGetValue(entity, out var entityState))
        {
            entityState = [];
            lastSentState[entity] = entityState;
        }

        // Save current state of all replicated components
        foreach (var (type, value) in World.GetComponents(entity))
        {
            if (serializer.IsNetworkSerializable(type))
            {
                // Store a copy of the value (boxing creates a copy for value types)
                entityState[type] = value;
            }
        }
    }

    /// <summary>
    /// Clears tracking state for an entity (call when entity is despawned).
    /// </summary>
    /// <param name="entity">The entity to clear.</param>
    public void ClearEntityState(Entity entity)
    {
        lastSentState.Remove(entity);
    }
}
