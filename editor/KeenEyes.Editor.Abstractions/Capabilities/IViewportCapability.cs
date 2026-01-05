using System.Numerics;

namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for customizing the viewport rendering and interaction.
/// Allows plugins to add custom gizmo renderers, overlays, and pick handlers.
/// </summary>
public interface IViewportCapability : IEditorCapability
{
    /// <summary>
    /// Adds a custom gizmo renderer to the viewport.
    /// </summary>
    /// <param name="renderer">The gizmo renderer to add.</param>
    void AddGizmoRenderer(IGizmoRenderer renderer);

    /// <summary>
    /// Removes a custom gizmo renderer from the viewport.
    /// </summary>
    /// <param name="renderer">The gizmo renderer to remove.</param>
    void RemoveGizmoRenderer(IGizmoRenderer renderer);

    /// <summary>
    /// Gets all registered gizmo renderers.
    /// </summary>
    /// <returns>The gizmo renderers.</returns>
    IEnumerable<IGizmoRenderer> GetGizmoRenderers();

    /// <summary>
    /// Adds an overlay to the viewport.
    /// </summary>
    /// <param name="id">The unique identifier for the overlay.</param>
    /// <param name="overlay">The overlay to add.</param>
    void AddOverlay(string id, IViewportOverlay overlay);

    /// <summary>
    /// Removes an overlay from the viewport.
    /// </summary>
    /// <param name="id">The overlay ID to remove.</param>
    /// <returns>True if the overlay was found and removed.</returns>
    bool RemoveOverlay(string id);

    /// <summary>
    /// Sets the visibility of an overlay.
    /// </summary>
    /// <param name="id">The overlay ID.</param>
    /// <param name="visible">Whether the overlay should be visible.</param>
    void SetOverlayVisible(string id, bool visible);

    /// <summary>
    /// Gets whether an overlay is currently visible.
    /// </summary>
    /// <param name="id">The overlay ID.</param>
    /// <returns>True if the overlay exists and is visible.</returns>
    bool IsOverlayVisible(string id);

    /// <summary>
    /// Gets all registered overlay IDs.
    /// </summary>
    /// <returns>The overlay IDs.</returns>
    IEnumerable<string> GetOverlayIds();

    /// <summary>
    /// Adds a pick handler for custom entity picking logic.
    /// </summary>
    /// <param name="handler">The pick handler to add.</param>
    void AddPickHandler(IPickHandler handler);

    /// <summary>
    /// Removes a pick handler.
    /// </summary>
    /// <param name="handler">The pick handler to remove.</param>
    void RemovePickHandler(IPickHandler handler);

    /// <summary>
    /// Gets all registered pick handlers.
    /// </summary>
    /// <returns>The pick handlers.</returns>
    IEnumerable<IPickHandler> GetPickHandlers();

    /// <summary>
    /// Event raised when a gizmo renderer is added.
    /// </summary>
    event Action<IGizmoRenderer>? GizmoRendererAdded;

    /// <summary>
    /// Event raised when a gizmo renderer is removed.
    /// </summary>
    event Action<IGizmoRenderer>? GizmoRendererRemoved;
}

/// <summary>
/// Interface for custom gizmo renderers that draw in the viewport.
/// </summary>
public interface IGizmoRenderer
{
    /// <summary>
    /// Gets the unique identifier for this renderer.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name for this renderer.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets or sets whether this renderer is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets the render order. Lower values render first (behind).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Called to render the gizmo.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    void Render(GizmoRenderContext context);

    /// <summary>
    /// Called to check if this renderer should render for the given entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="sceneWorld">The scene world.</param>
    /// <returns>True if this renderer should render for the entity.</returns>
    bool ShouldRender(Entity entity, IWorld sceneWorld);
}

/// <summary>
/// Context provided to gizmo renderers during rendering.
/// </summary>
public sealed class GizmoRenderContext
{
    /// <summary>
    /// Gets the scene world being rendered.
    /// </summary>
    public required IWorld SceneWorld { get; init; }

    /// <summary>
    /// Gets the currently selected entities.
    /// </summary>
    public required IReadOnlyList<Entity> SelectedEntities { get; init; }

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
    /// Gets the viewport bounds in screen coordinates.
    /// </summary>
    public required ViewportBounds Bounds { get; init; }

    /// <summary>
    /// Gets the delta time since last frame.
    /// </summary>
    public float DeltaTime { get; init; }

    /// <summary>
    /// Gets the gizmo drawing interface for rendering primitives.
    /// </summary>
    public required IGizmoDrawer Drawer { get; init; }

    /// <summary>
    /// Draws a triangle with the specified vertices and color.
    /// </summary>
    /// <param name="v0">First vertex.</param>
    /// <param name="v1">Second vertex.</param>
    /// <param name="v2">Third vertex.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    public void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector4 color)
        => Drawer.DrawTriangle(v0, v1, v2, color);

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="start">The start point.</param>
    /// <param name="end">The end point.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    /// <param name="width">The line width in pixels.</param>
    public void DrawLine(Vector3 start, Vector3 end, Vector4 color, float width = 1.0f)
        => Drawer.DrawLine(start, end, color, width);

    /// <summary>
    /// Draws a point at the specified position.
    /// </summary>
    /// <param name="position">The point position.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    /// <param name="size">The point size in pixels.</param>
    public void DrawPoint(Vector3 position, Vector4 color, float size = 4.0f)
        => Drawer.DrawPoint(position, color, size);
}

/// <summary>
/// Interface for drawing gizmo primitives in the viewport.
/// </summary>
public interface IGizmoDrawer
{
    /// <summary>
    /// Draws a triangle with the specified vertices and color.
    /// </summary>
    /// <param name="v0">First vertex.</param>
    /// <param name="v1">Second vertex.</param>
    /// <param name="v2">Third vertex.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector4 color);

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="start">The start point.</param>
    /// <param name="end">The end point.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    /// <param name="width">The line width in pixels.</param>
    void DrawLine(Vector3 start, Vector3 end, Vector4 color, float width = 1.0f);

    /// <summary>
    /// Draws a point at the specified position.
    /// </summary>
    /// <param name="position">The point position.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    /// <param name="size">The point size in pixels.</param>
    void DrawPoint(Vector3 position, Vector4 color, float size = 4.0f);

    /// <summary>
    /// Draws a wireframe box.
    /// </summary>
    /// <param name="min">The minimum corner.</param>
    /// <param name="max">The maximum corner.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    /// <param name="lineWidth">The line width in pixels.</param>
    void DrawWireBox(Vector3 min, Vector3 max, Vector4 color, float lineWidth = 1.0f);

    /// <summary>
    /// Draws a wireframe sphere.
    /// </summary>
    /// <param name="center">The center position.</param>
    /// <param name="radius">The sphere radius.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    /// <param name="segments">The number of segments for the circle approximation.</param>
    void DrawWireSphere(Vector3 center, float radius, Vector4 color, int segments = 16);

    /// <summary>
    /// Draws a text label at the specified world position.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="text">The text to display.</param>
    /// <param name="color">The text color (RGBA, 0-1).</param>
    void DrawText(Vector3 position, string text, Vector4 color);
}

/// <summary>
/// Represents viewport bounds in screen coordinates.
/// </summary>
public readonly struct ViewportBounds
{
    /// <summary>
    /// Gets the X position of the viewport.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y position of the viewport.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Gets the width of the viewport.
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// Gets the height of the viewport.
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    /// Gets the aspect ratio (width / height).
    /// </summary>
    public float AspectRatio => Height > 0 ? Width / Height : 1f;

    /// <summary>
    /// Creates a new viewport bounds instance.
    /// </summary>
    public ViewportBounds(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// Interface for viewport overlays that render 2D content on top of the viewport.
/// </summary>
public interface IViewportOverlay
{
    /// <summary>
    /// Gets whether this overlay is currently visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets the render order. Lower values render first (behind).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Called to render the overlay.
    /// </summary>
    /// <param name="context">The overlay render context.</param>
    void Render(OverlayRenderContext context);
}

/// <summary>
/// Context provided to overlays during rendering.
/// </summary>
public sealed class OverlayRenderContext
{
    /// <summary>
    /// Gets the editor world for creating UI entities.
    /// </summary>
    public required IWorld EditorWorld { get; init; }

    /// <summary>
    /// Gets the scene world being rendered.
    /// </summary>
    public IWorld? SceneWorld { get; init; }

    /// <summary>
    /// Gets the viewport bounds in screen coordinates.
    /// </summary>
    public required ViewportBounds Bounds { get; init; }

    /// <summary>
    /// Gets the delta time since last frame.
    /// </summary>
    public float DeltaTime { get; init; }
}

/// <summary>
/// Interface for custom pick handlers that can intercept entity picking.
/// </summary>
public interface IPickHandler
{
    /// <summary>
    /// Gets the priority of this handler. Higher values are checked first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to pick an entity at the given screen position.
    /// </summary>
    /// <param name="context">The pick context.</param>
    /// <returns>The pick result, or null if this handler didn't pick anything.</returns>
    PickResult? TryPick(PickContext context);
}

/// <summary>
/// Context provided to pick handlers during picking.
/// </summary>
public sealed class PickContext
{
    /// <summary>
    /// Gets the scene world being picked from.
    /// </summary>
    public required IWorld SceneWorld { get; init; }

    /// <summary>
    /// Gets the normalized screen X coordinate (0-1).
    /// </summary>
    public required float NormalizedX { get; init; }

    /// <summary>
    /// Gets the normalized screen Y coordinate (0-1).
    /// </summary>
    public required float NormalizedY { get; init; }

    /// <summary>
    /// Gets the ray origin (camera position).
    /// </summary>
    public required Vector3 RayOrigin { get; init; }

    /// <summary>
    /// Gets the ray direction.
    /// </summary>
    public required Vector3 RayDirection { get; init; }

    /// <summary>
    /// Gets the view matrix.
    /// </summary>
    public required Matrix4x4 ViewMatrix { get; init; }

    /// <summary>
    /// Gets the projection matrix.
    /// </summary>
    public required Matrix4x4 ProjectionMatrix { get; init; }

    /// <summary>
    /// Gets the viewport bounds.
    /// </summary>
    public required ViewportBounds Bounds { get; init; }
}

/// <summary>
/// Result of a pick operation.
/// </summary>
public sealed class PickResult
{
    /// <summary>
    /// Gets the picked entity.
    /// </summary>
    public required Entity Entity { get; init; }

    /// <summary>
    /// Gets the world position of the pick point.
    /// </summary>
    public Vector3 WorldPosition { get; init; }

    /// <summary>
    /// Gets the distance from the camera to the pick point.
    /// </summary>
    public float Distance { get; init; }

    /// <summary>
    /// Gets optional additional data about the pick.
    /// </summary>
    public object? UserData { get; init; }
}
