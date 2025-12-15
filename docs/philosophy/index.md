# Design Philosophy

KeenEyes makes deliberate design choices that differ from many game engines. This section explains *why* we made these choices and the tradeoffs involved.

## Core Principles

### 1. Explicit Over Implicit

KeenEyes prefers explicit, visible behavior over magic:

| Implicit (Avoided) | Explicit (Preferred) |
|-------------------|---------------------|
| Auto-register all systems | User registers each system |
| Global singletons | Per-world state |
| Hidden initialization | Clear setup code |
| Attribute-based wiring | Code-based configuration |

**Why?** Explicit code is debuggable. When something goes wrong, you can trace the exact path through your code. Magic that works is convenient; magic that fails is a nightmare.

### 2. Composition Over Inheritance

Traditional game engines use deep inheritance hierarchies:
```
GameObject → Actor → Pawn → Character → PlayerCharacter
```

ECS uses composition:
```
Entity + Position + Velocity + Health + PlayerInput + ...
```

**Why?** Inheritance creates rigid taxonomies that break down with cross-cutting concerns. Is a "flying enemy that can be ridden" a `FlyingEnemy` or a `Mount`? With composition, it's simply an entity with both `Flying` and `Mountable` components.

### 3. Data-Oriented Design

KeenEyes treats data locality as a first-class concern:

- Components are structs (contiguous memory)
- Systems process entities in bulk (cache-friendly iteration)
- References are avoided in hot paths

**Why?** Modern CPUs spend most of their time waiting for memory. Cache-friendly code can be 10-100x faster than pointer-chasing code.

### 4. No Hidden Dependencies

Every dependency should be visible:

- No static state means no initialization order dependencies
- Systems declare what components they access
- Worlds are completely isolated

**Why?** Hidden dependencies create subtle bugs that only appear in specific conditions. Explicit dependencies are testable and maintainable.

## Design Decisions In-Depth

- [Why ECS?](why-ecs.md) - The case for Entity Component System architecture
- [Why No Static State?](no-static-state.md) - Instance-based design rationale
- [Why Source Generators?](source-generators.md) - Code generation vs runtime reflection
- [Why Native AOT?](native-aot.md) - Ahead-of-time compilation support

## Tradeoffs We Accept

Every design choice has costs. Here's what we knowingly trade away:

### More Boilerplate Than Magic Frameworks

```csharp
// KeenEyes - explicit setup
var world = new World();
world.AddSystem<MovementSystem>();
world.AddSystem<RenderSystem>();
world.AddSystem<CollisionSystem>();
```

```csharp
// Magic framework (hypothetical)
var world = new World(); // Auto-discovers and registers everything
```

**Why accept this?** The 5 extra lines of setup code save hours of debugging when systems interact unexpectedly. You always know exactly what's running.

### Learning Curve

ECS requires a mental shift from OOP. You must learn to think in:
- Data vs logic separation
- Queries vs method calls
- Composition vs inheritance

**Why accept this?** The patterns, once learned, apply everywhere. ECS solutions compose naturally; OOP solutions often require refactoring as requirements change.

### Indirection Cost

Components require lookups:
```csharp
ref var pos = ref World.Get<Position>(entity);  // Lookup
```

vs direct access:
```csharp
enemy.Position  // Direct field access
```

**Why accept this?** The lookup cost is tiny (~1-5ns). The benefits (cache locality, composition, query efficiency) far outweigh it in any non-trivial application.

## Philosophy in Practice

These principles guide everyday decisions:

### Adding a Feature

1. **Where does the data go?** → New component(s)
2. **What processes it?** → New system
3. **How do users configure it?** → Clear registration API

### Fixing a Bug

1. **Can I reproduce it in an isolated test?** → Explicit dependencies make this easy
2. **Is the data visible in debug tools?** → Components are inspectable
3. **Can I trace the execution path?** → No magic means clear paths

### Reviewing a PR

1. **Does this add implicit behavior?** → Prefer explicit
2. **Does this add static state?** → Keep it instance-based
3. **Would this work with Native AOT?** → No reflection in hot paths

## Related Resources

- [Core Concepts](../concepts.md) - ECS fundamentals
- [Architecture Decisions](../adr/001-world-manager-architecture.md) - Specific ADRs
- [Research Articles](../research/index.md) - Future direction research
