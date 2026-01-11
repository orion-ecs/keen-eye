namespace KeenEyes.Graphics.Silk.Shaders;

/// <summary>
/// Debug visualization shaders for development and debugging purposes.
/// </summary>
internal static class DebugShaders
{
    /// <summary>
    /// Simple fullscreen quad vertex shader for post-processing and debug overlays.
    /// </summary>
    public const string FullscreenQuadVertexShader = """
        #version 330 core

        out vec2 vTexCoord;

        void main()
        {
            // Generate fullscreen triangle from vertex ID
            // Vertices: (-1,-1), (3,-1), (-1,3) covers entire screen
            float x = float((gl_VertexID & 1) << 2) - 1.0;
            float y = float((gl_VertexID & 2) << 1) - 1.0;
            vTexCoord = vec2((x + 1.0) * 0.5, (y + 1.0) * 0.5);
            gl_Position = vec4(x, y, 0.0, 1.0);
        }
        """;

    /// <summary>
    /// Fragment shader for visualizing 2D depth textures (shadow maps).
    /// </summary>
    public const string DepthVisualizationFragmentShader = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uDepthTexture;
        uniform float uNearPlane;
        uniform float uFarPlane;
        uniform int uLinearize;

        out vec4 FragColor;

        // Linearize depth from [0,1] to view-space distance
        float linearizeDepth(float depth)
        {
            float z = depth * 2.0 - 1.0; // Back to NDC
            return (2.0 * uNearPlane * uFarPlane) / (uFarPlane + uNearPlane - z * (uFarPlane - uNearPlane));
        }

        void main()
        {
            float depth = texture(uDepthTexture, vTexCoord).r;

            if (uLinearize == 1)
            {
                // Linearize and normalize to [0,1]
                float linearDepth = linearizeDepth(depth);
                depth = linearDepth / uFarPlane;
            }

            // Display as grayscale
            FragColor = vec4(vec3(depth), 1.0);
        }
        """;

    /// <summary>
    /// Fragment shader for visualizing cubemap depth textures (point light shadows).
    /// </summary>
    public const string CubemapDepthVisualizationFragmentShader = """
        #version 330 core

        in vec2 vTexCoord;

        uniform samplerCube uDepthCubemap;
        uniform int uFaceIndex; // 0-5: +X, -X, +Y, -Y, +Z, -Z
        uniform float uFarPlane;

        out vec4 FragColor;

        // Get direction for cubemap face based on UV coordinates
        vec3 getFaceDirection(int face, vec2 uv)
        {
            // Map UV from [0,1] to [-1,1]
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

            // Point shadow maps store linear depth / farPlane
            // So depth is already in [0,1] range

            // Display as grayscale
            FragColor = vec4(vec3(depth), 1.0);
        }
        """;

    /// <summary>
    /// Fragment shader for shadow map overlay with cascade coloring.
    /// </summary>
    public const string ShadowOverlayFragmentShader = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uDepthTexture;
        uniform vec3 uTintColor;
        uniform float uOpacity;

        out vec4 FragColor;

        void main()
        {
            float depth = texture(uDepthTexture, vTexCoord).r;

            // Apply tint color with opacity
            vec3 color = vec3(depth) * uTintColor;
            FragColor = vec4(color, uOpacity);
        }
        """;

    /// <summary>
    /// Cascade colors for visualizing CSM cascades.
    /// </summary>
    public static readonly float[][] CascadeColors =
    [
        [1.0f, 0.2f, 0.2f], // Red - Cascade 0 (near)
        [0.2f, 1.0f, 0.2f], // Green - Cascade 1
        [0.2f, 0.2f, 1.0f], // Blue - Cascade 2
        [1.0f, 1.0f, 0.2f]  // Yellow - Cascade 3 (far)
    ];
}
