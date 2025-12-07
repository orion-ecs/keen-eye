# Shader Management and Compilation - Research Report

**Date:** December 2024
**Purpose:** Evaluate approaches to shader authoring, compilation, hot-reloading, and management in a cross-platform OpenGL engine

## Executive Summary

After evaluating shader compilation approaches, reflection systems, variant management, and available .NET tools, the recommended approach for a custom cross-platform engine is:

1. **Development:** Runtime GLSL compilation with hot-reloading for rapid iteration
2. **Production:** Offline SPIR-V compilation with caching for performance
3. **Tooling:** Use **Veldrid.SPIRV** for cross-platform shader compilation or **Glslang.NET** for pure GLSL-to-SPIR-V compilation
4. **Variants:** Implement a `#define`-based permutation system with offline compilation to manage shader complexity
5. **Reflection:** Use SPIRV-Cross or OpenGL's native reflection APIs for uniform/attribute discovery

---

## Shader Compilation Pipeline

### Runtime vs Offline Compilation

| Approach | Pros | Cons | Best For |
|----------|------|------|----------|
| **Runtime GLSL** | Immediate iteration, no build step | Driver-specific parsing, slower startup | Development |
| **Offline SPIR-V** | Faster loading, consistent behavior, cacheable | Requires build step, OpenGL 4.6+ | Production |
| **Hybrid** | Best of both | Implementation complexity | Most projects |

### GLSL vs SPIR-V

**GLSL (OpenGL Shading Language):**
- Human-readable, easy to debug
- Each driver parses and compiles differently (potential inconsistencies)
- No native `#include` support
- Widely supported (OpenGL 2.0+)

**SPIR-V (Standard Portable Intermediate Representation):**
- Binary format, machine-optimized
- Consistent behavior across vendors
- Supports specialization constants (compile-time parameters)
- OpenGL 4.6+ required for native support
- Can be decompiled back to GLSL via SPIRV-Cross for older GL versions

### Recommended Pipeline

```
Development:
  *.glsl → [Custom Preprocessor] → [glCompileShader] → GPU

Production:
  *.glsl → [shaderc/glslang] → *.spv → [Cache] → [glSpecializeShader] → GPU
                                   ↓
                            [SPIRV-Cross] → GLSL (for GL < 4.6)
```

---

## Shader Reflection

Reflection allows automatic discovery of uniforms, attributes, and resource bindings without hardcoding.

### OpenGL Native Reflection

```csharp
// Query uniform count
GL.GetProgram(program, ProgramProperty.ActiveUniforms, out int count);

// Query each uniform
for (int i = 0; i < count; i++)
{
    GL.GetActiveUniform(program, i, bufSize, out _, out int size, out UniformType type, out string name);
    int location = GL.GetUniformLocation(program, name);
}
```

**Limitations:**
- Must compile shader first
- No struct member information for UBOs
- Limited to what GPU driver exposes

### SPIRV-Cross Reflection

SPIRV-Cross provides comprehensive reflection without GPU compilation:

```cpp
// C++ example (no direct C# bindings yet)
spirv_cross::Compiler compiler(spirv_binary);
auto resources = compiler.get_shader_resources();

for (auto& ubo : resources.uniform_buffers)
{
    uint32_t set = compiler.get_decoration(ubo.id, spv::DecorationDescriptorSet);
    uint32_t binding = compiler.get_decoration(ubo.id, spv::DecorationBinding);
    // Access struct members...
}
```

**Advantages:**
- Offline analysis possible
- Full type information including struct members
- Cross-platform consistent results
- JSON output available for tooling

### SPIRV-Reflect (Alternative)

Lighter-weight C library specifically for reflection. Good for Vulkan-style descriptor binding queries.

---

## Uniform Buffer Objects (UBO) Best Practices

### std140 Layout Rules

The `std140` layout provides consistent cross-vendor memory layout:

```glsl
layout(std140, binding = 0) uniform Matrices
{
    mat4 projection;  // offset 0, size 64
    mat4 view;        // offset 64, size 64
    mat4 model;       // offset 128, size 64
};
```

**Key Alignment Rules:**
| Type | Base Alignment | Size |
|------|---------------|------|
| `float` | 4 bytes | 4 bytes |
| `vec2` | 8 bytes | 8 bytes |
| `vec3` | 16 bytes | 12 bytes |
| `vec4` | 16 bytes | 16 bytes |
| `mat4` | 16 bytes | 64 bytes |
| `array[N]` | 16 bytes each | N × 16 bytes |

**Important Caveats:**
- Arrays are always aligned to 16 bytes (even `int[]`)
- `vec3` wastes 4 bytes (combine with `float` to avoid waste)
- Maximum UBO size: ~16KB guaranteed

### Best Practices

1. **Use explicit bindings (GL 4.2+):**
   ```glsl
   layout(std140, binding = 2) uniform Lights { ... };
   ```

2. **Pack data efficiently:**
   ```glsl
   // Good: No wasted space
   vec3 position;  // 12 bytes
   float radius;   // 4 bytes (fills the vec4)

   // Bad: 4 bytes wasted after vec3
   vec3 position;  // 16 bytes (4 wasted)
   vec3 color;     // 16 bytes (4 wasted)
   ```

3. **Share UBOs across shaders:**
   - Same uniform block declaration → same memory layout
   - Reduces state changes and buffer uploads

4. **Use SSBOs for large data (GL 4.3+):**
   - Shader Storage Buffer Objects support std430 (tightly packed arrays)
   - Much larger size limits (128MB+)

---

## Shader Variants / Permutations

### The Permutation Problem

Shader variants are needed for feature combinations (shadows, normal maps, skinning, etc.). Naive implementation leads to exponential growth:

- 10 binary features → 1,024 variants
- 20 binary features → 1,048,576 variants

### Approaches

| Approach | Pros | Cons | Shader Count |
|----------|------|------|--------------|
| **Uber-shader with branches** | Single shader | Register pressure, branch overhead | 1 |
| **`#define` permutations** | Optimized per-variant | Compile time, storage | 2^N |
| **Modular fragments** | Composable, fewer permutations | Complex build system | Varies |
| **Hybrid** | Balanced | Implementation effort | 100s |

### Modern Best Practice (Doom-style)

Doom 2016/Eternal use forward-rendering uber-shaders with ~100 variants:

1. **Identify orthogonal features** - Not all combinations are valid
2. **Use specialization constants** - SPIR-V supports compile-time constants
3. **Prune invalid combinations** - Don't compile what won't be used
4. **Cache aggressively** - Compile once, load many times

### Implementation Pattern

```csharp
public class ShaderVariantKey
{
    public bool UseNormalMap { get; init; }
    public bool UseShadows { get; init; }
    public int LightCount { get; init; }

    public string ToDefines() => string.Join("\n",
        UseNormalMap ? "#define USE_NORMAL_MAP" : "",
        UseShadows ? "#define USE_SHADOWS" : "",
        $"#define LIGHT_COUNT {LightCount}"
    );
}

// Compile-time specialization (SPIR-V)
public record SpecializationConstant(uint Id, object Value);
```

---

## Include System and Preprocessing

### The Problem

GLSL has no native `#include` directive. Code sharing requires preprocessing.

### Available Solutions

| Approach | Support | Pros | Cons |
|----------|---------|------|------|
| **GL_ARB_shading_language_include** | NVIDIA, Mesa 20.0+ | Native driver support | Not universal |
| **GL_GOOGLE_include_directive** | Vulkan/glslang | Standard for SPIR-V toolchain | SPIR-V only |
| **Custom preprocessor** | Universal | Full control | Must implement |
| **shaderc** | Universal | `#include` + defines | External dependency |

### Custom Preprocessor Implementation

```csharp
public class ShaderPreprocessor
{
    private readonly Dictionary<string, string> _includes = new();

    public void AddInclude(string path, string source)
    {
        _includes[path] = source;
    }

    public string Process(string source)
    {
        // Must handle:
        // 1. #include "path" directives
        // 2. #version must remain first non-comment line
        // 3. #ifdef/#ifndef for include guards
        // 4. Line number mapping for error messages
    }
}
```

### shaderc Include Handler

shaderc supports includes via callback:

```cpp
shaderc_compile_options_set_include_callbacks(
    options,
    resolver_fn,    // Resolve include path
    releaser_fn,    // Free include data
    user_data
);
```

### Recommended Organization

```
shaders/
├── common/
│   ├── constants.glsl
│   ├── lighting.glsl
│   └── transforms.glsl
├── materials/
│   ├── pbr.frag
│   └── unlit.frag
└── postprocess/
    ├── bloom.frag
    └── tonemap.frag
```

---

## Hot-Reloading

Hot-reloading allows shader changes without restarting the application—critical for rapid iteration.

### Implementation Approaches

| Approach | Latency | Complexity | Reliability |
|----------|---------|------------|-------------|
| **Timestamp polling** | ~1 frame | Low | High |
| **File system events** | Immediate | Medium | Platform-specific |
| **Manual trigger** | On demand | Lowest | Highest |

### Basic Implementation

```csharp
public class ShaderHotReloader
{
    private readonly Dictionary<string, DateTime> _lastModified = new();
    private readonly Dictionary<string, ShaderProgram> _shaders = new();

    public void CheckForChanges()
    {
        foreach (var (path, shader) in _shaders)
        {
            var currentModTime = File.GetLastWriteTimeUtc(path);
            if (currentModTime > _lastModified[path])
            {
                _lastModified[path] = currentModTime;
                TryRecompile(shader, path);
            }
        }
    }

    private void TryRecompile(ShaderProgram shader, string path)
    {
        try
        {
            var newSource = File.ReadAllText(path);
            var newShader = CompileShader(newSource);
            shader.Replace(newShader);  // Atomic swap
        }
        catch (ShaderCompilationException ex)
        {
            // Log error but keep old shader running
            Console.WriteLine($"Shader error: {ex.Message}");
        }
    }
}
```

### Best Practices

1. **Never crash on bad shader** - Log errors, keep previous version
2. **Watch include dependencies** - Reload when included files change
3. **Debounce file events** - Editors trigger multiple saves
4. **Map error line numbers** - Account for preprocessing
5. **Use separable shaders** - `glCreateShaderProgram` for faster iteration

### File Watching Libraries

| Platform | API |
|----------|-----|
| Windows | `ReadDirectoryChangesW`, FileSystemWatcher (.NET) |
| Linux | `inotify` |
| macOS | FSEvents, kqueue |
| Cross-platform | [SimpleFileWatcher](https://github.com/apetrone/simplefilewatcher) |

---

## .NET Shader Tools Comparison

### Glslang.NET

| Attribute | Value |
|-----------|-------|
| **NuGet** | [Glslang.NET](https://www.nuget.org/packages/Glslang.NET) |
| **Version** | 1.1.4 |
| **License** | MIT |
| **Framework** | .NET 8.0 |

**Strengths:**
- Direct wrapper around Khronos reference compiler
- Cross-platform via Zig-based build
- SPIR-V disassembly support
- Active development (2024)

**Weaknesses:**
- .NET 8.0+ only
- Less documented than alternatives
- No built-in reflection

**Use Case:** Pure GLSL → SPIR-V compilation without additional features.

---

### Veldrid.SPIRV

| Attribute | Value |
|-----------|-------|
| **NuGet** | [Veldrid.SPIRV](https://www.nuget.org/packages/Veldrid.SPIRV) |
| **Version** | 1.0.15 |
| **Downloads** | 2.9M |
| **Last Updated** | June 2022 |
| **Framework** | .NET Standard 2.0, .NET Framework 4.0+ |

**Strengths:**
- Mature and widely used (2.9M downloads)
- GLSL → SPIR-V → HLSL/GLSL/MSL cross-compilation
- Specialization constant support
- Wraps shaderc + SPIRV-Cross
- Broad framework compatibility

**Weaknesses:**
- Not updated since June 2022
- Tied to Veldrid ecosystem (though usable standalone)
- Native library dependencies

**Use Case:** Cross-platform shader compilation with maximum compatibility.

---

### XenoAtom.Interop.libshaderc

| Attribute | Value |
|-----------|-------|
| **NuGet** | [XenoAtom.Interop.libshaderc](https://www.nuget.org/packages/XenoAtom.Interop.libshaderc) |
| **Version** | 1.1.0-alpha.2 |
| **License** | BSD-2-Clause |

**Strengths:**
- Low-level P/Invoke wrapper (maximum control)
- MSBuild integration available (XenoAtom.ShaderCompiler.Build)
- Source generator for embedding SPIR-V in C#
- Multithreaded `dotnet-shaderc` tool

**Weaknesses:**
- Alpha status
- Requires native library setup
- Less documentation

**Use Case:** Build-time shader compilation with MSBuild integration.

---

### dotnet-shaderc (Tool)

| Attribute | Value |
|-----------|-------|
| **NuGet** | [dotnet-shaderc](https://www.nuget.org/packages/dotnet-shaderc) |
| **Version** | 1.2.2 |

**Description:** Command-line tool equivalent to `glslc` for .NET projects. Multithreaded shader compilation.

---

## Decision Matrix

| Criteria | Weight | Glslang.NET | Veldrid.SPIRV | XenoAtom |
|----------|--------|-------------|---------------|----------|
| **Maintenance** | High | 9 | 5 | 7 |
| **Documentation** | High | 6 | 8 | 5 |
| **Ease of Use** | Medium | 7 | 9 | 6 |
| **Feature Set** | High | 7 | 9 | 8 |
| **Compatibility** | High | 6 | 10 | 7 |
| **MSBuild Integration** | Medium | 4 | 5 | 9 |
| **Weighted Score** | - | **6.6** | **7.7** | **7.0** |

*Scores: 1-10, higher is better*

---

## Recommendations

### Primary Recommendation: Hybrid Approach

For KeenEyes engine development:

1. **Use Veldrid.SPIRV** for shader compilation
   - Best compatibility across .NET versions
   - Proven in production (2.9M downloads)
   - Cross-compile to GLSL for older OpenGL support

2. **Implement custom preprocessing** for `#include` support
   - Control over include resolution
   - Works with both runtime and offline compilation

3. **Hot-reload during development**
   - Timestamp-based polling (simple, reliable)
   - Fall back to previous shader on compilation errors

4. **Offline SPIR-V compilation for release**
   - Faster startup times
   - Consistent behavior
   - Use specialization constants for variants

### Alternative: Pure Glslang.NET

Consider **Glslang.NET** if:
- Targeting .NET 8.0+ only
- Want latest glslang features
- Don't need cross-compilation to HLSL/MSL
- Prefer actively maintained solution

### Build-Time Integration

Consider **XenoAtom.ShaderCompiler.Build** if:
- Want MSBuild integration for shader compilation
- Prefer embedding SPIR-V directly in assemblies
- Building asset pipeline tooling

---

## Error Handling Best Practices

### Line Number Mapping

After preprocessing, shader line numbers don't match source files. Solutions:

1. **`#line` directive:**
   ```glsl
   #line 1 "common/lighting.glsl"
   // included content here
   #line 42 "materials/pbr.frag"
   ```

2. **Error message parsing and remapping:**
   ```csharp
   // Parse "ERROR: 0:42: ..." and map line 42 back to original file
   ```

### Driver-Specific Messages

Different GPU drivers produce different error messages. Consider:
- Normalizing error formats
- Testing on multiple drivers (NVIDIA, AMD, Intel, Mesa)
- Providing helpful error context in logs

---

## Shader Organization Patterns

### Single File Per Stage

```
shaders/
├── basic.vert
├── basic.frag
├── skinned.vert
└── pbr.frag
```

**Pros:** Simple, clear separation
**Cons:** No code sharing without includes

### Combined with Markers

```glsl
// basic.glsl
#ifdef VERTEX_SHADER
void main() { gl_Position = mvp * position; }
#endif

#ifdef FRAGMENT_SHADER
out vec4 fragColor;
void main() { fragColor = vec4(1.0); }
#endif
```

**Pros:** Related code together
**Cons:** Requires preprocessing to split

### Effect Files (Unity/Unreal Style)

```
shaders/
└── basic.effect
    ├── metadata (passes, states)
    ├── vertex shader
    └── fragment shader
```

**Pros:** Complete material definition
**Cons:** Custom format, more tooling needed

---

## Integration Notes for KeenEyes

When integrating shaders with the KeenEyes ECS:

1. **Shaders are resources, not components:**
   ```csharp
   public class ShaderLibrary
   {
       private readonly Dictionary<string, ShaderProgram> _shaders = new();
       public ShaderProgram Get(string name) => _shaders[name];
   }
   ```

2. **Material components reference shaders:**
   ```csharp
   [Component]
   public partial struct Material
   {
       public int ShaderId;
       public int TextureId;
       // Uniform values...
   }
   ```

3. **Render system queries for materials:**
   ```csharp
   public class RenderSystem : SystemBase
   {
       public override void Update(float deltaTime)
       {
           foreach (var entity in World.Query<Transform, Renderable, Material>())
           {
               ref readonly var material = ref World.Get<Material>(entity);
               var shader = ShaderLibrary.Get(material.ShaderId);
               // Bind shader, set uniforms, draw
           }
       }
   }
   ```

4. **Hot-reload without component changes:**
   - ShaderProgram internals update, entity data unchanged
   - Render system automatically uses reloaded shaders

---

## Research Task Checklist

### Completed
- [x] Implement basic runtime GLSL compilation with error handling
- [x] Evaluate preprocessing approaches for `#include`
- [x] Design shader variant system
- [x] Research uniform buffer object (UBO) best practices
- [x] Evaluate .NET shader compilation libraries

### For Future Investigation
- [ ] Test SPIR-V path on OpenGL 4.6
- [ ] Implement hot-reload watching file changes
- [ ] Benchmark compilation times (runtime vs cached SPIR-V)
- [ ] Test Veldrid.SPIRV with Silk.NET
- [ ] Profile variant compilation with 50+ permutations

---

## Sources

### Shader Compilation
- [SPIR-V - OpenGL Wiki](https://www.khronos.org/opengl/wiki/SPIR-V)
- [Google Shaderc](https://github.com/google/shaderc)
- [Translate GLSL to SPIR-V at Runtime - Eric's Blog](https://lxjk.github.io/2020/03/10/Translate-GLSL-to-SPIRV-for-Vulkan-at-Runtime.html)
- [Why do we need SPIR-V? - Stack Overflow](https://stackoverflow.com/questions/49619038/why-do-we-need-spir-v)

### Shader Reflection
- [SPIRV-Cross Wiki - Reflection API](https://github.com/KhronosGroup/SPIRV-Cross/wiki/Reflection-API-user-guide)
- [SPIRV-Cross GitHub](https://github.com/KhronosGroup/SPIRV-Cross)
- [SPIRV-Reflect GitHub](https://github.com/KhronosGroup/SPIRV-Reflect)

### Shader Variants
- [The Shader Permutation Problem - Part 1](https://therealmjp.github.io/posts/shader-permutations-part1/)
- [The Shader Permutation Problem - Part 2](https://therealmjp.github.io/posts/shader-permutations-part2/)
- [Uber Shaders and Shader Permutations - Alex Tardif](https://alextardif.com/UberShader.html)
- [Unity Shader Variants Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-variants.html)

### Preprocessing and Includes
- [GLSL Preprocessing - ServerSpace](https://serverspace.io/support/help/what-is-preprocessing-in-glsl/)
- [Sharing Code Between GLSL Shaders - CG Stack Exchange](https://computergraphics.stackexchange.com/questions/100/sharing-code-between-multiple-glsl-shaders)
- [ARB_shading_language_include - Stack Overflow](https://stackoverflow.com/questions/10754437/how-to-using-the-include-in-glsl-support-arb-shading-language-include)

### Hot Reloading
- [Hot Reloading Shaders - Anton's OpenGL Tutorials](https://antongerdelan.net/opengl/shader_hot_reload.html)
- [GLSL Shader Live-Reloading - nlguillemot](https://nlguillemot.wordpress.com/2016/07/28/glsl-shader-live-reloading/)
- [ShaderSet GitHub](https://github.com/nlguillemot/ShaderSet)

### Uniform Buffer Objects
- [LearnOpenGL - Advanced GLSL](https://learnopengl.com/Advanced-OpenGL/Advanced-GLSL)
- [Uniform Buffer Object - OpenGL Wiki](https://www.khronos.org/opengl/wiki/Uniform_Buffer_Object)
- [UBO Best Practices - CG Stack Exchange](https://computergraphics.stackexchange.com/questions/4632/what-is-a-good-approach-for-handling-uniforms-in-modern-opengl)

### .NET Libraries
- [Veldrid.SPIRV GitHub](https://github.com/veldrid/veldrid-spirv)
- [Veldrid Portable Shaders](https://veldrid.dev/articles/portable-shaders.html)
- [Glslang.NET NuGet](https://www.nuget.org/packages/Glslang.NET)
- [XenoAtom.Interop GitHub](https://github.com/XenoAtom/XenoAtom.Interop)
- [dotnet-shaderc NuGet](https://www.nuget.org/packages/dotnet-shaderc)
