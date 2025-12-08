using System.Numerics;
using KeenEyes.Graphics.Backend;

namespace KeenEyes.Graphics;

/// <summary>
/// Represents a compiled shader program stored on the GPU.
/// </summary>
internal sealed class ShaderData : IDisposable
{
    /// <summary>
    /// The shader program handle.
    /// </summary>
    public uint Handle { get; init; }

    /// <summary>
    /// Cached uniform locations for fast access.
    /// </summary>
    public Dictionary<string, int> UniformLocations { get; } = [];

    private bool disposed;

    /// <summary>
    /// Action to delete GPU resources. Set by the ShaderManager.
    /// </summary>
    public Action<ShaderData>? DeleteAction { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DeleteAction?.Invoke(this);
    }
}

/// <summary>
/// Manages shader resources on the GPU.
/// </summary>
internal sealed class ShaderManager : IDisposable
{
    private readonly Dictionary<int, ShaderData> shaders = [];
    private int nextShaderId = 1;
    private bool disposed;

    /// <summary>
    /// Graphics device for GPU operations. Set during initialization.
    /// </summary>
    public IGraphicsDevice? Device { get; set; }

    /// <summary>
    /// Compiles and links a shader program from vertex and fragment source.
    /// </summary>
    /// <param name="vertexSource">The vertex shader GLSL source code.</param>
    /// <param name="fragmentSource">The fragment shader GLSL source code.</param>
    /// <returns>The shader resource handle.</returns>
    /// <exception cref="InvalidOperationException">Thrown when shader compilation fails.</exception>
    public int CreateShader(string vertexSource, string fragmentSource)
    {
        if (Device is null)
        {
            throw new InvalidOperationException("ShaderManager not initialized with graphics device");
        }

        uint vertexShader = CompileShader(ShaderType.Vertex, vertexSource);
        uint fragmentShader = CompileShader(ShaderType.Fragment, fragmentSource);

        uint program = Device.CreateProgram();
        Device.AttachShader(program, vertexShader);
        Device.AttachShader(program, fragmentShader);
        Device.LinkProgram(program);

        // Check for linking errors
        if (!Device.GetProgramLinkStatus(program))
        {
            string infoLog = Device.GetProgramInfoLog(program);
            Device.DeleteProgram(program);
            Device.DeleteShader(vertexShader);
            Device.DeleteShader(fragmentShader);
            throw new InvalidOperationException($"Shader program linking failed: {infoLog}");
        }

        // Clean up shader objects (they're now part of the program)
        Device.DetachShader(program, vertexShader);
        Device.DetachShader(program, fragmentShader);
        Device.DeleteShader(vertexShader);
        Device.DeleteShader(fragmentShader);

        var shaderData = new ShaderData
        {
            Handle = program,
            DeleteAction = DeleteShaderData
        };

        // Cache common uniform locations
        CacheUniformLocations(shaderData);

        int id = nextShaderId++;
        shaders[id] = shaderData;
        return id;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = Device!.CreateShader(type);
        Device.ShaderSource(shader, source);
        Device.CompileShader(shader);

        if (!Device.GetShaderCompileStatus(shader))
        {
            string infoLog = Device.GetShaderInfoLog(shader);
            Device.DeleteShader(shader);
            throw new InvalidOperationException($"Shader compilation failed ({type}): {infoLog}");
        }

        return shader;
    }

    private void CacheUniformLocations(ShaderData shader)
    {
        // Cache common uniform names
        string[] commonUniforms =
        [
            "uModel", "uView", "uProjection", "uMVP",
            "uColor", "uTexture", "uNormalMap",
            "uCameraPosition", "uTime",
            "uLightDirection", "uLightColor", "uLightIntensity",
            "uMetallic", "uRoughness", "uEmissive"
        ];

        foreach (string name in commonUniforms)
        {
            int location = Device!.GetUniformLocation(shader.Handle, name);
            if (location >= 0)
            {
                shader.UniformLocations[name] = location;
            }
        }
    }

    /// <summary>
    /// Gets the shader data for the specified handle.
    /// </summary>
    /// <param name="shaderId">The shader resource handle.</param>
    /// <returns>The shader data, or null if not found.</returns>
    public ShaderData? GetShader(int shaderId)
    {
        return shaders.GetValueOrDefault(shaderId);
    }

    /// <summary>
    /// Gets a uniform location, using the cache if available.
    /// </summary>
    /// <param name="shaderId">The shader resource handle.</param>
    /// <param name="name">The uniform name.</param>
    /// <returns>The uniform location, or -1 if not found.</returns>
    public int GetUniformLocation(int shaderId, string name)
    {
        if (!shaders.TryGetValue(shaderId, out var shader))
        {
            return -1;
        }

        if (shader.UniformLocations.TryGetValue(name, out int cachedLocation))
        {
            return cachedLocation;
        }

        if (Device is null)
        {
            return -1;
        }

        int location = Device.GetUniformLocation(shader.Handle, name);
        if (location >= 0)
        {
            shader.UniformLocations[name] = location;
        }
        return location;
    }

    /// <summary>
    /// Sets a matrix4x4 uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, in Matrix4x4 value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && Device is not null)
        {
            Device.UniformMatrix4(location, value);
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, in Vector4 value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && Device is not null)
        {
            Device.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, in Vector3 value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && Device is not null)
        {
            Device.Uniform3(location, value.X, value.Y, value.Z);
        }
    }

    /// <summary>
    /// Sets a float uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, float value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && Device is not null)
        {
            Device.Uniform1(location, value);
        }
    }

    /// <summary>
    /// Sets an int uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, int value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && Device is not null)
        {
            Device.Uniform1(location, value);
        }
    }

    /// <summary>
    /// Deletes a shader resource.
    /// </summary>
    /// <param name="shaderId">The shader resource handle.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteShader(int shaderId)
    {
        if (shaders.Remove(shaderId, out var shaderData))
        {
            shaderData.Dispose();
            return true;
        }
        return false;
    }

    private void DeleteShaderData(ShaderData data)
    {
        Device?.DeleteProgram(data.Handle);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var shader in shaders.Values)
        {
            shader.Dispose();
        }
        shaders.Clear();
    }
}
