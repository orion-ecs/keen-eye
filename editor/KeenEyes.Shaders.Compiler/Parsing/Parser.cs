using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.Parsing;

/// <summary>
/// Recursive descent parser for KESL (KeenEyes Shader Language).
/// </summary>
public sealed class Parser
{
    private readonly List<Token> _tokens;
    private readonly List<CompilerError> _errors;
    private int _current;

    /// <summary>
    /// Creates a new parser for the given tokens.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _errors = [];
        _current = 0;
    }

    /// <summary>
    /// Gets the parse errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<CompilerError> Errors => _errors;

    /// <summary>
    /// Gets whether the parser encountered any errors.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Parses the token stream into a source file AST.
    /// </summary>
    public SourceFile Parse()
    {
        var declarations = new List<Declaration>();
        var startLocation = Current.Location;

        while (!IsAtEnd())
        {
            try
            {
                var decl = ParseDeclaration();
                if (decl != null)
                {
                    declarations.Add(decl);
                }
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }

        return new SourceFile(declarations, startLocation);
    }

    private Declaration? ParseDeclaration()
    {
        if (Check(TokenKind.Component))
        {
            return ParseComponentDeclaration();
        }

        if (Check(TokenKind.Compute))
        {
            return ParseComputeDeclaration();
        }

        // Throw to trigger synchronization - otherwise we'd loop infinitely
        throw Error(Current, "Expected 'component' or 'compute' declaration");
    }

    private ComponentDeclaration ParseComponentDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Component, "Expected 'component'");
        var name = Consume(TokenKind.Identifier, "Expected component name").Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after component name");

        var fields = new List<FieldDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            fields.Add(ParseFieldDeclaration());

            // Allow optional comma or newline separation
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after component fields");

        return new ComponentDeclaration(name, fields, location);
    }

    private FieldDeclaration ParseFieldDeclaration()
    {
        var location = Current.Location;
        var name = Consume(TokenKind.Identifier, "Expected field name").Text;
        Consume(TokenKind.Colon, "Expected ':' after field name");
        var type = ParseType();

        return new FieldDeclaration(name, type, location);
    }

    private ComputeDeclaration ParseComputeDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Compute, "Expected 'compute'");
        var name = Consume(TokenKind.Identifier, "Expected shader name").Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after shader name");

        // Parse query block (required)
        var query = ParseQueryBlock();

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        // Parse execute block (required)
        var execute = ParseExecuteBlock();

        Consume(TokenKind.RightBrace, "Expected '}' after shader body");

        return new ComputeDeclaration(name, query, paramsBlock, execute, location);
    }

    private QueryBlock ParseQueryBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Query, "Expected 'query'");
        Consume(TokenKind.LeftBrace, "Expected '{' after 'query'");

        var bindings = new List<QueryBinding>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            bindings.Add(ParseQueryBinding());
        }

        Consume(TokenKind.RightBrace, "Expected '}' after query bindings");

        return new QueryBlock(bindings, location);
    }

    private QueryBinding ParseQueryBinding()
    {
        var location = Current.Location;

        var accessMode = Current.Kind switch
        {
            TokenKind.Read => AccessMode.Read,
            TokenKind.Write => AccessMode.Write,
            TokenKind.Optional => AccessMode.Optional,
            TokenKind.Without => AccessMode.Without,
            _ => throw Error(Current, "Expected 'read', 'write', 'optional', or 'without'")
        };
        Advance();

        var componentName = Consume(TokenKind.Identifier, "Expected component name").Text;

        return new QueryBinding(accessMode, componentName, location);
    }

    private ParamsBlock ParseParamsBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Params, "Expected 'params'");
        Consume(TokenKind.LeftBrace, "Expected '{' after 'params'");

        var parameters = new List<ParamDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            parameters.Add(ParseParamDeclaration());
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after parameters");

        return new ParamsBlock(parameters, location);
    }

    private ParamDeclaration ParseParamDeclaration()
    {
        var location = Current.Location;
        var name = Consume(TokenKind.Identifier, "Expected parameter name").Text;
        Consume(TokenKind.Colon, "Expected ':' after parameter name");
        var type = ParseType();

        return new ParamDeclaration(name, type, location);
    }

    private ExecuteBlock ParseExecuteBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Execute, "Expected 'execute'");
        Consume(TokenKind.LeftParen, "Expected '(' after 'execute'");
        Consume(TokenKind.RightParen, "Expected ')' after 'execute('");

        var statements = ParseBlock();

        return new ExecuteBlock(statements, location);
    }

    private List<Statement> ParseBlock()
    {
        Consume(TokenKind.LeftBrace, "Expected '{'");

        var statements = new List<Statement>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }

        Consume(TokenKind.RightBrace, "Expected '}'");

        return statements;
    }

    private Statement? ParseStatement()
    {
        if (Check(TokenKind.If))
        {
            return ParseIfStatement();
        }

        if (Check(TokenKind.For))
        {
            return ParseForStatement();
        }

        // Expression or assignment statement
        return ParseExpressionStatement();
    }

    private IfStatement ParseIfStatement()
    {
        var location = Current.Location;
        Consume(TokenKind.If, "Expected 'if'");
        Consume(TokenKind.LeftParen, "Expected '(' after 'if'");
        var condition = ParseExpression();
        Consume(TokenKind.RightParen, "Expected ')' after condition");

        var thenBranch = ParseBlock();

        List<Statement>? elseBranch = null;
        if (Match(TokenKind.Else))
        {
            elseBranch = ParseBlock();
        }

        return new IfStatement(condition, thenBranch, elseBranch, location);
    }

    private ForStatement ParseForStatement()
    {
        var location = Current.Location;
        Consume(TokenKind.For, "Expected 'for'");
        Consume(TokenKind.LeftParen, "Expected '(' after 'for'");
        var variable = Consume(TokenKind.Identifier, "Expected loop variable name").Text;
        Consume(TokenKind.Colon, "Expected ':' after variable");
        var start = ParseExpression();
        Consume(TokenKind.DotDot, "Expected '..' in range");
        var end = ParseExpression();
        Consume(TokenKind.RightParen, "Expected ')' after range");

        var body = ParseBlock();

        return new ForStatement(variable, start, end, body, location);
    }

    private Statement ParseExpressionStatement()
    {
        var location = Current.Location;
        var expr = ParseExpression();

        // Check for assignment
        if (Match(TokenKind.Equal))
        {
            var value = ParseExpression();
            Consume(TokenKind.Semicolon, "Expected ';' after assignment");
            return new AssignmentStatement(expr, value, location);
        }

        // Check for compound assignment
        if (Current.Kind is TokenKind.PlusEqual or TokenKind.MinusEqual
            or TokenKind.StarEqual or TokenKind.SlashEqual)
        {
            var op = Current.Kind switch
            {
                TokenKind.PlusEqual => CompoundOperator.PlusEquals,
                TokenKind.MinusEqual => CompoundOperator.MinusEquals,
                TokenKind.StarEqual => CompoundOperator.StarEquals,
                TokenKind.SlashEqual => CompoundOperator.SlashEquals,
                _ => throw new InvalidOperationException()
            };
            Advance();
            var value = ParseExpression();
            Consume(TokenKind.Semicolon, "Expected ';' after compound assignment");
            return new CompoundAssignmentStatement(expr, op, value, location);
        }

        Consume(TokenKind.Semicolon, "Expected ';' after expression");
        return new ExpressionStatement(expr, location);
    }

    private TypeRef ParseType()
    {
        var location = Current.Location;
        var kind = Current.Kind switch
        {
            TokenKind.Float => PrimitiveTypeKind.Float,
            TokenKind.Float2 => PrimitiveTypeKind.Float2,
            TokenKind.Float3 => PrimitiveTypeKind.Float3,
            TokenKind.Float4 => PrimitiveTypeKind.Float4,
            TokenKind.Int => PrimitiveTypeKind.Int,
            TokenKind.Int2 => PrimitiveTypeKind.Int2,
            TokenKind.Int3 => PrimitiveTypeKind.Int3,
            TokenKind.Int4 => PrimitiveTypeKind.Int4,
            TokenKind.Uint => PrimitiveTypeKind.Uint,
            TokenKind.Bool => PrimitiveTypeKind.Bool,
            TokenKind.Mat4 => PrimitiveTypeKind.Mat4,
            _ => throw Error(Current, "Expected type name")
        };
        Advance();

        return new PrimitiveType(kind, location);
    }

    #region Expression Parsing

    private Expression ParseExpression()
    {
        return ParseLogicalOr();
    }

    private Expression ParseLogicalOr()
    {
        var expr = ParseLogicalAnd();

        while (Match(TokenKind.PipePipe))
        {
            var location = Previous.Location;
            var right = ParseLogicalAnd();
            expr = new BinaryExpression(expr, BinaryOperator.Or, right, location);
        }

        return expr;
    }

    private Expression ParseLogicalAnd()
    {
        var expr = ParseEquality();

        while (Match(TokenKind.AmpAmp))
        {
            var location = Previous.Location;
            var right = ParseEquality();
            expr = new BinaryExpression(expr, BinaryOperator.And, right, location);
        }

        return expr;
    }

    private Expression ParseEquality()
    {
        var expr = ParseComparison();

        while (Match(TokenKind.EqualEqual, TokenKind.BangEqual))
        {
            var op = Previous.Kind == TokenKind.EqualEqual
                ? BinaryOperator.Equal
                : BinaryOperator.NotEqual;
            var location = Previous.Location;
            var right = ParseComparison();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseComparison()
    {
        var expr = ParseTerm();

        while (Match(TokenKind.Less, TokenKind.LessEqual, TokenKind.Greater, TokenKind.GreaterEqual))
        {
            var op = Previous.Kind switch
            {
                TokenKind.Less => BinaryOperator.Less,
                TokenKind.LessEqual => BinaryOperator.LessEqual,
                TokenKind.Greater => BinaryOperator.Greater,
                TokenKind.GreaterEqual => BinaryOperator.GreaterEqual,
                _ => throw new InvalidOperationException()
            };
            var location = Previous.Location;
            var right = ParseTerm();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseTerm()
    {
        var expr = ParseFactor();

        while (Match(TokenKind.Plus, TokenKind.Minus))
        {
            var op = Previous.Kind == TokenKind.Plus
                ? BinaryOperator.Add
                : BinaryOperator.Subtract;
            var location = Previous.Location;
            var right = ParseFactor();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseFactor()
    {
        var expr = ParseUnary();

        while (Match(TokenKind.Star, TokenKind.Slash))
        {
            var op = Previous.Kind == TokenKind.Star
                ? BinaryOperator.Multiply
                : BinaryOperator.Divide;
            var location = Previous.Location;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenKind.Bang, TokenKind.Minus))
        {
            var op = Previous.Kind == TokenKind.Bang
                ? UnaryOperator.Not
                : UnaryOperator.Negate;
            var location = Previous.Location;
            var operand = ParseUnary();
            return new UnaryExpression(op, operand, location);
        }

        return ParsePostfix();
    }

    private Expression ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(TokenKind.Dot))
            {
                var memberName = Consume(TokenKind.Identifier, "Expected member name after '.'").Text;
                expr = new MemberAccessExpression(expr, memberName, Previous.Location);
            }
            else if (Match(TokenKind.LeftParen))
            {
                // Function call - rewrite the expression
                if (expr is IdentifierExpression id)
                {
                    var args = ParseArguments();
                    Consume(TokenKind.RightParen, "Expected ')' after arguments");
                    expr = new CallExpression(id.Name, args, id.Location);
                }
                else
                {
                    throw Error(Previous, "Can only call functions by name");
                }
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private List<Expression> ParseArguments()
    {
        var args = new List<Expression>();

        if (!Check(TokenKind.RightParen))
        {
            do
            {
                args.Add(ParseExpression());
            } while (Match(TokenKind.Comma));
        }

        return args;
    }

    private Expression ParsePrimary()
    {
        var location = Current.Location;

        // Literals
        if (Match(TokenKind.IntLiteral))
        {
            return new IntLiteralExpression(Previous.IntValue, location);
        }

        if (Match(TokenKind.FloatLiteral))
        {
            return new FloatLiteralExpression(Previous.FloatValue, location);
        }

        if (Match(TokenKind.True))
        {
            return new BoolLiteralExpression(true, location);
        }

        if (Match(TokenKind.False))
        {
            return new BoolLiteralExpression(false, location);
        }

        // Has expression
        if (Match(TokenKind.Has))
        {
            var componentName = Consume(TokenKind.Identifier, "Expected component name after 'has'").Text;
            return new HasExpression(componentName, location);
        }

        // Identifier
        if (Match(TokenKind.Identifier))
        {
            return new IdentifierExpression(Previous.Text, location);
        }

        // Grouped expression
        if (Match(TokenKind.LeftParen))
        {
            var inner = ParseExpression();
            Consume(TokenKind.RightParen, "Expected ')' after expression");
            return new ParenthesizedExpression(inner, location);
        }

        throw Error(Current, "Expected expression");
    }

    #endregion

    #region Helper Methods

    private Token Current => _tokens[_current];
    private Token Previous => _tokens[_current - 1];
    private bool IsAtEnd() => Current.Kind == TokenKind.EndOfFile;

    private bool Check(TokenKind kind)
    {
        return !IsAtEnd() && Current.Kind == kind;
    }

    private bool Match(params TokenKind[] kinds)
    {
        foreach (var kind in kinds)
        {
            if (Check(kind))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }
        return Previous;
    }

    private Token Consume(TokenKind kind, string message)
    {
        if (Check(kind))
        {
            return Advance();
        }
        throw Error(Current, message);
    }

    private ParseException Error(Token token, string message)
    {
        _errors.Add(new CompilerError(message, token.Location));
        return new ParseException(message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            // Synchronize at statement boundaries
            if (Previous.Kind == TokenKind.Semicolon) return;
            if (Previous.Kind == TokenKind.RightBrace) return;

            // Only synchronize at top-level declaration keywords
            // (not Query, Params, Execute, etc. which are only valid inside declarations)
            switch (Current.Kind)
            {
                case TokenKind.Component:
                case TokenKind.Compute:
                    return;
            }

            Advance();
        }
    }

    #endregion
}

/// <summary>
/// Represents a compiler error with location information.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The source location of the error.</param>
public record CompilerError(string Message, SourceLocation Location)
{
    /// <summary>
    /// Returns a formatted error message.
    /// </summary>
    public override string ToString() => $"{Location}: error: {Message}";
}

/// <summary>
/// Internal exception used for parser error recovery.
/// </summary>
internal sealed class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}
