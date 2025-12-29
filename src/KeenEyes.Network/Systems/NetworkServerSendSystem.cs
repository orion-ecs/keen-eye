using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
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

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Advance tick
        if (!plugin.Tick(deltaTime))
        {
            return; // Not time for a network tick yet
        }

        // Send entity updates to clients
        foreach (var entity in World.Query<NetworkId, NetworkState>())
        {
            ref var networkState = ref World.Get<NetworkState>(entity);
            ref readonly var networkId = ref World.Get<NetworkId>(entity);

            // Check if entity needs to be sent this tick
            if (ShouldSendEntity(ref networkState))
            {
                SendEntityUpdate(entity, networkId, ref networkState);
                networkState.LastSentTick = plugin.CurrentTick;
            }
        }

        // Pump the transport to flush outgoing data
        plugin.Transport.Update();
    }

    private static bool ShouldSendEntity(ref NetworkState state)
    {
        // Always send if needs full sync
        if (state.NeedsFullSync)
        {
            return true;
        }

        // Check if entity is dirty (changed since last send)
        // TODO: Check ChangeTracker for dirty components
        return true; // For now, send every tick
    }

    private void SendEntityUpdate(Entity entity, NetworkId networkId, ref NetworkState state)
    {
        var writer = new NetworkMessageWriter(sendBuffer);

        if (state.NeedsFullSync)
        {
            // Send full entity state
            writer.WriteHeader(MessageType.EntitySpawn, plugin.CurrentTick);

            var owner = World.Has<NetworkOwner>(entity)
                ? World.Get<NetworkOwner>(entity)
                : NetworkOwner.Server;

            writer.WriteEntitySpawn(networkId.Value, owner.ClientId);

            // TODO: Write all replicated components

            state.NeedsFullSync = false;
        }
        else
        {
            // Send delta update
            writer.WriteHeader(MessageType.ComponentUpdate, plugin.CurrentTick);
            writer.WriteUInt32(networkId.Value);

            // TODO: Write dirty components only
        }

        plugin.SendToAll(writer.GetWrittenSpan(), DeliveryMode.UnreliableSequenced);
    }
}
