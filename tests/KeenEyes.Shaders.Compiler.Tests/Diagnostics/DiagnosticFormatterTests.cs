using KeenEyes.Shaders.Compiler.Diagnostics;
using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Shaders.Compiler.Tests.Diagnostics;

/// <summary>
/// Tests for the DiagnosticFormatter class with new Diagnostic type support.
/// </summary>
public class DiagnosticFormatterTests
{
    private const string TestSource = """
        component Position {
            x: float;
            y: float;
            z: float;
        }

        compute Physics {
            query { write Positon; read Velocity }
            execute {
                // Update position
            }
        }
        """;

    #region SourceSpan Tests

    [Fact]
    public void SourceSpan_SingleLine_HasCorrectProperties()
    {
        var start = new SourceLocation("test.kesl", 5, 10);
        var end = new SourceLocation("test.kesl", 5, 20);
        var span = new SourceSpan(start, end);

        Assert.False(span.IsMultiLine);
        Assert.Equal(10, span.Length);
    }

    [Fact]
    public void SourceSpan_MultiLine_HasCorrectProperties()
    {
        var start = new SourceLocation("test.kesl", 1, 10);
        var end = new SourceLocation("test.kesl", 3, 5);
        var span = new SourceSpan(start, end);

        Assert.True(span.IsMultiLine);
        Assert.Equal(0, span.Length); // Multi-line spans return 0 length
    }

    [Fact]
    public void SourceSpan_FromToken_CreatesCorrectSpan()
    {
        var location = new SourceLocation("test.kesl", 8, 19);
        var token = new Token(TokenKind.Identifier, "Positon", location);
        var span = SourceSpan.FromToken(token);

        Assert.Equal(8, span.Start.Line);
        Assert.Equal(19, span.Start.Column);
        Assert.Equal(8, span.End.Line);
        Assert.Equal(26, span.End.Column); // 19 + 7 = 26
        Assert.Equal(7, span.Length);
    }

    [Fact]
    public void SourceSpan_FromLocation_CreatesZeroLengthSpan()
    {
        var location = new SourceLocation("test.kesl", 5, 10);
        var span = SourceSpan.FromLocation(location);

        Assert.Equal(location, span.Start);
        Assert.Equal(location, span.End);
        Assert.Equal(0, span.Length);
    }

    [Fact]
    public void SourceSpan_Merge_CoversBothSpans()
    {
        var span1 = new SourceSpan(
            new SourceLocation("test.kesl", 1, 5),
            new SourceLocation("test.kesl", 1, 15));

        var span2 = new SourceSpan(
            new SourceLocation("test.kesl", 3, 10),
            new SourceLocation("test.kesl", 3, 25));

        var merged = span1.Merge(span2);

        Assert.Equal(1, merged.Start.Line);
        Assert.Equal(5, merged.Start.Column);
        Assert.Equal(3, merged.End.Line);
        Assert.Equal(25, merged.End.Column);
    }

    #endregion

    #region Diagnostic Tests

    [Fact]
    public void Diagnostic_Error_HasCorrectSeverity()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl");

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("KESL202", diagnostic.Code);
    }

    [Fact]
    public void Diagnostic_Warning_HasCorrectSeverity()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 5, 1),
            new SourceLocation("test.kesl", 5, 10));

        var diagnostic = Diagnostic.Warning(
            "KESL301",
            "Unused variable 'temp'",
            span,
            "test.kesl");

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void Diagnostic_WithSuggestions_ContainsSuggestions()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var suggestions = new[] { "Position", "Rotation" };
        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl",
            suggestions);

        Assert.NotNull(diagnostic.Suggestions);
        Assert.Equal(2, diagnostic.Suggestions!.Count);
        Assert.Contains("Position", diagnostic.Suggestions);
    }

    [Fact]
    public void Diagnostic_ToMsBuildFormat_ReturnsCorrectFormat()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl");

        var msbuild = diagnostic.ToMsBuildFormat();

        Assert.Equal("test.kesl(8,19): error KESL202: Unknown component type 'Positon'", msbuild);
    }

    [Fact]
    public void Diagnostic_Warning_ToMsBuildFormat()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 5, 1),
            new SourceLocation("test.kesl", 5, 10));

        var diagnostic = Diagnostic.Warning(
            "KESL301",
            "Unused variable",
            span,
            "test.kesl");

        var msbuild = diagnostic.ToMsBuildFormat();

        Assert.Contains("warning", msbuild);
        Assert.Contains("KESL301", msbuild);
    }

    #endregion

    #region DiagnosticFormatter Tests

    [Fact]
    public void Format_Diagnostic_IncludesHeader()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl");

        var output = formatter.Format(diagnostic);

        Assert.Contains("error[KESL202]", output);
        Assert.Contains("Unknown component type 'Positon'", output);
    }

    [Fact]
    public void Format_Diagnostic_IncludesSourceLine()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl");

        var output = formatter.Format(diagnostic);

        Assert.Contains("query { write Positon;", output);
    }

    [Fact]
    public void Format_Diagnostic_IncludesUnderline()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl");

        var output = formatter.Format(diagnostic);

        // Should contain an underline (^^^^^^^ for 7 characters)
        Assert.Contains("^^^^^^^", output);
    }

    [Fact]
    public void Format_Diagnostic_WithSuggestions_IncludesDidYouMean()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl",
            new[] { "Position", "Rotation" });

        var output = formatter.Format(diagnostic);

        Assert.Contains("Did you mean:", output);
        Assert.Contains("Position", output);
        Assert.Contains("Rotation", output);
    }

    [Fact]
    public void Format_Diagnostic_SingleSuggestion_FormatsCorrectly()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl",
            new[] { "Position" });

        var output = formatter.Format(diagnostic);

        Assert.Contains("Did you mean: Position?", output);
    }

    [Fact]
    public void Format_Diagnostic_WithoutSuggestions_NoDidYouMean()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl");

        var options = new DiagnosticFormatOptions { IncludeSuggestions = true };
        var output = formatter.Format(diagnostic, options);

        Assert.DoesNotContain("Did you mean", output);
    }

    [Fact]
    public void Format_Diagnostic_CompactOptions_ExcludesSuggestions()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl",
            new[] { "Position" });

        var output = formatter.Format(diagnostic, DiagnosticFormatOptions.Compact);

        Assert.DoesNotContain("Did you mean", output);
    }

    [Fact]
    public void FormatAll_Diagnostics_IncludesSummary()
    {
        var formatter = new DiagnosticFormatter(TestSource);
        var diagnostics = new[]
        {
            Diagnostic.Error(
                "KESL202",
                "Error 1",
                new SourceSpan(new SourceLocation("test.kesl", 1, 1), new SourceLocation("test.kesl", 1, 5)),
                "test.kesl"),
            Diagnostic.Warning(
                "KESL301",
                "Warning 1",
                new SourceSpan(new SourceLocation("test.kesl", 2, 1), new SourceLocation("test.kesl", 2, 5)),
                "test.kesl")
        };

        var output = formatter.FormatAll(diagnostics);

        Assert.Contains("1 error", output);
        Assert.Contains("1 warning", output);
    }

    [Fact]
    public void FormatCompact_Diagnostic_ReturnsMsBuildFormat()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 8, 19),
            new SourceLocation("test.kesl", 8, 26));

        var diagnostic = Diagnostic.Error(
            "KESL202",
            "Unknown component type 'Positon'",
            span,
            "test.kesl");

        var compact = DiagnosticFormatter.FormatCompact(diagnostic);

        Assert.Equal("test.kesl(8,19): error KESL202: Unknown component type 'Positon'", compact);
    }

    #endregion

    #region Multi-Line Span Tests

    [Fact]
    public void Format_MultiLineSpan_ShowsAllAffectedLines()
    {
        var multiLineSource = """
            component Position {
                x: float;
                y: float;
            }
            """;

        var formatter = new DiagnosticFormatter(multiLineSource);
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 1),
            new SourceLocation("test.kesl", 4, 2));

        var diagnostic = Diagnostic.Error(
            "KESL100",
            "Invalid component definition",
            span,
            "test.kesl");

        var output = formatter.Format(diagnostic);

        // Should contain lines 1-4
        Assert.Contains("component Position {", output);
        Assert.Contains("x: float;", output);
        Assert.Contains("y: float;", output);
        Assert.Contains("}", output);
    }

    #endregion

    #region DiagnosticFormatOptions Tests

    [Fact]
    public void DiagnosticFormatOptions_Default_HasExpectedValues()
    {
        var options = DiagnosticFormatOptions.Default;

        Assert.True(options.IncludeHints);
        Assert.True(options.IncludeSuggestions);
        Assert.Equal(0, options.ContextLines);
        Assert.False(options.UseColors);
    }

    [Fact]
    public void DiagnosticFormatOptions_Compact_HasMinimalOptions()
    {
        var options = DiagnosticFormatOptions.Compact;

        Assert.False(options.IncludeHints);
        Assert.False(options.IncludeSuggestions);
        Assert.Equal(0, options.ContextLines);
        Assert.False(options.UseColors);
    }

    #endregion
}
