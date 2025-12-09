using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the Camera component.
/// </summary>
public class CameraTests
{
    private const float Epsilon = 1e-5f;

    #region CreatePerspective Tests

    [Fact]
    public void CreatePerspective_SetsProjectionType()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(ProjectionType.Perspective, camera.Projection);
    }

    [Fact]
    public void CreatePerspective_SetsFieldOfView()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(60f, camera.FieldOfView);
    }

    [Fact]
    public void CreatePerspective_SetsAspectRatio()
    {
        float aspectRatio = 16f / 9f;
        var camera = Camera.CreatePerspective(60f, aspectRatio, 0.1f, 1000f);

        Assert.Equal(aspectRatio, camera.AspectRatio, Epsilon);
    }

    [Fact]
    public void CreatePerspective_SetsNearPlane()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(0.1f, camera.NearPlane, Epsilon);
    }

    [Fact]
    public void CreatePerspective_SetsFarPlane()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(1000f, camera.FarPlane, Epsilon);
    }

    [Fact]
    public void CreatePerspective_SetsDefaultViewport()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(new Vector4(0, 0, 1, 1), camera.Viewport);
    }

    [Fact]
    public void CreatePerspective_SetsClearBuffersToTrue()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.True(camera.ClearColorBuffer);
        Assert.True(camera.ClearDepthBuffer);
    }

    [Fact]
    public void CreatePerspective_SetsDefaultClearColor()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(new Vector4(0.1f, 0.1f, 0.1f, 1f), camera.ClearColor);
    }

    #endregion

    #region CreateOrthographic Tests

    [Fact]
    public void CreateOrthographic_SetsProjectionType()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(ProjectionType.Orthographic, camera.Projection);
    }

    [Fact]
    public void CreateOrthographic_SetsOrthographicSize()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(5f, camera.OrthographicSize, Epsilon);
    }

    [Fact]
    public void CreateOrthographic_SetsAspectRatio()
    {
        float aspectRatio = 16f / 9f;
        var camera = Camera.CreateOrthographic(5f, aspectRatio, 0.1f, 1000f);

        Assert.Equal(aspectRatio, camera.AspectRatio, Epsilon);
    }

    [Fact]
    public void CreateOrthographic_SetsNearPlane()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.5f, 500f);

        Assert.Equal(0.5f, camera.NearPlane, Epsilon);
    }

    [Fact]
    public void CreateOrthographic_SetsFarPlane()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.5f, 500f);

        Assert.Equal(500f, camera.FarPlane, Epsilon);
    }

    #endregion

    #region GetProjectionMatrix Tests

    [Fact]
    public void GetProjectionMatrix_Perspective_ReturnsValidMatrix()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        var matrix = camera.ProjectionMatrix();

        // Matrix should not be identity (it should have been transformed)
        Assert.NotEqual(Matrix4x4.Identity, matrix);
        // M44 should be 0 for perspective projection (homogeneous divide)
        Assert.Equal(0f, matrix.M44, Epsilon);
    }

    [Fact]
    public void GetProjectionMatrix_Orthographic_ReturnsValidMatrix()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.1f, 1000f);

        var matrix = camera.ProjectionMatrix();

        // Matrix should not be identity
        Assert.NotEqual(Matrix4x4.Identity, matrix);
        // M44 should be 1 for orthographic projection (no homogeneous divide)
        Assert.Equal(1f, matrix.M44, Epsilon);
    }

    [Fact]
    public void GetProjectionMatrix_Perspective_ProjectsPointCorrectly()
    {
        var camera = Camera.CreatePerspective(90f, 1f, 1f, 100f);
        var matrix = camera.ProjectionMatrix();

        // Point at the center of the view frustum
        var point = new Vector4(0, 0, -10, 1);
        var projected = Vector4.Transform(point, matrix);

        // After perspective divide, x and y should be 0 (center of view)
        float x = projected.X / projected.W;
        float y = projected.Y / projected.W;
        Assert.Equal(0f, x, Epsilon);
        Assert.Equal(0f, y, Epsilon);
    }

    [Fact]
    public void GetProjectionMatrix_Orthographic_ProjectsPointCorrectly()
    {
        var camera = Camera.CreateOrthographic(5f, 1f, 0.1f, 100f);
        var matrix = camera.ProjectionMatrix();

        // Point at the center
        var point = new Vector4(0, 0, -50, 1);
        var projected = Vector4.Transform(point, matrix);

        // Orthographic projection keeps x and y unchanged (relative to size)
        Assert.Equal(0f, projected.X, Epsilon);
        Assert.Equal(0f, projected.Y, Epsilon);
    }

    #endregion

    #region GetViewMatrix Tests

    [Fact]
    public void GetViewMatrix_IdentityTransform_ReturnsExpectedMatrix()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = Transform3D.Identity;

        var viewMatrix = camera.ViewMatrix(transform);

        // View matrix should transform world-to-camera
        // With identity transform, looking down -Z, the view matrix
        // should be identity (camera at origin looking down -Z)
        Assert.True(IsValidViewMatrix(viewMatrix));
    }

    [Fact]
    public void GetViewMatrix_TranslatedCamera_HasCorrectInverse()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = new Transform3D(
            new Vector3(10, 0, 0),
            Quaternion.Identity,
            Vector3.One);

        var viewMatrix = camera.ViewMatrix(transform);

        // A point at the camera position should transform to origin
        var cameraPos = new Vector4(10, 0, 0, 1);
        var viewSpace = Vector4.Transform(cameraPos, viewMatrix);

        Assert.Equal(0f, viewSpace.X, Epsilon);
        Assert.Equal(0f, viewSpace.Y, Epsilon);
        Assert.Equal(0f, viewSpace.Z, Epsilon);
    }

    [Fact]
    public void GetViewMatrix_PointInFrontOfCamera_HasNegativeZ()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = Transform3D.Identity;
        var viewMatrix = camera.ViewMatrix(transform);

        // Point 10 units in front of camera (negative world Z)
        var worldPoint = new Vector4(0, 0, -10, 1);
        var viewPoint = Vector4.Transform(worldPoint, viewMatrix);

        // In view space, points in front should have negative Z (OpenGL convention)
        Assert.True(viewPoint.Z < 0);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void Camera_DefaultPriority_IsZero()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(0, camera.Priority);
    }

    [Fact]
    public void Camera_DefaultOrthographicSize_IsZeroForPerspective()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(0f, camera.OrthographicSize);
    }

    [Fact]
    public void Camera_DefaultFieldOfView_IsZeroForOrthographic()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.1f, 1000f);

        Assert.Equal(0f, camera.FieldOfView);
    }

    #endregion

    private static bool IsValidViewMatrix(Matrix4x4 matrix)
    {
        // A valid view matrix should be invertible
        return Matrix4x4.Invert(matrix, out _);
    }
}
