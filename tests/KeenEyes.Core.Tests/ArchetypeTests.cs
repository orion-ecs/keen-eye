namespace KeenEyes.Tests;

/// <summary>
/// Tests for the archetype-based storage system.
/// </summary>
public class ArchetypeTests
{
    #region Test Components

    private struct Position : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Velocity : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Health : IComponent
    {
        public int Current;
        public int Max;

        public Health() { }
    }

    private struct EnemyTag : ITagComponent;

    #endregion

    #region ArchetypeId Tests

    [Fact]
    public void ArchetypeId_SameComponents_SameHash()
    {
        var id1 = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var id2 = new ArchetypeId([typeof(Velocity), typeof(Position)]);

        Assert.Equal(id1, id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ArchetypeId_DifferentComponents_DifferentHash()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Velocity)]);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ArchetypeId_Has_ReturnsCorrectResult()
    {
        var id = new ArchetypeId([typeof(Position), typeof(Velocity)]);

        Assert.True(id.Has(typeof(Position)));
        Assert.True(id.Has(typeof(Velocity)));
        Assert.False(id.Has(typeof(Health)));
    }

    [Fact]
    public void ArchetypeId_With_AddsComponentType()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = id1.With(typeof(Velocity));

        Assert.False(id1.Has(typeof(Velocity)));
        Assert.True(id2.Has(typeof(Velocity)));
        Assert.True(id2.Has(typeof(Position)));
    }

    [Fact]
    public void ArchetypeId_Without_RemovesComponentType()
    {
        var id1 = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var id2 = id1.Without(typeof(Velocity));

        Assert.True(id1.Has(typeof(Velocity)));
        Assert.False(id2.Has(typeof(Velocity)));
        Assert.True(id2.Has(typeof(Position)));
    }

    [Fact]
    public void ArchetypeId_ComponentCount_ReturnsCorrectValue()
    {
        var empty = new ArchetypeId([]);
        var single = new ArchetypeId([typeof(Position)]);
        var multiple = new ArchetypeId([typeof(Position), typeof(Velocity), typeof(Health)]);

        Assert.Equal(0, empty.ComponentCount);
        Assert.Equal(1, single.ComponentCount);
        Assert.Equal(3, multiple.ComponentCount);
    }

    [Fact]
    public void ArchetypeId_ToString_ReturnsReadableFormat()
    {
        var id = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var str = id.ToString();

        Assert.Contains("Position", str);
        Assert.Contains("Velocity", str);
    }

    #endregion

    #region ComponentArray Tests

    [Fact]
    public void ComponentArray_Add_StoresComponent()
    {
        using var array = new ComponentArray<Position>();

        var index = array.Add(new Position { X = 1, Y = 2 });

        Assert.Equal(0, index);
        Assert.Equal(1, array.Count);
        Assert.Equal(1, array.GetRef(0).X);
        Assert.Equal(2, array.GetRef(0).Y);
    }

    [Fact]
    public void ComponentArray_GetRef_AllowsModification()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1, Y = 2 });

        ref var pos = ref array.GetRef(0);
        pos.X = 10;

        Assert.Equal(10, array.GetRef(0).X);
    }

    [Fact]
    public void ComponentArray_RemoveAtSwapBack_RemovesComponent()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1, Y = 1 });
        array.Add(new Position { X = 2, Y = 2 });
        array.Add(new Position { X = 3, Y = 3 });

        array.RemoveAtSwapBack(0);

        Assert.Equal(2, array.Count);
        Assert.Equal(3, array.GetRef(0).X); // Last element swapped to index 0
        Assert.Equal(2, array.GetRef(1).X);
    }

    [Fact]
    public void ComponentArray_AsSpan_ReturnsValidSpan()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1, Y = 1 });
        array.Add(new Position { X = 2, Y = 2 });

        var span = array.AsSpan();

        Assert.Equal(2, span.Length);
        Assert.Equal(1, span[0].X);
        Assert.Equal(2, span[1].X);
    }

    [Fact]
    public void ComponentArray_GrowsAutomatically()
    {
        using var array = new ComponentArray<Position>(initialCapacity: 2);

        for (var i = 0; i < 100; i++)
        {
            array.Add(new Position { X = i, Y = i });
        }

        Assert.Equal(100, array.Count);
        Assert.True(array.Capacity >= 100);
    }

    #endregion

    #region Archetype Tests

    [Fact]
    public void Archetype_EntityAdditionAndRemoval()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        var id = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var infos = new[] { world.Components.Get<Position>()!, world.Components.Get<Velocity>()! };
        using var archetype = new Archetype(id, infos);

        var entity = new Entity(0, 1);
        archetype.AddEntity(entity);
        archetype.AddComponent(new Position { X = 1, Y = 2 });
        archetype.AddComponent(new Velocity { X = 3, Y = 4 });

        Assert.Equal(1, archetype.Count);
        Assert.Equal(1, archetype.Get<Position>(0).X);
        Assert.Equal(3, archetype.Get<Velocity>(0).X);

        archetype.RemoveEntity(entity);
        Assert.Equal(0, archetype.Count);
    }

    [Fact]
    public void Archetype_Has_ReturnsCorrectResult()
    {
        using var world = new World();
        world.Components.Register<Position>();

        var id = new ArchetypeId([typeof(Position)]);
        var infos = new[] { world.Components.Get<Position>()! };
        using var archetype = new Archetype(id, infos);

        Assert.True(archetype.Has<Position>());
        Assert.False(archetype.Has<Velocity>());
    }

    [Fact]
    public void Archetype_GetEntityIndex_ReturnsCorrectIndex()
    {
        using var world = new World();
        world.Components.Register<Position>();

        var id = new ArchetypeId([typeof(Position)]);
        var infos = new[] { world.Components.Get<Position>()! };
        using var archetype = new Archetype(id, infos);

        var entity1 = new Entity(0, 1);
        var entity2 = new Entity(1, 1);

        archetype.AddEntity(entity1);
        archetype.AddComponent(new Position());
        archetype.AddEntity(entity2);
        archetype.AddComponent(new Position());

        Assert.Equal(0, archetype.GetEntityIndex(entity1));
        Assert.Equal(1, archetype.GetEntityIndex(entity2));
        Assert.Equal(-1, archetype.GetEntityIndex(new Entity(99, 1)));
    }

    #endregion

    #region ArchetypeManager Tests

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_CreatesNew()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);

        var archetype = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        Assert.NotNull(archetype);
        Assert.True(archetype.Has<Position>());
        Assert.True(archetype.Has<Velocity>());
    }

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_ReturnsSameInstance()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);

        var archetype1 = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);
        var archetype2 = manager.GetOrCreateArchetype([typeof(Velocity), typeof(Position)]);

        Assert.Same(archetype1, archetype2);
    }

    [Fact]
    public void ArchetypeManager_ArchetypeCreated_EventFires()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);

        var eventFired = false;
        manager.ArchetypeCreated += _ => eventFired = true;

        manager.GetOrCreateArchetype([typeof(Position)]);

        Assert.True(eventFired);
    }

    #endregion

    #region World Integration Tests

    [Fact]
    public void World_EntitiesGroupedByArchetype()
    {
        using var world = new World();

        // Create entities with different component combinations
        var e1 = world.Spawn().With(new Position()).Build();
        var e2 = world.Spawn().With(new Position()).With(new Velocity()).Build();
        var e3 = world.Spawn().With(new Position()).Build();

        // Query should return correct entities
        var positionOnly = world.Query<Position>().Without<Velocity>().ToList();
        var both = world.Query<Position, Velocity>().ToList();

        Assert.Equal(2, positionOnly.Count);
        Assert.Single(both);
        Assert.Contains(e1, positionOnly);
        Assert.Contains(e3, positionOnly);
        Assert.Contains(e2, both);
    }

    [Fact]
    public void World_AddComponent_MigratesToNewArchetype()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position { X = 5 }).Build();

        // Initially matched by Position-only query
        Assert.Contains(entity, world.Query<Position>().Without<Velocity>().ToList());

        // Add Velocity
        world.Add(entity, new Velocity { X = 10 });

        // Now matched by both-components query
        Assert.DoesNotContain(entity, world.Query<Position>().Without<Velocity>().ToList());
        Assert.Contains(entity, world.Query<Position, Velocity>().ToList());

        // Original component preserved
        Assert.Equal(5, world.Get<Position>(entity).X);
        Assert.Equal(10, world.Get<Velocity>(entity).X);
    }

    [Fact]
    public void World_RemoveComponent_MigratesToNewArchetype()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new Position { X = 5 })
            .With(new Velocity { X = 10 })
            .Build();

        // Initially matched by both-components query
        Assert.Contains(entity, world.Query<Position, Velocity>().ToList());

        // Remove Velocity
        world.Remove<Velocity>(entity);

        // Now matched by Position-only query
        Assert.Contains(entity, world.Query<Position>().Without<Velocity>().ToList());
        Assert.DoesNotContain(entity, world.Query<Position, Velocity>().ToList());

        // Position preserved
        Assert.Equal(5, world.Get<Position>(entity).X);
        Assert.False(world.Has<Velocity>(entity));
    }

    [Fact]
    public void World_GetMemoryStats_ReturnsValidStats()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        var stats = world.GetMemoryStats();

        Assert.Equal(2, stats.EntitiesActive);
        Assert.Equal(2, stats.EntitiesAllocated);
        Assert.Equal(2, stats.ArchetypeCount);
        Assert.Equal(2, stats.ComponentTypeCount);
    }

    #endregion
}
