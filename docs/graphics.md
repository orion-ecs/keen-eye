# Graphics Plugin

The `KeenEyes.Graphics` plugin provides 3D rendering capabilities using Silk.NET for OpenGL/Vulkan integration. It includes components for cameras, lights, materials, and renderables, along with resource management for meshes, textures, and shaders.

## Installation

Add the graphics plugin to your world:

```csharp
using KeenEyes.Graphics;

var config = new GraphicsConfig
{
    WindowWidth = 1920,
    WindowHeight = 1080,
    WindowTitle = "My Game",
    VSync = true
};

using var world = new World();
world.InstallPlugin(new GraphicsPlugin(config));
```

## Configuration Options

```csharp
var config = new GraphicsConfig
{
    WindowWidth = 1280,           // Initial window width
    WindowHeight = 720,           // Initial window height
    WindowTitle = "KeenEyes App", // Window title
    VSync = true,                 // Enable vertical sync
    Resizable = true,             // Allow window resizing
    Fullscreen = false,           // Start in fullscreen
    TargetFps = 0,                // Target FPS (0 = unlimited)
    ClearColor = new Vector4(0.1f, 0.1f, 0.1f, 1f)  // Background color
};
```

## GraphicsContext

Access the graphics API through the world extension:

```csharp
var graphics = world.GetExtension<GraphicsContext>();
graphics.Initialize();
```

### Main Loop

**Recommended: Event-driven pattern**

The recommended approach is to use Silk.NET's event-driven pattern with `OnRender`:

```csharp
// Handle rendering each frame
graphics.OnRender += (deltaTime) =>
{
    // Update world and render
    world.Update((float)deltaTime);
};

// Start the window (blocks until closed)
graphics.Initialize();
graphics.Run();
```

This pattern ensures proper resource cleanup and follows Silk.NET best practices.

**Alternative: Manual control**

For advanced scenarios requiring custom loop control:

```csharp
graphics.Initialize();

while (!graphics.ShouldClose)
{
    graphics.ProcessEvents();
    world.Update(deltaTime);
    graphics.SwapBuffers();
}
```

⚠️ **Warning**: The manual control pattern requires careful handling of cleanup and may cause issues with GPU resource disposal on window close. Use the event-driven pattern unless you have specific requirements.

## Components

### Transform3D

From `KeenEyes.Spatial`, defines position, rotation, and scale:

```csharp
using KeenEyes.Spatial;

world.Spawn()
    .With(new Transform3D(
        position: new Vector3(0, 5, 10),
        rotation: Quaternion.Identity,
        scale: Vector3.One))
    .Build();

// Or use the identity transform
world.Spawn()
    .With(Transform3D.Identity)
    .Build();
```

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
var meshId = graphics.CreateCube();

world.Spawn()
    .With(Transform3D.Identity)
    .With(new Renderable(meshId, materialId: 0))
    .With(Material.Default)
    .Build();
```

Renderable properties:
- `MeshId` - Handle to mesh resource
- `MaterialId` - Handle to material resource
- `Layer` - Render order (lower = first)
- `CastShadows` / `ReceiveShadows` - Shadow settings

### Material

Defines surface appearance:

```csharp
// Default PBR material
world.Spawn()
    .With(Transform3D.Identity)
    .With(new Renderable(meshId, 0))
    .With(Material.Default)
    .Build();

// Unlit colored material
var material = Material.Unlit(new Vector4(1, 0, 0, 1));  // Red

// Custom PBR material
var material = new Material
{
    ShaderId = graphics.LitShaderId,
    TextureId = textureId,
    Color = Vector4.One,
    Metallic = 0.8f,
    Roughness = 0.2f,
    EmissiveColor = Vector3.Zero
};
```

### Light

Illuminates the scene:

```csharp
// Directional light (sun)
world.Spawn()
    .With(new Transform3D(Vector3.Zero,
        Quaternion.CreateFromYawPitchRoll(0, -0.5f, 0), Vector3.One))
    .With(Light.Directional(
        color: new Vector3(1, 0.95f, 0.8f),
        intensity: 1.0f))
    .Build();

// Point light (bulb)
world.Spawn()
    .With(new Transform3D(new Vector3(5, 3, 0), Quaternion.Identity, Vector3.One))
    .With(Light.Point(
        color: new Vector3(1, 0.8f, 0.6f),
        intensity: 1.0f,
        range: 10f))
    .Build();

// Spot light (flashlight)
world.Spawn()
    .With(new Transform3D(position, rotation, Vector3.One))
    .With(Light.Spot(
        color: new Vector3(1, 1, 1),
        intensity: 2.0f,
        range: 15f,
        innerAngle: 15f,   // Full intensity cone
        outerAngle: 30f))  // Falloff cone
    .Build();
```

## Resource Management

### Meshes

```csharp
// Built-in primitives
var quadMesh = graphics.CreateQuad(width: 1f, height: 1f);
var cubeMesh = graphics.CreateCube(size: 1f);

// Custom mesh from vertex data
var vertices = new Vertex[]
{
    new(position: new Vector3(-1, 0, 0), normal: Vector3.UnitY, uv: new Vector2(0, 0)),
    new(position: new Vector3(1, 0, 0), normal: Vector3.UnitY, uv: new Vector2(1, 0)),
    new(position: new Vector3(0, 0, 1), normal: Vector3.UnitY, uv: new Vector2(0.5f, 1)),
};
var indices = new uint[] { 0, 1, 2 };
var customMesh = graphics.CreateMesh(vertices, indices);

// Delete when done
graphics.DeleteMesh(meshId);
```

### Textures

```csharp
// Solid color texture
var redTexture = graphics.CreateSolidColorTexture(255, 0, 0, 255);

// From raw RGBA data
var textureId = graphics.CreateTexture(
    width: 256,
    height: 256,
    data: pixelData,
    filter: TextureFilter.Linear,
    wrap: TextureWrap.Repeat);

// Built-in white texture
var whiteTexture = graphics.WhiteTextureId;

// Delete when done
graphics.DeleteTexture(textureId);
```

### Shaders

```csharp
// Built-in shaders
var unlitShader = graphics.UnlitShaderId;
var litShader = graphics.LitShaderId;
var solidShader = graphics.SolidShaderId;

// Custom GLSL shader
var customShader = graphics.CreateShader(
    vertexSource: @"
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        uniform mat4 uModel;
        uniform mat4 uViewProjection;
        void main() {
            gl_Position = uViewProjection * uModel * vec4(aPosition, 1.0);
        }",
    fragmentSource: @"
        #version 330 core
        out vec4 FragColor;
        uniform vec4 uColor;
        void main() {
            FragColor = uColor;
        }");

graphics.DeleteShader(shaderId);
```

## Rendering Control

```csharp
// Clear screen
graphics.Clear();                           // Use config clear color
graphics.Clear(new Vector4(0, 0, 1, 1));   // Custom color

// Depth testing
graphics.EnableDepthTest();
graphics.DisableDepthTest();

// Backface culling
graphics.EnableCulling();
graphics.DisableCulling();

// Viewport
graphics.SetViewport(0, 0, 1920, 1080);
```

## Events

GraphicsContext provides several events for window and rendering lifecycle:

```csharp
// Called once when the window and OpenGL context are ready
graphics.OnLoad += () =>
{
    Console.WriteLine("Graphics initialized");
    // Create meshes, textures, shaders here
};

// Called each frame for game logic updates
graphics.OnUpdate += (deltaTime) =>
{
    Console.WriteLine($"Update: {deltaTime}s");
    // Update game logic (optional - can use OnRender instead)
};

// Called each frame for rendering
graphics.OnRender += (deltaTime) =>
{
    Console.WriteLine($"Render: {deltaTime}s");
    // Render the scene - call world.Update() here
};

// Called when the window is resized
graphics.OnResize += (width, height) =>
{
    Console.WriteLine($"Window resized to {width}x{height}");
    // Update camera aspect ratios
};

// Called when the window is about to close
graphics.OnClosing += () =>
{
    Console.WriteLine("Window closing");
    // GPU resources are automatically disposed here
};
```

**Note**: For most applications, use `OnRender` to call `world.Update()`. The `OnUpdate` and `OnRender` events are called at the same frequency, but `OnRender` is the standard place for game loop logic in Silk.NET applications.

## Complete Example

```csharp
using KeenEyes;
using KeenEyes.Graphics;
using KeenEyes.Spatial;
using System.Numerics;

// Create world with graphics
var config = new GraphicsConfig
{
    WindowWidth = 1280,
    WindowHeight = 720,
    WindowTitle = "Cube Demo",
    VSync = true
};

using var world = new World();
world.InstallPlugin(new GraphicsPlugin(config));

var graphics = world.GetExtension<GraphicsContext>();

// Track scene initialization
bool sceneReady = false;

// Called when graphics are initialized
graphics.OnLoad += () =>
{
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
    var cubeMesh = graphics.CreateCube();
    world.Spawn("SpinningCube")
        .With(Transform3D.Identity)
        .With(new Renderable(cubeMesh, 0))
        .With(Material.Default)
        .Build();

    sceneReady = true;
};

// Update camera aspect ratio on resize
graphics.OnResize += (width, height) =>
{
    foreach (var entity in world.Query<Camera>())
    {
        ref var camera = ref world.Get<Camera>(entity);
        camera.AspectRatio = (float)width / height;
    }
};

// Render each frame
graphics.OnRender += (deltaTime) =>
{
    if (sceneReady)
    {
        // Update world (runs all systems including render)
        world.Update((float)deltaTime);
    }
};

// Run the application (blocks until window closes)
graphics.Initialize();
graphics.Run();
```

## Systems

The plugin registers these systems automatically:

- **CameraSystem** (`EarlyUpdate`) - Updates camera matrices
- **RenderSystem** (`Render`) - Draws all renderables

## Dependencies

- **Silk.NET** - OpenGL/Vulkan bindings (.NET Foundation)
- **Silk.NET.Windowing** - Cross-platform windowing
- **System.Numerics** - SIMD-accelerated math
- **KeenEyes.Spatial** - Transform3D component
