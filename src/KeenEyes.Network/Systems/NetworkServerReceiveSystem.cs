namespace KeenEyes.Network.Systems;

/// <summary>
/// Server system that processes incoming network data.
/// </summary>
/// <remarks>
/// Runs in EarlyUpdate phase to process client inputs before game logic.
/// </remarks>
public sealed class NetworkServerReceiveSystem(NetworkServerPlugin plugin) : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Pump the transport to receive messages
        plugin.Transport.Update();
    }
}
