using System.Numerics;
using KeenEyes.Editor.Commands;
using KeenEyes.Editor.Layout;
using KeenEyes.Editor.Panels;
using KeenEyes.Editor.PlayMode;
using KeenEyes.Editor.Selection;
using KeenEyes.Editor.Settings;
using KeenEyes.Editor.Shortcuts;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Input.Abstractions;
using KeenEyes.Input.Silk;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;
using KeenEyes.Serialization;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Application;

/// <summary>
/// Main entry point for the KeenEyes Editor application.
/// Sets up the window, graphics, input, and UI systems, then creates the editor layout.
/// </summary>
public sealed class EditorApplication : IDisposable, IEditorShortcutActions
{
    private readonly World _editorWorld;
    private readonly EditorWorldManager _worldManager;
    private readonly ShortcutManager _shortcuts;
    private readonly UndoRedoManager _undoRedo;
    private readonly SelectionManager _selection;
    private readonly LayoutManager _layoutManager;
#pragma warning disable CS0649, IDE0044 // Field is never assigned - initialized lazily when scene world is created
    private PlayModeManager? _playMode;
#pragma warning restore CS0649, IDE0044
    private FontHandle _defaultFont;
    private bool _isDisposed;

    /// <summary>
    /// Gets the editor's ECS world (used for UI and editor state).
    /// </summary>
    public World EditorWorld => _editorWorld;

    /// <summary>
    /// Gets the world manager that handles scene editing worlds.
    /// </summary>
    public EditorWorldManager WorldManager => _worldManager;

    /// <summary>
    /// Gets the shortcut manager for keyboard shortcuts.
    /// </summary>
    public ShortcutManager Shortcuts => _shortcuts;

    /// <summary>
    /// Gets the undo/redo manager for command history.
    /// </summary>
    public UndoRedoManager UndoRedo => _undoRedo;

    /// <summary>
    /// Gets the selection manager for entity selection.
    /// </summary>
    public SelectionManager Selection => _selection;

    /// <summary>
    /// Gets the play mode manager, if initialized.
    /// </summary>
    public PlayModeManager? PlayMode => _playMode;

    /// <summary>
    /// Gets the layout manager for window arrangement.
    /// </summary>
    public LayoutManager LayoutManager => _layoutManager;

    /// <summary>
    /// Creates a new editor application instance.
    /// </summary>
    public EditorApplication()
    {
        _editorWorld = new World();
        _worldManager = new EditorWorldManager();
        _shortcuts = new ShortcutManager();
        _undoRedo = new UndoRedoManager(EditorSettings.UndoHistoryLimit);
        _selection = new SelectionManager();
        _layoutManager = LayoutManager.Instance;

        // Load editor settings
        EditorSettings.Load();

        // Load layout
        _layoutManager.Load();

        // Update undo limit when settings change
        EditorSettings.SettingChanged += (_, e) =>
        {
            if (e.SettingName == nameof(EditorSettings.UndoHistoryLimit))
            {
                _undoRedo.MaxHistorySize = EditorSettings.UndoHistoryLimit;
            }
        };

        // Register default shortcuts
        EditorShortcuts.RegisterDefaults(_shortcuts, this);

        // Load custom shortcuts if configured
        if (!string.IsNullOrEmpty(EditorSettings.ShortcutsFilePath) && File.Exists(EditorSettings.ShortcutsFilePath))
        {
            _shortcuts.Load(EditorSettings.ShortcutsFilePath);
        }
    }

    /// <summary>
    /// Runs the editor application.
    /// </summary>
    public void Run()
    {
        // Configure window from saved layout
        var windowState = _layoutManager.CurrentLayout.Window;
        var windowConfig = new WindowConfig
        {
            Title = "KeenEyes Editor",
            Width = windowState.Width,
            Height = windowState.Height,
            VSync = true
        };

        // Configure graphics
        var graphicsConfig = new SilkGraphicsConfig
        {
            ClearColor = new Vector4(0.1f, 0.1f, 0.12f, 1f),
            EnableDepthTest = false,
            EnableCulling = false
        };

        // Configure input
        var inputConfig = new SilkInputConfig
        {
            EnableGamepads = false,
            CaptureMouseOnClick = false
        };

        // Install plugins
        _editorWorld.InstallPlugin(new SilkWindowPlugin(windowConfig));
        _editorWorld.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
        _editorWorld.InstallPlugin(new SilkInputPlugin(inputConfig));
        _editorWorld.InstallPlugin(new UIPlugin());

        Console.WriteLine("KeenEyes Editor starting...");

        _editorWorld.CreateRunner()
            .OnReady(OnEditorReady)
            .OnResize(OnResize)
            .Run();
    }

    private void OnEditorReady()
    {
        Console.WriteLine("Editor initialized!");

        // Load font
        _defaultFont = LoadDefaultFont();

        // Create editor UI
        CreateEditorUI();

        // Set up input handling
        SetupInputHandling();
    }

    private void OnResize(int width, int height)
    {
        var layoutSystem = _editorWorld.GetSystem<UILayoutSystem>();
        layoutSystem?.SetScreenSize(width, height);
    }

    private FontHandle LoadDefaultFont()
    {
        var fontManagerProvider = _editorWorld.GetExtension<IFontManagerProvider>();
        var fontManager = fontManagerProvider?.GetFontManager();

        if (fontManager is null)
        {
            Console.WriteLine("Warning: Font manager not available");
            return default;
        }

        var fontPath = FindSystemFont();
        if (fontPath is null)
        {
            Console.WriteLine("Warning: No suitable system font found");
            return default;
        }

        try
        {
            var font = fontManager.LoadFont(fontPath, 14f);
            Console.WriteLine($"Font loaded: {fontPath}");
            return font;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load font: {ex.Message}");
            return default;
        }
    }

    private void CreateEditorUI()
    {
        var ui = _editorWorld.GetExtension<UIContext>();
        var canvas = ui.CreateCanvas("EditorCanvas");

        // Create main menu bar
        CreateMenuBar(canvas);

        // Create main editor layout with docking
        CreateEditorLayout(canvas);
    }

    private void CreateMenuBar(Entity canvas)
    {
        var menuDefs = new (string Label, MenuItemDef[] Items)[]
        {
            ("File", [
                new MenuItemDef("New Scene", "new_scene", Shortcut: "Ctrl+N"),
                new MenuItemDef("Open Scene", "open_scene", Shortcut: "Ctrl+O"),
                new MenuItemDef("Save Scene", "save_scene", Shortcut: "Ctrl+S"),
                new MenuItemDef("Save Scene As...", "save_scene_as", Shortcut: "Ctrl+Shift+S"),
                new MenuItemDef("---", "sep1", IsSeparator: true),
                new MenuItemDef("New Project", "new_project"),
                new MenuItemDef("Open Project", "open_project"),
                new MenuItemDef("---", "sep2", IsSeparator: true),
                new MenuItemDef("Exit", "exit", Shortcut: "Alt+F4")
            ]),
            ("Edit", [
                new MenuItemDef("Undo", "undo", Shortcut: "Ctrl+Z"),
                new MenuItemDef("Redo", "redo", Shortcut: "Ctrl+Y"),
                new MenuItemDef("---", "sep3", IsSeparator: true),
                new MenuItemDef("Cut", "cut", Shortcut: "Ctrl+X"),
                new MenuItemDef("Copy", "copy", Shortcut: "Ctrl+C"),
                new MenuItemDef("Paste", "paste", Shortcut: "Ctrl+V"),
                new MenuItemDef("Delete", "delete", Shortcut: "Del"),
                new MenuItemDef("---", "sep4", IsSeparator: true),
                new MenuItemDef("Select All", "select_all", Shortcut: "Ctrl+A"),
                new MenuItemDef("---", "sep_settings", IsSeparator: true),
                new MenuItemDef("Settings", "settings", Shortcut: "Ctrl+,")
            ]),
            ("Entity", [
                new MenuItemDef("Create Empty", "create_empty", Shortcut: "Ctrl+Shift+N"),
                new MenuItemDef("Create Child", "create_child"),
                new MenuItemDef("---", "sep5", IsSeparator: true),
                new MenuItemDef("Duplicate", "duplicate", Shortcut: "Ctrl+D"),
                new MenuItemDef("Rename", "rename", Shortcut: "F2")
            ]),
            ("Component", [
                new MenuItemDef("Add Component...", "add_component"),
                new MenuItemDef("Remove Component...", "remove_component")
            ]),
            ("Window", [
                new MenuItemDef("Hierarchy", "show_hierarchy"),
                new MenuItemDef("Inspector", "show_inspector"),
                new MenuItemDef("Project", "show_project"),
                new MenuItemDef("Console", "show_console"),
                new MenuItemDef("---", "sep6", IsSeparator: true),
                new MenuItemDef("Layout: Default", "layout_default"),
                new MenuItemDef("Layout: Tall", "layout_tall"),
                new MenuItemDef("Layout: Wide", "layout_wide"),
                new MenuItemDef("Layout: 2-Column", "layout_2column"),
                new MenuItemDef("Layout: 3-Column", "layout_3column"),
                new MenuItemDef("Layout: 4-Column", "layout_4column"),
                new MenuItemDef("---", "sep7", IsSeparator: true),
                new MenuItemDef("Save Layout...", "save_layout"),
                new MenuItemDef("Reset Layout", "reset_layout")
            ]),
            ("Help", [
                new MenuItemDef("Documentation", "docs", Shortcut: "F1"),
                new MenuItemDef("About KeenEyes", "about")
            ])
        };

        WidgetFactory.CreateMenuBar(_editorWorld, canvas, _defaultFont,
            menuDefs.Select(m => (m.Label, (IEnumerable<MenuItemDef>)m.Items)),
            new MenuBarConfig(
                Height: 26,
                BackgroundColor: new Vector4(0.15f, 0.15f, 0.18f, 1f),
                ItemColor: new Vector4(0.15f, 0.15f, 0.18f, 1f),
                ItemHoverColor: new Vector4(0.25f, 0.25f, 0.30f, 1f),
                TextColor: EditorColors.TextLight,
                FontSize: 13
            ));

        // Subscribe to menu actions
        SubscribeToMenuActions();
    }

    private void CreateEditorLayout(Entity canvas)
    {
        // Create main container below menu bar
        var mainContainer = WidgetFactory.CreatePanel(_editorWorld, canvas, "MainContainer", new PanelConfig(
            Direction: LayoutDirection.Horizontal,
            BackgroundColor: EditorColors.DarkPanel
        ));

        // Position below menu bar and fill remaining space
        ref var mainRect = ref _editorWorld.Get<UIRect>(mainContainer);
        mainRect.AnchorMin = new Vector2(0, 0);
        mainRect.AnchorMax = new Vector2(1, 1);
        mainRect.Offset = new UIEdges(0, 26, 0, 0); // Top offset for menu bar

        // Left panel: Hierarchy
        _ = HierarchyPanel.Create(_editorWorld, mainContainer, _defaultFont, _worldManager);

        // Center: Viewport (placeholder for now)
        _ = CreateViewportPlaceholder(mainContainer);

        // Right panel: Inspector
        _ = InspectorPanel.Create(_editorWorld, mainContainer, _defaultFont, _worldManager);
    }

    private Entity CreateViewportPlaceholder(Entity parent)
    {
        var viewport = WidgetFactory.CreatePanel(_editorWorld, parent, "Viewport", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            MainAxisAlign: LayoutAlign.Center,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: new Vector4(0.08f, 0.08f, 0.10f, 1f)
        ));

        // Make viewport fill remaining space
        ref var viewportRect = ref _editorWorld.Get<UIRect>(viewport);
        viewportRect.WidthMode = UISizeMode.Fill;
        viewportRect.HeightMode = UISizeMode.Fill;

        // Add placeholder text
        WidgetFactory.CreateLabel(_editorWorld, viewport, "ViewportLabel", "3D Viewport", _defaultFont, new LabelConfig(
            FontSize: 24,
            TextColor: EditorColors.TextMuted,
            HorizontalAlign: TextAlignH.Center
        ));

        WidgetFactory.CreateLabel(_editorWorld, viewport, "ViewportSubLabel", "(Scene rendering coming soon)", _defaultFont, new LabelConfig(
            FontSize: 14,
            TextColor: new Vector4(0.4f, 0.4f, 0.45f, 1f),
            HorizontalAlign: TextAlignH.Center
        ));

        return viewport;
    }

    private void SubscribeToMenuActions()
    {
        _editorWorld.Subscribe<UIMenuItemClickEvent>(e =>
        {
            switch (e.ItemId)
            {
                case "exit":
                    SaveLayoutAndExit();
                    break;
                case "new_scene":
                    _worldManager.NewScene();
                    Console.WriteLine("New scene created");
                    break;
                case "create_empty":
                    CreateEmptyEntity();
                    break;

                // Layout presets
                case "layout_default":
                    _layoutManager.ApplyPreset(LayoutPreset.Default);
                    Console.WriteLine("Applied default layout");
                    break;
                case "layout_tall":
                    _layoutManager.ApplyPreset(LayoutPreset.Tall);
                    Console.WriteLine("Applied tall layout");
                    break;
                case "layout_wide":
                    _layoutManager.ApplyPreset(LayoutPreset.Wide);
                    Console.WriteLine("Applied wide layout");
                    break;
                case "layout_2column":
                    _layoutManager.ApplyPreset(LayoutPreset.TwoColumn);
                    Console.WriteLine("Applied 2-column layout");
                    break;
                case "layout_3column":
                    _layoutManager.ApplyPreset(LayoutPreset.ThreeColumn);
                    Console.WriteLine("Applied 3-column layout");
                    break;
                case "layout_4column":
                    _layoutManager.ApplyPreset(LayoutPreset.FourColumn);
                    Console.WriteLine("Applied 4-column layout");
                    break;
                case "save_layout":
                    // TODO: Show dialog to save custom layout
                    Console.WriteLine("Save Layout (dialog not yet implemented)");
                    break;
                case "reset_layout":
                    _layoutManager.ResetToDefault();
                    Console.WriteLine("Layout reset to default");
                    break;
            }
        });
    }

    private void SaveLayoutAndExit()
    {
        // Save layout before exiting
        _layoutManager.Save();
        Environment.Exit(0);
    }

    private void SetupInputHandling()
    {
        var input = _editorWorld.GetExtension<IInputContext>();

        input.Keyboard.OnKeyDown += args =>
        {
            // Let the shortcut manager handle keyboard events first
            if (_shortcuts.ProcessKeyDown(args.Key, args.Modifiers))
            {
                return; // Shortcut was handled
            }

            // Fallback for Escape key
            if (args.Key == Key.Escape)
            {
                Console.WriteLine("Escape pressed - closing editor...");
                Environment.Exit(0);
            }
        };

        input.Keyboard.OnKeyUp += args =>
        {
            _shortcuts.ProcessKeyUp(args.Key, args.Modifiers);
        };
    }

    private void CreateEmptyEntity()
    {
        var sceneWorld = _worldManager.CurrentSceneWorld;
        if (sceneWorld is null)
        {
            _worldManager.NewScene();
            sceneWorld = _worldManager.CurrentSceneWorld;
        }

        var entity = sceneWorld!.Spawn("New Entity").Build();
        Console.WriteLine($"Created entity: {sceneWorld.GetName(entity)}");
    }

    private static string? FindSystemFont()
    {
        string[] candidates =
        [
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\calibri.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/TTF/DejaVuSans.ttf",
            "/System/Library/Fonts/Helvetica.ttc",
            "/Library/Fonts/Arial.ttf"
        ];

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    #region IEditorShortcutActions Implementation

    /// <inheritdoc/>
    void IEditorShortcutActions.NewScene()
    {
        _worldManager.NewScene();
        Console.WriteLine("New scene created");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.OpenScene()
    {
        // TODO: Implement file dialog
        Console.WriteLine("Open Scene (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.SaveScene()
    {
        // TODO: Implement save
        Console.WriteLine("Save Scene (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.SaveSceneAs()
    {
        // TODO: Implement save as
        Console.WriteLine("Save Scene As (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Exit()
    {
        SaveLayoutAndExit();
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Undo()
    {
        if (_undoRedo.CanUndo)
        {
            _undoRedo.Undo();
            Console.WriteLine("Undo");
        }
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Redo()
    {
        if (_undoRedo.CanRedo)
        {
            _undoRedo.Redo();
            Console.WriteLine("Redo");
        }
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Cut()
    {
        // TODO: Implement cut
        Console.WriteLine("Cut (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Copy()
    {
        // TODO: Implement copy
        Console.WriteLine("Copy (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Paste()
    {
        // TODO: Implement paste
        Console.WriteLine("Paste (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Delete()
    {
        var selected = _selection.SelectedEntities.ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var sceneWorld = _worldManager.CurrentSceneWorld;
        if (sceneWorld is null)
        {
            return;
        }

        foreach (var entity in selected)
        {
            if (sceneWorld.IsAlive(entity))
            {
                var command = new DeleteEntityCommand(sceneWorld, entity);
                _undoRedo.Execute(command);
            }
        }

        _selection.ClearSelection();
        Console.WriteLine($"Deleted {selected.Count} entity(ies)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.SelectAll()
    {
        var sceneWorld = _worldManager.CurrentSceneWorld;
        if (sceneWorld is null)
        {
            return;
        }

        _selection.ClearSelection();
        foreach (var entity in sceneWorld.GetAllEntities())
        {
            _selection.AddToSelection(entity);
        }

        Console.WriteLine($"Selected all entities ({_selection.SelectedEntities.Count()})");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Duplicate()
    {
        // TODO: Implement duplicate
        Console.WriteLine("Duplicate (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Rename()
    {
        // TODO: Implement rename (show rename dialog)
        Console.WriteLine("Rename (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Settings()
    {
        // TODO: Open settings window
        Console.WriteLine("Settings (Ctrl+,)");
        Console.WriteLine($"  - Auto-save enabled: {EditorSettings.AutoSaveEnabled}");
        Console.WriteLine($"  - Auto-save interval: {EditorSettings.AutoSaveIntervalSeconds}s");
        Console.WriteLine($"  - Undo history limit: {EditorSettings.UndoHistoryLimit}");
        Console.WriteLine($"  - Theme: {EditorSettings.Theme}");
        Console.WriteLine($"  - Font size: {EditorSettings.FontSize}");
        Console.WriteLine($"  - Grid visible: {EditorSettings.GridVisible}");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.CreateEmpty()
    {
        CreateEmptyEntity();
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.CreateChild()
    {
        var parent = _selection.PrimarySelection;
        var sceneWorld = _worldManager.CurrentSceneWorld;
        if (sceneWorld is null)
        {
            _worldManager.NewScene();
            sceneWorld = _worldManager.CurrentSceneWorld!;
        }

        var command = new CreateEntityCommand(sceneWorld, "New Entity", parent);
        _undoRedo.Execute(command);
        _selection.Select(command.CreatedEntity);
        Console.WriteLine($"Created child entity: {sceneWorld.GetName(command.CreatedEntity)}");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.FocusSelection()
    {
        // TODO: Implement focus selection in viewport
        Console.WriteLine("Focus Selection (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ToggleGrid()
    {
        // TODO: Implement toggle grid
        Console.WriteLine("Toggle Grid (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ToggleWireframe()
    {
        // TODO: Implement toggle wireframe
        Console.WriteLine("Toggle Wireframe (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.TranslateMode()
    {
        Console.WriteLine("Translate Mode");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.RotateMode()
    {
        Console.WriteLine("Rotate Mode");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ScaleMode()
    {
        Console.WriteLine("Scale Mode");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ToggleSpace()
    {
        Console.WriteLine("Toggle Local/World Space");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.PlayStop()
    {
        if (_playMode is null)
        {
            Console.WriteLine("Play mode not initialized");
            return;
        }

        if (_playMode.IsInPlayMode)
        {
            _playMode.Stop();
            Console.WriteLine("Stopped play mode");
        }
        else
        {
            _playMode.Play();
            Console.WriteLine("Started play mode");
        }
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Pause()
    {
        if (_playMode is null)
        {
            return;
        }

        _playMode.TogglePlayPause();
        Console.WriteLine(_playMode.IsPaused ? "Paused" : "Resumed");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.StepFrame()
    {
        // TODO: Implement step frame
        Console.WriteLine("Step Frame (not yet implemented)");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ShowHierarchy()
    {
        Console.WriteLine("Show Hierarchy");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ShowInspector()
    {
        Console.WriteLine("Show Inspector");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ShowProject()
    {
        Console.WriteLine("Show Project");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ShowConsole()
    {
        Console.WriteLine("Show Console");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ResetLayout()
    {
        _layoutManager.ResetToDefault();
        Console.WriteLine("Layout reset to default");
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Documentation()
    {
        Console.WriteLine("Opening documentation...");
        // TODO: Open browser to docs
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.About()
    {
        Console.WriteLine("KeenEyes Editor - A modern ECS-based game editor");
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Save layout before disposing
        _layoutManager.Save();

        _worldManager.Dispose();
        _editorWorld.Dispose();
    }
}

/// <summary>
/// Color palette for the editor UI.
/// </summary>
internal static class EditorColors
{
    public static Vector4 DarkPanel => new(0.12f, 0.12f, 0.16f, 0.98f);
    public static Vector4 MediumPanel => new(0.16f, 0.16f, 0.20f, 0.95f);
    public static Vector4 LightPanel => new(0.20f, 0.20f, 0.25f, 0.90f);
    public static Vector4 ViewportBackground => new(0.15f, 0.15f, 0.18f, 1f);

    public static Vector4 TextWhite => new(1f, 1f, 1f, 1f);
    public static Vector4 TextLight => new(0.85f, 0.85f, 0.90f, 1f);
    public static Vector4 TextMuted => new(0.6f, 0.6f, 0.65f, 1f);

    public static Vector4 Primary => new(0.25f, 0.47f, 0.85f, 1f);
    public static Vector4 Selection => new(0.3f, 0.5f, 0.8f, 0.5f);
    public static Vector4 Hover => new(0.25f, 0.25f, 0.30f, 1f);
}
