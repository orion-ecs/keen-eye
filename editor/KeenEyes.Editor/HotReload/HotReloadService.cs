using KeenEyes.Editor.PlayMode;
using KeenEyes.Editor.Settings;

namespace KeenEyes.Editor.HotReload;

/// <summary>
/// Current status of the hot reload service.
/// </summary>
public enum HotReloadStatus
{
    /// <summary>Hot reload is disabled or not configured.</summary>
    Disabled,

    /// <summary>Hot reload is idle, waiting for changes.</summary>
    Idle,

    /// <summary>File change detected, waiting for debounce.</summary>
    Pending,

    /// <summary>Building the project.</summary>
    Building,

    /// <summary>Loading the new assembly.</summary>
    Loading,

    /// <summary>Last reload completed successfully.</summary>
    Ready,

    /// <summary>Last reload failed.</summary>
    Failed
}

/// <summary>
/// Event arguments for hot reload status changes.
/// </summary>
/// <param name="Status">The new status.</param>
/// <param name="Message">Optional status message.</param>
public sealed record HotReloadStatusChangedEventArgs(HotReloadStatus Status, string? Message = null);

/// <summary>
/// Service that manages hot reload functionality in the editor.
/// </summary>
/// <remarks>
/// <para>
/// The HotReloadService bridges the <see cref="HotReloadManager"/> with the editor,
/// handling game project configuration, play mode interactions, and status reporting.
/// </para>
/// <para>
/// Key responsibilities:
/// <list type="bullet">
/// <item>Manages HotReloadManager lifecycle based on editor state</item>
/// <item>Auto-detects game project path from current directory</item>
/// <item>Pauses hot reload during play mode to prevent state corruption</item>
/// <item>Provides status events for UI feedback</item>
/// </list>
/// </para>
/// </remarks>
public sealed class HotReloadService : IDisposable
{
    private readonly World sceneWorld;
    private HotReloadManager? manager;
    private PlayModeManager? playModeManager;
    private bool isPlayModeActive;
    private bool pendingReloadAfterPlayMode;
    private bool disposed;

    /// <summary>
    /// Raised when the hot reload status changes.
    /// </summary>
    public event EventHandler<HotReloadStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Raised when a source file change is detected.
    /// </summary>
    public event EventHandler<string>? SourceFileChanged;

    /// <summary>
    /// Gets the current hot reload status.
    /// </summary>
    public HotReloadStatus CurrentStatus { get; private set; } = HotReloadStatus.Disabled;

    /// <summary>
    /// Gets the last status message.
    /// </summary>
    public string? LastMessage { get; private set; }

    /// <summary>
    /// Gets the path to the game project, if configured.
    /// </summary>
    public string? GameProjectPath => manager != null ? EditorSettings.GameProjectPath : null;

    /// <summary>
    /// Gets whether hot reload is enabled and configured.
    /// </summary>
    public bool IsEnabled => manager != null && EditorSettings.HotReloadEnabled;

    /// <summary>
    /// Gets whether a reload is currently in progress.
    /// </summary>
    public bool IsReloading => manager?.IsReloading ?? false;

    /// <summary>
    /// Gets whether file watching is active.
    /// </summary>
    public bool IsWatching => manager?.IsWatching ?? false;

    /// <summary>
    /// Gets the number of systems registered from the game assembly.
    /// </summary>
    public int RegisteredSystemCount => manager?.RegisteredSystems.Count ?? 0;

    /// <summary>
    /// Creates a new hot reload service.
    /// </summary>
    /// <param name="sceneWorld">The scene world to register game systems to.</param>
    public HotReloadService(World sceneWorld)
    {
        this.sceneWorld = sceneWorld ?? throw new ArgumentNullException(nameof(sceneWorld));

        // Listen for settings changes
        EditorSettings.SettingChanged += OnSettingChanged;
    }

    /// <summary>
    /// Initializes the hot reload service with the current settings.
    /// </summary>
    /// <remarks>
    /// This should be called after the scene world is ready.
    /// It will attempt to find and configure the game project path if not already set.
    /// </remarks>
    public void Initialize()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(HotReloadService));
        }

        // Try to auto-detect game project if not configured
        if (string.IsNullOrEmpty(EditorSettings.GameProjectPath))
        {
            var detected = TryDetectGameProject();
            if (detected != null)
            {
                EditorSettings.GameProjectPath = detected;
                Console.WriteLine($"[HotReload] Auto-detected game project: {detected}");
            }
        }

        ConfigureManager();
    }

    /// <summary>
    /// Connects to a play mode manager to coordinate hot reload with play mode.
    /// </summary>
    /// <param name="playMode">The play mode manager to connect to.</param>
    public void ConnectPlayMode(PlayModeManager? playMode)
    {
        // Disconnect from previous
        if (playModeManager != null)
        {
            playModeManager.StateChanged -= OnPlayModeStateChanged;
        }

        playModeManager = playMode;

        // Connect to new
        if (playModeManager != null)
        {
            playModeManager.StateChanged += OnPlayModeStateChanged;
            isPlayModeActive = playModeManager.IsInPlayMode;
        }
        else
        {
            isPlayModeActive = false;
        }
    }

    /// <summary>
    /// Manually triggers a hot reload.
    /// </summary>
    /// <returns>A task that completes when the reload finishes.</returns>
    public async Task<HotReloadResult> ReloadAsync()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(HotReloadService));
        }

        if (manager == null)
        {
            return new HotReloadResult(false, "Hot reload not configured");
        }

        if (isPlayModeActive)
        {
            return new HotReloadResult(false, "Cannot reload during play mode");
        }

        return await manager.ReloadAsync();
    }

    /// <summary>
    /// Sets the game project path and reconfigures hot reload.
    /// </summary>
    /// <param name="projectPath">Path to the game's .csproj file.</param>
    public static void SetGameProject(string? projectPath)
    {
        EditorSettings.GameProjectPath = projectPath ?? string.Empty;
        // ConfigureManager will be called via the SettingChanged event
    }

    /// <summary>
    /// Attempts to detect a game project in the current directory or parent directories.
    /// </summary>
    /// <returns>The path to the detected project, or null if not found.</returns>
    public static string? TryDetectGameProject()
    {
        var currentDir = Environment.CurrentDirectory;

        // Look for .csproj files in current directory
        var projectFiles = Directory.GetFiles(currentDir, "*.csproj", SearchOption.TopDirectoryOnly);

        // Prefer projects that look like game projects (exclude Editor, Tests, etc.)
        foreach (var projectFile in projectFiles)
        {
            var name = Path.GetFileNameWithoutExtension(projectFile);
            if (!name.Contains("Editor", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("Test", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("Generator", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("Sdk", StringComparison.OrdinalIgnoreCase))
            {
                // Check if it references KeenEyes
                var content = File.ReadAllText(projectFile);
                if (content.Contains("KeenEyes", StringComparison.OrdinalIgnoreCase))
                {
                    return projectFile;
                }
            }
        }

        // Also check src/ subdirectory if it exists
        var srcDir = Path.Combine(currentDir, "src");
        if (Directory.Exists(srcDir))
        {
            projectFiles = Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories);
            foreach (var projectFile in projectFiles)
            {
                var name = Path.GetFileNameWithoutExtension(projectFile);
                if (name.EndsWith(".Game", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Game", StringComparison.OrdinalIgnoreCase))
                {
                    return projectFile;
                }
            }
        }

        return null;
    }

    private void ConfigureManager()
    {
        // Dispose existing manager
        if (manager != null)
        {
            manager.ReloadStarted -= OnReloadStarted;
            manager.ReloadCompleted -= OnReloadCompleted;
            manager.CompilationFailed -= OnCompilationFailed;
            manager.SourceFileChanged -= OnSourceFileChangedInternal;
            manager.Dispose();
            manager = null;
        }

        var projectPath = EditorSettings.GameProjectPath;

        // Check if hot reload should be active
        if (!EditorSettings.HotReloadEnabled || string.IsNullOrEmpty(projectPath))
        {
            UpdateStatus(HotReloadStatus.Disabled, "Hot reload disabled or no project configured");
            return;
        }

        // Validate project path
        if (!File.Exists(projectPath))
        {
            UpdateStatus(HotReloadStatus.Failed, $"Project file not found: {projectPath}");
            return;
        }

        try
        {
            var debounce = TimeSpan.FromMilliseconds(EditorSettings.HotReloadDebounceMs);
            manager = new HotReloadManager(projectPath, sceneWorld, debounce);

            // Subscribe to events
            manager.ReloadStarted += OnReloadStarted;
            manager.ReloadCompleted += OnReloadCompleted;
            manager.CompilationFailed += OnCompilationFailed;
            manager.SourceFileChanged += OnSourceFileChangedInternal;

            // Start watching if auto-reload is enabled
            if (EditorSettings.HotReloadAutoReload && !isPlayModeActive)
            {
                manager.StartWatching();
            }

            UpdateStatus(HotReloadStatus.Idle, $"Watching: {Path.GetFileName(projectPath)}");
            Console.WriteLine($"[HotReload] Configured for project: {projectPath}");
        }
        catch (Exception ex)
        {
            UpdateStatus(HotReloadStatus.Failed, $"Failed to configure: {ex.Message}");
            Console.WriteLine($"[HotReload] Configuration failed: {ex.Message}");
        }
    }

    private void OnSettingChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.Category != "HotReload")
        {
            return;
        }

        // Reconfigure when hot reload settings change
        ConfigureManager();
    }

    private void OnPlayModeStateChanged(object? sender, PlayModeStateChangedEventArgs e)
    {
        var wasPlayMode = isPlayModeActive;
        isPlayModeActive = e.CurrentState is PlayModeState.Playing or PlayModeState.Paused;

        if (manager == null)
        {
            return;
        }

        if (isPlayModeActive && !wasPlayMode)
        {
            // Entering play mode - pause file watching
            manager.StopWatching();
            pendingReloadAfterPlayMode = false;
            Console.WriteLine("[HotReload] Paused - play mode active");
        }
        else if (!isPlayModeActive && wasPlayMode && EditorSettings.HotReloadAutoReload)
        {
            // Exiting play mode - resume watching
            manager.StartWatching();
            Console.WriteLine("[HotReload] Resumed - play mode ended");

            // Check if we need to reload after play mode
            if (pendingReloadAfterPlayMode)
            {
                pendingReloadAfterPlayMode = false;
                Console.WriteLine("[HotReload] Pending changes detected, triggering reload...");
                _ = ReloadAsync(); // Fire and forget
            }
        }
    }

    private void OnReloadStarted()
    {
        UpdateStatus(HotReloadStatus.Building, "Building...");
    }

    private void OnReloadCompleted()
    {
        var systemCount = manager?.RegisteredSystems.Count ?? 0;
        UpdateStatus(HotReloadStatus.Ready, $"Reload complete. {systemCount} system(s) registered.");
    }

    private void OnCompilationFailed(BuildResult result)
    {
        var errorCount = result.Errors.Length;
        var message = errorCount > 0
            ? $"Build failed: {result.Errors[0]}"
            : "Build failed";
        UpdateStatus(HotReloadStatus.Failed, message);
    }

    private void OnSourceFileChangedInternal(string path)
    {
        if (isPlayModeActive)
        {
            pendingReloadAfterPlayMode = true;
            Console.WriteLine($"[HotReload] File changed during play mode, will reload after: {Path.GetFileName(path)}");
        }
        else
        {
            UpdateStatus(HotReloadStatus.Pending, $"Change detected: {Path.GetFileName(path)}");
        }

        SourceFileChanged?.Invoke(this, path);
    }

    private void UpdateStatus(HotReloadStatus status, string? message)
    {
        if (CurrentStatus == status && LastMessage == message)
        {
            return;
        }

        CurrentStatus = status;
        LastMessage = message;
        StatusChanged?.Invoke(this, new HotReloadStatusChangedEventArgs(status, message));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        EditorSettings.SettingChanged -= OnSettingChanged;

        if (playModeManager != null)
        {
            playModeManager.StateChanged -= OnPlayModeStateChanged;
        }

        if (manager != null)
        {
            manager.ReloadStarted -= OnReloadStarted;
            manager.ReloadCompleted -= OnReloadCompleted;
            manager.CompilationFailed -= OnCompilationFailed;
            manager.SourceFileChanged -= OnSourceFileChangedInternal;
            manager.Dispose();
        }
    }
}
