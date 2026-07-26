# ADR-005: Graphics and Input Abstraction Layers

**Status:** Accepted
**Revision:** v2
**Implementation:** Shipped
**First accepted:** 2025-12-16 · **Last amended:** 2026-07-26

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
                              ▼
┌─────────────────────────────────────────────────────────────┐
│   KeenEyes.Platform.Silk (+ Platform.Silk.Abstractions)      │
│   SilkWindowPlugin registers SilkLoopProvider                 │
│   (Owns the window and the main loop)                         │
└─────────────────────────────────────────────────────────────┘
          │                                       │
          ▼                                       ▼
┌─────────────────────────┐         ┌─────────────────────────┐
│ KeenEyes.Graphics       │         │ KeenEyes.Input          │
│ .Abstractions / .Silk   │         │ .Abstractions / .Silk   │
│ IGraphicsContext        │         │ IInputContext           │
│ (subscribes to loop)    │         │ (subscribes to loop)    │
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

    // Render-thread marshaling for operations needing the GL context
    Task<T> InvokeOnRenderThreadAsync<T>(Func<T> action);
    void InvokeOnRenderThread(Action action);
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

#### Loop Ownership (Platform Layer)

The loop contract sits below the graphics and input layers: neither `IGraphicsContext` nor `IInputContext` extends `ILoopProvider` — both are `IDisposable` only. A dedicated platform layer (`KeenEyes.Platform.Silk` / `KeenEyes.Platform.Silk.Abstractions`) owns windowing and the main loop: `SilkWindowPlugin` registers `SilkLoopProvider` as the world's `ILoopProvider`, and the graphics and input plugins subscribe to that shared loop.

```csharp
public interface IGraphicsContext : IDisposable
{
    // Graphics-specific members (meshes, textures, shaders)
    // Use ILoopProvider (from SilkWindowPlugin) for the main loop
}
```

### Package Dependencies

```
KeenEyes.Abstractions (IWorld, ILoopProvider)
    ↑                              ↑
KeenEyes.Runtime          KeenEyes.Platform.Silk.Abstractions
(WorldRunnerBuilder)               ↑
                          KeenEyes.Platform.Silk
                          (SilkWindowPlugin, SilkLoopProvider)
                               ↑              ↑
                 KeenEyes.Graphics.Silk   KeenEyes.Input.Silk
                 (rendering only)         (input only)
```

The Silk graphics and input packages depend on the platform layer for the loop; they contribute no loop implementation of their own.

### Registration

Registration is split by responsibility. `SilkWindowPlugin` (KeenEyes.Platform.Silk) registers the `ILoopProvider` extension that enables `WorldRunnerBuilder`:

```csharp
// SilkWindowPlugin — owns window and loop
public void Install(IPluginContext context)
{
    provider = new SilkWindowProvider(config);
    var loopProvider = new SilkLoopProvider(provider);

    context.SetExtension<ISilkWindowProvider>(provider);
    context.SetExtension<ILoopProvider>(loopProvider);  // Enables WorldRunnerBuilder
}
```

`SilkGraphicsPlugin` registers only graphics-facing extensions and requires the window plugin to be installed first:

```csharp
// SilkGraphicsPlugin — graphics-facing extensions only
public void Install(IPluginContext context)
{
    graphicsContext = new SilkGraphicsContext(windowProvider, config);

    context.SetExtension<IGraphicsContext>(graphicsContext);
    context.SetExtension<I2DRendererProvider>(graphicsContext);
    context.SetExtension<ITextRendererProvider>(graphicsContext);
    context.SetExtension<IFontManagerProvider>(graphicsContext);
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
| **Testability** | Mock `ILoopProvider` for testing loop-dependent logic (`MockLoopProvider` ships in `KeenEyes.Testing`) |
| **Reduced boilerplate** | Auto-update removes repetitive `world.Update()` wiring |
| **Consistent patterns** | Follows existing builder patterns (EntityBuilder, QueryBuilder) |
| **Future-proof** | Input, audio, and other systems share the platform-provided loop |

### Negative

| Drawback | Mitigation |
|----------|------------|
| **Additional packages** | Clear separation makes dependencies explicit |
| **Learning curve** | Documentation shows simple path (most apps just call `Run()`) |
| **Indirection** | Single method call overhead, negligible at frame rate |

### Neutral

- Direct event wiring against `ILoopProvider` still works (for advanced use cases)
- No changes to component, system, or query APIs
- Build complexity unchanged (packages already existed)

## Future Work

This architecture enabled planned features; status as of the latest revision:

1. **Input Abstraction** (`KeenEyes.Input.Abstractions`) — ✅ Shipped
   - `KeenEyes.Input.Abstractions` and `KeenEyes.Input.Silk` exist
   - `IInputContext : IDisposable` — input subscribes to the platform-provided loop rather than providing one
   - Backend-agnostic input polling and events

2. **Headless Loop Provider** — Not yet implemented
   - Simple timer-based loop for servers
   - No window or graphics dependency
   - Would enable CLI tools and dedicated servers
   - The only `ILoopProvider` implementations today are `SilkLoopProvider` (windowed, KeenEyes.Platform.Silk) and `MockLoopProvider` (test-only, KeenEyes.Testing)

3. **Multi-Backend Applications** — Not yet implemented
   - Swap backends at runtime (e.g., Vulkan fallback to OpenGL)
   - Platform-specific backend selection

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Status Accepted confirmed; Implementation marked Shipped. Decision amended to as-built loop ownership: `IGraphicsContext`/`IInputContext` no longer extend `ILoopProvider` — the platform layer (`KeenEyes.Platform.Silk`) owns window and loop, with `SilkWindowPlugin` registering `SilkLoopProvider` and `SilkGraphicsPlugin` registering graphics-facing extensions only; architecture and package-dependency diagrams and the registration example updated to match. Future Work updated: input abstraction shipped (as `IInputContext : IDisposable`); headless server loop provider and runtime multi-backend swapping remain unimplemented (`MockLoopProvider` covers testing only).
- **v1 — 2025-12-16 (e63b8c0b):** Accepted — introduce layered loop abstraction: ILoopProvider in KeenEyes.Abstractions plus WorldRunnerBuilder in KeenEyes.Runtime, separating main-loop orchestration from the Silk.NET graphics backend and paving the way for backend-agnostic input.
