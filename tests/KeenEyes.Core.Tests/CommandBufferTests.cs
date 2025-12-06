namespace KeenEyes.Tests;

/// <summary>
/// Tests for CommandBuffer deferred entity operations.
/// </summary>
public class CommandBufferTests
{
    #region Test Components

    public struct TestPosition : IComponent { public float X, Y; }
    public struct TestVelocity : IComponent { public float X, Y; }
    public struct TestHealth : IComponent { public int Current, Max; }
    public struct ActiveTag : ITagComponent;
    public struct FrozenTag : ITagComponent;

    #endregion

    #region Spawn Tests

    [Fact]
    public void Spawn_CreatesEntity_AfterFlush()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn().With(new TestPosition { X = 10f, Y = 20f });

        // Entity should not exist yet
        Assert.Empty(world.GetAllEntities());

        buffer.Flush(world);

        // Now entity should exist
        var entities = world.GetAllEntities().ToList();
        Assert.Single(entities);
        Assert.True(world.Has<TestPosition>(entities[0]));
    }

    [Fact]
    public void Spawn_WithMultipleComponents_AddsAllComponents()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn()
            .With(new TestPosition { X = 1f, Y = 2f })
            .With(new TestVelocity { X = 3f, Y = 4f })
            .With(new TestHealth { Current = 100, Max = 100 });

        var entityMap = buffer.Flush(world);

        Assert.Single(entityMap);
        var entity = entityMap.Values.First();
        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));
        Assert.True(world.Has<TestHealth>(entity));

        ref var pos = ref world.Get<TestPosition>(entity);
        Assert.Equal(1f, pos.X);
        Assert.Equal(2f, pos.Y);
    }

    [Fact]
    public void Spawn_WithTag_AddsTagComponent()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .WithTag<ActiveTag>();

        var entityMap = buffer.Flush(world);

        var entity = entityMap.Values.First();
        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<ActiveTag>(entity));
    }

    [Fact]
    public void Spawn_MultipleEntities_CreatesAllEntities()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd1 = buffer.Spawn().With(new TestPosition { X = 1f, Y = 1f });
        var cmd2 = buffer.Spawn().With(new TestPosition { X = 2f, Y = 2f });
        var cmd3 = buffer.Spawn().With(new TestPosition { X = 3f, Y = 3f });

        var entityMap = buffer.Flush(world);

        Assert.Equal(3, entityMap.Count);
        Assert.Equal(3, world.GetAllEntities().Count());

        // Verify placeholder IDs are mapped correctly
        Assert.True(entityMap.ContainsKey(cmd1.PlaceholderId));
        Assert.True(entityMap.ContainsKey(cmd2.PlaceholderId));
        Assert.True(entityMap.ContainsKey(cmd3.PlaceholderId));
    }

    [Fact]
    public void Spawn_ReturnsCorrectPlaceholderId()
    {
        var buffer = new CommandBuffer();

        var cmd1 = buffer.Spawn();
        var cmd2 = buffer.Spawn();
        var cmd3 = buffer.Spawn();

        // Placeholder IDs should be negative and decreasing
        Assert.True(cmd1.PlaceholderId < 0);
        Assert.True(cmd2.PlaceholderId < 0);
        Assert.True(cmd3.PlaceholderId < 0);
        Assert.NotEqual(cmd1.PlaceholderId, cmd2.PlaceholderId);
        Assert.NotEqual(cmd2.PlaceholderId, cmd3.PlaceholderId);
    }

    [Fact]
    public void Spawn_FlushReturnsEntityMap_WithCorrectMappings()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd1 = buffer.Spawn().With(new TestPosition { X = 10f, Y = 20f });
        var cmd2 = buffer.Spawn().With(new TestPosition { X = 30f, Y = 40f });

        var entityMap = buffer.Flush(world);

        var entity1 = entityMap[cmd1.PlaceholderId];
        var entity2 = entityMap[cmd2.PlaceholderId];

        ref var pos1 = ref world.Get<TestPosition>(entity1);
        ref var pos2 = ref world.Get<TestPosition>(entity2);

        Assert.Equal(10f, pos1.X);
        Assert.Equal(30f, pos2.X);
    }

    #endregion

    #region Despawn Tests

    [Fact]
    public void Despawn_ExistingEntity_RemovesEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(world.IsAlive(entity));

        var buffer = new CommandBuffer();
        buffer.Despawn(entity);

        // Entity should still exist before flush
        Assert.True(world.IsAlive(entity));

        buffer.Flush(world);

        // Entity should be gone after flush
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void Despawn_PlaceholderId_RemovesSpawnedEntity()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn().With(new TestPosition { X = 0f, Y = 0f });
        buffer.Despawn(cmd.PlaceholderId);

        var entityMap = buffer.Flush(world);

        // Entity was spawned and then immediately despawned
        var entity = entityMap[cmd.PlaceholderId];
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void Despawn_NonExistentEntity_SilentlyIgnored()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var nonExistentEntity = new Entity(999, 1);
        buffer.Despawn(nonExistentEntity);

        // Should not throw
        buffer.Flush(world);
    }

    [Fact]
    public void Despawn_MultipleEntities_RemovesAll()
    {
        using var world = new World();
        var entity1 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();
        var entity3 = world.Spawn().With(new TestPosition { X = 3f, Y = 3f }).Build();

        var buffer = new CommandBuffer();
        buffer.Despawn(entity1);
        buffer.Despawn(entity3);

        buffer.Flush(world);

        Assert.False(world.IsAlive(entity1));
        Assert.True(world.IsAlive(entity2));  // Not despawned
        Assert.False(world.IsAlive(entity3));
    }

    #endregion

    #region AddComponent Tests

    [Fact]
    public void AddComponent_ToExistingEntity_AddsComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.False(world.Has<TestVelocity>(entity));

        var buffer = new CommandBuffer();
        buffer.AddComponent(entity, new TestVelocity { X = 5f, Y = 10f });

        // Component should not be added yet
        Assert.False(world.Has<TestVelocity>(entity));

        buffer.Flush(world);

        // Now component should exist
        Assert.True(world.Has<TestVelocity>(entity));
        ref var vel = ref world.Get<TestVelocity>(entity);
        Assert.Equal(5f, vel.X);
        Assert.Equal(10f, vel.Y);
    }

    [Fact]
    public void AddComponent_ToPlaceholderEntity_AddsComponent()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn().With(new TestPosition { X = 0f, Y = 0f });
        buffer.AddComponent(cmd.PlaceholderId, new TestVelocity { X = 1f, Y = 2f });

        var entityMap = buffer.Flush(world);

        var entity = entityMap[cmd.PlaceholderId];
        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));
    }

    [Fact]
    public void AddComponent_ToNonExistentEntity_SilentlyIgnored()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var nonExistentEntity = new Entity(999, 1);
        buffer.AddComponent(nonExistentEntity, new TestVelocity { X = 1f, Y = 1f });

        // Should not throw
        buffer.Flush(world);
    }

    [Fact]
    public void AddComponent_ToDespawnedEntity_SilentlyIgnored()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.Despawn(entity);

        var buffer = new CommandBuffer();
        buffer.AddComponent(entity, new TestVelocity { X = 1f, Y = 1f });

        // Should not throw
        buffer.Flush(world);
    }

    #endregion

    #region RemoveComponent Tests

    [Fact]
    public void RemoveComponent_FromExistingEntity_RemovesComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        Assert.True(world.Has<TestVelocity>(entity));

        var buffer = new CommandBuffer();
        buffer.RemoveComponent<TestVelocity>(entity);

        // Component should still exist before flush
        Assert.True(world.Has<TestVelocity>(entity));

        buffer.Flush(world);

        // Now component should be gone
        Assert.False(world.Has<TestVelocity>(entity));
        Assert.True(world.Has<TestPosition>(entity));  // Other component still there
    }

    [Fact]
    public void RemoveComponent_FromPlaceholderEntity_RemovesComponent()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f });
        buffer.RemoveComponent<TestVelocity>(cmd.PlaceholderId);

        var entityMap = buffer.Flush(world);

        var entity = entityMap[cmd.PlaceholderId];
        Assert.True(world.Has<TestPosition>(entity));
        Assert.False(world.Has<TestVelocity>(entity));
    }

    [Fact]
    public void RemoveComponent_ComponentNotPresent_SilentlyIgnored()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        var buffer = new CommandBuffer();
        buffer.RemoveComponent<TestVelocity>(entity);  // Entity doesn't have Velocity

        // Should not throw
        buffer.Flush(world);
    }

    [Fact]
    public void RemoveComponent_FromNonExistentEntity_SilentlyIgnored()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var nonExistentEntity = new Entity(999, 1);
        buffer.RemoveComponent<TestVelocity>(nonExistentEntity);

        // Should not throw
        buffer.Flush(world);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void ComplexScenario_SpawnAddDespawn()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        // Spawn entity with position
        var cmd = buffer.Spawn().With(new TestPosition { X = 0f, Y = 0f });

        // Add velocity to it
        buffer.AddComponent(cmd.PlaceholderId, new TestVelocity { X = 1f, Y = 1f });

        // Then despawn it
        buffer.Despawn(cmd.PlaceholderId);

        var entityMap = buffer.Flush(world);

        // Entity should have been created and immediately despawned
        var entity = entityMap[cmd.PlaceholderId];
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void ComplexScenario_SpawnThenAddMoreComponents()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn().With(new TestPosition { X = 5f, Y = 5f });
        buffer.AddComponent(cmd.PlaceholderId, new TestVelocity { X = 10f, Y = 10f });
        buffer.AddComponent(cmd.PlaceholderId, new TestHealth { Current = 50, Max = 100 });

        var entityMap = buffer.Flush(world);
        var entity = entityMap[cmd.PlaceholderId];

        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));
        Assert.True(world.Has<TestHealth>(entity));

        ref var health = ref world.Get<TestHealth>(entity);
        Assert.Equal(50, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void ComplexScenario_MixedOperations()
    {
        using var world = new World();

        // Create some existing entities
        var existing1 = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();
        var existing2 = world.Spawn()
            .With(new TestPosition { X = 100f, Y = 100f })
            .Build();

        var buffer = new CommandBuffer();

        // Spawn new entity
        var newCmd = buffer.Spawn()
            .With(new TestPosition { X = 50f, Y = 50f });

        // Add component to existing entity
        buffer.AddComponent(existing2, new TestHealth { Current = 100, Max = 100 });

        // Remove component from existing entity
        buffer.RemoveComponent<TestVelocity>(existing1);

        // Despawn existing entity
        buffer.Despawn(existing1);

        var entityMap = buffer.Flush(world);

        // existing1 should be despawned
        Assert.False(world.IsAlive(existing1));

        // existing2 should have new component
        Assert.True(world.Has<TestHealth>(existing2));

        // new entity should exist
        var newEntity = entityMap[newCmd.PlaceholderId];
        Assert.True(world.IsAlive(newEntity));
        Assert.True(world.Has<TestPosition>(newEntity));
    }

    [Fact]
    public void ComplexScenario_QueryDuringIteration()
    {
        using var world = new World();

        // Create entities with health
        for (int i = 0; i < 5; i++)
        {
            world.Spawn()
                .With(new TestPosition { X = i, Y = i })
                .With(new TestHealth { Current = i * 10, Max = 100 })
                .Build();
        }

        var buffer = new CommandBuffer();
        var entitiesToDespawn = new List<Entity>();

        // During iteration, queue despawn for entities with low health
        foreach (var entity in world.Query<TestHealth>())
        {
            ref var health = ref world.Get<TestHealth>(entity);
            if (health.Current < 30)
            {
                entitiesToDespawn.Add(entity);
            }
        }

        // Queue the despawns after iteration
        foreach (var entity in entitiesToDespawn)
        {
            buffer.Despawn(entity);
        }

        buffer.Flush(world);

        // Only entities with health >= 30 should remain
        var remaining = world.GetAllEntities().ToList();
        Assert.Equal(2, remaining.Count);  // Entities with health 30, 40 remain
    }

    #endregion

    #region Buffer State Tests

    [Fact]
    public void Count_ReflectsQueuedCommands()
    {
        var buffer = new CommandBuffer();

        Assert.Equal(0, buffer.Count);

        buffer.Spawn();
        Assert.Equal(1, buffer.Count);

        buffer.Spawn();
        Assert.Equal(2, buffer.Count);

        buffer.Despawn(new Entity(0, 1));
        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void Clear_RemovesAllQueuedCommands()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn().With(new TestPosition { X = 1f, Y = 1f });
        buffer.Spawn().With(new TestPosition { X = 2f, Y = 2f });

        Assert.Equal(2, buffer.Count);

        buffer.Clear();

        Assert.Equal(0, buffer.Count);

        // Flush should do nothing
        buffer.Flush(world);
        Assert.Empty(world.GetAllEntities());
    }

    [Fact]
    public void Flush_ClearsBuffer_AfterExecution()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn().With(new TestPosition { X = 1f, Y = 1f });
        Assert.Equal(1, buffer.Count);

        buffer.Flush(world);

        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Buffer_CanBeReusedAfterFlush()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn().With(new TestPosition { X = 1f, Y = 1f });
        buffer.Flush(world);

        Assert.Single(world.GetAllEntities());

        // Reuse buffer
        buffer.Spawn().With(new TestPosition { X = 2f, Y = 2f });
        buffer.Flush(world);

        Assert.Equal(2, world.GetAllEntities().Count());
    }

    [Fact]
    public void Buffer_CanBeReusedAfterClear()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn().With(new TestPosition { X = 1f, Y = 1f });
        buffer.Clear();

        // Reuse buffer
        buffer.Spawn().With(new TestPosition { X = 2f, Y = 2f });
        buffer.Flush(world);

        Assert.Single(world.GetAllEntities());
    }

    [Fact]
    public void PlaceholderIds_ResetAfterClear()
    {
        var buffer = new CommandBuffer();

        var cmd1 = buffer.Spawn();
        var cmd2 = buffer.Spawn();

        buffer.Clear();

        var cmd3 = buffer.Spawn();
        var cmd4 = buffer.Spawn();

        // After clear, placeholder IDs should start fresh
        Assert.Equal(cmd1.PlaceholderId, cmd3.PlaceholderId);
        Assert.Equal(cmd2.PlaceholderId, cmd4.PlaceholderId);
    }

    #endregion

    #region Execution Order Tests

    [Fact]
    public void ExecutionOrder_SpawnBeforeAdd()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn();  // No components initially
        buffer.AddComponent(cmd.PlaceholderId, new TestPosition { X = 1f, Y = 1f });

        var entityMap = buffer.Flush(world);

        var entity = entityMap[cmd.PlaceholderId];
        Assert.True(world.Has<TestPosition>(entity));
    }

    [Fact]
    public void ExecutionOrder_AddBeforeRemove()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f });

        // Add then remove same component type
        buffer.AddComponent(cmd.PlaceholderId, new TestVelocity { X = 1f, Y = 1f });
        buffer.RemoveComponent<TestVelocity>(cmd.PlaceholderId);

        var entityMap = buffer.Flush(world);

        var entity = entityMap[cmd.PlaceholderId];
        Assert.True(world.Has<TestPosition>(entity));
        Assert.False(world.Has<TestVelocity>(entity));  // Added then removed
    }

    [Fact]
    public void ExecutionOrder_CommandsInSequence()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        // Create entity with specific sequence of operations
        var cmd = buffer.Spawn()
            .With(new TestHealth { Current = 100, Max = 100 });

        buffer.AddComponent(cmd.PlaceholderId, new TestPosition { X = 0f, Y = 0f });
        buffer.AddComponent(cmd.PlaceholderId, new TestVelocity { X = 5f, Y = 5f });
        buffer.RemoveComponent<TestHealth>(cmd.PlaceholderId);

        var entityMap = buffer.Flush(world);
        var entity = entityMap[cmd.PlaceholderId];

        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));
        Assert.False(world.Has<TestHealth>(entity));  // Started with it, then removed
    }

    #endregion

    #region World Isolation Tests

    [Fact]
    public void WorldIsolation_SameBufferDifferentWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn().With(new TestPosition { X = 1f, Y = 1f });
        buffer.Flush(world1);

        // Buffer should be cleared, so flushing to world2 does nothing
        Assert.Single(world1.GetAllEntities());
        Assert.Empty(world2.GetAllEntities());
    }

    [Fact]
    public void WorldIsolation_EntitiesFromDifferentWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        var entityFromWorld1 = world1.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        var buffer = new CommandBuffer();
        buffer.Despawn(entityFromWorld1);

        // Flush to world2 - should silently fail since entity doesn't exist there
        buffer.Flush(world2);

        // Entity in world1 should still exist
        Assert.True(world1.IsAlive(entityFromWorld1));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EdgeCase_EmptyBuffer_Flush()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var entityMap = buffer.Flush(world);

        Assert.Empty(entityMap);
        Assert.Empty(world.GetAllEntities());
    }

    [Fact]
    public void EdgeCase_SpawnWithNoComponents()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn();  // No components

        var entityMap = buffer.Flush(world);

        Assert.Single(entityMap);
        var entity = entityMap.Values.First();
        Assert.True(world.IsAlive(entity));
        Assert.Empty(world.GetComponents(entity));
    }

    [Fact]
    public void EdgeCase_DespawnTwice()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        var buffer = new CommandBuffer();
        buffer.Despawn(entity);
        buffer.Despawn(entity);  // Despawn same entity twice

        // Should not throw
        buffer.Flush(world);

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void EdgeCase_InvalidPlaceholderId()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        // Use a placeholder ID that was never created
        buffer.AddComponent(999, new TestPosition { X = 0f, Y = 0f });
        buffer.RemoveComponent<TestPosition>(999);
        buffer.Despawn(999);

        // Should not throw - operations on invalid placeholders are silently ignored
        buffer.Flush(world);
    }

    [Fact]
    public void EdgeCase_LargeNumberOfCommands()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        const int entityCount = 1000;
        var commands = new List<EntityCommands>();

        for (int i = 0; i < entityCount; i++)
        {
            commands.Add(buffer.Spawn().With(new TestPosition { X = i, Y = i }));
        }

        var entityMap = buffer.Flush(world);

        Assert.Equal(entityCount, entityMap.Count);
        Assert.Equal(entityCount, world.GetAllEntities().Count());

        // Verify all entities have correct positions
        foreach (var cmd in commands)
        {
            var entity = entityMap[cmd.PlaceholderId];
            Assert.True(world.IsAlive(entity));
        }
    }

    #endregion

    #region Performance Awareness Tests

    [Fact]
    public void Performance_BufferReuseDoesNotLeak()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        for (int i = 0; i < 100; i++)
        {
            buffer.Spawn().With(new TestPosition { X = i, Y = i });
            buffer.Flush(world);
        }

        Assert.Equal(100, world.GetAllEntities().Count());
    }

    [Fact]
    public void Performance_CountIsO1()
    {
        var buffer = new CommandBuffer();

        for (int i = 0; i < 1000; i++)
        {
            buffer.Spawn();
            var count = buffer.Count;  // Should be O(1)
            Assert.Equal(i + 1, count);
        }
    }

    #endregion

    #region Named Entity Spawn Tests

    [Fact]
    public void Spawn_WithName_CreatesNamedEntity()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn("Player")
            .With(new TestPosition { X = 100f, Y = 200f });

        buffer.Flush(world);

        var player = world.GetEntityByName("Player");
        Assert.True(player.IsValid);
        Assert.True(world.IsAlive(player));
        Assert.True(world.Has<TestPosition>(player));

        ref var pos = ref world.Get<TestPosition>(player);
        Assert.Equal(100f, pos.X);
        Assert.Equal(200f, pos.Y);
    }

    [Fact]
    public void Spawn_WithName_CanRetrieveByName()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn("Enemy")
            .With(new TestPosition { X = 50f, Y = 50f })
            .With(new TestHealth { Current = 100, Max = 100 });

        var entityMap = buffer.Flush(world);

        var enemyFromMap = entityMap[cmd.PlaceholderId];
        var enemyByName = world.GetEntityByName("Enemy");

        Assert.Equal(enemyFromMap, enemyByName);
        Assert.Equal("Enemy", world.GetName(enemyByName));
    }

    [Fact]
    public void Spawn_MultipleNamedEntities_AllRetrievable()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn("Player").With(new TestPosition { X = 0f, Y = 0f });
        buffer.Spawn("Enemy1").With(new TestPosition { X = 10f, Y = 0f });
        buffer.Spawn("Enemy2").With(new TestPosition { X = 20f, Y = 0f });

        buffer.Flush(world);

        Assert.True(world.GetEntityByName("Player").IsValid);
        Assert.True(world.GetEntityByName("Enemy1").IsValid);
        Assert.True(world.GetEntityByName("Enemy2").IsValid);
    }

    [Fact]
    public void Spawn_NullName_CreatesUnnamedEntity()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn(null)
            .With(new TestPosition { X = 0f, Y = 0f });

        var entityMap = buffer.Flush(world);

        var entity = entityMap[cmd.PlaceholderId];
        Assert.True(world.IsAlive(entity));
        Assert.Null(world.GetName(entity));
    }

    [Fact]
    public void Spawn_DuplicateName_ThrowsOnFlush()
    {
        using var world = new World();

        // Create an entity with name "Player" directly
        world.Spawn("Player").With(new TestPosition { X = 0f, Y = 0f }).Build();

        var buffer = new CommandBuffer();
        buffer.Spawn("Player")  // Duplicate name
            .With(new TestPosition { X = 10f, Y = 10f });

        // Should throw because name already exists
        Assert.Throws<ArgumentException>(() => buffer.Flush(world));
    }

    [Fact]
    public void Spawn_NamedAndUnnamed_BothCreated()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        buffer.Spawn("Named").With(new TestPosition { X = 1f, Y = 1f });
        buffer.Spawn().With(new TestPosition { X = 2f, Y = 2f });  // Unnamed
        buffer.Spawn("AlsoNamed").With(new TestPosition { X = 3f, Y = 3f });

        buffer.Flush(world);

        Assert.Equal(3, world.GetAllEntities().Count());
        Assert.True(world.GetEntityByName("Named").IsValid);
        Assert.True(world.GetEntityByName("AlsoNamed").IsValid);
    }

    #endregion

    #region SetComponent Tests

    [Fact]
    public void SetComponent_UpdatesExistingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        var buffer = new CommandBuffer();
        buffer.SetComponent(entity, new TestPosition { X = 100f, Y = 200f });

        // Value should not change before flush
        ref var posBefore = ref world.Get<TestPosition>(entity);
        Assert.Equal(0f, posBefore.X);
        Assert.Equal(0f, posBefore.Y);

        buffer.Flush(world);

        // Value should be updated after flush
        ref var posAfter = ref world.Get<TestPosition>(entity);
        Assert.Equal(100f, posAfter.X);
        Assert.Equal(200f, posAfter.Y);
    }

    [Fact]
    public void SetComponent_OnPlaceholderEntity_UpdatesComponent()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f });

        // Set component after spawn
        buffer.SetComponent(cmd.PlaceholderId, new TestPosition { X = 50f, Y = 75f });

        var entityMap = buffer.Flush(world);
        var entity = entityMap[cmd.PlaceholderId];

        ref var pos = ref world.Get<TestPosition>(entity);
        Assert.Equal(50f, pos.X);
        Assert.Equal(75f, pos.Y);
    }

    [Fact]
    public void SetComponent_MultipleUpdates_LastOneWins()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        var buffer = new CommandBuffer();
        buffer.SetComponent(entity, new TestPosition { X = 10f, Y = 10f });
        buffer.SetComponent(entity, new TestPosition { X = 20f, Y = 20f });
        buffer.SetComponent(entity, new TestPosition { X = 30f, Y = 30f });

        buffer.Flush(world);

        ref var pos = ref world.Get<TestPosition>(entity);
        Assert.Equal(30f, pos.X);
        Assert.Equal(30f, pos.Y);
    }

    [Fact]
    public void SetComponent_OnNonExistentEntity_SilentlyIgnored()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var nonExistentEntity = new Entity(999, 1);
        buffer.SetComponent(nonExistentEntity, new TestPosition { X = 100f, Y = 100f });

        // Should not throw
        buffer.Flush(world);
    }

    [Fact]
    public void SetComponent_OnDespawnedEntity_SilentlyIgnored()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.Despawn(entity);

        var buffer = new CommandBuffer();
        buffer.SetComponent(entity, new TestPosition { X = 100f, Y = 100f });

        // Should not throw
        buffer.Flush(world);
    }

    [Fact]
    public void SetComponent_AddThenSet_BothApplied()
    {
        using var world = new World();
        var buffer = new CommandBuffer();

        var cmd = buffer.Spawn();

        buffer.AddComponent(cmd.PlaceholderId, new TestPosition { X = 0f, Y = 0f });
        buffer.SetComponent(cmd.PlaceholderId, new TestPosition { X = 25f, Y = 50f });

        var entityMap = buffer.Flush(world);
        var entity = entityMap[cmd.PlaceholderId];

        Assert.True(world.Has<TestPosition>(entity));
        ref var pos = ref world.Get<TestPosition>(entity);
        Assert.Equal(25f, pos.X);
        Assert.Equal(50f, pos.Y);
    }

    [Fact]
    public void SetComponent_WorksWithMultipleComponentTypes()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        var buffer = new CommandBuffer();
        buffer.SetComponent(entity, new TestPosition { X = 10f, Y = 20f });
        buffer.SetComponent(entity, new TestVelocity { X = 5f, Y = 5f });
        buffer.SetComponent(entity, new TestHealth { Current = 50, Max = 100 });

        buffer.Flush(world);

        ref var pos = ref world.Get<TestPosition>(entity);
        ref var vel = ref world.Get<TestVelocity>(entity);
        ref var health = ref world.Get<TestHealth>(entity);

        Assert.Equal(10f, pos.X);
        Assert.Equal(20f, pos.Y);
        Assert.Equal(5f, vel.X);
        Assert.Equal(5f, vel.Y);
        Assert.Equal(50, health.Current);
        Assert.Equal(100, health.Max);
    }

    #endregion
}
