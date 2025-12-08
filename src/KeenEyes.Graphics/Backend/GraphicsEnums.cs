namespace KeenEyes.Graphics.Backend;

/// <summary>
/// Buffer target types for GPU buffer operations.
/// </summary>
public enum BufferTarget
{
    /// <summary>
    /// Vertex attribute data buffer.
    /// </summary>
    ArrayBuffer,

    /// <summary>
    /// Vertex index data buffer.
    /// </summary>
    ElementArrayBuffer
}

/// <summary>
/// Buffer usage hints for GPU memory allocation.
/// </summary>
public enum BufferUsage
{
    /// <summary>
    /// Data set once, used many times for drawing.
    /// </summary>
    StaticDraw,

    /// <summary>
    /// Data modified frequently, used many times for drawing.
    /// </summary>
    DynamicDraw,

    /// <summary>
    /// Data set once, used at most a few times.
    /// </summary>
    StreamDraw
}

/// <summary>
/// Vertex attribute data types.
/// </summary>
public enum VertexAttribType
{
    /// <summary>
    /// 32-bit floating point.
    /// </summary>
    Float,

    /// <summary>
    /// 32-bit signed integer.
    /// </summary>
    Int,

    /// <summary>
    /// 8-bit unsigned integer.
    /// </summary>
    UnsignedByte
}

/// <summary>
/// Shader program types.
/// </summary>
public enum ShaderType
{
    /// <summary>
    /// Vertex shader stage.
    /// </summary>
    Vertex,

    /// <summary>
    /// Fragment shader stage.
    /// </summary>
    Fragment
}

/// <summary>
/// Texture binding targets.
/// </summary>
public enum TextureTarget
{
    /// <summary>
    /// 2D texture.
    /// </summary>
    Texture2D
}

/// <summary>
/// Texture parameter names.
/// </summary>
public enum TextureParam
{
    /// <summary>
    /// Minification filter.
    /// </summary>
    MinFilter,

    /// <summary>
    /// Magnification filter.
    /// </summary>
    MagFilter,

    /// <summary>
    /// Horizontal wrapping mode.
    /// </summary>
    WrapS,

    /// <summary>
    /// Vertical wrapping mode.
    /// </summary>
    WrapT
}

/// <summary>
/// Texture minification filter modes.
/// </summary>
public enum TextureMinFilter
{
    /// <summary>
    /// Nearest neighbor filtering.
    /// </summary>
    Nearest,

    /// <summary>
    /// Bilinear filtering.
    /// </summary>
    Linear,

    /// <summary>
    /// Trilinear filtering with mipmaps.
    /// </summary>
    LinearMipmapLinear
}

/// <summary>
/// Texture magnification filter modes.
/// </summary>
public enum TextureMagFilter
{
    /// <summary>
    /// Nearest neighbor filtering.
    /// </summary>
    Nearest,

    /// <summary>
    /// Bilinear filtering.
    /// </summary>
    Linear
}

/// <summary>
/// Texture wrapping modes.
/// </summary>
public enum TextureWrapMode
{
    /// <summary>
    /// Repeat the texture.
    /// </summary>
    Repeat,

    /// <summary>
    /// Mirror the texture on each repeat.
    /// </summary>
    MirroredRepeat,

    /// <summary>
    /// Clamp to edge color.
    /// </summary>
    ClampToEdge,

    /// <summary>
    /// Clamp to border color.
    /// </summary>
    ClampToBorder
}

/// <summary>
/// Texture units for multi-texturing.
/// </summary>
public enum TextureUnit
{
    /// <summary>Texture unit 0.</summary>
    Texture0,
    /// <summary>Texture unit 1.</summary>
    Texture1,
    /// <summary>Texture unit 2.</summary>
    Texture2,
    /// <summary>Texture unit 3.</summary>
    Texture3
}

/// <summary>
/// Render state capabilities.
/// </summary>
public enum RenderCapability
{
    /// <summary>
    /// Depth buffer testing.
    /// </summary>
    DepthTest,

    /// <summary>
    /// Face culling.
    /// </summary>
    CullFace,

    /// <summary>
    /// Alpha blending.
    /// </summary>
    Blend
}

/// <summary>
/// Triangle face selection for culling.
/// </summary>
public enum CullFaceMode
{
    /// <summary>
    /// Cull front-facing triangles.
    /// </summary>
    Front,

    /// <summary>
    /// Cull back-facing triangles.
    /// </summary>
    Back,

    /// <summary>
    /// Cull both front and back faces.
    /// </summary>
    FrontAndBack
}

/// <summary>
/// Clear buffer flags.
/// </summary>
[Flags]
public enum ClearMask
{
    /// <summary>
    /// No buffers to clear.
    /// </summary>
    None = 0,

    /// <summary>
    /// Clear the color buffer.
    /// </summary>
    ColorBuffer = 1,

    /// <summary>
    /// Clear the depth buffer.
    /// </summary>
    DepthBuffer = 2,

    /// <summary>
    /// Clear the stencil buffer.
    /// </summary>
    StencilBuffer = 4
}

/// <summary>
/// Primitive types for drawing.
/// </summary>
public enum PrimitiveType
{
    /// <summary>
    /// Individual triangles.
    /// </summary>
    Triangles,

    /// <summary>
    /// Triangle strip.
    /// </summary>
    TriangleStrip,

    /// <summary>
    /// Individual lines.
    /// </summary>
    Lines,

    /// <summary>
    /// Individual points.
    /// </summary>
    Points
}

/// <summary>
/// Index element types.
/// </summary>
public enum IndexType
{
    /// <summary>
    /// 16-bit unsigned integers.
    /// </summary>
    UnsignedShort,

    /// <summary>
    /// 32-bit unsigned integers.
    /// </summary>
    UnsignedInt
}
