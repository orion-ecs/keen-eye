// KeenEyes Graphics Sample
// Demonstrates the Graphics plugin with 3D rendering, cameras, lights, and materials.
//
// NOTE: This sample requires a display to run. It will not work in headless environments.

using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Runtime;
using KeenEyes.Sample.Graphics;

Console.WriteLine("KeenEyes Graphics Sample");
Console.WriteLine("========================");
Console.WriteLine();
Console.WriteLine("This sample demonstrates:");
Console.WriteLine("- Setting up a 3D scene with the Graphics plugin");
Console.WriteLine("- Creating cameras, lights, and renderable objects");
Console.WriteLine("- Animating objects with custom systems");
Console.WriteLine("- Using the WorldRunnerBuilder for clean main loop setup");
Console.WriteLine();

// Configure the graphics (includes window settings)
var graphicsConfig = new SilkGraphicsConfig
{
    WindowTitle = "KeenEyes Graphics Sample",
    WindowWidth = 1280,
    WindowHeight = 720,
    VSync = true,
    ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f), // Sky blue background
    EnableDepthTest = true,
    EnableCulling = true
};

// Create the world and install plugins
using var world = new World();

// Install graphics plugin (creates its own window)
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));

// Register graphics systems (from KeenEyes.Graphics)
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);

// Register custom systems for animation
world.AddSystem<SpinSystem>(SystemPhase.Update, order: 0);
world.AddSystem<BobSystem>(SystemPhase.Update, order: 1);

Console.WriteLine("Starting graphics...");

try
{
    // Use the backend-agnostic builder pattern to run the main loop
    world.CreateRunner()
        .OnReady(() =>
        {
            Console.WriteLine("Graphics initialized, creating scene...");

            // Get the graphics context for resource creation
            var graphics = world.GetExtension<IGraphicsContext>();

            // Create mesh resources
            var cubeMesh = graphics.CreateCube(1f);
            var quadMesh = graphics.CreateQuad(20f, 20f);

            // Create the scene
            CreateScene(world, graphics, cubeMesh, quadMesh);

            Console.WriteLine("Scene created! Use mouse/keyboard to interact (if implemented).");
            Console.WriteLine("Close the window to exit.");
        })
        .OnResize((width, height) =>
        {
            Console.WriteLine($"Window resized to {width}x{height}");
        })
        .OnClosing(() =>
        {
            Console.WriteLine("Window closing...");
        })
        .Run(); // Blocks until window closes, auto-calls world.Update() each frame
}
catch (Exception ex)
{
    Console.WriteLine($"Error running graphics: {ex.Message}");
    Console.WriteLine("This sample requires a display. Make sure you're running in a graphical environment.");
}

Console.WriteLine("Sample complete!");

// Helper method to create the scene
static void CreateScene(World world, IGraphicsContext graphics, MeshHandle cubeMesh, MeshHandle quadMesh)
{
    // Create a camera
    var cameraPosition = new Vector3(0, 5, 10);
    var cameraLookAt = Vector3.Zero;
    var cameraDirection = Vector3.Normalize(cameraLookAt - cameraPosition);

    // Calculate camera rotation to look at the center
    var forward = cameraDirection;
    var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
    var up = Vector3.Cross(forward, right);

    world.Spawn()
        .With(new Transform3D(
            cameraPosition,
            Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                -forward.X, -forward.Y, -forward.Z, 0,
                0, 0, 0, 1)),
            Vector3.One))
        .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
        .WithTag<MainCameraTag>()
        .Build();

    // Create a directional light (sun)
    world.Spawn()
        .With(new Transform3D(
            Vector3.Zero,
            Quaternion.CreateFromYawPitchRoll(0.5f, -0.8f, 0),
            Vector3.One))
        .With(Light.Directional(new Vector3(1f, 0.95f, 0.8f), 1.0f))
        .Build();

    // Create a ground plane
    var groundMaterial = new Material
    {
        ShaderId = graphics.LitShader.Id,
        TextureId = graphics.WhiteTexture.Id,
        Color = new Vector4(0.3f, 0.5f, 0.3f, 1f), // Green
        Metallic = 0f,
        Roughness = 0.9f
    };

    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, -0.5f, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(quadMesh.Id, 0))
        .With(groundMaterial)
        .WithTag<GroundTag>()
        .Build();

    // Create some spinning cubes
    var colors = new[]
    {
        new Vector4(1f, 0.3f, 0.3f, 1f),   // Red
        new Vector4(0.3f, 1f, 0.3f, 1f),   // Green
        new Vector4(0.3f, 0.3f, 1f, 1f),   // Blue
        new Vector4(1f, 1f, 0.3f, 1f),     // Yellow
        new Vector4(1f, 0.3f, 1f, 1f),     // Magenta
    };

    for (int i = 0; i < 5; i++)
    {
        var angle = i * MathF.PI * 2f / 5f;
        var radius = 3f;
        var x = MathF.Cos(angle) * radius;
        var z = MathF.Sin(angle) * radius;
        var y = 1f;

        var cubeMaterial = new Material
        {
            ShaderId = graphics.LitShader.Id,
            TextureId = graphics.WhiteTexture.Id,
            Color = colors[i],
            Metallic = 0.2f,
            Roughness = 0.5f
        };

        world.Spawn()
            .With(new Transform3D(
                new Vector3(x, y, z),
                Quaternion.Identity,
                Vector3.One))
            .With(new Renderable(cubeMesh.Id, 0))
            .With(cubeMaterial)
            .With(new Spin { Speed = new Vector3(0.5f, 1f, 0.3f) })
            .Build();
    }

    // Create a bobbing cube in the center
    var centerMaterial = new Material
    {
        ShaderId = graphics.LitShader.Id,
        TextureId = graphics.WhiteTexture.Id,
        Color = new Vector4(1f, 1f, 1f, 1f), // White
        Metallic = 0.8f,
        Roughness = 0.2f
    };

    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, 2f, 0),
            Quaternion.Identity,
            new Vector3(1.5f, 1.5f, 1.5f)))
        .With(new Renderable(cubeMesh.Id, 0))
        .With(centerMaterial)
        .With(new Spin { Speed = new Vector3(0f, 0.5f, 0f) })
        .With(new Bob { Amplitude = 0.5f, Frequency = 0.5f, Phase = 0, OriginY = 2f })
        .Build();

    // Create a point light near the center
    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, 4f, 0),
            Quaternion.Identity,
            Vector3.One))
        .With(Light.Point(new Vector3(1f, 0.8f, 0.6f), 0.5f, 15f))
        .Build();

    Console.WriteLine("Scene created successfully!");
}
