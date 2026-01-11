namespace KeenEyes.Shaders;

/// <summary>
/// Specifies how a GPU buffer will be used, allowing the driver to optimize memory allocation.
/// </summary>
[Flags]
public enum BufferUsage
{
    /// <summary>
    /// No specific usage hint.
    /// </summary>
    None = 0,

    /// <summary>
    /// Buffer will be read by shaders.
    /// </summary>
    ShaderRead = 1 << 0,

    /// <summary>
    /// Buffer will be written by shaders.
    /// </summary>
    ShaderWrite = 1 << 1,

    /// <summary>
    /// Buffer can be both read and written by shaders.
    /// </summary>
    ShaderReadWrite = ShaderRead | ShaderWrite,

    /// <summary>
    /// Buffer data will be uploaded from CPU frequently.
    /// </summary>
    DynamicUpload = 1 << 2,

    /// <summary>
    /// Buffer data will be downloaded to CPU frequently.
    /// </summary>
    DynamicDownload = 1 << 3,

    /// <summary>
    /// Buffer data is uploaded once and rarely changes.
    /// </summary>
    Static = 1 << 4,

    /// <summary>
    /// Buffer is used for indirect dispatch arguments.
    /// </summary>
    IndirectArguments = 1 << 5,

    /// <summary>
    /// Buffer is used for uniform/constant data.
    /// </summary>
    Uniform = 1 << 6,

    /// <summary>
    /// Buffer is used as a structured buffer (array of structs).
    /// </summary>
    Structured = 1 << 7
}
