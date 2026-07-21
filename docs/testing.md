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

### Unit Test with TestWorld

Build an isolated world with `TestWorldBuilder`, opting into only the mocks the test needs. `TestWorld.Step()` advances the manual clock and runs one update; the mock renderer records every draw call it received:

```csharp
[Fact]
public void RenderSystem_RecordsDrawCommands()
{
    using var test = new TestWorldBuilder()
        .WithManualTime()
        .WithMock2DRenderer()
        .Build();

    // Arrange entities in test.World and register your render system here...

    test.Step(); // advance one frame at the manual clock's fps

    Assert.NotEmpty(test.Mock2DRenderer!.Commands);
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

## Assertion Helpers

`KeenEyes.Testing` provides FluentAssertions-style extension methods that throw `AssertionException` with a descriptive message on failure. They live in the `KeenEyes.Testing` namespace and work against `Entity`, `World`/`IWorld`, `TestWorld`, and component values directly - no assertion library dependency required.

### Entity Assertions

`EntityAssertions` checks alive/dead state, component presence, tags, and predicate matches:

```csharp
var entity = world.Spawn().With(new Position { X = 10 }).Build();

entity.ShouldBeAlive(world);
entity.ShouldHaveComponent<Position>(world);
entity.ShouldNotHaveComponent<Velocity>(world);
entity.ShouldHaveTag<EnemyTag>(world);
entity.ShouldHaveComponentMatching<Position>(world, p => p.X > 0);
```

Overloads accepting a `TestWorld` instead of `World`/`IWorld` are also available, so assertions can chain directly off `TestWorldBuilder.Build()` results.

### Component Assertions

`ComponentAssertions` operates on component struct values directly - useful after pulling a component out with `world.Get<T>()`:

```csharp
var position = world.Get<Position>(entity);

position.ShouldEqual(new Position { X = 10, Y = 20 });
position.ShouldMatch(p => p.X >= 0 && p.Y >= 0, "position should be in positive quadrant");
position.ShouldHaveField(p => p.X, 10);
position.ShouldHaveFieldInRange(p => p.X, 0f, 100f);
position.ShouldBeDefault();
```

`ShouldHaveField` and `ShouldHaveFieldMatching` take an `Expression<Func<T, TField>>` field selector and are reflection-free (AOT-compatible) - the expression tree is compiled once and the field name is only used for the failure message.

### World Assertions

`WorldAssertions` checks entity counts, installed plugins, and query results:

```csharp
world.ShouldHaveEntityCount(2);
world.ShouldNotBeEmpty();
world.ShouldHavePlugin<PhysicsPlugin>();
world.ShouldNotHavePlugin<RenderingPlugin>();
world.ShouldContainEntitiesWith<Position, Velocity>();
world.ShouldContainExactlyWith<EnemyTag>(5);
```

Every assertion accepts an optional `because` string that is appended to the failure message.

## Snapshot Testing

`KeenEyes.Testing.Snapshots` captures the full state of a world (or a subset of entities) as plain data, then diffs two captures to verify that an operation produced exactly the changes you expect - or none at all.

### Capturing Snapshots

`WorldSnapshot.Create(world)` walks every live entity and records an `EntitySnapshot` per entity (ID, version, name, and a `Dictionary<string, Dictionary<string, object?>>` of component field values, keyed by component type name):

```csharp
using KeenEyes.Testing.Snapshots;

var before = WorldSnapshot.Create(world);
world.Update(1.0f);
var after = WorldSnapshot.Create(world);
```

An overload, `WorldSnapshot.Create(world, entities)`, captures only the specified entities. `WorldSnapshot` exposes `EntityCount`, `EntityIds`, `GetEntity(id)`, `EntitiesWithComponent<T>()`, and `AllComponentTypes` for inspecting a capture directly.

### Comparing Snapshots

`SnapshotComparer.Compare(expected, actual)` returns a `SnapshotComparison` describing every difference - added/removed entities, version or name changes, added/removed components, and per-field value changes - each as a `SnapshotDifference` with a `DifferenceType` (`EntityAdded`, `ComponentRemoved`, `FieldChanged`, etc.):

```csharp
var comparison = SnapshotComparer.Compare(before, after);

if (!comparison.AreEqual)
{
    Console.WriteLine(comparison.GetReport());
}
```

`SnapshotComparer.CompareEntities(expected, actual)` compares two `EntitySnapshot` instances directly when you only care about one entity.

### Snapshot Assertions

`SnapshotAssertions` wraps the comparer in fluent, `AssertionException`-throwing checks:

```csharp
after.ShouldEqual(before);
after.ShouldHaveEntityCount(before.EntityCount);
after.ShouldContainEntity(entity.Id);
after.ShouldHaveEntitiesWithComponent<Health>();

comparison.ShouldBeEqual();
comparison.ShouldHaveDifferenceCount(1);
```

`EntitySnapshot` has its own `ShouldHaveComponent<T>()`, `ShouldNotHaveComponent<T>()`, and `ShouldEqual()` for entity-level checks.

## Recording and Playback

Two recorders capture activity during a test run for later inspection or replay.

### InputRecorder / InputPlayer

`KeenEyes.Testing.Input.InputRecorder` subscribes to an `IInputContext` (keyboard, mouse, gamepad events) and records each event as a timestamped `RecordedInputEvent` (e.g. `RecordedKeyDownEvent`, `RecordedMouseMoveEvent`, `RecordedGamepadAxisEvent`). Pairing it with a `TestClock` synchronizes timestamps to simulation time:

```csharp
using KeenEyes.Testing.Input;

using var testWorld = new TestWorldBuilder()
    .WithManualTime()
    .WithMockInput()
    .Build();

var recorder = new InputRecorder(testWorld.MockInput!, testWorld.Clock);

recorder.StartRecording(name: "jump-sequence");
testWorld.MockInput!.SimulateKeyDown(Key.Space);
testWorld.Step();
testWorld.MockInput.SimulateKeyUp(Key.Space);

var recording = recorder.StopRecording();
var json = recording.ToJson(); // or recording.ToBinary()
```

`InputRecording` can round-trip through `ToJson()`/`FromJson()` or `ToBinary()`/`FromBinary()`, making recorded sequences reusable as fixtures across test runs.

`InputPlayer` plays a loaded `InputRecording` back through a `MockInputContext`, firing each event once the `TestClock` reaches its timestamp:

```csharp
var player = new InputPlayer(testWorld.MockInput!, testWorld.Clock!);

player.LoadRecording(InputRecording.FromJson(json));
player.Play();

while (player.IsPlaying)
{
    player.Update();
    testWorld.Step();
}
```

`InputPlayer` also supports `Pause()`, `Stop()`, `Seek(positionMs)`, and an `OnPlaybackComplete` event.

### SystemRecorder

`KeenEyes.Testing.Systems.SystemRecorder` attaches to a world's system hooks (`AttachTo(world, phase)`) and records every system execution as a `SystemCall` (system type, name, delta time, and UTC timestamp). `TestWorldBuilder.WithSystemRecording()` wires one up automatically and exposes it via `TestWorld.SystemRecorder`:

```csharp
using var testWorld = new TestWorldBuilder()
    .WithSystemRecording()
    .WithSystem<MovementSystem>()
    .WithManualTime()
    .Build();

testWorld.Step();

var recorder = testWorld.SystemRecorder!;
recorder
    .ShouldHaveCalledSystem<MovementSystem>()
    .ShouldHaveCalledSystemTimes<MovementSystem>(1);

Assert.Equal(1, recorder.GetCallCount<MovementSystem>());
```

`SystemRecorderAssertions` adds `ShouldHaveCalledSystemAtLeast<T>()`, `ShouldNotHaveCalledSystem<T>()`, `ShouldHaveTotalCallCount()`, `ShouldHaveNoCalls()`, and `ShouldHaveAccumulatedDeltaTime<T>()`. `SystemRecorder` itself also exposes `GetCalls<T>()`, `GetLastCall<T>()`, `GetTotalDeltaTime<T>()`, and `Clear()` for direct inspection.

## Test Fixtures

`KeenEyes.Testing.Fixtures` provides ready-made components and entity builders so tests don't need to declare throwaway component types.

### CommonComponents

A set of `[Component]`-generated structs prefixed `Test` (`TestPosition`, `TestPosition3D`, `TestVelocity`, `TestHealth`, `TestDamage`, `TestSpeed`, `TestRotation`, `TestScale`, `TestLifetime`, `TestTeam`, `TestCounter`) plus `[TagComponent]` tags (`PlayerTag`, `EnemyTag`, `ProjectileTag`, `PickupTag`, `DeadTag`, `ActiveTag`, `DisabledTag`, `InvulnerableTag`). Each component has a `Create(...)` factory and most have convenience properties like `TestHealth.Full(max)`, `TestPosition.Zero`, or `TestHealth.Percentage`:

```csharp
using KeenEyes.Testing.Fixtures;

var entity = world.Spawn()
    .With(TestPosition.Create(0, 0))
    .With(TestHealth.Full(100))
    .WithTag<EnemyTag>()
    .Build();
```

### EntityPresets / EntityPresetBuilder

`EntityPresets` provides factory methods - `Player(world)`, `Enemy(world)`, `Projectile(world)`, `Pickup(world)`, `MovingEntity(world)` - that return a fluent `EntityPresetBuilder` pre-populated with sensible defaults built from the `CommonComponents` above:

```csharp
using var world = new World();

var player = EntityPresets.Player(world)
    .WithName("Player1")
    .AtPosition(100, 50)
    .WithHealth(100)
    .Build();
```

`EntityPresetBuilder` supports `WithName`, `AtPosition`, `WithVelocity`, `WithHealth` (single value or current/max pair), `WithDamage`, `WithSpeed`, `WithLifetime`, `OnTeam`, and `WithTag<T>()`, finished with `Build()`.

For batches, `EntityPresets.CreatePlayers(world, count)`, `CreateEnemies(world, count)`, and `CreateProjectiles(world, count)` return a `BatchEntityBuilder` that applies the same modifiers - `WithHealth`, `WithDamage`, `WithSpeed`, `InGrid(columns, spacing)`, `InLine(spacing, horizontal)`, `WithSequentialTeams()`, `WithAlternatingTeams(teamA, teamB)`, or a custom `WithModifier((builder, index) => ...)` - to every entity before calling `Build()`, which returns an `Entity[]`:

```csharp
var enemies = EntityPresets.CreateEnemies(world, count: 5)
    .WithHealth(50)
    .InGrid(columns: 5, spacing: 32f)
    .Build();
```

## Next Steps

- [TestBridge Architecture Guide](testbridge.md) - TestBridge for external tool integration and automated testing
- [MCP Server Quick Start](mcp-server.md) - MCP server for AI tool integration
- [Plugins Guide](plugins.md) - Plugin architecture and capabilities
- [Systems Guide](systems.md) - System design patterns
- [Events Guide](events.md) - Testing event handlers
