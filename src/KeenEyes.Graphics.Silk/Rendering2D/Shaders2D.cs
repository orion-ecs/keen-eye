namespace KeenEyes.Graphics.Silk.Rendering2D;

/// <summary>
/// Contains shader source code for 2D rendering.
/// </summary>
internal static class Shaders2D
{
    /// <summary>
    /// Vertex shader for 2D rendering with position, texture coordinates, and color.
    /// </summary>
    public const string VertexShader = """
        #version 330 core

        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aTexCoord;
        layout (location = 2) in vec4 aColor;

        uniform mat4 uProjection;

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
            vTexCoord = aTexCoord;
            vColor = aColor;
        }
        """;

    /// <summary>
    /// Fragment shader for 2D rendering with texture sampling and color multiplication.
    /// </summary>
    public const string FragmentShader = """
        #version 330 core

        in vec2 vTexCoord;
        in vec4 vColor;

        uniform sampler2D uTexture;

        out vec4 FragColor;

        void main()
        {
            vec4 texColor = texture(uTexture, vTexCoord);
            FragColor = texColor * vColor;
        }
        """;

    /// <summary>
    /// Vertex shader for SDF-based rounded rectangle rendering.
    /// </summary>
    /// <remarks>
    /// Passes local coordinates relative to rect center for SDF calculation.
    /// </remarks>
    public const string RoundedRectVertexShader = """
        #version 330 core

        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aLocalPos;
        layout (location = 2) in vec2 aHalfSize;
        layout (location = 3) in float aRadius;
        layout (location = 4) in vec4 aColor;

        uniform mat4 uProjection;

        out vec2 vLocalPos;
        out vec2 vHalfSize;
        out float vRadius;
        out vec4 vColor;

        void main()
        {
            gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
            vLocalPos = aLocalPos;
            vHalfSize = aHalfSize;
            vRadius = aRadius;
            vColor = aColor;
        }
        """;

    /// <summary>
    /// Fragment shader for SDF-based rounded rectangle rendering.
    /// </summary>
    /// <remarks>
    /// Uses signed distance field to calculate smooth, anti-aliased rounded corners.
    /// The SDF for a rounded rectangle is: max(|p| - halfSize + radius, 0) - radius
    /// </remarks>
    public const string RoundedRectFragmentShader = """
        #version 330 core

        in vec2 vLocalPos;
        in vec2 vHalfSize;
        in float vRadius;
        in vec4 vColor;

        out vec4 FragColor;

        float roundedRectSDF(vec2 p, vec2 halfSize, float radius)
        {
            // Distance to the rounded rectangle boundary
            vec2 q = abs(p) - halfSize + radius;
            return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
        }

        void main()
        {
            float dist = roundedRectSDF(vLocalPos, vHalfSize, vRadius);

            // Anti-aliasing: smooth transition at the edge
            // Use 1.0 pixel width for smooth falloff
            float alpha = 1.0 - smoothstep(-1.0, 1.0, dist);

            FragColor = vec4(vColor.rgb, vColor.a * alpha);
        }
        """;
}
