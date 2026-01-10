using System.Numerics;

using KeenEyes.Graphics.Abstractions;

using SharpGLTF.Schema2;

using StbImageSharp;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for complete 3D models from glTF/GLB files.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="MeshLoader"/> which extracts only the first mesh, <see cref="ModelLoader"/>
/// extracts the complete model including:
/// </para>
/// <list type="bullet">
/// <item><description>All meshes in the file with full PBR vertex data</description></item>
/// <item><description>All materials with PBR properties (metallic-roughness workflow)</description></item>
/// <item><description>Embedded textures (base64 decoded from glTF)</description></item>
/// <item><description>External texture references (paths resolved relative to model)</description></item>
/// </list>
/// <para>
/// Use <see cref="ModelLoader"/> when you need access to materials and textures.
/// Use <see cref="MeshLoader"/> for simple mesh-only loading.
/// </para>
/// </remarks>
public sealed class ModelLoader : IAssetLoader<ModelAsset>
{
    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".gltf", ".glb"];

    /// <inheritdoc />
    public ModelAsset Load(Stream stream, AssetLoadContext context)
    {
        // Read the entire stream into memory (SharpGLTF needs random access)
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        // Load the glTF model
        var model = ModelRoot.ReadGLB(memoryStream);

        // Extract all components
        var textures = ExtractTextures(model, context);
        var materials = ExtractMaterials(model, textures);
        var meshes = ExtractMeshes(model);

        // Get model name from context path
        var name = Path.GetFileNameWithoutExtension(context.Path) ?? "Model";

        return new ModelAsset(name, meshes, materials, textures);
    }

    /// <inheritdoc />
    public async Task<ModelAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // glTF parsing is CPU-bound, so run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(ModelAsset asset)
        => asset.SizeBytes;

    /// <summary>
    /// Extracts all textures from the glTF model.
    /// </summary>
    private static TextureData[] ExtractTextures(ModelRoot model, AssetLoadContext context)
    {
        var textures = new List<TextureData>();
        var imageToTextureIndex = new Dictionary<int, int>();

        foreach (var image in model.LogicalImages)
        {
            var textureData = ExtractImage(image, context);
            if (textureData != null)
            {
                imageToTextureIndex[image.LogicalIndex] = textures.Count;
                textures.Add(textureData);
            }
        }

        return [.. textures];
    }

    /// <summary>
    /// Extracts a single image from the glTF model.
    /// </summary>
    private static TextureData? ExtractImage(Image image, AssetLoadContext context)
    {
        try
        {
            var content = image.Content;
            if (content.Content.Length == 0)
            {
                return null;
            }

            // Decode the image using StbImageSharp
            var imageBytes = content.Content.ToArray();
            var result = ImageResult.FromMemory(imageBytes, ColorComponents.RedGreenBlueAlpha);

            var name = image.Name ?? $"Texture_{image.LogicalIndex}";

            // Determine source path for external textures
            string? sourcePath = null;
            if (!string.IsNullOrEmpty(image.Content.SourcePath))
            {
                var basePath = Path.GetDirectoryName(context.Path) ?? string.Empty;
                sourcePath = Path.Combine(basePath, image.Content.SourcePath);
            }

            return new TextureData(
                name,
                result.Data,
                result.Width,
                result.Height,
                4, // RGBA
                sourcePath);
        }
        catch
        {
            // Failed to decode image, skip it
            return null;
        }
    }

    /// <summary>
    /// Extracts all materials from the glTF model.
    /// </summary>
    private static MaterialData[] ExtractMaterials(ModelRoot model, TextureData[] textures)
    {
        var materials = new List<MaterialData>();

        foreach (var material in model.LogicalMaterials)
        {
            materials.Add(ExtractMaterial(material, model));
        }

        // Add a default material if none exist
        if (materials.Count == 0)
        {
            materials.Add(MaterialData.Default);
        }

        return [.. materials];
    }

    /// <summary>
    /// Extracts PBR properties from a single glTF material.
    /// </summary>
    private static MaterialData ExtractMaterial(SharpGLTF.Schema2.Material material, ModelRoot model)
    {
        var name = material.Name ?? $"Material_{material.LogicalIndex}";

        // Base color (albedo)
        var baseColorFactor = Vector4.One;
        var baseColorTextureIndex = -1;

        var baseColorChannel = material.FindChannel("BaseColor");
        if (baseColorChannel.HasValue)
        {
            baseColorFactor = baseColorChannel.Value.Color;
            baseColorTextureIndex = GetTextureIndex(baseColorChannel.Value.Texture);
        }

        // Metallic-roughness
        var metallicFactor = 1.0f;
        var roughnessFactor = 1.0f;
        var metallicRoughnessTextureIndex = -1;

        var mrChannel = material.FindChannel("MetallicRoughness");
        if (mrChannel.HasValue)
        {
            // SharpGLTF stores metallic in Parameter.X and roughness in Parameter.Y
            var param = mrChannel.Value.Color;
            metallicFactor = param.X;
            roughnessFactor = param.Y;
            metallicRoughnessTextureIndex = GetTextureIndex(mrChannel.Value.Texture);
        }

        // Normal map
        var normalTextureIndex = -1;
        var normalScale = 1.0f;

        var normalChannel = material.FindChannel("Normal");
        if (normalChannel.HasValue)
        {
            normalTextureIndex = GetTextureIndex(normalChannel.Value.Texture);
            normalScale = normalChannel.Value.Color.X; // Scale stored in X component
        }

        // Occlusion
        var occlusionTextureIndex = -1;
        var occlusionStrength = 1.0f;

        var occlusionChannel = material.FindChannel("Occlusion");
        if (occlusionChannel.HasValue)
        {
            occlusionTextureIndex = GetTextureIndex(occlusionChannel.Value.Texture);
            occlusionStrength = occlusionChannel.Value.Color.X; // Strength stored in X component
        }

        // Emissive
        var emissiveFactor = Vector3.Zero;
        var emissiveTextureIndex = -1;

        var emissiveChannel = material.FindChannel("Emissive");
        if (emissiveChannel.HasValue)
        {
            var emColor = emissiveChannel.Value.Color;
            emissiveFactor = new Vector3(emColor.X, emColor.Y, emColor.Z);
            emissiveTextureIndex = GetTextureIndex(emissiveChannel.Value.Texture);
        }

        // Alpha mode
        var alphaMode = material.Alpha switch
        {
            SharpGLTF.Schema2.AlphaMode.OPAQUE => Graphics.Abstractions.AlphaMode.Opaque,
            SharpGLTF.Schema2.AlphaMode.MASK => Graphics.Abstractions.AlphaMode.Mask,
            SharpGLTF.Schema2.AlphaMode.BLEND => Graphics.Abstractions.AlphaMode.Blend,
            _ => Graphics.Abstractions.AlphaMode.Opaque
        };

        var alphaCutoff = material.AlphaCutoff;
        var doubleSided = material.DoubleSided;

        return new MaterialData(
            name,
            baseColorFactor,
            metallicFactor,
            roughnessFactor,
            emissiveFactor,
            alphaCutoff,
            alphaMode,
            doubleSided,
            baseColorTextureIndex,
            normalTextureIndex,
            metallicRoughnessTextureIndex,
            occlusionTextureIndex,
            emissiveTextureIndex,
            normalScale,
            occlusionStrength);
    }

    /// <summary>
    /// Gets the logical texture index, or -1 if no texture.
    /// </summary>
    private static int GetTextureIndex(Texture? texture)
    {
        if (texture?.PrimaryImage == null)
        {
            return -1;
        }

        return texture.PrimaryImage.LogicalIndex;
    }

    /// <summary>
    /// Extracts all meshes from the glTF model.
    /// </summary>
    private static MeshAsset[] ExtractMeshes(ModelRoot model)
    {
        var meshes = new List<MeshAsset>();

        foreach (var logicalMesh in model.LogicalMeshes)
        {
            meshes.Add(ExtractMesh(logicalMesh));
        }

        return [.. meshes];
    }

    /// <summary>
    /// Extracts a single mesh with all primitives as submeshes.
    /// </summary>
    private static MeshAsset ExtractMesh(Mesh logicalMesh)
    {
        var vertices = new List<MeshVertex>();
        var indices = new List<uint>();
        var submeshes = new List<Submesh>();
        var boundsMin = new Vector3(float.MaxValue);
        var boundsMax = new Vector3(float.MinValue);

        foreach (var primitive in logicalMesh.Primitives)
        {
            var baseVertex = (uint)vertices.Count;
            var startIndex = indices.Count;

            // Get vertex accessors
            var positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();
            var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var colors = primitive.GetVertexAccessor("COLOR_0")?.AsVector4Array();
            var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

            if (positions == null)
            {
                continue;
            }

            // Extract vertices
            for (var i = 0; i < positions.Count; i++)
            {
                var position = positions[i];
                var normal = GetValueOrDefault(normals, i, Vector3.UnitY);
                var texCoord = GetValueOrDefault(texCoords, i, Vector2.Zero);
                var tangent = GetValueOrDefault(tangents, i, new Vector4(1, 0, 0, 1));
                var color = GetValueOrDefault(colors, i, Vector4.One);
                var jointVec = GetValueOrDefault(joints, i, Vector4.Zero);
                var weight = GetValueOrDefault(weights, i, new Vector4(1, 0, 0, 0));

                // Convert joint indices from float to ushort
                var jointIndices = new JointIndices(
                    (ushort)jointVec.X,
                    (ushort)jointVec.Y,
                    (ushort)jointVec.Z,
                    (ushort)jointVec.W);

                vertices.Add(new MeshVertex(position, normal, texCoord, tangent, color, jointIndices, weight));

                // Update bounds
                boundsMin = Vector3.Min(boundsMin, position);
                boundsMax = Vector3.Max(boundsMax, position);
            }

            // Extract indices
            var indexAccessor = primitive.GetIndexAccessor();
            if (indexAccessor != null)
            {
                foreach (var index in indexAccessor.AsIndicesArray())
                {
                    indices.Add(baseVertex + index);
                }
            }
            else
            {
                // No indices, create sequential indices
                for (uint i = 0; i < positions.Count; i++)
                {
                    indices.Add(baseVertex + i);
                }
            }

            // Get material index (-1 if no material assigned)
            var materialIndex = primitive.Material?.LogicalIndex ?? -1;
            var indexCount = indices.Count - startIndex;

            submeshes.Add(new Submesh(startIndex, indexCount, materialIndex));
        }

        if (vertices.Count == 0)
        {
            throw new InvalidDataException("Mesh contains no vertices");
        }

        // Compute tangents if they weren't provided in the mesh
        var vertexArray = vertices.ToArray();
        var indexArray = indices.ToArray();

        if (!HasValidTangents(vertexArray))
        {
            ComputeTangents(vertexArray, indexArray);
        }

        return new MeshAsset(
            logicalMesh.Name ?? "Mesh",
            vertexArray,
            indexArray,
            [.. submeshes],
            boundsMin,
            boundsMax);
    }

    private static T GetValueOrDefault<T>(IList<T>? array, int index, T defaultValue)
        => array != null && index < array.Count ? array[index] : defaultValue;

    /// <summary>
    /// Checks if the mesh has valid tangents (not all default values).
    /// </summary>
    private static bool HasValidTangents(MeshVertex[] vertices)
    {
        var defaultTangent = new Vector4(1, 0, 0, 1);

        foreach (var vertex in vertices)
        {
            if (vertex.Tangent != defaultTangent)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Computes tangent vectors for normal mapping using a simplified MikkTSpace-like algorithm.
    /// </summary>
    private static void ComputeTangents(MeshVertex[] vertices, uint[] indices)
    {
        // Allocate tangent and bitangent accumulators
        var tangents = new Vector3[vertices.Length];
        var bitangents = new Vector3[vertices.Length];

        // Process each triangle
        for (var i = 0; i < indices.Length; i += 3)
        {
            var i0 = (int)indices[i];
            var i1 = (int)indices[i + 1];
            var i2 = (int)indices[i + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            // Position deltas
            var edge1 = v1.Position - v0.Position;
            var edge2 = v2.Position - v0.Position;

            // UV deltas
            var deltaUv1 = v1.TexCoord - v0.TexCoord;
            var deltaUv2 = v2.TexCoord - v0.TexCoord;

            // Calculate tangent and bitangent
            var r = deltaUv1.X * deltaUv2.Y - deltaUv2.X * deltaUv1.Y;

            // Avoid division by zero for degenerate triangles
            if (Math.Abs(r) < 1e-6f)
            {
                continue;
            }

            r = 1.0f / r;

            var tangent = new Vector3(
                (deltaUv2.Y * edge1.X - deltaUv1.Y * edge2.X) * r,
                (deltaUv2.Y * edge1.Y - deltaUv1.Y * edge2.Y) * r,
                (deltaUv2.Y * edge1.Z - deltaUv1.Y * edge2.Z) * r);

            var bitangent = new Vector3(
                (-deltaUv2.X * edge1.X + deltaUv1.X * edge2.X) * r,
                (-deltaUv2.X * edge1.Y + deltaUv1.X * edge2.Y) * r,
                (-deltaUv2.X * edge1.Z + deltaUv1.X * edge2.Z) * r);

            // Accumulate for each vertex of the triangle
            tangents[i0] += tangent;
            tangents[i1] += tangent;
            tangents[i2] += tangent;

            bitangents[i0] += bitangent;
            bitangents[i1] += bitangent;
            bitangents[i2] += bitangent;
        }

        // Orthonormalize and store tangents
        for (var i = 0; i < vertices.Length; i++)
        {
            var n = vertices[i].Normal;
            var t = tangents[i];
            var b = bitangents[i];

            // Gram-Schmidt orthogonalize: t' = normalize(t - n * dot(n, t))
            var tangentOrtho = Vector3.Normalize(t - n * Vector3.Dot(n, t));

            // Handle degenerate case
            if (float.IsNaN(tangentOrtho.X))
            {
                tangentOrtho = new Vector3(1, 0, 0);
            }

            // Calculate handedness (bitangent sign)
            var cross = Vector3.Cross(n, t);
            var handedness = Vector3.Dot(cross, b) < 0 ? -1.0f : 1.0f;

            // Update vertex with computed tangent
            vertices[i] = vertices[i] with { Tangent = new Vector4(tangentOrtho, handedness) };
        }
    }
}
