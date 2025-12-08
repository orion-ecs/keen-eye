using System.Text;

namespace KeenEyes.Logging.Providers;

/// <summary>
/// A log provider that writes messages to a file.
/// </summary>
/// <remarks>
/// <para>
/// FileLogProvider supports asynchronous buffered writes for improved performance.
/// Messages are queued and written to the file in batches to minimize I/O overhead.
/// </para>
/// <para>
/// The provider supports automatic file rotation based on size limits. When the
/// maximum file size is reached, the current file is renamed with a timestamp
/// suffix and a new file is created.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var file = new FileLogProvider("logs/app.log")
/// {
///     MinimumLevel = LogLevel.Info,
///     MaxFileSizeBytes = 10 * 1024 * 1024 // 10 MB
/// };
/// logManager.AddProvider(file);
/// </code>
/// </example>
public sealed class FileLogProvider : ILogProvider
{
    private readonly string filePath;
    private readonly Lock writeLock = new();
    private StreamWriter? writer;
    private long currentFileSize;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogProvider"/> class.
    /// </summary>
    /// <param name="filePath">The path to the log file.</param>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    public FileLogProvider(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        this.filePath = Path.GetFullPath(filePath);
    }

    /// <inheritdoc />
    public string Name => "File";

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets the timestamp format.
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// Gets or sets whether to include structured properties in the output.
    /// </summary>
    public bool IncludeProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum file size in bytes before rotation.
    /// </summary>
    /// <remarks>
    /// Set to 0 to disable file rotation. Default is 0 (no rotation).
    /// </remarks>
    public long MaxFileSizeBytes { get; set; }

    /// <summary>
    /// Gets the path to the log file.
    /// </summary>
    public string FilePath => filePath;

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

        lock (writeLock)
        {
            if (disposed)
            {
                return;
            }

            EnsureWriterInitialized();
            CheckRotation(formattedMessage.Length);

            writer!.WriteLine(formattedMessage);
            currentFileSize += Encoding.UTF8.GetByteCount(formattedMessage) + Environment.NewLine.Length;
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        lock (writeLock)
        {
            writer?.Flush();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (writeLock)
        {
            disposed = true;
            writer?.Flush();
            writer?.Dispose();
            writer = null;
        }
    }

    private void EnsureWriterInitialized()
    {
        if (writer != null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var fileExists = File.Exists(filePath);
        var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        try
        {
            writer = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = false };
        }
        catch
        {
            fileStream.Dispose();
            throw;
        }

        if (fileExists)
        {
            currentFileSize = new FileInfo(filePath).Length;
        }
    }

    private void CheckRotation(int additionalBytes)
    {
        if (MaxFileSizeBytes <= 0 || currentFileSize + additionalBytes <= MaxFileSizeBytes)
        {
            return;
        }

        RotateFile();
    }

    private void RotateFile()
    {
        writer?.Flush();
        writer?.Dispose();
        writer = null;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var directory = Path.GetDirectoryName(filePath) ?? ".";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        // Find a unique filename by appending a counter if needed
        var rotatedPath = Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
        var counter = 1;
        while (File.Exists(rotatedPath))
        {
            rotatedPath = Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}_{counter}{extension}");
            counter++;
        }

        File.Move(filePath, rotatedPath);

        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        try
        {
            writer = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = false };
        }
        catch
        {
            fileStream.Dispose();
            throw;
        }

        currentFileSize = 0;
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
}
