namespace KeenEyes.Graphics.Silk.Shaders;

/// <summary>
/// PBR shader variants with shadow mapping support.
/// </summary>
internal static class PbrShadowShaders
{
    /// <summary>
    /// PBR vertex shader with shadow map coordinate output.
    /// </summary>
    public const string PbrShadowVertexShader = """
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
        out float vViewDepth;

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
            T = normalize(T - dot(T, N) * N);
            vec3 B = cross(N, T) * aTangent.w;
            vTBN = mat3(T, B, N);

            vTexCoord = aTexCoord;
            vColor = aColor;

            vec4 viewPos = uView * worldPos;
            vViewDepth = -viewPos.z; // Store view-space depth for cascade selection

            gl_Position = uProjection * viewPos;
        }
        """;

    /// <summary>
    /// PBR fragment shader with Cascaded Shadow Maps support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Extends the standard PBR shader with:
    /// - Up to 4 cascade shadow maps for the first directional light
    /// - PCF soft shadow filtering
    /// - Normal bias and depth bias for shadow acne prevention
    /// </para>
    /// </remarks>
    public const string PbrShadowFragmentShader = """
        #version 330 core

        const float PI = 3.14159265359;
        const int MAX_LIGHTS = 8;
        const int MAX_CASCADES = 4;

        // Light types
        const int LIGHT_DIRECTIONAL = 0;
        const int LIGHT_POINT = 1;
        const int LIGHT_SPOT = 2;

        in vec3 vWorldPos;
        in vec3 vNormal;
        in vec2 vTexCoord;
        in vec4 vColor;
        in mat3 vTBN;
        in float vViewDepth;

        // PBR textures
        uniform sampler2D uBaseColorMap;
        uniform sampler2D uNormalMap;
        uniform sampler2D uMetallicRoughnessMap;
        uniform sampler2D uOcclusionMap;
        uniform sampler2D uEmissiveMap;

        // Shadow maps (cascades for directional light)
        uniform sampler2D uShadowMap0;
        uniform sampler2D uShadowMap1;
        uniform sampler2D uShadowMap2;
        uniform sampler2D uShadowMap3;

        // Material factors
        uniform vec4 uBaseColorFactor;
        uniform float uMetallicFactor;
        uniform float uRoughnessFactor;
        uniform vec3 uEmissiveFactor;
        uniform float uOcclusionStrength;
        uniform float uNormalScale;
        uniform float uAlphaCutoff;

        // Texture presence flags
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

        // Shadow uniforms
        uniform int uShadowEnabled;
        uniform int uCascadeCount;
        uniform mat4 uLightSpaceMatrix0;
        uniform mat4 uLightSpaceMatrix1;
        uniform mat4 uLightSpaceMatrix2;
        uniform mat4 uLightSpaceMatrix3;
        uniform float uCascadeSplit0;
        uniform float uCascadeSplit1;
        uniform float uCascadeSplit2;
        uniform float uCascadeSplit3;
        uniform float uShadowBias;
        uniform float uShadowNormalBias;

        out vec4 FragColor;

        // ==================== PBR Functions ====================

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

        float GeometrySchlickGGX(float NdotV, float roughness)
        {
            float r = roughness + 1.0;
            float k = (r * r) / 8.0;
            float nom = NdotV;
            float denom = NdotV * (1.0 - k) + k;
            return nom / max(denom, 0.0001);
        }

        float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
        {
            float NdotV = max(dot(N, V), 0.0);
            float NdotL = max(dot(N, L), 0.0);
            float ggx2 = GeometrySchlickGGX(NdotV, roughness);
            float ggx1 = GeometrySchlickGGX(NdotL, roughness);
            return ggx1 * ggx2;
        }

        vec3 FresnelSchlick(float cosTheta, vec3 F0)
        {
            return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
        }

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

        float getAttenuation(vec3 lightPos, float range)
        {
            float distance = length(lightPos - vWorldPos);
            float attenuation = 1.0 / (distance * distance + 1.0);
            float rangeFactor = clamp(1.0 - pow(distance / range, 4.0), 0.0, 1.0);
            return attenuation * rangeFactor * rangeFactor;
        }

        float getSpotIntensity(vec3 L, vec3 spotDir, float innerCone, float outerCone)
        {
            float theta = dot(L, normalize(-spotDir));
            float epsilon = innerCone - outerCone;
            return clamp((theta - outerCone) / epsilon, 0.0, 1.0);
        }

        // ==================== Shadow Functions ====================

        // Select cascade based on view-space depth
        int selectCascade()
        {
            if (vViewDepth < uCascadeSplit0) return 0;
            if (uCascadeCount > 1 && vViewDepth < uCascadeSplit1) return 1;
            if (uCascadeCount > 2 && vViewDepth < uCascadeSplit2) return 2;
            if (uCascadeCount > 3 && vViewDepth < uCascadeSplit3) return 3;
            return uCascadeCount - 1;
        }

        // Get light-space matrix for cascade
        mat4 getLightSpaceMatrix(int cascade)
        {
            if (cascade == 0) return uLightSpaceMatrix0;
            if (cascade == 1) return uLightSpaceMatrix1;
            if (cascade == 2) return uLightSpaceMatrix2;
            return uLightSpaceMatrix3;
        }

        // Sample shadow map for cascade
        float sampleShadowMap(int cascade, vec2 coords)
        {
            if (cascade == 0) return texture(uShadowMap0, coords).r;
            if (cascade == 1) return texture(uShadowMap1, coords).r;
            if (cascade == 2) return texture(uShadowMap2, coords).r;
            return texture(uShadowMap3, coords).r;
        }

        // Get shadow map texel size for cascade
        vec2 getShadowMapTexelSize(int cascade)
        {
            if (cascade == 0) return 1.0 / textureSize(uShadowMap0, 0);
            if (cascade == 1) return 1.0 / textureSize(uShadowMap1, 0);
            if (cascade == 2) return 1.0 / textureSize(uShadowMap2, 0);
            return 1.0 / textureSize(uShadowMap3, 0);
        }

        // Calculate shadow for directional light with PCF
        float calculateDirectionalShadow(vec3 N, vec3 L)
        {
            if (uShadowEnabled == 0) return 1.0;

            int cascade = selectCascade();
            mat4 lightSpaceMatrix = getLightSpaceMatrix(cascade);

            // Apply normal bias to world position
            vec3 biasedPos = vWorldPos + N * uShadowNormalBias;

            // Transform to light space
            vec4 fragPosLightSpace = lightSpaceMatrix * vec4(biasedPos, 1.0);
            vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

            // Transform to [0, 1] range
            projCoords = projCoords * 0.5 + 0.5;

            // Check if outside shadow map
            if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0 ||
                projCoords.y < 0.0 || projCoords.y > 1.0)
            {
                return 1.0;
            }

            // Apply depth bias based on surface angle
            float bias = max(uShadowBias * (1.0 - dot(N, L)), uShadowBias * 0.1);
            float currentDepth = projCoords.z - bias;

            // PCF 3x3 filtering
            float shadow = 0.0;
            vec2 texelSize = getShadowMapTexelSize(cascade);

            for (int x = -1; x <= 1; ++x)
            {
                for (int y = -1; y <= 1; ++y)
                {
                    vec2 offset = vec2(x, y) * texelSize;
                    float pcfDepth = sampleShadowMap(cascade, projCoords.xy + offset);
                    shadow += currentDepth > pcfDepth ? 0.0 : 1.0;
                }
            }
            shadow /= 9.0;

            return shadow;
        }

        // ==================== Main ====================

        void main()
        {
            // Sample base color
            vec4 baseColor = uBaseColorFactor * vColor;
            if (uHasBaseColorMap == 1)
            {
                baseColor *= texture(uBaseColorMap, vTexCoord);
            }

            // Alpha cutoff
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
                roughness *= mrSample.g;
                metallic *= mrSample.b;
            }
            roughness = clamp(roughness, 0.04, 1.0);

            // Get surface normal
            vec3 N = getNormal();
            vec3 V = normalize(uCameraPosition - vWorldPos);

            // Calculate F0
            vec3 F0 = vec3(0.04);
            F0 = mix(F0, baseColor.rgb, metallic);

            // Accumulate light contribution
            vec3 Lo = vec3(0.0);

            for (int i = 0; i < uLightCount && i < MAX_LIGHTS; i++)
            {
                vec3 L;
                vec3 radiance;
                float shadow = 1.0;

                if (uLightTypes[i] == LIGHT_DIRECTIONAL)
                {
                    L = normalize(-uLightDirections[i]);
                    radiance = uLightColors[i] * uLightIntensities[i];

                    // Apply shadow for first directional light only
                    if (i == 0)
                    {
                        shadow = calculateDirectionalShadow(N, L);
                    }
                }
                else if (uLightTypes[i] == LIGHT_POINT)
                {
                    L = normalize(uLightPositions[i] - vWorldPos);
                    float attenuation = getAttenuation(uLightPositions[i], uLightRanges[i]);
                    radiance = uLightColors[i] * uLightIntensities[i] * attenuation;
                    // TODO: Point light shadows with cubemap
                }
                else // LIGHT_SPOT
                {
                    L = normalize(uLightPositions[i] - vWorldPos);
                    float attenuation = getAttenuation(uLightPositions[i], uLightRanges[i]);
                    float spotIntensity = getSpotIntensity(L, uLightDirections[i],
                                                          uLightInnerCones[i], uLightOuterCones[i]);
                    radiance = uLightColors[i] * uLightIntensities[i] * attenuation * spotIntensity;
                    // TODO: Spot light shadows
                }

                vec3 H = normalize(V + L);

                // Cook-Torrance BRDF
                float NDF = DistributionGGX(N, H, roughness);
                float G = GeometrySmith(N, V, L, roughness);
                vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

                vec3 numerator = NDF * G * F;
                float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
                vec3 specular = numerator / denominator;

                vec3 kS = F;
                vec3 kD = vec3(1.0) - kS;
                kD *= 1.0 - metallic;

                float NdotL = max(dot(N, L), 0.0);

                // Apply shadow to direct lighting
                Lo += (kD * baseColor.rgb / PI + specular) * radiance * NdotL * shadow;
            }

            // Ambient lighting (unaffected by shadows)
            vec3 ambient = vec3(0.03) * baseColor.rgb;

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

            // HDR tonemapping
            color = color / (color + vec3(1.0));

            // Gamma correction
            color = pow(color, vec3(1.0 / 2.2));

            FragColor = vec4(color, baseColor.a);
        }
        """;
}
