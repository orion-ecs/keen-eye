# ADR-005: Graphics and Input Abstraction Layers

**Status:** Accepted
**Date:** 2025-12-16

## Context

The KeenEyes framework initially implemented graphics through `KeenEyes.Graphics.Silk`, tightly coupling application code to Silk.NET:

```csharp
// Previous approach - backend-specific
var graphics = world.GetExtension<IGraphicsContext>();

graphics.OnLoad += () => CreateScene();
graphics.OnUpdate += (dt) => world.Update((float)dt);
graphics.OnRender += (dt) => { };
graphics.OnResize += (w, h) => { };
graphics.OnClosing += () => { };

graphics.Initialize();
graphics.Run();
```

This design has several problems:

| Problem | Impact |
|---------|--------|
| **Backend coupling** | Swapping Silk.NET for SDL or another backend requires rewriting loop setup code |
| **Graphics-specific terminology** | `OnLoad` implies graphics, but the loop pattern applies to any windowed application |
| **Inconsistent types** | Silk.NET uses `double` for delta time, but `World.Update` uses `float` |
| **Manual wiring** | Every application must wire the same event pattern manually |
| **Testing difficulty** | Hard to test loop-dependent code without a real graphics context |

Additionally, as we plan input system support, the same pattern will apply - input handling needs a main loop but shouldn't require graphics.

## Decision

Create a layered abstraction architecture that separates loop management from graphics:

```
┌─────────────────────────────────────────────────────────────┐
│                      Application Code                        │
│   world.CreateRunner().OnReady(...).Run()                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    KeenEyes.Runtime                          │
│   WorldRunnerBuilder, WorldRunnerExtensions                  │
│   (Backend-agnostic loop orchestration)                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  KeenEyes.Abstractions                       │
│   ILoopProvider interface                                    │
│   (Core loop contract)                                       │
└─────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┴───────────────────┐
          ▼                                       ▼
┌─────────────────────────┐         ┌─────────────────────────┐
│ KeenEyes.Graphics       │         │ KeenEyes.Input          │
│ .Abstractions           │         │ .Abstractions           │
│ IGraphicsContext        │         │ IInputContext           │
│ (extends ILoopProvider) │         │ (extends ILoopProvider) │
└─────────────────────────┘         └─────────────────────────┘
          │                                       │
          ▼                                       ▼
┌─────────────────────────┐         ┌─────────────────────────┐
│ KeenEyes.Graphics.Silk  │         │ KeenEyes.Input.Silk     │
│ (Silk.NET OpenGL impl)  │         │ (Silk.NET input impl)   │
└─────────────────────────┘         └─────────────────────────┘
```

### Key Components

#### ILoopProvider Interface

Core abstraction for anything that provides a main loop:

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

This lives in `KeenEyes.Abstractions` (no graphics dependency) because:
- Input systems need loops too
- Console applications might want a simple tick loop
- Enables testing with mock loop providers

#### WorldRunnerBuilder

Fluent builder in `KeenEyes.Runtime` that wraps any `ILoopProvider`:

```csharp
world.CreateRunner()
    .OnReady(() => CreateScene(world))
    .OnResize((w, h) => Console.WriteLine($"Resized: {w}x{h}"))
    .OnClosing(() => Console.WriteLine("Closing..."))
    .Run();  // Auto-calls world.Update() each frame
```

Key features:
- **Auto-update**: If no `OnUpdate` callback provided, calls `world.Update(dt)` automatically
- **Backend-agnostic**: Works with any `ILoopProvider` implementation
- **Consistent API**: Same pattern regardless of graphics backend or input-only mode

#### IGraphicsContext Extension

`IGraphicsContext` now extends `ILoopProvider`:

```csharp
public interface IGraphicsContext : ILoopProvider, IDisposable
{
    // Graphics-specific members (meshes, textures, shaders)
    // Loop members inherited from ILoopProvider
}
```

### Package Dependencies

```
KeenEyes.Abstractions (IWorld, ILoopProvider)
    ↑
KeenEyes.Runtime (WorldRunnerBuilder)
    ↑
KeenEyes.Graphics.Abstractions (IGraphicsContext : ILoopProvider)
    ↑
KeenEyes.Graphics.Silk (SilkGraphicsContext implementation)
```

### Registration

Plugins register as both specific and abstract types:

```csharp
public void Install(IPluginContext context)
{
    graphicsContext = new SilkGraphicsContext(config);

    context.SetExtension<IGraphicsContext>(graphicsContext);
    context.SetExtension<ILoopProvider>(graphicsContext);  // Enables WorldRunnerBuilder
}
```

## Alternatives Considered

### Option 1: Keep Graphics-Specific Builder

Create `GraphicsRunnerBuilder` in `KeenEyes.Graphics`:

```csharp
world.CreateGraphicsRunner()
    .OnLoad(() => ...)
    .Run();
```

**Rejected because:**
- Duplicates pattern for input, audio, etc.
- Terminology (`OnLoad`) is graphics-specific
- Applications might want loop without graphics (server, CLI tools)

### Option 2: Abstract Factory Pattern

Create `ILoopProviderFactory` that backends implement:

```csharp
var loop = factory.CreateLoop(config);
loop.Run(() => world.Update());
```

**Rejected because:**
- More complex than necessary
- Doesn't leverage existing World/plugin infrastructure
- Less discoverable API

### Option 3: Make World Own the Loop

Put loop management directly in World:

```csharp
world.Run();  // World contains loop logic
```

**Rejected because:**
- World would need to know about windows, events, etc.
- Violates single responsibility
- Not all worlds need a loop (headless servers, tests)

## Consequences

### Positive

| Benefit | Description |
|---------|-------------|
| **Backend swapping** | Replace `SilkGraphicsPlugin` with `SDLGraphicsPlugin` without changing application code |
| **Platform migration** | Same game code runs on different platforms with appropriate backend |
| **Testability** | Mock `ILoopProvider` for testing loop-dependent logic |
| **Reduced boilerplate** | Auto-update removes repetitive `world.Update()` wiring |
| **Consistent patterns** | Follows existing builder patterns (EntityBuilder, QueryBuilder) |
| **Future-proof** | Input, audio, and other systems can provide loops |

### Negative

| Drawback | Mitigation |
|----------|------------|
| **Additional packages** | Clear separation makes dependencies explicit |
| **Learning curve** | Documentation shows simple path (most apps just call `Run()`) |
| **Indirection** | Single method call overhead, negligible at frame rate |

### Neutral

- Existing `IGraphicsContext` event wiring still works (for advanced use cases)
- No changes to component, system, or query APIs
- Build complexity unchanged (packages already existed)

## Future Work

This architecture enables planned features:

1. **Input Abstraction** (`KeenEyes.Input.Abstractions`)
   - `IInputContext : ILoopProvider`
   - Backend-agnostic input polling and events
   - Same builder pattern

2. **Headless Loop Provider**
   - Simple timer-based loop for servers
   - No window or graphics dependency
   - Enables CLI tools and dedicated servers

3. **Multi-Backend Applications**
   - Swap backends at runtime (e.g., Vulkan fallback to OpenGL)
   - Platform-specific backend selection
