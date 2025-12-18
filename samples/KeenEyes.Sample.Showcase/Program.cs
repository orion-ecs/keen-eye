// KeenEyes Showcase Sample
// A comprehensive demonstration of the KeenEyes ECS framework featuring:
// - 3D rendering with bouncing balls in an arena
// - UI system with buttons to spawn entities
// - First-person camera controls (WASD + mouse)
// - Entity spawning and physics simulation
//
// Controls:
//   WASD / Arrow Keys  - Move camera
//   E / Space          - Move up
//   Q / Ctrl           - Move down
//   Left Click         - Capture mouse for look control
//   Escape             - Release mouse / Exit
//
// NOTE: This sample requires a display to run.

using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Input.Abstractions;
using KeenEyes.Input.Silk;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;
using KeenEyes.Sample.Showcase;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

Console.WriteLine("KeenEyes Showcase Sample");
Console.WriteLine("========================");
Console.WriteLine();
Console.WriteLine("This sample demonstrates:");
Console.WriteLine("- 3D rendering with dynamic entity spawning");
Console.WriteLine("- ECS-based UI with interactive buttons");
Console.WriteLine("- First-person camera controls");
Console.WriteLine("- Simple physics simulation (bouncing balls)");
Console.WriteLine();
Console.WriteLine("Controls:");
Console.WriteLine("  WASD / Arrow Keys  - Move camera");
Console.WriteLine("  E / Space          - Move up");
Console.WriteLine("  Q / Ctrl           - Move down");
Console.WriteLine("  Left Click         - Capture mouse for look control");
Console.WriteLine("  Escape             - Release mouse / Exit");
Console.WriteLine();

// Find a suitable font file
var fontPath = FindSystemFont();
if (fontPath is null)
{
    Console.WriteLine("Warning: No suitable system font found. Text will not be visible.");
}
else
{
    Console.WriteLine($"Using font: {fontPath}");
}

// Configure window
var windowConfig = new WindowConfig
{
    Title = "KeenEyes Showcase - Click to capture mouse, Escape to release",
    Width = 1280,
    Height = 720,
    VSync = true
};

// Configure graphics
var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.1f, 0.15f, 0.2f, 1f),
    EnableDepthTest = true,
    EnableCulling = true
};

// Configure input
var inputConfig = new SilkInputConfig
{
    EnableGamepads = true,
    MaxGamepads = 4,
    GamepadDeadzone = 0.15f,
    CaptureMouseOnClick = false
};

using var world = new World();

// Install plugins (order matters)
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));
world.InstallPlugin(new UIPlugin());

// Register graphics systems
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);

// Register game systems
world.AddSystem<CameraControlSystem>(SystemPhase.Update, order: 0);
world.AddSystem<BallPhysicsSystem>(SystemPhase.Update, order: 10);
world.AddSystem<SpinSystem>(SystemPhase.Update, order: 20);

// Store global state
MeshHandle ballMesh = default;
Entity spawnerEntity = default;

Console.WriteLine("Starting...");

try
{
    world.CreateRunner()
        .OnReady(() =>
        {
            Console.WriteLine("Systems initialized!");

            var graphics = world.GetExtension<IGraphicsContext>();
            var input = world.GetExtension<IInputContext>();
            var ui = world.GetExtension<UIContext>();

            // Create mesh resources
            ballMesh = graphics.CreateCube(1f);
            var groundMesh = graphics.CreateQuad(20f, 20f);
            var wallMesh = graphics.CreateQuad(20f, 20f);

            // Create the scene
            CreateScene(world, graphics, groundMesh, wallMesh);

            // Create ball spawner singleton
            spawnerEntity = world.Spawn()
                .WithName("BallSpawner")
                .With(new BallSpawner
                {
                    BallCount = 0,
                    MaxBalls = 100,
                    SphereMeshId = ballMesh.Id,
                    ShaderId = graphics.LitShader.Id,
                    TextureId = graphics.WhiteTexture.Id
                })
                .Build();

            // Load font and create UI
            FontHandle font = default;
            var fontManagerProvider = world.GetExtension<IFontManagerProvider>();
            var fontManager = fontManagerProvider?.GetFontManager();
            if (fontManager is not null && fontPath is not null)
            {
                try
                {
                    font = fontManager.LoadFont(fontPath, 16f);
                    Console.WriteLine("Font loaded successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load font: {ex.Message}");
                }
            }

            // Create UI
            CreateUI(world, ui, font, spawnerEntity, ballMesh, graphics);

            // Subscribe to UI events
            SubscribeToUIEvents(world, spawnerEntity, ballMesh, graphics);

            // Setup input events
            SetupInputEvents(input, world);

            Console.WriteLine();
            Console.WriteLine("Scene created! Try spawning some balls with the UI buttons.");
        })
        .OnResize((width, height) =>
        {
            var layoutSystem = world.GetSystem<UILayoutSystem>();
            layoutSystem?.SetScreenSize(width, height);
        })
        .Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("This sample requires a display.");
}

Console.WriteLine("Sample complete!");

// Find a suitable system font
static string? FindSystemFont()
{
    string[] candidates =
    [
        @"C:\Windows\Fonts\segoeui.ttf",
        @"C:\Windows\Fonts\arial.ttf",
        @"C:\Windows\Fonts\calibri.ttf",
        @"C:\Windows\Fonts\verdana.ttf",
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

// Create the 3D scene
static void CreateScene(World world, IGraphicsContext graphics, MeshHandle groundMesh, MeshHandle wallMesh)
{
    // Create camera with controller
    var cameraPosition = new Vector3(0, 8, 15);

    world.Spawn()
        .WithName("MainCamera")
        .With(new Transform3D(
            cameraPosition,
            Quaternion.CreateFromYawPitchRoll(0, -0.3f, 0),
            Vector3.One))
        .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
        .With(new CameraController
        {
            Yaw = 0,
            Pitch = -0.3f,
            Sensitivity = 0.003f,
            MoveSpeed = 8f
        })
        .WithTag<MainCameraTag>()
        .Build();

    // Create directional light (sun)
    world.Spawn()
        .WithName("Sun")
        .With(new Transform3D(
            Vector3.Zero,
            Quaternion.CreateFromYawPitchRoll(0.5f, -0.8f, 0),
            Vector3.One))
        .With(Light.Directional(new Vector3(1f, 0.95f, 0.8f), 1.0f))
        .Build();

    // Create a point light for better visibility
    world.Spawn()
        .WithName("PointLight")
        .With(new Transform3D(
            new Vector3(0, 10f, 0),
            Quaternion.Identity,
            Vector3.One))
        .With(Light.Point(new Vector3(1f, 0.9f, 0.8f), 0.5f, 30f))
        .Build();

    // Create ground plane
    world.Spawn()
        .WithName("Ground")
        .With(new Transform3D(
            new Vector3(0, 0, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(groundMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.3f, 0.4f, 0.35f, 1f),
            Metallic = 0.1f,
            Roughness = 0.8f
        })
        .WithTag<GroundTag>()
        .Build();

    // Create walls around the arena (4 walls)
    var wallColor = new Vector4(0.4f, 0.45f, 0.5f, 0.8f);

    // Back wall (-Z)
    world.Spawn()
        .WithName("WallBack")
        .With(new Transform3D(
            new Vector3(0, 10f, -10f),
            Quaternion.Identity,
            Vector3.One))
        .With(new Renderable(wallMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = wallColor,
            Metallic = 0.2f,
            Roughness = 0.7f
        })
        .WithTag<WallTag>()
        .Build();

    // Front wall (+Z)
    world.Spawn()
        .WithName("WallFront")
        .With(new Transform3D(
            new Vector3(0, 10f, 10f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI),
            Vector3.One))
        .With(new Renderable(wallMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = wallColor,
            Metallic = 0.2f,
            Roughness = 0.7f
        })
        .WithTag<WallTag>()
        .Build();

    // Left wall (-X)
    world.Spawn()
        .WithName("WallLeft")
        .With(new Transform3D(
            new Vector3(-10f, 10f, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(wallMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = wallColor,
            Metallic = 0.2f,
            Roughness = 0.7f
        })
        .WithTag<WallTag>()
        .Build();

    // Right wall (+X)
    world.Spawn()
        .WithName("WallRight")
        .With(new Transform3D(
            new Vector3(10f, 10f, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, -MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(wallMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = wallColor,
            Metallic = 0.2f,
            Roughness = 0.7f
        })
        .WithTag<WallTag>()
        .Build();

    Console.WriteLine("Arena created with ground and walls!");
}

// Create the UI using WidgetFactory
static void CreateUI(World world, UIContext ui, FontHandle font, Entity spawnerEntity, MeshHandle ballMesh, IGraphicsContext graphics)
{
    // Color palette
    var panelBg = new Vector4(0.12f, 0.12f, 0.16f, 0.95f);
    var primaryBlue = new Vector4(0.25f, 0.47f, 0.85f, 1f);
    var primaryBorder = new Vector4(0.35f, 0.57f, 0.95f, 1f);
    var successGreen = new Vector4(0.22f, 0.65f, 0.35f, 1f);
    var successBorder = new Vector4(0.32f, 0.75f, 0.45f, 1f);
    var dangerRed = new Vector4(0.75f, 0.22f, 0.22f, 1f);
    var dangerBorder = new Vector4(0.85f, 0.32f, 0.32f, 1f);

    // Create root canvas
    var canvas = ui.CreateCanvas("MainCanvas");

    // Create a control panel in the top-left corner using WidgetFactory
    var panel = WidgetFactory.CreatePanel(world, canvas, "ControlPanel", new PanelConfig(
        Width: 220,
        Height: 350,
        Direction: LayoutDirection.Vertical,
        MainAxisAlign: LayoutAlign.Start,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 12,
        BackgroundColor: panelBg,
        CornerRadius: 10,
        Padding: UIEdges.All(15)
    ));

    // Position panel at top-left
    ref var panelRect = ref world.Get<UIRect>(panel);
    panelRect.AnchorMin = new Vector2(0, 0);
    panelRect.AnchorMax = new Vector2(0, 0);
    panelRect.Pivot = new Vector2(0, 0);
    panelRect.Offset = new UIEdges(20, 20, 0, 0);

    // Title
    WidgetFactory.CreateLabel(world, panel, "Title", "Ball Spawner", font, new LabelConfig(
        Width: 190,
        Height: 35,
        FontSize: 22,
        TextColor: new Vector4(1f, 1f, 1f, 1f),
        HorizontalAlign: TextAlignH.Center
    ));

    // Divider
    WidgetFactory.CreateDivider(world, panel, "TitleDivider", new DividerConfig(
        Thickness: 1,
        Color: new Vector4(0.4f, 0.4f, 0.5f, 0.5f),
        Margin: 5
    ));

    // Ball count label
    WidgetFactory.CreateLabel(world, panel, "CountLabel", "Balls: 0 / 100", font, new LabelConfig(
        Width: 190,
        Height: 25,
        FontSize: 14,
        TextColor: new Vector4(0.8f, 0.9f, 1f, 1f),
        HorizontalAlign: TextAlignH.Center
    ));

    // Progress bar for ball count
    WidgetFactory.CreateProgressBar(world, panel, "BallProgress", font, new ProgressBarConfig(
        Width: 190,
        Height: 12,
        Value: 0,
        MaxValue: 100,
        FillColor: successGreen,
        CornerRadius: 6
    ));

    // Spawn buttons
    WidgetFactory.CreateButton(world, panel, "Spawn1Btn", "Spawn 1 Ball", font, new ButtonConfig(
        Width: 180,
        Height: 38,
        BackgroundColor: primaryBlue,
        BorderColor: primaryBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        FontSize: 14,
        TabIndex: 1
    ));

    WidgetFactory.CreateButton(world, panel, "Spawn5Btn", "Spawn 5 Balls", font, new ButtonConfig(
        Width: 180,
        Height: 38,
        BackgroundColor: primaryBlue,
        BorderColor: primaryBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        FontSize: 14,
        TabIndex: 2
    ));

    WidgetFactory.CreateButton(world, panel, "Spawn10Btn", "Spawn 10 Balls", font, new ButtonConfig(
        Width: 180,
        Height: 38,
        BackgroundColor: successGreen,
        BorderColor: successBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        FontSize: 14,
        TabIndex: 3
    ));

    // Divider before clear
    WidgetFactory.CreateDivider(world, panel, "ClearDivider", new DividerConfig(
        Thickness: 1,
        Color: new Vector4(0.4f, 0.4f, 0.5f, 0.3f),
        Margin: 3
    ));

    // Clear button (danger red)
    WidgetFactory.CreateButton(world, panel, "ClearBtn", "Clear All", font, new ButtonConfig(
        Width: 180,
        Height: 38,
        BackgroundColor: dangerRed,
        BorderColor: dangerBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        FontSize: 14,
        TabIndex: 4
    ));

    // Instructions panel at bottom-center
    var instructions = WidgetFactory.CreatePanel(world, canvas, "Instructions", new PanelConfig(
        Width: 520,
        Height: 65,
        Direction: LayoutDirection.Vertical,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 5,
        BackgroundColor: new Vector4(0.08f, 0.08f, 0.12f, 0.85f),
        CornerRadius: 8,
        Padding: UIEdges.All(10)
    ));

    // Position at bottom-center
    ref var instrRect = ref world.Get<UIRect>(instructions);
    instrRect.AnchorMin = new Vector2(0.5f, 1);
    instrRect.AnchorMax = new Vector2(0.5f, 1);
    instrRect.Pivot = new Vector2(0.5f, 1);
    instrRect.Offset = new UIEdges(0, 0, 0, 20);

    WidgetFactory.CreateLabel(world, instructions, "InstrLine1",
        "WASD: Move | Mouse: Look (click to capture)", font, new LabelConfig(
            Width: 500,
            Height: 20,
            FontSize: 13,
            TextColor: new Vector4(0.7f, 0.7f, 0.75f, 1f),
            HorizontalAlign: TextAlignH.Center
        ));

    WidgetFactory.CreateLabel(world, instructions, "InstrLine2",
        "E/Space: Up | Q/Ctrl: Down | Escape: Release/Exit", font, new LabelConfig(
            Width: 500,
            Height: 20,
            FontSize: 13,
            TextColor: new Vector4(0.6f, 0.6f, 0.65f, 1f),
            HorizontalAlign: TextAlignH.Center
        ));

    Console.WriteLine("UI created with WidgetFactory!");
}

// Subscribe to UI click events
static void SubscribeToUIEvents(World world, Entity spawnerEntity, MeshHandle ballMesh, IGraphicsContext graphics)
{
    // Log all UI click events
    world.Subscribe<UIClickEvent>(e =>
    {
        var name = world.GetName(e.Element) ?? "unnamed";
        Console.WriteLine($"[UI Click] Element: {name} at ({e.Position.X:F0}, {e.Position.Y:F0})");

        if (name == "unnamed")
        {
            return;
        }

        ref var spawner = ref world.Get<BallSpawner>(spawnerEntity);

        switch (name)
        {
            case "Spawn1Btn":
                SpawnBalls(world, ref spawner, graphics, 1);
                break;
            case "Spawn5Btn":
                SpawnBalls(world, ref spawner, graphics, 5);
                break;
            case "Spawn10Btn":
                SpawnBalls(world, ref spawner, graphics, 10);
                break;
            case "ClearBtn":
                ClearAllBalls(world, ref spawner);
                break;
        }

        // Update the count label
        UpdateCountLabel(world, spawner.BallCount, spawner.MaxBalls);
    });

    // Log hover events
    world.Subscribe<UIPointerEnterEvent>(e =>
    {
        var name = world.GetName(e.Element) ?? "unnamed";
        Console.WriteLine($"[UI Hover Enter] {name}");
    });

    world.Subscribe<UIPointerExitEvent>(e =>
    {
        var name = world.GetName(e.Element) ?? "unnamed";
        Console.WriteLine($"[UI Hover Exit] {name}");
    });
}

// Log UI element bounds for debugging
static void LogUIElementBounds(World world)
{
    Console.WriteLine("\n[UI Diagnostics] Element bounds:");
    foreach (var entity in world.Query<UIElement, UIRect>())
    {
        var name = world.GetName(entity) ?? "unnamed";
        ref readonly var rect = ref world.Get<UIRect>(entity);
        var bounds = rect.ComputedBounds;
        Console.WriteLine($"  {name}: Bounds({bounds.X:F0}, {bounds.Y:F0}, {bounds.Width:F0}, {bounds.Height:F0}) " +
                          $"Anchor({rect.AnchorMin.X:F1},{rect.AnchorMin.Y:F1})-({rect.AnchorMax.X:F1},{rect.AnchorMax.Y:F1}) " +
                          $"Size({rect.Size.X:F0}, {rect.Size.Y:F0})");
    }
    Console.WriteLine();
}

// Spawn balls
static void SpawnBalls(World world, ref BallSpawner spawner, IGraphicsContext graphics, int count)
{
    var spawnedCount = 0;

    for (int i = 0; i < count && spawner.BallCount < spawner.MaxBalls; i++)
    {
        // Random position above the arena
        var x = world.NextFloat() * 12f - 6f;
        var y = world.NextFloat() * 5f + 10f;
        var z = world.NextFloat() * 12f - 6f;

        // Random velocity
        var vx = world.NextFloat() * 10f - 5f;
        var vy = world.NextFloat() * 5f;
        var vz = world.NextFloat() * 10f - 5f;

        // Random color
        var r = world.NextFloat() * 0.5f + 0.5f;
        var g = world.NextFloat() * 0.5f + 0.5f;
        var b = world.NextFloat() * 0.5f + 0.5f;

        // Random size
        var size = world.NextFloat() * 0.5f + 0.5f; // 0.5 to 1.0

        // Random spin
        var spinX = world.NextFloat() * 2f - 1f;
        var spinY = world.NextFloat() * 2f - 1f;
        var spinZ = world.NextFloat() * 2f - 1f;

        world.Spawn()
            .WithName($"Ball_{spawner.BallCount}")
            .With(new Transform3D(
                new Vector3(x, y, z),
                Quaternion.Identity,
                new Vector3(size, size, size)))
            .With(new Renderable(spawner.SphereMeshId, 0))
            .With(new Material
            {
                ShaderId = spawner.ShaderId,
                TextureId = spawner.TextureId,
                Color = new Vector4(r, g, b, 1f),
                Metallic = 0.4f,
                Roughness = 0.4f
            })
            .With(new BallPhysics
            {
                Velocity = new Vector3(vx, vy, vz),
                Radius = size * 0.5f,
                Bounciness = 0.8f
            })
            .With(new Spin { Speed = new Vector3(spinX, spinY, spinZ) })
            .WithTag<BallTag>()
            .Build();

        spawner.BallCount++;
        spawnedCount++;
    }

    Console.WriteLine($"Spawned {spawnedCount} balls (total: {spawner.BallCount})");
}

// Clear all balls
static void ClearAllBalls(World world, ref BallSpawner spawner)
{
    var toRemove = new List<Entity>();

    foreach (var entity in world.Query<BallTag>())
    {
        toRemove.Add(entity);
    }

    foreach (var entity in toRemove)
    {
        world.Despawn(entity);
    }

    spawner.BallCount = 0;
    Console.WriteLine("Cleared all balls!");
}

// Update the count label text
static void UpdateCountLabel(World world, int count, int max)
{
    // Find the count label by name and update text
    foreach (var entity in world.Query<UIText>())
    {
        var name = world.GetName(entity);
        if (name == "CountLabel")
        {
            ref var text = ref world.Get<UIText>(entity);
            text.Content = $"Balls: {count} / {max}";
            break;
        }
    }
}

// Setup input events
static void SetupInputEvents(IInputContext input, World world)
{
    var keyboard = input.Keyboard;
    var mouse = input.Mouse;

    // Toggle mouse capture on click
    mouse.OnButtonDown += args =>
    {
        Console.WriteLine($"[Mouse Button] {args.Button} at ({args.Position.X:F0}, {args.Position.Y:F0})");
        if (args.Button == MouseButton.Left && !mouse.IsCursorCaptured)
        {
            mouse.IsCursorCaptured = true;
            mouse.IsCursorVisible = false;
            Console.WriteLine("Mouse captured - move to look around");
        }
    };

    // Release mouse on escape, F1 for diagnostics
    keyboard.OnKeyDown += args =>
    {
        if (args.Key == Key.Escape)
        {
            if (mouse.IsCursorCaptured)
            {
                mouse.IsCursorCaptured = false;
                mouse.IsCursorVisible = true;
                Console.WriteLine("Mouse released");
            }
            else
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
        }
        else if (args.Key == Key.F1)
        {
            // Dump UI diagnostics
            LogUIElementBounds(world);
        }
        else if (args.Key == Key.F2)
        {
            // Log graphics info
            var graphics = world.GetExtension<IGraphicsContext>();
            Console.WriteLine($"[Graphics] Window size: {graphics.Width}x{graphics.Height}, Initialized: {graphics.IsInitialized}");
        }
    };
}

