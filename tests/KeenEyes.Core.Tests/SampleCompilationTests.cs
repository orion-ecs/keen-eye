namespace KeenEyes.Tests;

/// <summary>
/// Tests to ensure sample code compiles and runs correctly.
/// These tests catch issues like missing using statements that would confuse new users.
/// </summary>
public class SampleCompilationTests
{
    /// <summary>
    /// Verifies that the Simulation sample systems can be instantiated.
    /// This ensures all required using statements are present (e.g., KeenEyes.Commands for CommandBuffer).
    /// </summary>
    [Fact]
    public void SimulationSample_Systems_CanBeInstantiated()
    {
        // This test verifies that all systems from the Simulation sample can be instantiated
        // If the using statement for KeenEyes.Commands is missing, this would fail to compile

        using var world = new World();

        // Test that we can create instances of the systems that use CommandBuffer
        var movementSystem = new Sample.Simulation.MovementSystem();
        var healthSystem = new Sample.Simulation.HealthSystem();
        var lifetimeSystem = new Sample.Simulation.LifetimeSystem();
        var shootingSystem = new Sample.Simulation.ShootingSystem();
        var enemyShootingSystem = new Sample.Simulation.EnemyShootingSystem();

        // Verify systems can be added to world
        world.AddSystem(movementSystem);
        world.AddSystem(healthSystem);
        world.AddSystem(lifetimeSystem);
        world.AddSystem(shootingSystem);
        world.AddSystem(enemyShootingSystem);

        // Verify they were added successfully
        Assert.NotNull(movementSystem);
        Assert.NotNull(healthSystem);
        Assert.NotNull(lifetimeSystem);
        Assert.NotNull(shootingSystem);
        Assert.NotNull(enemyShootingSystem);
    }

    /// <summary>
    /// Verifies that systems from the Simulation sample can execute without errors.
    /// </summary>
    [Fact]
    public void SimulationSample_Systems_CanExecuteUpdate()
    {
        using var world = new World();

        var movementSystem = new Sample.Simulation.MovementSystem();
        world.AddSystem(movementSystem);

        // Should not throw when updating
        world.Update(0.016f);

        Assert.NotNull(movementSystem);
    }
}
