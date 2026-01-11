// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tools;

/// <summary>
/// Tool for moving selected entities in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// The move tool displays translation gizmo arrows for the X, Y, and Z axes.
/// Users can click and drag on an axis arrow to move selected entities along that axis,
/// or drag on a plane handle to move along two axes simultaneously.
/// </para>
/// </remarks>
internal sealed class MoveTool : EditorToolBase
{
    private bool isDragging;
    private TransformAxis activeAxis;
    private Vector3 dragStartWorldPos;
    private Dictionary<Entity, Vector3>? originalPositions;
    private const float AxisHitRadius = 0.1f;

    /// <inheritdoc />
    public override string DisplayName => "Move";

    /// <inheritdoc />
    public override string? Icon => "move";

    /// <inheritdoc />
    public override string Category => ToolCategories.Transform;

    /// <inheritdoc />
    public override string? Tooltip => "Move selected entities (W)";

    /// <inheritdoc />
    public override string? Shortcut => "W";

    /// <inheritdoc />
    public override void OnActivate(ToolContext context)
    {
        isDragging = false;
        activeAxis = TransformAxis.None;
        originalPositions = null;
    }

    /// <inheritdoc />
    public override void OnDeactivate(ToolContext context)
    {
        // Cancel any in-progress drag
        if (isDragging && originalPositions is not null)
        {
            RestoreOriginalPositions(context);
        }

        isDragging = false;
        activeAxis = TransformAxis.None;
        originalPositions = null;
    }

    /// <inheritdoc />
    public override bool OnMouseDown(ToolContext context, MouseButton button, Vector2 position)
    {
        if (button != MouseButton.Left || context.SelectedEntities.Count == 0)
        {
            return false;
        }

        // Calculate gizmo center (average of selected entity positions)
        var gizmoCenter = GetGizmoCenter(context);

        // Test which axis was clicked
        var ray = CreateRay(context, position);
        activeAxis = HitTestGizmo(gizmoCenter, ray, context);

        if (activeAxis == TransformAxis.None)
        {
            return false;
        }

        // Store original positions for undo
        originalPositions = [];
        foreach (var entity in context.SelectedEntities)
        {
            if (context.SceneWorld?.Has<Transform3D>(entity) == true)
            {
                var transform = context.SceneWorld.Get<Transform3D>(entity);
                originalPositions[entity] = transform.Position;
            }
        }

        // Calculate drag start position in world space
        dragStartWorldPos = ProjectOntoAxis(gizmoCenter, ray, activeAxis);
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

        // Finalize the move by recording an undo operation
        if (originalPositions is not null && originalPositions.Count > 0)
        {
            // Get final positions
            var finalPositions = new Dictionary<Entity, Vector3>();
            foreach (var entity in originalPositions.Keys)
            {
                if (context.SceneWorld?.Has<Transform3D>(entity) == true)
                {
                    var transform = context.SceneWorld.Get<Transform3D>(entity);
                    finalPositions[entity] = transform.Position;
                }
            }

            // Record undo operation
            var sceneWorld = context.SceneWorld;
            var original = originalPositions;
            context.EditorContext.UndoRedo.Execute(
                new DelegateCommand(
                    "Move Entities",
                    () => ApplyPositions(sceneWorld, finalPositions),
                    () => ApplyPositions(sceneWorld, original)));
        }

        originalPositions = null;
        activeAxis = TransformAxis.None;

        return true;
    }

    /// <inheritdoc />
    public override bool OnMouseMove(ToolContext context, Vector2 position, Vector2 delta)
    {
        if (!isDragging || originalPositions is null || activeAxis == TransformAxis.None)
        {
            return false;
        }

        var gizmoCenter = GetGizmoCenter(context, originalPositions);
        var ray = CreateRay(context, position);
        var currentWorldPos = ProjectOntoAxis(gizmoCenter, ray, activeAxis);

        // Calculate movement delta
        var moveDelta = currentWorldPos - dragStartWorldPos;

        // Apply movement to all selected entities
        foreach (var (entity, originalPos) in originalPositions)
        {
            if (context.SceneWorld?.Has<Transform3D>(entity) == true)
            {
                ref var transform = ref context.SceneWorld.Get<Transform3D>(entity);
                transform.Position = originalPos + moveDelta;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override void OnRender(GizmoRenderContext context)
    {
        if (context.SelectedEntities.Count == 0)
        {
            return;
        }

        // Calculate gizmo center
        var center = Vector3.Zero;
        var count = 0;
        foreach (var entity in context.SelectedEntities)
        {
            if (context.SceneWorld.Has<Transform3D>(entity))
            {
                var transform = context.SceneWorld.Get<Transform3D>(entity);
                center += transform.Position;
                count++;
            }
        }

        if (count == 0)
        {
            return;
        }

        center /= count;

        // Calculate gizmo scale based on distance from camera
        var distanceToCamera = Vector3.Distance(center, context.CameraPosition);
        var scale = distanceToCamera * 0.1f;

        // Draw axis arrows
        DrawAxisArrow(context, center, Vector3.UnitX * scale, new Vector4(1, 0, 0, 1), activeAxis == TransformAxis.X);
        DrawAxisArrow(context, center, Vector3.UnitY * scale, new Vector4(0, 1, 0, 1), activeAxis == TransformAxis.Y);
        DrawAxisArrow(context, center, Vector3.UnitZ * scale, new Vector4(0, 0, 1, 1), activeAxis == TransformAxis.Z);
    }

    private static void DrawAxisArrow(GizmoRenderContext context, Vector3 origin, Vector3 direction, Vector4 color, bool highlighted)
    {
        var endPoint = origin + direction;
        var lineColor = highlighted ? new Vector4(1, 1, 0, 1) : color;
        var lineWidth = highlighted ? 3f : 2f;

        context.DrawLine(origin, endPoint, lineColor, lineWidth);

        // Draw arrowhead
        var arrowSize = direction.Length() * 0.15f;
        var arrowDir = Vector3.Normalize(direction);

        // Find perpendicular vectors for arrowhead
        var perpX = Math.Abs(arrowDir.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
        var perp1 = Vector3.Normalize(Vector3.Cross(arrowDir, perpX)) * arrowSize * 0.3f;
        var perp2 = Vector3.Normalize(Vector3.Cross(arrowDir, perp1)) * arrowSize * 0.3f;
        var arrowBase = endPoint - arrowDir * arrowSize;

        context.DrawLine(endPoint, arrowBase + perp1, lineColor, lineWidth);
        context.DrawLine(endPoint, arrowBase - perp1, lineColor, lineWidth);
        context.DrawLine(endPoint, arrowBase + perp2, lineColor, lineWidth);
        context.DrawLine(endPoint, arrowBase - perp2, lineColor, lineWidth);
    }

    private static Vector3 GetGizmoCenter(ToolContext context)
    {
        var center = Vector3.Zero;
        var count = 0;
        foreach (var entity in context.SelectedEntities)
        {
            if (context.SceneWorld?.Has<Transform3D>(entity) == true)
            {
                var transform = context.SceneWorld.Get<Transform3D>(entity);
                center += transform.Position;
                count++;
            }
        }

        return count > 0 ? center / count : Vector3.Zero;
    }

    private static Vector3 GetGizmoCenter(ToolContext context, Dictionary<Entity, Vector3> positions)
    {
        var center = Vector3.Zero;
        var count = 0;
        foreach (var pos in positions.Values)
        {
            center += pos;
            count++;
        }

        return count > 0 ? center / count : Vector3.Zero;
    }

    private static (Vector3 Origin, Vector3 Direction) CreateRay(ToolContext context, Vector2 normalizedPosition)
    {
        Matrix4x4.Invert(context.ViewMatrix * context.ProjectionMatrix, out var invViewProj);

        var clipX = normalizedPosition.X * 2f - 1f;
        var clipY = 1f - normalizedPosition.Y * 2f;

        var nearPoint = Vector4.Transform(new Vector4(clipX, clipY, 0f, 1f), invViewProj);
        var farPoint = Vector4.Transform(new Vector4(clipX, clipY, 1f, 1f), invViewProj);

        var near3D = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z) / nearPoint.W;
        var far3D = new Vector3(farPoint.X, farPoint.Y, farPoint.Z) / farPoint.W;

        return (near3D, Vector3.Normalize(far3D - near3D));
    }

    private static TransformAxis HitTestGizmo(Vector3 gizmoCenter, (Vector3 Origin, Vector3 Direction) ray, ToolContext context)
    {
        var distanceToCamera = Vector3.Distance(gizmoCenter, context.CameraPosition);
        var scale = distanceToCamera * 0.1f;
        var hitRadius = AxisHitRadius * scale;

        // Test each axis
        var xDist = DistanceToLine(ray.Origin, ray.Direction, gizmoCenter, gizmoCenter + Vector3.UnitX * scale);
        var yDist = DistanceToLine(ray.Origin, ray.Direction, gizmoCenter, gizmoCenter + Vector3.UnitY * scale);
        var zDist = DistanceToLine(ray.Origin, ray.Direction, gizmoCenter, gizmoCenter + Vector3.UnitZ * scale);

        var minDist = float.MaxValue;
        var axis = TransformAxis.None;

        if (xDist < hitRadius && xDist < minDist)
        {
            minDist = xDist;
            axis = TransformAxis.X;
        }

        if (yDist < hitRadius && yDist < minDist)
        {
            minDist = yDist;
            axis = TransformAxis.Y;
        }

        if (zDist < hitRadius && zDist < minDist)
        {
            axis = TransformAxis.Z;
        }

        return axis;
    }

    private static float DistanceToLine(Vector3 rayOrigin, Vector3 rayDir, Vector3 lineStart, Vector3 lineEnd)
    {
        var u = lineEnd - lineStart;
        var v = rayDir;
        var w = lineStart - rayOrigin;

        var a = Vector3.Dot(u, u);
        var b = Vector3.Dot(u, v);
        var c = Vector3.Dot(v, v);
        var d = Vector3.Dot(u, w);
        var e = Vector3.Dot(v, w);

        var denom = a * c - b * b;
        if (Math.Abs(denom) < 1e-6f)
        {
            return float.MaxValue;
        }

        var sc = (b * e - c * d) / denom;
        sc = Math.Clamp(sc, 0f, 1f);

        var tc = (a * e - b * d) / denom;

        var closestOnLine = lineStart + sc * u;
        var closestOnRay = rayOrigin + tc * v;

        return Vector3.Distance(closestOnLine, closestOnRay);
    }

    private static Vector3 ProjectOntoAxis(Vector3 gizmoCenter, (Vector3 Origin, Vector3 Direction) ray, TransformAxis axis)
    {
        var axisDir = axis switch
        {
            TransformAxis.X => Vector3.UnitX,
            TransformAxis.Y => Vector3.UnitY,
            TransformAxis.Z => Vector3.UnitZ,
            _ => Vector3.Zero
        };

        // Project ray onto the axis
        var planeNormal = Vector3.Cross(axisDir, Vector3.Cross(ray.Direction, axisDir));
        if (planeNormal.LengthSquared() < 1e-6f)
        {
            planeNormal = Vector3.Cross(axisDir, Vector3.UnitX.LengthSquared() > 0.5f ? Vector3.UnitY : Vector3.UnitX);
        }

        planeNormal = Vector3.Normalize(planeNormal);

        // Ray-plane intersection
        var denom = Vector3.Dot(ray.Direction, planeNormal);
        if (Math.Abs(denom) < 1e-6f)
        {
            return gizmoCenter;
        }

        var t = Vector3.Dot(gizmoCenter - ray.Origin, planeNormal) / denom;
        var pointOnPlane = ray.Origin + ray.Direction * t;

        // Project onto axis line
        var toPoint = pointOnPlane - gizmoCenter;
        var projectedDistance = Vector3.Dot(toPoint, axisDir);

        return gizmoCenter + axisDir * projectedDistance;
    }

    private void RestoreOriginalPositions(ToolContext context)
    {
        if (originalPositions is null || context.SceneWorld is null)
        {
            return;
        }

        foreach (var (entity, position) in originalPositions)
        {
            if (context.SceneWorld.Has<Transform3D>(entity))
            {
                ref var transform = ref context.SceneWorld.Get<Transform3D>(entity);
                transform.Position = position;
            }
        }
    }

    private static void ApplyPositions(IWorld? world, Dictionary<Entity, Vector3> positions)
    {
        if (world is null)
        {
            return;
        }

        foreach (var (entity, position) in positions)
        {
            if (world.Has<Transform3D>(entity))
            {
                ref var transform = ref world.Get<Transform3D>(entity);
                transform.Position = position;
            }
        }
    }
}

/// <summary>
/// Represents the axis being manipulated by a transform tool.
/// </summary>
internal enum TransformAxis
{
    /// <summary>
    /// No axis selected.
    /// </summary>
    None,

    /// <summary>
    /// X axis (red).
    /// </summary>
    X,

    /// <summary>
    /// Y axis (green).
    /// </summary>
    Y,

    /// <summary>
    /// Z axis (blue).
    /// </summary>
    Z,

    /// <summary>
    /// XY plane.
    /// </summary>
    XY,

    /// <summary>
    /// XZ plane.
    /// </summary>
    XZ,

    /// <summary>
    /// YZ plane.
    /// </summary>
    YZ,

    /// <summary>
    /// All axes (uniform).
    /// </summary>
    All
}
