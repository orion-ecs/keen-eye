namespace KeenEyes.Testing.Fixtures;

/// <summary>
/// Common test systems for use in tests.
/// </summary>
/// <remarks>
/// <para>
/// These systems provide standard behaviors that are commonly needed
/// when testing ECS functionality. They can be used directly or as
/// templates for custom test systems.
/// </para>
/// </remarks>

/// <summary>
/// A simple movement system that updates position based on velocity.
/// </summary>
public sealed class TestMovementSystem : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<TestPosition, TestVelocity>())
        {
            ref var pos = ref World.Get<TestPosition>(entity);
            ref readonly var vel = ref World.Get<TestVelocity>(entity);

            pos.X += vel.VX * deltaTime;
            pos.Y += vel.VY * deltaTime;
        }
    }
}

/// <summary>
/// A system that decrements lifetime and marks expired entities with DeadTag.
/// </summary>
public sealed class TestLifetimeSystem : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<TestLifetime>().Without<DeadTag>())
        {
            ref var lifetime = ref World.Get<TestLifetime>(entity);
            lifetime.Remaining -= deltaTime;

            if (lifetime.HasExpired)
            {
                World.Add(entity, new DeadTag());
            }
        }
    }
}

/// <summary>
/// A system that despawns entities marked with DeadTag.
/// </summary>
public sealed class TestDespawnSystem : SystemBase
{
    private readonly List<Entity> entitiesToDespawn = [];

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        entitiesToDespawn.Clear();
        foreach (var entity in World.Query<DeadTag>())
        {
            entitiesToDespawn.Add(entity);
        }

        foreach (var entity in entitiesToDespawn)
        {
            World.Despawn(entity);
        }
    }
}

/// <summary>
/// A system that applies damage to health components.
/// </summary>
/// <remarks>
/// Uses a queue-based approach where damage events can be added
/// and processed during the update.
/// </remarks>
public sealed class TestDamageSystem : SystemBase
{
    private readonly List<(Entity target, int amount)> pendingDamage = [];

    /// <summary>
    /// Queues damage to be applied to an entity.
    /// </summary>
    /// <param name="target">The entity to damage.</param>
    /// <param name="amount">The amount of damage.</param>
    public void QueueDamage(Entity target, int amount)
    {
        pendingDamage.Add((target, amount));
    }

    /// <summary>
    /// Clears all pending damage.
    /// </summary>
    public void ClearPending()
    {
        pendingDamage.Clear();
    }

    /// <summary>
    /// Gets the number of pending damage events.
    /// </summary>
    public int PendingCount => pendingDamage.Count;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        foreach (var (target, amount) in pendingDamage)
        {
            if (!World.IsAlive(target) || !World.Has<TestHealth>(target))
            {
                continue;
            }

            ref var health = ref World.Get<TestHealth>(target);
            health.Current = Math.Max(0, health.Current - amount);

            if (!health.IsAlive && !World.Has<DeadTag>(target))
            {
                World.Add(target, new DeadTag());
            }
        }

        pendingDamage.Clear();
    }
}

/// <summary>
/// A system that counts how many times it has been updated.
/// </summary>
/// <remarks>
/// Useful for verifying system execution in tests.
/// </remarks>
public sealed class TestCountingSystem : SystemBase
{
    /// <summary>
    /// Gets the number of times Update has been called.
    /// </summary>
    public int UpdateCount { get; private set; }

    /// <summary>
    /// Gets the total delta time accumulated across all updates.
    /// </summary>
    public float TotalDeltaTime { get; private set; }

    /// <summary>
    /// Gets the delta time from the last update.
    /// </summary>
    public float LastDeltaTime { get; private set; }

    /// <summary>
    /// Resets all counters.
    /// </summary>
    public void Reset()
    {
        UpdateCount = 0;
        TotalDeltaTime = 0;
        LastDeltaTime = 0;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        UpdateCount++;
        TotalDeltaTime += deltaTime;
        LastDeltaTime = deltaTime;
    }
}

/// <summary>
/// A system that tracks entity counts per update.
/// </summary>
/// <typeparam name="T">The component type to track.</typeparam>
public sealed class TestEntityCountingSystem<T> : SystemBase where T : struct, IComponent
{
    /// <summary>
    /// Gets the entity count from the last update.
    /// </summary>
    public int LastEntityCount { get; private set; }

    /// <summary>
    /// Gets the total entities processed across all updates.
    /// </summary>
    public int TotalEntitiesProcessed { get; private set; }

    /// <summary>
    /// Gets the number of updates performed.
    /// </summary>
    public int UpdateCount { get; private set; }

    /// <summary>
    /// Resets all counters.
    /// </summary>
    public void Reset()
    {
        LastEntityCount = 0;
        TotalEntitiesProcessed = 0;
        UpdateCount = 0;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        UpdateCount++;
        LastEntityCount = 0;

        foreach (var _ in World.Query<T>())
        {
            LastEntityCount++;
            TotalEntitiesProcessed++;
        }
    }
}

/// <summary>
/// A system that records entities it processes each update.
/// </summary>
/// <typeparam name="T">The component type to track.</typeparam>
public sealed class TestEntityRecordingSystem<T> : SystemBase where T : struct, IComponent
{
    private readonly List<List<Entity>> history = [];

    /// <summary>
    /// Gets the entities processed in each update, indexed by update number.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Entity>> History => history;

    /// <summary>
    /// Gets the entities processed in the last update.
    /// </summary>
    public IReadOnlyList<Entity> LastProcessed =>
        history.Count > 0 ? history[^1] : [];

    /// <summary>
    /// Gets the number of updates performed.
    /// </summary>
    public int UpdateCount => history.Count;

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void ClearHistory()
    {
        history.Clear();
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        var processed = new List<Entity>();

        foreach (var entity in World.Query<T>())
        {
            processed.Add(entity);
        }

        history.Add(processed);
    }
}

/// <summary>
/// A configurable system that executes a custom action during update.
/// </summary>
/// <param name="updateAction">The action to execute during update.</param>
public sealed class TestActionSystem(Action<IWorld, float>? updateAction = null) : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        updateAction?.Invoke(World, deltaTime);
    }
}

/// <summary>
/// A system that can be enabled/disabled and tracks its state.
/// </summary>
public sealed class TestToggleableSystem : SystemBase
{
    /// <summary>
    /// Gets the number of times Update was called while enabled.
    /// </summary>
    public int EnabledUpdateCount { get; private set; }

    /// <summary>
    /// Gets the number of times Update was skipped due to being disabled.
    /// </summary>
    public int SkippedUpdateCount { get; private set; }

    /// <summary>
    /// Resets all counters.
    /// </summary>
    public void Reset()
    {
        EnabledUpdateCount = 0;
        SkippedUpdateCount = 0;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (Enabled)
        {
            EnabledUpdateCount++;
        }
        else
        {
            SkippedUpdateCount++;
        }
    }
}

/// <summary>
/// A system that throws an exception during update (for testing error handling).
/// </summary>
/// <param name="exceptionToThrow">The exception to throw, or null for default InvalidOperationException.</param>
/// <param name="throwOnUpdateNumber">Which update number to throw on (1-based). Defaults to 1.</param>
public sealed class TestThrowingSystem(Exception? exceptionToThrow = null, int throwOnUpdateNumber = 1) : SystemBase
{
    private int updateCount;

    /// <summary>
    /// Gets the number of updates completed before throwing.
    /// </summary>
    public int CompletedUpdates => updateCount;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        updateCount++;

        if (updateCount >= throwOnUpdateNumber)
        {
            throw exceptionToThrow ?? new InvalidOperationException("Test exception from TestThrowingSystem");
        }
    }
}

/// <summary>
/// A system that tracks component changes.
/// </summary>
/// <typeparam name="T">The component type to track.</typeparam>
public sealed class TestComponentTrackingSystem<T> : SystemBase where T : struct, IComponent
{
    private readonly Dictionary<int, T> previousValues = [];

    /// <summary>
    /// Gets entities whose component values changed in the last update.
    /// </summary>
    public IReadOnlyList<Entity> ChangedEntities { get; private set; } = [];

    /// <summary>
    /// Gets entities that were added (have component but no previous value).
    /// </summary>
    public IReadOnlyList<Entity> AddedEntities { get; private set; } = [];

    /// <summary>
    /// Gets entities that were removed (had previous value but no longer have component).
    /// </summary>
    public IReadOnlyList<int> RemovedEntityIds { get; private set; } = [];

    /// <summary>
    /// Clears tracking state.
    /// </summary>
    public void ClearTracking()
    {
        previousValues.Clear();
        ChangedEntities = [];
        AddedEntities = [];
        RemovedEntityIds = [];
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        var changed = new List<Entity>();
        var added = new List<Entity>();
        var currentIds = new HashSet<int>();

        foreach (var entity in World.Query<T>())
        {
            currentIds.Add(entity.Id);
            var current = World.Get<T>(entity);

            if (previousValues.TryGetValue(entity.Id, out var previous))
            {
                if (!EqualityComparer<T>.Default.Equals(current, previous))
                {
                    changed.Add(entity);
                }
            }
            else
            {
                added.Add(entity);
            }

            previousValues[entity.Id] = current;
        }

        var removed = new List<int>();
        foreach (var id in previousValues.Keys)
        {
            if (!currentIds.Contains(id))
            {
                removed.Add(id);
            }
        }

        foreach (var id in removed)
        {
            previousValues.Remove(id);
        }

        ChangedEntities = changed;
        AddedEntities = added;
        RemovedEntityIds = removed;
    }
}
