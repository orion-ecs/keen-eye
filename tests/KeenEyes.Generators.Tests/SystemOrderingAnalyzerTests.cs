using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the SystemOrderingAnalyzer diagnostic analyzer.
/// </summary>
public class SystemOrderingAnalyzerTests
{
    #region KEEN001: Self-Referential Constraint

    [Fact]
    public void SelfReferentialRunBefore_ReportsError()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(SelfRefSystem))]
            public partial class SelfRefSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN001");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("SelfRefSystem", diagnostic.GetMessage());
        Assert.Contains("RunBefore", diagnostic.GetMessage());
    }

    [Fact]
    public void SelfReferentialRunAfter_ReportsError()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            [KeenEyes.RunAfter(typeof(SelfRefSystem))]
            public partial class SelfRefSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN001");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("SelfRefSystem", diagnostic.GetMessage());
        Assert.Contains("RunAfter", diagnostic.GetMessage());
    }

    [Fact]
    public void NonSelfReferential_NoError()
    {
        var source = """
            namespace TestApp;

            public class OtherSystem : KeenEyes.ISystem
            {
                public bool Enabled { get; set; }
                public void Initialize(KeenEyes.IWorld world) { }
                public void Update(float deltaTime) { }
                public void Dispose() { }
            }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(OtherSystem))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN001");
    }

    #endregion

    #region KEEN002: Target Not A Class

    [Fact]
    public void RunBeforeWithStruct_ReportsError()
    {
        var source = """
            namespace TestApp;

            public struct NotAClass { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(NotAClass))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN002");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("NotAClass", diagnostic.GetMessage());
        Assert.Contains("struct", diagnostic.GetMessage());
    }

    [Fact]
    public void RunAfterWithInterface_ReportsError()
    {
        var source = """
            namespace TestApp;

            public interface INotAClass { }

            [KeenEyes.System]
            [KeenEyes.RunAfter(typeof(INotAClass))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN002");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("INotAClass", diagnostic.GetMessage());
        Assert.Contains("interface", diagnostic.GetMessage());
    }

    [Fact]
    public void RunBeforeWithEnum_ReportsError()
    {
        var source = """
            namespace TestApp;

            public enum NotAClass { Value }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(NotAClass))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN002");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("NotAClass", diagnostic.GetMessage());
        Assert.Contains("enum", diagnostic.GetMessage());
    }

    [Fact]
    public void RunBeforeWithDelegate_ReportsError()
    {
        var source = """
            namespace TestApp;

            public delegate void NotAClass();

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(NotAClass))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN002");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("NotAClass", diagnostic.GetMessage());
        Assert.Contains("delegate", diagnostic.GetMessage());
    }

    [Fact]
    public void RunBeforeWithClass_NoKEEN002Error()
    {
        var source = """
            namespace TestApp;

            public class ValidClass : KeenEyes.ISystem
            {
                public bool Enabled { get; set; }
                public void Initialize(KeenEyes.IWorld world) { }
                public void Update(float deltaTime) { }
                public void Dispose() { }
            }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(ValidClass))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN002");
    }

    #endregion

    #region KEEN003: Missing [System] Attribute

    [Fact]
    public void RunBeforeWithoutSystemAttribute_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class OtherSystem : KeenEyes.ISystem
            {
                public bool Enabled { get; set; }
                public void Initialize(KeenEyes.IWorld world) { }
                public void Update(float deltaTime) { }
                public void Dispose() { }
            }

            [KeenEyes.RunBefore(typeof(OtherSystem))]
            public partial class MissingSystemAttr { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN003");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("MissingSystemAttr", diagnostic.GetMessage());
    }

    [Fact]
    public void RunAfterWithoutSystemAttribute_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class OtherSystem : KeenEyes.ISystem
            {
                public bool Enabled { get; set; }
                public void Initialize(KeenEyes.IWorld world) { }
                public void Update(float deltaTime) { }
                public void Dispose() { }
            }

            [KeenEyes.RunAfter(typeof(OtherSystem))]
            public partial class MissingSystemAttr { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN003");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("MissingSystemAttr", diagnostic.GetMessage());
    }

    [Fact]
    public void RunBeforeWithSystemAttribute_NoKEEN003Warning()
    {
        var source = """
            namespace TestApp;

            public class OtherSystem : KeenEyes.ISystem
            {
                public bool Enabled { get; set; }
                public void Initialize(KeenEyes.IWorld world) { }
                public void Update(float deltaTime) { }
                public void Dispose() { }
            }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(OtherSystem))]
            public partial class HasSystemAttr { }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN003");
    }

    #endregion

    #region KEEN004: Target Not Implementing ISystem

    [Fact]
    public void RunBeforeWithNonSystem_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class NotASystem { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(NotASystem))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN004");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("NotASystem", diagnostic.GetMessage());
    }

    [Fact]
    public void RunAfterWithNonSystem_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public class NotASystem { }

            [KeenEyes.System]
            [KeenEyes.RunAfter(typeof(NotASystem))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN004");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("NotASystem", diagnostic.GetMessage());
    }

    [Fact]
    public void RunBeforeWithISystemImplementer_NoKEEN004Warning()
    {
        var source = """
            namespace TestApp;

            public class ValidSystem : KeenEyes.ISystem
            {
                public bool Enabled { get; set; }
                public void Initialize(KeenEyes.IWorld world) { }
                public void Update(float deltaTime) { }
                public void Dispose() { }
            }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(ValidSystem))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN004");
    }

    #endregion

    #region Multiple Attributes

    [Fact]
    public void MultipleRunBeforeAttributes_AnalyzesEach()
    {
        var source = """
            namespace TestApp;

            public struct Target1 { }
            public class Target2 { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(Target1))]
            [KeenEyes.RunBefore(typeof(Target2))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        // Target1 is a struct - should get KEEN002
        Assert.Contains(diagnostics, d => d.Id == "KEEN002" && d.GetMessage().Contains("Target1"));
        // Target2 is a class but doesn't implement ISystem - should get KEEN004
        Assert.Contains(diagnostics, d => d.Id == "KEEN004" && d.GetMessage().Contains("Target2"));
    }

    [Fact]
    public void MixedRunBeforeAndRunAfter_AnalyzesAll()
    {
        var source = """
            namespace TestApp;

            public interface ITarget { }
            public class ValidTarget { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(ITarget))]
            [KeenEyes.RunAfter(typeof(ValidTarget))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        // ITarget is an interface - should get KEEN002
        Assert.Contains(diagnostics, d => d.Id == "KEEN002" && d.GetMessage().Contains("ITarget"));
        // ValidTarget is a class but doesn't implement ISystem - should get KEEN004
        Assert.Contains(diagnostics, d => d.Id == "KEEN004" && d.GetMessage().Contains("ValidTarget"));
    }

    #endregion

    #region No False Positives

    [Fact]
    public void ClassWithoutOrderingAttributes_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class PlainSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("KEEN"));
    }

    [Fact]
    public void ValidSystemWithValidTarget_NoDiagnostics()
    {
        var source = """
            namespace TestApp;

            public class OtherSystem : KeenEyes.ISystem
            {
                public bool Enabled { get; set; }
                public void Initialize(KeenEyes.IWorld world) { }
                public void Update(float deltaTime) { }
                public void Dispose() { }
            }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(OtherSystem))]
            [KeenEyes.RunAfter(typeof(OtherSystem))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("KEEN"));
    }

    [Fact]
    public void RunBeforeWithClassTarget_WhenISystemNotInCompilation_NoKEEN004Warning()
    {
        // Test when ISystem interface is not available in compilation
        // This exercises the defensive code path where iSystemType == null
        var source = """
            using System;

            namespace KeenEyes
            {
                // Inline attribute definitions to avoid referencing KeenEyes.Abstractions
                [AttributeUsage(AttributeTargets.Class)]
                public class SystemAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class RunBeforeAttribute : Attribute
                {
                    public RunBeforeAttribute(Type targetSystem) { }
                }
            }

            namespace TestApp;

            public class OtherClass { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(OtherClass))]
            public partial class TestSystem { }
            """;

        var diagnostics = RunAnalyzerWithoutCore(source);

        // Should not report KEEN004 when ISystem can't be resolved
        // (assumes valid to avoid false positives)
        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN004");
    }

    #endregion

    #region Helper Methods

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source)
    {
        var attributesAssembly = typeof(SystemAttribute).Assembly;
        var abstractionsAssembly = typeof(KeenEyes.ISystem).Assembly;
        var coreAssembly = typeof(KeenEyes.World).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(attributesAssembly.Location),
            MetadataReference.CreateFromFile(abstractionsAssembly.Location),
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

        var analyzer = new SystemOrderingAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }

    private static IReadOnlyList<Diagnostic> RunAnalyzerWithoutCore(string source)
    {
        // Don't include any KeenEyes assemblies (attributes are defined inline in test source)
        // This ensures ISystem is not available to test the defensive code path
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            // Intentionally NOT including KeenEyes assemblies to test iSystemType == null path
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

        var analyzer = new SystemOrderingAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }

    #endregion
}
