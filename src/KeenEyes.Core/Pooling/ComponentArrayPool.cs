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
/// Provides AOT-compatible access for dynamic component types using explicit registration.
/// </summary>
/// <remarks>
/// <para>
/// This class uses explicit delegate registration to avoid reflection, making it compatible with
/// Native AOT compilation. Component types must be registered via <see cref="Register{T}"/>
/// before using the non-generic Rent/Return methods.
/// </para>
/// <para>
/// For AOT scenarios, call <see cref="Register{T}"/> during startup for all component types
/// that will be used dynamically. Generic <see cref="ComponentArrayPool{T}"/> can be used
/// directly without registration.
/// </para>
/// </remarks>
public static class ComponentArrayPool
{
    private delegate Array RentDelegate(int minimumLength);
    private delegate void ReturnDelegate(Array array, bool clearArray);

    private static readonly Dictionary<Type, RentDelegate> rentDelegates = [];
    private static readonly Dictionary<Type, ReturnDelegate> returnDelegates = [];
    private static readonly object lockObj = new();

    /// <summary>
    /// Registers a component type for non-generic pool access.
    /// </summary>
    /// <typeparam name="T">The component type to register.</typeparam>
    /// <remarks>
    /// This method should be called during initialization for all component types that will
    /// be accessed via the non-generic <see cref="Rent"/> and <see cref="Return"/> methods.
    /// This is required for AOT compatibility.
    /// </remarks>
    public static void Register<T>() where T : struct
    {
        var type = typeof(T);
        lock (lockObj)
        {
            if (!rentDelegates.ContainsKey(type))
            {
                rentDelegates[type] = static minLength => ComponentArrayPool<T>.Rent(minLength);
                returnDelegates[type] = static (array, clear) => ComponentArrayPool<T>.Return((T[])array, clear);
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
    /// before calling this method. For known types at compile time, use <see cref="ComponentArrayPool{T}.Rent"/>
    /// directly instead.
    /// </remarks>
    public static Array Rent(Type componentType, int minimumLength)
    {
        if (!rentDelegates.TryGetValue(componentType, out var rentDelegate))
        {
            throw new InvalidOperationException(
                $"Component type {componentType.Name} has not been registered with ComponentArrayPool. " +
                $"Call ComponentArrayPool.Register<{componentType.Name}>() before using non-generic Rent/Return methods.");
        }

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
    /// before calling this method. For known types at compile time, use <see cref="ComponentArrayPool{T}.Return"/>
    /// directly instead.
    /// </remarks>
    public static void Return(Type componentType, Array array, bool clearArray = false)
    {
        if (!returnDelegates.TryGetValue(componentType, out var returnDelegate))
        {
            throw new InvalidOperationException(
                $"Component type {componentType.Name} has not been registered with ComponentArrayPool. " +
                $"Call ComponentArrayPool.Register<{componentType.Name}>() before using non-generic Rent/Return methods.");
        }

        returnDelegate(array, clearArray);
    }
}
