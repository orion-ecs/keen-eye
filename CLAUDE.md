# CLAUDE.md

This file contains guidance for Claude Code when working on this repository.

For contribution guidelines, design philosophy, and PR requirements, see [CONTRIBUTING.md](CONTRIBUTING.md).

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

### No Reflection in Production Code

**All production code must be Native AOT compatible.** Do not use reflection in `KeenEyes.Core` or other runtime libraries.

**Prohibited patterns:**
- `Type.GetField()`, `Type.GetMethod()`, `Type.GetProperty()`
- `Type.GetFields()`, `Type.GetMethods()`, `Type.GetProperties()`
- `Activator.CreateInstance()` for dynamic object creation
- `System.Reflection.BindingFlags`
- Assembly scanning or type discovery at runtime

**AOT-compatible alternatives:**

1. **Factory delegates** instead of `Activator.CreateInstance()`:
```csharp
// ❌ BAD: Reflection-based creation
var component = Activator.CreateInstance(componentType);

// ✅ GOOD: Factory delegate stored at registration
public delegate object ComponentFactory();
var component = componentInfo.Factory();
```

2. **Static abstract interface members** instead of reflection on static fields:
```csharp
// ❌ BAD: Reflection to access static field
var field = typeof(TBundle).GetField("ComponentTypes", BindingFlags.Static);
var types = (Type[])field.GetValue(null);

// ✅ GOOD: Static abstract interface member
public interface IBundle
{
    static abstract Type[] ComponentTypes { get; }
}
var types = TBundle.ComponentTypes;  // Direct access, no reflection
```

3. **Source generators** for type metadata:
```csharp
// ❌ BAD: Runtime attribute reading
var attrs = componentType.GetCustomAttributes<RequiresComponentAttribute>();

// ✅ GOOD: Generated metadata lookup (per-world)
var world = new World();
world.ValidationManager.RegisterConstraintProvider(
    ComponentValidationMetadata.TryGetConstraints);
```

4. **Explicit registration** instead of assembly scanning:
```csharp
// ❌ BAD: Assembly scanning
var components = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(IComponent).IsAssignableFrom(t));

// ✅ GOOD: Explicit registration or generated registry
world.Components.Register<Position>();
// Or: use generated ComponentRegistry with all known components
```

**Exceptions:**
- Test code may use reflection for test infrastructure
- `object.GetType()` on existing instances is allowed (not assembly scanning)
- Debug/diagnostic code with `#if DEBUG` guards

When adding new features, always ask: "Would this work with Native AOT compilation?"

## Project Structure

```
keen-eye/
├── src/                              # Runtime libraries
│   ├── KeenEyes.Abstractions/        # Core interfaces, attributes, contracts
│   ├── KeenEyes.Common/              # Shared utilities (FloatExtensions, etc.)
│   ├── KeenEyes.Core/                # ECS framework (World, Entity, Components, Systems)
│   │   ├── Archetypes/               # Archetype storage and management
│   │   ├── Components/               # ComponentRegistry, validation
│   │   ├── Entities/                 # Entity, EntityBuilder
│   │   ├── Events/                   # ChangeTracker, lifecycle events
│   │   ├── Queries/                  # QueryBuilder, QueryManager
│   │   ├── Serialization/            # SnapshotManager, serializers
│   │   └── Systems/                  # ISystem, SystemBase
│   ├── KeenEyes.Generators/          # Source generators (also in editor/)
│   ├── KeenEyes.AI/                  # AI behaviors (behavior trees, GOAP, etc.)
│   ├── KeenEyes.Animation/           # Animation system
│   ├── KeenEyes.Assets/              # Asset management
│   ├── KeenEyes.Audio*/              # Audio system (Abstractions, Silk impl)
│   ├── KeenEyes.Debugging/           # Debug tooling
│   ├── KeenEyes.Graph*/              # Node graph system (Abstractions, KESL)
│   ├── KeenEyes.Graphics*/           # Graphics system (Abstractions, Silk impl)
│   ├── KeenEyes.Input*/              # Input system (Abstractions, Silk impl)
│   ├── KeenEyes.Localization/        # Localization support
│   ├── KeenEyes.Logging/             # Logging infrastructure
│   ├── KeenEyes.Navigation*/         # Pathfinding (Abstractions, DotRecast, Grid)
│   ├── KeenEyes.Network*/            # Networking (Abstractions, TCP, UDP)
│   ├── KeenEyes.Parallelism/         # Parallel job system
│   ├── KeenEyes.Particles/           # Particle system
│   ├── KeenEyes.Persistence/         # Save/load infrastructure
│   ├── KeenEyes.Physics/             # Physics simulation
│   ├── KeenEyes.Platform.Silk*/      # Platform abstraction (Silk.NET)
│   ├── KeenEyes.Replay/              # Replay recording/playback
│   ├── KeenEyes.Runtime/             # Game runtime/loop
│   ├── KeenEyes.Spatial/             # Spatial partitioning
│   ├── KeenEyes.Testing/             # Test utilities
│   └── KeenEyes.UI*/                 # UI system (Abstractions, impl)
├── editor/                           # Build-time and editor tooling
│   ├── KeenEyes.Cli/                 # Command-line interface
│   ├── KeenEyes.Editor*/             # Editor application (Abstractions, Common)
│   ├── KeenEyes.Generators/          # Source generators (shared with src/)
│   ├── KeenEyes.Graph.Kesl/          # KESL graph language support
│   ├── KeenEyes.Sdk/                 # MSBuild SDK for games
│   ├── KeenEyes.Sdk.Plugin/          # MSBuild SDK for plugins
│   ├── KeenEyes.Sdk.Library/         # MSBuild SDK for libraries
│   └── KeenEyes.Shaders.*/           # KESL shader compiler and generator
├── tests/                            # Unit and integration tests
├── samples/                          # Example projects (20+ samples)
├── templates/                        # dotnet new templates
├── benchmarks/                       # Performance benchmarks
└── docs/                             # Documentation and ADRs
```

Note: `*` indicates multiple related packages (e.g., `KeenEyes.Audio*` = `Audio`, `Audio.Abstractions`, `Audio.Silk`).

## Custom MSBuild SDK

KeenEyes provides custom MSBuild SDK packages that simplify project setup for consumers and enable future editor integration.

### Why Custom SDKs?

1. **Minimal boilerplate** - New projects need only the SDK reference
2. **Convention enforcement** - C# 13, nullable, AOT enabled by default
3. **Version coupling** - SDK version implies compatible package versions
4. **Editor foundation** - Project detection, metadata, custom item types
5. **Asset pipeline ready** - Custom ItemGroups for build-time processing

### SDK Packages

| Package | Use Case | Location |
|---------|----------|----------|
| `KeenEyes.Sdk` | Games/apps | `editor/KeenEyes.Sdk/` |
| `KeenEyes.Sdk.Plugin` | Plugin libraries | `editor/KeenEyes.Sdk.Plugin/` |
| `KeenEyes.Sdk.Library` | Reusable ECS libraries | `editor/KeenEyes.Sdk.Library/` |

### Usage

External consumers use the SDK like this:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
  <!-- Everything configured automatically -->
</Project>
```

This replaces 15+ lines of boilerplate (TFM, LangVersion, package references, etc.).

### Custom ItemGroup Types

The SDK defines item types for future editor integration:

- `<KeenEyesScene>` - Scene files (`.kescene`)
- `<KeenEyesPrefab>` - Prefab definitions (`.keprefab`)
- `<KeenEyesAsset>` - Game assets (auto-copied to output)
- `<KeenEyesWorld>` - World configuration (`.keworld`)

### Project Metadata

Each build generates `keeneyes.project.json` in the output directory with version info, enabling:
- Editor project detection
- Version compatibility checking
- Upgrade path recommendations

### Internal vs External Usage

- **External consumers**: Use `<Project Sdk="KeenEyes.Sdk/0.1.0">`
- **Monorepo samples**: Continue using `ProjectReference` (not SDK) for local development
- **Templates**: Updated to use SDK for accurate external consumer experience

See [ADR-006](docs/adr/006-custom-msbuild-sdk.md) and [SDK Documentation](docs/sdk.md) for details.

## Coding Conventions

- Target .NET 10 with C# 13+ features
- Use file-scoped namespaces
- Prefer `readonly record struct` for small value types
- Use `ref` returns for component access (zero-copy)
- Central package management via `Directory.Packages.props`
- Nullable reference types enabled everywhere
- Treat warnings as errors

### Naming Conventions

The project enforces strict naming conventions via `.editorconfig`:

| Symbol Type | Convention | Example |
|-------------|------------|---------|
| Interfaces | `I` prefix + PascalCase | `IWorld`, `IComponent` |
| Classes, Structs, Enums | PascalCase | `World`, `Entity`, `LogLevel` |
| Methods, Properties, Events | PascalCase | `Spawn()`, `EntityCount`, `OnChanged` |
| Private fields | camelCase (no underscore) | `entityCount`, `components` |
| Constants | PascalCase | `MaxEntities`, `DefaultCapacity` |
| Local variables | camelCase | `entity`, `componentData` |
| Parameters | camelCase | `world`, `deltaTime` |

**Important:** Private fields do NOT use underscore prefix. This is enforced by the formatter.

```csharp
// ❌ BAD: Underscore prefix
private readonly Dictionary<int, Entity> _entities;

// ✅ GOOD: camelCase without underscore
private readonly Dictionary<int, Entity> entities;
```

### Code Style

The following style rules are enforced:

```csharp
// File-scoped namespaces (required)
namespace KeenEyes.Core;

// Braces required for all control flow
if (condition)
{
    DoSomething();
}

// Allman brace style (new line before opening brace)
public void Method()
{
    // ...
}

// var preferred when type is apparent
var entity = world.Spawn().Build();
var components = new Dictionary<int, IComponent>();

// Expression-bodied members for simple cases
public int Count => entities.Count;
public Entity Get(int id) => entities[id];

// Pattern matching preferred
if (obj is Entity entity)
{
    // use entity
}

// System usings first, then others alphabetically
using System;
using System.Collections.Generic;
using KeenEyes.Core;
```

### Formatting Commands

Before committing, ensure code is properly formatted:

```bash
dotnet format                        # Auto-fix formatting issues
dotnet format --verify-no-changes    # Check without modifying (CI mode)
```

### Floating-Point Comparisons

**Never use `==` to compare floats.** Due to floating-point precision limitations, direct equality checks are unreliable.

Use the extension methods from `KeenEyes.Common.FloatExtensions`:

```csharp
using KeenEyes.Common;

// ❌ BAD: Direct equality (unreliable)
if (activity.SleepThreshold == 0)

// ✅ GOOD: Tolerance-based comparison
if (activity.SleepThreshold.IsApproximatelyZero())

// ❌ BAD: Direct comparison
if (valueA == valueB)

// ✅ GOOD: Approximate equality
if (valueA.ApproximatelyEquals(valueB))
```

**Available methods:**
| Method | Description |
|--------|-------------|
| `IsApproximatelyZero()` | Check if value is close to zero (epsilon: 1e-6f) |
| `IsApproximatelyZero(epsilon)` | Check with custom tolerance |
| `ApproximatelyEquals(other)` | Compare two floats for near-equality |
| `ApproximatelyEquals(other, epsilon)` | Compare with custom tolerance |

The default epsilon (1e-6f) is suitable for most game development scenarios. Use custom epsilon for specific precision requirements.

### C# 13 Extension Members

This project uses C# 13 **extension members** (not to be confused with traditional extension methods). Extension members allow adding properties directly to types:

```csharp
public static class WorldPluginExtensions
{
    extension(global::KeenEyes.IWorld world)
    {
        public MyExtension MyProperty => world.GetExtension<MyExtension>();
    }
}
```

**Note for code review agents**: The `extension(Type paramName)` syntax is **valid C# 13/14 syntax**, not an error. It generates implicit extension members that appear as instance members on the extended type.

## Build Commands

```bash
dotnet restore
dotnet build
dotnet test --max-parallel-test-modules 1
dotnet format --verify-no-changes  # Check formatting
```

**Important:** Always use `--max-parallel-test-modules 1` when running tests. This prevents test assemblies from running in parallel, which causes ThreadPool contention and intermittent hangs in the Parallelism, Network, and Debugging test suites.

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
├── HierarchyManager           - Parent-child entity relationships
├── SystemManager              - System registration, ordering, execution
├── SystemHookManager          - Before/after system execution hooks
├── PluginManager              - Plugin lifecycle
├── SingletonManager           - Global resource storage
├── ExtensionManager           - Plugin-provided APIs
├── EntityNamingManager        - Entity name registration and lookup
├── EventManager               - Component and entity lifecycle events
├── MessageManager             - Inter-system messaging
├── TagManager                 - String-based entity tagging
├── ChangeTracker              - Dirty flag tracking with entity reconstruction
├── ArchetypeManager           - Component storage
├── QueryManager               - Query caching
├── ComponentRegistry          - Component type registry
├── ComponentValidationManager - Component constraint enforcement
├── PrefabManager              - Entity prefab templates
├── SaveManager                - World persistence orchestration
├── SnapshotManager            - World state serialization (static utility class)
├── SceneManager               - Scene loading and management
├── StatisticsManager          - Memory and performance stats
└── ComponentArrayPoolManager  - Component array pooling
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
- Use Conventional Commits format: `<type>: <description>`

### Pull Request Titles

PR titles **must** follow [Conventional Commits](https://www.conventionalcommits.org/) format. This is enforced by CI.

**Format:** `<type>: <description>`

**Valid types:**

| Type | Use For |
|------|---------|
| `feat` | New feature or functionality |
| `fix` | Bug fix |
| `docs` | Documentation only changes |
| `chore` | Maintenance tasks, dependencies |
| `refactor` | Code restructuring without behavior change |
| `test` | Adding or updating tests |
| `ci` | CI/CD configuration changes |
| `perf` | Performance improvements |
| `style` | Code formatting, no logic change |
| `build` | Build system or dependency changes |

**Examples:**
```
feat: Add ComponentIntrospector for reflection-based inspection
fix: Resolve entity despawn race condition in HierarchyManager
docs: Update getting started guide with new API
refactor: Extract SystemManager from World class
test: Add integration tests for prefab spawning
```

**With optional scope:**
```
feat(editor): Add PropertyDrawer system
fix(physics): Correct collision detection for rotated boxes
```

### Pull Request Description

Use this template for PR descriptions:

```markdown
## Summary
<1-3 bullet points explaining what changed and why>

## Test plan
- [ ] Unit tests added/updated
- [ ] Manual testing performed
- [ ] Build passes with zero warnings

## Related Issues
Closes #XXX
```

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

### No Backwards Compatibility Layers

This is a **new engine** - there is no existing user base requiring backwards compatibility. Do not add:
- Legacy overloads or fallback methods
- Compatibility shims for "old" APIs
- Multiple code paths to support "previous versions"
- Deprecated alternatives alongside new implementations

If you believe backwards compatibility is genuinely needed for a specific case, **ask the user first** before implementing it. This falls under the "no speculative features" rule - don't add compatibility code "just in case."

**Bad:**
```csharp
// Don't add legacy overloads
public static byte[] ToBinary(WorldSnapshot snapshot, IBinaryComponentSerializer serializer)
{
    // Legacy fallback path...
}
```

**Good:**
```csharp
// Single, clean implementation
public static byte[] ToBinary<TSerializer>(WorldSnapshot snapshot, TSerializer serializer)
    where TSerializer : IComponentSerializer, IBinaryComponentSerializer
{
    // Native binary serialization
}
```

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

## System Hooks

Global system hooks enable cross-cutting concerns like profiling, logging, and metrics collection without modifying individual systems.

### Hook Patterns

**Profiling:**
```csharp
var profiler = new Dictionary<string, float>();
var hookSub = world.AddSystemHook(
    beforeHook: (system, dt) => /* Start timer */,
    afterHook: (system, dt) => profiler[system.GetType().Name] = /* elapsed time */
);
```

**Logging:**
```csharp
var hookSub = world.AddSystemHook(
    beforeHook: (system, dt) => logger.Debug($"Starting {system.GetType().Name}"),
    afterHook: (system, dt) => logger.Debug($"Finished {system.GetType().Name}")
);
```

**Conditional Execution:**
```csharp
var hookSub = world.AddSystemHook(
    beforeHook: (system, dt) =>
    {
        if (system is IDebugSystem && !debugMode)
            system.Enabled = false;
    },
    afterHook: null
);
```

**Phase Filtering:**
```csharp
// Hook only executes for systems in the FixedUpdate phase
var hookSub = world.AddSystemHook(
    beforeHook: (system, dt) => /* ... */,
    afterHook: null,
    phase: SystemPhase.FixedUpdate
);
```

### Plugin Integration

Plugins can register hooks during installation and clean them up during uninstall:

```csharp
public class ProfilingPlugin : IWorldPlugin
{
    public string Name => "Profiling";
    private EventSubscription? hookSubscription;

    public void Install(IPluginContext context)
    {
        hookSubscription = context.World.AddSystemHook(
            beforeHook: (system, dt) => /* Start profiling */,
            afterHook: (system, dt) => /* End profiling */
        );
    }

    public void Uninstall(IPluginContext context)
    {
        hookSubscription?.Dispose();
    }
}
```

### Performance Considerations

- **No hooks registered**: Minimal overhead (~2-3ns per system, just an empty check)
- **With hooks**: Overhead scales linearly with number of hooks
- **Phase filtering**: Use phase filters to reduce unnecessary hook invocations
- **Hook execution order**: Hooks execute in registration order (before hooks → system → after hooks)

### Best Practices

1. **Always dispose subscriptions**: Use `EventSubscription.Dispose()` to unregister hooks when no longer needed
2. **Keep hooks lightweight**: Avoid expensive operations in hooks; they run for every system execution
3. **Use phase filtering**: If hooks only apply to specific phases, use the phase parameter to reduce overhead
4. **Plugin cleanup**: Plugins should always dispose their hook subscriptions in `Uninstall()`
5. **Exception handling**: Hook exceptions propagate to the caller; handle errors appropriately

## Claude Code Web Sessions

Claude Code web environments have proxy restrictions that prevent NuGet from authenticating properly. SessionStart hooks automatically handle this.

### How It Works

The hooks are configured in `.claude/settings.json` and run automatically:

**On session startup:**
1. `./scripts/install-dotnet.sh` - Installs .NET SDK if needed
2. `./scripts/install-hooks.sh` - Sets up git hooks
3. `./.claude/hooks/install-packages.sh` - Downloads NuGet packages via `wget` (bypasses proxy auth issues)
4. `./.claude/hooks/install-gh-cli.sh` - Installs GitHub CLI

**On session resume:**
1. `./scripts/resume-dotnet.sh` - Restores .NET SDK path
2. `./.claude/hooks/install-gh-cli.sh` - Ensures GitHub CLI is available

The package install script:
- Downloads packages via `wget` (handles proxy auth correctly)
- Extracts packages to `~/.nuget/packages/` (global cache)
- Copies `.nupkg` files to `/tmp/nuget-feed/` (local source)

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

### GitHub CLI Usage

In web sessions, the git remote uses a local proxy URL that the GitHub CLI doesn't recognize as a GitHub host. Always use the `--repo` flag to specify the repository explicitly:

```bash
# ❌ BAD: Will fail with "none of the git remotes configured for this repository point to a known GitHub host"
gh issue list
gh pr create

# ✅ GOOD: Explicitly specify the repository
gh issue list --repo orion-ecs/keen-eye
gh pr create --repo orion-ecs/keen-eye --title "..." --body "..."
gh issue close 123 --repo orion-ecs/keen-eye --comment "Fixed in PR #456"
gh api repos/orion-ecs/keen-eye/issues/123
```

This applies to all `gh` commands that interact with the repository.
