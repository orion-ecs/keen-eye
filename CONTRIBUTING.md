# Contributing to KeenEyes

Thank you for your interest in contributing to KeenEyes! This document provides guidelines and information to help you contribute effectively.

## Table of Contents

- [Design Philosophy](#design-philosophy)
- [Code Quality Standards](#code-quality-standards)
- [Pull Request Guidelines](#pull-request-guidelines)
- [Commit Message Format](#commit-message-format)
- [Testing Requirements](#testing-requirements)
- [Architecture Decisions](#architecture-decisions)

---

## Design Philosophy

KeenEyes follows strict design principles that guide all contributions. Understanding these is essential before contributing.

### Core Principles

#### 1. Explicit Over Implicit

We prefer visible, traceable behavior over magic:

| Avoided (Implicit) | Preferred (Explicit) |
|-------------------|---------------------|
| Auto-register all systems | User registers each system |
| Global singletons | Per-world state |
| Hidden initialization | Clear setup code |
| Attribute-based wiring | Code-based configuration |

**Why?** Explicit code is debuggable. Magic that works is convenient; magic that fails is a nightmare.

#### 2. No Static State

All state must be instance-based. Each `World` is completely isolated with its own:
- Component registry (component IDs are per-world)
- Entity storage
- Systems

```csharp
// BAD: Static state
public static class ComponentRegistry
{
    private static readonly Dictionary<Type, int> ids = new();
}

// GOOD: Per-world state
public sealed class World
{
    public ComponentRegistry Components { get; } = new();
}
```

**Why?** This enables multiple independent ECS worlds, easy testing, and no hidden initialization order dependencies.

#### 3. Native AOT Compatibility

All production code must work with Native AOT compilation. **No reflection in runtime code.**

Prohibited:
- `Type.GetField()`, `Type.GetMethod()`, `Type.GetProperties()`
- `Activator.CreateInstance()` for dynamic object creation
- Assembly scanning at runtime

Alternatives:
- Factory delegates instead of `Activator.CreateInstance()`
- Static abstract interface members instead of reflection
- Source generators for type metadata

**Exception:** Editor-only code (like `ComponentIntrospector`) may use reflection since it doesn't ship in player builds.

#### 4. Composition Over Inheritance

ECS uses composition instead of deep inheritance hierarchies:

```csharp
// BAD: Inheritance hierarchy
class GameObject → Actor → Pawn → Character → PlayerCharacter

// GOOD: Composition
Entity + Position + Velocity + Health + PlayerInput + ...
```

**Why?** Inheritance creates rigid taxonomies. Composition allows flexible combinations.

#### 5. Data-Oriented Design

- Components are structs (contiguous memory)
- Systems process entities in bulk (cache-friendly iteration)
- References avoided in hot paths

For detailed philosophy documentation, see [docs/philosophy/](docs/philosophy/).

---

## Code Quality Standards

### Build Requirements

Before submitting code, ensure:

```bash
dotnet build                        # Zero warnings, zero errors
dotnet test                         # All tests pass
dotnet format --verify-no-changes   # Code is formatted
```

**Non-negotiable rules:**
- All existing tests must pass
- No new warnings (warnings are errors)
- Code must be formatted per `.editorconfig`

### Coding Conventions

- Target .NET 10 with C# 13+ features
- Use file-scoped namespaces
- Prefer `readonly record struct` for small value types
- Use `ref` returns for component access (zero-copy)
- Nullable reference types enabled everywhere
- XML documentation on all public APIs

### Naming Conventions

The codebase enforces strict naming conventions via `.editorconfig`. **These are errors, not suggestions.**

| Symbol Type | Convention | Example |
|-------------|------------|---------|
| Interfaces | `I` prefix + PascalCase | `IWorld`, `IComponent` |
| Type parameters | `T` prefix + PascalCase | `TComponent`, `TSystem` |
| Private fields | camelCase (NO underscore) | `entityCount`, `components` |
| Private static fields | camelCase (NO underscore) | `defaultRegistry` |
| Local variables | camelCase | `position`, `entity` |
| Parameters | camelCase | `deltaTime`, `entity` |
| Public members | PascalCase | `EntityCount`, `GetComponent` |
| Constants | PascalCase | `MaxEntities`, `DefaultCapacity` |
| Methods | PascalCase | `Spawn()`, `GetComponent()` |
| Properties | PascalCase | `Count`, `IsAlive` |

**Common mistakes to avoid:**

```csharp
// BAD: Underscore prefix on private fields
private int _entityCount;
private readonly Dictionary<int, Entity> _entities;

// GOOD: camelCase without underscore
private int entityCount;
private readonly Dictionary<int, Entity> entities;
```

### Code Style Rules

The following rules are enforced by analyzers:

```csharp
// Use 'var' when type is apparent
var entity = world.Spawn().Build();  // Type is obvious
World world = new World();           // Also fine, type on right

// Prefer expression body for single-line members
public int Count => entities.Count;
public void Clear() => entities.Clear();

// Use object initializers
var config = new Config { Width = 800, Height = 600 };

// Use collection expressions
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob"];

// Braces required for all control statements
if (condition)
{
    DoSomething();  // Even single lines need braces
}
```

### Formatting

Run formatting before committing:

```bash
# Check formatting without making changes
dotnet format --verify-no-changes

# Auto-fix formatting issues
dotnet format

# Format specific project
dotnet format src/KeenEyes.Core/
```

### Floating-Point Comparisons

Never use `==` to compare floats. Use extension methods from `KeenEyes.Common.FloatExtensions`:

```csharp
// BAD
if (value == 0)

// GOOD
if (value.IsApproximatelyZero())
```

### No Speculative Features

Do not add code "just in case":
- No unused methods for hypothetical future use
- No extra parameters for hypothetical use cases
- No backwards-compatibility layers (this is a new engine)

---

## Pull Request Guidelines

### PR Title Format

PR titles **must** follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>: <description>
```

**Valid types:**

| Type | Description |
|------|-------------|
| `feat` | New feature or functionality |
| `fix` | Bug fix |
| `docs` | Documentation only changes |
| `chore` | Maintenance tasks, dependencies |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or updating tests |
| `ci` | CI/CD changes |
| `perf` | Performance improvements |
| `style` | Code style/formatting changes |
| `build` | Build system changes |

**Examples:**
```
feat: Add ComponentIntrospector for reflection-based inspection
fix: Resolve entity despawn race condition
docs: Update getting started guide
refactor: Extract HierarchyManager from World class
test: Add integration tests for prefab spawning
perf: Optimize archetype iteration with SIMD
```

**Scope (optional):**
```
feat(editor): Add PropertyDrawer system
fix(physics): Correct collision detection for rotated boxes
```

### PR Description

Use this template:

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

### PR Size Guidelines

Keep PRs focused and reviewable:
- **XS** (1-10 lines): Typos, small fixes
- **S** (11-100 lines): Single feature or bug fix
- **M** (101-500 lines): Feature with tests
- **L** (501-1000 lines): Large feature (consider splitting)
- **XL** (1000+ lines): Requires justification

---

## Commit Message Format

Follow the same Conventional Commits format as PR titles:

```
<type>: <subject>

[optional body]

[optional footer]
```

**Subject line rules:**
- Use imperative mood ("Add feature" not "Added feature")
- Don't capitalize first letter after type
- No period at the end
- Max 72 characters

**Example:**
```
feat: Add entity hierarchy support

Implement parent-child relationships between entities with:
- SetParent/GetParent API
- Automatic child despawn on parent despawn
- Hierarchy traversal queries

Closes #123
```

---

## Testing Requirements

### Coverage Rules

- Coverage must not decrease on any PR
- New code must have corresponding tests
- Edge cases and error paths must be covered

### Test Naming Convention

Use the pattern: `MethodName_Scenario_ExpectedResult`

```csharp
// GOOD
[Fact]
public void Despawn_WithValidEntity_RemovesEntityFromWorld()

[Fact]
public void Get_WithDeadEntity_ThrowsInvalidOperationException()

// BAD
[Fact]
public void TestDespawn()

[Fact]
public void GetThrows()
```

### Test Organization

- One test class per unit under test
- Use `#region` for grouping related tests
- Each test should be independent (no shared mutable state)

---

## Architecture Decisions

Major architectural decisions are documented as ADRs (Architecture Decision Records) in `docs/adr/`.

Before making significant changes, check if an ADR exists. If proposing a new architectural direction, consider writing an ADR.

### Key ADRs

| ADR | Topic |
|-----|-------|
| [ADR-001](docs/adr/001-world-manager-architecture.md) | World Manager facade pattern |
| [ADR-004](docs/adr/004-reflection-elimination.md) | Native AOT and reflection elimination |
| [ADR-006](docs/adr/006-custom-msbuild-sdk.md) | Custom MSBuild SDK |
| [ADR-007](docs/adr/007-capability-based-plugin-architecture.md) | Plugin architecture |

---

## Getting Help

- **Questions about the codebase:** Open a discussion or issue
- **Bug reports:** Use the bug report issue template
- **Feature requests:** Open an issue describing the use case

Thank you for contributing to KeenEyes!
