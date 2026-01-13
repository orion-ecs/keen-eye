namespace KeenEyes.Shaders.HotReload;

/// <summary>
/// Watches for changes to KESL shader files and triggers recompilation.
/// </summary>
/// <param name="registry">The shader registry to update.</param>
/// <param name="backend">The shader backend to compile for.</param>
public sealed class KeslFileWatcher(ShaderRegistry registry, ShaderBackend backend = ShaderBackend.GLSL) : IDisposable
{
    private FileSystemWatcher? watcher;
    private readonly Lock syncLock = new();
    private bool isDisposed;

    /// <summary>
    /// Event raised when a shader file is changed and successfully recompiled.
    /// </summary>
    public event Action<string, ShaderRecompilationResult>? OnShaderRecompiled;

    /// <summary>
    /// Event raised when shader compilation fails.
    /// </summary>
    public event Action<string, IReadOnlyList<string>>? OnCompilationError;

    /// <summary>
    /// Gets whether the watcher is currently active.
    /// </summary>
    public bool IsWatching => watcher?.EnableRaisingEvents ?? false;

    /// <summary>
    /// Gets the directory being watched.
    /// </summary>
    public string? WatchDirectory { get; private set; }

    /// <summary>
    /// Starts watching a directory for KESL file changes.
    /// </summary>
    /// <param name="directory">The directory to watch.</param>
    /// <param name="includeSubdirectories">Whether to include subdirectories.</param>
    public void Watch(string directory, bool includeSubdirectories = true)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        Stop();

        lock (syncLock)
        {
            watcher = new FileSystemWatcher(directory, "*.kesl")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = includeSubdirectories,
                EnableRaisingEvents = true
            };

            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Renamed += OnFileRenamed;

            WatchDirectory = directory;
        }
    }

    /// <summary>
    /// Stops watching for file changes.
    /// </summary>
    public void Stop()
    {
        lock (syncLock)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnFileChanged;
                watcher.Created -= OnFileChanged;
                watcher.Renamed -= OnFileRenamed;
                watcher.Dispose();
                watcher = null;
                WatchDirectory = null;
            }
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        ProcessFile(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        ProcessFile(e.FullPath);
    }

    private void ProcessFile(string filePath)
    {
        try
        {
            // Wait a bit for file to be fully written
            Thread.Sleep(100);

            if (!File.Exists(filePath))
            {
                return;
            }

            var source = File.ReadAllText(filePath);
            var shaderName = Path.GetFileNameWithoutExtension(filePath);

            // Try to compile the shader
            var result = CompileShader(source, filePath);

            if (result.Success)
            {
                // Update the registry
                registry.Update(shaderName, result.GeneratedCode!, backend);
                OnShaderRecompiled?.Invoke(filePath, result);
            }
            else
            {
                OnCompilationError?.Invoke(filePath, result.Errors);
            }
        }
        catch (Exception ex)
        {
            OnCompilationError?.Invoke(filePath, [$"Failed to process file: {ex.Message}"]);
        }
    }

    private static ShaderRecompilationResult CompileShader(string source, string filePath)
    {
        // This is a simplified compilation - in a real implementation,
        // you would use the KESL compiler directly
        try
        {
            // For now, we just validate that the file can be read
            // The actual compilation would be done by the KESL compiler
            return new ShaderRecompilationResult(
                Success: true,
                GeneratedCode: source,
                Errors: [],
                FilePath: filePath
            );
        }
        catch (Exception ex)
        {
            return new ShaderRecompilationResult(
                Success: false,
                GeneratedCode: null,
                Errors: [ex.Message],
                FilePath: filePath
            );
        }
    }

    /// <summary>
    /// Disposes the file watcher.
    /// </summary>
    public void Dispose()
    {
        if (!isDisposed)
        {
            Stop();
            isDisposed = true;
        }
    }
}

/// <summary>
/// Result of a shader recompilation attempt.
/// </summary>
/// <param name="Success">Whether compilation succeeded.</param>
/// <param name="GeneratedCode">The generated shader code if successful.</param>
/// <param name="Errors">Any compilation errors.</param>
/// <param name="FilePath">The source file path.</param>
public record ShaderRecompilationResult(
    bool Success,
    string? GeneratedCode,
    IReadOnlyList<string> Errors,
    string FilePath
);
