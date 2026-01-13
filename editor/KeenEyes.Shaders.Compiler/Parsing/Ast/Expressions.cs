using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Shaders.Compiler.Parsing.Ast;

/// <summary>
/// Base class for all expressions.
/// </summary>
public abstract record Expression(SourceLocation Location) : AstNode(Location);

/// <summary>
/// An identifier expression.
/// </summary>
/// <param name="Name">The identifier name.</param>
public record IdentifierExpression(string Name, SourceLocation Location) : Expression(Location);

/// <summary>
/// An integer literal expression.
/// </summary>
/// <param name="Value">The integer value.</param>
public record IntLiteralExpression(int Value, SourceLocation Location) : Expression(Location);

/// <summary>
/// A float literal expression.
/// </summary>
/// <param name="Value">The float value.</param>
public record FloatLiteralExpression(float Value, SourceLocation Location) : Expression(Location);

/// <summary>
/// A boolean literal expression.
/// </summary>
/// <param name="Value">The boolean value.</param>
public record BoolLiteralExpression(bool Value, SourceLocation Location) : Expression(Location);

/// <summary>
/// A member access expression (e.g., Position.x).
/// </summary>
/// <param name="Object">The object to access the member on.</param>
/// <param name="MemberName">The name of the member.</param>
public record MemberAccessExpression(Expression Object, string MemberName, SourceLocation Location) : Expression(Location);

/// <summary>
/// A function call expression.
/// </summary>
/// <param name="FunctionName">The function name.</param>
/// <param name="Arguments">The function arguments.</param>
public record CallExpression(string FunctionName, IReadOnlyList<Expression> Arguments, SourceLocation Location) : Expression(Location);

/// <summary>
/// A binary operation expression.
/// </summary>
/// <param name="Left">The left operand.</param>
/// <param name="Operator">The operator.</param>
/// <param name="Right">The right operand.</param>
public record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right, SourceLocation Location) : Expression(Location);

/// <summary>
/// A unary operation expression.
/// </summary>
/// <param name="Operator">The operator.</param>
/// <param name="Operand">The operand.</param>
public record UnaryExpression(UnaryOperator Operator, Expression Operand, SourceLocation Location) : Expression(Location);

/// <summary>
/// A 'has' expression for checking if a component exists.
/// </summary>
/// <param name="ComponentName">The component name to check.</param>
public record HasExpression(string ComponentName, SourceLocation Location) : Expression(Location);

/// <summary>
/// A parenthesized expression.
/// </summary>
/// <param name="Inner">The inner expression.</param>
public record ParenthesizedExpression(Expression Inner, SourceLocation Location) : Expression(Location);

/// <summary>
/// An array index expression (e.g., vertices[i]).
/// </summary>
/// <param name="Array">The array expression to index.</param>
/// <param name="Index">The index expression.</param>
public record IndexExpression(Expression Array, Expression Index, SourceLocation Location) : Expression(Location);

/// <summary>
/// The binary operators.
/// </summary>
public enum BinaryOperator
{
    // Arithmetic
    Add,
    Subtract,
    Multiply,
    Divide,

    // Comparison
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    Equal,
    NotEqual,

    // Logical
    And,
    Or
}

/// <summary>
/// The unary operators.
/// </summary>
public enum UnaryOperator
{
    Negate,
    Not
}
