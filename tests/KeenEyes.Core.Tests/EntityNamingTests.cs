namespace KeenEyes.Tests;

/// <summary>
/// Test component for entity naming tests.
/// </summary>
public struct NamingTestPosition : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Tests for entity naming functionality including Spawn(name), GetName, and GetEntityByName.
/// </summary>
public class EntityNamingTests
{
    #region Spawn(string? name) Tests

    [Fact]
    public void Spawn_WithName_CreatesNamedEntity()
    {
        using var world = new World();

        var entity = world.Spawn("Player")
            .With(new NamingTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(entity.IsValid);
        Assert.True(world.IsAlive(entity));
        Assert.Equal("Player", world.GetName(entity));
    }

    [Fact]
    public void Spawn_WithNullName_CreatesUnnamedEntity()
    {
        using var world = new World();

        var entity = world.Spawn(null)
            .With(new NamingTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(entity.IsValid);
        Assert.True(world.IsAlive(entity));
        Assert.Null(world.GetName(entity));
    }

    [Fact]
    public void Spawn_WithoutName_CreatesUnnamedEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new NamingTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(entity.IsValid);
        Assert.Null(world.GetName(entity));
    }

    [Fact]
    public void Spawn_WithDuplicateName_ThrowsArgumentException()
    {
        using var world = new World();

        world.Spawn("Player")
            .With(new NamingTestPosition { X = 0f, Y = 0f })
            .Build();

        var ex = Assert.Throws<ArgumentException>(() =>
            world.Spawn("Player")
                .With(new NamingTestPosition { X = 1f, Y = 1f })
                .Build());

        Assert.Contains("Player", ex.Message);
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void Spawn_WithEmptyName_AllowsEmptyStringAsName()
    {
        using var world = new World();

        var entity = world.Spawn("")
            .With(new NamingTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(entity.IsValid);
        Assert.Equal("", world.GetName(entity));
    }

    [Fact]
    public void Spawn_MultipleNamedEntities_EachHasUniqueName()
    {
        using var world = new World();

        var player = world.Spawn("Player").Build();
        var enemy = world.Spawn("Enemy").Build();
        var npc = world.Spawn("NPC").Build();

        Assert.Equal("Player", world.GetName(player));
        Assert.Equal("Enemy", world.GetName(enemy));
        Assert.Equal("NPC", world.GetName(npc));
    }

    [Fact]
    public void Spawn_MixedNamedAndUnnamedEntities_WorksCorrectly()
    {
        using var world = new World();

        var named1 = world.Spawn("Named1").Build();
        var unnamed = world.Spawn().Build();
        var named2 = world.Spawn("Named2").Build();

        Assert.Equal("Named1", world.GetName(named1));
        Assert.Null(world.GetName(unnamed));
        Assert.Equal("Named2", world.GetName(named2));
    }

    [Fact]
    public void Spawn_BuilderReuse_ClearsNameBetweenEntities()
    {
        using var world = new World();

        // First entity with a name
        var named = world.Spawn("Named").Build();

        // Second entity without a name (using Spawn() after Spawn(name))
        var unnamed = world.Spawn().Build();

        Assert.Equal("Named", world.GetName(named));
        Assert.Null(world.GetName(unnamed));
    }

    [Fact]
    public void Spawn_BuilderReuse_ClearsNameWhenExplicitlyNull()
    {
        using var world = new World();

        // First entity with a name
        var named = world.Spawn("Named").Build();

        // Second entity with explicit null name
        var unnamed = world.Spawn(null).Build();

        Assert.Equal("Named", world.GetName(named));
        Assert.Null(world.GetName(unnamed));
    }

    #endregion

    #region GetName Tests

    [Fact]
    public void GetName_NamedEntity_ReturnsName()
    {
        using var world = new World();
        var entity = world.Spawn("TestEntity").Build();

        var name = world.GetName(entity);

        Assert.Equal("TestEntity", name);
    }

    [Fact]
    public void GetName_UnnamedEntity_ReturnsNull()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var name = world.GetName(entity);

        Assert.Null(name);
    }

    [Fact]
    public void GetName_DespawnedEntity_ReturnsNull()
    {
        using var world = new World();
        var entity = world.Spawn("TestEntity").Build();

        world.Despawn(entity);

        var name = world.GetName(entity);
        Assert.Null(name);
    }

    [Fact]
    public void GetName_InvalidEntity_ReturnsNull()
    {
        using var world = new World();
        var invalidEntity = new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion);

        var name = world.GetName(invalidEntity);

        Assert.Null(name);
    }

    [Fact]
    public void GetName_StaleEntity_ReturnsNull()
    {
        using var world = new World();
        var entity = world.Spawn("TestEntity").Build();

        world.Despawn(entity);
        // Create a new entity - the old handle is now stale
        var newEntity = world.Spawn("NewEntity").Build();

        // The old entity handle should return null
        Assert.Null(world.GetName(entity));
        Assert.Equal("NewEntity", world.GetName(newEntity));
    }

    [Fact]
    public void GetName_EntityNull_ReturnsNull()
    {
        using var world = new World();

        var name = world.GetName(Entity.Null);

        Assert.Null(name);
    }

    #endregion

    #region GetEntityByName Tests

    [Fact]
    public void GetEntityByName_ExistingName_ReturnsEntity()
    {
        using var world = new World();
        var original = world.Spawn("Player")
            .With(new NamingTestPosition { X = 10f, Y = 20f })
            .Build();

        var found = world.GetEntityByName("Player");

        Assert.True(found.IsValid);
        Assert.Equal(original, found);
    }

    [Fact]
    public void GetEntityByName_NonExistentName_ReturnsEntityNull()
    {
        using var world = new World();
        world.Spawn("Player").Build();

        var found = world.GetEntityByName("Enemy");

        Assert.False(found.IsValid);
        Assert.Equal(Entity.Null, found);
    }

    [Fact]
    public void GetEntityByName_DespawnedEntity_ReturnsEntityNull()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();

        world.Despawn(entity);

        var found = world.GetEntityByName("Player");
        Assert.False(found.IsValid);
        Assert.Equal(Entity.Null, found);
    }

    [Fact]
    public void GetEntityByName_ReturnsEntityWithCorrectVersion()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();

        var found = world.GetEntityByName("Player");

        Assert.Equal(entity.Version, found.Version);
        Assert.True(world.IsAlive(found));
    }

    [Fact]
    public void GetEntityByName_EmptyString_FindsEntityWithEmptyName()
    {
        using var world = new World();
        var entity = world.Spawn("").Build();

        var found = world.GetEntityByName("");

        Assert.True(found.IsValid);
        Assert.Equal(entity, found);
    }

    [Fact]
    public void GetEntityByName_CaseSensitive_DoesNotFindDifferentCase()
    {
        using var world = new World();
        world.Spawn("Player").Build();

        var found = world.GetEntityByName("player");

        Assert.False(found.IsValid);
        Assert.Equal(Entity.Null, found);
    }

    [Fact]
    public void GetEntityByName_MultipleEntities_FindsCorrectOne()
    {
        using var world = new World();
        var player = world.Spawn("Player")
            .With(new NamingTestPosition { X = 1f, Y = 1f })
            .Build();
        var enemy = world.Spawn("Enemy")
            .With(new NamingTestPosition { X = 2f, Y = 2f })
            .Build();
        var npc = world.Spawn("NPC")
            .With(new NamingTestPosition { X = 3f, Y = 3f })
            .Build();

        Assert.Equal(player, world.GetEntityByName("Player"));
        Assert.Equal(enemy, world.GetEntityByName("Enemy"));
        Assert.Equal(npc, world.GetEntityByName("NPC"));
    }

    #endregion

    #region Despawn Name Cleanup Tests

    [Fact]
    public void Despawn_NamedEntity_CleansUpNameMapping()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();

        world.Despawn(entity);

        // Name should no longer be found
        Assert.Equal(Entity.Null, world.GetEntityByName("Player"));
    }

    [Fact]
    public void Despawn_NamedEntity_AllowsNameReuse()
    {
        using var world = new World();
        var entity1 = world.Spawn("Player").Build();

        world.Despawn(entity1);

        // Should be able to create a new entity with the same name
        var entity2 = world.Spawn("Player").Build();

        Assert.True(entity2.IsValid);
        Assert.Equal("Player", world.GetName(entity2));
        // With entity pooling, IDs may be recycled but versions will differ
        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    public void Despawn_UnnamedEntity_DoesNotAffectOtherNames()
    {
        using var world = new World();
        var named = world.Spawn("Player").Build();
        var unnamed = world.Spawn().Build();

        world.Despawn(unnamed);

        Assert.Equal("Player", world.GetName(named));
        Assert.Equal(named, world.GetEntityByName("Player"));
    }

    [Fact]
    public void Despawn_OneOfManyNamedEntities_OnlyRemovesThatName()
    {
        using var world = new World();
        var player = world.Spawn("Player").Build();
        var enemy = world.Spawn("Enemy").Build();
        var npc = world.Spawn("NPC").Build();

        world.Despawn(enemy);

        Assert.Equal(player, world.GetEntityByName("Player"));
        Assert.Equal(Entity.Null, world.GetEntityByName("Enemy"));
        Assert.Equal(npc, world.GetEntityByName("NPC"));
    }

    [Fact]
    public void Despawn_AlreadyDespawnedEntity_DoesNotThrow()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();

        world.Despawn(entity);
        var result = world.Despawn(entity); // Second despawn

        Assert.False(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void NamedEntity_CanHaveComponentsAddedAndRemoved()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();

        world.Add(entity, new NamingTestPosition { X = 10f, Y = 20f });

        Assert.True(world.Has<NamingTestPosition>(entity));
        Assert.Equal("Player", world.GetName(entity));

        ref var pos = ref world.Get<NamingTestPosition>(entity);
        Assert.Equal(10f, pos.X);
        Assert.Equal(20f, pos.Y);
    }

    [Fact]
    public void NamedEntity_WorksWithQueries()
    {
        using var world = new World();

        var player = world.Spawn("Player")
            .With(new NamingTestPosition { X = 1f, Y = 1f })
            .Build();
        world.Spawn("Obstacle")
            .Build(); // No position

        var count = 0;
        foreach (var entity in world.Query<NamingTestPosition>())
        {
            count++;
            Assert.Equal(player, entity);
            Assert.Equal("Player", world.GetName(entity));
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public void GetAllEntities_IncludesBothNamedAndUnnamedEntities()
    {
        using var world = new World();

        var named1 = world.Spawn("Named1").Build();
        var unnamed = world.Spawn().Build();
        var named2 = world.Spawn("Named2").Build();

        var entities = world.GetAllEntities().ToList();

        Assert.Equal(3, entities.Count);
        Assert.Contains(named1, entities);
        Assert.Contains(unnamed, entities);
        Assert.Contains(named2, entities);
    }

    [Fact]
    public void NameRoundTrip_GetNameThenGetEntityByName_ReturnsSameEntity()
    {
        using var world = new World();
        var original = world.Spawn("TestEntity")
            .With(new NamingTestPosition { X = 42f, Y = 24f })
            .Build();

        var name = world.GetName(original);
        var found = world.GetEntityByName(name!);

        Assert.Equal(original, found);
    }

    [Fact]
    public void ComplexScenario_CreateDespawnRecreate_WorksCorrectly()
    {
        using var world = new World();

        // Create initial named entities
        var player = world.Spawn("Player").Build();
        var enemy1 = world.Spawn("Enemy1").Build();

        // Despawn one
        world.Despawn(enemy1);

        // Create new entities, reusing the name
        var enemy1New = world.Spawn("Enemy1").Build();
        var enemy2 = world.Spawn("Enemy2").Build();

        // Verify state
        Assert.Equal(player, world.GetEntityByName("Player"));
        Assert.Equal(enemy1New, world.GetEntityByName("Enemy1"));
        Assert.Equal(enemy2, world.GetEntityByName("Enemy2"));

        // Old enemy1 handle should be invalid
        Assert.False(world.IsAlive(enemy1));
        Assert.Null(world.GetName(enemy1));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Spawn_WithSpecialCharactersInName_Works()
    {
        using var world = new World();

        var entity = world.Spawn("Player_01 (Main)").Build();

        Assert.Equal("Player_01 (Main)", world.GetName(entity));
        Assert.Equal(entity, world.GetEntityByName("Player_01 (Main)"));
    }

    [Fact]
    public void Spawn_WithUnicodeCharactersInName_Works()
    {
        using var world = new World();

        var entity = world.Spawn("プレイヤー").Build();

        Assert.Equal("プレイヤー", world.GetName(entity));
        Assert.Equal(entity, world.GetEntityByName("プレイヤー"));
    }

    [Fact]
    public void Spawn_WithWhitespaceOnlyName_AllowsIt()
    {
        using var world = new World();

        var entity = world.Spawn("   ").Build();

        Assert.Equal("   ", world.GetName(entity));
        Assert.Equal(entity, world.GetEntityByName("   "));
    }

    [Fact]
    public void Spawn_ManyEntities_PerformanceTest()
    {
        using var world = new World();

        // Create many named entities
        for (int i = 0; i < 1000; i++)
        {
            world.Spawn($"Entity_{i}").Build();
        }

        // Verify lookup still works
        var entity500 = world.GetEntityByName("Entity_500");
        Assert.True(entity500.IsValid);
        Assert.Equal("Entity_500", world.GetName(entity500));
    }

    #endregion

    #region SetName Tests

    [Fact]
    public void SetName_ToExistingName_ThrowsArgumentException()
    {
        using var world = new World();

        var entity1 = world.Spawn("Player").Build();
        var entity2 = world.Spawn("Enemy").Build();

        // Try to rename entity2 to "Player" which already exists
        var ex = Assert.Throws<ArgumentException>(() =>
            world.SetName(entity2, "Player"));

        Assert.Contains("Player", ex.Message);
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void SetName_ToSameName_DoesNotThrow()
    {
        using var world = new World();

        var entity = world.Spawn("Player").Build();

        // Renaming to the same name should not throw
        world.SetName(entity, "Player");

        Assert.Equal("Player", world.GetName(entity));
    }

    [Fact]
    public void SetName_ToNull_RemovesName()
    {
        using var world = new World();

        var entity = world.Spawn("Player").Build();

        world.SetName(entity, null);

        Assert.Null(world.GetName(entity));
        Assert.Equal(Entity.Null, world.GetEntityByName("Player"));
    }

    [Fact]
    public void SetName_ToNewName_UpdatesNameMapping()
    {
        using var world = new World();

        var entity = world.Spawn("OldName").Build();

        world.SetName(entity, "NewName");

        Assert.Equal("NewName", world.GetName(entity));
        Assert.Equal(Entity.Null, world.GetEntityByName("OldName"));
        Assert.Equal(entity, world.GetEntityByName("NewName"));
    }

    [Fact]
    public void SetName_FromNullToName_AddsNameMapping()
    {
        using var world = new World();

        var entity = world.Spawn().Build();
        Assert.Null(world.GetName(entity));

        world.SetName(entity, "NewName");

        Assert.Equal("NewName", world.GetName(entity));
        Assert.Equal(entity, world.GetEntityByName("NewName"));
    }

    [Fact]
    public void SetName_OnDeadEntity_ThrowsInvalidOperationException()
    {
        using var world = new World();

        var entity = world.Spawn("Player").Build();
        world.Despawn(entity);

        // SetName on dead entity throws InvalidOperationException
        var ex = Assert.Throws<InvalidOperationException>(() =>
            world.SetName(entity, "NewName"));

        Assert.Contains("not alive", ex.Message);
    }

    #endregion
}
