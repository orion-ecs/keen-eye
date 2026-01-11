using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Compiler;

/// <summary>
/// The type of shader that was compiled.
/// </summary>
public enum CompiledShaderType
{
    /// <summary>No shader (compilation failed).</summary>
    None,

    /// <summary>Compute shader.</summary>
    Compute,

    /// <summary>Vertex shader.</summary>
    Vertex,

    /// <summary>Fragment shader.</summary>
    Fragment
}

/// <summary>
/// Result of compiling a KESL graph to AST.
/// </summary>
public sealed class CompilationResult
{
    private CompilationResult(
        bool isSuccess,
        CompiledShaderType shaderType,
        ComputeDeclaration? computeDeclaration,
        VertexDeclaration? vertexDeclaration,
        FragmentDeclaration? fragmentDeclaration,
        IReadOnlyList<CompilationError> errors)
    {
        IsSuccess = isSuccess;
        ShaderType = shaderType;
        ComputeDeclaration = computeDeclaration;
        VertexDeclaration = vertexDeclaration;
        FragmentDeclaration = fragmentDeclaration;
        Errors = errors;
    }

    /// <summary>
    /// Gets whether the compilation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the type of shader that was compiled.
    /// </summary>
    public CompiledShaderType ShaderType { get; }

    /// <summary>
    /// Gets the compiled compute shader AST (null if not a compute shader or compilation failed).
    /// </summary>
    public ComputeDeclaration? ComputeDeclaration { get; }

    /// <summary>
    /// Gets the compiled vertex shader AST (null if not a vertex shader or compilation failed).
    /// </summary>
    public VertexDeclaration? VertexDeclaration { get; }

    /// <summary>
    /// Gets the compiled fragment shader AST (null if not a fragment shader or compilation failed).
    /// </summary>
    public FragmentDeclaration? FragmentDeclaration { get; }

    /// <summary>
    /// Gets the compiled AST (for backwards compatibility - returns compute declaration).
    /// </summary>
    [Obsolete("Use ComputeDeclaration, VertexDeclaration, or FragmentDeclaration instead.")]
    public ComputeDeclaration? Declaration => ComputeDeclaration;

    /// <summary>
    /// Gets compilation errors.
    /// </summary>
    public IReadOnlyList<CompilationError> Errors { get; }

    /// <summary>
    /// Creates a successful compilation result for a compute shader.
    /// </summary>
    public static CompilationResult Success(ComputeDeclaration declaration)
        => new(true, CompiledShaderType.Compute, declaration, null, null, []);

    /// <summary>
    /// Creates a successful compilation result for a vertex shader.
    /// </summary>
    public static CompilationResult SuccessVertex(VertexDeclaration declaration)
        => new(true, CompiledShaderType.Vertex, null, declaration, null, []);

    /// <summary>
    /// Creates a successful compilation result for a fragment shader.
    /// </summary>
    public static CompilationResult SuccessFragment(FragmentDeclaration declaration)
        => new(true, CompiledShaderType.Fragment, null, null, declaration, []);

    /// <summary>
    /// Creates a failed compilation result with errors.
    /// </summary>
    public static CompilationResult Failure(params CompilationError[] errors)
        => new(false, CompiledShaderType.None, null, null, null, errors);

    /// <summary>
    /// Creates a failed compilation result with a single error message.
    /// </summary>
    public static CompilationResult Error(string message)
        => new(false, CompiledShaderType.None, null, null, null, [new CompilationError(Entity.Null, null, message, "KESL000")]);
}

/// <summary>
/// A compilation error from the graph compiler.
/// </summary>
/// <param name="Node">The node that caused the error (Invalid if graph-level error).</param>
/// <param name="PortIndex">The port index if port-specific (null otherwise).</param>
/// <param name="Message">The error message.</param>
/// <param name="Code">The error code.</param>
public readonly record struct CompilationError(
    Entity Node,
    int? PortIndex,
    string Message,
    string Code);
