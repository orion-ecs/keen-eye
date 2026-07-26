# ADR-009: KESL - KeenEyes Shader Language

**Status:** Accepted
**Revision:** v2
**Implementation:** Partial
**First accepted:** 2025-12-31 · **Last amended:** 2026-07-26

## Context

KeenEyes needs GPU compute support for high-performance systems (particles, physics, AI, large-scale simulations). Current approaches require verbose manual marshaling between ECS components and GPU buffers:

```csharp
// Current: 30+ lines for simple physics update
var positions = new Vector3[count];
var velocities = new Vector3[count];
int i = 0;
foreach (var entity in world.Query<Position, Velocity>())
{
    positions[i] = world.Get<Position>(entity).ToVector3();
    velocities[i] = world.Get<Velocity>(entity).ToVector3();
    i++;
}
positionBuffer.SetData(positions);
velocityBuffer.SetData(velocities);
shader.Dispatch(count / 64 + 1, 1, 1);
positionBuffer.GetData(positions);
// ... write back
```

This is error-prone, verbose, and requires maintaining synchronization between shader code and C# bindings.

## Decision

Implement **KESL (KeenEyes Shader Language)**, a custom shader language that:

1. Provides first-class ECS query semantics
2. Transpiles to GLSL and HLSL (`ShaderBackend` also reserves MSL and SPIR-V values for future backends)
3. Generates C# binding code automatically
4. Integrates with KeenEyes build system

### Language Syntax

```
// physics.kesl

compute UpdatePhysics {
    query {
        write Position
        read  Velocity
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

### Compilation Output

**1. GLSL Compute Shader:**
```glsl
#version 450
layout(std430, binding = 0) buffer PositionBuffer { vec3 Position[]; };
layout(std430, binding = 1) readonly buffer VelocityBuffer { vec3 Velocity[]; };
uniform float deltaTime;
uniform uint entityCount;
layout(local_size_x = 64) in;

void main() {
    uint idx = gl_GlobalInvocationID.x;
    if (idx >= entityCount) return;
    Position[idx].x += Velocity[idx].x * deltaTime;
    Position[idx].y += Velocity[idx].y * deltaTime;
    Position[idx].z += Velocity[idx].z * deltaTime;
}
```

**2. C# Binding:**
```csharp
public sealed class UpdatePhysicsShader : IGpuComputeSystem
{
    public void Execute(World world, float deltaTime) { /* generated */ }
}
```

## Architecture

### Project Structure

```
src/
└── KeenEyes.Shaders/                    # Runtime abstractions
    ├── IGpuComputeSystem.cs             # Interfaces for GPU systems
    ├── IGpuDevice.cs                    # Device abstraction
    ├── GpuBuffer.cs                     # Buffer abstraction
    ├── GpuCommandBuffer.cs              # Command recording
    ├── CompiledShader.cs
    ├── QueryDescriptor.cs               # ECS query description
    ├── ShaderBackend.cs                 # GLSL | HLSL | MSL | SPIRV
    └── HotReload/                       # KeslFileWatcher, ShaderRegistry
editor/
├── KeenEyes.Shaders.Compiler/           # Compiler library
│   ├── Lexing/                          # Token, TokenKind, Lexer
│   ├── Parsing/                         # Parser + Ast/ node types
│   ├── Diagnostics/                     # Error codes, formatter, suggestions
│   └── CodeGen/                         # GlslGenerator, HlslGenerator,
│                                        #   CSharpBindingGenerator
├── KeenEyes.Shaders.Generator/          # Roslyn source generator
│                                        #   (fills the planned keslc CLI role)
├── KeenEyes.Shaders.VsCode/             # TextMate grammar + snippets
└── KeenEyes.Graph.Kesl/                 # Node-graph authoring for KESL
tools/
├── KeenEyes.Lsp.Kesl/                   # KESL language server
└── vscode-kesl/                         # VSCode extension (LSP client)
```

The compiler lives under `editor/` (build-time tooling), not `src/`; only the runtime abstractions ship as a `src/` library. The originally planned `Semantics/` directory (TypeChecker, SymbolTable) and the standalone `keslc` CLI were not built — see the pipeline notes below.

### Compilation Pipeline

```
                    Source (.kesl)
                         │
                         ▼
┌────────────────────────────────────────────────┐
│                     Lexer                       │
│  Input:  "compute Foo { query { ... } }"       │
│  Output: [Compute, Identifier("Foo"), ...]     │
└────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────┐
│                    Parser                       │
│  Input:  Token stream                          │
│  Output: AST (ComputeShaderNode)               │
└────────────────────────────────────────────────┘
                         │
            ┌────────────┴────────────┐
            ▼                         ▼
┌────────────────────────┐ ┌─────────────────────┐
│  GLSL/HLSL Generator   │ │  C# Binding Gen     │
└────────────────────────┘ └─────────────────────┘
            │                         │
            ▼                         ▼
      Foo.comp.glsl          FooShader.g.cs
```

A semantic-analysis stage between parsing and code generation — resolving component types against ECS registration metadata, type-checking expressions, and validating GPU compatibility and read/write access — is designed but **not yet implemented**. `KeslCompiler.Compile` runs lexer → parser → code generators only; error codes KESL300–306 (`UndefinedComponent`, `TypeMismatch`, `UndefinedField`, ...) are reserved for it in `KeslErrorCodes.cs` but are never emitted, and component names are currently trusted as written. This is the main outstanding gap in the implementation.

### Grammar (EBNF)

```ebnf
program        = declaration* ;
declaration    = componentDecl | computeDecl ;

componentDecl  = "component" IDENTIFIER "{" fieldList "}" ;
fieldList      = (field ("," field)*)? ;
field          = IDENTIFIER ":" type ;

computeDecl    = "compute" IDENTIFIER "{" computeBody "}" ;
computeBody    = queryBlock paramsBlock? executeBlock ;

queryBlock     = "query" "{" queryBinding* "}" ;
queryBinding   = ("read" | "write" | "optional" | "without") IDENTIFIER ;

paramsBlock    = "params" "{" paramList "}" ;
paramList      = (param ("," param)*)? ;
param          = IDENTIFIER ":" type ;

executeBlock   = "execute" "(" ")" block ;

block          = "{" statement* "}" ;
statement      = assignStmt | ifStmt | forStmt | exprStmt ;
assignStmt     = expression ("=" | "+=" | "-=" | "*=" | "/=") expression ";" ;
ifStmt         = "if" "(" expression ")" block ("else" block)? ;
forStmt        = "for" "(" IDENTIFIER ":" expression ".." expression ")" block ;
exprStmt       = expression ";" ;

expression     = logicalOr ;
logicalOr      = logicalAnd ("||" logicalAnd)* ;
logicalAnd     = equality ("&&" equality)* ;
equality       = comparison (("==" | "!=") comparison)* ;
comparison     = term (("<" | ">" | "<=" | ">=") term)* ;
term           = factor (("+" | "-") factor)* ;
factor         = unary (("*" | "/") unary)* ;
unary          = ("!" | "-")? primary ;
primary        = literal | IDENTIFIER | memberAccess | call | "(" expression ")" ;
memberAccess   = primary "." IDENTIFIER ;
call           = IDENTIFIER "(" argList? ")" ;
argList        = expression ("," expression)* ;

type           = "float" | "float2" | "float3" | "float4"
               | "int" | "int2" | "int3" | "int4"
               | "uint" | "bool" | "mat4" ;

literal        = NUMBER | "true" | "false" ;
```

### Type System

| KESL Type | C# Type | GLSL Type | GPU Alignment |
|-----------|---------|-----------|---------------|
| `float` | `float` | `float` | 4 bytes |
| `float2` | `Vector2` | `vec2` | 8 bytes |
| `float3` | `Vector3` | `vec3` | 16 bytes* |
| `float4` | `Vector4` | `vec4` | 16 bytes |
| `int` | `int` | `int` | 4 bytes |
| `uint` | `uint` | `uint` | 4 bytes |
| `bool` | `bool` | `bool` | 4 bytes |
| `mat4` | `Matrix4x4` | `mat4` | 64 bytes |

*Note: `float3`/`vec3` has 16-byte alignment in std430, wastes 4 bytes.

### Component Mapping

KESL references components by name. The design has the compiler resolve these against registered component types (this resolution belongs to the not-yet-implemented semantic-analysis stage; today component names are trusted as written):

```csharp
// Component registration (source generator metadata)
public static class ComponentMetadata
{
    public static readonly Dictionary<string, ComponentInfo> Components = new()
    {
        ["Position"] = new ComponentInfo(typeof(Position), [
            new FieldInfo("x", typeof(float), 0),
            new FieldInfo("y", typeof(float), 4),
            new FieldInfo("z", typeof(float), 8),
        ]),
        // ...
    };
}
```

### Query Semantics

| Keyword | Meaning | Buffer Mode | Upload | Download |
|---------|---------|-------------|--------|----------|
| `read` | Read-only access | `readonly buffer` | Yes | No |
| `write` | Read-write access | `buffer` | Yes | Yes |
| `optional` | May not exist | Conditional | If exists | If exists |
| `without` | Exclude entities | N/A | N/A | N/A |

### Generated C# Binding Pattern

```csharp
// Generated: UpdatePhysicsShader.g.cs
public sealed partial class UpdatePhysicsShader : IGpuComputeSystem, IDisposable
{
    private readonly GpuDevice _device;
    private readonly CompiledShader _shader;
    private GpuBuffer<Position>? _positionBuffer;
    private GpuBuffer<Velocity>? _velocityBuffer;

    private static readonly QueryDescriptor Query = QueryDescriptor.Create()
        .With<Position>()
        .With<Velocity>()
        .Without<Frozen>();

    public UpdatePhysicsShader(GpuDevice device)
    {
        _device = device;
        _shader = device.CompileComputeShader(EmbeddedShaders.UpdatePhysics);
    }

    public void Execute(World world, float deltaTime)
    {
        foreach (var archetype in world.QueryArchetypes(Query))
        {
            int count = archetype.EntityCount;
            if (count == 0) continue;

            // Get component arrays (zero-copy span access)
            var positions = archetype.GetComponentSpan<Position>();
            var velocities = archetype.GetComponentSpan<Velocity>();

            // Resize buffers if needed
            EnsureBufferCapacity(ref _positionBuffer, count);
            EnsureBufferCapacity(ref _velocityBuffer, count);

            // Upload
            _positionBuffer!.Upload(positions);
            _velocityBuffer!.Upload(velocities);

            // Dispatch
            var cmd = _device.CreateCommandBuffer();
            cmd.BindComputeShader(_shader);
            cmd.BindBuffer(0, _positionBuffer);
            cmd.BindBuffer(1, _velocityBuffer);
            cmd.SetUniform("deltaTime", deltaTime);
            cmd.SetUniform("entityCount", (uint)count);
            cmd.Dispatch((count + 63) / 64, 1, 1);
            cmd.Execute();

            // Download modified components
            _positionBuffer.Download(positions);
        }
    }

    public void Dispose()
    {
        _positionBuffer?.Dispose();
        _velocityBuffer?.Dispose();
        _shader.Dispose();
    }
}
```

### Error Handling

Compiler errors include source location. The shipped diagnostics pipeline (`editor/KeenEyes.Shaders.Compiler/Diagnostics/`) provides a structured `Diagnostic` type with source spans, a KESL error-code taxonomy, caret-style formatting via `DiagnosticFormatter`, and "did you mean" suggestions via `SuggestionEngine`. Syntax-level errors (KESL1xx/2xx) are emitted today; semantic errors such as the one below require the unimplemented semantic-analysis stage (KESL3xx codes are reserved) and currently surface at GLSL compile or runtime instead:

```
physics.kesl:12:5: error: Cannot write to read-only component 'Velocity'
   12 |     Velocity.x = 0;
      |     ^^^^^^^^^
```

Runtime errors (GPU validation) are surfaced through the graphics abstraction layer.

### Build Integration

Build integration ships through the KeenEyes SDK plus a Roslyn incremental source generator — no external tool invocation (the originally planned `keslc` Exec-based MSBuild target was never built):

**SDK Integration:**
```xml
<!-- In KeenEyes.Sdk (Sdk.targets) — .kesl files feed the source generator -->
<ItemGroup>
  <KeenEyesShader Include="**/*.kesl" />
  <AdditionalFiles Include="**/*.kesl" />
</ItemGroup>
```

`KeslSourceGenerator` (`editor/KeenEyes.Shaders.Generator`, an `IIncrementalGenerator`) compiles the `.kesl` `AdditionalFiles` during Roslyn compilation and injects the generated C# bindings directly into the compilation.

## Implementation Phases

### Phase 1: Prototype ✅ (shipped with the initial prototype, Dec 2025)
- [x] Research document ([docs/research/shader-language.md](../research/shader-language.md))
- [x] Architecture document
- [x] Lexer implementation (`editor/KeenEyes.Shaders.Compiler/Lexing/`)
- [x] Parser implementation (recursive descent, `Parsing/` + `Parsing/Ast/`)
- [x] GLSL code generator (`CodeGen/GlslGenerator.cs`)
- [x] Basic C# binding generator (`CodeGen/CSharpBindingGenerator.cs`)
- [x] Unit tests (`tests/KeenEyes.Shaders.Compiler.Tests`)

### Phase 2: Integration ✅
- [x] `KeenEyes.Shaders` abstractions (`src/KeenEyes.Shaders`)
- [x] MSBuild targets — implemented as SDK-level `.kesl` → `AdditionalFiles` wiring for the source generator, not a standalone Exec target (see Build Integration)
- [x] Hot-reload support (`src/KeenEyes.Shaders/HotReload/` — `KeslFileWatcher`, `ShaderRegistry`, `IHotReloadable`)
- [x] Error message improvements (structured diagnostics with error codes and "did you mean" suggestions)

### Phase 3: Polish ✅
- [x] Source generator integration (`editor/KeenEyes.Shaders.Generator`)
- [x] IDE support — TextMate grammar and snippets (`editor/KeenEyes.Shaders.VsCode`), an LSP server with completion, hover, go-to-definition, and diagnostics (`tools/KeenEyes.Lsp.Kesl`), and a VSCode LSP client extension (`tools/vscode-kesl`)
- [x] HLSL backend (`CodeGen/HlslGenerator.cs`)
- [x] Rendering shader support (vertex/fragment) — plus geometry shaders and pipeline composition

The language has grown beyond the ADR's compute-only scope: vertex, fragment, and geometry shaders plus pipeline composition are supported, and `editor/KeenEyes.Graph.Kesl` adds node-graph authoring for KESL. See [docs/shaders.md](../shaders.md) for the user-facing documentation.

**Not yet implemented:** the semantic-analysis stage (component resolution against ECS metadata, expression type checking, read/write access validation — error codes KESL300–306 are reserved for it), and the `keslc` CLI (superseded by the Roslyn source generator).

## Alternatives Considered

### Option 1: Source Generator Only

Use C# source generators to generate GPU bindings from attribute-annotated code:

```csharp
[GpuCompute]
public partial class UpdatePhysics
{
    [Read] Position position;
    [Write] Velocity velocity;

    public void Execute(float deltaTime) { /* C# code */ }
}
```

**Rejected because:**
- Cannot express GPU-specific operations naturally
- Shader code still written separately
- No unified language for GPU and binding

### Option 2: Embed GLSL in C#

Use string literals with source generator parsing:

```csharp
[GpuCompute(@"
    Position.x += Velocity.x * deltaTime;
")]
public partial class UpdatePhysics { }
```

**Rejected because:**
- No syntax highlighting in strings
- Poor error messages
- Mixes concerns awkwardly

### Option 3: Use Existing Language (WGSL)

Adopt WGSL and generate bindings from it:

**Rejected because:**
- WGSL has no ECS awareness
- Still requires manual binding code
- Limited tooling for .NET

## Consequences

### Positive

- **Reduced boilerplate:** 80% less code for GPU systems
- **Type safety:** Compile-time validation of component access — partially realized: syntax-level diagnostics with source spans and suggestions ship today (KESL1xx/2xx), but compile-time component/type validation (KESL3xx) awaits the semantic-analysis stage
- **Single source of truth:** One file defines GPU behavior and CPU bindings
- **Better error messages:** Domain-specific errors, not generic GPU errors
- **Future extensibility:** Foundation for advanced GPU features

### Negative

- **Learning curve:** New language to learn (mitigated by familiar syntax)
- **Tooling investment:** IDE support, debugging, profiling
- **Maintenance burden:** Custom compiler to maintain
- **Build complexity:** Additional build step

### Neutral

- Existing shader workflows remain supported (KESL is additive)
- Performance equivalent to hand-written shaders
- Integrates with existing graphics abstractions

## References

- [Shader language research document](../research/shader-language.md)
- [KESL user documentation](../shaders.md)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Status corrected Proposed → Accepted (header year fixed 2024 → 2025); Implementation marked Partial — the semantic-analysis stage (component resolution, type checking, read-only-write validation; codes KESL300–306 reserved but never emitted) and the `keslc` CLI (superseded by the Roslyn source generator) never shipped. All Phase 1–3 checklist items checked as shipped; Architecture amended to the as-built layout (compiler under `editor/`, `KeenEyes.Shaders.Generator`, LSP server + VSCode extensions, `Graph.Kesl` node authoring); Build Integration rewritten from the `keslc` Exec target to SDK `AdditionalFiles` + source generator; Decision updated to reflect the shipped HLSL backend.
- **v1 — 2025-12-31 (c877d9e0):** Proposed — KESL, a custom ECS-aware shader language transpiling to GLSL with generated C# bindings, to replace verbose manual ECS-to-GPU marshaling; shipped alongside the prototype lexer, parser, GLSL/C# generators, and 30 unit tests.
