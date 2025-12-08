namespace KeenEyes.Logging.Providers;

/// <summary>
/// A log provider that discards all messages without processing.
/// </summary>
/// <remarks>
/// <para>
/// NullLogProvider is useful for:
/// </para>
/// <list type="bullet">
/// <item><description>Disabling logging without code changes</description></item>
/// <item><description>Performance benchmarking with minimal logging overhead</description></item>
/// <item><description>Testing scenarios where log output should be suppressed</description></item>
/// <item><description>Placeholder when a provider is required but output is not needed</description></item>
/// </list>
/// <para>
/// All operations are no-ops with minimal overhead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Disable all logging
/// logManager.RemoveProvider("Console");
/// logManager.AddProvider(new NullLogProvider());
/// </code>
/// </example>
public sealed class NullLogProvider : ILogProvider
{
    /// <inheritdoc />
    public string Name => "Null";

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <inheritdoc />
    public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties)
    {
        // Intentionally empty - discards all messages
    }

    /// <inheritdoc />
    public void Flush()
    {
        // Nothing to flush
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose
    }
}
