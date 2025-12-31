namespace KeenEyes.Shaders.Compiler.Lexing;

/// <summary>
/// Represents the kind of token in KESL source code.
/// </summary>
public enum TokenKind
{
    // End of file
    EndOfFile,

    // Literals
    Identifier,
    IntLiteral,
    FloatLiteral,

    // Keywords - Declarations
    Component,
    Compute,
    Query,
    Params,
    Execute,

    // Keywords - Query modifiers
    Read,
    Write,
    Optional,
    Without,

    // Keywords - Control flow
    If,
    Else,
    For,

    // Keywords - Types
    Float,
    Float2,
    Float3,
    Float4,
    Int,
    Int2,
    Int3,
    Int4,
    Uint,
    Bool,
    Mat4,

    // Keywords - Literals
    True,
    False,

    // Keywords - Built-in functions
    Has,

    // Punctuation
    LeftBrace,      // {
    RightBrace,     // }
    LeftParen,      // (
    RightParen,     // )
    LeftBracket,    // [
    RightBracket,   // ]
    Comma,          // ,
    Colon,          // :
    Semicolon,      // ;
    Dot,            // .
    DotDot,         // ..

    // Operators - Arithmetic
    Plus,           // +
    Minus,          // -
    Star,           // *
    Slash,          // /

    // Operators - Comparison
    Less,           // <
    LessEqual,      // <=
    Greater,        // >
    GreaterEqual,   // >=
    EqualEqual,     // ==
    BangEqual,      // !=

    // Operators - Logical
    Bang,           // !
    AmpAmp,         // &&
    PipePipe,       // ||

    // Operators - Assignment
    Equal,          // =
    PlusEqual,      // +=
    MinusEqual,     // -=
    StarEqual,      // *=
    SlashEqual,     // /=

    // Error
    Error
}
