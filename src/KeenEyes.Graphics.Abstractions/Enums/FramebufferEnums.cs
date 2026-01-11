namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Framebuffer binding targets.
/// </summary>
public enum FramebufferTarget
{
    /// <summary>
    /// Both read and draw framebuffer.
    /// </summary>
    Framebuffer,

    /// <summary>
    /// Draw framebuffer only.
    /// </summary>
    DrawFramebuffer,

    /// <summary>
    /// Read framebuffer only.
    /// </summary>
    ReadFramebuffer
}

/// <summary>
/// Framebuffer attachment points.
/// </summary>
public enum FramebufferAttachment
{
    /// <summary>
    /// Color attachment 0.
    /// </summary>
    ColorAttachment0,

    /// <summary>
    /// Color attachment 1.
    /// </summary>
    ColorAttachment1,

    /// <summary>
    /// Color attachment 2.
    /// </summary>
    ColorAttachment2,

    /// <summary>
    /// Color attachment 3.
    /// </summary>
    ColorAttachment3,

    /// <summary>
    /// Depth attachment.
    /// </summary>
    DepthAttachment,

    /// <summary>
    /// Stencil attachment.
    /// </summary>
    StencilAttachment,

    /// <summary>
    /// Combined depth and stencil attachment.
    /// </summary>
    DepthStencilAttachment
}

/// <summary>
/// Framebuffer completeness status.
/// </summary>
public enum FramebufferStatus
{
    /// <summary>
    /// Framebuffer is complete and ready for use.
    /// </summary>
    Complete,

    /// <summary>
    /// Attachment is incomplete.
    /// </summary>
    IncompleteAttachment,

    /// <summary>
    /// Required attachment is missing.
    /// </summary>
    IncompleteMissingAttachment,

    /// <summary>
    /// Draw buffer has no attachment.
    /// </summary>
    IncompleteDrawBuffer,

    /// <summary>
    /// Read buffer has no attachment.
    /// </summary>
    IncompleteReadBuffer,

    /// <summary>
    /// Framebuffer configuration is unsupported.
    /// </summary>
    Unsupported,

    /// <summary>
    /// Attachments have different dimensions.
    /// </summary>
    IncompleteMultisample,

    /// <summary>
    /// Layer attachment is incomplete.
    /// </summary>
    IncompleteLayerTargets,

    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown
}

/// <summary>
/// Renderbuffer storage formats.
/// </summary>
public enum RenderbufferFormat
{
    /// <summary>
    /// 16-bit depth buffer.
    /// </summary>
    DepthComponent16,

    /// <summary>
    /// 24-bit depth buffer.
    /// </summary>
    DepthComponent24,

    /// <summary>
    /// 32-bit floating point depth buffer.
    /// </summary>
    DepthComponent32F,

    /// <summary>
    /// Combined 24-bit depth and 8-bit stencil.
    /// </summary>
    Depth24Stencil8,

    /// <summary>
    /// 8-bit stencil buffer.
    /// </summary>
    StencilIndex8,

    /// <summary>
    /// 4-channel 8-bit color buffer.
    /// </summary>
    RGBA8,

    /// <summary>
    /// 4-channel 16-bit floating point color buffer.
    /// </summary>
    RGBA16F,

    /// <summary>
    /// 4-channel 32-bit floating point color buffer.
    /// </summary>
    RGBA32F
}

/// <summary>
/// Draw buffer selection modes.
/// </summary>
public enum DrawBufferMode
{
    /// <summary>
    /// No draw buffer.
    /// </summary>
    None,

    /// <summary>
    /// Front buffer (for default framebuffer).
    /// </summary>
    Front,

    /// <summary>
    /// Back buffer (for default framebuffer).
    /// </summary>
    Back,

    /// <summary>
    /// Color attachment 0.
    /// </summary>
    ColorAttachment0,

    /// <summary>
    /// Color attachment 1.
    /// </summary>
    ColorAttachment1,

    /// <summary>
    /// Color attachment 2.
    /// </summary>
    ColorAttachment2,

    /// <summary>
    /// Color attachment 3.
    /// </summary>
    ColorAttachment3
}

/// <summary>
/// Render target format for creating render targets.
/// </summary>
public enum RenderTargetFormat
{
    /// <summary>
    /// RGBA 8-bit per channel color with 24-bit depth.
    /// </summary>
    RGBA8Depth24,

    /// <summary>
    /// RGBA 16-bit floating point per channel color with 24-bit depth.
    /// </summary>
    RGBA16FDepth24,

    /// <summary>
    /// RGBA 32-bit floating point per channel color with 32-bit depth.
    /// </summary>
    RGBA32FDepth32F,

    /// <summary>
    /// Depth-only 24-bit.
    /// </summary>
    Depth24,

    /// <summary>
    /// Depth-only 32-bit floating point.
    /// </summary>
    Depth32F
}

/// <summary>
/// Cubemap face targets for texture operations.
/// </summary>
public enum CubemapFace
{
    /// <summary>
    /// Positive X face (+X).
    /// </summary>
    PositiveX,

    /// <summary>
    /// Negative X face (-X).
    /// </summary>
    NegativeX,

    /// <summary>
    /// Positive Y face (+Y).
    /// </summary>
    PositiveY,

    /// <summary>
    /// Negative Y face (-Y).
    /// </summary>
    NegativeY,

    /// <summary>
    /// Positive Z face (+Z).
    /// </summary>
    PositiveZ,

    /// <summary>
    /// Negative Z face (-Z).
    /// </summary>
    NegativeZ
}
