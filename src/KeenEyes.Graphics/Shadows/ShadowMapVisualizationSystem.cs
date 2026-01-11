using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Shadows;

/// <summary>
/// Debug system that renders shadow map visualizations as screen overlays.
/// </summary>
/// <remarks>
/// <para>
/// This system displays shadow maps as small thumbnails in the corner of the screen,
/// useful for debugging shadow rendering issues. It supports:
/// </para>
/// <list type="bullet">
/// <item>Directional light cascade shadow maps (up to 4 cascades)</item>
/// <item>Spot light shadow maps</item>
/// <item>Point light cubemap shadow maps (6 faces)</item>
/// </list>
/// <para>
/// The system runs in the Render phase after the main RenderSystem and requires
/// a ShadowRenderingSystem to be present for accessing shadow data.
/// </para>
/// </remarks>
public sealed class ShadowMapVisualizationSystem : ISystem
{
    private IWorld? world;
    private IGraphicsContext? graphics;
    private ShaderHandle depthVisualizationShader;
    private ShaderHandle cubemapVisualizationShader;
    private MeshHandle quadMesh;
    private bool shadersCreated;
    private bool disposed;

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the shadow rendering system to visualize.
    /// </summary>
    public ShadowRenderingSystem? ShadowSystem { get; set; }

    /// <summary>
    /// Gets or sets the visualization mode.
    /// </summary>
    public ShadowVisualizationMode Mode { get; set; } = ShadowVisualizationMode.Cascades;

    /// <summary>
    /// Gets or sets the size of the visualization thumbnails (0.0 to 1.0, as fraction of screen height).
    /// </summary>
    public float ThumbnailSize { get; set; } = 0.2f;

    /// <summary>
    /// Gets or sets the padding between thumbnails (in pixels).
    /// </summary>
    public float Padding { get; set; } = 10f;

    /// <summary>
    /// Gets or sets whether to show cascade color tints for directional shadows.
    /// </summary>
    public bool ShowCascadeColors { get; set; } = true;

    /// <summary>
    /// Gets or sets the opacity of the visualization overlay (0.0 to 1.0).
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the index of the specific shadow map to visualize.
    /// For cascades: 0-3, for point light faces: 0-5, for spot/point lights: light index.
    /// Set to -1 to show all available.
    /// </summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the corner where visualizations are displayed.
    /// </summary>
    public VisualizationCorner Corner { get; set; } = VisualizationCorner.BottomLeft;

    /// <summary>
    /// Gets or sets the viewport width in pixels.
    /// </summary>
    /// <remarks>
    /// This should be updated when the window/viewport resizes.
    /// </remarks>
    public int ViewportWidth { get; set; } = 1280;

    /// <summary>
    /// Gets or sets the viewport height in pixels.
    /// </summary>
    /// <remarks>
    /// This should be updated when the window/viewport resizes.
    /// </remarks>
    public int ViewportHeight { get; set; } = 720;

    /// <summary>
    /// Updates the viewport dimensions.
    /// </summary>
    /// <param name="width">The viewport width in pixels.</param>
    /// <param name="height">The viewport height in pixels.</param>
    public void SetViewportSize(int width, int height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
    }

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;

        if (!world.TryGetExtension<IGraphicsContext>(out graphics))
        {
            throw new InvalidOperationException("ShadowMapVisualizationSystem requires IGraphicsContext extension");
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (!Enabled || world is null || graphics is null || !graphics.IsInitialized || ShadowSystem?.ShadowManager is null)
        {
            return;
        }

        // Create shaders and quad mesh on first use
        if (!shadersCreated)
        {
            CreateResources();
            shadersCreated = true;
        }

        var shadowManager = ShadowSystem.ShadowManager;

        switch (Mode)
        {
            case ShadowVisualizationMode.Cascades:
                RenderCascadeVisualization(shadowManager);
                break;

            case ShadowVisualizationMode.SpotLights:
                RenderSpotLightVisualization(shadowManager);
                break;

            case ShadowVisualizationMode.PointLights:
                RenderPointLightVisualization(shadowManager);
                break;

            case ShadowVisualizationMode.All:
                RenderCascadeVisualization(shadowManager);
                RenderSpotLightVisualization(shadowManager);
                RenderPointLightVisualization(shadowManager);
                break;
        }
    }

    private void CreateResources()
    {
        // Fullscreen quad vertex shader
        const string quadVertexShader = """
            #version 330 core

            layout (location = 0) in vec3 aPosition;
            layout (location = 2) in vec2 aTexCoord;

            uniform mat4 uTransform;

            out vec2 vTexCoord;

            void main()
            {
                gl_Position = uTransform * vec4(aPosition, 1.0);
                vTexCoord = aTexCoord;
            }
            """;

        // 2D depth visualization fragment shader
        const string depthFragmentShader = """
            #version 330 core

            in vec2 vTexCoord;

            uniform sampler2D uDepthTexture;
            uniform vec3 uTintColor;
            uniform float uOpacity;

            out vec4 FragColor;

            void main()
            {
                float depth = texture(uDepthTexture, vTexCoord).r;

                // Apply tint color
                vec3 color = vec3(depth) * uTintColor;
                FragColor = vec4(color, uOpacity);
            }
            """;

        // Cubemap depth visualization fragment shader
        const string cubemapFragmentShader = """
            #version 330 core

            in vec2 vTexCoord;

            uniform samplerCube uDepthCubemap;
            uniform int uFaceIndex;
            uniform vec3 uTintColor;
            uniform float uOpacity;

            out vec4 FragColor;

            vec3 getFaceDirection(int face, vec2 uv)
            {
                vec2 coord = uv * 2.0 - 1.0;

                if (face == 0) return vec3( 1.0, -coord.y, -coord.x); // +X
                if (face == 1) return vec3(-1.0, -coord.y,  coord.x); // -X
                if (face == 2) return vec3( coord.x,  1.0,  coord.y); // +Y
                if (face == 3) return vec3( coord.x, -1.0, -coord.y); // -Y
                if (face == 4) return vec3( coord.x, -coord.y,  1.0); // +Z
                return vec3(-coord.x, -coord.y, -1.0);                // -Z
            }

            void main()
            {
                vec3 dir = getFaceDirection(uFaceIndex, vTexCoord);
                float depth = texture(uDepthCubemap, dir).r;

                vec3 color = vec3(depth) * uTintColor;
                FragColor = vec4(color, uOpacity);
            }
            """;

        depthVisualizationShader = graphics!.CreateShader(quadVertexShader, depthFragmentShader);
        cubemapVisualizationShader = graphics.CreateShader(quadVertexShader, cubemapFragmentShader);

        // Create a simple quad mesh (2x2, will be scaled by transform)
        quadMesh = graphics.CreateQuad(2f, 2f);
    }

    private void RenderCascadeVisualization(ShadowMapManager shadowManager)
    {
        var directionalLights = shadowManager.DirectionalShadowLights.ToList();
        if (directionalLights.Count == 0)
        {
            return;
        }

        // Get the first directional light's shadow data
        var shadowData = shadowManager.GetDirectionalShadowData(directionalLights[0]);
        if (!shadowData.HasValue)
        {
            return;
        }

        var data = shadowData.Value;
        int cascadeCount = data.Settings.ClampedCascadeCount;

        // Cascade colors: red, green, blue, yellow
        Vector3[] cascadeColors =
        [
            new Vector3(1.0f, 0.3f, 0.3f),
            new Vector3(0.3f, 1.0f, 0.3f),
            new Vector3(0.3f, 0.3f, 1.0f),
            new Vector3(1.0f, 1.0f, 0.3f)
        ];

        graphics!.BindShader(depthVisualizationShader);
        graphics.SetDepthTest(false);
        graphics.SetBlending(true);

        int startCascade = SelectedIndex >= 0 && SelectedIndex < cascadeCount ? SelectedIndex : 0;
        int endCascade = SelectedIndex >= 0 && SelectedIndex < cascadeCount ? SelectedIndex + 1 : cascadeCount;

        for (int i = startCascade; i < endCascade; i++)
        {
            var renderTarget = data.GetCascadeRenderTarget(i);
            if (!renderTarget.IsValid)
            {
                continue;
            }

            var depthTexture = graphics.GetRenderTargetDepthTexture(renderTarget);
            var transform = CalculateThumbnailTransform(i - startCascade, endCascade - startCascade);

            graphics.BindTexture(depthTexture, 0);
            graphics.SetUniform("uDepthTexture", 0);
            graphics.SetUniform("uTransform", transform);
            graphics.SetUniform("uTintColor", ShowCascadeColors ? cascadeColors[i] : Vector3.One);
            graphics.SetUniform("uOpacity", Opacity);

            graphics.DrawMesh(quadMesh);
        }

        graphics.SetBlending(false);
        graphics.SetDepthTest(true);
    }

    private void RenderSpotLightVisualization(ShadowMapManager shadowManager)
    {
        var spotLights = shadowManager.SpotShadowLights.ToList();
        if (spotLights.Count == 0)
        {
            return;
        }

        graphics!.BindShader(depthVisualizationShader);
        graphics.SetDepthTest(false);
        graphics.SetBlending(true);

        int startIndex = SelectedIndex >= 0 && SelectedIndex < spotLights.Count ? SelectedIndex : 0;
        int endIndex = SelectedIndex >= 0 && SelectedIndex < spotLights.Count ? SelectedIndex + 1 : spotLights.Count;
        int offset = Mode == ShadowVisualizationMode.All ? 4 : 0; // Offset if showing cascades first

        for (int i = startIndex; i < endIndex; i++)
        {
            var shadowData = shadowManager.GetSpotShadowData(spotLights[i]);
            if (!shadowData.HasValue)
            {
                continue;
            }

            var data = shadowData.Value;
            var depthTexture = graphics.GetRenderTargetDepthTexture(data.RenderTarget);
            var transform = CalculateThumbnailTransform(i - startIndex + offset, endIndex - startIndex + offset);

            graphics.BindTexture(depthTexture, 0);
            graphics.SetUniform("uDepthTexture", 0);
            graphics.SetUniform("uTransform", transform);
            graphics.SetUniform("uTintColor", new Vector3(1.0f, 0.6f, 0.2f)); // Orange tint for spot lights
            graphics.SetUniform("uOpacity", Opacity);

            graphics.DrawMesh(quadMesh);
        }

        graphics.SetBlending(false);
        graphics.SetDepthTest(true);
    }

    private void RenderPointLightVisualization(ShadowMapManager shadowManager)
    {
        var pointLights = shadowManager.PointShadowLights.ToList();
        if (pointLights.Count == 0)
        {
            return;
        }

        // For point lights, show the first light's 6 cubemap faces
        var shadowData = shadowManager.GetPointShadowData(pointLights[0]);
        if (!shadowData.HasValue)
        {
            return;
        }

        var data = shadowData.Value;
        var cubemapTexture = graphics!.GetCubemapRenderTargetTexture(data.RenderTarget);

        graphics.BindShader(cubemapVisualizationShader);
        graphics.SetDepthTest(false);
        graphics.SetBlending(true);

        // Face colors: +X=red, -X=cyan, +Y=green, -Y=magenta, +Z=blue, -Z=yellow
        Vector3[] faceColors =
        [
            new Vector3(1.0f, 0.3f, 0.3f), // +X
            new Vector3(0.3f, 1.0f, 1.0f), // -X
            new Vector3(0.3f, 1.0f, 0.3f), // +Y
            new Vector3(1.0f, 0.3f, 1.0f), // -Y
            new Vector3(0.3f, 0.3f, 1.0f), // +Z
            new Vector3(1.0f, 1.0f, 0.3f)  // -Z
        ];

        int startFace = SelectedIndex >= 0 && SelectedIndex < 6 ? SelectedIndex : 0;
        int endFace = SelectedIndex >= 0 && SelectedIndex < 6 ? SelectedIndex + 1 : 6;
        int offset = Mode == ShadowVisualizationMode.All ? 8 : 0; // Offset if showing cascades + spots first

        for (int face = startFace; face < endFace; face++)
        {
            var transform = CalculateThumbnailTransform(face - startFace + offset, endFace - startFace + offset);

            graphics.BindTexture(cubemapTexture, 0);
            graphics.SetUniform("uDepthCubemap", 0);
            graphics.SetUniform("uFaceIndex", face);
            graphics.SetUniform("uTransform", transform);
            graphics.SetUniform("uTintColor", ShowCascadeColors ? faceColors[face] : Vector3.One);
            graphics.SetUniform("uOpacity", Opacity);

            graphics.DrawMesh(quadMesh);
        }

        graphics.SetBlending(false);
        graphics.SetDepthTest(true);
    }

    private Matrix4x4 CalculateThumbnailTransform(int index, int total)
    {
        // Use configured viewport size
        int viewportWidth = ViewportWidth;
        int viewportHeight = ViewportHeight;

        if (viewportWidth == 0 || viewportHeight == 0)
        {
            return Matrix4x4.Identity;
        }

        // Calculate thumbnail size in pixels
        float thumbSize = viewportHeight * ThumbnailSize;
        float padding = Padding;

        // Calculate position based on corner
        float x, y;
        switch (Corner)
        {
            case VisualizationCorner.TopLeft:
                x = padding + index * (thumbSize + padding);
                y = viewportHeight - thumbSize - padding;
                break;
            case VisualizationCorner.TopRight:
                x = viewportWidth - thumbSize - padding - index * (thumbSize + padding);
                y = viewportHeight - thumbSize - padding;
                break;
            case VisualizationCorner.BottomRight:
                x = viewportWidth - thumbSize - padding - index * (thumbSize + padding);
                y = padding;
                break;
            default: // BottomLeft and any future values
                x = padding + index * (thumbSize + padding);
                y = padding;
                break;
        }

        // Convert to NDC (-1 to 1)
        float ndcX = (x / viewportWidth) * 2.0f - 1.0f + (thumbSize / viewportWidth);
        float ndcY = (y / viewportHeight) * 2.0f - 1.0f + (thumbSize / viewportHeight);
        float scaleX = thumbSize / viewportWidth;
        float scaleY = thumbSize / viewportHeight;

        // Create transform matrix (scale then translate)
        return Matrix4x4.CreateScale(scaleX, scaleY, 1.0f) *
               Matrix4x4.CreateTranslation(ndcX, ndcY, 0.0f);
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
            graphics.DeleteShader(depthVisualizationShader);
            graphics.DeleteShader(cubemapVisualizationShader);
            graphics.DeleteMesh(quadMesh);
        }
    }
}

/// <summary>
/// Visualization modes for shadow map debugging.
/// </summary>
public enum ShadowVisualizationMode
{
    /// <summary>
    /// Show directional light cascade shadow maps.
    /// </summary>
    Cascades,

    /// <summary>
    /// Show spot light shadow maps.
    /// </summary>
    SpotLights,

    /// <summary>
    /// Show point light cubemap shadow maps.
    /// </summary>
    PointLights,

    /// <summary>
    /// Show all shadow maps.
    /// </summary>
    All
}

/// <summary>
/// Corner positions for visualization overlay.
/// </summary>
public enum VisualizationCorner
{
    /// <summary>
    /// Top-left corner of the screen.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Top-right corner of the screen.
    /// </summary>
    TopRight,

    /// <summary>
    /// Bottom-left corner of the screen.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Bottom-right corner of the screen.
    /// </summary>
    BottomRight
}
