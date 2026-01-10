using System.Numerics;
using System.Runtime.InteropServices;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Per-instance data for GPU instanced rendering.
/// </summary>
/// <remarks>
/// <para>
/// This struct defines the data sent to the GPU for each instance in an instanced draw call.
/// It contains the model matrix (64 bytes at locations 4-7) and an optional color tint
/// (16 bytes at location 8) for per-instance color variation.
/// </para>
/// <para>
/// The struct is laid out sequentially to match the expected GPU buffer format and can be
/// uploaded directly to an instance buffer without transformation.
/// </para>
/// <para>
/// Vertex attribute layout for instanced rendering:
/// <list type="bullet">
///   <item><description>Locations 0-3: Per-vertex data (Position, Normal, TexCoord, Color)</description></item>
///   <item><description>Locations 4-7: Per-instance ModelMatrix (mat4 = 4 vec4s)</description></item>
///   <item><description>Location 8: Per-instance ColorTint (vec4)</description></item>
/// </list>
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct InstanceData
{
    /// <summary>
    /// The model transformation matrix for this instance.
    /// </summary>
    /// <remarks>
    /// This matrix transforms the mesh from model space to world space for this specific instance.
    /// It occupies vertex attribute locations 4-7 (a mat4 is represented as 4 vec4 attributes).
    /// </remarks>
    public Matrix4x4 ModelMatrix;

    /// <summary>
    /// The color tint multiplier for this instance.
    /// </summary>
    /// <remarks>
    /// This color is multiplied with the base material color to provide per-instance color variation.
    /// A value of (1, 1, 1, 1) represents no tint (original color). It occupies vertex attribute location 8.
    /// </remarks>
    public Vector4 ColorTint;

    /// <summary>
    /// Gets the size of this struct in bytes.
    /// </summary>
    /// <remarks>
    /// This value is used when allocating GPU buffers and setting up vertex attribute pointers.
    /// </remarks>
    public static int SizeInBytes => 80; // 64 (Matrix4x4) + 16 (Vector4)

    /// <summary>
    /// Creates instance data from a model matrix with no color tint.
    /// </summary>
    /// <param name="model">The model transformation matrix.</param>
    /// <returns>Instance data with the specified matrix and white (no tint) color.</returns>
    public static InstanceData FromTransform(in Matrix4x4 model)
        => new() { ModelMatrix = model, ColorTint = Vector4.One };

    /// <summary>
    /// Creates instance data from a model matrix and color tint.
    /// </summary>
    /// <param name="model">The model transformation matrix.</param>
    /// <param name="colorTint">The color tint multiplier.</param>
    /// <returns>Instance data with the specified matrix and color tint.</returns>
    public static InstanceData FromTransform(in Matrix4x4 model, Vector4 colorTint)
        => new() { ModelMatrix = model, ColorTint = colorTint };

    /// <summary>
    /// Creates instance data from position, rotation, and scale components with no color tint.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The rotation quaternion.</param>
    /// <param name="scale">The scale factor.</param>
    /// <returns>Instance data with the composed transformation matrix and white (no tint) color.</returns>
    public static InstanceData FromTRS(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var model = Matrix4x4.CreateScale(scale) *
                    Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(position);
        return new InstanceData { ModelMatrix = model, ColorTint = Vector4.One };
    }

    /// <summary>
    /// Creates instance data from position, rotation, scale, and color tint components.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The rotation quaternion.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="colorTint">The color tint multiplier.</param>
    /// <returns>Instance data with the composed transformation matrix and specified color tint.</returns>
    public static InstanceData FromTRS(Vector3 position, Quaternion rotation, Vector3 scale, Vector4 colorTint)
    {
        var model = Matrix4x4.CreateScale(scale) *
                    Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(position);
        return new InstanceData { ModelMatrix = model, ColorTint = colorTint };
    }
}
