using System.Collections.Immutable;
using KeenEyes.Generators.Utilities;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the <see cref="EquatableArray{T}"/> structural equality wrapper used
/// by incremental generator pipeline models.
/// </summary>
public class EquatableArrayTests
{
    #region Equals Tests

    [Fact]
    public void Equals_WithSameElementsInFreshArrays_ReturnsTrue()
    {
        // Two separately allocated arrays with identical content - this is exactly
        // the case where ImmutableArray<T> reference equality fails.
        EquatableArray<string> first = ImmutableArray.Create("a", "b", "c");
        EquatableArray<string> second = ImmutableArray.Create("a", "b", "c");

        Assert.True(first.Equals(second));
        Assert.True(first == second);
        Assert.False(first != second);
    }

    [Fact]
    public void Equals_WithDifferentElements_ReturnsFalse()
    {
        EquatableArray<string> first = ImmutableArray.Create("a", "b", "c");
        EquatableArray<string> second = ImmutableArray.Create("a", "b", "d");

        Assert.False(first.Equals(second));
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_WithDifferentLengths_ReturnsFalse()
    {
        EquatableArray<int> first = ImmutableArray.Create(1, 2, 3);
        EquatableArray<int> second = ImmutableArray.Create(1, 2);

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_WithDifferentOrder_ReturnsFalse()
    {
        EquatableArray<int> first = ImmutableArray.Create(1, 2, 3);
        EquatableArray<int> second = ImmutableArray.Create(3, 2, 1);

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_DefaultInstanceAndEmpty_ReturnsTrue()
    {
        var defaultInstance = default(EquatableArray<int>);

        Assert.True(defaultInstance.Equals(EquatableArray<int>.Empty));
        Assert.Equal(0, defaultInstance.Length);
    }

    [Fact]
    public void Equals_AsObject_UsesStructuralEquality()
    {
        EquatableArray<string> first = ImmutableArray.Create("x");
        object second = (EquatableArray<string>)ImmutableArray.Create("x");

        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Equals_InsideRecord_MakesRecordsWithFreshArraysEqual()
    {
        // Mirrors the pipeline-model shape: a record with an EquatableArray field
        // built freshly on every transform must still compare equal.
        var first = new PipelineModel("Name", ImmutableArray.Create("f1", "f2"));
        var second = new PipelineModel("Name", ImmutableArray.Create("f1", "f2"));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameElements_ReturnsSameHash()
    {
        EquatableArray<string> first = ImmutableArray.Create("a", "b");
        EquatableArray<string> second = ImmutableArray.Create("a", "b");

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DefaultInstanceAndEmpty_ReturnsSameHash()
    {
        Assert.Equal(EquatableArray<int>.Empty.GetHashCode(), default(EquatableArray<int>).GetHashCode());
    }

    #endregion

    #region Conversion and Access Tests

    [Fact]
    public void ImplicitConversion_RoundTripsThroughImmutableArray()
    {
        var source = ImmutableArray.Create(1, 2, 3);
        EquatableArray<int> wrapped = source;
        ImmutableArray<int> unwrapped = wrapped;

        Assert.Equal(source, unwrapped);
        Assert.Equal(3, wrapped.Length);
        Assert.Equal(2, wrapped[1]);
    }

    [Fact]
    public void AsImmutableArray_OnDefaultInstance_ReturnsEmptyNotDefault()
    {
        var defaultInstance = default(EquatableArray<string>);

        Assert.False(defaultInstance.AsImmutableArray().IsDefault);
        Assert.True(defaultInstance.AsImmutableArray().IsEmpty);
    }

    [Fact]
    public void ToArray_ReturnsAllElements()
    {
        EquatableArray<string> array = ImmutableArray.Create("a", "b");

        Assert.Equal(new[] { "a", "b" }, array.ToArray());
    }

    [Fact]
    public void GetEnumerator_EnumeratesAllElements()
    {
        EquatableArray<int> array = ImmutableArray.Create(1, 2, 3);

        var sum = 0;
        foreach (var item in array)
        {
            sum += item;
        }

        Assert.Equal(6, sum);
    }

    #endregion

    private sealed record PipelineModel(string Name, EquatableArray<string> Fields);
}
