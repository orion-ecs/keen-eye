# Runtime

The `KeenEyes.Runtime` package provides application runtime and main loop management through a fluent builder pattern.

## Overview

The `WorldRunnerBuilder` abstracts main loop setup, enabling backend-agnostic game loops. This means you can swap rendering backends (Silk.NET, SDL, etc.) without rewriting your loop setup code.

## Installation

Reference `KeenEyes.Runtime` in your project:

```xml
<PackageReference Include="KeenEyes.Runtime" />
```

Or add a project reference:

```xml
<ProjectReference Include="..\..\src\KeenEyes.Runtime\KeenEyes.Runtime.csproj" />
```

## Basic Usage

```csharp
using KeenEyes;
using KeenEyes.Runtime;
using KeenEyes.Graphics.Silk;

using var world = new World();
world.InstallPlugin(new SilkGraphicsPlugin(config));

// Simple usage - auto-calls world.Update() each frame
world.CreateRunner()
    .OnReady(() => CreateScene(world))
    .Run();
```

## ILoopProvider Interface

The `WorldRunnerBuilder` works with any plugin that implements `ILoopProvider`:

```csharp
public interface ILoopProvider
{
    event Action? OnReady;           // Once when ready
    event Action<float>? OnUpdate;   // Every frame
    event Action<float>? OnRender;   // Every frame
    event Action<int, int>? OnResize; // When resized
    event Action? OnClosing;         // When closing

    void Initialize();
    void Run();
    bool IsInitialized { get; }
}
```

Currently, `IGraphicsContext` extends `ILoopProvider`, so any graphics plugin (like `SilkGraphicsPlugin`) automatically provides this interface.

## Builder Methods

### OnReady

Called once when the loop is ready (window created, graphics context available):

```csharp
world.CreateRunner()
    .OnReady(() =>
    {
        var graphics = world.GetExtension<IGraphicsContext>();

        // Create resources
        var mesh = graphics.CreateCube();

        // Set up scene
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(mesh.Id, 0))
            .Build();
    })
    .Run();
```

### OnUpdate

Called every frame for update logic. **If not set, `world.Update(dt)` is called automatically.**

```csharp
// Auto-update (recommended for most cases)
world.CreateRunner()
    .OnReady(() => CreateScene())
    .Run();  // world.Update() called automatically

// Explicit control (for custom logic)
world.CreateRunner()
    .OnReady(() => CreateScene())
    .OnUpdate((dt) =>
    {
        // Pre-update logic
        ProcessInput();

        // Manual world update
        world.Update(dt);

        // Post-update logic
        UpdateUI();
    })
    .Run();
```

### OnRender

Called every frame for additional rendering (beyond what systems do):

```csharp
world.CreateRunner()
    .OnReady(() => CreateScene())
    .OnRender((dt) =>
    {
        // Additional rendering after systems
        RenderDebugOverlay();
    })
    .Run();
```

### OnResize

Called when the window/viewport is resized:

```csharp
world.CreateRunner()
    .OnReady(() => CreateScene())
    .OnResize((width, height) =>
    {
        Console.WriteLine($"Window resized to {width}x{height}");
        // CameraSystem handles aspect ratio updates automatically
    })
    .Run();
```

### OnClosing

Called when the loop is closing:

```csharp
world.CreateRunner()
    .OnReady(() => CreateScene())
    .OnClosing(() =>
    {
        Console.WriteLine("Goodbye!");
        // World disposal is handled by 'using' statement
    })
    .Run();
```

## Complete Example

```csharp
using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Runtime;

// Configure graphics
var config = new SilkGraphicsConfig
{
    WindowTitle = "WorldRunnerBuilder Demo",
    WindowWidth = 1280,
    WindowHeight = 720,
    VSync = true
};

// Create world
using var world = new World();
world.InstallPlugin(new SilkGraphicsPlugin(config));

// Add systems
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate);
world.AddSystem<RenderSystem>(SystemPhase.Render);

// Run with builder
world.CreateRunner()
    .OnReady(() =>
    {
        Console.WriteLine("Graphics initialized!");

        var graphics = world.GetExtension<IGraphicsContext>();

        // Camera
        world.Spawn()
            .With(new Transform3D(new Vector3(0, 2, 5), Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f/9f, 0.1f, 100f))
            .WithTag<MainCameraTag>()
            .Build();

        // Light
        world.Spawn()
            .With(Transform3D.Identity)
            .With(Light.Directional(Vector3.One, 1f))
            .Build();

        // Cube
        var cube = graphics.CreateCube();
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(cube.Id, 0))
            .With(new Material
            {
                ShaderId = graphics.LitShader.Id,
                TextureId = graphics.WhiteTexture.Id,
                Color = Vector4.One
            })
            .Build();

        Console.WriteLine("Scene created!");
    })
    .OnResize((w, h) =>
    {
        Console.WriteLine($"Resized: {w}x{h}");
    })
    .OnClosing(() =>
    {
        Console.WriteLine("Closing...");
    })
    .Run();

Console.WriteLine("Application ended.");
```

## Error Handling

If no `ILoopProvider` is registered, `CreateRunner()` throws:

```csharp
try
{
    world.CreateRunner().Run();
}
catch (InvalidOperationException ex)
{
    // "No ILoopProvider found. Install a plugin that provides a main loop..."
    Console.WriteLine(ex.Message);
}
```

## Benefits

| Benefit | Description |
|---------|-------------|
| **Backend-agnostic** | Same code works with any `ILoopProvider` |
| **Less boilerplate** | Auto `world.Update()` by default |
| **Explicit when needed** | `OnUpdate` callback for custom control |
| **Consistent style** | Matches EntityBuilder, QueryBuilder patterns |
| **Testable** | Can mock `ILoopProvider` for testing |

## Comparison

### Before (backend-specific)

```csharp
var graphics = world.GetExtension<IGraphicsContext>();

graphics.OnReady += () => CreateScene();
graphics.OnUpdate += (dt) => world.Update(dt);
graphics.OnRender += (dt) => { };
graphics.OnResize += (w, h) => Console.WriteLine($"Resized: {w}x{h}");
graphics.OnClosing += () => Console.WriteLine("Closing...");

graphics.Initialize();
graphics.Run();
```

### After (backend-agnostic)

```csharp
world.CreateRunner()
    .OnReady(() => CreateScene())
    .OnResize((w, h) => Console.WriteLine($"Resized: {w}x{h}"))
    .OnClosing(() => Console.WriteLine("Closing..."))
    .Run();  // Auto-calls world.Update()
```

## Custom Loop Providers

To create a custom backend, implement `ILoopProvider`:

```csharp
public class MyCustomLoopProvider : ILoopProvider
{
    public event Action? OnReady;
    public event Action<float>? OnUpdate;
    public event Action<float>? OnRender;
    public event Action<int, int>? OnResize;
    public event Action? OnClosing;

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        // Set up your loop infrastructure
        IsInitialized = true;
        OnReady?.Invoke();
    }

    public void Run()
    {
        while (/* running */)
        {
            float dt = /* calculate delta time */;
            OnUpdate?.Invoke(dt);
            OnRender?.Invoke(dt);
        }
        OnClosing?.Invoke();
    }
}

// Register in a plugin
public class MyPlugin : IWorldPlugin
{
    public string Name => "MyPlugin";

    public void Install(IPluginContext context)
    {
        var provider = new MyCustomLoopProvider();
        context.SetExtension<ILoopProvider>(provider);
    }

    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<ILoopProvider>();
    }
}
```
