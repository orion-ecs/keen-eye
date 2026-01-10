using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics;

/// <summary>
/// System that renders entities with <see cref="Transform3D"/> and <see cref="Renderable"/> components.
/// </summary>
/// <remarks>
/// <para>
/// The RenderSystem queries for entities with Transform3D and Renderable components,
/// sorts them by render layer, and submits draw calls to the GPU. It uses the active
/// camera's view and projection matrices for rendering.
/// </para>
/// <para>
/// This system supports both simple lit shaders and full PBR rendering with Cook-Torrance BRDF.
/// When using the PBR shader, it binds all PBR texture slots and sets appropriate uniforms.
/// </para>
/// <para>
/// This system runs in the Render phase and requires an <see cref="IGraphicsContext"/>
/// extension to be present on the world.
/// </para>
/// </remarks>
public sealed class RenderSystem : ISystem
{
    private const int MaxLights = 8;

    private IWorld? world;
    private IGraphicsContext? graphics;
    private readonly List<(Entity Entity, int Layer)> renderQueue = [];

    // Light data arrays for uniform upload
    private readonly Vector3[] lightPositions = new Vector3[MaxLights];
    private readonly Vector3[] lightDirections = new Vector3[MaxLights];
    private readonly Vector3[] lightColors = new Vector3[MaxLights];
    private readonly float[] lightIntensities = new float[MaxLights];
    private readonly int[] lightTypes = new int[MaxLights];
    private readonly float[] lightRanges = new float[MaxLights];
    private readonly float[] lightInnerCones = new float[MaxLights];
    private readonly float[] lightOuterCones = new float[MaxLights];

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;

        if (!world.TryGetExtension<IGraphicsContext>(out graphics))
        {
            throw new InvalidOperationException("RenderSystem requires IGraphicsContext extension");
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (world is null || graphics is null || !graphics.IsInitialized)
        {
            return;
        }

        // Find active camera
        Camera camera = default;
        Transform3D cameraTransform = default;
        bool foundCamera = false;

        // Prefer main camera tag
        foreach (var entity in world.Query<Camera, Transform3D, MainCameraTag>())
        {
            camera = world.Get<Camera>(entity);
            cameraTransform = world.Get<Transform3D>(entity);
            foundCamera = true;
            break;
        }

        // Fall back to any camera
        if (!foundCamera)
        {
            foreach (var entity in world.Query<Camera, Transform3D>())
            {
                camera = world.Get<Camera>(entity);
                cameraTransform = world.Get<Transform3D>(entity);
                foundCamera = true;
                break;
            }
        }

        if (!foundCamera)
        {
            // No camera, nothing to render
            return;
        }

        // Calculate camera matrices
        Matrix4x4 viewMatrix = camera.ViewMatrix(cameraTransform);
        Matrix4x4 projectionMatrix = camera.ProjectionMatrix();

        // Clear screen
        if (camera.ClearColorBuffer || camera.ClearDepthBuffer)
        {
            ClearMask clearMask = 0;
            if (camera.ClearColorBuffer)
            {
                graphics.SetClearColor(camera.ClearColor);
                clearMask |= ClearMask.ColorBuffer;
            }
            if (camera.ClearDepthBuffer)
            {
                clearMask |= ClearMask.DepthBuffer;
            }
            graphics.Clear(clearMask);
        }

        // Enable depth testing
        graphics.SetDepthTest(true);

        // Collect all lights (up to MaxLights)
        int lightCount = CollectLights();

        // Build render queue
        renderQueue.Clear();
        foreach (var entity in world.Query<Transform3D, Renderable>())
        {
            ref readonly var renderable = ref world.Get<Renderable>(entity);
            renderQueue.Add((entity, renderable.Layer));
        }

        // Sort by layer (stable sort preserves order within same layer)
        renderQueue.Sort((a, b) => a.Layer.CompareTo(b.Layer));

        // Render each entity
        ShaderHandle currentShader = default;
        bool isPbrShader = false;

        foreach (var (entity, _) in renderQueue)
        {
            ref readonly var transform = ref world.Get<Transform3D>(entity);
            ref readonly var renderable = ref world.Get<Renderable>(entity);

            // Skip if no mesh
            if (renderable.MeshId <= 0)
            {
                continue;
            }

            // Get material (use solid shader if no material component)
            Material material = Material.Default;
            ShaderHandle shader = graphics.SolidShader;

            if (world.Has<Material>(entity))
            {
                material = world.Get<Material>(entity);
                shader = material.ShaderId > 0 ? new ShaderHandle(material.ShaderId) : graphics.LitShader;
            }

            // Handle culling based on material double-sided flag
            if (material.DoubleSided)
            {
                graphics.SetCulling(false);
            }
            else
            {
                graphics.SetCulling(true, CullFaceMode.Back);
            }

            // Handle alpha blending
            if (material.AlphaMode == AlphaMode.Blend)
            {
                graphics.SetBlending(true);
            }
            else
            {
                graphics.SetBlending(false);
            }

            // Bind shader if changed
            if (currentShader.Id != shader.Id)
            {
                graphics.BindShader(shader);
                currentShader = shader;
                isPbrShader = shader.Id == graphics.PbrShader.Id;

                // Set per-frame uniforms
                graphics.SetUniform("uView", viewMatrix);
                graphics.SetUniform("uProjection", projectionMatrix);
                graphics.SetUniform("uCameraPosition", cameraTransform.Position);

                if (isPbrShader)
                {
                    // Set PBR light uniforms
                    SetPbrLightUniforms(lightCount);
                }
                else
                {
                    // Legacy single light uniform (for LitShader compatibility)
                    SetLegacyLightUniforms(lightCount);
                }
            }

            // Calculate model and normal matrices
            Matrix4x4 modelMatrix = transform.Matrix();

            // Set per-object uniforms
            graphics.SetUniform("uModel", modelMatrix);

            if (isPbrShader)
            {
                // Bind PBR textures and set material uniforms
                BindPbrMaterial(material);
            }
            else
            {
                // Legacy material binding (for LitShader compatibility)
                BindLegacyMaterial(material);
            }

            // Draw mesh
            var meshHandle = new MeshHandle(renderable.MeshId);
            graphics.BindMesh(meshHandle);
            graphics.DrawMesh(meshHandle);
        }
    }

    /// <summary>
    /// Collects all lights in the scene into arrays for uniform upload.
    /// </summary>
    /// <returns>The number of lights collected (capped at MaxLights).</returns>
    private int CollectLights()
    {
        int lightCount = 0;

        foreach (var entity in world!.Query<Light, Transform3D>())
        {
            if (lightCount >= MaxLights)
            {
                break;
            }

            ref readonly var light = ref world.Get<Light>(entity);
            ref readonly var lightTransform = ref world.Get<Transform3D>(entity);

            lightPositions[lightCount] = lightTransform.Position;
            lightDirections[lightCount] = lightTransform.Forward();
            lightColors[lightCount] = light.Color;
            lightIntensities[lightCount] = light.Intensity;
            lightTypes[lightCount] = (int)light.Type;
            lightRanges[lightCount] = light.Range;
            // Convert cone angles from degrees to cosine values for efficient shader comparison
            lightInnerCones[lightCount] = MathF.Cos(light.InnerConeAngle * MathF.PI / 180f);
            lightOuterCones[lightCount] = MathF.Cos(light.OuterConeAngle * MathF.PI / 180f);

            lightCount++;
        }

        return lightCount;
    }

    /// <summary>
    /// Sets PBR light uniforms for all collected lights.
    /// </summary>
    private void SetPbrLightUniforms(int lightCount)
    {
        graphics!.SetUniform("uLightCount", lightCount);

        for (int i = 0; i < lightCount; i++)
        {
            graphics.SetUniform($"uLightPositions[{i}]", lightPositions[i]);
            graphics.SetUniform($"uLightDirections[{i}]", lightDirections[i]);
            graphics.SetUniform($"uLightColors[{i}]", lightColors[i]);
            graphics.SetUniform($"uLightIntensities[{i}]", lightIntensities[i]);
            graphics.SetUniform($"uLightTypes[{i}]", lightTypes[i]);
            graphics.SetUniform($"uLightRanges[{i}]", lightRanges[i]);
            graphics.SetUniform($"uLightInnerCones[{i}]", lightInnerCones[i]);
            graphics.SetUniform($"uLightOuterCones[{i}]", lightOuterCones[i]);
        }
    }

    /// <summary>
    /// Sets legacy single-light uniforms for backward compatibility with LitShader.
    /// </summary>
    private void SetLegacyLightUniforms(int lightCount)
    {
        if (lightCount > 0)
        {
            // Find first directional light for legacy shader
            for (int i = 0; i < lightCount; i++)
            {
                if (lightTypes[i] == (int)LightType.Directional)
                {
                    graphics!.SetUniform("uLightDirection", lightDirections[i]);
                    graphics.SetUniform("uLightColor", lightColors[i]);
                    graphics.SetUniform("uLightIntensity", lightIntensities[i]);
                    return;
                }
            }

            // No directional light, use first light
            graphics!.SetUniform("uLightDirection", lightDirections[0]);
            graphics!.SetUniform("uLightColor", lightColors[0]);
            graphics!.SetUniform("uLightIntensity", lightIntensities[0]);
        }
        else
        {
            // Default light
            graphics!.SetUniform("uLightDirection", -Vector3.UnitY);
            graphics.SetUniform("uLightColor", Vector3.One);
            graphics.SetUniform("uLightIntensity", 1f);
        }
    }

    /// <summary>
    /// Binds all PBR textures and sets material uniforms.
    /// </summary>
    private void BindPbrMaterial(in Material material)
    {
        // Bind textures to their respective slots
        // Slot 0: Base Color
        var baseColorTexture = material.BaseColorTextureId > 0
            ? new TextureHandle(material.BaseColorTextureId)
            : graphics!.WhiteTexture;
        graphics!.BindTexture(baseColorTexture, 0);
        graphics.SetUniform("uBaseColorMap", 0);
        graphics.SetUniform("uHasBaseColorMap", material.BaseColorTextureId > 0 ? 1 : 0);

        // Slot 1: Normal Map
        var normalTexture = material.NormalMapId > 0
            ? new TextureHandle(material.NormalMapId)
            : graphics.WhiteTexture;
        graphics.BindTexture(normalTexture, 1);
        graphics.SetUniform("uNormalMap", 1);
        graphics.SetUniform("uHasNormalMap", material.NormalMapId > 0 ? 1 : 0);

        // Slot 2: Metallic-Roughness
        var mrTexture = material.MetallicRoughnessTextureId > 0
            ? new TextureHandle(material.MetallicRoughnessTextureId)
            : graphics.WhiteTexture;
        graphics.BindTexture(mrTexture, 2);
        graphics.SetUniform("uMetallicRoughnessMap", 2);
        graphics.SetUniform("uHasMetallicRoughnessMap", material.MetallicRoughnessTextureId > 0 ? 1 : 0);

        // Slot 3: Occlusion
        var occlusionTexture = material.OcclusionTextureId > 0
            ? new TextureHandle(material.OcclusionTextureId)
            : graphics.WhiteTexture;
        graphics.BindTexture(occlusionTexture, 3);
        graphics.SetUniform("uOcclusionMap", 3);
        graphics.SetUniform("uHasOcclusionMap", material.OcclusionTextureId > 0 ? 1 : 0);

        // Slot 4: Emissive
        var emissiveTexture = material.EmissiveTextureId > 0
            ? new TextureHandle(material.EmissiveTextureId)
            : graphics.WhiteTexture;
        graphics.BindTexture(emissiveTexture, 4);
        graphics.SetUniform("uEmissiveMap", 4);
        graphics.SetUniform("uHasEmissiveMap", material.EmissiveTextureId > 0 ? 1 : 0);

        // Set material factor uniforms
        graphics.SetUniform("uBaseColorFactor", material.BaseColorFactor);
        graphics.SetUniform("uMetallicFactor", material.MetallicFactor);
        graphics.SetUniform("uRoughnessFactor", material.RoughnessFactor);
        graphics.SetUniform("uEmissiveFactor", material.EmissiveFactor);
        graphics.SetUniform("uNormalScale", material.NormalScale);
        graphics.SetUniform("uOcclusionStrength", material.OcclusionStrength);
        graphics.SetUniform("uAlphaCutoff", material.AlphaCutoff);
    }

    /// <summary>
    /// Binds legacy material for LitShader and other non-PBR shaders.
    /// </summary>
    private void BindLegacyMaterial(in Material material)
    {
        // Bind base color texture
        var texture = material.BaseColorTextureId > 0
            ? new TextureHandle(material.BaseColorTextureId)
            : graphics!.WhiteTexture;
        graphics!.BindTexture(texture, 0);
        graphics.SetUniform("uTexture", 0);

        // Set legacy uniforms
        graphics.SetUniform("uColor", material.BaseColorFactor);
        graphics.SetUniform("uEmissive", material.EmissiveFactor);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        renderQueue.Clear();
    }
}
