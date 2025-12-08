namespace KeenEyes.Tests;

/// <summary>
/// Test component for string tag tests.
/// </summary>
public struct TagTestPosition : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Second test component for string tag tests.
/// </summary>
public struct TagTestVelocity : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Tests for string-based entity tagging functionality.
/// </summary>
public class StringTagTests
{
    #region AddTag Tests

    [Fact]
    public void AddTag_ValidTag_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var result = world.AddTag(entity, "Enemy");

        Assert.True(result);
    }

    [Fact]
    public void AddTag_DuplicateTag_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");
        var result = world.AddTag(entity, "Enemy");

        Assert.False(result);
    }

    [Fact]
    public void AddTag_MultipleTags_AllAdded()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");
        world.AddTag(entity, "Hostile");
        world.AddTag(entity, "Boss");

        Assert.True(world.HasTag(entity, "Enemy"));
        Assert.True(world.HasTag(entity, "Hostile"));
        Assert.True(world.HasTag(entity, "Boss"));
    }

    [Fact]
    public void AddTag_DeadEntity_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            world.AddTag(entity, "Enemy"));

        Assert.Contains("not alive", ex.Message);
    }

    [Fact]
    public void AddTag_NullTag_ThrowsArgumentNullException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.Throws<ArgumentNullException>(() =>
            world.AddTag(entity, null!));
    }

    [Fact]
    public void AddTag_EmptyTag_ThrowsArgumentException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.Throws<ArgumentException>(() =>
            world.AddTag(entity, ""));
    }

    [Fact]
    public void AddTag_WhitespaceTag_ThrowsArgumentException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.Throws<ArgumentException>(() =>
            world.AddTag(entity, "   "));
    }

    [Fact]
    public void AddTag_DifferentEntitiesSameTag_BothHaveTag()
    {
        using var world = new World();
        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        world.AddTag(entity1, "Enemy");
        world.AddTag(entity2, "Enemy");

        Assert.True(world.HasTag(entity1, "Enemy"));
        Assert.True(world.HasTag(entity2, "Enemy"));
    }

    #endregion

    #region RemoveTag Tests

    [Fact]
    public void RemoveTag_ExistingTag_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");
        var result = world.RemoveTag(entity, "Enemy");

        Assert.True(result);
        Assert.False(world.HasTag(entity, "Enemy"));
    }

    [Fact]
    public void RemoveTag_NonExistentTag_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var result = world.RemoveTag(entity, "Enemy");

        Assert.False(result);
    }

    [Fact]
    public void RemoveTag_DeadEntity_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.AddTag(entity, "Enemy");
        world.Despawn(entity);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            world.RemoveTag(entity, "Enemy"));

        Assert.Contains("not alive", ex.Message);
    }

    [Fact]
    public void RemoveTag_NullTag_ThrowsArgumentNullException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.Throws<ArgumentNullException>(() =>
            world.RemoveTag(entity, null!));
    }

    [Fact]
    public void RemoveTag_OneOfMany_OnlyRemovesThatTag()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");
        world.AddTag(entity, "Hostile");
        world.AddTag(entity, "Boss");

        world.RemoveTag(entity, "Hostile");

        Assert.True(world.HasTag(entity, "Enemy"));
        Assert.False(world.HasTag(entity, "Hostile"));
        Assert.True(world.HasTag(entity, "Boss"));
    }

    #endregion

    #region HasTag Tests

    [Fact]
    public void HasTag_ExistingTag_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");

        Assert.True(world.HasTag(entity, "Enemy"));
    }

    [Fact]
    public void HasTag_NonExistentTag_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.False(world.HasTag(entity, "Enemy"));
    }

    [Fact]
    public void HasTag_DeadEntity_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.AddTag(entity, "Enemy");
        world.Despawn(entity);

        Assert.False(world.HasTag(entity, "Enemy"));
    }

    [Fact]
    public void HasTag_StaleEntity_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.AddTag(entity, "Enemy");
        world.Despawn(entity);

        // Create new entity - old handle is now stale
        world.Spawn().Build();

        Assert.False(world.HasTag(entity, "Enemy"));
    }

    [Fact]
    public void HasTag_NullTag_ThrowsArgumentNullException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.Throws<ArgumentNullException>(() =>
            world.HasTag(entity, null!));
    }

    [Fact]
    public void HasTag_CaseSensitive()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");

        Assert.True(world.HasTag(entity, "Enemy"));
        Assert.False(world.HasTag(entity, "enemy"));
        Assert.False(world.HasTag(entity, "ENEMY"));
    }

    #endregion

    #region GetTags Tests

    [Fact]
    public void GetTags_EntityWithTags_ReturnsAllTags()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");
        world.AddTag(entity, "Hostile");
        world.AddTag(entity, "Boss");

        var tags = world.GetTags(entity);

        Assert.Equal(3, tags.Count);
        Assert.Contains("Enemy", tags);
        Assert.Contains("Hostile", tags);
        Assert.Contains("Boss", tags);
    }

    [Fact]
    public void GetTags_EntityWithNoTags_ReturnsEmptyCollection()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var tags = world.GetTags(entity);

        Assert.Empty(tags);
    }

    [Fact]
    public void GetTags_DeadEntity_ReturnsEmptyCollection()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.AddTag(entity, "Enemy");
        world.Despawn(entity);

        var tags = world.GetTags(entity);

        Assert.Empty(tags);
    }

    [Fact]
    public void GetTags_InvalidEntity_ReturnsEmptyCollection()
    {
        using var world = new World();
        var invalidEntity = new Entity(999, 1);

        var tags = world.GetTags(invalidEntity);

        Assert.Empty(tags);
    }

    #endregion

    #region QueryByTag Tests

    [Fact]
    public void QueryByTag_ExistingTag_ReturnsMatchingEntities()
    {
        using var world = new World();
        var enemy1 = world.Spawn().Build();
        var enemy2 = world.Spawn().Build();
        var player = world.Spawn().Build();

        world.AddTag(enemy1, "Enemy");
        world.AddTag(enemy2, "Enemy");
        world.AddTag(player, "Player");

        var enemies = world.QueryByTag("Enemy").ToList();

        Assert.Equal(2, enemies.Count);
        Assert.Contains(enemy1, enemies);
        Assert.Contains(enemy2, enemies);
        Assert.DoesNotContain(player, enemies);
    }

    [Fact]
    public void QueryByTag_NonExistentTag_ReturnsEmptyEnumerable()
    {
        using var world = new World();
        world.Spawn().Build();

        var entities = world.QueryByTag("NonExistent").ToList();

        Assert.Empty(entities);
    }

    [Fact]
    public void QueryByTag_NullTag_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.QueryByTag(null!).ToList());
    }

    [Fact]
    public void QueryByTag_DespawnedEntityNotReturned()
    {
        using var world = new World();
        var enemy1 = world.Spawn().Build();
        var enemy2 = world.Spawn().Build();

        world.AddTag(enemy1, "Enemy");
        world.AddTag(enemy2, "Enemy");

        world.Despawn(enemy1);

        var enemies = world.QueryByTag("Enemy").ToList();

        Assert.Single(enemies);
        Assert.Contains(enemy2, enemies);
        Assert.DoesNotContain(enemy1, enemies);
    }

    [Fact]
    public void QueryByTag_MultipleEntitiesWithMultipleTags_ReturnsCorrectEntities()
    {
        using var world = new World();
        var boss = world.Spawn().Build();
        var regularEnemy = world.Spawn().Build();
        var player = world.Spawn().Build();

        world.AddTag(boss, "Enemy");
        world.AddTag(boss, "Boss");
        world.AddTag(regularEnemy, "Enemy");
        world.AddTag(player, "Player");

        var enemies = world.QueryByTag("Enemy").ToList();
        var bosses = world.QueryByTag("Boss").ToList();
        var players = world.QueryByTag("Player").ToList();

        Assert.Equal(2, enemies.Count);
        Assert.Single(bosses);
        Assert.Single(players);

        Assert.Contains(boss, enemies);
        Assert.Contains(boss, bosses);
    }

    #endregion

    #region EntityBuilder WithTag(string) Tests

    [Fact]
    public void EntityBuilder_WithTag_AppliesTagOnBuild()
    {
        using var world = new World();

        var entity = world.Spawn()
            .WithTag("Enemy")
            .Build();

        Assert.True(world.HasTag(entity, "Enemy"));
    }

    [Fact]
    public void EntityBuilder_WithMultipleTags_AppliesAllTags()
    {
        using var world = new World();

        var entity = world.Spawn()
            .WithTag("Enemy")
            .WithTag("Hostile")
            .WithTag("Boss")
            .Build();

        Assert.True(world.HasTag(entity, "Enemy"));
        Assert.True(world.HasTag(entity, "Hostile"));
        Assert.True(world.HasTag(entity, "Boss"));
    }

    [Fact]
    public void EntityBuilder_WithTagAndComponents_BothApplied()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TagTestPosition { X = 10, Y = 20 })
            .WithTag("Enemy")
            .Build();

        Assert.True(world.Has<TagTestPosition>(entity));
        Assert.True(world.HasTag(entity, "Enemy"));

        ref var pos = ref world.Get<TagTestPosition>(entity);
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
    }

    [Fact]
    public void EntityBuilder_WithTagNullTag_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.Spawn().WithTag(null!).Build());
    }

    [Fact]
    public void EntityBuilder_WithTagEmptyTag_ThrowsArgumentException()
    {
        using var world = new World();

        Assert.Throws<ArgumentException>(() =>
            world.Spawn().WithTag("").Build());
    }

    [Fact]
    public void EntityBuilder_Reuse_ClearsTagsBetweenBuilds()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .WithTag("Tag1")
            .Build();

        var entity2 = world.Spawn().Build();

        Assert.True(world.HasTag(entity1, "Tag1"));
        Assert.False(world.HasTag(entity2, "Tag1"));
    }

    #endregion

    #region QueryBuilder WithTag(string) Tests

    [Fact]
    public void QueryBuilder_WithTag_FiltersEntitiesWithTag()
    {
        using var world = new World();

        var enemy1 = world.Spawn()
            .With(new TagTestPosition { X = 1, Y = 1 })
            .Build();
        var enemy2 = world.Spawn()
            .With(new TagTestPosition { X = 2, Y = 2 })
            .Build();
        var player = world.Spawn()
            .With(new TagTestPosition { X = 3, Y = 3 })
            .Build();

        world.AddTag(enemy1, "Enemy");
        world.AddTag(enemy2, "Enemy");
        world.AddTag(player, "Player");

        var enemies = world.Query<TagTestPosition>()
            .WithTag("Enemy")
            .ToList();

        Assert.Equal(2, enemies.Count);
        Assert.Contains(enemy1, enemies);
        Assert.Contains(enemy2, enemies);
        Assert.DoesNotContain(player, enemies);
    }

    [Fact]
    public void QueryBuilder_WithoutTag_ExcludesEntitiesWithTag()
    {
        using var world = new World();

        var enemy = world.Spawn()
            .With(new TagTestPosition { X = 1, Y = 1 })
            .Build();
        var player = world.Spawn()
            .With(new TagTestPosition { X = 2, Y = 2 })
            .Build();

        world.AddTag(enemy, "Enemy");
        world.AddTag(player, "Player");

        var nonEnemies = world.Query<TagTestPosition>()
            .WithoutTag("Enemy")
            .ToList();

        Assert.Single(nonEnemies);
        Assert.Contains(player, nonEnemies);
        Assert.DoesNotContain(enemy, nonEnemies);
    }

    [Fact]
    public void QueryBuilder_MultipleTagFilters_CombinesFilters()
    {
        using var world = new World();

        var hostileBoss = world.Spawn()
            .With(new TagTestPosition { X = 1, Y = 1 })
            .Build();
        var hostileMinion = world.Spawn()
            .With(new TagTestPosition { X = 2, Y = 2 })
            .Build();
        var friendlyNpc = world.Spawn()
            .With(new TagTestPosition { X = 3, Y = 3 })
            .Build();

        world.AddTag(hostileBoss, "Hostile");
        world.AddTag(hostileBoss, "Boss");
        world.AddTag(hostileMinion, "Hostile");
        world.AddTag(friendlyNpc, "Friendly");

        var hostileBosses = world.Query<TagTestPosition>()
            .WithTag("Hostile")
            .WithTag("Boss")
            .ToList();

        Assert.Single(hostileBosses);
        Assert.Contains(hostileBoss, hostileBosses);
    }

    [Fact]
    public void QueryBuilder_WithTagAndWithoutTag_CombinesFilters()
    {
        using var world = new World();

        var hostileBoss = world.Spawn()
            .With(new TagTestPosition { X = 1, Y = 1 })
            .Build();
        var hostileMinion = world.Spawn()
            .With(new TagTestPosition { X = 2, Y = 2 })
            .Build();

        world.AddTag(hostileBoss, "Hostile");
        world.AddTag(hostileBoss, "Boss");
        world.AddTag(hostileMinion, "Hostile");

        var hostileNonBosses = world.Query<TagTestPosition>()
            .WithTag("Hostile")
            .WithoutTag("Boss")
            .ToList();

        Assert.Single(hostileNonBosses);
        Assert.Contains(hostileMinion, hostileNonBosses);
        Assert.DoesNotContain(hostileBoss, hostileNonBosses);
    }

    [Fact]
    public void QueryBuilder_WithTagAndComponentFilters_CombinesFilters()
    {
        using var world = new World();

        var movingEnemy = world.Spawn()
            .With(new TagTestPosition { X = 1, Y = 1 })
            .With(new TagTestVelocity { X = 1, Y = 0 })
            .Build();
        var stationaryEnemy = world.Spawn()
            .With(new TagTestPosition { X = 2, Y = 2 })
            .Build();
        var movingPlayer = world.Spawn()
            .With(new TagTestPosition { X = 3, Y = 3 })
            .With(new TagTestVelocity { X = 0, Y = 1 })
            .Build();

        world.AddTag(movingEnemy, "Enemy");
        world.AddTag(stationaryEnemy, "Enemy");
        world.AddTag(movingPlayer, "Player");

        var movingEnemies = world.Query<TagTestPosition, TagTestVelocity>()
            .WithTag("Enemy")
            .ToList();

        Assert.Single(movingEnemies);
        Assert.Contains(movingEnemy, movingEnemies);
    }

    [Fact]
    public void QueryBuilder_WithTag_NullTag_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.Query<TagTestPosition>().WithTag(null!).ToList());
    }

    [Fact]
    public void QueryBuilder_WithTag_NoMatchingEntities_ReturnsEmpty()
    {
        using var world = new World();

        world.Spawn()
            .With(new TagTestPosition { X = 1, Y = 1 })
            .Build();

        var result = world.Query<TagTestPosition>()
            .WithTag("NonExistent")
            .ToList();

        Assert.Empty(result);
    }

    #endregion

    #region Despawn Cleanup Tests

    [Fact]
    public void Despawn_EntityWithTags_CleansUpAllTags()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy");
        world.AddTag(entity, "Hostile");
        world.AddTag(entity, "Boss");

        world.Despawn(entity);

        // Tags should be cleaned up
        Assert.False(world.HasTag(entity, "Enemy"));
        Assert.False(world.HasTag(entity, "Hostile"));
        Assert.False(world.HasTag(entity, "Boss"));
    }

    [Fact]
    public void Despawn_EntityWithTags_QueryByTagNoLongerReturnsEntity()
    {
        using var world = new World();
        var enemy1 = world.Spawn().Build();
        var enemy2 = world.Spawn().Build();

        world.AddTag(enemy1, "Enemy");
        world.AddTag(enemy2, "Enemy");

        world.Despawn(enemy1);

        var enemies = world.QueryByTag("Enemy").ToList();

        Assert.Single(enemies);
        Assert.Contains(enemy2, enemies);
        Assert.DoesNotContain(enemy1, enemies);
    }

    [Fact]
    public void Despawn_TagCanBeReusedOnNewEntity()
    {
        using var world = new World();
        var entity1 = world.Spawn().Build();
        world.AddTag(entity1, "UniqueTag");

        world.Despawn(entity1);

        var entity2 = world.Spawn().Build();
        world.AddTag(entity2, "UniqueTag");

        Assert.True(world.HasTag(entity2, "UniqueTag"));

        var entities = world.QueryByTag("UniqueTag").ToList();
        Assert.Single(entities);
        Assert.Contains(entity2, entities);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void ManyTags_PerformanceTest()
    {
        using var world = new World();

        // Create entities with many tags
        for (int i = 0; i < 100; i++)
        {
            var entity = world.Spawn().Build();
            for (int j = 0; j < 10; j++)
            {
                world.AddTag(entity, $"Tag_{j}");
            }
        }

        // Query should still work efficiently
        var tag5Entities = world.QueryByTag("Tag_5").ToList();
        Assert.Equal(100, tag5Entities.Count);
    }

    [Fact]
    public void QueryWithTagFilter_ManyEntities_PerformanceTest()
    {
        using var world = new World();

        // Create many entities with components
        for (int i = 0; i < 1000; i++)
        {
            var entity = world.Spawn()
                .With(new TagTestPosition { X = i, Y = i })
                .Build();

            if (i % 10 == 0)
            {
                world.AddTag(entity, "Selected");
            }
        }

        // Query with tag filter
        var selected = world.Query<TagTestPosition>()
            .WithTag("Selected")
            .ToList();

        Assert.Equal(100, selected.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddTag_SpecialCharacters_Works()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Enemy_01");
        world.AddTag(entity, "Boss (Final)");
        world.AddTag(entity, "!@#$%^&*()");

        Assert.True(world.HasTag(entity, "Enemy_01"));
        Assert.True(world.HasTag(entity, "Boss (Final)"));
        Assert.True(world.HasTag(entity, "!@#$%^&*()"));
    }

    [Fact]
    public void AddTag_UnicodeCharacters_Works()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "敵");
        world.AddTag(entity, "враг");

        Assert.True(world.HasTag(entity, "敵"));
        Assert.True(world.HasTag(entity, "враг"));
    }

    [Fact]
    public void GetTags_ReturnsLiveCollection()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.AddTag(entity, "Tag1");
        world.AddTag(entity, "Tag2");

        var tags = world.GetTags(entity);
        Assert.Equal(2, tags.Count);

        world.AddTag(entity, "Tag3");

        // The returned collection reflects internal state changes
        // (it's a live view of the HashSet)
        Assert.Equal(3, tags.Count);
    }

    [Fact]
    public void EntityBuilder_WithTag_ChainedWithName_BothWork()
    {
        using var world = new World();

        var entity = world.Spawn("NamedEnemy")
            .With(new TagTestPosition { X = 0, Y = 0 })
            .WithTag("Enemy")
            .Build();

        Assert.Equal("NamedEnemy", world.GetName(entity));
        Assert.True(world.HasTag(entity, "Enemy"));
        Assert.True(world.Has<TagTestPosition>(entity));
    }

    [Fact]
    public void QueryBuilder_TwoComponents_WithTag_Works()
    {
        using var world = new World();

        var movingEnemy = world.Spawn()
            .With(new TagTestPosition { X = 1, Y = 1 })
            .With(new TagTestVelocity { X = 1, Y = 0 })
            .Build();

        world.AddTag(movingEnemy, "Enemy");

        var result = world.Query<TagTestPosition, TagTestVelocity>()
            .WithTag("Enemy")
            .ToList();

        Assert.Single(result);
        Assert.Contains(movingEnemy, result);
    }

    #endregion
}
