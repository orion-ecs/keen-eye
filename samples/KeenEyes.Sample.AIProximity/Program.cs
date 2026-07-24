using System.Diagnostics;
using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial;

namespace KeenEyes.Sample.AIProximity;

/// <summary>
/// Demonstrates AI proximity detection for vision and hearing using spatial queries.
/// Shows how AI agents can detect players/enemies within sensory ranges.
/// </summary>
public static class Program
{
    /// <summary>Number of guard AI agents to spawn.</summary>
    public const int GuardCount = 50;

    /// <summary>Number of player targets to spawn.</summary>
    public const int PlayerCount = 10;

    /// <summary>Width and depth of the square world in units.</summary>
    public const float WorldSize = 500f;

    /// <summary>Number of simulation frames to run.</summary>
    public const int FrameCount = 200;

    /// <summary>Distance within which a guard can see a player.</summary>
    public const float VisionRange = 50f;

    /// <summary>Distance within which a guard can hear a noisy player.</summary>
    public const float HearingRange = 100f;

    /// <summary>Distance within which a guard broadcasts alerts to other guards.</summary>
    public const float AlertRange = 150f;

    /// <summary>Application entry point.</summary>
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
        using var world = new World(seed: 42);

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
        for (int i = 0; i < GuardCount; i++)
        {
            var position = new Vector3(
                world.NextFloat() * WorldSize - WorldSize / 2,
                0,
                world.NextFloat() * WorldSize - WorldSize / 2);

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
                world.NextFloat() * WorldSize - WorldSize / 2,
                0,
                world.NextFloat() * WorldSize - WorldSize / 2);

            var velocity = new Vector3(
                world.NextFloat() * 20f - 10f,
                0,
                world.NextFloat() * 20f - 10f);

            world.Spawn()
                .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
                .With(new Velocity { Value = velocity })
                .With(new Noisy { NoiseLevel = world.NextFloat() })  // 0.0 = silent, 1.0 = loud
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
