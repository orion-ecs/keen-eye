using KeenEyes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class ComponentGeneratorTests
{
    [Fact]
    public void ComponentGenerator_WithNoComponents_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public struct NotAComponent
            {
                public int X;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void ComponentGenerator_WithComponent_GeneratesPartialStruct()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct Position"));
    }

    [Fact]
    public void ComponentGenerator_WithTagComponent_GeneratesTagInterface()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [TagComponent]
            public partial struct Frozen { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("ITagComponent"));
    }

    [Fact]
    public void ComponentGenerator_GeneratesBuilderExtensions()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Velocity
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("WithVelocity"));
    }

    [Fact]
    public void ComponentGenerator_WithDefaultValueAttribute_UsesCustomDefault()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Health
            {
                [DefaultValue(100)]
                public int Current;
                [DefaultValue(100)]
                public int Max;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("int current = 100"));
        Assert.Contains(generatedTrees, t => t.Contains("int max = 100"));
    }

    [Fact]
    public void ComponentGenerator_WithBuilderIgnoreAttribute_ExcludesField()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct CachedData
            {
                public float Value;
                [BuilderIgnore]
                public float CachedResult;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("float value"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("cachedResult"));
    }

    [Fact]
    public void ComponentGenerator_WithFieldInitializer_UsesInitializerAsDefault()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Defaults
            {
                public int Count = 5;
                public float Speed = 1.5f;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("int count = 5"));
        Assert.Contains(generatedTrees, t => t.Contains("float speed = 1.5f"));
    }

    [Theory]
    [InlineData("int", "0")]
    [InlineData("float", "0f")]
    [InlineData("double", "0d")]
    [InlineData("bool", "false")]
    [InlineData("string", "\"\"")]
    [InlineData("long", "0L")]
    [InlineData("byte", "0")]
    [InlineData("short", "0")]
    [InlineData("uint", "0u")]
    [InlineData("ulong", "0ul")]
    [InlineData("decimal", "0m")]
    [InlineData("char", "'\\0'")]
    public void ComponentGenerator_PrimitiveTypes_HaveCorrectDefaults(string type, string expectedDefault)
    {
        var source = $$"""
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct PrimitiveComponent
            {
                public {{type}} Field;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains($"{type} field = {expectedDefault}"));
    }

    [Fact]
    public void ComponentGenerator_WithNoFields_GeneratesBuilder()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Empty { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Verify builder method is generated (signature may vary with no parameters)
        // Should have both generic and non-generic versions
        Assert.Contains(generatedTrees, t => t.Contains("WithEmpty<TSelf>(this TSelf builder"));
        Assert.Contains(generatedTrees, t => t.Contains("WithEmpty(this global::KeenEyes.IEntityBuilder builder"));
        Assert.Contains(generatedTrees, t => t.Contains("new TestApp.Empty"));
    }

    [Fact]
    public void ComponentGenerator_IgnoresStaticFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct WithStatic
            {
                public static int StaticField;
                public int InstanceField;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("int instanceField"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("staticField"));
    }

    [Fact]
    public void ComponentGenerator_IgnoresConstFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct WithConst
            {
                public const int Version = 1;
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("int value"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("version"));
    }

    [Fact]
    public void ComponentGenerator_InGlobalNamespace_GeneratesWithoutNamespace()
    {
        var source = """
            using KeenEyes;

            [Component]
            public partial struct GlobalComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct GlobalComponent"));
    }

    [Fact]
    public void ComponentGenerator_InNestedNamespace_GeneratesCorrectNamespace()
    {
        var source = """
            using KeenEyes;

            namespace Game.Components.Movement;

            [Component]
            public partial struct NestedPosition
            {
                public float X;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("namespace Game.Components.Movement;"));
    }

    [Fact]
    public void ComponentGenerator_MultipleComponents_GeneratesAllExtensions()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct First { public int A; }

            [Component]
            public partial struct Second { public int B; }

            [TagComponent]
            public partial struct ThirdTag { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("WithFirst"));
        Assert.Contains(generatedTrees, t => t.Contains("WithSecond"));
        Assert.Contains(generatedTrees, t => t.Contains("WithThirdTag"));
    }

    [Fact]
    public void ComponentGenerator_GeneratesAutoGeneratedHeader()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct HeaderComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.All(generatedTrees, t => Assert.Contains("// <auto-generated />", t));
        Assert.All(generatedTrees, t => Assert.Contains("#nullable enable", t));
    }

    [Fact]
    public void ComponentGenerator_GeneratesXmlDocumentation()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct DocComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("/// <summary>"));
    }

    [Fact]
    public void ComponentGenerator_ToCamelCase_HandlesVariousNames()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct CamelCaseTest
            {
                public int X;
                public int Y;
                public int LongFieldName;
                public int ID;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("int x"));
        Assert.Contains(generatedTrees, t => t.Contains("int y"));
        Assert.Contains(generatedTrees, t => t.Contains("int longFieldName"));
        Assert.Contains(generatedTrees, t => t.Contains("int iD"));
    }

    [Fact]
    public void ComponentGenerator_TagComponent_GeneratesWithTagMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [TagComponent]
            public partial struct Player { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("WithTag<TestApp.Player>()"));
    }

    [Fact]
    public void ComponentGenerator_WithDefaultValueString_FormatsCorrectly()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Named
            {
                [DefaultValue("Unknown")]
                public string Name;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("string name = \"Unknown\""));
    }

    [Fact]
    public void ComponentGenerator_WithDefaultValueBool_FormatsCorrectly()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Flags
            {
                [DefaultValue(true)]
                public bool IsActive;
                [DefaultValue(false)]
                public bool IsVisible;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("bool isActive = true"));
        Assert.Contains(generatedTrees, t => t.Contains("bool isVisible = false"));
    }

    [Fact]
    public void ComponentGenerator_WithDefaultValueFloat_FormatsCorrectly()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Speed
            {
                [DefaultValue(1.5f)]
                public float Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("float value = 1.5f"));
    }

    [Fact]
    public void ComponentGenerator_WithNullDefaultValue_FormatsNull()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Nullable
            {
                [DefaultValue(null)]
                public string? OptionalName;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("string? optionalName = null"));
    }

    [Fact]
    public void ComponentGenerator_UnknownType_UsesDefault()
    {
        var source = """
            using KeenEyes;
            using System;

            namespace TestApp;

            [Component]
            public partial struct Custom
            {
                public Guid Id;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("System.Guid id = default"));
    }

    [Fact]
    public void ComponentGenerator_OnClass_GeneratesNoOutput()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial class NotAStruct { }
            """;

        var (_, generatedTrees) = RunGenerator(source);

        // Should not generate for classes
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void ComponentGenerator_BuilderExtensionUsesFullyQualifiedName()
    {
        var source = """
            using KeenEyes;

            namespace Game.Components;

            [Component]
            public partial struct QualifiedComponent
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("new Game.Components.QualifiedComponent"));
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SystemPhase).Assembly.Location), // KeenEyes.Abstractions
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Collections.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run MarkerAttributesGenerator first to generate the [Component], [TagComponent], etc. attributes
        var markerGenerator = new MarkerAttributesGenerator();
        var componentGenerator = new ComponentGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(markerGenerator, componentGenerator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        // Filter to only include output from ComponentGenerator, not MarkerAttributesGenerator
        var componentResult = runResult.Results.FirstOrDefault(r => r.Generator.GetType() == typeof(ComponentGenerator));
        var generatedSources = componentResult.GeneratedSources.IsDefault
            ? []
            : componentResult.GeneratedSources.Select(s => s.SourceText.ToString()).ToList();

        return (diagnostics, generatedSources);
    }
}
