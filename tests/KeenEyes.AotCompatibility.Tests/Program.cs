// AOT Compatibility Test
// This project validates that KeenEyes.Core compiles and runs correctly with Native AOT.
// It exercises all major ECS features to ensure no reflection-based code paths remain.

using KeenEyes;

Console.WriteLine("KeenEyes AOT Compatibility Test");
Console.WriteLine("================================");

var passed = 0;
var failed = 0;

void Test(string name, Action action)
{
    try
    {
        action();
        Console.WriteLine($"[PASS] {name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] {name}: {ex.Message}");
        failed++;
    }
}

// Test 1: World creation
Test("World creation", () =>
{
    using var world = new World();
    if (world is null)
    {
        throw new Exception("World is null");
    }
});

// Test 2: Entity spawn
Test("Entity spawn", () =>
{
    using var world = new World();
    var entity = world.Spawn().Build();
    if (!world.IsAlive(entity))
    {
        throw new Exception("Entity not alive");
    }
});

// Test 3: Component registration and addition
Test("Component registration and addition", () =>
{
    using var world = new World();
    world.Components.Register<Position>();
    var entity = world.Spawn()
        .With(new Position { X = 10, Y = 20 })
        .Build();

    ref var pos = ref world.Get<Position>(entity);
    if (pos.X != 10 || pos.Y != 20)
    {
        throw new Exception($"Position mismatch: {pos.X}, {pos.Y}");
    }
});

// Test 4: Multiple components
Test("Multiple components", () =>
{
    using var world = new World();
    world.Components.Register<Position>();
    world.Components.Register<Velocity>();

    var entity = world.Spawn()
        .With(new Position { X = 0, Y = 0 })
        .With(new Velocity { Dx = 1, Dy = 2 })
        .Build();

    ref var vel = ref world.Get<Velocity>(entity);
    if (vel.Dx != 1 || vel.Dy != 2)
    {
        throw new Exception($"Velocity mismatch: {vel.Dx}, {vel.Dy}");
    }
});

// Test 5: Tag components
Test("Tag components", () =>
{
    using var world = new World();
    world.Components.Register<EnemyTag>();

    var entity = world.Spawn()
        .WithTag<EnemyTag>()
        .Build();

    if (!world.Has<EnemyTag>(entity))
    {
        throw new Exception("Entity missing EnemyTag");
    }
});

// Test 6: Query iteration
Test("Query iteration", () =>
{
    using var world = new World();
    world.Components.Register<Position>();
    world.Components.Register<Velocity>();

    for (int i = 0; i < 10; i++)
    {
        world.Spawn()
            .With(new Position { X = i, Y = i })
            .With(new Velocity { Dx = 1, Dy = 1 })
            .Build();
    }

    var count = 0;
    foreach (var entity in world.Query<Position, Velocity>())
    {
        ref var pos = ref world.Get<Position>(entity);
        ref var vel = ref world.Get<Velocity>(entity);
        pos.X += vel.Dx;
        pos.Y += vel.Dy;
        count++;
    }

    if (count != 10)
    {
        throw new Exception($"Query count mismatch: {count}");
    }
});

// Test 7: Query With filter
Test("Query With filter", () =>
{
    using var world = new World();
    world.Components.Register<Position>();
    world.Components.Register<EnemyTag>();
    world.Components.Register<PlayerTag>();

    // 5 enemies, 3 players
    for (int i = 0; i < 5; i++)
    {
        world.Spawn()
            .With(new Position { X = i, Y = i })
            .WithTag<EnemyTag>()
            .Build();
    }

    for (int i = 0; i < 3; i++)
    {
        world.Spawn()
            .With(new Position { X = i, Y = i })
            .WithTag<PlayerTag>()
            .Build();
    }

    var enemyCount = 0;
    foreach (var entity in world.Query<Position>().With<EnemyTag>())
    {
        enemyCount++;
    }

    if (enemyCount != 5)
    {
        throw new Exception($"Enemy count mismatch: {enemyCount}");
    }
});

// Test 8: Query Without filter
Test("Query Without filter", () =>
{
    using var world = new World();
    world.Components.Register<Position>();
    world.Components.Register<FrozenTag>();

    // 7 moving entities, 3 frozen
    for (int i = 0; i < 7; i++)
    {
        world.Spawn()
            .With(new Position { X = i, Y = i })
            .Build();
    }

    for (int i = 0; i < 3; i++)
    {
        world.Spawn()
            .With(new Position { X = i, Y = i })
            .WithTag<FrozenTag>()
            .Build();
    }

    var movingCount = 0;
    foreach (var entity in world.Query<Position>().Without<FrozenTag>())
    {
        movingCount++;
    }

    if (movingCount != 7)
    {
        throw new Exception($"Moving count mismatch: {movingCount}");
    }
});

// Test 9: Entity despawn
Test("Entity despawn", () =>
{
    using var world = new World();
    world.Components.Register<Position>();

    var entity = world.Spawn()
        .With(new Position { X = 1, Y = 2 })
        .Build();

    if (!world.IsAlive(entity))
    {
        throw new Exception("Entity should be alive before despawn");
    }

    world.Despawn(entity);

    if (world.IsAlive(entity))
    {
        throw new Exception("Entity should not be alive after despawn");
    }
});

// Test 10: Component add/remove on existing entity
Test("Component add/remove", () =>
{
    using var world = new World();
    world.Components.Register<Position>();
    world.Components.Register<Velocity>();

    var entity = world.Spawn()
        .With(new Position { X = 0, Y = 0 })
        .Build();

    if (world.Has<Velocity>(entity))
    {
        throw new Exception("Entity should not have Velocity yet");
    }

    world.Add(entity, new Velocity { Dx = 5, Dy = 5 });

    if (!world.Has<Velocity>(entity))
    {
        throw new Exception("Entity should have Velocity after Add");
    }

    world.Remove<Velocity>(entity);

    if (world.Has<Velocity>(entity))
    {
        throw new Exception("Entity should not have Velocity after Remove");
    }
});

// Test 11: Systems
Test("System registration and execution", () =>
{
    using var world = new World();
    world.Components.Register<Position>();
    world.Components.Register<Velocity>();

    var system = new MovementSystem();
    world.AddSystem(system);

    world.Spawn()
        .With(new Position { X = 0, Y = 0 })
        .With(new Velocity { Dx = 10, Dy = 20 })
        .Build();

    world.Update(1.0f);

    // Check that system ran (position should be updated)
    foreach (var entity in world.Query<Position>())
    {
        ref var pos = ref world.Get<Position>(entity);
        if (pos.X != 10 || pos.Y != 20)
        {
            throw new Exception($"Position not updated by system: {pos.X}, {pos.Y}");
        }
    }
});

// Test 12: WorldBuilder
Test("WorldBuilder with factory delegates", () =>
{
    var world = new WorldBuilder()
        .WithSystem<MovementSystem>()
        .Build();

    world.Components.Register<Position>();
    world.Components.Register<Velocity>();

    world.Spawn()
        .With(new Position { X = 0, Y = 0 })
        .With(new Velocity { Dx = 5, Dy = 10 })
        .Build();

    world.Update(1.0f);

    foreach (var entity in world.Query<Position>())
    {
        ref var pos = ref world.Get<Position>(entity);
        if (pos.X != 5 || pos.Y != 10)
        {
            throw new Exception($"WorldBuilder system didn't execute: {pos.X}, {pos.Y}");
        }
    }

    world.Dispose();
});

// Test 13: Singletons
Test("Singleton resources", () =>
{
    using var world = new World();
    world.SetSingleton(new GameConfig { TimeScale = 2.0f });

    ref var config = ref world.GetSingleton<GameConfig>();
    if (config.TimeScale != 2.0f)
    {
        throw new Exception($"Singleton mismatch: {config.TimeScale}");
    }
});

// Test 14: Named entity spawn
Test("Named entity spawn", () =>
{
    using var world = new World();

    var entity = world.Spawn("Player").Build();

    var name = world.GetName(entity);
    if (name != "Player")
    {
        throw new Exception($"Name mismatch: {name}");
    }
});

// Test 15: Events
Test("Component events", () =>
{
    using var world = new World();
    world.Components.Register<Position>();

    var addedCount = 0;
    var removedCount = 0;

    world.OnComponentAdded<Position>((entity, pos) => addedCount++);
    world.OnComponentRemoved<Position>(entity => removedCount++);

    var entity = world.Spawn()
        .With(new Position { X = 0, Y = 0 })
        .Build();

    if (addedCount != 1)
    {
        throw new Exception($"OnComponentAdded not fired: {addedCount}");
    }

    // Use explicit Remove<T> - OnComponentRemoved fires on explicit removal, not Despawn
    world.Remove<Position>(entity);

    if (removedCount != 1)
    {
        throw new Exception($"OnComponentRemoved not fired: {removedCount}");
    }
});

// Summary
Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed");

if (failed > 0)
{
    Console.WriteLine("AOT COMPATIBILITY TEST FAILED");
    return 1;
}

Console.WriteLine("AOT COMPATIBILITY TEST PASSED");
return 0;

// Component definitions
public struct Position : IComponent
{
    public float X;
    public float Y;
}

public struct Velocity : IComponent
{
    public float Dx;
    public float Dy;
}

public struct EnemyTag : ITagComponent;
public struct PlayerTag : ITagComponent;
public struct FrozenTag : ITagComponent;

public struct GameConfig
{
    public float TimeScale;
}

// System definition
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.Dx * deltaTime;
            pos.Y += vel.Dy * deltaTime;
        }
    }
}
