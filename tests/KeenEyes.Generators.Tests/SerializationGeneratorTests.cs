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
        Assert.Contains(generatedTrees, t => t.Contains("TypesByName"));
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
        Assert.Contains(generatedTrees, t => t.Contains("HashSet<Type>"));
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
                public const int Version = 1;
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("value"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("version"));
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

        var (diagnostics, generatedTrees) = RunGenerator(source);

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
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

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
