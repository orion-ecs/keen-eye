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
/// This system runs in the Render phase and requires an <see cref="IGraphicsContext"/>
/// extension to be present on the world.
/// </para>
/// </remarks>
public sealed class RenderSystem : ISystem
{
    private IWorld? world;
    private IGraphicsContext? graphics;
    private readonly List<(Entity Entity, int Layer)> renderQueue = [];

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

        // Enable depth testing and culling
        graphics.SetDepthTest(true);
        graphics.SetCulling(true, CullFaceMode.Back);

        // Collect light data
        Vector3 lightDirection = -Vector3.UnitY;
        Vector3 lightColor = Vector3.One;
        float lightIntensity = 1f;

        foreach (var entity in world.Query<Light, Transform3D>())
        {
            ref readonly var light = ref world.Get<Light>(entity);
            ref readonly var lightTransform = ref world.Get<Transform3D>(entity);

            if (light.Type == LightType.Directional)
            {
                lightDirection = lightTransform.Forward();
                lightColor = light.Color;
                lightIntensity = light.Intensity;
                break; // Use first directional light
            }
        }

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
        TextureHandle currentTexture = default;

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
            ShaderHandle shader = graphics.SolidShader;
            TextureHandle texture = graphics.WhiteTexture;
            Vector4 color = Vector4.One;
            Vector3 emissive = Vector3.Zero;

            if (world.Has<Material>(entity))
            {
                ref readonly var material = ref world.Get<Material>(entity);
                shader = material.ShaderId > 0 ? new ShaderHandle(material.ShaderId) : graphics.LitShader;
                texture = material.BaseColorTextureId > 0 ? new TextureHandle(material.BaseColorTextureId) : graphics.WhiteTexture;
                color = material.BaseColorFactor;
                emissive = material.EmissiveFactor;
            }

            // Bind shader
            if (currentShader.Id != shader.Id)
            {
                graphics.BindShader(shader);
                currentShader = shader;

                // Set per-frame uniforms
                graphics.SetUniform("uView", viewMatrix);
                graphics.SetUniform("uProjection", projectionMatrix);
                graphics.SetUniform("uCameraPosition", cameraTransform.Position);
                graphics.SetUniform("uLightDirection", lightDirection);
                graphics.SetUniform("uLightColor", lightColor);
                graphics.SetUniform("uLightIntensity", lightIntensity);
            }

            // Bind texture
            if (currentTexture.Id != texture.Id)
            {
                graphics.BindTexture(texture, 0);
                graphics.SetUniform("uTexture", 0);
                currentTexture = texture;
            }

            // Set per-object uniforms
            Matrix4x4 modelMatrix = transform.Matrix();
            graphics.SetUniform("uModel", modelMatrix);
            graphics.SetUniform("uColor", color);
            graphics.SetUniform("uEmissive", emissive);

            // Draw mesh
            var meshHandle = new MeshHandle(renderable.MeshId);
            graphics.BindMesh(meshHandle);
            graphics.DrawMesh(meshHandle);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        renderQueue.Clear();
    }
}
