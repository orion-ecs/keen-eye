namespace KeenEyes.Sample.Simulation;

// =============================================================================
// SYSTEM DEFINITIONS - Self-Running Simulation
// =============================================================================

/// <summary>
/// Moves entities by applying velocity to position, bouncing off walls.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 0)]
public partial class MovementSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    /// <summary>World width in units.</summary>
    public float WorldWidth { get; set; } = 60;

    /// <summary>World height in units.</summary>
    public float WorldHeight { get; set; } = 20;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Collect entities first to avoid iterator invalidation when adding Dead component
        var entities = World.Query<Position, Velocity>().Without<Dead>().ToList();

        foreach (var entity in entities)
        {
            ref var pos = ref World.Get<Position>(entity);
            ref var vel = ref World.Get<Velocity>(entity);

            // Apply velocity
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;

            // Bounce off walls (not for projectiles - they just die)
            if (World.Has<Projectile>(entity))
            {
                if (pos.X < 0 || pos.X >= WorldWidth || pos.Y < 0 || pos.Y >= WorldHeight)
                {
                    buffer.AddComponent(entity, new Dead());
                }
            }
            else
            {
                // Bounce X
                if (pos.X < 0)
                {
                    pos.X = 0;
                    vel.X = MathF.Abs(vel.X);
                }
                else if (pos.X >= WorldWidth)
                {
                    pos.X = WorldWidth - 0.1f;
                    vel.X = -MathF.Abs(vel.X);
                }

                // Bounce Y
                if (pos.Y < 0)
                {
                    pos.Y = 0;
                    vel.Y = MathF.Abs(vel.Y);
                }
                else if (pos.Y >= WorldHeight)
                {
                    pos.Y = WorldHeight - 0.1f;
                    vel.Y = -MathF.Abs(vel.Y);
                }
            }
        }

        buffer.Flush(World);
    }
}

/// <summary>
/// Detects collisions between entities and applies damage.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 10)]
public partial class CollisionSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Get all damageable entities (with position, collider, and health)
        var damageables = World.Query<Position, Collider, Health>()
            .Without<Dead>()
            .Without<Invulnerable>()
            .ToList();

        // Get all projectiles
        var projectiles = World.Query<Position, Collider, Damage>()
            .With<Projectile>()
            .Without<Dead>()
            .ToList();

        foreach (var projectile in projectiles)
        {
            ref readonly var projPos = ref World.Get<Position>(projectile);
            ref readonly var projCol = ref World.Get<Collider>(projectile);
            ref readonly var projDmg = ref World.Get<Damage>(projectile);

            foreach (var target in damageables)
            {
                // Don't hit self
                if (projectile == target)
                {
                    continue;
                }

                // Check if projectile hits enemy or player
                bool projectileIsFromPlayer = !World.Has<Enemy>(projectile);
                bool targetIsEnemy = World.Has<Enemy>(target);

                // Projectiles from player hit enemies, enemy projectiles hit player
                if (projectileIsFromPlayer != targetIsEnemy)
                {
                    continue;
                }

                ref readonly var targetPos = ref World.Get<Position>(target);
                ref readonly var targetCol = ref World.Get<Collider>(target);

                float dx = projPos.X - targetPos.X;
                float dy = projPos.Y - targetPos.Y;
                float distSq = dx * dx + dy * dy;
                float radiusSum = projCol.Radius + targetCol.Radius;

                if (distSq < radiusSum * radiusSum)
                {
                    // Hit! Apply damage and destroy projectile
                    ref var health = ref World.Get<Health>(target);
                    health.Current -= projDmg.Amount;

                    // Mark projectile as dead
                    World.Add(projectile, new Dead());
                    break; // Projectile can only hit one target
                }
            }
        }
    }
}

/// <summary>
/// Marks entities with zero health as dead.
/// </summary>
[System(Phase = SystemPhase.LateUpdate, Order = 0)]
public partial class HealthSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    /// <summary>Event raised when an enemy dies.</summary>
    public event Action? OnEnemyKilled;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Collect entities first to avoid iterator invalidation
        var entities = World.Query<Health>().Without<Dead>().ToList();

        foreach (var entity in entities)
        {
            ref readonly var health = ref World.Get<Health>(entity);

            if (!health.IsAlive)
            {
                buffer.AddComponent(entity, new Dead());

                if (World.Has<Enemy>(entity))
                {
                    OnEnemyKilled?.Invoke();
                }
            }
        }

        buffer.Flush(World);
    }
}

/// <summary>
/// Updates lifetime and marks expired entities as dead.
/// </summary>
[System(Phase = SystemPhase.LateUpdate, Order = 5)]
public partial class LifetimeSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Collect entities first to avoid iterator invalidation
        var entities = World.Query<Lifetime>().Without<Dead>().ToList();

        foreach (var entity in entities)
        {
            ref var lifetime = ref World.Get<Lifetime>(entity);
            lifetime.Remaining -= deltaTime;

            if (lifetime.Remaining <= 0)
            {
                buffer.AddComponent(entity, new Dead());
            }
        }

        buffer.Flush(World);
    }
}

/// <summary>
/// Updates cooldown timers.
/// </summary>
[System(Phase = SystemPhase.Update, Order = -10)]
public partial class CooldownSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Cooldown>().Without<Dead>())
        {
            ref var cooldown = ref World.Get<Cooldown>(entity);
            if (cooldown.Remaining > 0)
            {
                cooldown.Remaining -= deltaTime;
            }
        }
    }
}

/// <summary>
/// Removes all dead entities from the world.
/// </summary>
[System(Phase = SystemPhase.PostRender, Order = 100)]
public partial class CleanupSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var deadEntities = World.Query<Position>()
            .With<Dead>()
            .ToList();

        foreach (var entity in deadEntities)
        {
            World.Despawn(entity);
        }
    }
}

/// <summary>
/// Spawns enemies periodically.
/// </summary>
[System(Phase = SystemPhase.EarlyUpdate, Order = 0)]
public partial class SpawnerSystem : SystemBase
{
    private readonly Random random = new();
    private float spawnTimer;

    /// <summary>Number of edges (North, South, East, West) for enemy spawning.</summary>
    private const int EdgeCount = 4;

    /// <summary>Number of different enemy types.</summary>
    private const int EnemyTypeCount = 3;

    /// <summary>Base velocity multiplier for random enemy movement.</summary>
    private const float BaseVelocityMultiplier = 10f;

    /// <summary>Additional velocity component for directional movement.</summary>
    private const float DirectionalVelocityMultiplier = 5f;

    /// <summary>Minimum velocity added to ensure enemies move inward.</summary>
    private const float MinimumInwardVelocity = 2f;

    /// <summary>Speed multiplier for fast enemy type.</summary>
    private const float FastEnemySpeedMultiplier = 1.5f;

    /// <summary>Speed multiplier for slow enemy type.</summary>
    private const float SlowEnemySpeedMultiplier = 0.5f;

    /// <summary>Offset from world edge for spawn positioning.</summary>
    private const float EdgeSpawnOffset = 1f;

    /// <summary>Collision radius for enemy entities.</summary>
    private const float EnemyColliderRadius = 0.5f;

    /// <summary>Seconds between spawns.</summary>
    public float SpawnInterval { get; set; } = 2.0f;

    /// <summary>Maximum enemies alive at once.</summary>
    public int MaxEnemies { get; set; } = 8;

    /// <summary>World dimensions for spawn positioning.</summary>
    public float WorldWidth { get; set; } = 60;

    /// <summary>World height for spawn positioning.</summary>
    public float WorldHeight { get; set; } = 20;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        spawnTimer -= deltaTime;

        if (spawnTimer <= 0)
        {
            spawnTimer = SpawnInterval;

            // Count current enemies
            int enemyCount = World.Query<Position>()
                .With<Enemy>()
                .Without<Dead>()
                .Count();

            if (enemyCount < MaxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        // Random edge spawn
        float x, y, vx, vy;
        int edge = random.Next(EdgeCount);

        switch (edge)
        {
            case 0: // Top
                x = random.NextSingle() * WorldWidth;
                y = 0;
                vx = (random.NextSingle() - 0.5f) * BaseVelocityMultiplier;
                vy = random.NextSingle() * DirectionalVelocityMultiplier + MinimumInwardVelocity;
                break;
            case 1: // Bottom
                x = random.NextSingle() * WorldWidth;
                y = WorldHeight - EdgeSpawnOffset;
                vx = (random.NextSingle() - 0.5f) * BaseVelocityMultiplier;
                vy = -(random.NextSingle() * DirectionalVelocityMultiplier + MinimumInwardVelocity);
                break;
            case 2: // Left
                x = 0;
                y = random.NextSingle() * WorldHeight;
                vx = random.NextSingle() * DirectionalVelocityMultiplier + MinimumInwardVelocity;
                vy = (random.NextSingle() - 0.5f) * BaseVelocityMultiplier;
                break;
            default: // Right
                x = WorldWidth - EdgeSpawnOffset;
                y = random.NextSingle() * WorldHeight;
                vx = -(random.NextSingle() * DirectionalVelocityMultiplier + MinimumInwardVelocity);
                vy = (random.NextSingle() - 0.5f) * BaseVelocityMultiplier;
                break;
        }

        // Random enemy type
        char symbol;
        ConsoleColor color;
        int health;

        int type = random.Next(EnemyTypeCount);
        switch (type)
        {
            case 0: // Fast, weak
                symbol = 'o';
                color = ConsoleColor.Yellow;
                health = 1;
                vx *= FastEnemySpeedMultiplier;
                vy *= FastEnemySpeedMultiplier;
                break;
            case 1: // Medium
                symbol = 'O';
                color = ConsoleColor.Red;
                health = 2;
                break;
            default: // Slow, tough
                symbol = '@';
                color = ConsoleColor.Magenta;
                health = 3;
                vx *= SlowEnemySpeedMultiplier;
                vy *= SlowEnemySpeedMultiplier;
                break;
        }

        World.Spawn()
            .WithPosition(x: x, y: y)
            .WithVelocity(x: vx, y: vy)
            .WithHealth(current: health, max: health)
            .WithCollider(radius: EnemyColliderRadius)
            .WithRenderable(symbol: symbol, color: color)
            .WithEnemy()
            .Build();
    }
}

/// <summary>
/// Handles player shooting with auto-fire.
/// </summary>
[System(Phase = SystemPhase.Update, Order = -5)]
public partial class ShootingSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    /// <summary>Default time between auto-shots in seconds.</summary>
    private const float DefaultFireRate = 0.3f;

    /// <summary>Minimum distance threshold for shooting calculations.</summary>
    private const float MinimumShootingDistance = 0.1f;

    /// <summary>Speed of player projectiles in units per second.</summary>
    private const float PlayerProjectileSpeed = 30f;

    /// <summary>Collision radius for player projectiles.</summary>
    private const float PlayerProjectileRadius = 0.3f;

    /// <summary>Lifetime of player projectiles in seconds.</summary>
    private const float PlayerProjectileLifetime = 3f;

    /// <summary>Time between auto-shots.</summary>
    public float FireRate { get; set; } = DefaultFireRate;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Collect players to process first to avoid iterator invalidation
        var players = World.Query<Position, Cooldown>().With<Player>().Without<Dead>().ToList();

        foreach (var entity in players)
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            ref var cooldown = ref World.Get<Cooldown>(entity);

            if (cooldown.Remaining <= 0)
            {
                cooldown.Remaining = FireRate;

                // Find nearest enemy to shoot at
                Entity? nearestEnemy = null;
                float nearestDist = float.MaxValue;

                foreach (var enemy in World.Query<Position>().With<Enemy>().Without<Dead>())
                {
                    ref readonly var enemyPos = ref World.Get<Position>(enemy);
                    float dx = enemyPos.X - pos.X;
                    float dy = enemyPos.Y - pos.Y;
                    float dist = dx * dx + dy * dy;

                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestEnemy = enemy;
                    }
                }

                if (nearestEnemy.HasValue)
                {
                    ref readonly var targetPos = ref World.Get<Position>(nearestEnemy.Value);

                    // Calculate direction to enemy
                    float dx = targetPos.X - pos.X;
                    float dy = targetPos.Y - pos.Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    if (dist > MinimumShootingDistance)
                    {
                        float vx = (dx / dist) * PlayerProjectileSpeed;
                        float vy = (dy / dist) * PlayerProjectileSpeed;

                        // Queue projectile spawn via CommandBuffer
                        buffer.Spawn()
                            .With(new Position { X = pos.X, Y = pos.Y })
                            .With(new Velocity { X = vx, Y = vy })
                            .With(new Damage { Amount = 1 })
                            .With(new Collider { Radius = PlayerProjectileRadius })
                            .With(new Renderable { Symbol = '*', Color = ConsoleColor.Cyan })
                            .With(new Lifetime { Remaining = PlayerProjectileLifetime })
                            .With(new Projectile());
                    }
                }
            }
        }

        // Flush all queued spawns
        buffer.Flush(World);
    }
}

/// <summary>
/// Enemy shooting behavior.
/// </summary>
[System(Phase = SystemPhase.Update, Order = -4)]
public partial class EnemyShootingSystem : SystemBase
{
    private readonly Random random = new();
    private readonly CommandBuffer buffer = new();

    /// <summary>Base enemy fire rate in seconds.</summary>
    private const float BaseEnemyFireRate = 1.5f;

    /// <summary>Minimum distance threshold for enemy shooting.</summary>
    private const float MinimumEnemyShootingDistance = 1f;

    /// <summary>Speed of enemy projectiles in units per second.</summary>
    private const float EnemyProjectileSpeed = 15f;

    /// <summary>Collision radius for enemy projectiles.</summary>
    private const float EnemyProjectileRadius = 0.3f;

    /// <summary>Lifetime of enemy projectiles in seconds.</summary>
    private const float EnemyProjectileLifetime = 2f;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Find player
        Entity? player = null;
        foreach (var p in World.Query<Position>().With<Player>().Without<Dead>())
        {
            player = p;
            break;
        }

        if (!player.HasValue)
        {
            return;
        }

        ref readonly var playerPos = ref World.Get<Position>(player.Value);

        // Collect enemies first to avoid iterator invalidation
        var enemies = World.Query<Position, Cooldown>().With<Enemy>().Without<Dead>().ToList();

        foreach (var entity in enemies)
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            ref var cooldown = ref World.Get<Cooldown>(entity);

            // Only tough enemies (@) shoot back
            if (!World.Has<Renderable>(entity))
            {
                continue;
            }

            ref readonly var render = ref World.Get<Renderable>(entity);
            if (render.Symbol != '@')
            {
                continue;
            }

            if (cooldown.Remaining <= 0)
            {
                cooldown.Remaining = BaseEnemyFireRate + random.NextSingle();

                // Shoot toward player
                float dx = playerPos.X - pos.X;
                float dy = playerPos.Y - pos.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist > MinimumEnemyShootingDistance)
                {
                    float vx = (dx / dist) * EnemyProjectileSpeed;
                    float vy = (dy / dist) * EnemyProjectileSpeed;

                    // Queue enemy projectile spawn via CommandBuffer
                    buffer.Spawn()
                        .With(new Position { X = pos.X, Y = pos.Y })
                        .With(new Velocity { X = vx, Y = vy })
                        .With(new Damage { Amount = 1 })
                        .With(new Collider { Radius = EnemyProjectileRadius })
                        .With(new Renderable { Symbol = '.', Color = ConsoleColor.Red })
                        .With(new Lifetime { Remaining = EnemyProjectileLifetime })
                        .With(new Projectile())
                        .With(new Enemy()); // Mark as enemy projectile
                }
            }
        }

        // Flush all queued spawns
        buffer.Flush(World);
    }
}
