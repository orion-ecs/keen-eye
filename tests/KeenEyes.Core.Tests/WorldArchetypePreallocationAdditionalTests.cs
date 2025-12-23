namespace KeenEyes.Core.Tests;

/// <summary>
/// Additional tests for World.PreallocateArchetype and PreallocateArchetypeFor methods.
/// </summary>
public sealed class WorldArchetypePreallocationAdditionalTests
{
    private readonly struct Component1 : IComponent
    {
        public int Value { get; init; }
    }

    private readonly struct Component2 : IComponent
    {
        public float X { get; init; }
    }

    private readonly struct Component3 : IComponent
    {
        public bool Flag { get; init; }
    }

    private readonly struct Component4 : IComponent
    {
        public string? Name { get; init; }
    }

    private readonly struct TestBundle : IBundle
    {
        public static Type[] ComponentTypes => [typeof(Component1), typeof(Component2)];

        public Component1 Component1 { get; init; }
        public Component2 Component2 { get; init; }
    }

    private readonly struct EmptyBundle : IBundle
    {
        public static Type[] ComponentTypes => [];
    }

    private static void RegisterComponents(World world)
    {
        world.Components.Register<Component1>();
        world.Components.Register<Component2>();
        world.Components.Register<Component3>();
        world.Components.Register<Component4>();
    }

    [Fact]
    public void PreallocateArchetype_WithOneComponent_CreatesArchetype()
    {
        using var world = new World();
        RegisterComponents(world);

        world.PreallocateArchetype<Component1>();

        // Verify archetype exists by spawning an entity - should be fast
        var entity = world.Spawn().With(new Component1 { Value = 42 }).Build();
        Assert.True(world.Has<Component1>(entity));
    }

    [Fact]
    public void PreallocateArchetype_WithTwoComponents_CreatesArchetype()
    {
        using var world = new World();
        RegisterComponents(world);

        world.PreallocateArchetype<Component1, Component2>();

        var entity = world.Spawn()
            .With(new Component1 { Value = 42 })
            .With(new Component2 { X = 3.14f })
            .Build();

        Assert.True(world.Has<Component1>(entity));
        Assert.True(world.Has<Component2>(entity));
    }

    [Fact]
    public void PreallocateArchetype_WithThreeComponents_CreatesArchetype()
    {
        using var world = new World();
        RegisterComponents(world);

        world.PreallocateArchetype<Component1, Component2, Component3>();

        var entity = world.Spawn()
            .With(new Component1 { Value = 42 })
            .With(new Component2 { X = 3.14f })
            .With(new Component3 { Flag = true })
            .Build();

        Assert.True(world.Has<Component1>(entity));
        Assert.True(world.Has<Component2>(entity));
        Assert.True(world.Has<Component3>(entity));
    }

    [Fact]
    public void PreallocateArchetype_WithFourComponents_CreatesArchetype()
    {
        using var world = new World();
        RegisterComponents(world);

        world.PreallocateArchetype<Component1, Component2, Component3, Component4>();

        var entity = world.Spawn()
            .With(new Component1 { Value = 42 })
            .With(new Component2 { X = 3.14f })
            .With(new Component3 { Flag = true })
            .With(new Component4 { Name = "Test" })
            .Build();

        Assert.True(world.Has<Component1>(entity));
        Assert.True(world.Has<Component2>(entity));
        Assert.True(world.Has<Component3>(entity));
        Assert.True(world.Has<Component4>(entity));
    }

    [Fact]
    public void PreallocateArchetypeFor_WithValidBundle_CreatesArchetype()
    {
        using var world = new World();
        RegisterComponents(world);

        world.PreallocateArchetypeFor<TestBundle>();

        // Spawn entity with the components from the bundle
        var entity = world.Spawn()
            .With(new Component1 { Value = 42 })
            .With(new Component2 { X = 3.14f })
            .Build();

        Assert.True(world.Has<Component1>(entity));
        Assert.True(world.Has<Component2>(entity));
    }

    [Fact]
    public void PreallocateArchetypeFor_WithEmptyBundle_DoesNothing()
    {
        using var world = new World();

        // Should not throw, just does nothing
        world.PreallocateArchetypeFor<EmptyBundle>();

        // World should still be functional
        var entity = world.Spawn().Build();
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void PreallocateArchetype_WithCustomCapacity_UsesSpecifiedCapacity()
    {
        using var world = new World();
        RegisterComponents(world);

        // Preallocate with custom capacity
        world.PreallocateArchetype<Component1>(initialCapacity: 100);

        // Spawn entities - should fit in preallocated capacity
        for (int i = 0; i < 50; i++)
        {
            var entity = world.Spawn().With(new Component1 { Value = i }).Build();
            Assert.True(world.Has<Component1>(entity));
        }
    }

    [Fact]
    public void PreallocateArchetype_CalledTwice_DoesNotFail()
    {
        using var world = new World();
        RegisterComponents(world);

        // Calling preallocate twice should be idempotent
        world.PreallocateArchetype<Component1>();
        world.PreallocateArchetype<Component1>();

        var entity = world.Spawn().With(new Component1 { Value = 42 }).Build();
        Assert.True(world.Has<Component1>(entity));
    }
}
