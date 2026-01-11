using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Shaders.Compiler.Parsing.Ast;

/// <summary>
/// Base class for all AST nodes in KESL.
/// </summary>
public abstract record AstNode(SourceLocation Location);

/// <summary>
/// Represents a complete KESL source file.
/// </summary>
/// <param name="Declarations">The top-level declarations in the file.</param>
public record SourceFile(IReadOnlyList<Declaration> Declarations, SourceLocation Location) : AstNode(Location);

/// <summary>
/// Base class for top-level declarations.
/// </summary>
public abstract record Declaration(SourceLocation Location) : AstNode(Location);

/// <summary>
/// A component type declaration.
/// </summary>
/// <param name="Name">The component name.</param>
/// <param name="Fields">The component's fields.</param>
public record ComponentDeclaration(
    string Name,
    IReadOnlyList<FieldDeclaration> Fields,
    SourceLocation Location
) : Declaration(Location);

/// <summary>
/// A field within a component declaration.
/// </summary>
/// <param name="Name">The field name.</param>
/// <param name="Type">The field type.</param>
public record FieldDeclaration(string Name, TypeRef Type, SourceLocation Location) : AstNode(Location);

/// <summary>
/// A compute shader declaration.
/// </summary>
/// <param name="Name">The shader name.</param>
/// <param name="Query">The query block defining component access.</param>
/// <param name="Params">Optional parameters block.</param>
/// <param name="Execute">The execute block containing shader logic.</param>
public record ComputeDeclaration(
    string Name,
    QueryBlock Query,
    ParamsBlock? Params,
    ExecuteBlock Execute,
    SourceLocation Location
) : Declaration(Location);

/// <summary>
/// A vertex shader declaration.
/// </summary>
/// <param name="Name">The shader name.</param>
/// <param name="Inputs">The input attributes block.</param>
/// <param name="Outputs">The output attributes block.</param>
/// <param name="Params">Optional parameters block.</param>
/// <param name="Execute">The execute block containing shader logic.</param>
public record VertexDeclaration(
    string Name,
    InputBlock Inputs,
    OutputBlock Outputs,
    ParamsBlock? Params,
    ExecuteBlock Execute,
    SourceLocation Location
) : Declaration(Location);

/// <summary>
/// A fragment shader declaration.
/// </summary>
/// <param name="Name">The shader name.</param>
/// <param name="Inputs">The input attributes block.</param>
/// <param name="Outputs">The output attributes block.</param>
/// <param name="Params">Optional parameters block.</param>
/// <param name="Execute">The execute block containing shader logic.</param>
public record FragmentDeclaration(
    string Name,
    InputBlock Inputs,
    OutputBlock Outputs,
    ParamsBlock? Params,
    ExecuteBlock Execute,
    SourceLocation Location
) : Declaration(Location);

/// <summary>
/// Represents the query block of a compute shader.
/// </summary>
/// <param name="Bindings">The component bindings.</param>
public record QueryBlock(IReadOnlyList<QueryBinding> Bindings, SourceLocation Location) : AstNode(Location);

/// <summary>
/// A single component binding in a query block.
/// </summary>
/// <param name="AccessMode">The access mode (read, write, optional, without).</param>
/// <param name="ComponentName">The component name.</param>
public record QueryBinding(AccessMode AccessMode, string ComponentName, SourceLocation Location) : AstNode(Location);

/// <summary>
/// Specifies how a component is accessed in a shader.
/// </summary>
public enum AccessMode
{
    /// <summary>Read-only access.</summary>
    Read,
    /// <summary>Read-write access.</summary>
    Write,
    /// <summary>Optional component that may not exist.</summary>
    Optional,
    /// <summary>Exclude entities with this component.</summary>
    Without
}

/// <summary>
/// Represents the params block of a compute shader.
/// </summary>
/// <param name="Parameters">The parameter declarations.</param>
public record ParamsBlock(IReadOnlyList<ParamDeclaration> Parameters, SourceLocation Location) : AstNode(Location);

/// <summary>
/// A single parameter declaration.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Type">The parameter type.</param>
public record ParamDeclaration(string Name, TypeRef Type, SourceLocation Location) : AstNode(Location);

/// <summary>
/// Represents the input attributes block of a vertex or fragment shader.
/// </summary>
/// <param name="Attributes">The input attribute declarations.</param>
public record InputBlock(IReadOnlyList<AttributeDeclaration> Attributes, SourceLocation Location) : AstNode(Location);

/// <summary>
/// Represents the output attributes block of a vertex or fragment shader.
/// </summary>
/// <param name="Attributes">The output attribute declarations.</param>
public record OutputBlock(IReadOnlyList<AttributeDeclaration> Attributes, SourceLocation Location) : AstNode(Location);

/// <summary>
/// A single attribute declaration with optional location binding.
/// </summary>
/// <param name="Name">The attribute name.</param>
/// <param name="Type">The attribute type.</param>
/// <param name="LocationIndex">Optional layout location index (e.g., @ 0).</param>
public record AttributeDeclaration(
    string Name,
    TypeRef Type,
    int? LocationIndex,
    SourceLocation Location
) : AstNode(Location);

/// <summary>
/// Represents the execute block of a compute shader.
/// </summary>
/// <param name="Body">The statements in the execute block.</param>
public record ExecuteBlock(IReadOnlyList<Statement> Body, SourceLocation Location) : AstNode(Location);

/// <summary>
/// Represents a type reference.
/// </summary>
public abstract record TypeRef(SourceLocation Location) : AstNode(Location);

/// <summary>
/// A primitive type reference (float, int, vec3, etc.).
/// </summary>
/// <param name="Kind">The primitive type kind.</param>
public record PrimitiveType(PrimitiveTypeKind Kind, SourceLocation Location) : TypeRef(Location);

/// <summary>
/// The kinds of primitive types in KESL.
/// </summary>
public enum PrimitiveTypeKind
{
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
    Mat4
}
