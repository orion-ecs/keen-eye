using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Shaders.Compiler.Tests;

public class LexerTests
{
    [Fact]
    public void Tokenize_EmptySource_ReturnsEndOfFile()
    {
        var lexer = new Lexer("");
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenKind.EndOfFile, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Keywords_ReturnsCorrectTokenKinds()
    {
        var lexer = new Lexer("component compute query params execute read write optional without");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Component, tokens[0].Kind);
        Assert.Equal(TokenKind.Compute, tokens[1].Kind);
        Assert.Equal(TokenKind.Query, tokens[2].Kind);
        Assert.Equal(TokenKind.Params, tokens[3].Kind);
        Assert.Equal(TokenKind.Execute, tokens[4].Kind);
        Assert.Equal(TokenKind.Read, tokens[5].Kind);
        Assert.Equal(TokenKind.Write, tokens[6].Kind);
        Assert.Equal(TokenKind.Optional, tokens[7].Kind);
        Assert.Equal(TokenKind.Without, tokens[8].Kind);
    }

    [Fact]
    public void Tokenize_TypeKeywords_ReturnsCorrectTokenKinds()
    {
        var lexer = new Lexer("float float2 float3 float4 int uint bool mat4");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Float, tokens[0].Kind);
        Assert.Equal(TokenKind.Float2, tokens[1].Kind);
        Assert.Equal(TokenKind.Float3, tokens[2].Kind);
        Assert.Equal(TokenKind.Float4, tokens[3].Kind);
        Assert.Equal(TokenKind.Int, tokens[4].Kind);
        Assert.Equal(TokenKind.Uint, tokens[5].Kind);
        Assert.Equal(TokenKind.Bool, tokens[6].Kind);
        Assert.Equal(TokenKind.Mat4, tokens[7].Kind);
    }

    [Fact]
    public void Tokenize_Identifier_ReturnsIdentifierToken()
    {
        var lexer = new Lexer("Position Velocity myVar _underscore");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("Position", tokens[0].Text);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("Velocity", tokens[1].Text);
        Assert.Equal(TokenKind.Identifier, tokens[2].Kind);
        Assert.Equal("myVar", tokens[2].Text);
        Assert.Equal(TokenKind.Identifier, tokens[3].Kind);
        Assert.Equal("_underscore", tokens[3].Text);
    }

    [Fact]
    public void Tokenize_IntLiteral_ReturnsIntLiteralToken()
    {
        var lexer = new Lexer("42 0 123456");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.IntLiteral, tokens[0].Kind);
        Assert.Equal(42, tokens[0].IntValue);
        Assert.Equal(TokenKind.IntLiteral, tokens[1].Kind);
        Assert.Equal(0, tokens[1].IntValue);
        Assert.Equal(TokenKind.IntLiteral, tokens[2].Kind);
        Assert.Equal(123456, tokens[2].IntValue);
    }

    [Fact]
    public void Tokenize_FloatLiteral_ReturnsFloatLiteralToken()
    {
        var lexer = new Lexer("3.14 0.5 1.0f 2.5e10");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.FloatLiteral, tokens[0].Kind);
        Assert.Equal(3.14f, tokens[0].FloatValue, 0.001f);
        Assert.Equal(TokenKind.FloatLiteral, tokens[1].Kind);
        Assert.Equal(0.5f, tokens[1].FloatValue, 0.001f);
        Assert.Equal(TokenKind.FloatLiteral, tokens[2].Kind);
        Assert.Equal(1.0f, tokens[2].FloatValue, 0.001f);
        Assert.Equal(TokenKind.FloatLiteral, tokens[3].Kind);
    }

    [Fact]
    public void Tokenize_Operators_ReturnsCorrectTokenKinds()
    {
        var lexer = new Lexer("+ - * / = += -= *= /= == != < <= > >= && || !");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Plus, tokens[0].Kind);
        Assert.Equal(TokenKind.Minus, tokens[1].Kind);
        Assert.Equal(TokenKind.Star, tokens[2].Kind);
        Assert.Equal(TokenKind.Slash, tokens[3].Kind);
        Assert.Equal(TokenKind.Equal, tokens[4].Kind);
        Assert.Equal(TokenKind.PlusEqual, tokens[5].Kind);
        Assert.Equal(TokenKind.MinusEqual, tokens[6].Kind);
        Assert.Equal(TokenKind.StarEqual, tokens[7].Kind);
        Assert.Equal(TokenKind.SlashEqual, tokens[8].Kind);
        Assert.Equal(TokenKind.EqualEqual, tokens[9].Kind);
        Assert.Equal(TokenKind.BangEqual, tokens[10].Kind);
        Assert.Equal(TokenKind.Less, tokens[11].Kind);
        Assert.Equal(TokenKind.LessEqual, tokens[12].Kind);
        Assert.Equal(TokenKind.Greater, tokens[13].Kind);
        Assert.Equal(TokenKind.GreaterEqual, tokens[14].Kind);
        Assert.Equal(TokenKind.AmpAmp, tokens[15].Kind);
        Assert.Equal(TokenKind.PipePipe, tokens[16].Kind);
        Assert.Equal(TokenKind.Bang, tokens[17].Kind);
    }

    [Fact]
    public void Tokenize_Punctuation_ReturnsCorrectTokenKinds()
    {
        var lexer = new Lexer("{ } ( ) [ ] , : ; . ..");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.LeftBrace, tokens[0].Kind);
        Assert.Equal(TokenKind.RightBrace, tokens[1].Kind);
        Assert.Equal(TokenKind.LeftParen, tokens[2].Kind);
        Assert.Equal(TokenKind.RightParen, tokens[3].Kind);
        Assert.Equal(TokenKind.LeftBracket, tokens[4].Kind);
        Assert.Equal(TokenKind.RightBracket, tokens[5].Kind);
        Assert.Equal(TokenKind.Comma, tokens[6].Kind);
        Assert.Equal(TokenKind.Colon, tokens[7].Kind);
        Assert.Equal(TokenKind.Semicolon, tokens[8].Kind);
        Assert.Equal(TokenKind.Dot, tokens[9].Kind);
        Assert.Equal(TokenKind.DotDot, tokens[10].Kind);
    }

    [Fact]
    public void Tokenize_LineComment_SkipsComment()
    {
        var lexer = new Lexer("a // this is a comment\nb");
        var tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal("a", tokens[0].Text);
        Assert.Equal("b", tokens[1].Text);
        Assert.Equal(TokenKind.EndOfFile, tokens[2].Kind);
    }

    [Fact]
    public void Tokenize_BlockComment_SkipsComment()
    {
        var lexer = new Lexer("a /* this is a \n multi-line comment */ b");
        var tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal("a", tokens[0].Text);
        Assert.Equal("b", tokens[1].Text);
        Assert.Equal(TokenKind.EndOfFile, tokens[2].Kind);
    }

    [Fact]
    public void Tokenize_TracksLineNumbers()
    {
        var lexer = new Lexer("a\nb\nc");
        var tokens = lexer.Tokenize();

        Assert.Equal(1, tokens[0].Location.Line);
        Assert.Equal(2, tokens[1].Location.Line);
        Assert.Equal(3, tokens[2].Location.Line);
    }

    [Fact]
    public void Tokenize_TracksColumnNumbers()
    {
        var lexer = new Lexer("a b c");
        var tokens = lexer.Tokenize();

        Assert.Equal(1, tokens[0].Location.Column);
        Assert.Equal(3, tokens[1].Location.Column);
        Assert.Equal(5, tokens[2].Location.Column);
    }
}
