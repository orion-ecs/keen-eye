using System.Diagnostics.CodeAnalysis;

namespace KeenEyes.AI;

/// <summary>
/// A dictionary-based data store for sharing state between AI nodes.
/// </summary>
/// <remarks>
/// <para>
/// The blackboard provides a key-value store for AI systems to share data.
/// Common uses include storing targets, waypoints, and intermediate computation results.
/// </para>
/// <para>
/// Use <see cref="BBKeys"/> for standard key names to ensure consistency across actions.
/// </para>
/// </remarks>
public sealed class Blackboard
{
    private readonly Dictionary<string, object> data = [];

    /// <summary>
    /// Sets a value in the blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <remarks>
    /// Value-type values are stored in a reusable typed cell rather than boxed directly, so
    /// repeatedly writing the same key with the same value type (a common per-frame pattern,
    /// e.g. updating a target position) reuses the existing cell and allocates nothing after
    /// the first write. Reference-type values are stored as-is.
    /// </remarks>
    public void Set<T>(string key, T value) where T : notnull
    {
        if (typeof(T).IsValueType)
        {
            // Reuse an existing cell of the exact same type to avoid re-boxing on hot writes.
            if (data.TryGetValue(key, out var existing) && existing is ValueCell<T> cell)
            {
                cell.Value = value;
                return;
            }

            data[key] = new ValueCell<T>(value);
            return;
        }

        data[key] = value;
    }

    /// <summary>
    /// Gets a value from the blackboard.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to retrieve.</param>
    /// <returns>The value if found and of the correct type; otherwise, default.</returns>
    public T? Get<T>(string key)
    {
        if (data.TryGetValue(key, out var value) && TryUnwrap<T>(value, out var typed))
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// Gets a value from the blackboard with a default fallback.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The value if found and of the correct type; otherwise, the default value.</returns>
    public T Get<T>(string key, T defaultValue)
    {
        if (data.TryGetValue(key, out var value) && TryUnwrap<T>(value, out var typed))
        {
            return typed;
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a value from the blackboard.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="value">When this method returns, contains the value if found.</param>
    /// <returns>True if the key was found and the value is of the correct type; otherwise, false.</returns>
    public bool TryGet<T>(string key, out T? value)
    {
        if (data.TryGetValue(key, out var obj) && TryUnwrap<T>(obj, out var typed))
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Checks if the blackboard contains a key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists; otherwise, false.</returns>
    public bool Has(string key) => data.ContainsKey(key);

    /// <summary>
    /// Removes a key from the blackboard.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was removed; otherwise, false.</returns>
    public bool Remove(string key) => data.Remove(key);

    /// <summary>
    /// Clears all data from the blackboard.
    /// </summary>
    public void Clear() => data.Clear();

    /// <summary>
    /// Gets the number of entries in the blackboard.
    /// </summary>
    public int Count => data.Count;

    /// <summary>
    /// Enumerates every entry as a key and its boxed value, for inspection by debug and
    /// editor tooling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a read-only inspection surface, not a storage view: value-type entries are
    /// returned as their real boxed value with their real runtime type, so a caller sees
    /// <c>Single</c> / <c>1.5</c> rather than any internal storage representation. It is the
    /// enumeration counterpart to <see cref="TryGet{T}"/> and performs no mutation.
    /// </para>
    /// <para>
    /// Intended for diagnostics (debug panels, the test bridge, AI inspectors) rather than
    /// gameplay code: enumerating boxes every value-type entry, so prefer the typed
    /// <see cref="Get{T}(string)"/> / <see cref="TryGet{T}"/> accessors on hot paths.
    /// Enumerating while the blackboard is being modified throws, as with any dictionary.
    /// </para>
    /// </remarks>
    public IEnumerable<KeyValuePair<string, object>> Entries
    {
        get
        {
            foreach (var kvp in data)
            {
                yield return new KeyValuePair<string, object>(kvp.Key, Unwrap(kvp.Value));
            }
        }
    }

    // Resolves a stored value to the requested type, matching the semantics of the original
    // `stored is T` check. Values written through the value-type fast path live inside a
    // ValueCell; the exact-type branch unwraps them without boxing, while the IValueCell branch
    // falls back to the boxed value so cross-type / object / interface lookups behave exactly as
    // a plain Dictionary<string, object> store would.
    private static bool TryUnwrap<T>(object stored, [MaybeNullWhen(false)] out T typed)
    {
        if (stored is ValueCell<T> exact)
        {
            typed = exact.Value;
            return true;
        }

        if (stored is IValueCell cell)
        {
            if (cell.BoxedValue is T boxed)
            {
                typed = boxed;
                return true;
            }

            typed = default;
            return false;
        }

        if (stored is T direct)
        {
            typed = direct;
            return true;
        }

        typed = default;
        return false;
    }

    // Recovers the value a caller stored, undoing the value-type cell wrapper so no storage
    // detail escapes through the inspection surface.
    private static object Unwrap(object stored) => stored is IValueCell cell ? cell.BoxedValue : stored;

    // Non-generic view over a typed value cell, used to recover the boxed value on the rare
    // cross-type lookup path.
    private interface IValueCell
    {
        object BoxedValue { get; }
    }

    // Holds a value-type value so repeated same-type writes mutate in place instead of
    // allocating a fresh box each time. Only ever instantiated for value types (see Set),
    // so the boxed value is never null.
    private sealed class ValueCell<T>(T value) : IValueCell
    {
        public T Value = value;

        public object BoxedValue => Value!;
    }
}
