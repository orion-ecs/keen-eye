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
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;

// Configure the window
var windowConfig = new WindowConfig
{
    Title = "My Game",
    Width = 1280,
    Height = 720,
    VSync = true
};

// Configure graphics rendering
var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f),
    EnableDepthTest = true,
    EnableCulling = true
};

// Create world and install plugins
using var world = new World();
world.InstallPlugin(new SilkWindowPlugin(windowConfig));  // Window first
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));

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

For advanced scenarios, you can wire events directly on `ILoopProvider`:

```csharp
var loopProvider = world.GetExtension<ILoopProvider>();

loopProvider.OnReady += () => CreateScene(world);
loopProvider.OnUpdate += (dt) => world.Update(dt);
loopProvider.OnResize += (w, h) => Console.WriteLine($"Resized: {w}x{h}");
loopProvider.OnClosing += () => Console.WriteLine("Closing...");

loopProvider.Initialize();
loopProvider.Run();  // Blocks until closed
```

## Configuration

### Window Configuration

```csharp
var windowConfig = new WindowConfig
{
    Title = "My Game",           // Window title
    Width = 1920,                // Initial window width
    Height = 1080,               // Initial window height
    VSync = true,                // Enable vertical sync
    Resizable = true,            // Allow window resizing
    TargetFramerate = 60.0,      // Target FPS (0 = uncapped)
    TargetUpdateFrequency = 60.0 // Target fixed update rate
};
```

### Graphics Configuration

```csharp
var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.1f, 0.1f, 0.1f, 1f),  // Background color
    EnableDepthTest = true,      // Enable depth testing
    EnableCulling = true         // Enable backface culling
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

## Custom Shaders

KeenEyes uses GLSL shaders with the Silk.NET backend.

### Shader Structure

**Vertex Shader:**

```glsl
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 vTexCoord;
out vec3 vNormal;
out vec3 vFragPos;

void main()
{
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    gl_Position = uProjection * uView * worldPos;
    vTexCoord = aTexCoord;
    vNormal = mat3(transpose(inverse(uModel))) * aNormal;
    vFragPos = worldPos.xyz;
}
```

**Fragment Shader:**

```glsl
#version 330 core
in vec2 vTexCoord;
in vec3 vNormal;
in vec3 vFragPos;

uniform sampler2D uTexture;
uniform vec4 uColor;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uTexture, vTexCoord);
    FragColor = texColor * uColor;
}
```

### Creating Custom Shaders

```csharp
var graphics = world.GetExtension<IGraphicsContext>();

// Load from strings
string vertexSource = File.ReadAllText("shaders/custom.vert");
string fragmentSource = File.ReadAllText("shaders/custom.frag");
var shader = graphics.CreateShader(vertexSource, fragmentSource);

// Use in material
var material = new Material
{
    ShaderId = shader.Id,
    TextureId = myTexture.Id,
    Color = Vector4.One
};

// Clean up when done
graphics.DeleteShader(shader);
```

### Built-in Shaders

| Shader | Purpose | Key Uniforms |
|--------|---------|--------------|
| `LitShader` | PBR lighting | uModel, uView, uProjection, light uniforms |
| `UnlitShader` | No lighting (2D, UI) | uModel, uView, uProjection, uTexture, uColor |
| `SolidShader` | Solid color fill | uModel, uView, uProjection, uColor |

### Common Shader Patterns

**Tinting:**

```glsl
// Apply color tint to texture
FragColor = texture(uTexture, vTexCoord) * uColor;
```

**Alpha Cutout (for sprites):**

```glsl
vec4 texColor = texture(uTexture, vTexCoord);
if (texColor.a < 0.5)
    discard;
FragColor = texColor * uColor;
```

**Simple Gradient:**

```glsl
// Vertical gradient based on UV
float gradient = vTexCoord.y;
FragColor = mix(uColorTop, uColorBottom, gradient);
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

## 2D Rendering Patterns

While KeenEyes graphics primarily targets 3D, 2D games can use orthographic cameras and specialized patterns.

### Orthographic Camera Setup

```csharp
// Create an orthographic camera for 2D
world.Spawn()
    .With(new Transform3D(new Vector3(0, 0, 10), Quaternion.Identity, Vector3.One))
    .With(Camera.CreateOrthographic(
        size: 5f,            // Half-height (10 units total vertically)
        aspectRatio: 16f/9f,
        nearPlane: 0.1f,
        farPlane: 100f))
    .WithTag<MainCameraTag>()
    .Build();
```

### Sprite Rendering with Quads

```csharp
var graphics = world.GetExtension<IGraphicsContext>();
var quad = graphics.CreateQuad(1f, 1f);  // Unit quad

// Create a sprite entity
world.Spawn()
    .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
    .With(new Renderable(quad.Id, layer: 0))
    .With(new Material
    {
        ShaderId = graphics.UnlitShader.Id,  // No lighting for 2D
        TextureId = myTexture.Id,
        Color = Vector4.One
    })
    .Build();
```

### Layer-Based Rendering Order

Use `Renderable.Layer` for draw order. Lower layers render first (background), higher layers render on top (foreground):

```csharp
// Background (layer 0)
CreateSprite(backgroundTexture, layer: 0, z: 0);

// Gameplay objects (layer 1)
CreateSprite(playerTexture, layer: 1, z: 0);

// Foreground decorations (layer 2)
CreateSprite(foregroundTexture, layer: 2, z: 0);

// UI (handled by UI system, renders on top)
```

### Pixel-Perfect Rendering

For pixel-art games, calculate orthographic size from your virtual resolution:

```csharp
int gameHeight = 180;  // Virtual resolution height in pixels
float orthoSize = gameHeight / 2f;  // Half-height for ortho camera

world.Spawn()
    .With(Transform3D.Identity)
    .With(Camera.CreateOrthographic(
        size: orthoSize,
        aspectRatio: (float)windowWidth / windowHeight,
        nearPlane: 0.1f,
        farPlane: 100f))
    .WithTag<MainCameraTag>()
    .Build();
```

### Sprite Animation

Animate sprites by updating texture source rectangles or swapping textures:

```csharp
public class SpriteAnimationSystem : ISystem
{
    public void Update(float dt)
    {
        foreach (var entity in world.Query<SpriteAnimation, Material>())
        {
            ref var anim = ref world.Get<SpriteAnimation>(entity);
            ref var material = ref world.Get<Material>(entity);

            anim.Timer += dt;
            if (anim.Timer >= anim.FrameDuration)
            {
                anim.Timer = 0;
                anim.CurrentFrame = (anim.CurrentFrame + 1) % anim.FrameCount;
                material.TextureId = anim.Frames[anim.CurrentFrame];
            }
        }
    }
}
```

## Complete Example

```csharp
using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;

var windowConfig = new WindowConfig
{
    Title = "Spinning Cube",
    Width = 1280,
    Height = 720,
    VSync = true
};

var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f),
    EnableDepthTest = true,
    EnableCulling = true
};

using var world = new World();
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));

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

The `ILoopProvider` interface (provided by `SilkWindowPlugin`) provides these events:

| Event | Timing | Use Case |
|-------|--------|----------|
| `OnReady` | Once, when ready | Create resources, set up scene |
| `OnUpdate` | Every frame | Game logic (if not using auto-update) |
| `OnRender` | Every frame | Additional rendering |
| `OnResize` | When resized | Update viewports, aspect ratios |
| `OnClosing` | When closing | Final cleanup |

## Plugin Architecture

The graphics system requires the window plugin to be installed first:

```
SilkWindowPlugin (must be installed first)
├── Creates native window
├── Provides ILoopProvider (main loop)
└── Provides ISilkWindowProvider (shared window access)

SilkGraphicsPlugin (uses shared window)
├── Subscribes to window lifecycle events
├── Creates OpenGL context
└── Provides IGraphicsContext
```

**Required installation order:**

```csharp
// Window plugin first (provides ILoopProvider)
world.InstallPlugin(new SilkWindowPlugin(windowConfig));

// Then graphics and/or input plugins (any order after window)
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));
```

See the [Input documentation](input.md#plugin-architecture) for more details.

## Debugging & Troubleshooting

### Common Issues

#### Nothing Renders

1. **Check camera exists**: Ensure an entity with `Camera` and `MainCameraTag` exists
2. **Check clear color**: Use a bright clear color to verify camera is working
3. **Check systems added**: Verify `CameraSystem` and `RenderSystem` are registered
4. **Check loop provider**: Ensure `world.Update(dt)` is being called

```csharp
// Verify camera exists
var cameras = world.Query<Camera>().ToList();
Console.WriteLine($"Camera count: {cameras.Count}");
```

#### Objects Not Visible

1. **Position**: Check `Transform3D.Position` is in front of camera
2. **Scale**: Ensure scale is not zero
3. **Material**: Verify `ShaderId` and `TextureId` are valid handles
4. **Layer**: Check `Renderable.Layer` isn't being culled

```csharp
// Debug renderable entities
foreach (var entity in world.Query<Renderable, Transform3D>())
{
    var transform = world.Get<Transform3D>(entity);
    var renderable = world.Get<Renderable>(entity);
    Console.WriteLine($"Entity at {transform.Position}, mesh: {renderable.MeshId}");
}
```

#### Black Objects

1. **Lighting**: Add a `Light` entity or use `UnlitShader`
2. **Texture**: Verify texture loaded successfully (valid handle)
3. **Color alpha**: Check `Material.Color.W` is not zero
4. **Shader**: Use `UnlitShader` for 2D or debug

```csharp
// Quick fix: Add a directional light
world.Spawn()
    .With(new Transform3D(Vector3.Zero,
        Quaternion.CreateFromYawPitchRoll(0.5f, -0.5f, 0), Vector3.One))
    .With(Light.Directional(new Vector3(1, 1, 1), 1f))
    .Build();
```

#### Performance Issues

1. **Batching**: Group objects with the same material
2. **Draw calls**: Minimize unique material combinations
3. **Culling**: Enable backface culling for closed meshes
4. **VSync**: Disable for uncapped framerate testing

### Debug Visualization

```csharp
// Print rendering statistics
var cameras = world.Query<Camera>().Count();
var renderables = world.Query<Renderable>().Count();
var lights = world.Query<Light>().Count();

Console.WriteLine($"Cameras: {cameras}");
Console.WriteLine($"Renderables: {renderables}");
Console.WriteLine($"Lights: {lights}");
```

### Window Resize Handling

Update camera aspect ratio when the window resizes:

```csharp
world.CreateRunner()
    .OnResize((width, height) =>
    {
        float aspectRatio = (float)width / height;

        foreach (var entity in world.Query<Camera>())
        {
            ref var camera = ref world.Get<Camera>(entity);
            camera.AspectRatio = aspectRatio;
        }
    })
    .Run();
```

### Checking Resource Validity

```csharp
var graphics = world.GetExtension<IGraphicsContext>();

// Check if handles are valid (non-zero)
if (meshHandle.Id == 0)
    Console.WriteLine("Invalid mesh handle!");

if (textureHandle.Id == 0)
    Console.WriteLine("Invalid texture handle!");

// Built-in resources are always valid
var whiteTexture = graphics.WhiteTexture;  // Always valid
var litShader = graphics.LitShader;        // Always valid
```

## Dependencies

- **KeenEyes.Graphics.Abstractions** - Backend-agnostic interfaces
- **KeenEyes.Graphics** - Backend-agnostic systems
- **KeenEyes.Graphics.Silk** - Silk.NET OpenGL backend
- **KeenEyes.Platform.Silk** - Shared window provider
- **KeenEyes.Runtime** - WorldRunnerBuilder
- **KeenEyes.Common** - Transform3D component
- **Silk.NET** - OpenGL/Vulkan bindings
