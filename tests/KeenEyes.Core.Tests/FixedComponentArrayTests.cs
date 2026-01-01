using Position = KeenEyes.Tests.TestPosition;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the FixedComponentArray class.
/// </summary>
public class FixedComponentArrayTests
{
    #region Add Tests

    [Fact]
    public void Add_WithinCapacity_AddsComponent()
    {
        var array = new FixedComponentArray<Position>(capacity: 10);

        var index = array.Add(new Position { X = 1, Y = 2 });

        Assert.Equal(0, index);
        Assert.Equal(1, array.Count);
        Assert.Equal(1, array.GetRef(0).X);
        Assert.Equal(2, array.GetRef(0).Y);
    }

    [Fact]
    public void Add_MultipleComponents_ReturnsCorrectIndices()
    {
        var array = new FixedComponentArray<Position>(capacity: 10);

        var index1 = array.Add(new Position { X = 1, Y = 1 });
        var index2 = array.Add(new Position { X = 2, Y = 2 });
        var index3 = array.Add(new Position { X = 3, Y = 3 });

        Assert.Equal(0, index1);
        Assert.Equal(1, index2);
        Assert.Equal(2, index3);
        Assert.Equal(3, array.Count);
    }

    [Fact]
    public void Add_AtCapacity_ThrowsInvalidOperationException()
    {
        var array = new FixedComponentArray<Position>(capacity: 2);

        // Fill to capacity
        array.Add(new Position { X = 1, Y = 1 });
        array.Add(new Position { X = 2, Y = 2 });

        // Attempt to add beyond capacity should throw
        var exception = Assert.Throws<InvalidOperationException>(() =>
            array.Add(new Position { X = 3, Y = 3 }));

        Assert.Contains("Cannot add component: chunk is at capacity (2)", exception.Message);
    }

    [Fact]
    public void Add_ExactlyAtCapacity_ThrowsInvalidOperationException()
    {
        var array = new FixedComponentArray<Position>(capacity: 1);
        array.Add(new Position { X = 1, Y = 1 });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            array.Add(new Position { X = 2, Y = 2 }));

        Assert.Contains("Cannot add component: chunk is at capacity (1)", exception.Message);
    }

    [Fact]
    public void Add_UpToCapacity_DoesNotThrow()
    {
        var array = new FixedComponentArray<Position>(capacity: 5);

        // Should be able to add exactly 5 components
        for (int i = 0; i < 5; i++)
        {
            array.Add(new Position { X = i, Y = i });
        }

        Assert.Equal(5, array.Count);
        Assert.Equal(5, array.Capacity);
    }

    #endregion

    #region AddBoxed Tests

    [Fact]
    public void AddBoxed_AtCapacity_ThrowsInvalidOperationException()
    {
        var array = new FixedComponentArray<Position>(capacity: 2);

        array.AddBoxed(new Position { X = 1, Y = 1 });
        array.AddBoxed(new Position { X = 2, Y = 2 });

        // AddBoxed calls Add internally, so should also throw
        var exception = Assert.Throws<InvalidOperationException>(() =>
            array.AddBoxed(new Position { X = 3, Y = 3 }));

        Assert.Contains("Cannot add component: chunk is at capacity (2)", exception.Message);
    }

    [Fact]
    public void AddBoxed_WithinCapacity_AddsComponent()
    {
        var array = new FixedComponentArray<Position>(capacity: 10);

        var index = array.AddBoxed(new Position { X = 42, Y = 99 });

        Assert.Equal(0, index);
        Assert.Equal(1, array.Count);
        Assert.Equal(42, array.GetRef(0).X);
        Assert.Equal(99, array.GetRef(0).Y);
    }

    #endregion

    #region Other Methods Tests

    [Fact]
    public void RemoveAtSwapBack_DecreasesCount_AllowsAddAgain()
    {
        var array = new FixedComponentArray<Position>(capacity: 2);

        array.Add(new Position { X = 1, Y = 1 });
        array.Add(new Position { X = 2, Y = 2 });

        // Array is now at capacity
        Assert.Equal(2, array.Count);

        // Remove one element
        array.RemoveAtSwapBack(0);
        Assert.Equal(1, array.Count);

        // Should now be able to add another element
        var index = array.Add(new Position { X = 3, Y = 3 });
        Assert.Equal(1, index);
        Assert.Equal(2, array.Count);
    }

    [Fact]
    public void Clear_ResetsCount_AllowsAddingAgain()
    {
        var array = new FixedComponentArray<Position>(capacity: 2);

        array.Add(new Position { X = 1, Y = 1 });
        array.Add(new Position { X = 2, Y = 2 });

        // Clear the array
        array.Clear();
        Assert.Equal(0, array.Count);

        // Should now be able to add elements again
        array.Add(new Position { X = 3, Y = 3 });
        array.Add(new Position { X = 4, Y = 4 });

        Assert.Equal(2, array.Count);
    }

    [Fact]
    public void Capacity_RemainsConstant()
    {
        var array = new FixedComponentArray<Position>(capacity: 5);

        Assert.Equal(5, array.Capacity);

        array.Add(new Position());
        Assert.Equal(5, array.Capacity);

        array.Add(new Position());
        array.Add(new Position());
        Assert.Equal(5, array.Capacity);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Add_ToZeroCapacityArray_ThrowsImmediately()
    {
        var array = new FixedComponentArray<Position>(capacity: 0);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            array.Add(new Position()));

        Assert.Contains("Cannot add component: chunk is at capacity (0)", exception.Message);
    }

    [Fact]
    public void RemoveAtSwapBack_InvalidIndex_Throws()
    {
        var array = new FixedComponentArray<Position>(capacity: 10);
        array.Add(new Position());

        Assert.Throws<ArgumentOutOfRangeException>(() => array.RemoveAtSwapBack(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.RemoveAtSwapBack(5));
    }

    #endregion
}
