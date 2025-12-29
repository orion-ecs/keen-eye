namespace KeenEyes.Network.Systems;

/// <summary>
/// Client system that processes incoming network data from server.
/// </summary>
/// <remarks>
/// Runs in EarlyUpdate phase to receive state before game logic.
/// </remarks>
public sealed class NetworkClientReceiveSystem(NetworkClientPlugin plugin) : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Pump the transport to receive messages
        plugin.Transport.Update();
    }
}
