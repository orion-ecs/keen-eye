# ADR-009: KESL - KeenEyes Shader Language

**Status:** Proposed
**Date:** 2024-12-31

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
2. Transpiles to GLSL (with future HLSL/SPIR-V support)
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
├── KeenEyes.Shaders/                    # Core abstractions
│   ├── IGpuComputeSystem.cs             # Interface for GPU systems
│   ├── GpuBuffer.cs                     # Buffer abstraction
│   └── GpuQueryExtensions.cs            # World extensions
├── KeenEyes.Shaders.Compiler/           # Compiler library
│   ├── Lexer/
│   │   ├── Token.cs
│   │   ├── TokenKind.cs
│   │   └── Lexer.cs
│   ├── Parser/
│   │   ├── Ast/                         # AST node types
│   │   └── Parser.cs
│   ├── Semantics/
│   │   ├── TypeChecker.cs
│   │   └── SymbolTable.cs
│   └── CodeGen/
│       ├── GlslGenerator.cs
│       └── CSharpBindingGenerator.cs
└── KeenEyes.Shaders.Tools/              # CLI tool (keslc)
    └── Program.cs
```

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
                         ▼
┌────────────────────────────────────────────────┐
│              Semantic Analysis                  │
│  - Resolve component types                     │
│  - Type check expressions                      │
│  - Validate GPU compatibility                  │
└────────────────────────────────────────────────┘
                         │
            ┌────────────┴────────────┐
            ▼                         ▼
┌────────────────────────┐ ┌─────────────────────┐
│    GLSL Generator      │ │  C# Binding Gen     │
└────────────────────────┘ └─────────────────────┘
            │                         │
            ▼                         ▼
      Foo.comp.glsl          FooShader.g.cs
```

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

KESL references components by name. The compiler resolves these against registered component types:

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

Compiler errors include source location:

```
physics.kesl:12:5: error: Cannot write to read-only component 'Velocity'
   12 |     Velocity.x = 0;
      |     ^^^^^^^^^
```

Runtime errors (GPU validation) are surfaced through the graphics abstraction layer.

### Build Integration

**MSBuild Target:**
```xml
<Target Name="CompileKesl" BeforeTargets="CoreCompile">
  <ItemGroup>
    <KeslFile Include="**/*.kesl" />
  </ItemGroup>

  <Exec Command="keslc @(KeslFile) -o $(IntermediateOutputPath)kesl/"
        Condition="'@(KeslFile)' != ''" />

  <ItemGroup>
    <Compile Include="$(IntermediateOutputPath)kesl/*.g.cs" />
    <EmbeddedResource Include="$(IntermediateOutputPath)kesl/*.glsl" />
  </ItemGroup>
</Target>
```

**SDK Integration:**
```xml
<!-- In KeenEyes.Sdk.targets -->
<ItemGroup>
  <KeenEyesShader Include="**/*.kesl" />
</ItemGroup>
```

## Implementation Phases

### Phase 1: Prototype (This PR)
- [x] Research document
- [x] Architecture document
- [ ] Lexer implementation
- [ ] Parser implementation
- [ ] GLSL code generator
- [ ] Basic C# binding generator
- [ ] Unit tests

### Phase 2: Integration
- [ ] `KeenEyes.Shaders` abstractions
- [ ] MSBuild targets
- [ ] Hot-reload support
- [ ] Error message improvements

### Phase 3: Polish
- [ ] Source generator integration
- [ ] IDE support (syntax highlighting)
- [ ] HLSL backend
- [ ] Rendering shader support (vertex/fragment)

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
- **Type safety:** Compile-time validation of component access
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
