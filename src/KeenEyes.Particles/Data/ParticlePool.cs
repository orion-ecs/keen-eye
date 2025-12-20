namespace KeenEyes.Particles.Data;

/// <summary>
/// SOA (Structure of Arrays) storage for particle data.
/// All arrays are the same length and indexed by particle index.
/// </summary>
/// <remarks>
/// <para>
/// This class uses Structure of Arrays (SOA) layout for optimal cache efficiency
/// when iterating over single properties across all particles. Each property is
/// stored in its own contiguous array.
/// </para>
/// <para>
/// Allocation uses a free list for O(1) allocation and deallocation without
/// fragmentation within the pool.
/// </para>
/// </remarks>
public sealed class ParticlePool : IDisposable
{
    #region Position and Movement

    /// <summary>X positions of all particles.</summary>
    public float[] PositionsX = [];

    /// <summary>Y positions of all particles.</summary>
    public float[] PositionsY = [];

    /// <summary>X velocities of all particles (units per second).</summary>
    public float[] VelocitiesX = [];

    /// <summary>Y velocities of all particles (units per second).</summary>
    public float[] VelocitiesY = [];

    #endregion

    #region Visual Properties

    /// <summary>Red color component (0-1).</summary>
    public float[] ColorsR = [];

    /// <summary>Green color component (0-1).</summary>
    public float[] ColorsG = [];

    /// <summary>Blue color component (0-1).</summary>
    public float[] ColorsB = [];

    /// <summary>Alpha color component (0-1).</summary>
    public float[] ColorsA = [];

    /// <summary>Particle sizes (diameter in pixels).</summary>
    public float[] Sizes = [];

    /// <summary>Initial sizes for modifier calculations.</summary>
    public float[] InitialSizes = [];

    /// <summary>Particle rotations (in radians).</summary>
    public float[] Rotations = [];

    /// <summary>Rotation speeds (radians per second).</summary>
    public float[] RotationSpeeds = [];

    #endregion

    #region Lifecycle

    /// <summary>Current age of particles (seconds since spawn).</summary>
    public float[] Ages = [];

    /// <summary>Total lifetime of particles (seconds).</summary>
    public float[] Lifetimes = [];

    /// <summary>Normalized age (Age / Lifetime, 0-1).</summary>
    public float[] NormalizedAges = [];

    /// <summary>Whether each particle slot is currently active.</summary>
    public bool[] Alive = [];

    #endregion

    #region Management

    /// <summary>
    /// Gets the current capacity of the pool.
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Gets the number of currently active particles.
    /// </summary>
    public int ActiveCount { get; private set; }

    // Free list for O(1) allocation
    private int[] freeIndices = [];
    private int freeCount;
    private bool isDisposed;

    #endregion

    /// <summary>
    /// Creates a new particle pool with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the pool.</param>
    public ParticlePool(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);

        Capacity = initialCapacity;

        // Position and movement
        PositionsX = new float[initialCapacity];
        PositionsY = new float[initialCapacity];
        VelocitiesX = new float[initialCapacity];
        VelocitiesY = new float[initialCapacity];

        // Visual properties
        ColorsR = new float[initialCapacity];
        ColorsG = new float[initialCapacity];
        ColorsB = new float[initialCapacity];
        ColorsA = new float[initialCapacity];
        Sizes = new float[initialCapacity];
        InitialSizes = new float[initialCapacity];
        Rotations = new float[initialCapacity];
        RotationSpeeds = new float[initialCapacity];

        // Lifecycle
        Ages = new float[initialCapacity];
        Lifetimes = new float[initialCapacity];
        NormalizedAges = new float[initialCapacity];
        Alive = new bool[initialCapacity];

        // Initialize free list with all indices
        freeIndices = new int[initialCapacity];
        for (var i = 0; i < initialCapacity; i++)
        {
            freeIndices[i] = i;
        }
        freeCount = initialCapacity;
    }

    /// <summary>
    /// Allocates a new particle slot.
    /// </summary>
    /// <returns>The index of the allocated slot, or -1 if the pool is full.</returns>
    public int Allocate()
    {
        if (freeCount == 0)
        {
            return -1;
        }

        freeCount--;
        var index = freeIndices[freeCount];
        Alive[index] = true;
        ActiveCount++;
        return index;
    }

    /// <summary>
    /// Releases a particle slot back to the pool.
    /// </summary>
    /// <param name="index">The index of the slot to release.</param>
    public void Release(int index)
    {
        if (index < 0 || index >= Capacity)
        {
            return;
        }

        if (!Alive[index])
        {
            return;
        }

        Alive[index] = false;
        freeIndices[freeCount] = index;
        freeCount++;
        ActiveCount--;
    }

    /// <summary>
    /// Grows the pool capacity up to the specified maximum.
    /// </summary>
    /// <param name="newCapacity">The desired new capacity.</param>
    /// <param name="maxCapacity">The maximum allowed capacity.</param>
    public void Grow(int newCapacity, int maxCapacity)
    {
        newCapacity = Math.Min(newCapacity, maxCapacity);
        if (newCapacity <= Capacity)
        {
            return;
        }

        var oldCapacity = Capacity;

        // Resize all arrays
        Array.Resize(ref PositionsX, newCapacity);
        Array.Resize(ref PositionsY, newCapacity);
        Array.Resize(ref VelocitiesX, newCapacity);
        Array.Resize(ref VelocitiesY, newCapacity);
        Array.Resize(ref ColorsR, newCapacity);
        Array.Resize(ref ColorsG, newCapacity);
        Array.Resize(ref ColorsB, newCapacity);
        Array.Resize(ref ColorsA, newCapacity);
        Array.Resize(ref Sizes, newCapacity);
        Array.Resize(ref InitialSizes, newCapacity);
        Array.Resize(ref Rotations, newCapacity);
        Array.Resize(ref RotationSpeeds, newCapacity);
        Array.Resize(ref Ages, newCapacity);
        Array.Resize(ref Lifetimes, newCapacity);
        Array.Resize(ref NormalizedAges, newCapacity);
        Array.Resize(ref Alive, newCapacity);

        // Add new indices to free list
        Array.Resize(ref freeIndices, newCapacity);
        for (var i = oldCapacity; i < newCapacity; i++)
        {
            freeIndices[freeCount++] = i;
        }

        Capacity = newCapacity;
    }

    /// <summary>
    /// Clears all particles from the pool.
    /// </summary>
    public void Clear()
    {
        // Reset all alive flags
        Array.Fill(Alive, false);

        // Rebuild free list
        for (var i = 0; i < Capacity; i++)
        {
            freeIndices[i] = i;
        }
        freeCount = Capacity;
        ActiveCount = 0;
    }

    /// <summary>
    /// Disposes the pool and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }
        isDisposed = true;

        // Help GC by clearing references
        PositionsX = null!;
        PositionsY = null!;
        VelocitiesX = null!;
        VelocitiesY = null!;
        ColorsR = null!;
        ColorsG = null!;
        ColorsB = null!;
        ColorsA = null!;
        Sizes = null!;
        InitialSizes = null!;
        Rotations = null!;
        RotationSpeeds = null!;
        Ages = null!;
        Lifetimes = null!;
        NormalizedAges = null!;
        Alive = null!;
        freeIndices = null!;
    }
}
