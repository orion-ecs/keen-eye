using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Shaders;

namespace KeenEyes.Graphics.Silk.Ibl;

/// <summary>
/// Manages Image-Based Lighting (IBL) processing and resources.
/// </summary>
/// <remarks>
/// <para>
/// This manager handles the complete IBL pipeline:
/// </para>
/// <list type="bullet">
/// <item><description>Loading HDR environment maps (equirectangular format)</description></item>
/// <item><description>Converting equirectangular maps to cubemaps</description></item>
/// <item><description>Generating irradiance maps for diffuse IBL</description></item>
/// <item><description>Generating pre-filtered specular maps for specular IBL</description></item>
/// <item><description>Generating BRDF lookup tables</description></item>
/// </list>
/// </remarks>
/// <param name="graphics">The graphics context for creating textures and render targets.</param>
/// <param name="device">The graphics device for low-level GPU operations.</param>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
public sealed class IblManager(IGraphicsContext graphics, IGraphicsDevice device) : IDisposable
{
    private readonly Dictionary<int, IblData> iblDataStore = [];
    private int nextIblDataId = 1;
    private bool disposed;

    // Shared resources
    private TextureHandle sharedBrdfLut = TextureHandle.Invalid;
    private uint cubeVao;
    private uint cubeVbo;
    private uint quadVao;
    private uint quadVbo;
    private bool meshesInitialized;

    // Shader programs (created on demand)
    private uint equirectToCubemapProgram;
    private uint irradianceProgram;
    private uint specularPrefilterProgram;
    private uint brdfLutProgram;

    /// <summary>
    /// Gets the graphics context used by this manager.
    /// </summary>
    public IGraphicsContext Graphics => graphics;

    /// <summary>
    /// Gets the shared BRDF lookup table.
    /// </summary>
    /// <remarks>
    /// The BRDF LUT is the same for all environments and only needs to be generated once.
    /// </remarks>
    public TextureHandle SharedBrdfLut => sharedBrdfLut;

    /// <summary>
    /// Processes an HDR environment map and generates all IBL textures.
    /// </summary>
    /// <param name="environmentData">The HDR environment map data.</param>
    /// <param name="settings">The IBL processing settings.</param>
    /// <returns>The ID of the generated IBL data.</returns>
    public int ProcessEnvironmentMap(EnvironmentMapData environmentData, IblSettings? settings = null)
    {
        if (!environmentData.IsValid)
        {
            throw new ArgumentException("Invalid environment map data", nameof(environmentData));
        }

        var iblSettings = settings ?? IblSettings.Default;

        EnsureInitialized();

        // Create equirectangular texture from HDR data
        var equirectTexture = CreateHdrTexture(environmentData);

        try
        {
            // Convert to cubemap
            int envSize = iblSettings.SpecularResolutionPixels;
            var environmentMap = ConvertEquirectangularToCubemap(equirectTexture, envSize);

            // Generate irradiance map
            var irradianceMap = GenerateIrradianceMap(
                environmentMap,
                iblSettings.IrradianceResolutionPixels);

            // Generate specular map with mip levels
            var specularMap = GenerateSpecularMap(
                environmentMap,
                iblSettings.SpecularResolutionPixels,
                iblSettings.SpecularMipLevels,
                iblSettings.SpecularSampleCount);

            // Get or generate shared BRDF LUT
            if (!sharedBrdfLut.IsValid)
            {
                sharedBrdfLut = GenerateBrdfLut(iblSettings.BrdfLutResolution);
            }

            var iblData = new IblData
            {
                EnvironmentMap = environmentMap,
                IrradianceMap = irradianceMap,
                SpecularMap = specularMap,
                BrdfLut = sharedBrdfLut,
                Settings = iblSettings,
                SpecularMipLevels = iblSettings.SpecularMipLevels
            };

            int id = nextIblDataId++;
            iblDataStore[id] = iblData;
            return id;
        }
        finally
        {
            // Clean up temporary equirectangular texture
            graphics.DeleteTexture(equirectTexture);
        }
    }

    /// <summary>
    /// Gets the IBL data for the specified ID.
    /// </summary>
    /// <param name="id">The IBL data ID.</param>
    /// <returns>The IBL data, or <see cref="IblData.Invalid"/> if not found.</returns>
    public IblData GetIblData(int id)
    {
        return iblDataStore.TryGetValue(id, out var data) ? data : IblData.Invalid;
    }

    /// <summary>
    /// Deletes the IBL data for the specified ID.
    /// </summary>
    /// <param name="id">The IBL data ID.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteIblData(int id)
    {
        if (!iblDataStore.TryGetValue(id, out var data))
        {
            return false;
        }

        // Delete textures (except shared BRDF LUT)
        if (data.EnvironmentMap.IsValid)
        {
            graphics.DeleteCubemapTexture(data.EnvironmentMap);
        }

        if (data.IrradianceMap.IsValid)
        {
            graphics.DeleteCubemapTexture(data.IrradianceMap);
        }

        if (data.SpecularMap.IsValid)
        {
            graphics.DeleteCubemapTexture(data.SpecularMap);
        }

        iblDataStore.Remove(id);
        return true;
    }

    /// <summary>
    /// Gets all loaded IBL data IDs.
    /// </summary>
    public IEnumerable<int> LoadedIblDataIds => iblDataStore.Keys;

    private void EnsureInitialized()
    {
        if (!meshesInitialized)
        {
            InitializeMeshes();
            InitializeShaders();
            meshesInitialized = true;
        }
    }

    private void InitializeMeshes()
    {
        // Create unit cube for cubemap rendering
        float[] cubeVertices =
        [
            // Back face
            -1.0f, -1.0f, -1.0f,
            1.0f, 1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, 1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f, 1.0f, -1.0f,
            // Front face
            -1.0f, -1.0f, 1.0f,
            1.0f, -1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, 1.0f,
            -1.0f, -1.0f, 1.0f,
            // Left face
            -1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, 1.0f,
            -1.0f, 1.0f, 1.0f,
            // Right face
            1.0f, 1.0f, 1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, 1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, -1.0f, 1.0f,
            // Bottom face
            -1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, 1.0f,
            1.0f, -1.0f, 1.0f,
            -1.0f, -1.0f, 1.0f,
            -1.0f, -1.0f, -1.0f,
            // Top face
            -1.0f, 1.0f, -1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, -1.0f,
            1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, -1.0f,
            -1.0f, 1.0f, 1.0f
        ];

        cubeVao = device.GenVertexArray();
        cubeVbo = device.GenBuffer();

        device.BindVertexArray(cubeVao);
        device.BindBuffer(BufferTarget.ArrayBuffer, cubeVbo);
        device.BufferData(BufferTarget.ArrayBuffer, cubeVertices.AsSpan(), BufferUsage.StaticDraw);

        // Position attribute
        device.EnableVertexAttribArray(0);
        device.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 3 * sizeof(float), 0);

        // Create fullscreen quad for BRDF LUT
        float[] quadVertices =
        [
            // Position (XY)   TexCoord (UV)
            0.0f, 0.0f,       0.0f, 0.0f,
            1.0f, 0.0f,       1.0f, 0.0f,
            1.0f, 1.0f,       1.0f, 1.0f,

            0.0f, 0.0f,       0.0f, 0.0f,
            1.0f, 1.0f,       1.0f, 1.0f,
            0.0f, 1.0f,       0.0f, 1.0f
        ];

        quadVao = device.GenVertexArray();
        quadVbo = device.GenBuffer();

        device.BindVertexArray(quadVao);
        device.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
        device.BufferData(BufferTarget.ArrayBuffer, quadVertices.AsSpan(), BufferUsage.StaticDraw);

        // Position attribute (location 0)
        device.EnableVertexAttribArray(0);
        device.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 4 * sizeof(float), 0);

        // TexCoord attribute (location 2, as used in the shader)
        device.EnableVertexAttribArray(2);
        device.VertexAttribPointer(2, 2, VertexAttribType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

        device.BindVertexArray(0);
    }

    private void InitializeShaders()
    {
        equirectToCubemapProgram = CompileShaderProgram(
            IblShaders.CubemapVertexShader,
            IblShaders.EquirectToCubemapFragmentShader);

        irradianceProgram = CompileShaderProgram(
            IblShaders.CubemapVertexShader,
            IblShaders.IrradianceConvolutionFragmentShader);

        specularPrefilterProgram = CompileShaderProgram(
            IblShaders.CubemapVertexShader,
            IblShaders.SpecularPrefilterFragmentShader);

        brdfLutProgram = CompileShaderProgram(
            IblShaders.FullscreenQuadVertexShader,
            IblShaders.BrdfLutFragmentShader);
    }

    private uint CompileShaderProgram(string vertexSource, string fragmentSource)
    {
        uint vertexShader = device.CreateShader(ShaderType.Vertex);
        device.ShaderSource(vertexShader, vertexSource);
        device.CompileShader(vertexShader);

        if (!device.GetShaderCompileStatus(vertexShader))
        {
            string log = device.GetShaderInfoLog(vertexShader);
            device.DeleteShader(vertexShader);
            throw new InvalidOperationException($"Vertex shader compilation failed: {log}");
        }

        uint fragmentShader = device.CreateShader(ShaderType.Fragment);
        device.ShaderSource(fragmentShader, fragmentSource);
        device.CompileShader(fragmentShader);

        if (!device.GetShaderCompileStatus(fragmentShader))
        {
            string log = device.GetShaderInfoLog(fragmentShader);
            device.DeleteShader(vertexShader);
            device.DeleteShader(fragmentShader);
            throw new InvalidOperationException($"Fragment shader compilation failed: {log}");
        }

        uint program = device.CreateProgram();
        device.AttachShader(program, vertexShader);
        device.AttachShader(program, fragmentShader);
        device.LinkProgram(program);

        if (!device.GetProgramLinkStatus(program))
        {
            string log = device.GetProgramInfoLog(program);
            device.DeleteProgram(program);
            device.DeleteShader(vertexShader);
            device.DeleteShader(fragmentShader);
            throw new InvalidOperationException($"Shader program linking failed: {log}");
        }

        device.DetachShader(program, vertexShader);
        device.DetachShader(program, fragmentShader);
        device.DeleteShader(vertexShader);
        device.DeleteShader(fragmentShader);

        return program;
    }

    private TextureHandle CreateHdrTexture(EnvironmentMapData data)
    {
        return graphics.CreateHdrTexture(data.Width, data.Height, data.Pixels);
    }

    private TextureHandle ConvertEquirectangularToCubemap(TextureHandle equirectTexture, int size)
    {
        // Create cubemap render target
        var cubemapTarget = graphics.CreateCubemapRenderTarget(size, withDepth: true);

        // Setup view matrices for each face
        var captureViews = GetCubemapViewMatrices();
        var captureProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2f, 1.0f, 0.1f, 10.0f);

        device.UseProgram(equirectToCubemapProgram);

        // Set projection matrix
        int projLoc = device.GetUniformLocation(equirectToCubemapProgram, "uProjection");
        device.UniformMatrix4(projLoc, captureProjection);

        // Bind equirectangular texture
        device.ActiveTexture(TextureUnit.Texture0);
        graphics.BindTexture(equirectTexture, 0);
        int texLoc = device.GetUniformLocation(equirectToCubemapProgram, "uEquirectangularMap");
        device.Uniform1(texLoc, 0);

        int viewLoc = device.GetUniformLocation(equirectToCubemapProgram, "uView");

        // Render each face
        for (int face = 0; face < 6; face++)
        {
            graphics.BindCubemapRenderTarget(cubemapTarget, (CubemapFace)face);
            device.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);

            device.UniformMatrix4(viewLoc, captureViews[face]);

            device.BindVertexArray(cubeVao);
            device.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }

        graphics.UnbindRenderTarget();

        // Get the cubemap texture handle
        var textureHandle = graphics.GetCubemapRenderTargetTexture(cubemapTarget);

        // Generate mipmaps for the environment cubemap
        device.BindTexture(TextureTarget.TextureCubeMap, (uint)textureHandle.Id);
        device.GenerateMipmap(TextureTarget.TextureCubeMap);

        // Clean up render target (keep the texture)
        graphics.DeleteCubemapRenderTargetKeepTexture(cubemapTarget);

        return textureHandle;
    }

    private TextureHandle GenerateIrradianceMap(TextureHandle environmentCubemap, int size)
    {
        // Create cubemap render target for irradiance
        var irradianceTarget = graphics.CreateCubemapRenderTarget(size, withDepth: true);

        // Setup matrices
        var captureViews = GetCubemapViewMatrices();
        var captureProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2f, 1.0f, 0.1f, 10.0f);

        device.UseProgram(irradianceProgram);

        // Set uniforms
        int projLoc = device.GetUniformLocation(irradianceProgram, "uProjection");
        device.UniformMatrix4(projLoc, captureProjection);

        // Bind environment cubemap
        device.ActiveTexture(TextureUnit.Texture0);
        graphics.BindCubemapTexture(environmentCubemap, 0);
        int envLoc = device.GetUniformLocation(irradianceProgram, "uEnvironmentMap");
        device.Uniform1(envLoc, 0);

        int viewLoc = device.GetUniformLocation(irradianceProgram, "uView");

        // Render each face
        for (int face = 0; face < 6; face++)
        {
            graphics.BindCubemapRenderTarget(irradianceTarget, (CubemapFace)face);
            device.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);

            device.UniformMatrix4(viewLoc, captureViews[face]);

            device.BindVertexArray(cubeVao);
            device.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }

        graphics.UnbindRenderTarget();

        // Get the cubemap texture handle and clean up render target
        var textureHandle = graphics.GetCubemapRenderTargetTexture(irradianceTarget);
        graphics.DeleteCubemapRenderTargetKeepTexture(irradianceTarget);

        return textureHandle;
    }

    private TextureHandle GenerateSpecularMap(TextureHandle environmentCubemap, int size, int mipLevels, int sampleCount)
    {
        // Create cubemap render target with mip levels
        var specularTarget = graphics.CreateCubemapRenderTarget(size, withDepth: true, mipLevels);

        // Setup matrices
        var captureViews = GetCubemapViewMatrices();
        var captureProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2f, 1.0f, 0.1f, 10.0f);

        device.UseProgram(specularPrefilterProgram);

        // Set uniforms
        int projLoc = device.GetUniformLocation(specularPrefilterProgram, "uProjection");
        device.UniformMatrix4(projLoc, captureProjection);

        // Bind environment cubemap
        device.ActiveTexture(TextureUnit.Texture0);
        graphics.BindCubemapTexture(environmentCubemap, 0);
        int envLoc = device.GetUniformLocation(specularPrefilterProgram, "uEnvironmentMap");
        device.Uniform1(envLoc, 0);

        int viewLoc = device.GetUniformLocation(specularPrefilterProgram, "uView");
        int roughnessLoc = device.GetUniformLocation(specularPrefilterProgram, "uRoughness");
        int sampleCountLoc = device.GetUniformLocation(specularPrefilterProgram, "uSampleCount");

        device.Uniform1(sampleCountLoc, sampleCount);

        // Render each mip level with increasing roughness
        for (int mip = 0; mip < mipLevels; mip++)
        {
            float roughness = (float)mip / (mipLevels - 1);
            device.Uniform1(roughnessLoc, roughness);

            // Render each face at this mip level
            for (int face = 0; face < 6; face++)
            {
                graphics.BindCubemapRenderTarget(specularTarget, (CubemapFace)face, mip);
                device.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);

                device.UniformMatrix4(viewLoc, captureViews[face]);

                device.BindVertexArray(cubeVao);
                device.DrawArrays(PrimitiveType.Triangles, 0, 36);
            }
        }

        graphics.UnbindRenderTarget();

        // Get the cubemap texture handle and clean up render target
        var textureHandle = graphics.GetCubemapRenderTargetTexture(specularTarget);
        graphics.DeleteCubemapRenderTargetKeepTexture(specularTarget);

        return textureHandle;
    }

    private TextureHandle GenerateBrdfLut(int size)
    {
        // Create 2D render target for BRDF LUT (RG16F format)
        var brdfTarget = graphics.CreateRenderTarget(size, size, RenderTargetFormat.RGBA16FDepth24);

        graphics.BindRenderTarget(brdfTarget);
        device.Viewport(0, 0, (uint)size, (uint)size);
        device.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);

        device.UseProgram(brdfLutProgram);

        device.BindVertexArray(quadVao);
        device.DrawArrays(PrimitiveType.Triangles, 0, 6);

        graphics.UnbindRenderTarget();

        // Get the color texture from the render target
        var textureHandle = graphics.GetRenderTargetColorTexture(brdfTarget);
        graphics.DeleteRenderTargetKeepTexture(brdfTarget);

        return textureHandle;
    }

    private static Matrix4x4[] GetCubemapViewMatrices()
    {
        // View matrices for each cubemap face
        return
        [
            Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(1, 0, 0), new Vector3(0, -1, 0)),   // +X
            Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(-1, 0, 0), new Vector3(0, -1, 0)),  // -X
            Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0, 1, 0), new Vector3(0, 0, 1)),    // +Y
            Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0, -1, 0), new Vector3(0, 0, -1)),  // -Y
            Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0, 0, 1), new Vector3(0, -1, 0)),   // +Z
            Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0, 0, -1), new Vector3(0, -1, 0))   // -Z
        ];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Delete all IBL data
        foreach (var id in iblDataStore.Keys.ToList())
        {
            DeleteIblData(id);
        }

        // Delete shared BRDF LUT
        if (sharedBrdfLut.IsValid)
        {
            graphics.DeleteTexture(sharedBrdfLut);
            sharedBrdfLut = TextureHandle.Invalid;
        }

        // Delete meshes
        if (meshesInitialized)
        {
            device.DeleteVertexArray(cubeVao);
            device.DeleteBuffer(cubeVbo);
            device.DeleteVertexArray(quadVao);
            device.DeleteBuffer(quadVbo);
        }

        // Delete shaders
        if (equirectToCubemapProgram != 0)
        {
            device.DeleteProgram(equirectToCubemapProgram);
        }

        if (irradianceProgram != 0)
        {
            device.DeleteProgram(irradianceProgram);
        }

        if (specularPrefilterProgram != 0)
        {
            device.DeleteProgram(specularPrefilterProgram);
        }

        if (brdfLutProgram != 0)
        {
            device.DeleteProgram(brdfLutProgram);
        }
    }
}
