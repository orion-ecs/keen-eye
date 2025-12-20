using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the ParticleManager class.
/// </summary>
public class ParticleManagerTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Installation Tests

    [Fact]
    public void Install_WithDefaultConfig_Succeeds()
    {
        world = new World();

        world.InstallPlugin(new ParticlesPlugin());

        Assert.True(world.TryGetExtension<ParticleManager>(out _));
    }

    [Fact]
    public void Install_WithCustomConfig_Succeeds()
    {
        world = new World();

        var config = new ParticlesConfig
        {
            MaxParticlesPerEmitter = 5000,
            MaxEmitters = 50,
            InitialPoolCapacity = 256
        };

        world.InstallPlugin(new ParticlesPlugin(config));

        Assert.True(world.TryGetExtension<ParticleManager>(out var manager));
        Assert.NotNull(manager);
        Assert.Equal(5000, manager.Config.MaxParticlesPerEmitter);
    }

    #endregion

    #region Emitter Registration Tests

    [Fact]
    public void AddEmitter_CreatesPool()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var entity = world.Spawn()
            .With(ParticleEmitter.Default)
            .Build();

        Assert.True(manager.HasPool(entity));
        Assert.Equal(1, manager.EmitterCount);
    }

    [Fact]
    public void RemoveEmitter_RemovesPool()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var entity = world.Spawn()
            .With(ParticleEmitter.Default)
            .Build();

        Assert.True(manager.HasPool(entity));

        world.Remove<ParticleEmitter>(entity);

        Assert.False(manager.HasPool(entity));
        Assert.Equal(0, manager.EmitterCount);
    }

    [Fact]
    public void DespawnEntity_RemovesPool()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var entity = world.Spawn()
            .With(ParticleEmitter.Default)
            .Build();

        Assert.True(manager.HasPool(entity));

        world.Despawn(entity);

        Assert.False(manager.HasPool(entity));
    }

    #endregion

    #region Pool Access Tests

    [Fact]
    public void GetPool_ExistingEmitter_ReturnsPool()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var entity = world.Spawn()
            .With(ParticleEmitter.Default)
            .Build();

        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        Assert.IsType<ParticlePool>(pool);
    }

    [Fact]
    public void GetPool_NonExistentEmitter_ReturnsNull()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var entity = world.Spawn().Build(); // No emitter

        var pool = manager.GetPool(entity);

        Assert.Null(pool);
    }

    #endregion

    #region GetAllPools Tests

    [Fact]
    public void GetAllPools_ReturnsAllPools()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        _ = world.Spawn().With(ParticleEmitter.Default).Build();
        _ = world.Spawn().With(ParticleEmitter.Default).Build();
        _ = world.Spawn().With(ParticleEmitter.Default).Build();

        var pools = manager.GetAllPools().ToList();

        Assert.Equal(3, pools.Count);
    }

    [Fact]
    public void GetAllPools_EmptyWhenNoEmitters()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var pools = manager.GetAllPools().ToList();

        Assert.Empty(pools);
    }

    #endregion

    #region ClearAll Tests

    [Fact]
    public void ClearAll_ClearsAllPools()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var entity = world.Spawn().With(ParticleEmitter.Default).Build();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);

        // Allocate some particles
        pool.Allocate();
        pool.Allocate();
        pool.Allocate();
        Assert.Equal(3, pool.ActiveCount);

        manager.ClearAll();

        Assert.Equal(0, pool.ActiveCount);
    }

    #endregion

    #region TotalActiveParticles Tests

    [Fact]
    public void TotalActiveParticles_ReturnsSum()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        var entity1 = world.Spawn().With(ParticleEmitter.Default).Build();
        var entity2 = world.Spawn().With(ParticleEmitter.Default).Build();

        var pool1 = manager.GetPool(entity1);
        var pool2 = manager.GetPool(entity2);

        Assert.NotNull(pool1);
        Assert.NotNull(pool2);

        pool1.Allocate();
        pool1.Allocate();
        pool2.Allocate();
        pool2.Allocate();
        pool2.Allocate();

        Assert.Equal(5, manager.TotalActiveParticles);
    }

    [Fact]
    public void TotalActiveParticles_ZeroWhenEmpty()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var manager = world.GetExtension<ParticleManager>();

        Assert.Equal(0, manager.TotalActiveParticles);
    }

    #endregion

    #region MaxEmitters Tests

    [Fact]
    public void MaxEmitters_LimitsEmitterCount()
    {
        world = new World();

        var config = new ParticlesConfig
        {
            MaxEmitters = 2
        };

        world.InstallPlugin(new ParticlesPlugin(config));

        var manager = world.GetExtension<ParticleManager>();

        world.Spawn().With(ParticleEmitter.Default).Build();
        world.Spawn().With(ParticleEmitter.Default).Build();
        world.Spawn().With(ParticleEmitter.Default).Build(); // Should be ignored

        Assert.Equal(2, manager.EmitterCount);
    }

    #endregion
}
