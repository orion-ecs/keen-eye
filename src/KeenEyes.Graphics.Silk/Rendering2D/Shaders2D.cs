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
}
