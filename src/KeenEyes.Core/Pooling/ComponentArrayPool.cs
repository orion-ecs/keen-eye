using System.Buffers;
using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Provides pooled arrays for component storage, reducing garbage collection pressure.
/// Uses .NET's built-in ArrayPool&lt;T&gt;.Shared for efficient memory reuse.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <remarks>
/// <para>
/// This pool is a thin wrapper around <see cref="ArrayPool{T}.Shared"/> that provides
/// ECS-specific functionality and tracking. It can be used independently of the
/// archetype system for custom storage scenarios.
/// </para>
/// <para>
/// Rented arrays may be larger than requested due to ArrayPool's bucketing strategy.
/// Always track the actual count of elements separately from the array length.
/// </para>
/// </remarks>
public static class ComponentArrayPool<T> where T : struct
{
    private static long totalRented;
    private static long totalReturned;

    /// <summary>
    /// Gets the total number of arrays rented from this pool.
    /// </summary>
    public static long TotalRented => Interlocked.Read(ref totalRented);

    /// <summary>
    /// Gets the total number of arrays returned to this pool.
    /// </summary>
    public static long TotalReturned => Interlocked.Read(ref totalReturned);

    /// <summary>
    /// Gets the number of arrays currently rented (not returned).
    /// </summary>
    public static long OutstandingCount => TotalRented - TotalReturned;

    /// <summary>
    /// Rents an array of at least the specified minimum length.
    /// </summary>
    /// <param name="minimumLength">The minimum required length.</param>
    /// <returns>An array of at least the specified length.</returns>
    /// <remarks>
    /// The returned array may be longer than requested. Track the actual
    /// element count separately. When done, return the array using
    /// <see cref="Return(T[], bool)"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Rent(int minimumLength)
    {
        Interlocked.Increment(ref totalRented);
        return ArrayPool<T>.Shared.Rent(minimumLength);
    }

    /// <summary>
    /// Returns an array to the pool.
    /// </summary>
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
    public static void Return(T[] array, bool clearArray = false)
    {
        ArrayPool<T>.Shared.Return(array, clearArray);
        Interlocked.Increment(ref totalReturned);
    }
}

/// <summary>
/// Non-generic helper for ComponentArrayPool operations.
/// Provides reflection-based access for dynamic component types.
/// </summary>
public static class ComponentArrayPool
{
    private static readonly Dictionary<Type, object> rentMethods = [];
    private static readonly Dictionary<Type, object> returnMethods = [];

    /// <summary>
    /// Rents an array for the specified component type.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <param name="minimumLength">The minimum required length.</param>
    /// <returns>A boxed array of the appropriate type.</returns>
    public static Array Rent(Type componentType, int minimumLength)
    {
        var poolType = typeof(ComponentArrayPool<>).MakeGenericType(componentType);
        var rentMethod = poolType.GetMethod("Rent")!;
        return (Array)rentMethod.Invoke(null, [minimumLength])!;
    }

    /// <summary>
    /// Returns an array to the pool for the specified component type.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <param name="array">The array to return.</param>
    /// <param name="clearArray">Whether to clear the array contents.</param>
    public static void Return(Type componentType, Array array, bool clearArray = false)
    {
        var poolType = typeof(ComponentArrayPool<>).MakeGenericType(componentType);
        var returnMethod = poolType.GetMethod("Return")!;
        returnMethod.Invoke(null, [array, clearArray]);
    }
}
