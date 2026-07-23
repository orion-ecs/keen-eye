namespace KeenEyes.Tests;

/// <summary>
/// Regression tests for issue #1092: multi-chunk archetype storage corruption after a
/// swap-back removal leaves a hole in a non-last chunk.
/// </summary>
/// <remarks>
/// A single <see cref="ArchetypeChunk"/> holds up to <see cref="ArchetypeChunk.DefaultCapacity"/>
/// entities, so every scenario spawns more than that of one archetype to force multiple chunks,
/// then removes entities from an early chunk. Component values are set at creation time and carry
/// a unique per-entity tag so that cross-entity corruption (not just crashes) is detectable, and
/// so the wrong-chunk write path is exercised rather than masked by a later entity-keyed write.
/// </remarks>
public class MultiChunkArchetypeCorruptionTests
{
    private const int Capacity = ArchetypeChunk.DefaultCapacity;

    private static (int ChunkIndex, int IndexInChunk) LocationOf(World world, Entity entity)
    {
        world.ArchetypeManager.TryGetEntityLocation(entity, out var archetype, out _);
        return archetype!.GetEntityLocation(entity);
    }

    [Fact]
    public void Get_AfterEarlyChunkRemovalAndReadd_ReturnsCorrectComponentData()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var expected = new Dictionary<Entity, int>();
        var alive = new List<Entity>();
        var tag = 0;

        // Position.X is set at creation so the value flows through the component-add path and is
        // never re-written via an entity-keyed setter that would hide a wrong-chunk write.
        Entity Spawn()
        {
            tag++;
            var entity = world.Spawn().With(new TestPosition { X = tag }).Build();
            expected[entity] = tag;
            alive.Add(entity);
            return entity;
        }

        for (var i = 0; i < 200; i++)
        {
            Spawn();
        }

        // The archetype must span at least two chunks for this scenario to be meaningful.
        Assert.True(LocationOf(world, alive[199]).ChunkIndex >= 1);

        // Despawn entities from the first chunk (none at the final spawn slots) so swap-back
        // leaves the chunk non-full while later chunks still hold entities.
        foreach (var index in new[] { 3, 17, 42, 88, 120 })
        {
            var victim = alive[index];
            Assert.Equal(0, LocationOf(world, victim).ChunkIndex);
            world.Despawn(victim);
            expected.Remove(victim);
        }
        alive.RemoveAll(e => !world.IsAlive(e));

        // Re-add entities; these refill the early-chunk hole first, so entity and components must
        // land in the same chunk.
        for (var i = 0; i < 10; i++)
        {
            Spawn();
        }

        foreach (var entity in alive)
        {
            Assert.True(world.IsAlive(entity));
            Assert.Equal(expected[entity], (int)world.Get<TestPosition>(entity).X);
        }
    }

    [Fact]
    public void Query_OverHoledMultiChunkArchetype_YieldsExactlyAliveEntities()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var alive = new List<Entity>();
        var tag = 0;

        Entity Spawn()
        {
            tag++;
            var entity = world.Spawn().With(new TestPosition { X = tag }).Build();
            alive.Add(entity);
            return entity;
        }

        for (var i = 0; i < 200; i++)
        {
            Spawn();
        }

        // Despawn more than we re-add so a hole persists in a non-last chunk at query time.
        foreach (var index in new[] { 5, 11, 30, 44, 61, 77, 90, 102, 110, 125 })
        {
            world.Despawn(alive[index]);
        }
        alive.RemoveAll(e => !world.IsAlive(e));

        for (var i = 0; i < 3; i++)
        {
            Spawn();
        }

        var expected = alive.ToHashSet();
        var yielded = new List<Entity>();
        foreach (var entity in world.Query<TestPosition>())
        {
            yielded.Add(entity);
        }

        // No phantom/default entity is ever yielded, and every yielded entity is alive.
        Assert.DoesNotContain(Entity.Null, yielded);
        foreach (var entity in yielded)
        {
            Assert.True(world.IsAlive(entity));
        }

        // The yielded set equals the alive set: same count and same identities (none skipped).
        Assert.Equal(expected.Count, yielded.Count);
        Assert.Equal(expected, yielded.ToHashSet());
    }

    [Fact]
    public void AddComponent_AcrossChunkBoundaryAfterRemoval_PreservesData()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var positionTag = new Dictionary<Entity, int>();
        var velocityTag = new Dictionary<Entity, int>();
        var positionOnly = new List<Entity>();
        var tag = 0;

        for (var i = 0; i < 200; i++)
        {
            tag++;
            var entity = world.Spawn().With(new TestPosition { X = tag }).Build();
            positionTag[entity] = tag;
            positionOnly.Add(entity);
        }

        // Migrate 150 entities into the {Position, Velocity} archetype so it spans two chunks.
        var withVelocity = new List<Entity>();
        void AddVelocity(Entity entity)
        {
            tag++;
            world.Add(entity, new TestVelocity { X = tag });
            velocityTag[entity] = tag;
            withVelocity.Add(entity);
        }

        for (var i = 0; i < 150; i++)
        {
            AddVelocity(positionOnly[i]);
        }

        // Punch holes in the first chunk of {Position, Velocity}.
        var despawned = new HashSet<Entity>();
        foreach (var index in new[] { 2, 20, 55, 99 })
        {
            var victim = withVelocity[index];
            Assert.Equal(0, LocationOf(world, victim).ChunkIndex);
            world.Despawn(victim);
            despawned.Add(victim);
        }

        // Migrate the remaining survivors in; they refill the early-chunk holes, so their entity
        // slot lands in an early chunk while the archetype's last chunk is elsewhere.
        for (var i = 150; i < 200; i++)
        {
            AddVelocity(positionOnly[i]);
        }

        foreach (var entity in withVelocity)
        {
            if (despawned.Contains(entity))
            {
                continue;
            }

            Assert.True(world.IsAlive(entity));
            Assert.Equal(positionTag[entity], (int)world.Get<TestPosition>(entity).X);
            Assert.Equal(velocityTag[entity], (int)world.Get<TestVelocity>(entity).X);
        }
    }

    [Fact]
    public void Get_AfterChunkFullyEmptiedAndRemoved_ResolvesSurvivorsInLaterChunks()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var expected = new Dictionary<Entity, int>();
        var spawned = new List<Entity>();

        for (var i = 0; i < 300; i++)
        {
            var entity = world.Spawn().With(new TestPosition { X = i + 1 }).Build();
            expected[entity] = i + 1;
            spawned.Add(entity);
        }

        // The first Capacity entities occupy chunk 0; entities at index >= 2 * Capacity occupy
        // chunk 2. Swap-back removal only moves entities within their own chunk, so despawning
        // the first Capacity entities drains chunk 0 exactly and removes it.
        for (var i = 0; i < Capacity; i++)
        {
            world.Despawn(spawned[i]);
        }

        var survivors = spawned.GetRange(Capacity, spawned.Count - Capacity);

        // A survivor originally in chunk 2 must still resolve correctly after chunk 0 is removed.
        var deepSurvivor = spawned[(2 * Capacity) + 4];
        Assert.True(world.IsAlive(deepSurvivor));
        Assert.Equal(expected[deepSurvivor], (int)world.Get<TestPosition>(deepSurvivor).X);

        foreach (var entity in survivors)
        {
            Assert.True(world.IsAlive(entity));
            Assert.Equal(expected[entity], (int)world.Get<TestPosition>(entity).X);
        }
    }

    [Fact]
    public void AddComponent_AfterChunkFullyEmptiedAndRemoved_MigratesSurvivorCorrectly()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var spawned = new List<Entity>();
        for (var i = 0; i < 300; i++)
        {
            spawned.Add(world.Spawn().With(new TestPosition { X = i + 1 }).Build());
        }

        // Empty and remove chunk 0 of the {Position} archetype.
        for (var i = 0; i < Capacity; i++)
        {
            world.Despawn(spawned[i]);
        }

        // Migrate a survivor that originally lived in chunk 2 into {Position, Velocity}.
        var survivorIndex = (2 * Capacity) + 10;
        var survivor = spawned[survivorIndex];
        Assert.True(world.IsAlive(survivor));
        world.Add(survivor, new TestVelocity { X = 7777 });

        Assert.Equal(survivorIndex + 1, (int)world.Get<TestPosition>(survivor).X);
        Assert.Equal(7777, (int)world.Get<TestVelocity>(survivor).X);
    }
}
