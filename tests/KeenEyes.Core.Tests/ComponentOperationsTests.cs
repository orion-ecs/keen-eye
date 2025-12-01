namespace KeenEyes.Tests;

public struct Position : IComponent
{
    public float X;
    public float Y;
}

public struct Velocity : IComponent
{
    public float X;
    public float Y;
}

public struct Health : IComponent
{
    public int Current;
    public int Max;
}

public struct EnemyTag : ITagComponent;

public class ComponentOperationsTests
{
    [Fact]
    public void Get_ReturnsComponentData()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        ref var pos = ref world.Get<Position>(entity);

        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
    }

    [Fact]
    public void Get_AllowsModificationViaRef()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        ref var pos = ref world.Get<Position>(entity);
        pos.X = 100;
        pos.Y = 200;

        ref var pos2 = ref world.Get<Position>(entity);
        Assert.Equal(100, pos2.X);
        Assert.Equal(200, pos2.Y);
    }

    [Fact]
    public void Get_ThrowsForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() => world.Get<Position>(entity));
    }

    [Fact]
    public void Get_ThrowsForMissingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        Assert.Throws<KeyNotFoundException>(() => world.Get<Velocity>(entity));
    }

    [Fact]
    public void TryGet_ReturnsTrueWhenComponentExists()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var found = world.TryGet<Position>(entity, out var pos);

        Assert.True(found);
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
    }

    [Fact]
    public void TryGet_ReturnsFalseForMissingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var found = world.TryGet<Velocity>(entity, out var vel);

        Assert.False(found);
        Assert.Equal(default, vel);
    }

    [Fact]
    public void TryGet_ReturnsFalseForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        var found = world.TryGet<Position>(entity, out _);

        Assert.False(found);
    }

    [Fact]
    public void Set_UpdatesComponentData()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Set(entity, new Position { X = 100, Y = 200 });

        ref var pos = ref world.Get<Position>(entity);
        Assert.Equal(100, pos.X);
        Assert.Equal(200, pos.Y);
    }

    [Fact]
    public void Set_ThrowsForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new Position { X = 100, Y = 200 }));
    }

    [Fact]
    public void Set_ThrowsForMissingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        Assert.Throws<KeyNotFoundException>(() =>
            world.Set(entity, new Velocity { X = 1, Y = 1 }));
    }

    [Fact]
    public void Add_AddsNewComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Add(entity, new Velocity { X = 1, Y = 2 });

        Assert.True(world.Has<Velocity>(entity));
        ref var vel = ref world.Get<Velocity>(entity);
        Assert.Equal(1, vel.X);
        Assert.Equal(2, vel.Y);
    }

    [Fact]
    public void Add_ThrowsForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new Velocity { X = 1, Y = 2 }));
    }

    [Fact]
    public void Add_ThrowsForDuplicateComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new Position { X = 100, Y = 200 }));
    }

    [Fact]
    public void AddTag_AddsTagComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.AddTag<EnemyTag>(entity);

        Assert.True(world.Has<EnemyTag>(entity));
    }

    [Fact]
    public void AddTag_ThrowsForDuplicateTag()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .WithTag<EnemyTag>()
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            world.AddTag<EnemyTag>(entity));
    }

    [Fact]
    public void Remove_RemovesExistingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .With(new Velocity { X = 1, Y = 2 })
            .Build();

        var removed = world.Remove<Velocity>(entity);

        Assert.True(removed);
        Assert.False(world.Has<Velocity>(entity));
        Assert.True(world.Has<Position>(entity));
    }

    [Fact]
    public void Remove_ReturnsFalseForMissingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var removed = world.Remove<Velocity>(entity);

        Assert.False(removed);
    }

    [Fact]
    public void Remove_ThrowsForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() => world.Remove<Position>(entity));
    }

    [Fact]
    public void Has_ReturnsTrueForExistingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        Assert.True(world.Has<Position>(entity));
    }

    [Fact]
    public void Has_ReturnsFalseForMissingComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        Assert.False(world.Has<Velocity>(entity));
    }

    [Fact]
    public void Has_ReturnsFalseForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        Assert.False(world.Has<Position>(entity));
    }

    [Fact]
    public void GetComponents_ReturnsAllComponentTypes()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .With(new Velocity { X = 1, Y = 2 })
            .WithTag<EnemyTag>()
            .Build();

        var components = world.GetComponents(entity);

        Assert.Equal(3, components.Count);
        Assert.Contains(typeof(Position), components);
        Assert.Contains(typeof(Velocity), components);
        Assert.Contains(typeof(EnemyTag), components);
    }

    [Fact]
    public void GetComponents_ThrowsForDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() => world.GetComponents(entity));
    }
}
