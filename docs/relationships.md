# Entity Relationships Guide

Entity relationships enable hierarchical structures like scene graphs, UI layouts, and skeletal animations. This guide covers parent-child relationships in KeenEyes.

## Overview

KeenEyes provides built-in parent-child relationship management:

- **Parent lookup** - O(1) via dictionary
- **Children enumeration** - O(C) where C is child count
- **Cycle prevention** - Automatic detection and rejection
- **Cascading despawn** - Destroy entity and all descendants

## Basic Relationship Operations

### Setting a Parent

Use `SetParent()` to establish a parent-child relationship:

```csharp
using var world = new World();

var parent = world.Spawn().With(new Position { X = 0, Y = 0 }).Build();
var child = world.Spawn().With(new Position { X = 10, Y = 10 }).Build();

// Establish relationship
world.SetParent(child, parent);
```

### Adding Children

The `AddChild()` method provides an alternative syntax:

```csharp
var parent = world.Spawn().Build();
var child = world.Spawn().Build();

// Equivalent to SetParent(child, parent)
world.AddChild(parent, child);
```

### Getting Parent and Children

```csharp
// Get parent (returns Entity.Null if no parent)
Entity parentEntity = world.GetParent(child);

if (parentEntity.IsValid)
{
    Console.WriteLine($"Parent: {parentEntity}");
}

// Get all immediate children
foreach (var c in world.GetChildren(parent))
{
    Console.WriteLine($"Child: {c}");
}
```

### Removing Relationships

```csharp
// Remove parent (make child a root entity)
world.SetParent(child, Entity.Null);

// Or use RemoveChild
bool removed = world.RemoveChild(parent, child);
```

## Hierarchy Traversal

### Get All Descendants

`GetDescendants()` returns all descendants in breadth-first order:

```csharp
// Create hierarchy: root -> child -> grandchild
var root = world.Spawn().Build();
var child = world.Spawn().Build();
var grandchild = world.Spawn().Build();

world.SetParent(child, root);
world.SetParent(grandchild, child);

// Returns: child, grandchild
foreach (var descendant in world.GetDescendants(root))
{
    Console.WriteLine($"Descendant: {descendant}");
}
```

### Get All Ancestors

`GetAncestors()` walks up the hierarchy:

```csharp
// Returns: child, root (from grandchild's perspective)
foreach (var ancestor in world.GetAncestors(grandchild))
{
    Console.WriteLine($"Ancestor: {ancestor}");
}
```

### Find Root Entity

`GetRoot()` finds the topmost ancestor:

```csharp
Entity rootEntity = world.GetRoot(grandchild);
// rootEntity == root

// Root entities return themselves
Entity selfRoot = world.GetRoot(root);
// selfRoot == root
```

## Cascading Despawn

### DespawnRecursive

To destroy an entity and all its descendants, use `DespawnRecursive()`:

```csharp
var root = world.Spawn().Build();
var child1 = world.Spawn().Build();
var child2 = world.Spawn().Build();
var grandchild = world.Spawn().Build();

world.SetParent(child1, root);
world.SetParent(child2, root);
world.SetParent(grandchild, child1);

// Destroy root and all 3 descendants
int count = world.DespawnRecursive(root);
Console.WriteLine($"Destroyed {count} entities"); // 4

// All are now dead
Console.WriteLine(world.IsAlive(root));       // False
Console.WriteLine(world.IsAlive(child1));     // False
Console.WriteLine(world.IsAlive(child2));     // False
Console.WriteLine(world.IsAlive(grandchild)); // False
```

### Regular Despawn Behavior

Regular `Despawn()` removes only the entity itself. Children become orphaned (root entities):

```csharp
world.SetParent(child, parent);
world.Despawn(parent);

// Child is still alive but now has no parent
Console.WriteLine(world.IsAlive(child));           // True
Console.WriteLine(world.GetParent(child).IsValid); // False
```

## Cycle Prevention

KeenEyes automatically prevents circular hierarchies:

```csharp
var a = world.Spawn().Build();
var b = world.Spawn().Build();
var c = world.Spawn().Build();

world.SetParent(b, a);  // a -> b
world.SetParent(c, b);  // a -> b -> c

// This would create a cycle: a -> b -> c -> a
try
{
    world.SetParent(a, c);  // Throws InvalidOperationException
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
    // "Cannot set entity ... as parent ... would create circular hierarchy"
}
```

## Use Cases

### Scene Graph

```csharp
// Create scene hierarchy
var scene = world.Spawn("Scene").Build();
var player = world.Spawn("Player")
    .With(new Position { X = 0, Y = 0 })
    .Build();
var weapon = world.Spawn("Weapon")
    .With(new Position { X = 1, Y = 0 })
    .Build();

world.SetParent(player, scene);
world.SetParent(weapon, player);

// Weapon moves with player
```

### UI Layout

```csharp
var window = world.Spawn("Window")
    .With(new UIRect { X = 100, Y = 100, Width = 400, Height = 300 })
    .Build();

var titleBar = world.Spawn("TitleBar")
    .With(new UIRect { X = 0, Y = 0, Width = 400, Height = 30 })
    .Build();

var content = world.Spawn("Content")
    .With(new UIRect { X = 0, Y = 30, Width = 400, Height = 270 })
    .Build();

world.SetParent(titleBar, window);
world.SetParent(content, window);

// Close window destroys all UI elements
world.DespawnRecursive(window);
```

### Transform Propagation

```csharp
public class TransformPropagationSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Process root entities first
        foreach (var entity in World.Query<Position>())
        {
            var parent = World.GetParent(entity);
            if (!parent.IsValid) continue; // Skip roots

            // Get parent's world position
            ref readonly var parentPos = ref World.Get<Position>(parent);
            ref var childPos = ref World.Get<Position>(entity);

            // Child position is relative to parent
            // (In real code, you'd store local vs world positions separately)
        }
    }
}
```

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|------------|-------|
| `SetParent` | O(D) | D = hierarchy depth (cycle check) |
| `GetParent` | O(1) | Dictionary lookup |
| `GetChildren` | O(C) | C = child count |
| `GetDescendants` | O(N) | N = total descendants |
| `GetAncestors` | O(D) | D = hierarchy depth |
| `GetRoot` | O(D) | D = hierarchy depth |
| `DespawnRecursive` | O(N) | N = subtree size |

## Best Practices

### Do: Validate Before Access

```csharp
var parent = world.GetParent(entity);
if (parent.IsValid && world.IsAlive(parent))
{
    ref var parentPos = ref world.Get<Position>(parent);
}
```

### Do: Use DespawnRecursive for Hierarchies

```csharp
// Clean up entire hierarchy
world.DespawnRecursive(rootEntity);
```

### Don't: Create Deep Hierarchies

```csharp
// Avoid: Very deep nesting hurts traversal performance
// a -> b -> c -> d -> e -> f -> g -> h -> ...

// Better: Flatter structures with grouped children
// root -> [child1, child2, child3, ...]
```

### Don't: Store Stale Parent References

```csharp
// Bad: Parent may be despawned
var parent = world.GetParent(child);
// ... much later ...
world.Get<Position>(parent);  // May throw!

// Good: Always validate
var parent = world.GetParent(child);
if (parent.IsValid && world.IsAlive(parent))
{
    ref var pos = ref world.Get<Position>(parent);
}
```

## Next Steps

- [Events Guide](events.md) - React to hierarchy changes
- [Command Buffer](command-buffer.md) - Deferred hierarchy operations
- [Entities Guide](entities.md) - Entity lifecycle basics
