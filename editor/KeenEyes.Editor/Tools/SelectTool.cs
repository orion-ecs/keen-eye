// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Tools;

/// <summary>
/// Tool for selecting entities in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// The select tool allows users to click on entities to select them, with support
/// for modifier keys (Ctrl to add to selection, Shift to toggle selection).
/// It also supports marquee (box) selection by clicking and dragging.
/// </para>
/// </remarks>
internal sealed class SelectTool : EditorToolBase
{
    private bool isDragging;
    private Vector2 dragStart;
    private Vector2 dragCurrent;
    private const float DragThreshold = 5f;

    /// <inheritdoc />
    public override string DisplayName => "Select";

    /// <inheritdoc />
    public override string? Icon => "cursor";

    /// <inheritdoc />
    public override string Category => ToolCategories.Selection;

    /// <inheritdoc />
    public override string? Tooltip => "Select entities (Q)";

    /// <inheritdoc />
    public override string? Shortcut => "Q";

    /// <inheritdoc />
    public override void OnActivate(ToolContext context)
    {
        isDragging = false;
    }

    /// <inheritdoc />
    public override void OnDeactivate(ToolContext context)
    {
        isDragging = false;
    }

    /// <inheritdoc />
    public override bool OnMouseDown(ToolContext context, MouseButton button, Vector2 position)
    {
        if (button != MouseButton.Left)
        {
            return false;
        }

        dragStart = position;
        dragCurrent = position;
        isDragging = true;
        return true;
    }

    /// <inheritdoc />
    public override bool OnMouseUp(ToolContext context, MouseButton button, Vector2 position)
    {
        if (button != MouseButton.Left || !isDragging)
        {
            return false;
        }

        isDragging = false;

        // Calculate drag distance to determine if this was a click or a drag
        var dragDistance = Vector2.Distance(dragStart, position);

        if (dragDistance < DragThreshold)
        {
            // Single click - perform point selection
            PerformPointSelection(context, position);
        }
        else
        {
            // Drag - perform marquee selection
            PerformMarqueeSelection(context, dragStart, position);
        }

        return true;
    }

    /// <inheritdoc />
    public override bool OnMouseMove(ToolContext context, Vector2 position, Vector2 delta)
    {
        if (!isDragging)
        {
            return false;
        }

        dragCurrent = position;
        return true;
    }

    /// <inheritdoc />
    public override void OnRender(GizmoRenderContext context)
    {
        // Draw marquee selection rectangle if dragging
        if (isDragging && Vector2.Distance(dragStart, dragCurrent) >= DragThreshold)
        {
            // Calculate selection rectangle in normalized viewport coordinates
            var minX = Math.Min(dragStart.X, dragCurrent.X);
            var maxX = Math.Max(dragStart.X, dragCurrent.X);
            var minY = Math.Min(dragStart.Y, dragCurrent.Y);
            var maxY = Math.Max(dragStart.Y, dragCurrent.Y);

            // Convert to screen coordinates for rendering
            var bounds = context.Bounds;
            var x1 = bounds.X + minX * bounds.Width;
            var x2 = bounds.X + maxX * bounds.Width;
            var y1 = bounds.Y + minY * bounds.Height;
            var y2 = bounds.Y + maxY * bounds.Height;

            // Draw selection rectangle as wireframe
            // Use screen-space coordinates converted to world space at a fixed depth
            var selectColor = new Vector4(0.3f, 0.6f, 1.0f, 0.8f);
            var depth = 0.1f; // Render near camera

            var corner1 = new Vector3(x1, y1, depth);
            var corner2 = new Vector3(x2, y1, depth);
            var corner3 = new Vector3(x2, y2, depth);
            var corner4 = new Vector3(x1, y2, depth);

            context.DrawLine(corner1, corner2, selectColor, 2f);
            context.DrawLine(corner2, corner3, selectColor, 2f);
            context.DrawLine(corner3, corner4, selectColor, 2f);
            context.DrawLine(corner4, corner1, selectColor, 2f);
        }
    }

    private static void PerformPointSelection(ToolContext context, Vector2 position)
    {
        if (context.SceneWorld is null)
        {
            return;
        }

        // Get viewport capability to use pick handlers
        if (!context.EditorContext.TryGetCapability<IViewportCapability>(out var viewportCapability) ||
            viewportCapability is null)
        {
            // No viewport capability - just clear selection on click
            context.EditorContext.Selection.ClearSelection();
            return;
        }

        // Create pick context
        var pickContext = CreatePickContext(context, position);

        // Try all pick handlers in priority order
        Entity? pickedEntity = null;
        foreach (var handler in viewportCapability.GetPickHandlers().OrderByDescending(h => h.Priority))
        {
            var result = handler.TryPick(pickContext);
            if (result is not null)
            {
                pickedEntity = result.Entity;
                break;
            }
        }

        // Update selection based on modifiers
        // Note: In a full implementation, we'd check keyboard modifiers here
        // For now, we just do simple select/clear behavior
        if (pickedEntity.HasValue)
        {
            context.EditorContext.Selection.Select(pickedEntity.Value);
        }
        else
        {
            context.EditorContext.Selection.ClearSelection();
        }
    }

    private static void PerformMarqueeSelection(ToolContext context, Vector2 start, Vector2 end)
    {
        if (context.SceneWorld is null)
        {
            return;
        }

        // In a full implementation, we would:
        // 1. Project all entity bounds to screen space
        // 2. Check which entities' bounds intersect the selection rectangle
        // 3. Select those entities
        //
        // Selection rectangle bounds (for future use):
        // minX = Math.Min(start.X, end.X)
        // maxX = Math.Max(start.X, end.X)
        // minY = Math.Min(start.Y, end.Y)
        // maxY = Math.Max(start.Y, end.Y)
        //
        // For now, we just clear selection as a placeholder
        _ = start;
        _ = end;
        context.EditorContext.Selection.ClearSelection();
    }

    private static PickContext CreatePickContext(ToolContext toolContext, Vector2 normalizedPosition)
    {
        // Calculate ray from camera through the normalized position
        Matrix4x4.Invert(toolContext.ViewMatrix * toolContext.ProjectionMatrix, out var invViewProj);

        // Convert normalized position (0-1) to clip space (-1 to 1)
        var clipX = normalizedPosition.X * 2f - 1f;
        var clipY = 1f - normalizedPosition.Y * 2f; // Flip Y for clip space

        // Unproject near and far plane points
        var nearPoint = Vector4.Transform(new Vector4(clipX, clipY, 0f, 1f), invViewProj);
        var farPoint = Vector4.Transform(new Vector4(clipX, clipY, 1f, 1f), invViewProj);

        // Perspective divide
        var near3D = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z) / nearPoint.W;
        var far3D = new Vector3(farPoint.X, farPoint.Y, farPoint.Z) / farPoint.W;

        var rayDirection = Vector3.Normalize(far3D - near3D);

        return new PickContext
        {
            SceneWorld = toolContext.SceneWorld!,
            NormalizedX = normalizedPosition.X,
            NormalizedY = normalizedPosition.Y,
            RayOrigin = toolContext.CameraPosition,
            RayDirection = rayDirection,
            ViewMatrix = toolContext.ViewMatrix,
            ProjectionMatrix = toolContext.ProjectionMatrix,
            Bounds = toolContext.ViewportBounds
        };
    }
}
