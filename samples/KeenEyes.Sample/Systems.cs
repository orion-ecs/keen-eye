namespace KeenEyes.Sample;

// =============================================================================
// SYSTEM DEFINITIONS
// =============================================================================
// Systems process entities that match their queries.
// Use [System] attribute to add metadata (Phase, Order, Group).
// The source generator adds static properties for this metadata.
//
// Systems support lifecycle hooks:
// - OnInitialize()     - Called when system is added to a world
// - OnBeforeUpdate()   - Called before each Update
// - OnAfterUpdate()    - Called after each Update
// - OnEnabled()        - Called when system transitions to enabled
// - OnDisabled()       - Called when system transitions to disabled
//
// Use [RunBefore] and [RunAfter] for explicit dependency ordering:
// - [RunBefore(typeof(OtherSystem))] - This system runs before OtherSystem
// - [RunAfter(typeof(OtherSystem))]  - This system runs after OtherSystem
// =============================================================================

/// <summary>
/// Moves entities by applying velocity to position.
/// Demonstrates lifecycle hooks for system setup and teardown.
/// Uses [RunAfter] to ensure this runs after PlayerInputSystem.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 0)]
[RunAfter(typeof(PlayerInputSystem))]
public partial class MovementSystem : SystemBase
{
    private int frameCount;

    /// <summary>
    /// Called when the system is added to a world.
    /// </summary>
    protected override void OnInitialize()
    {
        Console.WriteLine("  [MovementSystem] Initialized");
    }

    /// <summary>
    /// Called before each Update - useful for accumulating time or resetting state.
    /// </summary>
    protected override void OnBeforeUpdate(float deltaTime)
    {
        frameCount++;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Using the fluent query API
        foreach (var entity in World.Query<Position, Velocity>())
        {
            // Access components via ref for zero-copy modification
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;

            Console.WriteLine($"  MovementSystem [frame {frameCount}]: Processing {entity}");
        }
    }

    /// <summary>
    /// Called after each Update - useful for cleanup or statistics.
    /// </summary>
    protected override void OnAfterUpdate(float deltaTime)
    {
        // Could log statistics, sync state, etc.
    }

    /// <summary>
    /// Called when system is enabled at runtime.
    /// </summary>
    protected override void OnEnabled()
    {
        Console.WriteLine("  [MovementSystem] Enabled");
    }

    /// <summary>
    /// Called when system is disabled at runtime.
    /// </summary>
    protected override void OnDisabled()
    {
        Console.WriteLine("  [MovementSystem] Disabled");
    }
}

/// <summary>
/// Processes player input (only for entities with Player tag).
/// </summary>
[System(Phase = SystemPhase.EarlyUpdate, Order = 0)]
public partial class PlayerInputSystem : SystemBase
{
    /// <inheritdoc />
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
    /// <inheritdoc />
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
    /// <inheritdoc />
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
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>().Without<Disabled>())
        {
            Console.WriteLine($"  HealthSystem: Checking health for {entity}");
        }
    }
}

/// <summary>
/// Physics simulation system.
/// Runs in FixedUpdate phase for deterministic physics at fixed timestep.
/// Use World.FixedUpdate() to run this system independently of the main update loop.
/// </summary>
[System(Phase = SystemPhase.FixedUpdate, Order = 0)]
public partial class PhysicsSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            Console.WriteLine($"  PhysicsSystem [FixedUpdate]: Simulating physics for {entity}");
        }
    }
}

// =============================================================================
// PLUGIN EXAMPLES
// =============================================================================
// Plugins encapsulate related systems, components, and functionality.
// They are installed per-world, maintaining isolation between worlds.
// Use IWorldPlugin interface to create reusable feature packages.
// =============================================================================

/// <summary>
/// Example extension class that a plugin might expose.
/// Extensions allow plugins to provide custom APIs to application code.
/// The [PluginExtension] attribute generates a typed property on World.
/// </summary>
[PluginExtension("DebugStats")]
public sealed class DebugStats
{
    /// <summary>
    /// Number of entities processed in the last frame.
    /// </summary>
    public int EntitiesProcessed { get; set; }

    /// <summary>
    /// Total update time in milliseconds.
    /// </summary>
    public float UpdateTimeMs { get; set; }
}

/// <summary>
/// Example debug system that tracks entity statistics.
/// </summary>
public class DebugStatsSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Access the extension set by our plugin
        if (World.TryGetExtension<DebugStats>(out var stats))
        {
            stats.EntitiesProcessed = World.GetAllEntities().Count();
            Console.WriteLine($"  DebugStatsSystem: {stats.EntitiesProcessed} entities");
        }
    }
}

/// <summary>
/// Example plugin that provides debug statistics functionality.
/// Demonstrates the full plugin lifecycle and extension API.
/// </summary>
public class DebugPlugin : IWorldPlugin
{
    /// <inheritdoc />
    public string Name => "Debug";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        Console.WriteLine($"  [{Name}Plugin] Installing...");

        // Register systems through the context (tracked for auto-cleanup)
        context.AddSystem<DebugStatsSystem>(SystemPhase.PostRender, order: 100);

        // Expose an extension API for application code
        context.SetExtension(new DebugStats());

        Console.WriteLine($"  [{Name}Plugin] Installed");
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        Console.WriteLine($"  [{Name}Plugin] Uninstalling...");

        // Remove our extension (systems are auto-removed)
        context.RemoveExtension<DebugStats>();

        Console.WriteLine($"  [{Name}Plugin] Uninstalled");
    }
}

/// <summary>
/// Example combat plugin that groups combat-related systems.
/// </summary>
public class CombatPlugin : IWorldPlugin
{
    /// <inheritdoc />
    public string Name => "Combat";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        Console.WriteLine($"  [{Name}Plugin] Installing combat systems...");

        // Register HealthSystem at LateUpdate phase
        context.AddSystem<HealthSystem>(SystemPhase.LateUpdate, order: 0);

        Console.WriteLine($"  [{Name}Plugin] Installed");
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        Console.WriteLine($"  [{Name}Plugin] Uninstalled");
    }
}
