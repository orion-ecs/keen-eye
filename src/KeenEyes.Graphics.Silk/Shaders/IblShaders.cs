namespace KeenEyes.Graphics.Silk.Shaders;

/// <summary>
/// Shaders for Image-Based Lighting (IBL) processing and rendering.
/// </summary>
internal static class IblShaders
{
    /// <summary>
    /// Vertex shader for fullscreen quad rendering (used by IBL processing).
    /// </summary>
    public const string FullscreenQuadVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 2) in vec2 aTexCoord;

        out vec2 vTexCoord;

        void main()
        {
            vTexCoord = aTexCoord;
            gl_Position = vec4(aPosition.xy * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    /// <summary>
    /// Vertex shader for cubemap rendering (skybox and IBL processing).
    /// </summary>
    public const string CubemapVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        uniform mat4 uProjection;
        uniform mat4 uView;

        out vec3 vLocalPos;

        void main()
        {
            vLocalPos = aPosition;

            // Remove translation from view matrix for skybox
            mat4 rotView = mat4(mat3(uView));
            vec4 clipPos = uProjection * rotView * vec4(aPosition, 1.0);

            // Set z to w so depth is always 1.0 (far plane)
            gl_Position = clipPos.xyww;
        }
        """;

    /// <summary>
    /// Fragment shader to convert equirectangular HDR map to cubemap.
    /// </summary>
    public const string EquirectToCubemapFragmentShader = """
        #version 330 core

        in vec3 vLocalPos;

        uniform sampler2D uEquirectangularMap;

        out vec4 FragColor;

        const vec2 invAtan = vec2(0.1591, 0.3183); // 1/(2*PI), 1/PI

        vec2 sampleSphericalMap(vec3 v)
        {
            vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
            uv *= invAtan;
            uv += 0.5;
            return uv;
        }

        void main()
        {
            vec2 uv = sampleSphericalMap(normalize(vLocalPos));
            vec3 color = texture(uEquirectangularMap, uv).rgb;
            FragColor = vec4(color, 1.0);
        }
        """;

    /// <summary>
    /// Fragment shader for irradiance convolution (diffuse IBL).
    /// </summary>
    /// <remarks>
    /// Convolves the environment map to compute the irradiance for each direction.
    /// This is used for diffuse ambient lighting.
    /// </remarks>
    public const string IrradianceConvolutionFragmentShader = """
        #version 330 core

        in vec3 vLocalPos;

        uniform samplerCube uEnvironmentMap;

        out vec4 FragColor;

        const float PI = 3.14159265359;

        void main()
        {
            // The sample direction equals the hemisphere's orientation
            vec3 N = normalize(vLocalPos);

            vec3 irradiance = vec3(0.0);

            // Tangent space calculation from normal
            vec3 up = vec3(0.0, 1.0, 0.0);
            vec3 right = normalize(cross(up, N));
            up = normalize(cross(N, right));

            float sampleDelta = 0.025;
            float nrSamples = 0.0;

            // Hemisphere integration
            for (float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
            {
                for (float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
                {
                    // Spherical to cartesian (in tangent space)
                    vec3 tangentSample = vec3(
                        sin(theta) * cos(phi),
                        sin(theta) * sin(phi),
                        cos(theta)
                    );

                    // Tangent space to world
                    vec3 sampleVec = tangentSample.x * right +
                                     tangentSample.y * up +
                                     tangentSample.z * N;

                    irradiance += texture(uEnvironmentMap, sampleVec).rgb *
                                  cos(theta) * sin(theta);
                    nrSamples++;
                }
            }

            irradiance = PI * irradiance * (1.0 / nrSamples);

            FragColor = vec4(irradiance, 1.0);
        }
        """;

    /// <summary>
    /// Fragment shader for specular pre-filtering (GGX importance sampling).
    /// </summary>
    /// <remarks>
    /// Pre-filters the environment map for different roughness levels using
    /// importance sampling based on the GGX distribution.
    /// </remarks>
    public const string SpecularPrefilterFragmentShader = """
        #version 330 core

        in vec3 vLocalPos;

        uniform samplerCube uEnvironmentMap;
        uniform float uRoughness;
        uniform int uSampleCount;

        out vec4 FragColor;

        const float PI = 3.14159265359;

        // Van der Corput sequence for quasi-random sampling
        float radicalInverse_VdC(uint bits)
        {
            bits = (bits << 16u) | (bits >> 16u);
            bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
            bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
            bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
            bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
            return float(bits) * 2.3283064365386963e-10; // 1 / 0x100000000
        }

        vec2 hammersley(uint i, uint N)
        {
            return vec2(float(i) / float(N), radicalInverse_VdC(i));
        }

        // GGX importance sampling
        vec3 importanceSampleGGX(vec2 Xi, vec3 N, float roughness)
        {
            float a = roughness * roughness;

            float phi = 2.0 * PI * Xi.x;
            float cosTheta = sqrt((1.0 - Xi.y) / (1.0 + (a * a - 1.0) * Xi.y));
            float sinTheta = sqrt(1.0 - cosTheta * cosTheta);

            // Spherical to cartesian
            vec3 H;
            H.x = cos(phi) * sinTheta;
            H.y = sin(phi) * sinTheta;
            H.z = cosTheta;

            // Tangent-space to world-space
            vec3 up = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
            vec3 tangent = normalize(cross(up, N));
            vec3 bitangent = cross(N, tangent);

            vec3 sampleVec = tangent * H.x + bitangent * H.y + N * H.z;
            return normalize(sampleVec);
        }

        // GGX distribution
        float distributionGGX(vec3 N, vec3 H, float roughness)
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

        void main()
        {
            vec3 N = normalize(vLocalPos);
            vec3 R = N;
            vec3 V = R;

            float totalWeight = 0.0;
            vec3 prefilteredColor = vec3(0.0);

            uint sampleCount = uint(uSampleCount);

            for (uint i = 0u; i < sampleCount; ++i)
            {
                vec2 Xi = hammersley(i, sampleCount);
                vec3 H = importanceSampleGGX(Xi, N, uRoughness);
                vec3 L = normalize(2.0 * dot(V, H) * H - V);

                float NdotL = max(dot(N, L), 0.0);
                if (NdotL > 0.0)
                {
                    // Sample from mip level based on roughness/pdf
                    float D = distributionGGX(N, H, uRoughness);
                    float NdotH = max(dot(N, H), 0.0);
                    float HdotV = max(dot(H, V), 0.0);
                    float pdf = D * NdotH / (4.0 * HdotV) + 0.0001;

                    float resolution = float(textureSize(uEnvironmentMap, 0).x);
                    float saTexel = 4.0 * PI / (6.0 * resolution * resolution);
                    float saSample = 1.0 / (float(sampleCount) * pdf + 0.0001);
                    float mipLevel = uRoughness == 0.0 ? 0.0 : 0.5 * log2(saSample / saTexel);

                    prefilteredColor += textureLod(uEnvironmentMap, L, mipLevel).rgb * NdotL;
                    totalWeight += NdotL;
                }
            }

            prefilteredColor = prefilteredColor / max(totalWeight, 0.0001);

            FragColor = vec4(prefilteredColor, 1.0);
        }
        """;

    /// <summary>
    /// Fragment shader for BRDF lookup table generation.
    /// </summary>
    /// <remarks>
    /// Generates a 2D LUT storing the scale and bias for the Fresnel term
    /// indexed by (NdotV, roughness).
    /// </remarks>
    public const string BrdfLutFragmentShader = """
        #version 330 core

        in vec2 vTexCoord;

        out vec2 FragColor;

        const float PI = 3.14159265359;

        float radicalInverse_VdC(uint bits)
        {
            bits = (bits << 16u) | (bits >> 16u);
            bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
            bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
            bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
            bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
            return float(bits) * 2.3283064365386963e-10;
        }

        vec2 hammersley(uint i, uint N)
        {
            return vec2(float(i) / float(N), radicalInverse_VdC(i));
        }

        vec3 importanceSampleGGX(vec2 Xi, vec3 N, float roughness)
        {
            float a = roughness * roughness;

            float phi = 2.0 * PI * Xi.x;
            float cosTheta = sqrt((1.0 - Xi.y) / (1.0 + (a * a - 1.0) * Xi.y));
            float sinTheta = sqrt(1.0 - cosTheta * cosTheta);

            vec3 H;
            H.x = cos(phi) * sinTheta;
            H.y = sin(phi) * sinTheta;
            H.z = cosTheta;

            vec3 up = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
            vec3 tangent = normalize(cross(up, N));
            vec3 bitangent = cross(N, tangent);

            vec3 sampleVec = tangent * H.x + bitangent * H.y + N * H.z;
            return normalize(sampleVec);
        }

        float geometrySchlickGGX(float NdotV, float roughness)
        {
            float a = roughness;
            float k = (a * a) / 2.0;

            float nom = NdotV;
            float denom = NdotV * (1.0 - k) + k;

            return nom / max(denom, 0.0001);
        }

        float geometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
        {
            float NdotV = max(dot(N, V), 0.0);
            float NdotL = max(dot(N, L), 0.0);
            float ggx2 = geometrySchlickGGX(NdotV, roughness);
            float ggx1 = geometrySchlickGGX(NdotL, roughness);

            return ggx1 * ggx2;
        }

        vec2 integrateBRDF(float NdotV, float roughness)
        {
            vec3 V;
            V.x = sqrt(1.0 - NdotV * NdotV);
            V.y = 0.0;
            V.z = NdotV;

            float A = 0.0;
            float B = 0.0;

            vec3 N = vec3(0.0, 0.0, 1.0);

            const uint SAMPLE_COUNT = 1024u;
            for (uint i = 0u; i < SAMPLE_COUNT; ++i)
            {
                vec2 Xi = hammersley(i, SAMPLE_COUNT);
                vec3 H = importanceSampleGGX(Xi, N, roughness);
                vec3 L = normalize(2.0 * dot(V, H) * H - V);

                float NdotL = max(L.z, 0.0);
                float NdotH = max(H.z, 0.0);
                float VdotH = max(dot(V, H), 0.0);

                if (NdotL > 0.0)
                {
                    float G = geometrySmith(N, V, L, roughness);
                    float G_Vis = (G * VdotH) / max(NdotH * NdotV, 0.0001);
                    float Fc = pow(1.0 - VdotH, 5.0);

                    A += (1.0 - Fc) * G_Vis;
                    B += Fc * G_Vis;
                }
            }

            A /= float(SAMPLE_COUNT);
            B /= float(SAMPLE_COUNT);

            return vec2(A, B);
        }

        void main()
        {
            vec2 integratedBRDF = integrateBRDF(vTexCoord.x, vTexCoord.y);
            FragColor = integratedBRDF;
        }
        """;

    /// <summary>
    /// Skybox fragment shader for rendering the environment as a background.
    /// </summary>
    public const string SkyboxFragmentShader = """
        #version 330 core

        in vec3 vLocalPos;

        uniform samplerCube uEnvironmentMap;
        uniform float uExposure;
        uniform float uMipLevel;
        uniform float uRotation; // Y-axis rotation in radians

        out vec4 FragColor;

        void main()
        {
            // Apply rotation around Y axis
            float cosR = cos(uRotation);
            float sinR = sin(uRotation);
            vec3 dir = normalize(vLocalPos);
            dir = vec3(
                dir.x * cosR + dir.z * sinR,
                dir.y,
                -dir.x * sinR + dir.z * cosR
            );

            vec3 color = textureLod(uEnvironmentMap, dir, uMipLevel).rgb;

            // Apply exposure
            color *= uExposure;

            // Tone mapping (Reinhard)
            color = color / (color + vec3(1.0));

            // Gamma correction
            color = pow(color, vec3(1.0 / 2.2));

            FragColor = vec4(color, 1.0);
        }
        """;
}
