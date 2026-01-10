// KeenEyes Input Sample
// Demonstrates keyboard, mouse, and gamepad input handling.
//
// Controls:
//   WASD / Left Stick  - Move the cube
//   Mouse              - Look around (when captured)
//   Left Click         - Capture/release mouse
//   Space / South      - Jump (using InputAction with multiple bindings)
//   Escape             - Exit
//
// This sample demonstrates:
//   - Direct device polling (keyboard.IsKeyDown, gamepad.LeftStick)
//   - Event-based input handlers (OnKeyDown, OnButtonDown)
//   - Action mapping (InputAction with multiple bindings for rebindable controls)
//
// NOTE: This sample requires a display to run.

using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Input;
using KeenEyes.Input.Abstractions;
using KeenEyes.Input.Silk;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;
using KeenEyes.Sample.Input;

Console.WriteLine("KeenEyes Input Sample");
Console.WriteLine("=====================");
Console.WriteLine();
Console.WriteLine("This sample demonstrates:");
Console.WriteLine("- Keyboard input (WASD to move)");
Console.WriteLine("- Mouse input (click to capture, move to look)");
Console.WriteLine("- Gamepad input (if connected)");
Console.WriteLine("- Polling-based input in systems");
Console.WriteLine("- Event-based input handlers");
Console.WriteLine("- Action mapping (InputAction for rebindable controls)");
Console.WriteLine();
Console.WriteLine("Press Escape to exit.");
Console.WriteLine();

// Configure window
var windowConfig = new WindowConfig
{
    Title = "KeenEyes Input Sample - Click to capture mouse, Escape to exit",
    Width = 1280,
    Height = 720,
    VSync = true
};

// Configure graphics rendering settings
var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.1f, 0.1f, 0.2f, 1f),
    EnableDepthTest = true,
    EnableCulling = true
};

// Configure input
var inputConfig = new SilkInputConfig
{
    EnableGamepads = true,
    MaxGamepads = 4,
    GamepadDeadzone = 0.15f,
    CaptureMouseOnClick = false // We'll handle this manually to demonstrate
};

using var world = new World();

// Install plugins (order matters - window first, then graphics/input)
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));

// Register graphics systems
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);

// Register input demo system (processes input each frame)
world.AddSystem<PlayerMovementSystem>(SystemPhase.Update, order: 0);
world.AddSystem<InputDisplaySystem>(SystemPhase.Update, order: 100);

Console.WriteLine("Starting...");

try
{
    world.CreateRunner()
        .OnReady(() =>
        {
            Console.WriteLine("Input system initialized!");

            var input = world.GetExtension<IInputContext>();
            var graphics = world.GetExtension<IGraphicsContext>();

            // Report connected devices
            Console.WriteLine($"Keyboards: {input.Keyboards.Length}");
            Console.WriteLine($"Mice: {input.Mice.Length}");
            Console.WriteLine($"Gamepads: {input.ConnectedGamepadCount}");

            // Set up event-based input handlers
            SetupInputEvents(input);

            // Create the scene
            CreateScene(world, graphics);

            Console.WriteLine();
            Console.WriteLine("Scene created! Try the controls:");
            Console.WriteLine("  WASD / Left Stick  - Move");
            Console.WriteLine("  Space / South      - Jump (temporary boost)");
            Console.WriteLine("  Left Click         - Capture mouse");
            Console.WriteLine("  Mouse Move         - Look around (when captured)");
            Console.WriteLine("  Escape             - Exit");
        })
        .Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("This sample requires a display.");
}

Console.WriteLine("Sample complete!");

// Event-based input setup (discrete actions)
static void SetupInputEvents(IInputContext input)
{
    var keyboard = input.Keyboard;
    var mouse = input.Mouse;

    // Keyboard events
    keyboard.OnKeyDown += args =>
    {
        if (args.Key == Key.Escape)
        {
            Console.WriteLine("Escape pressed - closing...");
            // Note: Calling Environment.Exit() bypasses proper cleanup.
            // In a real application, use window.Close() instead.
            Environment.Exit(0);
        }

        if (!args.IsRepeat)
        {
            Console.WriteLine($"Key pressed: {args.Key}");
        }
    };

    // Mouse button events
    mouse.OnButtonDown += args =>
    {
        Console.WriteLine($"Mouse button: {args.Button} at ({args.Position.X:F0}, {args.Position.Y:F0})");

        // Toggle mouse capture on left click
        if (args.Button == MouseButton.Left)
        {
            mouse.IsCursorCaptured = !mouse.IsCursorCaptured;
            mouse.IsCursorVisible = !mouse.IsCursorCaptured;
            Console.WriteLine($"Mouse capture: {mouse.IsCursorCaptured}");
        }
    };

    // Mouse scroll events
    mouse.OnScroll += args =>
    {
        Console.WriteLine($"Scroll: {args.DeltaY:F2}");
    };

    // Gamepad connection events
    input.OnGamepadConnected += gp =>
    {
        Console.WriteLine($"Gamepad connected: {gp.Name} (index {gp.Index})");
        gp.OnButtonDown += args =>
        {
            Console.WriteLine($"Gamepad button: {args.Button}");
        };
    };

    input.OnGamepadDisconnected += gp =>
    {
        Console.WriteLine($"Gamepad disconnected: {gp.Name}");
    };

    // Check for already-connected gamepads (avoid throwing if none connected)
    foreach (var gp in input.Gamepads)
    {
        if (gp.IsConnected)
        {
            Console.WriteLine($"Gamepad already connected: {gp.Name}");
            gp.OnButtonDown += args =>
            {
                Console.WriteLine($"Gamepad button: {args.Button}");
            };
        }
    }
}

static void CreateScene(World world, IGraphicsContext graphics)
{
    var cubeMesh = graphics.CreateCube(1f);
    var groundMesh = graphics.CreateQuad(20f, 20f);

    // Camera
    var cameraPos = new Vector3(0, 5, 10);
    var lookAt = Vector3.Zero;
    var dir = Vector3.Normalize(lookAt - cameraPos);
    var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dir));
    var up = Vector3.Cross(dir, right);

    world.Spawn()
        .With(new Transform3D(
            cameraPos,
            Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                -dir.X, -dir.Y, -dir.Z, 0,
                0, 0, 0, 1)),
            Vector3.One))
        .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
        .WithTag<MainCameraTag>()
        .Build();

    // Light
    world.Spawn()
        .With(new Transform3D(
            Vector3.Zero,
            Quaternion.CreateFromYawPitchRoll(0.5f, -0.8f, 0),
            Vector3.One))
        .With(Light.Directional(new Vector3(1f, 0.95f, 0.8f), 1.0f))
        .Build();

    // Ground
    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, -0.5f, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(groundMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            BaseColorTextureId = graphics.WhiteTexture.Id,
            BaseColorFactor = new Vector4(0.3f, 0.4f, 0.3f, 1f),
            MetallicFactor = 0f,
            RoughnessFactor = 0.9f
        })
        .Build();

    // Player cube (controllable)
    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, 0.5f, 0),
            Quaternion.Identity,
            Vector3.One))
        .With(new Renderable(cubeMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            BaseColorTextureId = graphics.WhiteTexture.Id,
            BaseColorFactor = new Vector4(0.2f, 0.6f, 1f, 1f),
            MetallicFactor = 0.3f,
            RoughnessFactor = 0.5f
        })
        .WithTag<PlayerTag>()
        .With(new PlayerVelocity())
        .Build();
}
