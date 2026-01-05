using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class SerializationGeneratorTests
{
    [Fact]
    public void SerializationGenerator_WithNoSerializableComponents_GeneratesNoOutput()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct NotSerializable
            {
                public int X;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should not generate ComponentSerializer when no components are serializable
        Assert.DoesNotContain(generatedTrees, t => t.Contains("ComponentSerializer"));
    }

    [Fact]
    public void SerializationGenerator_WithSerializableComponent_GeneratesSerializer()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct SerializablePosition
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("class ComponentSerializer"));
        Assert.Contains(generatedTrees, t => t.Contains("IComponentSerializer"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesDeserializeMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct Health
            {
                public int Current;
                public int Max;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Deserialize_Health"));
        Assert.Contains(generatedTrees, t => t.Contains("GetInt32()"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesSerializeMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct Velocity
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Serialize_Velocity"));
        Assert.Contains(generatedTrees, t => t.Contains("WriteNumber"));
    }

    [Fact]
    public void SerializationGenerator_HandlesIntField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct IntComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetInt32()"));
    }

    [Fact]
    public void SerializationGenerator_HandlesFloatField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct FloatComponent
            {
                public float Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetSingle()"));
    }

    [Fact]
    public void SerializationGenerator_HandlesDoubleField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct DoubleComponent
            {
                public double Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetDouble()"));
    }

    [Fact]
    public void SerializationGenerator_HandlesBoolField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BoolComponent
            {
                public bool IsActive;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetBoolean()"));
        Assert.Contains(generatedTrees, t => t.Contains("WriteBoolean"));
    }

    [Fact]
    public void SerializationGenerator_HandlesStringField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct StringComponent
            {
                public string Name;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetString()"));
        Assert.Contains(generatedTrees, t => t.Contains("WriteString"));
    }

    [Fact]
    public void SerializationGenerator_NullableStringField_AllowsNull()
    {
        var source = """
            #nullable enable
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct NullableStringComponent
            {
                public string? Name;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Nullable string should allow null values without throwing
        var generated = generatedTrees.FirstOrDefault(t => t.Contains("Deserialize_NullableStringComponent"));
        Assert.NotNull(generated);
        // Should not contain null-forgiving operator on GetString()
        Assert.DoesNotContain("GetString()!", generated);
        // Should not throw JsonException for this field
        Assert.DoesNotContain("Non-nullable field 'Name' was null", generated);
    }

    [Fact]
    public void SerializationGenerator_NonNullableStringField_DefaultsToEmptyString()
    {
        var source = """
            #nullable enable
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct NonNullableStringComponent
            {
                public string Name;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Non-nullable string should default to empty string when null in JSON
        var generated = generatedTrees.FirstOrDefault(t => t.Contains("Deserialize_NonNullableStringComponent"));
        Assert.NotNull(generated);
        // Should use ?? string.Empty for null coalescing
        Assert.Contains("?? string.Empty", generated);
    }

    [Fact]
    public void SerializationGenerator_HandlesLongField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct LongComponent
            {
                public long Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetInt64()"));
    }

    [Fact]
    public void SerializationGenerator_HandlesDecimalField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct DecimalComponent
            {
                public decimal Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetDecimal()"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesTypesByName()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct TypedComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("ComponentsByName"));
        Assert.Contains(generatedTrees, t => t.Contains("typeof(TestApp.TypedComponent)"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesSerializableTypes()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct SerializableTypes
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("SerializableTypes"));
        Assert.Contains(generatedTrees, t => t.Contains("ComponentsByType"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesInstance()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct InstanceTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static readonly ComponentSerializer Instance"));
    }

    [Fact]
    public void SerializationGenerator_MultipleComponents_GeneratesAllSerializers()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct First { public int A; }

            [Component(Serializable = true)]
            public partial struct Second { public float B; }

            [Component(Serializable = true)]
            public partial struct Third { public string C; }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Deserialize_First"));
        Assert.Contains(generatedTrees, t => t.Contains("Deserialize_Second"));
        Assert.Contains(generatedTrees, t => t.Contains("Deserialize_Third"));
        Assert.Contains(generatedTrees, t => t.Contains("Serialize_First"));
        Assert.Contains(generatedTrees, t => t.Contains("Serialize_Second"));
        Assert.Contains(generatedTrees, t => t.Contains("Serialize_Third"));
    }

    [Fact]
    public void SerializationGenerator_IgnoresStaticFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct WithStatic
            {
                public static int StaticField;
                public int InstanceField;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("instanceField"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("staticField"));
    }

    [Fact]
    public void SerializationGenerator_IgnoresConstFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct WithConst
            {
                public const int ConstantValue = 1;
                public int InstanceValue;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Instance field should be serialized
        Assert.Contains(generatedTrees, t => t.Contains("instanceValue"));
        // Const field should NOT be serialized (no JSON property name for it)
        Assert.DoesNotContain(generatedTrees, t => t.Contains("constantValue"));
    }

    [Fact]
    public void SerializationGenerator_UsesCamelCasePropertyNames()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct CamelCase
            {
                public int SomeValue;
                public float AnotherValue;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("\"someValue\""));
        Assert.Contains(generatedTrees, t => t.Contains("\"anotherValue\""));
    }

    [Fact]
    public void SerializationGenerator_GeneratesAutoGeneratedHeader()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct HeaderTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// <auto-generated />"));
        Assert.Contains(generatedTrees, t => t.Contains("#nullable enable"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesInKeenEyesGeneratedNamespace()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct NamespaceTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("namespace KeenEyes.Generated;"));
    }

    [Fact]
    public void SerializationGenerator_ImplementsIsSerializableType()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct IsSerializableTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public bool IsSerializable(Type type)"));
        Assert.Contains(generatedTrees, t => t.Contains("public bool IsSerializable(string typeName)"));
    }

    [Fact]
    public void SerializationGenerator_ImplementsGetType()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct GetTypeTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Type? IComponentSerializer.GetType(string typeName)"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesGetSerializableTypeNames()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct TypeNamesTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetSerializableTypeNames()"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesGetSerializableTypes()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct TypesTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetSerializableTypes()"));
    }

    [Fact]
    public void SerializationGenerator_MixedSerializable_OnlyGeneratesForMarked()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct SerializableComp { public int A; }

            [Component]
            public partial struct NotSerializable { public int B; }

            [Component(Serializable = true)]
            public partial struct AnotherSerializable { public int C; }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Deserialize_SerializableComp"));
        Assert.Contains(generatedTrees, t => t.Contains("Deserialize_AnotherSerializable"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("Deserialize_NotSerializable"));
    }

    [Fact]
    public void SerializationGenerator_OnClass_GeneratesNoOutput()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial class NotAStruct
            {
                public int Value;
            }
            """;

        var (_, generatedTrees) = RunGenerator(source);

        // Should not generate for classes
        Assert.DoesNotContain(generatedTrees, t => t.Contains("ComponentSerializer"));
    }

    [Fact]
    public void SerializationGenerator_HandlesComplexType()
    {
        var source = """
            using KeenEyes;
            using System;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct ComplexComponent
            {
                public Guid Id;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Complex types should use JsonSerializer.Deserialize
        Assert.Contains(generatedTrees, t => t.Contains("JsonSerializer.Deserialize"));
    }

    #region Binary Serialization Generator Tests

    [Fact]
    public void SerializationGenerator_ImplementsIBinaryComponentSerializer()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("IBinaryComponentSerializer"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesBinaryDeserializers()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryDeserializeTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("BinaryDeserializer"));
        Assert.Contains(generatedTrees, t => t.Contains("DeserializeBinary_BinaryDeserializeTest"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesBinarySerializers()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinarySerializeTest
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("BinarySerializer"));
        Assert.Contains(generatedTrees, t => t.Contains("SerializeBinary_BinarySerializeTest"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesWriteToMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct WriteToTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public bool WriteTo(Type type, object value, BinaryWriter writer)"));
    }

    [Fact]
    public void SerializationGenerator_GeneratesReadFromMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct ReadFromTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public object? ReadFrom(string typeName, BinaryReader reader)"));
    }

    [Fact]
    public void SerializationGenerator_BinaryDeserialize_HandlesIntField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryIntComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("reader.ReadInt32()"));
    }

    [Fact]
    public void SerializationGenerator_BinaryDeserialize_HandlesFloatField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryFloatComponent
            {
                public float Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("reader.ReadSingle()"));
    }

    [Fact]
    public void SerializationGenerator_BinaryDeserialize_HandlesDoubleField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryDoubleComponent
            {
                public double Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("reader.ReadDouble()"));
    }

    [Fact]
    public void SerializationGenerator_BinaryDeserialize_HandlesBoolField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryBoolComponent
            {
                public bool IsActive;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("reader.ReadBoolean()"));
    }

    [Fact]
    public void SerializationGenerator_BinaryDeserialize_HandlesStringField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryStringComponent
            {
                public string Name;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("reader.ReadString()"));
    }

    [Fact]
    public void SerializationGenerator_BinaryDeserialize_HandlesLongField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryLongComponent
            {
                public long Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("reader.ReadInt64()"));
    }

    [Fact]
    public void SerializationGenerator_BinarySerialize_UsesWriterWrite()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryWriteComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("writer.Write(value.Value)"));
    }

    [Fact]
    public void SerializationGenerator_Binary_MultipleComponents_GeneratesAllSerializers()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryFirst { public int A; }

            [Component(Serializable = true)]
            public partial struct BinarySecond { public float B; }

            [Component(Serializable = true)]
            public partial struct BinaryThird { public string C; }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("DeserializeBinary_BinaryFirst"));
        Assert.Contains(generatedTrees, t => t.Contains("DeserializeBinary_BinarySecond"));
        Assert.Contains(generatedTrees, t => t.Contains("DeserializeBinary_BinaryThird"));
        Assert.Contains(generatedTrees, t => t.Contains("SerializeBinary_BinaryFirst"));
        Assert.Contains(generatedTrees, t => t.Contains("SerializeBinary_BinarySecond"));
        Assert.Contains(generatedTrees, t => t.Contains("SerializeBinary_BinaryThird"));
    }

    [Fact]
    public void SerializationGenerator_Binary_ComplexType_UsesJsonSerialization()
    {
        var source = """
            using KeenEyes;
            using System;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct BinaryComplexComponent
            {
                public Guid Id;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Complex types in binary should fall back to JSON serialization
        Assert.Contains(generatedTrees, t => t.Contains("DeserializeBinary_BinaryComplexComponent"));
        Assert.Contains(generatedTrees, t => t.Contains("JsonSerializer.Deserialize"));
    }

    [Fact]
    public void SerializationGenerator_IncludesSystemIOUsing()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct IOTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("using System.IO;"));
    }

    #endregion

    #region Auto-Migration Generator Tests

    [Fact]
    public void SerializationGenerator_WithDefaultValueAndVersion2_GeneratesAutoMigration()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 2)]
            public partial struct Health
            {
                public int Current;
                public int Max;

                [DefaultValue(0)]
                public int Shield;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should generate auto-migration method
        Assert.Contains(generatedTrees, t => t.Contains("AutoMigrate_Health"));
        // Should register migration for version 1
        Assert.Contains(generatedTrees, t => t.Contains("[1] = AutoMigrate_Health"));
    }

    [Fact]
    public void SerializationGenerator_AutoMigration_AppliesDefaultValues()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 2)]
            public partial struct Stats
            {
                public int Strength;

                [DefaultValue(10)]
                public int Agility;

                [DefaultValue(1.5f)]
                public float DamageMultiplier;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var generated = generatedTrees.FirstOrDefault(t => t.Contains("AutoMigrate_Stats"));
        Assert.NotNull(generated);
        // Should apply default value for Agility
        Assert.Contains("result.Agility = 10", generated);
        // Should apply default value for DamageMultiplier
        Assert.Contains("result.DamageMultiplier = 1.5f", generated);
    }

    [Fact]
    public void SerializationGenerator_AutoMigration_ReadsExistingFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 2)]
            public partial struct Position
            {
                public float X;
                public float Y;

                [DefaultValue(0f)]
                public float Z;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var generated = generatedTrees.FirstOrDefault(t => t.Contains("AutoMigrate_Position"));
        Assert.NotNull(generated);
        // Should read existing X and Y fields from JSON
        Assert.Contains("\"x\"", generated);
        Assert.Contains("\"y\"", generated);
        // Should apply default for Z when missing
        Assert.Contains("result.Z = 0f", generated);
    }

    [Fact]
    public void SerializationGenerator_ExplicitMigrateFrom_OverridesAutoMigration()
    {
        var source = """
            using KeenEyes;
            using System.Text.Json;

            namespace TestApp;

            [Component(Serializable = true, Version = 2)]
            public partial struct CustomMigration
            {
                public int Value;

                [DefaultValue(100)]
                public int NewField;

                [MigrateFrom(1)]
                private static CustomMigration MigrateFromV1(JsonElement json)
                {
                    return new CustomMigration
                    {
                        Value = json.GetProperty("value").GetInt32(),
                        NewField = 50
                    };
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should use explicit migration, not auto-migration for version 1
        Assert.Contains(generatedTrees, t => t.Contains("[1] = json => TestApp.CustomMigration.MigrateFromV1(json)"));
        // Should NOT register auto-migration for version 1
        Assert.DoesNotContain(generatedTrees, t => t.Contains("[1] = AutoMigrate_CustomMigration"));
    }

    [Fact]
    public void SerializationGenerator_NoDefaultValue_NoAutoMigration()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 2)]
            public partial struct NoDefaults
            {
                public int Value;
                public float Other;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should NOT generate auto-migration when no fields have [DefaultValue]
        Assert.DoesNotContain(generatedTrees, t => t.Contains("AutoMigrate_NoDefaults"));
    }

    [Fact]
    public void SerializationGenerator_Version1_NoAutoMigration()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 1)]
            public partial struct VersionOne
            {
                public int Value;

                [DefaultValue(0)]
                public int WithDefault;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should NOT generate auto-migration for version 1 components
        Assert.DoesNotContain(generatedTrees, t => t.Contains("AutoMigrate_VersionOne"));
    }

    [Fact]
    public void SerializationGenerator_MultipleVersions_GeneratesAllAutoMigrations()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 4)]
            public partial struct MultiVersion
            {
                public int A;

                [DefaultValue(1)]
                public int B;

                [DefaultValue(2)]
                public int C;

                [DefaultValue(3)]
                public int D;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should generate auto-migration
        Assert.Contains(generatedTrees, t => t.Contains("AutoMigrate_MultiVersion"));
        // Should register migrations for all versions 1-3
        Assert.Contains(generatedTrees, t => t.Contains("[1] = AutoMigrate_MultiVersion"));
        Assert.Contains(generatedTrees, t => t.Contains("[2] = AutoMigrate_MultiVersion"));
        Assert.Contains(generatedTrees, t => t.Contains("[3] = AutoMigrate_MultiVersion"));
    }

    [Fact]
    public void SerializationGenerator_MixedExplicitAndAuto_WorksTogether()
    {
        var source = """
            using KeenEyes;
            using System.Text.Json;

            namespace TestApp;

            [Component(Serializable = true, Version = 3)]
            public partial struct MixedMigration
            {
                public int Value;

                [DefaultValue(0)]
                public int NewInV2;

                [DefaultValue(0)]
                public int NewInV3;

                [MigrateFrom(1)]
                private static MixedMigration MigrateFromV1(JsonElement json)
                {
                    return new MixedMigration
                    {
                        Value = json.GetProperty("value").GetInt32(),
                        NewInV2 = 42,
                        NewInV3 = 0
                    };
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Version 1 should use explicit migration
        Assert.Contains(generatedTrees, t => t.Contains("[1] = json => TestApp.MixedMigration.MigrateFromV1(json)"));
        // Version 2 should use auto-migration
        Assert.Contains(generatedTrees, t => t.Contains("[2] = AutoMigrate_MixedMigration"));
    }

    [Fact]
    public void SerializationGenerator_AutoMigration_HandlesStringDefault()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 2)]
            public partial struct StringDefaults
            {
                public int Id;

                [DefaultValue("Unknown")]
                public string Name;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var generated = generatedTrees.FirstOrDefault(t => t.Contains("AutoMigrate_StringDefaults"));
        Assert.NotNull(generated);
        Assert.Contains("result.Name = \"Unknown\"", generated);
    }

    [Fact]
    public void SerializationGenerator_AutoMigration_HandlesBoolDefault()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true, Version = 2)]
            public partial struct BoolDefaults
            {
                public int Id;

                [DefaultValue(true)]
                public bool IsActive;

                [DefaultValue(false)]
                public bool IsVisible;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var generated = generatedTrees.FirstOrDefault(t => t.Contains("AutoMigrate_BoolDefaults"));
        Assert.NotNull(generated);
        Assert.Contains("result.IsActive = true", generated);
        Assert.Contains("result.IsVisible = false", generated);
    }

    [Fact]
    public void SerializationGenerator_ImplementsIComponentMigrator()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component(Serializable = true)]
            public partial struct MigratorTest
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("IComponentMigrator"));
        Assert.Contains(generatedTrees, t => t.Contains("public bool CanMigrate"));
        Assert.Contains(generatedTrees, t => t.Contains("public JsonElement? Migrate"));
        Assert.Contains(generatedTrees, t => t.Contains("public IEnumerable<int> GetMigrationVersions"));
    }

    #endregion

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var attributesAssembly = typeof(ComponentAttribute).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(attributesAssembly.Location),
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "netstandard.dll")));

        // Add System.Text.Json for migration tests that use JsonElement
        references.Add(MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonElement).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var generator = new SerializationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();

        return (diagnostics, generatedSources);
    }
}
