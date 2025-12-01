namespace KeenEye.Tests;

public class WorldTests
{
    [Fact]
    public void World_NewWorld_HasEmptyComponentRegistry()
    {
        using var world = new World();

        Assert.NotNull(world.Components);
    }

    [Fact]
    public void World_Spawn_ReturnsEntityBuilder()
    {
        using var world = new World();

        var builder = world.Spawn();

        Assert.NotNull(builder);
    }

    [Fact]
    public void World_Dispose_CanBeCalledMultipleTimes()
    {
        var world = new World();

        world.Dispose();
        world.Dispose(); // Should not throw
    }

    [Fact]
    public void World_GetAllEntities_ReturnsEmpty_WhenNoEntities()
    {
        using var world = new World();

        var entities = world.GetAllEntities().ToList();

        Assert.Empty(entities);
    }

    [Fact]
    public void World_IsAlive_ReturnsFalse_ForInvalidEntity()
    {
        using var world = new World();
        var invalidEntity = new Entity(999, 1);

        var isAlive = world.IsAlive(invalidEntity);

        Assert.False(isAlive);
    }

    [Fact]
    public void World_IsAlive_ReturnsFalse_ForNullEntity()
    {
        using var world = new World();

        var isAlive = world.IsAlive(Entity.Null);

        Assert.False(isAlive);
    }
}
