using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Graph.Kesl.Editing;

/// <summary>
/// Maps between graph nodes and source code locations.
/// </summary>
/// <remarks>
/// <para>
/// The source mapping enables bidirectional navigation between the visual
/// graph representation and the textual KESL source. This is used for:
/// </para>
/// <list type="bullet">
/// <item>Click-to-navigate from graph to source</item>
/// <item>Click-to-navigate from source to graph</item>
/// <item>Error highlighting in both views</item>
/// <item>Synchronized selection</item>
/// </list>
/// </remarks>
public sealed class SourceMapping
{
    private readonly Dictionary<Entity, SourceSpan> nodeToSource = [];
    private readonly Dictionary<SourceLocation, Entity> sourceToNode = [];
    private readonly Dictionary<string, Entity> variableToNode = [];

    /// <summary>
    /// Gets all mapped nodes.
    /// </summary>
    public IEnumerable<Entity> Nodes => nodeToSource.Keys;

    /// <summary>
    /// Adds a mapping from a node to a source location.
    /// </summary>
    /// <param name="node">The graph node entity.</param>
    /// <param name="span">The source span for this node.</param>
    public void AddMapping(Entity node, SourceSpan span)
    {
        nodeToSource[node] = span;
        sourceToNode[span.Start] = node;
    }

    /// <summary>
    /// Adds a variable name to node mapping.
    /// </summary>
    /// <param name="variableName">The variable name in source.</param>
    /// <param name="node">The node that produces this variable.</param>
    public void AddVariableMapping(string variableName, Entity node)
    {
        variableToNode[variableName] = node;
    }

    /// <summary>
    /// Gets the source span for a graph node.
    /// </summary>
    /// <param name="node">The node to look up.</param>
    /// <returns>The source span, or null if not mapped.</returns>
    public SourceSpan? GetSourceSpan(Entity node)
    {
        return nodeToSource.TryGetValue(node, out var span) ? span : null;
    }

    /// <summary>
    /// Gets the graph node at a source location.
    /// </summary>
    /// <param name="location">The source location.</param>
    /// <returns>The node entity, or null if not mapped.</returns>
    public Entity? GetNodeAtLocation(SourceLocation location)
    {
        // First try exact match
        if (sourceToNode.TryGetValue(location, out var node))
        {
            return node;
        }

        // Find the nearest node whose span contains this location
        foreach (var (nodeEntity, span) in nodeToSource)
        {
            if (span.Contains(location))
            {
                return nodeEntity;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the node that produces a variable.
    /// </summary>
    /// <param name="variableName">The variable name.</param>
    /// <returns>The node entity, or null if not found.</returns>
    public Entity? GetNodeForVariable(string variableName)
    {
        return variableToNode.TryGetValue(variableName, out var node) ? node : null;
    }

    /// <summary>
    /// Clears all mappings.
    /// </summary>
    public void Clear()
    {
        nodeToSource.Clear();
        sourceToNode.Clear();
        variableToNode.Clear();
    }
}

/// <summary>
/// Represents a span of source code.
/// </summary>
/// <param name="Start">The start location.</param>
/// <param name="End">The end location.</param>
public readonly record struct SourceSpan(SourceLocation Start, SourceLocation End)
{
    /// <summary>
    /// Checks if a location is within this span.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <returns>True if the location is within the span.</returns>
    public bool Contains(SourceLocation location)
    {
        // Same file check
        if (location.FilePath != Start.FilePath)
        {
            return false;
        }

        // Check if within line range
        if (location.Line < Start.Line || location.Line > End.Line)
        {
            return false;
        }

        // Same line - check column
        if (location.Line == Start.Line && location.Column < Start.Column)
        {
            return false;
        }

        if (location.Line == End.Line && location.Column > End.Column)
        {
            return false;
        }

        return true;
    }
}
