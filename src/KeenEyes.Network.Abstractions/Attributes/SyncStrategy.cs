namespace KeenEyes.Network;

/// <summary>
/// Specifies how a replicated component should be synchronized between client and server.
/// </summary>
public enum SyncStrategy
{
    /// <summary>
    /// Server state is authoritative and applied directly.
    /// </summary>
    /// <remarks>
    /// Use for server-controlled entities like NPCs, world objects, or game state.
    /// No client prediction - updates are applied as received.
    /// </remarks>
    Authoritative = 0,

    /// <summary>
    /// Component values are interpolated between received states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use for remote player entities to achieve smooth visual movement.
    /// The client renders slightly behind server time (typically 100ms)
    /// and interpolates between received snapshots.
    /// </para>
    /// <para>
    /// Requires <see cref="ReplicatedAttribute.GenerateInterpolation"/> = true
    /// for the source generator to create interpolation helpers.
    /// </para>
    /// </remarks>
    Interpolated = 1,

    /// <summary>
    /// Client predicts state locally and reconciles with server corrections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use for the local player entity to provide responsive controls.
    /// The client runs simulation ahead of the server and corrects when
    /// server state diverges from predictions.
    /// </para>
    /// <para>
    /// Requires <see cref="ReplicatedAttribute.GeneratePrediction"/> = true
    /// for the source generator to create prediction/rollback helpers.
    /// </para>
    /// </remarks>
    Predicted = 2,

    /// <summary>
    /// Client owns state; server validates but doesn't override.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use for client-authoritative data like cosmetic choices or input state.
    /// The server receives updates from the owning client and may validate
    /// them, but doesn't send corrections.
    /// </para>
    /// <para>
    /// Be cautious with this strategy as it's vulnerable to cheating.
    /// Only use for non-gameplay-critical data.
    /// </para>
    /// </remarks>
    OwnerAuthoritative = 3,
}
