# Graphics

KeenEyes provides a modular graphics architecture with backend-agnostic abstractions and Silk.NET OpenGL implementation.

## Architecture Overview

The graphics system is split across multiple packages:

| Package | Purpose |
|---------|---------|
| `KeenEyes.Graphics.Abstractions` | Backend-agnostic interfaces and components |
| `KeenEyes.Graphics` | Backend-agnostic systems (CameraSystem, RenderSystem) |
| `KeenEyes.Graphics.Silk` | Silk.NET OpenGL implementation |
| `KeenEyes.Runtime` | Main loop builder (WorldRunnerBuilder) |

This separation allows:
- Swapping rendering backends without changing game code
- Writing backend-agnostic plugins and systems
- Testing graphics logic without GPU dependencies

## Quick Start

```csharp
using KeenEyes;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Runtime;

// Configure the graphics backend
var config = new SilkGraphicsConfig
{
    WindowTitle = "My Game",
    WindowWidth = 1280,
    WindowHeight = 720,
    VSync = true,
    ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f)
};

// Create world and install plugin
using var world = new World();
world.InstallPlugin(new SilkGraphicsPlugin(config));

// Add backend-agnostic systems
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate);
world.AddSystem<RenderSystem>(SystemPhase.Render);

// Run with the builder pattern (recommended)
world.CreateRunner()
    .OnReady(() => CreateScene(world))
    .Run();  // Blocks until window closes, auto-calls world.Update()
```

## Main Loop Setup

### Recommended: WorldRunnerBuilder

The `WorldRunnerBuilder` provides a clean, backend-agnostic way to run your game:

```csharp
world.CreateRunner()
    .OnReady(() =>
    {
        // Called once when graphics are ready
        var graphics = world.GetExtension<IGraphicsContext>();
        CreateScene(world, graphics);
    })
    .OnResize((width, height) =>
    {
        Console.WriteLine($"Window resized to {width}x{height}");
    })
    .OnClosing(() =>
    {
        Console.WriteLine("Goodbye!");
    })
    .Run();  // Blocks until closed
```

**Key features:**
- **Auto-update**: `world.Update(dt)` is called automatically each frame
- **Backend-agnostic**: Works with any `ILoopProvider` implementation
- **Clean syntax**: No manual event wiring

### Explicit Update Control

For custom update logic, provide an `OnUpdate` callback:

```csharp
world.CreateRunner()
    .OnReady(() => CreateScene(world))
    .OnUpdate((dt) =>
    {
        // Custom logic before update
        HandleInput();

        // Manually call world update
        world.Update(dt);

        // Custom logic after update
        UpdateUI();
    })
    .Run();
```

### Alternative: Direct Event Wiring

For advanced scenarios, you can wire events directly on `IGraphicsContext`:

```csharp
var graphics = world.GetExtension<IGraphicsContext>();

graphics.OnReady += () => CreateScene(world, graphics);
graphics.OnUpdate += (dt) => world.Update(dt);
graphics.OnResize += (w, h) => Console.WriteLine($"Resized: {w}x{h}");
graphics.OnClosing += () => Console.WriteLine("Closing...");

graphics.Initialize();
graphics.Run();  // Blocks until closed
```

## Configuration

```csharp
var config = new SilkGraphicsConfig
{
    WindowWidth = 1920,          // Initial window width
    WindowHeight = 1080,         // Initial window height
    WindowTitle = "My Game",     // Window title
    VSync = true,                // Enable vertical sync
    EnableDepthTest = true,      // Enable depth testing
    EnableCulling = true,        // Enable backface culling
    ClearColor = new Vector4(0.1f, 0.1f, 0.1f, 1f)  // Background color
};
```

## Components

All graphics components are in `KeenEyes.Graphics.Abstractions` for backend independence.

### Camera

Defines a viewpoint for rendering:

```csharp
// Perspective camera (3D with depth)
world.Spawn()
    .With(new Transform3D(new Vector3(0, 5, 10), Quaternion.Identity, Vector3.One))
    .With(Camera.CreatePerspective(
        fieldOfView: 60f,      // Vertical FOV in degrees
        aspectRatio: 16f/9f,   // Width / Height
        nearPlane: 0.1f,       // Near clip distance
        farPlane: 1000f))      // Far clip distance
    .WithTag<MainCameraTag>()
    .Build();

// Orthographic camera (2D-like, no perspective)
world.Spawn()
    .With(Transform3D.Identity)
    .With(Camera.CreateOrthographic(
        size: 5f,              // Half-height of view
        aspectRatio: 16f/9f,
        nearPlane: 0.1f,
        farPlane: 100f))
    .Build();
```

Camera properties:
- `Priority` - Higher values render later (on top)
- `Viewport` - Normalized screen region (x, y, width, height)
- `ClearColor` - Background color for this camera
- `ClearColorBuffer` / `ClearDepthBuffer` - What to clear before rendering

### Renderable

Marks an entity for rendering:

```csharp
var graphics = world.GetExtension<IGraphicsContext>();
var meshId = graphics.CreateCube();

world.Spawn()
    .With(Transform3D.Identity)
    .With(new Renderable(meshId.Id, layer: 0))
    .With(new Material
    {
        ShaderId = graphics.LitShader.Id,
        TextureId = graphics.WhiteTexture.Id,
        Color = new Vector4(1, 0, 0, 1)  // Red
    })
    .Build();
```

### Material

Defines surface appearance:

```csharp
var material = new Material
{
    ShaderId = graphics.LitShader.Id,
    TextureId = graphics.WhiteTexture.Id,
    Color = new Vector4(1, 1, 1, 1),    // White
    Metallic = 0.8f,                     // Metallic look
    Roughness = 0.2f                     // Smooth surface
};
```

### Light

Illuminates the scene:

```csharp
// Directional light (sun)
world.Spawn()
    .With(new Transform3D(Vector3.Zero,
        Quaternion.CreateFromYawPitchRoll(0.5f, -0.8f, 0), Vector3.One))
    .With(Light.Directional(
        color: new Vector3(1, 0.95f, 0.8f),
        intensity: 1.0f))
    .Build();

// Point light (bulb)
world.Spawn()
    .With(new Transform3D(new Vector3(5, 3, 0), Quaternion.Identity, Vector3.One))
    .With(Light.Point(
        color: new Vector3(1, 0.8f, 0.6f),
        intensity: 0.5f,
        range: 15f))
    .Build();

// Spot light (flashlight)
world.Spawn()
    .With(new Transform3D(position, rotation, Vector3.One))
    .With(Light.Spot(
        color: new Vector3(1, 1, 1),
        intensity: 2.0f,
        range: 15f,
        innerAngle: 15f,
        outerAngle: 30f))
    .Build();
```

## Resource Management

Access resources through `IGraphicsContext`:

```csharp
var graphics = world.GetExtension<IGraphicsContext>();
```

### Meshes

```csharp
// Built-in primitives
var quad = graphics.CreateQuad(width: 1f, height: 1f);
var cube = graphics.CreateCube(size: 1f);

// Delete when done
graphics.DeleteMesh(quad);
```

### Textures

```csharp
// From raw RGBA data
var texture = graphics.CreateTexture(256, 256, pixelData);

// Built-in white texture
var white = graphics.WhiteTexture;

// Delete when done
graphics.DeleteTexture(texture);
```

### Shaders

```csharp
// Built-in shaders
var lit = graphics.LitShader;      // PBR lighting
var unlit = graphics.UnlitShader;  // No lighting
var solid = graphics.SolidShader;  // Solid color

// Custom GLSL shader
var custom = graphics.CreateShader(vertexSource, fragmentSource);
graphics.DeleteShader(custom);
```

## Systems

The following systems should be added to your world:

```csharp
// Camera system - updates camera matrices based on window size
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);

// Render system - draws all renderables
world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);
```

These systems are in `KeenEyes.Graphics` and work with any backend implementing `IGraphicsContext`.

## Complete Example

```csharp
using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Runtime;

var config = new SilkGraphicsConfig
{
    WindowTitle = "Spinning Cube",
    WindowWidth = 1280,
    WindowHeight = 720,
    VSync = true,
    ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f),
    EnableDepthTest = true,
    EnableCulling = true
};

using var world = new World();
world.InstallPlugin(new SilkGraphicsPlugin(config));

// Add systems
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate);
world.AddSystem<RenderSystem>(SystemPhase.Render);
world.AddSystem<SpinSystem>(SystemPhase.Update);  // Custom system

// Run with builder pattern
world.CreateRunner()
    .OnReady(() =>
    {
        var graphics = world.GetExtension<IGraphicsContext>();

        // Create camera
        world.Spawn()
            .With(new Transform3D(new Vector3(0, 2, 5), Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f/9f, 0.1f, 100f))
            .WithTag<MainCameraTag>()
            .Build();

        // Create light
        world.Spawn()
            .With(new Transform3D(Vector3.Zero,
                Quaternion.CreateFromYawPitchRoll(0.5f, -0.5f, 0), Vector3.One))
            .With(Light.Directional(new Vector3(1, 1, 1), 1f))
            .Build();

        // Create spinning cube
        var cube = graphics.CreateCube();
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(cube.Id, 0))
            .With(new Material
            {
                ShaderId = graphics.LitShader.Id,
                TextureId = graphics.WhiteTexture.Id,
                Color = new Vector4(1, 0.3f, 0.3f, 1f),
                Metallic = 0.2f,
                Roughness = 0.5f
            })
            .With(new SpinSpeed { Value = 1f })
            .Build();
    })
    .OnResize((w, h) => Console.WriteLine($"Resized: {w}x{h}"))
    .Run();

// Custom component and system
[Component]
public partial struct SpinSpeed { public float Value; }

public class SpinSystem : ISystem
{
    private IWorld? world;
    public bool Enabled { get; set; } = true;

    public void Initialize(IWorld world) => this.world = world;

    public void Update(float dt)
    {
        foreach (var entity in world!.Query<Transform3D, SpinSpeed>())
        {
            ref var transform = ref world.Get<Transform3D>(entity);
            var speed = world.Get<SpinSpeed>(entity);
            transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, speed.Value * dt);
        }
    }

    public void Dispose() { }
}
```

## Lifecycle Events

The `ILoopProvider` interface (implemented by `IGraphicsContext`) provides these events:

| Event | Timing | Use Case |
|-------|--------|----------|
| `OnReady` | Once, when ready | Create resources, set up scene |
| `OnUpdate` | Every frame | Game logic (if not using auto-update) |
| `OnRender` | Every frame | Additional rendering |
| `OnResize` | When resized | Update viewports, aspect ratios |
| `OnClosing` | When closing | Final cleanup |

## Dependencies

- **KeenEyes.Graphics.Abstractions** - Backend-agnostic interfaces
- **KeenEyes.Graphics** - Backend-agnostic systems
- **KeenEyes.Graphics.Silk** - Silk.NET OpenGL backend
- **KeenEyes.Runtime** - WorldRunnerBuilder
- **KeenEyes.Common** - Transform3D component
- **Silk.NET** - OpenGL/Vulkan bindings
