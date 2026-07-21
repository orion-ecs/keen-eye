using KeenEyes.Capabilities;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Replication;

/// <summary>
/// Server-side, per-entity ring of tick-indexed authoritative component states.
/// </summary>
/// <remarks>
/// <para>
/// The server records each networked entity's authoritative state once per network
/// tick. This history is the enabler for lag compensation: a later system can rewind
/// the world to the tick a client acted on and resolve interactions against the state
/// the client actually saw.
/// </para>
/// <para>
/// Each entity keeps a fixed-capacity ring keyed by tick. Storing a new tick reuses the
/// dictionary that held <c>tick - capacity</c>, so steady-state capture allocates only
/// the boxed component values and never grows the ring. Ticks older than the ring window
/// are evicted implicitly as the ring advances; despawned entities are dropped via
/// <see cref="Remove(Entity)"/>.
/// </para>
/// <para>
/// The dictionaries returned from the query methods are the live storage and remain valid
/// only until the corresponding tick is evicted from the ring; callers must not retain
/// them across network ticks.
/// </para>
/// </remarks>
public sealed class ServerStateHistory
{
    private readonly int capacity;
    private readonly Dictionary<Entity, EntityRing> histories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerStateHistory"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The number of ticks to retain per entity. Roughly one tick per network tick,
    /// so <c>TickRate</c> ticks is approximately one second of history.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity"/> is not positive.
    /// </exception>
    public ServerStateHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        this.capacity = capacity;
    }

    /// <summary>
    /// Gets the per-entity ring capacity in ticks.
    /// </summary>
    public int Capacity => capacity;

    /// <summary>
    /// Records the authoritative network-serializable state of an entity for a tick.
    /// </summary>
    /// <param name="entity">The networked entity to capture.</param>
    /// <param name="tick">The network tick the state belongs to.</param>
    /// <param name="snapshot">The world snapshot capability used to read components.</param>
    /// <param name="serializer">The serializer used to filter replicated component types.</param>
    /// <remarks>
    /// Only components that are network-serializable are recorded, matching the state the
    /// server replicates. Component values are boxed structs, so a stored value is an
    /// independent copy that is unaffected by later mutations of the live component.
    /// </remarks>
    public void Capture(Entity entity, uint tick, ISnapshotCapability snapshot, INetworkSerializer serializer)
    {
        if (!histories.TryGetValue(entity, out var ring))
        {
            ring = new EntityRing(capacity);
            histories[entity] = ring;
        }

        var slot = ring.BeginWrite(tick);
        foreach (var (type, value) in snapshot.GetComponents(entity))
        {
            if (serializer.IsNetworkSerializable(type))
            {
                slot[type] = value;
            }
        }
    }

    /// <summary>
    /// Gets the recorded state of an entity at an exact tick.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <param name="tick">The exact tick to look up.</param>
    /// <param name="state">
    /// When this method returns <see langword="true"/>, the recorded component states
    /// keyed by component type; otherwise an empty dictionary.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a state was recorded for the entity at the tick and has
    /// not been evicted; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryGetState(Entity entity, uint tick, out IReadOnlyDictionary<Type, object> state)
    {
        if (histories.TryGetValue(entity, out var ring) && ring.TryGet(tick, out var slot))
        {
            state = slot;
            return true;
        }

        state = EmptyState.Instance;
        return false;
    }

    /// <summary>
    /// Gets the recorded state of an entity at the latest tick at or before the given tick.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <param name="tick">The upper bound tick (inclusive).</param>
    /// <param name="matchedTick">
    /// When this method returns <see langword="true"/>, the actual tick whose state was
    /// returned; otherwise zero.
    /// </param>
    /// <param name="state">
    /// When this method returns <see langword="true"/>, the recorded component states for
    /// <paramref name="matchedTick"/>; otherwise an empty dictionary.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if any retained state at or before <paramref name="tick"/>
    /// exists for the entity; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Lag compensation uses this to locate the pair of ticks surrounding a client's
    /// action time; interpolation between ticks is a later concern and is not performed here.
    /// </remarks>
    public bool TryGetStateAtOrBefore(Entity entity, uint tick, out uint matchedTick, out IReadOnlyDictionary<Type, object> state)
    {
        if (histories.TryGetValue(entity, out var ring) && ring.TryGetAtOrBefore(tick, out matchedTick, out var slot))
        {
            state = slot;
            return true;
        }

        matchedTick = 0;
        state = EmptyState.Instance;
        return false;
    }

    /// <summary>
    /// Gets the recorded state of an entity at the earliest tick at or after the given tick.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <param name="tick">The lower bound tick (inclusive).</param>
    /// <param name="matchedTick">
    /// When this method returns <see langword="true"/>, the actual tick whose state was
    /// returned; otherwise zero.
    /// </param>
    /// <param name="state">
    /// When this method returns <see langword="true"/>, the recorded component states for
    /// <paramref name="matchedTick"/>; otherwise an empty dictionary.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if any retained state at or after <paramref name="tick"/>
    /// exists for the entity; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This is the forward-looking counterpart to <see cref="TryGetStateAtOrBefore"/>.
    /// Lag compensation pairs the two to bracket a client's action time and interpolate
    /// between the surrounding recorded ticks.
    /// </remarks>
    public bool TryGetStateAtOrAfter(Entity entity, uint tick, out uint matchedTick, out IReadOnlyDictionary<Type, object> state)
    {
        if (histories.TryGetValue(entity, out var ring) && ring.TryGetAtOrAfter(tick, out matchedTick, out var slot))
        {
            state = slot;
            return true;
        }

        matchedTick = 0;
        state = EmptyState.Instance;
        return false;
    }

    /// <summary>
    /// Drops all recorded history for an entity. Call when the entity despawns.
    /// </summary>
    /// <param name="entity">The entity whose history should be dropped.</param>
    /// <returns><see langword="true"/> if history existed and was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(Entity entity) => histories.Remove(entity);

    /// <summary>
    /// Clears all recorded history for all entities.
    /// </summary>
    public void Clear() => histories.Clear();

    /// <summary>
    /// A fixed-capacity ring of tick-indexed component snapshots for a single entity.
    /// </summary>
    private sealed class EntityRing(int capacity)
    {
        private readonly uint[] ticks = new uint[capacity];
        private readonly bool[] occupied = new bool[capacity];
        private readonly Dictionary<Type, object>[] slots = new Dictionary<Type, object>[capacity];
        private bool hasAny;
        private uint newestTick;

        public Dictionary<Type, object> BeginWrite(uint tick)
        {
            var index = (int)(tick % (uint)capacity);
            var slot = slots[index];
            if (slot is null)
            {
                slot = [];
                slots[index] = slot;
            }
            else
            {
                slot.Clear();
            }

            ticks[index] = tick;
            occupied[index] = true;

            if (!hasAny || tick > newestTick)
            {
                newestTick = tick;
            }

            hasAny = true;
            return slot;
        }

        public bool TryGet(uint tick, out Dictionary<Type, object> slot)
        {
            var index = (int)(tick % (uint)capacity);
            if (occupied[index] && ticks[index] == tick)
            {
                slot = slots[index];
                return true;
            }

            slot = null!;
            return false;
        }

        public bool TryGetAtOrBefore(uint tick, out uint matchedTick, out Dictionary<Type, object> slot)
        {
            if (hasAny)
            {
                // Never look past the newest recorded tick; that also bounds the search
                // to within the ring window so eviction can't produce a false match.
                var start = tick < newestTick ? tick : newestTick;
                for (uint offset = 0; offset < (uint)capacity && offset <= start; offset++)
                {
                    var candidate = start - offset;
                    var index = (int)(candidate % (uint)capacity);
                    if (occupied[index] && ticks[index] == candidate)
                    {
                        matchedTick = candidate;
                        slot = slots[index];
                        return true;
                    }
                }
            }

            matchedTick = 0;
            slot = null!;
            return false;
        }

        public bool TryGetAtOrAfter(uint tick, out uint matchedTick, out Dictionary<Type, object> slot)
        {
            // Nothing at or after a tick beyond the newest recorded one.
            if (hasAny && tick <= newestTick)
            {
                // The ring retains ticks in (newestTick - capacity, newestTick]; never search
                // below that window or an evicted slot could produce a false match. Bounding
                // the start there also caps the walk at one full ring pass.
                var oldest = newestTick + 1 > (uint)capacity ? newestTick + 1 - (uint)capacity : 0u;
                var start = tick > oldest ? tick : oldest;
                for (var candidate = start; candidate <= newestTick; candidate++)
                {
                    var index = (int)(candidate % (uint)capacity);
                    if (occupied[index] && ticks[index] == candidate)
                    {
                        matchedTick = candidate;
                        slot = slots[index];
                        return true;
                    }
                }
            }

            matchedTick = 0;
            slot = null!;
            return false;
        }
    }

    /// <summary>
    /// Shared empty state returned when no history is found, avoiding per-miss allocation.
    /// </summary>
    private static class EmptyState
    {
        public static readonly IReadOnlyDictionary<Type, object> Instance =
            new Dictionary<Type, object>();
    }
}
