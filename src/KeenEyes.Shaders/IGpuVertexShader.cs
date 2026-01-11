namespace KeenEyes.Shaders;

/// <summary>
/// Interface implemented by generated GPU vertex shaders.
/// </summary>
/// <remarks>
/// <para>
/// Each KESL vertex shader compiles to a class implementing this interface.
/// The generated class provides:
/// </para>
/// <list type="bullet">
/// <item><description>Shader source for multiple backends (GLSL, HLSL)</description></item>
/// <item><description>Input layout metadata for vertex buffer binding</description></item>
/// <item><description>Uniform parameter metadata</description></item>
/// </list>
/// <para>
/// Example generated implementation:
/// <code>
/// public sealed partial class TransformVertexShader : IGpuVertexShader
/// {
///     public string Name => "TransformVertex";
///
///     public InputLayoutDescriptor InputLayout { get; } = new([
///         new InputAttribute("position", AttributeType.Float3, 0),
///         new InputAttribute("normal", AttributeType.Float3, 1),
///         new InputAttribute("texCoord", AttributeType.Float2, 2)
///     ]);
///
///     public IReadOnlyList&lt;UniformDescriptor&gt; Uniforms { get; } = [
///         new UniformDescriptor("model", UniformType.Matrix4),
///         new UniformDescriptor("view", UniformType.Matrix4),
///         new UniformDescriptor("projection", UniformType.Matrix4)
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
public interface IGpuVertexShader
{
    /// <summary>
    /// Gets the name of this vertex shader.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the input layout describing vertex attributes.
    /// </summary>
    InputLayoutDescriptor InputLayout { get; }

    /// <summary>
    /// Gets the uniform parameters used by this shader.
    /// </summary>
    IReadOnlyList<UniformDescriptor> Uniforms { get; }

    /// <summary>
    /// Gets the output attributes produced by this shader.
    /// </summary>
    /// <remarks>
    /// These outputs are passed to the fragment shader as interpolated values.
    /// </remarks>
    IReadOnlyList<InputAttribute> Outputs { get; }

    /// <summary>
    /// Gets the shader source code for the specified backend.
    /// </summary>
    /// <param name="backend">The target shader backend.</param>
    /// <returns>The shader source code.</returns>
    /// <exception cref="NotSupportedException">Thrown if the backend is not supported.</exception>
    string GetShaderSource(ShaderBackend backend);
}
