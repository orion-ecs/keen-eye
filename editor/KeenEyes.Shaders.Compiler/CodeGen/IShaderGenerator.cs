using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.CodeGen;

/// <summary>
/// Interface for shader code generators that translate KESL AST to target shader languages.
/// </summary>
public interface IShaderGenerator
{
    /// <summary>
    /// Gets the shader backend this generator targets.
    /// </summary>
    ShaderBackend Backend { get; }

    /// <summary>
    /// Generates shader code for a compute shader declaration.
    /// </summary>
    /// <param name="compute">The compute shader AST.</param>
    /// <returns>The generated shader source code.</returns>
    string Generate(ComputeDeclaration compute);

    /// <summary>
    /// Generates shader code for a vertex shader declaration.
    /// </summary>
    /// <param name="vertex">The vertex shader AST.</param>
    /// <returns>The generated shader source code.</returns>
    string Generate(VertexDeclaration vertex);

    /// <summary>
    /// Generates shader code for a fragment shader declaration.
    /// </summary>
    /// <param name="fragment">The fragment shader AST.</param>
    /// <returns>The generated shader source code.</returns>
    string Generate(FragmentDeclaration fragment);

    /// <summary>
    /// Generates shader code for a geometry shader declaration.
    /// </summary>
    /// <param name="geometry">The geometry shader AST.</param>
    /// <returns>The generated shader source code.</returns>
    string Generate(GeometryDeclaration geometry);

    /// <summary>
    /// Gets the file extension for this shader language (without the dot).
    /// </summary>
    string FileExtension { get; }
}

/// <summary>
/// Specifies the shader language backend for code generation.
/// </summary>
/// <remarks>
/// This is a compile-time enum matching KeenEyes.Shaders.ShaderBackend,
/// used when KeenEyes.Shaders package is not referenced.
/// </remarks>
public enum ShaderBackend
{
    /// <summary>
    /// OpenGL Shading Language (GLSL) for OpenGL/Vulkan platforms.
    /// </summary>
    GLSL,

    /// <summary>
    /// High-Level Shader Language (HLSL) for DirectX platforms.
    /// </summary>
    HLSL,

    /// <summary>
    /// Metal Shading Language for Apple platforms.
    /// </summary>
    MSL,

    /// <summary>
    /// SPIR-V intermediate representation for Vulkan.
    /// </summary>
    SPIRV
}
