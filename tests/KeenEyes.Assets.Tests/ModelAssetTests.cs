using System.Numerics;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for ModelAsset, MaterialData, TextureData, and related types.
/// </summary>
public class ModelAssetTests
{
    #region AlphaMode Tests

    [Fact]
    public void AlphaMode_HasExpectedValues()
    {
        Assert.Equal(0, (int)AlphaMode.Opaque);
        Assert.Equal(1, (int)AlphaMode.Mask);
        Assert.Equal(2, (int)AlphaMode.Blend);
    }

    #endregion

    #region TextureData Tests

    [Fact]
    public void TextureData_StoresProperties()
    {
        var pixels = new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 }; // 2 RGBA pixels
        using var texture = new TextureData("test", pixels, 2, 1, 4, "/path/to/texture.png");

        Assert.Equal("test", texture.Name);
        Assert.Equal(pixels, texture.Pixels);
        Assert.Equal(2, texture.Width);
        Assert.Equal(1, texture.Height);
        Assert.Equal(4, texture.Components);
        Assert.Equal("/path/to/texture.png", texture.SourcePath);
    }

    [Fact]
    public void TextureData_SizeBytes_ReturnsPixelArrayLength()
    {
        var pixels = new byte[256 * 128 * 4]; // 256x128 RGBA
        using var texture = new TextureData("test", pixels, 256, 128, 4);

        Assert.Equal(256 * 128 * 4, texture.SizeBytes);
    }

    [Fact]
    public void TextureData_HasAlpha_ReturnsCorrectly()
    {
        var rgbaPixels = new byte[4];
        var rgbPixels = new byte[3];

        using var rgbaTexture = new TextureData("rgba", rgbaPixels, 1, 1, 4);
        using var rgbTexture = new TextureData("rgb", rgbPixels, 1, 1, 3);

        Assert.True(rgbaTexture.HasAlpha);
        Assert.False(rgbTexture.HasAlpha);
    }

    [Fact]
    public void TextureData_SourcePath_NullForEmbedded()
    {
        var pixels = new byte[4];
        using var texture = new TextureData("embedded", pixels, 1, 1, 4);

        Assert.Null(texture.SourcePath);
    }

    [Fact]
    public void TextureData_AsSpan_ReturnsPixelData()
    {
        var pixels = new byte[] { 1, 2, 3, 4 };
        using var texture = new TextureData("test", pixels, 1, 1, 4);

        var span = texture.AsSpan();

        Assert.Equal(4, span.Length);
        Assert.Equal(1, span[0]);
        Assert.Equal(4, span[3]);
    }

    [Fact]
    public void TextureData_AsMemory_ReturnsPixelData()
    {
        var pixels = new byte[] { 1, 2, 3, 4 };
        using var texture = new TextureData("test", pixels, 1, 1, 4);

        var memory = texture.AsMemory();

        Assert.Equal(4, memory.Length);
    }

    [Fact]
    public void TextureData_Dispose_IsIdempotent()
    {
        var texture = new TextureData("test", [1, 2, 3, 4], 1, 1, 4);
        texture.Dispose();
        texture.Dispose(); // Should not throw
    }

    #endregion

    #region MaterialData Tests

    [Fact]
    public void MaterialData_Default_HasExpectedValues()
    {
        var material = MaterialData.Default;

        Assert.Equal("Default", material.Name);
        Assert.Equal(Vector4.One, material.BaseColorFactor);
        Assert.Equal(0f, material.MetallicFactor);
        Assert.Equal(0.5f, material.RoughnessFactor);
        Assert.Equal(Vector3.Zero, material.EmissiveFactor);
        Assert.Equal(0.5f, material.AlphaCutoff);
        Assert.Equal(AlphaMode.Opaque, material.AlphaMode);
        Assert.False(material.DoubleSided);
        Assert.Equal(-1, material.BaseColorTextureIndex);
        Assert.Equal(-1, material.NormalTextureIndex);
        Assert.Equal(-1, material.MetallicRoughnessTextureIndex);
        Assert.Equal(-1, material.OcclusionTextureIndex);
        Assert.Equal(-1, material.EmissiveTextureIndex);
        Assert.Equal(1.0f, material.NormalScale);
        Assert.Equal(1.0f, material.OcclusionStrength);
    }

    [Fact]
    public void MaterialData_HasTextureProperties_ReturnCorrectly()
    {
        var withTextures = new MaterialData(
            "WithTextures",
            Vector4.One, 0.5f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Opaque, false,
            0, 1, 2, 3, 4);

        var withoutTextures = MaterialData.Default;

        Assert.True(withTextures.HasBaseColorTexture);
        Assert.True(withTextures.HasNormalTexture);
        Assert.True(withTextures.HasMetallicRoughnessTexture);
        Assert.True(withTextures.HasOcclusionTexture);
        Assert.True(withTextures.HasEmissiveTexture);
        Assert.True(withTextures.HasAnyTexture);

        Assert.False(withoutTextures.HasBaseColorTexture);
        Assert.False(withoutTextures.HasNormalTexture);
        Assert.False(withoutTextures.HasMetallicRoughnessTexture);
        Assert.False(withoutTextures.HasOcclusionTexture);
        Assert.False(withoutTextures.HasEmissiveTexture);
        Assert.False(withoutTextures.HasAnyTexture);
    }

    [Fact]
    public void MaterialData_RequiresBlending_ReturnsTrueForBlendMode()
    {
        var blendMaterial = new MaterialData(
            "Blend", Vector4.One, 0f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Blend, false,
            -1, -1, -1, -1, -1);

        var opaqueMaterial = MaterialData.Default;
        var maskMaterial = new MaterialData(
            "Mask", Vector4.One, 0f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Mask, false,
            -1, -1, -1, -1, -1);

        Assert.True(blendMaterial.RequiresBlending);
        Assert.False(opaqueMaterial.RequiresBlending);
        Assert.False(maskMaterial.RequiresBlending);
    }

    [Fact]
    public void MaterialData_RequiresAlphaTest_ReturnsTrueForMaskMode()
    {
        var maskMaterial = new MaterialData(
            "Mask", Vector4.One, 0f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Mask, false,
            -1, -1, -1, -1, -1);

        var opaqueMaterial = MaterialData.Default;
        var blendMaterial = new MaterialData(
            "Blend", Vector4.One, 0f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Blend, false,
            -1, -1, -1, -1, -1);

        Assert.True(maskMaterial.RequiresAlphaTest);
        Assert.False(opaqueMaterial.RequiresAlphaTest);
        Assert.False(blendMaterial.RequiresAlphaTest);
    }

    [Fact]
    public void MaterialData_Equality_WorksCorrectly()
    {
        var material1 = new MaterialData(
            "Test", Vector4.One, 0.5f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Opaque, false,
            0, 1, 2, 3, 4);

        var material2 = new MaterialData(
            "Test", Vector4.One, 0.5f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Opaque, false,
            0, 1, 2, 3, 4);

        var material3 = new MaterialData(
            "Different", Vector4.One, 0.5f, 0.5f, Vector3.Zero, 0.5f,
            AlphaMode.Opaque, false,
            0, 1, 2, 3, 4);

        Assert.Equal(material1, material2);
        Assert.NotEqual(material1, material3);
    }

    [Fact]
    public void MaterialData_WithExpression_CreatesModifiedCopy()
    {
        var original = MaterialData.Default;
        var modified = original with { MetallicFactor = 1.0f, RoughnessFactor = 0.0f };

        Assert.Equal(0f, original.MetallicFactor);
        Assert.Equal(0.5f, original.RoughnessFactor);

        Assert.Equal(1.0f, modified.MetallicFactor);
        Assert.Equal(0.0f, modified.RoughnessFactor);
    }

    #endregion

    #region ModelAsset Tests

    [Fact]
    public void ModelAsset_StoresProperties()
    {
        var meshes = new[] { CreateTestMesh("Mesh1"), CreateTestMesh("Mesh2") };
        var materials = new[] { MaterialData.Default };
        var textures = Array.Empty<TextureData>();

        using var model = new ModelAsset("TestModel", meshes, materials, textures);

        Assert.Equal("TestModel", model.Name);
        Assert.Equal(2, model.Meshes.Length);
        Assert.Single(model.Materials);
        Assert.Empty(model.Textures);
    }

    [Fact]
    public void ModelAsset_SizeBytes_IncludesMeshesAndTextures()
    {
        var meshes = new[] { CreateTestMesh("Mesh1") };
        var materials = new[] { MaterialData.Default };
        var pixels = new byte[64 * 64 * 4];
        var textures = new[] { new TextureData("Tex", pixels, 64, 64, 4) };

        using var model = new ModelAsset("Test", meshes, materials, textures);

        // Mesh size + texture size + material estimate (100 bytes each)
        var expectedMinSize = meshes[0].SizeBytes + textures[0].SizeBytes + 100;
        Assert.True(model.SizeBytes >= expectedMinSize);
    }

    [Fact]
    public void ModelAsset_BoundsMin_ReturnsMinOfAllMeshes()
    {
        var mesh1 = CreateTestMeshWithBounds(new Vector3(-1, -2, -3), new Vector3(0, 0, 0));
        var mesh2 = CreateTestMeshWithBounds(new Vector3(-5, 0, -1), new Vector3(1, 1, 1));

        using var model = new ModelAsset("Test", [mesh1, mesh2], [], []);

        Assert.Equal(new Vector3(-5, -2, -3), model.BoundsMin);
    }

    [Fact]
    public void ModelAsset_BoundsMax_ReturnsMaxOfAllMeshes()
    {
        var mesh1 = CreateTestMeshWithBounds(new Vector3(-1, -2, -3), new Vector3(5, 3, 2));
        var mesh2 = CreateTestMeshWithBounds(new Vector3(0, 0, 0), new Vector3(2, 4, 1));

        using var model = new ModelAsset("Test", [mesh1, mesh2], [], []);

        Assert.Equal(new Vector3(5, 4, 2), model.BoundsMax);
    }

    [Fact]
    public void ModelAsset_BoundsWithNoMeshes_ReturnsZero()
    {
        using var model = new ModelAsset("Empty", [], [], []);

        Assert.Equal(Vector3.Zero, model.BoundsMin);
        Assert.Equal(Vector3.Zero, model.BoundsMax);
    }

    [Fact]
    public void ModelAsset_GetMaterial_ReturnsCorrectMaterial()
    {
        var materials = new[]
        {
            MaterialData.Default with { Name = "Mat0" },
            MaterialData.Default with { Name = "Mat1" }
        };

        using var model = new ModelAsset("Test", [], materials, []);

        Assert.Equal("Mat0", model.GetMaterial(0).Name);
        Assert.Equal("Mat1", model.GetMaterial(1).Name);
    }

    [Fact]
    public void ModelAsset_GetMaterial_ReturnsDefaultForInvalidIndex()
    {
        using var model = new ModelAsset("Test", [], [], []);

        Assert.Equal(MaterialData.Default, model.GetMaterial(-1));
        Assert.Equal(MaterialData.Default, model.GetMaterial(0));
        Assert.Equal(MaterialData.Default, model.GetMaterial(100));
    }

    [Fact]
    public void ModelAsset_GetTexture_ReturnsCorrectTexture()
    {
        var textures = new[]
        {
            new TextureData("Tex0", [1, 2, 3, 4], 1, 1, 4),
            new TextureData("Tex1", [5, 6, 7, 8], 1, 1, 4)
        };

        using var model = new ModelAsset("Test", [], [], textures);

        Assert.Equal("Tex0", model.GetTexture(0)?.Name);
        Assert.Equal("Tex1", model.GetTexture(1)?.Name);
    }

    [Fact]
    public void ModelAsset_GetTexture_ReturnsNullForInvalidIndex()
    {
        using var model = new ModelAsset("Test", [], [], []);

        Assert.Null(model.GetTexture(-1));
        Assert.Null(model.GetTexture(0));
        Assert.Null(model.GetTexture(100));
    }

    [Fact]
    public void ModelAsset_Dispose_DisposesAllResources()
    {
        var meshes = new[] { CreateTestMesh("Mesh") };
        var textures = new[] { new TextureData("Tex", [1, 2, 3, 4], 1, 1, 4) };

        var model = new ModelAsset("Test", meshes, [], textures);
        model.Dispose();

        // Verify dispose is idempotent
        model.Dispose(); // Should not throw
    }

    #endregion

    #region ModelLoader Tests

    [Fact]
    public void ModelLoader_Extensions_IncludesGltfAndGlb()
    {
        var loader = new ModelLoader();

        Assert.Contains(".gltf", loader.Extensions);
        Assert.Contains(".glb", loader.Extensions);
    }

    [Fact]
    public void ModelLoader_EstimateSize_ReturnsModelSizeBytes()
    {
        var loader = new ModelLoader();
        using var model = new ModelAsset("Test", [], [], []);

        var estimate = loader.EstimateSize(model);

        Assert.Equal(model.SizeBytes, estimate);
    }

    #endregion

    #region Helper Methods

    private static MeshAsset CreateTestMesh(string name)
    {
        var vertices = new[]
        {
            MeshVertex.CreateBasic(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            MeshVertex.CreateBasic(Vector3.UnitX, Vector3.UnitY, Vector2.UnitX),
            MeshVertex.CreateBasic(Vector3.UnitZ, Vector3.UnitY, Vector2.UnitY)
        };
        var indices = new uint[] { 0, 1, 2 };

        return MeshAsset.Create(name, vertices, indices);
    }

    private static MeshAsset CreateTestMeshWithBounds(Vector3 boundsMin, Vector3 boundsMax)
    {
        var vertices = new[]
        {
            MeshVertex.CreateBasic(boundsMin, Vector3.UnitY, Vector2.Zero),
            MeshVertex.CreateBasic(boundsMax, Vector3.UnitY, Vector2.One)
        };
        var indices = new uint[] { 0, 1 };
        var submeshes = new[] { new Submesh(0, 2, -1) };

        return new MeshAsset("TestMesh", vertices, indices, submeshes, boundsMin, boundsMax);
    }

    #endregion
}
