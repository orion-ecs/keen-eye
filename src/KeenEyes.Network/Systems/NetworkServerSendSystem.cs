using System.Runtime.InteropServices;
using KeenEyes.Capabilities;
using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Replication;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Systems;

/// <summary>
/// Server system that sends state updates to clients.
/// </summary>
/// <remarks>
/// <para>
/// Runs in LateUpdate phase after game logic has executed.
/// </para>
/// <para>
/// Without an interest manager configured, updates are built once per tick and
/// broadcast to all clients. With <see cref="ServerNetworkConfig.InterestManager"/>
/// set, replication is per client: each client has its own relevance set, dirty
/// tracking baseline, and bandwidth budget, with scope enter/exit translated
/// into targeted entity spawn/despawn messages.
/// </para>
/// </remarks>
public sealed class NetworkServerSendSystem(NetworkServerPlugin plugin) : SystemBase
{
    private readonly byte[] sendBuffer = new byte[4096];

    // Track last sent component values per entity for delta detection (broadcast path).
    private readonly Dictionary<Entity, Dictionary<Type, object>> lastSentState = [];

    // Track bytes sent this tick for bandwidth limiting
    private int bytesSentThisTick;

    // Cached owner-authoritative strategy lookup (rebuilt if the serializer changes).
    private OwnerAuthoritativeComponentSet? ownerAuthTypes;
    private INetworkSerializer? ownerAuthTypesSource;

    // Pre-allocated list to avoid per-tick allocations
    private readonly List<(Entity entity, float priority, bool needsFullSync)> entitiesToUpdate = [];

    // Interest management state (only used when an interest manager is configured).
    private float interestAccumulator;
    private readonly List<int> clientIdScratch = [];
    private readonly HashSet<Entity> relevanceScratch = [];
    private readonly List<Entity> scopeExitScratch = [];
    private readonly HashSet<Entity> sentEntitiesScratch = [];

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Time toward the next relevance recomputation passes regardless of
        // whether this frame lands on a network tick.
        interestAccumulator += deltaTime;

        // Advance tick
        if (!plugin.Tick(deltaTime))
        {
            return; // Not time for a network tick yet
        }

        if (plugin.Config.InterestManager is { } interestManager)
        {
            UpdateFiltered(deltaTime, interestManager);
        }
        else
        {
            UpdateBroadcast(deltaTime);
        }
    }

    #region Broadcast path (no interest manager)

    private void UpdateBroadcast(float deltaTime)
    {
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
        entitiesToUpdate.Clear();

        // Capture authoritative state for lag compensation once per network tick.
        // This reuses the same iteration over networked entities and records every
        // entity (not just those sent this tick), so history stays complete even for
        // entities that did not change or were dropped by the bandwidth budget.
        var history = plugin.StateHistory;

        foreach (var entity in World.Query<NetworkId, NetworkState>())
        {
            ref var networkState = ref World.Get<NetworkState>(entity);

            // Accumulate priority over time
            networkState.AccumulatedPriority += deltaTime;

            if (history is not null && serializer is not null && World is ISnapshotCapability snapshot)
            {
                history.Capture(entity, plugin.CurrentTick, snapshot, serializer);
            }

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
        if (World is ISnapshotCapability snapshot)
        {
            foreach (var (type, value) in snapshot.GetComponents(entity))
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
        }

        return false;
    }

    private void SendEntityUpdate(Entity entity, NetworkId networkId, ref NetworkState state)
    {
        var serializer = plugin.Config.Serializer;

        if (state.NeedsFullSync)
        {
            // Send full entity state. The full snapshot is always broadcast to every
            // client (including a client owner) because it carries the entity's initial
            // component values; the owner immediately overrides its owner-authoritative
            // components with its own upstream state.
            var writer = new NetworkMessageWriter(sendBuffer);
            writer.WriteHeader(MessageType.EntitySpawn, plugin.CurrentTick);

            var owner = World.Has<NetworkOwner>(entity)
                ? World.Get<NetworkOwner>(entity)
                : NetworkOwner.Server;

            writer.WriteEntitySpawn(networkId.Value, owner.ClientId);

            // Write all replicated components (full serialization)
            WriteReplicatedComponentsFull(entity, ref writer, serializer);

            state.NeedsFullSync = false;

            var span = writer.GetWrittenSpan();
            bytesSentThisTick += span.Length;
            plugin.SendToAll(span, DeliveryMode.UnreliableSequenced);
        }
        else
        {
            SendDeltaUpdate(entity, networkId, serializer);
        }

        // Update last sent state for delta tracking
        SaveSentState(entity, serializer);
    }

    private void SendDeltaUpdate(Entity entity, NetworkId networkId, INetworkSerializer? serializer)
    {
        var owner = World.Has<NetworkOwner>(entity)
            ? World.Get<NetworkOwner>(entity)
            : NetworkOwner.Server;

        // Echo suppression: for a client-owned entity, owner-authoritative components
        // must not be sent back to the owner (its own state is authoritative). Other
        // components (server-authoritative, predicted, interpolated) still reach the
        // owner so reconciliation and server updates work. Server-owned entities have
        // no client owner to echo to and use the standard single broadcast.
        if (serializer is not null && owner.ClientId != NetworkOwner.ServerClientId)
        {
            SendClientOwnedDelta(entity, networkId, owner.ClientId, serializer);
            return;
        }

        var writer = new NetworkMessageWriter(sendBuffer);
        writer.WriteHeader(MessageType.ComponentDelta, plugin.CurrentTick);
        writer.WriteUInt32(networkId.Value);
        WriteReplicatedComponentsDelta(entity, ref writer, serializer);

        var span = writer.GetWrittenSpan();
        bytesSentThisTick += span.Length;
        plugin.SendToAll(span, DeliveryMode.UnreliableSequenced);
    }

    private void SendClientOwnedDelta(Entity entity, NetworkId networkId, int ownerId, INetworkSerializer serializer)
    {
        var ownerAuth = GetOwnerAuthTypes(serializer);
        var changed = CollectChangedComponents(entity, serializer, lastSentState);

        var toOwnerAndOthers = new List<(Type type, object current, object? baseline)>();
        var toOthersOnly = new List<(Type type, object current, object? baseline)>();
        foreach (var component in changed)
        {
            if (ownerAuth.Contains(component.type))
            {
                toOthersOnly.Add(component);
            }
            else
            {
                toOwnerAndOthers.Add(component);
            }
        }

        // Owner-authoritative components: relay to every client except the owner.
        if (toOthersOnly.Count > 0)
        {
            var span = WriteDeltaMessage(networkId, toOthersOnly, serializer);
            bytesSentThisTick += span.Length;
            plugin.SendToAllExcept(ownerId, span, DeliveryMode.UnreliableSequenced);
        }

        // Remaining components: broadcast to all clients including the owner.
        if (toOwnerAndOthers.Count > 0)
        {
            var span = WriteDeltaMessage(networkId, toOwnerAndOthers, serializer);
            bytesSentThisTick += span.Length;
            plugin.SendToAll(span, DeliveryMode.UnreliableSequenced);
        }
    }

    #endregion

    #region Filtered path (interest manager configured)

    private void UpdateFiltered(float deltaTime, IInterestManager interestManager)
    {
        var serializer = plugin.Config.Serializer;
        var config = plugin.Config;

        // The bandwidth budget applies per client in filtered mode.
        var bytesPerTick = config.EnableBandwidthLimiting
            ? config.MaxBandwidthBytesPerSecond / config.TickRate
            : int.MaxValue;

        // Late joiners are synchronized through scope entry (a targeted
        // EntitySpawn per relevant entity) instead of the broadcast full
        // snapshot, which would leak out-of-scope entities.
        foreach (var client in plugin.GetConnectedClients())
        {
            client.NeedsFullSnapshot = false;
        }

        // Capture lag-compensation history and accumulate priority exactly as
        // the broadcast path does, reusing the same single iteration.
        var history = plugin.StateHistory;
        entitiesToUpdate.Clear();

        foreach (var entity in World.Query<NetworkId, NetworkState>())
        {
            ref var networkState = ref World.Get<NetworkState>(entity);
            networkState.AccumulatedPriority += deltaTime;

            if (history is not null && serializer is not null && World is ISnapshotCapability snapshot)
            {
                history.Capture(entity, plugin.CurrentTick, snapshot, serializer);
            }

            entitiesToUpdate.Add((entity, networkState.AccumulatedPriority, false));
        }

        RecomputeRelevanceIfDue(interestManager);

        // Sort by priority (higher first). Per-client full syncs are implied by a
        // missing per-client baseline, so no separate full-sync ordering is needed.
        entitiesToUpdate.Sort(static (a, b) => b.priority.CompareTo(a.priority));

        sentEntitiesScratch.Clear();
        foreach (var client in plugin.GetConnectedClients())
        {
            SendUpdatesToClient(client, serializer, bytesPerTick);
        }

        // Reset priority for entities that produced at least one message this tick.
        foreach (var entity in sentEntitiesScratch)
        {
            ref var networkState = ref World.Get<NetworkState>(entity);
            networkState.LastSentTick = plugin.CurrentTick;
            networkState.AccumulatedPriority = 0;
            networkState.NeedsFullSync = false;
        }

        // Pump the transport to flush outgoing data
        plugin.Transport.Update();
    }

    private void RecomputeRelevanceIfDue(IInterestManager interestManager)
    {
        var due = interestManager.UpdateFrequencyHz <= 0f
            || interestAccumulator >= 1f / interestManager.UpdateFrequencyHz;

        if (!due)
        {
            // New clients get an immediate relevance set so initial replication
            // does not wait for the next scheduled update.
            foreach (var client in plugin.GetConnectedClients())
            {
                if (!client.InterestInitialized)
                {
                    due = true;
                    break;
                }
            }
        }

        if (!due)
        {
            return;
        }

        interestAccumulator = 0f;

        clientIdScratch.Clear();
        foreach (var client in plugin.GetConnectedClients())
        {
            clientIdScratch.Add(client.ClientId);
        }

        interestManager.BeginUpdate(World, CollectionsMarshal.AsSpan(clientIdScratch));

        foreach (var client in plugin.GetConnectedClients())
        {
            RecomputeClientRelevance(client, interestManager);
        }
    }

    private void RecomputeClientRelevance(ClientState client, IInterestManager interestManager)
    {
        relevanceScratch.Clear();

        foreach (var (entity, _, _) in entitiesToUpdate)
        {
            var ownerId = World.Has<NetworkOwner>(entity)
                ? World.Get<NetworkOwner>(entity).ClientId
                : NetworkOwner.ServerClientId;

            // Invariant: a client's own entities are always relevant to it,
            // regardless of the interest manager's verdict.
            if (ownerId == client.ClientId || interestManager.IsRelevant(World, client.ClientId, entity))
            {
                relevanceScratch.Add(entity);
            }
        }

        // Entities leaving scope are despawned on this client. Entities entering
        // scope need no explicit action: having no per-client baseline, they get
        // a full EntitySpawn in the send phase.
        scopeExitScratch.Clear();
        foreach (var entity in client.RelevantEntities)
        {
            if (!relevanceScratch.Contains(entity))
            {
                scopeExitScratch.Add(entity);
            }
        }

        foreach (var entity in scopeExitScratch)
        {
            SendScopeExit(client, entity);
        }

        client.RelevantEntities.Clear();
        client.RelevantEntities.UnionWith(relevanceScratch);
        client.InterestInitialized = true;
    }

    private void SendScopeExit(ClientState client, Entity entity)
    {
        // Drop the per-client baseline so a later re-entry re-sends full state.
        client.LastSentState.Remove(entity);

        if (!plugin.NetworkIds.TryGetNetworkId(entity, out var networkId))
        {
            // Entity was destroyed; the plugin already broadcast its despawn.
            return;
        }

        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.EntityDespawn, plugin.CurrentTick);
        writer.WriteEntityDespawn(networkId.Value);
        plugin.SendToClient(client.ClientId, writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
    }

    private void SendUpdatesToClient(ClientState client, INetworkSerializer? serializer, int bytesPerTick)
    {
        var limiting = plugin.Config.EnableBandwidthLimiting;
        var bytesSent = 0;

        foreach (var (entity, _, _) in entitiesToUpdate)
        {
            // Per-client bandwidth budget: stop sending to this client once
            // exhausted; unsent changes remain dirty against this client's
            // baseline and go out on later ticks.
            if (limiting && bytesSent >= bytesPerTick)
            {
                break;
            }

            if (!client.RelevantEntities.Contains(entity))
            {
                continue;
            }

            // No baseline means the entity is new to this client (scope entry,
            // registration, or re-entry after exit): send a full spawn.
            var sent = client.LastSentState.ContainsKey(entity)
                ? SendDeltaToClient(client, entity, serializer)
                : SendSpawnToClient(client, entity, serializer);

            if (sent > 0)
            {
                bytesSent += sent;
                sentEntitiesScratch.Add(entity);
            }
        }
    }

    private int SendSpawnToClient(ClientState client, Entity entity, INetworkSerializer? serializer)
    {
        ref readonly var networkId = ref World.Get<NetworkId>(entity);

        var owner = World.Has<NetworkOwner>(entity)
            ? World.Get<NetworkOwner>(entity)
            : NetworkOwner.Server;

        var writer = new NetworkMessageWriter(sendBuffer);
        writer.WriteHeader(MessageType.EntitySpawn, plugin.CurrentTick);
        writer.WriteEntitySpawn(networkId.Value, owner.ClientId);
        WriteReplicatedComponentsFull(entity, ref writer, serializer);

        var span = writer.GetWrittenSpan();

        // Scope transitions must arrive: a dropped spawn would leave the entity
        // invisible to this client until it re-enters scope.
        plugin.SendToClient(client.ClientId, span, DeliveryMode.ReliableOrdered);

        SaveSentStateForClient(client, entity, serializer);
        return span.Length;
    }

    private int SendDeltaToClient(ClientState client, Entity entity, INetworkSerializer? serializer)
    {
        if (serializer is null)
        {
            return 0; // Without a serializer only spawn/despawn replicate.
        }

        var changed = CollectChangedComponents(entity, serializer, client.LastSentState);
        if (changed.Count == 0)
        {
            return 0;
        }

        var owner = World.Has<NetworkOwner>(entity)
            ? World.Get<NetworkOwner>(entity)
            : NetworkOwner.Server;

        // Echo suppression: owner-authoritative components are never sent back
        // to the owning client (its own state is authoritative for them).
        if (owner.ClientId == client.ClientId && owner.ClientId != NetworkOwner.ServerClientId)
        {
            var ownerAuth = GetOwnerAuthTypes(serializer);
            changed.RemoveAll(component => ownerAuth.Contains(component.type));
            if (changed.Count == 0)
            {
                return 0;
            }
        }

        ref readonly var networkId = ref World.Get<NetworkId>(entity);
        var span = WriteDeltaMessage(networkId, changed, serializer);
        plugin.SendToClient(client.ClientId, span, DeliveryMode.UnreliableSequenced);

        SaveSentStateForClient(client, entity, serializer);
        return span.Length;
    }

    private void SaveSentStateForClient(ClientState client, Entity entity, INetworkSerializer? serializer)
    {
        if (!client.LastSentState.TryGetValue(entity, out var entityState))
        {
            // Record the entity even without a serializer so the spawn is not resent.
            entityState = [];
            client.LastSentState[entity] = entityState;
        }

        if (serializer is null)
        {
            return;
        }

        if (World is ISnapshotCapability snapshot)
        {
            foreach (var (type, value) in snapshot.GetComponents(entity))
            {
                if (serializer.IsNetworkSerializable(type))
                {
                    entityState[type] = value;
                }
            }
        }
    }

    #endregion

    private ReadOnlySpan<byte> WriteDeltaMessage(
        NetworkId networkId,
        List<(Type type, object current, object? baseline)> components,
        INetworkSerializer serializer)
    {
        var writer = new NetworkMessageWriter(sendBuffer);
        writer.WriteHeader(MessageType.ComponentDelta, plugin.CurrentTick);
        writer.WriteUInt32(networkId.Value);
        WriteDeltaComponents(ref writer, components, serializer);
        return writer.GetWrittenSpan();
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
        if (World is ISnapshotCapability snapshot)
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

    private void WriteReplicatedComponentsDelta(Entity entity, ref NetworkMessageWriter writer, INetworkSerializer? serializer)
    {
        if (serializer is null)
        {
            writer.WriteComponentCount(0);
            return;
        }

        var toSend = CollectChangedComponents(entity, serializer, lastSentState);
        WriteDeltaComponents(ref writer, toSend, serializer);
    }

    private List<(Type type, object current, object? baseline)> CollectChangedComponents(
        Entity entity,
        INetworkSerializer serializer,
        Dictionary<Entity, Dictionary<Type, object>> sentStates)
    {
        // Get last sent state for delta comparison
        sentStates.TryGetValue(entity, out var entityLastState);

        // Collect components that have changed
        var toSend = new List<(Type type, object current, object? baseline)>();
        if (World is ISnapshotCapability snapshot)
        {
            foreach (var (type, value) in snapshot.GetComponents(entity))
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
        }

        return toSend;
    }

    private static void WriteDeltaComponents(
        ref NetworkMessageWriter writer,
        List<(Type type, object current, object? baseline)> components,
        INetworkSerializer serializer)
    {
        writer.WriteComponentCount((byte)components.Count);

        // Write each component with delta encoding where supported
        foreach (var (type, current, baseline) in components)
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

    private OwnerAuthoritativeComponentSet GetOwnerAuthTypes(INetworkSerializer serializer)
    {
        if (ownerAuthTypes is null || !ReferenceEquals(ownerAuthTypesSource, serializer))
        {
            ownerAuthTypes = new OwnerAuthoritativeComponentSet(serializer);
            ownerAuthTypesSource = serializer;
        }

        return ownerAuthTypes;
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
        if (World is ISnapshotCapability snapshot)
        {
            foreach (var (type, value) in snapshot.GetComponents(entity))
            {
                if (serializer.IsNetworkSerializable(type))
                {
                    // Store a copy of the value (boxing creates a copy for value types)
                    entityState[type] = value;
                }
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

        foreach (var client in plugin.GetConnectedClients())
        {
            client.LastSentState.Remove(entity);
            client.RelevantEntities.Remove(entity);
        }
    }
}
