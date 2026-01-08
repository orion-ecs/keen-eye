using System.Runtime.CompilerServices;

namespace KeenEyes.Logging;

/// <summary>
/// Central manager for logging operations that coordinates multiple log providers.
/// </summary>
/// <remarks>
/// <para>
/// LogManager provides a facade for logging operations, distributing log messages
/// to all registered providers. It supports structured logging with properties,
/// scoped contexts, and level-based filtering.
/// </para>
/// <para>
/// Each World instance should have its own LogManager to maintain isolation
/// between ECS worlds.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var logManager = new LogManager();
/// logManager.AddProvider(new ConsoleLogProvider());
/// logManager.MinimumLevel = LogLevel.Debug;
///
/// logManager.Info("MySystem", "System initialized");
/// logManager.Debug("MySystem", "Processing {Count} entities", new { Count = 100 });
/// </code>
/// </example>
public sealed class LogManager : IDisposable
{
    private readonly List<ILogProvider> providers = [];
    private readonly Lock providersLock = new();
    private readonly AsyncLocal<LogScope?> currentScope = new();
    private bool disposed;

    /// <summary>
    /// Gets or sets the global minimum log level.
    /// </summary>
    /// <remarks>
    /// Messages below this level are not sent to any provider.
    /// Individual providers may have their own minimum levels that are higher.
    /// Defaults to <see cref="LogLevel.Trace"/> (all messages).
    /// </remarks>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets whether logging is enabled (at least one provider is registered).
    /// </summary>
    /// <remarks>
    /// Use this property for early-exit checks to avoid expensive string
    /// formatting when logging is disabled.
    /// </remarks>
    public bool IsEnabled
    {
        get
        {
            lock (providersLock)
            {
                return providers.Count > 0;
            }
        }
    }

    /// <summary>
    /// Gets the number of registered providers.
    /// </summary>
    public int ProviderCount
    {
        get
        {
            lock (providersLock)
            {
                return providers.Count;
            }
        }
    }

    /// <summary>
    /// Checks if logging is enabled for the specified level.
    /// </summary>
    /// <param name="level">The log level to check.</param>
    /// <returns>True if messages at this level would be logged.</returns>
    /// <remarks>
    /// Use this for early-exit checks before performing expensive operations
    /// to build log messages.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLevelEnabled(LogLevel level)
    {
        return level >= MinimumLevel && IsEnabled;
    }

    /// <summary>
    /// Adds a log provider to receive log messages.
    /// </summary>
    /// <param name="provider">The provider to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a provider with the same name is already registered.</exception>
    /// <remarks>
    /// Providers are called in the order they are added.
    /// Each provider must have a unique name.
    /// </remarks>
    public void AddProvider(ILogProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (providersLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            foreach (var existing in providers)
            {
                if (existing.Name == provider.Name)
                {
                    throw new InvalidOperationException($"A provider with name '{provider.Name}' is already registered.");
                }
            }

            providers.Add(provider);
        }
    }

    /// <summary>
    /// Removes a log provider by name.
    /// </summary>
    /// <param name="name">The name of the provider to remove.</param>
    /// <returns>True if the provider was found and removed; otherwise, false.</returns>
    public bool RemoveProvider(string name)
    {
        lock (providersLock)
        {
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].Name == name)
                {
                    providers[i].Dispose();
                    providers.RemoveAt(i);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a registered provider by name.
    /// </summary>
    /// <param name="name">The name of the provider to find.</param>
    /// <returns>The provider if found; otherwise, null.</returns>
    public ILogProvider? GetProvider(string name)
    {
        lock (providersLock)
        {
            foreach (var provider in providers)
            {
                if (provider.Name == name)
                {
                    return provider;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the first registered provider that implements the specified type.
    /// </summary>
    /// <typeparam name="T">The type to find.</typeparam>
    /// <returns>The first provider of type T, or null if none found.</returns>
    /// <remarks>
    /// Use this to find providers with specific capabilities, such as <see cref="ILogQueryable"/>.
    /// </remarks>
    public T? GetProvider<T>() where T : class
    {
        lock (providersLock)
        {
            foreach (var provider in providers)
            {
                if (provider is T typed)
                {
                    return typed;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Begins a new logging scope with optional properties.
    /// </summary>
    /// <param name="name">A name for the scope.</param>
    /// <param name="properties">Optional properties to include in all log messages within this scope.</param>
    /// <returns>A disposable scope object. Dispose it to end the scope.</returns>
    /// <remarks>
    /// <para>
    /// Scopes can be nested. Properties from parent scopes are included in child scopes,
    /// with child properties taking precedence for duplicate keys.
    /// </para>
    /// <para>
    /// Scopes are stored in AsyncLocal storage, so they work correctly across async operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using (logManager.BeginScope("EntityProcessing", new Dictionary&lt;string, object?&gt; { ["EntityId"] = 42 }))
    /// {
    ///     logManager.Debug("System", "Processing started");
    ///     // All log messages here include EntityId = 42
    /// }
    /// </code>
    /// </example>
    public LogScope BeginScope(string name, IReadOnlyDictionary<string, object?>? properties = null)
    {
        var scope = new LogScope(this, currentScope.Value, name, properties);
        currentScope.Value = scope;
        return scope;
    }

    /// <summary>
    /// Ends the specified scope and restores the parent scope.
    /// </summary>
    /// <param name="scope">The scope to end.</param>
    /// <remarks>
    /// This is called automatically when the scope is disposed.
    /// </remarks>
    internal void EndScope(LogScope scope)
    {
        if (currentScope.Value == scope)
        {
            currentScope.Value = scope.Parent;
        }
    }

    /// <summary>
    /// Logs a message at the specified level.
    /// </summary>
    /// <param name="level">The severity level of the message.</param>
    /// <param name="category">The category or source of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="properties">Optional structured properties to include with the message.</param>
    public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (level < MinimumLevel)
        {
            return;
        }

        // Merge scope properties with message properties
        var mergedProperties = MergeProperties(properties);

        // Take a snapshot of providers to avoid holding the lock during logging
        ILogProvider[] snapshot;
        lock (providersLock)
        {
            if (providers.Count == 0)
            {
                return;
            }

            snapshot = [.. providers];
        }

        foreach (var provider in snapshot)
        {
            if (level >= provider.MinimumLevel)
            {
                try
                {
                    provider.Log(level, category, message, mergedProperties);
                }
                catch (Exception)
                {
                    // Intentionally swallow exceptions from providers to prevent logging from disrupting application flow.
                    // Logging infrastructure must not throw exceptions that could crash the application.
                }
            }
        }
    }

    /// <summary>
    /// Logs a trace-level message.
    /// </summary>
    /// <param name="category">The category or source of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="properties">Optional structured properties.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trace(string category, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Trace, category, message, properties);

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <param name="category">The category or source of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="properties">Optional structured properties.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(string category, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Debug, category, message, properties);

    /// <summary>
    /// Logs an info-level message.
    /// </summary>
    /// <param name="category">The category or source of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="properties">Optional structured properties.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Info(string category, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Info, category, message, properties);

    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    /// <param name="category">The category or source of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="properties">Optional structured properties.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warning(string category, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Warning, category, message, properties);

    /// <summary>
    /// Logs an error-level message.
    /// </summary>
    /// <param name="category">The category or source of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="properties">Optional structured properties.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(string category, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Error, category, message, properties);

    /// <summary>
    /// Logs a fatal-level message.
    /// </summary>
    /// <param name="category">The category or source of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="properties">Optional structured properties.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fatal(string category, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Fatal, category, message, properties);

    /// <summary>
    /// Flushes all registered providers.
    /// </summary>
    /// <remarks>
    /// Call this before application shutdown to ensure all buffered messages are written.
    /// </remarks>
    public void Flush()
    {
        ILogProvider[] snapshot;
        lock (providersLock)
        {
            snapshot = [.. providers];
        }

        foreach (var provider in snapshot)
        {
            try
            {
                provider.Flush();
            }
            catch (Exception)
            {
                // Intentionally swallow flush exceptions - logging infrastructure must not throw.
            }
        }
    }

    /// <summary>
    /// Disposes the LogManager and all registered providers.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (providersLock)
        {
            disposed = true;
            foreach (var provider in providers)
            {
                try
                {
                    provider.Dispose();
                }
                catch (Exception)
                {
                    // Intentionally swallow disposal exceptions - logging infrastructure must not throw.
                }
            }

            providers.Clear();
        }
    }

    private IReadOnlyDictionary<string, object?>? MergeProperties(IReadOnlyDictionary<string, object?>? messageProperties)
    {
        var scopeProperties = currentScope.Value?.GetMergedProperties();

        if (scopeProperties == null)
        {
            return messageProperties;
        }

        if (messageProperties == null)
        {
            return scopeProperties;
        }

        // Merge scope and message properties, with message properties taking precedence
        var merged = new Dictionary<string, object?>(scopeProperties);
        foreach (var kvp in messageProperties)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }
}
