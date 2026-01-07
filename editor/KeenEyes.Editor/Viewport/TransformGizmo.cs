using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Editor.Application;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Viewport;

/// <summary>
/// The type of transform operation to perform.
/// </summary>
public enum GizmoMode
{
    /// <summary>
    /// Move the entity in world or local space.
    /// </summary>
    Translate,

    /// <summary>
    /// Rotate the entity around its axes.
    /// </summary>
    Rotate,

    /// <summary>
    /// Scale the entity uniformly or along individual axes.
    /// </summary>
    Scale
}

/// <summary>
/// The coordinate space for transform operations.
/// </summary>
public enum GizmoSpace
{
    /// <summary>
    /// Transform in world coordinates.
    /// </summary>
    World,

    /// <summary>
    /// Transform in local (entity) coordinates.
    /// </summary>
    Local
}

/// <summary>
/// Identifies which part of the gizmo is being interacted with.
/// </summary>
public enum GizmoAxis
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
    /// All axes (uniform scale or free rotation).
    /// </summary>
    All
}

/// <summary>
/// Provides transform manipulation gizmos for the editor viewport.
/// </summary>
/// <remarks>
/// <para>
/// The TransformGizmo renders visual handles for manipulating entity transforms:
/// <list type="bullet">
/// <item>Translation: Arrows along each axis</item>
/// <item>Rotation: Circles around each axis</item>
/// <item>Scale: Cubes at the end of each axis</item>
/// </list>
/// </para>
/// <para>
/// The gizmo automatically scales based on distance from the camera to maintain
/// a consistent screen size regardless of zoom level.
/// </para>
/// </remarks>
public sealed class TransformGizmo
{
    private const float AxisLength = 1.2f;
    private const float ArrowHeadSize = 0.15f;
    private const float ScaleCubeSize = 0.1f;
    private const float RotationCircleRadius = 1.0f;
    private const float AxisPickRadius = 0.1f;

    // Gizmo colors
    private static readonly Vector4 XAxisColor = new(1f, 0.2f, 0.2f, 1f);       // Red
    private static readonly Vector4 YAxisColor = new(0.2f, 1f, 0.2f, 1f);       // Green
    private static readonly Vector4 ZAxisColor = new(0.2f, 0.4f, 1f, 1f);       // Blue
    private static readonly Vector4 HighlightColor = new(1f, 1f, 0f, 1f);       // Yellow

    // Cached mesh handles
    private MeshHandle _cubeMesh;
    private bool _meshesCreated;

    // State
    private GizmoAxis _hoveredAxis = GizmoAxis.None;
    private GizmoAxis _activeAxis = GizmoAxis.None;
    private bool _isDragging;
    private Vector3 _dragStartPosition;
    private Vector3 _dragStartScale;
    private Quaternion _dragStartRotation;
    private Vector2 _dragStartMouse;
    private Vector3 _dragPlaneNormal;
    private Vector3 _dragPlanePoint;

    /// <summary>
    /// Gets or sets the current gizmo mode.
    /// </summary>
    public GizmoMode Mode { get; set; } = GizmoMode.Translate;

    /// <summary>
    /// Gets or sets the coordinate space for transform operations.
    /// </summary>
    public GizmoSpace Space { get; set; } = GizmoSpace.World;

    /// <summary>
    /// Gets whether the gizmo is currently being dragged.
    /// </summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// Gets the currently hovered axis.
    /// </summary>
    public GizmoAxis HoveredAxis => _hoveredAxis;

    /// <summary>
    /// Initializes the gizmo meshes.
    /// </summary>
    /// <param name="graphics">The graphics context.</param>
    public void Initialize(IGraphicsContext graphics)
    {
        if (_meshesCreated || !graphics.IsInitialized)
        {
            return;
        }

        _cubeMesh = graphics.CreateCube(ScaleCubeSize);
        _meshesCreated = true;
    }

    /// <summary>
    /// Updates the gizmo state and processes input.
    /// </summary>
    /// <param name="input">The input provider.</param>
    /// <param name="cameraController">The camera controller for view information.</param>
    /// <param name="sceneWorld">The scene world containing entities.</param>
    /// <param name="selectedEntity">The currently selected entity.</param>
    /// <param name="viewportBounds">The viewport bounds in screen coordinates.</param>
    /// <param name="projectionMatrix">The projection matrix.</param>
    /// <returns>True if the gizmo consumed the input.</returns>
    public bool Update(
        IInputProvider input,
        EditorCameraController cameraController,
        World sceneWorld,
        Entity selectedEntity,
        in Rectangle viewportBounds,
        in Matrix4x4 projectionMatrix)
    {
        if (!selectedEntity.IsValid || !sceneWorld.Has<Transform3D>(selectedEntity))
        {
            _hoveredAxis = GizmoAxis.None;
            _isDragging = false;
            return false;
        }

        ref var transform = ref sceneWorld.Get<Transform3D>(selectedEntity);
        var gizmoPosition = transform.Position;

        // Calculate gizmo scale based on distance from camera
        var distanceToCamera = Vector3.Distance(cameraController.Position, gizmoPosition);
        var gizmoScale = distanceToCamera * 0.1f;

        // Get mouse position relative to viewport
        var mousePos = input.MousePosition;
        var localX = mousePos.X - viewportBounds.X;
        var localY = mousePos.Y - viewportBounds.Y;
        var normalizedX = localX / viewportBounds.Width;
        var normalizedY = localY / viewportBounds.Height;

        var leftMouseDown = input.IsMouseButtonDown(MouseButton.Left);

        if (_isDragging)
        {
            if (leftMouseDown)
            {
                // Continue dragging
                ProcessDrag(
                    input,
                    cameraController,
                    ref transform,
                    gizmoScale,
                    viewportBounds,
                    projectionMatrix);
                return true;
            }
            else
            {
                // End drag
                _isDragging = false;
                _activeAxis = GizmoAxis.None;
            }
        }
        else
        {
            // Update hover state
            _hoveredAxis = HitTestGizmo(
                normalizedX,
                normalizedY,
                gizmoPosition,
                gizmoScale,
                cameraController,
                viewportBounds,
                projectionMatrix);

            // Start drag if clicking on gizmo
            if (leftMouseDown && _hoveredAxis != GizmoAxis.None)
            {
                StartDrag(input, transform, cameraController, gizmoScale);
                return true;
            }
        }

        return _hoveredAxis != GizmoAxis.None;
    }

    /// <summary>
    /// Renders the gizmo for the selected entity.
    /// </summary>
    /// <param name="graphics">The graphics context.</param>
    /// <param name="sceneWorld">The scene world.</param>
    /// <param name="selectedEntity">The selected entity.</param>
    /// <param name="viewMatrix">The view matrix.</param>
    /// <param name="projectionMatrix">The projection matrix.</param>
    /// <param name="cameraPosition">The camera position.</param>
    public void Render(
        IGraphicsContext graphics,
        World sceneWorld,
        Entity selectedEntity,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix,
        Vector3 cameraPosition)
    {
        if (!selectedEntity.IsValid || !sceneWorld.Has<Transform3D>(selectedEntity))
        {
            return;
        }

        if (!_meshesCreated)
        {
            Initialize(graphics);
        }

        if (!_meshesCreated)
        {
            return;
        }

        ref readonly var transform = ref sceneWorld.Get<Transform3D>(selectedEntity);
        var gizmoPosition = transform.Position;

        // Calculate gizmo scale based on distance from camera
        var distanceToCamera = Vector3.Distance(cameraPosition, gizmoPosition);
        var gizmoScale = distanceToCamera * 0.1f;

        // Get rotation for local space
        var gizmoRotation = Space == GizmoSpace.Local ? transform.Rotation : Quaternion.Identity;

        // Disable depth testing so gizmo is always visible
        graphics.SetDepthTest(false);
        graphics.SetBlending(true);

        switch (Mode)
        {
            case GizmoMode.Translate:
                RenderTranslateGizmo(graphics, gizmoPosition, gizmoRotation, gizmoScale, viewMatrix, projectionMatrix);
                break;
            case GizmoMode.Rotate:
                RenderRotateGizmo(graphics, gizmoPosition, gizmoRotation, gizmoScale, viewMatrix, projectionMatrix);
                break;
            case GizmoMode.Scale:
                RenderScaleGizmo(graphics, gizmoPosition, gizmoRotation, gizmoScale, viewMatrix, projectionMatrix);
                break;
        }

        // Restore state
        graphics.SetDepthTest(true);
        graphics.SetBlending(false);
    }

    /// <summary>
    /// Cycles to the next gizmo mode.
    /// </summary>
    public void CycleMode()
    {
        Mode = Mode switch
        {
            GizmoMode.Translate => GizmoMode.Rotate,
            GizmoMode.Rotate => GizmoMode.Scale,
            GizmoMode.Scale => GizmoMode.Translate,
            _ => GizmoMode.Translate
        };
    }

    /// <summary>
    /// Toggles between world and local space.
    /// </summary>
    public void ToggleSpace()
    {
        Space = Space == GizmoSpace.World ? GizmoSpace.Local : GizmoSpace.World;
    }

    private void StartDrag(
        IInputProvider input,
        in Transform3D transform,
        EditorCameraController cameraController,
        float gizmoScale)
    {
        _isDragging = true;
        _activeAxis = _hoveredAxis;
        _dragStartPosition = transform.Position;
        _dragStartScale = transform.Scale;
        _dragStartRotation = transform.Rotation;
        _dragStartMouse = input.MousePosition;

        // Set up drag plane based on mode and axis
        var cameraForward = cameraController.Forward;

        switch (Mode)
        {
            case GizmoMode.Translate:
            case GizmoMode.Scale:
                SetupAxisDragPlane(cameraForward, transform.Position);
                break;
            case GizmoMode.Rotate:
                SetupRotationDragPlane(transform.Position);
                break;
        }
    }

    private void SetupAxisDragPlane(Vector3 cameraForward, Vector3 position)
    {
        _dragPlanePoint = position;

        switch (_activeAxis)
        {
            case GizmoAxis.X:
                // Plane normal perpendicular to X, facing camera
                _dragPlaneNormal = Vector3.Normalize(Vector3.Cross(Vector3.UnitX, cameraForward));
                if (_dragPlaneNormal.LengthSquared() < 0.01f)
                {
                    _dragPlaneNormal = Vector3.UnitY;
                }
                break;
            case GizmoAxis.Y:
                _dragPlaneNormal = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, cameraForward));
                if (_dragPlaneNormal.LengthSquared() < 0.01f)
                {
                    _dragPlaneNormal = Vector3.UnitZ;
                }
                break;
            case GizmoAxis.Z:
                _dragPlaneNormal = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, cameraForward));
                if (_dragPlaneNormal.LengthSquared() < 0.01f)
                {
                    _dragPlaneNormal = Vector3.UnitY;
                }
                break;
            case GizmoAxis.XY:
                _dragPlaneNormal = Vector3.UnitZ;
                break;
            case GizmoAxis.XZ:
                _dragPlaneNormal = Vector3.UnitY;
                break;
            case GizmoAxis.YZ:
                _dragPlaneNormal = Vector3.UnitX;
                break;
            default:
                _dragPlaneNormal = -cameraForward;
                break;
        }
    }

    private void SetupRotationDragPlane(Vector3 position)
    {
        _dragPlanePoint = position;

        _dragPlaneNormal = _activeAxis switch
        {
            GizmoAxis.X => Vector3.UnitX,
            GizmoAxis.Y => Vector3.UnitY,
            GizmoAxis.Z => Vector3.UnitZ,
            _ => Vector3.UnitY
        };
    }

    private void ProcessDrag(
        IInputProvider input,
        EditorCameraController cameraController,
        ref Transform3D transform,
        float gizmoScale,
        in Rectangle viewportBounds,
        in Matrix4x4 projectionMatrix)
    {
        var mouseDelta = input.MousePosition - _dragStartMouse;

        switch (Mode)
        {
            case GizmoMode.Translate:
                ProcessTranslateDrag(input, cameraController, ref transform, gizmoScale, viewportBounds, projectionMatrix);
                break;
            case GizmoMode.Rotate:
                ProcessRotateDrag(mouseDelta, ref transform);
                break;
            case GizmoMode.Scale:
                ProcessScaleDrag(mouseDelta, ref transform);
                break;
        }
    }

    private void ProcessTranslateDrag(
        IInputProvider input,
        EditorCameraController cameraController,
        ref Transform3D transform,
        float gizmoScale,
        in Rectangle viewportBounds,
        in Matrix4x4 projectionMatrix)
    {
        // Get mouse ray
        var mousePos = input.MousePosition;
        var localX = mousePos.X - viewportBounds.X;
        var localY = mousePos.Y - viewportBounds.Y;
        var normalizedX = localX / viewportBounds.Width;
        var normalizedY = localY / viewportBounds.Height;

        var rayOrigin = cameraController.Position;
        var rayDir = CalculateRayDirection(cameraController, normalizedX, normalizedY, projectionMatrix);

        // Intersect with drag plane
        var denom = Vector3.Dot(_dragPlaneNormal, rayDir);
        if (MathF.Abs(denom) > 0.0001f)
        {
            var t = Vector3.Dot(_dragPlanePoint - rayOrigin, _dragPlaneNormal) / denom;
            if (t > 0)
            {
                var intersection = rayOrigin + rayDir * t;
                var offset = intersection - _dragPlanePoint;

                // Constrain to axis if single axis selected
                Vector3 movement;
                switch (_activeAxis)
                {
                    case GizmoAxis.X:
                        movement = new Vector3(offset.X, 0, 0);
                        break;
                    case GizmoAxis.Y:
                        movement = new Vector3(0, offset.Y, 0);
                        break;
                    case GizmoAxis.Z:
                        movement = new Vector3(0, 0, offset.Z);
                        break;
                    case GizmoAxis.XY:
                        movement = new Vector3(offset.X, offset.Y, 0);
                        break;
                    case GizmoAxis.XZ:
                        movement = new Vector3(offset.X, 0, offset.Z);
                        break;
                    case GizmoAxis.YZ:
                        movement = new Vector3(0, offset.Y, offset.Z);
                        break;
                    default:
                        movement = offset;
                        break;
                }

                transform.Position = _dragStartPosition + movement;
            }
        }
    }

    private void ProcessRotateDrag(Vector2 mouseDelta, ref Transform3D transform)
    {
        const float rotationSensitivity = 0.5f;
        var deltaAngle = mouseDelta.X * rotationSensitivity * MathF.PI / 180f;

        Vector3 axis = _activeAxis switch
        {
            GizmoAxis.X => Vector3.UnitX,
            GizmoAxis.Y => Vector3.UnitY,
            GizmoAxis.Z => Vector3.UnitZ,
            _ => Vector3.UnitY
        };

        if (Space == GizmoSpace.Local)
        {
            axis = Vector3.Transform(axis, _dragStartRotation);
        }

        var rotation = Quaternion.CreateFromAxisAngle(axis, deltaAngle);
        transform.Rotation = rotation * _dragStartRotation;
    }

    private void ProcessScaleDrag(Vector2 mouseDelta, ref Transform3D transform)
    {
        const float scaleSensitivity = 0.01f;
        var scaleDelta = mouseDelta.X * scaleSensitivity;

        switch (_activeAxis)
        {
            case GizmoAxis.X:
                transform.Scale = _dragStartScale + new Vector3(scaleDelta, 0, 0);
                break;
            case GizmoAxis.Y:
                transform.Scale = _dragStartScale + new Vector3(0, scaleDelta, 0);
                break;
            case GizmoAxis.Z:
                transform.Scale = _dragStartScale + new Vector3(0, 0, scaleDelta);
                break;
            case GizmoAxis.All:
                transform.Scale = _dragStartScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);
                break;
            default:
                transform.Scale = _dragStartScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);
                break;
        }

        // Clamp scale to prevent negative values
        transform.Scale = Vector3.Max(transform.Scale, new Vector3(0.01f, 0.01f, 0.01f));
    }

    private static GizmoAxis HitTestGizmo(
        float normalizedX,
        float normalizedY,
        Vector3 gizmoPosition,
        float gizmoScale,
        EditorCameraController cameraController,
        in Rectangle viewportBounds,
        in Matrix4x4 projectionMatrix)
    {
        var rayOrigin = cameraController.Position;
        var rayDir = CalculateRayDirection(cameraController, normalizedX, normalizedY, projectionMatrix);

        var closestAxis = GizmoAxis.None;
        var closestDistance = float.MaxValue;

        // Scaled axis length
        var axisLength = AxisLength * gizmoScale;

        // Test each axis
        var axes = new (Vector3 Direction, GizmoAxis Axis)[]
        {
            (Vector3.UnitX, GizmoAxis.X),
            (Vector3.UnitY, GizmoAxis.Y),
            (Vector3.UnitZ, GizmoAxis.Z)
        };

        foreach (var (direction, axis) in axes)
        {
            var axisEnd = gizmoPosition + direction * axisLength;
            var distance = RayLineDistance(rayOrigin, rayDir, gizmoPosition, axisEnd);

            if (distance < AxisPickRadius * gizmoScale && distance < closestDistance)
            {
                closestDistance = distance;
                closestAxis = axis;
            }
        }

        return closestAxis;
    }

    private static float RayLineDistance(Vector3 rayOrigin, Vector3 rayDir, Vector3 lineStart, Vector3 lineEnd)
    {
        var lineDir = lineEnd - lineStart;
        var lineLength = lineDir.Length();
        if (lineLength < 0.0001f)
        {
            return Vector3.Cross(rayDir, lineStart - rayOrigin).Length();
        }

        lineDir /= lineLength;

        var w0 = rayOrigin - lineStart;
        var a = Vector3.Dot(rayDir, rayDir);
        var b = Vector3.Dot(rayDir, lineDir);
        var c = Vector3.Dot(lineDir, lineDir);
        var d = Vector3.Dot(rayDir, w0);
        var e = Vector3.Dot(lineDir, w0);

        var denom = a * c - b * b;
        if (MathF.Abs(denom) < 0.0001f)
        {
            return Vector3.Cross(rayDir, w0).Length();
        }

        var sc = (b * e - c * d) / denom;
        var tc = (a * e - b * d) / denom;

        tc = Math.Clamp(tc, 0, lineLength);
        sc = Math.Max(sc, 0);

        var closestOnRay = rayOrigin + rayDir * sc;
        var closestOnLine = lineStart + lineDir * tc;

        return Vector3.Distance(closestOnRay, closestOnLine);
    }

    private static Vector3 CalculateRayDirection(
        EditorCameraController camera,
        float normalizedX,
        float normalizedY,
        in Matrix4x4 projectionMatrix)
    {
        // Convert normalized coordinates to clip space (-1 to 1)
        var clipX = normalizedX * 2f - 1f;
        var clipY = 1f - normalizedY * 2f; // Flip Y

        // Use camera orientation to calculate ray direction
        var forward = camera.Forward;
        var right = camera.Right;
        var up = camera.Up;

        // Get FOV from projection matrix
        var fovY = 2f * MathF.Atan(1f / projectionMatrix.M22);
        var tanHalfFov = MathF.Tan(fovY * 0.5f);
        var aspectRatio = projectionMatrix.M22 / projectionMatrix.M11;

        // Calculate ray direction in world space
        var rayDir = Vector3.Normalize(
            forward +
            right * (clipX * tanHalfFov * aspectRatio) +
            up * (clipY * tanHalfFov));

        return rayDir;
    }

    private void RenderTranslateGizmo(
        IGraphicsContext graphics,
        Vector3 position,
        Quaternion rotation,
        float scale,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix)
    {
        // Draw arrows for each axis
        RenderAxis(graphics, position, rotation, scale, Vector3.UnitX, GetAxisColor(GizmoAxis.X), viewMatrix, projectionMatrix);
        RenderAxis(graphics, position, rotation, scale, Vector3.UnitY, GetAxisColor(GizmoAxis.Y), viewMatrix, projectionMatrix);
        RenderAxis(graphics, position, rotation, scale, Vector3.UnitZ, GetAxisColor(GizmoAxis.Z), viewMatrix, projectionMatrix);
    }

    private void RenderRotateGizmo(
        IGraphicsContext graphics,
        Vector3 position,
        Quaternion rotation,
        float scale,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix)
    {
        // Draw rotation circles (represented as axes for now - full circles would need line rendering)
        RenderRotationCircle(graphics, position, rotation, scale, Vector3.UnitX, GetAxisColor(GizmoAxis.X), viewMatrix, projectionMatrix);
        RenderRotationCircle(graphics, position, rotation, scale, Vector3.UnitY, GetAxisColor(GizmoAxis.Y), viewMatrix, projectionMatrix);
        RenderRotationCircle(graphics, position, rotation, scale, Vector3.UnitZ, GetAxisColor(GizmoAxis.Z), viewMatrix, projectionMatrix);
    }

    private void RenderScaleGizmo(
        IGraphicsContext graphics,
        Vector3 position,
        Quaternion rotation,
        float scale,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix)
    {
        // Draw scale handles (cubes at end of axes)
        RenderScaleHandle(graphics, position, rotation, scale, Vector3.UnitX, GetAxisColor(GizmoAxis.X), viewMatrix, projectionMatrix);
        RenderScaleHandle(graphics, position, rotation, scale, Vector3.UnitY, GetAxisColor(GizmoAxis.Y), viewMatrix, projectionMatrix);
        RenderScaleHandle(graphics, position, rotation, scale, Vector3.UnitZ, GetAxisColor(GizmoAxis.Z), viewMatrix, projectionMatrix);

        // Draw center cube for uniform scaling
        RenderCenterCube(graphics, position, scale * 0.15f, GetAxisColor(GizmoAxis.All), viewMatrix, projectionMatrix);
    }

    private void RenderAxis(
        IGraphicsContext graphics,
        Vector3 origin,
        Quaternion rotation,
        float scale,
        Vector3 direction,
        Vector4 color,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix)
    {
        // Transform direction if in local space
        var worldDirection = Space == GizmoSpace.Local
            ? Vector3.Transform(direction, rotation)
            : direction;

        var axisEnd = origin + worldDirection * AxisLength * scale;

        // Render axis line using the solid shader
        // For now, we'll render a thin cube along the axis as a simple representation
        var axisCenter = (origin + axisEnd) * 0.5f;
        var axisLength = Vector3.Distance(origin, axisEnd);

        // Create rotation to align cube with axis
        var axisRotation = CreateRotationFromDirection(worldDirection);

        var modelMatrix =
            Matrix4x4.CreateScale(scale * 0.02f, scale * 0.02f, axisLength) *
            Matrix4x4.CreateFromQuaternion(axisRotation) *
            Matrix4x4.CreateTranslation(axisCenter);

        graphics.BindShader(graphics.SolidShader);
        graphics.SetUniform("uModel", modelMatrix);
        graphics.SetUniform("uView", viewMatrix);
        graphics.SetUniform("uProjection", projectionMatrix);
        graphics.SetUniform("uColor", color);

        graphics.BindMesh(_cubeMesh);
        graphics.DrawMesh(_cubeMesh);

        // Render arrow head at end
        var arrowHeadMatrix =
            Matrix4x4.CreateScale(ArrowHeadSize * scale) *
            Matrix4x4.CreateTranslation(axisEnd);

        graphics.SetUniform("uModel", arrowHeadMatrix);
        graphics.DrawMesh(_cubeMesh);
    }

    private void RenderRotationCircle(
        IGraphicsContext graphics,
        Vector3 origin,
        Quaternion rotation,
        float scale,
        Vector3 axis,
        Vector4 color,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix)
    {
        // For rotation gizmo, we draw a torus-like representation
        // Simplified: draw small cubes along the circle circumference
        var worldAxis = Space == GizmoSpace.Local
            ? Vector3.Transform(axis, rotation)
            : axis;

        var perpendicular1 = Vector3.Cross(worldAxis, Vector3.UnitY);
        if (perpendicular1.LengthSquared() < 0.01f)
        {
            perpendicular1 = Vector3.Cross(worldAxis, Vector3.UnitX);
        }
        perpendicular1 = Vector3.Normalize(perpendicular1);
        var perpendicular2 = Vector3.Cross(worldAxis, perpendicular1);

        var radius = RotationCircleRadius * scale;
        const int segments = 24;

        graphics.BindShader(graphics.SolidShader);
        graphics.SetUniform("uView", viewMatrix);
        graphics.SetUniform("uProjection", projectionMatrix);
        graphics.SetUniform("uColor", color);
        graphics.BindMesh(_cubeMesh);

        for (var i = 0; i < segments; i++)
        {
            var angle = i * MathF.PI * 2f / segments;
            var point = origin +
                perpendicular1 * (MathF.Cos(angle) * radius) +
                perpendicular2 * (MathF.Sin(angle) * radius);

            var pointMatrix =
                Matrix4x4.CreateScale(scale * 0.03f) *
                Matrix4x4.CreateTranslation(point);

            graphics.SetUniform("uModel", pointMatrix);
            graphics.DrawMesh(_cubeMesh);
        }
    }

    private void RenderScaleHandle(
        IGraphicsContext graphics,
        Vector3 origin,
        Quaternion rotation,
        float scale,
        Vector3 direction,
        Vector4 color,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix)
    {
        var worldDirection = Space == GizmoSpace.Local
            ? Vector3.Transform(direction, rotation)
            : direction;

        var handlePos = origin + worldDirection * AxisLength * scale;

        // Render axis line (same as translate)
        var axisCenter = (origin + handlePos) * 0.5f;
        var axisLength = Vector3.Distance(origin, handlePos);
        var axisRotation = CreateRotationFromDirection(worldDirection);

        var lineMatrix =
            Matrix4x4.CreateScale(scale * 0.02f, scale * 0.02f, axisLength) *
            Matrix4x4.CreateFromQuaternion(axisRotation) *
            Matrix4x4.CreateTranslation(axisCenter);

        graphics.BindShader(graphics.SolidShader);
        graphics.SetUniform("uView", viewMatrix);
        graphics.SetUniform("uProjection", projectionMatrix);
        graphics.SetUniform("uColor", color);

        graphics.BindMesh(_cubeMesh);
        graphics.SetUniform("uModel", lineMatrix);
        graphics.DrawMesh(_cubeMesh);

        // Render cube at end
        var cubeMatrix =
            Matrix4x4.CreateScale(ScaleCubeSize * scale) *
            Matrix4x4.CreateTranslation(handlePos);

        graphics.SetUniform("uModel", cubeMatrix);
        graphics.DrawMesh(_cubeMesh);
    }

    private void RenderCenterCube(
        IGraphicsContext graphics,
        Vector3 position,
        float size,
        Vector4 color,
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix)
    {
        var modelMatrix =
            Matrix4x4.CreateScale(size) *
            Matrix4x4.CreateTranslation(position);

        graphics.BindShader(graphics.SolidShader);
        graphics.SetUniform("uModel", modelMatrix);
        graphics.SetUniform("uView", viewMatrix);
        graphics.SetUniform("uProjection", projectionMatrix);
        graphics.SetUniform("uColor", color);

        graphics.BindMesh(_cubeMesh);
        graphics.DrawMesh(_cubeMesh);
    }

    private Vector4 GetAxisColor(GizmoAxis axis)
    {
        if (_isDragging && axis == _activeAxis)
        {
            return HighlightColor;
        }

        if (_hoveredAxis == axis)
        {
            return HighlightColor;
        }

        return axis switch
        {
            GizmoAxis.X => XAxisColor,
            GizmoAxis.Y => YAxisColor,
            GizmoAxis.Z => ZAxisColor,
            GizmoAxis.All => new Vector4(0.8f, 0.8f, 0.8f, 1f),
            _ => new Vector4(1f, 1f, 1f, 1f)
        };
    }

    private static Quaternion CreateRotationFromDirection(Vector3 direction)
    {
        var forward = Vector3.Normalize(direction);
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
