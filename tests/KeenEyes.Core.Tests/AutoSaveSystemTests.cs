using KeenEyes.Serialization;
using KeenEyes.Systems;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the AutoSaveSystem.
/// </summary>
public class AutoSaveSystemTests : IDisposable
{
    private readonly string testSaveDirectory;
    private readonly TestComponentSerializer serializer;

    public AutoSaveSystemTests()
    {
        testSaveDirectory = Path.Combine(Path.GetTempPath(), $"keen_eye_autosave_tests_{Guid.NewGuid():N}");
        serializer = TestSerializerFactory.CreateForSerializationTests();
    }

    public void Dispose()
    {
        if (Directory.Exists(testSaveDirectory))
        {
            Directory.Delete(testSaveDirectory, recursive: true);
        }
    }

    #region Configuration Tests

    [Fact]
    public void DefaultConfig_HasReasonableDefaults()
    {
        var config = AutoSaveConfig.Default;

        Assert.Equal(300f, config.AutoSaveIntervalSeconds);
        Assert.Equal(1000, config.ChangeThreshold);
        Assert.True(config.UseDeltaSaves);
        Assert.Equal(10, config.MaxDeltasBeforeBaseline);
        Assert.Equal("autosave", config.BaseSlotName);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void FrequentConfig_HasShorterIntervals()
    {
        var config = AutoSaveConfig.Frequent;

        Assert.Equal(60f, config.AutoSaveIntervalSeconds);
        Assert.Equal(500, config.ChangeThreshold);
        Assert.True(config.UseDeltaSaves);
    }

    [Fact]
    public void InfrequentConfig_DisablesDeltaSaves()
    {
        var config = AutoSaveConfig.Infrequent;

        Assert.Equal(600f, config.AutoSaveIntervalSeconds);
        Assert.False(config.UseDeltaSaves);
    }

    [Fact]
    public void Config_BaselineSlotName_ReturnsCorrectFormat()
    {
        var config = new AutoSaveConfig { BaseSlotName = "mysave" };

        Assert.Equal("mysave_baseline", config.BaselineSlotName);
    }

    [Fact]
    public void Config_GetDeltaSlotName_ReturnsCorrectFormat()
    {
        var config = new AutoSaveConfig { BaseSlotName = "mysave" };

        Assert.Equal("mysave_delta_1", config.GetDeltaSlotName(1));
        Assert.Equal("mysave_delta_5", config.GetDeltaSlotName(5));
    }

    #endregion

    #region System Initialization Tests

    [Fact]
    public void Constructor_WithSerializer_CreatesSystem()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer);

        Assert.NotNull(system);
        Assert.Equal(0, system.TimeSinceLastSave);
        Assert.Equal(0, system.CurrentDeltaSequence);
        Assert.False(system.HasBaseline);
    }

    [Fact]
    public void Constructor_WithConfig_UsesConfig()
    {
        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 120f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);

        Assert.Equal(120f, system.Config.AutoSaveIntervalSeconds);
    }

    [Fact]
    public void Config_CanBeChanged()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer);
        var newConfig = AutoSaveConfig.Frequent;

        system.Config = newConfig;

        Assert.Equal(60f, system.Config.AutoSaveIntervalSeconds);
    }

    #endregion

    #region Auto-Save Trigger Tests

    [Fact]
    public void Update_BeforeInterval_DoesNotSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Update for less than the interval
        world.Update(5f);

        Assert.False(world.SaveSlotExists(config.BaselineSlotName));
    }

    [Fact]
    public void Update_AfterInterval_TriggersSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Update past the interval
        world.Update(15f);

        Assert.True(world.SaveSlotExists(config.BaselineSlotName));
    }

    [Fact]
    public void Update_WhenDisabled_DoesNotSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f, Enabled = false };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Update past the interval
        world.Update(15f);

        Assert.False(world.SaveSlotExists(config.BaselineSlotName));
    }

    [Fact]
    public void Update_AfterSave_ResetsTimer()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        world.Update(15f); // First save
        Assert.True(system.TimeSinceLastSave < 1f); // Timer reset
    }

    #endregion

    #region Manual Save Tests

    [Fact]
    public void SaveNow_CreatesBaseline()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1000f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        var info = system.SaveNow();

        Assert.NotNull(info);
        Assert.True(world.SaveSlotExists(config.BaselineSlotName));
        Assert.True(system.HasBaseline);
    }

    [Fact]
    public void CreateNewBaseline_ResetsSequence()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Create multiple saves
        world.Update(2f); // First baseline
        world.Update(2f); // Delta 1

        // Force new baseline
        system.CreateNewBaseline();

        Assert.Equal(0, system.CurrentDeltaSequence);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1000f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        system.SaveNow();
        system.Reset();

        Assert.Equal(0, system.TimeSinceLastSave);
        Assert.Equal(0, system.CurrentDeltaSequence);
        Assert.False(system.HasBaseline);
    }

    #endregion

    #region Delta Save Tests

    [Fact]
    public void DeltaSave_AfterBaseline_IncrementSequence()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var entity = world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1f, UseDeltaSaves = true };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        world.Update(2f); // Baseline
        Assert.Equal(0, system.CurrentDeltaSequence);

        // Make a change before delta 1
        world.Set(entity, new SerializablePosition { X = 10, Y = 20 });
        world.Update(2f); // Delta 1
        Assert.Equal(1, system.CurrentDeltaSequence);

        // Make another change before delta 2
        world.Set(entity, new SerializablePosition { X = 30, Y = 40 });
        world.Update(2f); // Delta 2
        Assert.Equal(2, system.CurrentDeltaSequence);
    }

    [Fact]
    public void DeltaSave_AtMaxDeltas_CreatesNewBaseline()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var entity = world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig
        {
            AutoSaveIntervalSeconds = 1f,
            UseDeltaSaves = true,
            MaxDeltasBeforeBaseline = 3
        };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Create baseline + 3 deltas (with changes for each)
        for (int i = 0; i < 5; i++)
        {
            // Make a change before each save
            world.Set(entity, new SerializablePosition { X = i * 10, Y = i * 10 });
            world.Update(2f);
        }

        // After max deltas, should have reset to new baseline
        Assert.True(system.CurrentDeltaSequence <= config.MaxDeltasBeforeBaseline);
    }

    [Fact]
    public void FullSaveMode_DoesNotCreateDeltas()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig
        {
            AutoSaveIntervalSeconds = 1f,
            UseDeltaSaves = false
        };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        world.Update(2f);
        world.Update(2f);
        world.Update(2f);

        // Should always be at sequence 0 (only baselines)
        Assert.Equal(0, system.CurrentDeltaSequence);
    }

    [Fact]
    public void DeltaSave_ProducesSmallerFileThanFullSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        // Create many entities to ensure measurable file sizes
        for (int i = 0; i < 100; i++)
        {
            world.Spawn($"Entity{i}")
                .With(new SerializablePosition { X = i, Y = i * 2f })
                .With(new SerializableHealth { Current = 100, Max = 100 })
                .Build();
        }

        var config = new AutoSaveConfig
        {
            AutoSaveIntervalSeconds = 1f,
            UseDeltaSaves = true
        };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Trigger baseline save
        world.Update(2f);
        var baselineSlotPath = world.GetSaveSlotPath(config.BaselineSlotName);
        var baselineSize = new FileInfo(baselineSlotPath).Length;

        // Make a small change (modify just one entity)
        var firstEntity = world.GetEntityByName("Entity0");
        world.Set(firstEntity, new SerializablePosition { X = 999, Y = 999 });

        // Trigger delta save
        world.Update(2f);
        var deltaSlotPath = world.GetSaveSlotPath(config.GetDeltaSlotName(1));
        var deltaSize = new FileInfo(deltaSlotPath).Length;

        // Delta should be significantly smaller than baseline
        // (since we only changed 1 of 100 entities)
        Assert.True(deltaSize < baselineSize,
            $"Delta save ({deltaSize} bytes) should be smaller than baseline ({baselineSize} bytes)");

        // Delta should be much smaller - at least 50% reduction
        Assert.True(deltaSize < baselineSize * 0.5,
            $"Delta save ({deltaSize} bytes) should be less than 50% of baseline ({baselineSize} bytes)");
    }

    [Fact]
    public void DeltaSave_WithNoChanges_DoesNotCreateDeltaFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition { X = 10, Y = 20 }).Build();

        var config = new AutoSaveConfig
        {
            AutoSaveIntervalSeconds = 1f,
            UseDeltaSaves = true
        };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Trigger baseline save
        world.Update(2f);
        Assert.True(world.SaveSlotExists(config.BaselineSlotName));

        // Don't make any changes - trigger delta save
        world.Update(2f);

        // Since there are no changes, delta should not be created
        // and sequence should not increment
        Assert.Equal(0, system.CurrentDeltaSequence);
        Assert.False(world.SaveSlotExists(config.GetDeltaSlotName(1)));
    }

    #endregion

    #region Event Tests

    [Fact]
    public void OnAutoSave_IsRaisedOnSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);

        SaveSlotInfo? capturedInfo = null;
        system.OnAutoSave += info => capturedInfo = info;

        world.AddSystem(system);
        world.Update(2f);

        Assert.NotNull(capturedInfo);
        Assert.Contains("autosave", capturedInfo!.SlotName);
    }

    #endregion

    #region DeltaSnapshot Tests

    [Fact]
    public void DeltaSnapshot_EmptyDelta_HasNoChanges()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline"
        };

        Assert.True(delta.IsEmpty);
        Assert.Equal(0, delta.ChangeCount);
    }

    [Fact]
    public void DeltaSnapshot_WithCreatedEntities_IsNotEmpty()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline",
            CreatedEntities = [new SerializedEntity { Id = 1, Name = "Test", Components = [] }]
        };

        Assert.False(delta.IsEmpty);
        Assert.Equal(1, delta.ChangeCount);
    }

    [Fact]
    public void DeltaSnapshot_WithDestroyedEntities_IsNotEmpty()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline",
            DestroyedEntityIds = [1, 2, 3]
        };

        Assert.False(delta.IsEmpty);
        Assert.Equal(3, delta.ChangeCount);
    }

    [Fact]
    public void DeltaSnapshot_WithModifiedEntities_IsNotEmpty()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline",
            ModifiedEntities = [new EntityDelta { EntityId = 1 }]
        };

        Assert.False(delta.IsEmpty);
        Assert.Equal(1, delta.ChangeCount);
    }

    [Fact]
    public void EntityDelta_EmptyDelta_HasNoChanges()
    {
        var delta = new EntityDelta { EntityId = 1 };

        Assert.True(delta.IsEmpty);
    }

    [Fact]
    public void EntityDelta_WithAddedComponents_IsNotEmpty()
    {
        var delta = new EntityDelta
        {
            EntityId = 1,
            AddedComponents = [new SerializedComponent { TypeName = "Position" }]
        };

        Assert.False(delta.IsEmpty);
    }

    #endregion

    #region DeltaDiffer Tests

    [Fact]
    public void DeltaDiffer_NoChanges_ReturnsEmptyDelta()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create an entity
        world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();

        // Create baseline snapshot
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Create delta (no changes)
        var delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);

        Assert.True(delta.IsEmpty);
        Assert.Empty(delta.CreatedEntities);
        Assert.Empty(delta.DestroyedEntityIds);
        Assert.Empty(delta.ModifiedEntities);
    }

    [Fact]
    public void DeltaDiffer_NewEntity_IncludedInCreatedEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create initial entity
        world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();

        // Create baseline snapshot
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Add a new entity
        world.Spawn().With(new SerializablePosition { X = 3, Y = 4 }).Build();

        // Create delta
        var delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);

        Assert.False(delta.IsEmpty);
        Assert.Single(delta.CreatedEntities);
        Assert.Empty(delta.DestroyedEntityIds);
    }

    [Fact]
    public void DeltaDiffer_DestroyedEntity_IncludedInDestroyedEntityIds()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create two entities
        var entity1 = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();
        world.Spawn().With(new SerializablePosition { X = 3, Y = 4 }).Build();

        // Create baseline snapshot
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Destroy one entity
        world.Despawn(entity1);

        // Create delta
        var delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);

        Assert.False(delta.IsEmpty);
        Assert.Empty(delta.CreatedEntities);
        Assert.Single(delta.DestroyedEntityIds);
        Assert.Equal(entity1.Id, delta.DestroyedEntityIds[0]);
    }

    [Fact]
    public void DeltaDiffer_ModifiedComponent_IncludedInModifiedEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create entity
        var entity = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();

        // Create baseline snapshot
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Modify the component
        world.Set(entity, new SerializablePosition { X = 100, Y = 200 });

        // Create delta
        var delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);

        Assert.False(delta.IsEmpty);
        Assert.Empty(delta.CreatedEntities);
        Assert.Empty(delta.DestroyedEntityIds);
        Assert.Single(delta.ModifiedEntities);
        Assert.Equal(entity.Id, delta.ModifiedEntities[0].EntityId);
        Assert.Single(delta.ModifiedEntities[0].ModifiedComponents);
    }

    [Fact]
    public void DeltaDiffer_AddedComponent_IncludedInEntityDelta()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();
        world.Components.Register<SerializableVelocity>();

        // Create entity with only position
        var entity = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();

        // Create baseline snapshot
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Add velocity component
        world.Add(entity, new SerializableVelocity { X = 5, Y = 10 });

        // Create delta
        var delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);

        Assert.False(delta.IsEmpty);
        Assert.Single(delta.ModifiedEntities);
        Assert.Single(delta.ModifiedEntities[0].AddedComponents);
    }

    [Fact]
    public void DeltaDiffer_RemovedComponent_IncludedInEntityDelta()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();
        world.Components.Register<SerializableVelocity>();

        // Create entity with both components
        var entity = world.Spawn()
            .With(new SerializablePosition { X = 1, Y = 2 })
            .With(new SerializableVelocity { X = 5, Y = 10 })
            .Build();

        // Create baseline snapshot
        var baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Remove velocity component
        world.Remove<SerializableVelocity>(entity);

        // Create delta
        var delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);

        Assert.False(delta.IsEmpty);
        Assert.Single(delta.ModifiedEntities);
        Assert.Single(delta.ModifiedEntities[0].RemovedComponentTypes);
    }

    #endregion

    #region DeltaRestorer Tests

    [Fact]
    public void DeltaRestorer_ApplyDelta_CreatesNewEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create initial state
        var entity = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();
        var entityMap = new Dictionary<int, Entity> { [entity.Id] = entity };

        // Create delta with a new entity
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            CreatedEntities = [
                new SerializedEntity
                {
                    Id = 999,
                    Name = "NewEntity",
                    Components = []
                }
            ]
        };

        // Apply delta
        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify new entity was created
        Assert.True(newEntityMap.ContainsKey(999));
        Assert.True(world.IsAlive(newEntityMap[999]));
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_DestroysEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create initial state with two entities
        var entity1 = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();
        var entity2 = world.Spawn().With(new SerializablePosition { X = 3, Y = 4 }).Build();
        var entityMap = new Dictionary<int, Entity>
        {
            [entity1.Id] = entity1,
            [entity2.Id] = entity2
        };

        // Create delta that destroys entity1
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            DestroyedEntityIds = [entity1.Id]
        };

        // Apply delta
        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify entity1 was destroyed
        Assert.False(newEntityMap.ContainsKey(entity1.Id));
        Assert.False(world.IsAlive(entity1));
        Assert.True(world.IsAlive(entity2));
    }

    [Fact]
    public void DeltaRestorer_ApplyEmptyDelta_NoChanges()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create initial state
        var entity = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();
        var entityMap = new Dictionary<int, Entity> { [entity.Id] = entity };
        var initialCount = 1;

        // Create empty delta
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1
        };

        // Apply delta
        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify nothing changed
        Assert.Equal(initialCount, newEntityMap.Count);
        Assert.True(world.IsAlive(entity));
        Assert.Equal(1f, world.Get<SerializablePosition>(entity).X);
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithEntityNameChange_UpdatesName()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create initial state with named entity
        var entity = world.Spawn("OriginalName").With(new SerializablePosition { X = 1, Y = 2 }).Build();
        var entityMap = new Dictionary<int, Entity> { [entity.Id] = entity };

        // Create delta that changes the entity's name
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = entity.Id,
                    NewName = "UpdatedName"
                }
            ]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify name was updated
        Assert.Equal("UpdatedName", world.GetName(entity));
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithParentChange_UpdatesParent()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create entities
        var parent1 = world.Spawn("Parent1").With(new SerializablePosition()).Build();
        var parent2 = world.Spawn("Parent2").With(new SerializablePosition()).Build();
        var child = world.Spawn("Child").With(new SerializablePosition()).Build();
        world.SetParent(child, parent1);

        var entityMap = new Dictionary<int, Entity>
        {
            [parent1.Id] = parent1,
            [parent2.Id] = parent2,
            [child.Id] = child
        };

        // Create delta that changes parent
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = child.Id,
                    NewParentId = parent2.Id
                }
            ]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify parent was changed
        Assert.Equal(parent2, world.GetParent(child));
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithParentRemoved_ClearsParent()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create parent and child
        var parent = world.Spawn("Parent").With(new SerializablePosition()).Build();
        var child = world.Spawn("Child").With(new SerializablePosition()).Build();
        world.SetParent(child, parent);

        var entityMap = new Dictionary<int, Entity>
        {
            [parent.Id] = parent,
            [child.Id] = child
        };

        // Create delta that removes parent
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = child.Id,
                    ParentRemoved = true
                }
            ]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify parent was removed
        Assert.False(world.GetParent(child).IsValid);
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithAddedComponents_AddsComponents()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();
        world.Components.Register<SerializableVelocity>();

        // Create entity with only position
        var entity = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();
        var entityMap = new Dictionary<int, Entity> { [entity.Id] = entity };

        // Create delta that adds velocity component
        var velocityJson = serializer.Serialize(
            typeof(SerializableVelocity),
            new SerializableVelocity { X = 5, Y = 10 });

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = entity.Id,
                    AddedComponents = [
                        new SerializedComponent
                        {
                            TypeName = typeof(SerializableVelocity).AssemblyQualifiedName!,
                            Data = velocityJson,
                            IsTag = false
                        }
                    ]
                }
            ]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify component was added (entity ID is preserved across archetype migration)
        Assert.True(world.Has<SerializableVelocity>(entity));
        ref var velocity = ref world.Get<SerializableVelocity>(entity);
        Assert.Equal(5f, velocity.X);
        Assert.Equal(10f, velocity.Y);
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithRemovedComponents_RemovesComponents()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();
        world.Components.Register<SerializableVelocity>();

        // Create entity with both components
        var entity = world.Spawn()
            .With(new SerializablePosition { X = 1, Y = 2 })
            .With(new SerializableVelocity { X = 5, Y = 10 })
            .Build();
        var entityMap = new Dictionary<int, Entity> { [entity.Id] = entity };

        // Create delta that removes velocity
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = entity.Id,
                    RemovedComponentTypes = [typeof(SerializableVelocity).AssemblyQualifiedName!]
                }
            ]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify component was removed
        Assert.False(world.Has<SerializableVelocity>(entity));
        Assert.True(world.Has<SerializablePosition>(entity));
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithModifiedComponents_UpdatesComponents()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create entity with initial position
        var entity = world.Spawn().With(new SerializablePosition { X = 1, Y = 2 }).Build();
        var entityMap = new Dictionary<int, Entity> { [entity.Id] = entity };

        // Create delta that modifies position
        var newPositionJson = serializer.Serialize(
            typeof(SerializablePosition),
            new SerializablePosition { X = 100, Y = 200 });

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = entity.Id,
                    ModifiedComponents = [
                        new SerializedComponent
                        {
                            TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                            Data = newPositionJson,
                            IsTag = false
                        }
                    ]
                }
            ]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify component was updated
        ref var position = ref world.Get<SerializablePosition>(entity);
        Assert.Equal(100f, position.X);
        Assert.Equal(200f, position.Y);
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithModifiedSingletons_UpdatesSingletons()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        // Set initial singleton value
        world.SetSingleton(new SerializableGameTime { TotalTime = 10f, DeltaTime = 0.016f });

        // Create delta that modifies singleton
        var newTimeJson = serializer.Serialize(
            typeof(SerializableGameTime),
            new SerializableGameTime { TotalTime = 20f, DeltaTime = 0.032f });

        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedSingletons = [
                new SerializedSingleton
                {
                    TypeName = typeof(SerializableGameTime).AssemblyQualifiedName!,
                    Data = newTimeJson!.Value
                }
            ]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, []);

        // Verify singleton was updated
        Assert.True(world.HasSingleton<SerializableGameTime>());
        ref var time = ref world.GetSingleton<SerializableGameTime>();
        Assert.Equal(20f, time.TotalTime);
        Assert.Equal(0.032f, time.DeltaTime);
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithRemovedSingletons_RemovesSingletons()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        // Set initial singleton value
        world.SetSingleton(new SerializableGameTime { TotalTime = 10f, DeltaTime = 0.016f });

        // Create delta that removes singleton
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            RemovedSingletonTypes = [typeof(SerializableGameTime).AssemblyQualifiedName!]
        };

        // Apply delta
        DeltaRestorer.ApplyDelta(world, delta, serializer, []);

        // Verify singleton was removed
        Assert.False(world.HasSingleton<SerializableGameTime>());
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithCreatedEntityHavingParent_RestoresHierarchy()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create parent entity
        var parent = world.Spawn("Parent").With(new SerializablePosition()).Build();
        var entityMap = new Dictionary<int, Entity> { [parent.Id] = parent };

        // Create delta with child entity that has parent
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            CreatedEntities = [
                new SerializedEntity
                {
                    Id = 999,
                    Name = "Child",
                    ParentId = parent.Id,
                    Components = []
                }
            ]
        };

        // Apply delta
        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify child was created with correct parent
        Assert.True(newEntityMap.ContainsKey(999));
        var child = newEntityMap[999];
        Assert.Equal(parent, world.GetParent(child));
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithNonExistentModifiedEntity_SkipsModification()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create entity map without the target entity
        var entityMap = new Dictionary<int, Entity>();

        // Create delta that tries to modify non-existent entity
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = 999,
                    NewName = "ShouldNotExist"
                }
            ]
        };

        // Apply delta (should not throw)
        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify no entity was created
        Assert.Empty(newEntityMap);
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithDeadModifiedEntity_SkipsModification()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        // Create entity and despawn it
        var entity = world.Spawn().With(new SerializablePosition()).Build();
        var entityId = entity.Id;
        world.Despawn(entity);

        var entityMap = new Dictionary<int, Entity> { [entityId] = entity };

        // Create delta that tries to modify dead entity
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = entityId,
                    NewName = "ShouldNotApply"
                }
            ]
        };

        // Apply delta (should not throw)
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify entity is still dead
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithUnknownComponentType_SkipsComponent()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Components.Register<SerializablePosition>();

        var entity = world.Spawn().With(new SerializablePosition()).Build();
        var entityMap = new Dictionary<int, Entity> { [entity.Id] = entity };

        // Create delta with unknown component type
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            ModifiedEntities = [
                new EntityDelta
                {
                    EntityId = entity.Id,
                    AddedComponents = [
                        new SerializedComponent
                        {
                            TypeName = "NonExistent.Component.Type",
                            Data = null,
                            IsTag = false
                        }
                    ]
                }
            ]
        };

        // Apply delta (should not throw)
        DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        // Verify entity still exists with original component
        Assert.True(world.IsAlive(entity));
        Assert.True(world.Has<SerializablePosition>(entity));
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithDestroyNonExistentEntity_DoesNotThrow()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        var entityMap = new Dictionary<int, Entity>();

        // Create delta that tries to destroy non-existent entity
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "baseline",
            SequenceNumber = 1,
            DestroyedEntityIds = [999]
        };

        // Apply delta (should not throw)
        var newEntityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);

        Assert.Empty(newEntityMap);
    }

    [Fact]
    public void DeltaRestorer_ApplyDelta_WithNullArguments_ThrowsArgumentNullException()
    {
        using var world = new World();
        var delta = new DeltaSnapshot { BaselineSlotName = "test", SequenceNumber = 1 };
        var entityMap = new Dictionary<int, Entity>();

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(null!, delta, serializer, entityMap));

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(world, null!, serializer, entityMap));

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(world, delta, null!, entityMap));

        Assert.Throws<ArgumentNullException>(() =>
            DeltaRestorer.ApplyDelta(world, delta, serializer, null!));
    }

    #endregion
}
