using System.Diagnostics;

namespace KeenEyes.Sample;

/// <summary>
/// Demonstrates using global system hooks for profiling, logging, and conditional execution.
/// </summary>
public static class SystemHooksExample
{
    /// <summary>
    /// Simple profiler that tracks system execution times.
    /// </summary>
    public class SystemProfiler
    {
        private readonly Dictionary<string, ProfileData> profiles = [];
        private readonly Dictionary<ISystem, Stopwatch> activeTimers = [];

        /// <summary>
        /// Begins profiling a system execution.
        /// </summary>
        /// <param name="system">The system being profiled.</param>
        public void BeginSystem(ISystem system)
        {
            var timer = Stopwatch.StartNew();
            activeTimers[system] = timer;
        }

        /// <summary>
        /// Ends profiling a system execution and records metrics.
        /// </summary>
        /// <param name="system">The system that finished executing.</param>
        public void EndSystem(ISystem system)
        {
            if (!activeTimers.TryGetValue(system, out var timer))
            {
                return;
            }

            timer.Stop();
            var systemName = system.GetType().Name;

            if (!profiles.ContainsKey(systemName))
            {
                profiles[systemName] = new ProfileData();
            }

            var profile = profiles[systemName];
            profile.TotalTime += timer.Elapsed.TotalMilliseconds;
            profile.CallCount++;
            profile.MinTime = Math.Min(profile.MinTime, timer.Elapsed.TotalMilliseconds);
            profile.MaxTime = Math.Max(profile.MaxTime, timer.Elapsed.TotalMilliseconds);

            activeTimers.Remove(system);
        }

        /// <summary>
        /// Prints a profiling report with metrics for all systems.
        /// </summary>
        public void PrintReport()
        {
            Console.WriteLine("\n=== System Profiling Report ===");
            Console.WriteLine($"{"System",-30} {"Calls",8} {"Total",10} {"Avg",10} {"Min",10} {"Max",10}");
            Console.WriteLine(new string('-', 88));

            foreach (var (systemName, profile) in profiles.OrderByDescending(p => p.Value.TotalTime))
            {
                var avg = profile.TotalTime / profile.CallCount;
                Console.WriteLine(
                    $"{systemName,-30} {profile.CallCount,8} {profile.TotalTime,10:F3}ms {avg,10:F3}ms {profile.MinTime,10:F3}ms {profile.MaxTime,10:F3}ms");
            }

            Console.WriteLine();
        }

        private sealed class ProfileData
        {
            public double TotalTime { get; set; }
            public int CallCount { get; set; }
            public double MinTime { get; set; } = double.MaxValue;
            public double MaxTime { get; set; }
        }
    }

    /// <summary>
    /// Demonstrates profiling with system hooks.
    /// </summary>
    public static void ProfilingExample()
    {
        Console.WriteLine("=== System Hooks: Profiling Example ===\n");

        using var world = new World();
        var profiler = new SystemProfiler();

        // Register profiling hook
        var profilingHook = world.AddSystemHook(
            beforeHook: (system, dt) => profiler.BeginSystem(system),
            afterHook: (system, dt) => profiler.EndSystem(system)
        );

        // Add some systems
        world.AddSystem<MovementSystem>();
        world.AddSystem<HealthSystem>();
        world.AddSystem<RenderSystem>();

        // Create entities
        for (int i = 0; i < 1000; i++)
        {
            world.Spawn()
                .With(new Position { X = i * 10f, Y = 0f })
                .With(new Velocity { X = 1f, Y = 0f })
                .Build();
        }

        // Run simulation
        for (int frame = 0; frame < 60; frame++)
        {
            world.Update(0.016f);
        }

        // Print profiling report
        profiler.PrintReport();

        // Clean up
        profilingHook.Dispose();
    }

    /// <summary>
    /// Demonstrates logging with system hooks.
    /// </summary>
    public static void LoggingExample()
    {
        Console.WriteLine("=== System Hooks: Logging Example ===\n");

        using var world = new World();
        var logs = new List<string>();

        // Register logging hook
        var loggingHook = world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                var message = $"[{DateTime.Now:HH:mm:ss.fff}] Starting {system.GetType().Name}";
                logs.Add(message);
                Console.WriteLine(message);
            },
            afterHook: (system, dt) =>
            {
                var message = $"[{DateTime.Now:HH:mm:ss.fff}] Finished {system.GetType().Name}";
                logs.Add(message);
                Console.WriteLine(message);
            }
        );

        // Add systems
        world.AddSystem<MovementSystem>();
        world.AddSystem<HealthSystem>();

        // Run one update
        world.Update(0.016f);

        Console.WriteLine($"\nTotal log entries: {logs.Count}");

        // Clean up
        loggingHook.Dispose();
    }

    /// <summary>
    /// Demonstrates conditional execution with system hooks.
    /// </summary>
    public static void ConditionalExecutionExample()
    {
        Console.WriteLine("=== System Hooks: Conditional Execution Example ===\n");

        using var world = new World();
        var debugMode = false; // Toggle this to enable/disable debug systems

        // Register conditional execution hook
        var conditionalHook = world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                // Disable debug systems when not in debug mode
                if (system.GetType().Name.Contains("Debug") && !debugMode)
                {
                    system.Enabled = false;
                    Console.WriteLine($"Disabled {system.GetType().Name} (debug mode off)");
                }
            },
            afterHook: null
        );

        // Add systems (including a debug system)
        world.AddSystem<MovementSystem>();
        world.AddSystem<HealthSystem>();

        // Run update
        world.Update(0.016f);

        Console.WriteLine($"\nDebug mode: {debugMode}");

        // Clean up
        conditionalHook.Dispose();
    }

    /// <summary>
    /// Demonstrates phase-filtered hooks.
    /// </summary>
    public static void PhaseFilterExample()
    {
        Console.WriteLine("=== System Hooks: Phase Filter Example ===\n");

        using var world = new World();
        var updatePhaseCount = 0;
        var fixedUpdatePhaseCount = 0;

        // Register hook only for Update phase
        var updateHook = world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                updatePhaseCount++;
                Console.WriteLine($"[Update Phase] Executing {system.GetType().Name}");
            },
            afterHook: null,
            phase: SystemPhase.Update
        );

        // Register hook only for FixedUpdate phase
        var fixedUpdateHook = world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                fixedUpdatePhaseCount++;
                Console.WriteLine($"[FixedUpdate Phase] Executing {system.GetType().Name}");
            },
            afterHook: null,
            phase: SystemPhase.FixedUpdate
        );

        // Add systems in different phases
        world.AddSystem<MovementSystem>(SystemPhase.Update);
        world.AddSystem<PhysicsSystem>(SystemPhase.FixedUpdate);

        // Run updates
        world.Update(0.016f);
        world.FixedUpdate(0.02f);

        Console.WriteLine($"\nUpdate phase hook invocations: {updatePhaseCount}");
        Console.WriteLine($"FixedUpdate phase hook invocations: {fixedUpdatePhaseCount}");

        // Clean up
        updateHook.Dispose();
        fixedUpdateHook.Dispose();
    }

    /// <summary>
    /// Demonstrates multiple independent hooks.
    /// </summary>
    public static void MultipleHooksExample()
    {
        Console.WriteLine("=== System Hooks: Multiple Hooks Example ===\n");

        using var world = new World();
        var profiler = new SystemProfiler();
        var executionOrder = new List<string>();

        // Register multiple hooks
        var hook1 = world.AddSystemHook(
            beforeHook: (system, dt) => executionOrder.Add($"Hook1-Before-{system.GetType().Name}"),
            afterHook: (system, dt) => executionOrder.Add($"Hook1-After-{system.GetType().Name}")
        );

        var hook2 = world.AddSystemHook(
            beforeHook: (system, dt) => profiler.BeginSystem(system),
            afterHook: (system, dt) => profiler.EndSystem(system)
        );

        var hook3 = world.AddSystemHook(
            beforeHook: (system, dt) => executionOrder.Add($"Hook3-Before-{system.GetType().Name}"),
            afterHook: (system, dt) => executionOrder.Add($"Hook3-After-{system.GetType().Name}")
        );

        // Add system
        world.AddSystem<MovementSystem>();

        // Run update
        world.Update(0.016f);

        // Show execution order
        Console.WriteLine("Hook execution order:");
        foreach (var entry in executionOrder)
        {
            Console.WriteLine($"  {entry}");
        }

        // Show profiling
        profiler.PrintReport();

        // Clean up
        hook1.Dispose();
        hook2.Dispose();
        hook3.Dispose();
    }

    /// <summary>
    /// Runs all system hooks examples.
    /// </summary>
    public static void RunAll()
    {
        ProfilingExample();
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        LoggingExample();
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        ConditionalExecutionExample();
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        PhaseFilterExample();
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        MultipleHooksExample();
    }
}
