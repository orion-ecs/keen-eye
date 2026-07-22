using System.Reflection;
using System.Text;
using KeenEyes.Shaders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Shaders.Generator.IntegrationTests;

/// <summary>
/// Integration tests verifying that the KESL source generator produces valid, compilable
/// C# bindings from a real <c>.kesl</c> file.
/// </summary>
/// <remarks>
/// The generator is driven explicitly in-process via <see cref="CSharpGeneratorDriver"/>
/// (the same approach used by <c>KeenEyes.Generators.Tests</c>) rather than relying on
/// build-time generator output. This keeps the project loadable by MSBuildWorkspace
/// (<c>dotnet format</c>), which does not materialize generator output produced by an
/// analyzer-via-<c>ProjectReference</c> and therefore used to fail solution-wide format
/// verification with CS0246 on the generated types (issue #1026).
///
/// The generated syntax trees are compiled against the stub types in <c>GpuStubs.cs</c>
/// (referenced through this test assembly) and loaded, so the assertions still exercise
/// real generated type metadata and runtime behavior.
/// </remarks>
public class GeneratorIntegrationTests
{
    private const string GeneratedNamespace = "KeenEyes.Shaders.Generator.IntegrationTests";
    private const string ShaderTypeName = GeneratedNamespace + ".UpdatePhysicsShader";
    private const string GlslSourceTypeName = GeneratedNamespace + ".UpdatePhysicsGlslSource";

    private static readonly Lazy<GeneratorRun> lazyRun = new(RunGenerator);
    private static readonly Lazy<Assembly> lazyCompiledAssembly = new(() => CompileAndLoad(lazyRun.Value));

    [Fact]
    public void Generator_ProducesNoErrorDiagnostics()
    {
        var run = lazyRun.Value;

        Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_EmitsShaderAndGlslSourceFiles()
    {
        var run = lazyRun.Value;

        // One file for the shader class, one for the embedded GLSL source constant.
        Assert.Contains(run.GeneratedSources, s => s.Contains("class UpdatePhysicsShader"));
        Assert.Contains(run.GeneratedSources, s => s.Contains("class UpdatePhysicsGlslSource"));
    }

    [Fact]
    public void GeneratedCode_CompilesWithoutErrors()
    {
        // Exercises CompileAndLoad, which asserts the emit produced no errors.
        Assert.NotNull(lazyCompiledAssembly.Value);
    }

    [Fact]
    public void GeneratedShaderClass_Exists()
    {
        var shaderType = lazyCompiledAssembly.Value.GetType(ShaderTypeName);

        Assert.NotNull(shaderType);
        Assert.Equal("UpdatePhysicsShader", shaderType!.Name);
    }

    [Fact]
    public void GeneratedGlslSource_ContainsShaderCode()
    {
        var glslType = lazyCompiledAssembly.Value.GetType(GlslSourceTypeName);
        Assert.NotNull(glslType);

        var sourceField = glslType!.GetField("Source", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(sourceField);

        var glslSource = (string?)sourceField!.GetRawConstantValue();

        Assert.NotNull(glslSource);
        Assert.Contains("#version 450", glslSource!);
        Assert.Contains("Position", glslSource!);
        Assert.Contains("Velocity", glslSource!);
        Assert.Contains("deltaTime", glslSource!);
    }

    [Fact]
    public void GeneratedShaderClass_HasExpectedMembers()
    {
        var shaderType = lazyCompiledAssembly.Value.GetType(ShaderTypeName);
        Assert.NotNull(shaderType);

        // Should have a constructor that takes IGpuDevice.
        var constructor = shaderType!.GetConstructor([typeof(IGpuDevice)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void GeneratedShaderClass_CanBeInstantiated()
    {
        var shaderType = lazyCompiledAssembly.Value.GetType(ShaderTypeName);
        Assert.NotNull(shaderType);

        var shader = Activator.CreateInstance(shaderType!, new MockGpuDevice());

        Assert.NotNull(shader);
    }

    [Fact]
    public void GeneratedShaderClass_ImplementsIGpuComputeSystem()
    {
        var shaderType = lazyCompiledAssembly.Value.GetType(ShaderTypeName);
        Assert.NotNull(shaderType);

        Assert.True(typeof(IGpuComputeSystem).IsAssignableFrom(shaderType));
    }

    /// <summary>
    /// Runs the KESL source generator over the embedded <c>Physics.kesl</c> file.
    /// </summary>
    private static GeneratorRun RunGenerator()
    {
        var keslSource = ReadEmbeddedKesl();
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        // The generator only reads the assembly name off the compilation to pick the
        // generated namespace, so an otherwise minimal compilation is sufficient. The
        // namespace is chosen so the generated types can see the stub component and GPU
        // types (declared in GpuStubs.cs under this same namespace / its ancestors).
        var inputCompilation = CSharpCompilation.Create(
            assemblyName: GeneratedNamespace,
            syntaxTrees: [],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        AdditionalText keslFile = new InMemoryAdditionalText("Physics.kesl", keslSource);

        var driver = CSharpGeneratorDriver.Create(
            generators: [new KeslSourceGenerator().AsSourceGenerator()],
            additionalTexts: [keslFile],
            parseOptions: parseOptions);

        var runResult = driver.RunGenerators(inputCompilation).GetRunResult();

        var trees = runResult.GeneratedTrees;
        var sources = trees.Select(t => t.GetText().ToString()).ToList();

        return new GeneratorRun(trees, sources, runResult.Diagnostics);
    }

    /// <summary>
    /// Compiles the generator output against the stub types in this test assembly and
    /// loads the resulting assembly, asserting that emit produced no errors.
    /// </summary>
    private static Assembly CompileAndLoad(GeneratorRun run)
    {
        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratedShaders_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: run.GeneratedTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        var errors = emitResult.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(
            errors.Count == 0,
            "Generated code failed to compile:" + Environment.NewLine +
            string.Join(Environment.NewLine, errors));

        ms.Position = 0;
        return Assembly.Load(ms.ToArray());
    }

    /// <summary>
    /// Builds the metadata references needed to compile the generated code: the framework
    /// assemblies plus this test assembly (which supplies the GPU / component stub types).
    /// </summary>
    private static List<MetadataReference> GetReferences()
    {
        var references = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                references[Path.GetFileName(path)] = MetadataReference.CreateFromFile(path);
            }
        }

        // Ensure the stub types (GpuStubs.cs) are referenced; this overwrites any TPA entry
        // with the same file name so there is never a duplicate reference.
        var testAssemblyPath = typeof(GeneratorIntegrationTests).Assembly.Location;
        references[Path.GetFileName(testAssemblyPath)] = MetadataReference.CreateFromFile(testAssemblyPath);

        return references.Values.ToList();
    }

    private static string ReadEmbeddedKesl()
    {
        var assembly = typeof(GeneratorIntegrationTests).Assembly;
        using var stream = assembly.GetManifestResourceStream("Physics.kesl")
            ?? throw new InvalidOperationException("Embedded KESL resource 'Physics.kesl' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Captures the result of a single generator run.
    /// </summary>
    private sealed record GeneratorRun(
        IReadOnlyList<SyntaxTree> GeneratedTrees,
        IReadOnlyList<string> GeneratedSources,
        IReadOnlyList<Diagnostic> Diagnostics);

    /// <summary>
    /// A minimal in-memory <see cref="AdditionalText"/> used to feed KESL source to the generator.
    /// </summary>
    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly string text = text;

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
            => SourceText.From(text, Encoding.UTF8);
    }
}
