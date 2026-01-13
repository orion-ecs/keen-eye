namespace KeenEyes.Shaders.Compiler.Diagnostics;

/// <summary>
/// Standard error codes for KESL (KeenEyes Shader Language) compilation.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the pattern KESL{category}{number} where:
/// <list type="bullet">
/// <item><b>000-099</b>: Reserved for graph validation (defined in KeenEyes.Graph.Kesl)</item>
/// <item><b>100-199</b>: Lexer errors (tokenization)</item>
/// <item><b>200-299</b>: Parser errors (syntax)</item>
/// <item><b>300-399</b>: Semantic errors (type checking, resolution)</item>
/// </list>
/// </para>
/// </remarks>
public static class KeslErrorCodes
{
    #region Lexer Errors (100-199)

    /// <summary>
    /// An unexpected character was encountered during tokenization.
    /// </summary>
    public const string UnexpectedCharacter = "KESL100";

    /// <summary>
    /// A block comment was not properly terminated.
    /// </summary>
    public const string UnterminatedComment = "KESL101";

    /// <summary>
    /// A string literal was not properly terminated.
    /// </summary>
    public const string UnterminatedString = "KESL102";

    /// <summary>
    /// An invalid numeric literal was encountered.
    /// </summary>
    public const string InvalidNumber = "KESL103";

    /// <summary>
    /// An invalid escape sequence was found in a string.
    /// </summary>
    public const string InvalidEscapeSequence = "KESL104";

    #endregion

    #region Parser Errors (200-299)

    /// <summary>
    /// An unexpected token was encountered where something else was expected.
    /// </summary>
    public const string UnexpectedToken = "KESL200";

    /// <summary>
    /// A required token was missing from the input.
    /// </summary>
    public const string MissingToken = "KESL201";

    /// <summary>
    /// Expected a component or compute declaration at the top level.
    /// </summary>
    public const string ExpectedDeclaration = "KESL202";

    /// <summary>
    /// An invalid expression was encountered.
    /// </summary>
    public const string InvalidExpression = "KESL203";

    /// <summary>
    /// Expected a type name in a type annotation.
    /// </summary>
    public const string ExpectedTypeName = "KESL204";

    /// <summary>
    /// Expected a binding mode (read, write, optional, without).
    /// </summary>
    public const string ExpectedBindingMode = "KESL205";

    /// <summary>
    /// Expected an identifier (variable or field name).
    /// </summary>
    public const string ExpectedIdentifier = "KESL206";

    /// <summary>
    /// Expected a semicolon to end a statement.
    /// </summary>
    public const string ExpectedSemicolon = "KESL207";

    /// <summary>
    /// Expected an opening brace for a block.
    /// </summary>
    public const string ExpectedOpenBrace = "KESL208";

    /// <summary>
    /// Expected a closing brace to end a block.
    /// </summary>
    public const string ExpectedCloseBrace = "KESL209";

    /// <summary>
    /// Expected an opening parenthesis.
    /// </summary>
    public const string ExpectedOpenParen = "KESL210";

    /// <summary>
    /// Expected a closing parenthesis.
    /// </summary>
    public const string ExpectedCloseParen = "KESL211";

    /// <summary>
    /// Expected an opening bracket for array access.
    /// </summary>
    public const string ExpectedOpenBracket = "KESL212";

    /// <summary>
    /// Expected a closing bracket to end array access.
    /// </summary>
    public const string ExpectedCloseBracket = "KESL213";

    /// <summary>
    /// Expected a geometry input or output topology.
    /// </summary>
    public const string ExpectedTopology = "KESL214";

    #endregion

    #region Semantic Errors (300-399)

    /// <summary>
    /// A referenced component was not found.
    /// </summary>
    public const string UndefinedComponent = "KESL300";

    /// <summary>
    /// A duplicate definition was encountered.
    /// </summary>
    public const string DuplicateDefinition = "KESL301";

    /// <summary>
    /// A type mismatch was detected in an expression or assignment.
    /// </summary>
    public const string TypeMismatch = "KESL302";

    /// <summary>
    /// An undefined variable was referenced.
    /// </summary>
    public const string UndefinedVariable = "KESL303";

    /// <summary>
    /// A field does not exist on the specified component.
    /// </summary>
    public const string UndefinedField = "KESL304";

    /// <summary>
    /// A function or method was called with incorrect arguments.
    /// </summary>
    public const string InvalidArguments = "KESL305";

    /// <summary>
    /// An operation is not supported for the given types.
    /// </summary>
    public const string UnsupportedOperation = "KESL306";

    #endregion

    #region Graph Validation Reference

    // The following codes are defined in KeenEyes.Graph.Kesl/Validation/Rules:
    //
    // KESL001 - No ComputeShader root (SingleRootRule)
    // KESL002 - Multiple ComputeShader roots (SingleRootRule)
    // KESL010 - Missing required inputs (RequiredInputsRule)
    // KESL020 - Type incompatibility (TypeCompatibilityRule)
    // KESL030 - Graph contains cycles (NoCyclesRule)

    #endregion

    /// <summary>
    /// Gets a human-readable description for an error code.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <returns>A description of the error, or the code itself if unknown.</returns>
    public static string GetDescription(string code)
    {
        return code switch
        {
            // Lexer
            UnexpectedCharacter => "Unexpected character in source",
            UnterminatedComment => "Block comment was not closed",
            UnterminatedString => "String literal was not terminated",
            InvalidNumber => "Invalid numeric literal",
            InvalidEscapeSequence => "Invalid escape sequence in string",

            // Parser
            UnexpectedToken => "Unexpected token",
            MissingToken => "Missing expected token",
            ExpectedDeclaration => "Expected component or compute declaration",
            InvalidExpression => "Invalid expression",
            ExpectedTypeName => "Expected type name",
            ExpectedBindingMode => "Expected binding mode (read, write, optional, without)",
            ExpectedIdentifier => "Expected identifier",
            ExpectedSemicolon => "Expected semicolon",
            ExpectedOpenBrace => "Expected opening brace",
            ExpectedCloseBrace => "Expected closing brace",
            ExpectedOpenParen => "Expected opening parenthesis",
            ExpectedCloseParen => "Expected closing parenthesis",
            ExpectedOpenBracket => "Expected opening bracket",
            ExpectedCloseBracket => "Expected closing bracket",
            ExpectedTopology => "Expected geometry topology",

            // Semantic
            UndefinedComponent => "Component not found",
            DuplicateDefinition => "Duplicate definition",
            TypeMismatch => "Type mismatch",
            UndefinedVariable => "Undefined variable",
            UndefinedField => "Undefined field",
            InvalidArguments => "Invalid function arguments",
            UnsupportedOperation => "Unsupported operation for types",

            // Graph (reference)
            "KESL001" => "No ComputeShader root node",
            "KESL002" => "Multiple ComputeShader root nodes",
            "KESL010" => "Missing required inputs",
            "KESL020" => "Type incompatibility on connected ports",
            "KESL030" => "Graph contains a cycle",

            _ => code
        };
    }
}
