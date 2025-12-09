# Component Validation

Component validation enforces constraints between components at runtime, catching invalid entity configurations early. The system supports both attribute-based declarative constraints and custom validation logic.

## Attribute-Based Validation

### RequiresComponent

Declare that a component requires another component to be present:

```csharp
[Component]
[RequiresComponent(typeof(Transform))]
public partial struct RigidBody
{
    public float Mass;
    public float Drag;
}
```

When validation is enabled, adding `RigidBody` to an entity without `Transform` throws `ComponentValidationException`:

```csharp
// This throws - Transform is required
var entity = world.Spawn()
    .With(new RigidBody { Mass = 1.0f })
    .Build();

// This works
var entity = world.Spawn()
    .With(new Transform())
    .With(new RigidBody { Mass = 1.0f })
    .Build();
```

### Multiple Requirements

Apply multiple `RequiresComponent` attributes for multiple dependencies:

```csharp
[Component]
[RequiresComponent(typeof(Transform))]
[RequiresComponent(typeof(Renderable))]
public partial struct Sprite
{
    public string TextureId;
    public int Layer;
}
```

### ConflictsWith

Declare that two components cannot coexist on the same entity:

```csharp
[Component]
[ConflictsWith(typeof(DynamicBody))]
public partial struct StaticBody
{
    public bool IsKinematic;
}

[Component]
[ConflictsWith(typeof(StaticBody))]
public partial struct DynamicBody
{
    public float Mass;
    public float Drag;
}
```

Adding both components to the same entity throws:

```csharp
// This throws - StaticBody conflicts with DynamicBody
var entity = world.Spawn()
    .With(new StaticBody())
    .With(new DynamicBody())
    .Build();
```

Conflicts are bidirectional - if A conflicts with B, then B implicitly conflicts with A.

## Validation Modes

Control when validation runs via `World.ValidationMode`:

```csharp
// Always validate (default)
world.ValidationMode = ValidationMode.Enabled;

// Never validate (maximum performance)
world.ValidationMode = ValidationMode.Disabled;

// Only validate in DEBUG builds
world.ValidationMode = ValidationMode.DebugOnly;
```

### Choosing a Mode

| Mode | When to Use |
|------|-------------|
| `Enabled` | Development, testing, or when correctness is critical |
| `Disabled` | Production builds where performance is critical and code is trusted |
| `DebugOnly` | Balance between safety during development and performance in production |

```csharp
// Common pattern: debug-only validation
#if DEBUG
world.ValidationMode = ValidationMode.Enabled;
#else
world.ValidationMode = ValidationMode.Disabled;
#endif
```

## Custom Validators

Register custom validation logic for complex constraints:

```csharp
// Validate Health component values
world.RegisterValidator<Health>((world, entity, health) =>
    health.Current >= 0 &&
    health.Current <= health.Max &&
    health.Max > 0);

// This throws ComponentValidationException
world.Add(entity, new Health { Current = 150, Max = 100 });
```

### Validator Parameters

Custom validators receive:
- `world` - The world instance
- `entity` - The entity receiving the component
- `component` - The component data being added

Return `true` if validation passes, `false` to throw an exception.

### Cross-Component Validation

Custom validators can check other components on the entity:

```csharp
// Weapon requires enough strength to wield
world.RegisterValidator<Weapon>((world, entity, weapon) =>
{
    if (!world.Has<Stats>(entity))
    {
        return true; // Let RequiresComponent handle this
    }

    ref readonly var stats = ref world.Get<Stats>(entity);
    return stats.Strength >= weapon.RequiredStrength;
});
```

### Removing Validators

```csharp
bool removed = world.UnregisterValidator<Health>();
```

## When Validation Runs

Validation occurs:

1. **During `EntityBuilder.Build()`** - All components are validated together
2. **During `World.Add<T>()`** - When adding to an existing entity

```csharp
// Validated at Build()
var entity = world.Spawn()
    .With(new Transform())
    .With(new RigidBody())
    .Build();

// Validated immediately
world.Add(entity, new Sprite { TextureId = "player.png" });
```

## Handling Validation Errors

Catch `ComponentValidationException` to handle validation failures:

```csharp
try
{
    world.Add(entity, new RigidBody { Mass = 1.0f });
}
catch (ComponentValidationException ex)
{
    Console.WriteLine($"Validation failed for {ex.ComponentType.Name}");
    Console.WriteLine($"Entity: {ex.Entity}");
    Console.WriteLine($"Reason: {ex.Message}");
}
```

## Use Cases

### Physics System Constraints

```csharp
// Colliders require Transform
[Component]
[RequiresComponent(typeof(Transform))]
public partial struct BoxCollider
{
    public float Width;
    public float Height;
}

// RigidBody requires Collider
[Component]
[RequiresComponent(typeof(BoxCollider))]
public partial struct RigidBody
{
    public float Mass;
}

// Static and Dynamic bodies are mutually exclusive
[Component]
[ConflictsWith(typeof(DynamicBody))]
public partial struct StaticBody { }

[Component]
[ConflictsWith(typeof(StaticBody))]
public partial struct DynamicBody
{
    public float Mass;
}
```

### Rendering Pipeline

```csharp
[Component]
[RequiresComponent(typeof(Transform))]
public partial struct Renderable
{
    public int Layer;
}

[Component]
[RequiresComponent(typeof(Renderable))]
public partial struct Sprite
{
    public string TextureId;
}

[Component]
[RequiresComponent(typeof(Renderable))]
[ConflictsWith(typeof(Sprite))]
public partial struct Model3D
{
    public string ModelPath;
}
```

### Game Logic Validation

```csharp
// Custom validator for inventory slots
world.RegisterValidator<Inventory>((world, entity, inventory) =>
    inventory.Slots > 0 && inventory.Slots <= 100);

// Custom validator for experience/level consistency
world.RegisterValidator<Experience>((world, entity, exp) =>
{
    if (!world.Has<Level>(entity))
    {
        return true;
    }

    ref readonly var level = ref world.Get<Level>(entity);
    return exp.Total >= GetExpForLevel(level.Current);
});
```

## Performance Considerations

- **Attribute scanning**: Component constraints are cached after first access
- **Validation overhead**: Adds checking during component addition
- **Production builds**: Use `ValidationMode.Disabled` for maximum performance

The source generator creates metadata for attribute-based constraints, avoiding reflection in hot paths when generated code is available.

## Best Practices

1. **Use attributes for structural constraints** - Dependencies between component types
2. **Use custom validators for data constraints** - Value range checks, cross-component logic
3. **Keep validators fast** - They run on every component add
4. **Document constraints** - Make dependencies clear in code comments
5. **Test validation paths** - Ensure constraints catch invalid configurations

```csharp
/// <summary>
/// Sprite rendering component.
/// Requires <see cref="Transform"/> and <see cref="Renderable"/>.
/// </summary>
[Component]
[RequiresComponent(typeof(Transform))]
[RequiresComponent(typeof(Renderable))]
public partial struct Sprite
{
    public string TextureId;
}
```
