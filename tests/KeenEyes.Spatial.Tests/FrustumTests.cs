using System.Numerics;
using KeenEyes.Spatial;
using Xunit;

namespace KeenEyes.Spatial.Tests;

public class FrustumTests
{
    [Fact]
    public void FromMatrix_CreatesValidFrustum()
    {
        // Create a simple perspective projection matrix
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,  // 45 degree FOV
            1.0f,          // Aspect ratio
            0.1f,          // Near plane
            100.0f);       // Far plane

        var view = Matrix4x4.CreateLookAt(
            new Vector3(0, 0, -10),
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0));

        var viewProjection = view * projection;

        var frustum = Frustum.FromMatrix(viewProjection);

        // Frustum should have non-zero planes
        Assert.NotEqual(default(Plane), frustum.Near);
        Assert.NotEqual(default(Plane), frustum.Far);
        Assert.NotEqual(default(Plane), frustum.Left);
        Assert.NotEqual(default(Plane), frustum.Right);
        Assert.NotEqual(default(Plane), frustum.Top);
        Assert.NotEqual(default(Plane), frustum.Bottom);
    }

    [Fact]
    public void Contains_PointInsideFrustum_ReturnsTrue()
    {
        var frustum = CreateSimpleFrustum();

        // Point at origin should be inside
        var pointInside = new Vector3(0, 0, 0);

        Assert.True(frustum.Contains(pointInside));
    }

    [Fact]
    public void Contains_PointOutsideFrustum_ReturnsFalse()
    {
        var frustum = CreateSimpleFrustum();

        // Point far behind camera
        var pointOutside = new Vector3(0, 0, 100);

        Assert.False(frustum.Contains(pointOutside));
    }

    [Fact]
    public void Intersects_AABBInsideFrustum_ReturnsTrue()
    {
        var frustum = CreateSimpleFrustum();

        // Small box at origin
        var min = new Vector3(-1, -1, -1);
        var max = new Vector3(1, 1, 1);

        Assert.True(frustum.Intersects(min, max));
    }

    [Fact]
    public void Intersects_AABBOutsideFrustum_ReturnsFalse()
    {
        var frustum = CreateSimpleFrustum();

        // Box far behind camera
        var min = new Vector3(-1, -1, 99);
        var max = new Vector3(1, 1, 101);

        Assert.False(frustum.Intersects(min, max));
    }

    [Fact]
    public void Intersects_SphereInsideFrustum_ReturnsTrue()
    {
        var frustum = CreateSimpleFrustum();

        // Sphere at origin
        var center = new Vector3(0, 0, 0);
        var radius = 1.0f;

        Assert.True(frustum.Intersects(center, radius));
    }

    [Fact]
    public void Intersects_SphereOutsideFrustum_ReturnsFalse()
    {
        var frustum = CreateSimpleFrustum();

        // Sphere far behind camera
        var center = new Vector3(0, 0, 100);
        var radius = 1.0f;

        Assert.False(frustum.Intersects(center, radius));
    }

    [Fact]
    public void Intersects_SphereTouchingFrustum_ReturnsTrue()
    {
        var frustum = CreateSimpleFrustum();

        // Sphere at edge of frustum (just touching)
        var center = new Vector3(5, 0, 0); // To the right
        var radius = 10.0f; // Large enough to touch frustum

        Assert.True(frustum.Intersects(center, radius));
    }

    /// <summary>
    /// Creates a simple frustum for testing purposes.
    /// </summary>
    private static Frustum CreateSimpleFrustum()
    {
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,  // 45 degree FOV
            1.0f,          // Aspect ratio
            0.1f,          // Near plane
            100.0f);       // Far plane

        var view = Matrix4x4.CreateLookAt(
            new Vector3(0, 0, -10),
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0));

        return Frustum.FromMatrix(view * projection);
    }
}
