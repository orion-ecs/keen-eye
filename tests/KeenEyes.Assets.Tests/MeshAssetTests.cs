using System.Numerics;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for <see cref="MeshAsset"/> and <see cref="MeshVertex"/>.
/// </summary>
public class MeshAssetTests
{
    #region Helper Methods

    private static MeshVertex CreateBasicVertex(Vector3 position, Vector3 normal, Vector2 texCoord)
        => MeshVertex.CreateBasic(position, normal, texCoord);

    #endregion

    #region MeshVertex Tests

    [Fact]
    public void MeshVertex_Constructor_SetsAllProperties()
    {
        var position = new Vector3(1, 2, 3);
        var normal = new Vector3(0, 1, 0);
        var texCoord = new Vector2(0.5f, 0.5f);
        var tangent = new Vector4(1, 0, 0, 1);
        var color = new Vector4(1, 0, 0, 1);
        var joints = new JointIndices(0, 1, 2, 3);
        var weights = new Vector4(0.5f, 0.3f, 0.2f, 0);

        var vertex = new MeshVertex(position, normal, texCoord, tangent, color, joints, weights);

        Assert.Equal(position, vertex.Position);
        Assert.Equal(normal, vertex.Normal);
        Assert.Equal(texCoord, vertex.TexCoord);
        Assert.Equal(tangent, vertex.Tangent);
        Assert.Equal(color, vertex.Color);
        Assert.Equal(joints, vertex.Joints);
        Assert.Equal(weights, vertex.Weights);
    }

    [Fact]
    public void MeshVertex_CreateBasic_SetsDefaults()
    {
        var position = new Vector3(1, 2, 3);
        var normal = new Vector3(0, 1, 0);
        var texCoord = new Vector2(0.5f, 0.5f);

        var vertex = MeshVertex.CreateBasic(position, normal, texCoord);

        Assert.Equal(position, vertex.Position);
        Assert.Equal(normal, vertex.Normal);
        Assert.Equal(texCoord, vertex.TexCoord);
        Assert.Equal(new Vector4(1, 0, 0, 1), vertex.Tangent);
        Assert.Equal(Vector4.One, vertex.Color);
        Assert.Equal(JointIndices.Default, vertex.Joints);
        Assert.Equal(new Vector4(1, 0, 0, 0), vertex.Weights);
    }

    [Fact]
    public void MeshVertex_Equality_SameValues_AreEqual()
    {
        var v1 = CreateBasicVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0, 0));
        var v2 = CreateBasicVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0, 0));

        Assert.Equal(v1, v2);
        Assert.True(v1 == v2);
    }

    [Fact]
    public void MeshVertex_Equality_DifferentPosition_NotEqual()
    {
        var v1 = CreateBasicVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0, 0));
        var v2 = CreateBasicVertex(new Vector3(4, 5, 6), new Vector3(0, 1, 0), new Vector2(0, 0));

        Assert.NotEqual(v1, v2);
        Assert.True(v1 != v2);
    }

    [Fact]
    public void MeshVertex_GetHashCode_SameForEqualVertices()
    {
        var v1 = CreateBasicVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0.5f, 0.5f));
        var v2 = CreateBasicVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0.5f, 0.5f));

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }

    [Fact]
    public void MeshVertex_ToString_ContainsValues()
    {
        var vertex = CreateBasicVertex(new Vector3(1, 2, 3), new Vector3(0, 1, 0), new Vector2(0.5f, 0.5f));

        var str = vertex.ToString();

        Assert.Contains("Position", str);
        Assert.Contains("Normal", str);
        Assert.Contains("TexCoord", str);
        Assert.Contains("Tangent", str);
        Assert.Contains("Color", str);
    }

    [Fact]
    public void MeshVertex_Equality_DifferentTangent_NotEqual()
    {
        var joints = JointIndices.Default;
        var weights = new Vector4(1, 0, 0, 0);

        var v1 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(1, 0, 0, 1), Vector4.One, joints, weights);
        var v2 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(0, 1, 0, -1), Vector4.One, joints, weights);

        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void MeshVertex_Equality_DifferentColor_NotEqual()
    {
        var joints = JointIndices.Default;
        var weights = new Vector4(1, 0, 0, 0);

        var v1 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1), joints, weights);
        var v2 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1), joints, weights);

        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void MeshVertex_Equality_DifferentJoints_NotEqual()
    {
        var weights = new Vector4(1, 0, 0, 0);

        var v1 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(1, 0, 0, 1), Vector4.One, new JointIndices(0, 1, 2, 3), weights);
        var v2 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(1, 0, 0, 1), Vector4.One, new JointIndices(4, 5, 6, 7), weights);

        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void MeshVertex_Equality_DifferentWeights_NotEqual()
    {
        var joints = JointIndices.Default;

        var v1 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(1, 0, 0, 1), Vector4.One, joints, new Vector4(1, 0, 0, 0));
        var v2 = new MeshVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero,
            new Vector4(1, 0, 0, 1), Vector4.One, joints, new Vector4(0.5f, 0.3f, 0.2f, 0));

        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void MeshVertex_FullPBRVertex_StoresAllAttributes()
    {
        var position = new Vector3(1, 2, 3);
        var normal = Vector3.UnitZ;
        var texCoord = new Vector2(0.25f, 0.75f);
        var tangent = new Vector4(1, 0, 0, -1);
        var color = new Vector4(0.8f, 0.2f, 0.1f, 1.0f);
        var joints = new JointIndices(5, 10, 15, 20);
        var weights = new Vector4(0.4f, 0.3f, 0.2f, 0.1f);

        var vertex = new MeshVertex(position, normal, texCoord, tangent, color, joints, weights);

        Assert.Equal(position, vertex.Position);
        Assert.Equal(normal, vertex.Normal);
        Assert.Equal(texCoord, vertex.TexCoord);
        Assert.Equal(tangent, vertex.Tangent);
        Assert.Equal(color, vertex.Color);
        Assert.Equal(joints, vertex.Joints);
        Assert.Equal(weights, vertex.Weights);
    }

    [Fact]
    public void MeshVertex_CreateBasic_TangentHasPositiveBitangentSign()
    {
        var vertex = MeshVertex.CreateBasic(Vector3.Zero, Vector3.UnitY, Vector2.Zero);

        Assert.Equal(1.0f, vertex.Tangent.W);
    }

    [Fact]
    public void MeshVertex_CreateBasic_WeightsFullyOnFirstJoint()
    {
        var vertex = MeshVertex.CreateBasic(Vector3.Zero, Vector3.UnitY, Vector2.Zero);

        Assert.Equal(1.0f, vertex.Weights.X);
        Assert.Equal(0.0f, vertex.Weights.Y);
        Assert.Equal(0.0f, vertex.Weights.Z);
        Assert.Equal(0.0f, vertex.Weights.W);
    }

    #endregion

    #region JointIndices Tests

    [Fact]
    public void JointIndices_Default_IsAllZeros()
    {
        var joints = JointIndices.Default;

        Assert.Equal((ushort)0, joints.Joint0);
        Assert.Equal((ushort)0, joints.Joint1);
        Assert.Equal((ushort)0, joints.Joint2);
        Assert.Equal((ushort)0, joints.Joint3);
    }

    [Fact]
    public void JointIndices_Constructor_SetsAllValues()
    {
        var joints = new JointIndices(1, 2, 3, 4);

        Assert.Equal((ushort)1, joints.Joint0);
        Assert.Equal((ushort)2, joints.Joint1);
        Assert.Equal((ushort)3, joints.Joint2);
        Assert.Equal((ushort)4, joints.Joint3);
    }

    [Fact]
    public void JointIndices_Equality_SameValues_AreEqual()
    {
        var j1 = new JointIndices(1, 2, 3, 4);
        var j2 = new JointIndices(1, 2, 3, 4);

        Assert.Equal(j1, j2);
        Assert.True(j1 == j2);
    }

    [Fact]
    public void JointIndices_Equality_DifferentValues_NotEqual()
    {
        var j1 = new JointIndices(1, 2, 3, 4);
        var j2 = new JointIndices(5, 6, 7, 8);

        Assert.NotEqual(j1, j2);
        Assert.True(j1 != j2);
    }

    [Fact]
    public void JointIndices_GetHashCode_SameForEqualValues()
    {
        var j1 = new JointIndices(10, 20, 30, 40);
        var j2 = new JointIndices(10, 20, 30, 40);

        Assert.Equal(j1.GetHashCode(), j2.GetHashCode());
    }

    [Fact]
    public void JointIndices_MaxValue_HandlesFullRange()
    {
        var joints = new JointIndices(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue);

        Assert.Equal(ushort.MaxValue, joints.Joint0);
        Assert.Equal(ushort.MaxValue, joints.Joint1);
        Assert.Equal(ushort.MaxValue, joints.Joint2);
        Assert.Equal(ushort.MaxValue, joints.Joint3);
    }

    #endregion

    #region Submesh Tests

    [Fact]
    public void Submesh_Constructor_SetsAllProperties()
    {
        var submesh = new Submesh(10, 100, 5);

        Assert.Equal(10, submesh.StartIndex);
        Assert.Equal(100, submesh.IndexCount);
        Assert.Equal(5, submesh.MaterialIndex);
    }

    [Fact]
    public void Submesh_NegativeMaterialIndex_RepresentsNoMaterial()
    {
        var submesh = new Submesh(0, 50, -1);

        Assert.Equal(-1, submesh.MaterialIndex);
    }

    [Fact]
    public void Submesh_Equality_SameValues_AreEqual()
    {
        var s1 = new Submesh(10, 100, 5);
        var s2 = new Submesh(10, 100, 5);

        Assert.Equal(s1, s2);
        Assert.True(s1 == s2);
    }

    [Fact]
    public void Submesh_Equality_DifferentStartIndex_NotEqual()
    {
        var s1 = new Submesh(10, 100, 5);
        var s2 = new Submesh(20, 100, 5);

        Assert.NotEqual(s1, s2);
    }

    [Fact]
    public void Submesh_Equality_DifferentIndexCount_NotEqual()
    {
        var s1 = new Submesh(10, 100, 5);
        var s2 = new Submesh(10, 200, 5);

        Assert.NotEqual(s1, s2);
    }

    [Fact]
    public void Submesh_Equality_DifferentMaterialIndex_NotEqual()
    {
        var s1 = new Submesh(10, 100, 5);
        var s2 = new Submesh(10, 100, 6);

        Assert.NotEqual(s1, s2);
    }

    [Fact]
    public void Submesh_GetHashCode_SameForEqualValues()
    {
        var s1 = new Submesh(10, 100, 5);
        var s2 = new Submesh(10, 100, 5);

        Assert.Equal(s1.GetHashCode(), s2.GetHashCode());
    }

    #endregion

    #region MeshAsset Tests

    [Fact]
    public void MeshAsset_Constructor_SetsAllProperties()
    {
        var vertices = new[]
        {
            CreateBasicVertex(new Vector3(0, 0, 0), Vector3.UnitY, Vector2.Zero),
            CreateBasicVertex(new Vector3(1, 0, 0), Vector3.UnitY, Vector2.UnitX),
            CreateBasicVertex(new Vector3(0, 1, 0), Vector3.UnitY, Vector2.UnitY),
        };
        var indices = new uint[] { 0, 1, 2 };
        var submeshes = new[] { new Submesh(0, 3, 0) };
        var boundsMin = new Vector3(0, 0, 0);
        var boundsMax = new Vector3(1, 1, 0);

        using var mesh = new MeshAsset("TestMesh", vertices, indices, submeshes, boundsMin, boundsMax);

        Assert.Equal("TestMesh", mesh.Name);
        Assert.Equal(vertices, mesh.Vertices);
        Assert.Equal(indices, mesh.Indices);
        Assert.Equal(submeshes, mesh.Submeshes);
        Assert.Equal(boundsMin, mesh.BoundsMin);
        Assert.Equal(boundsMax, mesh.BoundsMax);
    }

    [Fact]
    public void MeshAsset_SizeBytes_CalculatesCorrectly()
    {
        var vertices = new[]
        {
            CreateBasicVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            CreateBasicVertex(Vector3.One, Vector3.UnitY, Vector2.One),
        };
        var indices = new uint[] { 0, 1 };
        var submeshes = new[] { new Submesh(0, 2, -1) };

        using var mesh = new MeshAsset("Test", vertices, indices, submeshes, Vector3.Zero, Vector3.One);

        // Each vertex: (3+3+2+4+4+4) floats = 20 floats * 4 bytes = 80 bytes
        // Plus joints: 4 ushorts * 2 bytes = 8 bytes per vertex
        // 2 vertices = (80 + 8) * 2 = 176 bytes
        // 2 indices * 4 bytes = 8 bytes
        // 1 submesh * 3 ints * 4 bytes = 12 bytes
        // Total = 176 + 8 + 12 = 196 bytes
        Assert.Equal(196, mesh.SizeBytes);
    }

    [Fact]
    public void MeshAsset_Create_ComputesBoundsAutomatically()
    {
        var vertices = new[]
        {
            CreateBasicVertex(new Vector3(-1, -2, -3), Vector3.UnitY, Vector2.Zero),
            CreateBasicVertex(new Vector3(5, 6, 7), Vector3.UnitY, Vector2.Zero),
            CreateBasicVertex(new Vector3(2, 3, 4), Vector3.UnitY, Vector2.Zero),
        };
        var indices = new uint[] { 0, 1, 2 };

        using var mesh = MeshAsset.Create("AutoBounds", vertices, indices);

        Assert.Equal(new Vector3(-1, -2, -3), mesh.BoundsMin);
        Assert.Equal(new Vector3(5, 6, 7), mesh.BoundsMax);
    }

    [Fact]
    public void MeshAsset_Create_CreatesSingleSubmesh()
    {
        var vertices = new[]
        {
            CreateBasicVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            CreateBasicVertex(Vector3.One, Vector3.UnitY, Vector2.One),
        };
        var indices = new uint[] { 0, 1, 0 };

        using var mesh = MeshAsset.Create("Test", vertices, indices);

        Assert.Single(mesh.Submeshes);
        Assert.Equal(0, mesh.Submeshes[0].StartIndex);
        Assert.Equal(3, mesh.Submeshes[0].IndexCount);
        Assert.Equal(-1, mesh.Submeshes[0].MaterialIndex);
    }

    [Fact]
    public void MeshAsset_CreateWithSubmeshes_UsesProvidedSubmeshes()
    {
        var vertices = new[]
        {
            CreateBasicVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            CreateBasicVertex(Vector3.One, Vector3.UnitY, Vector2.One),
            CreateBasicVertex(Vector3.UnitX, Vector3.UnitY, Vector2.UnitX),
        };
        var indices = new uint[] { 0, 1, 2, 0, 2, 1 };
        var submeshes = new[]
        {
            new Submesh(0, 3, 0),
            new Submesh(3, 3, 1),
        };

        using var mesh = MeshAsset.Create("MultiSubmesh", vertices, indices, submeshes);

        Assert.Equal(2, mesh.Submeshes.Length);
        Assert.Equal(0, mesh.Submeshes[0].MaterialIndex);
        Assert.Equal(1, mesh.Submeshes[1].MaterialIndex);
    }

    [Fact]
    public void MeshAsset_Dispose_CanBeCalledMultipleTimes()
    {
        var mesh = new MeshAsset("Test", [], [], [], Vector3.Zero, Vector3.One);

        mesh.Dispose();
        mesh.Dispose(); // Should not throw

        Assert.True(true); // If we got here, no exception was thrown
    }

    [Fact]
    public void MeshAsset_EmptyMesh_HasZeroSize()
    {
        using var mesh = new MeshAsset("Empty", [], [], [], Vector3.Zero, Vector3.Zero);

        Assert.Equal(0, mesh.SizeBytes);
        Assert.Empty(mesh.Vertices);
        Assert.Empty(mesh.Indices);
        Assert.Empty(mesh.Submeshes);
    }

    [Fact]
    public void MeshAsset_MultipleSubmeshes_SizeBytesIncludesAll()
    {
        var vertices = new[]
        {
            CreateBasicVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            CreateBasicVertex(Vector3.One, Vector3.UnitY, Vector2.One),
        };
        var indices = new uint[] { 0, 1 };
        var submeshes = new[]
        {
            new Submesh(0, 1, 0),
            new Submesh(1, 1, 1),
            new Submesh(0, 2, 2),
        };

        using var mesh = new MeshAsset("Test", vertices, indices, submeshes, Vector3.Zero, Vector3.One);

        // Each vertex: (3+3+2+4+4+4) floats = 20 floats * 4 bytes = 80 bytes
        // Plus joints: 4 ushorts * 2 bytes = 8 bytes per vertex
        // 2 vertices = (80 + 8) * 2 = 176 bytes
        // 2 indices * 4 bytes = 8 bytes
        // 3 submeshes * 3 ints * 4 bytes = 36 bytes
        // Total = 176 + 8 + 36 = 220 bytes
        Assert.Equal(220, mesh.SizeBytes);
    }

    #endregion

    #region MeshLoader Tests

    [Fact]
    public void MeshLoader_Extensions_ContainsGltf()
    {
        var loader = new MeshLoader();

        Assert.Contains(".gltf", loader.Extensions);
    }

    [Fact]
    public void MeshLoader_Extensions_ContainsGlb()
    {
        var loader = new MeshLoader();

        Assert.Contains(".glb", loader.Extensions);
    }

    [Fact]
    public void MeshLoader_EstimateSize_ReturnsMeshSizeBytes()
    {
        var loader = new MeshLoader();
        var vertices = new[]
        {
            CreateBasicVertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
        };
        var indices = new uint[] { 0 };
        var submeshes = new[] { new Submesh(0, 1, -1) };
        using var mesh = new MeshAsset("Test", vertices, indices, submeshes, Vector3.Zero, Vector3.Zero);

        var size = loader.EstimateSize(mesh);

        Assert.Equal(mesh.SizeBytes, size);
    }

    #endregion
}
