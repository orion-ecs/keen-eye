using System.Numerics;
using Silk.NET.OpenGL;

namespace KeenEyes.Graphics.Backend;

/// <summary>
/// OpenGL implementation of <see cref="IGraphicsDevice"/> using Silk.NET.
/// </summary>
public sealed class OpenGLDevice : IGraphicsDevice
{
    private readonly GL gl;
    private bool disposed;

    /// <summary>
    /// Creates a new OpenGL device wrapper.
    /// </summary>
    /// <param name="gl">The Silk.NET OpenGL context.</param>
    public OpenGLDevice(GL gl)
    {
        this.gl = gl ?? throw new ArgumentNullException(nameof(gl));
    }

    #region Buffer Operations

    /// <inheritdoc />
    public uint GenVertexArray() => gl.GenVertexArray();

    /// <inheritdoc />
    public uint GenBuffer() => gl.GenBuffer();

    /// <inheritdoc />
    public void BindVertexArray(uint vao) => gl.BindVertexArray(vao);

    /// <inheritdoc />
    public void BindBuffer(BufferTarget target, uint buffer)
        => gl.BindBuffer(ToGL(target), buffer);

    /// <inheritdoc />
    public void BufferData(BufferTarget target, ReadOnlySpan<byte> data, BufferUsage usage)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.BufferData(ToGL(target), (nuint)data.Length, ptr, ToGL(usage));
            }
        }
    }

    /// <inheritdoc />
    public void DeleteVertexArray(uint vao) => gl.DeleteVertexArray(vao);

    /// <inheritdoc />
    public void DeleteBuffer(uint buffer) => gl.DeleteBuffer(buffer);

    /// <inheritdoc />
    public void EnableVertexAttribArray(uint index) => gl.EnableVertexAttribArray(index);

    /// <inheritdoc />
    public void VertexAttribPointer(uint index, int size, VertexAttribType type, bool normalized, uint stride, nuint offset)
    {
        unsafe
        {
            gl.VertexAttribPointer(index, size, ToGL(type), normalized, stride, (void*)offset);
        }
    }

    #endregion

    #region Texture Operations

    /// <inheritdoc />
    public uint GenTexture() => gl.GenTexture();

    /// <inheritdoc />
    public void BindTexture(TextureTarget target, uint texture)
        => gl.BindTexture(ToGL(target), texture);

    /// <inheritdoc />
    public void TexImage2D(TextureTarget target, int level, int width, int height, ReadOnlySpan<byte> data)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.TexImage2D(
                    ToGL(target),
                    level,
                    InternalFormat.Rgba,
                    (uint)width,
                    (uint)height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
            }
        }
    }

    /// <inheritdoc />
    public void TexParameter(TextureTarget target, TextureParam param, int value)
        => gl.TexParameter(ToGL(target), ToGL(param), value);

    /// <inheritdoc />
    public void GenerateMipmap(TextureTarget target)
        => gl.GenerateMipmap(ToGL(target));

    /// <inheritdoc />
    public void DeleteTexture(uint texture) => gl.DeleteTexture(texture);

    /// <inheritdoc />
    public void ActiveTexture(TextureUnit unit)
        => gl.ActiveTexture(ToGL(unit));

    #endregion

    #region Shader Operations

    /// <inheritdoc />
    public uint CreateProgram() => gl.CreateProgram();

    /// <inheritdoc />
    public uint CreateShader(ShaderType type) => gl.CreateShader(ToGL(type));

    /// <inheritdoc />
    public void ShaderSource(uint shader, string source) => gl.ShaderSource(shader, source);

    /// <inheritdoc />
    public void CompileShader(uint shader) => gl.CompileShader(shader);

    /// <inheritdoc />
    public bool GetShaderCompileStatus(uint shader)
    {
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        return status != 0;
    }

    /// <inheritdoc />
    public string GetShaderInfoLog(uint shader) => gl.GetShaderInfoLog(shader);

    /// <inheritdoc />
    public void AttachShader(uint program, uint shader) => gl.AttachShader(program, shader);

    /// <inheritdoc />
    public void DetachShader(uint program, uint shader) => gl.DetachShader(program, shader);

    /// <inheritdoc />
    public void LinkProgram(uint program) => gl.LinkProgram(program);

    /// <inheritdoc />
    public bool GetProgramLinkStatus(uint program)
    {
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);
        return status != 0;
    }

    /// <inheritdoc />
    public string GetProgramInfoLog(uint program) => gl.GetProgramInfoLog(program);

    /// <inheritdoc />
    public void DeleteShader(uint shader) => gl.DeleteShader(shader);

    /// <inheritdoc />
    public void DeleteProgram(uint program) => gl.DeleteProgram(program);

    /// <inheritdoc />
    public void UseProgram(uint program) => gl.UseProgram(program);

    /// <inheritdoc />
    public int GetUniformLocation(uint program, string name)
        => gl.GetUniformLocation(program, name);

    /// <inheritdoc />
    public void Uniform1(int location, float value) => gl.Uniform1(location, value);

    /// <inheritdoc />
    public void Uniform1(int location, int value) => gl.Uniform1(location, value);

    /// <inheritdoc />
    public void Uniform3(int location, float x, float y, float z)
        => gl.Uniform3(location, x, y, z);

    /// <inheritdoc />
    public void Uniform4(int location, float x, float y, float z, float w)
        => gl.Uniform4(location, x, y, z, w);

    /// <inheritdoc />
    public void UniformMatrix4(int location, in Matrix4x4 matrix)
    {
        unsafe
        {
            fixed (float* ptr = &matrix.M11)
            {
                gl.UniformMatrix4(location, 1, false, ptr);
            }
        }
    }

    #endregion

    #region Rendering Operations

    /// <inheritdoc />
    public void ClearColor(float r, float g, float b, float a)
        => gl.ClearColor(r, g, b, a);

    /// <inheritdoc />
    public void Clear(ClearMask mask) => gl.Clear(ToGL(mask));

    /// <inheritdoc />
    public void Enable(RenderCapability cap) => gl.Enable(ToGL(cap));

    /// <inheritdoc />
    public void Disable(RenderCapability cap) => gl.Disable(ToGL(cap));

    /// <inheritdoc />
    public void CullFace(CullFaceMode mode) => gl.CullFace(ToGL(mode));

    /// <inheritdoc />
    public void Viewport(int x, int y, uint width, uint height)
        => gl.Viewport(x, y, width, height);

    /// <inheritdoc />
    public void DrawElements(PrimitiveType mode, uint count, IndexType type)
    {
        unsafe
        {
            gl.DrawElements(ToGL(mode), count, ToGL(type), null);
        }
    }

    #endregion

    #region Enum Conversions

    private static BufferTargetARB ToGL(BufferTarget target) => target switch
    {
        BufferTarget.ArrayBuffer => BufferTargetARB.ArrayBuffer,
        BufferTarget.ElementArrayBuffer => BufferTargetARB.ElementArrayBuffer,
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private static BufferUsageARB ToGL(BufferUsage usage) => usage switch
    {
        BufferUsage.StaticDraw => BufferUsageARB.StaticDraw,
        BufferUsage.DynamicDraw => BufferUsageARB.DynamicDraw,
        BufferUsage.StreamDraw => BufferUsageARB.StreamDraw,
        _ => throw new ArgumentOutOfRangeException(nameof(usage))
    };

    private static VertexAttribPointerType ToGL(VertexAttribType type) => type switch
    {
        VertexAttribType.Float => VertexAttribPointerType.Float,
        VertexAttribType.Int => VertexAttribPointerType.Int,
        VertexAttribType.UnsignedByte => VertexAttribPointerType.UnsignedByte,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static Silk.NET.OpenGL.TextureTarget ToGL(TextureTarget target) => target switch
    {
        TextureTarget.Texture2D => Silk.NET.OpenGL.TextureTarget.Texture2D,
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private static TextureParameterName ToGL(TextureParam param) => param switch
    {
        TextureParam.MinFilter => TextureParameterName.TextureMinFilter,
        TextureParam.MagFilter => TextureParameterName.TextureMagFilter,
        TextureParam.WrapS => TextureParameterName.TextureWrapS,
        TextureParam.WrapT => TextureParameterName.TextureWrapT,
        _ => throw new ArgumentOutOfRangeException(nameof(param))
    };

    private static Silk.NET.OpenGL.TextureUnit ToGL(TextureUnit unit) => unit switch
    {
        TextureUnit.Texture0 => Silk.NET.OpenGL.TextureUnit.Texture0,
        TextureUnit.Texture1 => Silk.NET.OpenGL.TextureUnit.Texture1,
        TextureUnit.Texture2 => Silk.NET.OpenGL.TextureUnit.Texture2,
        TextureUnit.Texture3 => Silk.NET.OpenGL.TextureUnit.Texture3,
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static Silk.NET.OpenGL.ShaderType ToGL(ShaderType type) => type switch
    {
        ShaderType.Vertex => Silk.NET.OpenGL.ShaderType.VertexShader,
        ShaderType.Fragment => Silk.NET.OpenGL.ShaderType.FragmentShader,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static ClearBufferMask ToGL(ClearMask mask)
    {
        ClearBufferMask result = 0;
        if ((mask & ClearMask.ColorBuffer) != 0)
        {
            result |= ClearBufferMask.ColorBufferBit;
        }
        if ((mask & ClearMask.DepthBuffer) != 0)
        {
            result |= ClearBufferMask.DepthBufferBit;
        }
        if ((mask & ClearMask.StencilBuffer) != 0)
        {
            result |= ClearBufferMask.StencilBufferBit;
        }
        return result;
    }

    private static EnableCap ToGL(RenderCapability cap) => cap switch
    {
        RenderCapability.DepthTest => EnableCap.DepthTest,
        RenderCapability.CullFace => EnableCap.CullFace,
        RenderCapability.Blend => EnableCap.Blend,
        _ => throw new ArgumentOutOfRangeException(nameof(cap))
    };

    private static TriangleFace ToGL(CullFaceMode mode) => mode switch
    {
        CullFaceMode.Front => TriangleFace.Front,
        CullFaceMode.Back => TriangleFace.Back,
        CullFaceMode.FrontAndBack => TriangleFace.FrontAndBack,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static Silk.NET.OpenGL.PrimitiveType ToGL(PrimitiveType mode) => mode switch
    {
        PrimitiveType.Triangles => Silk.NET.OpenGL.PrimitiveType.Triangles,
        PrimitiveType.TriangleStrip => Silk.NET.OpenGL.PrimitiveType.TriangleStrip,
        PrimitiveType.Lines => Silk.NET.OpenGL.PrimitiveType.Lines,
        PrimitiveType.Points => Silk.NET.OpenGL.PrimitiveType.Points,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static DrawElementsType ToGL(IndexType type) => type switch
    {
        IndexType.UnsignedShort => DrawElementsType.UnsignedShort,
        IndexType.UnsignedInt => DrawElementsType.UnsignedInt,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        gl.Dispose();
    }
}
