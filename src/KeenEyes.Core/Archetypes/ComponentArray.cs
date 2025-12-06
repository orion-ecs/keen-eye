using System.Buffers;
using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Typed component storage for an archetype, using pooled arrays for efficiency.
/// Stores components contiguously in memory for cache-friendly iteration.
/// </summary>
/// <typeparam name="T">The component type to store.</typeparam>
public sealed class ComponentArray<T> : IComponentArray, IDisposable where T : struct
{
    private const int DefaultCapacity = 16;
    private const int MaxPooledArraySize = 1024 * 1024; // 1MB worth of elements

    private T[] data;
    private int count;
    private bool isPooled;

    /// <inheritdoc />
    public Type ComponentType => typeof(T);

    /// <inheritdoc />
    public int Count => count;

    /// <inheritdoc />
    public int Capacity => data.Length;

    /// <summary>
    /// Creates a new component array with default capacity.
    /// </summary>
    public ComponentArray() : this(DefaultCapacity)
    {
    }

    /// <summary>
    /// Creates a new component array with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity.</param>
    public ComponentArray(int initialCapacity)
    {
        var capacity = Math.Max(DefaultCapacity, initialCapacity);

        // Use ArrayPool for reasonable sizes
        if (capacity <= MaxPooledArraySize)
        {
            data = ArrayPool<T>.Shared.Rent(capacity);
            isPooled = true;
        }
        else
        {
            data = new T[capacity];
            isPooled = false;
        }
    }

    /// <summary>
    /// Gets a reference to the component at the specified index.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>A reference to the component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(int index)
    {
        return ref data[index];
    }

    /// <summary>
    /// Gets a readonly reference to the component at the specified index.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>A readonly reference to the component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadonly(int index)
    {
        return ref data[index];
    }

    /// <summary>
    /// Gets a span over the valid components in this array.
    /// </summary>
    /// <returns>A span of the components.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        return data.AsSpan(0, count);
    }

    /// <summary>
    /// Gets a readonly span over the valid components in this array.
    /// </summary>
    /// <returns>A readonly span of the components.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan()
    {
        return data.AsSpan(0, count);
    }

    /// <summary>
    /// Adds a component to the array.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <returns>The index where the component was added.</returns>
    public int Add(in T component)
    {
        EnsureCapacity(count + 1);
        var index = count;
        data[index] = component;
        count++;
        return index;
    }

    /// <inheritdoc />
    public int AddBoxed(object value)
    {
        return Add((T)value);
    }

    /// <summary>
    /// Sets the component value at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="component">The new component value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, in T component)
    {
        data[index] = component;
    }

    /// <inheritdoc />
    public void RemoveAtSwapBack(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var lastIndex = count - 1;

        if (index != lastIndex)
        {
            // Swap with last element
            data[index] = data[lastIndex];
        }

        // Clear the last slot and decrement count
        data[lastIndex] = default;
        count--;
    }

    /// <inheritdoc />
    public void CopyTo(int sourceIndex, IComponentArray destination)
    {
        if (destination is not ComponentArray<T> typedDest)
        {
            throw new InvalidOperationException(
                $"Cannot copy {typeof(T).Name} to array of type {destination.ComponentType.Name}");
        }

        typedDest.Add(in data[sourceIndex]);
    }

    /// <inheritdoc />
    public object GetBoxed(int index)
    {
        return data[index];
    }

    /// <inheritdoc />
    public void SetBoxed(int index, object value)
    {
        data[index] = (T)value;
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (count > 0)
        {
            Array.Clear(data, 0, count);
            count = 0;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (required <= data.Length)
        {
            return;
        }

        var newCapacity = Math.Max(data.Length * 2, required);

        T[] newArray;
        var newIsPooled = false;

        if (newCapacity <= MaxPooledArraySize)
        {
            newArray = ArrayPool<T>.Shared.Rent(newCapacity);
            newIsPooled = true;
        }
        else
        {
            newArray = new T[newCapacity];
        }

        // Copy existing data
        Array.Copy(data, newArray, count);

        // Return old array to pool if it was pooled
        if (isPooled)
        {
            ArrayPool<T>.Shared.Return(data, clearArray: true);
        }

        data = newArray;
        isPooled = newIsPooled;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (isPooled && data != null)
        {
            ArrayPool<T>.Shared.Return(data, clearArray: true);
            isPooled = false;
        }
#pragma warning disable CS8625 // Cannot convert null literal - intentional for dispose pattern
        data = null!;
#pragma warning restore CS8625
        count = 0;
    }
}
