namespace KeenEyes.Tests;

public class EntityNamingTests
{
    [Fact]
    public void Spawn_WithName_AssignsName()
    {
        using var world = new World();

        var entity = world.Spawn("Player").Build();

        Assert.Equal("Player", world.GetName(entity));
    }

    [Fact]
    public void Spawn_WithoutName_HasNullName()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        Assert.Null(world.GetName(entity));
    }

    [Fact]
    public void SetName_UpdatesEntityName()
    {
        using var world = new World();
        var entity = world.Spawn("OldName").Build();

        var result = world.SetName(entity, "NewName");

        Assert.True(result);
        Assert.Equal("NewName", world.GetName(entity));
    }

    [Fact]
    public void SetName_WithNull_RemovesName()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();

        world.SetName(entity, null);

        Assert.Null(world.GetName(entity));
    }

    [Fact]
    public void SetName_ReturnsFalseForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();
        world.Despawn(entity);

        var result = world.SetName(entity, "NewName");

        Assert.False(result);
    }

    [Fact]
    public void GetName_ReturnsNullForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn("Player").Build();
        world.Despawn(entity);

        Assert.Null(world.GetName(entity));
    }

    [Fact]
    public void MultipleEntities_CanHaveSameName()
    {
        using var world = new World();

        var entity1 = world.Spawn("Enemy").Build();
        var entity2 = world.Spawn("Enemy").Build();

        Assert.Equal("Enemy", world.GetName(entity1));
        Assert.Equal("Enemy", world.GetName(entity2));
        Assert.NotEqual(entity1.Id, entity2.Id);
    }
}
