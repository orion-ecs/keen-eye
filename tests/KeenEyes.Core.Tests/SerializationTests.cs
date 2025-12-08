using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Test components for serialization tests.
/// </summary>
public struct SerializablePosition : IComponent
{
    public float X;
    public float Y;
}

public struct SerializableVelocity : IComponent
{
    public float X;
    public float Y;
}

public struct SerializableHealth : IComponent
{
    public int Current;
    public int Max;
}

public struct SerializableTag : ITagComponent { }

public struct SerializableGameTime
{
    public float TotalTime;
    public float DeltaTime;
}

public struct SerializableConfig
{
    public int MaxPlayers;
    public string GameName;
}

/// <summary>
/// Tests for the World serialization and snapshot system.
/// </summary>
public class SerializationTests
{
    #region CreateSnapshot Tests

    [Fact]
    public void CreateSnapshot_EmptyWorld_ReturnsEmptySnapshot()
    {
        using var world = new World();

        var snapshot = SnapshotManager.CreateSnapshot(world);

        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.Entities);
        Assert.Empty(snapshot.Singletons);
        Assert.Equal(1, snapshot.Version);
    }

    [Fact]
    public void CreateSnapshot_CapturesAllEntities()
    {
        using var world = new World();
        world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();
        world.Spawn().With(new SerializablePosition { X = 3, Y = 4 }).Build();
        world.Spawn().With(new SerializablePosition { X = 5, Y = 6 }).Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);

        Assert.Equal(3, snapshot.Entities.Count);
    }

    [Fact]
    public void CreateSnapshot_CapturesComponentData()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new SerializablePosition { X = 10f, Y = 20f })
            .With(new SerializableHealth { Current = 80, Max = 100 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);

        Assert.Single(snapshot.Entities);
        Assert.Equal(2, snapshot.Entities[0].Components.Count);

        var posComponent = snapshot.Entities[0].Components
            .First(c => c.TypeName.Contains("SerializablePosition"));
        Assert.NotNull(posComponent);

        var healthComponent = snapshot.Entities[0].Components
            .First(c => c.TypeName.Contains("SerializableHealth"));
        Assert.NotNull(healthComponent);
    }

    [Fact]
    public void CreateSnapshot_CapturesEntityNames()
    {
        using var world = new World();
        var namedEntity = world.Spawn("Player")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var unnamedEntity = world.Spawn()
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);

        var namedSerialized = snapshot.Entities.First(e => e.Name == "Player");
        var unnamedSerialized = snapshot.Entities.First(e => e.Name == null);

        Assert.NotNull(namedSerialized);
        Assert.NotNull(unnamedSerialized);
    }

    [Fact]
    public void CreateSnapshot_CapturesHierarchy()
    {
        using var world = new World();
        var parent = world.Spawn("Parent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var child = world.Spawn("Child")
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();

        world.SetParent(child, parent);

        var snapshot = SnapshotManager.CreateSnapshot(world);

        var parentSerialized = snapshot.Entities.First(e => e.Name == "Parent");
        var childSerialized = snapshot.Entities.First(e => e.Name == "Child");

        Assert.Null(parentSerialized.ParentId);
        Assert.Equal(parent.Id, childSerialized.ParentId);
    }

    [Fact]
    public void CreateSnapshot_CapturesSingletons()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 100f, DeltaTime = 0.016f });
        world.SetSingleton(new SerializableConfig { MaxPlayers = 8, GameName = "Test Game" });

        var snapshot = SnapshotManager.CreateSnapshot(world);

        Assert.Equal(2, snapshot.Singletons.Count);
    }

    [Fact]
    public void CreateSnapshot_IncludesTimestamp()
    {
        using var world = new World();
        var before = DateTimeOffset.UtcNow;

        var snapshot = SnapshotManager.CreateSnapshot(world);

        var after = DateTimeOffset.UtcNow;
        Assert.True(snapshot.Timestamp >= before);
        Assert.True(snapshot.Timestamp <= after);
    }

    [Fact]
    public void CreateSnapshot_IncludesMetadata()
    {
        using var world = new World();
        var metadata = new Dictionary<string, object>
        {
            ["saveSlot"] = 1,
            ["playerName"] = "TestPlayer"
        };

        var snapshot = SnapshotManager.CreateSnapshot(world, metadata);

        Assert.NotNull(snapshot.Metadata);
        Assert.Equal(1, snapshot.Metadata["saveSlot"]);
        Assert.Equal("TestPlayer", snapshot.Metadata["playerName"]);
    }

    [Fact]
    public void CreateSnapshot_CapturesTagComponents()
    {
        using var world = new World();
        world.Spawn()
            .With(new SerializablePosition { X = 0, Y = 0 })
            .WithTag<SerializableTag>()
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);

        Assert.Equal(2, snapshot.Entities[0].Components.Count);
        var tagComponent = snapshot.Entities[0].Components
            .First(c => c.TypeName.Contains("SerializableTag"));
        Assert.True(tagComponent.IsTag);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        using var world = new World();
        world.Spawn()
            .With(new SerializablePosition { X = 10, Y = 20 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);
        var json = SnapshotManager.ToJson(snapshot);

        Assert.NotNull(json);
        Assert.Contains("entities", json);
        Assert.Contains("singletons", json);
    }

    [Fact]
    public void FromJson_DeserializesSnapshot()
    {
        using var world = new World();
        world.Spawn("TestEntity")
            .With(new SerializablePosition { X = 5, Y = 10 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);
        var json = SnapshotManager.ToJson(snapshot);
        var restored = SnapshotManager.FromJson(json);

        Assert.NotNull(restored);
        Assert.Single(restored!.Entities);
        Assert.Equal("TestEntity", restored.Entities[0].Name);
    }

    [Fact]
    public void JsonRoundTrip_PreservesEntityData()
    {
        using var world = new World();
        world.Spawn("Player")
            .With(new SerializablePosition { X = 100f, Y = 200f })
            .With(new SerializableHealth { Current = 75, Max = 100 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);
        var json = SnapshotManager.ToJson(snapshot);
        var restored = SnapshotManager.FromJson(json);

        Assert.NotNull(restored);
        Assert.Single(restored!.Entities);
        Assert.Equal("Player", restored.Entities[0].Name);
        Assert.Equal(2, restored.Entities[0].Components.Count);
    }

    [Fact]
    public void JsonRoundTrip_PreservesHierarchy()
    {
        using var world = new World();
        var parent = world.Spawn("Parent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var child = world.Spawn("Child")
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();
        world.SetParent(child, parent);

        var snapshot = SnapshotManager.CreateSnapshot(world);
        var json = SnapshotManager.ToJson(snapshot);
        var restored = SnapshotManager.FromJson(json);

        Assert.NotNull(restored);
        var childEntity = restored!.Entities.First(e => e.Name == "Child");
        Assert.Equal(parent.Id, childEntity.ParentId);
    }

    [Fact]
    public void JsonRoundTrip_PreservesSingletons()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 50f, DeltaTime = 0.033f });

        var snapshot = SnapshotManager.CreateSnapshot(world);
        var json = SnapshotManager.ToJson(snapshot);
        var restored = SnapshotManager.FromJson(json);

        Assert.NotNull(restored);
        Assert.Single(restored!.Singletons);
    }

    #endregion

    #region RestoreSnapshot Tests

    [Fact]
    public void RestoreSnapshot_RecreatesEntities()
    {
        using var world1 = new World();
        world1.Spawn("Entity1").With(new SerializablePosition { X = 1, Y = 2 }).Build();
        world1.Spawn("Entity2").With(new SerializablePosition { X = 3, Y = 4 }).Build();

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();
        var entityMap = SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!);

        Assert.Equal(2, entityMap.Count);
        Assert.Equal(2, world2.GetAllEntities().Count());
    }

    [Fact]
    public void RestoreSnapshot_RecreatesComponentData()
    {
        using var world1 = new World();
        world1.Spawn("Player")
            .With(new SerializablePosition { X = 100f, Y = 200f })
            .With(new SerializableHealth { Current = 85, Max = 100 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();
        _ = SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!);

        var restoredEntity = world2.GetEntityByName("Player");
        Assert.True(restoredEntity.IsValid);

        ref var pos = ref world2.Get<SerializablePosition>(restoredEntity);
        Assert.Equal(100f, pos.X);
        Assert.Equal(200f, pos.Y);

        ref var health = ref world2.Get<SerializableHealth>(restoredEntity);
        Assert.Equal(85, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void RestoreSnapshot_RecreatesHierarchy()
    {
        using var world1 = new World();
        var parent = world1.Spawn("Parent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var child = world1.Spawn("Child")
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();
        world1.SetParent(child, parent);

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();
        SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!);

        var restoredParent = world2.GetEntityByName("Parent");
        var restoredChild = world2.GetEntityByName("Child");

        Assert.True(restoredParent.IsValid);
        Assert.True(restoredChild.IsValid);

        var actualParent = world2.GetParent(restoredChild);
        Assert.Equal(restoredParent, actualParent);
    }

    [Fact]
    public void RestoreSnapshot_RestoresSingletons()
    {
        using var world1 = new World();
        world1.SetSingleton(new SerializableGameTime { TotalTime = 123.456f, DeltaTime = 0.016f });

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();
        SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!);

        Assert.True(world2.HasSingleton<SerializableGameTime>());
        ref var time = ref world2.GetSingleton<SerializableGameTime>();
        Assert.Equal(123.456f, time.TotalTime);
        Assert.Equal(0.016f, time.DeltaTime);
    }

    [Fact]
    public void RestoreSnapshot_ClearsExistingWorld()
    {
        using var world = new World();

        // Add some initial entities
        world.Spawn("Existing1").With(new SerializablePosition { X = 0, Y = 0 }).Build();
        world.Spawn("Existing2").With(new SerializablePosition { X = 1, Y = 1 }).Build();
        world.SetSingleton(new SerializableGameTime { TotalTime = 999f, DeltaTime = 1f });

        // Create a snapshot with different entities
        using var sourceWorld = new World();
        sourceWorld.Spawn("New1").With(new SerializablePosition { X = 10, Y = 10 }).Build();

        var snapshot = SnapshotManager.CreateSnapshot(sourceWorld);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        // Restore should clear the existing entities
        SnapshotManager.RestoreSnapshot(world, loadedSnapshot!);

        var allEntities = world.GetAllEntities().ToList();
        Assert.Single(allEntities);

        var existing1 = world.GetEntityByName("Existing1");
        Assert.False(existing1.IsValid);

        var new1 = world.GetEntityByName("New1");
        Assert.True(new1.IsValid);

        // Old singleton should be cleared
        Assert.False(world.HasSingleton<SerializableGameTime>());
    }

    [Fact]
    public void RestoreSnapshot_ReturnsEntityIdMapping()
    {
        using var world1 = new World();
        var original = world1.Spawn("Test")
            .With(new SerializablePosition { X = 5, Y = 10 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();
        var entityMap = SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!);

        Assert.Single(entityMap);
        Assert.True(entityMap.TryGetValue(original.Id, out var restoredEntity));
        Assert.True(world2.IsAlive(restoredEntity));
    }

    #endregion

    #region World.Clear Tests

    [Fact]
    public void World_Clear_RemovesAllEntities()
    {
        using var world = new World();
        world.Spawn().With(new SerializablePosition { X = 0, Y = 0 }).Build();
        world.Spawn().With(new SerializablePosition { X = 1, Y = 1 }).Build();
        world.Spawn().With(new SerializablePosition { X = 2, Y = 2 }).Build();

        world.Clear();

        Assert.Empty(world.GetAllEntities());
    }

    [Fact]
    public void World_Clear_RemovesSingletons()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 100f, DeltaTime = 0.016f });

        world.Clear();

        Assert.False(world.HasSingleton<SerializableGameTime>());
    }

    [Fact]
    public void World_Clear_InvalidatesOldEntityHandles()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();

        world.Clear();

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void World_Clear_AllowsNewEntitiesToBeCreated()
    {
        using var world = new World();
        world.Spawn().With(new SerializablePosition { X = 0, Y = 0 }).Build();

        world.Clear();

        var newEntity = world.Spawn()
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();

        Assert.True(world.IsAlive(newEntity));
    }

    [Fact]
    public void World_Clear_ClearsHierarchy()
    {
        using var world = new World();
        var parent = world.Spawn("Parent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var child = world.Spawn("Child")
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();
        world.SetParent(child, parent);

        world.Clear();

        // Create new entities
        var newParent = world.Spawn("Parent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();

        // New entity should have no parent
        Assert.False(world.GetParent(newParent).IsValid);
    }

    [Fact]
    public void World_Clear_ClearsEntityNames()
    {
        using var world = new World();
        world.Spawn("TestName")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();

        world.Clear();

        // Name should be available for reuse
        var newEntity = world.Spawn("TestName")
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();

        Assert.True(world.IsAlive(newEntity));
        Assert.Equal("TestName", world.GetName(newEntity));
    }

    #endregion

    #region GetAllSingletons Tests

    [Fact]
    public void GetAllSingletons_ReturnsEmpty_WhenNoSingletons()
    {
        using var world = new World();

        var singletons = world.GetAllSingletons().ToList();

        Assert.Empty(singletons);
    }

    [Fact]
    public void GetAllSingletons_ReturnsAllSingletons()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 100f, DeltaTime = 0.016f });
        world.SetSingleton(new SerializableConfig { MaxPlayers = 4, GameName = "Test" });

        var singletons = world.GetAllSingletons().ToList();

        Assert.Equal(2, singletons.Count);
    }

    [Fact]
    public void GetAllSingletons_ReturnsCorrectTypes()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 50f, DeltaTime = 0.033f });

        var singletons = world.GetAllSingletons().ToList();

        Assert.Single(singletons);
        Assert.Equal(typeof(SerializableGameTime), singletons[0].Type);
    }

    [Fact]
    public void GetAllSingletons_ReturnsCorrectValues()
    {
        using var world = new World();
        world.SetSingleton(new SerializableGameTime { TotalTime = 99.5f, DeltaTime = 0.01f });

        var singletons = world.GetAllSingletons().ToList();

        var gameTime = (SerializableGameTime)singletons[0].Value;
        Assert.Equal(99.5f, gameTime.TotalTime);
        Assert.Equal(0.01f, gameTime.DeltaTime);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CreateSnapshot_ThrowsOnNullWorld()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotManager.CreateSnapshot(null!));
    }

    [Fact]
    public void RestoreSnapshot_ThrowsOnNullWorld()
    {
        using var world = new World();
        var snapshot = SnapshotManager.CreateSnapshot(world);

        Assert.Throws<ArgumentNullException>(() => SnapshotManager.RestoreSnapshot(null!, snapshot));
    }

    [Fact]
    public void RestoreSnapshot_ThrowsOnNullSnapshot()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() => SnapshotManager.RestoreSnapshot(world, null!));
    }

    [Fact]
    public void ToJson_ThrowsOnNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotManager.ToJson(null!));
    }

    [Fact]
    public void FromJson_ThrowsOnNullJson()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotManager.FromJson(null!));
    }

    [Fact]
    public void ComplexHierarchy_RoundTripsCorrectly()
    {
        using var world1 = new World();

        // Create a complex hierarchy: grandparent -> parent -> child
        var grandparent = world1.Spawn("Grandparent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var parent = world1.Spawn("Parent")
            .With(new SerializablePosition { X = 1, Y = 1 })
            .Build();
        var child = world1.Spawn("Child")
            .With(new SerializablePosition { X = 2, Y = 2 })
            .Build();

        world1.SetParent(parent, grandparent);
        world1.SetParent(child, parent);

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();
        SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!);

        var restoredGrandparent = world2.GetEntityByName("Grandparent");
        var restoredParent = world2.GetEntityByName("Parent");
        var restoredChild = world2.GetEntityByName("Child");

        // Verify hierarchy
        Assert.False(world2.GetParent(restoredGrandparent).IsValid);
        Assert.Equal(restoredGrandparent, world2.GetParent(restoredParent));
        Assert.Equal(restoredParent, world2.GetParent(restoredChild));
    }

    [Fact]
    public void MultipleChildrenHierarchy_RoundTripsCorrectly()
    {
        using var world1 = new World();

        var parent = world1.Spawn("Parent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var child1 = world1.Spawn("Child1")
            .With(new SerializablePosition { X = 1, Y = 0 })
            .Build();
        var child2 = world1.Spawn("Child2")
            .With(new SerializablePosition { X = 2, Y = 0 })
            .Build();
        var child3 = world1.Spawn("Child3")
            .With(new SerializablePosition { X = 3, Y = 0 })
            .Build();

        world1.SetParent(child1, parent);
        world1.SetParent(child2, parent);
        world1.SetParent(child3, parent);

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();
        SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!);

        var restoredParent = world2.GetEntityByName("Parent");
        var restoredChildren = world2.GetChildren(restoredParent).ToList();

        Assert.Equal(3, restoredChildren.Count);
    }

    [Fact]
    public void LargeWorld_SnapshotPerformance()
    {
        using var world = new World();

        // Create 1000 entities
        for (int i = 0; i < 1000; i++)
        {
            world.Spawn($"Entity{i}")
                .With(new SerializablePosition { X = i, Y = i * 2f })
                .With(new SerializableHealth { Current = i % 100, Max = 100 })
                .Build();
        }

        var snapshot = SnapshotManager.CreateSnapshot(world);

        Assert.Equal(1000, snapshot.Entities.Count);
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public void GetDefaultJsonOptions_ReturnsValidOptions()
    {
        var options = SnapshotManager.GetdefaultJsonOptions();

        Assert.NotNull(options);
        Assert.True(options.WriteIndented);
        Assert.True(options.IncludeFields);
    }

    [Fact]
    public void RestoreSnapshot_WithCustomTypeResolver_UsesResolver()
    {
        using var world1 = new World();
        world1.Spawn("Test")
            .With(new SerializablePosition { X = 10, Y = 20 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();

        // Custom type resolver that maps type names
        Type? CustomResolver(string typeName)
        {
            if (typeName.Contains("SerializablePosition"))
            {
                return typeof(SerializablePosition);
            }
            return Type.GetType(typeName);
        }

        var entityMap = SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!, CustomResolver);

        Assert.Single(entityMap);
        var entity = world2.GetEntityByName("Test");
        Assert.True(entity.IsValid);
    }

    [Fact]
    public void RestoreSnapshot_WithUnknownComponentType_SkipsComponent()
    {
        // Create a snapshot with a fake component type that won't resolve
        var fakeSnapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = new List<SerializedEntity>
            {
                new SerializedEntity
                {
                    Id = 1,
                    Name = "TestEntity",
                    Components = new List<SerializedComponent>
                    {
                        new SerializedComponent
                        {
                            TypeName = "NonExistent.FakeComponent, FakeAssembly",
                            Data = new { X = 1, Y = 2 },
                            IsTag = false
                        }
                    }
                }
            },
            Singletons = new List<SerializedSingleton>()
        };

        using var world = new World();

        // Should not throw - just skip the unknown component
        var entityMap = SnapshotManager.RestoreSnapshot(world, fakeSnapshot);

        Assert.Single(entityMap);
        var entity = world.GetEntityByName("TestEntity");
        Assert.True(entity.IsValid);
    }

    [Fact]
    public void RestoreSnapshot_WithUnknownSingletonType_SkipsSingleton()
    {
        var fakeSnapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = new List<SerializedEntity>(),
            Singletons = new List<SerializedSingleton>
            {
                new SerializedSingleton
                {
                    TypeName = "NonExistent.FakeSingleton, FakeAssembly",
                    Data = new { Value = 42 }
                }
            }
        };

        using var world = new World();

        // Should not throw - just skip the unknown singleton
        var entityMap = SnapshotManager.RestoreSnapshot(world, fakeSnapshot);

        Assert.Empty(entityMap);
        // World should not have any singletons
        Assert.Empty(world.GetAllSingletons());
    }

    [Fact]
    public void RestoreSnapshot_WithMockSerializer_UsesAotPath()
    {
        using var world1 = new World();
        world1.Spawn("Test")
            .With(new SerializablePosition { X = 5, Y = 10 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();

        // Use a mock serializer
        var mockSerializer = new MockComponentSerializer();
        _ = SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!, serializer: mockSerializer);

        // The mock serializer was called
        Assert.True(mockSerializer.GetTypeCalled);
    }

    [Fact]
    public void ToJson_WithCustomOptions_UsesProvidedOptions()
    {
        using var world = new World();
        world.Spawn("Test")
            .With(new SerializablePosition { X = 1, Y = 2 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);

        var customOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            IncludeFields = true
        };

        var json = SnapshotManager.ToJson(snapshot, customOptions);

        // Non-indented JSON should not have newlines within the object
        Assert.DoesNotContain("\n  ", json);
    }

    [Fact]
    public void FromJson_WithCustomOptions_UsesProvidedOptions()
    {
        using var world = new World();
        world.Spawn("Test")
            .With(new SerializablePosition { X = 1, Y = 2 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world);
        var json = SnapshotManager.ToJson(snapshot);

        var customOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            IncludeFields = true
        };

        var restored = SnapshotManager.FromJson(json, customOptions);

        Assert.NotNull(restored);
        Assert.Single(restored!.Entities);
    }

    [Fact]
    public void RestoreSnapshot_WithWorkingAotSerializer_UsesSerializerType()
    {
        using var world1 = new World();
        world1.Spawn("Test")
            .With(new SerializablePosition { X = 15, Y = 25 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();

        // Use a working AOT serializer that returns the correct type
        var aotSerializer = new WorkingAotSerializer();
        var entityMap = SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!, serializer: aotSerializer);

        // The serializer was used for type resolution
        Assert.True(aotSerializer.GetTypeCalled);
        Assert.Single(entityMap);
    }

    [Fact]
    public void RestoreSnapshot_WithDeserializingAotSerializer_UsesDeserializedValue()
    {
        using var world1 = new World();
        world1.Spawn("Test")
            .With(new SerializablePosition { X = 100, Y = 200 })
            .Build();

        var snapshot = SnapshotManager.CreateSnapshot(world1);
        var json = SnapshotManager.ToJson(snapshot);
        var loadedSnapshot = SnapshotManager.FromJson(json);

        using var world2 = new World();

        // Use an AOT serializer that actually deserializes
        var aotSerializer = new DeserializingAotSerializer();
        var entityMap = SnapshotManager.RestoreSnapshot(world2, loadedSnapshot!, serializer: aotSerializer);

        Assert.True(aotSerializer.DeserializeCalled);
        Assert.Single(entityMap);

        // Verify the deserialized values
        var entity = world2.GetEntityByName("Test");
        ref var pos = ref world2.Get<SerializablePosition>(entity);
        Assert.Equal(100f, pos.X);
        Assert.Equal(200f, pos.Y);
    }

    [Fact]
    public void SerializedComponent_WithExpression_CreatesCopy()
    {
        var original = new SerializedComponent
        {
            TypeName = "Test.Component",
            Data = new { X = 1 },
            IsTag = false
        };

        var copy = original with { IsTag = true };

        Assert.Equal(original.TypeName, copy.TypeName);
        Assert.Equal(original.Data, copy.Data);
        Assert.True(copy.IsTag);
        Assert.False(original.IsTag);
    }

    [Fact]
    public void SerializedEntity_WithExpression_CreatesCopy()
    {
        var original = new SerializedEntity
        {
            Id = 1,
            Name = "Original",
            Components = new List<SerializedComponent>(),
            ParentId = null
        };

        var copy = original with { Name = "Copy", ParentId = 5 };

        Assert.Equal(original.Id, copy.Id);
        Assert.Equal("Copy", copy.Name);
        Assert.Equal(5, copy.ParentId);
        Assert.Null(original.ParentId);
    }

    [Fact]
    public void SerializedSingleton_WithExpression_CreatesCopy()
    {
        var original = new SerializedSingleton
        {
            TypeName = "Test.Singleton",
            Data = new { Value = 42 }
        };

        var copy = original with { TypeName = "Modified.Singleton" };

        Assert.Equal("Modified.Singleton", copy.TypeName);
        Assert.Equal(original.Data, copy.Data);
    }

    [Fact]
    public void WorldSnapshot_WithExpression_CreatesCopy()
    {
        var original = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = new List<SerializedEntity>(),
            Singletons = new List<SerializedSingleton>(),
            Version = 1
        };

        var copy = original with { Version = 2 };

        Assert.Equal(2, copy.Version);
        Assert.Equal(1, original.Version);
        Assert.Equal(original.Timestamp, copy.Timestamp);
    }

    #endregion
}

/// <summary>
/// Mock component serializer for testing the AOT serialization path.
/// </summary>
internal sealed class MockComponentSerializer : IComponentSerializer
{
    public bool GetTypeCalled { get; private set; }
    public bool DeserializeCalled { get; private set; }
    public bool SerializeCalled { get; private set; }

    public bool IsSerializable(Type type) => false;
    public bool IsSerializable(string typeName) => false;

    public object? Deserialize(string typeName, System.Text.Json.JsonElement json)
    {
        DeserializeCalled = true;
        return null; // Return null to fall back to reflection
    }

    public System.Text.Json.JsonElement? Serialize(Type type, object value)
    {
        SerializeCalled = true;
        return null;
    }

    public Type? GetType(string typeName)
    {
        GetTypeCalled = true;
        return null; // Return null to fall back to other resolvers
    }
}

/// <summary>
/// AOT serializer that returns valid types but doesn't deserialize.
/// Used to test the type resolution AOT path.
/// </summary>
internal sealed class WorkingAotSerializer : IComponentSerializer
{
    public bool GetTypeCalled { get; private set; }

    public bool IsSerializable(Type type) => type == typeof(SerializablePosition);
    public bool IsSerializable(string typeName) => typeName.Contains("SerializablePosition");

    public object? Deserialize(string typeName, System.Text.Json.JsonElement json)
    {
        return null; // Fall back to reflection for actual deserialization
    }

    public System.Text.Json.JsonElement? Serialize(Type type, object value) => null;

    public Type? GetType(string typeName)
    {
        GetTypeCalled = true;
        if (typeName.Contains("SerializablePosition"))
        {
            return typeof(SerializablePosition);
        }
        return null;
    }
}

/// <summary>
/// AOT serializer that actually deserializes components.
/// Used to test the full AOT deserialization path.
/// </summary>
internal sealed class DeserializingAotSerializer : IComponentSerializer
{
    public bool GetTypeCalled { get; private set; }
    public bool DeserializeCalled { get; private set; }

    public bool IsSerializable(Type type) => type == typeof(SerializablePosition);
    public bool IsSerializable(string typeName) => typeName.Contains("SerializablePosition");

    public object? Deserialize(string typeName, System.Text.Json.JsonElement json)
    {
        DeserializeCalled = true;
        if (typeName.Contains("SerializablePosition"))
        {
            var x = json.TryGetProperty("x", out var xProp) ? xProp.GetSingle() : 0f;
            var y = json.TryGetProperty("y", out var yProp) ? yProp.GetSingle() : 0f;
            return new SerializablePosition { X = x, Y = y };
        }
        return null;
    }

    public System.Text.Json.JsonElement? Serialize(Type type, object value) => null;

    public Type? GetType(string typeName)
    {
        GetTypeCalled = true;
        if (typeName.Contains("SerializablePosition"))
        {
            return typeof(SerializablePosition);
        }
        return null;
    }
}
