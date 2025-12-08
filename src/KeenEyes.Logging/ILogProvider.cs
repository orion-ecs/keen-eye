namespace KeenEyes.Logging;

/// <summary>
/// Defines the contract for log output providers.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to create custom log destinations such as
/// files, databases, network endpoints, or third-party logging services.
/// </para>
/// <para>
/// Providers are registered with <see cref="LogManager"/> and receive
/// all log messages that pass the configured filters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomLogProvider : ILogProvider
/// {
///     public string Name => "Custom";
///     public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
///
///     public void Log(LogLevel level, string category, string message, IReadOnlyDictionary&lt;string, object?&gt;? properties)
///     {
///         // Write to custom destination
///     }
///
///     public void Flush() { }
///     public void Dispose() { }
/// }
/// </code>
/// </example>
public interface ILogProvider : IDisposable
{
    /// <summary>
    /// Gets the unique name of this provider.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the provider when removing or configuring it.
    /// Each provider registered with <see cref="LogManager"/> must have a unique name.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets or sets the minimum log level this provider will handle.
    /// </summary>
    /// <remarks>
    /// Messages below this level are ignored by this provider.
    /// The default is typically <see cref="LogLevel.Trace"/> to receive all messages.
    /// </remarks>
    LogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Writes a log message to the provider's output.
    /// </summary>
    /// <param name="level">The severity level of the message.</param>
    /// <param name="category">The category or source of the message (e.g., class name, subsystem).</param>
    /// <param name="message">The formatted log message.</param>
    /// <param name="properties">Optional structured properties associated with the message.</param>
    /// <remarks>
    /// <para>
    /// This method should be thread-safe as it may be called from multiple threads concurrently.
    /// </para>
    /// <para>
    /// Implementations should handle exceptions internally and not throw,
    /// as logging failures should not disrupt application flow.
    /// </para>
    /// </remarks>
    void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties);

    /// <summary>
    /// Ensures all buffered log messages are written to the output.
    /// </summary>
    /// <remarks>
    /// Call this method before application shutdown or when immediate
    /// persistence of log messages is required.
    /// </remarks>
    void Flush();
}
