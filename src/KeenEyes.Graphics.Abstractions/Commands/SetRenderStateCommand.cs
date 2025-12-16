namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Command to modify render state settings.
/// </summary>
/// <param name="DepthTest">Enable or disable depth testing (null = no change).</param>
/// <param name="DepthWrite">Enable or disable depth buffer writes (null = no change).</param>
/// <param name="Blending">Enable or disable alpha blending (null = no change).</param>
/// <param name="CullFace">Enable or disable face culling (null = no change).</param>
/// <param name="CullMode">The face culling mode (if culling is enabled).</param>
/// <param name="BlendSrc">The source blend factor (if blending is enabled).</param>
/// <param name="BlendDst">The destination blend factor (if blending is enabled).</param>
/// <remarks>
/// <para>
/// Render state commands allow fine-grained control over the GPU pipeline state.
/// Each parameter is nullable - null values indicate no change to that state.
/// </para>
/// <para>
/// State changes are relatively expensive, so group draw calls that share the same
/// state together to minimize state switching.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Enable alpha blending with standard blend function
/// var blendState = SetRenderStateCommand.EnableBlending(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
///
/// // Disable depth writing for transparent objects
/// var transparentState = new SetRenderStateCommand(DepthWrite: false, Blending: true);
///
/// // Set opaque rendering state
/// var opaqueState = SetRenderStateCommand.Opaque;
/// </code>
/// </example>
public readonly record struct SetRenderStateCommand(
    bool? DepthTest = null,
    bool? DepthWrite = null,
    bool? Blending = null,
    bool? CullFace = null,
    CullFaceMode? CullMode = null,
    BlendFactor? BlendSrc = null,
    BlendFactor? BlendDst = null) : IRenderCommand
{
    /// <summary>
    /// Sort key for state commands. State commands execute before draw commands (sort key 2).
    /// </summary>
    public ulong SortKey => 2;

    /// <summary>
    /// Standard state for opaque rendering: depth test on, depth write on, blending off, back-face culling.
    /// </summary>
    public static readonly SetRenderStateCommand Opaque = new(
        DepthTest: true,
        DepthWrite: true,
        Blending: false,
        CullFace: true,
        CullMode: CullFaceMode.Back);

    /// <summary>
    /// Standard state for transparent rendering: depth test on, depth write off, blending on.
    /// </summary>
    public static readonly SetRenderStateCommand Transparent = new(
        DepthTest: true,
        DepthWrite: false,
        Blending: true,
        CullFace: false,
        BlendSrc: BlendFactor.SrcAlpha,
        BlendDst: BlendFactor.OneMinusSrcAlpha);

    /// <summary>
    /// Standard state for additive blending (particles, glow effects).
    /// </summary>
    public static readonly SetRenderStateCommand Additive = new(
        DepthTest: true,
        DepthWrite: false,
        Blending: true,
        CullFace: false,
        BlendSrc: BlendFactor.SrcAlpha,
        BlendDst: BlendFactor.One);

    /// <summary>
    /// Creates a state command that enables blending with the specified factors.
    /// </summary>
    /// <param name="srcFactor">The source blend factor.</param>
    /// <param name="dstFactor">The destination blend factor.</param>
    /// <returns>A render state command enabling blending.</returns>
    public static SetRenderStateCommand EnableBlending(BlendFactor srcFactor, BlendFactor dstFactor) =>
        new(Blending: true, BlendSrc: srcFactor, BlendDst: dstFactor);

    /// <summary>
    /// Creates a state command that disables blending.
    /// </summary>
    /// <returns>A render state command disabling blending.</returns>
    public static SetRenderStateCommand DisableBlending() =>
        new(Blending: false);
}
