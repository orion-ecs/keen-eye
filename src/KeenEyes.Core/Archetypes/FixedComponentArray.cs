using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Fixed-capacity component storage for use in archetype chunks.
/// Unlike <see cref="ComponentArray{T}"/>, this array does not grow and uses
/// a simple pre-allocated array for predictable memory layout.
/// </summary>
/// <typeparam name="T">The component type to store.</typeparam>
/// <param name="capacity">The fixed capacity.</param>
public sealed class FixedComponentArray<T>(int capacity) : IComponentArray where T : struct, IComponent
{
    private readonly T[] data = new T[capacity];
    private int count;

    /// <inheritdoc />
    public Type ComponentType => typeof(T);

    /// <inheritdoc />
    public int Count => count;

    /// <inheritdoc />
    public int Capacity { get; } = capacity;

    /// <summary>
    /// Gets a reference to the component at the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(int index)
    {
        return ref data[index];
    }

    /// <summary>
    /// Gets a readonly reference to the component at the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadonly(int index)
    {
        return ref data[index];
    }

    /// <summary>
    /// Gets a span over the valid components in this array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        return data.AsSpan(0, count);
    }

    /// <summary>
    /// Gets a readonly span over the valid components in this array.
    /// </summary>
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
    /// <exception cref="InvalidOperationException">Thrown when the array is at capacity.</exception>
    public int Add(in T component)
    {
        if (count >= Capacity)
        {
            throw new InvalidOperationException($"Cannot add component: chunk is at capacity ({Capacity})");
        }

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
            data[index] = data[lastIndex];
        }

        data[lastIndex] = default;
        count--;
    }

    /// <inheritdoc />
    public void CopyTo(int sourceIndex, IComponentArray destination)
    {
        if (destination is not FixedComponentArray<T> typedDest)
        {
            if (destination is ComponentArray<T> growableDest)
            {
                growableDest.Add(in data[sourceIndex]);
                return;
            }
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
}
