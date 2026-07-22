using System.Text.Json;
using KeenEyes.Common;
using KeenEyes.Editor.Serialization;

namespace KeenEyes.Editor.Tests.Serialization;

public class EditorComponentSerializerTests
{
    private readonly EditorComponentSerializer serializer = new();

    private struct TestComponent
    {
        public float X;
        public float Y;
        public string? Label;
    }

    #region Serialize Tests

    [Fact]
    public void Serialize_WithStructComponent_IncludesFields()
    {
        var value = new TestComponent { X = 1.5f, Y = -2f, Label = "hello" };

        var element = serializer.Serialize(typeof(TestComponent), value);

        Assert.NotNull(element);
        Assert.Equal(1.5f, element.Value.GetProperty("X").GetSingle());
        Assert.Equal(-2f, element.Value.GetProperty("Y").GetSingle());
        Assert.Equal("hello", element.Value.GetProperty("Label").GetString());
    }

    #endregion

    #region Deserialize Tests

    [Fact]
    public void Deserialize_WithSerializedElement_RoundTripsValues()
    {
        var value = new TestComponent { X = 3.25f, Y = 4.75f, Label = "round-trip" };
        var element = serializer.Serialize(typeof(TestComponent), value);
        Assert.NotNull(element);

        var typeName = typeof(TestComponent).AssemblyQualifiedName!;
        var result = serializer.Deserialize(typeName, element.Value);

        var restored = Assert.IsType<TestComponent>(result);
        Assert.Equal(3.25f, restored.X);
        Assert.Equal(4.75f, restored.Y);
        Assert.Equal("round-trip", restored.Label);
    }

    [Fact]
    public void Deserialize_WithUnknownTypeName_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("{}");

        var result = serializer.Deserialize("Not.A.Real.Type", doc.RootElement.Clone());

        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithMalformedElement_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("\"not-an-object\"");

        var result = serializer.Deserialize(
            typeof(TestComponent).AssemblyQualifiedName!,
            doc.RootElement.Clone());

        Assert.Null(result);
    }

    #endregion

    #region GetType Tests

    [Fact]
    public void GetType_WithAssemblyQualifiedName_ResolvesType()
    {
        var resolved = serializer.GetType(typeof(TestComponent).AssemblyQualifiedName!);

        Assert.Equal(typeof(TestComponent), resolved);
    }

    [Fact]
    public void GetType_WithUnknownTypeName_ReturnsNullRepeatedly()
    {
        // Exercised twice to cover both the cache-miss and cache-hit paths.
        Assert.Null(serializer.GetType("Not.A.Real.Type"));
        Assert.Null(serializer.GetType("Not.A.Real.Type"));
    }

    #endregion

    #region CreateDefault Tests

    [Fact]
    public void CreateDefault_WithKnownType_ReturnsDefaultInstance()
    {
        var result = serializer.CreateDefault(typeof(TestComponent).AssemblyQualifiedName!);

        var component = Assert.IsType<TestComponent>(result);
        Assert.True(component.X.IsApproximatelyZero());
        Assert.True(component.Y.IsApproximatelyZero());
        Assert.Null(component.Label);
    }

    [Fact]
    public void CreateDefault_WithUnknownType_ReturnsNull()
    {
        Assert.Null(serializer.CreateDefault("Not.A.Real.Type"));
    }

    #endregion
}
