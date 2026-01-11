namespace KeenEyes.Shaders;

/// <summary>
/// Specifies the data type of a shader uniform.
/// </summary>
public enum UniformType
{
    /// <summary>
    /// Single 32-bit float.
    /// </summary>
    Float,

    /// <summary>
    /// Two-component float vector.
    /// </summary>
    Float2,

    /// <summary>
    /// Three-component float vector.
    /// </summary>
    Float3,

    /// <summary>
    /// Four-component float vector.
    /// </summary>
    Float4,

    /// <summary>
    /// Single 32-bit signed integer.
    /// </summary>
    Int,

    /// <summary>
    /// Two-component integer vector.
    /// </summary>
    Int2,

    /// <summary>
    /// Three-component integer vector.
    /// </summary>
    Int3,

    /// <summary>
    /// Four-component integer vector.
    /// </summary>
    Int4,

    /// <summary>
    /// Single 32-bit unsigned integer.
    /// </summary>
    UInt,

    /// <summary>
    /// Boolean value.
    /// </summary>
    Bool,

    /// <summary>
    /// 4x4 float matrix.
    /// </summary>
    Matrix4,

    /// <summary>
    /// 2D texture sampler.
    /// </summary>
    Sampler2D,

    /// <summary>
    /// Cube map texture sampler.
    /// </summary>
    SamplerCube
}

/// <summary>
/// Describes a shader uniform parameter.
/// </summary>
/// <param name="Name">The uniform name in the shader.</param>
/// <param name="Type">The data type of the uniform.</param>
/// <param name="ArraySize">The array size (1 for non-arrays).</param>
public readonly record struct UniformDescriptor(
    string Name,
    UniformType Type,
    int ArraySize = 1)
{
    /// <summary>
    /// Gets the size in bytes of this uniform (single element).
    /// </summary>
    public int ElementSizeInBytes => Type switch
    {
        UniformType.Float => 4,
        UniformType.Float2 => 8,
        UniformType.Float3 => 12,
        UniformType.Float4 => 16,
        UniformType.Int => 4,
        UniformType.Int2 => 8,
        UniformType.Int3 => 12,
        UniformType.Int4 => 16,
        UniformType.UInt => 4,
        UniformType.Bool => 4, // GPU typically pads to 4 bytes
        UniformType.Matrix4 => 64,
        UniformType.Sampler2D => 0, // Samplers don't have a size
        UniformType.SamplerCube => 0,
        _ => throw new InvalidOperationException($"Unknown uniform type: {Type}")
    };

    /// <summary>
    /// Gets the total size in bytes of this uniform (including array elements).
    /// </summary>
    public int TotalSizeInBytes => ElementSizeInBytes * ArraySize;

    /// <summary>
    /// Gets whether this uniform is an array.
    /// </summary>
    public bool IsArray => ArraySize > 1;

    /// <summary>
    /// Gets whether this uniform is a texture sampler.
    /// </summary>
    public bool IsSampler => Type is UniformType.Sampler2D or UniformType.SamplerCube;
}
