using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Verifies that generator pipelines cache correctly: when the driver re-runs over
/// semantically unchanged inputs (an unrelated syntax tree is added), every tracked
/// output must be reused from cache instead of being recomputed. This guards the
/// structural equality of the pipeline model records (EquatableArray fields) - a
/// reference-equal-only collection field would make every output report Modified/New.
/// </summary>
public class IncrementalCachingTests
{
    [Fact]
    public void ComponentGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Position { public float X; public float Y; }

            [KeenEyes.TagComponent]
            public partial struct FrozenTag;
            """;

        AssertOutputsCachedOnUnrelatedChange(new ComponentGenerator(), source);
    }

    [Fact]
    public void QueryGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Position { public float X; }

            [KeenEyes.Query]
            public partial struct MovingQuery { public Position Pos; }
            """;

        AssertOutputsCachedOnUnrelatedChange(new QueryGenerator(), source);
    }

    [Fact]
    public void SystemGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.FixedUpdate, Order = 5)]
            [KeenEyes.RunBefore(typeof(OtherSystem))]
            public partial class MovementSystem { }

            [KeenEyes.System]
            public partial class OtherSystem { }
            """;

        AssertOutputsCachedOnUnrelatedChange(new SystemGenerator(), source);
    }

    [Fact]
    public void BundleGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Position { public float X; }

            [KeenEyes.Component]
            public partial struct Velocity { public float X; }

            [KeenEyes.Bundle]
            public partial struct PlayerBundle
            {
                public Position Position;
                public Velocity Velocity;
            }
            """;

        AssertOutputsCachedOnUnrelatedChange(new BundleGenerator(), source);
    }

    [Fact]
    public void MixinGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            public struct Positioned { public float X; public float Y; }

            [KeenEyes.Component]
            [KeenEyes.Mixin(typeof(Positioned))]
            public partial struct Player { public int Health; }
            """;

        AssertOutputsCachedOnUnrelatedChange(new MixinGenerator(), source);
    }

    [Fact]
    public void PluginExtensionGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.PluginExtension("Physics")]
            public class PhysicsWorld { public int Gravity { get; set; } }
            """;

        AssertOutputsCachedOnUnrelatedChange(new PluginExtensionGenerator(), source);
    }

    [Fact]
    public void EditorExtensionGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        // The EditorExtension attribute lives in KeenEyes.Editor.Abstractions, which this
        // test project does not reference; declaring it in-source is sufficient because
        // ForAttributeWithMetadataName matches by full metadata name.
        var source = """
            namespace KeenEyes.Editor.Abstractions
            {
                public sealed class EditorExtensionAttribute : System.Attribute
                {
                    public EditorExtensionAttribute(string propertyName) { }
                    public bool Nullable { get; set; }
                }
            }

            namespace TestApp
            {
                [KeenEyes.Editor.Abstractions.EditorExtension("SceneAnalyzer")]
                public class SceneAnalyzerExtension { }
            }
            """;

        AssertOutputsCachedOnUnrelatedChange(new EditorExtensionGenerator(), source);
    }

    [Fact]
    public void ReplicatedGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        // The Replicated attribute lives in KeenEyes.Network.Abstractions, which this
        // test project does not reference; declaring it in-source is sufficient.
        var source = """
            namespace KeenEyes.Network
            {
                public sealed class ReplicatedAttribute : System.Attribute { }
            }

            namespace TestApp
            {
                [KeenEyes.Network.Replicated]
                public partial struct NetworkedPosition { public float X; public float Y; }
            }
            """;

        AssertOutputsCachedOnUnrelatedChange(new ReplicatedGenerator(), source);
    }

    [Fact]
    public void SerializationGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component(Serializable = true, Version = 2)]
            public partial struct SavedPosition { public float X; public float Y; }
            """;

        AssertOutputsCachedOnUnrelatedChange(new SerializationGenerator(), source);
    }

    [Fact]
    public void ComponentValidationGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Velocity { public float X; }

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(Velocity))]
            public partial struct Position { public float X; }
            """;

        AssertOutputsCachedOnUnrelatedChange(new ComponentValidationGenerator(), source);
    }

    [Fact]
    public void ComponentMigrationMetadataGenerator_UnrelatedChange_ReusesCachedOutputs()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component(Version = 4)]
            public partial struct VersionedComponent { public float X; }

            [KeenEyes.TagComponent]
            public partial struct MarkerTag;
            """;

        AssertOutputsCachedOnUnrelatedChange(new ComponentMigrationMetadataGenerator(), source);
    }

    private static void AssertOutputsCachedOnUnrelatedChange(IIncrementalGenerator generator, string source)
    {
        var compilation = CreateCompilation(source);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        // First run computes everything.
        driver = driver.RunGenerators(compilation);
        var firstResult = driver.GetRunResult().Results[0];
        Assert.NotEmpty(firstResult.GeneratedSources);

        // Second run over the same inputs plus one unrelated tree: the pipeline
        // re-evaluates, but every model value is equal, so outputs must be cached.
        var updatedCompilation = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("// unrelated change that must not invalidate the pipeline"));
        driver = driver.RunGenerators(updatedCompilation);
        var secondResult = driver.GetRunResult().Results[0];

        var outputReasons = secondResult.TrackedOutputSteps
            .SelectMany(static stepsByName => stepsByName.Value)
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .ToList();

        Assert.NotEmpty(outputReasons);
        Assert.All(outputReasons, static reason =>
            Assert.True(
                reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"Expected all outputs to be Cached/Unchanged after an unrelated change, but got {reason}. " +
                "A pipeline model field is likely breaking record value-equality."));
    }

    private static CSharpCompilation CreateCompilation(string source)
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
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "netstandard.dll")));

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
