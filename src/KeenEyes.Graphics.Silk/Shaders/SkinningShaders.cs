namespace KeenEyes.Graphics.Silk.Shaders;

/// <summary>
/// Contains GPU skinning shader source code for skeletal animation.
/// </summary>
/// <remarks>
/// <para>
/// These shaders transform vertices by weighted bone matrices for skeletal animation.
/// Each vertex can be influenced by up to 4 bones (standard glTF skinning).
/// </para>
/// <para>
/// Bone matrices are uploaded as a uniform array and indexed by the vertex's joint indices.
/// The final vertex position is computed as:
/// <code>
/// position = sum(weight[i] * boneMatrix[joint[i]] * position)
/// </code>
/// </para>
/// </remarks>
internal static class SkinningShaders
{
    /// <summary>
    /// Maximum number of bones supported in the skeleton.
    /// </summary>
    /// <remarks>
    /// This value must match the MAX_BONES constant in the shaders.
    /// 128 bones is sufficient for most humanoid characters and fits in a UBO (8KB).
    /// </remarks>
    public const int MaxBones = 128;

    /// <summary>
    /// Skinned mesh vertex shader with GPU bone transforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Vertex attribute layout:
    /// <list type="bullet">
    ///   <item><description>Location 0: Position (vec3)</description></item>
    ///   <item><description>Location 1: Normal (vec3)</description></item>
    ///   <item><description>Location 2: TexCoord (vec2)</description></item>
    ///   <item><description>Location 3: Tangent (vec4)</description></item>
    ///   <item><description>Location 4: Color (vec4)</description></item>
    ///   <item><description>Location 5: Joints (uvec4) - bone indices</description></item>
    ///   <item><description>Location 6: Weights (vec4) - bone weights</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public const string SkinnedVertexShader = """
        #version 330 core

        const int MAX_BONES = 128;

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        layout (location = 3) in vec4 aTangent;
        layout (location = 4) in vec4 aColor;
        layout (location = 5) in uvec4 aJoints;
        layout (location = 6) in vec4 aWeights;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uBoneMatrices[MAX_BONES];

        out vec3 vWorldPos;
        out vec3 vNormal;
        out vec2 vTexCoord;
        out vec4 vColor;
        out mat3 vTBN;

        void main()
        {
            // Compute skinning matrix from bone weights
            mat4 skinMatrix =
                aWeights.x * uBoneMatrices[aJoints.x] +
                aWeights.y * uBoneMatrices[aJoints.y] +
                aWeights.z * uBoneMatrices[aJoints.z] +
                aWeights.w * uBoneMatrices[aJoints.w];

            // Apply skinning to position
            vec4 skinnedPosition = skinMatrix * vec4(aPosition, 1.0);

            // Transform to world space
            vec4 worldPos = uModel * skinnedPosition;
            vWorldPos = worldPos.xyz;

            // Calculate normal matrix for skinned mesh
            // Note: For accurate normals, we should use the inverse transpose
            // but for performance, we use the 3x3 portion which works well
            // when skin matrices are orthonormal (no non-uniform scaling)
            mat3 skinNormalMatrix = mat3(skinMatrix);
            mat3 normalMatrix = mat3(transpose(inverse(uModel)));

            // Transform normal with skinning
            vec3 skinnedNormal = skinNormalMatrix * aNormal;
            vec3 N = normalize(normalMatrix * skinnedNormal);
            vNormal = N;

            // Transform tangent with skinning for TBN matrix
            vec3 skinnedTangent = skinNormalMatrix * aTangent.xyz;
            vec3 T = normalize(normalMatrix * skinnedTangent);
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
    /// Skinned mesh fragment shader with PBR lighting.
    /// </summary>
    /// <remarks>
    /// This is identical to the standard PBR fragment shader since skinning
    /// only affects vertex positions, not fragment shading.
    /// </remarks>
    public const string SkinnedFragmentShader = DefaultShaders.PbrFragmentShader;

    /// <summary>
    /// Skinned mesh vertex shader for unlit rendering.
    /// </summary>
    /// <remarks>
    /// Simplified version without normal/tangent transformation for unlit meshes.
    /// </remarks>
    public const string SkinnedUnlitVertexShader = """
        #version 330 core

        const int MAX_BONES = 128;

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        layout (location = 3) in vec4 aTangent;
        layout (location = 4) in vec4 aColor;
        layout (location = 5) in uvec4 aJoints;
        layout (location = 6) in vec4 aWeights;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uBoneMatrices[MAX_BONES];

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            // Compute skinning matrix from bone weights
            mat4 skinMatrix =
                aWeights.x * uBoneMatrices[aJoints.x] +
                aWeights.y * uBoneMatrices[aJoints.y] +
                aWeights.z * uBoneMatrices[aJoints.z] +
                aWeights.w * uBoneMatrices[aJoints.w];

            // Apply skinning to position
            vec4 skinnedPosition = skinMatrix * vec4(aPosition, 1.0);

            gl_Position = uProjection * uView * uModel * skinnedPosition;
            vTexCoord = aTexCoord;
            vColor = aColor;
        }
        """;

    /// <summary>
    /// Skinned mesh unlit fragment shader.
    /// </summary>
    public const string SkinnedUnlitFragmentShader = DefaultShaders.UnlitFragmentShader;

    /// <summary>
    /// Skinned mesh vertex shader for shadow mapping.
    /// </summary>
    /// <remarks>
    /// Outputs only position for depth-only shadow pass.
    /// </remarks>
    public const string SkinnedShadowVertexShader = """
        #version 330 core

        const int MAX_BONES = 128;

        layout (location = 0) in vec3 aPosition;
        layout (location = 5) in uvec4 aJoints;
        layout (location = 6) in vec4 aWeights;

        uniform mat4 uModel;
        uniform mat4 uLightSpaceMatrix;
        uniform mat4 uBoneMatrices[MAX_BONES];

        void main()
        {
            // Compute skinning matrix from bone weights
            mat4 skinMatrix =
                aWeights.x * uBoneMatrices[aJoints.x] +
                aWeights.y * uBoneMatrices[aJoints.y] +
                aWeights.z * uBoneMatrices[aJoints.z] +
                aWeights.w * uBoneMatrices[aJoints.w];

            // Apply skinning to position
            vec4 skinnedPosition = skinMatrix * vec4(aPosition, 1.0);

            gl_Position = uLightSpaceMatrix * uModel * skinnedPosition;
        }
        """;

    /// <summary>
    /// Shadow depth fragment shader (empty - only depth write).
    /// </summary>
    public const string SkinnedShadowFragmentShader = """
        #version 330 core

        void main()
        {
            // Depth is automatically written to depth buffer
            // No color output needed for shadow mapping
        }
        """;
}
