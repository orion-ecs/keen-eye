using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockInspectionCapabilityTests
{
    #region SetName/GetName

    [Fact]
    public void SetName_StoresName()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);

        capability.SetName(entity, "Player");

        Assert.Equal("Player", capability.GetName(entity));
    }

    [Fact]
    public void SetName_WithNull_StoresNull()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);

        capability.SetName(entity, null);

        Assert.Null(capability.GetName(entity));
    }

    [Fact]
    public void SetName_OverwritesExistingName()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);
        capability.SetName(entity, "OldName");

        capability.SetName(entity, "NewName");

        Assert.Equal("NewName", capability.GetName(entity));
    }

    [Fact]
    public void GetName_WhenEntityNotSet_ReturnsNull()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);

        var name = capability.GetName(entity);

        Assert.Null(name);
    }

    #endregion

    #region AddComponentToEntity/HasComponent

    [Fact]
    public void AddComponentToEntity_WithType_AddsComponent()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);

        capability.AddComponentToEntity(entity, typeof(TestComponent));

        Assert.True(capability.HasComponent(entity, typeof(TestComponent)));
    }

    [Fact]
    public void AddComponentToEntity_Generic_AddsComponent()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);

        capability.AddComponentToEntity<TestComponent>(entity);

        Assert.True(capability.HasComponent(entity, typeof(TestComponent)));
    }

    [Fact]
    public void AddComponentToEntity_MultipleComponents_AddsAll()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);

        capability.AddComponentToEntity<TestComponent>(entity);
        capability.AddComponentToEntity(entity, typeof(OtherComponent));

        Assert.True(capability.HasComponent(entity, typeof(TestComponent)));
        Assert.True(capability.HasComponent(entity, typeof(OtherComponent)));
    }

    [Fact]
    public void HasComponent_WhenNotAdded_ReturnsFalse()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);

        var result = capability.HasComponent(entity, typeof(TestComponent));

        Assert.False(result);
    }

    [Fact]
    public void HasComponent_WhenEntityHasOtherComponent_ReturnsFalse()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);
        capability.AddComponentToEntity<TestComponent>(entity);

        var result = capability.HasComponent(entity, typeof(OtherComponent));

        Assert.False(result);
    }

    #endregion

    #region RegisterComponent/GetRegisteredComponents

    [Fact]
    public void RegisterComponent_Generic_RegistersComponent()
    {
        var capability = new MockInspectionCapability();

        capability.RegisterComponent<TestComponent>();

        var components = capability.GetRegisteredComponents().ToList();
        Assert.Single(components);
        Assert.Equal(typeof(TestComponent), components[0].Type);
        Assert.Equal("TestComponent", components[0].Name);
        Assert.False(components[0].IsTag);
    }

    [Fact]
    public void RegisterComponent_Generic_AsTag_RegistersAsTag()
    {
        var capability = new MockInspectionCapability();

        capability.RegisterComponent<TestComponent>(isTag: true);

        var components = capability.GetRegisteredComponents().ToList();
        Assert.Single(components);
        Assert.True(components[0].IsTag);
        Assert.Equal(0, components[0].Size);
    }

    [Fact]
    public void RegisterComponent_WithType_RegistersComponent()
    {
        var capability = new MockInspectionCapability();

        capability.RegisterComponent(typeof(TestComponent));

        var components = capability.GetRegisteredComponents().ToList();
        Assert.Single(components);
        Assert.Equal(typeof(TestComponent), components[0].Type);
    }

    [Fact]
    public void RegisterComponent_WithType_AsTag_RegistersAsTag()
    {
        var capability = new MockInspectionCapability();

        capability.RegisterComponent(typeof(TestComponent), isTag: true);

        var components = capability.GetRegisteredComponents().ToList();
        Assert.True(components[0].IsTag);
    }

    [Fact]
    public void GetRegisteredComponents_WithMultiple_ReturnsAll()
    {
        var capability = new MockInspectionCapability();
        capability.RegisterComponent<TestComponent>();
        capability.RegisterComponent<OtherComponent>();

        var components = capability.GetRegisteredComponents().ToList();

        Assert.Equal(2, components.Count);
    }

    [Fact]
    public void GetRegisteredComponents_WhenEmpty_ReturnsEmpty()
    {
        var capability = new MockInspectionCapability();

        var components = capability.GetRegisteredComponents();

        Assert.Empty(components);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllState()
    {
        var capability = new MockInspectionCapability();
        var entity = new Entity(1, 0);
        capability.SetName(entity, "Player");
        capability.AddComponentToEntity<TestComponent>(entity);
        capability.RegisterComponent<OtherComponent>();

        capability.Clear();

        Assert.Null(capability.GetName(entity));
        Assert.False(capability.HasComponent(entity, typeof(TestComponent)));
        Assert.Empty(capability.GetRegisteredComponents());
    }

    #endregion

    private struct TestComponent : IComponent;
    private struct OtherComponent : IComponent;
}
