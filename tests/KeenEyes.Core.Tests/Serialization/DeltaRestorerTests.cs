using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Serialization;

namespace KeenEyes.Tests.Serialization;

/// <summary>
/// Tests for the DeltaRestorer class which applies delta snapshots to restore world state.
/// </summary>
public class DeltaRestorerTests
{
    private readonly TestComponentSerializer serializer = TestSerializerFactory.CreateForSerializationTests();

    // JSON options matching the test serializer
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    #region ApplyDelta Tests

    [Fact]
    public void ApplyDelta_WithEmptyDelta_MakesNoChanges()
    {
        using var world = new World();
        var entity = world.Spawn("Entity").With(new SerializablePosition { X = 1, Y = 2 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var emptyDelta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1
        };

        var newEntityMap = DeltaRestorer.ApplyDelta(world, emptyDelta, serializer, entityMap);

        Assert.Single(newEntityMap);
        Assert.Single(world.GetAllEntities());
        var restoredEntity = world.GetEntityByName("Entity");
        ref var pos = ref world.Get<SerializablePosition>(restoredEntity);
        Assert.Equal(1f, pos.X);
        Assert.Equal(2f, pos.Y);
    }

    [Fact]
    public void ApplyDelta_WithCreatedEntities_CreatesNewEntities()
    {
        using var world = new World();
        world.Spawn("Existing").With(new SerializablePosition { X = 0, Y = 0 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var newEntity1 = new SerializedEntity
        {
            Id = 100,
            Name = "New1",
            Components =
            [
                new SerializedComponent
                {
                    TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                    Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 10, Y = 20 }, jsonOptions),
                    IsTag = false
                }
            ]
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            CreatedEntities = new[] { newEntity1 }
        };

        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        Assert.Equal(2, newEntityMap.Count);
        Assert.Equal(2, world.GetAllEntities().Count());
        var created = world.GetEntityByName("New1");
        Assert.True(created.IsValid);
    }

    [Fact]
    public void ApplyDelta_WithDestroyedEntities_RemovesEntities()
    {
        using var world = new World();
        var entity1 = world.Spawn("Entity1").With(new SerializablePosition { X = 0, Y = 0 }).Build();
        var entity2 = world.Spawn("Entity2").With(new SerializablePosition { X = 1, Y = 1 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            DestroyedEntityIds = new[] { entity1.Id }
        };

        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        Assert.Single(newEntityMap);
        Assert.DoesNotContain(entity1.Id, newEntityMap.Keys);
        Assert.Single(world.GetAllEntities());
        Assert.False(world.GetEntityByName("Entity1").IsValid);
        Assert.True(world.GetEntityByName("Entity2").IsValid);
    }

    [Fact]
    public void ApplyDelta_WithModifiedComponents_UpdatesComponentData()
    {
        using var world = new World();
        var entity = world.Spawn("Entity")
            .With(new SerializablePosition { X = 10, Y = 20 })
            .Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var modifiedComponent = new SerializedComponent
        {
            TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
            Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 100, Y = 200 }, jsonOptions),
            IsTag = false
        };

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            ModifiedComponents = new[] { modifiedComponent }
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        var restoredEntity = world.GetEntityByName("Entity");
        ref var pos = ref world.Get<SerializablePosition>(restoredEntity);
        Assert.Equal(100f, pos.X);
        Assert.Equal(200f, pos.Y);
    }

    [Fact]
    public void ApplyDelta_WithAddedComponents_AddsComponentsToEntity()
    {
        using var world = new World();
        var entity = world.Spawn("Entity")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var addedComponent = new SerializedComponent
        {
            TypeName = typeof(SerializableHealth).AssemblyQualifiedName!,
            Data = JsonSerializer.SerializeToElement(new SerializableHealth { Current = 80, Max = 100 }, jsonOptions),
            IsTag = false
        };

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            AddedComponents = new[] { addedComponent }
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        var restoredEntity = world.GetEntityByName("Entity");
        Assert.True(world.Has<SerializableHealth>(restoredEntity));
        ref var health = ref world.Get<SerializableHealth>(restoredEntity);
        Assert.Equal(80, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void ApplyDelta_WithRemovedComponents_RemovesComponentsFromEntity()
    {
        using var world = new World();
        var entity = world.Spawn("Entity")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .With(new SerializableHealth { Current = 100, Max = 100 })
            .Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            RemovedComponentTypes = new[] { typeof(SerializableHealth).AssemblyQualifiedName! }
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        var restoredEntity = world.GetEntityByName("Entity");
        Assert.False(world.Has<SerializableHealth>(restoredEntity));
        Assert.True(world.Has<SerializablePosition>(restoredEntity));
    }

    [Fact]
    public void ApplyDelta_WithNameChange_UpdatesEntityName()
    {
        using var world = new World();
        var entity = world.Spawn("OldName")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            NewName = "NewName"
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        Assert.False(world.GetEntityByName("OldName").IsValid);
        var renamed = world.GetEntityByName("NewName");
        Assert.True(renamed.IsValid);
    }

    [Fact]
    public void ApplyDelta_WithParentChange_UpdatesHierarchy()
    {
        using var world = new World();
        var parent = world.Spawn("Parent").With(new SerializablePosition { X = 0, Y = 0 }).Build();
        var child = world.Spawn("Child").With(new SerializablePosition { X = 1, Y = 1 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var entityDelta = new EntityDelta
        {
            EntityId = child.Id,
            NewParentId = parent.Id
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        var restoredParent = world.GetEntityByName("Parent");
        var restoredChild = world.GetEntityByName("Child");
        Assert.Equal(restoredParent, world.GetParent(restoredChild));
    }

    [Fact]
    public void ApplyDelta_WithParentRemoved_OrphansEntity()
    {
        using var world = new World();
        var parent = world.Spawn("Parent").With(new SerializablePosition { X = 0, Y = 0 }).Build();
        var child = world.Spawn("Child").With(new SerializablePosition { X = 1, Y = 1 }).Build();
        world.SetParent(child, parent);

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var entityDelta = new EntityDelta
        {
            EntityId = child.Id,
            ParentRemoved = true
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        var restoredChild = world.GetEntityByName("Child");
        Assert.False(world.GetParent(restoredChild).IsValid);
    }

    [Fact]
    public void ApplyDelta_WithModifiedSingletons_UpdatesSingletonData()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 100f, DeltaTime = 0.016f });

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var modifiedSingleton = new SerializedSingleton
        {
            TypeName = typeof(SerializableGameTime).AssemblyQualifiedName!,
            Data = JsonSerializer.SerializeToElement(new SerializableGameTime { TotalTime = 200f, DeltaTime = 0.033f }, jsonOptions)
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedSingletons = new[] { modifiedSingleton }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        ref var time = ref world.GetSingleton<SerializableGameTime>();
        Assert.Equal(200f, time.TotalTime);
        Assert.Equal(0.033f, time.DeltaTime);
    }

    [Fact]
    public void ApplyDelta_WithRemovedSingletons_RemovesSingletons()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 100f, DeltaTime = 0.016f });
        world.SetSingleton(new SerializableConfig { MaxPlayers = 4, GameName = "Test" });

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            RemovedSingletonTypes = new[] { typeof(SerializableGameTime).AssemblyQualifiedName! }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        Assert.False(world.HasSingleton<SerializableGameTime>());
        Assert.True(world.HasSingleton<SerializableConfig>());
    }

    [Fact]
    public void ApplyDelta_WithCreatedEntitiesAndHierarchy_RestoresParentRelationships()
    {
        using var world = new World();
        world.Spawn("Existing").With(new SerializablePosition { X = 0, Y = 0 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var parentSerialized = new SerializedEntity
        {
            Id = 100,
            Name = "Parent",
            Components =
            [
                new SerializedComponent
                {
                    TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                    Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 1, Y = 1 }, jsonOptions),
                    IsTag = false
                }
            ]
        };

        var childSerialized = new SerializedEntity
        {
            Id = 101,
            Name = "Child",
            ParentId = 100,
            Components =
            [
                new SerializedComponent
                {
                    TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                    Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 2, Y = 2 }, jsonOptions),
                    IsTag = false
                }
            ]
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            CreatedEntities = new[] { parentSerialized, childSerialized }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        var parent = world.GetEntityByName("Parent");
        var child = world.GetEntityByName("Child");
        Assert.Equal(parent, world.GetParent(child));
    }

    [Fact]
    public void ApplyDelta_WithMultipleChanges_AppliesAllChanges()
    {
        using var world = new World();
        var entity1 = world.Spawn("Entity1").With(new SerializablePosition { X = 1, Y = 2 }).Build();
        var entity2 = world.Spawn("Entity2").With(new SerializablePosition { X = 3, Y = 4 }).Build();
        world.SetSingleton(new SerializableGameTime { TotalTime = 50f, DeltaTime = 0.016f });

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var newEntity = new SerializedEntity
        {
            Id = 200,
            Name = "New",
            Components =
            [
                new SerializedComponent
                {
                    TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                    Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 5, Y = 6 }, jsonOptions),
                    IsTag = false
                }
            ]
        };

        var entityDelta = new EntityDelta
        {
            EntityId = entity2.Id,
            AddedComponents = new[]
            {
                new SerializedComponent
                {
                    TypeName = typeof(SerializableHealth).AssemblyQualifiedName!,
                    Data = JsonSerializer.SerializeToElement(new SerializableHealth { Current = 80, Max = 100 }, jsonOptions),
                    IsTag = false
                }
            }
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            CreatedEntities = new[] { newEntity },
            DestroyedEntityIds = new[] { entity1.Id },
            ModifiedEntities = new[] { entityDelta },
            ModifiedSingletons = new[]
            {
                new SerializedSingleton
                {
                    TypeName = typeof(SerializableGameTime).AssemblyQualifiedName!,
                    Data = JsonSerializer.SerializeToElement(new SerializableGameTime { TotalTime = 100f, DeltaTime = 0.033f }, jsonOptions)
                }
            }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        Assert.Equal(2, world.GetAllEntities().Count());
        Assert.False(world.GetEntityByName("Entity1").IsValid);
        Assert.True(world.GetEntityByName("Entity2").IsValid);
        Assert.True(world.GetEntityByName("New").IsValid);
        Assert.True(world.Has<SerializableHealth>(world.GetEntityByName("Entity2")));
        ref var time = ref world.GetSingleton<SerializableGameTime>();
        Assert.Equal(100f, time.TotalTime);
    }

    [Fact]
    public void ApplyDelta_ReturnsUpdatedEntityMap()
    {
        using var world = new World();
        var entity = world.Spawn("Entity").With(new SerializablePosition { X = 0, Y = 0 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var newEntity = new SerializedEntity
        {
            Id = 100,
            Name = "New",
            Components =
            [
                new SerializedComponent
                {
                    TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                    Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 1, Y = 1 }, jsonOptions),
                    IsTag = false
                }
            ]
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            CreatedEntities = new[] { newEntity },
            DestroyedEntityIds = new[] { entity.Id }
        };

        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        Assert.Single(newEntityMap);
        Assert.DoesNotContain(entity.Id, newEntityMap.Keys);
        Assert.Contains(100, newEntityMap.Keys);
    }

    [Fact]
    public void ApplyDelta_WithTagComponents_RestoresTagsCorrectly()
    {
        using var world = new World();
        var entity = world.Spawn("Entity")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var addedTag = new SerializedComponent
        {
            TypeName = typeof(SerializableTag).AssemblyQualifiedName!,
            Data = null,
            IsTag = true
        };

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            AddedComponents = new[] { addedTag }
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        var restoredEntity = world.GetEntityByName("Entity");
        Assert.True(world.Has<SerializableTag>(restoredEntity));
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void ApplyDelta_WithNullWorld_ThrowsArgumentNullException()
    {
        var delta = new DeltaSnapshot { BaselineSlotName = "baseline", SequenceNumber = 1 };
        var entityMap = new Dictionary<int, Entity>();

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(null!, delta, serializer, entityMap));
    }

    [Fact]
    public void ApplyDelta_WithNullDelta_ThrowsArgumentNullException()
    {
        using var world = new World();
        var entityMap = new Dictionary<int, Entity>();

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(world, null!, serializer, entityMap));
    }

    [Fact]
    public void ApplyDelta_WithNullSerializer_ThrowsArgumentNullException()
    {
        using var world = new World();
        var delta = new DeltaSnapshot { BaselineSlotName = "baseline", SequenceNumber = 1 };
        var entityMap = new Dictionary<int, Entity>();

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(world, delta, null!, entityMap));
    }

    [Fact]
    public void ApplyDelta_WithNullEntityMap_ThrowsArgumentNullException()
    {
        using var world = new World();
        var delta = new DeltaSnapshot { BaselineSlotName = "baseline", SequenceNumber = 1 };

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(world, delta, serializer, null!));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ApplyDelta_WithNonExistentEntity_SkipsModification()
    {
        using var world = new World();
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var entityDelta = new EntityDelta
        {
            EntityId = 999, // Non-existent
            NewName = "NewName"
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        // Should not throw
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);
    }

    [Fact]
    public void ApplyDelta_WithDeadEntity_SkipsModification()
    {
        using var world = new World();
        var entity = world.Spawn("Entity").With(new SerializablePosition { X = 0, Y = 0 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        // Despawn the entity
        world.Despawn(world.GetEntityByName("Entity"));

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            NewName = "NewName"
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        // Should not throw
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);
    }

    [Fact]
    public void ApplyDelta_WithUnknownComponentType_SkipsComponent()
    {
        using var world = new World();
        var entity = world.Spawn("Entity").With(new SerializablePosition { X = 0, Y = 0 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var unknownComponent = new SerializedComponent
        {
            TypeName = "Unknown.Component.Type",
            Data = JsonSerializer.SerializeToElement(new { Value = 42 }, jsonOptions),
            IsTag = false
        };

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            AddedComponents = new[] { unknownComponent }
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        // Should not throw - just skip unknown component
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);
    }

    [Fact]
    public void ApplyDelta_WithUnknownSingletonType_SkipsSingleton()
    {
        using var world = new World();
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var unknownSingleton = new SerializedSingleton
        {
            TypeName = "Unknown.Singleton.Type",
            Data = System.Text.Json.JsonSerializer.SerializeToElement(new { Value = 42 })
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedSingletons = new[] { unknownSingleton }
        };

        // Should not throw - just skip unknown singleton
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);
    }

    [Fact]
    public void ApplyDelta_WithInvalidParentId_SkipsParentChange()
    {
        using var world = new World();
        var entity = world.Spawn("Entity").With(new SerializablePosition { X = 0, Y = 0 }).Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        var entityDelta = new EntityDelta
        {
            EntityId = entity.Id,
            NewParentId = 999 // Non-existent parent
        };

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[] { entityDelta }
        };

        // Should not throw - just skip invalid parent
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);
    }

    [Fact]
    public void ApplyDelta_SequentialDeltas_AppliesInOrder()
    {
        using var world = new World();
        var entity = world.Spawn("Entity")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();

        var baseline = SnapshotManager.CreateSnapshot(world, serializer);
        var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);

        // First delta: modify X
        var delta1 = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = new[]
            {
                new EntityDelta
                {
                    EntityId = entity.Id,
                    ModifiedComponents = new[]
                    {
                        new SerializedComponent
                        {
                            TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                            Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 100, Y = 0 }, jsonOptions),
                            IsTag = false
                        }
                    }
                }
            }
        };

        // Second delta: modify Y
        var delta2 = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 2,
            ModifiedEntities = new[]
            {
                new EntityDelta
                {
                    EntityId = entity.Id,
                    ModifiedComponents = new[]
                    {
                        new SerializedComponent
                        {
                            TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                            Data = JsonSerializer.SerializeToElement(new SerializablePosition { X = 100, Y = 200 }, jsonOptions),
                            IsTag = false
                        }
                    }
                }
            }
        };

        entityMap = DeltaRestorer.ApplyDelta(world, delta1, serializer, entityMap);
        entityMap = DeltaRestorer.ApplyDelta(world, delta2, serializer, entityMap);

        var restoredEntity = world.GetEntityByName("Entity");
        ref var pos = ref world.Get<SerializablePosition>(restoredEntity);
        Assert.Equal(100f, pos.X);
        Assert.Equal(200f, pos.Y);
    }

    #endregion
}
