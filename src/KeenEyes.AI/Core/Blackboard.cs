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
    public void Set<T>(string key, T value) where T : notnull
    {
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
        if (data.TryGetValue(key, out var value) && value is T typed)
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
        if (data.TryGetValue(key, out var value) && value is T typed)
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
        if (data.TryGetValue(key, out var obj) && obj is T typed)
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
}
