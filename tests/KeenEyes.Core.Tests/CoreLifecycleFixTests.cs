namespace KeenEyes.Tests;

/// <summary>
/// Regression tests for the core entity-lifecycle bug cluster: stale tags and
/// wiped tracking config after <see cref="World.Clear"/>, validator rollback on a
/// failed <c>Build()</c>, recycled entities leaking through
/// <see cref="World.GetDirtyEntities{T}"/>, shared-state corruption when branching a
/// <see cref="QueryBuilder"/>, and re-entrant <c>Spawn()</c> during <c>Build()</c>.
/// </summary>
public class CoreLifecycleFixTests
{
    /// <summary>Marker message type for the message-subscription preservation test.</summary>
    private struct LifecycleTestMessage;

    #region World.Clear() — string tags (#1107)

    [Fact]
    public void Clear_ThenSpawnRecycledId_DoesNotInheritStaleStringTags()
    {
        using var world = new World();

        var original = world.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();
        world.AddTag(original, "Boss");

        world.Clear();

        // After Clear, entity IDs restart from zero, so this entity reuses the ID that
        // "Boss" was applied to. It must not inherit the pre-Clear tag.
        var recycled = world.Spawn().With(new TestPosition { X = 3, Y = 4 }).Build();

        Assert.Equal(original.Id, recycled.Id);
        Assert.False(world.HasTag(recycled, "Boss"));
        Assert.Empty(world.GetTags(recycled));
    }

    #endregion

    #region World.Clear() — auto-tracking config and message subscriptions (#1110)

    [Fact]
    public void Clear_PreservesAutoTrackingConfiguration()
    {
        using var world = new World();

        world.EnableAutoTracking<TestPosition>();
        Assert.True(world.IsAutoTrackingEnabled<TestPosition>());

        world.Clear();

        // Clear is a state-only snapshot-restore entry point; the per-type tracking
        // opt-in registered by a system must survive it.
        Assert.True(world.IsAutoTrackingEnabled<TestPosition>());
    }

    [Fact]
    public void Clear_PreservesMessageSubscriptions()
    {
        using var world = new World();

        using var subscription = world.Subscribe<LifecycleTestMessage>(_ => { });
        Assert.True(world.HasMessageSubscribers<LifecycleTestMessage>());

        world.Clear();

        // Handler subscriptions are wiring registered by systems, not per-entity state,
        // so they are preserved just like event subscriptions.
        Assert.True(world.HasMessageSubscribers<LifecycleTestMessage>());
    }

    #endregion

    #region Failed custom validator during Build() (#1111)

    [Fact]
    public void Build_WithFailingCustomValidator_DoesNotLeakEntityOrName()
    {
        using var world = new World();

        world.ValidationManager.RegisterValidator<TestHealth>(
            (_, _, health) => health.Current >= 0);

        Assert.Throws<ComponentValidationException>(() =>
            world.Spawn("Boss")
                .With(new TestHealth { Current = -5, Max = 10 })
                .Build());

        // The rejected build must roll back completely: no live entity, no reserved name.
        Assert.Equal(0, world.EntityCount);
        Assert.False(world.GetEntityByName("Boss").IsValid);
    }

    [Fact]
    public void Build_WithFailingCustomValidator_ReleasesNameForReuse()
    {
        using var world = new World();

        world.ValidationManager.RegisterValidator<TestHealth>(
            (_, _, health) => health.Current >= 0);

        Assert.Throws<ComponentValidationException>(() =>
            world.Spawn("Boss")
                .With(new TestHealth { Current = -5, Max = 10 })
                .Build());

        // Because the failed build released the name, a subsequent valid build can claim it.
        var boss = world.Spawn("Boss")
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        Assert.True(boss.IsValid);
        Assert.Equal(boss, world.GetEntityByName("Boss"));
    }

    #endregion

    #region GetDirtyEntities recycled-ID leak (#1112)

    [Fact]
    public void GetDirtyEntities_WithRecycledIdDuringEnumeration_ExcludesNewEntity()
    {
        using var world = new World();

        var marked = world.Spawn().With(new TestPosition { X = 1, Y = 1 }).Build();
        world.MarkDirty<TestPosition>(marked);

        // Snapshot the dirty set (captures the marked ID + version) before mutating.
        var dirty = world.GetDirtyEntities<TestPosition>();

        // Despawn the marked entity and recycle its ID onto a brand-new entity that was
        // never marked dirty. This interleaves recycling with the lazy enumeration.
        world.Despawn(marked);
        var recycled = world.Spawn().With(new TestPosition { X = 2, Y = 2 }).Build();
        Assert.Equal(marked.Id, recycled.Id);

        var results = dirty.ToList();

        // The recycled entity was never dirtied and must not be reported.
        Assert.DoesNotContain(recycled, results);
        Assert.Empty(results);
    }

    #endregion

    #region QueryBuilder branch aliasing (#1114)

    [Fact]
    public void QueryBuilder_BranchingSharedBase_DoesNotLeakFiltersBetweenSiblings()
    {
        using var world = new World();

        // Entity A has all three components; entity B has only Position.
        var entityA = world.Spawn()
            .With(new TestPosition { X = 0, Y = 0 })
            .With(new TestVelocity { X = 1, Y = 0 })
            .With(new TestHealth { Current = 10, Max = 10 })
            .Build();
        var entityB = world.Spawn()
            .With(new TestPosition { X = 5, Y = 5 })
            .Build();

        var baseQuery = world.Query<TestPosition>();
        var withVelocity = baseQuery.With<TestVelocity>();
        var withoutHealth = baseQuery.Without<TestHealth>();

        // withVelocity requires Position + Velocity -> matches only A.
        // Without copy-on-write, Without<TestHealth> would leak in and exclude A (it has
        // Health), wrongly returning 0.
        var withVelocityResults = withVelocity.ToList();
        Assert.Single(withVelocityResults);
        Assert.Equal(entityA, withVelocityResults[0]);

        // withoutHealth requires Position and excludes Health -> matches only B.
        // Without copy-on-write, With<TestVelocity> would leak in and exclude B (no
        // Velocity), wrongly returning 0.
        var withoutHealthResults = withoutHealth.ToList();
        Assert.Single(withoutHealthResults);
        Assert.Equal(entityB, withoutHealthResults[0]);
    }

    #endregion

    #region Re-entrant Spawn during Build() (#1148)

    [Fact]
    public void Build_WithReentrantSpawnInLifecycleCallback_BuildsBothEntitiesCorrectly()
    {
        using var world = new World();

        Entity nested = default;
        var reentered = false;

        // A ComponentAdded callback fires synchronously while the outer entity's
        // component list is being enumerated. Re-entering Spawn() here previously
        // corrupted the shared thread-local builder and threw "Collection was modified".
        using var subscription = world.OnComponentAdded<TestPosition>((_, _) =>
        {
            if (!reentered)
            {
                reentered = true;
                nested = world.Spawn()
                    .With(new TestVelocity { X = 9, Y = 9 })
                    .Build();
            }
        });

        var outer = world.Spawn()
            .With(new TestPosition { X = 1, Y = 2 })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        Assert.True(reentered);

        // Outer entity built with all its components intact despite the nested Spawn.
        Assert.True(outer.IsValid);
        Assert.True(world.Has<TestPosition>(outer));
        Assert.True(world.Has<TestHealth>(outer));
        ref readonly var position = ref world.GetReadonly<TestPosition>(outer);
        Assert.Equal(1f, position.X);
        Assert.Equal(2f, position.Y);

        // Nested entity built correctly and is distinct from the outer entity.
        Assert.True(nested.IsValid);
        Assert.NotEqual(outer.Id, nested.Id);
        Assert.True(world.Has<TestVelocity>(nested));
        Assert.False(world.Has<TestPosition>(nested));
    }

    #endregion
}
