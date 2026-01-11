// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tools;

/// <summary>
/// Tool for scaling selected entities in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// The scale tool displays scaling gizmo cubes for the X, Y, and Z axes.
/// Users can click and drag on an axis cube to scale selected entities along that axis,
/// or drag on the center cube for uniform scaling.
/// </para>
/// </remarks>
internal sealed class ScaleTool : EditorToolBase
{
    private bool isDragging;
    private TransformAxis activeAxis;
    private float dragStartValue;
    private Vector3 gizmoCenter;
    private Dictionary<Entity, Vector3>? originalScales;
    private const float HandleSize = 0.08f;
    private const float AxisHitRadius = 0.1f;

    /// <inheritdoc />
    public override string DisplayName => "Scale";

    /// <inheritdoc />
    public override string? Icon => "scale";

    /// <inheritdoc />
    public override string Category => ToolCategories.Transform;

    /// <inheritdoc />
    public override string? Tooltip => "Scale selected entities (R)";

    /// <inheritdoc />
    public override string? Shortcut => "R";

    /// <inheritdoc />
    public override void OnActivate(ToolContext context)
    {
        isDragging = false;
        activeAxis = TransformAxis.None;
        originalScales = null;
    }

    /// <inheritdoc />
    public override void OnDeactivate(ToolContext context)
    {
        // Cancel any in-progress drag
        if (isDragging && originalScales is not null)
        {
            RestoreOriginalScales(context);
        }

        isDragging = false;
        activeAxis = TransformAxis.None;
        originalScales = null;
    }

    /// <inheritdoc />
    public override bool OnMouseDown(ToolContext context, MouseButton button, Vector2 position)
    {
        if (button != MouseButton.Left || context.SelectedEntities.Count == 0)
        {
            return false;
        }

        // Calculate gizmo center
        gizmoCenter = GetGizmoCenter(context);

        // Test which axis was clicked
        var ray = CreateRay(context, position);
        activeAxis = HitTestGizmo(gizmoCenter, ray, context);

        if (activeAxis == TransformAxis.None)
        {
            return false;
        }

        // Store original scales for undo
        originalScales = [];
        foreach (var entity in context.SelectedEntities)
        {
            if (context.SceneWorld?.Has<Transform3D>(entity) == true)
            {
                var transform = context.SceneWorld.Get<Transform3D>(entity);
                originalScales[entity] = transform.Scale;
            }
        }

        // Calculate initial drag value
        dragStartValue = GetDragValue(position);
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

        // Finalize the scale by recording an undo operation
        if (originalScales is not null && originalScales.Count > 0)
        {
            // Get final scales
            var finalScales = new Dictionary<Entity, Vector3>();
            foreach (var entity in originalScales.Keys)
            {
                if (context.SceneWorld?.Has<Transform3D>(entity) == true)
                {
                    var transform = context.SceneWorld.Get<Transform3D>(entity);
                    finalScales[entity] = transform.Scale;
                }
            }

            // Record undo operation
            var sceneWorld = context.SceneWorld;
            var original = originalScales;
            context.EditorContext.UndoRedo.Execute(
                new DelegateCommand(
                    "Scale Entities",
                    () => ApplyScales(sceneWorld, finalScales),
                    () => ApplyScales(sceneWorld, original)));
        }

        originalScales = null;
        activeAxis = TransformAxis.None;

        return true;
    }

    /// <inheritdoc />
    public override bool OnMouseMove(ToolContext context, Vector2 position, Vector2 delta)
    {
        if (!isDragging || originalScales is null || activeAxis == TransformAxis.None)
        {
            return false;
        }

        var currentValue = GetDragValue(position);
        var scaleFactor = 1.0f + (currentValue - dragStartValue);

        // Clamp scale factor to prevent negative or zero scale
        scaleFactor = Math.Max(0.01f, scaleFactor);

        // Calculate scale multiplier based on axis
        var scaleMultiplier = activeAxis switch
        {
            TransformAxis.X => new Vector3(scaleFactor, 1f, 1f),
            TransformAxis.Y => new Vector3(1f, scaleFactor, 1f),
            TransformAxis.Z => new Vector3(1f, 1f, scaleFactor),
            TransformAxis.All => new Vector3(scaleFactor, scaleFactor, scaleFactor),
            _ => Vector3.One
        };

        // Apply scale to all selected entities
        foreach (var (entity, originalScale) in originalScales)
        {
            if (context.SceneWorld?.Has<Transform3D>(entity) == true)
            {
                ref var transform = ref context.SceneWorld.Get<Transform3D>(entity);
                transform.Scale = originalScale * scaleMultiplier;
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
        var handleScale = distanceToCamera * HandleSize;

        // Draw axis lines and scale handles
        DrawScaleAxis(context, center, Vector3.UnitX * scale, handleScale, new Vector4(1, 0, 0, 1), activeAxis == TransformAxis.X);
        DrawScaleAxis(context, center, Vector3.UnitY * scale, handleScale, new Vector4(0, 1, 0, 1), activeAxis == TransformAxis.Y);
        DrawScaleAxis(context, center, Vector3.UnitZ * scale, handleScale, new Vector4(0, 0, 1, 1), activeAxis == TransformAxis.Z);

        // Draw center cube for uniform scaling
        DrawCenterCube(context, center, handleScale, new Vector4(1, 1, 1, 1), activeAxis == TransformAxis.All);
    }

    private static void DrawScaleAxis(GizmoRenderContext context, Vector3 origin, Vector3 direction, float handleSize, Vector4 color, bool highlighted)
    {
        var endPoint = origin + direction;
        var lineColor = highlighted ? new Vector4(1, 1, 0, 1) : color;
        var lineWidth = highlighted ? 3f : 2f;

        // Draw axis line
        context.DrawLine(origin, endPoint, lineColor, lineWidth);

        // Draw cube handle at the end using Drawer
        var halfSize = handleSize * 0.5f;
        var cubeMin = endPoint - new Vector3(halfSize);
        var cubeMax = endPoint + new Vector3(halfSize);
        context.Drawer.DrawWireBox(cubeMin, cubeMax, lineColor, lineWidth);
    }

    private static void DrawCenterCube(GizmoRenderContext context, Vector3 center, float size, Vector4 color, bool highlighted)
    {
        var lineColor = highlighted ? new Vector4(1, 1, 0, 1) : color;
        var lineWidth = highlighted ? 3f : 2f;
        var halfSize = size * 0.5f;

        var cubeMin = center - new Vector3(halfSize);
        var cubeMax = center + new Vector3(halfSize);
        context.Drawer.DrawWireBox(cubeMin, cubeMax, lineColor, lineWidth);
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

    private static TransformAxis HitTestGizmo(Vector3 center, (Vector3 Origin, Vector3 Direction) ray, ToolContext context)
    {
        var distanceToCamera = Vector3.Distance(center, context.CameraPosition);
        var scale = distanceToCamera * 0.1f;
        var handleSize = distanceToCamera * HandleSize;
        var hitRadius = AxisHitRadius * distanceToCamera;

        // Test center cube first (uniform scale)
        var centerDist = DistanceToCube(ray, center, handleSize);
        if (centerDist < hitRadius)
        {
            return TransformAxis.All;
        }

        // Test each axis handle
        var xEnd = center + Vector3.UnitX * scale;
        var yEnd = center + Vector3.UnitY * scale;
        var zEnd = center + Vector3.UnitZ * scale;

        var xDist = DistanceToCube(ray, xEnd, handleSize);
        var yDist = DistanceToCube(ray, yEnd, handleSize);
        var zDist = DistanceToCube(ray, zEnd, handleSize);

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

        // If no handle hit, check axis lines
        if (axis == TransformAxis.None)
        {
            xDist = DistanceToLine(ray.Origin, ray.Direction, center, xEnd);
            yDist = DistanceToLine(ray.Origin, ray.Direction, center, yEnd);
            zDist = DistanceToLine(ray.Origin, ray.Direction, center, zEnd);

            var lineHitRadius = hitRadius * 0.5f;

            if (xDist < lineHitRadius && xDist < minDist)
            {
                minDist = xDist;
                axis = TransformAxis.X;
            }

            if (yDist < lineHitRadius && yDist < minDist)
            {
                minDist = yDist;
                axis = TransformAxis.Y;
            }

            if (zDist < lineHitRadius && zDist < minDist)
            {
                axis = TransformAxis.Z;
            }
        }

        return axis;
    }

    private static float DistanceToCube((Vector3 Origin, Vector3 Direction) ray, Vector3 center, float size)
    {
        var halfSize = size * 0.5f;
        var min = center - new Vector3(halfSize);
        var max = center + new Vector3(halfSize);

        // Ray-box intersection
        var t1 = (min.X - ray.Origin.X) / ray.Direction.X;
        var t2 = (max.X - ray.Origin.X) / ray.Direction.X;
        var t3 = (min.Y - ray.Origin.Y) / ray.Direction.Y;
        var t4 = (max.Y - ray.Origin.Y) / ray.Direction.Y;
        var t5 = (min.Z - ray.Origin.Z) / ray.Direction.Z;
        var t6 = (max.Z - ray.Origin.Z) / ray.Direction.Z;

        var tMin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
        var tMax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

        if (tMax < 0 || tMin > tMax)
        {
            // No intersection - return distance to center
            return Vector3.Distance(ray.Origin, center);
        }

        return 0; // Hit
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

    private static float GetDragValue(Vector2 position)
    {
        // Use screen-space Y coordinate for scaling (drag up = larger, drag down = smaller)
        return -(position.Y - 0.5f) * 2f;
    }

    private void RestoreOriginalScales(ToolContext context)
    {
        if (originalScales is null || context.SceneWorld is null)
        {
            return;
        }

        foreach (var (entity, scale) in originalScales)
        {
            if (context.SceneWorld.Has<Transform3D>(entity))
            {
                ref var transform = ref context.SceneWorld.Get<Transform3D>(entity);
                transform.Scale = scale;
            }
        }
    }

    private static void ApplyScales(IWorld? world, Dictionary<Entity, Vector3> scales)
    {
        if (world is null)
        {
            return;
        }

        foreach (var (entity, scale) in scales)
        {
            if (world.Has<Transform3D>(entity))
            {
                ref var transform = ref world.Get<Transform3D>(entity);
                transform.Scale = scale;
            }
        }
    }
}
