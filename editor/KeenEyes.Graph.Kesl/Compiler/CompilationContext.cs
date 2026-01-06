using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Compiler;

/// <summary>
/// Context for tracking state during graph compilation.
/// </summary>
/// <remarks>
/// <para>
/// The compilation context tracks variable bindings, intermediate expressions,
/// and other state needed during the graph-to-AST conversion process.
/// </para>
/// </remarks>
public sealed class CompilationContext
{
    private readonly Dictionary<Entity, string> nodeVariables = [];
    private readonly Dictionary<string, Expression> namedVariables = [];
    private readonly List<CompilationError> errors = [];
    private int tempVarCounter;

    /// <summary>
    /// Gets compilation errors accumulated during compilation.
    /// </summary>
    public IReadOnlyList<CompilationError> Errors => errors;

    /// <summary>
    /// Generates a unique temporary variable name.
    /// </summary>
    public string GenerateTempVar()
    {
        return $"_t{tempVarCounter++}";
    }

    /// <summary>
    /// Registers a variable name for a node's output.
    /// </summary>
    /// <param name="node">The node entity.</param>
    /// <param name="varName">The variable name assigned to this node's output.</param>
    public void SetNodeVariable(Entity node, string varName)
    {
        nodeVariables[node] = varName;
    }

    /// <summary>
    /// Gets the variable name for a node's output.
    /// </summary>
    /// <param name="node">The node entity.</param>
    /// <returns>The variable name, or null if not set.</returns>
    public string? GetNodeVariable(Entity node)
    {
        return nodeVariables.TryGetValue(node, out var varName) ? varName : null;
    }

    /// <summary>
    /// Sets a named variable expression.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="expression">The expression representing the variable's value.</param>
    public void SetVariable(string name, Expression expression)
    {
        namedVariables[name] = expression;
    }

    /// <summary>
    /// Gets a named variable expression.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <returns>The expression, or null if not found.</returns>
    public Expression? GetVariable(string name)
    {
        return namedVariables.TryGetValue(name, out var expr) ? expr : null;
    }

    /// <summary>
    /// Adds a compilation error.
    /// </summary>
    public void AddError(Entity node, int? portIndex, string message, string code)
    {
        errors.Add(new CompilationError(node, portIndex, message, code));
    }

    /// <summary>
    /// Clears all state for a fresh compilation.
    /// </summary>
    public void Clear()
    {
        nodeVariables.Clear();
        namedVariables.Clear();
        errors.Clear();
        tempVarCounter = 0;
    }
}
