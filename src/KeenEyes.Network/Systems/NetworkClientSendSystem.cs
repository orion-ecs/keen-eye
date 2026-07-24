using KeenEyes.Capabilities;
using KeenEyes.Network.Components;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Replication;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Systems;

/// <summary>
/// Client system that sends input to the server.
/// </summary>
/// <remarks>
/// Runs in LateUpdate phase after local prediction.
/// </remarks>
public sealed class NetworkClientSendSystem(NetworkClientPlugin plugin) : SystemBase
{
    private readonly byte[] sendBuffer = new byte[1024];
    private float pingTimer;
    private const float PingInterval = 1.0f; // Send ping every second

    // High-water mark of the newest input tick already sent, tracked per predicted
    // entity. A single shared field skipped every predicted entity after the first one
    // reached a given tick, so only one entity's input ever reached the server (#1100).
    private readonly Dictionary<Entity, uint> lastSentInputTicks = [];

    // Owner-authoritative state tracking.
    private float ownerStateAccumulator;
    private OwnerAuthoritativeComponentSet? ownerAuthTypes;
    private INetworkSerializer? ownerAuthTypesSource;

    // Track last sent owner-authoritative component values per entity for dirty detection.
    private readonly Dictionary<Entity, Dictionary<Type, object>> lastSentOwnerState = [];

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (!plugin.IsConnected)
        {
            return;
        }

        // Send periodic ping for latency measurement
        pingTimer += deltaTime;
        if (pingTimer >= PingInterval)
        {
            pingTimer -= PingInterval;
            SendPing();
        }

        // Send input for predicted entities
        if (plugin.Config.EnablePrediction)
        {
            SendInput();
        }

        // Send owner-authoritative component state at the network tick cadence.
        SendOwnerState(deltaTime);

        // Pump the transport to flush outgoing data
        plugin.Transport.Update();
    }

    private void SendPing()
    {
        var writer = new NetworkMessageWriter(sendBuffer);
        writer.WriteHeader(MessageType.Ping, plugin.CurrentTick);
        plugin.SendToServer(writer.GetWrittenSpan(), DeliveryMode.Unreliable);
    }

    private void SendInput()
    {
        var inputSerializer = plugin.Config.InputSerializer;
        if (inputSerializer is null)
        {
            return;
        }

        // Find locally owned predicted entities and send their input
        foreach (var entity in World.Query<LocallyOwned, Predicted, NetworkId>())
        {
            var inputBuffer = plugin.GetInputBuffer(entity) as IInputBuffer;
            if (inputBuffer is null)
            {
                continue;
            }

            // Per-entity high-water mark: each predicted entity advances independently
            // so that two entities recording input for the same tick both send (#1100).
            lastSentInputTicks.TryGetValue(entity, out var lastSentInputTick);
            if (inputBuffer.NewestTick <= lastSentInputTick)
            {
                continue;
            }

            ref readonly var networkId = ref World.Get<NetworkId>(entity);

            // Send all unsent inputs (redundantly for reliability)
            // We send the last few inputs to handle packet loss
            var startTick = lastSentInputTick > 0 ? lastSentInputTick : inputBuffer.OldestTick;

            foreach (var input in inputBuffer.GetInputsFromBoxed(startTick))
            {
                var writer = new NetworkMessageWriter(sendBuffer);
                writer.WriteHeader(MessageType.ClientInput, plugin.CurrentTick);
                writer.WriteNetworkId(networkId.Value);
                writer.WriteInput(inputSerializer, input);

                plugin.SendToServer(writer.GetWrittenSpan(), DeliveryMode.UnreliableSequenced);
            }

            lastSentInputTicks[entity] = inputBuffer.NewestTick;
        }
    }

    private void SendOwnerState(float deltaTime)
    {
        var serializer = plugin.Config.Serializer;
        if (serializer is null)
        {
            return;
        }

        // Gate sends to the configured network tick cadence.
        var tickInterval = 1f / plugin.Config.TickRate;
        ownerStateAccumulator += deltaTime;
        if (ownerStateAccumulator < tickInterval)
        {
            return;
        }

        ownerStateAccumulator -= tickInterval;

        var ownerAuth = GetOwnerAuthTypes(serializer);
        if (World is not ISnapshotCapability snapshot)
        {
            return;
        }

        // Send owner-authoritative state for entities this client owns.
        foreach (var entity in World.Query<LocallyOwned, NetworkId>())
        {
            ref readonly var networkId = ref World.Get<NetworkId>(entity);
            SendEntityOwnerState(entity, networkId, snapshot, serializer, ownerAuth);
        }
    }

    private void SendEntityOwnerState(
        Entity entity,
        NetworkId networkId,
        ISnapshotCapability snapshot,
        INetworkSerializer serializer,
        OwnerAuthoritativeComponentSet ownerAuth)
    {
        // Collect owner-authoritative components whose value changed since the last send.
        lastSentOwnerState.TryGetValue(entity, out var entityLastState);

        var toSend = new List<(Type type, object value)>();
        foreach (var (type, value) in snapshot.GetComponents(entity))
        {
            if (!ownerAuth.Contains(type) || !serializer.IsNetworkSerializable(type))
            {
                continue;
            }

            if (!HasChanged(serializer, type, value, entityLastState))
            {
                continue;
            }

            toSend.Add((type, value));
        }

        if (toSend.Count == 0)
        {
            return;
        }

        var writer = new NetworkMessageWriter(sendBuffer);
        writer.WriteHeader(MessageType.OwnerStateUpdate, plugin.CurrentTick);
        writer.WriteNetworkId(networkId.Value);
        writer.WriteComponentCount((byte)toSend.Count);
        foreach (var (type, value) in toSend)
        {
            writer.WriteComponent(serializer, type, value);
        }

        plugin.SendToServer(writer.GetWrittenSpan(), DeliveryMode.UnreliableSequenced);

        // Record the sent state so unchanged components are not re-sent next tick.
        if (entityLastState is null)
        {
            entityLastState = [];
            lastSentOwnerState[entity] = entityLastState;
        }

        foreach (var (type, value) in toSend)
        {
            entityLastState[type] = value;
        }
    }

    private static bool HasChanged(
        INetworkSerializer serializer,
        Type type,
        object value,
        Dictionary<Type, object>? entityLastState)
    {
        if (entityLastState is null || !entityLastState.TryGetValue(type, out var lastValue))
        {
            return true;
        }

        if (serializer.SupportsDelta(type))
        {
            return serializer.GetDirtyMask(type, value, lastValue) != 0;
        }

        return !Equals(lastValue, value);
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
}
