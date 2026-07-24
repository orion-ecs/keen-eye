using System.Reflection;
using System.Text.Json;
using KeenEyes.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// End-to-end verification that the generators emit code which actually compiles against
/// KeenEyes.Core (and round-trips at runtime) for the component/bundle/replicated shapes
/// that used to break only for consumers - the latent bugs in #1137-#1146.
/// </summary>
/// <remarks>
/// Unlike the string-matching generator tests, these drive the generator, add the emitted
/// trees back into a compilation that references the real runtime assemblies, and assert the
/// result has no compile errors (and, for serialization, emit + load the assembly and invoke
/// the generated <c>ComponentSerializer</c>).
/// </remarks>
public class GeneratorShapeTests
{
    private static readonly MetadataReference[] references = BuildReferences();

    private static MetadataReference[] BuildReferences()
    {
        // Explicitly pull in the key runtime assemblies (their .Assembly access forces a load
        // and is not elided), then add everything else already loaded (System.*, etc.).
        var explicitAssemblies = new[]
        {
            typeof(global::KeenEyes.World).Assembly,
            typeof(global::KeenEyes.IComponent).Assembly,
            typeof(global::KeenEyes.Common.FloatExtensions).Assembly,
            typeof(global::KeenEyes.Network.Serialization.BitWriter).Assembly,
            typeof(global::KeenEyes.Network.ReplicatedAttribute).Assembly,
            typeof(global::KeenEyes.Serialization.IComponentSerializer).Assembly,
            // System.Text.Json is used by the generated serializer but may not yet be loaded
            // when this static initializer runs, so pull it in explicitly.
            typeof(System.Text.Json.JsonElement).Assembly,
        };

        return AppDomain.CurrentDomain.GetAssemblies()
            .Concat(explicitAssemblies)
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Distinct()
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToArray();
    }

    private static (Compilation Output, IReadOnlyList<Diagnostic> GeneratorDiagnostics, IReadOnlyList<string> Sources) Run(
        string source, params IIncrementalGenerator[] generators)
    {
        // Use default parse options so the source tree's language version matches the
        // trees the generator driver produces (otherwise CSharpCompilation rejects the mix).
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            "ShapeTestAssembly_" + Guid.NewGuid().ToString("N"),
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var runResult = driver.GetRunResult();
        var sources = runResult.GeneratedTrees.Select(t => t.GetText().ToString()).ToList();
        return (output, runResult.Diagnostics.ToList(), sources);
    }

    private static void AssertNoCompileErrors(Compilation output)
    {
        var errors = output.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(
            errors.Count == 0,
            "Generated code failed to compile against KeenEyes.Core:\n" +
            string.Join("\n", errors.Select(e => e.ToString())));
    }

    private static Assembly EmitAndLoad(Compilation output)
    {
        using var stream = new MemoryStream();
        var result = output.Emit(stream);

        Assert.True(
            result.Success,
            "Emit failed:\n" + string.Join(
                "\n",
                result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString())));

        return Assembly.Load(stream.ToArray());
    }

    #region #1138 - Bundle helpers must compile against Core (no consumer-side World partial)

    [Fact]
    public void BundleGenerator_SingleBundle_CompilesAgainstCore()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp;

            [Component]
            public partial struct Position { public float X; public float Y; }

            [Component]
            public partial struct Rotation { public float Angle; }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
            }
            """;

        var (output, _, _) = Run(source, new ComponentGenerator(), new BundleGenerator());

        AssertNoCompileErrors(output);
    }

    [Fact]
    public void BundleGenerator_TwoBundles_DoNotProduceDuplicateMembers()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp;

            [Component]
            public partial struct Position { public float X; public float Y; }

            [Component]
            public partial struct Velocity { public float X; public float Y; }

            [Component]
            public partial struct Health { public int Current; public int Max; }

            [Bundle]
            public partial struct PhysicsBundle
            {
                public Position Position;
                public Velocity Velocity;
            }

            [Bundle]
            public partial struct HealthBundle
            {
                public Health Health;
            }
            """;

        var (output, _, _) = Run(source, new ComponentGenerator(), new BundleGenerator());

        // Two bundles previously each emitted a colliding World.Query<T>() (CS0111) inside a
        // consumer-side World partial that also failed with CS0759/CS0103/CS1061.
        AssertNoCompileErrors(output);
    }

    #endregion

    #region #1140 - Optional nullable bundle field compiles (no world.Get<T?>)

    [Fact]
    public void BundleGenerator_OptionalNullableField_CompilesAgainstCore()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp;

            [Component]
            public partial struct Position { public float X; public float Y; }

            [Component]
            public partial struct Tint { public float R; public float G; public float B; }

            [Bundle]
            public partial struct SpriteBundle
            {
                public Position Position;

                [Optional]
                public Tint? Tint;
            }
            """;

        var (output, _, _) = Run(source, new ComponentGenerator(), new BundleGenerator());

        AssertNoCompileErrors(output);
    }

    #endregion

    #region #1142 - Nested bundle field compiles (recursive Add)

    [Fact]
    public void BundleGenerator_NestedBundle_CompilesAgainstCore()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp;

            [Component]
            public partial struct Position { public float X; public float Y; }

            [Component]
            public partial struct Rotation { public float Angle; }

            [Component]
            public partial struct Health { public int Current; public int Max; }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
            }

            [Bundle]
            public partial struct CharacterBundle
            {
                public TransformBundle Transform;
                public Health Health;
            }
            """;

        var (output, _, _) = Run(source, new ComponentGenerator(), new BundleGenerator());

        // The Add side previously emitted world.Add(entity, bundle.Transform) for the nested
        // bundle field (CS0315). It must recurse into the nested bundle's Add overload instead.
        AssertNoCompileErrors(output);
    }

    #endregion

    #region #1141 - Auto-property components compile (no <X>k__BackingField)

    [Fact]
    public void SerializationGenerator_AutoPropertyComponent_CompilesAndOmitsBackingField()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp;

            [Component(Serializable = true)]
            public partial struct Stats
            {
                public int Level { get; set; }
                public float Ratio;
            }
            """;

        var (output, _, sources) = Run(source, new ComponentGenerator(), new SerializationGenerator());

        Assert.DoesNotContain(sources, s => s.Contains("k__BackingField"));
        AssertNoCompileErrors(output);
    }

    #endregion

    #region #1137 / #1139 - Complex serializable field shapes compile and round-trip

    [Fact]
    public void SerializationGenerator_ComplexFieldShapes_CompileAndRoundTrip()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp;

            public enum Facing { North, East, South, West }

            public struct Inner
            {
                public int A;
                public int B;
            }

            [Component(Serializable = true)]
            public partial struct Sample
            {
                public Facing Direction;              // enum -> Object
                public System.Numerics.Vector2 Offset; // Vector2 -> Object
                public Inner Nested;                   // nested struct -> Object
                public char Symbol;                    // char -> Object
                public string Label;                   // string
            }
            """;

        var (output, _, _) = Run(source, new ComponentGenerator(), new SerializationGenerator());

        // #1137: non-nullable value-type Object fields must not get `?? throw` (CS0019).
        AssertNoCompileErrors(output);

        var assembly = EmitAndLoad(output);
        var sampleType = assembly.GetType("ShapeApp.Sample")!;
        var facingType = assembly.GetType("ShapeApp.Facing")!;

        var directionField = sampleType.GetField("Direction")!;
        var symbolField = sampleType.GetField("Symbol")!;
        var labelField = sampleType.GetField("Label")!;

        // Leave Label null to exercise #1139 (binary write must not throw on a null string).
        object value = Activator.CreateInstance(sampleType)!;
        directionField.SetValue(value, Enum.ToObject(facingType, 2)); // Facing.South
        symbolField.SetValue(value, 'Z');

        var serializerType = assembly.GetType("KeenEyes.Generated.ComponentSerializer")!;
        var instance = serializerType.GetField("Instance")!.GetValue(null)!;
        var jsonSerializer = (IComponentSerializer)instance;
        var binarySerializer = (IBinaryComponentSerializer)instance;

        // JSON round-trip
        JsonElement? json = jsonSerializer.Serialize(sampleType, value);
        Assert.NotNull(json);
        object jsonBack = jsonSerializer.Deserialize("ShapeApp.Sample", json!.Value)!;

        Assert.Equal(
            Convert.ToInt32(directionField.GetValue(value)),
            Convert.ToInt32(directionField.GetValue(jsonBack)));
        Assert.Equal('Z', (char)symbolField.GetValue(jsonBack)!);
        Assert.Equal(string.Empty, (string)labelField.GetValue(jsonBack)!);

        // Binary round-trip - must not throw on the null Label (#1139).
        using var memory = new MemoryStream();
        using (var writer = new BinaryWriter(memory, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            Assert.True(binarySerializer.WriteTo(sampleType, value, writer));
        }

        memory.Position = 0;
        object binaryBack;
        using (var reader = new BinaryReader(memory, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            binaryBack = binarySerializer.ReadFrom("ShapeApp.Sample", reader)!;
        }

        Assert.Equal(
            Convert.ToInt32(directionField.GetValue(value)),
            Convert.ToInt32(directionField.GetValue(binaryBack)));
        Assert.Equal('Z', (char)symbolField.GetValue(binaryBack)!);
        Assert.Equal(string.Empty, (string)labelField.GetValue(binaryBack)!);
    }

    #endregion

    #region #1143 - [Replicated] long/ulong fields emit no #error

    [Fact]
    public void ReplicatedGenerator_LongAndUlongFields_CompileWithoutErrorDirective()
    {
        var source = """
            using KeenEyes.Network;

            namespace ShapeApp;

            [Replicated]
            public partial struct BigCounters
            {
                public long Signed;
                public ulong Unsigned;
            }
            """;

        var (output, _, sources) = Run(source, new ReplicatedGenerator());

        Assert.DoesNotContain(sources, s => s.Contains("#error"));
        AssertNoCompileErrors(output);
    }

    #endregion

    #region #1144 - Same-named serializable components in different namespaces

    [Fact]
    public void SerializationGenerator_SameNameDifferentNamespaces_CompileWithoutCollision()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp.Ui
            {
                [Component(Serializable = true)]
                public partial struct Anchor { public int X; }
            }

            namespace ShapeApp.Physics
            {
                [Component(Serializable = true)]
                public partial struct Anchor { public int Y; }
            }
            """;

        var (output, _, _) = Run(source, new ComponentGenerator(), new SerializationGenerator());

        // Both used to emit Deserialize_Anchor / info_Anchor (CS0111 / CS0128).
        AssertNoCompileErrors(output);
    }

    #endregion

    #region #1145 - Diagnostic for more than 32 replicated fields

    [Fact]
    public void ReplicatedGenerator_MoreThan32Fields_EmitsTooManyFieldsDiagnostic()
    {
        var fields = string.Join(
            "\n    ",
            Enumerable.Range(0, 33).Select(i => $"public int Field{i};"));

        var source = $$"""
            using KeenEyes.Network;

            namespace ShapeApp;

            [Replicated]
            public partial struct Wide
            {
                {{fields}}
            }
            """;

        var (_, generatorDiagnostics, _) = Run(source, new ReplicatedGenerator());

        Assert.Contains(generatorDiagnostics, d => d.Id == "KEEN100");
    }

    [Fact]
    public void ReplicatedGenerator_Exactly32Fields_EmitsNoDiagnostic()
    {
        var fields = string.Join(
            "\n    ",
            Enumerable.Range(0, 32).Select(i => $"public int Field{i};"));

        var source = $$"""
            using KeenEyes.Network;

            namespace ShapeApp;

            [Replicated]
            public partial struct Exactly32
            {
                {{fields}}
            }
            """;

        var (_, generatorDiagnostics, _) = Run(source, new ReplicatedGenerator());

        Assert.DoesNotContain(generatorDiagnostics, d => d.Id == "KEEN100");
    }

    #endregion

    #region #1146 - Auto-migration graph edges match CanMigrate

    [Fact]
    public void SerializationGenerator_AutoMigrations_GraphMatchesCanMigrate()
    {
        var source = """
            using KeenEyes;

            namespace ShapeApp;

            [Component(Serializable = true, Version = 3)]
            public partial struct Settings
            {
                public int Volume;

                [DefaultValue(50)]
                public int Brightness;
            }
            """;

        var (output, _, sources) = Run(source, new ComponentGenerator(), new SerializationGenerator());

        // The migration graph must gain edges for the auto-generated migrations, not just
        // explicit ones - otherwise FindGaps contradicts CanMigrate.
        Assert.Contains(sources, s => s.Contains("AddEdge(1, 2)") && s.Contains("AddEdge(2, 3)"));

        AssertNoCompileErrors(output);

        var assembly = EmitAndLoad(output);
        var serializerType = assembly.GetType("KeenEyes.Generated.ComponentSerializer")!;
        var instance = serializerType.GetField("Instance")!.GetValue(null)!;
        var diagnostics = (IMigrationDiagnostics)instance;

        Assert.True(diagnostics.CanMigrate("ShapeApp.Settings", 1, 3));

        var gaps = diagnostics.FindAllMigrationGaps();
        Assert.False(
            gaps.ContainsKey("ShapeApp.Settings"),
            "Auto-migrated component should report no migration gaps.");
    }

    #endregion
}
