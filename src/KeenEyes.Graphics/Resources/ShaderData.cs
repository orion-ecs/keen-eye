using System.Numerics;

namespace KeenEyes.Graphics;

/// <summary>
/// Represents a compiled shader program stored on the GPU.
/// </summary>
internal sealed class ShaderData : IDisposable
{
    /// <summary>
    /// The OpenGL shader program handle.
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
    /// Silk.NET OpenGL context. Set during initialization.
    /// </summary>
    public Silk.NET.OpenGL.GL? GL { get; set; }

    /// <summary>
    /// Compiles and links a shader program from vertex and fragment source.
    /// </summary>
    /// <param name="vertexSource">The vertex shader GLSL source code.</param>
    /// <param name="fragmentSource">The fragment shader GLSL source code.</param>
    /// <returns>The shader resource handle.</returns>
    /// <exception cref="InvalidOperationException">Thrown when shader compilation fails.</exception>
    public int CreateShader(string vertexSource, string fragmentSource)
    {
        if (GL is null)
        {
            throw new InvalidOperationException("ShaderManager not initialized with GL context");
        }

        uint vertexShader = CompileShader(Silk.NET.OpenGL.ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CompileShader(Silk.NET.OpenGL.ShaderType.FragmentShader, fragmentSource);

        uint program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        // Check for linking errors
        GL.GetProgram(program, Silk.NET.OpenGL.ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            GL.DeleteProgram(program);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            throw new InvalidOperationException($"Shader program linking failed: {infoLog}");
        }

        // Clean up shader objects (they're now part of the program)
        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

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

    private uint CompileShader(Silk.NET.OpenGL.ShaderType type, string source)
    {
        uint shader = GL!.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, Silk.NET.OpenGL.ShaderParameterName.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
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
            int location = GL!.GetUniformLocation(shader.Handle, name);
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

        if (GL is null)
        {
            return -1;
        }

        int location = GL.GetUniformLocation(shader.Handle, name);
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
        if (location >= 0 && GL is not null)
        {
            unsafe
            {
                fixed (float* ptr = &value.M11)
                {
                    GL.UniformMatrix4(location, 1, false, ptr);
                }
            }
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, in Vector4 value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && GL is not null)
        {
            GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, in Vector3 value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && GL is not null)
        {
            GL.Uniform3(location, value.X, value.Y, value.Z);
        }
    }

    /// <summary>
    /// Sets a float uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, float value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && GL is not null)
        {
            GL.Uniform1(location, value);
        }
    }

    /// <summary>
    /// Sets an int uniform value.
    /// </summary>
    public void SetUniform(int shaderId, string name, int value)
    {
        int location = GetUniformLocation(shaderId, name);
        if (location >= 0 && GL is not null)
        {
            GL.Uniform1(location, value);
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
        GL?.DeleteProgram(data.Handle);
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
