namespace KeenEyes.Shaders;

/// <summary>
/// Specifies the data type of a vertex input attribute.
/// </summary>
public enum AttributeType
{
    /// <summary>
    /// Single 32-bit float.
    /// </summary>
    Float,

    /// <summary>
    /// Two 32-bit floats (vector2).
    /// </summary>
    Float2,

    /// <summary>
    /// Three 32-bit floats (vector3).
    /// </summary>
    Float3,

    /// <summary>
    /// Four 32-bit floats (vector4).
    /// </summary>
    Float4,

    /// <summary>
    /// Single 32-bit signed integer.
    /// </summary>
    Int,

    /// <summary>
    /// Two 32-bit signed integers.
    /// </summary>
    Int2,

    /// <summary>
    /// Three 32-bit signed integers.
    /// </summary>
    Int3,

    /// <summary>
    /// Four 32-bit signed integers.
    /// </summary>
    Int4,

    /// <summary>
    /// Single 32-bit unsigned integer.
    /// </summary>
    UInt,

    /// <summary>
    /// 4x4 float matrix (16 floats).
    /// </summary>
    Mat4
}

/// <summary>
/// Describes a single vertex input attribute.
/// </summary>
/// <param name="Name">The attribute name in the shader.</param>
/// <param name="Type">The data type of the attribute.</param>
/// <param name="Location">The binding location (layout location in GLSL, semantic index in HLSL).</param>
public readonly record struct InputAttribute(
    string Name,
    AttributeType Type,
    int Location)
{
    /// <summary>
    /// Gets the size in bytes of this attribute.
    /// </summary>
    public int SizeInBytes => Type switch
    {
        AttributeType.Float => 4,
        AttributeType.Float2 => 8,
        AttributeType.Float3 => 12,
        AttributeType.Float4 => 16,
        AttributeType.Int => 4,
        AttributeType.Int2 => 8,
        AttributeType.Int3 => 12,
        AttributeType.Int4 => 16,
        AttributeType.UInt => 4,
        AttributeType.Mat4 => 64,
        _ => throw new InvalidOperationException($"Unknown attribute type: {Type}")
    };
}

/// <summary>
/// Describes the vertex input layout for a vertex shader.
/// </summary>
/// <remarks>
/// <para>
/// The input layout defines the vertex attributes that the shader expects,
/// including their names, types, and binding locations. This information is
/// used to configure the graphics pipeline and validate vertex buffer bindings.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var layout = new InputLayoutDescriptor([
///     new InputAttribute("position", AttributeType.Float3, 0),
///     new InputAttribute("normal", AttributeType.Float3, 1),
///     new InputAttribute("texCoord", AttributeType.Float2, 2)
/// ]);
/// </code>
/// </para>
/// </remarks>
public sealed class InputLayoutDescriptor
{
    /// <summary>
    /// Gets the input attributes in this layout.
    /// </summary>
    public IReadOnlyList<InputAttribute> Attributes { get; }

    /// <summary>
    /// Gets the total stride (size in bytes) of one vertex.
    /// </summary>
    public int Stride { get; }

    /// <summary>
    /// Creates a new input layout descriptor.
    /// </summary>
    /// <param name="attributes">The vertex input attributes.</param>
    public InputLayoutDescriptor(IEnumerable<InputAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        Attributes = attributes.ToList().AsReadOnly();
        Stride = Attributes.Sum(a => a.SizeInBytes);
    }

    /// <summary>
    /// Creates a new input layout descriptor.
    /// </summary>
    /// <param name="attributes">The vertex input attributes.</param>
    public InputLayoutDescriptor(params InputAttribute[] attributes)
        : this((IEnumerable<InputAttribute>)attributes)
    {
    }

    /// <summary>
    /// Gets the attribute at the specified location.
    /// </summary>
    /// <param name="location">The binding location.</param>
    /// <returns>The attribute, or null if not found.</returns>
    public InputAttribute? GetAttribute(int location)
    {
        foreach (var attr in Attributes)
        {
            if (attr.Location == location)
            {
                return attr;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the attribute with the specified name.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute, or null if not found.</returns>
    public InputAttribute? GetAttribute(string name)
    {
        foreach (var attr in Attributes)
        {
            if (attr.Name == name)
            {
                return attr;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the byte offset of an attribute within a vertex.
    /// </summary>
    /// <param name="location">The binding location.</param>
    /// <returns>The offset in bytes, or -1 if not found.</returns>
    public int GetOffset(int location)
    {
        int offset = 0;
        foreach (var attr in Attributes.OrderBy(a => a.Location))
        {
            if (attr.Location == location)
            {
                return offset;
            }
            offset += attr.SizeInBytes;
        }
        return -1;
    }
}
