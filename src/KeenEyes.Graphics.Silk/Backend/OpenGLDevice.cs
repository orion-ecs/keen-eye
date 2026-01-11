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
    public void BufferData(BufferTarget target, ReadOnlySpan<float> data, BufferUsage usage)
    {
        unsafe
        {
            fixed (float* ptr = data)
            {
                gl.BufferData(ToGL(target), (nuint)(data.Length * sizeof(float)), ptr, ToGL(usage));
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

    /// <inheritdoc />
    public void VertexAttribDivisor(uint index, uint divisor)
        => gl.VertexAttribDivisor(index, divisor);

    /// <inheritdoc />
    public void BufferSubData(BufferTarget target, nint offset, ReadOnlySpan<byte> data)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.BufferSubData(ToGL(target), offset, (nuint)data.Length, ptr);
            }
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
            var pixelType = ToGLPixelType(format);

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
                    pixelType,
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
                        pixelType,
                        ptr);
                }
            }
        }
    }

    /// <inheritdoc />
    public void TexImage2D(Abstractions.TextureTarget target, int level, int width, int height, Abstractions.PixelFormat format, ReadOnlySpan<float> data)
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
                    PixelType.Float,
                    null);
            }
            else
            {
                fixed (float* ptr = data)
                {
                    gl.TexImage2D(
                        ToGL(target),
                        level,
                        ToGLInternalFormat(format),
                        (uint)width,
                        (uint)height,
                        0,
                        ToGL(format),
                        PixelType.Float,
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
    public void DrawElementsInstanced(Abstractions.PrimitiveType mode, uint count, Abstractions.IndexType type, uint instanceCount)
    {
        unsafe
        {
            gl.DrawElementsInstanced(ToGL(mode), count, ToGL(type), null, instanceCount);
        }
    }

    /// <inheritdoc />
    public void DrawArraysInstanced(Abstractions.PrimitiveType mode, int first, uint count, uint instanceCount)
        => gl.DrawArraysInstanced(ToGL(mode), first, count, instanceCount);

    /// <inheritdoc />
    public void LineWidth(float width) => gl.LineWidth(width);

    /// <inheritdoc />
    public void PointSize(float size) => gl.PointSize(size);

    #endregion

    #region Framebuffer Operations

    /// <inheritdoc />
    public uint GenFramebuffer() => gl.GenFramebuffer();

    /// <inheritdoc />
    public void BindFramebuffer(Abstractions.FramebufferTarget target, uint framebuffer)
        => gl.BindFramebuffer(ToGL(target), framebuffer);

    /// <inheritdoc />
    public void DeleteFramebuffer(uint framebuffer) => gl.DeleteFramebuffer(framebuffer);

    /// <inheritdoc />
    public void FramebufferTexture2D(Abstractions.FramebufferTarget target, Abstractions.FramebufferAttachment attachment,
                                     Abstractions.TextureTarget texTarget, uint texture, int level)
        => gl.FramebufferTexture2D(ToGL(target), ToGL(attachment), ToGL(texTarget), texture, level);

    /// <inheritdoc />
    public Abstractions.FramebufferStatus CheckFramebufferStatus(Abstractions.FramebufferTarget target)
    {
        var status = gl.CheckFramebufferStatus(ToGL(target));
        return FromGL(status);
    }

    /// <inheritdoc />
    public uint GenRenderbuffer() => gl.GenRenderbuffer();

    /// <inheritdoc />
    public void BindRenderbuffer(uint renderbuffer)
        => gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbuffer);

    /// <inheritdoc />
    public void DeleteRenderbuffer(uint renderbuffer) => gl.DeleteRenderbuffer(renderbuffer);

    /// <inheritdoc />
    public void RenderbufferStorage(Abstractions.RenderbufferFormat format, uint width, uint height)
        => gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, ToGL(format), width, height);

    /// <inheritdoc />
    public void FramebufferRenderbuffer(Abstractions.FramebufferTarget target, Abstractions.FramebufferAttachment attachment,
                                        uint renderbuffer)
        => gl.FramebufferRenderbuffer(ToGL(target), ToGL(attachment), RenderbufferTarget.Renderbuffer, renderbuffer);

    /// <inheritdoc />
    public void DrawBuffer(Abstractions.DrawBufferMode mode)
        => gl.DrawBuffer(ToGL(mode));

    /// <inheritdoc />
    public void ReadBuffer(Abstractions.DrawBufferMode mode)
        => gl.ReadBuffer(ToGLReadBuffer(mode));

    /// <inheritdoc />
    public void DepthMask(bool flag) => gl.DepthMask(flag);

    /// <inheritdoc />
    public void ColorMask(bool red, bool green, bool blue, bool alpha)
        => gl.ColorMask(red, green, blue, alpha);

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
        TextureParam.WrapR => TextureParameterName.TextureWrapR,
        TextureParam.CompareMode => TextureParameterName.TextureCompareMode,
        TextureParam.CompareFunc => TextureParameterName.TextureCompareFunc,
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
        Abstractions.TextureUnit.Texture8 => global::Silk.NET.OpenGL.TextureUnit.Texture8,
        Abstractions.TextureUnit.Texture9 => global::Silk.NET.OpenGL.TextureUnit.Texture9,
        Abstractions.TextureUnit.Texture10 => global::Silk.NET.OpenGL.TextureUnit.Texture10,
        Abstractions.TextureUnit.Texture11 => global::Silk.NET.OpenGL.TextureUnit.Texture11,
        Abstractions.TextureUnit.Texture12 => global::Silk.NET.OpenGL.TextureUnit.Texture12,
        Abstractions.TextureUnit.Texture13 => global::Silk.NET.OpenGL.TextureUnit.Texture13,
        Abstractions.TextureUnit.Texture14 => global::Silk.NET.OpenGL.TextureUnit.Texture14,
        Abstractions.TextureUnit.Texture15 => global::Silk.NET.OpenGL.TextureUnit.Texture15,
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
        Abstractions.PixelFormat.Depth16 or Abstractions.PixelFormat.Depth24 or Abstractions.PixelFormat.Depth32F
            => global::Silk.NET.OpenGL.PixelFormat.DepthComponent,
        Abstractions.PixelFormat.Depth24Stencil8
            => global::Silk.NET.OpenGL.PixelFormat.DepthStencil,
        Abstractions.PixelFormat.RGB16F or Abstractions.PixelFormat.RGB32F
            => global::Silk.NET.OpenGL.PixelFormat.Rgb,
        Abstractions.PixelFormat.RGBA16F or Abstractions.PixelFormat.RGBA32F
            => global::Silk.NET.OpenGL.PixelFormat.Rgba,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static InternalFormat ToGLInternalFormat(Abstractions.PixelFormat format) => format switch
    {
        Abstractions.PixelFormat.R => InternalFormat.R8,
        Abstractions.PixelFormat.RG => InternalFormat.RG8,
        Abstractions.PixelFormat.RGB => InternalFormat.Rgb8,
        Abstractions.PixelFormat.RGBA => InternalFormat.Rgba8,
        Abstractions.PixelFormat.Depth16 => InternalFormat.DepthComponent16,
        Abstractions.PixelFormat.Depth24 => InternalFormat.DepthComponent24,
        Abstractions.PixelFormat.Depth32F => InternalFormat.DepthComponent32f,
        Abstractions.PixelFormat.Depth24Stencil8 => InternalFormat.Depth24Stencil8,
        Abstractions.PixelFormat.RGB16F => InternalFormat.Rgb16f,
        Abstractions.PixelFormat.RGB32F => InternalFormat.Rgb32f,
        Abstractions.PixelFormat.RGBA16F => InternalFormat.Rgba16f,
        Abstractions.PixelFormat.RGBA32F => InternalFormat.Rgba32f,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static PixelType ToGLPixelType(Abstractions.PixelFormat format) => format switch
    {
        Abstractions.PixelFormat.Depth32F or Abstractions.PixelFormat.RGB16F or
        Abstractions.PixelFormat.RGB32F or Abstractions.PixelFormat.RGBA16F or
        Abstractions.PixelFormat.RGBA32F => PixelType.Float,
        Abstractions.PixelFormat.Depth24Stencil8 => PixelType.UnsignedInt248,
        _ => PixelType.UnsignedByte
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

    private static global::Silk.NET.OpenGL.FramebufferTarget ToGL(Abstractions.FramebufferTarget target) => target switch
    {
        Abstractions.FramebufferTarget.Framebuffer => global::Silk.NET.OpenGL.FramebufferTarget.Framebuffer,
        Abstractions.FramebufferTarget.DrawFramebuffer => global::Silk.NET.OpenGL.FramebufferTarget.DrawFramebuffer,
        Abstractions.FramebufferTarget.ReadFramebuffer => global::Silk.NET.OpenGL.FramebufferTarget.ReadFramebuffer,
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private static global::Silk.NET.OpenGL.FramebufferAttachment ToGL(Abstractions.FramebufferAttachment attachment) => attachment switch
    {
        Abstractions.FramebufferAttachment.ColorAttachment0 => global::Silk.NET.OpenGL.FramebufferAttachment.ColorAttachment0,
        Abstractions.FramebufferAttachment.ColorAttachment1 => global::Silk.NET.OpenGL.FramebufferAttachment.ColorAttachment1,
        Abstractions.FramebufferAttachment.ColorAttachment2 => global::Silk.NET.OpenGL.FramebufferAttachment.ColorAttachment2,
        Abstractions.FramebufferAttachment.ColorAttachment3 => global::Silk.NET.OpenGL.FramebufferAttachment.ColorAttachment3,
        Abstractions.FramebufferAttachment.DepthAttachment => global::Silk.NET.OpenGL.FramebufferAttachment.DepthAttachment,
        Abstractions.FramebufferAttachment.StencilAttachment => global::Silk.NET.OpenGL.FramebufferAttachment.StencilAttachment,
        Abstractions.FramebufferAttachment.DepthStencilAttachment => global::Silk.NET.OpenGL.FramebufferAttachment.DepthStencilAttachment,
        _ => throw new ArgumentOutOfRangeException(nameof(attachment))
    };

    private static InternalFormat ToGL(Abstractions.RenderbufferFormat format) => format switch
    {
        Abstractions.RenderbufferFormat.DepthComponent16 => InternalFormat.DepthComponent16,
        Abstractions.RenderbufferFormat.DepthComponent24 => InternalFormat.DepthComponent24,
        Abstractions.RenderbufferFormat.DepthComponent32F => InternalFormat.DepthComponent32f,
        Abstractions.RenderbufferFormat.Depth24Stencil8 => InternalFormat.Depth24Stencil8,
        Abstractions.RenderbufferFormat.StencilIndex8 => InternalFormat.StencilIndex8,
        Abstractions.RenderbufferFormat.RGBA8 => InternalFormat.Rgba8,
        Abstractions.RenderbufferFormat.RGBA16F => InternalFormat.Rgba16f,
        Abstractions.RenderbufferFormat.RGBA32F => InternalFormat.Rgba32f,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static global::Silk.NET.OpenGL.DrawBufferMode ToGL(Abstractions.DrawBufferMode mode) => mode switch
    {
        Abstractions.DrawBufferMode.None => global::Silk.NET.OpenGL.DrawBufferMode.None,
        Abstractions.DrawBufferMode.Front => global::Silk.NET.OpenGL.DrawBufferMode.Front,
        Abstractions.DrawBufferMode.Back => global::Silk.NET.OpenGL.DrawBufferMode.Back,
        Abstractions.DrawBufferMode.ColorAttachment0 => global::Silk.NET.OpenGL.DrawBufferMode.ColorAttachment0,
        Abstractions.DrawBufferMode.ColorAttachment1 => global::Silk.NET.OpenGL.DrawBufferMode.ColorAttachment1,
        Abstractions.DrawBufferMode.ColorAttachment2 => global::Silk.NET.OpenGL.DrawBufferMode.ColorAttachment2,
        Abstractions.DrawBufferMode.ColorAttachment3 => global::Silk.NET.OpenGL.DrawBufferMode.ColorAttachment3,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static ReadBufferMode ToGLReadBuffer(Abstractions.DrawBufferMode mode) => mode switch
    {
        Abstractions.DrawBufferMode.None => ReadBufferMode.None,
        Abstractions.DrawBufferMode.Front => ReadBufferMode.Front,
        Abstractions.DrawBufferMode.Back => ReadBufferMode.Back,
        Abstractions.DrawBufferMode.ColorAttachment0 => ReadBufferMode.ColorAttachment0,
        Abstractions.DrawBufferMode.ColorAttachment1 => ReadBufferMode.ColorAttachment1,
        Abstractions.DrawBufferMode.ColorAttachment2 => ReadBufferMode.ColorAttachment2,
        Abstractions.DrawBufferMode.ColorAttachment3 => ReadBufferMode.ColorAttachment3,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static Abstractions.FramebufferStatus FromGL(GLEnum status) => status switch
    {
        GLEnum.FramebufferComplete => Abstractions.FramebufferStatus.Complete,
        GLEnum.FramebufferIncompleteAttachment => Abstractions.FramebufferStatus.IncompleteAttachment,
        GLEnum.FramebufferIncompleteMissingAttachment => Abstractions.FramebufferStatus.IncompleteMissingAttachment,
        GLEnum.FramebufferIncompleteDrawBuffer => Abstractions.FramebufferStatus.IncompleteDrawBuffer,
        GLEnum.FramebufferIncompleteReadBuffer => Abstractions.FramebufferStatus.IncompleteReadBuffer,
        GLEnum.FramebufferUnsupported => Abstractions.FramebufferStatus.Unsupported,
        GLEnum.FramebufferIncompleteMultisample => Abstractions.FramebufferStatus.IncompleteMultisample,
        GLEnum.FramebufferIncompleteLayerTargets => Abstractions.FramebufferStatus.IncompleteLayerTargets,
        _ => Abstractions.FramebufferStatus.Unknown
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
