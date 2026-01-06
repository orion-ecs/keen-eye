using System.Numerics;
using KeenEyes.Editor.Assets;
using KeenEyes.Editor.Commands;
using KeenEyes.Editor.HotReload;
using KeenEyes.Editor.Layout;
using KeenEyes.Editor.Logging;
using KeenEyes.Editor.Panels;
using KeenEyes.Editor.PlayMode;
using KeenEyes.Editor.Selection;
using KeenEyes.Editor.Serialization;
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
    private readonly EditorLogProvider _logProvider;
    private readonly AssetDatabase _assetDatabase;
    private readonly EditorComponentSerializer _serializer;
    private PlayModeManager? _playMode;
    private HotReloadService? _hotReload;
    private Entity _viewportPanel;
    private Entity _hierarchyPanel;
    private Entity _inspectorPanel;
    private Entity _consolePanel;
    private Entity _projectPanel;
    private Entity _bottomTabView;
    private Entity[] _bottomTabContentPanels = [];
    private Entity _bottomDockSplitter;
    private FontHandle _defaultFont;
    private Entity _unsavedChangesDialog;
    private Entity _saveAsDialog;
    private Action? _pendingActionAfterDialog;
    private string? _pendingOpenScenePath;
    private bool _isDisposed;
    private bool _isBottomDockCollapsed;

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
    /// Gets the hot reload service, if initialized.
    /// </summary>
    public HotReloadService? HotReload => _hotReload;

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
        _logProvider = new EditorLogProvider();
        _assetDatabase = new AssetDatabase(Environment.CurrentDirectory);
        _serializer = new EditorComponentSerializer();

        // Scan for known asset types
        _assetDatabase.Scan(".kescene", ".keprefab", ".keworld");

        // Subscribe to scene events for play mode manager
        _worldManager.SceneOpened += OnSceneOpened;
        _worldManager.SceneClosed += OnSceneClosed;

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
            .OnUpdate(OnEditorUpdate)
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

    private void OnEditorUpdate(float deltaTime)
    {
        // Update viewport for camera controls and gizmo interaction
        if (_viewportPanel.IsValid)
        {
            ViewportPanel.Update(_editorWorld, _viewportPanel, deltaTime);
        }
    }

    private void OnSceneOpened(World sceneWorld)
    {
        _playMode = new PlayModeManager(sceneWorld, _serializer);
        Console.WriteLine("Play mode manager initialized for scene");

        // Initialize hot reload service
        _hotReload = new HotReloadService(sceneWorld);
        _hotReload.StatusChanged += OnHotReloadStatusChanged;
        _hotReload.SourceFileChanged += OnHotReloadFileChanged;
        _hotReload.ConnectPlayMode(_playMode);
        _hotReload.Initialize();
        Console.WriteLine("Hot reload service initialized for scene");
    }

    private void OnSceneClosed()
    {
        // Dispose hot reload service
        if (_hotReload != null)
        {
            _hotReload.StatusChanged -= OnHotReloadStatusChanged;
            _hotReload.SourceFileChanged -= OnHotReloadFileChanged;
            _hotReload.Dispose();
            _hotReload = null;
            Console.WriteLine("Hot reload service disposed");
        }

        _playMode = null;
        Console.WriteLine("Play mode manager disposed");
    }

    private void OnHotReloadStatusChanged(object? sender, HotReloadStatusChangedEventArgs e)
    {
        var prefix = e.Status switch
        {
            HotReloadStatus.Disabled => "[HotReload] Disabled",
            HotReloadStatus.Idle => "[HotReload] Idle",
            HotReloadStatus.Pending => "[HotReload] Pending",
            HotReloadStatus.Building => "[HotReload] Building",
            HotReloadStatus.Loading => "[HotReload] Loading",
            HotReloadStatus.Ready => "[HotReload] Ready",
            HotReloadStatus.Failed => "[HotReload] Failed",
            _ => "[HotReload] Unknown"
        };

        if (!string.IsNullOrEmpty(e.Message))
        {
            Console.WriteLine($"{prefix}: {e.Message}");
        }
        else
        {
            Console.WriteLine(prefix);
        }
    }

    private void OnHotReloadFileChanged(object? sender, string filePath)
    {
        Console.WriteLine($"[HotReload] File changed: {Path.GetFileName(filePath)}");
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
        // Create main container below menu bar with vertical splitter for bottom dock
        var mainContainer = WidgetFactory.CreatePanel(_editorWorld, canvas, "MainContainer", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        // Position below menu bar and fill remaining space
        ref var mainRect = ref _editorWorld.Get<UIRect>(mainContainer);
        mainRect.AnchorMin = new Vector2(0, 0);
        mainRect.AnchorMax = new Vector2(1, 1);
        mainRect.Offset = new UIEdges(0, 26, 0, 0); // Top offset for menu bar

        // Create vertical splitter for main content (top) and bottom dock (bottom)
        var bottomDockRatio = _layoutManager.GetPanelState("console")?.Collapsed == true ? 0.95f : 0.75f;
        _isBottomDockCollapsed = bottomDockRatio > 0.9f;

        var (splitterContainer, topPane, bottomPane) = WidgetFactory.CreateSplitter(
            _editorWorld, mainContainer, "MainSplitter", new SplitterConfig(
                Orientation: LayoutDirection.Vertical,
                InitialRatio: bottomDockRatio,
                HandleSize: 4,
                MinFirstPane: 200,
                MinSecondPane: 80,
                HandleColor: new Vector4(0.2f, 0.2f, 0.25f, 1f),
                HandleHoverColor: new Vector4(0.3f, 0.3f, 0.4f, 1f)
            ));

        _bottomDockSplitter = splitterContainer;

        // Set up the top pane with horizontal layout for main panels
        ref var topLayout = ref _editorWorld.Get<UILayout>(topPane);
        topLayout.Direction = LayoutDirection.Horizontal;

        // Left panel: Hierarchy
        _hierarchyPanel = HierarchyPanel.Create(_editorWorld, topPane, _defaultFont, _worldManager);

        // Center: Viewport with 3D rendering
        var graphicsContext = _editorWorld.GetExtension<IGraphicsContext>();
        var inputContext = _editorWorld.GetExtension<IInputContext>();
        _viewportPanel = ViewportPanel.Create(_editorWorld, topPane, _defaultFont, _worldManager,
            graphicsContext!, inputContext!);

        // Right panel: Inspector
        _inspectorPanel = InspectorPanel.Create(_editorWorld, topPane, _defaultFont, _worldManager);

        // Create bottom dock area with tabbed Console and Project panels
        CreateBottomDock(bottomPane);
    }

    private void CreateBottomDock(Entity parent)
    {
        // Determine which tab should be selected based on layout state
        var selectedTabState = _layoutManager.GetPanelState("project");

        // Default to console tab (index 0), unless project was last visible
        var selectedTabIndex = selectedTabState?.Visible == true ? 1 : 0;

        // Tab configurations
        var tabs = new TabConfig[]
        {
            new("Console", MinWidth: 80, Padding: 12),
            new("Project", MinWidth: 80, Padding: 12)
        };

        // Create tab view with editor styling
        var (tabView, contentPanels) = WidgetFactory.CreateTabView(
            _editorWorld, parent, "BottomDockTabs", tabs, _defaultFont,
            new TabViewConfig(
                TabBarHeight: 28,
                TabSpacing: 2,
                SelectedIndex: selectedTabIndex,
                TabBarColor: EditorColors.MediumPanel,
                ContentColor: EditorColors.DarkPanel,
                TabColor: new Vector4(0.18f, 0.18f, 0.22f, 1f),
                ActiveTabColor: new Vector4(0.12f, 0.12f, 0.16f, 1f),
                TabTextColor: EditorColors.TextMuted,
                ActiveTabTextColor: EditorColors.TextLight,
                FontSize: 12
            ));

        _bottomTabView = tabView;
        _bottomTabContentPanels = contentPanels;

        // Make tab view fill the bottom pane
        ref var tabViewRect = ref _editorWorld.Get<UIRect>(tabView);
        tabViewRect.WidthMode = UISizeMode.Fill;
        tabViewRect.HeightMode = UISizeMode.Fill;

        // Create Console panel inside first tab content area
        _consolePanel = ConsolePanel.Create(_editorWorld, contentPanels[0], _defaultFont, _logProvider);

        // Make console panel fill its container
        ref var consoleRect = ref _editorWorld.Get<UIRect>(_consolePanel);
        consoleRect.WidthMode = UISizeMode.Fill;
        consoleRect.HeightMode = UISizeMode.Fill;

        // Create Project panel inside second tab content area
        _projectPanel = ProjectPanel.Create(_editorWorld, contentPanels[1], _defaultFont, _assetDatabase);

        // Make project panel fill its container
        ref var projectRect = ref _editorWorld.Get<UIRect>(_projectPanel);
        projectRect.WidthMode = UISizeMode.Fill;
        projectRect.HeightMode = UISizeMode.Fill;
    }

    private void SubscribeToMenuActions()
    {
        _editorWorld.Subscribe<UIMenuItemClickEvent>(e =>
        {
            switch (e.ItemId)
            {
                case "exit":
                    HandleExitWithUnsavedChangesCheck();
                    break;
                case "new_scene":
                    HandleNewSceneWithUnsavedChangesCheck();
                    break;
                case "open_scene":
                    HandleOpenSceneWithUnsavedChangesCheck();
                    break;
                case "save_scene":
                    HandleSaveScene();
                    break;
                case "save_scene_as":
                    HandleSaveSceneAs();
                    break;
                case "create_empty":
                    CreateEmptyEntity();
                    break;

                // Panel visibility toggles
                case "show_hierarchy":
                    TogglePanelVisibility("hierarchy", _hierarchyPanel);
                    break;
                case "show_inspector":
                    TogglePanelVisibility("inspector", _inspectorPanel);
                    break;
                case "show_project":
                    ShowBottomDockTab(1); // Project is tab index 1
                    break;
                case "show_console":
                    ShowBottomDockTab(0); // Console is tab index 0
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

        // Subscribe to asset open events from Project panel
        _editorWorld.Subscribe<ProjectAssetOpenRequestedEvent>(e =>
        {
            if (e.AssetType == AssetType.Scene)
            {
                var fullPath = Path.Combine(_assetDatabase.ProjectRoot, e.RelativePath);
                HandleOpenSceneWithUnsavedChangesCheck(fullPath);
            }
        });

        // Subscribe to modal result events for dialog handling
        _editorWorld.Subscribe<UIModalResultEvent>(OnModalResult);
    }

    #region Scene File Operations

    private void HandleNewSceneWithUnsavedChangesCheck()
    {
        if (_worldManager.HasUnsavedChanges)
        {
            ShowUnsavedChangesDialog(() =>
            {
                _worldManager.NewScene();
                Console.WriteLine("New scene created");
            });
        }
        else
        {
            _worldManager.NewScene();
            Console.WriteLine("New scene created");
        }
    }

    private void HandleOpenSceneWithUnsavedChangesCheck(string? specificPath = null)
    {
        _pendingOpenScenePath = specificPath;

        if (_worldManager.HasUnsavedChanges)
        {
            ShowUnsavedChangesDialog(() => PerformOpenScene());
        }
        else
        {
            PerformOpenScene();
        }
    }

    private void PerformOpenScene()
    {
        if (_pendingOpenScenePath is not null)
        {
            // Open the specific scene file
            if (File.Exists(_pendingOpenScenePath))
            {
                _worldManager.OpenScene(_pendingOpenScenePath);
                Console.WriteLine($"Scene opened: {_pendingOpenScenePath}");
            }
            else
            {
                Console.WriteLine($"Scene file not found: {_pendingOpenScenePath}");
            }
            _pendingOpenScenePath = null;
        }
        else
        {
            // No specific path - look for scenes in asset database
            var sceneAssets = _assetDatabase.AllAssets
                .Where(a => a.Type == AssetType.Scene)
                .ToList();

            if (sceneAssets.Count == 0)
            {
                Console.WriteLine("No scene files found in project. Create a new scene first.");
            }
            else if (sceneAssets.Count == 1)
            {
                // Only one scene, open it directly
                var fullPath = Path.Combine(_assetDatabase.ProjectRoot, sceneAssets[0].RelativePath);
                _worldManager.OpenScene(fullPath);
                Console.WriteLine($"Scene opened: {fullPath}");
            }
            else
            {
                // Multiple scenes - show the most recent one or first
                Console.WriteLine($"Found {sceneAssets.Count} scene files. Double-click a .kescene file in the Project panel to open it.");
                Console.WriteLine("Available scenes:");
                foreach (var scene in sceneAssets.Take(5))
                {
                    Console.WriteLine($"  - {scene.RelativePath}");
                }
            }
        }
    }

    private void HandleSaveScene()
    {
        if (_worldManager.CurrentScenePath is not null)
        {
            if (_worldManager.SaveScene())
            {
                Console.WriteLine($"Scene saved: {_worldManager.CurrentScenePath}");
            }
            else
            {
                Console.WriteLine("Failed to save scene");
            }
        }
        else
        {
            // No path set, need to Save As
            HandleSaveSceneAs();
        }
    }

    private void HandleSaveSceneAs()
    {
        ShowSaveAsDialog();
    }

    private void HandleExitWithUnsavedChangesCheck()
    {
        if (_worldManager.HasUnsavedChanges)
        {
            ShowUnsavedChangesDialog(() => SaveLayoutAndExit());
        }
        else
        {
            SaveLayoutAndExit();
        }
    }

    #endregion

    #region Dialog Management

    private void ShowUnsavedChangesDialog(Action onConfirm)
    {
        _pendingActionAfterDialog = onConfirm;

        // Find the canvas for dialog parenting
        var canvas = FindCanvas();
        if (!canvas.IsValid)
        {
            // No canvas available, just proceed
            onConfirm();
            return;
        }

        // Create the confirm dialog if it doesn't exist
        if (!_unsavedChangesDialog.IsValid)
        {
            var config = new ConfirmConfig(
                Title: "Unsaved Changes",
                Width: 400,
                OkButtonText: "Discard",
                CancelButtonText: "Cancel",
                CloseOnBackdropClick: false
            );

            var result = WidgetFactory.CreateConfirm(
                _editorWorld,
                canvas,
                "You have unsaved changes. Do you want to discard them?",
                _defaultFont,
                config);

            _unsavedChangesDialog = result.Modal;
        }

        // Show the dialog
        var modalSystem = _editorWorld.GetSystem<UIModalSystem>();
        modalSystem?.OpenModal(_unsavedChangesDialog);
    }

    private void ShowSaveAsDialog()
    {
        // Find the canvas for dialog parenting
        var canvas = FindCanvas();
        if (!canvas.IsValid)
        {
            Console.WriteLine("Save As: Cannot show dialog (no canvas)");
            return;
        }

        // Create the prompt dialog if it doesn't exist
        if (!_saveAsDialog.IsValid)
        {
            var defaultName = _worldManager.CurrentScenePath is not null
                ? Path.GetFileName(_worldManager.CurrentScenePath)
                : "scene.kescene";

            var config = new PromptConfig(
                Title: "Save Scene As",
                Width: 450,
                Placeholder: "Enter scene filename...",
                InitialValue: defaultName,
                OkButtonText: "Save",
                CancelButtonText: "Cancel"
            );

            var result = WidgetFactory.CreatePrompt(
                _editorWorld,
                canvas,
                "Enter a filename for the scene:",
                _defaultFont,
                config);

            _saveAsDialog = result.Modal;
        }
        else
        {
            // Update the initial value with current scene name
            var defaultName = _worldManager.CurrentScenePath is not null
                ? Path.GetFileName(_worldManager.CurrentScenePath)
                : "scene.kescene";

            // Find the text input and update its content
            UpdateSaveAsDialogText(defaultName);
        }

        // Show the dialog
        var modalSystem = _editorWorld.GetSystem<UIModalSystem>();
        modalSystem?.OpenModal(_saveAsDialog);
    }

    private void UpdateSaveAsDialogText(string text)
    {
        if (!_saveAsDialog.IsValid)
        {
            return;
        }

        // Find the content container
        if (_editorWorld.Has<UIModal>(_saveAsDialog))
        {
            ref readonly var modal = ref _editorWorld.Get<UIModal>(_saveAsDialog);
            if (modal.ContentContainer.IsValid)
            {
                // Find the text input in children
                foreach (var child in _editorWorld.GetChildren(modal.ContentContainer))
                {
                    if (_editorWorld.Has<UITextInput>(child) && _editorWorld.Has<UIText>(child))
                    {
                        ref var textComponent = ref _editorWorld.Get<UIText>(child);
                        ref var inputComponent = ref _editorWorld.Get<UITextInput>(child);
                        textComponent.Content = text;
                        inputComponent.CursorPosition = text.Length;
                        inputComponent.ShowingPlaceholder = string.IsNullOrEmpty(text);
                        break;
                    }
                }
            }
        }
    }

    private void OnModalResult(UIModalResultEvent e)
    {
        if (e.Modal == _unsavedChangesDialog)
        {
            if (e.Result == ModalResult.OK)
            {
                // User confirmed discarding changes
                _pendingActionAfterDialog?.Invoke();
            }
            _pendingActionAfterDialog = null;
        }
        else if (e.Modal == _saveAsDialog)
        {
            if (e.Result == ModalResult.OK)
            {
                // Get the text from the input
                var filename = GetSaveAsDialogText();
                if (!string.IsNullOrWhiteSpace(filename))
                {
                    // Ensure .kescene extension
                    if (!filename.EndsWith(".kescene", StringComparison.OrdinalIgnoreCase))
                    {
                        filename += ".kescene";
                    }

                    // Determine save path
                    var savePath = Path.IsPathRooted(filename)
                        ? filename
                        : Path.Combine(_assetDatabase.ProjectRoot, filename);

                    if (_worldManager.SaveSceneAs(savePath))
                    {
                        Console.WriteLine($"Scene saved as: {savePath}");
                        // Refresh asset database to show new file
                        _assetDatabase.Scan(".kescene", ".keprefab", ".keworld");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to save scene to: {savePath}");
                    }
                }
            }
        }
    }

    private string GetSaveAsDialogText()
    {
        if (!_saveAsDialog.IsValid || !_editorWorld.Has<UIModal>(_saveAsDialog))
        {
            return string.Empty;
        }

        ref readonly var modal = ref _editorWorld.Get<UIModal>(_saveAsDialog);
        if (!modal.ContentContainer.IsValid)
        {
            return string.Empty;
        }

        // Find the text input in children
        foreach (var child in _editorWorld.GetChildren(modal.ContentContainer))
        {
            if (_editorWorld.Has<UITextInput>(child) && _editorWorld.Has<UIText>(child))
            {
                ref readonly var textComponent = ref _editorWorld.Get<UIText>(child);
                ref readonly var inputComponent = ref _editorWorld.Get<UITextInput>(child);
                return inputComponent.ShowingPlaceholder ? string.Empty : textComponent.Content;
            }
        }

        return string.Empty;
    }

    private Entity FindCanvas()
    {
        // Find the UI canvas by looking for entities with UIRootTag component
        foreach (var entity in _editorWorld.GetAllEntities())
        {
            if (_editorWorld.Has<UIRootTag>(entity))
            {
                return entity;
            }
        }
        return Entity.Null;
    }

    #endregion

    #region Panel Management

    private void TogglePanelVisibility(string panelId, Entity panel)
    {
        if (!panel.IsValid)
        {
            return;
        }

        ref var element = ref _editorWorld.Get<UIElement>(panel);
        element.Visible = !element.Visible;

        _layoutManager.UpdatePanelState(panelId, element.Visible);
        Console.WriteLine($"{panelId} panel {(element.Visible ? "shown" : "hidden")}");
    }

    private void ShowBottomDockTab(int tabIndex)
    {
        // Expand the bottom dock if it's collapsed
        if (_isBottomDockCollapsed)
        {
            ExpandBottomDock();
        }

        // Switch to the requested tab
        if (_bottomTabView.IsValid && _editorWorld.Has<UITabViewState>(_bottomTabView))
        {
            ref var tabState = ref _editorWorld.Get<UITabViewState>(_bottomTabView);

            // Hide current tab content, show new tab content
            if (tabState.SelectedIndex != tabIndex)
            {
                // Hide old content panel
                if (tabState.SelectedIndex >= 0 && tabState.SelectedIndex < _bottomTabContentPanels.Length)
                {
                    ref var oldElement = ref _editorWorld.Get<UIElement>(_bottomTabContentPanels[tabState.SelectedIndex]);
                    oldElement.Visible = false;

                    // Add hidden tag for layout system
                    if (!_editorWorld.Has<UIHiddenTag>(_bottomTabContentPanels[tabState.SelectedIndex]))
                    {
                        _editorWorld.Add(_bottomTabContentPanels[tabState.SelectedIndex], new UIHiddenTag());
                    }
                }

                // Show new content panel
                if (tabIndex >= 0 && tabIndex < _bottomTabContentPanels.Length)
                {
                    ref var newElement = ref _editorWorld.Get<UIElement>(_bottomTabContentPanels[tabIndex]);
                    newElement.Visible = true;

                    // Remove hidden tag
                    if (_editorWorld.Has<UIHiddenTag>(_bottomTabContentPanels[tabIndex]))
                    {
                        _editorWorld.Remove<UIHiddenTag>(_bottomTabContentPanels[tabIndex]);
                    }
                }

                tabState.SelectedIndex = tabIndex;
            }

            var tabName = tabIndex == 0 ? "Console" : "Project";
            Console.WriteLine($"Switched to {tabName} panel");
        }
    }

    private void ExpandBottomDock()
    {
        if (!_bottomDockSplitter.IsValid || !_editorWorld.Has<UISplitter>(_bottomDockSplitter))
        {
            return;
        }

        ref var splitter = ref _editorWorld.Get<UISplitter>(_bottomDockSplitter);
        splitter.SplitRatio = 0.75f; // Expand to show bottom dock

        // Update the first pane size
        var firstPane = Entity.Null;
        foreach (var child in _editorWorld.GetChildren(_bottomDockSplitter))
        {
            if (_editorWorld.Has<UISplitterFirstPane>(child))
            {
                firstPane = child;
                break;
            }
        }

        if (firstPane.IsValid)
        {
            ref var rect = ref _editorWorld.Get<UIRect>(firstPane);
            rect.Size = new Vector2(0, splitter.SplitRatio * 100);
        }

        _isBottomDockCollapsed = false;
        _layoutManager.UpdatePanelState("console", true, collapsed: false);
        Console.WriteLine("Bottom dock expanded");
    }

    private void CollapseBottomDock()
    {
        if (!_bottomDockSplitter.IsValid || !_editorWorld.Has<UISplitter>(_bottomDockSplitter))
        {
            return;
        }

        ref var splitter = ref _editorWorld.Get<UISplitter>(_bottomDockSplitter);
        splitter.SplitRatio = 0.95f; // Collapse to minimal height

        // Update the first pane size
        var firstPane = Entity.Null;
        foreach (var child in _editorWorld.GetChildren(_bottomDockSplitter))
        {
            if (_editorWorld.Has<UISplitterFirstPane>(child))
            {
                firstPane = child;
                break;
            }
        }

        if (firstPane.IsValid)
        {
            ref var rect = ref _editorWorld.Get<UIRect>(firstPane);
            rect.Size = new Vector2(0, splitter.SplitRatio * 100);
        }

        _isBottomDockCollapsed = true;
        _layoutManager.UpdatePanelState("console", true, collapsed: true);
        Console.WriteLine("Bottom dock collapsed");
    }

    private void ToggleBottomDock()
    {
        if (_isBottomDockCollapsed)
        {
            ExpandBottomDock();
        }
        else
        {
            CollapseBottomDock();
        }
    }

    #endregion

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
        HandleNewSceneWithUnsavedChangesCheck();
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.OpenScene()
    {
        HandleOpenSceneWithUnsavedChangesCheck();
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.SaveScene()
    {
        HandleSaveScene();
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.SaveSceneAs()
    {
        HandleSaveSceneAs();
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.Exit()
    {
        HandleExitWithUnsavedChangesCheck();
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
        TogglePanelVisibility("hierarchy", _hierarchyPanel);
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ShowInspector()
    {
        TogglePanelVisibility("inspector", _inspectorPanel);
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ShowProject()
    {
        ShowBottomDockTab(1); // Project is tab index 1
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ShowConsole()
    {
        ShowBottomDockTab(0); // Console is tab index 0
    }

    /// <inheritdoc/>
    void IEditorShortcutActions.ToggleBottomDock()
    {
        ToggleBottomDock();
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

        _hotReload?.Dispose();
        _logProvider.Dispose();
        _assetDatabase.Dispose();
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
