// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using System.Runtime.InteropServices;

using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Resources;

namespace KeenEyes.Editor.Viewport;

/// <summary>
/// Draws gizmo primitives into the viewport using the solid shader.
/// </summary>
/// <remarks>
/// <para>
/// Lines and points are approximated with stretched cubes - the same technique
/// <see cref="TransformGizmo"/> uses - because the graphics context has no
/// immediate-mode line API. Pixel sizes are converted to world units from the
/// camera distance and the projection's vertical field of view.
/// </para>
/// <para>
/// Call <see cref="Begin"/> once per frame before any renderer draws; it binds the
/// solid shader and captures the camera state used for pixel-to-world conversion.
/// </para>
/// </remarks>
internal sealed class ViewportGizmoDrawer : IGizmoDrawer
{
    private const float FallbackWorldUnitsPerPixel = 0.01f;
    private const float MinCameraDistance = 0.01f;

    private IGraphicsContext? graphics;
    private MeshHandle cubeMesh;
    private MeshHandle triangleMesh;
    private bool meshesCreated;

    private Vector3 cameraPosition;
    private float projectionScaleY;
    private float viewportHeight;

    /// <summary>
    /// Prepares the drawer for a frame: creates meshes on first use, binds the solid
    /// shader, and uploads the view and projection matrices.
    /// </summary>
    internal void Begin(
        IGraphicsContext graphicsContext,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix,
        Vector3 camera,
        float viewportHeightPixels)
    {
        graphics = graphicsContext;
        cameraPosition = camera;
        projectionScaleY = projectionMatrix.M22;
        viewportHeight = viewportHeightPixels;

        EnsureMeshes(graphicsContext);

        graphicsContext.BindShader(graphicsContext.SolidShader);
        graphicsContext.SetUniform("uView", viewMatrix);
        graphicsContext.SetUniform("uProjection", projectionMatrix);
    }

    /// <inheritdoc />
    public void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector4 color)
    {
        if (graphics is null || !meshesCreated)
        {
            return;
        }

        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var normal = Vector3.Cross(edge1, edge2);
        if (normal.LengthSquared().IsApproximatelyZero())
        {
            return;
        }

        normal = Vector3.Normalize(normal);

        // Map the canonical unit triangle (origin, +X, +Y) onto (v0, v1, v2):
        // matrix rows are the target basis vectors, translation is v0.
        var model = new Matrix4x4(
            edge1.X, edge1.Y, edge1.Z, 0f,
            edge2.X, edge2.Y, edge2.Z, 0f,
            normal.X, normal.Y, normal.Z, 0f,
            v0.X, v0.Y, v0.Z, 1f);

        DrawMesh(triangleMesh, model, color);
    }

    /// <inheritdoc />
    public void DrawLine(Vector3 start, Vector3 end, Vector4 color, float width = 1.0f)
    {
        if (graphics is null || !meshesCreated)
        {
            return;
        }

        var direction = end - start;
        var length = direction.Length();
        if (length.IsApproximatelyZero())
        {
            return;
        }

        var center = (start + end) * 0.5f;
        var thickness = MathF.Max(width, 1f) * WorldUnitsPerPixel(center);

        var model =
            Matrix4x4.CreateScale(thickness, thickness, length) *
            Matrix4x4.CreateFromQuaternion(CreateRotationFromDirection(direction / length)) *
            Matrix4x4.CreateTranslation(center);

        DrawMesh(cubeMesh, model, color);
    }

    /// <inheritdoc />
    public void DrawPoint(Vector3 position, Vector4 color, float size = 4.0f)
    {
        if (graphics is null || !meshesCreated)
        {
            return;
        }

        var worldSize = MathF.Max(size, 1f) * WorldUnitsPerPixel(position);
        var model =
            Matrix4x4.CreateScale(worldSize) *
            Matrix4x4.CreateTranslation(position);

        DrawMesh(cubeMesh, model, color);
    }

    /// <inheritdoc />
    public void DrawWireBox(Vector3 min, Vector3 max, Vector4 color, float lineWidth = 1.0f)
    {
        var v000 = new Vector3(min.X, min.Y, min.Z);
        var v100 = new Vector3(max.X, min.Y, min.Z);
        var v010 = new Vector3(min.X, max.Y, min.Z);
        var v110 = new Vector3(max.X, max.Y, min.Z);
        var v001 = new Vector3(min.X, min.Y, max.Z);
        var v101 = new Vector3(max.X, min.Y, max.Z);
        var v011 = new Vector3(min.X, max.Y, max.Z);
        var v111 = new Vector3(max.X, max.Y, max.Z);

        // Bottom face
        DrawLine(v000, v100, color, lineWidth);
        DrawLine(v100, v101, color, lineWidth);
        DrawLine(v101, v001, color, lineWidth);
        DrawLine(v001, v000, color, lineWidth);

        // Top face
        DrawLine(v010, v110, color, lineWidth);
        DrawLine(v110, v111, color, lineWidth);
        DrawLine(v111, v011, color, lineWidth);
        DrawLine(v011, v010, color, lineWidth);

        // Vertical edges
        DrawLine(v000, v010, color, lineWidth);
        DrawLine(v100, v110, color, lineWidth);
        DrawLine(v101, v111, color, lineWidth);
        DrawLine(v001, v011, color, lineWidth);
    }

    /// <inheritdoc />
    public void DrawWireSphere(Vector3 center, float radius, Vector4 color, int segments = 16)
    {
        if (segments < 3 || radius.IsApproximatelyZero())
        {
            return;
        }

        DrawCircle(center, radius, Vector3.UnitX, Vector3.UnitY, color, segments);
        DrawCircle(center, radius, Vector3.UnitX, Vector3.UnitZ, color, segments);
        DrawCircle(center, radius, Vector3.UnitY, Vector3.UnitZ, color, segments);
    }

    /// <inheritdoc />
    public void DrawText(Vector3 position, string text, Vector4 color)
    {
        // The viewport has no world-space text rendering path yet.
    }

    private void DrawCircle(Vector3 center, float radius, Vector3 axisA, Vector3 axisB, Vector4 color, int segments)
    {
        var previous = center + axisA * radius;
        for (var i = 1; i <= segments; i++)
        {
            var angle = i * MathF.PI * 2f / segments;
            var point = center +
                axisA * (MathF.Cos(angle) * radius) +
                axisB * (MathF.Sin(angle) * radius);

            DrawLine(previous, point, color);
            previous = point;
        }
    }

    private void EnsureMeshes(IGraphicsContext graphicsContext)
    {
        if (meshesCreated || !graphicsContext.IsInitialized)
        {
            return;
        }

        cubeMesh = graphicsContext.CreateCube(1f);

        // Canonical unit triangle in the XY plane; DrawTriangle maps it onto arbitrary
        // vertices via the model matrix. Vertex matches the layout CreateMesh expects.
        Span<Vertex> vertices =
        [
            new(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
            new(Vector3.UnitX, Vector3.UnitZ, Vector2.Zero),
            new(Vector3.UnitY, Vector3.UnitZ, Vector2.Zero),
        ];

        triangleMesh = graphicsContext.CreateMesh(MemoryMarshal.AsBytes(vertices), 3, [0u, 1u, 2u]);
        meshesCreated = true;
    }

    private void DrawMesh(MeshHandle mesh, in Matrix4x4 model, Vector4 color)
    {
        graphics!.SetUniform("uModel", model);
        graphics.SetUniform("uColor", color);
        graphics.BindMesh(mesh);
        graphics.DrawMesh(mesh);
    }

    private float WorldUnitsPerPixel(Vector3 position)
    {
        if (viewportHeight <= 0f || projectionScaleY.IsApproximatelyZero())
        {
            return FallbackWorldUnitsPerPixel;
        }

        var distance = MathF.Max(Vector3.Distance(cameraPosition, position), MinCameraDistance);
        return 2f * distance / (projectionScaleY * viewportHeight);
    }

    private static Quaternion CreateRotationFromDirection(Vector3 direction)
    {
        var forward = direction;
        var up = Vector3.UnitY;

        if (MathF.Abs(Vector3.Dot(forward, up)) > 0.999f)
        {
            up = Vector3.UnitZ;
        }

        var right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Cross(forward, right);

        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);

        return Quaternion.CreateFromRotationMatrix(m);
    }
}
