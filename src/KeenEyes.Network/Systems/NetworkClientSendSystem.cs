using KeenEyes.Network.Components;
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
        // Find locally owned predicted entities and send their input
        foreach (var _ in World.Query<LocallyOwned, Predicted>())
        {
            // TODO: Gather and send input for this entity
            // This would be game-specific input like movement, actions, etc.
        }
    }
}
