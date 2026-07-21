# Shaders & KESL

The `KeenEyes.Shaders` library provides the GPU abstractions (devices, buffers, command buffers, compiled shaders) used by GPU compute and rendering code. Alongside it, `KeenEyes.Shaders.Compiler` implements **KESL** (KeenEyes Shader Language) — a small, ECS-aware shader language that transpiles to GLSL/HLSL and generates matching C# bindings — and `KeenEyes.Shaders.Generator` is a Roslyn incremental source generator that runs the compiler automatically over `.kesl` files at build time.

## Overview

KESL lets you describe a compute, vertex, fragment, or geometry shader once and get three outputs for free:

1. **Shader source** for one or more `ShaderBackend`s (GLSL and HLSL are implemented today; `ShaderBackend` also defines `MSL` and `SPIRV` values for future backends).
2. **C# bindings** — a generated partial class that describes the shader's inputs, outputs, and uniforms.
3. **Diagnostics** with source spans and "did you mean?" suggestions when a `.kesl` file fails to compile.

Unlike most KeenEyes subsystems, there is no `IWorldPlugin` for shaders — `KeenEyes.Shaders` is a standalone abstraction library. You supply an `IGpuDevice` implementation from your graphics backend and use it directly, or implement `IGpuComputeSystem`/`IWorldGpuComputeSystem` yourself (by hand or via KESL) to run compute work against a world's entities.

The three pieces:

| Project | Role |
|---------|------|
| `KeenEyes.Shaders` | Runtime abstractions: `IGpuDevice`, `GpuBuffer<T>`, `GpuCommandBuffer`, `CompiledShader`, `IGpuComputeSystem`, `IGpuVertexShader`, `IGpuFragmentShader`, descriptor types, and shader hot-reload support. |
| `KeenEyes.Shaders.Compiler` | The KESL compiler: lexer, parser, AST, GLSL/HLSL/C# code generators, and diagnostics. Exposed through `KeslCompiler`. |
| `KeenEyes.Shaders.Generator` | An `IIncrementalGenerator` (`KeslSourceGenerator`) that compiles `.kesl` files marked as `AdditionalFiles` and emits the generated C# directly into your build. |

## Quick Start

### Writing a KESL compute shader

A `.kesl` compute shader declares which components it needs via a `query` block, any scalar parameters via `params`, and its per-entity logic via `execute()`:

```
// physics.kesl

compute UpdatePhysics {
    query {
        write Position
        read Velocity
        without Frozen
    }

    params {
        deltaTime: float
    }

    execute() {
        Position.x += Velocity.x * deltaTime;
        Position.y += Velocity.y * deltaTime;
        Position.z += Velocity.z * deltaTime;
    }
}
```

`write`, `read`, and `optional` map to the `ComponentAccess` values on a `QueryDescriptor`'s `ComponentBinding`s — `write` components are uploaded and downloaded, `read` components are uploaded only, and `optional` components are included when present. `without` is tracked separately as an exclusion filter (`QueryDescriptor.WithoutComponents`) and excludes entities that have the named component.

### Enabling shader compilation

If your project uses `KeenEyes.Sdk`, `.kesl` files are picked up automatically: the SDK adds a reference to `KeenEyes.Shaders.Generator` as an analyzer and includes `**/*.kesl` as `AdditionalFiles` whenever the `IncludeKeenEyesShaders` MSBuild property is `true` (the default). No extra project configuration is needed — just add the `.kesl` file to your project.

Outside the SDK, wire it up manually:

```xml
<ItemGroup>
  <PackageReference Include="KeenEyes.Shaders.Generator" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  <AdditionalFiles Include="**/*.kesl" />
</ItemGroup>
```

### Compiling KESL directly

You can also drive the compiler yourself with `KeslCompiler`, which is useful for tooling, tests, or a custom build step:

```csharp
using KeenEyes.Shaders.Compiler;

var compiler = new KeslCompiler { Namespace = "MyGame.Shaders" };
var output = compiler.CompileAndGenerate(source, "physics.kesl");

if (output.HasErrors)
{
    foreach (var diagnostic in output.Diagnostics)
    {
        Console.WriteLine(diagnostic.ToMsBuildFormat());
    }
}
else
{
    foreach (var shader in output.Shaders)
    {
        // shader.GlslCode, shader.HlslCode, and shader.CSharpCode
        // are the generated outputs for shader.ShaderName
    }
}
```

`CompileAndGenerate` handles `compute`, `vertex`, `fragment`, `geometry`, and `pipeline` declarations in a single source file and returns a `CompilationOutput` containing one `ShaderOutput` per declared shader. Lower-level entry points (`KeslCompiler.Compile`, `KeslCompiler.GenerateGlsl`, `GenerateHlsl`, `GenerateShader`) are available if you already have a parsed AST and only need one output.

## Core Concepts

### GPU device and buffers

`IGpuDevice` is the main runtime entry point: it compiles shader source, allocates typed `GpuBuffer<T>` instances, and creates `GpuCommandBuffer`s for recording GPU work.

```csharp
using KeenEyes.Shaders;

// device is an IGpuDevice implementation supplied by your graphics backend.
IGpuDevice device = ...;

var positions = device.CreateBuffer<float>(entityCount, BufferUsage.ShaderReadWrite);
var shader = device.CompileComputeShader(glslSource, ShaderBackend.GLSL, "UpdatePhysics");

using var cmd = device.CreateCommandBuffer();
cmd.Begin();
cmd.BindComputeShader(shader);
cmd.BindBuffer(0, positions);
cmd.SetUniform("deltaTime", 0.016f);
cmd.DispatchAuto(shader, entityCount);
cmd.End();

device.Submit(cmd);
device.WaitIdle();
```

`GpuBuffer<T>.Upload`/`Download` move data between CPU and GPU; `DownloadAll()` is a convenience that allocates and returns a fresh array. `CompiledShader.CalculateDispatchX(itemCount)` (used internally by `GpuCommandBuffer.DispatchAuto`) and `CalculateDispatch2D(width, height)` compute the workgroup counts needed to cover a given item count from the shader's `LocalSizeX`/`LocalSizeY`/`LocalSizeZ`.

### Query descriptors

`QueryDescriptor` describes which components a compute shader reads, writes, treats as optional, or excludes — the runtime representation of a KESL `query` block. Build one with `QueryDescriptorBuilder`:

```csharp
using KeenEyes.Shaders;

var query = QueryDescriptor.Builder("UpdatePhysics")
    .Write("Position")
    .Read("Velocity")
    .Without("Frozen")
    .Build();

var binding = query.GetBinding("Position"); // ComponentBinding? with a GPU binding index
bool required = query.IsRequired("Velocity");
```

Bindings are assigned GPU binding indices in the order they're added (`Read`, then `Write`, then `Optional`), and `QueryDescriptor.AllBindings` exposes them sorted by binding index for buffer setup.

### Compute systems

`IGpuComputeSystem` is the interface each generated (or hand-written) compute shader class implements. It exposes the shader's `Query`, target `Backend`, and lifecycle (`Initialize(IGpuDevice)`, `GetShaderSource()`, `IsInitialized`/`IsDisposed`).

`IWorldGpuComputeSystem` extends this with world-aware execution — `Execute(IGpuDevice, IWorld, float deltaTime)` and an async `ExecuteAsync` overload. The `IWorld` referenced here is a minimal, `KeenEyes.Shaders`-local interface exposing only `EntityCount`, defined to avoid a compile-time dependency from `KeenEyes.Shaders` on `KeenEyes.Core`.

### Vertex and fragment shaders

Compiled vertex and fragment shaders implement `IGpuVertexShader` and `IGpuFragmentShader` respectively. Each exposes its name, an `InputLayoutDescriptor`/`IReadOnlyList<InputAttribute>` describing its I/O, its `IReadOnlyList<UniformDescriptor>`, and `GetShaderSource(ShaderBackend)`:

```csharp
public sealed partial class TransformVertexShader : IGpuVertexShader
{
    public string Name => "TransformVertex";

    public InputLayoutDescriptor InputLayout { get; } = new([
        new InputAttribute("position", AttributeType.Float3, 0),
        new InputAttribute("normal", AttributeType.Float3, 1),
        new InputAttribute("texCoord", AttributeType.Float2, 2)
    ]);

    public IReadOnlyList<UniformDescriptor> Uniforms { get; } = [
        new UniformDescriptor("model", UniformType.Matrix4),
        new UniformDescriptor("view", UniformType.Matrix4),
        new UniformDescriptor("projection", UniformType.Matrix4)
    ];

    public IReadOnlyList<InputAttribute> Outputs { get; } = [];

    public string GetShaderSource(ShaderBackend backend) => backend switch
    {
        ShaderBackend.GLSL => GlslSource,
        ShaderBackend.HLSL => HlslSource,
        _ => throw new NotSupportedException($"Backend {backend} not supported")
    };
}
```

`InputLayoutDescriptor` computes a vertex `Stride` from its attributes and can look attributes up `GetAttribute(int location)` / `GetAttribute(string name)` / `GetOffset(int location)`. `UniformDescriptor` similarly reports `ElementSizeInBytes`, `TotalSizeInBytes` (accounting for `ArraySize`), and whether it `IsSampler`.

### Diagnostics

Compiler errors carry a `DiagnosticSeverity`, a `KESLxxx` error code, and a `SourceSpan`. `DiagnosticFormatter` renders them with the offending source line underlined and an optional hint and "did you mean?" suggestion:

```
error[KESL202] at input:1:8:
  Expected 'component' or 'compute' declaration

  invalid_keyword MyShader {
         ^

  Hint: Valid declarations are 'component' or 'compute'.
```

`DiagnosticFormatter.FormatCompact(Diagnostic)` (equivalently `diagnostic.ToMsBuildFormat()`) produces a single-line `file(line,column): severity code: message` form suitable for build output.

### Hot reload

`KeenEyes.Shaders.HotReload` provides a small runtime registry for reloading shaders during development. Implement `IHotReloadable` (`Name`, `SourcePath`, `Reload(newSource, backend)`) on your shader classes, register them, and point a `KeslFileWatcher` at your shader directory:

```csharp
using KeenEyes.Shaders.HotReload;

var registry = new ShaderRegistry();
registry.Register(myShader); // myShader : IHotReloadable
registry.OnShaderUpdated += (name, shader) => Console.WriteLine($"{name} reloaded");

using var watcher = new KeslFileWatcher(registry, ShaderBackend.GLSL);
watcher.OnShaderRecompiled += (path, result) => Console.WriteLine($"Recompiled {path}");
watcher.OnCompilationError += (path, errors) => Console.WriteLine($"{path}: {string.Join(", ", errors)}");
watcher.Watch("Assets/Shaders");
```

`KeslFileWatcher` watches `*.kesl` files under the given directory (recursively by default) and calls `ShaderRegistry.Update` on successful recompilation, which in turn invokes `IHotReloadable.Reload` on the matching registered shader.

## Performance

- Prefer batch uploads/downloads over frequent small transfers when working with `GpuBuffer<T>`; use `BufferUsage.DynamicUpload` for buffers that change every frame and `BufferUsage.Static` for buffers that rarely change.
- `IGpuDevice.WaitIdle()` is a hard synchronization point that blocks until all submitted GPU work completes — use it sparingly, and prefer `IGpuFence` for finer-grained synchronization when you only need to know that specific work has finished.
- `GpuCommandBuffer.DispatchAuto` and `CompiledShader.CalculateDispatchX`/`CalculateDispatch2D` compute workgroup counts from the shader's declared local size, so dispatch sizing stays correct if a shader's `numthreads`/`local_size` changes.

## Compiler internals

This section is for contributors working on `KeenEyes.Shaders.Compiler` itself. If you're only consuming KESL from a game or plugin project, the sections above are all you need.

`KeslCompiler` orchestrates a straightforward, linear pipeline: **lexer → parser → AST → generators**. Each stage is a separate, independently testable class with no shared mutable state between compilations — a new `Lexer`/`Parser` pair is created per call, so nothing carries over between files.

### Lexing

`Lexer` (`Lexing/Lexer.cs`) turns raw KESL source text into a flat `List<Token>`. `Tokenize()` repeatedly calls `NextToken()` until it produces a `TokenKind.EndOfFile` token. Each `Token` carries its `TokenKind`, raw text, and a `SourceLocation` (file, line, column) used later for diagnostics.

The lexer recognizes KESL's keywords (`component`, `compute`, `vertex`, `fragment`, `geometry`, `pipeline`, the query modifiers `read`/`write`/`optional`/`without`, primitive types like `float3` and `mat4`, and so on) via a static `Dictionary<string, TokenKind>` lookup applied to scanned identifiers — anything not in that table becomes a plain `TokenKind.Identifier`. It also handles `//` line comments and `/* */` block comments, integer and float literals (including exponents and an `f`/`F` suffix), and multi-character operators such as `==`, `!=`, `&&`, and `+=`. Any character the lexer doesn't recognize produces a `TokenKind.Error` token rather than throwing; `KeslCompiler.Compile` scans the token list for `TokenKind.Error` up front and turns each into a `Diagnostic` with the `KeslErrorCodes.UnexpectedCharacter` code before parsing ever starts.

### Parsing

`Parser` (`Parsing/Parser.cs`) is a hand-written recursive-descent parser over the token list produced by the lexer. `Parse()` loops over top-level declarations, dispatching on the current token's `TokenKind` to one of `ParseComponentDeclaration`, `ParseComputeDeclaration`, `ParseVertexDeclaration`, `ParseFragmentDeclaration`, `ParseGeometryDeclaration`, or `ParsePipelineDeclaration`. Each of those methods recursively parses the nested blocks specific to that declaration kind — `query`/`params`/`execute` for compute shaders, `input`/`output`/`textures`/`samplers`/`layout` for vertex, fragment, and geometry shaders.

Parse errors are represented by an internal `ParseException`. When a parse method can't consume the token it expects, it records a `Diagnostic` (via the parser's `Error`/`Consume` helpers) and throws `ParseException` to unwind out of the current declaration. `Parse()` catches this at the top level and calls `Synchronize()` to skip forward to a likely declaration boundary, so a single malformed declaration doesn't abort the entire file — the parser keeps collecting diagnostics for the rest of the source. Several error paths (`ErrorExpectedDeclaration`, `ErrorExpectedBindingMode`, `ErrorExpectedTypeName`, `ErrorExpectedInputTopology`, and similar) also call `SuggestionEngine.GetSuggestions` against a fixed candidate list (declaration keywords, binding modes, type names, topology names) to populate the "did you mean?" suggestions rendered by `DiagnosticFormatter`.

### AST

The parser's output is a `SourceFile`, a `Declaration` list of `ComponentDeclaration`, `ComputeDeclaration`, `VertexDeclaration`, `FragmentDeclaration`, `GeometryDeclaration`, and `PipelineDeclaration` nodes (`Parsing/Ast/AstNode.cs`). Every AST node is an immutable `record` deriving from the abstract `AstNode(SourceLocation Location)`, so nodes are cheap to construct and safe to share across the code generators that read them. Nested blocks are their own records too — `QueryBlock`/`QueryBinding`, `ParamsBlock`/`ParamDeclaration`, `InputBlock`/`OutputBlock`/`AttributeDeclaration`, `TexturesBlock`/`SamplersBlock`, `GeometryLayoutBlock`, and `ExecuteBlock`, whose `Body` is a list of `Statement` nodes (assignments, `if`/`for`, etc. — defined in `Parsing/Ast/Statements.cs` and `Expressions.cs`). This AST is the sole hand-off point between parsing and code generation; none of the generators re-consult tokens or source text.

### Code generators

`GlslGenerator` and `HlslGenerator` both implement `IShaderGenerator`, which defines one `Generate` overload per declaration type (`ComputeDeclaration`, `VertexDeclaration`, `FragmentDeclaration`, `GeometryDeclaration`) plus a `Backend` and `FileExtension`. Both walk the same AST shape but emit different textual conventions for the same concepts — for example, a `write` query binding becomes an `std430` GLSL buffer under `GlslGenerator` but a `RWStructuredBuffer`/`register(u#)` declaration under `HlslGenerator`, and `mat4` becomes GLSL's `mat4` versus HLSL's `float4x4`. Each generator builds its output into an internal `StringBuilder`, tracking its own indentation and buffer/resource binding-index counters (`_bindingIndex` in `GlslGenerator`; separate `srvIndex`/`uavIndex` counters for read-only vs. read-write resources in `HlslGenerator`) as it walks the query bindings and execute-block statements.

`CSharpBindingGenerator` is a third generator with the same per-declaration-type `Generate` overloads (plus one for `PipelineDeclaration`, which the shader-language generators don't handle) but no `IShaderGenerator` conformance, since it emits C# rather than shader source. It produces the partial binding classes described in the SDK integration above — one per shader — using its own `Namespace` property to control the generated namespace.

### How `KeslCompiler` ties it together

`KeslCompiler.Compile` runs the lexer, checks for `TokenKind.Error` tokens, then runs the parser and merges its diagnostics, returning a `CompilationResult` (a parsed `SourceFile?` plus the accumulated `Diagnostics`). The static `GenerateGlsl`/`GenerateHlsl`/`GenerateShader` methods and the instance `GenerateCSharp` methods are thin overload sets that construct the matching generator and call `Generate` on a single AST node — `GenerateShader(declaration, backend)` is the one that switches on `ShaderBackend` to pick `GlslGenerator` or `HlslGenerator` at runtime.

`CompileAndGenerate` is the all-in-one entry point: it calls `Compile`, and if there are no errors, iterates every top-level `Declaration` in the `SourceFile`, running the appropriate GLSL, HLSL, and C# generators for each and collecting the results into a `ShaderOutput` (name plus each generated file's name and contents) per shader. `PipelineDeclaration`s are handled specially — a pipeline itself only produces a C# binding (no shader source), but `GenerateInlineShaderOutputs` recurses into any `PipelineStage.InlineShader` (an inline `vertex`/`geometry`/`fragment` block defined directly inside the `pipeline` rather than referenced by name) to emit outputs for those stages too. The final `CompilationOutput` is what `KeslSourceGenerator` (in `KeenEyes.Shaders.Generator`) writes into the build as generated files, and what `KeslCompiler.CompileAndGenerate`'s direct callers inspect via `HasErrors`/`Diagnostics` when driving the compiler by hand.

## Next Steps

- [ADR-009: KESL Shader Language](adr/009-kesl-shader-language.md) - Original design document
- [Shader Language Design](research/shader-language.md) - Research document on the KESL language
- [Shader Management Design](research/shader-management.md) - Research document on shader lifecycle and hot-reload
- [Plugins Guide](plugins.md) - How `IWorldPlugin` subsystems are installed, for contrast with this standalone library
- [Graphics Guide](graphics.md) - The graphics abstractions that shaders render into
- [SDK Documentation](sdk.md) - How `KeenEyes.Sdk` wires up `.kesl` files automatically
