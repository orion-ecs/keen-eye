namespace KeenEyes.Network.Prediction;

/// <summary>
/// Stores predicted component states for rollback during reconciliation.
/// </summary>
/// <remarks>
/// <para>
/// When reconciliation is triggered, the client rolls back to the last
/// server-confirmed state and replays inputs from that point forward.
/// This buffer stores the predicted states to enable comparison and rollback.
/// </para>
/// </remarks>
/// <param name="maxTicks">Maximum number of ticks to store.</param>
public sealed class PredictionBuffer(int maxTicks = 64)
{
    private readonly Dictionary<uint, Dictionary<Type, object>> statesByTick = [];

    /// <summary>
    /// Gets the oldest tick stored in the buffer.
    /// </summary>
    public uint OldestTick { get; private set; }

    /// <summary>
    /// Gets the newest tick stored in the buffer.
    /// </summary>
    public uint NewestTick { get; private set; }

    /// <summary>
    /// Saves the predicted state for a component at a specific tick.
    /// </summary>
    /// <param name="tick">The tick number.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="state">The component state (boxed).</param>
    public void SaveState(uint tick, Type componentType, object state)
    {
        if (!statesByTick.TryGetValue(tick, out var components))
        {
            components = [];
            statesByTick[tick] = components;
        }

        components[componentType] = state;

        if (statesByTick.Count == 1)
        {
            OldestTick = tick;
            NewestTick = tick;
        }
        else if (tick > NewestTick)
        {
            NewestTick = tick;
            // Clean up old ticks
            CleanupOldTicks();
        }
    }

    /// <summary>
    /// Gets the predicted state for a component at a specific tick.
    /// </summary>
    /// <param name="tick">The tick number.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="state">The component state if found.</param>
    /// <returns>True if the state was found; false otherwise.</returns>
    public bool TryGetState(uint tick, Type componentType, out object? state)
    {
        if (statesByTick.TryGetValue(tick, out var components) &&
            components.TryGetValue(componentType, out state))
        {
            return true;
        }

        state = null;
        return false;
    }

    /// <summary>
    /// Gets all component states for a specific tick.
    /// </summary>
    /// <param name="tick">The tick number.</param>
    /// <returns>Dictionary of component states, or null if tick not found.</returns>
    public IReadOnlyDictionary<Type, object>? GetStatesForTick(uint tick)
    {
        return statesByTick.TryGetValue(tick, out var states) ? states : null;
    }

    /// <summary>
    /// Removes all states for ticks older than the specified tick.
    /// </summary>
    /// <param name="tick">Ticks older than this will be removed.</param>
    public void RemoveOlderThan(uint tick)
    {
        var toRemove = statesByTick.Keys.Where(t => t < tick).ToList();
        foreach (var t in toRemove)
        {
            statesByTick.Remove(t);
        }

        if (statesByTick.Count > 0)
        {
            OldestTick = statesByTick.Keys.Min();
        }
        else
        {
            OldestTick = 0;
            NewestTick = 0;
        }
    }

    /// <summary>
    /// Clears all stored states.
    /// </summary>
    public void Clear()
    {
        statesByTick.Clear();
        OldestTick = 0;
        NewestTick = 0;
    }

    private void CleanupOldTicks()
    {
        while (statesByTick.Count > maxTicks && OldestTick < NewestTick)
        {
            statesByTick.Remove(OldestTick);
            OldestTick++;
            while (!statesByTick.ContainsKey(OldestTick) && OldestTick < NewestTick)
            {
                OldestTick++;
            }
        }
    }
}
