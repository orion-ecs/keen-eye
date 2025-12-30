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

        // Compare current state to last sent state using delta masks for efficiency
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

            // Use dirty mask for delta-supported types, fallback to Equals for others
            if (serializer.SupportsDelta(type))
            {
                if (serializer.GetDirtyMask(type, value, lastValue) != 0)
                {
                    return true;
                }
            }
            else if (!Equals(lastValue, value))
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

            // Write all replicated components (full serialization)
            WriteReplicatedComponentsFull(entity, ref writer, serializer);

            state.NeedsFullSync = false;
        }
        else
        {
            // Send delta update (only changed fields within components)
            writer.WriteHeader(MessageType.ComponentDelta, plugin.CurrentTick);
            writer.WriteUInt32(networkId.Value);

            // Write components with delta encoding
            WriteReplicatedComponentsDelta(entity, ref writer, serializer);
        }

        var span = writer.GetWrittenSpan();
        bytesSentThisTick += span.Length;
        plugin.SendToAll(span, DeliveryMode.UnreliableSequenced);

        // Update last sent state for delta tracking
        SaveSentState(entity, serializer);
    }

    private void WriteReplicatedComponentsFull(Entity entity, ref NetworkMessageWriter writer, INetworkSerializer? serializer)
    {
        if (serializer is null)
        {
            writer.WriteComponentCount(0);
            return;
        }

        // Collect all replicated components
        var toSend = new List<(Type, object)>();
        foreach (var (type, value) in World.GetComponents(entity))
        {
            if (serializer.IsNetworkSerializable(type))
            {
                toSend.Add((type, value));
            }
        }

        writer.WriteComponentCount((byte)toSend.Count);
        foreach (var (type, value) in toSend)
        {
            writer.WriteComponent(serializer, type, value);
        }
    }

    private void WriteReplicatedComponentsDelta(Entity entity, ref NetworkMessageWriter writer, INetworkSerializer? serializer)
    {
        if (serializer is null)
        {
            writer.WriteComponentCount(0);
            return;
        }

        // Get last sent state for delta comparison
        lastSentState.TryGetValue(entity, out var entityLastState);

        // Collect components that have changed
        var toSend = new List<(Type type, object current, object? baseline)>();
        foreach (var (type, value) in World.GetComponents(entity))
        {
            if (!serializer.IsNetworkSerializable(type))
            {
                continue;
            }

            object? lastValue = null;
            if (entityLastState is not null)
            {
                entityLastState.TryGetValue(type, out lastValue);
            }

            // Check if changed using delta mask or equality
            bool hasChanged;
            if (lastValue is null)
            {
                hasChanged = true; // New component
            }
            else if (serializer.SupportsDelta(type))
            {
                hasChanged = serializer.GetDirtyMask(type, value, lastValue) != 0;
            }
            else
            {
                hasChanged = !Equals(lastValue, value);
            }

            if (hasChanged)
            {
                toSend.Add((type, value, lastValue));
            }
        }

        writer.WriteComponentCount((byte)toSend.Count);

        // Write each component with delta encoding where supported
        foreach (var (type, current, baseline) in toSend)
        {
            // Use delta serialization if we have a baseline and the type supports it
            if (baseline is not null && serializer.SupportsDelta(type))
            {
                writer.WriteComponentDelta(serializer, type, current, baseline);
            }
            else
            {
                // Fall back to full serialization
                writer.WriteComponent(serializer, type, current);
            }
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
