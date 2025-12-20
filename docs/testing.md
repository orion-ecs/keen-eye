# Testing Guide

The `KeenEyes.Testing` package provides mock implementations and utilities for testing ECS code without requiring full `World` instances.

## Why KeenEyes.Testing?

Testing ECS code often requires complex setup:

```csharp
// Without KeenEyes.Testing - heavy setup
using var world = new World();
world.InstallPlugin<PhysicsPlugin>();
world.InstallPlugin<RenderingPlugin>();

var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();

// Run your system...
mySystem.Update(0.016f);
```

With `KeenEyes.Testing`, you can test in isolation:

```csharp
// With KeenEyes.Testing - focused, fast tests
var mockWorld = new MockWorld();
var mockContext = new MockPluginContext("Test");

var plugin = new MyPlugin();
plugin.Install(mockContext);

// Verify behavior without heavy infrastructure
Assert.Single(mockContext.RegisteredSystems);
```

## Installation

Reference `KeenEyes.Testing` in your test project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\KeenEyes.Testing\KeenEyes.Testing.csproj" />
</ItemGroup>
```

## Mock Capabilities

Capability mocks let you test plugins without a real World.

### MockHierarchyCapability

Tests entity parent-child relationships:

```csharp
var hierarchy = new MockHierarchyCapability();

// Set up test data
var parent = new Entity(1, 1);
var child1 = new Entity(2, 1);
var child2 = new Entity(3, 1);

hierarchy.SetParent(child1, parent);
hierarchy.SetParent(child2, parent);

// Query relationships
var children = hierarchy.GetChildren(parent).ToList();
Assert.Equal(2, children.Count);

// Check operation log
Assert.Equal(2, hierarchy.OperationLog.Count);
Assert.Contains(("SetParent", child1, parent), hierarchy.OperationLog);
```

### MockValidationCapability

Tests component validation configuration:

```csharp
var validation = new MockValidationCapability();

// Register a validator
validation.RegisterValidator<Health>((world, entity, health) =>
    health.Current >= 0 && health.Current <= health.Max);

// Verify registration
Assert.True(validation.HasValidator<Health>());
Assert.Single(validation.RegisteredValidators);
```

### MockTagCapability

Tests string-based entity tagging:

```csharp
var tags = new MockTagCapability();
var entity = new Entity(1, 1);

tags.AddTag(entity, "Enemy");
tags.AddTag(entity, "Boss");

Assert.True(tags.HasTag(entity, "Enemy"));
Assert.True(tags.HasTag(entity, "Boss"));

var entityTags = tags.GetTags(entity);
Assert.Equal(2, entityTags.Count);
```

### MockStatisticsCapability

Tests memory profiling with configurable stats:

```csharp
var stats = new MockStatisticsCapability
{
    TotalAllocatedBytes = 1024 * 1024,
    EntityCount = 500,
    ArchetypeCount = 10,
    ComponentTypeCount = 25
};

var memStats = stats.GetMemoryStats();
Assert.Equal(1024 * 1024, memStats.TotalAllocatedBytes);
Assert.Equal(500, memStats.EntityCount);
```

### MockPrefabCapability

Tests prefab registration and spawning:

```csharp
var prefabs = new MockPrefabCapability();

var enemyPrefab = new EntityPrefab()
    .With(new Health { Current = 100, Max = 100 })
    .With(new Position { X = 0, Y = 0 });

prefabs.RegisterPrefab("Enemy", enemyPrefab);

// Verify registration
Assert.True(prefabs.HasPrefab("Enemy"));
Assert.Contains("Enemy", prefabs.RegistrationOrder);

// Track spawning (requires World for actual spawns)
// prefabs.SpawnFromPrefab("Enemy"); // Would log to SpawnLog
```

### MockInspectionCapability

Tests entity inspection for debugging tools:

```csharp
var inspection = new MockInspectionCapability();
var entity = new Entity(1, 1);

// Configure mock inspection results
inspection.SetEntityInfo(entity, new EntityInfo
{
    Name = "Player",
    Components = new List<ComponentInfo>
    {
        new() { TypeName = "Position", Value = new Position { X = 10, Y = 20 } }
    }
});

// Inspect entity
var info = inspection.Inspect(entity);
Assert.Equal("Player", info.Name);
Assert.Single(info.Components);
```

### MockSystemHookCapability

Tests system execution hooks for profiling and debugging:

```csharp
var hooks = new MockSystemHookCapability();

// Register hooks via plugin
plugin.Install(context.SetCapability<ISystemHookCapability>(hooks));

// Verify hooks were registered
Assert.True(hooks.WasHookAdded);
Assert.Equal(2, hooks.HookCount);

// Simulate system execution to test hook behavior
hooks.SimulateSystemExecution(mockSystem, 0.016f);
```

## Testing Plugins

### Basic Plugin Test

```csharp
[Fact]
public void MyPlugin_RegistersSystems()
{
    // Arrange
    var mockContext = new MockPluginContext("TestWorld");

    // Act
    var plugin = new MyPlugin();
    plugin.Install(mockContext);

    // Assert
    Assert.Equal(3, mockContext.RegisteredSystems.Count);
    Assert.Contains(mockContext.RegisteredSystems,
        s => s.SystemType == typeof(MovementSystem));
}
```

### Testing with Capabilities

```csharp
[Fact]
public void CombatPlugin_RegistersPrefabs()
{
    // Arrange
    var mockPrefabs = new MockPrefabCapability();
    var mockContext = new MockPluginContext("TestWorld")
        .WithCapability<IPrefabCapability>(mockPrefabs);

    // Act
    var plugin = new CombatPlugin();
    plugin.Install(mockContext);

    // Assert
    Assert.Contains("Sword", mockPrefabs.RegistrationOrder);
    Assert.Contains("Arrow", mockPrefabs.RegistrationOrder);
}
```

### Testing Plugin Cleanup

```csharp
[Fact]
public void MyPlugin_CleansUpOnUninstall()
{
    // Arrange
    var mockContext = new MockPluginContext("TestWorld");
    var plugin = new MyPlugin();
    plugin.Install(mockContext);

    // Act
    plugin.Uninstall(mockContext);

    // Assert
    Assert.Empty(mockContext.Extensions);
}
```

## Testing Systems

For system testing, you typically want a real World for integration tests, but can use mocks for unit tests:

### Integration Test with Real World

```csharp
[Fact]
public void MovementSystem_UpdatesPositions()
{
    using var world = new World();
    var system = new MovementSystem();
    world.AddSystem(system);

    var entity = world.Spawn()
        .With(new Position { X = 0, Y = 0 })
        .With(new Velocity { X = 10, Y = 0 })
        .Build();

    world.Update(1.0f);

    ref var pos = ref world.Get<Position>(entity);
    Assert.Equal(10, pos.X);
}
```

### Unit Test with MockWorld

```csharp
[Fact]
public void RenderSystem_SkipsInvisibleEntities()
{
    var mockWorld = new MockWorld();
    var renderer = new MockRenderer();

    // Set up test entity as invisible
    mockWorld.SetupEntity(entity, visible: false);

    var system = new RenderSystem(renderer);
    system.Initialize(mockWorld);
    system.Update(0.016f);

    Assert.Empty(renderer.RenderedEntities);
}
```

## Mock Graphics Infrastructure

For rendering tests:

```csharp
var mock2D = new Mock2DRenderer();
var mockText = new MockTextRenderer();
var mockFont = new MockFontManager();

// Use in tests
mock2D.FillRect(bounds, color);
Assert.Single(mock2D.DrawnRects);
```

## Mock Platform Infrastructure

For input and platform tests:

```csharp
var mockInput = new MockInputState();
mockInput.SetKeyDown(Key.Space);
mockInput.SetMousePosition(100, 200);

Assert.True(mockInput.IsKeyDown(Key.Space));
Assert.Equal(100, mockInput.MouseX);
```

## Best Practices

### Do

- Use `MockPluginContext` for plugin installation tests
- Use capability mocks for testing specific World features
- Use real `World` for integration tests
- Reset mock state between tests with `.Clear()` methods

### Don't

- Over-mock - sometimes a real World is simpler
- Test implementation details - test behavior
- Share mock state between tests

### Test Naming Convention

```csharp
// Pattern: MethodName_Scenario_ExpectedResult
[Fact]
public void SetParent_WithValidEntities_UpdatesHierarchy() { }

[Fact]
public void RegisterPrefab_WithDuplicateName_ThrowsArgumentException() { }

[Fact]
public void Update_WhenDisabled_SkipsExecution() { }
```

## Next Steps

- [Plugins Guide](plugins.md) - Plugin architecture and capabilities
- [Systems Guide](systems.md) - System design patterns
- [Events Guide](events.md) - Testing event handlers
