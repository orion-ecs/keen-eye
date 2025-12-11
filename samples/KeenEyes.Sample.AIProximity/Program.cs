using System.Diagnostics;
using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Spatial;

/// <summary>
/// Demonstrates AI proximity detection for vision and hearing using spatial queries.
/// Shows how AI agents can detect players/enemies within sensory ranges.
/// </summary>
public class Program
{
    public const int GuardCount = 50;
    public const int PlayerCount = 10;
    public const float WorldSize = 500f;
    public const int FrameCount = 200;

    // AI sensory ranges
    public const float VisionRange = 50f;
    public const float HearingRange = 100f;
    public const float AlertRange = 150f;

    public static void Main()
    {
        Console.WriteLine("=== AI Proximity Detection Sample ===\n");
        Console.WriteLine($"Simulating {GuardCount} guards and {PlayerCount} players");
        Console.WriteLine($"World size: {WorldSize}x{WorldSize}");
        Console.WriteLine($"Vision range: {VisionRange}, Hearing: {HearingRange}, Alert: {AlertRange}");
        Console.WriteLine($"Running {FrameCount} frames...\n");

        RunSimulation();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void RunSimulation()
    {
        using var world = new World();

        // Install spatial plugin with Grid (efficient for 2D AI)
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig
            {
                CellSize = VisionRange * 2f,  // Cell size based on vision range
                WorldMin = new Vector3(-WorldSize / 2, -10, -WorldSize / 2),
                WorldMax = new Vector3(WorldSize / 2, 10, WorldSize / 2)
            }
        };

        world.InstallPlugin(new SpatialPlugin(config));

        // Spawn guards (AI agents) at random positions
        var random = new Random(42);
        for (int i = 0; i < GuardCount; i++)
        {
            var position = new Vector3(
                random.NextSingle() * WorldSize - WorldSize / 2,
                0,
                random.NextSingle() * WorldSize - WorldSize / 2);

            world.Spawn()
                .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
                .With(new Guard
                {
                    VisionRange = VisionRange,
                    HearingRange = HearingRange,
                    AlertRange = AlertRange,
                    State = GuardState.Idle
                })
                .WithTag<SpatialIndexed>()
                .Build();
        }

        // Spawn players (moving targets)
        for (int i = 0; i < PlayerCount; i++)
        {
            var position = new Vector3(
                random.NextSingle() * WorldSize - WorldSize / 2,
                0,
                random.NextSingle() * WorldSize - WorldSize / 2);

            var velocity = new Vector3(
                random.NextSingle() * 20f - 10f,
                0,
                random.NextSingle() * 20f - 10f);

            world.Spawn()
                .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
                .With(new Velocity { Value = velocity })
                .With(new Noisy { NoiseLevel = random.NextSingle() })  // 0.0 = silent, 1.0 = loud
                .WithTag<Player>()
                .WithTag<SpatialIndexed>()
                .Build();
        }

        // Create stats tracker
        var stats = new DetectionStats();

        // Add systems
        world.AddSystem(new PlayerMovementSystem(), SystemPhase.Update, order: 0);
        world.AddSystem(new GuardAISystem(stats), SystemPhase.Update, order: 10);

        // Run simulation
        var stopwatch = Stopwatch.StartNew();
        for (int frame = 0; frame < FrameCount; frame++)
        {
            world.Update(deltaTime: 0.016f);

            if (frame % 50 == 0)
            {
                Console.Write(".");
            }
        }
        stopwatch.Stop();

        // Print final results
        Console.WriteLine();
        Console.WriteLine($"\nTotal time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average frame time: {stopwatch.ElapsedMilliseconds / (float)FrameCount:F2}ms");
        Console.WriteLine($"\nDetection Summary:");
        Console.WriteLine($"  Vision detections: {stats.TotalVisionDetections}");
        Console.WriteLine($"  Hearing detections: {stats.TotalHearingDetections}");
        Console.WriteLine($"  Alert broadcasts: {stats.TotalAlertBroadcasts}");
        Console.WriteLine($"  Guards in Alert state: {stats.GuardsInAlertState}");
        Console.WriteLine($"  Guards in Searching state: {stats.GuardsInSearchingState}");
        Console.WriteLine($"\nAverage per frame:");
        Console.WriteLine($"  Vision checks: {stats.TotalVisionDetections / (float)FrameCount:F1}");
        Console.WriteLine($"  Hearing checks: {stats.TotalHearingDetections / (float)FrameCount:F1}");
    }
}

/// <summary>
/// Stats for tracking AI detection performance.
/// </summary>
public class DetectionStats
{
    public int TotalVisionDetections;
    public int TotalHearingDetections;
    public int TotalAlertBroadcasts;
    public int GuardsInAlertState;
    public int GuardsInSearchingState;
}

/// <summary>
/// Guard AI state machine.
/// </summary>
public enum GuardState
{
    Idle,       // Patrolling, no threats detected
    Searching,  // Heard something, investigating
    Alert       // Saw player, actively pursuing
}

/// <summary>
/// Component for guard AI agents.
/// </summary>
[Component]
public partial struct Guard
{
    public float VisionRange;
    public float HearingRange;
    public float AlertRange;
    public GuardState State;
    public float SearchTimer;
}

/// <summary>
/// Component for entity velocity.
/// </summary>
[Component]
public partial struct Velocity
{
    public Vector3 Value;
}

/// <summary>
/// Component for noise generation (affects hearing detection).
/// </summary>
[Component]
public partial struct Noisy
{
    public float NoiseLevel;  // 0.0 = silent, 1.0 = very loud
}

/// <summary>
/// Tag component for player entities.
/// </summary>
[TagComponent]
public partial struct Player
{
}

/// <summary>
/// System that moves players around the world.
/// </summary>
public class PlayerMovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Velocity>().With<Player>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var velocity = ref World.Get<Velocity>(entity);

            // Move player
            transform.Position += velocity.Value * deltaTime;

            // Wrap around world bounds (toroidal world)
            const float halfSize = Program.WorldSize / 2;
            if (transform.Position.X < -halfSize)
            {
                transform.Position.X += Program.WorldSize;
            }

            if (transform.Position.X > halfSize)
            {
                transform.Position.X -= Program.WorldSize;
            }

            if (transform.Position.Z < -halfSize)
            {
                transform.Position.Z += Program.WorldSize;
            }

            if (transform.Position.Z > halfSize)
            {
                transform.Position.Z -= Program.WorldSize;
            }

            // Randomly change direction occasionally
            if (Random.Shared.NextSingle() < 0.02f)
            {
                velocity.Value = new Vector3(
                    Random.Shared.NextSingle() * 20f - 10f,
                    0,
                    Random.Shared.NextSingle() * 20f - 10f);
            }
        }
    }
}

/// <summary>
/// System that handles guard AI sensory detection and state management.
/// </summary>
public class GuardAISystem(DetectionStats stats) : SystemBase
{
    private SpatialQueryApi? spatial;

    protected override void OnInitialize()
    {
        spatial = World.GetExtension<SpatialQueryApi>();
    }

    public override void Update(float deltaTime)
    {
        // Reset per-frame stats
        stats.GuardsInAlertState = 0;
        stats.GuardsInSearchingState = 0;

        foreach (var guardEntity in World.Query<Transform3D, Guard>())
        {
            ref readonly var guardTransform = ref World.Get<Transform3D>(guardEntity);
            ref var guard = ref World.Get<Guard>(guardEntity);

            // Update guard state based on sensory input
            UpdateGuardState(guardEntity, ref guard, guardTransform, deltaTime);

            // Count guards by state
            if (guard.State == GuardState.Alert)
            {
                stats.GuardsInAlertState++;
            }
            else if (guard.State == GuardState.Searching)
            {
                stats.GuardsInSearchingState++;
            }
        }
    }

    private void UpdateGuardState(Entity guardEntity, ref Guard guard, Transform3D guardTransform, float deltaTime)
    {
        switch (guard.State)
        {
            case GuardState.Idle:
                CheckForThreats(guardEntity, ref guard, guardTransform);
                break;

            case GuardState.Searching:
                guard.SearchTimer -= deltaTime;
                if (guard.SearchTimer <= 0)
                {
                    guard.State = GuardState.Idle;
                }
                else
                {
                    // While searching, still check for visual confirmation
                    CheckForThreats(guardEntity, ref guard, guardTransform);
                }
                break;

            case GuardState.Alert:
                // In alert state, check if player is still visible
                if (!CanSeePlayer(guardTransform, guard.VisionRange))
                {
                    // Lost sight, return to searching
                    guard.State = GuardState.Searching;
                    guard.SearchTimer = 5.0f;
                }
                else
                {
                    // Broadcast alert to nearby guards
                    BroadcastAlert(guardEntity, guardTransform, guard.AlertRange);
                }
                break;
        }
    }

    private void CheckForThreats(Entity guardEntity, ref Guard guard, Transform3D guardTransform)
    {
        // Vision check (can see players)
        if (CanSeePlayer(guardTransform, guard.VisionRange))
        {
            guard.State = GuardState.Alert;
            stats.TotalVisionDetections++;
            return;
        }

        // Hearing check (can hear noisy players further away)
        if (CanHearPlayer(guardTransform, guard.HearingRange))
        {
            if (guard.State == GuardState.Idle)
            {
                guard.State = GuardState.Searching;
                guard.SearchTimer = 3.0f;
                stats.TotalHearingDetections++;
            }
        }
    }

    private bool CanSeePlayer(Transform3D guardTransform, float visionRange)
    {
        // Query nearby entities within vision range
        foreach (var entity in spatial!.QueryRadius<Player>(guardTransform.Position, visionRange))
        {
            ref readonly var playerTransform = ref World.Get<Transform3D>(entity);

            // Line-of-sight check (simplified - no obstacles)
            float distSq = Vector3.DistanceSquared(guardTransform.Position, playerTransform.Position);
            if (distSq <= visionRange * visionRange)
            {
                return true;
            }
        }

        return false;
    }

    private bool CanHearPlayer(Transform3D guardTransform, float hearingRange)
    {
        // Query nearby entities within hearing range
        foreach (var entity in spatial!.QueryRadius<Player>(guardTransform.Position, hearingRange))
        {
            if (!World.Has<Noisy>(entity))
            {
                continue;
            }

            ref readonly var playerTransform = ref World.Get<Transform3D>(entity);
            ref readonly var noisy = ref World.Get<Noisy>(entity);

            // Hearing distance affected by noise level
            float effectiveRange = hearingRange * noisy.NoiseLevel;
            float distSq = Vector3.DistanceSquared(guardTransform.Position, playerTransform.Position);

            if (distSq <= effectiveRange * effectiveRange)
            {
                return true;
            }
        }

        return false;
    }

    private void BroadcastAlert(Entity sourceGuard, Transform3D sourceTransform, float alertRange)
    {
        // Alert nearby guards (they join the alert state)
        foreach (var otherGuard in spatial!.QueryRadius<Guard>(sourceTransform.Position, alertRange))
        {
            if (otherGuard == sourceGuard)
            {
                continue;
            }

            ref var guard = ref World.Get<Guard>(otherGuard);
            if (guard.State == GuardState.Idle)
            {
                guard.State = GuardState.Searching;
                guard.SearchTimer = 4.0f;
                stats.TotalAlertBroadcasts++;
            }
        }
    }
}
