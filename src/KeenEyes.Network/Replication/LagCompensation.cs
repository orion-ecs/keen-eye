using KeenEyes.Capabilities;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Replication;

/// <summary>
/// Server-side lag compensation: estimates the tick a client acted on and rewinds
/// authoritative entity state to that tick for hit testing.
/// </summary>
/// <remarks>
/// <para>
/// Players aim at where they see targets, but network latency means those targets have
/// already moved by the time the action reaches the server. This helper reconstructs the
/// state the acting client actually saw — from the tick-indexed <see cref="ServerStateHistory"/>
/// captured each network tick — so interactions resolve fairly.
/// </para>
/// <para>
/// It is obtained from <see cref="NetworkServerPlugin.LagCompensation"/> and is available only
/// when state history is enabled. It reads the plugin's current tick and per-client round-trip
/// time live, so estimates track the latest measurements.
/// </para>
/// </remarks>
public sealed class LagCompensation
{
    private readonly NetworkServerPlugin server;
    private readonly IWorld world;
    private readonly ISnapshotCapability snapshot;
    private readonly ServerStateHistory history;
    private readonly ServerNetworkConfig config;

    private static readonly IReadOnlyDictionary<Type, object> emptyState = new Dictionary<Type, object>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LagCompensation"/> class.
    /// </summary>
    /// <param name="server">The owning server plugin, queried for current tick and client RTT.</param>
    /// <param name="world">The live world whose components rewinding swaps.</param>
    /// <param name="history">The tick-indexed authoritative state history to rewind through.</param>
    /// <param name="config">The server configuration providing tick rate, interpolation delay, and interpolator.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="world"/> does not support snapshot access.</exception>
    internal LagCompensation(NetworkServerPlugin server, IWorld world, ServerStateHistory history, ServerNetworkConfig config)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(config);

        if (world is not ISnapshotCapability snapshotCapability)
        {
            throw new ArgumentException("World must support snapshot access for lag compensation.", nameof(world));
        }

        this.server = server;
        this.world = world;
        snapshot = snapshotCapability;
        this.history = history;
        this.config = config;
    }

    /// <summary>
    /// Estimates the network tick whose state a client perceived when it acted.
    /// </summary>
    /// <param name="clientId">The acting client.</param>
    /// <returns>
    /// The estimated perceived tick, clamped into the retained history window
    /// <c>[currentTick - (Capacity - 1), currentTick]</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The estimate walks back from the server's current tick by two latency terms:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// <b>One-way latency</b> — half the client's round-trip time
    /// (<see cref="NetworkServerPlugin.GetClientRoundTripTimeMs"/>). Only the upstream leg
    /// matters: the action was sampled that long before it arrived. Converted to ticks by
    /// <c>rttMs / 2 * TickRate / 1000</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Interpolation delay</b> — clients render remote entities
    /// <see cref="NetworkPluginConfig.InterpolationDelayMs"/> behind server time for smooth
    /// motion, so the client aimed at state that much further in the past. Converted to ticks
    /// by <c>InterpolationDelayMs * TickRate / 1000</c>.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// The two terms are summed, rounded to whole ticks, and subtracted from the current tick.
    /// The result is clamped into the retained window: it can never predate the oldest recorded
    /// tick (nothing older survives to rewind to) nor exceed the current tick. Clamping also
    /// caps how far a client can rewind, which bounds the anti-cheat surface.
    /// </para>
    /// </remarks>
    public uint EstimateClientPerceivedTick(int clientId)
    {
        var currentTick = server.CurrentTick;
        var tickRate = config.TickRate;

        // One-way (upstream) latency in ticks: half the round trip, scaled from ms to ticks.
        var oneWayLatencyTicks = server.GetClientRoundTripTimeMs(clientId) * 0.5f * tickRate / 1000f;

        // Interpolation delay the client renders behind, in ticks.
        var interpolationDelayTicks = config.InterpolationDelayMs * tickRate / 1000f;

        var offsetTicks = (uint)MathF.Round(oneWayLatencyTicks + interpolationDelayTicks);

        // Walk back from the current tick, guarding against unsigned underflow.
        var perceived = offsetTicks >= currentTick ? 0u : currentTick - offsetTicks;

        // Clamp into the retained history window [oldest, currentTick].
        var capacity = (uint)history.Capacity;
        var oldest = currentTick + 1 > capacity ? currentTick + 1 - capacity : 0u;
        if (perceived < oldest)
        {
            perceived = oldest;
        }

        if (perceived > currentTick)
        {
            perceived = currentTick;
        }

        return perceived;
    }

    /// <summary>
    /// Gets an entity's recorded state at a tick, interpolating between the surrounding
    /// recorded ticks for components that support it.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <param name="tick">The tick to reconstruct state for.</param>
    /// <param name="state">
    /// When this method returns <see langword="true"/>, the reconstructed component values
    /// keyed by type; otherwise an empty dictionary.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if any retained state at or before <paramref name="tick"/> exists
    /// for the entity; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// When the exact tick is recorded, its state is returned as-is. Otherwise the state at or
    /// before the tick is bracketed with the next recorded tick and each component is blended by
    /// the fractional position of <paramref name="tick"/> between them: interpolatable components
    /// (per the configured <see cref="INetworkInterpolator"/>) are interpolated; all others snap
    /// to the at-or-before value. When there is no later recorded tick, the at-or-before state is
    /// returned unchanged.
    /// </para>
    /// </remarks>
    public bool TryGetStateAt(Entity entity, uint tick, out IReadOnlyDictionary<Type, object> state)
    {
        if (!history.TryGetStateAtOrBefore(entity, tick, out var beforeTick, out var beforeState))
        {
            state = emptyState;
            return false;
        }

        // Exact hit, or no later sample to interpolate toward: use the at-or-before state.
        if (beforeTick == tick
            || !history.TryGetStateAtOrAfter(entity, tick, out var afterTick, out var afterState)
            || afterTick <= beforeTick)
        {
            state = beforeState;
            return true;
        }

        // tick falls strictly between beforeTick and afterTick; blend by its fractional position.
        var factor = (float)(tick - beforeTick) / (afterTick - beforeTick);
        var interpolator = config.Interpolator;
        var result = new Dictionary<Type, object>(beforeState.Count);

        foreach (var (type, fromValue) in beforeState)
        {
            if (interpolator is not null
                && interpolator.IsInterpolatable(type)
                && afterState.TryGetValue(type, out var toValue))
            {
                var interpolated = interpolator.Interpolate(type, fromValue, toValue, factor);
                result[type] = interpolated ?? fromValue;
            }
            else
            {
                // Non-interpolatable (or no matching later value): snap to the at-or-before value.
                result[type] = fromValue;
            }
        }

        state = result;
        return true;
    }

    /// <summary>
    /// Temporarily rewinds the given entities' live components to their state at a tick.
    /// </summary>
    /// <param name="entities">The entities to rewind (typically hit-test candidates).</param>
    /// <param name="tick">The tick to rewind to, usually from <see cref="EstimateClientPerceivedTick"/>.</param>
    /// <returns>
    /// A <see cref="RewindScope"/> that restores the originals when disposed. Consume it with a
    /// <c>using</c> statement and perform hit testing against the live world inside the scope.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Entities that are not alive or have no retained state at the tick are skipped, leaving
    /// their live components untouched; skipping one entity never affects the others. Only
    /// components the entity currently has and that are present in the recorded state are
    /// swapped, so restoration is exact.
    /// </remarks>
    public RewindScope Rewind(IEnumerable<Entity> entities, uint tick)
    {
        ArgumentNullException.ThrowIfNull(entities);

        // Build the swap list up front. Reading the historical state (which may interpolate)
        // happens before any live mutation, so the captured live values are pristine.
        var swaps = new List<(Entity Entity, Type Type, object Live, object Historical)>();
        foreach (var entity in entities)
        {
            if (!world.IsAlive(entity))
            {
                continue;
            }

            if (!TryGetStateAt(entity, tick, out var historical) || historical.Count == 0)
            {
                continue;
            }

            foreach (var (type, liveValue) in snapshot.GetComponents(entity))
            {
                if (historical.TryGetValue(type, out var historicalValue))
                {
                    swaps.Add((entity, type, liveValue, historicalValue));
                }
            }
        }

        return new RewindScope(world, swaps);
    }
}
