using System.Numerics;

namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for registering and managing editor tools.
/// Allows plugins to add custom tools for viewport interaction.
/// </summary>
public interface IToolCapability : IEditorCapability
{
    /// <summary>
    /// Gets the currently active tool, or null if no tool is active.
    /// </summary>
    IEditorTool? ActiveTool { get; }

    /// <summary>
    /// Gets the ID of the currently active tool.
    /// </summary>
    string? ActiveToolId { get; }

    /// <summary>
    /// Registers a tool with the editor.
    /// </summary>
    /// <param name="id">The unique identifier for the tool.</param>
    /// <param name="tool">The tool instance.</param>
    void RegisterTool(string id, IEditorTool tool);

    /// <summary>
    /// Unregisters a tool from the editor.
    /// </summary>
    /// <param name="id">The tool ID to unregister.</param>
    /// <returns>True if the tool was found and unregistered.</returns>
    bool UnregisterTool(string id);

    /// <summary>
    /// Activates a tool by its ID.
    /// </summary>
    /// <param name="id">The tool ID to activate.</param>
    /// <returns>True if the tool was found and activated.</returns>
    bool ActivateTool(string id);

    /// <summary>
    /// Deactivates the current tool.
    /// </summary>
    void DeactivateTool();

    /// <summary>
    /// Gets a tool by its ID.
    /// </summary>
    /// <param name="id">The tool ID.</param>
    /// <returns>The tool, or null if not found.</returns>
    IEditorTool? GetTool(string id);

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    /// <returns>The registered tools with their IDs.</returns>
    IEnumerable<(string Id, IEditorTool Tool)> GetTools();

    /// <summary>
    /// Gets all registered tools in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>The tools in the specified category.</returns>
    IEnumerable<(string Id, IEditorTool Tool)> GetToolsInCategory(string category);

    /// <summary>
    /// Event raised when the active tool changes.
    /// </summary>
    event Action<ToolChangedEventArgs>? ActiveToolChanged;

    /// <summary>
    /// Event raised when a tool is registered.
    /// </summary>
    event Action<string, IEditorTool>? ToolRegistered;

    /// <summary>
    /// Event raised when a tool is unregistered.
    /// </summary>
    event Action<string>? ToolUnregistered;
}

/// <summary>
/// Event arguments for when the active tool changes.
/// </summary>
public sealed class ToolChangedEventArgs
{
    /// <summary>
    /// Gets the previously active tool ID, or null if no tool was active.
    /// </summary>
    public string? PreviousToolId { get; init; }

    /// <summary>
    /// Gets the previously active tool, or null if no tool was active.
    /// </summary>
    public IEditorTool? PreviousTool { get; init; }

    /// <summary>
    /// Gets the newly active tool ID, or null if no tool is now active.
    /// </summary>
    public string? NewToolId { get; init; }

    /// <summary>
    /// Gets the newly active tool, or null if no tool is now active.
    /// </summary>
    public IEditorTool? NewTool { get; init; }
}

/// <summary>
/// Interface for editor tools that handle viewport interaction.
/// </summary>
public interface IEditorTool
{
    /// <summary>
    /// Gets the display name of the tool.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the optional icon identifier for the tool.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets the category for organizing tools in the toolbar.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the optional tooltip text.
    /// </summary>
    string? Tooltip { get; }

    /// <summary>
    /// Gets the optional keyboard shortcut to activate this tool.
    /// </summary>
    string? Shortcut { get; }

    /// <summary>
    /// Gets whether this tool is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Called when the tool is activated.
    /// </summary>
    /// <param name="context">The tool context.</param>
    void OnActivate(ToolContext context);

    /// <summary>
    /// Called when the tool is deactivated.
    /// </summary>
    /// <param name="context">The tool context.</param>
    void OnDeactivate(ToolContext context);

    /// <summary>
    /// Called every frame while the tool is active.
    /// </summary>
    /// <param name="context">The tool context.</param>
    /// <param name="deltaTime">Time since last update.</param>
    void Update(ToolContext context, float deltaTime);

    /// <summary>
    /// Called when the mouse button is pressed in the viewport.
    /// </summary>
    /// <param name="context">The tool context.</param>
    /// <param name="button">The mouse button that was pressed.</param>
    /// <param name="position">The mouse position in viewport-normalized coordinates.</param>
    /// <returns>True if the tool handled the input.</returns>
    bool OnMouseDown(ToolContext context, MouseButton button, Vector2 position);

    /// <summary>
    /// Called when the mouse button is released in the viewport.
    /// </summary>
    /// <param name="context">The tool context.</param>
    /// <param name="button">The mouse button that was released.</param>
    /// <param name="position">The mouse position in viewport-normalized coordinates.</param>
    /// <returns>True if the tool handled the input.</returns>
    bool OnMouseUp(ToolContext context, MouseButton button, Vector2 position);

    /// <summary>
    /// Called when the mouse moves in the viewport.
    /// </summary>
    /// <param name="context">The tool context.</param>
    /// <param name="position">The mouse position in viewport-normalized coordinates.</param>
    /// <param name="delta">The mouse movement delta.</param>
    /// <returns>True if the tool handled the input.</returns>
    bool OnMouseMove(ToolContext context, Vector2 position, Vector2 delta);

    /// <summary>
    /// Called to render tool-specific overlays.
    /// </summary>
    /// <param name="context">The gizmo render context.</param>
    void OnRender(GizmoRenderContext context);
}

/// <summary>
/// Context provided to editor tools.
/// </summary>
public sealed class ToolContext
{
    /// <summary>
    /// Gets the editor context for accessing editor services.
    /// </summary>
    public required IEditorContext EditorContext { get; init; }

    /// <summary>
    /// Gets the scene world, or null if no scene is open.
    /// </summary>
    public IWorld? SceneWorld { get; init; }

    /// <summary>
    /// Gets the currently selected entities.
    /// </summary>
    public required IReadOnlyList<Entity> SelectedEntities { get; init; }

    /// <summary>
    /// Gets the viewport bounds.
    /// </summary>
    public required ViewportBounds ViewportBounds { get; init; }

    /// <summary>
    /// Gets the view matrix.
    /// </summary>
    public required Matrix4x4 ViewMatrix { get; init; }

    /// <summary>
    /// Gets the projection matrix.
    /// </summary>
    public required Matrix4x4 ProjectionMatrix { get; init; }

    /// <summary>
    /// Gets the camera position.
    /// </summary>
    public required Vector3 CameraPosition { get; init; }

    /// <summary>
    /// Gets the camera forward direction.
    /// </summary>
    public required Vector3 CameraForward { get; init; }
}

/// <summary>
/// Base class for editor tools that provides default implementations.
/// </summary>
public abstract class EditorToolBase : IEditorTool
{
    /// <inheritdoc/>
    public abstract string DisplayName { get; }

    /// <inheritdoc/>
    public virtual string? Icon => null;

    /// <inheritdoc/>
    public virtual string Category => "General";

    /// <inheritdoc/>
    public virtual string? Tooltip => null;

    /// <inheritdoc/>
    public virtual string? Shortcut => null;

    /// <inheritdoc/>
    public virtual bool IsEnabled => true;

    /// <inheritdoc/>
    public virtual void OnActivate(ToolContext context) { }

    /// <inheritdoc/>
    public virtual void OnDeactivate(ToolContext context) { }

    /// <inheritdoc/>
    public virtual void Update(ToolContext context, float deltaTime) { }

    /// <inheritdoc/>
    public virtual bool OnMouseDown(ToolContext context, MouseButton button, Vector2 position) => false;

    /// <inheritdoc/>
    public virtual bool OnMouseUp(ToolContext context, MouseButton button, Vector2 position) => false;

    /// <inheritdoc/>
    public virtual bool OnMouseMove(ToolContext context, Vector2 position, Vector2 delta) => false;

    /// <inheritdoc/>
    public virtual void OnRender(GizmoRenderContext context) { }
}

/// <summary>
/// Standard tool categories for organizing tools.
/// </summary>
public static class ToolCategories
{
    /// <summary>
    /// Selection and navigation tools.
    /// </summary>
    public const string Selection = "Selection";

    /// <summary>
    /// Transform tools (move, rotate, scale).
    /// </summary>
    public const string Transform = "Transform";

    /// <summary>
    /// Creation and placement tools.
    /// </summary>
    public const string Creation = "Creation";

    /// <summary>
    /// Terrain editing tools.
    /// </summary>
    public const string Terrain = "Terrain";

    /// <summary>
    /// Physics debugging tools.
    /// </summary>
    public const string Physics = "Physics";

    /// <summary>
    /// Custom plugin tools.
    /// </summary>
    public const string Custom = "Custom";
}

/// <summary>
/// Mouse button enumeration for tool input.
/// </summary>
public enum MouseButton
{
    /// <summary>
    /// Left mouse button.
    /// </summary>
    Left,

    /// <summary>
    /// Right mouse button.
    /// </summary>
    Right,

    /// <summary>
    /// Middle mouse button (scroll wheel click).
    /// </summary>
    Middle,

    /// <summary>
    /// Extra button 1 (typically back).
    /// </summary>
    Button4,

    /// <summary>
    /// Extra button 2 (typically forward).
    /// </summary>
    Button5
}
