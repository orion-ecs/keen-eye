# OpenGL Binding Libraries for C# - Research Report

**Date:** December 2024
**Purpose:** Evaluate OpenGL binding libraries for a custom cross-platform game engine

## Executive Summary

After evaluating Silk.NET, OpenTK, and Veldrid, **Silk.NET is the recommended choice** for a custom game engine due to its active maintenance, broader API coverage, .NET Foundation backing, and flexible architecture that doesn't impose design decisions.

## Library Comparison

### Silk.NET

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/dotnet/Silk.NET](https://github.com/dotnet/Silk.NET) |
| **Stars** | 4.8k |
| **Contributors** | 109 |
| **Open Issues** | 72 |
| **Latest Version** | 2.22.0 (November 2024) |
| **License** | MIT |

**Strengths:**
- **.NET Foundation project** - Guarantees project longevity and governance
- **Broadest API coverage** - OpenGL, Vulkan, DirectX, WebGPU, OpenXR, OpenAL, OpenCL, SDL, GLFW, Assimp
- **Active development** - Monthly releases, v3.0 under active development
- **Mobile support** - Proven on Android and iOS
- **Performance-focused** - Hand-optimized JIT assembly generation
- **Source-generated bindings** - Type-safe, low overhead

**Weaknesses:**
- v2.x in "limited investment" mode as team focuses on v3.0
- More complex API surface due to breadth of coverage
- Uses unsafe code extensively (necessary for low-level APIs)

**API Example:**
```csharp
using Silk.NET.Windowing;
using Silk.NET.OpenGL;

var options = WindowOptions.Default with
{
    Size = new Vector2D<int>(800, 600),
    Title = "Silk.NET Window"
};

var window = Window.Create(options);
GL gl = null;

window.Load += () => gl = window.CreateOpenGL();
window.Render += dt => { /* rendering code */ };
window.Run();
```

---

### OpenTK

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/opentk/opentk](https://github.com/opentk/opentk) |
| **Stars** | 3.5k |
| **Contributors** | 122 |
| **Open Issues** | 50 |
| **Latest Version** | 4.9.4 (March 2025) |
| **License** | MIT |

**Strengths:**
- **Mature and battle-tested** - 15+ years of development
- **Included math library** - Robust `Vector`, `Matrix`, `Quaternion` types
- **GameWindow abstraction** - Convenient for simple applications
- **Active v5.0 development** - New bindings generator, Vulkan support, pure C# windowing
- **Simpler API** - More approachable for beginners

**Weaknesses:**
- **No mobile support** - Desktop only (Windows, Linux, macOS)
- **Narrower scope** - OpenGL, OpenAL, OpenCL only
- v4.x and v5.x are separate codebases with different APIs
- Community-maintained (no foundation backing)

**API Example:**
```csharp
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

public class Game : GameWindow
{
    public Game() : base(
        GameWindowSettings.Default,
        new NativeWindowSettings
        {
            Size = new Vector2i(800, 600),
            Title = "OpenTK Window"
        })
    { }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // rendering code
        SwapBuffers();
    }
}

using var game = new Game();
game.Run();
```

---

### Veldrid

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/veldrid/veldrid](https://github.com/veldrid/veldrid) |
| **Stars** | 2.6k |
| **Contributors** | 53 |
| **Open Issues** | 106 |
| **Latest Version** | 4.9.0 (February 2023) |
| **License** | MIT |

**Strengths:**
- **Multi-backend abstraction** - Vulkan, Metal, D3D11, OpenGL, OpenGL ES from single API
- **Portable shaders** - SPIR-V support for write-once shaders
- **Modern design** - Command-based architecture like Vulkan/D3D12
- **Allocation-free rendering** - Zero GC pressure in hot paths
- **VR support** - OpenVR and Oculus integration

**Weaknesses:**
- **Maintenance concerns** - Creator announced in Feb 2023 they can no longer publicly share updates
- **No releases since 2023** - 106 open issues, unclear future
- **Higher abstraction level** - Less control than raw OpenGL bindings
- Active fork exists (`ppy.Veldrid`) but fragmentation risk

**API Example:**
```csharp
using Veldrid;
using Veldrid.StartupUtilities;

var windowCI = new WindowCreateInfo
{
    X = 100, Y = 100,
    WindowWidth = 800, WindowHeight = 600,
    WindowTitle = "Veldrid Window"
};
var window = VeldridStartup.CreateWindow(ref windowCI);

var options = new GraphicsDeviceOptions
{
    PreferStandardClipSpaceYDirection = true,
    PreferDepthRangeZeroToOne = true
};
var graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options);

while (window.Exists)
{
    window.PumpEvents();
    // rendering code
}
```

---

## Decision Matrix

| Criteria | Weight | Silk.NET | OpenTK | Veldrid |
|----------|--------|----------|--------|---------|
| **Performance** | High | 9 | 8 | 8 |
| **Maintenance** | High | 9 | 8 | 4 |
| **Flexibility** | High | 9 | 7 | 6 |
| **Documentation** | Medium | 7 | 8 | 7 |
| **Community** | Medium | 8 | 8 | 5 |
| **Mobile Support** | Medium | 9 | 3 | 7 |
| **API Coverage** | Medium | 10 | 6 | 8 |
| **Weighted Score** | - | **8.6** | **6.9** | **6.1** |

*Scores: 1-10, higher is better*

---

## Recommendations

### Primary Recommendation: Silk.NET

For a custom cross-platform game engine, **Silk.NET** is the best choice:

1. **Doesn't force architecture** - Provides raw bindings, lets you build your own abstractions
2. **Future-proof** - Supports modern APIs (Vulkan, WebGPU) alongside OpenGL
3. **Foundation backing** - Long-term viability guaranteed
4. **Mobile path exists** - Android/iOS support when needed

### Alternative: OpenTK

Consider **OpenTK** if:
- Desktop-only is acceptable
- You want included math types without external dependency
- Simpler API surface is preferred
- You're following LearnOpenGL-style tutorials (better OpenTK coverage)

### Avoid: Veldrid

**Veldrid** is not recommended due to:
- Uncertain maintenance status since Feb 2023
- No public releases in nearly 2 years
- Risk of being stuck on unmaintained library

If you need a multi-backend abstraction layer, consider building a thin one on top of Silk.NET rather than depending on Veldrid.

---

## Integration Notes for KeenEyes

When integrating graphics with the KeenEyes ECS:

1. **Keep graphics decoupled** - Graphics should be a system, not core ECS
2. **Component design** - Use components for render-relevant data:
   ```csharp
   [Component]
   public partial struct Renderable
   {
       public int MeshId;
       public int MaterialId;
   }

   [Component]
   public partial struct Transform
   {
       public Vector3 Position;
       public Quaternion Rotation;
       public Vector3 Scale;
   }
   ```
3. **Query for rendering** - Let systems gather renderables:
   ```csharp
   foreach (var entity in World.Query<Transform, Renderable>())
   {
       // Extract transform and submit draw call
   }
   ```

---

## Next Steps

- [ ] Create minimal "hello triangle" with Silk.NET
- [ ] Benchmark draw call overhead
- [ ] Prototype RenderSystem for KeenEyes
- [ ] Evaluate math library options (System.Numerics vs Silk.NET.Maths)

---

## Sources

- [Silk.NET GitHub](https://github.com/dotnet/Silk.NET)
- [Silk.NET Documentation](https://dotnet.github.io/Silk.NET/)
- [Silk.NET NuGet](https://www.nuget.org/packages/Silk.NET.OpenGL)
- [OpenTK GitHub](https://github.com/opentk/opentk)
- [OpenTK Documentation](https://opentk.net/)
- [OpenTK NuGet](https://www.nuget.org/packages/OpenTK/)
- [Veldrid GitHub](https://github.com/veldrid/veldrid)
- [Veldrid Documentation](https://veldrid.dev/)
- [LibHunt Comparison](https://dotnet.libhunt.com/compare-silk-net-vs-opentk)
- [Silk.NET Game Viability Discussion](https://github.com/dotnet/Silk.NET/discussions/438)
