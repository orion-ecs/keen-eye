# CLAUDE.md

This file contains guidance for Claude Code when working on this repository.

## Project Overview

KeenEyes is a high-performance Entity Component System (ECS) framework for .NET 10, reimplementing [OrionECS](https://github.com/tyevco/OrionECS) in C#.

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
│   ├── KeenEyes.Core/                 # Runtime ECS framework
│   │   ├── Components/               # IComponent, ComponentRegistry
│   │   ├── Entities/                 # Entity, EntityBuilder
│   │   ├── Queries/                  # QueryBuilder, QueryEnumerator
│   │   └── Systems/                  # ISystem, SystemBase
│   ├── KeenEyes.Generators/           # Source generators
│   └── KeenEyes.Generators.Attributes/ # Attributes (netstandard2.0)
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

## World Manager Architecture

The `World` class uses a **facade pattern** with specialized internal managers. This keeps `World` as a thin coordinator (~300-400 lines) while delegating to focused managers.

See [ADR-001](docs/adr/001-world-manager-architecture.md) for the full decision record.

### Current Architecture

```
World (facade)
├── HierarchyManager      - Parent-child entity relationships
├── SystemManager         - System registration, ordering, execution
├── PluginManager         - Plugin lifecycle
├── SingletonManager      - Global resource storage
├── ExtensionManager      - Plugin-provided APIs
├── EntityNamingManager   - Entity name registration and lookup
├── EventManager          - Component and entity lifecycle events
├── ChangeTracker         - Dirty flag tracking with entity reconstruction
├── ArchetypeManager      - Component storage
├── QueryManager          - Query caching
└── ComponentRegistry     - Component type registry
```

### Manager Design Rules

When working with or creating managers:

1. **Managers are `internal`** - Not part of public API; `World` is the only entry point
2. **Single responsibility** - Each manager owns one concern completely
3. **Minimal dependencies** - Managers take only what they need (World ref or specific collaborators)
4. **No circular dependencies** - If A needs B, B cannot need A
5. **Testable in isolation** - Each manager should be unit-testable without full World

### Adding New Functionality

When adding new features to World:

1. **Identify the owner** - Which manager should own this feature?
2. **Implement in manager** - Add the logic to the appropriate manager
3. **Delegate from World** - World methods should be thin pass-throughs
4. **Test the manager** - Write unit tests for the manager directly

**Good:**
```csharp
// In World.cs - thin delegation
public void SetParent(Entity child, Entity parent)
    => hierarchyManager.SetParent(child, parent);

// In HierarchyManager.cs - actual logic
internal void SetParent(Entity child, Entity parent)
{
    // All the validation and state management here
}
```

**Bad:**
```csharp
// In World.cs - logic in World (violates SRP)
public void SetParent(Entity child, Entity parent)
{
    // 50 lines of validation and state management
}
```

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

## Code Quality Requirements

All code contributions must meet strict quality standards. No exceptions.

### Build Validation

Before committing, ensure:

```bash
dotnet build                         # Zero warnings, zero errors (TreatWarningsAsErrors enabled)
dotnet test                          # All tests pass
dotnet format --verify-no-changes   # Code is formatted
```

Note: NuGet dependency warnings (NU1603) may appear during restore due to central
package management pinning higher versions than some packages request. These are
suppressed from being treated as errors via `NoWarn` in Directory.Build.props.

**Non-negotiable rules:**
- All existing tests must continue to pass
- No new warnings introduced (warnings are errors)
- No new analyzer violations
- Code must be formatted per `.editorconfig`

### Introducing Changes

When modifying code:
1. Run the full build before AND after changes
2. If a warning exists, fix it - don't suppress it without justification
3. If a test fails, fix the code or update the test with clear reasoning
4. Never commit code that breaks the build

### No Speculative Features

Do not add speculative or "future nice to have" code unless specifically requested in an issue:
- Don't add unused methods "in case they're useful later"
- Don't add extra parameters or overloads for hypothetical use cases
- Don't add internal APIs that aren't immediately consumed
- If a feature isn't in the requirements, don't implement it

Speculative code creates:
- **Maintenance burden** - Code that must be tested, documented, and maintained
- **Confusion** - Future developers wonder why unused code exists
- **Tech debt** - Unused abstractions that complicate refactoring

When you think "this might be useful later," stop. Either it's needed now (add it), or it's not (don't add it). YAGNI (You Aren't Gonna Need It).

### Suppressing Warnings

Only suppress warnings when absolutely necessary:

```csharp
// GOOD: Documented justification
#pragma warning disable CS8618 // Non-nullable field not initialized - set in Initialize()
private World world;
#pragma warning restore CS8618

// BAD: No explanation
#pragma warning disable CS8618
private World world;
#pragma warning restore CS8618
```

## Code Coverage Requirements

Code coverage is enforced by CI. If coverage drops below the current baseline, the build fails.

### Running Coverage Locally

Use xUnit.net v3 with Microsoft Testing Platform (MTP) to generate coverage reports:

```bash
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.xml
```

This generates Cobertura XML reports in each test project's `bin/Debug/net10.0/TestResults/` directory.

### Coverage Rules

1. **Coverage must not decrease** - CI fails if any PR reduces overall coverage percentage
2. **New code must be covered** - All new functionality requires corresponding tests
3. **Edge cases matter** - Cover error paths, boundary conditions, and exceptional flows
4. **No artificial inflation** - Tests must verify behavior, not just execute code

### Test Quality Standards

Tests are as important as production code. Every test must be:

**Meaningful:**
- Test actual behavior, not implementation details
- Verify outcomes, not just that code runs without throwing
- Cover both happy paths and error conditions

**Well-named:**
- Use pattern: `MethodName_Scenario_ExpectedResult`
- Names should describe what is being tested
- A failing test's name should indicate what went wrong

```csharp
// ✅ GOOD: Clear, descriptive names
[Fact]
public void Despawn_WithValidEntity_RemovesEntityFromWorld()

[Fact]
public void Get_WithDeadEntity_ThrowsInvalidOperationException()

[Fact]
public void Query_WithNoMatchingArchetypes_ReturnsEmptyEnumerable()

// ❌ BAD: Vague, unclear names
[Fact]
public void TestDespawn()

[Fact]
public void GetThrows()

[Fact]
public void QueryWorks()
```

**Properly organized:**
- Group related tests using `#region` or separate test classes
- One test class per unit under test (e.g., `WorldTests`, `QueryEnumeratorTests`)
- Use nested classes or regions for method-specific tests when appropriate

```csharp
public class WorldTests
{
    #region Spawn Tests
    [Fact]
    public void Spawn_CreatesEntity() { ... }

    [Fact]
    public void Spawn_WithComponents_AddsComponents() { ... }
    #endregion

    #region Despawn Tests
    [Fact]
    public void Despawn_RemovesEntity() { ... }
    #endregion
}
```

**Self-contained:**
- Each test should be independent (no shared mutable state)
- Use `using` with `World` instances for proper cleanup
- Create fresh test data in each test

```csharp
// ✅ GOOD: Self-contained, isolated test
[Fact]
public void Spawn_CreatesEntityWithUniqueId()
{
    using var world = new World();

    var entity1 = world.Spawn().Build();
    var entity2 = world.Spawn().Build();

    Assert.NotEqual(entity1.Id, entity2.Id);
}
```

## Sample & Tutorial Code Quality

**All code is teaching code.** Samples, tutorials, examples, and documentation snippets are as important as production code - often more so.

### Why This Matters

- Users learn by copying examples
- Bad habits in samples become bad habits in user code
- First impressions establish patterns that persist
- Examples are the most-read code in any project

### Sample Code Standards

Every code sample must:

1. **Compile and run** - No pseudo-code in samples
2. **Follow all conventions** - Same standards as production code
3. **Demonstrate best practices** - Show the RIGHT way, not shortcuts
4. **Be self-contained** - Runnable without hidden dependencies
5. **Include error handling** - Show proper patterns, not happy-path only
6. **Use meaningful names** - `Position`, `Velocity`, not `Foo`, `Bar`

### Example Quality Checklist

```csharp
// ✅ GOOD: Complete, follows conventions, demonstrates patterns
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

public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}
```

```csharp
// ❌ BAD: Incomplete, bad names, missing patterns
public class MySystem : SystemBase
{
    public override void Update(float dt)
    {
        // TODO: implement
        foreach (var e in World.Query<Foo>())
        {
            // do stuff
        }
    }
}
```

### Establish Patterns Early

When writing examples, reinforce core patterns:
- Show `readonly record struct` for entities
- Show `struct` for components
- Show proper `ref` usage for zero-copy access
- Show `using` statements for disposables
- Show query patterns with `With<>()` and `Without<>()`

## ECS Principles

When implementing features, always keep ECS fundamentals in mind.

### Core ECS Tenets

1. **Composition over Inheritance**
   - Entities are IDs, not objects
   - Behavior comes from component combinations
   - No entity base classes or inheritance hierarchies

2. **Data-Oriented Design**
   - Components are plain data (structs)
   - Systems contain logic, components contain state
   - Optimize for cache locality and iteration

3. **Separation of Concerns**
   - Components: WHAT an entity has (data only)
   - Systems: HOW entities behave (logic only)
   - Queries: WHICH entities to process (filtering)

4. **Explicit over Implicit**
   - No hidden registration or auto-wiring
   - Users explicitly add systems to worlds
   - No magic - behavior is traceable

### Anti-Patterns to Avoid

```csharp
// ❌ BAD: Logic in components
public struct Health
{
    public int Current;
    public int Max;

    public void TakeDamage(int amount) => Current -= amount;  // NO!
}

// ✅ GOOD: Pure data component
public struct Health
{
    public int Current;
    public int Max;
}

// Logic in systems
public class DamageSystem : SystemBase { ... }
```

```csharp
// ❌ BAD: Inheritance-based entities
public class Enemy : Entity { }       // NO!
public class Player : Enemy { }       // NO!

// ✅ GOOD: Composition-based
var enemy = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .WithTag<EnemyTag>()
    .Build();
```

```csharp
// ❌ BAD: God component with everything
public struct Actor
{
    public float X, Y;
    public float VelX, VelY;
    public int Health, MaxHealth;
    public int Damage;
    public string Name;
    // ... 50 more fields
}

// ✅ GOOD: Small, focused components
public struct Position { public float X, Y; }
public struct Velocity { public float X, Y; }
public struct Health { public int Current, Max; }
public struct Damage { public int Amount; }
public struct Named { public string Name; }
```

### Performance Mindset

Always consider:
- **Allocations**: Minimize heap allocations in hot paths
- **Cache locality**: Keep related data together
- **Iteration**: Optimize for bulk processing, not individual entity access
- **Indirection**: Reduce pointer chasing, prefer contiguous arrays

### When in Doubt

Ask these questions:
1. Is this component pure data with no logic?
2. Is this system processing entities in bulk?
3. Could this be composed from smaller components?
4. Am I relying on implicit behavior?
5. Would this work with 10,000 entities?

## Claude Code Web Sessions

Claude Code web environments have proxy restrictions that prevent NuGet from authenticating properly. A SessionStart hook automatically handles this.

### How It Works

1. **SessionStart hook** (`.claude/hooks/install-packages.sh`) runs automatically on web sessions
2. Downloads packages via `wget` (which handles proxy auth correctly)
3. Extracts packages to `~/.nuget/packages/` (global cache)
4. Copies `.nupkg` files to `/tmp/nuget-feed/` (local source)
5. **PreToolUse hook** (`.claude/hooks/dotnet-pre.sh`) disables `nuget.org` before any `dotnet` command
6. **PostToolUse hook** (`.claude/hooks/dotnet-post.sh`) re-enables `nuget.org` after (keeps config clean for commits)

### If Restore Fails with Missing Packages

Run the install script manually and check for failures:

```bash
CLAUDE_CODE_REMOTE=true .claude/hooks/install-packages.sh
```

Then add missing packages to the script. Common causes:
- New package added to a `.csproj`
- Version updated in `Directory.Packages.props`
- New transitive dependency introduced

### Maintaining the Package List

The package list in `.claude/hooks/install-packages.sh` must be kept in sync with project dependencies:

**When adding packages:**
1. Add the package reference to the project
2. Run `dotnet restore` locally to verify it works
3. Add the package (and any new transitive dependencies) to `install-packages.sh`
4. Test on web: `CLAUDE_CODE_REMOTE=true .claude/hooks/install-packages.sh`

**Finding transitive dependencies:**
```bash
# Run restore and note missing packages from errors
dotnet restore 2>&1 | grep "Unable to find package"

# Or check the package's dependencies on nuget.org
```

**Package list structure in the script:**
```bash
# Direct dependencies (from Directory.Packages.props)
download_pkg "PackageName" "Version"

# Transitive dependencies (required by direct deps)
download_pkg "TransitivePackage" "Version"
```

Keep the script organized with comments grouping related packages.
