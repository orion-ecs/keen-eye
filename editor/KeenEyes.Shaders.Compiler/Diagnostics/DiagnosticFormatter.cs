using System.Text;

using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing;

namespace KeenEyes.Shaders.Compiler.Diagnostics;

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
}
