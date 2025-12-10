using System.Text;

namespace KeenEyes.Logging.Providers;

/// <summary>
/// A log provider that writes messages to the console with color-coded output.
/// </summary>
/// <remarks>
/// <para>
/// ConsoleLogProvider formats log messages with timestamps, log levels, and categories.
/// Log levels are color-coded for easier visual scanning:
/// </para>
/// <list type="bullet">
/// <item><description>Trace: Gray</description></item>
/// <item><description>Debug: Cyan</description></item>
/// <item><description>Info: White</description></item>
/// <item><description>Warning: Yellow</description></item>
/// <item><description>Error: Red</description></item>
/// <item><description>Fatal: Dark Red</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var console = new ConsoleLogProvider { MinimumLevel = LogLevel.Debug };
/// logManager.AddProvider(console);
/// </code>
/// </example>
public sealed class ConsoleLogProvider(TextWriter output, TextWriter errorOutput) : ILogProvider
{
    private readonly Lock consoleLock = new();
    private readonly TextWriter output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly TextWriter errorOutput = errorOutput ?? throw new ArgumentNullException(nameof(errorOutput));
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogProvider"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor uses the default console output and error streams.
    /// </remarks>
    public ConsoleLogProvider()
        : this(Console.Out, Console.Error)
    {
    }

    /// <inheritdoc />
    public string Name => "Console";

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets whether to use colors in the output.
    /// </summary>
    /// <remarks>
    /// Defaults to true. Set to false when outputting to a non-console destination
    /// or when colors are not supported.
    /// </remarks>
    public bool UseColors { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp format.
    /// </summary>
    /// <remarks>
    /// Defaults to "HH:mm:ss.fff" for a compact time-only format.
    /// Use "yyyy-MM-dd HH:mm:ss.fff" for a full date-time format.
    /// </remarks>
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

        var writer = level >= LogLevel.Error ? errorOutput : output;

        lock (consoleLock)
        {
            if (UseColors && (writer == Console.Out || writer == Console.Error))
            {
                WriteColored(level, formattedMessage, writer);
            }
            else
            {
                writer.WriteLine(formattedMessage);
            }
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        lock (consoleLock)
        {
            output.Flush();
            errorOutput.Flush();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Flush();
        // Don't dispose Console.Out/Error - they're shared resources
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
        sb.Append('[').Append(Sanitize(category)).Append("] ");
        sb.Append(Sanitize(message));

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
                sb.Append(Sanitize(kvp.Key)).Append('=');
                sb.Append(Sanitize(kvp.Value?.ToString() ?? "null"));
            }

            sb.Append('}');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Sanitizes a string to prevent log injection attacks by replacing newlines and control characters.
    /// </summary>
    private static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        // Replace newlines and carriage returns to prevent log injection
        return value
            .Replace("\r\n", "\\r\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }

    private static void WriteColored(LogLevel level, string message, TextWriter writer)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = GetLevelColor(level);
            writer.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    private static ConsoleColor GetLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.Cyan,
        LogLevel.Info => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Fatal => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };
}
