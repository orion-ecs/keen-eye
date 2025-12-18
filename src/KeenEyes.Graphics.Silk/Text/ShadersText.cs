namespace KeenEyes.Graphics.Silk.Text;

/// <summary>
/// Contains shader source code for text rendering.
/// </summary>
internal static class ShadersText
{
    /// <summary>
    /// Vertex shader for text rendering with position, texture coordinates, and color.
    /// Matches FontStashSharp's VertexPositionColorTexture layout:
    /// - Position (vec3) at location 0
    /// - Color (vec4 from normalized bytes) at location 1
    /// - TexCoord (vec2) at location 2
    /// </summary>
    public const string VertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec4 aColor;
        layout (location = 2) in vec2 aTexCoord;

        uniform mat4 uProjection;

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            gl_Position = uProjection * vec4(aPosition, 1.0);
            vTexCoord = aTexCoord;
            vColor = aColor;
        }
        """;

    /// <summary>
    /// Fragment shader for text rendering.
    /// FontStashSharp uses premultiplied alpha where R=G=B=A=coverage.
    /// Multiplying vertex color by all texture channels gives correct blending.
    /// </summary>
    public const string FragmentShader = """
        #version 330 core

        in vec2 vTexCoord;
        in vec4 vColor;

        uniform sampler2D uTexture;

        out vec4 FragColor;

        void main()
        {
            // FontStashSharp premultiplied alpha: multiply vertex color by texture
            vec4 tex = texture(uTexture, vTexCoord);
            FragColor = vColor * tex;
        }
        """;
}
