// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tools;

/// <summary>
/// Tool for rotating selected entities in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// The rotate tool displays rotation gizmo circles for the X, Y, and Z axes.
/// Users can click and drag on a circle to rotate selected entities around that axis.
/// The gizmo also displays a screen-aligned circle for view-relative rotation.
/// </para>
/// </remarks>
internal sealed class RotateTool : EditorToolBase
{
    private bool isDragging;
    private TransformAxis activeAxis;
    private float dragStartAngle;
    private Dictionary<Entity, Quaternion>? originalRotations;
    private Vector3 gizmoCenter;
    private const int CircleSegments = 64;
    private const float CircleHitWidth = 0.05f;

    /// <inheritdoc />
    public override string DisplayName => "Rotate";

    /// <inheritdoc />
    public override string? Icon => "rotate";

    /// <inheritdoc />
    public override string Category => ToolCategories.Transform;

    /// <inheritdoc />
    public override string? Tooltip => "Rotate selected entities (E)";

    /// <inheritdoc />
    public override string? Shortcut => "E";

    /// <inheritdoc />
    public override void OnActivate(ToolContext context)
    {
        isDragging = false;
        activeAxis = TransformAxis.None;
        originalRotations = null;
    }

    /// <inheritdoc />
    public override void OnDeactivate(ToolContext context)
    {
        // Cancel any in-progress drag
        if (isDragging && originalRotations is not null)
        {
            RestoreOriginalRotations(context);
        }

        isDragging = false;
        activeAxis = TransformAxis.None;
        originalRotations = null;
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

        // Store original rotations for undo
        originalRotations = [];
        foreach (var entity in context.SelectedEntities)
        {
            if (context.SceneWorld?.Has<Transform3D>(entity) == true)
            {
                var transform = context.SceneWorld.Get<Transform3D>(entity);
                originalRotations[entity] = transform.Rotation;
            }
        }

        // Calculate initial angle
        dragStartAngle = GetAngleOnAxis(context, position, gizmoCenter, activeAxis);
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

        // Finalize the rotation by recording an undo operation
        if (originalRotations is not null && originalRotations.Count > 0)
        {
            // Get final rotations
            var finalRotations = new Dictionary<Entity, Quaternion>();
            foreach (var entity in originalRotations.Keys)
            {
                if (context.SceneWorld?.Has<Transform3D>(entity) == true)
                {
                    var transform = context.SceneWorld.Get<Transform3D>(entity);
                    finalRotations[entity] = transform.Rotation;
                }
            }

            // Record undo operation
            var sceneWorld = context.SceneWorld;
            var original = originalRotations;
            context.EditorContext.UndoRedo.Execute(
                new DelegateCommand(
                    "Rotate Entities",
                    () => ApplyRotations(sceneWorld, finalRotations),
                    () => ApplyRotations(sceneWorld, original)));
        }

        originalRotations = null;
        activeAxis = TransformAxis.None;

        return true;
    }

    /// <inheritdoc />
    public override bool OnMouseMove(ToolContext context, Vector2 position, Vector2 delta)
    {
        if (!isDragging || originalRotations is null || activeAxis == TransformAxis.None)
        {
            return false;
        }

        var currentAngle = GetAngleOnAxis(context, position, gizmoCenter, activeAxis);
        var deltaAngle = currentAngle - dragStartAngle;

        // Get axis direction
        var axisDir = activeAxis switch
        {
            TransformAxis.X => Vector3.UnitX,
            TransformAxis.Y => Vector3.UnitY,
            TransformAxis.Z => Vector3.UnitZ,
            _ => Vector3.Zero
        };

        // Create rotation quaternion
        var rotation = Quaternion.CreateFromAxisAngle(axisDir, deltaAngle);

        // Apply rotation to all selected entities
        foreach (var (entity, originalRot) in originalRotations)
        {
            if (context.SceneWorld?.Has<Transform3D>(entity) == true)
            {
                ref var transform = ref context.SceneWorld.Get<Transform3D>(entity);

                // Rotate around gizmo center
                transform.Rotation = rotation * originalRot;

                // Also rotate position around gizmo center if multiple entities
                if (originalRotations.Count > 1)
                {
                    var originalPos = transform.Position;
                    var toEntity = originalPos - gizmoCenter;
                    var rotatedPos = Vector3.Transform(toEntity, rotation);
                    transform.Position = gizmoCenter + rotatedPos;
                }
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
        var radius = distanceToCamera * 0.1f;

        // Draw rotation circles for each axis
        DrawRotationCircle(context, center, radius, Vector3.UnitX, new Vector4(1, 0, 0, 1), activeAxis == TransformAxis.X);
        DrawRotationCircle(context, center, radius, Vector3.UnitY, new Vector4(0, 1, 0, 1), activeAxis == TransformAxis.Y);
        DrawRotationCircle(context, center, radius, Vector3.UnitZ, new Vector4(0, 0, 1, 1), activeAxis == TransformAxis.Z);
    }

    private static void DrawRotationCircle(GizmoRenderContext context, Vector3 center, float radius, Vector3 axis, Vector4 color, bool highlighted)
    {
        var lineColor = highlighted ? new Vector4(1, 1, 0, 1) : color;
        var lineWidth = highlighted ? 3f : 2f;

        // Find perpendicular vectors
        var perpX = Math.Abs(axis.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
        var u = Vector3.Normalize(Vector3.Cross(axis, perpX));
        var v = Vector3.Normalize(Vector3.Cross(axis, u));

        // Draw circle as line segments
        var prevPoint = center + u * radius;
        for (int i = 1; i <= CircleSegments; i++)
        {
            var angle = i * MathF.PI * 2f / CircleSegments;
            var currentPoint = center + (MathF.Cos(angle) * u + MathF.Sin(angle) * v) * radius;
            context.DrawLine(prevPoint, currentPoint, lineColor, lineWidth);
            prevPoint = currentPoint;
        }
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
        var radius = distanceToCamera * 0.1f;
        var hitWidth = CircleHitWidth * distanceToCamera;

        var xDist = DistanceToCircle(ray, center, radius, Vector3.UnitX);
        var yDist = DistanceToCircle(ray, center, radius, Vector3.UnitY);
        var zDist = DistanceToCircle(ray, center, radius, Vector3.UnitZ);

        var minDist = float.MaxValue;
        var axis = TransformAxis.None;

        if (xDist < hitWidth && xDist < minDist)
        {
            minDist = xDist;
            axis = TransformAxis.X;
        }

        if (yDist < hitWidth && yDist < minDist)
        {
            minDist = yDist;
            axis = TransformAxis.Y;
        }

        if (zDist < hitWidth && zDist < minDist)
        {
            axis = TransformAxis.Z;
        }

        return axis;
    }

    private static float DistanceToCircle((Vector3 Origin, Vector3 Direction) ray, Vector3 center, float radius, Vector3 axis)
    {
        // Intersect ray with plane defined by the circle
        var denom = Vector3.Dot(ray.Direction, axis);
        if (Math.Abs(denom) < 1e-6f)
        {
            return float.MaxValue;
        }

        var t = Vector3.Dot(center - ray.Origin, axis) / denom;
        if (t < 0)
        {
            return float.MaxValue;
        }

        var pointOnPlane = ray.Origin + ray.Direction * t;
        var distFromCenter = Vector3.Distance(pointOnPlane, center);

        // Return distance to the circle edge
        return Math.Abs(distFromCenter - radius);
    }

    private static float GetAngleOnAxis(ToolContext context, Vector2 position, Vector3 center, TransformAxis axis)
    {
        var ray = CreateRay(context, position);
        var axisDir = axis switch
        {
            TransformAxis.X => Vector3.UnitX,
            TransformAxis.Y => Vector3.UnitY,
            TransformAxis.Z => Vector3.UnitZ,
            _ => Vector3.Zero
        };

        // Intersect ray with plane
        var denom = Vector3.Dot(ray.Direction, axisDir);
        if (Math.Abs(denom) < 1e-6f)
        {
            return 0;
        }

        var t = Vector3.Dot(center - ray.Origin, axisDir) / denom;
        var pointOnPlane = ray.Origin + ray.Direction * t;

        // Find perpendicular vectors for the plane
        var perpX = Math.Abs(axisDir.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
        var u = Vector3.Normalize(Vector3.Cross(axisDir, perpX));
        var v = Vector3.Normalize(Vector3.Cross(axisDir, u));

        // Get angle
        var toPoint = pointOnPlane - center;
        var x = Vector3.Dot(toPoint, u);
        var y = Vector3.Dot(toPoint, v);

        return MathF.Atan2(y, x);
    }

    private void RestoreOriginalRotations(ToolContext context)
    {
        if (originalRotations is null || context.SceneWorld is null)
        {
            return;
        }

        foreach (var (entity, rotation) in originalRotations)
        {
            if (context.SceneWorld.Has<Transform3D>(entity))
            {
                ref var transform = ref context.SceneWorld.Get<Transform3D>(entity);
                transform.Rotation = rotation;
            }
        }
    }

    private static void ApplyRotations(IWorld? world, Dictionary<Entity, Quaternion> rotations)
    {
        if (world is null)
        {
            return;
        }

        foreach (var (entity, rotation) in rotations)
        {
            if (world.Has<Transform3D>(entity))
            {
                ref var transform = ref world.Get<Transform3D>(entity);
                transform.Rotation = rotation;
            }
        }
    }
}
