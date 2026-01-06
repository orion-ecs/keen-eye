using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Compiler;

/// <summary>
/// Result of compiling a KESL graph to AST.
/// </summary>
public sealed class CompilationResult
{
    private CompilationResult(bool isSuccess, ComputeDeclaration? declaration, IReadOnlyList<CompilationError> errors)
    {
        IsSuccess = isSuccess;
        Declaration = declaration;
        Errors = errors;
    }

    /// <summary>
    /// Gets whether the compilation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the compiled AST (null if compilation failed).
    /// </summary>
    public ComputeDeclaration? Declaration { get; }

    /// <summary>
    /// Gets compilation errors.
    /// </summary>
    public IReadOnlyList<CompilationError> Errors { get; }

    /// <summary>
    /// Creates a successful compilation result.
    /// </summary>
    public static CompilationResult Success(ComputeDeclaration declaration)
        => new(true, declaration, []);

    /// <summary>
    /// Creates a failed compilation result with errors.
    /// </summary>
    public static CompilationResult Failure(params CompilationError[] errors)
        => new(false, null, errors);

    /// <summary>
    /// Creates a failed compilation result with a single error message.
    /// </summary>
    public static CompilationResult Error(string message)
        => new(false, null, [new CompilationError(Entity.Null, null, message, "KESL000")]);
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
