using System.Numerics;

using KeenEyes.Editor.Abstractions.Inspector;
using KeenEyes.Editor.Inspector.Drawers;

namespace KeenEyes.Editor.Tests.Inspector;

public class PropertyDrawerRegistryTests
{
    #region GetDrawer Tests

    [Fact]
    public void GetDrawer_ForInt_ReturnsIntDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<int>();

        Assert.IsType<IntDrawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForFloat_ReturnsFloatDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<float>();

        Assert.IsType<FloatDrawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForBool_ReturnsBoolDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<bool>();

        Assert.IsType<BoolDrawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForString_ReturnsStringDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<string>();

        Assert.IsType<StringDrawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForDouble_ReturnsDoubleDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<double>();

        Assert.IsType<DoubleDrawer>(drawer);
    }

    #endregion

    #region Vector Type Tests

    [Fact]
    public void GetDrawer_ForVector2_ReturnsVector2Drawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<Vector2>();

        Assert.IsType<Vector2Drawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForVector3_ReturnsVector3Drawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<Vector3>();

        Assert.IsType<Vector3Drawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForVector4_ReturnsVector4Drawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<Vector4>();

        Assert.IsType<Vector4Drawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForQuaternion_ReturnsQuaternionDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<Quaternion>();

        Assert.IsType<QuaternionDrawer>(drawer);
    }

    #endregion

    #region Special Type Tests

    [Fact]
    public void GetDrawer_ForEnum_ReturnsEnumDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer(typeof(DayOfWeek));

        Assert.IsType<EnumDrawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForEntity_ReturnsEntityDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<Entity>();

        Assert.IsType<EntityDrawer>(drawer);
    }

    #endregion

    #region Default Drawer Tests

    [Fact]
    public void GetDrawer_ForUnknownType_ReturnsDefaultDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<object>();

        Assert.IsType<DefaultPropertyDrawer>(drawer);
    }

    [Fact]
    public void GetDrawer_ForCustomStruct_ReturnsDefaultDrawer()
    {
        var registry = PropertyDrawerRegistry.Instance;

        var drawer = registry.GetDrawer<CustomStruct>();

        Assert.IsType<DefaultPropertyDrawer>(drawer);
    }

    private struct CustomStruct
    {
#pragma warning disable CS0649 // Field is never assigned to
        public int Value;
#pragma warning restore CS0649
    }

    #endregion

    #region Registration Tests

    [Fact]
    public void HasDrawer_ForRegisteredType_ReturnsTrue()
    {
        var registry = PropertyDrawerRegistry.Instance;

        Assert.True(registry.HasDrawer(typeof(int)));
        Assert.True(registry.HasDrawer(typeof(float)));
        Assert.True(registry.HasDrawer(typeof(bool)));
    }

    [Fact]
    public void HasDrawer_ForUnregisteredType_ReturnsFalse()
    {
        var registry = PropertyDrawerRegistry.Instance;

        Assert.False(registry.HasDrawer(typeof(DateTime)));
        Assert.False(registry.HasDrawer(typeof(Guid)));
    }

    #endregion

    #region TargetType Tests

    [Fact]
    public void IntDrawer_TargetType_ReturnsInt()
    {
        var drawer = new IntDrawer();

        Assert.Equal(typeof(int), drawer.TargetType);
    }

    [Fact]
    public void FloatDrawer_TargetType_ReturnsFloat()
    {
        var drawer = new FloatDrawer();

        Assert.Equal(typeof(float), drawer.TargetType);
    }

    [Fact]
    public void Vector3Drawer_TargetType_ReturnsVector3()
    {
        var drawer = new Vector3Drawer();

        Assert.Equal(typeof(Vector3), drawer.TargetType);
    }

    [Fact]
    public void EnumDrawer_TargetType_ReturnsEnum()
    {
        var drawer = new EnumDrawer();

        Assert.Equal(typeof(Enum), drawer.TargetType);
    }

    [Fact]
    public void EntityDrawer_TargetType_ReturnsEntity()
    {
        var drawer = new EntityDrawer();

        Assert.Equal(typeof(Entity), drawer.TargetType);
    }

    #endregion
}
