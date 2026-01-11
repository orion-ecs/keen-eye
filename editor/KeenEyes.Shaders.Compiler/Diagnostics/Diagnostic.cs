using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Shaders.Compiler.Diagnostics;

/// <summary>
/// Represents a compiler diagnostic with rich location and suggestion information.
/// </summary>
/// <param name="Severity">The severity level of the diagnostic.</param>
/// <param name="Code">The diagnostic code (e.g., "KESL200").</param>
/// <param name="Message">The human-readable diagnostic message.</param>
/// <param name="Span">The source span where the diagnostic occurred.</param>
/// <param name="FilePath">The path to the source file.</param>
/// <param name="Suggestions">Optional list of "did you mean?" suggestions.</param>
public sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    SourceSpan Span,
    string FilePath,
    IReadOnlyList<string>? Suggestions = null)
{
    /// <summary>
    /// Gets the primary source location (start of span).
    /// </summary>
    public SourceLocation Location => Span.Start;

    /// <summary>
    /// Creates an error diagnostic.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="span">The source span.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="suggestions">Optional suggestions.</param>
    /// <returns>A new error diagnostic.</returns>
    public static Diagnostic Error(
        string code,
        string message,
        SourceSpan span,
        string filePath,
        IReadOnlyList<string>? suggestions = null)
    {
        return new Diagnostic(DiagnosticSeverity.Error, code, message, span, filePath, suggestions);
    }

    /// <summary>
    /// Creates a warning diagnostic.
    /// </summary>
    /// <param name="code">The warning code.</param>
    /// <param name="message">The warning message.</param>
    /// <param name="span">The source span.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="suggestions">Optional suggestions.</param>
    /// <returns>A new warning diagnostic.</returns>
    public static Diagnostic Warning(
        string code,
        string message,
        SourceSpan span,
        string filePath,
        IReadOnlyList<string>? suggestions = null)
    {
        return new Diagnostic(DiagnosticSeverity.Warning, code, message, span, filePath, suggestions);
    }

    /// <summary>
    /// Creates an info diagnostic.
    /// </summary>
    /// <param name="code">The info code.</param>
    /// <param name="message">The info message.</param>
    /// <param name="span">The source span.</param>
    /// <param name="filePath">The file path.</param>
    /// <returns>A new info diagnostic.</returns>
    public static Diagnostic Info(
        string code,
        string message,
        SourceSpan span,
        string filePath)
    {
        return new Diagnostic(DiagnosticSeverity.Info, code, message, span, filePath);
    }

    /// <summary>
    /// Returns a compact MSBuild-compatible format.
    /// </summary>
    /// <returns>A string in the format "file(line,column): severity code: message".</returns>
    public string ToMsBuildFormat()
    {
        var severityText = Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info"
        };

        return $"{FilePath}({Span.Start.Line},{Span.Start.Column}): {severityText} {Code}: {Message}";
    }

    /// <summary>
    /// Returns a formatted string representation.
    /// </summary>
    public override string ToString() => ToMsBuildFormat();
}
