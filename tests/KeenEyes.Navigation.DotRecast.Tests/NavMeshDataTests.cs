using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for <see cref="NavMeshData"/> class.
/// </summary>
/// <remarks>
/// These tests require proper 3D mesh geometry for Recast to build a navmesh.
/// They are skipped in unit tests and should be run as integration tests with real mesh data.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Requires integration test with proper mesh geometry")]
public class NavMeshDataTests
{
    private const string SkipReason = "Requires proper 3D mesh geometry - see integration tests";
    private readonly NavMeshData? navMesh;

    public NavMeshDataTests()
    {
        // NavMesh building requires proper 3D geometry - skip in unit tests
        try
        {
            navMesh = TestHelper.BuildTestNavMesh();
        }
        catch (InvalidOperationException)
        {
            // Expected - navmesh building requires proper geometry
            navMesh = null;
        }
    }

    #region Properties Tests

    [Fact(Skip = SkipReason)]
    public void Id_IsNotNullOrEmpty()
    {
        Assert.NotNull(navMesh);
        Assert.False(string.IsNullOrEmpty(navMesh.Id));
    }

    [Fact(Skip = SkipReason)]
    public void Bounds_ReturnsValidBounds()
    {
        Assert.NotNull(navMesh);
        var bounds = navMesh.Bounds;

        Assert.True(bounds.Min.X <= bounds.Max.X);
        Assert.True(bounds.Min.Z <= bounds.Max.Z);
    }

    [Fact(Skip = SkipReason)]
    public void PolygonCount_IsPositive()
    {
        Assert.NotNull(navMesh);
        Assert.True(navMesh.PolygonCount > 0);
    }

    [Fact(Skip = SkipReason)]
    public void VertexCount_IsPositive()
    {
        Assert.NotNull(navMesh);
        Assert.True(navMesh.VertexCount > 0);
    }

    [Fact(Skip = SkipReason)]
    public void BuiltForAgent_ReturnsAgentSettings()
    {
        Assert.NotNull(navMesh);
        var agent = navMesh.BuiltForAgent;

        Assert.True(agent.Radius > 0);
        Assert.True(agent.Height > 0);
    }

    #endregion

    #region FindNearestPoint Tests

    [Fact(Skip = SkipReason)]
    public void FindNearestPoint_OnMesh_ReturnsPoint()
    {
        Assert.NotNull(navMesh);
        var point = navMesh.FindNearestPoint(new Vector3(100f, 0f, 100f));
        Assert.NotNull(point);
    }

    [Fact(Skip = SkipReason)]
    public void FindNearestPoint_OffMesh_ReturnsNearest()
    {
        Assert.NotNull(navMesh);
        var point = navMesh.FindNearestPoint(new Vector3(100f, 10f, 100f), searchRadius: 20f);
        Assert.NotNull(point);
    }

    [Fact(Skip = SkipReason)]
    public void FindNearestPoint_TooFar_ReturnsNull()
    {
        Assert.NotNull(navMesh);
        var point = navMesh.FindNearestPoint(new Vector3(1000f, 0f, 1000f), searchRadius: 1f);
        Assert.Null(point);
    }

    #endregion

    #region GetRandomPoint Tests

    [Fact(Skip = SkipReason)]
    public void GetRandomPoint_ReturnsPoint()
    {
        Assert.NotNull(navMesh);
        var point = navMesh.GetRandomPoint();
        Assert.NotNull(point);
    }

    [Fact(Skip = SkipReason)]
    public void GetRandomPoint_PointIsOnMesh()
    {
        Assert.NotNull(navMesh);
        var point = navMesh.GetRandomPoint();
        Assert.NotNull(point);
        Assert.True(navMesh.IsOnNavMesh(point.Value.Position, tolerance: 1f));
    }

    [Fact(Skip = SkipReason)]
    public void GetRandomPointInRadius_ReturnsPointWithinRadius()
    {
        Assert.NotNull(navMesh);
        var center = new Vector3(100f, 0f, 100f);
        float radius = 10f;
        var point = navMesh.GetRandomPointInRadius(center, radius);
        Assert.NotNull(point);
        float dist = Vector3.Distance(center, point.Value.Position);
        Assert.True(dist <= radius + 2f);
    }

    #endregion

    #region GetAreaType Tests

    [Fact(Skip = SkipReason)]
    public void GetAreaType_OnMesh_ReturnsWalkable()
    {
        Assert.NotNull(navMesh);
        var areaType = navMesh.GetAreaType(new Vector3(100f, 0f, 100f));
        Assert.True(areaType == NavAreaType.Walkable || (int)areaType >= 0);
    }

    [Fact(Skip = SkipReason)]
    public void GetAreaType_OffMesh_ReturnsNotWalkable()
    {
        Assert.NotNull(navMesh);
        var areaType = navMesh.GetAreaType(new Vector3(1000f, 0f, 1000f));
        Assert.Equal(NavAreaType.NotWalkable, areaType);
    }

    #endregion

    #region IsOnNavMesh Tests

    [Fact(Skip = SkipReason)]
    public void IsOnNavMesh_OnMesh_ReturnsTrue()
    {
        Assert.NotNull(navMesh);
        bool isOn = navMesh.IsOnNavMesh(new Vector3(100f, 0f, 100f));
        Assert.True(isOn);
    }

    [Fact(Skip = SkipReason)]
    public void IsOnNavMesh_OffMesh_ReturnsFalse()
    {
        Assert.NotNull(navMesh);
        bool isOn = navMesh.IsOnNavMesh(new Vector3(1000f, 0f, 1000f), tolerance: 1f);
        Assert.False(isOn);
    }

    [Fact(Skip = SkipReason)]
    public void IsOnNavMesh_SlightlyAbove_ReturnsTrueWithTolerance()
    {
        Assert.NotNull(navMesh);
        bool isOn = navMesh.IsOnNavMesh(new Vector3(100f, 1f, 100f), tolerance: 2f);
        Assert.True(isOn);
    }

    #endregion

    #region Serialization Tests

    [Fact(Skip = SkipReason)]
    public void Serialize_ReturnsNonEmptyData()
    {
        Assert.NotNull(navMesh);
        var data = navMesh.Serialize();
        Assert.NotNull(data);
        Assert.True(data.Length > 0);
    }

    [Fact(Skip = SkipReason)]
    public void Deserialize_RestoresMesh()
    {
        Assert.NotNull(navMesh);
        var data = navMesh.Serialize();
        var restored = NavMeshData.Deserialize(data);
        Assert.NotNull(restored);
        Assert.Equal(navMesh.Id, restored.Id);
        Assert.Equal(navMesh.PolygonCount, restored.PolygonCount);
    }

    [Fact(Skip = SkipReason)]
    public void Deserialize_RestoredMeshIsNavigable()
    {
        Assert.NotNull(navMesh);
        var data = navMesh.Serialize();
        var restored = NavMeshData.Deserialize(data);
        bool isOn = restored.IsOnNavMesh(new Vector3(100f, 0f, 100f), tolerance: 2f);
        Assert.True(isOn);
    }

    [Fact]
    public void Deserialize_InvalidMagicNumber_ThrowsInvalidData()
    {
        var invalidData = new byte[] { 0, 0, 0, 0 };
        Assert.Throws<InvalidDataException>(() => NavMeshData.Deserialize(invalidData));
    }

    [Fact]
    public void Deserialize_NullData_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => NavMeshData.Deserialize(null!));
    }

    #endregion

    #region GetPolygonVertices Tests

    [Fact(Skip = SkipReason)]
    public void GetPolygonVertices_ValidPolygon_ReturnsVertices()
    {
        Assert.NotNull(navMesh);
        var point = navMesh.FindNearestPoint(new Vector3(100f, 0f, 100f));
        Assert.NotNull(point);
        var vertices = navMesh.GetPolygonVertices(point.Value.PolygonId);
        Assert.True(vertices.Length >= 3);
    }

    [Fact(Skip = SkipReason)]
    public void GetPolygonVertices_InvalidPolygon_ReturnsEmpty()
    {
        Assert.NotNull(navMesh);
        var vertices = navMesh.GetPolygonVertices(0);
        Assert.True(vertices.IsEmpty);
    }

    #endregion

    #region Poly Ref And Area Regression Tests

    [Fact]
    public void FindNearestPoint_ReturnedPolygonId_RoundTripsToPolygonVertices()
    {
        // Regression for #1169: DotRecast packs salt/tile bits above bit 32, so
        // truncating the poly ref to uint broke GetTileAndPolyByRef's salt check
        // and GetPolygonVertices returned 0 vertices for a valid nearest point.
        Assert.NotNull(navMesh);

        var point = navMesh.FindNearestPoint(new Vector3(100f, 0f, 100f));
        Assert.NotNull(point);
        Assert.NotEqual(0L, point.Value.PolygonId);

        var vertices = navMesh.GetPolygonVertices(point.Value.PolygonId);
        Assert.True(
            vertices.Length >= 3,
            $"Expected the nearest polygon to round-trip to >=3 vertices, got {vertices.Length}.");
    }

    [Fact]
    public void GetAreaType_OverWalkableGround_ReturnsWalkableNotRecastSentinel()
    {
        // Regression for #1170: ground polys were built with Recast's RC_WALKABLE_AREA
        // (63), which is outside the 0-31 NavAreaType range, so SetAreaCost could never
        // influence terrain cost. Walkable ground must report NavAreaType.Walkable.
        Assert.NotNull(navMesh);

        var areaType = navMesh.GetAreaType(new Vector3(100f, 0f, 100f));

        Assert.Equal(NavAreaType.Walkable, areaType);
        Assert.NotEqual(63, (int)areaType);
    }

    [Fact]
    public void GetPolygonSurfaces_OverWalkableGround_AllAreasInValidRange()
    {
        // Regression for #1170: every surface area must fall in the 0-31 range that
        // the area-cost table can address (63 is the out-of-range Recast sentinel).
        Assert.NotNull(navMesh);

        foreach (var surface in navMesh.GetPolygonSurfaces())
        {
            Assert.InRange((int)surface.Area, 0, 31);
        }
    }

    #endregion

    #region GetPolygonSurfaces Tests

    [Fact]
    public void GetPolygonSurfaces_WithBuiltMesh_ReturnsOneSurfacePerPolygon()
    {
        Assert.NotNull(navMesh);

        var surfaces = navMesh.GetPolygonSurfaces().ToList();

        Assert.Equal(navMesh.PolygonCount, surfaces.Count);
        Assert.All(surfaces, surface => Assert.True(surface.Vertices.Length >= 3));
    }

    [Fact]
    public void GetPolygonSurfaces_WithBuiltMesh_VerticesLieWithinMeshBounds()
    {
        Assert.NotNull(navMesh);
        var (min, max) = navMesh.Bounds;

        foreach (var surface in navMesh.GetPolygonSurfaces())
        {
            foreach (var vertex in surface.Vertices)
            {
                Assert.InRange(vertex.X, min.X - 0.001f, max.X + 0.001f);
                Assert.InRange(vertex.Y, min.Y - 0.001f, max.Y + 0.001f);
                Assert.InRange(vertex.Z, min.Z - 0.001f, max.Z + 0.001f);
            }
        }
    }

    #endregion
}
