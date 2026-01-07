using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Viewport;

/// <summary>
/// Camera control modes for the editor viewport.
/// </summary>
public enum EditorCameraMode
{
    /// <summary>
    /// Orbit around a target point.
    /// </summary>
    Orbit,

    /// <summary>
    /// First-person fly camera.
    /// </summary>
    Fly,

    /// <summary>
    /// Top-down 2D view.
    /// </summary>
    TopDown,

    /// <summary>
    /// Front orthographic view.
    /// </summary>
    Front,

    /// <summary>
    /// Side orthographic view.
    /// </summary>
    Side
}

/// <summary>
/// Controls the editor viewport camera with orbit, pan, zoom, and fly modes.
/// </summary>
/// <remarks>
/// <para>
/// The controller supports multiple camera modes:
/// <list type="bullet">
/// <item><see cref="EditorCameraMode.Orbit"/>: Orbit around a target point</item>
/// <item><see cref="EditorCameraMode.Fly"/>: First-person WASD navigation</item>
/// <item><see cref="EditorCameraMode.TopDown"/>: Top-down 2D view</item>
/// </list>
/// </para>
/// <para>
/// Controls:
/// <list type="bullet">
/// <item>Middle Mouse + Drag: Pan the camera</item>
/// <item>Alt + Middle Mouse + Drag: Orbit around target</item>
/// <item>Scroll Wheel: Zoom in/out</item>
/// <item>F key: Focus on selected entity</item>
/// <item>WASD: Fly mode movement (when in fly mode)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class EditorCameraController
{
    private const float DefaultDistance = 10f;
    private const float MinDistance = 0.1f;
    private const float MaxDistance = 1000f;
    private const float OrbitSensitivity = 0.3f;
    private const float PanSensitivity = 0.01f;
    private const float ZoomSensitivity = 0.1f;
    private const float FlySpeed = 10f;
    private const float MinPitch = -89f;
    private const float MaxPitch = 89f;

    private float distance = DefaultDistance;
    private float pitch = -30f;

    /// <summary>
    /// Gets or sets the target point to orbit around.
    /// </summary>
    public Vector3 Target { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the distance from the target.
    /// </summary>
    public float Distance
    {
        get => distance;
        set => distance = Math.Clamp(value, MinDistance, MaxDistance);
    }

    /// <summary>
    /// Gets or sets the yaw angle in degrees.
    /// </summary>
    public float Yaw { get; set; }

    /// <summary>
    /// Gets or sets the pitch angle in degrees.
    /// </summary>
    public float Pitch
    {
        get => pitch;
        set => pitch = Math.Clamp(value, MinPitch, MaxPitch);
    }

    /// <summary>
    /// Gets or sets the camera control mode.
    /// </summary>
    public EditorCameraMode Mode { get; set; } = EditorCameraMode.Orbit;

    /// <summary>
    /// Gets the computed camera position based on current settings.
    /// </summary>
    public Vector3 Position
    {
        get
        {
            var yawRad = MathF.PI * Yaw / 180f;
            var pitchRad = MathF.PI * pitch / 180f;

            var x = distance * MathF.Cos(pitchRad) * MathF.Sin(yawRad);
            var y = distance * MathF.Sin(pitchRad);
            var z = distance * MathF.Cos(pitchRad) * MathF.Cos(yawRad);

            return Target + new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Gets the forward direction of the camera.
    /// </summary>
    public Vector3 Forward => Vector3.Normalize(Target - Position);

    /// <summary>
    /// Gets the right direction of the camera.
    /// </summary>
    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));

    /// <summary>
    /// Gets the up direction of the camera.
    /// </summary>
    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

    /// <summary>
    /// Computes the transform for the camera.
    /// </summary>
    /// <returns>The camera transform.</returns>
    public Transform3D GetTransform()
    {
        var position = Position;
        var forward = Forward;

        // Create rotation that looks at target
        var rotation = CreateLookAtRotation(forward, Vector3.UnitY);

        return new Transform3D(position, rotation, Vector3.One);
    }

    /// <summary>
    /// Computes the view matrix for rendering.
    /// </summary>
    /// <returns>The view matrix.</returns>
    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitY);
    }

    /// <summary>
    /// Processes input to update the camera state.
    /// </summary>
    /// <param name="input">The input provider.</param>
    /// <param name="deltaTime">Time elapsed since last update.</param>
    /// <param name="viewportHovered">Whether the viewport is currently hovered.</param>
    public void ProcessInput(IInputProvider input, float deltaTime, bool viewportHovered)
    {
        if (!viewportHovered)
        {
            return;
        }

        switch (Mode)
        {
            case EditorCameraMode.Orbit:
                ProcessOrbitInput(input, deltaTime);
                break;
            case EditorCameraMode.Fly:
                ProcessFlyInput(input, deltaTime);
                break;
            case EditorCameraMode.TopDown:
                ProcessTopDownInput(input, deltaTime);
                break;
        }

        // Zoom with scroll wheel (all modes)
        var scroll = input.MouseScrollDelta;
        if (MathF.Abs(scroll.Y) > 0.01f)
        {
            distance *= 1f - scroll.Y * ZoomSensitivity;
            distance = Math.Clamp(distance, MinDistance, MaxDistance);
        }
    }

    /// <summary>
    /// Focuses the camera on a specific position.
    /// </summary>
    /// <param name="position">The position to focus on.</param>
    /// <param name="newDistance">Optional distance from the target.</param>
    public void FocusOn(Vector3 position, float? newDistance = null)
    {
        Target = position;
        if (newDistance.HasValue)
        {
            Distance = Math.Clamp(newDistance.Value, MinDistance, MaxDistance);
        }
    }

    /// <summary>
    /// Focuses the camera on a transform.
    /// </summary>
    /// <param name="transform">The transform to focus on.</param>
    public void FocusOn(in Transform3D transform)
    {
        FocusOn(transform.Position, 5f);
    }

    /// <summary>
    /// Resets the camera to the default state.
    /// </summary>
    public void Reset()
    {
        Target = Vector3.Zero;
        distance = DefaultDistance;
        Yaw = 0f;
        pitch = -30f;
        Mode = EditorCameraMode.Orbit;
    }

    /// <summary>
    /// Sets the camera to a preset view.
    /// </summary>
    /// <param name="preset">The preset view.</param>
    public void SetPresetView(ViewPreset preset)
    {
        switch (preset)
        {
            case ViewPreset.Front:
                Yaw = 0f;
                pitch = 0f;
                break;
            case ViewPreset.Back:
                Yaw = 180f;
                pitch = 0f;
                break;
            case ViewPreset.Left:
                Yaw = -90f;
                pitch = 0f;
                break;
            case ViewPreset.Right:
                Yaw = 90f;
                pitch = 0f;
                break;
            case ViewPreset.Top:
                Yaw = 0f;
                pitch = -89f;
                break;
            case ViewPreset.Bottom:
                Yaw = 0f;
                pitch = 89f;
                break;
        }
    }

    private void ProcessOrbitInput(IInputProvider input, float deltaTime)
    {
        var middleDown = input.IsMouseButtonDown(MouseButton.Middle);
        var altDown = input.IsKeyDown(Key.LeftAlt) || input.IsKeyDown(Key.RightAlt);
        var mouseDelta = input.MouseDelta;

        if (middleDown)
        {
            if (altDown)
            {
                // Orbit around target
                Yaw += mouseDelta.X * OrbitSensitivity;
                pitch -= mouseDelta.Y * OrbitSensitivity;
                pitch = Math.Clamp(pitch, MinPitch, MaxPitch);
            }
            else
            {
                // Pan
                var panAmount = new Vector3(
                    -mouseDelta.X * PanSensitivity * distance,
                    mouseDelta.Y * PanSensitivity * distance,
                    0);

                Target += Right * panAmount.X;
                Target += Up * panAmount.Y;
            }
        }
    }

    private void ProcessFlyInput(IInputProvider input, float deltaTime)
    {
        var rightMouseDown = input.IsMouseButtonDown(MouseButton.Right);

        if (rightMouseDown)
        {
            // Mouse look
            var mouseDelta = input.MouseDelta;
            Yaw += mouseDelta.X * OrbitSensitivity;
            pitch -= mouseDelta.Y * OrbitSensitivity;
            pitch = Math.Clamp(pitch, MinPitch, MaxPitch);
        }

        // WASD movement
        var movement = Vector3.Zero;
        var speed = FlySpeed * deltaTime;

        if (input.IsKeyDown(Key.W))
        {
            movement += Forward * speed;
        }

        if (input.IsKeyDown(Key.S))
        {
            movement -= Forward * speed;
        }

        if (input.IsKeyDown(Key.A))
        {
            movement -= Right * speed;
        }

        if (input.IsKeyDown(Key.D))
        {
            movement += Right * speed;
        }

        if (input.IsKeyDown(Key.E) || input.IsKeyDown(Key.Space))
        {
            movement += Vector3.UnitY * speed;
        }

        if (input.IsKeyDown(Key.Q) || input.IsKeyDown(Key.LeftControl))
        {
            movement -= Vector3.UnitY * speed;
        }

        Target += movement;
    }

    private void ProcessTopDownInput(IInputProvider input, float deltaTime)
    {
        var middleDown = input.IsMouseButtonDown(MouseButton.Middle);
        var mouseDelta = input.MouseDelta;

        if (middleDown)
        {
            // Pan in XZ plane
            var panAmount = new Vector3(
                -mouseDelta.X * PanSensitivity * distance,
                0,
                mouseDelta.Y * PanSensitivity * distance);

            Target += panAmount;
        }

        // Force top-down view
        pitch = -89f;
    }

    private static Quaternion CreateLookAtRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
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

/// <summary>
/// Preset camera view angles.
/// </summary>
public enum ViewPreset
{
    /// <summary>Front view (looking at -Z).</summary>
    Front,
    /// <summary>Back view (looking at +Z).</summary>
    Back,
    /// <summary>Left view (looking at +X).</summary>
    Left,
    /// <summary>Right view (looking at -X).</summary>
    Right,
    /// <summary>Top view (looking at -Y).</summary>
    Top,
    /// <summary>Bottom view (looking at +Y).</summary>
    Bottom
}
