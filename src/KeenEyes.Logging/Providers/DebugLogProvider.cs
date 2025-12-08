using System.Diagnostics;
using System.Text;

namespace KeenEyes.Logging.Providers;

/// <summary>
/// A log provider that writes messages to the debug output (IDE debug console).
/// </summary>
/// <remarks>
/// <para>
/// DebugLogProvider writes to <see cref="System.Diagnostics.Debug"/>, which outputs
/// to the IDE's debug console (Output window in Visual Studio, Debug Console in VS Code, etc.).
/// </para>
/// <para>
/// Output is only generated when a debugger is attached or when the DEBUG
/// conditional compilation symbol is defined. In release builds without a debugger,
/// this provider has zero overhead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var debug = new DebugLogProvider { MinimumLevel = LogLevel.Debug };
/// logManager.AddProvider(debug);
/// </code>
/// </example>
public sealed class DebugLogProvider : ILogProvider
{
    private bool disposed;

    /// <inheritdoc />
    public string Name => "Debug";

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets the timestamp format.
    /// </summary>
    public string TimestampFormat { get; set; } = "HH:mm:ss.fff";

    /// <summary>
    /// Gets or sets whether to include structured properties in the output.
    /// </summary>
    public bool IncludeProperties { get; set; } = true;

    /// <inheritdoc />
    public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties)
    {
        if (disposed || level < MinimumLevel)
        {
            return;
        }

        var timestamp = DateTime.Now.ToString(TimestampFormat);
        var levelText = GetLevelText(level);
        var formattedMessage = FormatMessage(timestamp, levelText, category, message, properties);

        Debug.WriteLine(formattedMessage);
    }

    /// <inheritdoc />
    public void Flush()
    {
        Debug.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposed = true;
    }

    private static string GetLevelText(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Fatal => "FTL",
        _ => "???"
    };

    private string FormatMessage(
        string timestamp,
        string levelText,
        string category,
        string message,
        IReadOnlyDictionary<string, object?>? properties)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(timestamp).Append("] ");
        sb.Append(levelText).Append(' ');
        sb.Append('[').Append(category).Append("] ");
        sb.Append(message);

        if (IncludeProperties && properties != null && properties.Count > 0)
        {
            sb.Append(" {");
            var first = true;
            foreach (var kvp in properties)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                first = false;
                sb.Append(kvp.Key).Append('=');
                sb.Append(kvp.Value?.ToString() ?? "null");
            }

            sb.Append('}');
        }

        return sb.ToString();
    }
}
