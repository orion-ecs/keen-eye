namespace KeenEyes.Tests;

/// <summary>
/// Simple test struct for registry tests.
/// </summary>
public struct RegistryTestComponent : IComponent
{
    public int Value;
}

/// <summary>
/// Another test struct for registry tests.
/// </summary>
public struct AnotherRegistryComponent : IComponent
{
    public string Name;
}

/// <summary>
/// Test tag component for registry tests.
/// </summary>
public struct RegistryTestTag : ITagComponent;

/// <summary>
/// Tests for ComponentRegistry class.
/// </summary>
public class ComponentRegistryTests
{
    #region Constructor and Initial State

    [Fact]
    public void ComponentRegistry_NewInstance_HasEmptyAll()
    {
        var registry = new ComponentRegistry();

        Assert.Empty(registry.All);
    }

    [Fact]
    public void ComponentRegistry_NewInstance_HasZeroCount()
    {
        var registry = new ComponentRegistry();

        Assert.Equal(0, registry.Count);
    }

    #endregion

    #region Register Tests

    [Fact]
    public void Register_FirstComponent_ReturnsInfoWithIdZero()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<RegistryTestComponent>();

        Assert.Equal(0, info.Id.Value);
    }

    [Fact]
    public void Register_SecondComponent_ReturnsInfoWithIdOne()
    {
        var registry = new ComponentRegistry();

        registry.Register<RegistryTestComponent>();
        var info = registry.Register<AnotherRegistryComponent>();

        Assert.Equal(1, info.Id.Value);
    }

    [Fact]
    public void Register_SameTypeTwice_ReturnsSameInfo()
    {
        var registry = new ComponentRegistry();

        var info1 = registry.Register<RegistryTestComponent>();
        var info2 = registry.Register<RegistryTestComponent>();

        Assert.Same(info1, info2);
        Assert.Equal(0, info1.Id.Value);
    }

    [Fact]
    public void Register_ReturnsCorrectType()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<RegistryTestComponent>();

        Assert.Equal(typeof(RegistryTestComponent), info.Type);
    }

    [Fact]
    public void Register_RegularComponent_HasNonZeroSize()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<RegistryTestComponent>();

        Assert.True(info.Size > 0);
    }

    [Fact]
    public void Register_RegularComponent_IsNotTag()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<RegistryTestComponent>();

        Assert.False(info.IsTag);
    }

    [Fact]
    public void Register_TagComponent_HasZeroSize()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<RegistryTestTag>(isTag: true);

        Assert.Equal(0, info.Size);
    }

    [Fact]
    public void Register_TagComponent_IsTag()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<RegistryTestTag>(isTag: true);

        Assert.True(info.IsTag);
    }

    [Fact]
    public void Register_IncrementsCount()
    {
        var registry = new ComponentRegistry();

        registry.Register<RegistryTestComponent>();
        Assert.Equal(1, registry.Count);

        registry.Register<AnotherRegistryComponent>();
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void Register_AddsToAll()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<RegistryTestComponent>();

        Assert.Contains(info, registry.All);
    }

    #endregion

    #region Get Tests

    [Fact]
    public void GetGeneric_RegisteredComponent_ReturnsInfo()
    {
        var registry = new ComponentRegistry();
        var registered = registry.Register<RegistryTestComponent>();

        var retrieved = registry.Get<RegistryTestComponent>();

        Assert.Same(registered, retrieved);
    }

    [Fact]
    public void GetGeneric_UnregisteredComponent_ReturnsNull()
    {
        var registry = new ComponentRegistry();

        var retrieved = registry.Get<RegistryTestComponent>();

        Assert.Null(retrieved);
    }

    [Fact]
    public void GetByType_RegisteredComponent_ReturnsInfo()
    {
        var registry = new ComponentRegistry();
        var registered = registry.Register<RegistryTestComponent>();

        var retrieved = registry.Get(typeof(RegistryTestComponent));

        Assert.Same(registered, retrieved);
    }

    [Fact]
    public void GetByType_UnregisteredComponent_ReturnsNull()
    {
        var registry = new ComponentRegistry();

        var retrieved = registry.Get(typeof(RegistryTestComponent));

        Assert.Null(retrieved);
    }

    #endregion

    #region GetOrRegister Tests

    [Fact]
    public void GetOrRegister_UnregisteredComponent_RegistersAndReturns()
    {
        var registry = new ComponentRegistry();

        var info = registry.GetOrRegister<RegistryTestComponent>();

        Assert.NotNull(info);
        Assert.Equal(typeof(RegistryTestComponent), info.Type);
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void GetOrRegister_RegisteredComponent_ReturnsExisting()
    {
        var registry = new ComponentRegistry();
        var original = registry.Register<RegistryTestComponent>();

        var retrieved = registry.GetOrRegister<RegistryTestComponent>();

        Assert.Same(original, retrieved);
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void GetOrRegister_TagComponent_RegistersAsTag()
    {
        var registry = new ComponentRegistry();

        var info = registry.GetOrRegister<RegistryTestTag>(isTag: true);

        Assert.True(info.IsTag);
        Assert.Equal(0, info.Size);
    }

    #endregion

    #region IsRegistered Tests

    [Fact]
    public void IsRegistered_RegisteredComponent_ReturnsTrue()
    {
        var registry = new ComponentRegistry();
        registry.Register<RegistryTestComponent>();

        Assert.True(registry.IsRegistered<RegistryTestComponent>());
    }

    [Fact]
    public void IsRegistered_UnregisteredComponent_ReturnsFalse()
    {
        var registry = new ComponentRegistry();

        Assert.False(registry.IsRegistered<RegistryTestComponent>());
    }

    [Fact]
    public void IsRegistered_AfterGetOrRegister_ReturnsTrue()
    {
        var registry = new ComponentRegistry();
        registry.GetOrRegister<RegistryTestComponent>();

        Assert.True(registry.IsRegistered<RegistryTestComponent>());
    }

    #endregion

    #region Isolation Tests

    [Fact]
    public void ComponentRegistry_DifferentInstances_HaveIndependentIds()
    {
        var registry1 = new ComponentRegistry();
        var registry2 = new ComponentRegistry();

        var info1 = registry1.Register<RegistryTestComponent>();
        var info2 = registry2.Register<AnotherRegistryComponent>();

        // Both should have ID 0 since they're in different registries
        Assert.Equal(0, info1.Id.Value);
        Assert.Equal(0, info2.Id.Value);
    }

    [Fact]
    public void ComponentRegistry_DifferentInstances_HaveIndependentRegistrations()
    {
        var registry1 = new ComponentRegistry();
        var registry2 = new ComponentRegistry();

        registry1.Register<RegistryTestComponent>();

        Assert.True(registry1.IsRegistered<RegistryTestComponent>());
        Assert.False(registry2.IsRegistered<RegistryTestComponent>());
    }

    #endregion
}

/// <summary>
/// Tests for ComponentInfo class.
/// </summary>
public class ComponentInfoTests
{
    [Fact]
    public void ComponentInfo_Name_ReturnsTypeName()
    {
        var registry = new ComponentRegistry();
        var info = registry.Register<RegistryTestComponent>();

        Assert.Equal("RegistryTestComponent", info.Name);
    }

    [Fact]
    public void ComponentInfo_ToString_ContainsRelevantInfo()
    {
        var registry = new ComponentRegistry();
        var info = registry.Register<RegistryTestComponent>();

        var str = info.ToString();

        Assert.Contains("RegistryTestComponent", str);
        Assert.Contains("Id=", str);
        Assert.Contains("Size=", str);
        Assert.Contains("IsTag=", str);
    }
}

/// <summary>
/// Tests for ComponentMeta static class.
/// </summary>
public class ComponentMetaTests
{
    [Fact]
    public void ComponentMeta_Size_ReturnsCorrectSize()
    {
        // RegistryTestComponent has a single int field (4 bytes)
        Assert.True(ComponentMeta<RegistryTestComponent>.Size > 0);
    }

    [Fact]
    public void ComponentMeta_Type_ReturnsCorrectType()
    {
        Assert.Equal(typeof(RegistryTestComponent), ComponentMeta<RegistryTestComponent>.Type);
    }
}

/// <summary>
/// Tests for ComponentId struct.
/// </summary>
public class ComponentIdTests
{
    [Fact]
    public void ComponentId_Value_ReturnsConstructedValue()
    {
        var id = new ComponentId(42);

        Assert.Equal(42, id.Value);
    }

    [Fact]
    public void ComponentId_Equality_SameValue_AreEqual()
    {
        var id1 = new ComponentId(5);
        var id2 = new ComponentId(5);

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void ComponentId_Equality_DifferentValue_AreNotEqual()
    {
        var id1 = new ComponentId(5);
        var id2 = new ComponentId(10);

        Assert.NotEqual(id1, id2);
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
    }

    [Fact]
    public void ComponentId_GetHashCode_SameValue_SameHash()
    {
        var id1 = new ComponentId(42);
        var id2 = new ComponentId(42);

        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ComponentId_None_HasNegativeValue()
    {
        Assert.Equal(-1, ComponentId.None.Value);
    }

    [Fact]
    public void ComponentId_None_IsNotValid()
    {
        Assert.False(ComponentId.None.IsValid);
    }

    [Fact]
    public void ComponentId_PositiveValue_IsValid()
    {
        var id = new ComponentId(0);

        Assert.True(id.IsValid);
    }

    [Fact]
    public void ComponentId_NegativeValue_IsNotValid()
    {
        var id = new ComponentId(-5);

        Assert.False(id.IsValid);
    }

    [Fact]
    public void ComponentId_CompareTo_ReturnsCorrectOrder()
    {
        var id1 = new ComponentId(5);
        var id2 = new ComponentId(10);
        var id3 = new ComponentId(5);

        Assert.True(id1.CompareTo(id2) < 0);
        Assert.True(id2.CompareTo(id1) > 0);
        Assert.Equal(0, id1.CompareTo(id3));
    }

    [Fact]
    public void ComponentId_ToString_ContainsValue()
    {
        var id = new ComponentId(42);

        var str = id.ToString();

        Assert.Contains("42", str);
        Assert.Contains("ComponentId", str);
    }

    [Fact]
    public void ComponentId_ImplicitConversionToInt_ReturnsValue()
    {
        var id = new ComponentId(42);

        int value = id;

        Assert.Equal(42, value);
    }
}
