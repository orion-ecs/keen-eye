namespace KeenEyes.Shaders.Compiler.Lexing;

/// <summary>
/// Represents a location in source code for error reporting.
/// </summary>
/// <param name="FilePath">The path to the source file.</param>
/// <param name="Line">The 1-based line number.</param>
/// <param name="Column">The 1-based column number.</param>
public readonly record struct SourceLocation(string FilePath, int Line, int Column)
{
    /// <summary>
    /// Returns a string representation suitable for error messages.
    /// </summary>
    public override string ToString() => $"{FilePath}:{Line}:{Column}";
}
