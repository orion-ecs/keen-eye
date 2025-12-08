using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Manages singleton (world-level) data storage.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all singleton operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Singletons are world-level data not tied to any entity. They are useful for
/// storing global state like time, input, configuration, or other resources that
/// systems need to access.
/// </para>
/// </remarks>
internal sealed class SingletonManager
{
    private readonly Dictionary<Type, object> singletons = [];

    /// <summary>
    /// Sets or updates a singleton value.
    /// </summary>
    /// <typeparam name="T">The singleton type. Must be a value type.</typeparam>
    /// <param name="value">The singleton value to store.</param>
    internal void SetSingleton<T>(in T value) where T : struct
    {
        singletons[typeof(T)] = value;
    }

    /// <summary>
    /// Gets a singleton value by reference, allowing direct modification.
    /// </summary>
    /// <typeparam name="T">The singleton type to retrieve.</typeparam>
    /// <returns>A reference to the singleton data for zero-copy access.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no singleton of type <typeparamref name="T"/> exists.
    /// </exception>
    internal ref T GetSingleton<T>() where T : struct
    {
        if (!singletons.TryGetValue(typeof(T), out var boxed))
        {
            throw new InvalidOperationException(
                $"Singleton of type {typeof(T).Name} does not exist in this world. " +
                $"Use SetSingleton<{typeof(T).Name}>() to add it first.");
        }

        return ref Unsafe.Unbox<T>(boxed);
    }

    /// <summary>
    /// Attempts to get a singleton value.
    /// </summary>
    /// <typeparam name="T">The singleton type to retrieve.</typeparam>
    /// <param name="value">When this method returns true, contains the singleton value.</param>
    /// <returns>True if the singleton exists; false otherwise.</returns>
    internal bool TryGetSingleton<T>(out T value) where T : struct
    {
        if (singletons.TryGetValue(typeof(T), out var boxed))
        {
            value = (T)boxed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Checks if a singleton of the specified type exists.
    /// </summary>
    /// <typeparam name="T">The singleton type to check for.</typeparam>
    /// <returns>True if the singleton exists; false otherwise.</returns>
    internal bool HasSingleton<T>() where T : struct
    {
        return singletons.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Removes a singleton.
    /// </summary>
    /// <typeparam name="T">The singleton type to remove.</typeparam>
    /// <returns>True if the singleton was removed; false if it didn't exist.</returns>
    internal bool RemoveSingleton<T>() where T : struct
    {
        return singletons.Remove(typeof(T));
    }

    /// <summary>
    /// Clears all singletons.
    /// </summary>
    internal void Clear()
    {
        singletons.Clear();
    }

    /// <summary>
    /// Gets all singletons as type-value pairs.
    /// </summary>
    /// <returns>An enumerable of all singleton types and their boxed values.</returns>
    /// <remarks>
    /// <para>
    /// This method is primarily intended for serialization and debugging scenarios.
    /// Values are boxed, so avoid using this in performance-critical paths.
    /// </para>
    /// </remarks>
    internal IEnumerable<(Type Type, object Value)> GetAllSingletons()
    {
        foreach (var kvp in singletons)
        {
            yield return (kvp.Key, kvp.Value);
        }
    }
}
