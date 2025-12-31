namespace KeenEyes.Shaders.Compiler.Lexing;

/// <summary>
/// Represents a token in KESL source code.
/// </summary>
/// <param name="Kind">The kind of token.</param>
/// <param name="Text">The literal text of the token from source.</param>
/// <param name="Location">The source location where the token starts.</param>
public readonly record struct Token(TokenKind Kind, string Text, SourceLocation Location)
{
    /// <summary>
    /// Gets the integer value of an IntLiteral token.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this is not an IntLiteral.</exception>
    public int IntValue => Kind == TokenKind.IntLiteral
        ? int.Parse(Text)
        : throw new InvalidOperationException($"Token is {Kind}, not IntLiteral");

    /// <summary>
    /// Gets the float value of a FloatLiteral token.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this is not a FloatLiteral.</exception>
    public float FloatValue => Kind == TokenKind.FloatLiteral
        ? float.Parse(Text)
        : throw new InvalidOperationException($"Token is {Kind}, not FloatLiteral");

    /// <summary>
    /// Returns a human-readable representation of the token.
    /// </summary>
    public override string ToString() => Kind switch
    {
        TokenKind.Identifier => $"Identifier({Text})",
        TokenKind.IntLiteral => $"Int({Text})",
        TokenKind.FloatLiteral => $"Float({Text})",
        TokenKind.Error => $"Error({Text})",
        _ => Kind.ToString()
    };
}
