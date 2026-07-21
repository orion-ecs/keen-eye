using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the network replication source generator, focused on the
/// per-component <c>SyncStrategy</c> metadata it emits into the generated
/// <c>NetworkSerializer</c>.
/// </summary>
public class ReplicatedGeneratorTests
{
    [Theory]
    [InlineData("Authoritative", "SyncStrategy.Authoritative")]
    [InlineData("Interpolated", "SyncStrategy.Interpolated")]
    [InlineData("Predicted", "SyncStrategy.Predicted")]
    [InlineData("OwnerAuthoritative", "SyncStrategy.OwnerAuthoritative")]
    public void ReplicatedGenerator_EmitsCorrectStrategyMetadata(string strategy, string expectedEmit)
    {
        var source = $$"""
            namespace KeenEyes.Network
            {
                public enum SyncStrategy
                {
                    Authoritative = 0,
                    Interpolated = 1,
                    Predicted = 2,
                    OwnerAuthoritative = 3,
                }

                [System.AttributeUsage(System.AttributeTargets.Struct)]
                public sealed class ReplicatedAttribute : System.Attribute
                {
                    public SyncStrategy Strategy { get; set; }
                }
            }

            namespace TestApp
            {
                using KeenEyes.Network;

                [Replicated(Strategy = SyncStrategy.{{strategy}})]
                public partial struct Position
                {
                    public float X;
                    public float Y;
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains($"Strategy = {expectedEmit},"));
    }

    [Fact]
    public void ReplicatedGenerator_DoesNotEmitInvalidStrategyNames()
    {
        // Regression guard: the generator previously emitted the non-existent
        // "OwnerPredicted" and "InterpolatedOnly" enum members.
        var source = """
            namespace KeenEyes.Network
            {
                public enum SyncStrategy
                {
                    Authoritative = 0,
                    Interpolated = 1,
                    Predicted = 2,
                    OwnerAuthoritative = 3,
                }

                [System.AttributeUsage(System.AttributeTargets.Struct)]
                public sealed class ReplicatedAttribute : System.Attribute
                {
                    public SyncStrategy Strategy { get; set; }
                }
            }

            namespace TestApp
            {
                using KeenEyes.Network;

                [Replicated(Strategy = SyncStrategy.OwnerAuthoritative)]
                public partial struct Position
                {
                    public float X;
                }
            }
            """;

        var (_, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(generatedTrees, t => t.Contains("SyncStrategy.OwnerPredicted"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("SyncStrategy.InterpolatedOnly"));
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        };

        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ReplicatedGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();

        return (diagnostics, generatedSources);
    }
}
