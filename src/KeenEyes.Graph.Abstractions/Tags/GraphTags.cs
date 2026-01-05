namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Marks an entity as a graph canvas root.
/// </summary>
public struct GraphCanvasTag : ITagComponent;

/// <summary>
/// Marks a graph node as currently selected.
/// </summary>
public struct GraphNodeSelectedTag : ITagComponent;

/// <summary>
/// Marks a graph connection as currently selected.
/// </summary>
public struct GraphConnectionSelectedTag : ITagComponent;

/// <summary>
/// Marks a graph node as being actively dragged.
/// </summary>
public struct GraphNodeDraggingTag : ITagComponent;

/// <summary>
/// Marks a graph node as a ghost preview during duplication.
/// </summary>
public struct GraphNodeGhostTag : ITagComponent;

/// <summary>
/// Marks that space+drag panning mode is active.
/// </summary>
public struct GraphSpacePanningTag : ITagComponent;
