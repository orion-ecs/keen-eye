namespace KeenEyes.Logging;

/// <summary>
/// Represents a logging scope that adds contextual properties to all log messages
/// within its lifetime.
/// </summary>
/// <remarks>
/// <para>
/// Scopes provide a way to add structured context to log messages without
/// modifying each individual log call. Properties defined in the scope
/// are automatically included in all log messages until the scope is disposed.
/// </para>
/// <para>
/// Scopes can be nested. When nested, properties from all active scopes
/// are merged, with inner scopes taking precedence for duplicate keys.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using (logManager.BeginScope("Processing", new Dictionary&lt;string, object?&gt;
/// {
///     ["EntityId"] = entity.Id,
///     ["Component"] = "Position"
/// }))
/// {
///     logManager.Info("System", "Processing entity");
///     // Log output includes EntityId and Component properties
/// }
/// </code>
/// </example>
public sealed class LogScope : IDisposable
{
    private readonly LogManager manager;
    private readonly LogScope? parent;
    private readonly string name;
    private readonly IReadOnlyDictionary<string, object?>? properties;
    private bool disposed;

    internal LogScope(LogManager manager, LogScope? parent, string name, IReadOnlyDictionary<string, object?>? properties)
    {
        this.manager = manager;
        this.parent = parent;
        this.name = name;
        this.properties = properties;
    }

    /// <summary>
    /// Gets the name of this scope.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets the parent scope, if any.
    /// </summary>
    internal LogScope? Parent => parent;

    /// <summary>
    /// Gets the properties associated with this scope.
    /// </summary>
    internal IReadOnlyDictionary<string, object?>? Properties => properties;

    /// <summary>
    /// Gets all properties from this scope and all parent scopes, merged together.
    /// </summary>
    /// <returns>A dictionary containing all scope properties, or null if no properties exist.</returns>
    /// <remarks>
    /// Properties from child scopes override properties with the same key from parent scopes.
    /// </remarks>
    internal IReadOnlyDictionary<string, object?>? GetMergedProperties()
    {
        if (parent == null)
        {
            return properties;
        }

        var parentProperties = parent.GetMergedProperties();
        if (parentProperties == null)
        {
            return properties;
        }

        if (properties == null)
        {
            return parentProperties;
        }

        // Merge parent and current properties, with current taking precedence
        var merged = new Dictionary<string, object?>(parentProperties);
        foreach (var kvp in properties)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    /// <summary>
    /// Ends this logging scope.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        manager.EndScope(this);
    }
}
