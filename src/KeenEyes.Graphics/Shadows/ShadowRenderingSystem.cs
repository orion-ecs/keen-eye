using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Shadows;

/// <summary>
/// System that renders shadow maps for shadow-casting lights.
/// </summary>
/// <remarks>
/// <para>
/// This system runs before the main <see cref="RenderSystem"/> to render depth-only
/// passes for each shadow-casting light. The resulting shadow maps are then used
/// by the render system for shadow calculations.
/// </para>
/// <para>
/// For directional lights, this system implements Cascaded Shadow Maps (CSM) with
/// configurable cascade counts. For point lights, it uses cubemap shadow maps.
/// </para>
/// </remarks>
public sealed class ShadowRenderingSystem : ISystem
{
    private IWorld? world;
    private IGraphicsContext? graphics;
    private ShadowMapManager? shadowManager;
    private ShaderHandle depthShader;
    private ShaderHandle pointDepthShader;
    private bool shadersCreated;
    private bool disposed;

    // Cached entity data for shadow casters
    private readonly List<(Entity Entity, Transform3D Transform, Renderable Renderable)> shadowCasters = [];

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the shadow settings used for all shadow maps.
    /// </summary>
    public ShadowSettings Settings { get; set; } = ShadowSettings.Default;

    /// <summary>
    /// Gets the shadow map manager for accessing shadow data.
    /// </summary>
    public ShadowMapManager? ShadowManager => shadowManager;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;

        if (!world.TryGetExtension<IGraphicsContext>(out graphics))
        {
            throw new InvalidOperationException("ShadowRenderingSystem requires IGraphicsContext extension");
        }

        shadowManager = new ShadowMapManager(graphics!)
        {
            Settings = Settings
        };
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (world is null || graphics is null || !graphics.IsInitialized || shadowManager is null)
        {
            return;
        }

        // Create shaders on first use (graphics must be initialized)
        if (!shadersCreated)
        {
            CreateShaders();
            shadersCreated = true;
        }

        // Find active camera for cascade calculations
        Camera camera = default;
        Transform3D cameraTransform = default;
        bool foundCamera = false;

        foreach (var entity in world.Query<Camera, Transform3D, MainCameraTag>())
        {
            camera = world.Get<Camera>(entity);
            cameraTransform = world.Get<Transform3D>(entity);
            foundCamera = true;
            break;
        }

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
            return; // No camera, no shadows needed
        }

        // Calculate camera matrices
        Matrix4x4 viewMatrix = camera.ViewMatrix(cameraTransform);
        Matrix4x4 projectionMatrix = camera.ProjectionMatrix();

        // Collect shadow casters
        CollectShadowCasters();

        // Process each shadow-casting light
        foreach (var lightEntity in world.Query<Light, Transform3D>())
        {
            ref readonly var light = ref world.Get<Light>(lightEntity);
            if (!light.CastShadows)
            {
                continue;
            }

            ref readonly var lightTransform = ref world.Get<Transform3D>(lightEntity);

            switch (light.Type)
            {
                case LightType.Directional:
                    RenderDirectionalShadow(
                        lightEntity.Id,
                        lightTransform.Forward(),
                        viewMatrix,
                        projectionMatrix,
                        camera.NearPlane);
                    break;

                case LightType.Point:
                    RenderPointLightShadow(
                        lightEntity.Id,
                        lightTransform.Position,
                        light.Range);
                    break;

                case LightType.Spot:
                    RenderSpotLightShadow(
                        lightEntity.Id,
                        lightTransform.Position,
                        lightTransform.Forward(),
                        light.OuterConeAngle,
                        light.Range);
                    break;
            }
        }
    }

    private void CreateShaders()
    {
        // Create depth-only shader for shadow passes
        // Using a simple shader that just transforms vertices
        const string depthVertexSource = """
            #version 330 core

            layout (location = 0) in vec3 aPosition;

            uniform mat4 uModel;
            uniform mat4 uLightSpaceMatrix;

            void main()
            {
                gl_Position = uLightSpaceMatrix * uModel * vec4(aPosition, 1.0);
            }
            """;

        const string depthFragmentSource = """
            #version 330 core

            void main()
            {
                // Depth is automatically written
            }
            """;

        depthShader = graphics!.CreateShader(depthVertexSource, depthFragmentSource);

        // Point-light pass: the sampled cubemap is the COLOR attachment of the cubemap
        // render target (depth goes to a discarded renderbuffer), so the fragment shader
        // must write the normalized light distance as color — a depth-only shader leaves
        // the sampled faces unwritten and point shadows never appear (#1280).
        const string pointVertexSource = """
            #version 330 core

            layout (location = 0) in vec3 aPosition;

            uniform mat4 uModel;
            uniform mat4 uLightSpaceMatrix;

            out vec3 vWorldPos;

            void main()
            {
                vec4 worldPos = uModel * vec4(aPosition, 1.0);
                vWorldPos = worldPos.xyz;
                gl_Position = uLightSpaceMatrix * worldPos;
            }
            """;

        const string pointFragmentSource = """
            #version 330 core

            in vec3 vWorldPos;

            uniform vec3 uLightPos;
            uniform float uFarPlane;

            out vec4 FragColor;

            void main()
            {
                // Linear distance normalized to [0, 1]; the PBR shader reconstructs it
                // by multiplying with the light's far plane.
                float lightDistance = length(vWorldPos - uLightPos) / uFarPlane;
                FragColor = vec4(lightDistance, 0.0, 0.0, 1.0);
            }
            """;

        pointDepthShader = graphics.CreateShader(pointVertexSource, pointFragmentSource);
    }

    private void CollectShadowCasters()
    {
        shadowCasters.Clear();

        foreach (var entity in world!.Query<Transform3D, Renderable>())
        {
            ref readonly var renderable = ref world.Get<Renderable>(entity);

            // Skip entities that don't cast shadows
            if (!renderable.CastShadows || renderable.MeshId <= 0)
            {
                continue;
            }

            ref readonly var transform = ref world.Get<Transform3D>(entity);
            shadowCasters.Add((entity, transform, renderable));
        }
    }

    private void RenderDirectionalShadow(
        int lightEntityId,
        Vector3 lightDirection,
        Matrix4x4 cameraView,
        Matrix4x4 cameraProjection,
        float cameraNear)
    {
        // Ensure shadow map exists
        shadowManager!.CreateDirectionalShadowMap(lightEntityId, Settings);

        // Update light-space matrices
        shadowManager.UpdateDirectionalLightMatrices(
            lightEntityId,
            lightDirection,
            cameraView,
            cameraProjection,
            cameraNear);

        var shadowData = shadowManager.GetDirectionalShadowData(lightEntityId);
        if (!shadowData.HasValue)
        {
            return;
        }

        var data = shadowData.Value;
        int cascadeCount = data.Settings.ClampedCascadeCount;
        int resolution = data.Settings.ResolutionPixels;

        // Bind depth shader
        graphics!.BindShader(depthShader);

        // Save current render state
        graphics.SetDepthTest(true);
        graphics.SetCulling(true, CullFaceMode.Front); // Render back faces to reduce peter-panning

        // Render each cascade
        for (int cascade = 0; cascade < cascadeCount; cascade++)
        {
            var renderTarget = data.GetCascadeRenderTarget(cascade);
            var lightSpaceMatrix = data.GetLightSpaceMatrix(cascade);

            // Bind render target
            graphics.BindRenderTarget(renderTarget);
            graphics.SetViewport(0, 0, resolution, resolution);
            graphics.Clear(ClearMask.DepthBuffer);

            // Set light-space matrix uniform
            graphics.SetUniform("uLightSpaceMatrix", lightSpaceMatrix);

            // Render all shadow casters
            foreach (var (_, transform, renderable) in shadowCasters)
            {
                Matrix4x4 modelMatrix = transform.Matrix();
                graphics.SetUniform("uModel", modelMatrix);

                var meshHandle = new MeshHandle(renderable.MeshId);
                graphics.DrawMesh(meshHandle);
            }
        }

        // Unbind render target and restore state
        graphics.UnbindRenderTarget();
        graphics.SetCulling(true, CullFaceMode.Back); // Restore normal culling
    }

    private void RenderSpotLightShadow(
        int lightEntityId,
        Vector3 lightPosition,
        Vector3 lightDirection,
        float outerConeAngle,
        float range)
    {
        // Ensure shadow map exists
        shadowManager!.CreateSpotShadowMap(lightEntityId);

        // Update light-space matrix
        shadowManager.UpdateSpotLightMatrix(
            lightEntityId,
            lightPosition,
            lightDirection,
            outerConeAngle,
            range);

        var shadowData = shadowManager.GetSpotShadowData(lightEntityId);
        if (!shadowData.HasValue)
        {
            return;
        }

        var data = shadowData.Value;
        int resolution = Settings.ResolutionPixels;

        // Bind depth shader (reuse the same shader as directional)
        graphics!.BindShader(depthShader);

        // Configure render state
        graphics.SetDepthTest(true);
        graphics.SetCulling(true, CullFaceMode.Front); // Render back faces to reduce peter-panning

        // Bind render target
        graphics.BindRenderTarget(data.RenderTarget);
        graphics.SetViewport(0, 0, resolution, resolution);
        graphics.Clear(ClearMask.DepthBuffer);

        // Set light-space matrix uniform
        graphics.SetUniform("uLightSpaceMatrix", data.LightSpaceMatrix);

        // Render all shadow casters
        foreach (var (_, transform, renderable) in shadowCasters)
        {
            Matrix4x4 modelMatrix = transform.Matrix();
            graphics.SetUniform("uModel", modelMatrix);

            var meshHandle = new MeshHandle(renderable.MeshId);
            graphics.DrawMesh(meshHandle);
        }

        // Unbind render target and restore state
        graphics.UnbindRenderTarget();
        graphics.SetCulling(true, CullFaceMode.Back);
    }

    private void RenderPointLightShadow(
        int lightEntityId,
        Vector3 lightPosition,
        float range)
    {
        // Ensure shadow map exists
        shadowManager!.CreatePointShadowMap(lightEntityId);

        // Update light data
        shadowManager.UpdatePointLightData(lightEntityId, lightPosition, range);

        var shadowData = shadowManager.GetPointShadowData(lightEntityId);
        if (!shadowData.HasValue)
        {
            return;
        }

        var data = shadowData.Value;
        int resolution = data.RenderTarget.Size;

        // Bind the distance-writing point shadow shader
        graphics!.BindShader(pointDepthShader);
        graphics.SetUniform("uLightPos", lightPosition);
        graphics.SetUniform("uFarPlane", range);

        // Configure render state
        graphics.SetDepthTest(true);
        graphics.SetCulling(true, CullFaceMode.Front); // Render back faces to reduce peter-panning

        // Faces clear to 1.0 = max distance = fully lit where nothing is rendered.
        graphics.SetClearColor(new Vector4(1f, 1f, 1f, 1f));

        // Render each cubemap face
        CubemapFace[] faces =
        [
            CubemapFace.PositiveX,
            CubemapFace.NegativeX,
            CubemapFace.PositiveY,
            CubemapFace.NegativeY,
            CubemapFace.PositiveZ,
            CubemapFace.NegativeZ
        ];

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            // Bind the cubemap face as render target
            graphics.BindCubemapRenderTarget(data.RenderTarget, faces[faceIndex]);
            graphics.SetViewport(0, 0, resolution, resolution);
            graphics.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);

            // Get the light-space matrix for this face
            var lightSpaceMatrix = CascadeUtils.GetPointLightShadowMatrix(lightPosition, range, faceIndex);
            graphics.SetUniform("uLightSpaceMatrix", lightSpaceMatrix);

            // Render all shadow casters
            foreach (var (_, transform, renderable) in shadowCasters)
            {
                Matrix4x4 modelMatrix = transform.Matrix();
                graphics.SetUniform("uModel", modelMatrix);

                var meshHandle = new MeshHandle(renderable.MeshId);
                graphics.DrawMesh(meshHandle);
            }
        }

        // Unbind render target and restore state
        graphics.UnbindRenderTarget();
        graphics.SetCulling(true, CullFaceMode.Back);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (shadersCreated && graphics is not null)
        {
            graphics.DeleteShader(depthShader);
            graphics.DeleteShader(pointDepthShader);
        }

        shadowManager?.Dispose();
        shadowCasters.Clear();
    }
}
