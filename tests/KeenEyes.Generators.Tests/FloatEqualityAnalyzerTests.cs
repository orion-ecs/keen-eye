using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the FloatEqualityAnalyzer diagnostic analyzer.
/// </summary>
public class FloatEqualityAnalyzerTests
{
    #region KEEN040: Float Equality (==)

    [Fact]
    public void FloatEqualsFloat_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(float a, float b)
                {
                    return a == b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("ApproximatelyEquals()", diagnostic.GetMessage());
    }

    [Fact]
    public void FloatEqualsZero_ReportsWarning_SuggestsIsApproximatelyZero()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool IsZero(float value)
                {
                    return value == 0;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("IsApproximatelyZero()", diagnostic.GetMessage());
    }

    [Fact]
    public void ZeroEqualsFloat_ReportsWarning_SuggestsIsApproximatelyZero()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool IsZero(float value)
                {
                    return 0 == value;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Contains("IsApproximatelyZero()", diagnostic.GetMessage());
    }

    [Fact]
    public void FloatEqualsFloatLiteralZero_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool IsZero(float value)
                {
                    return value == 0.0f;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Contains("IsApproximatelyZero()", diagnostic.GetMessage());
    }

    #endregion

    #region KEEN040: Float Inequality (!=)

    [Fact]
    public void FloatNotEqualsFloat_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(float a, float b)
                {
                    return a != b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("!=", diagnostic.GetMessage());
        Assert.Contains("!ApproximatelyEquals()", diagnostic.GetMessage());
    }

    [Fact]
    public void FloatNotEqualsZero_ReportsWarning_SuggestsNotIsApproximatelyZero()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool IsNotZero(float value)
                {
                    return value != 0;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Contains("!IsApproximatelyZero()", diagnostic.GetMessage());
    }

    #endregion

    #region FloatExtensions Class Exclusion

    [Fact]
    public void FloatComparisonInFloatExtensionsClass_NoWarning()
    {
        var source = """
            namespace KeenEyes.Common;

            public static class FloatExtensions
            {
                public static bool IsApproximatelyZero(this float value)
                {
                    return System.Math.Abs(value) < 1e-6f;
                }

                public static bool ApproximatelyEquals(this float value, float other)
                {
                    return System.Math.Abs(value - other) < 1e-6f;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN040");
    }

    #endregion

    #region No False Positives

    [Fact]
    public void IntegerEquality_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(int a, int b)
                {
                    return a == b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN040");
    }

    [Fact]
    public void StringEquality_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(string a, string b)
                {
                    return a == b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN040");
    }

    [Fact]
    public void ObjectEquality_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(object a, object b)
                {
                    return a == b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN040");
    }

    [Fact]
    public void BooleanEquality_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(bool a, bool b)
                {
                    return a == b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN040");
    }

    [Fact]
    public void FloatLessThan_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(float a, float b)
                {
                    return a < b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN040");
    }

    [Fact]
    public void FloatGreaterThan_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public bool Compare(float a, float b)
                {
                    return a > b;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN040");
    }

    #endregion

    #region Multiple Violations

    [Fact]
    public void MultipleFloatComparisons_ReportsAll()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public void Check(float a, float b, float c)
                {
                    var x = a == b;
                    var y = b != c;
                    var z = c == 0;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.Equal(3, diagnostics.Count(d => d.Id == "KEEN040"));
    }

    #endregion

    #region Field and Property Comparisons

    [Fact]
    public void FieldFloatEquality_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                private float _threshold;

                public bool IsAtThreshold(float value)
                {
                    return value == _threshold;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Contains("ApproximatelyEquals()", diagnostic.GetMessage());
    }

    [Fact]
    public void PropertyFloatEquality_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public float Threshold { get; set; }

                public bool IsAtThreshold(float value)
                {
                    return value == Threshold;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Contains("ApproximatelyEquals()", diagnostic.GetMessage());
    }

    #endregion

    #region Conditional Expressions

    [Fact]
    public void FloatEqualityInTernary_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public string GetStatus(float value)
                {
                    return value == 0 ? "zero" : "non-zero";
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Contains("IsApproximatelyZero()", diagnostic.GetMessage());
    }

    [Fact]
    public void FloatEqualityInIfCondition_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public void Process(float value)
                {
                    if (value == 0)
                    {
                        // Do something
                    }
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN040");
        Assert.Contains("IsApproximatelyZero()", diagnostic.GetMessage());
    }

    #endregion

    #region Helper Methods

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
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

        var analyzer = new FloatEqualityAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }

    #endregion
}
