using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using KeenEyes.Graphics.Abstractions;

using Silk.NET.OpenGL;

namespace KeenEyes.Graphics.Silk.Backend;

/// <summary>
/// OpenGL implementation of <see cref="IGraphicsDevice"/> using Silk.NET.
/// </summary>
/// <param name="gl">The Silk.NET OpenGL context.</param>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context; logic tested via MockGraphicsDevice")]
public sealed class OpenGLDevice(GL gl) : IGraphicsDevice
{
    private readonly GL gl = gl ?? throw new ArgumentNullException(nameof(gl));
    private bool disposed;

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
    public void VertexAttribPointer(uint index, int size, Abstractions.VertexAttribType type, bool normalized, uint stride, nuint offset)
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
    public void BindTexture(Abstractions.TextureTarget target, uint texture)
        => gl.BindTexture(ToGL(target), texture);

    /// <inheritdoc />
    public void TexImage2D(Abstractions.TextureTarget target, int level, int width, int height, Abstractions.PixelFormat format, ReadOnlySpan<byte> data)
    {
        unsafe
        {

            if (data.IsEmpty)
            {
                // Allocate GPU memory without initializing (null pointer)
                gl.TexImage2D(
                    ToGL(target),
                    level,
                    ToGLInternalFormat(format),
                    (uint)width,
                    (uint)height,
                    0,
                    ToGL(format),
                    PixelType.UnsignedByte,
                    null);
            }
            else
            {
                fixed (byte* ptr = data)
                {
                    gl.TexImage2D(
                        ToGL(target),
                        level,
                        ToGLInternalFormat(format),
                        (uint)width,
                        (uint)height,
                        0,
                        ToGL(format),
                        PixelType.UnsignedByte,
                        ptr);
                }
            }
        }
    }

    /// <inheritdoc />
    public void TexSubImage2D(Abstractions.TextureTarget target, int level, int xOffset, int yOffset, int width, int height, Abstractions.PixelFormat format, ReadOnlySpan<byte> data)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.TexSubImage2D(
                    ToGL(target),
                    level,
                    xOffset,
                    yOffset,
                    (uint)width,
                    (uint)height,
                    ToGL(format),
                    PixelType.UnsignedByte,
                    ptr);
            }
        }
    }

    /// <inheritdoc />
    public void TexParameter(Abstractions.TextureTarget target, TextureParam param, int value)
        => gl.TexParameter(ToGL(target), ToGL(param), value);

    /// <inheritdoc />
    public void GenerateMipmap(Abstractions.TextureTarget target)
        => gl.GenerateMipmap(ToGL(target));

    /// <inheritdoc />
    public void CompressedTexImage2D(
        Abstractions.TextureTarget target,
        int level,
        int width,
        int height,
        Abstractions.CompressedTextureFormat format,
        ReadOnlySpan<byte> data)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.CompressedTexImage2D(
                    ToGL(target),
                    level,
                    ToGLCompressed(format),
                    (uint)width,
                    (uint)height,
                    0, // border must be 0
                    (uint)data.Length,
                    ptr);
            }
        }
    }

    /// <inheritdoc />
    public void DeleteTexture(uint texture) => gl.DeleteTexture(texture);

    /// <inheritdoc />
    public void ActiveTexture(Abstractions.TextureUnit unit)
        => gl.ActiveTexture(ToGL(unit));

    /// <inheritdoc />
    public void PixelStore(Abstractions.PixelStoreParameter param, int value)
        => gl.PixelStore(ToGL(param), value);

    #endregion

    #region Shader Operations

    /// <inheritdoc />
    public uint CreateProgram() => gl.CreateProgram();

    /// <inheritdoc />
    public uint CreateShader(Abstractions.ShaderType type) => gl.CreateShader(ToGL(type));

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
    public void Uniform2(int location, float x, float y)
        => gl.Uniform2(location, x, y);

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
    public void Scissor(int x, int y, uint width, uint height)
        => gl.Scissor(x, y, width, height);

    /// <inheritdoc />
    public void BlendFunc(Abstractions.BlendFactor srcFactor, Abstractions.BlendFactor dstFactor)
        => gl.BlendFunc(ToGL(srcFactor), ToGL(dstFactor));

    /// <inheritdoc />
    public void DepthFunc(Abstractions.DepthFunction func)
        => gl.DepthFunc(ToGL(func));

    /// <inheritdoc />
    public void DrawElements(Abstractions.PrimitiveType mode, uint count, Abstractions.IndexType type)
    {
        unsafe
        {
            gl.DrawElements(ToGL(mode), count, ToGL(type), null);
        }
    }

    /// <inheritdoc />
    public void DrawArrays(Abstractions.PrimitiveType mode, int first, uint count)
        => gl.DrawArrays(ToGL(mode), first, count);

    /// <inheritdoc />
    public void LineWidth(float width) => gl.LineWidth(width);

    /// <inheritdoc />
    public void PointSize(float size) => gl.PointSize(size);

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

    private static VertexAttribPointerType ToGL(Abstractions.VertexAttribType type) => type switch
    {
        Abstractions.VertexAttribType.Float => VertexAttribPointerType.Float,
        Abstractions.VertexAttribType.Int => VertexAttribPointerType.Int,
        Abstractions.VertexAttribType.UnsignedByte => VertexAttribPointerType.UnsignedByte,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static global::Silk.NET.OpenGL.TextureTarget ToGL(Abstractions.TextureTarget target) => target switch
    {
        Abstractions.TextureTarget.Texture2D => global::Silk.NET.OpenGL.TextureTarget.Texture2D,
        Abstractions.TextureTarget.TextureCubeMap => global::Silk.NET.OpenGL.TextureTarget.TextureCubeMap,
        Abstractions.TextureTarget.Texture2DArray => global::Silk.NET.OpenGL.TextureTarget.Texture2DArray,
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

    private static global::Silk.NET.OpenGL.TextureUnit ToGL(Abstractions.TextureUnit unit) => unit switch
    {
        Abstractions.TextureUnit.Texture0 => global::Silk.NET.OpenGL.TextureUnit.Texture0,
        Abstractions.TextureUnit.Texture1 => global::Silk.NET.OpenGL.TextureUnit.Texture1,
        Abstractions.TextureUnit.Texture2 => global::Silk.NET.OpenGL.TextureUnit.Texture2,
        Abstractions.TextureUnit.Texture3 => global::Silk.NET.OpenGL.TextureUnit.Texture3,
        Abstractions.TextureUnit.Texture4 => global::Silk.NET.OpenGL.TextureUnit.Texture4,
        Abstractions.TextureUnit.Texture5 => global::Silk.NET.OpenGL.TextureUnit.Texture5,
        Abstractions.TextureUnit.Texture6 => global::Silk.NET.OpenGL.TextureUnit.Texture6,
        Abstractions.TextureUnit.Texture7 => global::Silk.NET.OpenGL.TextureUnit.Texture7,
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static global::Silk.NET.OpenGL.ShaderType ToGL(Abstractions.ShaderType type) => type switch
    {
        Abstractions.ShaderType.Vertex => global::Silk.NET.OpenGL.ShaderType.VertexShader,
        Abstractions.ShaderType.Fragment => global::Silk.NET.OpenGL.ShaderType.FragmentShader,
        Abstractions.ShaderType.Geometry => global::Silk.NET.OpenGL.ShaderType.GeometryShader,
        Abstractions.ShaderType.Compute => global::Silk.NET.OpenGL.ShaderType.ComputeShader,
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
        RenderCapability.ScissorTest => EnableCap.ScissorTest,
        RenderCapability.StencilTest => EnableCap.StencilTest,
        _ => throw new ArgumentOutOfRangeException(nameof(cap))
    };

    private static TriangleFace ToGL(CullFaceMode mode) => mode switch
    {
        CullFaceMode.Front => TriangleFace.Front,
        CullFaceMode.Back => TriangleFace.Back,
        CullFaceMode.FrontAndBack => TriangleFace.FrontAndBack,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static global::Silk.NET.OpenGL.PrimitiveType ToGL(Abstractions.PrimitiveType mode) => mode switch
    {
        Abstractions.PrimitiveType.Triangles => global::Silk.NET.OpenGL.PrimitiveType.Triangles,
        Abstractions.PrimitiveType.TriangleStrip => global::Silk.NET.OpenGL.PrimitiveType.TriangleStrip,
        Abstractions.PrimitiveType.TriangleFan => global::Silk.NET.OpenGL.PrimitiveType.TriangleFan,
        Abstractions.PrimitiveType.Lines => global::Silk.NET.OpenGL.PrimitiveType.Lines,
        Abstractions.PrimitiveType.LineStrip => global::Silk.NET.OpenGL.PrimitiveType.LineStrip,
        Abstractions.PrimitiveType.LineLoop => global::Silk.NET.OpenGL.PrimitiveType.LineLoop,
        Abstractions.PrimitiveType.Points => global::Silk.NET.OpenGL.PrimitiveType.Points,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static DrawElementsType ToGL(Abstractions.IndexType type) => type switch
    {
        Abstractions.IndexType.UnsignedByte => DrawElementsType.UnsignedByte,
        Abstractions.IndexType.UnsignedShort => DrawElementsType.UnsignedShort,
        Abstractions.IndexType.UnsignedInt => DrawElementsType.UnsignedInt,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static global::Silk.NET.OpenGL.BlendingFactor ToGL(Abstractions.BlendFactor factor) => factor switch
    {
        Abstractions.BlendFactor.Zero => global::Silk.NET.OpenGL.BlendingFactor.Zero,
        Abstractions.BlendFactor.One => global::Silk.NET.OpenGL.BlendingFactor.One,
        Abstractions.BlendFactor.SrcColor => global::Silk.NET.OpenGL.BlendingFactor.SrcColor,
        Abstractions.BlendFactor.OneMinusSrcColor => global::Silk.NET.OpenGL.BlendingFactor.OneMinusSrcColor,
        Abstractions.BlendFactor.DstColor => global::Silk.NET.OpenGL.BlendingFactor.DstColor,
        Abstractions.BlendFactor.OneMinusDstColor => global::Silk.NET.OpenGL.BlendingFactor.OneMinusDstColor,
        Abstractions.BlendFactor.SrcAlpha => global::Silk.NET.OpenGL.BlendingFactor.SrcAlpha,
        Abstractions.BlendFactor.OneMinusSrcAlpha => global::Silk.NET.OpenGL.BlendingFactor.OneMinusSrcAlpha,
        Abstractions.BlendFactor.DstAlpha => global::Silk.NET.OpenGL.BlendingFactor.DstAlpha,
        Abstractions.BlendFactor.OneMinusDstAlpha => global::Silk.NET.OpenGL.BlendingFactor.OneMinusDstAlpha,
        _ => throw new ArgumentOutOfRangeException(nameof(factor))
    };

    private static global::Silk.NET.OpenGL.DepthFunction ToGL(Abstractions.DepthFunction func) => func switch
    {
        Abstractions.DepthFunction.Never => global::Silk.NET.OpenGL.DepthFunction.Never,
        Abstractions.DepthFunction.Less => global::Silk.NET.OpenGL.DepthFunction.Less,
        Abstractions.DepthFunction.Equal => global::Silk.NET.OpenGL.DepthFunction.Equal,
        Abstractions.DepthFunction.LessOrEqual => global::Silk.NET.OpenGL.DepthFunction.Lequal,
        Abstractions.DepthFunction.Greater => global::Silk.NET.OpenGL.DepthFunction.Greater,
        Abstractions.DepthFunction.NotEqual => global::Silk.NET.OpenGL.DepthFunction.Notequal,
        Abstractions.DepthFunction.GreaterOrEqual => global::Silk.NET.OpenGL.DepthFunction.Gequal,
        Abstractions.DepthFunction.Always => global::Silk.NET.OpenGL.DepthFunction.Always,
        _ => throw new ArgumentOutOfRangeException(nameof(func))
    };

    private static global::Silk.NET.OpenGL.PixelFormat ToGL(Abstractions.PixelFormat format) => format switch
    {
        Abstractions.PixelFormat.R => global::Silk.NET.OpenGL.PixelFormat.Red,
        Abstractions.PixelFormat.RG => global::Silk.NET.OpenGL.PixelFormat.RG,
        Abstractions.PixelFormat.RGB => global::Silk.NET.OpenGL.PixelFormat.Rgb,
        Abstractions.PixelFormat.RGBA => global::Silk.NET.OpenGL.PixelFormat.Rgba,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static InternalFormat ToGLInternalFormat(Abstractions.PixelFormat format) => format switch
    {
        Abstractions.PixelFormat.R => InternalFormat.R8,
        Abstractions.PixelFormat.RG => InternalFormat.RG8,
        Abstractions.PixelFormat.RGB => InternalFormat.Rgb8,
        Abstractions.PixelFormat.RGBA => InternalFormat.Rgba8,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static global::Silk.NET.OpenGL.PixelStoreParameter ToGL(Abstractions.PixelStoreParameter param) => param switch
    {
        Abstractions.PixelStoreParameter.UnpackRowLength => global::Silk.NET.OpenGL.PixelStoreParameter.UnpackRowLength,
        Abstractions.PixelStoreParameter.UnpackSkipRows => global::Silk.NET.OpenGL.PixelStoreParameter.UnpackSkipRows,
        Abstractions.PixelStoreParameter.UnpackSkipPixels => global::Silk.NET.OpenGL.PixelStoreParameter.UnpackSkipPixels,
        Abstractions.PixelStoreParameter.UnpackAlignment => global::Silk.NET.OpenGL.PixelStoreParameter.UnpackAlignment,
        _ => throw new ArgumentOutOfRangeException(nameof(param))
    };

    private static InternalFormat ToGLCompressed(Abstractions.CompressedTextureFormat format) => format switch
    {
        Abstractions.CompressedTextureFormat.Bc1 => InternalFormat.CompressedRgbS3TCDxt1Ext,
        Abstractions.CompressedTextureFormat.Bc1Alpha => InternalFormat.CompressedRgbaS3TCDxt1Ext,
        Abstractions.CompressedTextureFormat.Bc2 => InternalFormat.CompressedRgbaS3TCDxt3Ext,
        Abstractions.CompressedTextureFormat.Bc3 => InternalFormat.CompressedRgbaS3TCDxt5Ext,
        Abstractions.CompressedTextureFormat.Bc4 => InternalFormat.CompressedRedRgtc1,
        Abstractions.CompressedTextureFormat.Bc5 => InternalFormat.CompressedRGRgtc2,
        Abstractions.CompressedTextureFormat.Bc6h => InternalFormat.CompressedRgbBptcUnsignedFloat,
        Abstractions.CompressedTextureFormat.Bc7 => InternalFormat.CompressedRgbaBptcUnorm,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    #endregion

    #region Debug

    /// <inheritdoc />
    public void GetTexImage(Abstractions.TextureTarget target, int level, Abstractions.PixelFormat format, Span<byte> data)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.GetTexImage(ToGL(target), level, ToGL(format), PixelType.UnsignedByte, ptr);
            }
        }
    }

    /// <inheritdoc />
    public void ReadFramebuffer(int x, int y, int width, int height, Abstractions.PixelFormat format, Span<byte> data)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.ReadPixels(x, y, (uint)width, (uint)height, ToGL(format), PixelType.UnsignedByte, ptr);
            }
        }
    }

    #endregion

    /// <inheritdoc />
    public int GetError() => (int)gl.GetError();

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
