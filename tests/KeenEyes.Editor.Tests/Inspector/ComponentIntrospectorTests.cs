using System.Numerics;

using KeenEyes.Editor.Abstractions.Inspector;
using KeenEyes.Editor.Common.Inspector;

namespace KeenEyes.Editor.Tests.Inspector;

#region Test Components

[Component]
public partial struct TestComponent
{
    public int PublicField;
    public float AnotherField;

    [HideInInspector]
    public int HiddenField;
}

[Component]
public partial struct AnnotatedComponent
{
    [Header("Movement")]
    [Tooltip("The speed of movement")]
    public float Speed;

    [Range(0, 100)]
    public int Health;

    [ReadOnlyInInspector]
    public int Level;

    [Space(16)]
    [DisplayName("Custom Name")]
    public string? Label;

    [FoldoutGroup("Advanced")]
    public float AdvancedSetting;

    [TextArea(3, 10)]
    public string? Description;
}

[Component]
public partial struct VectorComponent
{
    public Vector2 Position2D;
    public Vector3 Position3D;
    public Vector4 Color;
    public Quaternion Rotation;
}

[Component]
public partial struct CollectionComponent
{
    public int[]? Numbers;
    public List<string>? Names;
}

[Component]
public partial struct EntityRefComponent
{
    public Entity Target;
}

#endregion

public class ComponentIntrospectorTests
{
    public ComponentIntrospectorTests()
    {
        // Clear cache before each test class to ensure isolation
        ComponentIntrospector.ClearCache();
    }

    #region GetEditableFields Tests

    [Fact]
    public void GetEditableFields_ReturnsPublicFields()
    {
        var fields = ComponentIntrospector.GetEditableFields(typeof(TestComponent)).ToList();

        Assert.Equal(2, fields.Count);
        Assert.Contains(fields, f => f.Name == "PublicField");
        Assert.Contains(fields, f => f.Name == "AnotherField");
    }

    [Fact]
    public void GetEditableFields_ExcludesHiddenFields()
    {
        var fields = ComponentIntrospector.GetEditableFields(typeof(TestComponent)).ToList();

        Assert.DoesNotContain(fields, f => f.Name == "HiddenField");
    }

    [Fact]
    public void GetEditableFields_CachesResults()
    {
        var fields1 = ComponentIntrospector.GetEditableFields(typeof(TestComponent));
        var fields2 = ComponentIntrospector.GetEditableFields(typeof(TestComponent));

        Assert.Same(fields1, fields2);
    }

    #endregion

    #region GetFieldMetadata Tests

    [Fact]
    public void GetFieldMetadata_ExtractsHeader()
    {
        var field = typeof(AnnotatedComponent).GetField("Speed")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.Equal("Movement", metadata.Header);
    }

    [Fact]
    public void GetFieldMetadata_ExtractsTooltip()
    {
        var field = typeof(AnnotatedComponent).GetField("Speed")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.Equal("The speed of movement", metadata.Tooltip);
    }

    [Fact]
    public void GetFieldMetadata_ExtractsRange()
    {
        var field = typeof(AnnotatedComponent).GetField("Health")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.NotNull(metadata.Range);
        Assert.Equal(0, metadata.Range.Value.Min);
        Assert.Equal(100, metadata.Range.Value.Max);
    }

    [Fact]
    public void GetFieldMetadata_ExtractsReadOnly()
    {
        var field = typeof(AnnotatedComponent).GetField("Level")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.True(metadata.IsReadOnly);
    }

    [Fact]
    public void GetFieldMetadata_ExtractsSpace()
    {
        var field = typeof(AnnotatedComponent).GetField("Label")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.Equal(16f, metadata.SpaceHeight);
    }

    [Fact]
    public void GetFieldMetadata_ExtractsDisplayName()
    {
        var field = typeof(AnnotatedComponent).GetField("Label")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.Equal("Custom Name", metadata.DisplayName);
    }

    [Fact]
    public void GetFieldMetadata_ExtractsFoldoutGroup()
    {
        var field = typeof(AnnotatedComponent).GetField("AdvancedSetting")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.Equal("Advanced", metadata.FoldoutGroup);
    }

    [Fact]
    public void GetFieldMetadata_ExtractsTextArea()
    {
        var field = typeof(AnnotatedComponent).GetField("Description")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.NotNull(metadata.TextArea);
        Assert.Equal(3, metadata.TextArea.Value.MinLines);
        Assert.Equal(10, metadata.TextArea.Value.MaxLines);
    }

    [Fact]
    public void GetFieldMetadata_FormatsFieldName_WhenNoDisplayName()
    {
        var field = typeof(AnnotatedComponent).GetField("Speed")!;
        var metadata = ComponentIntrospector.GetFieldMetadata(field);

        Assert.Equal("Speed", metadata.DisplayName);
    }

    #endregion

    #region FormatFieldName Tests

    [Fact]
    public void FormatFieldName_InsertsSpaces_BeforeCapitals()
    {
        Assert.Equal("Max Health", ComponentIntrospector.FormatFieldName("maxHealth"));
    }

    [Fact]
    public void FormatFieldName_CapitalizesFirstLetter()
    {
        Assert.Equal("Speed", ComponentIntrospector.FormatFieldName("speed"));
    }

    [Fact]
    public void FormatFieldName_RemovesLeadingUnderscore()
    {
        Assert.Equal("Private Field", ComponentIntrospector.FormatFieldName("_privateField"));
    }

    #endregion

    #region Type Detection Tests

    [Fact]
    public void IsSimpleType_ReturnsTrue_ForPrimitives()
    {
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(int)));
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(float)));
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(bool)));
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(double)));
    }

    [Fact]
    public void IsSimpleType_ReturnsTrue_ForString()
    {
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(string)));
    }

    [Fact]
    public void IsSimpleType_ReturnsTrue_ForEnums()
    {
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(DayOfWeek)));
    }

    [Fact]
    public void IsSimpleType_ReturnsTrue_ForVectors()
    {
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(Vector2)));
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(Vector3)));
        Assert.True(ComponentIntrospector.IsSimpleType(typeof(Vector4)));
    }

    [Fact]
    public void IsVectorType_IdentifiesVectors()
    {
        Assert.True(ComponentIntrospector.IsVectorType(typeof(Vector2)));
        Assert.True(ComponentIntrospector.IsVectorType(typeof(Vector3)));
        Assert.True(ComponentIntrospector.IsVectorType(typeof(Vector4)));
        Assert.True(ComponentIntrospector.IsVectorType(typeof(Quaternion)));
    }

    [Fact]
    public void IsCollectionType_IdentifiesArrays()
    {
        Assert.True(ComponentIntrospector.IsCollectionType(typeof(int[])));
        Assert.True(ComponentIntrospector.IsCollectionType(typeof(string[])));
    }

    [Fact]
    public void IsCollectionType_IdentifiesLists()
    {
        Assert.True(ComponentIntrospector.IsCollectionType(typeof(List<int>)));
        Assert.True(ComponentIntrospector.IsCollectionType(typeof(List<string>)));
    }

    [Fact]
    public void GetCollectionElementType_ReturnsElementType_ForArray()
    {
        Assert.Equal(typeof(int), ComponentIntrospector.GetCollectionElementType(typeof(int[])));
    }

    [Fact]
    public void GetCollectionElementType_ReturnsElementType_ForList()
    {
        Assert.Equal(typeof(string), ComponentIntrospector.GetCollectionElementType(typeof(List<string>)));
    }

    [Fact]
    public void IsEntityType_IdentifiesEntity()
    {
        Assert.True(ComponentIntrospector.IsEntityType(typeof(Entity)));
        Assert.False(ComponentIntrospector.IsEntityType(typeof(int)));
    }

    #endregion

    #region Value Access Tests

    [Fact]
    public void GetFieldValue_ReadsFieldValue()
    {
        var component = new TestComponent { PublicField = 42 };
        var field = typeof(TestComponent).GetField("PublicField")!;

        var value = ComponentIntrospector.GetFieldValue(component, field);

        Assert.Equal(42, value);
    }

    [Fact]
    public void SetFieldValue_WritesFieldValue()
    {
        object component = new TestComponent { PublicField = 0 };
        var field = typeof(TestComponent).GetField("PublicField")!;

        ComponentIntrospector.SetFieldValue(ref component, field, 99);

        Assert.Equal(99, ((TestComponent)component).PublicField);
    }

    #endregion
}
