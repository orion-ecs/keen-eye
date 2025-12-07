# ADR-001: World Manager Architecture

**Status:** Accepted
**Date:** 2025-12-07
**Issue:** [#82](https://github.com/orion-ecs/keen-eye/issues/82)

## Context

The `World` class has grown to **3,073 lines** with **10+ distinct responsibilities**:

| Region | Lines | Responsibility |
|--------|-------|----------------|
| Entity Management | ~633 | Spawn, Despawn, Get, Has, Add, Set, Remove, naming |
| Entity Hierarchy | ~676 | Parent-child relationships, ancestors, descendants |
| Systems | ~365 | Registration, ordering, topological sort, execution |
| Events | ~189 | Component/entity lifecycle event handlers |
| Change Tracking | ~251 | Dirty flags, auto-tracking |
| Singletons | ~209 | Global data storage |
| Plugins | ~169 | Plugin lifecycle management |
| Extensions | ~107 | Plugin-provided APIs |
| Memory Statistics | ~51 | Diagnostics |
| Queries | ~39 | Delegation to QueryManager |

This violates the Single Responsibility Principle. The class is difficult to:
- Test individual concerns in isolation
- Reason about without understanding all shared state
- Modify without risk of unintended side effects
- Navigate and maintain

## Decision

Refactor `World` into a **facade pattern** with specialized internal managers:

```
World (facade, target ~300-400 lines)
├── HierarchyManager      - Parent-child entity relationships
├── SystemManager         - System registration, ordering, execution
├── PluginManager         - Plugin lifecycle
├── SingletonManager      - Global resource storage
├── ExtensionManager      - Plugin-provided APIs
├── EntityNamingManager   - Entity name registration and lookup
├── EventManager          - Component and entity lifecycle events
├── ChangeTracker         - Dirty flag tracking (enhanced with EntityPool)
├── ArchetypeManager      - (existing) Component storage
├── QueryManager          - (existing) Query caching
└── ComponentRegistry     - (existing) Component type registry
```

### Implementation Order

Extract managers in order of size and isolation (largest/cleanest first):

1. ✅ **HierarchyManager** (~676 lines) - No dependencies on other inline code
2. ✅ **SystemManager** (~365 lines) - Complex topological sort, well-bounded
3. ✅ **PluginManager** (~169 lines) - Interacts with systems
4. ✅ **SingletonManager** (~209 lines) - Simple key-value pattern
5. ✅ **ExtensionManager** (~107 lines) - Plugin-provided APIs
6. ✅ **EntityNamingManager** (~100 lines) - Entity name registration and lookup
7. ✅ **EventManager** (~140 lines) - Consolidates EventBus, ComponentEventHandlers, EntityEventHandlers
8. ✅ **ChangeTracker** (enhanced) - Added EntityPool dependency for entity reconstruction

**Current Status:** World.cs reduced from 3,073 to ~2,272 lines (~26% reduction)

### Design Constraints

- Managers are `internal` (not public API)
- `World` remains the single entry point (facade pattern)
- Public API unchanged - **no breaking changes**
- Each manager takes minimal dependencies
- Unit tests added for each manager before extraction

## Alternatives Considered

### Option 1: Partial Class Split

Split `World` across multiple files using `partial class`:

```
World.cs              - Core fields, constructor, Dispose
World.Entities.cs     - Spawn, Despawn, Get, Has, etc.
World.Hierarchy.cs    - Parent/child relationships
...
```

**Rejected because:** This is cosmetic organization. The class still has 10+ responsibilities sharing mutable state. Doesn't improve testability, coupling, or maintainability.

### Option 2: Extension Methods

Move stateless operations to extension methods:

```csharp
public static class WorldHierarchyExtensions
{
    public static IEnumerable<Entity> GetDescendants(this World world, Entity entity) { ... }
}
```

**Rejected because:** Only works for methods that don't need private state. Hierarchy needs internal dictionaries, so limited applicability.

### Option 3: Defer to v1.0 (YAGNI)

Keep monolithic design through v0.x, refactor for v1.0.

**Rejected because:** The class has already crossed the maintainability threshold at 3,000+ lines. Waiting will make refactoring harder as more code accumulates.

## Consequences

### Positive

- Each manager can be tested in isolation
- Clearer ownership of state and behavior
- Easier to reason about individual concerns
- Follows existing patterns (`ArchetypeManager`, `QueryManager`)
- Enables future parallelization (managers could have separate locks)

### Negative

- Additional indirection (facade → manager → implementation)
- Slight increase in type count
- Migration effort required

### Neutral

- Public API unchanged
- Performance impact negligible (one extra method call)
