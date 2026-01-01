using System.Numerics;
using KeenEyes.Editor.Panels;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Input.Abstractions;
using KeenEyes.Input.Silk;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Application;

/// <summary>
/// Main entry point for the KeenEyes Editor application.
/// Sets up the window, graphics, input, and UI systems, then creates the editor layout.
/// </summary>
public sealed class EditorApplication : IDisposable
{
    private readonly World _editorWorld;
    private readonly EditorWorldManager _worldManager;
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
    /// Creates a new editor application instance.
    /// </summary>
    public EditorApplication()
    {
        _editorWorld = new World();
        _worldManager = new EditorWorldManager();
    }

    /// <summary>
    /// Runs the editor application.
    /// </summary>
    public void Run()
    {
        // Configure window
        var windowConfig = new WindowConfig
        {
            Title = "KeenEyes Editor",
            Width = 1600,
            Height = 900,
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
                new MenuItemDef("Select All", "select_all", Shortcut: "Ctrl+A")
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
                    Environment.Exit(0);
                    break;
                case "new_scene":
                    _worldManager.NewScene();
                    Console.WriteLine("New scene created");
                    break;
                case "create_empty":
                    CreateEmptyEntity();
                    break;
                    // Add more menu action handlers
            }
        });
    }

    private void SetupInputHandling()
    {
        var input = _editorWorld.GetExtension<IInputContext>();
        input.Keyboard.OnKeyDown += args =>
        {
            if (args.Key == Key.Escape)
            {
                Console.WriteLine("Escape pressed - closing editor...");
                Environment.Exit(0);
            }
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

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

    public static Vector4 TextWhite => new(1f, 1f, 1f, 1f);
    public static Vector4 TextLight => new(0.85f, 0.85f, 0.90f, 1f);
    public static Vector4 TextMuted => new(0.6f, 0.6f, 0.65f, 1f);

    public static Vector4 Primary => new(0.25f, 0.47f, 0.85f, 1f);
    public static Vector4 Selection => new(0.3f, 0.5f, 0.8f, 0.5f);
    public static Vector4 Hover => new(0.25f, 0.25f, 0.30f, 1f);
}
