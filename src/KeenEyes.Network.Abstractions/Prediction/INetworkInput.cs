namespace KeenEyes.Network.Prediction;

/// <summary>
/// Interface for network inputs that can be predicted and replayed.
/// </summary>
/// <remarks>
/// <para>
/// Game-specific input types should implement this interface to enable
/// client-side prediction. Inputs are buffered and sent to the server,
/// then replayed if misprediction is detected.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public struct PlayerInput : INetworkInput
/// {
///     public uint Tick { get; set; }
///     public float MoveX;
///     public float MoveY;
///     public bool Jump;
///     public bool Fire;
/// }
/// </code>
/// </example>
public interface INetworkInput
{
    /// <summary>
    /// Gets or sets the tick number this input was generated for.
    /// </summary>
    uint Tick { get; set; }
}

/// <summary>
/// Non-generic interface for input buffers to allow polymorphic access.
/// </summary>
public interface IInputBuffer
{
    /// <summary>
    /// Gets the oldest tick in the buffer.
    /// </summary>
    uint OldestTick { get; }

    /// <summary>
    /// Gets the newest tick in the buffer.
    /// </summary>
    uint NewestTick { get; }

    /// <summary>
    /// Gets the number of inputs stored in the buffer.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets inputs from a starting tick to the newest tick (boxed).
    /// </summary>
    /// <param name="startTick">The tick to start from (inclusive).</param>
    /// <returns>An enumerable of boxed inputs.</returns>
    IEnumerable<object> GetInputsFromBoxed(uint startTick);

    /// <summary>
    /// Removes all inputs older than the specified tick.
    /// </summary>
    /// <param name="tick">Inputs older than this tick will be removed.</param>
    void RemoveOlderThan(uint tick);

    /// <summary>
    /// Clears all inputs from the buffer.
    /// </summary>
    void Clear();
}

/// <summary>
/// Circular buffer for storing network inputs by tick.
/// </summary>
/// <typeparam name="T">The input type.</typeparam>
/// <remarks>
/// <para>
/// Stores inputs for replay during reconciliation. Old inputs are
/// automatically discarded when the buffer fills up.
/// </para>
/// </remarks>
/// <param name="capacity">The maximum number of inputs to store.</param>
public sealed class InputBuffer<T>(int capacity = 64) : IInputBuffer where T : struct, INetworkInput
{
    private readonly T[] buffer = new T[capacity];

    /// <summary>
    /// Gets the number of inputs stored in the buffer.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the oldest tick in the buffer.
    /// </summary>
    public uint OldestTick { get; private set; }

    /// <summary>
    /// Gets the newest tick in the buffer.
    /// </summary>
    public uint NewestTick { get; private set; }

    /// <summary>
    /// Adds an input to the buffer.
    /// </summary>
    /// <param name="input">The input to add.</param>
    public void Add(T input)
    {
        var index = (int)(input.Tick % capacity);
        buffer[index] = input;

        if (Count == 0)
        {
            OldestTick = input.Tick;
            NewestTick = input.Tick;
            Count = 1;
        }
        else if (input.Tick > NewestTick)
        {
            NewestTick = input.Tick;
            Count = Math.Min(Count + 1, capacity);

            // Update oldest tick if buffer wrapped
            if (NewestTick - OldestTick >= capacity)
            {
                OldestTick = NewestTick - (uint)capacity + 1;
            }
        }
    }

    /// <summary>
    /// Gets the input for a specific tick.
    /// </summary>
    /// <param name="tick">The tick to get input for.</param>
    /// <param name="input">The input if found.</param>
    /// <returns>True if the input was found; false otherwise.</returns>
    public bool TryGet(uint tick, out T input)
    {
        if (tick < OldestTick || tick > NewestTick)
        {
            input = default;
            return false;
        }

        var index = (int)(tick % capacity);
        input = buffer[index];
        return input.Tick == tick;
    }

    /// <summary>
    /// Removes all inputs older than the specified tick.
    /// </summary>
    /// <param name="tick">Inputs older than this tick will be removed.</param>
    public void RemoveOlderThan(uint tick)
    {
        if (tick > OldestTick)
        {
            OldestTick = tick;
            Count = (int)(NewestTick - OldestTick + 1);
            if (Count < 0)
            {
                Count = 0;
            }
        }
    }

    /// <summary>
    /// Clears all inputs from the buffer.
    /// </summary>
    public void Clear()
    {
        Count = 0;
        OldestTick = 0;
        NewestTick = 0;
    }

    /// <summary>
    /// Gets inputs from a starting tick to the newest tick.
    /// </summary>
    /// <param name="startTick">The tick to start from (inclusive).</param>
    /// <returns>An enumerable of inputs.</returns>
    public IEnumerable<T> GetInputsFrom(uint startTick)
    {
        for (uint tick = Math.Max(startTick, OldestTick); tick <= NewestTick; tick++)
        {
            if (TryGet(tick, out var input))
            {
                yield return input;
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<object> GetInputsFromBoxed(uint startTick)
    {
        foreach (var input in GetInputsFrom(startTick))
        {
            yield return input;
        }
    }
}
