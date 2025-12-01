namespace KeenEye.Sample;

// =============================================================================
// SYSTEM DEFINITIONS
// =============================================================================
// Systems process entities that match their queries.
// Use [System] attribute to add metadata (Phase, Order, Group).
// The source generator adds static properties for this metadata.
// =============================================================================

/// <summary>
/// Moves entities by applying velocity to position.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 0)]
public partial class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Using the fluent query API
        foreach (var entity in World.Query<Position, Velocity>())
        {
            // In a full implementation, we'd access components via ref:
            // ref var pos = ref World.Get<Position>(entity);
            // ref readonly var vel = ref World.Get<Velocity>(entity);
            // pos.X += vel.X * deltaTime;
            // pos.Y += vel.Y * deltaTime;

            Console.WriteLine($"  MovementSystem: Processing {entity}");
        }
    }
}

/// <summary>
/// Processes player input (only for entities with Player tag).
/// </summary>
[System(Phase = SystemPhase.EarlyUpdate, Order = 0)]
public partial class PlayerInputSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Query with filter - only entities that have Player tag
        foreach (var entity in World.Query<Position, Velocity>().With<Player>())
        {
            Console.WriteLine($"  PlayerInputSystem: Processing player {entity}");
        }
    }
}

/// <summary>
/// AI system for enemies (excludes players and disabled entities).
/// </summary>
[System(Phase = SystemPhase.Update, Order = 10)]
public partial class EnemyAISystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Complex query: entities with Position and Velocity,
        // that have Enemy tag, but NOT Disabled tag
        foreach (var entity in World.Query<Position, Velocity>()
            .With<Enemy>()
            .Without<Disabled>())
        {
            Console.WriteLine($"  EnemyAISystem: Processing enemy {entity}");
        }
    }
}

/// <summary>
/// Renders sprites (runs in Render phase).
/// </summary>
[System(Phase = SystemPhase.Render, Order = 0)]
public partial class RenderSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Sprite>())
        {
            Console.WriteLine($"  RenderSystem: Rendering {entity}");
        }
    }
}

/// <summary>
/// Processes health and damage.
/// </summary>
[System(Phase = SystemPhase.LateUpdate, Order = 0, Group = "Combat")]
public partial class HealthSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>().Without<Disabled>())
        {
            Console.WriteLine($"  HealthSystem: Checking health for {entity}");
        }
    }
}
