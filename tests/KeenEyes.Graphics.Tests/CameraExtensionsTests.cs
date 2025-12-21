using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for CameraExtensions methods.
/// </summary>
public class CameraExtensionsTests
{
    private const float Epsilon = 1e-5f;

    #region ProjectionMatrix Tests

    [Fact]
    public void ProjectionMatrix_PerspectiveCamera_ReturnsNonIdentityMatrix()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        var matrix = camera.ProjectionMatrix();

        Assert.NotEqual(Matrix4x4.Identity, matrix);
    }

    [Fact]
    public void ProjectionMatrix_PerspectiveCamera_M44IsZero()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);

        var matrix = camera.ProjectionMatrix();

        // Perspective projection has M44 = 0 for homogeneous divide
        Assert.Equal(0f, matrix.M44, Epsilon);
    }

    [Fact]
    public void ProjectionMatrix_OrthographicCamera_ReturnsNonIdentityMatrix()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.1f, 1000f);

        var matrix = camera.ProjectionMatrix();

        Assert.NotEqual(Matrix4x4.Identity, matrix);
    }

    [Fact]
    public void ProjectionMatrix_OrthographicCamera_M44IsOne()
    {
        var camera = Camera.CreateOrthographic(5f, 16f / 9f, 0.1f, 1000f);

        var matrix = camera.ProjectionMatrix();

        // Orthographic projection has M44 = 1 (no homogeneous divide)
        Assert.Equal(1f, matrix.M44, Epsilon);
    }

    [Fact]
    public void ProjectionMatrix_InvalidProjectionType_ReturnsIdentity()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        camera = camera with { Projection = (ProjectionType)999 };

        var matrix = camera.ProjectionMatrix();

        Assert.Equal(Matrix4x4.Identity, matrix);
    }

    [Fact]
    public void ProjectionMatrix_PerspectiveDifferentFOV_ProducesDifferentMatrices()
    {
        var camera1 = Camera.CreatePerspective(45f, 1f, 0.1f, 100f);
        var camera2 = Camera.CreatePerspective(90f, 1f, 0.1f, 100f);

        var matrix1 = camera1.ProjectionMatrix();
        var matrix2 = camera2.ProjectionMatrix();

        Assert.NotEqual(matrix1, matrix2);
    }

    [Fact]
    public void ProjectionMatrix_PerspectiveDifferentAspectRatio_ProducesDifferentMatrices()
    {
        var camera1 = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 100f);
        var camera2 = Camera.CreatePerspective(60f, 4f / 3f, 0.1f, 100f);

        var matrix1 = camera1.ProjectionMatrix();
        var matrix2 = camera2.ProjectionMatrix();

        Assert.NotEqual(matrix1, matrix2);
    }

    [Fact]
    public void ProjectionMatrix_PerspectiveDifferentNearPlane_ProducesDifferentMatrices()
    {
        var camera1 = Camera.CreatePerspective(60f, 1f, 0.1f, 100f);
        var camera2 = Camera.CreatePerspective(60f, 1f, 1f, 100f);

        var matrix1 = camera1.ProjectionMatrix();
        var matrix2 = camera2.ProjectionMatrix();

        Assert.NotEqual(matrix1, matrix2);
    }

    [Fact]
    public void ProjectionMatrix_PerspectiveDifferentFarPlane_ProducesDifferentMatrices()
    {
        var camera1 = Camera.CreatePerspective(60f, 1f, 0.1f, 100f);
        var camera2 = Camera.CreatePerspective(60f, 1f, 0.1f, 1000f);

        var matrix1 = camera1.ProjectionMatrix();
        var matrix2 = camera2.ProjectionMatrix();

        Assert.NotEqual(matrix1, matrix2);
    }

    [Fact]
    public void ProjectionMatrix_OrthographicDifferentSize_ProducesDifferentMatrices()
    {
        var camera1 = Camera.CreateOrthographic(5f, 1f, 0.1f, 100f);
        var camera2 = Camera.CreateOrthographic(10f, 1f, 0.1f, 100f);

        var matrix1 = camera1.ProjectionMatrix();
        var matrix2 = camera2.ProjectionMatrix();

        Assert.NotEqual(matrix1, matrix2);
    }

    [Fact]
    public void ProjectionMatrix_Perspective_CenterPointProjectsToOrigin()
    {
        var camera = Camera.CreatePerspective(90f, 1f, 1f, 100f);
        var matrix = camera.ProjectionMatrix();

        var point = new Vector4(0, 0, -10, 1);
        var projected = Vector4.Transform(point, matrix);

        // After perspective divide, center point should be at origin
        float x = projected.X / projected.W;
        float y = projected.Y / projected.W;

        Assert.True(Math.Abs(x) < Epsilon);
        Assert.True(Math.Abs(y) < Epsilon);
    }

    [Fact]
    public void ProjectionMatrix_Orthographic_CenterPointProjectsToOrigin()
    {
        var camera = Camera.CreateOrthographic(5f, 1f, 0.1f, 100f);
        var matrix = camera.ProjectionMatrix();

        var point = new Vector4(0, 0, -50, 1);
        var projected = Vector4.Transform(point, matrix);

        Assert.True(Math.Abs(projected.X) < Epsilon);
        Assert.True(Math.Abs(projected.Y) < Epsilon);
    }

    #endregion

    #region ViewMatrix Tests

    [Fact]
    public void ViewMatrix_IdentityTransform_ReturnsValidMatrix()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = Transform3D.Identity;

        var viewMatrix = camera.ViewMatrix(transform);

        // Should be invertible
        Assert.True(Matrix4x4.Invert(viewMatrix, out _));
    }

    [Fact]
    public void ViewMatrix_TranslatedCamera_TransformsCameraPositionToOrigin()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = new Transform3D(
            new Vector3(10, 5, 3),
            Quaternion.Identity,
            Vector3.One);

        var viewMatrix = camera.ViewMatrix(transform);

        // Camera position should transform to origin in view space
        var cameraPos = new Vector4(10, 5, 3, 1);
        var viewSpace = Vector4.Transform(cameraPos, viewMatrix);

        Assert.True(Math.Abs(viewSpace.X) < Epsilon);
        Assert.True(Math.Abs(viewSpace.Y) < Epsilon);
        Assert.True(Math.Abs(viewSpace.Z) < Epsilon);
    }

    [Fact]
    public void ViewMatrix_PointInFrontOfCamera_HasNegativeZ()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = Transform3D.Identity;

        var viewMatrix = camera.ViewMatrix(transform);

        // Point 10 units in front of camera (negative world Z)
        var worldPoint = new Vector4(0, 0, -10, 1);
        var viewPoint = Vector4.Transform(worldPoint, viewMatrix);

        // In view space, points in front should have negative Z
        Assert.True(viewPoint.Z < 0);
    }

    [Fact]
    public void ViewMatrix_PointBehindCamera_HasPositiveZ()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = Transform3D.Identity;

        var viewMatrix = camera.ViewMatrix(transform);

        // Point 10 units behind camera (positive world Z)
        var worldPoint = new Vector4(0, 0, 10, 1);
        var viewPoint = Vector4.Transform(worldPoint, viewMatrix);

        // In view space, points behind should have positive Z
        Assert.True(viewPoint.Z > 0);
    }

    [Fact]
    public void ViewMatrix_RotatedCamera_TransformsPointsCorrectly()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        // Rotate 90 degrees around Y axis
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        var transform = new Transform3D(Vector3.Zero, rotation, Vector3.One);

        var viewMatrix = camera.ViewMatrix(transform);

        // Point to the right in world space
        var worldPoint = new Vector4(10, 0, 0, 1);
        var viewPoint = Vector4.Transform(worldPoint, viewMatrix);

        // The view matrix should transform the point
        Assert.NotEqual(worldPoint.X, viewPoint.X);
    }

    [Fact]
    public void ViewMatrix_DifferentTransforms_ProduceDifferentMatrices()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform1 = new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One);
        var transform2 = new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One);

        var matrix1 = camera.ViewMatrix(transform1);
        var matrix2 = camera.ViewMatrix(transform2);

        Assert.NotEqual(matrix1, matrix2);
    }

    [Fact]
    public void ViewMatrix_CameraParameter_IsUnused()
    {
        // The camera parameter is only for extension method syntax
        // Different cameras with same transform should produce same view matrix
        var camera1 = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var camera2 = Camera.CreateOrthographic(5f, 16f / 9f, 0.1f, 1000f);
        var transform = Transform3D.Identity;

        var matrix1 = camera1.ViewMatrix(transform);
        var matrix2 = camera2.ViewMatrix(transform);

        Assert.Equal(matrix1, matrix2);
    }

    [Fact]
    public void ViewMatrix_ComplexTransform_IsInvertible()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform = new Transform3D(
            new Vector3(10, 20, 30),
            Quaternion.CreateFromYawPitchRoll(0.5f, 0.3f, 0.1f),
            Vector3.One);

        var viewMatrix = camera.ViewMatrix(transform);

        Assert.True(Matrix4x4.Invert(viewMatrix, out _));
    }

    [Fact]
    public void ViewMatrix_ScaleInTransform_DoesNotAffectViewMatrix()
    {
        var camera = Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f);
        var transform1 = new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One);
        var transform2 = new Transform3D(Vector3.Zero, Quaternion.Identity, new Vector3(2, 2, 2));

        var matrix1 = camera.ViewMatrix(transform1);
        var matrix2 = camera.ViewMatrix(transform2);

        // Scale should not affect view matrix (only position and rotation matter)
        Assert.Equal(matrix1, matrix2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ProjectionAndViewMatrices_Combined_TransformWorldToClipSpace()
    {
        var camera = Camera.CreatePerspective(90f, 1f, 1f, 100f);
        var transform = new Transform3D(
            new Vector3(0, 0, 10),
            Quaternion.Identity,
            Vector3.One);

        var viewMatrix = camera.ViewMatrix(transform);
        var projectionMatrix = camera.ProjectionMatrix();
        var viewProjection = viewMatrix * projectionMatrix;

        // The combined view-projection matrix should be valid (invertible)
        Assert.True(Matrix4x4.Invert(viewProjection, out _));
    }

    [Fact]
    public void ProjectionMatrix_Perspective_ConvertsFieldOfViewToRadians()
    {
        // Test that FOV in degrees is properly converted to radians
        var camera = Camera.CreatePerspective(90f, 1f, 1f, 100f);
        var matrix1 = camera.ProjectionMatrix();

        // 90 degrees should equal PI/2 radians
        // CreatePerspectiveFieldOfView expects radians, so the extension should convert
        var expectedMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2f,
            1f,
            1f,
            100f);

        // Matrices should be approximately equal
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                float value1 = row switch
                {
                    0 => col switch { 0 => matrix1.M11, 1 => matrix1.M12, 2 => matrix1.M13, 3 => matrix1.M14, _ => 0 },
                    1 => col switch { 0 => matrix1.M21, 1 => matrix1.M22, 2 => matrix1.M23, 3 => matrix1.M24, _ => 0 },
                    2 => col switch { 0 => matrix1.M31, 1 => matrix1.M32, 2 => matrix1.M33, 3 => matrix1.M34, _ => 0 },
                    3 => col switch { 0 => matrix1.M41, 1 => matrix1.M42, 2 => matrix1.M43, 3 => matrix1.M44, _ => 0 },
                    _ => 0
                };
                float value2 = row switch
                {
                    0 => col switch { 0 => expectedMatrix.M11, 1 => expectedMatrix.M12, 2 => expectedMatrix.M13, 3 => expectedMatrix.M14, _ => 0 },
                    1 => col switch { 0 => expectedMatrix.M21, 1 => expectedMatrix.M22, 2 => expectedMatrix.M23, 3 => expectedMatrix.M24, _ => 0 },
                    2 => col switch { 0 => expectedMatrix.M31, 1 => expectedMatrix.M32, 2 => expectedMatrix.M33, 3 => expectedMatrix.M34, _ => 0 },
                    3 => col switch { 0 => expectedMatrix.M41, 1 => expectedMatrix.M42, 2 => expectedMatrix.M43, 3 => expectedMatrix.M44, _ => 0 },
                    _ => 0
                };
                Assert.True(Math.Abs(value1 - value2) < Epsilon, $"Matrix mismatch at [{row},{col}]");
            }
        }
    }

    #endregion
}
