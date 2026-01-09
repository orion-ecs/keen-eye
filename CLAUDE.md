# CLAUDE.md

This file contains guidance for Claude Code when working on this repository.

For contribution guidelines, design philosophy, and PR requirements, see [CONTRIBUTING.md](CONTRIBUTING.md).

## Project Overview

KeenEyes is a high-performance Entity Component System (ECS) framework for .NET 10, reimplementing [OrionECS](https://github.com/tyevco/OrionECS) in C#.

## Quick Reference Links

| Topic | Documentation |
|-------|---------------|
| Design Philosophy | [docs/philosophy/](docs/philosophy/) |
| Contribution Guidelines | [CONTRIBUTING.md](CONTRIBUTING.md) |
| Architecture Decisions | [docs/adr/](docs/adr/) |
| SDK Documentation | [docs/sdk.md](docs/sdk.md) |
| MCP Server Reference | [docs/mcp-server.md](docs/mcp-server.md) |

## Architecture Principles

> For detailed rationale, see [docs/philosophy/](docs/philosophy/).

### No Static State

All state must be instance-based. Each `World` is completely isolated with its own component registry, entity storage, and systems.

```csharp
// ❌ BAD: Static state
public static class ComponentRegistry { private static readonly Dictionary<Type, int> ids = new(); }

// ✅ GOOD: Per-world state
public sealed class World { public ComponentRegistry Components { get; } = new(); }
```

### Respect User Intent

Don't generate code that assumes how users will structure their application:
- Don't auto-register all systems (users may want different systems per world)
- Don't force global singletons
- Provide metadata and helpers, but let users wire things up explicitly

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
- `Activator.CreateInstance()` for dynamic object creation
- `System.Reflection.BindingFlags`
- Assembly scanning or type discovery at runtime

**AOT-compatible alternatives:**
- Factory delegates instead of `Activator.CreateInstance()`
- Static abstract interface members instead of reflection on static fields
- Source generators for type metadata
- Explicit registration instead of assembly scanning

**Exceptions:** Test code may use reflection. `object.GetType()` on existing instances is allowed.

See [docs/philosophy/native-aot.md](docs/philosophy/native-aot.md) for detailed examples of each alternative.

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

KeenEyes provides custom MSBuild SDK packages (`KeenEyes.Sdk`, `KeenEyes.Sdk.Plugin`, `KeenEyes.Sdk.Library`) that simplify project setup. See [docs/sdk.md](docs/sdk.md) for full documentation.

## Coding Conventions

> For full style guide details, see [CONTRIBUTING.md](CONTRIBUTING.md).

### Naming Conventions

| Symbol Type | Convention | Example |
|-------------|------------|---------|
| Interfaces | `I` prefix + PascalCase | `IWorld`, `IComponent` |
| Classes, Structs, Enums | PascalCase | `World`, `Entity`, `LogLevel` |
| Methods, Properties, Events | PascalCase | `Spawn()`, `EntityCount`, `OnChanged` |
| Private fields | camelCase (NO underscore) | `entityCount`, `components` |
| Constants | PascalCase | `MaxEntities`, `DefaultCapacity` |
| Local variables | camelCase | `entity`, `componentData` |
| Parameters | camelCase | `world`, `deltaTime` |

### Code Style

- File-scoped namespaces required
- Braces required for all control flow (even single lines)
- Allman brace style (new line before opening brace)
- `var` preferred when type is apparent
- Expression-bodied members for simple cases
- Pattern matching preferred

### Floating-Point Comparisons

**Never use `==` to compare floats.** Use `KeenEyes.Common.FloatExtensions`:

```csharp
// ❌ BAD: if (value == 0)
// ✅ GOOD: if (value.IsApproximatelyZero())

// ❌ BAD: if (a == b)
// ✅ GOOD: if (a.ApproximatelyEquals(b))
```

Methods: `IsApproximatelyZero()`, `ApproximatelyEquals(other)` (default epsilon: 1e-6f)

### C# 13 Extension Members

This project uses C# 13 **extension members** syntax:

```csharp
extension(global::KeenEyes.IWorld world)
{
    public MyExtension MyProperty => world.GetExtension<MyExtension>();
}
```

**Note for code review agents**: The `extension(Type paramName)` syntax is **valid C# 13/14 syntax**, not an error.

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

The `World` class uses a **facade pattern** with specialized internal managers. See [ADR-001](docs/adr/001-world-manager-architecture.md) for design rules.

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

## Work Tracking

> For commit message format and PR guidelines, see [CONTRIBUTING.md](CONTRIBUTING.md).

### Todo Lists (Claude-specific)

- Use `TodoWrite` for any task with 3+ steps
- Mark todos `in_progress` before starting work
- Mark todos `completed` immediately after finishing (don't batch)
- Break complex features into granular, trackable items

### Progress Documentation

- Update ROADMAP.md when completing roadmap items
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

> For full quality standards, testing guidelines, and coverage requirements, see [CONTRIBUTING.md](CONTRIBUTING.md).

### No Speculative Features

Do not add "future nice to have" code:
- Don't add unused methods "in case they're useful later"
- Don't add extra parameters for hypothetical use cases
- If a feature isn't in the requirements, don't implement it

YAGNI: Either it's needed now (add it), or it's not (don't add it).

### No Backwards Compatibility Layers

This is a **new engine** - no existing user base requires compatibility. Do not add:
- Legacy overloads or fallback methods
- Compatibility shims for "old" APIs
- Deprecated alternatives alongside new implementations

Ask the user first if you believe backwards compatibility is genuinely needed.

## ECS Principles

> For detailed ECS philosophy, see [docs/philosophy/why-ecs.md](docs/philosophy/why-ecs.md).

### Core ECS Tenets

1. **Composition over Inheritance** - Entities are IDs, behavior comes from component combinations
2. **Data-Oriented Design** - Components are structs (data only), systems contain logic
3. **Separation of Concerns** - Components: WHAT, Systems: HOW, Queries: WHICH
4. **Explicit over Implicit** - No hidden registration, users wire things up explicitly

### Anti-Patterns to Avoid

```csharp
// ❌ BAD: Logic in components
public struct Health { public int Current; public void TakeDamage(int amt) => Current -= amt; }

// ✅ GOOD: Pure data, logic in systems
public struct Health { public int Current; public int Max; }

// ❌ BAD: God component
public struct Actor { public float X, Y, VelX, VelY; public int Health, Damage; /* 50 fields */ }

// ✅ GOOD: Small, focused components
public struct Position { public float X, Y; }
public struct Velocity { public float X, Y; }
```

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

## MCP TestBridge Integration

The TestBridge enables external tools (like Claude Code) to inspect and control running KeenEyes applications via MCP. See [docs/mcp-server.md](docs/mcp-server.md) for full tool reference.

### Architecture Overview

```
Claude Code → (stdio) → KeenEyes.Mcp.TestBridge
                          ↓ (named pipe IPC)
                        KeenEyes.TestBridge.Ipc
                          ↓ (in-process)
                        KeenEyes.TestBridge → World (ECS)
```

### Key Packages

| Package | Purpose |
|---------|---------|
| `KeenEyes.TestBridge.Abstractions` | Interfaces |
| `KeenEyes.TestBridge` | In-process implementation |
| `KeenEyes.TestBridge.Ipc` | Named pipe IPC layer |
| `KeenEyes.Mcp.TestBridge` | MCP server for Claude Code |

### Editor Named Pipes

- `KeenEyes.Editor.TestBridge` - Editor UI world
- `KeenEyes.Editor.Scene.TestBridge` - Currently loaded scene

### MCP Configuration

Configured in `.mcp.json`:

```json
{
  "mcpServers": {
    "keeneyes-bridge": {
      "type": "stdio",
      "command": ".mcp/KeenEyes.Mcp.TestBridge.exe",
      "env": {
        "KEENEYES_TRANSPORT": "pipe",
        "KEENEYES_PIPE_NAME": "KeenEyes.Editor.TestBridge"
      }
    }
  }
}
```

Publish the server: `dotnet publish tools/KeenEyes.Mcp.TestBridge -c Release -o .mcp`

### Known Issues

- **#843**: `state_get_entity`, `state_get_component`, `state_query_entities` with component data fail (AOT serialization)
- **#844**: Screenshot capture fails (must run on render thread)

### Debugging Tips

1. Use `game_status` to verify connection and latency
2. Use `state_query_entities` without component data to see entity hierarchy
3. Use `state_get_children` and `state_get_parent` for hierarchy traversal
4. Use `state_get_performance` to track frame times

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
