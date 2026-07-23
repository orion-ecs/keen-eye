using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace KeenEyes.Generators.Utilities;

/// <summary>
/// An immutable array wrapper with structural equality semantics for use in
/// incremental generator pipeline models.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// <see cref="ImmutableArray{T}"/> compares by reference to the underlying array,
/// which makes compiler-generated <c>record</c> equality always false for freshly
/// built collections and defeats <c>IIncrementalGenerator</c> caching. This wrapper
/// compares element-by-element so equivalent pipeline values are recognized as
/// unchanged between runs.
/// </remarks>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
{
    /// <summary>An empty array instance.</summary>
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    private readonly ImmutableArray<T> array;

    /// <summary>
    /// Initializes a new instance wrapping the given immutable array.
    /// </summary>
    /// <param name="array">The underlying immutable array.</param>
    public EquatableArray(ImmutableArray<T> array)
    {
        this.array = array;
    }

    /// <summary>Gets the number of elements in the array.</summary>
    public int Length => array.IsDefault ? 0 : array.Length;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based index.</param>
    public T this[int index] => AsImmutableArray()[index];

    /// <summary>
    /// Returns the underlying <see cref="ImmutableArray{T}"/>, normalizing a
    /// default instance to an empty array.
    /// </summary>
    public ImmutableArray<T> AsImmutableArray() => array.IsDefault ? ImmutableArray<T>.Empty : array;

    /// <summary>Copies the elements into a new mutable array.</summary>
    public T[] ToArray()
    {
        var source = AsImmutableArray();
        var result = new T[source.Length];
        source.CopyTo(result);
        return result;
    }

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other)
    {
        var self = AsImmutableArray();
        var them = other.AsImmutableArray();

        if (self.Length != them.Length)
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < self.Length; i++)
        {
            if (!comparer.Equals(self[i], them[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var comparer = EqualityComparer<T>.Default;

        unchecked
        {
            var hash = 17;
            foreach (var item in AsImmutableArray())
            {
                hash = (hash * 31) + (item is null ? 0 : comparer.GetHashCode(item));
            }

            return hash;
        }
    }

    /// <summary>Returns an enumerator over the array elements.</summary>
    public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)AsImmutableArray()).GetEnumerator();

    /// <summary>Wraps an immutable array in an <see cref="EquatableArray{T}"/>.</summary>
    /// <param name="array">The array to wrap.</param>
    public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);

    /// <summary>Unwraps the underlying <see cref="ImmutableArray{T}"/>.</summary>
    /// <param name="array">The array to unwrap.</param>
    public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.AsImmutableArray();

    /// <summary>Compares two arrays for structural equality.</summary>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>Compares two arrays for structural inequality.</summary>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
