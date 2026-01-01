using KeenEyes;
using Health = KeenEyes.Tests.TestHealth;
using Position = KeenEyes.Tests.TestPosition;
using Rotation = KeenEyes.Tests.TestRotation;
using Scale = KeenEyes.Tests.TestScale;
using Velocity = KeenEyes.Tests.TestVelocity;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for archetype pre-allocation optimization.
/// </summary>
public class ArchetypePreallocationTests
{
    #region Helper Methods

    /// <summary>
    /// Helper method to register all test components with a world.
    /// </summary>
    private static void RegisterTestComponents(World world)
    {
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();
        world.Components.Register<Rotation>();
        world.Components.Register<Scale>();
        world.Components.Register<Health>();
    }

    #endregion

    #region ArchetypeManager PreallocateArchetype Tests

    [Fact]
    public void PreallocateArchetype_CreatesArchetype()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var archetypeManager = world.ArchetypeManager;

        // Pre-allocate archetype
        var archetype = archetypeManager.PreallocateArchetype([typeof(Position), typeof(Velocity)]);

        // Verify archetype was created
        Assert.NotNull(archetype);
        Assert.True(archetype.Has<Position>());
        Assert.True(archetype.Has<Velocity>());
    }

    [Fact]
    public void PreallocateArchetype_IdempotentCreation()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var archetypeManager = world.ArchetypeManager;

        // Pre-allocate same archetype twice
        var archetype1 = archetypeManager.PreallocateArchetype([typeof(Position), typeof(Velocity)]);
        var archetype2 = archetypeManager.PreallocateArchetype([typeof(Position), typeof(Velocity)]);

        // Should return the same archetype instance
        Assert.Same(archetype1, archetype2);
    }

    [Fact]
    public void PreallocateArchetype_IncludesInArchetypesList()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var archetypeManager = world.ArchetypeManager;
        var initialCount = archetypeManager.ArchetypeCount;

        // Pre-allocate archetype
        archetypeManager.PreallocateArchetype([typeof(Position), typeof(Velocity)]);

        // Verify archetype count increased
        Assert.Equal(initialCount + 1, archetypeManager.ArchetypeCount);
    }

    [Fact]
    public void PreallocateArchetype_WithSingleComponent_Works()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var archetypeManager = world.ArchetypeManager;

        var archetype = archetypeManager.PreallocateArchetype([typeof(Position)]);

        Assert.NotNull(archetype);
        Assert.True(archetype.Has<Position>());
        Assert.False(archetype.Has<Velocity>());
    }

    [Fact]
    public void PreallocateArchetype_WithMultipleComponents_Works()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var archetypeManager = world.ArchetypeManager;

        var archetype = archetypeManager.PreallocateArchetype([
            typeof(Position),
            typeof(Velocity),
            typeof(Rotation),
            typeof(Scale)
        ]);

        Assert.NotNull(archetype);
        Assert.True(archetype.Has<Position>());
        Assert.True(archetype.Has<Velocity>());
        Assert.True(archetype.Has<Rotation>());
        Assert.True(archetype.Has<Scale>());
    }

    #endregion

    #region World PreallocateArchetype API Tests

    [Fact]
    public void World_PreallocateArchetype_SingleComponent_Works()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var initialCount = world.ArchetypeManager.ArchetypeCount;

        world.PreallocateArchetype<Position>();

        // Verify archetype was created
        Assert.Equal(initialCount + 1, world.ArchetypeManager.ArchetypeCount);
    }

    [Fact]
    public void World_PreallocateArchetype_TwoComponents_Works()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var initialCount = world.ArchetypeManager.ArchetypeCount;

        world.PreallocateArchetype<Position, Velocity>();

        // Verify archetype was created
        Assert.Equal(initialCount + 1, world.ArchetypeManager.ArchetypeCount);
    }

    [Fact]
    public void World_PreallocateArchetype_ThreeComponents_Works()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var initialCount = world.ArchetypeManager.ArchetypeCount;

        world.PreallocateArchetype<Position, Velocity, Rotation>();

        // Verify archetype was created
        Assert.Equal(initialCount + 1, world.ArchetypeManager.ArchetypeCount);
    }

    [Fact]
    public void World_PreallocateArchetype_FourComponents_Works()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var initialCount = world.ArchetypeManager.ArchetypeCount;

        world.PreallocateArchetype<Position, Velocity, Rotation, Scale>();

        // Verify archetype was created
        Assert.Equal(initialCount + 1, world.ArchetypeManager.ArchetypeCount);
    }

    [Fact]
    public void World_PreallocateArchetype_IsIdempotent()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var initialCount = world.ArchetypeManager.ArchetypeCount;

        world.PreallocateArchetype<Position, Velocity>();
        world.PreallocateArchetype<Position, Velocity>();

        // Should only create one archetype
        Assert.Equal(initialCount + 1, world.ArchetypeManager.ArchetypeCount);
    }

    #endregion

    #region Performance Optimization Tests

    [Fact]
    public void PreallocateArchetype_ReducesArchetypeTransitions_SingleTransition()
    {
        using var world = new World();
        RegisterTestComponents(world);

        // Pre-allocate archetype for Position + Velocity
        world.PreallocateArchetype<Position, Velocity>();
        var archetypeCountAfterPrealloc = world.ArchetypeManager.ArchetypeCount;

        // Spawn entity with pre-allocated archetype
        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 3, Y = 4 })
            .Build();

        // No new archetypes should be created (single transition to pre-allocated archetype)
        Assert.Equal(archetypeCountAfterPrealloc, world.ArchetypeManager.ArchetypeCount);
        Assert.True(world.Has<Position>(entity));
        Assert.True(world.Has<Velocity>(entity));
    }

    [Fact]
    public void PreallocateArchetype_OptimizesBulkEntityCreation()
    {
        using var world = new World();
        RegisterTestComponents(world);

        // Pre-allocate archetype
        world.PreallocateArchetype<Position, Velocity, Health>();
        var archetypeCountAfterPrealloc = world.ArchetypeManager.ArchetypeCount;

        // Create 100 entities with the same components
        var entities = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            var entity = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
            entities.Add(entity);
        }

        // No additional archetypes should be created
        Assert.Equal(archetypeCountAfterPrealloc, world.ArchetypeManager.ArchetypeCount);

        // All entities should exist and have components
        Assert.Equal(100, entities.Count);
        foreach (var entity in entities)
        {
            Assert.True(world.Has<Position>(entity));
            Assert.True(world.Has<Velocity>(entity));
            Assert.True(world.Has<Health>(entity));
        }
    }

    [Fact]
    public void WithoutPreallocation_CreatesMultipleArchetypesForBuilder()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var initialCount = world.ArchetypeManager.ArchetypeCount;

        // Without pre-allocation, entity builder creates intermediate archetypes
        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 3, Y = 4 })
            .With(new Rotation { Angle = 45 })
            .Build();

        // Multiple archetype transitions likely occurred
        // (empty -> [Position] -> [Position, Velocity] -> [Position, Velocity, Rotation])
        var finalCount = world.ArchetypeManager.ArchetypeCount;

        // We expect at least one archetype to have been created
        Assert.True(finalCount > initialCount);

        // Verify entity has all components
        Assert.True(world.Has<Position>(entity));
        Assert.True(world.Has<Velocity>(entity));
        Assert.True(world.Has<Rotation>(entity));
    }

    [Fact]
    public void PreallocateArchetype_WorksWithQuerySystem()
    {
        using var world = new World();
        RegisterTestComponents(world);

        // Pre-allocate archetype
        world.PreallocateArchetype<Position, Velocity>();

        // Create entities
        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();
        }

        // Query should find all entities
        var results = world.Query<Position, Velocity>().ToList();
        Assert.Equal(10, results.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void PreallocateArchetype_WithNoComponents_CreatesEmptyArchetype()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var archetypeManager = world.ArchetypeManager;

        var archetype = archetypeManager.PreallocateArchetype([]);

        Assert.NotNull(archetype);
        Assert.Empty(archetype.ComponentTypes);
    }

    [Fact]
    public void PreallocateArchetype_AfterEntityCreation_StillWorks()
    {
        using var world = new World();
        RegisterTestComponents(world);

        // Create entity first
        var entity1 = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .Build();

        // Pre-allocate different archetype
        world.PreallocateArchetype<Position, Velocity>();

        // Create entity with pre-allocated archetype
        var entity2 = world.Spawn()
            .With(new Position { X = 3, Y = 4 })
            .With(new Velocity { X = 5, Y = 6 })
            .Build();

        // Both entities should exist
        Assert.True(world.Has<Position>(entity1));
        Assert.False(world.Has<Velocity>(entity1));

        Assert.True(world.Has<Position>(entity2));
        Assert.True(world.Has<Velocity>(entity2));
    }

    [Fact]
    public void PreallocateArchetype_DoesNotAffectExistingEntities()
    {
        using var world = new World();
        RegisterTestComponents(world);

        // Create entity
        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .Build();

        var componentCountBefore = world.ArchetypeManager.GetComponentTypes(entity).Count();

        // Pre-allocate different archetype
        world.PreallocateArchetype<Position, Velocity, Rotation>();

        // Entity components should be unchanged
        var componentCountAfter = world.ArchetypeManager.GetComponentTypes(entity).Count();
        Assert.Equal(componentCountBefore, componentCountAfter);
        Assert.True(world.Has<Position>(entity));
        Assert.False(world.Has<Velocity>(entity));
        Assert.False(world.Has<Rotation>(entity));
    }

    [Fact]
    public void PreallocateArchetype_MultiplePreallocations_AllStored()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var initialCount = world.ArchetypeManager.ArchetypeCount;

        // Pre-allocate multiple different archetypes
        world.PreallocateArchetype<Position>();
        world.PreallocateArchetype<Position, Velocity>();
        world.PreallocateArchetype<Position, Velocity, Rotation>();
        world.PreallocateArchetype<Health>();

        // Should have 4 new archetypes
        Assert.Equal(initialCount + 4, world.ArchetypeManager.ArchetypeCount);
    }

    #endregion

    #region Integration with Entity Operations

    [Fact]
    public void PreallocateArchetype_WorksWithAddComponent()
    {
        using var world = new World();
        RegisterTestComponents(world);

        // Pre-allocate target archetype
        world.PreallocateArchetype<Position, Velocity>();

        // Create entity with Position
        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .Build();

        var archetypeCountBefore = world.ArchetypeManager.ArchetypeCount;

        // Add Velocity (should migrate to pre-allocated archetype)
        world.Add(entity, new Velocity { X = 3, Y = 4 });

        // No new archetype should be created
        Assert.Equal(archetypeCountBefore, world.ArchetypeManager.ArchetypeCount);
        Assert.True(world.Has<Position>(entity));
        Assert.True(world.Has<Velocity>(entity));
    }

    [Fact]
    public void PreallocateArchetype_WorksWithRemoveComponent()
    {
        using var world = new World();
        RegisterTestComponents(world);

        // Pre-allocate target archetype (just Position)
        world.PreallocateArchetype<Position>();

        // Create entity with Position + Velocity
        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 3, Y = 4 })
            .Build();

        var archetypeCountBefore = world.ArchetypeManager.ArchetypeCount;

        // Remove Velocity (should migrate to pre-allocated archetype)
        world.Remove<Velocity>(entity);

        // No new archetype should be created
        Assert.Equal(archetypeCountBefore, world.ArchetypeManager.ArchetypeCount);
        Assert.True(world.Has<Position>(entity));
        Assert.False(world.Has<Velocity>(entity));
    }

    #endregion
}
