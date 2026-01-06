using Fixtures = KeenEyes.Testing.Fixtures;

namespace KeenEyes.Testing.Tests;

/// <summary>
/// Tests for the test fixtures (CommonComponents, EntityPresets, SystemPresets).
/// </summary>
public sealed class FixtureTests
{
    #region CommonComponents Tests

    [Fact]
    public void TestPosition_Create_SetsCoordinates()
    {
        var pos = Fixtures.TestPosition.Create(10f, 20f);

        Assert.Equal(10f, pos.X);
        Assert.Equal(20f, pos.Y);
    }

    [Fact]
    public void TestPosition_Zero_ReturnsOrigin()
    {
        var pos = Fixtures.TestPosition.Zero;

        Assert.Equal(0f, pos.X);
        Assert.Equal(0f, pos.Y);
    }

    [Fact]
    public void TestPosition3D_Create_SetsCoordinates()
    {
        var pos = Fixtures.TestPosition3D.Create(10f, 20f, 30f);

        Assert.Equal(10f, pos.X);
        Assert.Equal(20f, pos.Y);
        Assert.Equal(30f, pos.Z);
    }

    [Fact]
    public void TestVelocity_Create_SetsVelocity()
    {
        var vel = Fixtures.TestVelocity.Create(5f, -3f);

        Assert.Equal(5f, vel.VX);
        Assert.Equal(-3f, vel.VY);
    }

    [Fact]
    public void TestHealth_Create_SetsValues()
    {
        var health = Fixtures.TestHealth.Create(50, 100);

        Assert.Equal(50, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void TestHealth_Full_SetsBothToMax()
    {
        var health = Fixtures.TestHealth.Full(100);

        Assert.Equal(100, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void TestHealth_Percentage_CalculatesCorrectly()
    {
        var health = Fixtures.TestHealth.Create(25, 100);

        Assert.Equal(0.25f, health.Percentage);
    }

    [Fact]
    public void TestHealth_IsAlive_TrueWhenHealthPositive()
    {
        var alive = Fixtures.TestHealth.Create(1, 100);
        var dead = Fixtures.TestHealth.Create(0, 100);

        Assert.True(alive.IsAlive);
        Assert.False(dead.IsAlive);
    }

    [Fact]
    public void TestLifetime_HasExpired_TrueWhenZeroOrNegative()
    {
        var active = Fixtures.TestLifetime.Create(1f);
        var expired = Fixtures.TestLifetime.Create(0f);
        var negative = Fixtures.TestLifetime.Create(-1f);

        Assert.False(active.HasExpired);
        Assert.True(expired.HasExpired);
        Assert.True(negative.HasExpired);
    }

    [Fact]
    public void TestScale_One_ReturnsUnitScale()
    {
        var scale = Fixtures.TestScale.One;

        Assert.Equal(1.0f, scale.Value);
    }

    #endregion

    #region EntityPresets Tests

    [Fact]
    public void Player_CreatesEntityWithPlayerTag()
    {
        using var world = new World();

        var player = Fixtures.EntityPresets.Player(world).Build();

        Assert.True(world.IsAlive(player));
        Assert.True(world.Has<Fixtures.PlayerTag>(player));
    }

    [Fact]
    public void Player_HasDefaultHealth()
    {
        using var world = new World();

        var player = Fixtures.EntityPresets.Player(world).Build();
        var health = world.Get<Fixtures.TestHealth>(player);

        Assert.Equal(100, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void Player_HasDefaultSpeed()
    {
        using var world = new World();

        var player = Fixtures.EntityPresets.Player(world).Build();
        var speed = world.Get<Fixtures.TestSpeed>(player);

        Assert.Equal(10f, speed.Value);
    }

    [Fact]
    public void Enemy_CreatesEntityWithEnemyTag()
    {
        using var world = new World();

        var enemy = Fixtures.EntityPresets.Enemy(world).Build();

        Assert.True(world.IsAlive(enemy));
        Assert.True(world.Has<Fixtures.EnemyTag>(enemy));
    }

    [Fact]
    public void Enemy_HasDefaultDamage()
    {
        using var world = new World();

        var enemy = Fixtures.EntityPresets.Enemy(world).Build();
        var damage = world.Get<Fixtures.TestDamage>(enemy);

        Assert.Equal(10, damage.Amount);
    }

    [Fact]
    public void Projectile_CreatesEntityWithProjectileTag()
    {
        using var world = new World();

        var projectile = Fixtures.EntityPresets.Projectile(world).Build();

        Assert.True(world.IsAlive(projectile));
        Assert.True(world.Has<Fixtures.ProjectileTag>(projectile));
    }

    [Fact]
    public void Projectile_HasLifetime()
    {
        using var world = new World();

        var projectile = Fixtures.EntityPresets.Projectile(world).Build();
        var lifetime = world.Get<Fixtures.TestLifetime>(projectile);

        Assert.Equal(5f, lifetime.Remaining);
    }

    [Fact]
    public void Pickup_CreatesEntityWithPickupTag()
    {
        using var world = new World();

        var pickup = Fixtures.EntityPresets.Pickup(world).Build();

        Assert.True(world.IsAlive(pickup));
        Assert.True(world.Has<Fixtures.PickupTag>(pickup));
    }

    [Fact]
    public void EntityPresetBuilder_WithName_SetsEntityName()
    {
        using var world = new World();

        var entity = Fixtures.EntityPresets.Player(world)
            .WithName("Hero")
            .Build();

        Assert.Equal("Hero", world.GetName(entity));
    }

    [Fact]
    public void EntityPresetBuilder_AtPosition_SetsPosition()
    {
        using var world = new World();

        var entity = Fixtures.EntityPresets.Player(world)
            .AtPosition(100f, 200f)
            .Build();
        var pos = world.Get<Fixtures.TestPosition>(entity);

        Assert.Equal(100f, pos.X);
        Assert.Equal(200f, pos.Y);
    }

    [Fact]
    public void EntityPresetBuilder_WithVelocity_AddsVelocityComponent()
    {
        using var world = new World();

        var entity = Fixtures.EntityPresets.MovingEntity(world)
            .WithVelocity(5f, 10f)
            .Build();
        var vel = world.Get<Fixtures.TestVelocity>(entity);

        Assert.Equal(5f, vel.VX);
        Assert.Equal(10f, vel.VY);
    }

    [Fact]
    public void EntityPresetBuilder_WithHealth_OverridesDefault()
    {
        using var world = new World();

        var entity = Fixtures.EntityPresets.Player(world)
            .WithHealth(50)
            .Build();
        var health = world.Get<Fixtures.TestHealth>(entity);

        Assert.Equal(50, health.Current);
        Assert.Equal(50, health.Max);
    }

    [Fact]
    public void EntityPresetBuilder_WithHealth_DifferentCurrentAndMax()
    {
        using var world = new World();

        var entity = Fixtures.EntityPresets.Player(world)
            .WithHealth(25, 50)
            .Build();
        var health = world.Get<Fixtures.TestHealth>(entity);

        Assert.Equal(25, health.Current);
        Assert.Equal(50, health.Max);
    }

    [Fact]
    public void EntityPresetBuilder_OnTeam_SetsTeam()
    {
        using var world = new World();

        var entity = Fixtures.EntityPresets.Player(world)
            .OnTeam(2)
            .Build();
        var team = world.Get<Fixtures.TestTeam>(entity);

        Assert.Equal(2, team.Id);
    }

    [Fact]
    public void EntityPresetBuilder_WithTag_AddsCustomTag()
    {
        using var world = new World();

        var entity = Fixtures.EntityPresets.Player(world)
            .WithTag<Fixtures.InvulnerableTag>()
            .Build();

        Assert.True(world.Has<Fixtures.InvulnerableTag>(entity));
    }

    [Fact]
    public void CreateEnemies_CreatesMultipleEnemies()
    {
        using var world = new World();

        var enemies = Fixtures.EntityPresets.CreateEnemies(world, 5).Build();

        Assert.Equal(5, enemies.Length);
        foreach (var enemy in enemies)
        {
            Assert.True(world.Has<Fixtures.EnemyTag>(enemy));
        }
    }

    [Fact]
    public void BatchEntityBuilder_WithModifier_AppliesPerIndex()
    {
        using var world = new World();

        var enemies = Fixtures.EntityPresets.CreateEnemies(world, 3)
            .WithModifier((b, i) => b.WithHealth(100 + (i * 10)))
            .Build();

        Assert.Equal(100, world.Get<Fixtures.TestHealth>(enemies[0]).Current);
        Assert.Equal(110, world.Get<Fixtures.TestHealth>(enemies[1]).Current);
        Assert.Equal(120, world.Get<Fixtures.TestHealth>(enemies[2]).Current);
    }

    [Fact]
    public void BatchEntityBuilder_InGrid_PositionsCorrectly()
    {
        using var world = new World();

        var enemies = Fixtures.EntityPresets.CreateEnemies(world, 4)
            .InGrid(2, 10f)
            .Build();

        Assert.Equal(0f, world.Get<Fixtures.TestPosition>(enemies[0]).X);
        Assert.Equal(0f, world.Get<Fixtures.TestPosition>(enemies[0]).Y);
        Assert.Equal(10f, world.Get<Fixtures.TestPosition>(enemies[1]).X);
        Assert.Equal(0f, world.Get<Fixtures.TestPosition>(enemies[1]).Y);
        Assert.Equal(0f, world.Get<Fixtures.TestPosition>(enemies[2]).X);
        Assert.Equal(10f, world.Get<Fixtures.TestPosition>(enemies[2]).Y);
        Assert.Equal(10f, world.Get<Fixtures.TestPosition>(enemies[3]).X);
        Assert.Equal(10f, world.Get<Fixtures.TestPosition>(enemies[3]).Y);
    }

    [Fact]
    public void BatchEntityBuilder_InLine_PositionsHorizontally()
    {
        using var world = new World();

        var enemies = Fixtures.EntityPresets.CreateEnemies(world, 3)
            .InLine(5f, horizontal: true)
            .Build();

        Assert.Equal(0f, world.Get<Fixtures.TestPosition>(enemies[0]).X);
        Assert.Equal(5f, world.Get<Fixtures.TestPosition>(enemies[1]).X);
        Assert.Equal(10f, world.Get<Fixtures.TestPosition>(enemies[2]).X);
    }

    [Fact]
    public void BatchEntityBuilder_WithAlternatingTeams_AlternatesCorrectly()
    {
        using var world = new World();

        var enemies = Fixtures.EntityPresets.CreateEnemies(world, 4)
            .WithAlternatingTeams(1, 2)
            .Build();

        Assert.Equal(1, world.Get<Fixtures.TestTeam>(enemies[0]).Id);
        Assert.Equal(2, world.Get<Fixtures.TestTeam>(enemies[1]).Id);
        Assert.Equal(1, world.Get<Fixtures.TestTeam>(enemies[2]).Id);
        Assert.Equal(2, world.Get<Fixtures.TestTeam>(enemies[3]).Id);
    }

    #endregion

    #region SystemPresets Tests

    [Fact]
    public void TestMovementSystem_UpdatesPosition()
    {
        using var world = new World();
        var system = new Fixtures.TestMovementSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(Fixtures.TestPosition.Create(0f, 0f))
            .With(Fixtures.TestVelocity.Create(10f, 5f))
            .Build();

        world.Update(1f);

        var pos = world.Get<Fixtures.TestPosition>(entity);
        Assert.Equal(10f, pos.X);
        Assert.Equal(5f, pos.Y);
    }

    [Fact]
    public void TestMovementSystem_ScalesByDeltaTime()
    {
        using var world = new World();
        var system = new Fixtures.TestMovementSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(Fixtures.TestPosition.Create(0f, 0f))
            .With(Fixtures.TestVelocity.Create(10f, 10f))
            .Build();

        world.Update(0.5f);

        var pos = world.Get<Fixtures.TestPosition>(entity);
        Assert.Equal(5f, pos.X);
        Assert.Equal(5f, pos.Y);
    }

    [Fact]
    public void TestLifetimeSystem_DecrementsLifetime()
    {
        using var world = new World();
        var system = new Fixtures.TestLifetimeSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(Fixtures.TestLifetime.Create(5f))
            .Build();

        world.Update(2f);

        var lifetime = world.Get<Fixtures.TestLifetime>(entity);
        Assert.Equal(3f, lifetime.Remaining);
    }

    [Fact]
    public void TestLifetimeSystem_AddsDeadTagWhenExpired()
    {
        using var world = new World();
        var system = new Fixtures.TestLifetimeSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(Fixtures.TestLifetime.Create(1f))
            .Build();

        world.Update(2f);

        Assert.True(world.Has<Fixtures.DeadTag>(entity));
    }

    [Fact]
    public void TestDespawnSystem_DespawnsDeadEntities()
    {
        using var world = new World();
        var system = new Fixtures.TestDespawnSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(new Fixtures.DeadTag())
            .Build();

        world.Update(0f);

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void TestDamageSystem_AppliesDamage()
    {
        using var world = new World();
        var system = new Fixtures.TestDamageSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(Fixtures.TestHealth.Create(100, 100))
            .Build();

        system.QueueDamage(entity, 30);
        world.Update(0f);

        var health = world.Get<Fixtures.TestHealth>(entity);
        Assert.Equal(70, health.Current);
    }

    [Fact]
    public void TestDamageSystem_MarksDeadWhenHealthZero()
    {
        using var world = new World();
        var system = new Fixtures.TestDamageSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(Fixtures.TestHealth.Create(50, 100))
            .Build();

        system.QueueDamage(entity, 100);
        world.Update(0f);

        Assert.True(world.Has<Fixtures.DeadTag>(entity));
        Assert.Equal(0, world.Get<Fixtures.TestHealth>(entity).Current);
    }

    [Fact]
    public void TestDamageSystem_ClearsPendingAfterUpdate()
    {
        using var world = new World();
        var system = new Fixtures.TestDamageSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(Fixtures.TestHealth.Create(100, 100))
            .Build();

        system.QueueDamage(entity, 10);
        Assert.Equal(1, system.PendingCount);

        world.Update(0f);

        Assert.Equal(0, system.PendingCount);
    }

    [Fact]
    public void TestCountingSystem_CountsUpdates()
    {
        using var world = new World();
        var system = new Fixtures.TestCountingSystem();
        world.AddSystem(system);

        world.Update(1f);
        world.Update(1f);
        world.Update(1f);

        Assert.Equal(3, system.UpdateCount);
    }

    [Fact]
    public void TestCountingSystem_AccumulatesDeltaTime()
    {
        using var world = new World();
        var system = new Fixtures.TestCountingSystem();
        world.AddSystem(system);

        world.Update(0.5f);
        world.Update(1.0f);
        world.Update(0.25f);

        Assert.Equal(1.75f, system.TotalDeltaTime);
        Assert.Equal(0.25f, system.LastDeltaTime);
    }

    [Fact]
    public void TestCountingSystem_ResetClearsCounters()
    {
        using var world = new World();
        var system = new Fixtures.TestCountingSystem();
        world.AddSystem(system);

        world.Update(1f);
        system.Reset();

        Assert.Equal(0, system.UpdateCount);
        Assert.Equal(0f, system.TotalDeltaTime);
    }

    [Fact]
    public void TestEntityCountingSystem_CountsMatchingEntities()
    {
        using var world = new World();
        var system = new Fixtures.TestEntityCountingSystem<Fixtures.TestPosition>();
        world.AddSystem(system);

        world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        world.Spawn().With(Fixtures.TestVelocity.Zero).Build();

        world.Update(0f);

        Assert.Equal(2, system.LastEntityCount);
    }

    [Fact]
    public void TestEntityRecordingSystem_RecordsProcessedEntities()
    {
        using var world = new World();
        var system = new Fixtures.TestEntityRecordingSystem<Fixtures.TestPosition>();
        world.AddSystem(system);

        var e1 = world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        var e2 = world.Spawn().With(Fixtures.TestPosition.Zero).Build();

        world.Update(0f);

        Assert.Equal(2, system.LastProcessed.Count);
        Assert.Contains(e1, system.LastProcessed);
        Assert.Contains(e2, system.LastProcessed);
    }

    [Fact]
    public void TestEntityRecordingSystem_KeepsHistory()
    {
        using var world = new World();
        var system = new Fixtures.TestEntityRecordingSystem<Fixtures.TestPosition>();
        world.AddSystem(system);

        world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        world.Update(0f);

        world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        world.Update(0f);

        Assert.Equal(2, system.UpdateCount);
        Assert.Single(system.History[0]);
        Assert.Equal(2, system.History[1].Count);
    }

    [Fact]
    public void TestActionSystem_ExecutesAction()
    {
        using var world = new World();
        var actionCalled = false;
        var system = new Fixtures.TestActionSystem((_, _) => actionCalled = true);
        world.AddSystem(system);

        world.Update(1f);

        Assert.True(actionCalled);
    }

    [Fact]
    public void TestActionSystem_ReceivesCorrectParameters()
    {
        using var world = new World();
        IWorld? receivedWorld = null;
        float receivedDeltaTime = 0f;

        var system = new Fixtures.TestActionSystem((w, dt) =>
        {
            receivedWorld = w;
            receivedDeltaTime = dt;
        });
        world.AddSystem(system);

        world.Update(0.5f);

        Assert.Same(world, receivedWorld);
        Assert.Equal(0.5f, receivedDeltaTime);
    }

    [Fact]
    public void TestToggleableSystem_TracksEnabledUpdates()
    {
        using var world = new World();
        var system = new Fixtures.TestToggleableSystem();
        world.AddSystem(system);

        world.Update(1f);
        world.Update(1f);

        Assert.Equal(2, system.EnabledUpdateCount);
    }

    [Fact]
    public void TestThrowingSystem_ThrowsOnFirstUpdate()
    {
        using var world = new World();
        var system = new Fixtures.TestThrowingSystem();
        world.AddSystem(system);

        Assert.Throws<InvalidOperationException>(() => world.Update(1f));
    }

    [Fact]
    public void TestThrowingSystem_ThrowsCustomException()
    {
        using var world = new World();
        var customException = new ArgumentException("Custom error");
        var system = new Fixtures.TestThrowingSystem(customException);
        world.AddSystem(system);

        var thrown = Assert.Throws<ArgumentException>(() => world.Update(1f));
        Assert.Same(customException, thrown);
    }

    [Fact]
    public void TestThrowingSystem_ThrowsOnSpecificUpdate()
    {
        using var world = new World();
        var system = new Fixtures.TestThrowingSystem(null, 3);
        world.AddSystem(system);

        world.Update(1f);
        world.Update(1f);

        Assert.Equal(2, system.CompletedUpdates);
        Assert.Throws<InvalidOperationException>(() => world.Update(1f));
    }

    [Fact]
    public void TestComponentTrackingSystem_DetectsAddedEntities()
    {
        using var world = new World();
        var system = new Fixtures.TestComponentTrackingSystem<Fixtures.TestPosition>();
        world.AddSystem(system);

        world.Update(0f);

        world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        world.Update(0f);

        Assert.Single(system.AddedEntities);
    }

    [Fact]
    public void TestComponentTrackingSystem_DetectsChangedComponents()
    {
        using var world = new World();
        var system = new Fixtures.TestComponentTrackingSystem<Fixtures.TestPosition>();
        world.AddSystem(system);

        var entity = world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        world.Update(0f);

        world.Get<Fixtures.TestPosition>(entity).X = 100f;
        world.Update(0f);

        Assert.Single(system.ChangedEntities);
        Assert.Equal(entity, system.ChangedEntities[0]);
    }

    [Fact]
    public void TestComponentTrackingSystem_DetectsRemovedEntities()
    {
        using var world = new World();
        var system = new Fixtures.TestComponentTrackingSystem<Fixtures.TestPosition>();
        world.AddSystem(system);

        var entity = world.Spawn().With(Fixtures.TestPosition.Zero).Build();
        world.Update(0f);

        world.Despawn(entity);
        world.Update(0f);

        Assert.Single(system.RemovedEntityIds);
    }

    #endregion
}
