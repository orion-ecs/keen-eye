namespace KeenEyes.Network;

/// <summary>
/// Decides which networked entities are relevant to each connected client,
/// enabling area-of-interest filtering of server replication.
/// </summary>
/// <remarks>
/// <para>
/// When an interest manager is configured via
/// <see cref="ServerNetworkConfig.InterestManager"/>, the server maintains a
/// relevance set per client and only replicates entities inside that set.
/// An entity entering a client's scope is spawned on that client with its
/// full component state; an entity leaving scope is despawned on that client.
/// </para>
/// <para>
/// The server enforces one invariant on top of any manager: entities owned by
/// a client are always relevant to that client, regardless of the manager's
/// verdict. A manager can therefore never scope a client's own entities out.
/// </para>
/// <para>
/// The server calls <see cref="BeginUpdate"/> once per relevance update
/// (throttled to <see cref="UpdateFrequencyHz"/>), then calls
/// <see cref="IsRelevant"/> for each (client, entity) pair. Implementations
/// can use <see cref="BeginUpdate"/> to build acceleration structures so the
/// per-pair queries are cheap.
/// </para>
/// </remarks>
public interface IInterestManager
{
    /// <summary>
    /// Gets how often per-client relevance sets are recomputed, in updates per second.
    /// </summary>
    /// <remarks>
    /// Values less than or equal to zero recompute relevance on every network tick.
    /// Between recomputations the server replicates against the cached relevance sets.
    /// </remarks>
    float UpdateFrequencyHz { get; }

    /// <summary>
    /// Prepares the manager for a batch of <see cref="IsRelevant"/> queries.
    /// </summary>
    /// <param name="world">The authoritative server world.</param>
    /// <param name="clientIds">The IDs of all currently connected clients.</param>
    /// <remarks>
    /// Called once at the start of every relevance update, before any
    /// <see cref="IsRelevant"/> call of that update. Implementations should
    /// refresh any cached spatial structures or per-client viewpoints here.
    /// </remarks>
    void BeginUpdate(IWorld world, ReadOnlySpan<int> clientIds);

    /// <summary>
    /// Determines whether an entity is relevant to a client and should be
    /// replicated to it.
    /// </summary>
    /// <param name="world">The authoritative server world.</param>
    /// <param name="clientId">The client whose interest is being evaluated.</param>
    /// <param name="entity">The networked entity to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> to replicate the entity to the client;
    /// <see langword="false"/> to filter it out.
    /// </returns>
    bool IsRelevant(IWorld world, int clientId, Entity entity);
}
