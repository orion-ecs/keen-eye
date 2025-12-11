namespace KeenEyes.Tests;

/// <summary>
/// Test component for rotation data.
/// </summary>
public struct TestRotation : IComponent
{
    public float Angle;
}

/// <summary>
/// Test tag component for marking entities as frozen.
/// </summary>
public struct FrozenTag : ITagComponent;

/// <summary>
/// Test tag component for marking entities as active.
/// </summary>
public struct ActiveTag : ITagComponent;

/// <summary>
/// Tests for QueryDescription matching logic.
/// </summary>
public class QueryDescriptionTests
{
    [Fact]
    public void QueryDescription_NewInstance_HasEmptyCollections()
    {
        var description = new QueryDescription();

        Assert.Empty(description.Read);
        Assert.Empty(description.Write);
        Assert.Empty(description.With);
        Assert.Empty(description.Without);
        Assert.Empty(description.AllRequired);
    }

    [Fact]
    public void Matches_ReturnsTrue_WhenEntityHasAllRequiredComponents()
    {
        var description = new QueryDescription();
        description.AddWrite<TestPosition>();

        var entityComponents = new[] { typeof(TestPosition) };

        Assert.True(description.Matches(entityComponents));
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenEntityMissingRequiredComponent()
    {
        var description = new QueryDescription();
        description.AddWrite<TestPosition>();
        description.AddWrite<TestVelocity>();

        var entityComponents = new[] { typeof(TestPosition) }; // Missing TestVelocity

        Assert.False(description.Matches(entityComponents));
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenEntityHasExcludedComponent()
    {
        var description = new QueryDescription();
        description.AddWrite<TestPosition>();
        description.AddWithout<FrozenTag>();

        var entityComponents = new[] { typeof(TestPosition), typeof(FrozenTag) };

        Assert.False(description.Matches(entityComponents));
    }

    [Fact]
    public void Matches_ReturnsTrue_WhenEntityDoesNotHaveExcludedComponent()
    {
        var description = new QueryDescription();
        description.AddWrite<TestPosition>();
        description.AddWithout<FrozenTag>();

        var entityComponents = new[] { typeof(TestPosition) };

        Assert.True(description.Matches(entityComponents));
    }

    [Fact]
    public void Matches_ReturnsTrue_WhenEntityHasExtraComponents()
    {
        var description = new QueryDescription();
        description.AddWrite<TestPosition>();

        var entityComponents = new[] { typeof(TestPosition), typeof(TestVelocity), typeof(TestHealth) };

        Assert.True(description.Matches(entityComponents));
    }

    [Fact]
    public void AllRequired_CombinesReadWriteAndWith()
    {
        var description = new QueryDescription();
        description.AddRead<TestPosition>();
        description.AddWrite<TestVelocity>();
        description.AddWith<TestHealth>();

        var allRequired = description.AllRequired.ToList();

        Assert.Contains(typeof(TestPosition), allRequired);
        Assert.Contains(typeof(TestVelocity), allRequired);
        Assert.Contains(typeof(TestHealth), allRequired);
    }

    [Fact]
    public void AllRequired_DeduplicatesSameType()
    {
        var description = new QueryDescription();
        description.AddRead<TestPosition>();
        description.AddWrite<TestPosition>(); // Same type in read and write

        var allRequired = description.AllRequired.ToList();

        // Should only appear once due to Distinct()
        Assert.Single(allRequired);
        Assert.Contains(typeof(TestPosition), allRequired);
    }

    [Fact]
    public void Matches_EmptyDescription_MatchesAnyEntity()
    {
        var description = new QueryDescription();

        var entityComponents = new[] { typeof(TestPosition), typeof(TestVelocity) };

        Assert.True(description.Matches(entityComponents));
    }

    [Fact]
    public void Matches_EmptyDescription_MatchesEmptyEntity()
    {
        var description = new QueryDescription();

        var entityComponents = Array.Empty<Type>();

        Assert.True(description.Matches(entityComponents));
    }

    [Fact]
    public void AllRequired_ReturnsCachedInstance_OnSubsequentCalls()
    {
        var description = new QueryDescription();
        description.AddRead<TestPosition>();
        description.AddWrite<TestVelocity>();

        var first = description.AllRequired;
        var second = description.AllRequired;

        // Should return the same cached ImmutableArray instance
        Assert.True(first.Equals(second));
        Assert.Equal(first.Length, second.Length);
    }

    [Fact]
    public void AllRequired_InvalidatesCache_WhenComponentAdded()
    {
        var description = new QueryDescription();
        description.AddRead<TestPosition>();

        var firstCall = description.AllRequired;
        Assert.Single(firstCall);
        Assert.Contains(typeof(TestPosition), firstCall);

        // Add another component
        description.AddWrite<TestVelocity>();

        var secondCall = description.AllRequired;
        Assert.Equal(2, secondCall.Length);
        Assert.Contains(typeof(TestPosition), secondCall);
        Assert.Contains(typeof(TestVelocity), secondCall);
    }

    [Fact]
    public void AllRequired_ReturnsImmutableArray_NotEnumerable()
    {
        var description = new QueryDescription();
        description.AddRead<TestPosition>();
        description.AddWrite<TestVelocity>();

        var allRequired = description.AllRequired;

        // Verify it's an ImmutableArray, not just IEnumerable
        Assert.IsType<System.Collections.Immutable.ImmutableArray<Type>>(allRequired);
    }

    [Fact]
    public void Matches_WithMultipleExclusions_AllMustBeAbsent()
    {
        var description = new QueryDescription();
        description.AddWrite<TestPosition>();
        description.AddWithout<FrozenTag>();
        description.AddWithout<ActiveTag>();

        // Has one excluded component
        var entityWithFrozen = new[] { typeof(TestPosition), typeof(FrozenTag) };
        var entityWithActive = new[] { typeof(TestPosition), typeof(ActiveTag) };
        var entityWithBoth = new[] { typeof(TestPosition), typeof(FrozenTag), typeof(ActiveTag) };
        var entityWithNeither = new[] { typeof(TestPosition) };

        Assert.False(description.Matches(entityWithFrozen));
        Assert.False(description.Matches(entityWithActive));
        Assert.False(description.Matches(entityWithBoth));
        Assert.True(description.Matches(entityWithNeither));
    }
}

/// <summary>
/// Tests for QueryBuilder fluent API and iteration.
/// </summary>
public class QueryBuilderTests
{
    #region Single Component Query

    [Fact]
    public void QueryBuilder_SingleComponent_ReturnsMatchingEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();
        var entity3 = world.Spawn()
            .With(new TestVelocity { X = 3f, Y = 3f })
            .Build();

        var results = world.Query<TestPosition>().ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
        Assert.DoesNotContain(entity3, results);
    }

    [Fact]
    public void QueryBuilder_SingleComponent_With_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition>().With<TestVelocity>().ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBuilder_SingleComponent_Without_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .WithTag<FrozenTag>()
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition>().Without<FrozenTag>().ToList();

        Assert.Single(results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void QueryBuilder_SingleComponent_Description_ContainsWriteComponent()
    {
        using var world = new World();

        var query = world.Query<TestPosition>();

        Assert.Contains(typeof(TestPosition), query.Description.Write);
    }

    [Fact]
    public void QueryBuilder_SingleComponent_World_ReturnsCorrectWorld()
    {
        using var world = new World();

        var query = world.Query<TestPosition>();

        Assert.Same(world, query.World);
    }

    #endregion

    #region Two Component Query

    [Fact]
    public void QueryBuilder_TwoComponents_ReturnsMatchingEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity>().ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBuilder_TwoComponents_With_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity>().With<TestHealth>().ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBuilder_TwoComponents_Without_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .WithTag<FrozenTag>()
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity>().Without<FrozenTag>().ToList();

        Assert.Single(results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void QueryBuilder_TwoComponents_Description_ContainsBothComponents()
    {
        using var world = new World();

        var query = world.Query<TestPosition, TestVelocity>();

        Assert.Contains(typeof(TestPosition), query.Description.Write);
        Assert.Contains(typeof(TestVelocity), query.Description.Write);
    }

    #endregion

    #region Three Component Query

    [Fact]
    public void QueryBuilder_ThreeComponents_ReturnsMatchingEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth>().ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBuilder_ThreeComponents_With_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 0f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 50, Max = 100 })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth>().With<TestRotation>().ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBuilder_ThreeComponents_Without_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .WithTag<FrozenTag>()
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 50, Max = 100 })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth>().Without<FrozenTag>().ToList();

        Assert.Single(results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void QueryBuilder_ThreeComponents_Description_ContainsAllComponents()
    {
        using var world = new World();

        var query = world.Query<TestPosition, TestVelocity, TestHealth>();

        Assert.Contains(typeof(TestPosition), query.Description.Write);
        Assert.Contains(typeof(TestVelocity), query.Description.Write);
        Assert.Contains(typeof(TestHealth), query.Description.Write);
    }

    #endregion

    #region Four Component Query

    [Fact]
    public void QueryBuilder_FourComponents_ReturnsMatchingEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 45f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 50, Max = 100 })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth, TestRotation>().ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBuilder_FourComponents_With_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 45f })
            .WithTag<ActiveTag>()
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 50, Max = 100 })
            .With(new TestRotation { Angle = 90f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth, TestRotation>().With<ActiveTag>().ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBuilder_FourComponents_Without_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 45f })
            .WithTag<FrozenTag>()
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 50, Max = 100 })
            .With(new TestRotation { Angle = 90f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth, TestRotation>().Without<FrozenTag>().ToList();

        Assert.Single(results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void QueryBuilder_FourComponents_Description_ContainsAllComponents()
    {
        using var world = new World();

        var query = world.Query<TestPosition, TestVelocity, TestHealth, TestRotation>();

        Assert.Contains(typeof(TestPosition), query.Description.Write);
        Assert.Contains(typeof(TestVelocity), query.Description.Write);
        Assert.Contains(typeof(TestHealth), query.Description.Write);
        Assert.Contains(typeof(TestRotation), query.Description.Write);
    }

    #endregion

    #region Empty Query Results

    [Fact]
    public void QueryBuilder_ReturnsEmpty_WhenNoEntitiesMatch()
    {
        using var world = new World();

        world.Spawn()
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        var results = world.Query<TestPosition>().ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryBuilder_ReturnsEmpty_WhenWorldHasNoEntities()
    {
        using var world = new World();

        var results = world.Query<TestPosition>().ToList();

        Assert.Empty(results);
    }

    #endregion

    #region Chained Filtering

    [Fact]
    public void QueryBuilder_ChainingWithAndWithout_WorksCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .WithTag<ActiveTag>()
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .WithTag<FrozenTag>()
            .Build();
        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .WithTag<ActiveTag>()
            .Build();

        var results = world.Query<TestPosition>()
            .With<TestVelocity>()
            .With<ActiveTag>()
            .Without<FrozenTag>()
            .ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    #endregion

    #region IEnumerable Interface

    [Fact]
    public void QueryBuilder_ImplementsIEnumerable_ForForeach()
    {
        using var world = new World();

        world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        var count = 0;
        foreach (var entity in world.Query<TestPosition>())
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public void QueryBuilder_IEnumerableEntity_GetEnumerator_Works()
    {
        using var world = new World();

        world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();

        IEnumerable<Entity> query = world.Query<TestPosition>();
        var enumerator = query.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsValid);
        Assert.False(enumerator.MoveNext());

        enumerator.Dispose();
    }

    [Fact]
    public void QueryBuilder_NonGenericIEnumerable_GetEnumerator_Works()
    {
        using var world = new World();

        world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();

        System.Collections.IEnumerable query = world.Query<TestPosition>();
        var enumerator = query.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.NotNull(enumerator.Current);
    }

    #endregion
}

/// <summary>
/// Tests for QueryEnumerator behavior.
/// </summary>
public class QueryEnumeratorTests
{
    [Fact]
    public void QueryEnumerator_Current_ReturnsCurrentEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();

        var enumerator = world.Query<TestPosition>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
    }

    [Fact]
    public void QueryEnumerator_MoveNext_ReturnsFalse_WhenNoEntities()
    {
        using var world = new World();

        var enumerator = world.Query<TestPosition>().GetEnumerator();

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void QueryEnumerator_Reset_ResetsEnumerator()
    {
        using var world = new World();

        world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();

        var enumerator = world.Query<TestPosition>().GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext()); // Exhausted

        // Archetype-based enumerators support Reset
        enumerator.Reset();
        Assert.True(enumerator.MoveNext()); // Can iterate again after reset

        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_Dispose_CanBeCalledMultipleTimes()
    {
        using var world = new World();

        var enumerator = world.Query<TestPosition>().GetEnumerator();

        enumerator.Dispose();
        enumerator.Dispose(); // Should not throw
    }

    [Fact]
    public void QueryEnumerator_TwoComponents_Current_ReturnsCurrentEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        var enumerator = world.Query<TestPosition, TestVelocity>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_ThreeComponents_Current_ReturnsCurrentEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        var enumerator = world.Query<TestPosition, TestVelocity, TestHealth>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_FourComponents_Current_ReturnsCurrentEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 45f })
            .Build();

        var enumerator = world.Query<TestPosition, TestVelocity, TestHealth, TestRotation>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_IEnumeratorCurrent_ReturnsBoxedEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();

        System.Collections.IEnumerator enumerator = world.Query<TestPosition>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
    }
}

/// <summary>
/// Tests for string tag filtering in queries.
/// </summary>
public class QueryStringTagFilteringTests
{
    #region Single Component Query with String Tags

    [Fact]
    public void Query_WithStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        world.AddTag(entity1, "player");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();
        world.AddTag(entity2, "enemy");

        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .Build();

        var results = world.Query<TestPosition>().WithTag("player").ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void Query_WithoutStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        world.AddTag(entity1, "frozen");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .Build();

        var results = world.Query<TestPosition>().WithoutTag("frozen").ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(entity2, results);
        Assert.Contains(entity3, results);
    }

    [Fact]
    public void Query_WithMultipleStringTags_RequiresAllTags()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        world.AddTag(entity1, "player");
        world.AddTag(entity1, "active");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();
        world.AddTag(entity2, "player");

        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .Build();
        world.AddTag(entity3, "active");

        var results = world.Query<TestPosition>()
            .WithTag("player")
            .WithTag("active")
            .ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void Query_WithMultipleWithoutStringTags_ExcludesAnyMatch()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        world.AddTag(entity1, "frozen");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();
        world.AddTag(entity2, "dead");

        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .Build();

        var results = world.Query<TestPosition>()
            .WithoutTag("frozen")
            .WithoutTag("dead")
            .ToList();

        Assert.Single(results);
        Assert.Contains(entity3, results);
    }

    [Fact]
    public void Query_CombinedWithAndWithoutStringTags_FiltersCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        world.AddTag(entity1, "player");
        world.AddTag(entity1, "active");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();
        world.AddTag(entity2, "player");
        world.AddTag(entity2, "frozen");

        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .Build();
        world.AddTag(entity3, "enemy");
        world.AddTag(entity3, "active");

        var results = world.Query<TestPosition>()
            .WithTag("active")
            .WithoutTag("frozen")
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
    }

    #endregion

    #region Two Component Query with String Tags

    [Fact]
    public void QueryTwoComponents_WithStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();
        world.AddTag(entity1, "moving");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 0f, Y = 0f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity>().WithTag("moving").ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryTwoComponents_WithoutStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();
        world.AddTag(entity1, "frozen");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity>().WithoutTag("frozen").ToList();

        Assert.Single(results);
        Assert.Contains(entity2, results);
    }

    #endregion

    #region Three Component Query with String Tags

    [Fact]
    public void QueryThreeComponents_WithStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();
        world.AddTag(entity1, "alive");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 0, Max = 100 })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth>().WithTag("alive").ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryThreeComponents_WithoutStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();
        world.AddTag(entity1, "poisoned");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth>().WithoutTag("poisoned").ToList();

        Assert.Single(results);
        Assert.Contains(entity2, results);
    }

    #endregion

    #region Four Component Query with String Tags

    [Fact]
    public void QueryFourComponents_WithStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 45f })
            .Build();
        world.AddTag(entity1, "rotatable");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 0f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth, TestRotation>()
            .WithTag("rotatable")
            .ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryFourComponents_WithoutStringTag_FiltersEntitiesCorrectly()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 45f })
            .Build();
        world.AddTag(entity1, "locked");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .With(new TestRotation { Angle = 90f })
            .Build();

        var results = world.Query<TestPosition, TestVelocity, TestHealth, TestRotation>()
            .WithoutTag("locked")
            .ToList();

        Assert.Single(results);
        Assert.Contains(entity2, results);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Query_WithStringTag_ReturnsEmpty_WhenNoEntitiesHaveTag()
    {
        using var world = new World();

        world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();

        var results = world.Query<TestPosition>().WithTag("nonexistent").ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Query_WithoutStringTag_ReturnsAll_WhenNoEntitiesHaveTag()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        var results = world.Query<TestPosition>().WithoutTag("nonexistent").ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void Query_StringTagFilter_WorksWithComponentFilters()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .WithTag<ActiveTag>()
            .Build();
        world.AddTag(entity1, "special");

        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();
        world.AddTag(entity2, "special");

        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .WithTag<ActiveTag>()
            .Build();
        world.AddTag(entity3, "special");

        var results = world.Query<TestPosition>()
            .With<TestVelocity>()
            .With<ActiveTag>()
            .WithTag("special")
            .ToList();

        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    #endregion
}
