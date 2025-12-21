using KeenEyes.Capabilities;

namespace KeenEyes.Core.Tests;

/// <summary>
/// Tests for World inspection capabilities.
/// </summary>
public partial class WorldInspectionTests
{
    [Component]
    private partial struct TestPosition
    {
        public float X;
        public float Y;
    }

    [Component]
    private partial struct TestVelocity
    {
#pragma warning disable CS0649 // Field never assigned - used for component registration testing
        public float X;
        public float Y;
#pragma warning restore CS0649
    }

    [TagComponent]
    private partial struct TestTag;

    [Fact]
    public void GetRegisteredComponents_ReturnsEmptyForNewWorld()
    {
        using var world = new World();

        var components = world.GetRegisteredComponents().ToList();

        // No components registered yet (except possibly internal ones)
        Assert.DoesNotContain(components, c => c.Type == typeof(TestPosition));
    }

    [Fact]
    public void GetRegisteredComponents_ReturnsRegisteredComponent()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var components = world.GetRegisteredComponents().ToList();

        Assert.Contains(components, c => c.Type == typeof(TestPosition));
    }

    [Fact]
    public void GetRegisteredComponents_ReturnsCorrectInfo()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var components = world.GetRegisteredComponents().ToList();
        var positionInfo = components.FirstOrDefault(c => c.Type == typeof(TestPosition));

        Assert.NotNull(positionInfo.Type);
        Assert.Equal("TestPosition", positionInfo.Name);
        Assert.True(positionInfo.Size > 0);
        Assert.False(positionInfo.IsTag);
    }

    [Fact]
    public void GetRegisteredComponents_ReturnsTagAsTag()
    {
        using var world = new World();
        world.Components.Register<TestTag>(isTag: true);

        var components = world.GetRegisteredComponents().ToList();
        var tagInfo = components.FirstOrDefault(c => c.Type == typeof(TestTag));

        Assert.NotNull(tagInfo.Type);
        Assert.Equal("TestTag", tagInfo.Name);
        Assert.Equal(0, tagInfo.Size);
        Assert.True(tagInfo.IsTag);
    }

    [Fact]
    public void GetRegisteredComponents_ReturnsMultipleComponents()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();
        world.Components.Register<TestTag>(isTag: true);

        var components = world.GetRegisteredComponents().ToList();

        Assert.Contains(components, c => c.Type == typeof(TestPosition));
        Assert.Contains(components, c => c.Type == typeof(TestVelocity));
        Assert.Contains(components, c => c.Type == typeof(TestTag));
    }

    [Fact]
    public void GetRegisteredComponents_ComponentsFromEntitiesAreRegistered()
    {
        using var world = new World();

        // Spawning an entity with a component auto-registers it
        world.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();

        var components = world.GetRegisteredComponents().ToList();

        Assert.Contains(components, c => c.Type == typeof(TestPosition));
    }
}
