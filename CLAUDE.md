# CLAUDE.md

This file contains guidance for Claude Code when working on this repository.

## Project Overview

KeenEye is a high-performance Entity Component System (ECS) framework for .NET 10, reimplementing [OrionECS](https://github.com/tyevco/OrionECS) in C#.

## Architecture Principles

### No Static State

All state must be instance-based. Each `World` is completely isolated with its own:
- Component registry (component IDs are per-world)
- Entity storage
- Systems

This enables:
- Multiple independent ECS worlds in one process
- Easy testing with isolated worlds
- No hidden initialization order dependencies

**Bad:**
```csharp
public static class ComponentRegistry
{
    private static readonly Dictionary<Type, int> ids = new();  // NO!
}
```

**Good:**
```csharp
public sealed class World
{
    public ComponentRegistry Components { get; } = new();  // Per-world
}
```

### Respect User Intent

Don't generate code that assumes how users will structure their application:
- Don't auto-register all systems (users may want different systems per world)
- Don't force global singletons
- Provide metadata and helpers, but let users wire things up explicitly

**Bad:**
```csharp
// Generated - forces all systems into every world
public static World RegisterAll(this World world) { ... }
```

**Good:**
```csharp
// Generated - provides metadata, user decides registration
public partial class MovementSystem
{
    public static SystemPhase Phase => SystemPhase.Update;
    public static int Order => 0;
}
```

### Source Generators for Ergonomics

Use Roslyn source generators to reduce boilerplate while maintaining performance:
- `[Component]` → generates `WithComponentName()` fluent builder methods
- `[TagComponent]` → generates parameterless tag methods
- `[System]` → generates metadata properties (Phase, Order, Group)
- `[Query]` → generates efficient query iterators

## Project Structure

```
keen-eye/
├── src/
│   ├── KeenEye.Core/                 # Runtime ECS framework
│   │   ├── Components/               # IComponent, ComponentRegistry
│   │   ├── Entities/                 # Entity, EntityBuilder
│   │   ├── Queries/                  # QueryBuilder, QueryEnumerator
│   │   └── Systems/                  # ISystem, SystemBase
│   ├── KeenEye.Generators/           # Source generators
│   └── KeenEye.Generators.Attributes/ # Attributes (netstandard2.0)
├── tests/                            # Unit and integration tests
├── samples/                          # Example projects
├── benchmarks/                       # Performance benchmarks
└── docs/                             # Documentation
```

## Coding Conventions

- Target .NET 10 with C# 13+ features
- Use file-scoped namespaces
- Prefer `readonly record struct` for small value types
- Use `ref` returns for component access (zero-copy)
- Central package management via `Directory.Packages.props`
- Nullable reference types enabled everywhere
- Treat warnings as errors

## Build Commands

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes  # Check formatting
```

## Key Design Decisions

1. **Components are structs** - Cache-friendly, value semantics
2. **Entities are IDs** - Just `(int Id, int Version)` for staleness detection
3. **Queries are fluent** - `world.Query<A, B>().With<C>().Without<D>()`
4. **Systems are explicit** - User registers what they need per-world
5. **Builders are generated** - `WithPosition(x, y)` instead of `With(new Position{...})`
