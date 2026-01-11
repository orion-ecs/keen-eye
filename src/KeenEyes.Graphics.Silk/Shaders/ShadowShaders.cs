namespace KeenEyes.Graphics.Silk.Shaders;

/// <summary>
/// Contains shadow mapping shader source code.
/// </summary>
internal static class ShadowShaders
{
    /// <summary>
    /// Depth-only vertex shader for directional and spot light shadows.
    /// Uses a single light-space matrix to transform vertices.
    /// </summary>
    public const string DepthVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        uniform mat4 uModel;
        uniform mat4 uLightSpaceMatrix;

        void main()
        {
            gl_Position = uLightSpaceMatrix * uModel * vec4(aPosition, 1.0);
        }
        """;

    /// <summary>
    /// Depth-only fragment shader for directional and spot light shadows.
    /// Outputs nothing - depth is written automatically.
    /// </summary>
    public const string DepthFragmentShader = """
        #version 330 core

        void main()
        {
            // Depth is automatically written to the depth buffer.
            // We could optionally use gl_FragDepth = gl_FragCoord.z; for custom depth.
        }
        """;

    /// <summary>
    /// Depth-only vertex shader with alpha cutoff support.
    /// Used for transparent objects that still cast shadows.
    /// </summary>
    public const string DepthAlphaVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 2) in vec2 aTexCoord;

        uniform mat4 uModel;
        uniform mat4 uLightSpaceMatrix;

        out vec2 vTexCoord;

        void main()
        {
            gl_Position = uLightSpaceMatrix * uModel * vec4(aPosition, 1.0);
            vTexCoord = aTexCoord;
        }
        """;

    /// <summary>
    /// Depth-only fragment shader with alpha cutoff support.
    /// Discards fragments below the alpha threshold.
    /// </summary>
    public const string DepthAlphaFragmentShader = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uBaseColorMap;
        uniform float uAlphaCutoff;

        void main()
        {
            float alpha = texture(uBaseColorMap, vTexCoord).a;
            if (alpha < uAlphaCutoff) {
                discard;
            }
            // Depth is automatically written.
        }
        """;

    /// <summary>
    /// Point light shadow vertex shader.
    /// Transforms to world space for geometry shader processing.
    /// </summary>
    public const string PointShadowVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        uniform mat4 uModel;

        void main()
        {
            gl_Position = uModel * vec4(aPosition, 1.0);
        }
        """;

    /// <summary>
    /// Point light shadow geometry shader.
    /// Renders the mesh to all 6 cubemap faces in a single pass.
    /// </summary>
    public const string PointShadowGeometryShader = """
        #version 330 core

        layout(triangles) in;
        layout(triangle_strip, max_vertices = 18) out;

        uniform mat4 uShadowMatrices[6];

        out vec4 vFragPos;

        void main()
        {
            for (int face = 0; face < 6; ++face)
            {
                gl_Layer = face;
                for (int i = 0; i < 3; ++i)
                {
                    vFragPos = gl_in[i].gl_Position;
                    gl_Position = uShadowMatrices[face] * vFragPos;
                    EmitVertex();
                }
                EndPrimitive();
            }
        }
        """;

    /// <summary>
    /// Point light shadow fragment shader.
    /// Writes linear depth for omnidirectional shadow mapping.
    /// </summary>
    public const string PointShadowFragmentShader = """
        #version 330 core

        in vec4 vFragPos;

        uniform vec3 uLightPos;
        uniform float uFarPlane;

        void main()
        {
            // Calculate distance from light to fragment
            float lightDistance = length(vFragPos.xyz - uLightPos);

            // Normalize to [0, 1] range
            lightDistance = lightDistance / uFarPlane;

            // Write to depth buffer
            gl_FragDepth = lightDistance;
        }
        """;

    /// <summary>
    /// Shadow sampling function for the PBR shader.
    /// Calculates shadow factor for directional lights with PCF.
    /// </summary>
    public const string DirectionalShadowFunction = """
        // Shadow calculation for directional lights with CSM
        float calculateDirectionalShadow(vec3 fragPosWorld, vec3 normal, vec3 lightDir, int lightIndex)
        {
            // Determine which cascade to use based on view-space depth
            vec4 fragPosView = uView * vec4(fragPosWorld, 1.0);
            float depth = abs(fragPosView.z);

            int cascadeIndex = 0;
            for (int i = 0; i < uCascadeCount; ++i)
            {
                if (depth < uCascadeSplits[i])
                {
                    cascadeIndex = i;
                    break;
                }
            }

            // Transform to light space
            vec4 fragPosLightSpace = uLightSpaceMatrices[cascadeIndex] * vec4(fragPosWorld, 1.0);
            vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

            // Transform to [0, 1] range
            projCoords = projCoords * 0.5 + 0.5;

            // Check if outside shadow map
            if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0 ||
                projCoords.y < 0.0 || projCoords.y > 1.0)
            {
                return 1.0; // No shadow outside frustum
            }

            // Apply bias based on surface angle
            float bias = max(uShadowNormalBias * (1.0 - dot(normal, lightDir)), uShadowDepthBias);
            float currentDepth = projCoords.z - bias;

            // PCF filtering
            float shadow = 0.0;
            vec2 texelSize = 1.0 / textureSize(uShadowMaps[cascadeIndex], 0);

            for (int x = -1; x <= 1; ++x)
            {
                for (int y = -1; y <= 1; ++y)
                {
                    float pcfDepth = texture(uShadowMaps[cascadeIndex], projCoords.xy + vec2(x, y) * texelSize).r;
                    shadow += currentDepth > pcfDepth ? 0.0 : 1.0;
                }
            }
            shadow /= 9.0;

            return shadow;
        }
        """;

    /// <summary>
    /// Shadow sampling function for point lights.
    /// Uses cubemap sampling with linear depth comparison.
    /// </summary>
    public const string PointShadowFunction = """
        // Shadow calculation for point lights
        float calculatePointShadow(vec3 fragPosWorld, vec3 lightPos, float farPlane, int lightIndex)
        {
            // Direction from light to fragment
            vec3 fragToLight = fragPosWorld - lightPos;

            // Sample from cubemap
            float closestDepth = texture(uPointShadowMaps[lightIndex], fragToLight).r;

            // Convert from [0, 1] to actual depth
            closestDepth *= farPlane;

            // Current depth (distance from light to fragment)
            float currentDepth = length(fragToLight);

            // Apply bias
            float bias = 0.05;

            // Check if in shadow
            float shadow = currentDepth - bias > closestDepth ? 0.0 : 1.0;

            return shadow;
        }
        """;

    /// <summary>
    /// Shadow sampling function for spot lights.
    /// Similar to directional but with single matrix and no cascades.
    /// </summary>
    public const string SpotShadowFunction = """
        // Shadow calculation for spot lights
        float calculateSpotShadow(vec3 fragPosWorld, vec3 normal, vec3 lightDir, mat4 lightSpaceMatrix, int lightIndex)
        {
            // Transform to light space
            vec4 fragPosLightSpace = lightSpaceMatrix * vec4(fragPosWorld, 1.0);
            vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

            // Transform to [0, 1] range
            projCoords = projCoords * 0.5 + 0.5;

            // Check if outside shadow map
            if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0 ||
                projCoords.y < 0.0 || projCoords.y > 1.0)
            {
                return 1.0;
            }

            // Apply bias
            float bias = max(0.01 * (1.0 - dot(normal, lightDir)), 0.005);
            float currentDepth = projCoords.z - bias;

            // PCF filtering
            float shadow = 0.0;
            vec2 texelSize = 1.0 / textureSize(uSpotShadowMaps[lightIndex], 0);

            for (int x = -1; x <= 1; ++x)
            {
                for (int y = -1; y <= 1; ++y)
                {
                    float pcfDepth = texture(uSpotShadowMaps[lightIndex], projCoords.xy + vec2(x, y) * texelSize).r;
                    shadow += currentDepth > pcfDepth ? 0.0 : 1.0;
                }
            }
            shadow /= 9.0;

            return shadow;
        }
        """;

    /// <summary>
    /// Instanced depth-only vertex shader for shadow mapping with GPU instancing.
    /// </summary>
    public const string InstancedDepthVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        // Instance data
        layout (location = 5) in mat4 aInstanceModel;

        uniform mat4 uLightSpaceMatrix;

        void main()
        {
            gl_Position = uLightSpaceMatrix * aInstanceModel * vec4(aPosition, 1.0);
        }
        """;
}
