namespace KeenEyes.Shaders.Compiler.Lexing;

/// <summary>
/// Represents a span of source code from a start location to an end location.
/// </summary>
/// <param name="Start">The starting location of the span.</param>
/// <param name="End">The ending location of the span.</param>
public readonly record struct SourceSpan(SourceLocation Start, SourceLocation End)
{
    /// <summary>
    /// Gets whether this span covers multiple lines.
    /// </summary>
    public bool IsMultiLine => Start.Line != End.Line;

    /// <summary>
    /// Gets the length of the span in characters on the start line.
    /// For multi-line spans, this returns the length to end of the first line.
    /// </summary>
    public int Length => IsMultiLine ? 0 : End.Column - Start.Column;

    /// <summary>
    /// Creates a span from a single token.
    /// </summary>
    /// <param name="token">The token to create a span from.</param>
    /// <returns>A span covering the token's text.</returns>
    public static SourceSpan FromToken(Token token)
    {
        var endColumn = token.Location.Column + token.Text.Length;
        var end = new SourceLocation(token.Location.FilePath, token.Location.Line, endColumn);
        return new SourceSpan(token.Location, end);
    }

    /// <summary>
    /// Creates a span from a single location (zero-length span).
    /// </summary>
    /// <param name="location">The location for the span.</param>
    /// <returns>A zero-length span at the location.</returns>
    public static SourceSpan FromLocation(SourceLocation location)
    {
        return new SourceSpan(location, location);
    }

    /// <summary>
    /// Merges two spans into one that covers both.
    /// </summary>
    /// <param name="other">The other span to merge with.</param>
    /// <returns>A span that covers both input spans.</returns>
    public SourceSpan Merge(SourceSpan other)
    {
        var newStart = Start.Line < other.Start.Line ||
                       (Start.Line == other.Start.Line && Start.Column < other.Start.Column)
            ? Start
            : other.Start;

        var newEnd = End.Line > other.End.Line ||
                     (End.Line == other.End.Line && End.Column > other.End.Column)
            ? End
            : other.End;

        return new SourceSpan(newStart, newEnd);
    }

    /// <summary>
    /// Returns a string representation of the span.
    /// </summary>
    public override string ToString() =>
        IsMultiLine
            ? $"{Start} - {End}"
            : $"{Start}";
}
