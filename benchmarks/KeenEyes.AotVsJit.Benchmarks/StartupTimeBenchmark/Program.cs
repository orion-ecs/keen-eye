using System.Diagnostics;
using KeenEyes;
using KeenEyes.Common;

// Startup Time Benchmark
// Measures time from process start to first ECS operation
//
// Usage:
//   JIT mode:
//     dotnet run -c Release
//
//   AOT mode:
//     dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT
//     ./bin/Release/net10.0/linux-x64/publish/StartupTimeBenchmark
//
// This benchmark should be run 100+ times and median/p95/p99 calculated.

// Start measuring immediately
var sw = Stopwatch.StartNew();

// Minimal world setup
using var world = new World();
world.AddSystem<MovementSystem>();

// Spawn a single entity
var entity = world.Spawn()
    .WithPosition(0, 0)
    .WithVelocity(1, 1)
    .Build();

// Run one update
world.Update(0.016f);

sw.Stop();

// Output result
Console.WriteLine($"Startup time: {sw.ElapsedMilliseconds}ms ({sw.Elapsed.TotalMicroseconds:F0}μs)");

// Verify entity was processed
ref readonly var pos = ref world.Get<Position>(entity);
if (pos.X.ApproximatelyEquals(0.016f) && pos.Y.ApproximatelyEquals(0.016f))
{
    Console.WriteLine("Verification: PASSED");
}
else
{
    Console.WriteLine($"Verification: FAILED (position = {pos.X}, {pos.Y})");
}

// Type declarations must come after top-level statements

[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

[Component]
public partial struct Velocity
{
    public float X;
    public float Y;
}

[System]
public partial class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}
