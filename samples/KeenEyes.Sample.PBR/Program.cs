// KeenEyes PBR Sample
// Demonstrates Physically Based Rendering with the Cook-Torrance BRDF shader.
//
// This sample demonstrates:
// - PBR metallic-roughness workflow
// - Multiple light sources (directional, point, spot)
// - Interactive orbit camera controls
// - Material variations showing different metallic/roughness combinations
//
// Controls:
//   Left Mouse Drag  - Rotate camera around target
//   Right Mouse Drag - Rotate camera around target
//   Scroll Wheel     - Zoom in/out
//   Escape           - Exit
//
// NOTE: This sample requires a display to run. It will not work in headless environments.

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
using KeenEyes.Sample.PBR;

Console.WriteLine("KeenEyes PBR Sample");
Console.WriteLine("===================");
Console.WriteLine();
Console.WriteLine("This sample demonstrates Physically Based Rendering (PBR):");
Console.WriteLine("- Cook-Torrance BRDF with GGX distribution");
Console.WriteLine("- Metallic-roughness workflow");
Console.WriteLine("- Multiple light types (directional, point, spot)");
Console.WriteLine("- Interactive orbit camera");
Console.WriteLine();
Console.WriteLine("Controls:");
Console.WriteLine("  Left/Right Mouse Drag - Rotate camera");
Console.WriteLine("  Scroll Wheel          - Zoom in/out");
Console.WriteLine("  Escape                - Exit");
Console.WriteLine();

// Configure window
var windowConfig = new WindowConfig
{
    Title = "KeenEyes PBR Sample - Drag to rotate, scroll to zoom",
    Width = 1280,
    Height = 720,
    VSync = true
};

// Configure graphics
var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.05f, 0.05f, 0.1f, 1f), // Dark blue-ish background
    EnableDepthTest = true,
    EnableCulling = true
};

// Configure input
var inputConfig = new SilkInputConfig
{
    EnableGamepads = false,
    CaptureMouseOnClick = false
};

using var world = new World();

// Install plugins
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));

// Register graphics systems
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);

// Register custom systems
world.AddSystem<OrbitCameraSystem>(SystemPhase.Update, order: 0);
world.AddSystem<CameraZoomSystem>(SystemPhase.Update, order: 1);
world.AddSystem<AutoRotateSystem>(SystemPhase.Update, order: 2);
world.AddSystem<OrbitingLightSystem>(SystemPhase.Update, order: 3);

Console.WriteLine("Starting graphics...");

try
{
    world.CreateRunner()
        .OnReady(() =>
        {
            Console.WriteLine("Graphics initialized, creating PBR scene...");

            var input = world.GetExtension<IInputContext>();
            var graphics = world.GetExtension<IGraphicsContext>();

            // Set up exit handler
            input.Keyboard.OnKeyDown += args =>
            {
                if (args.Key == Key.Escape)
                {
                    Environment.Exit(0);
                }
            };

            // Create the PBR demo scene
            CreatePbrScene(world, graphics);

            Console.WriteLine("Scene created!");
            Console.WriteLine();
            Console.WriteLine("Material Grid:");
            Console.WriteLine("  Rows: Metallic (0.0 bottom to 1.0 top)");
            Console.WriteLine("  Columns: Roughness (0.0 left to 1.0 right)");
        })
        .OnResize((width, height) =>
        {
            Console.WriteLine($"Window resized to {width}x{height}");
        })
        .Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("This sample requires a display.");
}

Console.WriteLine("Sample complete!");

// Create the PBR demonstration scene
static void CreatePbrScene(World world, IGraphicsContext graphics)
{
    // Create mesh resources
    var cubeMesh = graphics.CreateCube(1f);
    var groundMesh = graphics.CreateQuad(30f, 30f);

    // Create orbit camera
    var orbitCamera = OrbitCamera.Default with
    {
        Distance = 12f,
        Yaw = MathF.PI / 4f,
        Pitch = 0.4f,
        Target = new Vector3(0, 1.5f, 0),
        MinDistance = 3f,
        MaxDistance = 30f
    };

    // Calculate initial camera position for orbit camera
    var cosP = MathF.Cos(orbitCamera.Pitch);
    var sinP = MathF.Sin(orbitCamera.Pitch);
    var cosY = MathF.Cos(orbitCamera.Yaw);
    var sinY = MathF.Sin(orbitCamera.Yaw);
    var offset = new Vector3(cosP * sinY * orbitCamera.Distance, sinP * orbitCamera.Distance, cosP * cosY * orbitCamera.Distance);
    var cameraPos = orbitCamera.Target + offset;
    var forward = Vector3.Normalize(orbitCamera.Target - cameraPos);
    var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
    var up = Vector3.Cross(forward, right);

    world.Spawn()
        .With(new Transform3D(
            cameraPos,
            Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                -forward.X, -forward.Y, -forward.Z, 0,
                0, 0, 0, 1)),
            Vector3.One))
        .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
        .With(orbitCamera)
        .WithTag<MainCameraTag>()
        .Build();

    // Create directional light (sun)
    world.Spawn()
        .With(new Transform3D(
            Vector3.Zero,
            Quaternion.CreateFromYawPitchRoll(0.8f, -0.6f, 0),
            Vector3.One))
        .With(Light.Directional(new Vector3(1f, 0.95f, 0.9f), 0.8f))
        .Build();

    // Create orbiting point light (warm)
    world.Spawn()
        .With(new Transform3D(
            new Vector3(5f, 3f, 0),
            Quaternion.Identity,
            Vector3.One))
        .With(Light.Point(new Vector3(1f, 0.6f, 0.2f), 1.5f, 15f))
        .With(new OrbitingLight
        {
            Center = new Vector3(0, 0, 0),
            Radius = 5f,
            Height = 3f,
            Angle = 0f,
            Speed = 0.5f
        })
        .Build();

    // Create second orbiting point light (cool, opposite direction)
    world.Spawn()
        .With(new Transform3D(
            new Vector3(-5f, 3f, 0),
            Quaternion.Identity,
            Vector3.One))
        .With(Light.Point(new Vector3(0.2f, 0.5f, 1f), 1.2f, 15f))
        .With(new OrbitingLight
        {
            Center = new Vector3(0, 0, 0),
            Radius = 5f,
            Height = 4f,
            Angle = MathF.PI,
            Speed = -0.3f
        })
        .Build();

    // Create spot light pointing down at center
    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, 8f, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f), // Point down
            Vector3.One))
        .With(Light.Spot(new Vector3(1f, 1f, 1f), 0.6f, 20f, 15f, 30f))
        .Build();

    // Create ground plane
    var groundMaterial = new Material
    {
        ShaderId = graphics.PbrShader.Id,
        BaseColorTextureId = graphics.WhiteTexture.Id,
        BaseColorFactor = new Vector4(0.15f, 0.15f, 0.17f, 1f), // Dark gray
        MetallicFactor = 0f,
        RoughnessFactor = 0.9f,
        NormalScale = 1f,
        OcclusionStrength = 1f,
        AlphaCutoff = 0.5f,
        AlphaMode = AlphaMode.Opaque
    };

    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, 0, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Vector3.One))
        .With(new Renderable(groundMesh.Id, 0))
        .With(groundMaterial)
        .WithTag<GroundTag>()
        .Build();

    // Create material variation grid (5x5)
    // X axis: Roughness from 0 to 1
    // Z axis: Metallic from 0 to 1
    var gridSize = 5;
    var spacing = 1.8f;
    var startX = -(gridSize - 1) * spacing / 2f;
    var startZ = -(gridSize - 1) * spacing / 2f;

    // Base colors for the spheres
    var baseColors = new[]
    {
        new Vector4(0.9f, 0.2f, 0.2f, 1f),  // Red
        new Vector4(0.2f, 0.8f, 0.3f, 1f),  // Green
        new Vector4(0.2f, 0.4f, 0.9f, 1f),  // Blue
        new Vector4(0.95f, 0.8f, 0.1f, 1f), // Gold
        new Vector4(0.85f, 0.85f, 0.9f, 1f) // Silver/White
    };

    for (var row = 0; row < gridSize; row++)
    {
        for (var col = 0; col < gridSize; col++)
        {
            var x = startX + col * spacing;
            var z = startZ + row * spacing;
            var metallic = row / (float)(gridSize - 1);
            var roughness = col / (float)(gridSize - 1);

            // Use different base color per row
            var baseColor = baseColors[row];

            // For metallic materials (metallic > 0.5), use a more neutral base
            // as the F0 reflectance will dominate
            if (metallic > 0.5f)
            {
                baseColor = Vector4.Lerp(baseColor, new Vector4(0.95f, 0.93f, 0.88f, 1f), (metallic - 0.5f) * 2f);
            }

            var material = new Material
            {
                ShaderId = graphics.PbrShader.Id,
                BaseColorTextureId = graphics.WhiteTexture.Id,
                BaseColorFactor = baseColor,
                MetallicFactor = metallic,
                RoughnessFactor = Math.Max(0.05f, roughness), // Clamp to avoid perfectly smooth (artifacts)
                NormalScale = 1f,
                OcclusionStrength = 1f,
                AlphaCutoff = 0.5f,
                AlphaMode = AlphaMode.Opaque
            };

            world.Spawn()
                .With(new Transform3D(
                    new Vector3(x, 0.7f, z),
                    Quaternion.Identity,
                    Vector3.One * 0.8f))
                .With(new Renderable(cubeMesh.Id, 0))
                .With(material)
                .Build();
        }
    }

    // Create a larger center piece with auto-rotate
    var centerMaterial = new Material
    {
        ShaderId = graphics.PbrShader.Id,
        BaseColorTextureId = graphics.WhiteTexture.Id,
        BaseColorFactor = new Vector4(1f, 0.85f, 0.6f, 1f), // Gold-ish
        MetallicFactor = 0.9f,
        RoughnessFactor = 0.1f,
        NormalScale = 1f,
        OcclusionStrength = 1f,
        EmissiveFactor = new Vector3(0.1f, 0.05f, 0f), // Slight emissive glow
        AlphaCutoff = 0.5f,
        AlphaMode = AlphaMode.Opaque
    };

    world.Spawn()
        .With(new Transform3D(
            new Vector3(0, 3f, 0),
            Quaternion.Identity,
            new Vector3(1.5f, 1.5f, 1.5f)))
        .With(new Renderable(cubeMesh.Id, 0))
        .With(centerMaterial)
        .With(new AutoRotate { Speed = 0.3f })
        .WithTag<MainModelTag>()
        .Build();

    Console.WriteLine($"Created {gridSize * gridSize} material samples + 1 center piece");
    Console.WriteLine($"Lights: 1 directional + 2 orbiting point + 1 spot = 4 total");
}
