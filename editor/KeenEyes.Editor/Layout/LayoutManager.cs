namespace KeenEyes.Editor.Layout;

/// <summary>
/// Manages editor layout persistence, including saving/loading layouts and applying presets.
/// </summary>
public sealed class LayoutManager
{
    private static readonly string DefaultLayoutPath;
    private static readonly string CustomLayoutsFolder;
    private static readonly Lazy<LayoutManager> LazyInstance = new(() => new LayoutManager());

    private EditorLayout _currentLayout;
    private string? _currentLayoutPath;
    private bool _isLoaded;

    /// <summary>
    /// Gets the singleton instance of the layout manager.
    /// </summary>
    public static LayoutManager Instance => LazyInstance.Value;

    static LayoutManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var keenEyesFolder = Path.Combine(appData, "KeenEyes");
        DefaultLayoutPath = Path.Combine(keenEyesFolder, "layout.json");
        CustomLayoutsFolder = Path.Combine(keenEyesFolder, "layouts");
    }

    private LayoutManager()
    {
        _currentLayout = EditorLayout.CreateDefault();
    }

    /// <summary>
    /// Gets the current layout.
    /// </summary>
    public EditorLayout CurrentLayout => _currentLayout;

    /// <summary>
    /// Gets whether a layout has been loaded from disk.
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// Occurs when the layout changes.
    /// </summary>
    public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

    /// <summary>
    /// Loads the layout from the default location.
    /// </summary>
    public void Load()
    {
        Load(DefaultLayoutPath);
    }

    /// <summary>
    /// Loads a layout from the specified path.
    /// </summary>
    /// <param name="path">The path to the layout file.</param>
    public void Load(string path)
    {
        _currentLayoutPath = path;

        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var layout = EditorLayout.FromJson(json);

                if (layout is not null && layout.Version <= EditorLayout.CurrentVersion)
                {
                    _currentLayout = layout;
                    _isLoaded = true;
                    OnLayoutChanged(LayoutChangeReason.Loaded);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load layout from {path}: {ex.Message}");
        }

        // Fall back to defaults
        _currentLayout = EditorLayout.CreateDefault();
        _isLoaded = true;
        OnLayoutChanged(LayoutChangeReason.Reset);
    }

    /// <summary>
    /// Saves the current layout to the default location.
    /// </summary>
    public void Save()
    {
        Save(_currentLayoutPath ?? DefaultLayoutPath);
    }

    /// <summary>
    /// Saves the current layout to the specified path.
    /// </summary>
    /// <param name="path">The path to save to.</param>
    public void Save(string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = _currentLayout.ToJson();
            File.WriteAllText(path, json);
            _currentLayoutPath = path;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save layout to {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies a preset layout.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    public void ApplyPreset(LayoutPreset preset)
    {
        _currentLayout = preset switch
        {
            LayoutPreset.Default => EditorLayout.CreateDefault(),
            LayoutPreset.Tall => EditorLayout.CreateTall(),
            LayoutPreset.Wide => EditorLayout.CreateWide(),
            LayoutPreset.TwoColumn => EditorLayout.Create2Column(),
            LayoutPreset.ThreeColumn => EditorLayout.Create3Column(),
            LayoutPreset.FourColumn => EditorLayout.Create4Column(),
            _ => EditorLayout.CreateDefault()
        };

        OnLayoutChanged(LayoutChangeReason.PresetApplied);
        Save();
    }

    /// <summary>
    /// Resets to the default layout.
    /// </summary>
    public void ResetToDefault()
    {
        _currentLayout = EditorLayout.CreateDefault();
        OnLayoutChanged(LayoutChangeReason.Reset);
        Save();
    }

    /// <summary>
    /// Gets all available custom layouts.
    /// </summary>
    public IEnumerable<string> GetCustomLayouts()
    {
        if (!Directory.Exists(CustomLayoutsFolder))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(CustomLayoutsFolder, "*.json"))
        {
            yield return Path.GetFileNameWithoutExtension(file);
        }
    }

    /// <summary>
    /// Saves the current layout as a custom layout.
    /// </summary>
    /// <param name="name">The name for the custom layout.</param>
    public void SaveCustomLayout(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        // Sanitize the name for a file
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(CustomLayoutsFolder, $"{safeName}.json");

        _currentLayout.Name = name;
        Save(path);
    }

    /// <summary>
    /// Loads a custom layout by name.
    /// </summary>
    /// <param name="name">The name of the custom layout.</param>
    /// <returns>True if the layout was loaded successfully.</returns>
    public bool LoadCustomLayout(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(CustomLayoutsFolder, $"{safeName}.json");

        if (!File.Exists(path))
        {
            return false;
        }

        Load(path);
        return true;
    }

    /// <summary>
    /// Deletes a custom layout.
    /// </summary>
    /// <param name="name">The name of the custom layout to delete.</param>
    /// <returns>True if deleted successfully.</returns>
    public bool DeleteCustomLayout(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(CustomLayoutsFolder, $"{safeName}.json");

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Updates the window state in the current layout.
    /// </summary>
    /// <param name="x">Window X position.</param>
    /// <param name="y">Window Y position.</param>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    /// <param name="maximized">Whether the window is maximized.</param>
    /// <param name="monitor">Which monitor the window is on.</param>
    public void UpdateWindowState(int x, int y, int width, int height, bool maximized = false, int monitor = 0)
    {
        _currentLayout.Window.X = x;
        _currentLayout.Window.Y = y;
        _currentLayout.Window.Width = width;
        _currentLayout.Window.Height = height;
        _currentLayout.Window.Maximized = maximized;
        _currentLayout.Window.Monitor = monitor;
    }

    /// <summary>
    /// Updates a panel's state in the current layout.
    /// </summary>
    /// <param name="panelId">The panel identifier.</param>
    /// <param name="visible">Whether the panel is visible.</param>
    /// <param name="collapsed">Whether the panel is collapsed.</param>
    /// <param name="width">Optional fixed width.</param>
    /// <param name="height">Optional fixed height.</param>
    public void UpdatePanelState(string panelId, bool visible, bool collapsed = false, float? width = null, float? height = null)
    {
        if (!_currentLayout.Panels.TryGetValue(panelId, out var state))
        {
            state = new PanelState();
            _currentLayout.Panels[panelId] = state;
        }

        state.Visible = visible;
        state.Collapsed = collapsed;
        state.Width = width;
        state.Height = height;
    }

    /// <summary>
    /// Gets the state of a panel.
    /// </summary>
    /// <param name="panelId">The panel identifier.</param>
    /// <returns>The panel state, or null if not found.</returns>
    public PanelState? GetPanelState(string panelId)
    {
        return _currentLayout.Panels.GetValueOrDefault(panelId);
    }

    /// <summary>
    /// Toggles a panel's visibility.
    /// </summary>
    /// <param name="panelId">The panel identifier.</param>
    public void TogglePanelVisibility(string panelId)
    {
        if (!_currentLayout.Panels.TryGetValue(panelId, out var state))
        {
            state = new PanelState { Visible = true };
            _currentLayout.Panels[panelId] = state;
        }

        state.Visible = !state.Visible;
        OnLayoutChanged(LayoutChangeReason.PanelToggled);
    }

    private void OnLayoutChanged(LayoutChangeReason reason)
    {
        LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(reason, _currentLayout));
    }
}

/// <summary>
/// Available layout presets.
/// </summary>
public enum LayoutPreset
{
    /// <summary>
    /// Default layout with hierarchy, viewport, and inspector.
    /// </summary>
    Default,

    /// <summary>
    /// Tall layout optimized for vertical screens.
    /// </summary>
    Tall,

    /// <summary>
    /// Wide layout optimized for ultra-wide screens.
    /// </summary>
    Wide,

    /// <summary>
    /// 2-column layout.
    /// </summary>
    TwoColumn,

    /// <summary>
    /// 3-column layout.
    /// </summary>
    ThreeColumn,

    /// <summary>
    /// 4-column layout.
    /// </summary>
    FourColumn
}

/// <summary>
/// Reason for layout change.
/// </summary>
public enum LayoutChangeReason
{
    /// <summary>
    /// Layout was loaded from disk.
    /// </summary>
    Loaded,

    /// <summary>
    /// Layout was reset to default.
    /// </summary>
    Reset,

    /// <summary>
    /// A preset was applied.
    /// </summary>
    PresetApplied,

    /// <summary>
    /// A panel was toggled.
    /// </summary>
    PanelToggled,

    /// <summary>
    /// Layout was modified.
    /// </summary>
    Modified
}

/// <summary>
/// Event arguments for layout changes.
/// </summary>
public sealed class LayoutChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the reason for the change.
    /// </summary>
    public LayoutChangeReason Reason { get; }

    /// <summary>
    /// Gets the new layout.
    /// </summary>
    public EditorLayout Layout { get; }

    /// <summary>
    /// Creates new layout changed event args.
    /// </summary>
    /// <param name="reason">The reason for the change.</param>
    /// <param name="layout">The new layout.</param>
    public LayoutChangedEventArgs(LayoutChangeReason reason, EditorLayout layout)
    {
        Reason = reason;
        Layout = layout;
    }
}
