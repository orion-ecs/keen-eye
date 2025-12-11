using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the RandomClassAnalyzer diagnostic analyzer.
/// </summary>
public class RandomClassAnalyzerTests
{
    #region KEEN030: Object Creation

    [Fact]
    public void NewRandomDefault_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public void DoSomething()
                {
                    var rng = new System.Random();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN030");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("World.NextInt()", diagnostic.GetMessage());
    }

    [Fact]
    public void NewRandomWithSeed_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public void DoSomething()
                {
                    var rng = new System.Random(42);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN030");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void NewRandomUsingDirective_ReportsWarning()
    {
        var source = """
            using System;

            namespace TestApp;

            public class TestClass
            {
                public void DoSomething()
                {
                    var rng = new Random();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN030");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void NewRandomInFieldInitializer_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                private readonly System.Random _random = new System.Random();
            }
            """;

        var diagnostics = RunAnalyzer(source);

        // Should report both field declaration AND object creation
        Assert.Contains(diagnostics, d => d.Id == "KEEN030");
    }

    #endregion

    #region KEEN030: Field Declarations

    [Fact]
    public void FieldOfTypeRandom_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                private System.Random _random;
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN030");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void StaticFieldOfTypeRandom_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                private static System.Random SharedRandom;
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN030");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void ReadonlyFieldOfTypeRandom_ReportsWarning()
    {
        var source = """
            using System;

            namespace TestApp;

            public class TestClass
            {
                private readonly Random _random;
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN030");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    #endregion

    #region KEEN030: Parameter Declarations

    [Fact]
    public void ParameterOfTypeRandom_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public void DoSomething(System.Random random)
                {
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN030");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void MultipleParametersWithRandom_ReportsWarning()
    {
        var source = """
            using System;

            namespace TestApp;

            public class TestClass
            {
                public void DoSomething(Random random1, int x, Random random2)
                {
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.Equal(2, diagnostics.Count(d => d.Id == "KEEN030"));
    }

    #endregion

    #region World Class Exclusion

    [Fact]
    public void RandomFieldInWorldClass_NoWarning()
    {
        var source = """
            namespace KeenEyes;

            public sealed partial class World
            {
                private readonly System.Random random;
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN030");
    }

    [Fact]
    public void RandomParameterInWorldClass_NoWarning()
    {
        var source = """
            namespace KeenEyes;

            public sealed partial class World
            {
                public void Initialize(System.Random random)
                {
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN030");
    }

    #endregion

    #region No False Positives

    [Fact]
    public void NoRandomUsage_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                private int _value;

                public void DoSomething()
                {
                    var x = 42;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN030");
    }

    [Fact]
    public void OtherRandomClass_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class Random
            {
                public int Next() => 42;
            }

            public class TestClass
            {
                private Random _random;

                public void DoSomething()
                {
                    var rng = new Random();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        // Custom Random class in different namespace should not trigger warning
        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN030");
    }

    [Fact]
    public void UsingWorldNextMethods_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public void DoSomething(KeenEyes.World world)
                {
                    var value = world.NextInt(100);
                    var flag = world.NextBool();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN030");
    }

    #endregion

    #region Multiple Violations

    [Fact]
    public void MultipleRandomUsages_ReportsAll()
    {
        var source = """
            using System;

            namespace TestApp;

            public class TestClass
            {
                private Random _field1;
                private Random _field2;

                public void DoSomething(Random param)
                {
                    var local = new Random();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        // Should detect: 2 fields + 1 parameter + 1 object creation = 4
        Assert.Equal(4, diagnostics.Count(d => d.Id == "KEEN030"));
    }

    #endregion

    #region Helper Methods

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source)
    {
        var coreAssembly = typeof(KeenEyes.World).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
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

        var analyzer = new RandomClassAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }

    #endregion
}
