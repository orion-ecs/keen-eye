using System.Numerics;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for <see cref="MeshAsset"/> and <see cref="MeshVertex"/>.
/// </summary>
public class MeshAssetTests
{
    #region MeshVertex Tests

    [Fact]
    public void MeshVertex_Constructor_SetsAllProperties()
    {
        var position = new Vector3(1, 2, 3);
        var normal = new Vector3(0, 1, 0);
        var texCoord = new Vector2(0.5f, 0.5f);

        var vertex = new MeshVertex(position, normal, texCoord);

        Assert.Equal(position, vertex.Position);
        Assert.Equal(normal, vertex.Normal);
        Assert.Equal(texCoord, vertex.TexCoord);
    }

    [Fact]
    public void MeshVertex_Equality_SameValues_AreEqual()
    {
        var v1 = new MeshVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0, 0));
        var v2 = new MeshVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0, 0));

        Assert.Equal(v1, v2);
        Assert.True(v1 == v2);
    }

    [Fact]
    public void MeshVertex_Equality_DifferentPosition_NotEqual()
    {
        var v1 = new MeshVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0, 0));
        var v2 = new MeshVertex(new Vector3(4, 5, 6), new Vector3(0, 1, 0), new Vector2(0, 0));

        Assert.NotEqual(v1, v2);
        Assert.True(v1 != v2);
    }

    [Fact]
    public void MeshVertex_GetHashCode_SameForEqualVertices()
    {
        var v1 = new MeshVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0.5f, 0.5f));
        var v2 = new MeshVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0.5f, 0.5f));

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }

    [Fact]
    public void MeshVertex_ToString_ContainsValues()
    {
        var vertex = new MeshVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0.5f, 0.5f));

        var str = vertex.ToString();

        Assert.Contains("Position", str);
        Assert.Contains("Normal", str);
        Assert.Contains("TexCoord", str);
    }

    #endregion

    #region MeshAsset Tests

    [Fact]
    public void MeshAsset_Constructor_SetsAllProperties()
    {
        var vertices = new[]
        {
            new MeshVertex(new Vector3(0, 0, 0), Vector3.UnitY, Vector2.Zero),
            new MeshVertex(new Vector3(1, 0, 0), Vector3.UnitY, Vector2.UnitX),
            new MeshVertex(new Vector3(0, 1, 0), Vector3.UnitY, Vector2.UnitY),
        };
        var indices = new uint[] { 0, 1, 2 };
        var boundsMin = new Vector3(0, 0, 0);
        var boundsMax = new Vector3(1, 1, 0);

        using var mesh = new MeshAsset("TestMesh", vertices, indices, boundsMin, boundsMax);

        Assert.Equal("TestMesh", mesh.Name);
        Assert.Equal(vertices, mesh.Vertices);
        Assert.Equal(indices, mesh.Indices);
        Assert.Equal(boundsMin, mesh.BoundsMin);
        Assert.Equal(boundsMax, mesh.BoundsMax);
    }

    [Fact]
    public void MeshAsset_SizeBytes_CalculatesCorrectly()
    {
        var vertices = new[]
        {
            new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            new MeshVertex(Vector3.One, Vector3.UnitY, Vector2.One),
        };
        var indices = new uint[] { 0, 1 };

        using var mesh = new MeshAsset("Test", vertices, indices, Vector3.Zero, Vector3.One);

        // Each vertex: (3 + 3 + 2) floats = 8 floats * 4 bytes = 32 bytes
        // 2 vertices = 64 bytes
        // 2 indices * 4 bytes = 8 bytes
        // Total = 72 bytes
        Assert.Equal(72, mesh.SizeBytes);
    }

    [Fact]
    public void MeshAsset_Create_ComputesBoundsAutomatically()
    {
        var vertices = new[]
        {
            new MeshVertex(new Vector3(-1, -2, -3), Vector3.UnitY, Vector2.Zero),
            new MeshVertex(new Vector3(5, 6, 7), Vector3.UnitY, Vector2.Zero),
            new MeshVertex(new Vector3(2, 3, 4), Vector3.UnitY, Vector2.Zero),
        };
        var indices = new uint[] { 0, 1, 2 };

        using var mesh = MeshAsset.Create("AutoBounds", vertices, indices);

        Assert.Equal(new Vector3(-1, -2, -3), mesh.BoundsMin);
        Assert.Equal(new Vector3(5, 6, 7), mesh.BoundsMax);
    }

    [Fact]
    public void MeshAsset_Dispose_CanBeCalledMultipleTimes()
    {
        var mesh = new MeshAsset("Test", [], [], Vector3.Zero, Vector3.One);

        mesh.Dispose();
        mesh.Dispose(); // Should not throw

        Assert.True(true); // If we got here, no exception was thrown
    }

    [Fact]
    public void MeshAsset_EmptyMesh_HasZeroSize()
    {
        using var mesh = new MeshAsset("Empty", [], [], Vector3.Zero, Vector3.Zero);

        Assert.Equal(0, mesh.SizeBytes);
        Assert.Empty(mesh.Vertices);
        Assert.Empty(mesh.Indices);
    }

    #endregion
}
