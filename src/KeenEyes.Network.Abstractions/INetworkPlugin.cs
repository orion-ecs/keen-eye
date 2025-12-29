using KeenEyes.Network.Prediction;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network;

/// <summary>
/// Interface for network plugins that provide multiplayer functionality.
/// </summary>
/// <remarks>
/// <para>
/// Network plugins handle entity replication, state synchronization, and
/// transport management. Different implementations may provide different
/// synchronization strategies (e.g., simple state sync vs full prediction).
/// </para>
/// <para>
/// This interface extends <see cref="IWorldPlugin"/> with network-specific
/// functionality, allowing plugin code to access network state and operations.
/// </para>
/// </remarks>
public interface INetworkPlugin : IWorldPlugin
{
    /// <summary>
    /// Gets the network transport used by this plugin.
    /// </summary>
    INetworkTransport Transport { get; }

    /// <summary>
    /// Gets whether this plugin is running as a server.
    /// </summary>
    bool IsServer { get; }

    /// <summary>
    /// Gets whether this plugin is running as a client.
    /// </summary>
    bool IsClient { get; }

    /// <summary>
    /// Gets the current network tick (server-authoritative frame number).
    /// </summary>
    uint CurrentTick { get; }

    /// <summary>
    /// Gets the local client ID (0 for server, 1+ for clients).
    /// </summary>
    int LocalClientId { get; }
}

/// <summary>
/// Configuration for network plugins.
/// </summary>
public record class NetworkPluginConfig
{
    /// <summary>
    /// Gets or sets the network tick rate (ticks per second).
    /// </summary>
    /// <remarks>
    /// Higher values provide more responsive networking but increase bandwidth.
    /// Typical values: 20-60 for action games, 10-20 for slower-paced games.
    /// </remarks>
    public int TickRate { get; set; } = 30;

    /// <summary>
    /// Gets or sets the interpolation delay in milliseconds.
    /// </summary>
    /// <remarks>
    /// Remote entities render this far behind server time for smooth interpolation.
    /// Higher values are smoother but increase perceived latency.
    /// </remarks>
    public float InterpolationDelayMs { get; set; } = 100f;

    /// <summary>
    /// Gets or sets the maximum prediction window in ticks.
    /// </summary>
    /// <remarks>
    /// How far ahead the client can predict. Larger values handle more latency
    /// but increase the cost of misprediction correction.
    /// </remarks>
    public int MaxPredictionTicks { get; set; } = 10;

    /// <summary>
    /// Gets or sets the snapshot buffer size for interpolation.
    /// </summary>
    public int SnapshotBufferSize { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether to enable bandwidth limiting.
    /// </summary>
    public bool EnableBandwidthLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum bandwidth in bytes per second.
    /// </summary>
    public int MaxBandwidthBytesPerSecond { get; set; } = 64 * 1024; // 64 KB/s

    /// <summary>
    /// Gets or sets the network serializer for replicated components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be set to an instance of the generated <c>NetworkSerializer</c>
    /// class from your application. The generator creates this class when you have
    /// components marked with <see cref="ReplicatedAttribute"/>.
    /// </para>
    /// <para>
    /// If null, component state will not be replicated (only entity spawn/despawn).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new ServerNetworkConfig
    /// {
    ///     Serializer = new NetworkSerializer() // Generated class
    /// };
    /// </code>
    /// </example>
    public INetworkSerializer? Serializer { get; set; }

    /// <summary>
    /// Gets or sets the network interpolator for smooth remote entity rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be set to an instance of the generated <c>NetworkInterpolator</c>
    /// class from your application. The generator creates this class when you have
    /// components marked with <see cref="ReplicatedAttribute"/> with
    /// <c>GenerateInterpolation = true</c>.
    /// </para>
    /// <para>
    /// If null, interpolation will not be applied (remote entities may stutter).
    /// </para>
    /// </remarks>
    public INetworkInterpolator? Interpolator { get; set; }
}

/// <summary>
/// Server-specific network plugin configuration.
/// </summary>
public record class ServerNetworkConfig : NetworkPluginConfig
{
    /// <summary>
    /// Gets or sets the port to listen on.
    /// </summary>
    public int Port { get; set; } = 7777;

    /// <summary>
    /// Gets or sets the maximum number of clients.
    /// </summary>
    public int MaxClients { get; set; } = 16;
}

/// <summary>
/// Client-specific network plugin configuration.
/// </summary>
public record class ClientNetworkConfig : NetworkPluginConfig
{
    /// <summary>
    /// Gets or sets the server address to connect to.
    /// </summary>
    public string ServerAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the server port to connect to.
    /// </summary>
    public int ServerPort { get; set; } = 7777;

    /// <summary>
    /// Gets or sets whether to enable client-side prediction.
    /// </summary>
    public bool EnablePrediction { get; set; } = true;

    /// <summary>
    /// Gets or sets the input serializer for client-side prediction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be set to a game-specific input serializer that handles
    /// packing player inputs for network transmission.
    /// </para>
    /// <para>
    /// If null and prediction is enabled, inputs will not be sent to the server.
    /// </para>
    /// </remarks>
    public IInputSerializer? InputSerializer { get; set; }

    /// <summary>
    /// Gets or sets the size of the input buffer for prediction replay.
    /// </summary>
    /// <remarks>
    /// Larger values support higher latency but use more memory.
    /// </remarks>
    public int InputBufferSize { get; set; } = 64;

    /// <summary>
    /// Gets or sets the misprediction threshold.
    /// </summary>
    /// <remarks>
    /// If the difference between predicted and server state exceeds this
    /// threshold, reconciliation is triggered. Set to 0 for exact comparison.
    /// </remarks>
    public float MispredictionThreshold { get; set; } = 0.01f;

    /// <summary>
    /// Gets or sets the input applicator for prediction replay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This callback is invoked during reconciliation to replay inputs.
    /// The first parameter is the entity, the second is the input (boxed).
    /// </para>
    /// <para>
    /// If null, prediction will detect mispredictions but cannot replay inputs.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// config.InputApplicator = (entity, input) =>
    /// {
    ///     if (input is PlayerInput playerInput)
    ///     {
    ///         ApplyPlayerMovement(entity, playerInput);
    ///     }
    /// };
    /// </code>
    /// </example>
    public Action<Entity, object>? InputApplicator { get; set; }
}
