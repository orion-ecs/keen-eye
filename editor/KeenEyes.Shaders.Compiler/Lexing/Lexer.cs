using System.Text;

namespace KeenEyes.Shaders.Compiler.Lexing;

/// <summary>
/// Lexer for KESL (KeenEyes Shader Language) source code.
/// Converts source text into a sequence of tokens.
/// </summary>
public sealed class Lexer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        // Declarations
        ["component"] = TokenKind.Component,
        ["compute"] = TokenKind.Compute,
        ["query"] = TokenKind.Query,
        ["params"] = TokenKind.Params,
        ["execute"] = TokenKind.Execute,
        ["vertex"] = TokenKind.Vertex,
        ["fragment"] = TokenKind.Fragment,

        // Shader I/O
        ["in"] = TokenKind.In,
        ["out"] = TokenKind.Out,

        // Query modifiers
        ["read"] = TokenKind.Read,
        ["write"] = TokenKind.Write,
        ["optional"] = TokenKind.Optional,
        ["without"] = TokenKind.Without,

        // Control flow
        ["if"] = TokenKind.If,
        ["else"] = TokenKind.Else,
        ["for"] = TokenKind.For,

        // Types
        ["float"] = TokenKind.Float,
        ["float2"] = TokenKind.Float2,
        ["float3"] = TokenKind.Float3,
        ["float4"] = TokenKind.Float4,
        ["int"] = TokenKind.Int,
        ["int2"] = TokenKind.Int2,
        ["int3"] = TokenKind.Int3,
        ["int4"] = TokenKind.Int4,
        ["uint"] = TokenKind.Uint,
        ["bool"] = TokenKind.Bool,
        ["mat4"] = TokenKind.Mat4,

        // Literals
        ["true"] = TokenKind.True,
        ["false"] = TokenKind.False,

        // Built-in
        ["has"] = TokenKind.Has,
    };

    private readonly string _source;
    private readonly string _filePath;
    private int _position;
    private int _line;
    private int _column;

    /// <summary>
    /// Creates a new lexer for the given source code.
    /// </summary>
    /// <param name="source">The source code to tokenize.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    public Lexer(string source, string filePath = "<input>")
    {
        _source = source;
        _filePath = filePath;
        _position = 0;
        _line = 1;
        _column = 1;
    }

    /// <summary>
    /// Tokenizes the entire source and returns all tokens.
    /// </summary>
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        Token token;

        do
        {
            token = NextToken();
            tokens.Add(token);
        } while (token.Kind != TokenKind.EndOfFile);

        return tokens;
    }

    /// <summary>
    /// Returns the next token from the source.
    /// </summary>
    public Token NextToken()
    {
        SkipWhitespaceAndComments();

        if (IsAtEnd())
        {
            return MakeToken(TokenKind.EndOfFile, "");
        }

        var location = CurrentLocation();
        char c = Advance();

        // Identifiers and keywords
        if (IsIdentifierStart(c))
        {
            return ScanIdentifier(c, location);
        }

        // Numbers
        if (char.IsDigit(c))
        {
            return ScanNumber(c, location);
        }

        // Punctuation and operators
        return c switch
        {
            '{' => MakeToken(TokenKind.LeftBrace, "{", location),
            '}' => MakeToken(TokenKind.RightBrace, "}", location),
            '(' => MakeToken(TokenKind.LeftParen, "(", location),
            ')' => MakeToken(TokenKind.RightParen, ")", location),
            '[' => MakeToken(TokenKind.LeftBracket, "[", location),
            ']' => MakeToken(TokenKind.RightBracket, "]", location),
            ',' => MakeToken(TokenKind.Comma, ",", location),
            ':' => MakeToken(TokenKind.Colon, ":", location),
            ';' => MakeToken(TokenKind.Semicolon, ";", location),
            '.' => Match('.') ? MakeToken(TokenKind.DotDot, "..", location) : MakeToken(TokenKind.Dot, ".", location),
            '@' => MakeToken(TokenKind.At, "@", location),

            '+' => Match('=') ? MakeToken(TokenKind.PlusEqual, "+=", location) : MakeToken(TokenKind.Plus, "+", location),
            '-' => Match('=') ? MakeToken(TokenKind.MinusEqual, "-=", location) : MakeToken(TokenKind.Minus, "-", location),
            '*' => Match('=') ? MakeToken(TokenKind.StarEqual, "*=", location) : MakeToken(TokenKind.Star, "*", location),
            '/' => Match('=') ? MakeToken(TokenKind.SlashEqual, "/=", location) : MakeToken(TokenKind.Slash, "/", location),

            '<' => Match('=') ? MakeToken(TokenKind.LessEqual, "<=", location) : MakeToken(TokenKind.Less, "<", location),
            '>' => Match('=') ? MakeToken(TokenKind.GreaterEqual, ">=", location) : MakeToken(TokenKind.Greater, ">", location),
            '=' => Match('=') ? MakeToken(TokenKind.EqualEqual, "==", location) : MakeToken(TokenKind.Equal, "=", location),
            '!' => Match('=') ? MakeToken(TokenKind.BangEqual, "!=", location) : MakeToken(TokenKind.Bang, "!", location),

            '&' => Match('&') ? MakeToken(TokenKind.AmpAmp, "&&", location) : MakeToken(TokenKind.Error, $"Unexpected character '&'", location),
            '|' => Match('|') ? MakeToken(TokenKind.PipePipe, "||", location) : MakeToken(TokenKind.Error, $"Unexpected character '|'", location),

            _ => MakeToken(TokenKind.Error, $"Unexpected character '{c}'", location)
        };
    }

    private Token ScanIdentifier(char first, SourceLocation location)
    {
        var sb = new StringBuilder();
        sb.Append(first);

        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        var text = sb.ToString();
        var kind = Keywords.TryGetValue(text, out var keyword) ? keyword : TokenKind.Identifier;

        return MakeToken(kind, text, location);
    }

    private Token ScanNumber(char first, SourceLocation location)
    {
        var sb = new StringBuilder();
        sb.Append(first);

        // Integer part
        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        // Check for decimal point (but not range operator ..)
        bool isFloat = false;
        if (!IsAtEnd() && Peek() == '.' && _position + 1 < _source.Length && _source[_position + 1] != '.')
        {
            isFloat = true;
            sb.Append(Advance()); // consume '.'

            // Fractional part
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        // Check for exponent
        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            isFloat = true;
            sb.Append(Advance()); // consume 'e' or 'E'

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                sb.Append(Advance());
            }

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        // Check for float suffix
        if (!IsAtEnd() && (Peek() == 'f' || Peek() == 'F'))
        {
            isFloat = true;
            Advance(); // consume 'f', but don't include in text
        }

        var text = sb.ToString();
        return MakeToken(isFloat ? TokenKind.FloatLiteral : TokenKind.IntLiteral, text, location);
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            char c = Peek();

            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                    Advance();
                    break;

                case '\n':
                    Advance();
                    _line++;
                    _column = 1;
                    break;

                case '/':
                    if (_position + 1 < _source.Length)
                    {
                        if (_source[_position + 1] == '/')
                        {
                            // Line comment
                            Advance();
                            Advance();
                            while (!IsAtEnd() && Peek() != '\n')
                            {
                                Advance();
                            }
                            break;
                        }
                        else if (_source[_position + 1] == '*')
                        {
                            // Block comment
                            Advance();
                            Advance();
                            while (!IsAtEnd())
                            {
                                if (Peek() == '*' && _position + 1 < _source.Length && _source[_position + 1] == '/')
                                {
                                    Advance();
                                    Advance();
                                    break;
                                }
                                if (Peek() == '\n')
                                {
                                    _line++;
                                    _column = 0;
                                }
                                Advance();
                            }
                            break;
                        }
                    }
                    return; // Not a comment, stop skipping

                default:
                    return;
            }
        }
    }

    private bool IsAtEnd() => _position >= _source.Length;

    private char Peek() => _source[_position];

    private char Advance()
    {
        char c = _source[_position++];
        _column++;
        return c;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || Peek() != expected) return false;
        Advance();
        return true;
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    private SourceLocation CurrentLocation() => new(_filePath, _line, _column);

    private Token MakeToken(TokenKind kind, string text) => new(kind, text, CurrentLocation());

    private static Token MakeToken(TokenKind kind, string text, SourceLocation location) => new(kind, text, location);
}
