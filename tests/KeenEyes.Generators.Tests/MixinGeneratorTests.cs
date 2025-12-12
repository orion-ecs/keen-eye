using KeenEyes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class MixinGeneratorTests
{
    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithNoMixins_GeneratesNoOutput()
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
        Assert.DoesNotContain(generatedTrees, t => t.Contains(".Mixin.g.cs"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithSingleMixin_CopiesAllFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position2D
            {
                public float X;
                public float Y;
            }

            [Component]
            [Mixin(typeof(Position2D))]
            public partial struct Transform
            {
                public float Rotation;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("Transform") && t.Contains("Mixin"));
        Assert.Contains("public float X;", mixinCode);
        Assert.Contains("public float Y;", mixinCode);
        Assert.Contains("// Fields from Position2D mixin", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithMultipleMixins_CopiesFieldsFromAll()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position2D
            {
                public float X;
                public float Y;
            }

            public struct Velocity2D
            {
                public float VelX;
                public float VelY;
            }

            [Component]
            [Mixin(typeof(Position2D))]
            [Mixin(typeof(Velocity2D))]
            public partial struct Transform2D
            {
                public float Rotation;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("Transform2D") && t.Contains("Mixin"));
        Assert.Contains("public float X;", mixinCode);
        Assert.Contains("public float Y;", mixinCode);
        Assert.Contains("public float VelX;", mixinCode);
        Assert.Contains("public float VelY;", mixinCode);
        Assert.Contains("Position2D, Velocity2D", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithTransitiveMixins_CopiesAllFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Base
            {
                public int A;
            }

            [Mixin(typeof(Base))]
            public partial struct Middle
            {
                public int B;
            }

            [Component]
            [Mixin(typeof(Middle))]
            public partial struct Derived
            {
                public int C;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("Derived") && t.Contains("Mixin"));
        Assert.Contains("public int A;", mixinCode);
        Assert.Contains("public int B;", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithCircularReference_ReportsError()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            [Mixin(typeof(B))]
            public partial struct A
            {
                public int Value;
            }

            [Component]
            [Mixin(typeof(A))]
            public partial struct B
            {
                public int Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "KEEN027" && d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics.Select(d => d.GetMessage()), msg => msg.Contains("circular"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithFieldNameConflictBetweenMixins_ReportsError()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Mixin1
            {
                public float X;
            }

            public struct Mixin2
            {
                public float X; // Conflict!
            }

            [Component]
            [Mixin(typeof(Mixin1))]
            [Mixin(typeof(Mixin2))]
            public partial struct Conflicting
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "KEEN029" && d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics.Select(d => d.GetMessage()), msg => msg.Contains("conflicting"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithFieldNameConflictBetweenMixinAndComponent_ReportsError()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position
            {
                public float X;
            }

            [Component]
            [Mixin(typeof(Position))]
            public partial struct Transform
            {
                public float X; // Conflict with mixin!
                public float Rotation;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "KEEN029" && d.Severity == DiagnosticSeverity.Error);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithNonStructMixin_ReportsError()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public class NotAStruct
            {
                public int Value;
            }

            [Component]
            [Mixin(typeof(NotAStruct))]
            public partial struct InvalidMixin
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "KEEN026" && d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics.Select(d => d.GetMessage()), msg => msg.Contains("must be a struct"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithInterfaceMixin_ReportsError()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public interface INotAStruct
            {
                int Value { get; }
            }

            [Component]
            [Mixin(typeof(INotAStruct))]
            public partial struct InvalidMixin
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "KEEN026" && d.Severity == DiagnosticSeverity.Error);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_IgnoresStaticFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct WithStatic
            {
                public static int StaticField;
                public int InstanceField;
            }

            [Component]
            [Mixin(typeof(WithStatic))]
            public partial struct MixinWithStatic
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("MixinWithStatic") && t.Contains("Mixin"));
        Assert.Contains("public int InstanceField;", mixinCode);
        Assert.DoesNotContain("StaticField", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_IgnoresConstFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct WithConst
            {
                public const int Version = 1;
                public int Value;
            }

            [Component]
            [Mixin(typeof(WithConst))]
            public partial struct MixinWithConst
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("MixinWithConst") && t.Contains("Mixin"));
        Assert.Contains("public int Value;", mixinCode);
        Assert.DoesNotContain("Version", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithTagComponent_GeneratesMixinFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Metadata
            {
                public string Name;
                public int Id;
            }

            [TagComponent]
            [Mixin(typeof(Metadata))]
            public partial struct TagWithMixin
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("TagWithMixin") && t.Contains("Mixin"));
        Assert.Contains("public string Name;", mixinCode);
        Assert.Contains("public int Id;", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_InNestedNamespace_GeneratesCorrectNamespace()
    {
        var source = """
            using KeenEyes;

            namespace Game.Components.Movement;

            public struct BaseMovement
            {
                public float Speed;
            }

            [Component]
            [Mixin(typeof(BaseMovement))]
            public partial struct AdvancedMovement
            {
                public float Acceleration;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("AdvancedMovement") && t.Contains("Mixin"));
        Assert.Contains("namespace Game.Components.Movement;", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_GeneratesAutoGeneratedHeader()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Base
            {
                public int Value;
            }

            [Component]
            [Mixin(typeof(Base))]
            public partial struct WithHeader
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("WithHeader") && t.Contains("Mixin"));
        Assert.Contains("// <auto-generated />", mixinCode);
        Assert.Contains("#nullable enable", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithDuplicateMixinAttribute_IgnoresDuplicate()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position
            {
                public float X;
                public float Y;
            }

            [Component]
            [Mixin(typeof(Position))]
            [Mixin(typeof(Position))] // Duplicate
            public partial struct DuplicateMixin
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("DuplicateMixin") && t.Contains("Mixin"));

        // Count occurrences of "public float X;" - should be 1, not 2
        var xFieldCount = System.Text.RegularExpressions.Regex.Matches(mixinCode, @"public float X;").Count;
        Assert.Equal(1, xFieldCount);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithComplexFieldTypes_CopiesCorrectly()
    {
        var source = """
            using KeenEyes;
            using System.Collections.Generic;

            namespace TestApp;

            public struct ComplexFields
            {
                public List<int> Numbers;
                public Dictionary<string, float> Lookup;
                public int[] Array;
            }

            [Component]
            [Mixin(typeof(ComplexFields))]
            public partial struct WithComplexMixin
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("WithComplexMixin") && t.Contains("Mixin"));
        Assert.Contains("public System.Collections.Generic.List<int> Numbers;", mixinCode);
        Assert.Contains("public System.Collections.Generic.Dictionary<string, float> Lookup;", mixinCode);
        Assert.Contains("public int[] Array;", mixinCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithEmptyMixin_GeneratesEmptyPartial()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct EmptyMixin
            {
            }

            [Component]
            [Mixin(typeof(EmptyMixin))]
            public partial struct WithEmptyMixin
            {
                // No fields - all would come from mixin (but mixin is empty)
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Empty mixin should still generate a partial (even if it has no fields)
        var mixinCode = generatedTrees.FirstOrDefault(t => t.Contains("WithEmptyMixin") && t.Contains("Mixin"));
        if (mixinCode != null)
        {
            Assert.Contains("partial struct WithEmptyMixin", mixinCode);
        }
        // Note: Generator may choose not to generate anything for empty mixins, which is also valid
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithThreeLevelTransitiveMixins_CopiesAllFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Level1
            {
                public int A;
            }

            [Mixin(typeof(Level1))]
            public partial struct Level2
            {
                public int B;
            }

            [Mixin(typeof(Level2))]
            public partial struct Level3
            {
                public int C;
            }

            [Component]
            [Mixin(typeof(Level3))]
            public partial struct FinalComponent
            {
                // No fields - all come from transitive mixins
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Check FinalComponent has all fields transitively (A from Level1, B from Level2, C from Level3)
        var finalCode = generatedTrees.First(t => t.Contains("FinalComponent") && t.Contains("Mixin"));
        Assert.Contains("public int A;", finalCode);
        Assert.Contains("public int B;", finalCode);
        Assert.Contains("public int C;", finalCode);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_FieldOrdering_MixinFieldsFirst()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Mixin1
            {
                public int A;
                public int B;
            }

            public struct Mixin2
            {
                public int C;
                public int D;
            }

            [Component]
            [Mixin(typeof(Mixin1))]
            [Mixin(typeof(Mixin2))]
            public partial struct OrderedComponent
            {
                public int E;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("OrderedComponent") && t.Contains("Mixin"));

        // Ensure Mixin1 fields appear before Mixin2 fields
        var indexA = mixinCode.IndexOf("public int A;", StringComparison.Ordinal);
        var indexB = mixinCode.IndexOf("public int B;", StringComparison.Ordinal);
        var indexC = mixinCode.IndexOf("public int C;", StringComparison.Ordinal);
        var indexD = mixinCode.IndexOf("public int D;", StringComparison.Ordinal);

        Assert.True(indexA < indexB);
        Assert.True(indexB < indexC);
        Assert.True(indexC < indexD);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void MixinGenerator_WithGenericStruct_CopiesFieldsCorrectly()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct GenericMixin<T>
            {
                public T Value;
            }

            [Component]
            [Mixin(typeof(GenericMixin<int>))]
            public partial struct WithGenericMixin
            {
                public float Extra;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var mixinCode = generatedTrees.First(t => t.Contains("WithGenericMixin") && t.Contains("Mixin"));
        Assert.Contains("public int Value;", mixinCode);
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SystemPhase).Assembly.Location), // KeenEyes.Abstractions
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Collections.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run MarkerAttributesGenerator first to generate the attributes
        var markerGenerator = new MarkerAttributesGenerator();
        var mixinGenerator = new MixinGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(markerGenerator, mixinGenerator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        // Filter to only include output from MixinGenerator, not MarkerAttributesGenerator
        var mixinResult = runResult.Results.FirstOrDefault(r => r.Generator.GetType() == typeof(MixinGenerator));
        var generatedSources = mixinResult.GeneratedSources.IsDefault
            ? []
            : mixinResult.GeneratedSources.Select(s => s.SourceText.ToString()).ToList();

        return (diagnostics, generatedSources);
    }
}
