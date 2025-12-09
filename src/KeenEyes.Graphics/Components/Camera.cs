using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Graphics;

/// <summary>
/// Specifies the projection type for a camera.
/// </summary>
public enum ProjectionType
{
    /// <summary>
    /// Perspective projection with depth-based foreshortening.
    /// </summary>
    Perspective,

    /// <summary>
    /// Orthographic projection with no foreshortening.
    /// </summary>
    Orthographic
}

/// <summary>
/// Component that defines a camera for rendering the scene.
/// </summary>
/// <remarks>
/// <para>
/// Cameras require a <see cref="Transform3D"/> component to define their position
/// and orientation in the world. The view matrix is computed from the transform,
/// while the projection matrix is computed from this component's settings.
/// </para>
/// <para>
/// For multiple cameras, use the <see cref="Priority"/> field to control render order.
/// Higher priority cameras render later (on top).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// world.Spawn()
///     .With(new Transform3D(new Vector3(0, 5, 10), Quaternion.Identity, Vector3.One))
///     .With(Camera.CreatePerspective(60f, 16f/9f, 0.1f, 1000f))
///     .WithTag&lt;MainCameraTag&gt;()
///     .Build();
/// </code>
/// </example>
public struct Camera : IComponent
{
    /// <summary>
    /// The projection type (perspective or orthographic).
    /// </summary>
    public ProjectionType Projection;

    /// <summary>
    /// The vertical field of view in degrees (for perspective projection).
    /// </summary>
    public float FieldOfView;

    /// <summary>
    /// The orthographic size (half-height of the view, for orthographic projection).
    /// </summary>
    public float OrthographicSize;

    /// <summary>
    /// The near clipping plane distance.
    /// </summary>
    public float NearPlane;

    /// <summary>
    /// The far clipping plane distance.
    /// </summary>
    public float FarPlane;

    /// <summary>
    /// The aspect ratio (width / height).
    /// </summary>
    public float AspectRatio;

    /// <summary>
    /// The camera's render priority. Higher values render later.
    /// </summary>
    public int Priority;

    /// <summary>
    /// The viewport rectangle (x, y, width, height) in normalized coordinates [0-1].
    /// </summary>
    public Vector4 Viewport;

    /// <summary>
    /// The clear color for this camera.
    /// </summary>
    public Vector4 ClearColor;

    /// <summary>
    /// Whether to clear the color buffer before rendering.
    /// </summary>
    public bool ClearColorBuffer;

    /// <summary>
    /// Whether to clear the depth buffer before rendering.
    /// </summary>
    public bool ClearDepthBuffer;

    /// <summary>
    /// Creates a perspective camera with the specified settings.
    /// </summary>
    /// <param name="fieldOfView">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Aspect ratio (width / height).</param>
    /// <param name="nearPlane">Near clipping plane distance.</param>
    /// <param name="farPlane">Far clipping plane distance.</param>
    /// <returns>A new perspective camera.</returns>
    public static Camera CreatePerspective(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        return new Camera
        {
            Projection = ProjectionType.Perspective,
            FieldOfView = fieldOfView,
            AspectRatio = aspectRatio,
            NearPlane = nearPlane,
            FarPlane = farPlane,
            Viewport = new Vector4(0, 0, 1, 1),
            ClearColor = new Vector4(0.1f, 0.1f, 0.1f, 1f),
            ClearColorBuffer = true,
            ClearDepthBuffer = true
        };
    }

    /// <summary>
    /// Creates an orthographic camera with the specified settings.
    /// </summary>
    /// <param name="size">Half-height of the orthographic view.</param>
    /// <param name="aspectRatio">Aspect ratio (width / height).</param>
    /// <param name="nearPlane">Near clipping plane distance.</param>
    /// <param name="farPlane">Far clipping plane distance.</param>
    /// <returns>A new orthographic camera.</returns>
    public static Camera CreateOrthographic(float size, float aspectRatio, float nearPlane, float farPlane)
    {
        return new Camera
        {
            Projection = ProjectionType.Orthographic,
            OrthographicSize = size,
            AspectRatio = aspectRatio,
            NearPlane = nearPlane,
            FarPlane = farPlane,
            Viewport = new Vector4(0, 0, 1, 1),
            ClearColor = new Vector4(0.1f, 0.1f, 0.1f, 1f),
            ClearColorBuffer = true,
            ClearDepthBuffer = true
        };
    }

    /// <summary>
    /// Computes the projection matrix for this camera.
    /// </summary>
    /// <returns>The projection matrix.</returns>
    public readonly Matrix4x4 GetProjectionMatrix()
    {
        return Projection switch
        {
            ProjectionType.Perspective => Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfView * (MathF.PI / 180f),
                AspectRatio,
                NearPlane,
                FarPlane),
            ProjectionType.Orthographic => Matrix4x4.CreateOrthographic(
                OrthographicSize * 2f * AspectRatio,
                OrthographicSize * 2f,
                NearPlane,
                FarPlane),
            _ => Matrix4x4.Identity
        };
    }

    /// <summary>
    /// Computes the view matrix from a transform.
    /// </summary>
    /// <param name="transform">The camera's transform.</param>
    /// <returns>The view matrix.</returns>
    public static Matrix4x4 GetViewMatrix(in Transform3D transform)
    {
        var forward = transform.Forward;
        var up = transform.Up;
        return Matrix4x4.CreateLookAt(transform.Position, transform.Position + forward, up);
    }
}

/// <summary>
/// Tag component to mark the main camera.
/// </summary>
public struct MainCameraTag : ITagComponent;
