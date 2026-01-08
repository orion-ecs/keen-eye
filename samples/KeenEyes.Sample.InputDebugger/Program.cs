// KeenEyes Input Debugger Sample
// Visualizes keyboard, mouse, and gamepad input for debugging and testing.
//
// Features:
//   - Console logging of all input events
//   - Mouse position marker (cube following cursor)
//   - Click markers (cubes spawned on click that fade out)
//   - Keyboard visualization (WASD keys light up when pressed)
//   - Gamepad stick position markers
//   - Gamepad button indicators
//
// TestBridge Integration:
//   - Installs TestBridgePlugin for MCP tool integration
//   - Starts IPC server on pipe: KeenEyes.InputDebugger.TestBridge
//   - Both real hardware input AND virtual MCP input work together (hybrid mode)
//
// MCP Testing:
//   1. Run this sample
//   2. Connect via MCP: mcp__keeneyes-bridge__game_connect pipeName="KeenEyes.InputDebugger.TestBridge"
//   3. Inject input: mcp__keeneyes-bridge__input_key_press key="W"
//   4. See both console output and visual feedback
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
using KeenEyes.Sample.InputDebugger;
using KeenEyes.TestBridge;
using KeenEyes.TestBridge.Ipc;

Console.WriteLine("KeenEyes Input Debugger");
Console.WriteLine("=======================");
Console.WriteLine();
Console.WriteLine("This sample visualizes all input for debugging:");
Console.WriteLine("- Keyboard: WASD keys shown, all keys logged to console");
Console.WriteLine("- Mouse: Position marker, click markers spawn on click");
Console.WriteLine("- Gamepad: Stick position markers, button indicators");
Console.WriteLine();
Console.WriteLine("TestBridge Integration:");
Console.WriteLine("- IPC Pipe: KeenEyes.InputDebugger.TestBridge");
Console.WriteLine("- Hybrid mode: Real AND virtual input both work");
Console.WriteLine();
Console.WriteLine("Press Escape to exit.");
Console.WriteLine();

// Configure window
var windowConfig = new WindowConfig
{
    Title = "KeenEyes Input Debugger - Escape to exit",
    Width = 1280,
    Height = 720,
    VSync = true
};

// Configure graphics
var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.15f, 0.15f, 0.2f, 1f),
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

// Install core plugins
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));

// Install TestBridge for MCP integration
// This enables external tools to inject input via IPC
var testBridgeOptions = new TestBridgeOptions
{
    EnableIpc = true,
    IpcOptions = new IpcOptions
    {
        PipeName = "KeenEyes.InputDebugger.TestBridge",
        TransportMode = IpcTransportMode.NamedPipe
    }
};
world.InstallPlugin(new TestBridgePlugin(testBridgeOptions));

// Register systems (use fully-qualified names to avoid ambiguity with KeenEyes.Graphics systems)
world.AddSystem<KeenEyes.Graphics.CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
world.AddSystem<InputLogSystem>(SystemPhase.Update, order: 0);
world.AddSystem<MouseVisualizerSystem>(SystemPhase.Update, order: 10);
world.AddSystem<KeyboardVisualizerSystem>(SystemPhase.Update, order: 20);
world.AddSystem<GamepadVisualizerSystem>(SystemPhase.Update, order: 30);
world.AddSystem<FadeOutSystem>(SystemPhase.Update, order: 100);
world.AddSystem<KeenEyes.Graphics.RenderSystem>(SystemPhase.Render, order: 0);

// IPC server reference for cleanup
IpcBridgeServer? ipcServer = null;

Console.WriteLine("Starting...");

try
{
    world.CreateRunner()
        .OnReady(() =>
        {
            Console.WriteLine("Input system initialized!");

            var input = world.GetExtension<IInputContext>();
            var graphics = world.GetExtension<IGraphicsContext>();
            var bridge = world.GetExtension<InProcessBridge>();

            Console.WriteLine($"Keyboards: {input.Keyboards.Length}");
            Console.WriteLine($"Mice: {input.Mice.Length}");
            Console.WriteLine($"Gamepads: {input.ConnectedGamepadCount}");

            // Set up escape handler
            SetupInputEvents(input);

            // Create the debug scene (must be on render thread)
            CreateDebugScene(world, graphics);

            // Start IPC server for external connections (fire and forget - runs on background thread)
            ipcServer = new IpcBridgeServer(bridge, testBridgeOptions.IpcOptions);
            _ = ipcServer.StartAsync().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    Console.WriteLine($"IPC server started on pipe: {testBridgeOptions.IpcOptions.PipeName}");
                }
                else if (t.IsFaulted)
                {
                    Console.WriteLine($"IPC server failed to start: {t.Exception?.InnerException?.Message}");
                }
            });

            Console.WriteLine();
            Console.WriteLine("Ready! Try:");
            Console.WriteLine("  - Press WASD to see key visualization");
            Console.WriteLine("  - Click mouse to spawn markers");
            Console.WriteLine("  - Connect gamepad to see stick markers");
            Console.WriteLine("  - Use MCP tools to inject virtual input");
            Console.WriteLine();
        })
        .Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("This sample requires a display.");
}
finally
{
    // Clean up IPC server
    if (ipcServer is not null)
    {
        ipcServer.StopAsync().GetAwaiter().GetResult();
        ipcServer.Dispose();
    }
}

Console.WriteLine("Sample complete!");

void SetupInputEvents(IInputContext input)
{
    var keyboard = input.Keyboard;

    keyboard.OnKeyDown += args =>
    {
        if (args.Key == Key.Escape)
        {
            Console.WriteLine("Escape pressed - closing...");
            Environment.Exit(0);
        }
    };

    input.OnGamepadConnected += gp =>
    {
        Console.WriteLine($"Gamepad connected: {gp.Name} (index {gp.Index})");
    };

    input.OnGamepadDisconnected += gp =>
    {
        Console.WriteLine($"Gamepad disconnected: {gp.Name}");
    };
}

void CreateDebugScene(World world, IGraphicsContext graphics)
{
    // Create meshes
    var groundMesh = graphics.CreateQuad(30f, 20f);
    var cubeMesh = graphics.CreateCube(0.8f);
    var smallCubeMesh = graphics.CreateCube(0.4f);
    var markerMesh = graphics.CreateCube(0.5f);
    var stickMarkerMesh = graphics.CreateCube(0.6f);

    // Camera - top-down view
    var cameraPos = new Vector3(0, 15, 8);
    var lookAt = new Vector3(0, 0, 0);
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
            Quaternion.CreateFromYawPitchRoll(0.3f, -0.7f, 0),
            Vector3.One))
        .With(Light.Directional(new Vector3(1f, 0.98f, 0.9f), 1.2f))
        .Build();

    // Ground
    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, -0.01f, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(groundMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.2f, 0.25f, 0.2f, 1f),
            Metallic = 0f,
            Roughness = 0.95f
        })
        .Build();

    // Mouse position marker
    world.Spawn()
        .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
        .With(new Renderable(markerMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(1f, 1f, 0f, 1f),
            Metallic = 0.5f,
            Roughness = 0.3f
        })
        .WithTag<MouseMarkerTag>()
        .Build();

    // Keyboard visualizers - WASD layout
    CreateKeyVisualizer(world, graphics, smallCubeMesh, Key.W, new Vector3(0, 0.2f, -3f));
    CreateKeyVisualizer(world, graphics, smallCubeMesh, Key.A, new Vector3(-0.5f, 0.2f, -2.5f));
    CreateKeyVisualizer(world, graphics, smallCubeMesh, Key.S, new Vector3(0, 0.2f, -2.5f));
    CreateKeyVisualizer(world, graphics, smallCubeMesh, Key.D, new Vector3(0.5f, 0.2f, -2.5f));

    // Additional common keys
    CreateKeyVisualizer(world, graphics, smallCubeMesh, Key.Space, new Vector3(0, 0.2f, -2f));
    CreateKeyVisualizer(world, graphics, smallCubeMesh, Key.LeftShift, new Vector3(-1f, 0.2f, -2f));
    CreateKeyVisualizer(world, graphics, smallCubeMesh, Key.LeftControl, new Vector3(-1.5f, 0.2f, -2f));

    // Label cube for keyboard section
    world.Spawn()
        .With(new Transform3D(new Vector3(0, 0.01f, -3.6f), Quaternion.Identity, new Vector3(2f, 0.05f, 0.3f)))
        .With(new Renderable(cubeMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.5f, 0.5f, 0.5f, 1f),
            Metallic = 0f,
            Roughness = 0.9f
        })
        .Build();

    // Gamepad stick markers
    // Left stick base
    world.Spawn()
        .With(new Transform3D(new Vector3(-6f, 0.05f, 3f), Quaternion.Identity, new Vector3(4.5f, 0.1f, 4.5f)))
        .With(new Renderable(cubeMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.25f, 0.25f, 0.3f, 1f),
            Metallic = 0f,
            Roughness = 0.9f
        })
        .Build();

    // Left stick marker
    world.Spawn()
        .With(new Transform3D(new Vector3(-6f, 0.5f, 3f), Quaternion.Identity, Vector3.One))
        .With(new Renderable(stickMarkerMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.3f, 0.7f, 1f, 1f),
            Metallic = 0.6f,
            Roughness = 0.3f
        })
        .WithTag<LeftStickMarkerTag>()
        .Build();

    // Right stick base
    world.Spawn()
        .With(new Transform3D(new Vector3(-3f, 0.05f, 3f), Quaternion.Identity, new Vector3(4.5f, 0.1f, 4.5f)))
        .With(new Renderable(cubeMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.25f, 0.25f, 0.3f, 1f),
            Metallic = 0f,
            Roughness = 0.9f
        })
        .Build();

    // Right stick marker
    world.Spawn()
        .With(new Transform3D(new Vector3(-3f, 0.5f, 3f), Quaternion.Identity, Vector3.One))
        .With(new Renderable(stickMarkerMesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(1f, 0.5f, 0.3f, 1f),
            Metallic = 0.6f,
            Roughness = 0.3f
        })
        .WithTag<RightStickMarkerTag>()
        .Build();

    // Gamepad button indicators
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.South, new Vector3(6f, 0.2f, 3f));
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.East, new Vector3(6.5f, 0.2f, 3.5f));
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.West, new Vector3(5.5f, 0.2f, 3.5f));
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.North, new Vector3(6f, 0.2f, 4f));

    // D-pad indicators
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.DPadUp, new Vector3(4f, 0.2f, 4f));
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.DPadDown, new Vector3(4f, 0.2f, 3f));
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.DPadLeft, new Vector3(3.5f, 0.2f, 3.5f));
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.DPadRight, new Vector3(4.5f, 0.2f, 3.5f));

    // Shoulder buttons
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.LeftShoulder, new Vector3(3f, 0.2f, 5f));
    CreateGamepadButtonVisualizer(world, graphics, smallCubeMesh, GamepadButton.RightShoulder, new Vector3(7f, 0.2f, 5f));
}

void CreateKeyVisualizer(World world, IGraphicsContext graphics, MeshHandle mesh, Key key, Vector3 position)
{
    world.Spawn()
        .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
        .With(new Renderable(mesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.4f, 0.4f, 0.4f, 1f),
            Metallic = 0.2f,
            Roughness = 0.6f
        })
        .WithTag<KeyVisualizerTag>()
        .With(new KeyBinding { Key = key })
        .Build();
}

void CreateGamepadButtonVisualizer(World world, IGraphicsContext graphics, MeshHandle mesh, GamepadButton button, Vector3 position)
{
    world.Spawn()
        .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
        .With(new Renderable(mesh.Id, 0))
        .With(new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = new Vector4(0.3f, 0.3f, 0.3f, 1f),
            Metallic = 0.2f,
            Roughness = 0.6f
        })
        .WithTag<GamepadButtonTag>()
        .With(new GamepadButtonBinding { Button = button })
        .Build();
}
