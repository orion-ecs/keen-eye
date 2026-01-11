namespace KeenEyes.Shaders;

/// <summary>
/// Specifies the shader language backend for code generation and runtime execution.
/// </summary>
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
