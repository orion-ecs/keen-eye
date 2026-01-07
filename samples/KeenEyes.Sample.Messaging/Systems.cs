namespace KeenEyes.Sample.Messaging;

// =============================================================================
// SYSTEMS DEMONSTRATING INTER-SYSTEM MESSAGING
// =============================================================================
// These systems show different messaging patterns:
// 1. CombatSystem - Sends damage messages to decouple damage dealing from handling
// 2. HealthSystem - Subscribes to damage messages and updates health
// 3. AudioSystem - Subscribes to damage/death messages for sound effects
// 4. UISystem - Subscribes to score changes for display updates
// 5. SpawnerSystem - Uses request/response pattern with queued messages
// =============================================================================

/// <summary>
/// Combat system that processes attacks and sends damage messages.
/// Demonstrates: Sending messages to broadcast events to multiple systems.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 10)]
public partial class CombatSystem : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Find entities that have an attack and a target
        foreach (var entity in World.Query<Attack, Target>().Without<Dead>())
        {
            ref readonly var attack = ref World.Get<Attack>(entity);
            ref readonly var target = ref World.Get<Target>(entity);

            // Skip if target is invalid or dead
            if (!World.IsAlive(target.Entity) || World.Has<Dead>(target.Entity))
            {
                continue;
            }

            // Check if target has health
            if (!World.Has<Health>(target.Entity))
            {
                continue;
            }

            // Send damage message - other systems will respond
            // CombatSystem doesn't need to know about Health, Audio, UI, etc.
            World.Send(new DamageMessage(
                Target: target.Entity,
                Amount: attack.Damage,
                Source: entity));
        }
    }
}

/// <summary>
/// Health system that subscribes to damage messages and updates entity health.
/// Demonstrates: Subscribing to messages and sending follow-up messages.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 20)]
public partial class HealthSystem : SystemBase
{
    private EventSubscription? damageSubscription;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        // Subscribe to damage messages
        damageSubscription = World.Subscribe<DamageMessage>(HandleDamage);
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Nothing to do in Update - all work is done via messages
    }

    private void HandleDamage(DamageMessage msg)
    {
        if (!World.Has<Health>(msg.Target))
        {
            return;
        }

        ref var health = ref World.Get<Health>(msg.Target);
        var oldHealth = health.Current;
        health.Current = Math.Max(0, health.Current - msg.Amount);

        Console.WriteLine($"  [Health] Entity {msg.Target.Id} took {msg.Amount} damage ({oldHealth} -> {health.Current})");

        // Check for death
        if (health.Current <= 0 && oldHealth > 0)
        {
            // Mark as dead
            World.Add(msg.Target, default(Dead));

            // Send death message
            World.Send(new DeathMessage(msg.Target, "combat damage"));
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            damageSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Audio system that plays sounds in response to game events.
/// Demonstrates: Multiple subscriptions to different message types.
/// </summary>
[System(Phase = SystemPhase.LateUpdate, Order = 0)]
public partial class AudioSystem : SystemBase
{
    private EventSubscription? damageSubscription;
    private EventSubscription? deathSubscription;
    private EventSubscription? pickupSubscription;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        // Subscribe to various events for audio feedback
        damageSubscription = World.Subscribe<DamageMessage>(HandleDamage);
        deathSubscription = World.Subscribe<DeathMessage>(HandleDeath);
        pickupSubscription = World.Subscribe<ItemPickupMessage>(HandlePickup);
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Nothing to do in Update - audio is event-driven
    }

    private static void HandleDamage(DamageMessage msg)
    {
        Console.WriteLine($"  [Audio] Playing hit sound for entity {msg.Target.Id}");
    }

    private static void HandleDeath(DeathMessage msg)
    {
        Console.WriteLine($"  [Audio] Playing death sound for entity {msg.Entity.Id}");
    }

    private static void HandlePickup(ItemPickupMessage msg)
    {
        Console.WriteLine($"  [Audio] Playing pickup sound for {msg.ItemType}");
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            damageSubscription?.Dispose();
            deathSubscription?.Dispose();
            pickupSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// UI system that updates display in response to game events.
/// Demonstrates: Subscribing to score changes for reactive UI updates.
/// </summary>
[System(Phase = SystemPhase.LateUpdate, Order = 10)]
public partial class UISystem : SystemBase
{
    private EventSubscription? scoreSubscription;
    private EventSubscription? deathSubscription;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        scoreSubscription = World.Subscribe<ScoreChangedMessage>(HandleScoreChanged);
        deathSubscription = World.Subscribe<DeathMessage>(HandleDeath);
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Nothing to do - UI updates are event-driven
    }

    private static void HandleScoreChanged(ScoreChangedMessage msg)
    {
        Console.WriteLine($"  [UI] Score updated: {msg.OldScore} -> {msg.NewScore}");
    }

    private static void HandleDeath(DeathMessage msg)
    {
        Console.WriteLine($"  [UI] Entity {msg.Entity.Id} death notification displayed");
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            scoreSubscription?.Dispose();
            deathSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Score system that tracks kills and updates score.
/// Demonstrates: Chaining messages (death -> score change).
/// </summary>
[System(Phase = SystemPhase.Update, Order = 30)]
public partial class ScoreSystem : SystemBase
{
    private EventSubscription? deathSubscription;
    private int currentScore;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        deathSubscription = World.Subscribe<DeathMessage>(HandleDeath);
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Nothing to do in Update - score is event-driven
    }

    private void HandleDeath(DeathMessage msg)
    {
        // Only award points for enemy deaths
        if (!World.Has<Enemy>(msg.Entity))
        {
            return;
        }

        var oldScore = currentScore;
        currentScore += 100;

        // Send score changed message (other systems like UI will respond)
        World.Send(new ScoreChangedMessage(oldScore, currentScore));
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            deathSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Spawner system that processes spawn requests using message queuing.
/// Demonstrates: Deferred message processing with QueueMessage/ProcessQueuedMessages.
/// </summary>
[System(Phase = SystemPhase.EarlyUpdate, Order = 0)]
public partial class SpawnerSystem : SystemBase
{
    private EventSubscription? spawnRequestSubscription;
    private readonly Queue<SpawnEnemyRequest> pendingSpawns = new();

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        spawnRequestSubscription = World.Subscribe<SpawnEnemyRequest>(HandleSpawnRequest);
    }

    private void HandleSpawnRequest(SpawnEnemyRequest request)
    {
        // Queue the spawn request for processing
        pendingSpawns.Enqueue(request);
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Process all pending spawn requests
        while (pendingSpawns.Count > 0)
        {
            var request = pendingSpawns.Dequeue();

            var enemy = World.Spawn()
                .WithPosition(request.X, request.Y)
                .WithHealth(request.Health, request.Health)
                .WithEnemy()
                .Build();

            Console.WriteLine($"  [Spawner] Created enemy {enemy.Id} at ({request.X}, {request.Y})");

            // Send confirmation message
            World.Send(new EnemySpawnedMessage(enemy));
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            spawnRequestSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Analytics system demonstrating HasMessageSubscribers optimization.
/// Demonstrates: Checking for subscribers before expensive operations.
/// </summary>
[System(Phase = SystemPhase.PostRender, Order = 0)]
public partial class AnalyticsSystem : SystemBase
{
    private int frameCount;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        frameCount++;

        // Only gather analytics if someone is listening
        // This optimization prevents expensive work when no one cares
        if (World.HasMessageSubscribers<AnalyticsMessage>())
        {
            var entityCount = World.EntityCount;
            var aliveEnemies = World.Query<Position>().With<Enemy>().Without<Dead>().Count();

            World.Send(new AnalyticsMessage(frameCount, entityCount, aliveEnemies));
        }
    }
}

/// <summary>
/// Analytics message containing frame statistics.
/// </summary>
/// <param name="Frame">The current frame number.</param>
/// <param name="TotalEntities">Total entity count.</param>
/// <param name="AliveEnemies">Number of alive enemies.</param>
public readonly record struct AnalyticsMessage(int Frame, int TotalEntities, int AliveEnemies);
