# Native AOT Deployment Guide

KeenEyes is fully compatible with .NET Native AOT, allowing you to compile your ECS applications into self-contained native executables with no JIT compilation required.

## Benefits of Native AOT

- **Faster startup** - No JIT warmup; code is pre-compiled to native
- **Smaller memory footprint** - No JIT compiler in memory
- **Self-contained deployment** - Single executable, no .NET runtime required
- **Improved security** - Reduced attack surface, no dynamic code generation

## Quick Start

### 1. Create an AOT-enabled project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="KeenEyes.Core" Version="*" />
    <PackageReference Include="KeenEyes.Generators" Version="*" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

### 2. Define components with source generators

Source generators are essential for AOT - they eliminate reflection:

```csharp
using KeenEyes.Generators.Attributes;

[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

[Component]
public partial struct Velocity
{
    public float X;
    public float Y;
}

[TagComponent]
public partial struct EnemyTag { }
```

### 3. Write AOT-safe code

```csharp
using KeenEyes.Core;

// Use WorldBuilder with explicit component registration
using var world = new WorldBuilder()
    .WithComponent<Position>()
    .WithComponent<Velocity>()
    .WithTagComponent<EnemyTag>()
    .Build();

// Spawn entities
var entity = world.Spawn()
    .WithPosition(10, 20)
    .WithVelocity(1, 0)
    .WithEnemyTag()
    .Build();

// Query and update
foreach (var e in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(e);
    ref readonly var vel = ref world.Get<Velocity>(e);
    pos.X += vel.X;
    pos.Y += vel.Y;
}
```

### 4. Publish as Native AOT

```bash
# Publish for current platform
dotnet publish -c Release

# Publish for specific platform
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r osx-arm64
```

## Platform Support

| Platform | Runtime Identifier | Notes |
|----------|-------------------|-------|
| Linux x64 | `linux-x64` | Most common for servers |
| Linux ARM64 | `linux-arm64` | Raspberry Pi, cloud ARM instances |
| Windows x64 | `win-x64` | Windows desktop/server |
| Windows ARM64 | `win-arm64` | Windows on ARM devices |
| macOS x64 | `osx-x64` | Intel Macs |
| macOS ARM64 | `osx-arm64` | Apple Silicon (M1/M2/M3) |

## Best Practices

### Use Source Generators

All KeenEyes source generators produce AOT-compatible code:

- `[Component]` - Generates fluent builder methods
- `[TagComponent]` - Generates parameterless tag methods
- `[System]` - Generates system metadata
- `[Query]` - Generates efficient query iterators
- `[Serializable]` - Generates JSON serialization context

### Avoid Reflection Patterns

```csharp
// BAD: Reflection-based (fails in AOT)
var componentType = Type.GetType("MyGame.Position");
var component = Activator.CreateInstance(componentType);

// GOOD: Direct instantiation
var component = new Position { X = 0, Y = 0 };

// GOOD: Factory delegate (registered at startup)
Func<Position> factory = () => new Position();
```

### Register Components Explicitly

```csharp
// GOOD: Explicit registration with WorldBuilder
var world = new WorldBuilder()
    .WithComponent<Position>()
    .WithComponent<Velocity>()
    .Build();

// BAD: Dynamic registration from unknown types
// world.Components.Register(unknownType); // May fail in AOT
```

### Use InvariantGlobalization

For smaller binaries and predictable behavior:

```xml
<PropertyGroup>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

## Troubleshooting

### Build Warnings

Enable AOT analyzers to catch issues at build time:

```xml
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

Common warnings and fixes:

| Warning | Cause | Fix |
|---------|-------|-----|
| IL2026 | Reflection usage | Use source generators or explicit types |
| IL2057 | Unrecognized type | Register type explicitly at startup |
| IL2070 | Generic MakeGenericType | Use factory delegates instead |
| IL3050 | RequiresDynamicCode | Avoid or use compile-time alternatives |

### Runtime Errors

If your AOT app crashes at runtime:

1. **Missing type metadata** - Ensure all components are registered with `WorldBuilder`
2. **Serialization issues** - Use `[Serializable]` attribute for JSON support
3. **Plugin loading** - Plugins must be AOT-compiled and linked at build time

### Binary Size

To reduce native executable size:

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

## Sample Project

See the complete working example at [`samples/KeenEyes.Sample.Aot/`](../samples/KeenEyes.Sample.Aot/).

To build and run:

```bash
cd samples/KeenEyes.Sample.Aot
dotnet publish -c Release -r linux-x64
./bin/Release/net10.0/linux-x64/publish/KeenEyes.Sample.Aot
```

## Further Reading

- [.NET Native AOT Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Trimming and AOT Warnings](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/fixing-warnings)
- [ADR-004: Reflection Elimination](adr/004-reflection-elimination.md) - Technical details on AOT-safe patterns
