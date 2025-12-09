using System.Buffers;
using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Per-world manager for pooled component arrays.
/// Provides pooled arrays for component storage, reducing garbage collection pressure.
/// Uses .NET's built-in ArrayPool&lt;T&gt;.Shared for efficient memory reuse.
/// </summary>
/// <remarks>
/// <para>
/// This manager is instance-based, with one instance per World. Each World has its own
/// independent pool statistics and tracking, enabling true multi-world isolation.
/// </para>
/// <para>
/// Rented arrays may be larger than requested due to ArrayPool's bucketing strategy.
/// Always track the actual count of elements separately from the array length.
/// </para>
/// </remarks>
internal sealed class ComponentArrayPoolManager
{
    private long totalRented;
    private long totalReturned;

    private delegate Array RentDelegate(int minimumLength);
    private delegate void ReturnDelegate(Array array, bool clearArray);

    // Static delegate cache for AOT - shared across all worlds but doesn't affect behavior
    private static readonly Dictionary<Type, RentDelegate> rentDelegates = [];
    private static readonly Dictionary<Type, ReturnDelegate> returnDelegates = [];
    private static readonly object lockObj = new();

    /// <summary>
    /// Gets the total number of arrays rented from this pool.
    /// </summary>
    public long TotalRented => Interlocked.Read(ref totalRented);

    /// <summary>
    /// Gets the total number of arrays returned to this pool.
    /// </summary>
    public long TotalReturned => Interlocked.Read(ref totalReturned);

    /// <summary>
    /// Gets the number of arrays currently rented (not returned).
    /// </summary>
    public long OutstandingCount => TotalRented - TotalReturned;

    /// <summary>
    /// Rents an array of at least the specified minimum length.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="minimumLength">The minimum required length.</param>
    /// <returns>An array of at least the specified length.</returns>
    /// <remarks>
    /// The returned array may be longer than requested. Track the actual
    /// element count separately. When done, return the array using
    /// <see cref="Return{T}(T[], bool)"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] Rent<T>(int minimumLength) where T : struct
    {
        Interlocked.Increment(ref totalRented);
        return ArrayPool<T>.Shared.Rent(minimumLength);
    }

    /// <summary>
    /// Returns an array to the pool.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="array">The array to return.</param>
    /// <param name="clearArray">Whether to clear the array contents before returning.</param>
    /// <remarks>
    /// <para>
    /// It is recommended to set <paramref name="clearArray"/> to true for
    /// reference types or when arrays may contain sensitive data.
    /// </para>
    /// <para>
    /// After calling this method, the caller should not access the array again.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return<T>(T[] array, bool clearArray = false) where T : struct
    {
        ArrayPool<T>.Shared.Return(array, clearArray);
        Interlocked.Increment(ref totalReturned);
    }

    /// <summary>
    /// Registers a component type for non-generic pool access.
    /// </summary>
    /// <typeparam name="T">The component type to register.</typeparam>
    /// <remarks>
    /// This method should be called during initialization for all component types that will
    /// be accessed via the non-generic <see cref="Rent(Type, int)"/> and <see cref="Return(Type, Array, bool)"/> methods.
    /// This is required for AOT compatibility.
    /// </remarks>
    public static void Register<T>() where T : struct
    {
        var type = typeof(T);
        lock (lockObj)
        {
            if (!rentDelegates.ContainsKey(type))
            {
                // Create delegates that use ArrayPool<T>.Shared directly
                rentDelegates[type] = static minLength => ArrayPool<T>.Shared.Rent(minLength);
                returnDelegates[type] = static (array, clear) => ArrayPool<T>.Shared.Return((T[])array, clear);
            }
        }
    }

    /// <summary>
    /// Rents an array for the specified component type.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <param name="minimumLength">The minimum required length.</param>
    /// <returns>A boxed array of the appropriate type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the component type has not been registered via <see cref="Register{T}"/>.
    /// </exception>
    /// <remarks>
    /// For AOT compatibility, the component type must be registered via <see cref="Register{T}"/>
    /// before calling this method. For known types at compile time, use <see cref="Rent{T}(int)"/>
    /// directly instead.
    /// </remarks>
    public Array Rent(Type componentType, int minimumLength)
    {
        if (!rentDelegates.TryGetValue(componentType, out var rentDelegate))
        {
            throw new InvalidOperationException(
                $"Component type {componentType.Name} has not been registered with ComponentArrayPoolManager. " +
                $"Call ComponentArrayPoolManager.Register<{componentType.Name}>() before using non-generic Rent/Return methods.");
        }

        Interlocked.Increment(ref totalRented);
        return rentDelegate(minimumLength);
    }

    /// <summary>
    /// Returns an array to the pool for the specified component type.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <param name="array">The array to return.</param>
    /// <param name="clearArray">Whether to clear the array contents.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the component type has not been registered via <see cref="Register{T}"/>.
    /// </exception>
    /// <remarks>
    /// For AOT compatibility, the component type must be registered via <see cref="Register{T}"/>
    /// before calling this method. For known types at compile time, use <see cref="Return{T}(T[], bool)"/>
    /// directly instead.
    /// </remarks>
    public void Return(Type componentType, Array array, bool clearArray = false)
    {
        if (!returnDelegates.TryGetValue(componentType, out var returnDelegate))
        {
            throw new InvalidOperationException(
                $"Component type {componentType.Name} has not been registered with ComponentArrayPoolManager. " +
                $"Call ComponentArrayPoolManager.Register<{componentType.Name}>() before using non-generic Rent/Return methods.");
        }

        returnDelegate(array, clearArray);
        Interlocked.Increment(ref totalReturned);
    }

    /// <summary>
    /// Clears all statistics for this pool manager.
    /// Called during World disposal.
    /// </summary>
    internal void Clear()
    {
        Interlocked.Exchange(ref totalRented, 0);
        Interlocked.Exchange(ref totalReturned, 0);
    }
}
