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

    /// <summary>
    /// Gets or sets whether smoothing interpolation is available.
    /// </summary>
    public bool SmoothingAvailable { get; set; }
}

/// <summary>
/// Stores component snapshots for interpolation.
/// </summary>
/// <remarks>
/// <para>
/// When component updates are received from the server, the previous state becomes
/// the "from" snapshot and the new state becomes the "to" snapshot. The
/// InterpolationSystem interpolates between these snapshots.
/// </para>
/// <para>
/// This is not an ECS component - it's stored in the network plugin and looked up
/// by entity ID since it contains reference types that can't be stored in a struct.
/// </para>
/// </remarks>
public sealed class SnapshotBuffer
{
    /// <summary>
    /// Gets the "from" snapshots (previous state) keyed by component type.
    /// </summary>
    public Dictionary<Type, object> FromSnapshots { get; } = [];

    /// <summary>
    /// Gets the "to" snapshots (target state) keyed by component type.
    /// </summary>
    public Dictionary<Type, object> ToSnapshots { get; } = [];

    /// <summary>
    /// Updates the snapshot buffer with a new component value.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <param name="newValue">The new component value.</param>
    /// <remarks>
    /// The current "to" snapshot becomes the new "from" snapshot,
    /// and the new value becomes the new "to" snapshot.
    /// </remarks>
    public void PushSnapshot(Type componentType, object newValue)
    {
        if (ToSnapshots.TryGetValue(componentType, out var previousTo))
        {
            FromSnapshots[componentType] = previousTo;
        }
        ToSnapshots[componentType] = newValue;
    }

    /// <summary>
    /// Clears all stored snapshots.
    /// </summary>
    public void Clear()
    {
        FromSnapshots.Clear();
        ToSnapshots.Clear();
    }
}
