using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests.Mocks;

/// <summary>
/// Mock implementation of <see cref="IGraphicsDevice"/> for unit testing.
/// </summary>
/// <remarks>
/// Tracks all method calls and generates fake handles for resources.
/// Use the Call* lists to verify operations were performed correctly.
/// </remarks>
public sealed class MockGraphicsDevice : IGraphicsDevice
{
    private uint nextHandle = 1;
    private bool disposed;

    /// <summary>
    /// Records of method calls made to this device.
    /// </summary>
    public List<string> Calls { get; } = [];

    /// <summary>
    /// Generated VAO handles.
    /// </summary>
    public List<uint> GeneratedVAOs { get; } = [];

    /// <summary>
    /// Generated buffer handles.
    /// </summary>
    public List<uint> GeneratedBuffers { get; } = [];

    /// <summary>
    /// Generated texture handles.
    /// </summary>
    public List<uint> GeneratedTextures { get; } = [];

    /// <summary>
    /// Generated shader handles.
    /// </summary>
    public List<uint> GeneratedShaders { get; } = [];

    /// <summary>
    /// Generated program handles.
    /// </summary>
    public List<uint> GeneratedPrograms { get; } = [];

    /// <summary>
    /// Deleted VAO handles.
    /// </summary>
    public List<uint> DeletedVAOs { get; } = [];

    /// <summary>
    /// Deleted buffer handles.
    /// </summary>
    public List<uint> DeletedBuffers { get; } = [];

    /// <summary>
    /// Deleted texture handles.
    /// </summary>
    public List<uint> DeletedTextures { get; } = [];

    /// <summary>
    /// Deleted shader handles.
    /// </summary>
    public List<uint> DeletedShaders { get; } = [];

    /// <summary>
    /// Deleted program handles.
    /// </summary>
    public List<uint> DeletedPrograms { get; } = [];

    /// <summary>
    /// Uniform values set, keyed by "location:type".
    /// </summary>
    public Dictionary<string, object> UniformValues { get; } = [];

    /// <summary>
    /// Whether shader compilation should succeed.
    /// </summary>
    public bool ShaderCompileSuccess { get; set; } = true;

    /// <summary>
    /// Whether program linking should succeed.
    /// </summary>
    public bool ProgramLinkSuccess { get; set; } = true;

    /// <summary>
    /// Info log to return for shader/program errors.
    /// </summary>
    public string InfoLog { get; set; } = "";

    /// <summary>
    /// Uniform locations to return, keyed by name.
    /// </summary>
    public Dictionary<string, int> UniformLocations { get; } = [];

    #region Buffer Operations

    public uint GenVertexArray()
    {
        uint handle = nextHandle++;
        GeneratedVAOs.Add(handle);
        Calls.Add($"GenVertexArray() => {handle}");
        return handle;
    }

    public uint GenBuffer()
    {
        uint handle = nextHandle++;
        GeneratedBuffers.Add(handle);
        Calls.Add($"GenBuffer() => {handle}");
        return handle;
    }

    public void BindVertexArray(uint vao)
    {
        Calls.Add($"BindVertexArray({vao})");
    }

    public void BindBuffer(BufferTarget target, uint buffer)
    {
        Calls.Add($"BindBuffer({target}, {buffer})");
    }

    public void BufferData(BufferTarget target, ReadOnlySpan<byte> data, BufferUsage usage)
    {
        Calls.Add($"BufferData({target}, {data.Length} bytes, {usage})");
    }

    public void DeleteVertexArray(uint vao)
    {
        DeletedVAOs.Add(vao);
        Calls.Add($"DeleteVertexArray({vao})");
    }

    public void DeleteBuffer(uint buffer)
    {
        DeletedBuffers.Add(buffer);
        Calls.Add($"DeleteBuffer({buffer})");
    }

    public void EnableVertexAttribArray(uint index)
    {
        Calls.Add($"EnableVertexAttribArray({index})");
    }

    public void VertexAttribPointer(uint index, int size, VertexAttribType type, bool normalized, uint stride, nuint offset)
    {
        Calls.Add($"VertexAttribPointer({index}, {size}, {type}, {normalized}, {stride}, {offset})");
    }

    #endregion

    #region Texture Operations

    public uint GenTexture()
    {
        uint handle = nextHandle++;
        GeneratedTextures.Add(handle);
        Calls.Add($"GenTexture() => {handle}");
        return handle;
    }

    public void BindTexture(TextureTarget target, uint texture)
    {
        Calls.Add($"BindTexture({target}, {texture})");
    }

    public void TexImage2D(TextureTarget target, int level, int width, int height, PixelFormat format, ReadOnlySpan<byte> data)
    {
        Calls.Add($"TexImage2D({target}, {level}, {width}x{height}, {format}, {data.Length} bytes)");
    }

    public void TexParameter(TextureTarget target, TextureParam param, int value)
    {
        Calls.Add($"TexParameter({target}, {param}, {value})");
    }

    public void GenerateMipmap(TextureTarget target)
    {
        Calls.Add($"GenerateMipmap({target})");
    }

    public void DeleteTexture(uint texture)
    {
        DeletedTextures.Add(texture);
        Calls.Add($"DeleteTexture({texture})");
    }

    public void ActiveTexture(TextureUnit unit)
    {
        Calls.Add($"ActiveTexture({unit})");
    }

    #endregion

    #region Shader Operations

    public uint CreateProgram()
    {
        uint handle = nextHandle++;
        GeneratedPrograms.Add(handle);
        Calls.Add($"CreateProgram() => {handle}");
        return handle;
    }

    public uint CreateShader(ShaderType type)
    {
        uint handle = nextHandle++;
        GeneratedShaders.Add(handle);
        Calls.Add($"CreateShader({type}) => {handle}");
        return handle;
    }

    public void ShaderSource(uint shader, string source)
    {
        Calls.Add($"ShaderSource({shader}, {source.Length} chars)");
    }

    public void CompileShader(uint shader)
    {
        Calls.Add($"CompileShader({shader})");
    }

    public bool GetShaderCompileStatus(uint shader)
    {
        Calls.Add($"GetShaderCompileStatus({shader}) => {ShaderCompileSuccess}");
        return ShaderCompileSuccess;
    }

    public string GetShaderInfoLog(uint shader)
    {
        Calls.Add($"GetShaderInfoLog({shader})");
        return InfoLog;
    }

    public void AttachShader(uint program, uint shader)
    {
        Calls.Add($"AttachShader({program}, {shader})");
    }

    public void DetachShader(uint program, uint shader)
    {
        Calls.Add($"DetachShader({program}, {shader})");
    }

    public void LinkProgram(uint program)
    {
        Calls.Add($"LinkProgram({program})");
    }

    public bool GetProgramLinkStatus(uint program)
    {
        Calls.Add($"GetProgramLinkStatus({program}) => {ProgramLinkSuccess}");
        return ProgramLinkSuccess;
    }

    public string GetProgramInfoLog(uint program)
    {
        Calls.Add($"GetProgramInfoLog({program})");
        return InfoLog;
    }

    public void DeleteShader(uint shader)
    {
        DeletedShaders.Add(shader);
        Calls.Add($"DeleteShader({shader})");
    }

    public void DeleteProgram(uint program)
    {
        DeletedPrograms.Add(program);
        Calls.Add($"DeleteProgram({program})");
    }

    public void UseProgram(uint program)
    {
        Calls.Add($"UseProgram({program})");
    }

    public int GetUniformLocation(uint program, string name)
    {
        int location = UniformLocations.GetValueOrDefault(name, -1);
        Calls.Add($"GetUniformLocation({program}, \"{name}\") => {location}");
        return location;
    }

    public void Uniform1(int location, float value)
    {
        UniformValues[$"{location}:float"] = value;
        Calls.Add($"Uniform1({location}, {value}f)");
    }

    public void Uniform1(int location, int value)
    {
        UniformValues[$"{location}:int"] = value;
        Calls.Add($"Uniform1({location}, {value})");
    }

    public void Uniform2(int location, float x, float y)
    {
        UniformValues[$"{location}:vec2"] = new Vector2(x, y);
        Calls.Add($"Uniform2({location}, {x}, {y})");
    }

    public void Uniform3(int location, float x, float y, float z)
    {
        UniformValues[$"{location}:vec3"] = new Vector3(x, y, z);
        Calls.Add($"Uniform3({location}, {x}, {y}, {z})");
    }

    public void Uniform4(int location, float x, float y, float z, float w)
    {
        UniformValues[$"{location}:vec4"] = new Vector4(x, y, z, w);
        Calls.Add($"Uniform4({location}, {x}, {y}, {z}, {w})");
    }

    public void UniformMatrix4(int location, in Matrix4x4 matrix)
    {
        UniformValues[$"{location}:mat4"] = matrix;
        Calls.Add($"UniformMatrix4({location}, ...)");
    }

    #endregion

    #region Rendering Operations

    public void ClearColor(float r, float g, float b, float a)
    {
        Calls.Add($"ClearColor({r}, {g}, {b}, {a})");
    }

    public void Clear(ClearMask mask)
    {
        Calls.Add($"Clear({mask})");
    }

    public void Enable(RenderCapability cap)
    {
        Calls.Add($"Enable({cap})");
    }

    public void Disable(RenderCapability cap)
    {
        Calls.Add($"Disable({cap})");
    }

    public void CullFace(CullFaceMode mode)
    {
        Calls.Add($"CullFace({mode})");
    }

    public void Viewport(int x, int y, uint width, uint height)
    {
        Calls.Add($"Viewport({x}, {y}, {width}, {height})");
    }

    public void Scissor(int x, int y, uint width, uint height)
    {
        Calls.Add($"Scissor({x}, {y}, {width}, {height})");
    }

    public void BlendFunc(BlendFactor srcFactor, BlendFactor dstFactor)
    {
        Calls.Add($"BlendFunc({srcFactor}, {dstFactor})");
    }

    public void DepthFunc(DepthFunction func)
    {
        Calls.Add($"DepthFunc({func})");
    }

    public void DrawElements(PrimitiveType mode, uint count, IndexType type)
    {
        Calls.Add($"DrawElements({mode}, {count}, {type})");
    }

    public void DrawArrays(PrimitiveType mode, int first, uint count)
    {
        Calls.Add($"DrawArrays({mode}, {first}, {count})");
    }

    public void LineWidth(float width)
    {
        Calls.Add($"LineWidth({width})");
    }

    public void PointSize(float size)
    {
        Calls.Add($"PointSize({size})");
    }

    #endregion

    /// <summary>
    /// Clears all recorded calls and resets state.
    /// </summary>
    public void Reset()
    {
        Calls.Clear();
        GeneratedVAOs.Clear();
        GeneratedBuffers.Clear();
        GeneratedTextures.Clear();
        GeneratedShaders.Clear();
        GeneratedPrograms.Clear();
        DeletedVAOs.Clear();
        DeletedBuffers.Clear();
        DeletedTextures.Clear();
        DeletedShaders.Clear();
        DeletedPrograms.Clear();
        UniformValues.Clear();
        nextHandle = 1;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Calls.Add("Dispose()");
    }
}
