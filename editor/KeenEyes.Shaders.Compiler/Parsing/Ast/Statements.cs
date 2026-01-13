using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Shaders.Compiler.Parsing.Ast;

/// <summary>
/// Base class for all statements.
/// </summary>
public abstract record Statement(SourceLocation Location) : AstNode(Location);

/// <summary>
/// An expression used as a statement.
/// </summary>
/// <param name="Expression">The expression.</param>
public record ExpressionStatement(Expression Expression, SourceLocation Location) : Statement(Location);

/// <summary>
/// An assignment statement.
/// </summary>
/// <param name="Target">The assignment target.</param>
/// <param name="Value">The value to assign.</param>
public record AssignmentStatement(Expression Target, Expression Value, SourceLocation Location) : Statement(Location);

/// <summary>
/// A compound assignment statement (+=, -=, *=, /=).
/// </summary>
/// <param name="Target">The assignment target.</param>
/// <param name="Operator">The compound operator (+=, -=, etc.).</param>
/// <param name="Value">The value to combine.</param>
public record CompoundAssignmentStatement(
    Expression Target,
    CompoundOperator Operator,
    Expression Value,
    SourceLocation Location
) : Statement(Location);

/// <summary>
/// The compound assignment operators.
/// </summary>
public enum CompoundOperator
{
    PlusEquals,
    MinusEquals,
    StarEquals,
    SlashEquals
}

/// <summary>
/// An if statement with optional else clause.
/// </summary>
/// <param name="Condition">The condition expression.</param>
/// <param name="ThenBranch">The statements to execute if condition is true.</param>
/// <param name="ElseBranch">The statements to execute if condition is false (optional).</param>
public record IfStatement(
    Expression Condition,
    IReadOnlyList<Statement> ThenBranch,
    IReadOnlyList<Statement>? ElseBranch,
    SourceLocation Location
) : Statement(Location);

/// <summary>
/// A for loop statement with range syntax.
/// </summary>
/// <param name="VariableName">The loop variable name.</param>
/// <param name="Start">The start of the range (inclusive).</param>
/// <param name="End">The end of the range (exclusive).</param>
/// <param name="Body">The loop body statements.</param>
public record ForStatement(
    string VariableName,
    Expression Start,
    Expression End,
    IReadOnlyList<Statement> Body,
    SourceLocation Location
) : Statement(Location);

/// <summary>
/// A block of statements.
/// </summary>
/// <param name="Statements">The statements in the block.</param>
public record BlockStatement(IReadOnlyList<Statement> Statements, SourceLocation Location) : Statement(Location);

/// <summary>
/// An emit statement in a geometry shader that outputs a vertex.
/// </summary>
/// <param name="Position">The position expression for the emitted vertex.</param>
public record EmitStatement(Expression Position, SourceLocation Location) : Statement(Location);

/// <summary>
/// An end primitive statement in a geometry shader that completes the current primitive.
/// </summary>
public record EndPrimitiveStatement(SourceLocation Location) : Statement(Location);
