using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the IWorldCastAnalyzer diagnostic analyzer.
/// </summary>
public class IWorldCastAnalyzerTests
{
    private const string IWorldInterface = """
        namespace KeenEyes;

        public interface IWorld
        {
            int EntityCount { get; }
        }
        """;

    private const string WorldClass = """
        namespace KeenEyes;

        public sealed class World : IWorld
        {
            public int EntityCount => 0;
        }
        """;

    #region KEEN050: Direct Cast (Type)expression

    [Fact]
    public void DirectCastIWorldToWorld_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    var w = (World)world;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void DirectCastInMethodCall_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    AcceptWorld((World)world);
                }

                private void AcceptWorld(World world) { }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region KEEN050: As Cast

    [Fact]
    public void AsCastIWorldToWorld_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    var w = world as World;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void AsCastWithNullCheck_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    if (world as World != null)
                    {
                        // Do something
                    }
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region KEEN050: Is Type Check

    [Fact]
    public void IsTypeCheck_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public bool IsWorldType(IWorld world)
                {
                    return world is World;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void IsTypeCheckInIfCondition_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    if (world is World)
                    {
                        // Do something
                    }
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region KEEN050: Pattern Matching

    [Fact]
    public void PatternMatchingWithVariable_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    if (world is World w)
                    {
                        var count = w.EntityCount;
                    }
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void PatternMatchingInSwitch_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public string GetTypeName(IWorld world)
                {
                    return world switch
                    {
                        World w => "World",
                        _ => "Unknown"
                    };
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void NegatedPatternMatching_ReportsError()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    if (world is not World)
                    {
                        // Do something
                    }
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN050");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region No False Positives

    [Fact]
    public void CastUnrelatedTypes_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class TestClass
            {
                public void Process(object obj)
                {
                    var str = (string)obj;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN050");
    }

    [Fact]
    public void CastIWorldToOtherInterface_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;
            using System;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    var disposable = (IDisposable)world;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN050");
    }

    [Fact]
    public void ImplicitConversion_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process()
                {
                    IWorld world = new World();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN050");
    }

    [Fact]
    public void WorldToIWorldCast_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(World world)
                {
                    var iworld = (IWorld)world;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN050");
    }

    [Fact]
    public void UsingIWorldInterface_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    var count = world.EntityCount;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN050");
    }

    [Fact]
    public void IsCheckWithOtherType_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public bool IsString(object obj)
                {
                    return obj is string;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN050");
    }

    #endregion

    #region Multiple Violations

    [Fact]
    public void MultipleCasts_ReportsAll()
    {
        var source = """
            namespace TestApp;

            using KeenEyes;

            public class TestClass
            {
                public void Process(IWorld world)
                {
                    var w1 = (World)world;
                    var w2 = world as World;
                    var isWorld = world is World;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.Equal(3, diagnostics.Count(d => d.Id == "KEEN050"));
    }

    #endregion

    #region Helper Methods

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source)
    {
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(IWorldInterface),
            CSharpSyntaxTree.ParseText(WorldClass),
            CSharpSyntaxTree.ParseText(source)
        };

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
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new IWorldCastAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }

    #endregion
}
