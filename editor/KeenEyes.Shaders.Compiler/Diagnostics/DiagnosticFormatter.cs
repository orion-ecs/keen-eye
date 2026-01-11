using System.Text;

using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing;

namespace KeenEyes.Shaders.Compiler.Diagnostics;

/// <summary>
/// Options for formatting diagnostics.
/// </summary>
public sealed class DiagnosticFormatOptions
{
    /// <summary>
    /// Gets or sets whether to include hints for common errors.
    /// </summary>
    public bool IncludeHints { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include "did you mean?" suggestions.
    /// </summary>
    public bool IncludeSuggestions { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of context lines to show before and after the error.
    /// </summary>
    public int ContextLines { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to use ANSI color codes in output.
    /// </summary>
    public bool UseColors { get; set; } = false;

    /// <summary>
    /// Gets the default formatting options.
    /// </summary>
    public static DiagnosticFormatOptions Default { get; } = new();

    /// <summary>
    /// Gets formatting options for compact build output.
    /// </summary>
    public static DiagnosticFormatOptions Compact { get; } = new()
    {
        IncludeHints = false,
        IncludeSuggestions = false,
        ContextLines = 0,
        UseColors = false
    };
}

/// <summary>
/// Formats compiler errors with source context for display.
/// </summary>
/// <remarks>
/// <para>
/// The formatter displays errors with the offending source line and
/// a visual marker pointing to the error location:
/// </para>
/// <code>
/// error[KESL202] at input:1:8:
///   Expected 'component' or 'compute' declaration
///
///   invalid_keyword MyShader {
///          ^
///
///   Hint: Valid declarations are 'component' or 'compute'.
/// </code>
/// </remarks>
public sealed class DiagnosticFormatter
{
    private readonly string[] lines;

    /// <summary>
    /// Creates a new diagnostic formatter for the given source text.
    /// </summary>
    /// <param name="source">The original source text.</param>
    public DiagnosticFormatter(string source)
    {
        lines = source.Split('\n');

        // Normalize line endings (remove trailing \r)
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd('\r');
        }
    }

    /// <summary>
    /// Formats a compiler error with source context.
    /// </summary>
    /// <param name="error">The error to format.</param>
    /// <param name="includeHints">Whether to include helpful hints.</param>
    /// <returns>A formatted error string.</returns>
    public string Format(CompilerError error, bool includeHints = true)
    {
        var sb = new StringBuilder();

        // Error header with code
        var code = error.Code ?? "KESL000";
        sb.AppendLine($"error[{code}] at {error.Location}:");
        sb.AppendLine($"  {error.Message}");
        sb.AppendLine();

        // Source context
        var lineIndex = error.Location.Line - 1; // Convert to 0-based
        if (lineIndex >= 0 && lineIndex < lines.Length)
        {
            var sourceLine = lines[lineIndex];
            var column = Math.Max(0, error.Location.Column - 1); // Convert to 0-based

            // Show the source line with line number
            var lineNumber = error.Location.Line.ToString().PadLeft(4);
            sb.AppendLine($"  {lineNumber} | {sourceLine}");

            // Show the marker pointing to the error
            var markerPadding = new string(' ', 4 + 3 + column); // line num + " | " + column offset
            sb.AppendLine($"  {markerPadding}^");
        }

        // Optional hints
        if (includeHints)
        {
            var hint = GetHint(error);
            if (hint is not null)
            {
                sb.AppendLine();
                sb.AppendLine($"  Hint: {hint}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats multiple compiler errors.
    /// </summary>
    /// <param name="errors">The errors to format.</param>
    /// <param name="includeHints">Whether to include helpful hints.</param>
    /// <returns>A formatted string with all errors.</returns>
    public string FormatAll(IEnumerable<CompilerError> errors, bool includeHints = true)
    {
        var sb = new StringBuilder();
        var errorList = errors.ToList();

        foreach (var error in errorList)
        {
            sb.Append(Format(error, includeHints));
            sb.AppendLine();
        }

        // Summary
        var errorCount = errorList.Count;
        if (errorCount > 0)
        {
            sb.AppendLine($"Found {errorCount} error{(errorCount == 1 ? "" : "s")}.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats an error in a compact single-line format suitable for build output.
    /// </summary>
    /// <param name="error">The error to format.</param>
    /// <returns>A compact error string.</returns>
    public static string FormatCompact(CompilerError error)
    {
        var code = error.Code ?? "KESL000";
        return $"{error.Location}: error {code}: {error.Message}";
    }

    /// <summary>
    /// Formats a diagnostic in MSBuild-compatible single-line format.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to format.</param>
    /// <returns>A compact diagnostic string.</returns>
    public static string FormatCompact(Diagnostic diagnostic)
    {
        return diagnostic.ToMsBuildFormat();
    }

    /// <summary>
    /// Formats a diagnostic with source context, span underlining, and suggestions.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to format.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>A formatted diagnostic string.</returns>
    public string Format(Diagnostic diagnostic, DiagnosticFormatOptions? options = null)
    {
        options ??= DiagnosticFormatOptions.Default;
        var sb = new StringBuilder();

        // Diagnostic header with severity and code
        var severityText = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info"
        };

        sb.AppendLine($"{severityText}[{diagnostic.Code}] at {diagnostic.FilePath}:{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}:");
        sb.AppendLine($"  {diagnostic.Message}");
        sb.AppendLine();

        // Source context with span underlining
        FormatSourceContext(sb, diagnostic.Span, options.ContextLines);

        // Suggestions
        if (options.IncludeSuggestions && diagnostic.Suggestions is { Count: > 0 })
        {
            sb.AppendLine();
            if (diagnostic.Suggestions.Count == 1)
            {
                sb.AppendLine($"  Did you mean: {diagnostic.Suggestions[0]}?");
            }
            else
            {
                sb.AppendLine($"  Did you mean: {string.Join(", ", diagnostic.Suggestions)}?");
            }
        }

        // Hints based on error code
        if (options.IncludeHints)
        {
            var hint = GetHintForCode(diagnostic.Code);
            if (hint is not null)
            {
                sb.AppendLine();
                sb.AppendLine($"  Hint: {hint}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats multiple diagnostics.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to format.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>A formatted string with all diagnostics.</returns>
    public string FormatAll(IEnumerable<Diagnostic> diagnostics, DiagnosticFormatOptions? options = null)
    {
        options ??= DiagnosticFormatOptions.Default;
        var sb = new StringBuilder();
        var diagnosticList = diagnostics.ToList();

        foreach (var diagnostic in diagnosticList)
        {
            sb.Append(Format(diagnostic, options));
            sb.AppendLine();
        }

        // Summary
        var errorCount = diagnosticList.Count(d => d.Severity == DiagnosticSeverity.Error);
        var warningCount = diagnosticList.Count(d => d.Severity == DiagnosticSeverity.Warning);

        if (errorCount > 0 || warningCount > 0)
        {
            var parts = new List<string>();
            if (errorCount > 0)
            {
                parts.Add($"{errorCount} error{(errorCount == 1 ? "" : "s")}");
            }
            if (warningCount > 0)
            {
                parts.Add($"{warningCount} warning{(warningCount == 1 ? "" : "s")}");
            }
            sb.AppendLine($"Found {string.Join(" and ", parts)}.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats the source context with the span underlined.
    /// </summary>
    private void FormatSourceContext(StringBuilder sb, SourceSpan span, int contextLines)
    {
        var startLine = span.Start.Line - 1; // Convert to 0-based
        var endLine = span.End.Line - 1;

        // Add context lines before
        for (int i = Math.Max(0, startLine - contextLines); i < startLine; i++)
        {
            if (i >= 0 && i < lines.Length)
            {
                var lineNumber = (i + 1).ToString().PadLeft(4);
                sb.AppendLine($"  {lineNumber} | {lines[i]}");
            }
        }

        // Single-line span
        if (!span.IsMultiLine)
        {
            if (startLine >= 0 && startLine < lines.Length)
            {
                var sourceLine = lines[startLine];
                var column = Math.Max(0, span.Start.Column - 1); // Convert to 0-based
                var length = Math.Max(1, span.Length); // At least 1 character underline

                // Ensure we don't underline past the line length
                if (column + length > sourceLine.Length)
                {
                    length = Math.Max(1, sourceLine.Length - column);
                }

                // Show the source line with line number
                var lineNumber = span.Start.Line.ToString().PadLeft(4);
                sb.AppendLine($"  {lineNumber} | {sourceLine}");

                // Show the underline for the span
                var markerPadding = new string(' ', 4 + 3 + column); // line num + " | " + column offset
                var underline = new string('^', length);
                sb.AppendLine($"  {markerPadding}{underline}");
            }
        }
        else
        {
            // Multi-line span: show all affected lines with markers
            for (int i = startLine; i <= endLine && i < lines.Length; i++)
            {
                if (i < 0) continue;

                var sourceLine = lines[i];
                var lineNumber = (i + 1).ToString().PadLeft(4);

                // Determine underline range for this line
                int underlineStart, underlineEnd;
                if (i == startLine)
                {
                    underlineStart = span.Start.Column - 1;
                    underlineEnd = sourceLine.Length;
                }
                else if (i == endLine)
                {
                    underlineStart = 0;
                    underlineEnd = span.End.Column - 1;
                }
                else
                {
                    underlineStart = 0;
                    underlineEnd = sourceLine.Length;
                }

                underlineStart = Math.Max(0, underlineStart);
                underlineEnd = Math.Min(sourceLine.Length, Math.Max(underlineStart + 1, underlineEnd));

                sb.AppendLine($"  {lineNumber} | {sourceLine}");

                // Show the underline
                var padding = new string(' ', 4 + 3 + underlineStart);
                var underline = new string('^', underlineEnd - underlineStart);
                sb.AppendLine($"  {padding}{underline}");
            }
        }

        // Add context lines after
        for (int i = endLine + 1; i <= endLine + contextLines; i++)
        {
            if (i >= 0 && i < lines.Length)
            {
                var lineNumber = (i + 1).ToString().PadLeft(4);
                sb.AppendLine($"  {lineNumber} | {lines[i]}");
            }
        }
    }

    /// <summary>
    /// Gets hint text for a diagnostic code.
    /// </summary>
    private static string? GetHintForCode(string? code)
    {
        if (code is null) return null;

        return code switch
        {
            KeslErrorCodes.ExpectedDeclaration =>
                "Valid declarations are 'component' or 'compute'.",

            KeslErrorCodes.ExpectedBindingMode =>
                "Binding modes are 'read' (input), 'write' (output), 'optional' (nullable input), or 'without' (exclusion).",

            KeslErrorCodes.ExpectedTypeName =>
                "Built-in types include: int, uint, float, bool, vec2, vec3, vec4, mat3, mat4.",

            KeslErrorCodes.InvalidExpression =>
                "Expressions can be literals, identifiers, operators, or function calls.",

            KeslErrorCodes.DuplicateDefinition =>
                "Each component and compute shader must have a unique name.",

            KeslErrorCodes.TypeMismatch =>
                "Ensure the types on both sides of the operation are compatible.",

            _ => null
        };
    }

    /// <summary>
    /// Gets the source line at the specified line number.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number.</param>
    /// <returns>The source line, or null if out of range.</returns>
    public string? GetLine(int lineNumber)
    {
        var index = lineNumber - 1;
        if (index >= 0 && index < lines.Length)
        {
            return lines[index];
        }
        return null;
    }

    /// <summary>
    /// Gets contextual hint text for common errors.
    /// </summary>
    private static string? GetHint(CompilerError error)
    {
        var code = error.Code;

        return code switch
        {
            KeslErrorCodes.ExpectedDeclaration =>
                "Valid declarations are 'component' or 'compute'.",

            KeslErrorCodes.ExpectedBindingMode =>
                "Binding modes are 'read' (input), 'write' (output), 'optional' (nullable input), or 'without' (exclusion).",

            KeslErrorCodes.ExpectedTypeName =>
                "Built-in types include: int, uint, float, bool, vec2, vec3, vec4, mat3, mat4.",

            KeslErrorCodes.InvalidExpression =>
                "Expressions can be literals, identifiers, operators, or function calls.",

            KeslErrorCodes.DuplicateDefinition =>
                "Each component and compute shader must have a unique name.",

            KeslErrorCodes.TypeMismatch =>
                "Ensure the types on both sides of the operation are compatible.",

            _ => null
        };
    }
}

/// <summary>
/// Provides static formatting utilities for diagnostics.
/// </summary>
public static class DiagnosticOutput
{
    /// <summary>
    /// Writes formatted errors to the console with colors.
    /// </summary>
    /// <param name="errors">The errors to write.</param>
    /// <param name="source">The source text for context.</param>
    public static void WriteToConsole(IEnumerable<CompilerError> errors, string source)
    {
        var formatter = new DiagnosticFormatter(source);
        var originalColor = Console.ForegroundColor;

        try
        {
            foreach (var error in errors)
            {
                // Error header in red
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("error");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{error.Code ?? "KESL000"}]");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($" at {error.Location}:");

                // Message in white
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  {error.Message}");
                Console.WriteLine();

                // Source context
                var lineIndex = error.Location.Line - 1;
                if (lineIndex >= 0)
                {
                    var line = formatter.GetLine(error.Location.Line);
                    if (line is not null)
                    {
                        var lineNumber = error.Location.Line.ToString().PadLeft(4);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write($"  {lineNumber} | ");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(line);

                        // Error marker
                        var column = Math.Max(0, error.Location.Column - 1);
                        var padding = new string(' ', 4 + 3 + column);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  {padding}^");
                    }
                }

                Console.WriteLine();
            }
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// Writes formatted diagnostics to the console with colors.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to write.</param>
    /// <param name="source">The source text for context.</param>
    public static void WriteToConsole(IEnumerable<Diagnostic> diagnostics, string source)
    {
        var formatter = new DiagnosticFormatter(source);
        var originalColor = Console.ForegroundColor;

        try
        {
            foreach (var diagnostic in diagnostics)
            {
                // Severity-based coloring
                var severityColor = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => ConsoleColor.Red,
                    DiagnosticSeverity.Warning => ConsoleColor.Yellow,
                    _ => ConsoleColor.Blue
                };

                var severityText = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => "error",
                    DiagnosticSeverity.Warning => "warning",
                    _ => "info"
                };

                // Diagnostic header
                Console.ForegroundColor = severityColor;
                Console.Write(severityText);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{diagnostic.Code}]");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($" at {diagnostic.FilePath}:{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}:");

                // Message in white
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  {diagnostic.Message}");
                Console.WriteLine();

                // Source context with span underline
                WriteSourceContext(formatter, diagnostic.Span, severityColor);

                // Suggestions
                if (diagnostic.Suggestions is { Count: > 0 })
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (diagnostic.Suggestions.Count == 1)
                    {
                        Console.WriteLine($"  Did you mean: {diagnostic.Suggestions[0]}?");
                    }
                    else
                    {
                        Console.WriteLine($"  Did you mean: {string.Join(", ", diagnostic.Suggestions)}?");
                    }
                    Console.WriteLine();
                }

                Console.WriteLine();
            }
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// Writes source context with span underline to console.
    /// </summary>
    private static void WriteSourceContext(DiagnosticFormatter formatter, SourceSpan span, ConsoleColor underlineColor)
    {
        var startLine = span.Start.Line;
        var endLine = span.End.Line;

        // Single-line span
        if (!span.IsMultiLine)
        {
            var line = formatter.GetLine(startLine);
            if (line is not null)
            {
                var lineNumber = startLine.ToString().PadLeft(4);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  {lineNumber} | ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(line);

                // Span underline
                var column = Math.Max(0, span.Start.Column - 1);
                var length = Math.Max(1, span.Length);
                if (column + length > line.Length)
                {
                    length = Math.Max(1, line.Length - column);
                }

                var padding = new string(' ', 4 + 3 + column);
                var underline = new string('^', length);
                Console.ForegroundColor = underlineColor;
                Console.WriteLine($"  {padding}{underline}");
            }
        }
        else
        {
            // Multi-line span
            for (int i = startLine; i <= endLine; i++)
            {
                var line = formatter.GetLine(i);
                if (line is null) continue;

                var lineNumber = i.ToString().PadLeft(4);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  {lineNumber} | ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(line);

                // Determine underline range
                int underlineStart, underlineEnd;
                if (i == startLine)
                {
                    underlineStart = span.Start.Column - 1;
                    underlineEnd = line.Length;
                }
                else if (i == endLine)
                {
                    underlineStart = 0;
                    underlineEnd = span.End.Column - 1;
                }
                else
                {
                    underlineStart = 0;
                    underlineEnd = line.Length;
                }

                underlineStart = Math.Max(0, underlineStart);
                underlineEnd = Math.Min(line.Length, Math.Max(underlineStart + 1, underlineEnd));

                var padding = new string(' ', 4 + 3 + underlineStart);
                var underline = new string('^', underlineEnd - underlineStart);
                Console.ForegroundColor = underlineColor;
                Console.WriteLine($"  {padding}{underline}");
            }
        }
    }
}
