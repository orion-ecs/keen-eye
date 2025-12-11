namespace KeenEyes.Tests;

/// <summary>
/// Tests for the prefab system (reusable entity templates).
/// </summary>
public class PrefabTests
{
    #region Test Components

    public struct Position : IComponent
    {
        public float X;
        public float Y;
    }

    public struct Velocity : IComponent
    {
        public float X;
        public float Y;
    }

    public struct Health : IComponent
    {
        public int Current;
        public int Max;
    }

    public struct Damage : IComponent
    {
        public int Amount;
    }

    public struct EnemyTag : ITagComponent;

    public struct PlayerTag : ITagComponent;

    public struct BossTag : ITagComponent;

    /// <summary>
    /// Registers all test component types with the world.
    /// Required for AOT-compatible prefab spawning.
    /// </summary>
    private static void RegisterTestComponents(World world)
    {
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();
        world.Components.Register<Health>();
        world.Components.Register<Damage>();
        world.Components.Register<EnemyTag>(isTag: true);
        world.Components.Register<PlayerTag>(isTag: true);
        world.Components.Register<BossTag>(isTag: true);
    }

    #endregion

    #region RegisterPrefab Tests

    [Fact]
    public void RegisterPrefab_WithValidPrefab_Succeeds()
    {
        using var world = new World();
        var prefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 });

        world.RegisterPrefab("TestPrefab", prefab);

        Assert.True(world.HasPrefab("TestPrefab"));
    }

    [Fact]
    public void RegisterPrefab_WithNullName_ThrowsArgumentNullException()
    {
        using var world = new World();
        var prefab = new EntityPrefab();

        Assert.Throws<ArgumentNullException>(() => world.RegisterPrefab(null!, prefab));
    }

    [Fact]
    public void RegisterPrefab_WithNullPrefab_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() => world.RegisterPrefab("Test", null!));
    }

    [Fact]
    public void RegisterPrefab_WithDuplicateName_ThrowsArgumentException()
    {
        using var world = new World();
        var prefab1 = new EntityPrefab().With(new Position { X = 0, Y = 0 });
        var prefab2 = new EntityPrefab().With(new Position { X = 1, Y = 1 });

        world.RegisterPrefab("Enemy", prefab1);

        var ex = Assert.Throws<ArgumentException>(() => world.RegisterPrefab("Enemy", prefab2));
        Assert.Contains("already registered", ex.Message);
    }

    #endregion

    #region HasPrefab Tests

    [Fact]
    public void HasPrefab_WithRegisteredPrefab_ReturnsTrue()
    {
        using var world = new World();
        var prefab = new EntityPrefab();
        world.RegisterPrefab("Test", prefab);

        Assert.True(world.HasPrefab("Test"));
    }

    [Fact]
    public void HasPrefab_WithUnregisteredPrefab_ReturnsFalse()
    {
        using var world = new World();

        Assert.False(world.HasPrefab("NonExistent"));
    }

    [Fact]
    public void HasPrefab_WithNullName_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() => world.HasPrefab(null!));
    }

    #endregion

    #region UnregisterPrefab Tests

    [Fact]
    public void UnregisterPrefab_WithRegisteredPrefab_ReturnsTrue()
    {
        using var world = new World();
        world.RegisterPrefab("Test", new EntityPrefab());

        var result = world.UnregisterPrefab("Test");

        Assert.True(result);
        Assert.False(world.HasPrefab("Test"));
    }

    [Fact]
    public void UnregisterPrefab_WithUnregisteredPrefab_ReturnsFalse()
    {
        using var world = new World();

        var result = world.UnregisterPrefab("NonExistent");

        Assert.False(result);
    }

    [Fact]
    public void UnregisterPrefab_DoesNotAffectExistingEntities()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Health { Current = 100, Max = 100 });
        world.RegisterPrefab("Enemy", prefab);
        var entity = world.SpawnFromPrefab("Enemy").Build();

        world.UnregisterPrefab("Enemy");

        Assert.True(world.IsAlive(entity));
        Assert.True(world.Has<Health>(entity));
        Assert.Equal(100, world.Get<Health>(entity).Current);
    }

    #endregion

    #region GetAllPrefabNames Tests

    [Fact]
    public void GetAllPrefabNames_WithNoPrefabs_ReturnsEmpty()
    {
        using var world = new World();

        var names = world.GetAllPrefabNames().ToList();

        Assert.Empty(names);
    }

    [Fact]
    public void GetAllPrefabNames_ReturnsAllRegisteredNames()
    {
        using var world = new World();
        world.RegisterPrefab("Enemy", new EntityPrefab());
        world.RegisterPrefab("Player", new EntityPrefab());
        world.RegisterPrefab("Item", new EntityPrefab());

        var names = world.GetAllPrefabNames().ToList();

        Assert.Equal(3, names.Count);
        Assert.Contains("Enemy", names);
        Assert.Contains("Player", names);
        Assert.Contains("Item", names);
    }

    #endregion

    #region SpawnFromPrefab Tests

    [Fact]
    public void SpawnFromPrefab_CreatesEntityWithComponents()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 10, Y = 20 })
            .With(new Health { Current = 100, Max = 100 });
        world.RegisterPrefab("Enemy", prefab);

        var entity = world.SpawnFromPrefab("Enemy").Build();

        Assert.True(world.IsAlive(entity));
        Assert.True(world.Has<Position>(entity));
        Assert.True(world.Has<Health>(entity));
        Assert.Equal(10, world.Get<Position>(entity).X);
        Assert.Equal(20, world.Get<Position>(entity).Y);
        Assert.Equal(100, world.Get<Health>(entity).Current);
    }

    [Fact]
    public void SpawnFromPrefab_WithTagComponent_CreatesEntityWithTag()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 })
            .WithTag<EnemyTag>();
        world.RegisterPrefab("Enemy", prefab);

        var entity = world.SpawnFromPrefab("Enemy").Build();

        Assert.True(world.Has<EnemyTag>(entity));
    }

    [Fact]
    public void SpawnFromPrefab_WithUnregisteredPrefab_ThrowsInvalidOperationException()
    {
        using var world = new World();

        var ex = Assert.Throws<InvalidOperationException>(() => world.SpawnFromPrefab("NonExistent"));
        Assert.Contains("No prefab registered", ex.Message);
    }

    [Fact]
    public void SpawnFromPrefab_WithNullName_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() => world.SpawnFromPrefab(null!));
    }

    [Fact]
    public void SpawnFromPrefab_CreatesDistinctEntities()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 });
        world.RegisterPrefab("Test", prefab);

        var entity1 = world.SpawnFromPrefab("Test").Build();
        var entity2 = world.SpawnFromPrefab("Test").Build();

        Assert.NotEqual(entity1, entity2);
        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void SpawnFromPrefab_AllowsModifyingBuilderBeforeBuild()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 });
        world.RegisterPrefab("Test", prefab);

        var entity = world.SpawnFromPrefab("Test")
            .With(new Velocity { X = 5, Y = 10 })
            .Build();

        Assert.True(world.Has<Position>(entity));
        Assert.True(world.Has<Velocity>(entity));
        Assert.Equal(5, world.Get<Velocity>(entity).X);
    }

    #endregion

    #region SpawnFromPrefab with Entity Name Tests

    [Fact]
    public void SpawnFromPrefab_WithEntityName_CreatesNamedEntity()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 });
        world.RegisterPrefab("Player", prefab);

        var entity = world.SpawnFromPrefab("Player", "MainPlayer").Build();

        Assert.Equal("MainPlayer", world.GetName(entity));
        var found = world.GetEntityByName("MainPlayer");
        Assert.Equal(entity, found);
    }

    [Fact]
    public void SpawnFromPrefab_WithNullEntityName_CreatesUnnamedEntity()
    {
        using var world = new World();
        var prefab = new EntityPrefab();
        world.RegisterPrefab("Test", prefab);

        var entity = world.SpawnFromPrefab("Test", null).Build();

        Assert.Null(world.GetName(entity));
    }

    #endregion

    #region Component Override Tests

    [Fact]
    public void SpawnFromPrefab_WithOverride_UsesOverriddenValues()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 })
            .With(new Health { Current = 100, Max = 100 });
        world.RegisterPrefab("Enemy", prefab);

        var entity = world.SpawnFromPrefab("Enemy")
            .With(new Position { X = 50, Y = 75 })
            .Build();

        Assert.Equal(50, world.Get<Position>(entity).X);
        Assert.Equal(75, world.Get<Position>(entity).Y);
        Assert.Equal(100, world.Get<Health>(entity).Current);
    }

    [Fact]
    public void SpawnFromPrefab_WithTagOverride_KeepsTag()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .WithTag<EnemyTag>();
        world.RegisterPrefab("Enemy", prefab);

        var entity = world.SpawnFromPrefab("Enemy")
            .WithTag<BossTag>()
            .Build();

        Assert.True(world.Has<EnemyTag>(entity));
        Assert.True(world.Has<BossTag>(entity));
    }

    #endregion

    #region Prefab Inheritance Tests

    [Fact]
    public void SpawnFromPrefab_WithInheritance_InheritsBaseComponents()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var basePrefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 })
            .With(new Health { Current = 100, Max = 100 });
        var derivedPrefab = new EntityPrefab()
            .Extends("BaseEntity")
            .With(new Damage { Amount = 10 });

        world.RegisterPrefab("BaseEntity", basePrefab);
        world.RegisterPrefab("Enemy", derivedPrefab);

        var entity = world.SpawnFromPrefab("Enemy").Build();

        Assert.True(world.Has<Position>(entity));
        Assert.True(world.Has<Health>(entity));
        Assert.True(world.Has<Damage>(entity));
        Assert.Equal(100, world.Get<Health>(entity).Current);
        Assert.Equal(10, world.Get<Damage>(entity).Amount);
    }

    [Fact]
    public void SpawnFromPrefab_WithInheritance_DerivedOverridesBase()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var basePrefab = new EntityPrefab()
            .With(new Health { Current = 100, Max = 100 });
        var derivedPrefab = new EntityPrefab()
            .Extends("BaseEntity")
            .With(new Health { Current = 500, Max = 500 });

        world.RegisterPrefab("BaseEntity", basePrefab);
        world.RegisterPrefab("Boss", derivedPrefab);

        var entity = world.SpawnFromPrefab("Boss").Build();

        Assert.Equal(500, world.Get<Health>(entity).Current);
        Assert.Equal(500, world.Get<Health>(entity).Max);
    }

    [Fact]
    public void SpawnFromPrefab_WithMultiLevelInheritance_ResolvesCorrectly()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var level0 = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 });
        var level1 = new EntityPrefab()
            .Extends("Level0")
            .With(new Health { Current = 50, Max = 50 });
        var level2 = new EntityPrefab()
            .Extends("Level1")
            .With(new Damage { Amount = 5 });

        world.RegisterPrefab("Level0", level0);
        world.RegisterPrefab("Level1", level1);
        world.RegisterPrefab("Level2", level2);

        var entity = world.SpawnFromPrefab("Level2").Build();

        Assert.True(world.Has<Position>(entity));
        Assert.True(world.Has<Health>(entity));
        Assert.True(world.Has<Damage>(entity));
    }

    [Fact]
    public void SpawnFromPrefab_WithInheritedTags_InheritsTags()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var basePrefab = new EntityPrefab()
            .WithTag<EnemyTag>();
        var derivedPrefab = new EntityPrefab()
            .Extends("BaseEnemy")
            .WithTag<BossTag>();

        world.RegisterPrefab("BaseEnemy", basePrefab);
        world.RegisterPrefab("Boss", derivedPrefab);

        var entity = world.SpawnFromPrefab("Boss").Build();

        Assert.True(world.Has<EnemyTag>(entity));
        Assert.True(world.Has<BossTag>(entity));
    }

    [Fact]
    public void SpawnFromPrefab_WithMissingBasePrefab_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var derivedPrefab = new EntityPrefab()
            .Extends("NonExistentBase")
            .With(new Position { X = 0, Y = 0 });

        world.RegisterPrefab("Derived", derivedPrefab);

        var ex = Assert.Throws<InvalidOperationException>(() => world.SpawnFromPrefab("Derived"));
        Assert.Contains("Base prefab 'NonExistentBase' not found", ex.Message);
    }

    [Fact]
    public void SpawnFromPrefab_WithCircularInheritance_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var prefabA = new EntityPrefab()
            .Extends("PrefabB")
            .With(new Position { X = 0, Y = 0 });
        var prefabB = new EntityPrefab()
            .Extends("PrefabA")
            .With(new Health { Current = 100, Max = 100 });

        world.RegisterPrefab("PrefabA", prefabA);
        world.RegisterPrefab("PrefabB", prefabB);

        var ex = Assert.Throws<InvalidOperationException>(() => world.SpawnFromPrefab("PrefabA"));
        Assert.Contains("Circular inheritance", ex.Message);
    }

    [Fact]
    public void SpawnFromPrefab_WithSelfReference_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var prefab = new EntityPrefab()
            .Extends("Self")
            .With(new Position { X = 0, Y = 0 });

        world.RegisterPrefab("Self", prefab);

        var ex = Assert.Throws<InvalidOperationException>(() => world.SpawnFromPrefab("Self"));
        Assert.Contains("Circular inheritance", ex.Message);
    }

    #endregion

    #region EntityPrefab Builder Tests

    [Fact]
    public void EntityPrefab_With_ReplacesExistingComponent()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 10, Y = 10 })
            .With(new Position { X = 20, Y = 30 });

        world.RegisterPrefab("Test", prefab);
        var entity = world.SpawnFromPrefab("Test").Build();

        Assert.Equal(20, world.Get<Position>(entity).X);
        Assert.Equal(30, world.Get<Position>(entity).Y);
    }

    [Fact]
    public void EntityPrefab_Extends_WithNullName_ThrowsArgumentNullException()
    {
        var prefab = new EntityPrefab();

        Assert.Throws<ArgumentNullException>(() => prefab.Extends(null!));
    }

    [Fact]
    public void EntityPrefab_FluentChaining_Works()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 3, Y = 4 })
            .With(new Health { Current = 5, Max = 6 })
            .WithTag<EnemyTag>();

        world.RegisterPrefab("Test", prefab);
        var entity = world.SpawnFromPrefab("Test").Build();

        Assert.Equal(1, world.Get<Position>(entity).X);
        Assert.Equal(3, world.Get<Velocity>(entity).X);
        Assert.Equal(5, world.Get<Health>(entity).Current);
        Assert.True(world.Has<EnemyTag>(entity));
    }

    [Fact]
    public void EntityPrefab_WithTag_CalledTwiceWithSameType_DoesNotDuplicate()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .WithTag<EnemyTag>()
            .WithTag<EnemyTag>(); // Call again - should not add duplicate

        world.RegisterPrefab("Test", prefab);
        var entity = world.SpawnFromPrefab("Test").Build();

        // Entity should still have the tag (just once)
        Assert.True(world.Has<EnemyTag>(entity));
    }

    #endregion

    #region Query Integration Tests

    [Fact]
    public void SpawnFromPrefab_EntitiesAppearInQueries()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var prefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { X = 1, Y = 1 })
            .WithTag<EnemyTag>();
        world.RegisterPrefab("Enemy", prefab);

        var entity1 = world.SpawnFromPrefab("Enemy").Build();
        var entity2 = world.SpawnFromPrefab("Enemy").Build();

        var queryResults = world.Query<Position, Velocity>().With<EnemyTag>().ToList();

        Assert.Equal(2, queryResults.Count);
        Assert.Contains(entity1, queryResults);
        Assert.Contains(entity2, queryResults);
    }

    #endregion

    #region Multiple Prefabs Test

    [Fact]
    public void World_SupportsMultipleDifferentPrefabs()
    {
        using var world = new World();
        RegisterTestComponents(world);
        var enemyPrefab = new EntityPrefab()
            .With(new Position { X = 0, Y = 0 })
            .With(new Health { Current = 50, Max = 50 })
            .WithTag<EnemyTag>();
        var playerPrefab = new EntityPrefab()
            .With(new Position { X = 100, Y = 100 })
            .With(new Health { Current = 100, Max = 100 })
            .WithTag<PlayerTag>();

        world.RegisterPrefab("Enemy", enemyPrefab);
        world.RegisterPrefab("Player", playerPrefab);

        var enemy = world.SpawnFromPrefab("Enemy").Build();
        var player = world.SpawnFromPrefab("Player").Build();

        Assert.True(world.Has<EnemyTag>(enemy));
        Assert.False(world.Has<PlayerTag>(enemy));
        Assert.Equal(50, world.Get<Health>(enemy).Current);

        Assert.True(world.Has<PlayerTag>(player));
        Assert.False(world.Has<EnemyTag>(player));
        Assert.Equal(100, world.Get<Health>(player).Current);
    }

    #endregion
}
