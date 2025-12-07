# Change Tracking Guide

Change tracking enables efficient detection of modified entities for network synchronization, undo/redo systems, and reactive updates. This guide covers the change tracking API in KeenEyes.

## Overview

The change tracking system provides:

- **Manual dirty marking** - Explicitly flag entities as modified
- **Automatic tracking** - Optionally track `Set()` calls automatically
- **Per-component tracking** - Separate dirty flags for each component type
- **Efficient querying** - Get all dirty entities for a component type

## Basic Usage

### Manual Dirty Marking

Mark entities as dirty when you modify their components:

```csharp
using var world = new World();

var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();

// Modify component via ref
ref var pos = ref world.Get<Position>(entity);
pos.X = 100;

// Mark as dirty for change tracking
world.MarkDirty<Position>(entity);
```

### Querying Dirty Entities

Get all entities that have been marked dirty for a component type:

```csharp
foreach (var dirtyEntity in world.GetDirtyEntities<Position>())
{
    ref var pos = ref world.Get<Position>(dirtyEntity);
    Console.WriteLine($"Entity {dirtyEntity} modified: ({pos.X}, {pos.Y})");
}
```

### Clearing Dirty Flags

After processing, clear the dirty flags:

```csharp
// Process dirty entities
foreach (var entity in world.GetDirtyEntities<Position>())
{
    SyncPositionToNetwork(entity);
}

// Clear flags after processing
world.ClearDirtyFlags<Position>();
```

## Automatic Tracking

### Enabling Auto-Tracking

Enable automatic tracking to mark entities dirty when using `Set()`:

```csharp
// Enable auto-tracking for Position
world.EnableAutoTracking<Position>();

var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();

// This automatically marks entity dirty for Position
world.Set(entity, new Position { X = 100, Y = 200 });

// Check
Console.WriteLine(world.IsDirty<Position>(entity)); // True
```

### Disabling Auto-Tracking

```csharp
world.DisableAutoTracking<Position>();

// Set() no longer auto-marks as dirty
world.Set(entity, new Position { X = 50, Y = 50 });
Console.WriteLine(world.IsDirty<Position>(entity)); // False (after clear)
```

### Checking Auto-Tracking Status

```csharp
if (world.IsAutoTrackingEnabled<Position>())
{
    Console.WriteLine("Position changes are being auto-tracked");
}
```

**Important**: Auto-tracking only works with `Set()`. Direct modifications via `ref` are not tracked:

```csharp
world.EnableAutoTracking<Position>();

// NOT tracked - uses ref directly
ref var pos = ref world.Get<Position>(entity);
pos.X = 100;

// IS tracked - uses Set()
world.Set(entity, new Position { X = 100, Y = 200 });
```

## Checking Individual Entities

### IsDirty

Check if a specific entity is dirty for a component type:

```csharp
if (world.IsDirty<Position>(entity))
{
    Console.WriteLine($"Entity {entity} has modified position");
}
```

### GetDirtyCount

Get the count of dirty entities without iterating:

```csharp
int dirtyCount = world.GetDirtyCount<Position>();
Console.WriteLine($"{dirtyCount} entities have modified positions");
```

## Use Cases

### Network Synchronization

Replicate only modified entities across the network:

```csharp
public class NetworkSyncSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Sync positions
        foreach (var entity in World.GetDirtyEntities<Position>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            NetworkManager.SendPositionUpdate(entity.Id, pos.X, pos.Y);
        }
        World.ClearDirtyFlags<Position>();

        // Sync health
        foreach (var entity in World.GetDirtyEntities<Health>())
        {
            ref readonly var health = ref World.Get<Health>(entity);
            NetworkManager.SendHealthUpdate(entity.Id, health.Current, health.Max);
        }
        World.ClearDirtyFlags<Health>();
    }
}
```

### Undo/Redo System

Track changes for reversal:

```csharp
public class UndoManager
{
    private readonly Stack<UndoAction> undoStack = [];
    private readonly World world;

    public void CaptureChanges()
    {
        // Capture position changes
        foreach (var entity in world.GetDirtyEntities<Position>())
        {
            ref readonly var currentPos = ref world.Get<Position>(entity);
            undoStack.Push(new PositionUndoAction(entity, currentPos));
        }
        world.ClearDirtyFlags<Position>();
    }

    public void Undo()
    {
        if (undoStack.TryPop(out var action))
        {
            action.Undo(world);
        }
    }
}
```

### Reactive Rendering

Only update visuals for changed entities:

```csharp
public class RenderSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Only update sprites for entities with changed positions
        foreach (var entity in World.GetDirtyEntities<Position>())
        {
            if (World.Has<Sprite>(entity))
            {
                ref readonly var pos = ref World.Get<Position>(entity);
                ref var sprite = ref World.Get<Sprite>(entity);
                sprite.ScreenX = pos.X;
                sprite.ScreenY = pos.Y;
            }
        }
        World.ClearDirtyFlags<Position>();
    }
}
```

### Physics Integration

Sync with external physics engine:

```csharp
public class PhysicsSyncSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Push dirty transforms to physics engine
        foreach (var entity in World.GetDirtyEntities<Position>())
        {
            if (World.Has<PhysicsBody>(entity))
            {
                ref readonly var pos = ref World.Get<Position>(entity);
                ref readonly var body = ref World.Get<PhysicsBody>(entity);
                physicsWorld.SetBodyPosition(body.BodyId, pos.X, pos.Y);
            }
        }
        World.ClearDirtyFlags<Position>();
    }
}
```

## Per-Component Tracking

Dirty flags are tracked separately per component type:

```csharp
var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 1, Y = 0 })
    .Build();

// Mark dirty for Position only
world.MarkDirty<Position>(entity);

Console.WriteLine(world.IsDirty<Position>(entity)); // True
Console.WriteLine(world.IsDirty<Velocity>(entity)); // False

// Clear Position, Velocity unaffected
world.ClearDirtyFlags<Position>();
```

## Combining with Events

Change tracking works well alongside the event system:

```csharp
// Use events for immediate reactions
world.OnComponentChanged<Health>((entity, old, newVal) =>
{
    if (newVal.Current <= 0 && old.Current > 0)
    {
        PlayDeathAnimation(entity);
    }
});

// Use change tracking for batched updates
public class HealthBarSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Update UI for all health changes this frame
        foreach (var entity in World.GetDirtyEntities<Health>())
        {
            UpdateHealthBar(entity);
        }
        World.ClearDirtyFlags<Health>();
    }
}
```

## Performance Considerations

### Minimal Memory Overhead

Dirty flags use `HashSet<int>` per component type, only allocated when first marked.

### Efficient Clearing

`ClearDirtyFlags<T>()` is O(1) - it clears the hash set without reallocating.

### No Overhead When Unused

If you never call `MarkDirty<T>()` for a component type, no tracking structures are allocated.

## Best Practices

### Do: Clear After Processing

```csharp
// Always clear after processing to avoid stale data
foreach (var entity in world.GetDirtyEntities<Position>())
{
    ProcessChange(entity);
}
world.ClearDirtyFlags<Position>();
```

### Do: Use Auto-Tracking for Set-Heavy Code

```csharp
// If you primarily use Set(), enable auto-tracking
world.EnableAutoTracking<Position>();

// All Set() calls automatically mark dirty
foreach (var entity in world.Query<Position, Velocity>())
{
    ref readonly var vel = ref world.Get<Velocity>(entity);
    ref var pos = ref world.Get<Position>(entity);
    world.Set(entity, pos with { X = pos.X + vel.X });
}
```

### Do: Use Manual Marking for Ref-Heavy Code

```csharp
// If you primarily use ref, mark manually
foreach (var entity in world.Query<Position, Velocity>())
{
    ref readonly var vel = ref world.Get<Velocity>(entity);
    ref var pos = ref world.Get<Position>(entity);
    pos.X += vel.X;
    pos.Y += vel.Y;
    world.MarkDirty<Position>(entity);
}
```

### Don't: Forget to Clear

```csharp
// Bad: Never clearing causes entities to stay "dirty" forever
foreach (var entity in world.GetDirtyEntities<Position>())
{
    SyncToNetwork(entity);
}
// Missing: world.ClearDirtyFlags<Position>();
```

### Don't: Mix Auto and Manual Without Care

```csharp
// Be consistent within a component type
world.EnableAutoTracking<Position>();

// Manual mark still works, but may be redundant if using Set()
world.MarkDirty<Position>(entity);  // Fine, but check if needed
```

## API Summary

| Method | Description |
|--------|-------------|
| `MarkDirty<T>(entity)` | Mark entity as dirty for component type T |
| `GetDirtyEntities<T>()` | Get all dirty entities for component type T |
| `ClearDirtyFlags<T>()` | Clear all dirty flags for component type T |
| `IsDirty<T>(entity)` | Check if entity is dirty for component type T |
| `GetDirtyCount<T>()` | Get count of dirty entities for component type T |
| `EnableAutoTracking<T>()` | Auto-mark dirty on `Set()` calls |
| `DisableAutoTracking<T>()` | Disable auto-tracking |
| `IsAutoTrackingEnabled<T>()` | Check if auto-tracking is enabled |

## Next Steps

- [Events Guide](events.md) - React to component changes immediately
- [Relationships](relationships.md) - Parent-child hierarchies
- [Systems Guide](systems.md) - Processing entities in bulk
