namespace KeenEyes.Shaders;

/// <summary>
/// Describes a fragment shader output target.
/// </summary>
/// <param name="Name">The output variable name.</param>
/// <param name="Type">The data type of the output.</param>
/// <param name="Location">The output location (render target index).</param>
public readonly record struct OutputTarget(
    string Name,
    AttributeType Type,
    int Location);

/// <summary>
/// Interface implemented by generated GPU fragment (pixel) shaders.
/// </summary>
/// <remarks>
/// <para>
/// Each KESL fragment shader compiles to a class implementing this interface.
/// The generated class provides:
/// </para>
/// <list type="bullet">
/// <item><description>Shader source for multiple backends (GLSL, HLSL)</description></item>
/// <item><description>Input metadata matching vertex shader outputs</description></item>
/// <item><description>Output target metadata for render target binding</description></item>
/// <item><description>Uniform parameter metadata</description></item>
/// </list>
/// <para>
/// Example generated implementation:
/// <code>
/// public sealed partial class LitSurfaceShader : IGpuFragmentShader
/// {
///     public string Name => "LitSurface";
///
///     public IReadOnlyList&lt;InputAttribute&gt; Inputs { get; } = [
///         new InputAttribute("worldPos", AttributeType.Float3, 0),
///         new InputAttribute("worldNormal", AttributeType.Float3, 1),
///         new InputAttribute("uv", AttributeType.Float2, 2)
///     ];
///
///     public IReadOnlyList&lt;OutputTarget&gt; Outputs { get; } = [
///         new OutputTarget("fragColor", AttributeType.Float4, 0)
///     ];
///
///     public IReadOnlyList&lt;UniformDescriptor&gt; Uniforms { get; } = [
///         new UniformDescriptor("lightDir", UniformType.Float3),
///         new UniformDescriptor("lightColor", UniformType.Float3),
///         new UniformDescriptor("ambientColor", UniformType.Float3)
///     ];
///
///     public string GetShaderSource(ShaderBackend backend) => backend switch
///     {
///         ShaderBackend.GLSL => GlslSource,
///         ShaderBackend.HLSL => HlslSource,
///         _ => throw new NotSupportedException($"Backend {backend} not supported")
///     };
/// }
/// </code>
/// </para>
/// </remarks>
public interface IGpuFragmentShader
{
    /// <summary>
    /// Gets the name of this fragment shader.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the input attributes received from the vertex shader.
    /// </summary>
    /// <remarks>
    /// These inputs must match the outputs of the paired vertex shader.
    /// The inputs are interpolated values from vertex shader outputs.
    /// </remarks>
    IReadOnlyList<InputAttribute> Inputs { get; }

    /// <summary>
    /// Gets the output targets for this shader.
    /// </summary>
    /// <remarks>
    /// Each output corresponds to a render target binding point.
    /// Location 0 is typically the main color buffer.
    /// </remarks>
    IReadOnlyList<OutputTarget> Outputs { get; }

    /// <summary>
    /// Gets the uniform parameters used by this shader.
    /// </summary>
    IReadOnlyList<UniformDescriptor> Uniforms { get; }

    /// <summary>
    /// Gets the shader source code for the specified backend.
    /// </summary>
    /// <param name="backend">The target shader backend.</param>
    /// <returns>The shader source code.</returns>
    /// <exception cref="NotSupportedException">Thrown if the backend is not supported.</exception>
    string GetShaderSource(ShaderBackend backend);
}
