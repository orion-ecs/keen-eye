namespace KeenEyes.Network;

/// <summary>
/// Interest manager that treats every entity as relevant to every client.
/// </summary>
/// <remarks>
/// <para>
/// This provides the same visibility semantics as leaving
/// <see cref="ServerNetworkConfig.InterestManager"/> unset (every client sees
/// every entity), but exercises the per-client replication path: each client
/// gets its own dirty tracking, scope bookkeeping, and bandwidth budget.
/// Leave the config property <see langword="null"/> instead when the shared
/// broadcast path is sufficient.
/// </para>
/// </remarks>
public sealed class AlwaysRelevantInterestManager : IInterestManager
{
    /// <inheritdoc/>
    /// <remarks>
    /// Always zero: relevance never changes, so recomputing every tick is free.
    /// </remarks>
    public float UpdateFrequencyHz => 0f;

    /// <inheritdoc/>
    public void BeginUpdate(IWorld world, ReadOnlySpan<int> clientIds)
    {
        // No acceleration structures needed; every entity is always relevant.
    }

    /// <inheritdoc/>
    public bool IsRelevant(IWorld world, int clientId, Entity entity) => true;
}
