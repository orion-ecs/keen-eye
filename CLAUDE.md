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

## Work Tracking

Agents must track their work to maintain visibility and enable collaboration:

### Todo Lists
- Use `TodoWrite` for any task with 3+ steps
- Mark todos `in_progress` before starting work
- Mark todos `completed` immediately after finishing (don't batch)
- Break complex features into granular, trackable items

### Commit Messages
- Write clear, descriptive commit messages explaining the "why"
- Reference related features or issues when applicable
- Group related changes into logical commits
- Use conventional format: `Add/Update/Fix/Remove <what> - <why>`

### Progress Documentation
- Update ROADMAP.md when completing roadmap items (check the box)
- Note any deviations or design decisions in commit messages
- Document blockers or open questions for future agents

## XML Documentation

All public APIs must have XML documentation comments for API doc generation.

### Required Documentation

**All public types** (classes, structs, interfaces, enums):
```csharp
/// <summary>
/// Brief description of what this type represents.
/// </summary>
/// <remarks>
/// Additional details, usage notes, or examples if needed.
/// </remarks>
public sealed class World : IDisposable
```

**All public members** (methods, properties, fields, events):
```csharp
/// <summary>
/// Creates a new entity with the specified components.
/// </summary>
/// <param name="components">The components to add to the entity.</param>
/// <returns>The created entity handle.</returns>
/// <exception cref="ArgumentNullException">Thrown when components is null.</exception>
public Entity CreateEntity(params IComponent[] components)
```

**Generic type parameters**:
```csharp
/// <summary>
/// Gets a component from an entity.
/// </summary>
/// <typeparam name="T">The component type to retrieve.</typeparam>
/// <param name="entity">The entity to get the component from.</param>
/// <returns>A reference to the component data.</returns>
public ref T Get<T>(Entity entity) where T : struct, IComponent
```

### Documentation Style Guide

1. **Be concise** - First sentence should be a complete summary
2. **Use active voice** - "Creates..." not "This method creates..."
3. **Document behavior** - What it does, not how it's implemented
4. **Include examples** for complex APIs:
```csharp
/// <summary>
/// Builds a query for entities matching the specified component types.
/// </summary>
/// <example>
/// <code>
/// foreach (var entity in world.Query&lt;Position, Velocity&gt;().Without&lt;Frozen&gt;())
/// {
///     // Process moving entities
/// }
/// </code>
/// </example>
```

5. **Document exceptions** that callers should handle
6. **Cross-reference related types** with `<see cref="TypeName"/>`
7. **Mark obsolete APIs** with `[Obsolete]` attribute AND `<remarks>` explaining migration

### What NOT to Document

- Private/internal members (unless complex)
- Self-evident properties (e.g., `public int Count => items.Count;`)
- Generated code (source generators handle this)

### Validation

The build enforces documentation:
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Remove this to enforce -->
</PropertyGroup>
```

Future goal: Enable CS1591 warning to require docs on all public members.
