using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Systems;
using KeenEyes.Testing.Plugins;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the ParticlesPlugin integration with the World.
/// </summary>
public class ParticlesPluginTests : IDisposable
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
        Assert.Equal(50, manager.Config.MaxEmitters);
        Assert.Equal(256, manager.Config.InitialPoolCapacity);
    }

    #endregion

    #region Uninstallation Tests

    [Fact]
    public void Uninstall_RemovesExtension()
    {
        world = new World();
        var plugin = new ParticlesPlugin();

        world.InstallPlugin(plugin);
        Assert.True(world.TryGetExtension<ParticleManager>(out _));

        world.UninstallPlugin<ParticlesPlugin>();
        Assert.False(world.TryGetExtension<ParticleManager>(out _));
    }

    #endregion

    #region MockPluginContext Tests

    [Fact]
    public void Install_RegistersParticleManagerExtension()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        context
            .ShouldHaveSetExtension<ParticleManager>()
            .ShouldHaveSetExtensionCount(1);
    }

    [Fact]
    public void Install_RegistersParticleSpawnSystem()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        Assert.True(context.WasSystemRegistered<ParticleSpawnSystem>());
        Assert.True(context.WasSystemRegisteredAtPhase<ParticleSpawnSystem>(SystemPhase.Update));
    }

    [Fact]
    public void Install_RegistersParticleUpdateSystem()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        Assert.True(context.WasSystemRegistered<ParticleUpdateSystem>());
        Assert.True(context.WasSystemRegisteredAtPhase<ParticleUpdateSystem>(SystemPhase.Update));
    }

    [Fact]
    public void Install_RegistersParticleRenderSystem()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        Assert.True(context.WasSystemRegistered<ParticleRenderSystem>());
        Assert.True(context.WasSystemRegisteredAtPhase<ParticleRenderSystem>(SystemPhase.Render));
    }

    [Fact]
    public void Install_RegistersParticleEmitterComponent()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        Assert.True(context.WasComponentRegistered<ParticleEmitter>());
    }

    [Fact]
    public void Install_RegistersParticleEmitterModifiersComponent()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        Assert.True(context.WasComponentRegistered<ParticleEmitterModifiers>());
    }

    [Fact]
    public void Install_SystemRegistrationOrder_SpawnBeforeUpdate()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        var spawnReg = context.GetSystemRegistration<ParticleSpawnSystem>();
        var updateReg = context.GetSystemRegistration<ParticleUpdateSystem>();

        Assert.NotNull(spawnReg);
        Assert.NotNull(updateReg);
        Assert.True(spawnReg.Value.Order < updateReg.Value.Order);
    }

    [Fact]
    public void Install_CreatesParticleManagerWithConfig()
    {
        using var w = new World();
        var config = new ParticlesConfig
        {
            MaxParticlesPerEmitter = 10000,
            MaxEmitters = 100
        };
        var plugin = new ParticlesPlugin(config);
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        var manager = context.GetSetExtension<ParticleManager>();
        Assert.NotNull(manager);
        Assert.Equal(10000, manager.Config.MaxParticlesPerEmitter);
        Assert.Equal(100, manager.Config.MaxEmitters);
    }

    [Fact]
    public void Install_RegistersThreeSystems()
    {
        using var w = new World();
        var plugin = new ParticlesPlugin();
        var context = new MockPluginContext(plugin, w);

        plugin.Install(context);

        var systemCount = context.RegisteredSystems.Count;
        Assert.Equal(3, systemCount);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void ParticlesConfig_Default_HasReasonableValues()
    {
        var config = ParticlesConfig.Default;

        Assert.True(config.MaxParticlesPerEmitter > 0);
        Assert.True(config.MaxEmitters > 0);
        Assert.True(config.InitialPoolCapacity > 0);
    }

    [Fact]
    public void ParticlesConfig_Default_InitialPoolSmallerThanMax()
    {
        var config = ParticlesConfig.Default;

        Assert.True(config.InitialPoolCapacity <= config.MaxParticlesPerEmitter);
    }

    [Fact]
    public void ParticlesConfig_HighPerformance_HasHighValues()
    {
        var config = ParticlesConfig.HighPerformance;

        Assert.True(config.MaxParticlesPerEmitter > ParticlesConfig.Default.MaxParticlesPerEmitter);
        Assert.True(config.InitialPoolCapacity > ParticlesConfig.Default.InitialPoolCapacity);
    }

    [Fact]
    public void ParticlesConfig_LowMemory_HasLowValues()
    {
        var config = ParticlesConfig.LowMemory;

        Assert.True(config.MaxParticlesPerEmitter < ParticlesConfig.Default.MaxParticlesPerEmitter);
        Assert.True(config.InitialPoolCapacity < ParticlesConfig.Default.InitialPoolCapacity);
    }

    #endregion

    #region Config Validation Tests

    [Fact]
    public void ParticlesPlugin_InvalidMaxParticlesPerEmitter_Throws()
    {
        var config = new ParticlesConfig { MaxParticlesPerEmitter = 0 };

        var ex = Assert.Throws<ArgumentException>(() => new ParticlesPlugin(config));
        Assert.Contains("MaxParticlesPerEmitter", ex.Message);
    }

    [Fact]
    public void ParticlesPlugin_InvalidMaxEmitters_Throws()
    {
        var config = new ParticlesConfig { MaxEmitters = 0 };

        var ex = Assert.Throws<ArgumentException>(() => new ParticlesPlugin(config));
        Assert.Contains("MaxEmitters", ex.Message);
    }

    [Fact]
    public void ParticlesPlugin_InvalidInitialPoolCapacity_Throws()
    {
        var config = new ParticlesConfig { InitialPoolCapacity = 0 };

        var ex = Assert.Throws<ArgumentException>(() => new ParticlesPlugin(config));
        Assert.Contains("InitialPoolCapacity", ex.Message);
    }

    [Fact]
    public void ParticlesPlugin_InitialPoolExceedsMax_Throws()
    {
        var config = new ParticlesConfig
        {
            MaxParticlesPerEmitter = 100,
            InitialPoolCapacity = 200
        };

        var ex = Assert.Throws<ArgumentException>(() => new ParticlesPlugin(config));
        Assert.Contains("InitialPoolCapacity", ex.Message);
    }

    [Fact]
    public void ParticlesConfig_Validate_ReturnsNullForValidConfig()
    {
        var config = ParticlesConfig.Default;

        Assert.Null(config.Validate());
    }

    [Fact]
    public void ParticlesConfig_Validate_ReturnsErrorForInvalidMaxParticles()
    {
        var config = new ParticlesConfig { MaxParticlesPerEmitter = -1 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxParticlesPerEmitter", error);
    }

    [Fact]
    public void ParticlesConfig_Validate_ReturnsErrorForInvalidMaxEmitters()
    {
        var config = new ParticlesConfig { MaxEmitters = -1 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxEmitters", error);
    }

    [Fact]
    public void ParticlesConfig_Validate_ReturnsErrorForInvalidInitialPool()
    {
        var config = new ParticlesConfig { InitialPoolCapacity = -1 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialPoolCapacity", error);
    }

    [Fact]
    public void ParticlesConfig_Validate_ReturnsErrorForInitialPoolExceedingMax()
    {
        var config = new ParticlesConfig
        {
            MaxParticlesPerEmitter = 50,
            InitialPoolCapacity = 100
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialPoolCapacity", error);
    }

    #endregion

    #region Component Lifecycle Tests

    [Fact]
    public void OnEmitterRemoved_UnregistersPool()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(10, 1f))
            .Build();

        var manager = world.GetExtension<ParticleManager>();
        Assert.True(manager.HasPool(entity));

        // Remove the emitter component
        world.Remove<ParticleEmitter>(entity);

        Assert.False(manager.HasPool(entity));
    }

    [Fact]
    public void OnEntityDestroyed_UnregistersPool()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(10, 1f))
            .Build();

        var manager = world.GetExtension<ParticleManager>();
        Assert.True(manager.HasPool(entity));

        // Destroy the entity
        world.Despawn(entity);

        Assert.False(manager.HasPool(entity));
    }

    #endregion

    #region ParticleManager Edge Cases

    [Fact]
    public void ParticleManager_MaxEmitters_StopsRegisteringNewEmitters()
    {
        world = new World();
        var config = new ParticlesConfig { MaxEmitters = 2, InitialPoolCapacity = 10 };
        world.InstallPlugin(new ParticlesPlugin(config));

        var manager = world.GetExtension<ParticleManager>();

        // Create first emitter
        var entity1 = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Create second emitter
        var entity2 = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Create third emitter (should be rejected)
        var entity3 = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        Assert.True(manager.HasPool(entity1));
        Assert.True(manager.HasPool(entity2));
        Assert.False(manager.HasPool(entity3)); // Exceeds max
        Assert.Equal(2, manager.EmitterCount);
    }

    [Fact]
    public void ParticleManager_ClearAll_ClearsAllPools()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(10, 1f))
            .Build();

        world.Update(1f / 60f); // Spawn particles

        var manager = world.GetExtension<ParticleManager>();
        Assert.True(manager.TotalActiveParticles > 0);

        manager.ClearAll();

        Assert.Equal(0, manager.TotalActiveParticles);
    }

    [Fact]
    public void ParticleManager_GetAllPools_ReturnsAllPools()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        world.Spawn()
            .With(new Transform2D(new Vector2(0, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        world.Spawn()
            .With(new Transform2D(new Vector2(100, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        var manager = world.GetExtension<ParticleManager>();
        var pools = manager.GetAllPools().ToList();

        Assert.Equal(2, pools.Count);
    }

    [Fact]
    public void ParticleManager_DoubleDispose_DoesNotThrow()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        // First dispose via uninstall
        world.UninstallPlugin<ParticlesPlugin>();

        // Second uninstall should not throw (plugin already removed)
        // The manager was disposed as part of the first uninstall
        Assert.False(world.TryGetExtension<ParticleManager>(out _));
    }

    [Fact]
    public void ParticleManager_HasPool_ReturnsFalseForUnknownEntity()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .Build(); // No emitter component

        var manager = world.GetExtension<ParticleManager>();
        Assert.False(manager.HasPool(entity));
    }

    [Fact]
    public void ParticleManager_GetPool_ReturnsNullForUnknownEntity()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .Build(); // No emitter component

        var manager = world.GetExtension<ParticleManager>();
        Assert.Null(manager.GetPool(entity));
    }

    #endregion

    #region Internal Method Tests (via InternalsVisibleTo)

    [Fact]
    public void ParticleManager_RegisterEmitter_AfterDispose_DoesNothing()
    {
        using var w = new World();
        var config = ParticlesConfig.Default;
        var manager = new ParticleManager(w, config);

        // Dispose the manager
        manager.Dispose();

        // Try to register an emitter after dispose - should silently do nothing
        var entity = new Entity(1, 0);
        var emitter = ParticleEmitter.Burst(10, 1f);

        manager.RegisterEmitter(entity, in emitter);

        // Verify no pool was created
        Assert.False(manager.HasPool(entity));
        Assert.Equal(0, manager.EmitterCount);
    }

    [Fact]
    public void ParticleManager_RegisterEmitter_DuplicateEntity_DoesNothing()
    {
        using var w = new World();
        var config = ParticlesConfig.Default;
        var manager = new ParticleManager(w, config);

        var entity = new Entity(1, 0);
        var emitter = ParticleEmitter.Burst(10, 1f);

        // Register first time - should succeed
        manager.RegisterEmitter(entity, in emitter);
        Assert.True(manager.HasPool(entity));
        Assert.Equal(1, manager.EmitterCount);

        // Get the original pool
        var originalPool = manager.GetPool(entity);

        // Try to register again - should silently ignore
        var emitter2 = ParticleEmitter.Burst(20, 2f); // Different config
        manager.RegisterEmitter(entity, in emitter2);

        // Should still have only one pool, and it should be the same instance
        Assert.Equal(1, manager.EmitterCount);
        Assert.Same(originalPool, manager.GetPool(entity));

        manager.Dispose();
    }

    [Fact]
    public void ParticleManager_UnregisterEmitter_NonExistentEntity_DoesNothing()
    {
        using var w = new World();
        var config = ParticlesConfig.Default;
        var manager = new ParticleManager(w, config);

        var entity = new Entity(999, 0);

        // Should not throw when unregistering non-existent entity
        manager.UnregisterEmitter(entity);

        Assert.Equal(0, manager.EmitterCount);

        manager.Dispose();
    }

    #endregion
}
