using System.Numerics;
using KeenEyes.Spatial;
using Silk.NET.OpenGL;

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
/// This system runs in the <see cref="SystemPhase.Render"/> phase and requires the
/// <see cref="GraphicsContext"/> extension to be present.
/// </para>
/// </remarks>
public sealed class RenderSystem : SystemBase
{
    private GraphicsContext? graphics;
    private readonly List<(Entity Entity, int Layer)> renderQueue = [];

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        if (!World.TryGetExtension<GraphicsContext>(out graphics))
        {
            throw new InvalidOperationException("RenderSystem requires GraphicsContext extension");
        }
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (graphics?.GL is null || !graphics.IsInitialized)
        {
            return;
        }

        var gl = graphics.GL;

        // Find active camera
        Camera camera = default;
        Transform3D cameraTransform = default;
        bool foundCamera = false;

        // Prefer main camera tag
        foreach (var entity in World.Query<Camera, Transform3D, MainCameraTag>())
        {
            camera = World.Get<Camera>(entity);
            cameraTransform = World.Get<Transform3D>(entity);
            foundCamera = true;
            break;
        }

        // Fall back to any camera
        if (!foundCamera)
        {
            foreach (var entity in World.Query<Camera, Transform3D>())
            {
                camera = World.Get<Camera>(entity);
                cameraTransform = World.Get<Transform3D>(entity);
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
        Matrix4x4 viewMatrix = Camera.GetViewMatrix(cameraTransform);
        Matrix4x4 projectionMatrix = camera.GetProjectionMatrix();

        // Clear screen
        if (camera.ClearColorBuffer || camera.ClearDepthBuffer)
        {
            ClearBufferMask clearMask = 0;
            if (camera.ClearColorBuffer)
            {
                gl.ClearColor(camera.ClearColor.X, camera.ClearColor.Y, camera.ClearColor.Z, camera.ClearColor.W);
                clearMask |= ClearBufferMask.ColorBufferBit;
            }
            if (camera.ClearDepthBuffer)
            {
                clearMask |= ClearBufferMask.DepthBufferBit;
            }
            gl.Clear(clearMask);
        }

        // Enable depth testing and culling
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);

        // Collect light data
        Vector3 lightDirection = -Vector3.UnitY;
        Vector3 lightColor = Vector3.One;
        float lightIntensity = 1f;

        foreach (var entity in World.Query<Light, Transform3D>())
        {
            ref readonly var light = ref World.Get<Light>(entity);
            ref readonly var lightTransform = ref World.Get<Transform3D>(entity);

            if (light.Type == LightType.Directional)
            {
                lightDirection = lightTransform.Forward;
                lightColor = light.Color;
                lightIntensity = light.Intensity;
                break; // Use first directional light
            }
        }

        // Build render queue
        renderQueue.Clear();
        foreach (var entity in World.Query<Transform3D, Renderable>())
        {
            ref readonly var renderable = ref World.Get<Renderable>(entity);
            renderQueue.Add((entity, renderable.Layer));
        }

        // Sort by layer (stable sort preserves order within same layer)
        renderQueue.Sort((a, b) => a.Layer.CompareTo(b.Layer));

        // Render each entity
        int currentShaderId = -1;
        int currentTextureId = -1;

        foreach (var (entity, _) in renderQueue)
        {
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            ref readonly var renderable = ref World.Get<Renderable>(entity);

            // Get mesh data
            var meshData = graphics.MeshManager.GetMesh(renderable.MeshId);
            if (meshData is null)
            {
                continue;
            }

            // Get material (use solid shader if no material component)
            int shaderId = graphics.SolidShaderId;
            int textureId = graphics.WhiteTextureId;
            Vector4 color = Vector4.One;
            Vector3 emissive = Vector3.Zero;

            if (World.Has<Material>(entity))
            {
                ref readonly var material = ref World.Get<Material>(entity);
                shaderId = material.ShaderId > 0 ? material.ShaderId : graphics.LitShaderId;
                textureId = material.TextureId > 0 ? material.TextureId : graphics.WhiteTextureId;
                color = material.Color;
                emissive = material.EmissiveColor;
            }

            // Bind shader
            var shaderData = graphics.ShaderManager.GetShader(shaderId);
            if (shaderData is null)
            {
                continue;
            }

            if (currentShaderId != shaderId)
            {
                gl.UseProgram(shaderData.Handle);
                currentShaderId = shaderId;

                // Set per-frame uniforms
                graphics.ShaderManager.SetUniform(shaderId, "uView", viewMatrix);
                graphics.ShaderManager.SetUniform(shaderId, "uProjection", projectionMatrix);
                graphics.ShaderManager.SetUniform(shaderId, "uCameraPosition", cameraTransform.Position);
                graphics.ShaderManager.SetUniform(shaderId, "uLightDirection", lightDirection);
                graphics.ShaderManager.SetUniform(shaderId, "uLightColor", lightColor);
                graphics.ShaderManager.SetUniform(shaderId, "uLightIntensity", lightIntensity);
            }

            // Bind texture
            var textureData = graphics.TextureManager.GetTexture(textureId);
            if (textureData is not null && currentTextureId != textureId)
            {
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, textureData.Handle);
                graphics.ShaderManager.SetUniform(shaderId, "uTexture", 0);
                currentTextureId = textureId;
            }

            // Set per-object uniforms
            Matrix4x4 modelMatrix = transform.ToMatrix();
            graphics.ShaderManager.SetUniform(shaderId, "uModel", modelMatrix);
            graphics.ShaderManager.SetUniform(shaderId, "uColor", color);
            graphics.ShaderManager.SetUniform(shaderId, "uEmissive", emissive);

            // Draw mesh
            gl.BindVertexArray(meshData.Vao);
            unsafe
            {
                gl.DrawElements(
                    PrimitiveType.Triangles,
                    (uint)meshData.IndexCount,
                    DrawElementsType.UnsignedInt,
                    null);
            }
        }

        // Cleanup
        gl.BindVertexArray(0);
        gl.UseProgram(0);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        renderQueue.Clear();
        base.Dispose();
    }
}
