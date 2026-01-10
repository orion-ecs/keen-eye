namespace KeenEyes.Graphics.Silk.Shaders;

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

    /// <summary>
    /// PBR vertex shader with tangent space support for normal mapping.
    /// </summary>
    public const string PbrVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        layout (location = 3) in vec4 aTangent;
        layout (location = 4) in vec4 aColor;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec3 vWorldPos;
        out vec3 vNormal;
        out vec2 vTexCoord;
        out vec4 vColor;
        out mat3 vTBN;

        void main()
        {
            vec4 worldPos = uModel * vec4(aPosition, 1.0);
            vWorldPos = worldPos.xyz;

            // Calculate normal matrix (inverse transpose of model matrix upper 3x3)
            mat3 normalMatrix = mat3(transpose(inverse(uModel)));

            // Transform normal to world space
            vec3 N = normalize(normalMatrix * aNormal);
            vNormal = N;

            // Transform tangent to world space and compute TBN matrix
            vec3 T = normalize(normalMatrix * aTangent.xyz);
            // Re-orthogonalize T with respect to N
            T = normalize(T - dot(T, N) * N);
            // Compute bitangent using tangent handedness
            vec3 B = cross(N, T) * aTangent.w;
            vTBN = mat3(T, B, N);

            vTexCoord = aTexCoord;
            vColor = aColor;

            gl_Position = uProjection * uView * worldPos;
        }
        """;

    /// <summary>
    /// PBR fragment shader implementing Cook-Torrance BRDF with metallic-roughness workflow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implements the standard glTF 2.0 PBR metallic-roughness shading model with:
    /// </para>
    /// <list type="bullet">
    /// <item><description>GGX/Trowbridge-Reitz normal distribution</description></item>
    /// <item><description>Schlick-GGX geometry function</description></item>
    /// <item><description>Fresnel-Schlick approximation</description></item>
    /// <item><description>Support for up to 8 lights (directional, point, spot)</description></item>
    /// </list>
    /// <para>
    /// Texture slots:
    /// 0 = Base Color, 1 = Normal, 2 = MetallicRoughness (G=roughness, B=metallic),
    /// 3 = Occlusion (R channel), 4 = Emissive
    /// </para>
    /// </remarks>
    public const string PbrFragmentShader = """
        #version 330 core

        const float PI = 3.14159265359;
        const int MAX_LIGHTS = 8;

        // Light types
        const int LIGHT_DIRECTIONAL = 0;
        const int LIGHT_POINT = 1;
        const int LIGHT_SPOT = 2;

        in vec3 vWorldPos;
        in vec3 vNormal;
        in vec2 vTexCoord;
        in vec4 vColor;
        in mat3 vTBN;

        // PBR textures
        uniform sampler2D uBaseColorMap;
        uniform sampler2D uNormalMap;
        uniform sampler2D uMetallicRoughnessMap;
        uniform sampler2D uOcclusionMap;
        uniform sampler2D uEmissiveMap;

        // Material factors
        uniform vec4 uBaseColorFactor;
        uniform float uMetallicFactor;
        uniform float uRoughnessFactor;
        uniform vec3 uEmissiveFactor;
        uniform float uOcclusionStrength;
        uniform float uNormalScale;
        uniform float uAlphaCutoff;

        // Texture presence flags (1 = has texture, 0 = no texture)
        uniform int uHasBaseColorMap;
        uniform int uHasNormalMap;
        uniform int uHasMetallicRoughnessMap;
        uniform int uHasOcclusionMap;
        uniform int uHasEmissiveMap;

        // Camera
        uniform vec3 uCameraPosition;

        // Lights
        uniform int uLightCount;
        uniform vec3 uLightPositions[MAX_LIGHTS];
        uniform vec3 uLightDirections[MAX_LIGHTS];
        uniform vec3 uLightColors[MAX_LIGHTS];
        uniform float uLightIntensities[MAX_LIGHTS];
        uniform int uLightTypes[MAX_LIGHTS];
        uniform float uLightRanges[MAX_LIGHTS];
        uniform float uLightInnerCones[MAX_LIGHTS];
        uniform float uLightOuterCones[MAX_LIGHTS];

        out vec4 FragColor;

        // GGX/Trowbridge-Reitz normal distribution function
        float DistributionGGX(vec3 N, vec3 H, float roughness)
        {
            float a = roughness * roughness;
            float a2 = a * a;
            float NdotH = max(dot(N, H), 0.0);
            float NdotH2 = NdotH * NdotH;

            float nom = a2;
            float denom = (NdotH2 * (a2 - 1.0) + 1.0);
            denom = PI * denom * denom;

            return nom / max(denom, 0.0001);
        }

        // Schlick-GGX geometry function for direct lighting
        float GeometrySchlickGGX(float NdotV, float roughness)
        {
            float r = roughness + 1.0;
            float k = (r * r) / 8.0;

            float nom = NdotV;
            float denom = NdotV * (1.0 - k) + k;

            return nom / max(denom, 0.0001);
        }

        // Smith's method combining geometry obstruction and shadowing
        float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
        {
            float NdotV = max(dot(N, V), 0.0);
            float NdotL = max(dot(N, L), 0.0);
            float ggx2 = GeometrySchlickGGX(NdotV, roughness);
            float ggx1 = GeometrySchlickGGX(NdotL, roughness);

            return ggx1 * ggx2;
        }

        // Fresnel-Schlick approximation
        vec3 FresnelSchlick(float cosTheta, vec3 F0)
        {
            return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
        }

        // Get normal from normal map or vertex normal
        vec3 getNormal()
        {
            if (uHasNormalMap == 1)
            {
                vec3 tangentNormal = texture(uNormalMap, vTexCoord).rgb * 2.0 - 1.0;
                tangentNormal.xy *= uNormalScale;
                return normalize(vTBN * tangentNormal);
            }
            return normalize(vNormal);
        }

        // Calculate light attenuation for point/spot lights
        float getAttenuation(vec3 lightPos, float range)
        {
            float distance = length(lightPos - vWorldPos);
            // Physically-based inverse square falloff with range limit
            float attenuation = 1.0 / (distance * distance + 1.0);
            // Smooth falloff at range boundary
            float rangeFactor = clamp(1.0 - pow(distance / range, 4.0), 0.0, 1.0);
            return attenuation * rangeFactor * rangeFactor;
        }

        // Calculate spotlight intensity
        float getSpotIntensity(vec3 L, vec3 spotDir, float innerCone, float outerCone)
        {
            float theta = dot(L, normalize(-spotDir));
            float epsilon = innerCone - outerCone;
            return clamp((theta - outerCone) / epsilon, 0.0, 1.0);
        }

        void main()
        {
            // Sample base color
            vec4 baseColor = uBaseColorFactor * vColor;
            if (uHasBaseColorMap == 1)
            {
                baseColor *= texture(uBaseColorMap, vTexCoord);
            }

            // Alpha cutoff for masked materials
            if (baseColor.a < uAlphaCutoff)
            {
                discard;
            }

            // Sample metallic-roughness
            float metallic = uMetallicFactor;
            float roughness = uRoughnessFactor;
            if (uHasMetallicRoughnessMap == 1)
            {
                vec4 mrSample = texture(uMetallicRoughnessMap, vTexCoord);
                // glTF spec: roughness in G channel, metallic in B channel
                roughness *= mrSample.g;
                metallic *= mrSample.b;
            }
            // Clamp roughness to avoid division issues
            roughness = clamp(roughness, 0.04, 1.0);

            // Get surface normal
            vec3 N = getNormal();
            vec3 V = normalize(uCameraPosition - vWorldPos);

            // Calculate F0 (reflectance at normal incidence)
            // Dielectrics use 0.04, metals use their albedo
            vec3 F0 = vec3(0.04);
            F0 = mix(F0, baseColor.rgb, metallic);

            // Accumulate light contribution
            vec3 Lo = vec3(0.0);

            for (int i = 0; i < uLightCount && i < MAX_LIGHTS; i++)
            {
                vec3 L;
                vec3 radiance;

                if (uLightTypes[i] == LIGHT_DIRECTIONAL)
                {
                    // Directional light
                    L = normalize(-uLightDirections[i]);
                    radiance = uLightColors[i] * uLightIntensities[i];
                }
                else if (uLightTypes[i] == LIGHT_POINT)
                {
                    // Point light
                    L = normalize(uLightPositions[i] - vWorldPos);
                    float attenuation = getAttenuation(uLightPositions[i], uLightRanges[i]);
                    radiance = uLightColors[i] * uLightIntensities[i] * attenuation;
                }
                else // LIGHT_SPOT
                {
                    // Spot light
                    L = normalize(uLightPositions[i] - vWorldPos);
                    float attenuation = getAttenuation(uLightPositions[i], uLightRanges[i]);
                    float spotIntensity = getSpotIntensity(L, uLightDirections[i],
                                                          uLightInnerCones[i], uLightOuterCones[i]);
                    radiance = uLightColors[i] * uLightIntensities[i] * attenuation * spotIntensity;
                }

                vec3 H = normalize(V + L);

                // Cook-Torrance BRDF
                float NDF = DistributionGGX(N, H, roughness);
                float G = GeometrySmith(N, V, L, roughness);
                vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

                vec3 numerator = NDF * G * F;
                float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
                vec3 specular = numerator / denominator;

                // Energy conservation: kS is specular contribution, kD is diffuse
                vec3 kS = F;
                vec3 kD = vec3(1.0) - kS;
                // Metals have no diffuse component
                kD *= 1.0 - metallic;

                // Lambertian diffuse
                float NdotL = max(dot(N, L), 0.0);
                Lo += (kD * baseColor.rgb / PI + specular) * radiance * NdotL;
            }

            // Ambient lighting (simple approximation)
            vec3 ambient = vec3(0.03) * baseColor.rgb;

            // Apply ambient occlusion
            if (uHasOcclusionMap == 1)
            {
                float ao = texture(uOcclusionMap, vTexCoord).r;
                ambient *= mix(1.0, ao, uOcclusionStrength);
            }

            vec3 color = ambient + Lo;

            // Add emissive
            vec3 emissive = uEmissiveFactor;
            if (uHasEmissiveMap == 1)
            {
                emissive *= texture(uEmissiveMap, vTexCoord).rgb;
            }
            color += emissive;

            // HDR tonemapping (Reinhard)
            color = color / (color + vec3(1.0));

            // Gamma correction
            color = pow(color, vec3(1.0 / 2.2));

            FragColor = vec4(color, baseColor.a);
        }
        """;
}
