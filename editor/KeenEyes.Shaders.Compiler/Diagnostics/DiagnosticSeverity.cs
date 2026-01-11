namespace KeenEyes.Shaders.Compiler.Diagnostics;

/// <summary>
/// Represents the severity level of a compiler diagnostic.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// An informational message with no impact on compilation.
    /// </summary>
    Info,

    /// <summary>
    /// A warning that doesn't prevent compilation but may indicate issues.
    /// </summary>
    Warning,

    /// <summary>
    /// An error that prevents successful compilation.
    /// </summary>
    Error
}
