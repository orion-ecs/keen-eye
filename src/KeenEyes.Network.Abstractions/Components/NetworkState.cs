namespace KeenEyes.Network.Components;

/// <summary>
/// Component that tracks network replication state for an entity.
/// </summary>
/// <remarks>
/// <para>
/// This component is managed by the network plugin and tracks when an entity
/// was last synchronized, enabling priority-based bandwidth allocation.
/// </para>
/// </remarks>
public record struct NetworkState : IComponent
{
    /// <summary>
    /// Gets or sets the server tick when this entity was last sent to clients.
    /// </summary>
    public uint LastSentTick { get; set; }

    /// <summary>
    /// Gets or sets the server tick when this entity was last received from server.
    /// </summary>
    public uint LastReceivedTick { get; set; }

    /// <summary>
    /// Gets or sets the accumulated priority for bandwidth allocation.
    /// </summary>
    /// <remarks>
    /// Priority accumulates over time since last send. Higher priority entities
    /// are sent first when bandwidth is limited.
    /// </remarks>
    public float AccumulatedPriority { get; set; }

    /// <summary>
    /// Gets or sets whether this entity needs a full sync (not delta).
    /// </summary>
    /// <remarks>
    /// Set to true when an entity is first replicated to a client, or when
    /// the client misses too many updates and needs a full state refresh.
    /// </remarks>
    public bool NeedsFullSync { get; set; }
}

/// <summary>
/// Component that stores interpolation state for smooth remote entity rendering.
/// </summary>
/// <remarks>
/// <para>
/// The network plugin maintains a buffer of recent snapshots. This component
/// tracks the current interpolation position between snapshots.
/// </para>
/// </remarks>
public record struct InterpolationState : IComponent
{
    /// <summary>
    /// Gets or sets the server time of the snapshot we're interpolating from.
    /// </summary>
    public double FromTime { get; set; }

    /// <summary>
    /// Gets or sets the server time of the snapshot we're interpolating to.
    /// </summary>
    public double ToTime { get; set; }

    /// <summary>
    /// Gets or sets the current interpolation factor (0 = from, 1 = to).
    /// </summary>
    public float Factor { get; set; }
}

/// <summary>
/// Component that stores prediction state for client-side prediction.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the last server-confirmed state and the sequence of inputs applied
/// since then, enabling rollback and replay on misprediction.
/// </para>
/// </remarks>
public record struct PredictionState : IComponent
{
    /// <summary>
    /// Gets or sets the last server-confirmed tick.
    /// </summary>
    public uint LastConfirmedTick { get; set; }

    /// <summary>
    /// Gets or sets the last predicted tick (client-side).
    /// </summary>
    public uint LastPredictedTick { get; set; }

    /// <summary>
    /// Gets or sets whether a misprediction was detected.
    /// </summary>
    public bool MispredictionDetected { get; set; }

    /// <summary>
    /// Gets or sets the magnitude of the last correction (for smoothing).
    /// </summary>
    public float LastCorrectionMagnitude { get; set; }
}
