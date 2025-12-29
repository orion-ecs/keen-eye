using KeenEyes.Network.Components;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Protocol;
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
    private uint lastSentInputTick;

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
            if (inputBuffer is null || inputBuffer.NewestTick <= lastSentInputTick)
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

            lastSentInputTick = inputBuffer.NewestTick;
        }
    }
}
