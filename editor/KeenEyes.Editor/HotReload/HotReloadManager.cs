using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace KeenEyes.Editor.HotReload;

/// <summary>
/// Manages hot reloading of game assemblies during development.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="AssemblyLoadContext"/> to load game assemblies into a collectible
/// context that can be unloaded and replaced when source files change.
/// </para>
/// <para>
/// The hot reload process:
/// <list type="number">
/// <item>Detect source file changes via <see cref="FileSystemWatcher"/></item>
/// <item>Unregister all game systems from the world</item>
/// <item>Unload the previous assembly context</item>
/// <item>Rebuild the project via dotnet build</item>
/// <item>Load the new assembly into a fresh context</item>
/// <item>Re-register systems from the new assembly</item>
/// </list>
/// </para>
/// <para>
/// Component data survives reload as it's stored as bytes in archetypes.
/// Only system logic is replaced.
/// </para>
/// </remarks>
public sealed class HotReloadManager : IDisposable
{
    private readonly string _projectPath;
    private readonly string _outputDirectory;
    private readonly World _sceneWorld;
    private readonly TimeSpan _debounceDelay;
    private readonly List<ISystem> _registeredSystems = [];

    private GameAssemblyContext? _gameContext;
    private Assembly? _gameAssembly;
    private FileSystemWatcher? _sourceWatcher;
    private CancellationTokenSource? _debounceCts;
    private bool _isReloading;
    private bool _disposed;

    /// <summary>
    /// Raised when a reload operation starts.
    /// </summary>
    public event Action? ReloadStarted;

    /// <summary>
    /// Raised when a reload operation completes successfully.
    /// </summary>
    public event Action? ReloadCompleted;

    /// <summary>
    /// Raised when compilation fails during reload.
    /// </summary>
    public event Action<BuildResult>? CompilationFailed;

    /// <summary>
    /// Raised when a source file change is detected.
    /// </summary>
    public event Action<string>? SourceFileChanged;

    /// <summary>
    /// Gets whether a reload is currently in progress.
    /// </summary>
    public bool IsReloading => _isReloading;

    /// <summary>
    /// Gets whether file watching is enabled.
    /// </summary>
    public bool IsWatching => _sourceWatcher?.EnableRaisingEvents ?? false;

    /// <summary>
    /// Gets the currently loaded game assembly, if any.
    /// </summary>
    public Assembly? GameAssembly => _gameAssembly;

    /// <summary>
    /// Gets the list of systems registered from the game assembly.
    /// </summary>
    public IReadOnlyList<ISystem> RegisteredSystems => _registeredSystems;

    /// <summary>
    /// Creates a new hot reload manager.
    /// </summary>
    /// <param name="projectPath">Path to the game's .csproj file.</param>
    /// <param name="sceneWorld">The scene world to register systems to.</param>
    /// <param name="debounceDelay">Delay before triggering rebuild after file change.</param>
    public HotReloadManager(
        string projectPath,
        World sceneWorld,
        TimeSpan? debounceDelay = null)
    {
        ArgumentNullException.ThrowIfNull(sceneWorld);

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be empty", nameof(projectPath));
        }

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException("Project file not found", projectPath);
        }

        _projectPath = Path.GetFullPath(projectPath);
        _outputDirectory = Path.Combine(
            Path.GetDirectoryName(_projectPath)!,
            "bin", "Debug", "net10.0");
        _sceneWorld = sceneWorld;
        _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(500);
    }

    /// <summary>
    /// Starts watching for source file changes.
    /// </summary>
    public void StartWatching()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HotReloadManager));
        }

        if (_sourceWatcher != null)
        {
            return;
        }

        var projectDir = Path.GetDirectoryName(_projectPath)!;

        _sourceWatcher = new FileSystemWatcher(projectDir, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        _sourceWatcher.Changed += OnSourceChanged;
        _sourceWatcher.Created += OnSourceChanged;
        _sourceWatcher.Renamed += OnSourceRenamed;
        _sourceWatcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Stops watching for source file changes.
    /// </summary>
    public void StopWatching()
    {
        if (_sourceWatcher != null)
        {
            _sourceWatcher.EnableRaisingEvents = false;
            _sourceWatcher.Changed -= OnSourceChanged;
            _sourceWatcher.Created -= OnSourceChanged;
            _sourceWatcher.Renamed -= OnSourceRenamed;
            _sourceWatcher.Dispose();
            _sourceWatcher = null;
        }

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }

    /// <summary>
    /// Performs a hot reload of the game assembly.
    /// </summary>
    /// <returns>The result of the reload operation.</returns>
    public async Task<HotReloadResult> ReloadAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HotReloadManager));
        }

        if (_isReloading)
        {
            return new HotReloadResult(false, "Reload already in progress");
        }

        _isReloading = true;
        ReloadStarted?.Invoke();

        try
        {
            // 1. Unregister all game systems
            foreach (var system in _registeredSystems)
            {
                _sceneWorld.RemoveSystem(system);
            }
            _registeredSystems.Clear();

            // 2. Unload previous assembly context
            await UnloadGameContextAsync();

            // 3. Rebuild the project
            var buildResult = await BuildProjectAsync();
            if (!buildResult.Success)
            {
                CompilationFailed?.Invoke(buildResult);
                return new HotReloadResult(false, "Compilation failed", buildResult);
            }

            // 4. Load the new assembly
            var loadResult = LoadGameAssembly(buildResult.OutputPath!);
            if (!loadResult.Success)
            {
                return loadResult;
            }

            // 5. Register systems from the new assembly
            RegisterSystemsFromAssembly();

            ReloadCompleted?.Invoke();
            return new HotReloadResult(true, "Reload successful");
        }
        catch (Exception ex)
        {
            var failedResult = new BuildResult(false, null, [$"Exception: {ex.Message}"]);
            CompilationFailed?.Invoke(failedResult);
            return new HotReloadResult(false, ex.Message);
        }
        finally
        {
            _isReloading = false;
        }
    }

    /// <summary>
    /// Loads a game assembly without file watching.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly DLL.</param>
    /// <returns>Result of the load operation.</returns>
    public HotReloadResult LoadGameAssembly(string assemblyPath)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HotReloadManager));
        }

        if (!File.Exists(assemblyPath))
        {
            return new HotReloadResult(false, $"Assembly not found: {assemblyPath}");
        }

        try
        {
            _gameContext = new GameAssemblyContext(assemblyPath);
            _gameAssembly = _gameContext.LoadFromAssemblyPath(assemblyPath);
            return new HotReloadResult(true, "Assembly loaded");
        }
        catch (Exception ex)
        {
            return new HotReloadResult(false, $"Failed to load assembly: {ex.Message}");
        }
    }

    private async Task UnloadGameContextAsync()
    {
        if (_gameContext == null)
        {
            return;
        }

        var contextRef = new WeakReference(_gameContext);
        _gameContext.Unload();
        _gameContext = null;
        _gameAssembly = null;

        // Force garbage collection to fully unload the assembly context
        // GC.Collect is required for proper AssemblyLoadContext unloading - this is the recommended pattern
        // See: https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability
#pragma warning disable S1215 // GC.Collect should not be called
        for (int i = 0; i < 10 && contextRef.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(10);
        }
#pragma warning restore S1215

        if (contextRef.IsAlive)
        {
            Console.Error.WriteLine("[HotReload] Warning: Previous context may not have fully unloaded");
        }
    }

    private async Task<BuildResult> BuildProjectAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{_projectPath}\" -c Debug --no-restore",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(_projectPath) + ".dll";
                var outputPath = Path.Combine(_outputDirectory, assemblyName);
                return new BuildResult(true, outputPath, []);
            }

            var errors = ParseBuildErrors(error + "\n" + output);
            return new BuildResult(false, null, errors);
        }
        catch (Exception ex)
        {
            return new BuildResult(false, null, [$"Build process failed: {ex.Message}"]);
        }
    }

    private void RegisterSystemsFromAssembly()
    {
        if (_gameAssembly == null)
        {
            return;
        }

        try
        {
            var systemTypes = _gameAssembly.GetTypes()
                .Where(t => typeof(ISystem).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            foreach (var systemType in systemTypes)
            {
                try
                {
                    var system = (ISystem)Activator.CreateInstance(systemType)!;
                    _sceneWorld.AddSystem(system);
                    _registeredSystems.Add(system);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[HotReload] Failed to create system {systemType.Name}: {ex.Message}");
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            Console.Error.WriteLine($"[HotReload] Failed to load types: {ex.Message}");
            foreach (var loaderEx in ex.LoaderExceptions)
            {
                if (loaderEx != null)
                {
                    Console.Error.WriteLine($"  {loaderEx.Message}");
                }
            }
        }
    }

    private static string[] ParseBuildErrors(string output)
    {
        var errors = new List<string>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains(": error ") || trimmed.Contains(": warning "))
            {
                errors.Add(trimmed);
            }
        }

        return errors.Count > 0 ? [.. errors] : ["Build failed with unknown error"];
    }

    private async void OnSourceChanged(object sender, FileSystemEventArgs e)
    {
        await HandleFileChangeAsync(e.FullPath);
    }

    private async void OnSourceRenamed(object sender, RenamedEventArgs e)
    {
        await HandleFileChangeAsync(e.FullPath);
    }

    private async Task HandleFileChangeAsync(string filePath)
    {
        if (_disposed || _isReloading)
        {
            return;
        }

        // Skip hidden files and obj/bin directories
        if (filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
            filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        {
            return;
        }

        SourceFileChanged?.Invoke(filePath);

        // Cancel any pending debounce
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(_debounceDelay, _debounceCts.Token);
            await ReloadAsync();
        }
        catch (OperationCanceledException)
        {
            // Debounce was cancelled by a newer change
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopWatching();

        // Unregister systems
        foreach (var system in _registeredSystems)
        {
            _sceneWorld.RemoveSystem(system);
        }
        _registeredSystems.Clear();

        // Unload context synchronously
        _gameContext?.Unload();
        _gameContext = null;
        _gameAssembly = null;
    }
}

/// <summary>
/// Result of a build operation.
/// </summary>
/// <param name="Success">Whether the build succeeded.</param>
/// <param name="OutputPath">Path to the output assembly if successful.</param>
/// <param name="Errors">Build errors if failed.</param>
public sealed record BuildResult(bool Success, string? OutputPath, string[] Errors);

/// <summary>
/// Result of a hot reload operation.
/// </summary>
/// <param name="Success">Whether the reload succeeded.</param>
/// <param name="Message">Status message.</param>
/// <param name="BuildResult">The build result if a build was attempted.</param>
public sealed record HotReloadResult(bool Success, string Message, BuildResult? BuildResult = null);
