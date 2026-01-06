namespace KeenEyes.Graph.Nodes;

/// <summary>
/// Reserved type IDs for built-in node types.
/// </summary>
/// <remarks>
/// <para>
/// IDs 1-100 are reserved for built-in node types. User-defined node types
/// should use IDs starting at 101.
/// </para>
/// </remarks>
public static class BuiltInNodeIds
{
    /// <summary>
    /// Comment node - text annotation without ports.
    /// </summary>
    public const int Comment = 1;

    /// <summary>
    /// Reroute node - pass-through for routing connections.
    /// </summary>
    public const int Reroute = 2;

    /// <summary>
    /// Group node - subgraph container with interface ports.
    /// </summary>
    public const int Group = 3;

    /// <summary>
    /// The first available ID for user-defined node types.
    /// </summary>
    public const int UserDefinedStart = 101;
}
