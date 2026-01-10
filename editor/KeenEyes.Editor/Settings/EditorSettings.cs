using System.Text.Json;
using System.Text.Json.Serialization;

using KeenEyes.Common;

namespace KeenEyes.Editor.Settings;

/// <summary>
/// Central storage for all editor settings with automatic persistence.
/// </summary>
public static class EditorSettings
{
    private static readonly SettingsData _data = new();
    private static string? _settingsPath;
    private static bool _isLoaded;

    /// <summary>
    /// Event raised when any setting changes.
    /// </summary>
    public static event EventHandler<SettingChangedEventArgs>? SettingChanged;

    #region General Settings

    /// <summary>
    /// Gets or sets whether auto-save is enabled.
    /// </summary>
    public static bool AutoSaveEnabled
    {
        get => _data.General.AutoSaveEnabled;
        set
        {
            if (_data.General.AutoSaveEnabled == value) return;
            var oldValue = _data.General.AutoSaveEnabled;
            _data.General.AutoSaveEnabled = value;
            OnSettingChanged(nameof(AutoSaveEnabled), "General", oldValue, value);
        }
    }

    /// <summary>
    /// Gets or sets the auto-save interval in seconds.
    /// </summary>
    public static int AutoSaveIntervalSeconds
    {
        get => _data.General.AutoSaveIntervalSeconds;
        set
        {
            var clamped = Math.Max(30, value);
            if (_data.General.AutoSaveIntervalSeconds == clamped) return;
            var oldValue = _data.General.AutoSaveIntervalSeconds;
            _data.General.AutoSaveIntervalSeconds = clamped;
            OnSettingChanged(nameof(AutoSaveIntervalSeconds), "General", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the maximum undo history size.
    /// </summary>
    public static int UndoHistoryLimit
    {
        get => _data.General.UndoHistoryLimit;
        set
        {
            var clamped = Math.Max(1, value);
            if (_data.General.UndoHistoryLimit == clamped) return;
            var oldValue = _data.General.UndoHistoryLimit;
            _data.General.UndoHistoryLimit = clamped;
            OnSettingChanged(nameof(UndoHistoryLimit), "General", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of recent files to remember.
    /// </summary>
    public static int RecentFilesCount
    {
        get => _data.General.RecentFilesCount;
        set
        {
            var clamped = Math.Clamp(value, 1, 50);
            if (_data.General.RecentFilesCount == clamped) return;
            var oldValue = _data.General.RecentFilesCount;
            _data.General.RecentFilesCount = clamped;
            OnSettingChanged(nameof(RecentFilesCount), "General", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets whether to show the splash screen on startup.
    /// </summary>
    public static bool ShowSplashScreen
    {
        get => _data.General.ShowSplashScreen;
        set
        {
            if (_data.General.ShowSplashScreen == value) return;
            var oldValue = _data.General.ShowSplashScreen;
            _data.General.ShowSplashScreen = value;
            OnSettingChanged(nameof(ShowSplashScreen), "General", oldValue, value);
        }
    }

    #endregion

    #region Appearance Settings

    /// <summary>
    /// Gets or sets the editor theme name.
    /// </summary>
    public static string Theme
    {
        get => _data.Appearance.Theme;
        set
        {
            var newValue = value ?? "Dark";
            if (_data.Appearance.Theme == newValue) return;
            var oldValue = _data.Appearance.Theme;
            _data.Appearance.Theme = newValue;
            OnSettingChanged(nameof(Theme), "Appearance", oldValue, newValue);
        }
    }

    /// <summary>
    /// Gets or sets the editor font size.
    /// </summary>
    public static int FontSize
    {
        get => _data.Appearance.FontSize;
        set
        {
            var clamped = Math.Clamp(value, 8, 32);
            if (_data.Appearance.FontSize == clamped) return;
            var oldValue = _data.Appearance.FontSize;
            _data.Appearance.FontSize = clamped;
            OnSettingChanged(nameof(FontSize), "Appearance", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the UI scale factor (1.0 = 100%).
    /// </summary>
    public static float UiScale
    {
        get => _data.Appearance.UiScale;
        set
        {
            var clamped = Math.Clamp(value, 0.5f, 3.0f);
            if (_data.Appearance.UiScale.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.Appearance.UiScale;
            _data.Appearance.UiScale = clamped;
            OnSettingChanged(nameof(UiScale), "Appearance", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets whether to use high contrast mode.
    /// </summary>
    public static bool HighContrastMode
    {
        get => _data.Appearance.HighContrastMode;
        set
        {
            if (_data.Appearance.HighContrastMode == value) return;
            var oldValue = _data.Appearance.HighContrastMode;
            _data.Appearance.HighContrastMode = value;
            OnSettingChanged(nameof(HighContrastMode), "Appearance", oldValue, value);
        }
    }

    #endregion

    #region Viewport Settings

    /// <summary>
    /// Gets or sets whether the grid is visible in the viewport.
    /// </summary>
    public static bool GridVisible
    {
        get => _data.Viewport.GridVisible;
        set
        {
            if (_data.Viewport.GridVisible == value) return;
            var oldValue = _data.Viewport.GridVisible;
            _data.Viewport.GridVisible = value;
            OnSettingChanged(nameof(GridVisible), "Viewport", oldValue, value);
        }
    }

    /// <summary>
    /// Gets or sets the grid cell size.
    /// </summary>
    public static float GridSize
    {
        get => _data.Viewport.GridSize;
        set
        {
            var clamped = Math.Max(0.1f, value);
            if (_data.Viewport.GridSize.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.Viewport.GridSize;
            _data.Viewport.GridSize = clamped;
            OnSettingChanged(nameof(GridSize), "Viewport", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the gizmo size multiplier.
    /// </summary>
    public static float GizmoSize
    {
        get => _data.Viewport.GizmoSize;
        set
        {
            var clamped = Math.Clamp(value, 0.1f, 5.0f);
            if (_data.Viewport.GizmoSize.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.Viewport.GizmoSize;
            _data.Viewport.GizmoSize = clamped;
            OnSettingChanged(nameof(GizmoSize), "Viewport", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the camera movement speed.
    /// </summary>
    public static float CameraSpeed
    {
        get => _data.Viewport.CameraSpeed;
        set
        {
            var clamped = Math.Max(0.1f, value);
            if (_data.Viewport.CameraSpeed.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.Viewport.CameraSpeed;
            _data.Viewport.CameraSpeed = clamped;
            OnSettingChanged(nameof(CameraSpeed), "Viewport", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the default camera field of view in degrees.
    /// </summary>
    public static float CameraFieldOfView
    {
        get => _data.Viewport.CameraFieldOfView;
        set
        {
            var clamped = Math.Clamp(value, 10f, 170f);
            if (_data.Viewport.CameraFieldOfView.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.Viewport.CameraFieldOfView;
            _data.Viewport.CameraFieldOfView = clamped;
            OnSettingChanged(nameof(CameraFieldOfView), "Viewport", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the camera near clipping plane distance.
    /// </summary>
    public static float CameraNearClip
    {
        get => _data.Viewport.CameraNearClip;
        set
        {
            var clamped = Math.Max(0.001f, value);
            if (_data.Viewport.CameraNearClip.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.Viewport.CameraNearClip;
            _data.Viewport.CameraNearClip = clamped;
            OnSettingChanged(nameof(CameraNearClip), "Viewport", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the camera far clipping plane distance.
    /// </summary>
    public static float CameraFarClip
    {
        get => _data.Viewport.CameraFarClip;
        set
        {
            var clamped = Math.Max(1f, value);
            if (_data.Viewport.CameraFarClip.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.Viewport.CameraFarClip;
            _data.Viewport.CameraFarClip = clamped;
            OnSettingChanged(nameof(CameraFarClip), "Viewport", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets whether wireframe rendering is enabled in the viewport.
    /// </summary>
    public static bool WireframeMode
    {
        get => _data.Viewport.WireframeMode;
        set
        {
            if (_data.Viewport.WireframeMode == value) return;
            var oldValue = _data.Viewport.WireframeMode;
            _data.Viewport.WireframeMode = value;
            OnSettingChanged(nameof(WireframeMode), "Viewport", oldValue, value);
        }
    }

    #endregion

    #region Play Mode Settings

    /// <summary>
    /// Gets or sets whether to maximize the viewport when entering play mode.
    /// </summary>
    public static bool MaximizeOnPlay
    {
        get => _data.PlayMode.MaximizeOnPlay;
        set
        {
            if (_data.PlayMode.MaximizeOnPlay == value) return;
            var oldValue = _data.PlayMode.MaximizeOnPlay;
            _data.PlayMode.MaximizeOnPlay = value;
            OnSettingChanged(nameof(MaximizeOnPlay), "PlayMode", oldValue, value);
        }
    }

    /// <summary>
    /// Gets or sets whether to mute audio when entering play mode.
    /// </summary>
    public static bool MuteAudioOnPlay
    {
        get => _data.PlayMode.MuteAudioOnPlay;
        set
        {
            if (_data.PlayMode.MuteAudioOnPlay == value) return;
            var oldValue = _data.PlayMode.MuteAudioOnPlay;
            _data.PlayMode.MuteAudioOnPlay = value;
            OnSettingChanged(nameof(MuteAudioOnPlay), "PlayMode", oldValue, value);
        }
    }

    /// <summary>
    /// Gets or sets the default time scale for play mode.
    /// </summary>
    public static float DefaultTimeScale
    {
        get => _data.PlayMode.DefaultTimeScale;
        set
        {
            var clamped = Math.Clamp(value, 0f, 100f);
            if (_data.PlayMode.DefaultTimeScale.ApproximatelyEquals(clamped)) return;
            var oldValue = _data.PlayMode.DefaultTimeScale;
            _data.PlayMode.DefaultTimeScale = clamped;
            OnSettingChanged(nameof(DefaultTimeScale), "PlayMode", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets whether to pause on script errors during play mode.
    /// </summary>
    public static bool PauseOnScriptError
    {
        get => _data.PlayMode.PauseOnScriptError;
        set
        {
            if (_data.PlayMode.PauseOnScriptError == value) return;
            var oldValue = _data.PlayMode.PauseOnScriptError;
            _data.PlayMode.PauseOnScriptError = value;
            OnSettingChanged(nameof(PauseOnScriptError), "PlayMode", oldValue, value);
        }
    }

    #endregion

    #region Hot Reload Settings

    /// <summary>
    /// Gets or sets the path to the game project (.csproj) for hot reload.
    /// </summary>
    public static string GameProjectPath
    {
        get => _data.HotReload.GameProjectPath;
        set
        {
            var newValue = value ?? string.Empty;
            if (_data.HotReload.GameProjectPath == newValue) return;
            var oldValue = _data.HotReload.GameProjectPath;
            _data.HotReload.GameProjectPath = newValue;
            OnSettingChanged(nameof(GameProjectPath), "HotReload", oldValue, newValue);
        }
    }

    /// <summary>
    /// Gets or sets whether hot reload is enabled.
    /// </summary>
    public static bool HotReloadEnabled
    {
        get => _data.HotReload.Enabled;
        set
        {
            if (_data.HotReload.Enabled == value) return;
            var oldValue = _data.HotReload.Enabled;
            _data.HotReload.Enabled = value;
            OnSettingChanged(nameof(HotReloadEnabled), "HotReload", oldValue, value);
        }
    }

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds before triggering a rebuild.
    /// </summary>
    public static int HotReloadDebounceMs
    {
        get => _data.HotReload.DebounceMs;
        set
        {
            var clamped = Math.Clamp(value, 100, 5000);
            if (_data.HotReload.DebounceMs == clamped) return;
            var oldValue = _data.HotReload.DebounceMs;
            _data.HotReload.DebounceMs = clamped;
            OnSettingChanged(nameof(HotReloadDebounceMs), "HotReload", oldValue, clamped);
        }
    }

    /// <summary>
    /// Gets or sets whether to automatically reload when files change.
    /// </summary>
    public static bool HotReloadAutoReload
    {
        get => _data.HotReload.AutoReload;
        set
        {
            if (_data.HotReload.AutoReload == value) return;
            var oldValue = _data.HotReload.AutoReload;
            _data.HotReload.AutoReload = value;
            OnSettingChanged(nameof(HotReloadAutoReload), "HotReload", oldValue, value);
        }
    }

    #endregion

    #region External Tools Settings

    /// <summary>
    /// Gets or sets the path to the external script editor.
    /// </summary>
    public static string ScriptEditorPath
    {
        get => _data.ExternalTools.ScriptEditorPath;
        set
        {
            var newValue = value ?? string.Empty;
            if (_data.ExternalTools.ScriptEditorPath == newValue) return;
            var oldValue = _data.ExternalTools.ScriptEditorPath;
            _data.ExternalTools.ScriptEditorPath = newValue;
            OnSettingChanged(nameof(ScriptEditorPath), "ExternalTools", oldValue, newValue);
        }
    }

    /// <summary>
    /// Gets or sets the path to the external diff tool.
    /// </summary>
    public static string DiffToolPath
    {
        get => _data.ExternalTools.DiffToolPath;
        set
        {
            var newValue = value ?? string.Empty;
            if (_data.ExternalTools.DiffToolPath == newValue) return;
            var oldValue = _data.ExternalTools.DiffToolPath;
            _data.ExternalTools.DiffToolPath = newValue;
            OnSettingChanged(nameof(DiffToolPath), "ExternalTools", oldValue, newValue);
        }
    }

    /// <summary>
    /// Gets or sets the path to the external image editor.
    /// </summary>
    public static string ImageEditorPath
    {
        get => _data.ExternalTools.ImageEditorPath;
        set
        {
            var newValue = value ?? string.Empty;
            if (_data.ExternalTools.ImageEditorPath == newValue) return;
            var oldValue = _data.ExternalTools.ImageEditorPath;
            _data.ExternalTools.ImageEditorPath = newValue;
            OnSettingChanged(nameof(ImageEditorPath), "ExternalTools", oldValue, newValue);
        }
    }

    #endregion

    #region Keyboard Shortcuts Settings

    /// <summary>
    /// Gets or sets the path to custom keyboard shortcuts file.
    /// </summary>
    public static string ShortcutsFilePath
    {
        get => _data.Shortcuts.CustomShortcutsPath;
        set
        {
            var newValue = value ?? string.Empty;
            if (_data.Shortcuts.CustomShortcutsPath == newValue) return;
            var oldValue = _data.Shortcuts.CustomShortcutsPath;
            _data.Shortcuts.CustomShortcutsPath = newValue;
            OnSettingChanged(nameof(ShortcutsFilePath), "Shortcuts", oldValue, newValue);
        }
    }

    #endregion

    #region Persistence

    /// <summary>
    /// Gets the default settings file path.
    /// </summary>
    public static string DefaultSettingsPath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "KeenEyes", "settings.json");
        }
    }

    /// <summary>
    /// Gets a value indicating whether settings have been loaded.
    /// </summary>
    public static bool IsLoaded => _isLoaded;

    /// <summary>
    /// Loads settings from the specified path or the default path.
    /// </summary>
    /// <param name="path">Optional custom settings file path.</param>
    public static void Load(string? path = null)
    {
        _settingsPath = path ?? DefaultSettingsPath;

        if (!File.Exists(_settingsPath))
        {
            _isLoaded = true;
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<SettingsData>(json, GetJsonOptions());

            if (loaded != null)
            {
                CopySettings(loaded, _data);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load settings: {ex.Message}");
        }

        _isLoaded = true;
    }

    /// <summary>
    /// Saves settings to the current path.
    /// </summary>
    public static void Save()
    {
        var path = _settingsPath ?? DefaultSettingsPath;
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            var json = JsonSerializer.Serialize(_data, GetJsonOptions());
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        var defaults = new SettingsData();
        CopySettings(defaults, _data);
        Save();
        SettingChanged?.Invoke(null, new SettingChangedEventArgs("*", "All", null, null));
    }

    /// <summary>
    /// Resets settings for a specific category to their defaults.
    /// </summary>
    /// <param name="category">The category to reset.</param>
    public static void ResetCategory(string category)
    {
        var defaults = new SettingsData();

        switch (category)
        {
            case "General":
                _data.General = defaults.General;
                break;
            case "Appearance":
                _data.Appearance = defaults.Appearance;
                break;
            case "Viewport":
                _data.Viewport = defaults.Viewport;
                break;
            case "PlayMode":
                _data.PlayMode = defaults.PlayMode;
                break;
            case "ExternalTools":
                _data.ExternalTools = defaults.ExternalTools;
                break;
            case "Shortcuts":
                _data.Shortcuts = defaults.Shortcuts;
                break;
            case "HotReload":
                _data.HotReload = defaults.HotReload;
                break;
        }

        Save();
        SettingChanged?.Invoke(null, new SettingChangedEventArgs("*", category, null, null));
    }

    /// <summary>
    /// Gets all available category names.
    /// </summary>
    public static IReadOnlyList<string> Categories { get; } =
    [
        "General",
        "Appearance",
        "Viewport",
        "PlayMode",
        "HotReload",
        "ExternalTools",
        "Shortcuts"
    ];

    #endregion

    #region Private Helpers

    private static void OnSettingChanged(string name, string category, object? oldValue, object? newValue)
    {
        SettingChanged?.Invoke(null, new SettingChangedEventArgs(name, category, oldValue, newValue));

        if (_isLoaded)
        {
            Save();
        }
    }

    private static void CopySettings(SettingsData source, SettingsData target)
    {
        target.General = source.General;
        target.Appearance = source.Appearance;
        target.Viewport = source.Viewport;
        target.PlayMode = source.PlayMode;
        target.HotReload = source.HotReload;
        target.ExternalTools = source.ExternalTools;
        target.Shortcuts = source.Shortcuts;
    }

    private static JsonSerializerOptions GetJsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        // Note: We don't use WhenWritingDefault because bool false values need to be serialized
    };

    #endregion
}

/// <summary>
/// Event arguments for setting changes.
/// </summary>
/// <param name="SettingName">The name of the setting that changed, or "*" for all.</param>
/// <param name="Category">The category of the setting.</param>
/// <param name="OldValue">The previous value.</param>
/// <param name="NewValue">The new value.</param>
public sealed class SettingChangedEventArgs(
    string SettingName,
    string Category,
    object? OldValue,
    object? NewValue) : EventArgs
{
    /// <summary>Gets the name of the setting that changed.</summary>
    public string SettingName { get; } = SettingName;

    /// <summary>Gets the category of the setting.</summary>
    public string Category { get; } = Category;

    /// <summary>Gets the previous value.</summary>
    public object? OldValue { get; } = OldValue;

    /// <summary>Gets the new value.</summary>
    public object? NewValue { get; } = NewValue;
}

#region Settings Data Classes

/// <summary>
/// Root container for all settings data.
/// </summary>
internal sealed class SettingsData
{
    public GeneralSettings General { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public ViewportSettings Viewport { get; set; } = new();
    public PlayModeSettings PlayMode { get; set; } = new();
    public HotReloadSettings HotReload { get; set; } = new();
    public ExternalToolsSettings ExternalTools { get; set; } = new();
    public ShortcutsSettings Shortcuts { get; set; } = new();
}

/// <summary>
/// General editor settings.
/// </summary>
internal sealed class GeneralSettings
{
    public bool AutoSaveEnabled { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 300; // 5 minutes
    public int UndoHistoryLimit { get; set; } = 100;
    public int RecentFilesCount { get; set; } = 10;
    public bool ShowSplashScreen { get; set; } = true;
}

/// <summary>
/// Appearance settings.
/// </summary>
internal sealed class AppearanceSettings
{
    public string Theme { get; set; } = "Dark";
    public int FontSize { get; set; } = 14;
    public float UiScale { get; set; } = 1.0f;
    public bool HighContrastMode { get; set; } = false;
}

/// <summary>
/// Viewport settings.
/// </summary>
internal sealed class ViewportSettings
{
    public bool GridVisible { get; set; } = true;
    public bool WireframeMode { get; set; } = false;
    public float GridSize { get; set; } = 1.0f;
    public float GizmoSize { get; set; } = 1.0f;
    public float CameraSpeed { get; set; } = 10.0f;
    public float CameraFieldOfView { get; set; } = 60.0f;
    public float CameraNearClip { get; set; } = 0.1f;
    public float CameraFarClip { get; set; } = 1000.0f;
}

/// <summary>
/// Play mode settings.
/// </summary>
internal sealed class PlayModeSettings
{
    public bool MaximizeOnPlay { get; set; } = false;
    public bool MuteAudioOnPlay { get; set; } = false;
    public float DefaultTimeScale { get; set; } = 1.0f;
    public bool PauseOnScriptError { get; set; } = true;
}

/// <summary>
/// External tools settings.
/// </summary>
internal sealed class ExternalToolsSettings
{
    public string ScriptEditorPath { get; set; } = string.Empty;
    public string DiffToolPath { get; set; } = string.Empty;
    public string ImageEditorPath { get; set; } = string.Empty;
}

/// <summary>
/// Keyboard shortcuts settings.
/// </summary>
internal sealed class ShortcutsSettings
{
    public string CustomShortcutsPath { get; set; } = string.Empty;
}

/// <summary>
/// Hot reload settings.
/// </summary>
internal sealed class HotReloadSettings
{
    public string GameProjectPath { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int DebounceMs { get; set; } = 500;
    public bool AutoReload { get; set; } = true;
}

#endregion
