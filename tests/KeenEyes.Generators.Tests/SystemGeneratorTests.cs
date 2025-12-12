using KeenEyes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class SystemGeneratorTests
{
    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithNoSystems_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public class NotASystem
            {
                public void Update() { }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithSystem_GeneratesPartialClass()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class MovementSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial class MovementSystem"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithDefaultValues_GeneratesUpdatePhase()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class DefaultSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("Phase => global::KeenEyes.SystemPhase.Update"));
        Assert.Contains(generatedTrees, t => t.Contains("Order => 0"));
        Assert.Contains(generatedTrees, t => t.Contains("Group => null"));
    }

    [Trait("Category", "SourceGenerator")]
    [Theory]
    [InlineData("EarlyUpdate")]
    [InlineData("FixedUpdate")]
    [InlineData("Update")]
    [InlineData("LateUpdate")]
    [InlineData("Render")]
    [InlineData("PostRender")]
    public void SystemGenerator_WithPhase_GeneratesCorrectPhase(string phase)
    {
        var source = $$"""
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.{{phase}})]
            public partial class PhaseTestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains($"Phase => global::KeenEyes.SystemPhase.{phase}"));
    }

    [Trait("Category", "SourceGenerator")]
    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void SystemGenerator_WithOrder_GeneratesCorrectOrder(int order)
    {
        var source = $$"""
            namespace TestApp;

            [KeenEyes.System(Order = {{order}})]
            public partial class OrderTestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains($"Order => {order}"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithGroup_GeneratesCorrectGroup()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Group = "Physics")]
            public partial class GroupTestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Group => \"Physics\""));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithAllProperties_GeneratesAllMetadata()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.FixedUpdate, Order = 10, Group = "AI")]
            public partial class FullMetadataSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("Phase => global::KeenEyes.SystemPhase.FixedUpdate"));
        Assert.Contains(generatedTrees, t => t.Contains("Order => 10"));
        Assert.Contains(generatedTrees, t => t.Contains("Group => \"AI\""));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_InGlobalNamespace_GeneratesWithoutNamespace()
    {
        var source = """
            [KeenEyes.System]
            public partial class GlobalSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial class GlobalSystem"));
        // Should not have namespace declaration (only auto-generated header)
        Assert.Contains(generatedTrees, t =>
        {
            var lines = t.Split('\n');
            return !lines.Any(l => l.TrimStart().StartsWith("namespace") && !l.Contains("<global namespace>"));
        });
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_InNestedNamespace_GeneratesCorrectNamespace()
    {
        var source = """
            namespace Game.Systems.Movement;

            [KeenEyes.System]
            public partial class WalkSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("namespace Game.Systems.Movement;"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_GeneratesXmlDocumentation()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class DocumentedSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>The execution phase for this system.</summary>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>The execution order within the phase.</summary>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>The system group name, if any.</summary>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_MultipleSystems_GeneratesSeparateFiles()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Order = 1)]
            public partial class FirstSystem { }

            [KeenEyes.System(Order = 2)]
            public partial class SecondSystem { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial class FirstSystem"));
        Assert.Contains(generatedTrees, t => t.Contains("partial class SecondSystem"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_GeneratesAutoGeneratedHeader()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class HeaderSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// <auto-generated />"));
        Assert.Contains(generatedTrees, t => t.Contains("#nullable enable"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_OnStruct_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial struct NotAClassSystem { }
            """;

        var (_, generatedTrees) = RunGenerator(source);

        // The attribute should not apply to structs, so no output
        Assert.Empty(generatedTrees);
    }

    #region Extension Method Generation Tests

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_GeneratesWorldExtensionMethod()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class MovementSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static global::KeenEyes.World AddMovementSystem(this global::KeenEyes.World world)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_GeneratesSystemGroupExtensionMethod()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class MovementSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static global::KeenEyes.SystemGroup AddMovementSystem(this global::KeenEyes.SystemGroup group)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_ExtensionMethod_UsesCorrectPhaseAndOrder()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.FixedUpdate, Order = 42)]
            public partial class PhysicsSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Check World extension uses correct phase and order
        Assert.Contains(generatedTrees, t =>
            t.Contains("world.AddSystem<global::TestApp.PhysicsSystem>(") &&
            t.Contains("global::KeenEyes.SystemPhase.FixedUpdate,") &&
            t.Contains("order: 42,"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_ExtensionMethod_SystemGroupUsesCorrectOrder()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Order = 100)]
            public partial class LateSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Check SystemGroup extension uses correct order
        Assert.Contains(generatedTrees, t =>
            t.Contains("group.Add<global::TestApp.LateSystem>(order: 100)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_ExtensionMethod_GeneratesExtensionClass()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class InputSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static class InputSystemExtensions"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_ExtensionMethod_GeneratesXmlDocumentation()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.Render, Order = 5, Group = "Rendering")]
            public partial class RenderSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Check XML docs include phase, order, and group info
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <para>Phase: <see cref=\"global::KeenEyes.SystemPhase.Render\"/></para>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <para>Order: 5</para>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <para>Group: Rendering</para>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_ExtensionMethod_InGlobalNamespace_GeneratesCorrectly()
    {
        var source = """
            [KeenEyes.System(Phase = KeenEyes.SystemPhase.EarlyUpdate)]
            public partial class GlobalInputSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static class GlobalInputSystemExtensions"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("world.AddSystem<global::GlobalInputSystem>(") &&
            t.Contains("global::KeenEyes.SystemPhase.EarlyUpdate,") &&
            t.Contains("order: 0,"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_ExtensionMethod_InNestedNamespace_GeneratesCorrectly()
    {
        var source = """
            namespace Game.Systems.AI;

            [KeenEyes.System(Order = -10)]
            public partial class DecisionSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("namespace Game.Systems.AI;") &&
            t.Contains("public static class DecisionSystemExtensions"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("world.AddSystem<global::Game.Systems.AI.DecisionSystem>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_ExtensionMethod_WithNegativeOrder_GeneratesCorrectly()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Order = -50)]
            public partial class PrioritySystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("order: -50"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_MultipleSystems_GeneratesSeparateExtensionClasses()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.EarlyUpdate, Order = 0)]
            public partial class InputSystem { }

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.Update, Order = 10)]
            public partial class MovementSystem { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static class InputSystemExtensions"));
        Assert.Contains(generatedTrees, t => t.Contains("public static class MovementSystemExtensions"));
        Assert.Contains(generatedTrees, t => t.Contains("AddInputSystem"));
        Assert.Contains(generatedTrees, t => t.Contains("AddMovementSystem"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_GeneratesBothPartialAndExtensions()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class TestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Should generate two files: one for partial class, one for extensions
        Assert.Equal(2, generatedTrees.Count);

        // Partial class with static properties
        Assert.Contains(generatedTrees, t =>
            t.Contains("partial class TestSystem") &&
            t.Contains("public static global::KeenEyes.SystemPhase Phase"));

        // Extension class
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static class TestSystemExtensions") &&
            t.Contains("AddTestSystem"));
    }

    #endregion

    #region RunBefore/RunAfter Tests

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithNoConstraints_GeneratesEmptyArrays()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class DefaultSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsBefore => []"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsAfter => []"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithSingleRunBefore_GeneratesCorrectArray()
    {
        var source = """
            namespace TestApp;

            public class TargetSystem { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(TargetSystem))]
            public partial class TestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsBefore => [typeof(global::TestApp.TargetSystem)]"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithSingleRunAfter_GeneratesCorrectArray()
    {
        var source = """
            namespace TestApp;

            public class TargetSystem { }

            [KeenEyes.System]
            [KeenEyes.RunAfter(typeof(TargetSystem))]
            public partial class TestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsAfter => [typeof(global::TestApp.TargetSystem)]"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithMultipleRunBefore_GeneratesCorrectArray()
    {
        var source = """
            namespace TestApp;

            public class Target1 { }
            public class Target2 { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(Target1))]
            [KeenEyes.RunBefore(typeof(Target2))]
            public partial class TestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("typeof(global::TestApp.Target1)") &&
            t.Contains("typeof(global::TestApp.Target2)") &&
            t.Contains("RunsBefore"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithMultipleRunAfter_GeneratesCorrectArray()
    {
        var source = """
            namespace TestApp;

            public class Target1 { }
            public class Target2 { }

            [KeenEyes.System]
            [KeenEyes.RunAfter(typeof(Target1))]
            [KeenEyes.RunAfter(typeof(Target2))]
            public partial class TestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("typeof(global::TestApp.Target1)") &&
            t.Contains("typeof(global::TestApp.Target2)") &&
            t.Contains("RunsAfter"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithBothRunBeforeAndRunAfter_GeneratesBothArrays()
    {
        var source = """
            namespace TestApp;

            public class BeforeTarget { }
            public class AfterTarget { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(BeforeTarget))]
            [KeenEyes.RunAfter(typeof(AfterTarget))]
            public partial class TestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsBefore => [typeof(global::TestApp.BeforeTarget)]"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsAfter => [typeof(global::TestApp.AfterTarget)]"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_WithNestedNamespaceTarget_GeneratesFullyQualifiedType()
    {
        var source = """
            namespace TestApp.Systems;

            public class TargetSystem { }

            [KeenEyes.System]
            [KeenEyes.RunBefore(typeof(TargetSystem))]
            public partial class TestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsBefore => [typeof(global::TestApp.Systems.TargetSystem)]"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_GeneratesDocumentationForConstraints()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class DocumentedSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>Systems that this system must run before.</summary>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>Systems that this system must run after.</summary>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void SystemGenerator_RunBeforeRunAfter_WithAllMetadata_GeneratesComplete()
    {
        var source = """
            namespace TestApp;

            public class Before1 { }
            public class Before2 { }
            public class After1 { }

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.FixedUpdate, Order = 10, Group = "Physics")]
            [KeenEyes.RunBefore(typeof(Before1))]
            [KeenEyes.RunBefore(typeof(Before2))]
            [KeenEyes.RunAfter(typeof(After1))]
            public partial class CompleteSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify all metadata is generated
        Assert.Contains(generatedTrees, t =>
            t.Contains("Phase => global::KeenEyes.SystemPhase.FixedUpdate"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("Order => 10"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("Group => \"Physics\""));
        Assert.Contains(generatedTrees, t =>
            t.Contains("typeof(global::TestApp.Before1)") &&
            t.Contains("typeof(global::TestApp.Before2)"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("RunsAfter => [typeof(global::TestApp.After1)]"));
    }

    #endregion

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
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Collections.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run MarkerAttributesGenerator first to generate the attributes
        var markerGenerator = new MarkerAttributesGenerator();
        var generator = new SystemGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(markerGenerator, generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        // Only get the trees generated by SystemGenerator (skip MarkerAttributesGenerator output)
        var generatorResult = runResult.Results.FirstOrDefault(r => r.Generator.GetType() == typeof(SystemGenerator));
        var generatedSources = generatorResult.GeneratedSources.IsDefault
            ? []
            : generatorResult.GeneratedSources.Select(s => s.SourceText.ToString()).ToList();

        return (diagnostics, generatedSources);
    }
}
