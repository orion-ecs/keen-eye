namespace KeenEyes.Graphics.Abstractions;

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
    Fragment,

    /// <summary>
    /// Geometry shader stage.
    /// </summary>
    Geometry,

    /// <summary>
    /// Compute shader stage.
    /// </summary>
    Compute
}

/// <summary>
/// Texture binding targets.
/// </summary>
public enum TextureTarget
{
    /// <summary>
    /// 2D texture.
    /// </summary>
    Texture2D,

    /// <summary>
    /// Cube map texture.
    /// </summary>
    TextureCubeMap,

    /// <summary>
    /// 2D texture array.
    /// </summary>
    Texture2DArray
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
/// Values match OpenGL constants directly.
/// </summary>
public enum TextureMinFilter
{
    /// <summary>
    /// Nearest neighbor filtering.
    /// </summary>
    Nearest = 0x2600,

    /// <summary>
    /// Bilinear filtering.
    /// </summary>
    Linear = 0x2601,

    /// <summary>
    /// Nearest with nearest mipmap.
    /// </summary>
    NearestMipmapNearest = 0x2700,

    /// <summary>
    /// Linear with nearest mipmap.
    /// </summary>
    LinearMipmapNearest = 0x2701,

    /// <summary>
    /// Nearest with linear mipmap.
    /// </summary>
    NearestMipmapLinear = 0x2702,

    /// <summary>
    /// Trilinear filtering with mipmaps.
    /// </summary>
    LinearMipmapLinear = 0x2703
}

/// <summary>
/// Texture magnification filter modes.
/// Values match OpenGL constants directly.
/// </summary>
public enum TextureMagFilter
{
    /// <summary>
    /// Nearest neighbor filtering.
    /// </summary>
    Nearest = 0x2600,

    /// <summary>
    /// Bilinear filtering.
    /// </summary>
    Linear = 0x2601
}

/// <summary>
/// Texture wrapping modes.
/// Values match OpenGL constants directly.
/// </summary>
public enum TextureWrapMode
{
    /// <summary>
    /// Repeat the texture.
    /// </summary>
    Repeat = 0x2901,

    /// <summary>
    /// Mirror the texture on each repeat.
    /// </summary>
    MirroredRepeat = 0x8370,

    /// <summary>
    /// Clamp to edge color.
    /// </summary>
    ClampToEdge = 0x812F,

    /// <summary>
    /// Clamp to border color.
    /// </summary>
    ClampToBorder = 0x812D
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
    Texture3,
    /// <summary>Texture unit 4.</summary>
    Texture4,
    /// <summary>Texture unit 5.</summary>
    Texture5,
    /// <summary>Texture unit 6.</summary>
    Texture6,
    /// <summary>Texture unit 7.</summary>
    Texture7
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
    Blend,

    /// <summary>
    /// Scissor testing.
    /// </summary>
    ScissorTest,

    /// <summary>
    /// Stencil testing.
    /// </summary>
    StencilTest
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
    /// Triangle fan.
    /// </summary>
    TriangleFan,

    /// <summary>
    /// Individual lines.
    /// </summary>
    Lines,

    /// <summary>
    /// Line strip.
    /// </summary>
    LineStrip,

    /// <summary>
    /// Line loop.
    /// </summary>
    LineLoop,

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
    /// 8-bit unsigned integers.
    /// </summary>
    UnsignedByte,

    /// <summary>
    /// 16-bit unsigned integers.
    /// </summary>
    UnsignedShort,

    /// <summary>
    /// 32-bit unsigned integers.
    /// </summary>
    UnsignedInt
}

/// <summary>
/// Blend factors for alpha blending.
/// </summary>
public enum BlendFactor
{
    /// <summary>Factor is zero.</summary>
    Zero,

    /// <summary>Factor is one.</summary>
    One,

    /// <summary>Factor is source color.</summary>
    SrcColor,

    /// <summary>Factor is (1 - source color).</summary>
    OneMinusSrcColor,

    /// <summary>Factor is destination color.</summary>
    DstColor,

    /// <summary>Factor is (1 - destination color).</summary>
    OneMinusDstColor,

    /// <summary>Factor is source alpha.</summary>
    SrcAlpha,

    /// <summary>Factor is (1 - source alpha).</summary>
    OneMinusSrcAlpha,

    /// <summary>Factor is destination alpha.</summary>
    DstAlpha,

    /// <summary>Factor is (1 - destination alpha).</summary>
    OneMinusDstAlpha
}

/// <summary>
/// Depth comparison functions.
/// </summary>
public enum DepthFunction
{
    /// <summary>Never passes.</summary>
    Never,

    /// <summary>Passes if less than.</summary>
    Less,

    /// <summary>Passes if equal.</summary>
    Equal,

    /// <summary>Passes if less than or equal.</summary>
    LessOrEqual,

    /// <summary>Passes if greater than.</summary>
    Greater,

    /// <summary>Passes if not equal.</summary>
    NotEqual,

    /// <summary>Passes if greater than or equal.</summary>
    GreaterOrEqual,

    /// <summary>Always passes.</summary>
    Always
}

/// <summary>
/// Pixel format for texture data.
/// </summary>
public enum PixelFormat
{
    /// <summary>Red channel only.</summary>
    R,

    /// <summary>Red and green channels.</summary>
    RG,

    /// <summary>Red, green, and blue channels.</summary>
    RGB,

    /// <summary>Red, green, blue, and alpha channels.</summary>
    RGBA
}

/// <summary>
/// Pixel storage mode parameters for texture uploads.
/// </summary>
public enum PixelStoreParameter
{
    /// <summary>
    /// Row length in pixels for unpacking. 0 means use the width from the texture call.
    /// </summary>
    UnpackRowLength,

    /// <summary>
    /// Number of rows to skip when unpacking.
    /// </summary>
    UnpackSkipRows,

    /// <summary>
    /// Number of pixels to skip at the start of each row when unpacking.
    /// </summary>
    UnpackSkipPixels,

    /// <summary>
    /// Byte alignment for unpacking rows (1, 2, 4, or 8).
    /// </summary>
    UnpackAlignment
}

/// <summary>
/// GPU-compressed texture formats (Block Compression / S3TC).
/// </summary>
/// <remarks>
/// <para>
/// Block compression formats store 4x4 pixel blocks in fixed-size compressed data.
/// These formats are GPU-native and remain compressed in VRAM, reducing memory
/// bandwidth and improving performance.
/// </para>
/// <para>
/// Common uses:
/// <list type="bullet">
/// <item><description>BC1 (DXT1): RGB, 4 bpp - opaque textures, cutout alpha</description></item>
/// <item><description>BC3 (DXT5): RGBA, 8 bpp - textures with smooth alpha</description></item>
/// <item><description>BC5: RG, 8 bpp - normal maps (two-channel)</description></item>
/// <item><description>BC7: RGBA, 8 bpp - high-quality textures</description></item>
/// </list>
/// </para>
/// </remarks>
public enum CompressedTextureFormat
{
    /// <summary>
    /// BC1/DXT1 - RGB with optional 1-bit alpha. 4 bits per pixel.
    /// Best for opaque textures or textures with cutout (on/off) alpha.
    /// </summary>
    Bc1,

    /// <summary>
    /// BC1/DXT1 with explicit alpha support. 4 bits per pixel.
    /// </summary>
    Bc1Alpha,

    /// <summary>
    /// BC2/DXT3 - RGB with explicit 4-bit alpha. 8 bits per pixel.
    /// Best for textures with sharp alpha transitions.
    /// </summary>
    Bc2,

    /// <summary>
    /// BC3/DXT5 - RGB with interpolated 8-bit alpha. 8 bits per pixel.
    /// Best for textures with smooth alpha gradients.
    /// </summary>
    Bc3,

    /// <summary>
    /// BC4 - Single red channel. 4 bits per pixel.
    /// Best for grayscale textures or height maps.
    /// </summary>
    Bc4,

    /// <summary>
    /// BC5 - Two channels (RG). 8 bits per pixel.
    /// Best for normal maps stored as RG.
    /// </summary>
    Bc5,

    /// <summary>
    /// BC6H - HDR RGB without alpha. 8 bits per pixel.
    /// Best for HDR environment maps.
    /// </summary>
    Bc6h,

    /// <summary>
    /// BC7 - High-quality RGBA. 8 bits per pixel.
    /// Best quality for textures requiring both RGB and alpha.
    /// </summary>
    Bc7
}
