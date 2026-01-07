using KeenEyes.Shaders.Compiler.CodeGen;
using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler;

/// <summary>
/// Main entry point for the KESL (KeenEyes Shader Language) compiler.
/// </summary>
public sealed class KeslCompiler
{
    /// <summary>
    /// Gets or sets the namespace for generated C# code.
    /// </summary>
    public string Namespace { get; set; } = "Generated";

    /// <summary>
    /// Compiles KESL source code and returns the compilation result.
    /// </summary>
    /// <param name="source">The KESL source code.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>The compilation result.</returns>
    public static CompilationResult Compile(string source, string filePath = "<input>")
    {
        var errors = new List<CompilerError>();

        // Lexing
        var lexer = new Lexer(source, filePath);
        var tokens = lexer.Tokenize();

        // Check for lexer errors
        foreach (var token in tokens)
        {
            if (token.Kind == TokenKind.Error)
            {
                errors.Add(new CompilerError(token.Text, token.Location));
            }
        }

        if (errors.Count > 0)
        {
            return new CompilationResult(null, errors);
        }

        // Parsing
        var parser = new Parser(tokens);
        var sourceFile = parser.Parse();
        errors.AddRange(parser.Errors);

        if (errors.Count > 0)
        {
            return new CompilationResult(sourceFile, errors);
        }

        return new CompilationResult(sourceFile, errors);
    }

    /// <summary>
    /// Generates GLSL code for a compute shader.
    /// </summary>
    /// <param name="compute">The compute shader AST.</param>
    /// <returns>The generated GLSL code.</returns>
    public static string GenerateGlsl(ComputeDeclaration compute)
    {
        var generator = new GlslGenerator();
        return generator.Generate(compute);
    }

    /// <summary>
    /// Generates C# binding code for a compute shader.
    /// </summary>
    /// <param name="compute">The compute shader AST.</param>
    /// <returns>The generated C# code.</returns>
    public string GenerateCSharp(ComputeDeclaration compute)
    {
        var generator = new CSharpBindingGenerator
        {
            Namespace = Namespace
        };
        return generator.Generate(compute);
    }

    /// <summary>
    /// Compiles KESL source and generates all outputs for each compute shader.
    /// </summary>
    /// <param name="source">The KESL source code.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>The compilation outputs or errors.</returns>
    public CompilationOutput CompileAndGenerate(string source, string filePath = "<input>")
    {
        var result = Compile(source, filePath);

        if (result.HasErrors)
        {
            return new CompilationOutput([], result.Errors);
        }

        var outputs = new List<ShaderOutput>();

        foreach (var decl in result.SourceFile!.Declarations)
        {
            if (decl is ComputeDeclaration compute)
            {
                var glsl = GenerateGlsl(compute);
                var csharp = GenerateCSharp(compute);

                outputs.Add(new ShaderOutput(
                    compute.Name,
                    $"{compute.Name}.comp.glsl",
                    glsl,
                    $"{compute.Name}Shader.g.cs",
                    csharp
                ));
            }
        }

        return new CompilationOutput(outputs, []);
    }
}

/// <summary>
/// Result of parsing KESL source code.
/// </summary>
/// <param name="SourceFile">The parsed AST, or null if parsing failed.</param>
/// <param name="Errors">Any errors encountered during compilation.</param>
public record CompilationResult(
    SourceFile? SourceFile,
    IReadOnlyList<CompilerError> Errors
)
{
    /// <summary>
    /// Gets whether the compilation had any errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// Output from compiling and generating KESL code.
/// </summary>
/// <param name="Shaders">The generated shader outputs.</param>
/// <param name="Errors">Any errors encountered during compilation.</param>
public record CompilationOutput(
    IReadOnlyList<ShaderOutput> Shaders,
    IReadOnlyList<CompilerError> Errors
)
{
    /// <summary>
    /// Gets whether the compilation had any errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// Output for a single shader.
/// </summary>
/// <param name="ShaderName">The shader name.</param>
/// <param name="GlslFileName">The GLSL file name.</param>
/// <param name="GlslCode">The generated GLSL code.</param>
/// <param name="CSharpFileName">The C# file name.</param>
/// <param name="CSharpCode">The generated C# code.</param>
public record ShaderOutput(
    string ShaderName,
    string GlslFileName,
    string GlslCode,
    string CSharpFileName,
    string CSharpCode
);
