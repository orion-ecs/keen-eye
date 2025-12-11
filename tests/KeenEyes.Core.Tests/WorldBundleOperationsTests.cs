using KeenEyes;

namespace KeenEyes.Tests;

/// <summary>
/// Test components for bundle operations tests.
/// Note: We use IComponent directly since generators don't run on test projects.
/// </summary>
public struct BundleTestPosition : IComponent
{
    public float X;
    public float Y;
}

public struct BundleTestVelocity : IComponent
{
    public float X;
    public float Y;
}

public struct BundleTestRotation : IComponent
{
    public float Angle;
}

public struct BundleTestScale : IComponent
{
    public float X;
    public float Y;
}

public struct BundleTestHealth : IComponent
{
    public int Current;
    public int Max;
}

public struct BundleTestPlayerTag : ITagComponent { }

/// <summary>
/// Integration tests for World bundle Add/Remove operations.
/// Tests the runtime behavior of generated bundle extension methods.
/// </summary>
/// <remarks>
/// Note: These tests simulate what generated bundle extensions would do.
/// Direct integration testing of bundle operations requires a separate test project that
/// references a project with [Bundle] definitions, so the source generator can run.
/// The generator tests (WorldBundleExtensionsTests) verify the actual code generation.
/// </remarks>
public class WorldBundleOperationsTests
{
    #region Manual Bundle Operations (Simulating Generated Code)

    // Helper methods that simulate what the generated bundle extensions would do
    private static void AddTransformBundle(World world, Entity entity,
        BundleTestPosition position, BundleTestRotation rotation, BundleTestScale scale)
    {
        world.Add(entity, position);
        world.Add(entity, rotation);
        world.Add(entity, scale);
    }

    private static void RemoveTransformBundle(World world, Entity entity)
    {
        world.Remove<BundleTestPosition>(entity);
        world.Remove<BundleTestRotation>(entity);
        world.Remove<BundleTestScale>(entity);
    }

    private static void AddPhysicsBundle(World world, Entity entity,
        BundleTestPosition position, BundleTestVelocity velocity)
    {
        world.Add(entity, position);
        world.Add(entity, velocity);
    }

    private static void RemovePhysicsBundle(World world, Entity entity)
    {
        world.Remove<BundleTestPosition>(entity);
        world.Remove<BundleTestVelocity>(entity);
    }

    private static void AddPlayerBundle(World world, Entity entity,
        BundleTestPosition position, BundleTestHealth health, BundleTestPlayerTag tag)
    {
        world.Add(entity, position);
        world.Add(entity, health);
        world.Add(entity, tag);
    }

    private static void RemovePlayerBundle(World world, Entity entity)
    {
        world.Remove<BundleTestPosition>(entity);
        world.Remove<BundleTestHealth>(entity);
        world.Remove<BundleTestPlayerTag>(entity);
    }

    #endregion

    #region Add Bundle Tests

    [Fact]
    public void AddBundle_AddsAllComponentsToEntity()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        // Add bundle to existing entity (simulating generated extension)
        AddTransformBundle(world, entity,
            new BundleTestPosition { X = 10, Y = 20 },
            new BundleTestRotation { Angle = 45 },
            new BundleTestScale { X = 2, Y = 2 });

        // Verify all components exist
        Assert.True(world.Has<BundleTestPosition>(entity));
        Assert.True(world.Has<BundleTestRotation>(entity));
        Assert.True(world.Has<BundleTestScale>(entity));
    }

    [Fact]
    public void AddBundle_PreservesComponentValues()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var position = new BundleTestPosition { X = 100, Y = 200 };
        var rotation = new BundleTestRotation { Angle = 90 };
        var scale = new BundleTestScale { X = 3, Y = 3 };

        AddTransformBundle(world, entity, position, rotation, scale);

        // Verify values are preserved
        Assert.Equal(100, world.Get<BundleTestPosition>(entity).X);
        Assert.Equal(200, world.Get<BundleTestPosition>(entity).Y);
        Assert.Equal(90, world.Get<BundleTestRotation>(entity).Angle);
        Assert.Equal(3, world.Get<BundleTestScale>(entity).X);
        Assert.Equal(3, world.Get<BundleTestScale>(entity).Y);
    }

    [Fact]
    public void AddBundle_WithDeadEntity_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        // Attempting to add bundle to dead entity should throw
        Assert.Throws<InvalidOperationException>(() =>
            AddTransformBundle(world,
                entity,
                new BundleTestPosition { X = 0, Y = 0 },
                new BundleTestRotation { Angle = 0 },
                new BundleTestScale { X = 1, Y = 1 }));
    }

    [Fact]
    public void AddBundle_FailsIfComponentAlreadyExists()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestPosition { X = 1, Y = 1 })
            .Build();

        // Add bundle when position already exists should throw
        // (This matches World.Add() behavior - use Set() to replace)
        Assert.Throws<InvalidOperationException>(() =>
            AddPhysicsBundle(world,
                entity,
                new BundleTestPosition { X = 10, Y = 20 },
                new BundleTestVelocity { X = 5, Y = 5 }));
    }

    [Fact]
    public void AddBundle_WithTagComponent_Works()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        AddPlayerBundle(world,
            entity,
            new BundleTestPosition { X = 0, Y = 0 },
            new BundleTestHealth { Current = 100, Max = 100 },
            new BundleTestPlayerTag());

        // Verify all components including tag
        Assert.True(world.Has<BundleTestPosition>(entity));
        Assert.True(world.Has<BundleTestHealth>(entity));
        Assert.True(world.Has<BundleTestPlayerTag>(entity));
    }

    [Fact]
    public void AddBundle_TriggersArchetypeTransition()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        // Entity starts with no components (empty archetype)
        Assert.False(world.Has<BundleTestPosition>(entity));

        // Add bundle
        AddPhysicsBundle(world,
            entity,
            new BundleTestPosition { X = 0, Y = 0 },
            new BundleTestVelocity { X = 1, Y = 1 });

        // Entity should have transitioned to new archetype
        Assert.True(world.Has<BundleTestPosition>(entity));
        Assert.True(world.Has<BundleTestVelocity>(entity));

        // Should be queryable
        var results = world.Query<BundleTestPosition, BundleTestVelocity>().ToList();
        Assert.Single(results);
        Assert.Equal(entity, results[0]);
    }

    #endregion

    #region Remove Bundle Tests

    [Fact]
    public void RemoveBundle_RemovesAllComponents()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestPosition { X = 10, Y = 20 })
            .With(new BundleTestRotation { Angle = 45 })
            .With(new BundleTestScale { X = 2, Y = 2 })
            .Build();

        // Verify components exist
        Assert.True(world.Has<BundleTestPosition>(entity));
        Assert.True(world.Has<BundleTestRotation>(entity));
        Assert.True(world.Has<BundleTestScale>(entity));

        // Remove bundle
        RemoveTransformBundle(world, entity);

        // Verify all components removed
        Assert.False(world.Has<BundleTestPosition>(entity));
        Assert.False(world.Has<BundleTestRotation>(entity));
        Assert.False(world.Has<BundleTestScale>(entity));
    }

    [Fact]
    public void RemoveBundle_WithDeadEntity_DoesNotThrow()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        // Should not throw (Remove is idempotent)
        RemoveTransformBundle(world, entity);
    }

    [Fact]
    public void RemoveBundle_PartialRemoval_OnlyRemovesExistingComponents()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestPosition { X = 10, Y = 20 })
            // Note: BundleTestRotation and BundleTestScale are NOT added
            .Build();

        // Entity only has BundleTestPosition, not full TransformBundle
        Assert.True(world.Has<BundleTestPosition>(entity));
        Assert.False(world.Has<BundleTestRotation>(entity));
        Assert.False(world.Has<BundleTestScale>(entity));

        // Remove bundle (should only remove BundleTestPosition)
        RemoveTransformBundle(world, entity);

        // BundleTestPosition should be removed
        Assert.False(world.Has<BundleTestPosition>(entity));

        // No error should occur for missing components
    }

    [Fact]
    public void RemoveBundle_IsIdempotent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestPosition { X = 10, Y = 20 })
            .With(new BundleTestVelocity { X = 1, Y = 1 })
            .Build();

        // Remove bundle first time
        RemovePhysicsBundle(world, entity);
        Assert.False(world.Has<BundleTestPosition>(entity));
        Assert.False(world.Has<BundleTestVelocity>(entity));

        // Remove bundle again (should not throw)
        RemovePhysicsBundle(world, entity);
    }

    [Fact]
    public void RemoveBundle_TriggersArchetypeTransition()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestPosition { X = 0, Y = 0 })
            .With(new BundleTestVelocity { X = 1, Y = 1 })
            .Build();

        // Entity should match query
        var beforeRemove = world.Query<BundleTestPosition, BundleTestVelocity>().ToList();
        Assert.Single(beforeRemove);

        // Remove bundle
        RemovePhysicsBundle(world, entity);

        // Entity should no longer match query
        var afterRemove = world.Query<BundleTestPosition, BundleTestVelocity>().ToList();
        Assert.Empty(afterRemove);
    }

    [Fact]
    public void RemoveBundle_WithTagComponent_Works()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestPosition { X = 0, Y = 0 })
            .With(new BundleTestHealth { Current = 100, Max = 100 })
            .WithTag<BundleTestPlayerTag>()
            .Build();

        Assert.True(world.Has<BundleTestPlayerTag>(entity));

        // Remove bundle
        RemovePlayerBundle(world, entity);

        // All components including tag should be removed
        Assert.False(world.Has<BundleTestPosition>(entity));
        Assert.False(world.Has<BundleTestHealth>(entity));
        Assert.False(world.Has<BundleTestPlayerTag>(entity));
    }

    #endregion

    #region Combined Add/Remove Tests

    [Fact]
    public void AddRemoveBundle_RoundTrip_Works()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        // Add bundle
        AddPhysicsBundle(world,
            entity,
            new BundleTestPosition { X = 10, Y = 20 },
            new BundleTestVelocity { X = 5, Y = 5 });

        Assert.True(world.Has<BundleTestPosition>(entity));
        Assert.True(world.Has<BundleTestVelocity>(entity));

        // Remove bundle
        RemovePhysicsBundle(world, entity);

        Assert.False(world.Has<BundleTestPosition>(entity));
        Assert.False(world.Has<BundleTestVelocity>(entity));
    }

    [Fact]
    public void AddBundle_WithOtherComponents_OnlyAddsBundle()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestHealth { Current = 100, Max = 100 })
            .Build();

        // Add bundle
        AddPhysicsBundle(world,
            entity,
            new BundleTestPosition { X = 0, Y = 0 },
            new BundleTestVelocity { X = 1, Y = 1 });

        // Bundle components added
        Assert.True(world.Has<BundleTestPosition>(entity));
        Assert.True(world.Has<BundleTestVelocity>(entity));

        // Other components unchanged
        Assert.True(world.Has<BundleTestHealth>(entity));
    }

    [Fact]
    public void RemoveBundle_WithOtherComponents_OnlyRemovesBundle()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BundleTestPosition { X = 0, Y = 0 })
            .With(new BundleTestVelocity { X = 1, Y = 1 })
            .With(new BundleTestHealth { Current = 100, Max = 100 })
            .Build();

        // Remove physics bundle
        RemovePhysicsBundle(world, entity);

        // Bundle components removed
        Assert.False(world.Has<BundleTestPosition>(entity));
        Assert.False(world.Has<BundleTestVelocity>(entity));

        // Other components unchanged
        Assert.True(world.Has<BundleTestHealth>(entity));
    }

    #endregion
}
