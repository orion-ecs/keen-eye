using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Sample.PBR;

/// <summary>
/// System that updates orbit cameras based on mouse input.
/// </summary>
public class OrbitCameraSystem : SystemBase
{
    private IInputContext? input;
    private Vector2 lastMousePosition;
    private bool initialized;

    /// <summary>
    /// Updates the orbit camera based on mouse input.
    /// </summary>
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null)
        {
            return;
        }

        var mouse = input.Mouse;
        var currentPos = mouse.Position;

        // Initialize last position on first frame
        if (!initialized)
        {
            lastMousePosition = currentPos;
            initialized = true;
            return;
        }

        // Calculate mouse delta
        var mouseDelta = currentPos - lastMousePosition;
        lastMousePosition = currentPos;

        foreach (var entity in World.Query<Transform3D, OrbitCamera, MainCameraTag>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var orbit = ref World.Get<OrbitCamera>(entity);

            // Rotate when right mouse button is held (or left click drag)
            if (mouse.IsButtonDown(MouseButton.Right) || mouse.IsButtonDown(MouseButton.Left))
            {
                orbit.Yaw -= mouseDelta.X * orbit.RotationSensitivity;
                orbit.Pitch -= mouseDelta.Y * orbit.RotationSensitivity;

                // Clamp pitch
                orbit.Pitch = Math.Clamp(orbit.Pitch, orbit.MinPitch, orbit.MaxPitch);
            }

            // Calculate camera position from spherical coordinates
            var cosP = MathF.Cos(orbit.Pitch);
            var sinP = MathF.Sin(orbit.Pitch);
            var cosY = MathF.Cos(orbit.Yaw);
            var sinY = MathF.Sin(orbit.Yaw);

            var offset = new Vector3(
                cosP * sinY * orbit.Distance,
                sinP * orbit.Distance,
                cosP * cosY * orbit.Distance);

            var cameraPos = orbit.Target + offset;

            // Calculate look-at rotation
            var forward = Vector3.Normalize(orbit.Target - cameraPos);
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
            var up = Vector3.Cross(forward, right);

            // Handle edge case when looking straight up/down
            if (float.IsNaN(right.X))
            {
                right = new Vector3(1, 0, 0);
                up = Vector3.Cross(forward, right);
            }

            transform.Position = cameraPos;
            transform.Rotation = Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                -forward.X, -forward.Y, -forward.Z, 0,
                0, 0, 0, 1));
        }
    }
}

/// <summary>
/// System that handles camera zoom via mouse scroll.
/// </summary>
public class CameraZoomSystem : SystemBase
{
    private IInputContext? input;
    private float accumulatedScroll;
    private Action<MouseScrollEventArgs>? scrollHandler;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        if (World.TryGetExtension<IInputContext>(out var ctx))
        {
            input = ctx;
            scrollHandler = args =>
            {
                accumulatedScroll += args.DeltaY;
            };
            ctx!.Mouse.OnScroll += scrollHandler;
        }
    }

    /// <summary>
    /// Updates camera zoom based on scroll input.
    /// </summary>
    public override void Update(float deltaTime)
    {
        if (input is null || accumulatedScroll.IsApproximatelyZero())
        {
            return;
        }

        foreach (var entity in World.Query<OrbitCamera, MainCameraTag>())
        {
            ref var orbit = ref World.Get<OrbitCamera>(entity);

            // Zoom in/out
            orbit.Distance -= accumulatedScroll * orbit.ZoomSensitivity;
            orbit.Distance = Math.Clamp(orbit.Distance, orbit.MinDistance, orbit.MaxDistance);
        }

        accumulatedScroll = 0;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && input is not null && scrollHandler is not null)
        {
            input.Mouse.OnScroll -= scrollHandler;
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// System that slowly rotates entities with the AutoRotate component.
/// </summary>
public class AutoRotateSystem : SystemBase
{
    /// <summary>
    /// Updates the rotation of auto-rotating entities.
    /// </summary>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, AutoRotate>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var autoRotate = ref World.Get<AutoRotate>(entity);

            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, autoRotate.Speed * deltaTime);
            transform.Rotation = Quaternion.Normalize(transform.Rotation * rotation);
        }
    }
}

/// <summary>
/// System that animates orbiting lights.
/// </summary>
public class OrbitingLightSystem : SystemBase
{
    /// <summary>
    /// Updates the position of orbiting lights.
    /// </summary>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, OrbitingLight>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var orbitingLight = ref World.Get<OrbitingLight>(entity);

            orbitingLight.Angle += orbitingLight.Speed * deltaTime;

            transform.Position = new Vector3(
                orbitingLight.Center.X + MathF.Cos(orbitingLight.Angle) * orbitingLight.Radius,
                orbitingLight.Center.Y + orbitingLight.Height,
                orbitingLight.Center.Z + MathF.Sin(orbitingLight.Angle) * orbitingLight.Radius);
        }
    }
}
