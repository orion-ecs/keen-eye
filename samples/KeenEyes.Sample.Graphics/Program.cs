// KeenEyes Graphics Sample
// Demonstrates the Graphics plugin with 3D rendering, cameras, lights, and materials.
//
// NOTE: This sample requires a display to run. It will not work in headless environments.

using System.Numerics;
using KeenEyes;
using KeenEyes.Graphics;
using KeenEyes.Sample.Graphics;
using KeenEyes.Common;

Console.WriteLine("KeenEyes Graphics Sample");
Console.WriteLine("========================");
Console.WriteLine();
Console.WriteLine("This sample demonstrates:");
Console.WriteLine("- Setting up a 3D scene with the Graphics plugin");
Console.WriteLine("- Creating cameras, lights, and renderable objects");
Console.WriteLine("- Animating objects with custom systems");
Console.WriteLine();

// Configure the graphics window
var config = new GraphicsConfig
{
    WindowWidth = 1280,
    WindowHeight = 720,
    WindowTitle = "KeenEyes Graphics Sample",
    VSync = true,
    ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f) // Sky blue background
};

// Create the world and install graphics plugin
using var world = new World();
world.InstallPlugin(new GraphicsPlugin(config));

// Get the graphics context for creating resources
var graphics = world.GetExtension<GraphicsContext>();

// Register custom systems for animation
world.AddSystem<SpinSystem>(SystemPhase.Update, order: 0);
world.AddSystem<BobSystem>(SystemPhase.Update, order: 1);

// Track initialization state
bool sceneReady = false;
int cubeMesh = 0;
int quadMesh = 0;

// Set up scene when graphics are ready
graphics.OnLoad += () =>
{
    Console.WriteLine("Graphics initialized, creating scene...");

    // Create mesh resources
    cubeMesh = graphics.CreateCube(1f);
    quadMesh = graphics.CreateQuad(20f, 20f);

    // Enable depth testing and backface culling
    graphics.EnableDepthTest();
    graphics.EnableCulling();

    // Create the scene
    CreateScene(world, graphics, cubeMesh, quadMesh);

    sceneReady = true;
    Console.WriteLine("Scene created! Use mouse/keyboard to interact (if implemented).");
    Console.WriteLine("Close the window to exit.");
};

// Handle window resize
graphics.OnResize += (width, height) =>
{
    Console.WriteLine($"Window resized to {width}x{height}");

    // Update camera aspect ratio
    foreach (var entity in world.Query<Camera>())
    {
        ref var camera = ref world.Get<Camera>(entity);
        camera.AspectRatio = (float)width / height;
    }
};

// Handle window close
graphics.OnClosing += () =>
{
    Console.WriteLine("Window closing...");
};

// Handle render events - this is called each frame by Silk.NET
graphics.OnRender += (deltaTime) =>
{
    if (sceneReady)
    {
        // Update the world (runs all systems including render)
        world.Update((float)deltaTime);
    }
};

// Initialize and run the graphics (blocks until window closes)
Console.WriteLine("Starting graphics...");

try
{
    graphics.Initialize();
    graphics.Run(); // This blocks until the window is closed
}
catch (Exception ex)
{
    Console.WriteLine($"Error running graphics: {ex.Message}");
    Console.WriteLine("This sample requires a display. Make sure you're running in a graphical environment.");
}

Console.WriteLine("Sample complete!");

// Helper method to create the scene
static void CreateScene(World world, GraphicsContext graphics, int cubeMesh, int quadMesh)
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
        ShaderId = graphics.LitShaderId,
        TextureId = graphics.WhiteTextureId,
        Color = new Vector4(0.3f, 0.5f, 0.3f, 1f), // Green
        Metallic = 0f,
        Roughness = 0.9f
    };

    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, -0.5f, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(quadMesh, 0))
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
            ShaderId = graphics.LitShaderId,
            TextureId = graphics.WhiteTextureId,
            Color = colors[i],
            Metallic = 0.2f,
            Roughness = 0.5f
        };

        world.Spawn()
            .With(new Transform3D(
                new Vector3(x, y, z),
                Quaternion.Identity,
                Vector3.One))
            .With(new Renderable(cubeMesh, 0))
            .With(cubeMaterial)
            .With(new Spin { Speed = new Vector3(0.5f, 1f, 0.3f) })
            .Build();
    }

    // Create a bobbing cube in the center
    var centerMaterial = new Material
    {
        ShaderId = graphics.LitShaderId,
        TextureId = graphics.WhiteTextureId,
        Color = new Vector4(1f, 1f, 1f, 1f), // White
        Metallic = 0.8f,
        Roughness = 0.2f
    };

    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, 2f, 0),
            Quaternion.Identity,
            new Vector3(1.5f, 1.5f, 1.5f)))
        .With(new Renderable(cubeMesh, 0))
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
