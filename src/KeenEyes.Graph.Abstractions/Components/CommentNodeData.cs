namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component storing the state of a comment node.
/// </summary>
/// <remarks>
/// Comment nodes display editable text annotations on the graph canvas.
/// They have no ports and serve purely as documentation.
/// </remarks>
public struct CommentNodeData : IComponent
{
    /// <summary>
    /// The comment text content.
    /// </summary>
    public string Text;

    /// <summary>
    /// The font size multiplier for the comment text.
    /// </summary>
    /// <remarks>
    /// Default is 1.0. Values greater than 1.0 increase text size.
    /// </remarks>
    public float FontScale;

    /// <summary>
    /// Creates a default comment node data.
    /// </summary>
    public static CommentNodeData Default => new()
    {
        Text = "Comment",
        FontScale = 1.0f
    };
}
