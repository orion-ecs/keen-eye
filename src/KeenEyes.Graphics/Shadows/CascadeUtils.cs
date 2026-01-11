using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Shadows;

/// <summary>
/// Utility methods for Cascaded Shadow Map (CSM) calculations.
/// </summary>
public static class CascadeUtils
{
    /// <summary>
    /// Calculates the cascade split distances using a blend of logarithmic and uniform distribution.
    /// </summary>
    /// <param name="nearPlane">The camera near plane distance.</param>
    /// <param name="farPlane">The maximum shadow distance.</param>
    /// <param name="cascadeCount">Number of cascades (1-4).</param>
    /// <param name="lambda">Blend factor: 0 = uniform, 1 = logarithmic. 0.75 is a good default.</param>
    /// <returns>Array of split distances (length = cascadeCount).</returns>
    public static float[] CalculateCascadeSplits(float nearPlane, float farPlane, int cascadeCount, float lambda)
    {
        var splits = new float[cascadeCount];
        float ratio = farPlane / nearPlane;

        for (int i = 0; i < cascadeCount; i++)
        {
            float p = (i + 1) / (float)cascadeCount;

            // Logarithmic split
            float log = nearPlane * MathF.Pow(ratio, p);

            // Uniform split
            float uniform = nearPlane + (farPlane - nearPlane) * p;

            // Blend between logarithmic and uniform
            splits[i] = lambda * log + (1 - lambda) * uniform;
        }

        return splits;
    }

    /// <summary>
    /// Calculates the light-space view-projection matrix for a cascade.
    /// </summary>
    /// <param name="lightDirection">The normalized light direction (pointing toward the light).</param>
    /// <param name="frustumCorners">The 8 corners of the camera frustum in world space.</param>
    /// <returns>The light-space view-projection matrix.</returns>
    public static Matrix4x4 CalculateLightSpaceMatrix(Vector3 lightDirection, ReadOnlySpan<Vector3> frustumCorners)
    {
        if (frustumCorners.Length != 8)
        {
            throw new ArgumentException("Must provide exactly 8 frustum corners", nameof(frustumCorners));
        }

        // Calculate the center of the frustum
        var center = Vector3.Zero;
        for (int i = 0; i < 8; i++)
        {
            center += frustumCorners[i];
        }
        center /= 8f;

        // Create the light view matrix looking at the center from the light direction
        var lightView = Matrix4x4.CreateLookAt(
            center - lightDirection * 100f, // Position behind the frustum
            center,
            Vector3.UnitY);

        // Transform frustum corners to light space to find bounding box
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        for (int i = 0; i < 8; i++)
        {
            var lightSpaceCorner = Vector3.Transform(frustumCorners[i], lightView);
            minX = MathF.Min(minX, lightSpaceCorner.X);
            maxX = MathF.Max(maxX, lightSpaceCorner.X);
            minY = MathF.Min(minY, lightSpaceCorner.Y);
            maxY = MathF.Max(maxY, lightSpaceCorner.Y);
            minZ = MathF.Min(minZ, lightSpaceCorner.Z);
            maxZ = MathF.Max(maxZ, lightSpaceCorner.Z);
        }

        // Add some padding to avoid edge artifacts
        float padding = (maxZ - minZ) * 0.1f;
        minZ -= padding;
        maxZ += padding;

        // Create orthographic projection that encompasses the frustum
        var lightProjection = Matrix4x4.CreateOrthographicOffCenter(
            minX, maxX,
            minY, maxY,
            minZ, maxZ);

        return lightView * lightProjection;
    }

    /// <summary>
    /// Calculates the 8 corners of a camera frustum between two split distances.
    /// </summary>
    /// <param name="inverseViewProjection">The inverse of the camera's view-projection matrix.</param>
    /// <param name="nearSplit">The near split distance (0-1 in NDC depth).</param>
    /// <param name="farSplit">The far split distance (0-1 in NDC depth).</param>
    /// <returns>Array of 8 frustum corners in world space.</returns>
    public static Vector3[] CalculateFrustumCorners(Matrix4x4 inverseViewProjection, float nearSplit, float farSplit)
    {
        var corners = new Vector3[8];

        // NDC corners
        Span<Vector4> ndcCorners =
        [
            new Vector4(-1, -1, nearSplit, 1), // Near bottom-left
            new Vector4(1, -1, nearSplit, 1),  // Near bottom-right
            new Vector4(1, 1, nearSplit, 1),   // Near top-right
            new Vector4(-1, 1, nearSplit, 1),  // Near top-left
            new Vector4(-1, -1, farSplit, 1),  // Far bottom-left
            new Vector4(1, -1, farSplit, 1),   // Far bottom-right
            new Vector4(1, 1, farSplit, 1),    // Far top-right
            new Vector4(-1, 1, farSplit, 1)    // Far top-left
        ];

        for (int i = 0; i < 8; i++)
        {
            var worldPos = Vector4.Transform(ndcCorners[i], inverseViewProjection);
            corners[i] = new Vector3(worldPos.X, worldPos.Y, worldPos.Z) / worldPos.W;
        }

        return corners;
    }

    /// <summary>
    /// Calculates frustum corners for a specific cascade using split distances.
    /// </summary>
    /// <param name="cameraView">The camera view matrix.</param>
    /// <param name="cameraProjection">The camera projection matrix.</param>
    /// <param name="nearPlane">The camera near plane.</param>
    /// <param name="cascadeSplits">The cascade split distances.</param>
    /// <param name="cascadeIndex">The cascade index to calculate (0-based).</param>
    /// <returns>Array of 8 frustum corners in world space.</returns>
    public static Vector3[] CalculateCascadeFrustumCorners(
        Matrix4x4 cameraView,
        Matrix4x4 cameraProjection,
        float nearPlane,
        float[] cascadeSplits,
        int cascadeIndex)
    {
        // Get the view-projection matrix and its inverse
        var viewProjection = cameraView * cameraProjection;
        if (!Matrix4x4.Invert(viewProjection, out var inverseViewProjection))
        {
            throw new InvalidOperationException("Could not invert view-projection matrix");
        }

        // Determine the near and far distances for this cascade
        float nearDist = cascadeIndex == 0 ? nearPlane : cascadeSplits[cascadeIndex - 1];
        float farDist = cascadeSplits[cascadeIndex];

        // Calculate NDC depth for near and far distances using the projection matrix
        float nearNdc = DepthToNdc(nearDist, cameraProjection);
        float farNdc = DepthToNdc(farDist, cameraProjection);

        return CalculateFrustumCorners(inverseViewProjection, nearNdc, farNdc);
    }

    /// <summary>
    /// Converts a linear depth value to NDC depth using the projection matrix.
    /// </summary>
    /// <param name="linearDepth">The linear depth in view space.</param>
    /// <param name="projection">The projection matrix.</param>
    /// <returns>The NDC depth value (-1 to 1 for OpenGL).</returns>
    private static float DepthToNdc(float linearDepth, Matrix4x4 projection)
    {
        // For a standard perspective projection matrix:
        // z_clip = projection.M33 * z_view + projection.M43
        // w_clip = -z_view (for perspective)
        // z_ndc = z_clip / w_clip
        float zClip = projection.M33 * (-linearDepth) + projection.M43;
        float wClip = linearDepth; // -(-linearDepth)
        return zClip / wClip;
    }

    /// <summary>
    /// Stabilizes a light-space matrix to reduce shadow swimming/shimmering.
    /// </summary>
    /// <param name="lightSpaceMatrix">The light-space matrix to stabilize.</param>
    /// <param name="shadowMapSize">The shadow map resolution in pixels.</param>
    /// <returns>A stabilized light-space matrix.</returns>
    public static Matrix4x4 StabilizeLightSpaceMatrix(Matrix4x4 lightSpaceMatrix, int shadowMapSize)
    {
        // Transform the origin to shadow map space
        var shadowOrigin = Vector4.Transform(Vector4.UnitW, lightSpaceMatrix);
        shadowOrigin *= shadowMapSize / 2.0f;

        // Round to the nearest texel
        var roundedOrigin = new Vector4(
            MathF.Round(shadowOrigin.X),
            MathF.Round(shadowOrigin.Y),
            shadowOrigin.Z,
            shadowOrigin.W);

        // Calculate the offset needed to snap to texel grid
        var offset = (roundedOrigin - shadowOrigin) * (2.0f / shadowMapSize);

        // Apply the offset to the projection matrix
        var result = lightSpaceMatrix;
        result.M41 += offset.X;
        result.M42 += offset.Y;

        return result;
    }
}
