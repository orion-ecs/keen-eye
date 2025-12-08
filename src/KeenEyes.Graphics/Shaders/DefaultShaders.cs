namespace KeenEyes.Graphics;

/// <summary>
/// Contains built-in shader source code.
/// </summary>
internal static class DefaultShaders
{
    /// <summary>
    /// Basic unlit vertex shader with MVP transform.
    /// </summary>
    public const string UnlitVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        layout (location = 3) in vec4 aColor;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
            vTexCoord = aTexCoord;
            vColor = aColor;
        }
        """;

    /// <summary>
    /// Basic unlit fragment shader with texture and color.
    /// </summary>
    public const string UnlitFragmentShader = """
        #version 330 core

        in vec2 vTexCoord;
        in vec4 vColor;

        uniform sampler2D uTexture;
        uniform vec4 uColor;

        out vec4 FragColor;

        void main()
        {
            vec4 texColor = texture(uTexture, vTexCoord);
            FragColor = texColor * vColor * uColor;
        }
        """;

    /// <summary>
    /// Basic lit vertex shader with normal transformation.
    /// </summary>
    public const string LitVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        layout (location = 3) in vec4 aColor;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec3 vWorldPos;
        out vec3 vNormal;
        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            vec4 worldPos = uModel * vec4(aPosition, 1.0);
            vWorldPos = worldPos.xyz;
            vNormal = mat3(transpose(inverse(uModel))) * aNormal;
            vTexCoord = aTexCoord;
            vColor = aColor;
            gl_Position = uProjection * uView * worldPos;
        }
        """;

    /// <summary>
    /// Basic lit fragment shader with simple directional lighting.
    /// </summary>
    public const string LitFragmentShader = """
        #version 330 core

        in vec3 vWorldPos;
        in vec3 vNormal;
        in vec2 vTexCoord;
        in vec4 vColor;

        uniform sampler2D uTexture;
        uniform vec4 uColor;
        uniform vec3 uCameraPosition;
        uniform vec3 uLightDirection;
        uniform vec3 uLightColor;
        uniform float uLightIntensity;
        uniform vec3 uEmissive;

        out vec4 FragColor;

        void main()
        {
            vec3 normal = normalize(vNormal);
            vec3 lightDir = normalize(-uLightDirection);

            // Ambient
            vec3 ambient = 0.1 * uLightColor;

            // Diffuse
            float diff = max(dot(normal, lightDir), 0.0);
            vec3 diffuse = diff * uLightColor * uLightIntensity;

            // Specular (Blinn-Phong)
            vec3 viewDir = normalize(uCameraPosition - vWorldPos);
            vec3 halfDir = normalize(lightDir + viewDir);
            float spec = pow(max(dot(normal, halfDir), 0.0), 32.0);
            vec3 specular = spec * uLightColor * 0.5;

            // Sample texture
            vec4 texColor = texture(uTexture, vTexCoord);
            vec3 baseColor = texColor.rgb * vColor.rgb * uColor.rgb;

            // Combine lighting
            vec3 result = (ambient + diffuse) * baseColor + specular + uEmissive;
            FragColor = vec4(result, texColor.a * vColor.a * uColor.a);
        }
        """;

    /// <summary>
    /// Simple solid color vertex shader (no textures).
    /// </summary>
    public const string SolidVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        layout (location = 3) in vec4 aColor;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec4 vColor;

        void main()
        {
            gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
            vColor = aColor;
        }
        """;

    /// <summary>
    /// Simple solid color fragment shader.
    /// </summary>
    public const string SolidFragmentShader = """
        #version 330 core

        in vec4 vColor;

        uniform vec4 uColor;

        out vec4 FragColor;

        void main()
        {
            FragColor = vColor * uColor;
        }
        """;
}
