using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles;

/// <summary>
/// Central manager for all particle pools in the world.
/// </summary>
/// <remarks>
/// <para>
/// The ParticleManager maintains a pool of particle data for each emitter entity.
/// Pools are created when <see cref="ParticleEmitter"/> components are added
/// and disposed when they are removed.
/// </para>
/// <para>
/// Access the manager through the world extension:
/// <code>
/// var manager = world.GetExtension&lt;ParticleManager&gt;();
/// var pool = manager.GetPool(emitterEntity);
/// </code>
/// </para>
/// </remarks>
public sealed class ParticleManager : IDisposable
{
    private readonly ParticlesConfig config;

    // Entity ID -> Pool mapping
    private readonly Dictionary<int, ParticlePool> pools = [];
    private readonly Dictionary<int, Entity> poolEntities = [];
    private bool isDisposed;

    /// <summary>
    /// Creates a new particle manager.
    /// </summary>
    /// <param name="world">The ECS world (unused, reserved for future use).</param>
    /// <param name="config">The particle configuration.</param>
    internal ParticleManager(IWorld world, ParticlesConfig config)
    {
        _ = world; // Reserved for future use
        this.config = config;
    }

    /// <summary>
    /// Gets the particle system configuration.
    /// </summary>
    public ParticlesConfig Config => config;

    /// <summary>
    /// Gets the number of registered emitters.
    /// </summary>
    public int EmitterCount => pools.Count;

    /// <summary>
    /// Gets the total number of active particles across all emitters.
    /// </summary>
    public int TotalActiveParticles
    {
        get
        {
            var total = 0;
            foreach (var pool in pools.Values)
            {
                total += pool.ActiveCount;
            }
            return total;
        }
    }

    /// <summary>
    /// Registers an emitter entity and creates its particle pool.
    /// </summary>
    /// <param name="entity">The emitter entity.</param>
    /// <param name="emitter">The emitter configuration.</param>
    internal void RegisterEmitter(Entity entity, in ParticleEmitter emitter)
    {
        if (isDisposed)
        {
            return;
        }

        if (pools.ContainsKey(entity.Id))
        {
            return;
        }

        if (pools.Count >= config.MaxEmitters)
        {
            return;
        }

        var pool = new ParticlePool(config.InitialPoolCapacity);
        pools[entity.Id] = pool;
        poolEntities[entity.Id] = entity;
    }

    /// <summary>
    /// Unregisters an emitter entity and disposes its particle pool.
    /// </summary>
    /// <param name="entity">The emitter entity.</param>
    internal void UnregisterEmitter(Entity entity)
    {
        if (pools.TryGetValue(entity.Id, out var pool))
        {
            pool.Dispose();
            pools.Remove(entity.Id);
            poolEntities.Remove(entity.Id);
        }
    }

    /// <summary>
    /// Gets the particle pool for an emitter entity.
    /// </summary>
    /// <param name="entity">The emitter entity.</param>
    /// <returns>The particle pool, or null if not found.</returns>
    public ParticlePool? GetPool(Entity entity)
    {
        return pools.TryGetValue(entity.Id, out var pool) ? pool : null;
    }

    /// <summary>
    /// Checks if an emitter is registered.
    /// </summary>
    /// <param name="entity">The emitter entity.</param>
    /// <returns>True if the emitter is registered.</returns>
    public bool HasPool(Entity entity)
    {
        return pools.ContainsKey(entity.Id);
    }

    /// <summary>
    /// Iterates all active pools.
    /// </summary>
    /// <returns>An enumerable of (entity, pool) pairs.</returns>
    public IEnumerable<(Entity Entity, ParticlePool Pool)> GetAllPools()
    {
        foreach (var kvp in pools)
        {
            if (poolEntities.TryGetValue(kvp.Key, out var entity))
            {
                yield return (entity, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Clears all particles from all pools.
    /// </summary>
    public void ClearAll()
    {
        foreach (var pool in pools.Values)
        {
            pool.Clear();
        }
    }

    /// <summary>
    /// Disposes the manager and all pools.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        foreach (var pool in pools.Values)
        {
            pool.Dispose();
        }
        pools.Clear();
        poolEntities.Clear();
    }
}
