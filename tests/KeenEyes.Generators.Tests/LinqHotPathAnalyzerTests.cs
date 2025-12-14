using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the LinqHotPathAnalyzer diagnostic analyzer.
/// </summary>
public class LinqHotPathAnalyzerTests
{
    #region KEEN031: LINQ in Update Method

    [Fact]
    public void LinqWhere_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Where", diagnostic.GetMessage());
        Assert.Contains("Update", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqSelect_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var doubled = numbers.Select(n => n * 2);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Select", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqToList_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var list = numbers.ToList();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("ToList", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqAny_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var hasEvens = numbers.Any(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("Any", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqCount_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var count = numbers.Count();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("Count", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqFirst_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var first = numbers.First();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("First", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqOrderBy_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 3, 1, 2 };

                public override void Update(float deltaTime)
                {
                    var sorted = numbers.OrderBy(n => n);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("OrderBy", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqGroupBy_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3, 4 };

                public override void Update(float deltaTime)
                {
                    var groups = numbers.GroupBy(n => n % 2);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("GroupBy", diagnostic.GetMessage());
    }

    [Fact]
    public void MultipleLinqCalls_InUpdateMethod_ReportsMultipleWarnings()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var result = numbers.Where(n => n > 1).Select(n => n * 2).ToList();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.Equal(3, diagnostics.Count(d => d.Id == "KEEN031"));
    }

    #endregion

    #region KEEN031: LINQ in Lifecycle Methods

    [Fact]
    public void LinqWhere_InOnBeforeUpdate_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime) { }

                protected override void OnBeforeUpdate(float deltaTime)
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("OnBeforeUpdate", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqWhere_InOnAfterUpdate_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime) { }

                protected override void OnAfterUpdate(float deltaTime)
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("OnAfterUpdate", diagnostic.GetMessage());
    }

    #endregion

    #region KEEN031: LINQ in HotPath Attributed Methods

    [Fact]
    public void LinqWhere_InHotPathMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestClass
            {
                private int[] numbers = new[] { 1, 2, 3 };

                [HotPath]
                public void ProcessEntities()
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("ProcessEntities", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqWhere_InHotPathMethodWithReason_IncludesReasonInMessage()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestClass
            {
                private int[] numbers = new[] { 1, 2, 3 };

                [HotPath(Reason = "Called every frame")]
                public void ProcessEntities()
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("Called every frame", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqWhere_InPrivateHotPathMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestClass
            {
                private int[] numbers = new[] { 1, 2, 3 };

                [HotPath]
                private void InternalProcess()
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("InternalProcess", diagnostic.GetMessage());
    }

    #endregion

    #region No False Positives

    [Fact]
    public void LinqWhere_InNonHotPathMethod_NoDiagnostic()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestClass
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public void Initialize()
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN031");
    }

    [Fact]
    public void LinqWhere_InOnInitialize_NoDiagnostic()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime) { }

                protected override void OnInitialize()
                {
                    var evens = numbers.Where(n => n % 2 == 0).ToList();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN031");
    }

    [Fact]
    public void ForeachLoop_InUpdateMethod_NoDiagnostic()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    foreach (var n in numbers)
                    {
                        // Process without LINQ
                    }
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN031");
    }

    [Fact]
    public void CustomWhereMethod_InUpdateMethod_NoDiagnostic()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public static class MyExtensions
            {
                public static int[] Where(this int[] arr, int value) => arr;
            }

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var result = numbers.Where(5);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        // Custom Where method from different namespace should not trigger
        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN031");
    }

    [Fact]
    public void NoLinqUsage_InUpdateMethod_NoDiagnostic()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int sum;

                public override void Update(float deltaTime)
                {
                    sum += 1;
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN031");
    }

    [Fact]
    public void LinqInNonSystemClass_WithoutHotPath_NoDiagnostic()
    {
        var source = """
            using System.Linq;

            namespace TestApp;

            public class RegularClass
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public void Update(float deltaTime)
                {
                    // This Update is not in a SystemBase, so no warning
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN031");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void LinqInNestedLambda_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System;
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    Action process = () =>
                    {
                        var evens = numbers.Where(n => n % 2 == 0);
                    };
                    process();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        // The LINQ call is still within the Update method's scope
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("Where", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqDistinct_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 1, 2, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var unique = numbers.Distinct();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("Distinct", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqToArray_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var arr = numbers.ToArray();
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("ToArray", diagnostic.GetMessage());
    }

    [Fact]
    public void LinqToDictionary_InUpdateMethod_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public class TestSystem : SystemBase
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var dict = numbers.ToDictionary(n => n, n => n * 2);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("ToDictionary", diagnostic.GetMessage());
    }

    #endregion

    #region Inheritance Scenarios

    [Fact]
    public void LinqInDerivedSystem_ReportsWarning()
    {
        var source = """
            using System.Linq;
            using KeenEyes;

            namespace TestApp;

            public abstract class BaseSystem : SystemBase
            {
            }

            public class DerivedSystem : BaseSystem
            {
                private int[] numbers = new[] { 1, 2, 3 };

                public override void Update(float deltaTime)
                {
                    var evens = numbers.Where(n => n % 2 == 0);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN031");
        Assert.Contains("Where", diagnostic.GetMessage());
    }

    #endregion

    #region Helper Methods

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source)
    {
        var coreAssembly = typeof(KeenEyes.World).Assembly;
        var abstractionsAssembly = typeof(KeenEyes.SystemBase).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.HashSet<>).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
            MetadataReference.CreateFromFile(abstractionsAssembly.Location),
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

        var analyzer = new LinqHotPathAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }

    #endregion
}
