using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the ParticlePool class.
/// </summary>
public class ParticlePoolTests : IDisposable
{
    private ParticlePool? pool;

    public void Dispose()
    {
        pool?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidCapacity_CreatesPool()
    {
        pool = new ParticlePool(100);

        Assert.Equal(100, pool.Capacity);
        Assert.Equal(0, pool.ActiveCount);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParticlePool(0));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParticlePool(-1));
    }

    [Fact]
    public void Constructor_InitializesAllArrays()
    {
        pool = new ParticlePool(10);

        Assert.NotNull(pool.PositionsX);
        Assert.NotNull(pool.PositionsY);
        Assert.NotNull(pool.VelocitiesX);
        Assert.NotNull(pool.VelocitiesY);
        Assert.NotNull(pool.ColorsR);
        Assert.NotNull(pool.ColorsG);
        Assert.NotNull(pool.ColorsB);
        Assert.NotNull(pool.ColorsA);
        Assert.NotNull(pool.Sizes);
        Assert.NotNull(pool.InitialSizes);
        Assert.NotNull(pool.Rotations);
        Assert.NotNull(pool.RotationSpeeds);
        Assert.NotNull(pool.Ages);
        Assert.NotNull(pool.Lifetimes);
        Assert.NotNull(pool.NormalizedAges);
        Assert.NotNull(pool.Alive);

        Assert.Equal(10, pool.PositionsX.Length);
        Assert.Equal(10, pool.Alive.Length);
    }

    #endregion

    #region Allocate Tests

    [Fact]
    public void Allocate_ReturnsValidIndex()
    {
        pool = new ParticlePool(10);

        var index = pool.Allocate();

        Assert.InRange(index, 0, 9);
        Assert.True(pool.Alive[index]);
        Assert.Equal(1, pool.ActiveCount);
    }

    [Fact]
    public void Allocate_MultipleAllocations_ReturnsUniqueIndices()
    {
        pool = new ParticlePool(10);
        var indices = new HashSet<int>();

        for (int i = 0; i < 5; i++)
        {
            var index = pool.Allocate();
            Assert.True(indices.Add(index), $"Duplicate index {index} returned");
        }

        Assert.Equal(5, pool.ActiveCount);
    }

    [Fact]
    public void Allocate_WhenPoolFull_ReturnsNegativeOne()
    {
        pool = new ParticlePool(3);

        pool.Allocate();
        pool.Allocate();
        pool.Allocate();

        var index = pool.Allocate();

        Assert.Equal(-1, index);
        Assert.Equal(3, pool.ActiveCount);
    }

    #endregion

    #region Release Tests

    [Fact]
    public void Release_ValidIndex_MarksAsNotAlive()
    {
        pool = new ParticlePool(10);
        var index = pool.Allocate();

        pool.Release(index);

        Assert.False(pool.Alive[index]);
        Assert.Equal(0, pool.ActiveCount);
    }

    [Fact]
    public void Release_IndexOutOfRange_DoesNotThrow()
    {
        pool = new ParticlePool(10);

        // Should not throw
        pool.Release(-1);
        pool.Release(100);
    }

    [Fact]
    public void Release_AlreadyReleased_DoesNothing()
    {
        pool = new ParticlePool(10);
        var index = pool.Allocate();

        pool.Release(index);
        var countAfterFirst = pool.ActiveCount;

        pool.Release(index);
        var countAfterSecond = pool.ActiveCount;

        Assert.Equal(countAfterFirst, countAfterSecond);
        Assert.Equal(0, countAfterSecond);
    }

    [Fact]
    public void Release_AllowsReuse()
    {
        pool = new ParticlePool(1);

        var index1 = pool.Allocate();
        pool.Release(index1);

        var index2 = pool.Allocate();

        Assert.Equal(index1, index2);
        Assert.Equal(1, pool.ActiveCount);
    }

    #endregion

    #region Grow Tests

    [Fact]
    public void Grow_IncreasesCapacity()
    {
        pool = new ParticlePool(10);

        pool.Grow(20, 100);

        Assert.Equal(20, pool.Capacity);
        Assert.Equal(20, pool.PositionsX.Length);
    }

    [Fact]
    public void Grow_RespectsMaxCapacity()
    {
        pool = new ParticlePool(10);

        pool.Grow(100, 50);

        Assert.Equal(50, pool.Capacity);
    }

    [Fact]
    public void Grow_SmallerThanCurrent_DoesNothing()
    {
        pool = new ParticlePool(20);

        pool.Grow(10, 100);

        Assert.Equal(20, pool.Capacity);
    }

    [Fact]
    public void Grow_PreservesExistingData()
    {
        pool = new ParticlePool(5);

        var index = pool.Allocate();
        pool.PositionsX[index] = 42f;
        pool.PositionsY[index] = 99f;

        pool.Grow(10, 100);

        Assert.Equal(42f, pool.PositionsX[index]);
        Assert.Equal(99f, pool.PositionsY[index]);
        Assert.True(pool.Alive[index]);
    }

    [Fact]
    public void Grow_AllowsNewAllocations()
    {
        pool = new ParticlePool(2);

        pool.Allocate();
        pool.Allocate();
        Assert.Equal(-1, pool.Allocate()); // Full

        pool.Grow(4, 100);

        var index = pool.Allocate();
        Assert.NotEqual(-1, index);
        Assert.Equal(3, pool.ActiveCount);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ResetsActiveCount()
    {
        pool = new ParticlePool(10);

        pool.Allocate();
        pool.Allocate();
        pool.Allocate();

        pool.Clear();

        Assert.Equal(0, pool.ActiveCount);
    }

    [Fact]
    public void Clear_MarksAllAsNotAlive()
    {
        pool = new ParticlePool(5);

        for (int i = 0; i < 5; i++)
        {
            pool.Allocate();
        }

        pool.Clear();

        for (int i = 0; i < 5; i++)
        {
            Assert.False(pool.Alive[i]);
        }
    }

    [Fact]
    public void Clear_AllowsReallocation()
    {
        pool = new ParticlePool(3);

        pool.Allocate();
        pool.Allocate();
        pool.Allocate();
        Assert.Equal(-1, pool.Allocate()); // Full

        pool.Clear();

        var index = pool.Allocate();
        Assert.NotEqual(-1, index);
        Assert.Equal(1, pool.ActiveCount);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        pool = new ParticlePool(10);

        pool.Dispose();
        pool.Dispose(); // Should not throw
    }

    #endregion

    #region Free List Behavior Tests

    [Fact]
    public void FreeList_AllocateRelease_LIFOOrder()
    {
        pool = new ParticlePool(10);

        _ = pool.Allocate(); // index1, not used
        var index2 = pool.Allocate();
        var index3 = pool.Allocate();

        pool.Release(index2);
        pool.Release(index3);

        // Should get back index3 first (LIFO)
        var newIndex1 = pool.Allocate();
        var newIndex2 = pool.Allocate();

        Assert.Equal(index3, newIndex1);
        Assert.Equal(index2, newIndex2);
    }

    #endregion
}
