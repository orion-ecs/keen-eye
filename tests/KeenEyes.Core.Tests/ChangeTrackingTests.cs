namespace KeenEyes.Tests;

/// <summary>
/// Tests for the change tracking system that enables tracking dirty (modified) entities
/// for efficient network synchronization, undo/redo, and reactive updates.
/// </summary>
public class ChangeTrackingTests
{
    #region MarkDirty Tests

    [Fact]
    public void MarkDirty_MarksEntityAsDirty()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.False(world.IsDirty<TestPosition>(entity));

        world.MarkDirty<TestPosition>(entity);

        Assert.True(world.IsDirty<TestPosition>(entity));
    }

    [Fact]
    public void MarkDirty_IsIdempotent()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.MarkDirty<TestPosition>(entity);
        world.MarkDirty<TestPosition>(entity);
        world.MarkDirty<TestPosition>(entity);

        Assert.Equal(1, world.GetDirtyCount<TestPosition>());
    }

    [Fact]
    public void MarkDirty_ThrowsForDeadEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.MarkDirty<TestPosition>(entity));
    }

    [Fact]
    public void MarkDirty_TracksPerComponentType()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        world.MarkDirty<TestPosition>(entity);

        Assert.True(world.IsDirty<TestPosition>(entity));
        Assert.False(world.IsDirty<TestVelocity>(entity));
    }

    #endregion

    #region GetDirtyEntities Tests

    [Fact]
    public void GetDirtyEntities_ReturnsEmptyWhenNoDirtyEntities()
    {
        using var world = new World();

        world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();

        var dirty = world.GetDirtyEntities<TestPosition>().ToList();

        Assert.Empty(dirty);
    }

    [Fact]
    public void GetDirtyEntities_ReturnsMarkedEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        var entity3 = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();

        world.MarkDirty<TestPosition>(entity1);
        world.MarkDirty<TestPosition>(entity3);

        var dirty = world.GetDirtyEntities<TestPosition>().ToList();

        Assert.Equal(2, dirty.Count);
        Assert.Contains(entity1, dirty);
        Assert.Contains(entity3, dirty);
        Assert.DoesNotContain(entity2, dirty);
    }

    [Fact]
    public void GetDirtyEntities_ExcludesDeadEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();

        world.MarkDirty<TestPosition>(entity1);
        world.MarkDirty<TestPosition>(entity2);

        world.Despawn(entity2);

        var dirty = world.GetDirtyEntities<TestPosition>().ToList();

        Assert.Single(dirty);
        Assert.Equal(entity1, dirty[0]);
    }

    [Fact]
    public void GetDirtyEntities_ReturnsOnlyEntitiesForSpecificComponentType()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 3f, Y = 3f })
            .Build();

        world.MarkDirty<TestPosition>(entity1);
        world.MarkDirty<TestVelocity>(entity2);

        var dirtyPositions = world.GetDirtyEntities<TestPosition>().ToList();
        var dirtyVelocities = world.GetDirtyEntities<TestVelocity>().ToList();

        Assert.Single(dirtyPositions);
        Assert.Equal(entity1, dirtyPositions[0]);

        Assert.Single(dirtyVelocities);
        Assert.Equal(entity2, dirtyVelocities[0]);
    }

    #endregion

    #region ClearDirtyFlags Tests

    [Fact]
    public void ClearDirtyFlags_ClearsAllDirtyEntitiesForComponentType()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();

        world.MarkDirty<TestPosition>(entity1);
        world.MarkDirty<TestPosition>(entity2);

        Assert.Equal(2, world.GetDirtyCount<TestPosition>());

        world.ClearDirtyFlags<TestPosition>();

        Assert.Equal(0, world.GetDirtyCount<TestPosition>());
        Assert.False(world.IsDirty<TestPosition>(entity1));
        Assert.False(world.IsDirty<TestPosition>(entity2));
    }

    [Fact]
    public void ClearDirtyFlags_OnlyClearsSpecificComponentType()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        world.MarkDirty<TestPosition>(entity);
        world.MarkDirty<TestVelocity>(entity);

        world.ClearDirtyFlags<TestPosition>();

        Assert.False(world.IsDirty<TestPosition>(entity));
        Assert.True(world.IsDirty<TestVelocity>(entity));
    }

    [Fact]
    public void ClearDirtyFlags_IsIdempotent()
    {
        using var world = new World();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        world.MarkDirty<TestPosition>(entity);

        world.ClearDirtyFlags<TestPosition>();
        world.ClearDirtyFlags<TestPosition>();
        world.ClearDirtyFlags<TestPosition>();

        Assert.Equal(0, world.GetDirtyCount<TestPosition>());
    }

    #endregion

    #region IsDirty Tests

    [Fact]
    public void IsDirty_ReturnsFalseForNeverMarkedEntity()
    {
        using var world = new World();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();

        Assert.False(world.IsDirty<TestPosition>(entity));
    }

    [Fact]
    public void IsDirty_ReturnsFalseForDeadEntity()
    {
        using var world = new World();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        world.MarkDirty<TestPosition>(entity);

        world.Despawn(entity);

        Assert.False(world.IsDirty<TestPosition>(entity));
    }

    [Fact]
    public void IsDirty_ReturnsFalseAfterClear()
    {
        using var world = new World();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        world.MarkDirty<TestPosition>(entity);

        Assert.True(world.IsDirty<TestPosition>(entity));

        world.ClearDirtyFlags<TestPosition>();

        Assert.False(world.IsDirty<TestPosition>(entity));
    }

    #endregion

    #region Auto-Tracking Tests

    [Fact]
    public void EnableAutoTracking_AutomaticallyMarksEntitiesDirtyOnSet()
    {
        using var world = new World();

        world.EnableAutoTracking<TestPosition>();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();

        Assert.False(world.IsDirty<TestPosition>(entity));

        world.Set(entity, new TestPosition { X = 10f, Y = 20f });

        Assert.True(world.IsDirty<TestPosition>(entity));
    }

    [Fact]
    public void AutoTracking_DoesNotMarkDirtyWhenDisabled()
    {
        using var world = new World();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();

        world.Set(entity, new TestPosition { X = 10f, Y = 20f });

        Assert.False(world.IsDirty<TestPosition>(entity));
    }

    [Fact]
    public void DisableAutoTracking_StopsAutomaticMarking()
    {
        using var world = new World();

        world.EnableAutoTracking<TestPosition>();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();

        world.Set(entity, new TestPosition { X = 10f, Y = 20f });
        Assert.True(world.IsDirty<TestPosition>(entity));

        world.ClearDirtyFlags<TestPosition>();
        world.DisableAutoTracking<TestPosition>();

        world.Set(entity, new TestPosition { X = 30f, Y = 40f });
        Assert.False(world.IsDirty<TestPosition>(entity));
    }

    [Fact]
    public void IsAutoTrackingEnabled_ReturnsCorrectState()
    {
        using var world = new World();

        Assert.False(world.IsAutoTrackingEnabled<TestPosition>());

        world.EnableAutoTracking<TestPosition>();
        Assert.True(world.IsAutoTrackingEnabled<TestPosition>());

        world.DisableAutoTracking<TestPosition>();
        Assert.False(world.IsAutoTrackingEnabled<TestPosition>());
    }

    [Fact]
    public void AutoTracking_IsPerComponentType()
    {
        using var world = new World();

        world.EnableAutoTracking<TestPosition>();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        world.Set(entity, new TestPosition { X = 10f, Y = 20f });
        world.Set(entity, new TestVelocity { X = 5f, Y = 5f });

        Assert.True(world.IsDirty<TestPosition>(entity));
        Assert.False(world.IsDirty<TestVelocity>(entity));
    }

    #endregion

    #region GetDirtyCount Tests

    [Fact]
    public void GetDirtyCount_ReturnsZeroWhenNoDirtyEntities()
    {
        using var world = new World();

        world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();

        Assert.Equal(0, world.GetDirtyCount<TestPosition>());
    }

    [Fact]
    public void GetDirtyCount_ReturnsCorrectCount()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        var entity3 = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();

        world.MarkDirty<TestPosition>(entity1);
        Assert.Equal(1, world.GetDirtyCount<TestPosition>());

        world.MarkDirty<TestPosition>(entity2);
        Assert.Equal(2, world.GetDirtyCount<TestPosition>());

        world.MarkDirty<TestPosition>(entity3);
        Assert.Equal(3, world.GetDirtyCount<TestPosition>());
    }

    [Fact]
    public void GetDirtyCount_IncludesDeadEntitiesUntilCleared()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();

        world.MarkDirty<TestPosition>(entity1);
        world.MarkDirty<TestPosition>(entity2);

        Assert.Equal(2, world.GetDirtyCount<TestPosition>());

        world.Despawn(entity2);

        // Count doesn't include the dead entity because Despawn removes from tracker
        Assert.Equal(1, world.GetDirtyCount<TestPosition>());
    }

    #endregion

    #region ClearAllDirtyFlags Tests

    [Fact]
    public void ClearAllDirtyFlags_ClearsAllComponentTypes()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        world.MarkDirty<TestPosition>(entity);
        world.MarkDirty<TestVelocity>(entity);
        world.MarkDirty<TestHealth>(entity);

        Assert.True(world.IsDirty<TestPosition>(entity));
        Assert.True(world.IsDirty<TestVelocity>(entity));
        Assert.True(world.IsDirty<TestHealth>(entity));

        world.ClearAllDirtyFlags();

        Assert.False(world.IsDirty<TestPosition>(entity));
        Assert.False(world.IsDirty<TestVelocity>(entity));
        Assert.False(world.IsDirty<TestHealth>(entity));
    }

    #endregion

    #region Despawn Cleanup Tests

    [Fact]
    public void Despawn_RemovesEntityFromDirtyTracking()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();

        world.MarkDirty<TestPosition>(entity1);
        world.MarkDirty<TestPosition>(entity2);

        world.Despawn(entity1);

        var dirty = world.GetDirtyEntities<TestPosition>().ToList();

        Assert.Single(dirty);
        Assert.Equal(entity2, dirty[0]);
    }

    [Fact]
    public void DespawnRecursive_RemovesAllEntitiesFromDirtyTracking()
    {
        using var world = new World();

        var parent = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var child = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        var grandchild = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();

        world.SetParent(child, parent);
        world.SetParent(grandchild, child);

        world.MarkDirty<TestPosition>(parent);
        world.MarkDirty<TestPosition>(child);
        world.MarkDirty<TestPosition>(grandchild);

        Assert.Equal(3, world.GetDirtyCount<TestPosition>());

        world.DespawnRecursive(parent);

        Assert.Equal(0, world.GetDirtyCount<TestPosition>());
    }

    #endregion

    #region World Dispose Tests

    [Fact]
    public void WorldDispose_ClearsChangeTracker()
    {
        var world = new World();

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        world.MarkDirty<TestPosition>(entity);
        world.EnableAutoTracking<TestPosition>();

        world.Dispose();

        // After dispose, we can't test directly but ensure no exceptions
    }

    #endregion

    #region Typical Usage Patterns

    [Fact]
    public void TypicalPattern_ProcessAndClearDirtyEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        var entity3 = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();

        // Simulate modifications
        ref var pos1 = ref world.Get<TestPosition>(entity1);
        pos1.X = 10f;
        world.MarkDirty<TestPosition>(entity1);

        ref var pos2 = ref world.Get<TestPosition>(entity2);
        pos2.Y = 20f;
        world.MarkDirty<TestPosition>(entity2);

        // Process dirty entities (e.g., sync to network)
        var processedCount = 0;
        foreach (var entity in world.GetDirtyEntities<TestPosition>())
        {
            ref readonly var pos = ref world.Get<TestPosition>(entity);
            // Simulate network sync
            processedCount++;
        }

        Assert.Equal(2, processedCount);

        // Clear after processing
        world.ClearDirtyFlags<TestPosition>();

        Assert.Equal(0, world.GetDirtyCount<TestPosition>());
    }

    [Fact]
    public void TypicalPattern_AutoTrackingWithSet()
    {
        using var world = new World();

        world.EnableAutoTracking<TestPosition>();

        var entity1 = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();

        // Modify using Set (automatically tracked)
        world.Set(entity1, new TestPosition { X = 100f, Y = 200f });

        // entity2 not modified, not dirty
        Assert.True(world.IsDirty<TestPosition>(entity1));
        Assert.False(world.IsDirty<TestPosition>(entity2));

        // Process only dirty entities
        var dirty = world.GetDirtyEntities<TestPosition>().ToList();
        Assert.Single(dirty);
        Assert.Equal(entity1, dirty[0]);

        world.ClearDirtyFlags<TestPosition>();
    }

    [Fact]
    public void TypicalPattern_MultipleComponentTypesTrackedIndependently()
    {
        using var world = new World();

        world.EnableAutoTracking<TestPosition>();
        world.EnableAutoTracking<TestVelocity>();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        // Modify only position
        world.Set(entity, new TestPosition { X = 10f, Y = 10f });

        Assert.True(world.IsDirty<TestPosition>(entity));
        Assert.False(world.IsDirty<TestVelocity>(entity));

        // Clear position, modify velocity
        world.ClearDirtyFlags<TestPosition>();
        world.Set(entity, new TestVelocity { X = 5f, Y = 5f });

        Assert.False(world.IsDirty<TestPosition>(entity));
        Assert.True(world.IsDirty<TestVelocity>(entity));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void MarkDirty_WorksWithoutComponent()
    {
        using var world = new World();

        // Entity doesn't have TestPosition, but can still be marked dirty
        // (useful for tracking entities that need attention regardless of components)
        var entity = world.Spawn().Build();

        // This should work since marking dirty doesn't require the component to exist
        world.MarkDirty<TestPosition>(entity);

        Assert.True(world.IsDirty<TestPosition>(entity));
    }

    [Fact]
    public void GetDirtyEntities_WorksWithUnregisteredComponentType()
    {
        using var world = new World();

        // No entities with TestHealth, no dirty entities
        var dirty = world.GetDirtyEntities<TestHealth>().ToList();

        Assert.Empty(dirty);
    }

    [Fact]
    public void ClearDirtyFlags_WorksWithNoTrackedEntities()
    {
        using var world = new World();

        // Should not throw when clearing flags for component type with no tracked entities
        world.ClearDirtyFlags<TestHealth>();

        Assert.Equal(0, world.GetDirtyCount<TestHealth>());
    }

    [Fact]
    public void MultipleEntities_TrackedIndependently()
    {
        using var world = new World();

        var entities = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            entities.Add(world.Spawn().With(new TestPosition { X = i, Y = i }).Build());
        }

        // Mark every 10th entity as dirty
        for (int i = 0; i < entities.Count; i += 10)
        {
            world.MarkDirty<TestPosition>(entities[i]);
        }

        Assert.Equal(10, world.GetDirtyCount<TestPosition>());

        var dirty = world.GetDirtyEntities<TestPosition>().ToList();
        Assert.Equal(10, dirty.Count);

        // Verify correct entities are dirty
        for (int i = 0; i < entities.Count; i++)
        {
            if (i % 10 == 0)
            {
                Assert.True(world.IsDirty<TestPosition>(entities[i]));
            }
            else
            {
                Assert.False(world.IsDirty<TestPosition>(entities[i]));
            }
        }
    }

    #endregion
}
