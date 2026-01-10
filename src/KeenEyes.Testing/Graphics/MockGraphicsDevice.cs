using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Testing.Graphics;

/// <summary>
/// A mock implementation of <see cref="IGraphicsDevice"/> for testing GPU operations
/// without a real graphics context.
/// </summary>
/// <remarks>
/// <para>
/// MockGraphicsDevice tracks all GPU operations and state changes, enabling verification
/// of rendering code without actual GPU calls. All operations are recorded for later
/// assertion in tests.
/// </para>
/// <para>
/// Use the tracking collections (<see cref="DrawCalls"/>, <see cref="Buffers"/>, etc.)
/// and counters to verify that your rendering code is making the expected calls.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var device = new MockGraphicsDevice();
///
/// // Set up a shader
/// var program = device.CreateProgram();
/// device.UseProgram(program);
///
/// // Draw something
/// device.DrawElements(PrimitiveType.Triangles, 36, IndexType.UnsignedInt);
///
/// // Verify
/// device.DrawCalls.Should().HaveCount(1);
/// device.BoundProgram.Should().Be(program);
/// </code>
/// </example>
public sealed class MockGraphicsDevice : IGraphicsDevice
{
    private uint nextHandle = 1;
    private bool disposed;

    #region State Tracking

    /// <summary>
    /// Gets the list of all draw calls made to this device.
    /// </summary>
    public List<DrawCall> DrawCalls { get; } = [];

    /// <summary>
    /// Gets the dictionary of created buffers by handle.
    /// </summary>
    public Dictionary<uint, MockBuffer> Buffers { get; } = [];

    /// <summary>
    /// Gets the dictionary of created textures by handle.
    /// </summary>
    public Dictionary<uint, MockTexture> Textures { get; } = [];

    /// <summary>
    /// Gets the dictionary of created shaders by handle.
    /// </summary>
    public Dictionary<uint, MockShader> Shaders { get; } = [];

    /// <summary>
    /// Gets the dictionary of created programs by handle.
    /// </summary>
    public Dictionary<uint, MockProgram> Programs { get; } = [];

    /// <summary>
    /// Gets the dictionary of created VAOs by handle.
    /// </summary>
    public Dictionary<uint, MockVao> VAOs { get; } = [];

    /// <summary>
    /// Gets the current render state.
    /// </summary>
    public MockRenderState RenderState { get; } = new();

    /// <summary>
    /// Gets the currently bound VAO, or null if none.
    /// </summary>
    public uint? BoundVAO { get; private set; }

    /// <summary>
    /// Gets the currently bound shader program, or null if none.
    /// </summary>
    public uint? BoundProgram { get; private set; }

    /// <summary>
    /// Gets the dictionary of bound buffers by target.
    /// </summary>
    public Dictionary<BufferTarget, uint> BoundBuffers { get; } = [];

    /// <summary>
    /// Gets the dictionary of bound textures by unit.
    /// </summary>
    public Dictionary<TextureUnit, uint> BoundTextures { get; } = [];

    /// <summary>
    /// Gets the current active texture unit.
    /// </summary>
    public TextureUnit ActiveTextureUnit { get; private set; } = TextureUnit.Texture0;

    #endregion

    #region Counters

    /// <summary>
    /// Gets the number of buffers created.
    /// </summary>
    public int CreateBufferCount => Buffers.Count;

    /// <summary>
    /// Gets the number of textures created.
    /// </summary>
    public int CreateTextureCount => Textures.Count;

    /// <summary>
    /// Gets the number of shaders created.
    /// </summary>
    public int CreateShaderCount => Shaders.Count;

    /// <summary>
    /// Gets the number of programs created.
    /// </summary>
    public int CreateProgramCount => Programs.Count;

    /// <summary>
    /// Gets the number of draw calls made.
    /// </summary>
    public int DrawCount => DrawCalls.Count;

    #endregion

    #region Configuration

    /// <summary>
    /// Gets or sets whether texture loads should fail.
    /// </summary>
    public bool ShouldFailTextureLoad { get; set; }

    /// <summary>
    /// Gets or sets whether shader compilation should fail.
    /// </summary>
    public bool ShouldFailShaderCompile { get; set; }

    /// <summary>
    /// Gets or sets whether program linking should fail.
    /// </summary>
    public bool ShouldFailProgramLink { get; set; }

    /// <summary>
    /// Gets or sets the simulated error code returned by <see cref="GetError"/>.
    /// </summary>
    public int SimulatedErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the simulated framebuffer data returned by <see cref="ReadFramebuffer"/>.
    /// </summary>
    /// <remarks>
    /// Set this property to provide pixel data that <see cref="ReadFramebuffer"/> will copy
    /// to the output span. The data should be in RGBA format with 4 bytes per pixel.
    /// </remarks>
    public byte[]? SimulatedFramebufferData { get; set; }

    /// <summary>
    /// Gets or sets the simulated framebuffer width.
    /// </summary>
    public int SimulatedFramebufferWidth { get; set; }

    /// <summary>
    /// Gets or sets the simulated framebuffer height.
    /// </summary>
    public int SimulatedFramebufferHeight { get; set; }

    #endregion

    #region Test Control

    /// <summary>
    /// Resets all tracking state and counters.
    /// </summary>
    public void Reset()
    {
        DrawCalls.Clear();
        Buffers.Clear();
        Textures.Clear();
        Shaders.Clear();
        Programs.Clear();
        VAOs.Clear();
        BoundBuffers.Clear();
        BoundTextures.Clear();
        BoundVAO = null;
        BoundProgram = null;
        ActiveTextureUnit = TextureUnit.Texture0;
        RenderState.Reset();
        nextHandle = 1;
        ShouldFailTextureLoad = false;
        ShouldFailShaderCompile = false;
        ShouldFailProgramLink = false;
        SimulatedErrorCode = 0;
        SimulatedFramebufferData = null;
        SimulatedFramebufferWidth = 0;
        SimulatedFramebufferHeight = 0;
    }

    /// <summary>
    /// Clears only the draw calls, keeping other state.
    /// </summary>
    public void ClearDrawCalls()
    {
        DrawCalls.Clear();
    }

    #endregion

    #region Buffer Operations

    /// <inheritdoc />
    public uint GenVertexArray()
    {
        var handle = nextHandle++;
        VAOs[handle] = new MockVao(handle);
        return handle;
    }

    /// <inheritdoc />
    public uint GenBuffer()
    {
        var handle = nextHandle++;
        Buffers[handle] = new MockBuffer(handle);
        return handle;
    }

    /// <inheritdoc />
    public void BindVertexArray(uint vao)
    {
        BoundVAO = vao == 0 ? null : vao;
    }

    /// <inheritdoc />
    public void BindBuffer(BufferTarget target, uint buffer)
    {
        if (buffer == 0)
        {
            BoundBuffers.Remove(target);
        }
        else
        {
            BoundBuffers[target] = buffer;
        }
    }

    /// <inheritdoc />
    public void BufferData(BufferTarget target, ReadOnlySpan<byte> data, BufferUsage usage)
    {
        if (BoundBuffers.TryGetValue(target, out var handle) && Buffers.TryGetValue(handle, out var buffer))
        {
            buffer.Data = data.ToArray();
            buffer.Usage = usage;
            buffer.Target = target;
        }
    }

    /// <inheritdoc />
    public void DeleteVertexArray(uint vao)
    {
        VAOs.Remove(vao);
        if (BoundVAO == vao)
        {
            BoundVAO = null;
        }
    }

    /// <inheritdoc />
    public void DeleteBuffer(uint buffer)
    {
        Buffers.Remove(buffer);
        foreach (var target in BoundBuffers.Where(kv => kv.Value == buffer).Select(kv => kv.Key).ToList())
        {
            BoundBuffers.Remove(target);
        }
    }

    /// <inheritdoc />
    public void EnableVertexAttribArray(uint index)
    {
        if (BoundVAO.HasValue && VAOs.TryGetValue(BoundVAO.Value, out var vao))
        {
            vao.EnabledAttributes.Add(index);
        }
    }

    /// <inheritdoc />
    public void VertexAttribPointer(uint index, int size, VertexAttribType type, bool normalized, uint stride, nuint offset)
    {
        if (BoundVAO.HasValue && VAOs.TryGetValue(BoundVAO.Value, out var vao))
        {
            vao.Attributes[index] = new VertexAttribute(index, size, type, normalized, stride, offset);
        }
    }

    /// <inheritdoc />
    public void VertexAttribDivisor(uint index, uint divisor)
    {
        if (BoundVAO.HasValue && VAOs.TryGetValue(BoundVAO.Value, out var vao))
        {
            vao.AttributeDivisors[index] = divisor;
        }
    }

    /// <inheritdoc />
    public void BufferSubData(BufferTarget target, nint offset, ReadOnlySpan<byte> data)
    {
        if (BoundBuffers.TryGetValue(target, out var handle) &&
            Buffers.TryGetValue(handle, out var buffer) &&
            buffer.Data is not null &&
            offset >= 0 &&
            offset + data.Length <= buffer.Data.Length)
        {
            data.CopyTo(buffer.Data.AsSpan((int)offset));
        }
    }

    #endregion

    #region Texture Operations

    /// <inheritdoc />
    public uint GenTexture()
    {
        var handle = nextHandle++;
        Textures[handle] = new MockTexture(handle);
        return handle;
    }

    /// <inheritdoc />
    public void BindTexture(TextureTarget target, uint texture)
    {
        if (texture == 0)
        {
            BoundTextures.Remove(ActiveTextureUnit);
        }
        else
        {
            BoundTextures[ActiveTextureUnit] = texture;
            if (Textures.TryGetValue(texture, out var tex))
            {
                tex.Target = target;
            }
        }
    }

    /// <inheritdoc />
    public void TexImage2D(TextureTarget target, int level, int width, int height, PixelFormat format, ReadOnlySpan<byte> data)
    {
        if (ShouldFailTextureLoad)
        {
            SimulatedErrorCode = 1;
            return;
        }

        if (BoundTextures.TryGetValue(ActiveTextureUnit, out var handle) && Textures.TryGetValue(handle, out var texture))
        {
            texture.Width = width;
            texture.Height = height;
            texture.Format = format;
            texture.Data = data.ToArray();
            texture.MipLevels[level] = true;
        }
    }

    /// <inheritdoc />
    public void TexParameter(TextureTarget target, TextureParam param, int value)
    {
        if (BoundTextures.TryGetValue(ActiveTextureUnit, out var handle) && Textures.TryGetValue(handle, out var texture))
        {
            texture.Parameters[param] = value;
        }
    }

    /// <inheritdoc />
    public void TexSubImage2D(TextureTarget target, int level, int xOffset, int yOffset, int width, int height, PixelFormat format, ReadOnlySpan<byte> data)
    {
        // Track that a subimage update occurred
        if (BoundTextures.TryGetValue(ActiveTextureUnit, out var handle) && Textures.TryGetValue(handle, out var texture))
        {
            texture.SubImageUpdateCount++;
        }
    }

    /// <inheritdoc />
    public void GenerateMipmap(TextureTarget target)
    {
        if (BoundTextures.TryGetValue(ActiveTextureUnit, out var handle) && Textures.TryGetValue(handle, out var texture))
        {
            texture.HasGeneratedMipmaps = true;
        }
    }

    /// <inheritdoc />
    public void CompressedTexImage2D(
        TextureTarget target,
        int level,
        int width,
        int height,
        CompressedTextureFormat format,
        ReadOnlySpan<byte> data)
    {
        if (ShouldFailTextureLoad)
        {
            SimulatedErrorCode = 1;
            return;
        }

        if (BoundTextures.TryGetValue(ActiveTextureUnit, out var handle) && Textures.TryGetValue(handle, out var texture))
        {
            texture.Width = width;
            texture.Height = height;
            texture.CompressedFormat = format;
            texture.Data = data.ToArray();
            texture.MipLevels[level] = true;
        }
    }

    /// <inheritdoc />
    public void DeleteTexture(uint texture)
    {
        Textures.Remove(texture);
        foreach (var unit in BoundTextures.Where(kv => kv.Value == texture).Select(kv => kv.Key).ToList())
        {
            BoundTextures.Remove(unit);
        }
    }

    /// <inheritdoc />
    public void ActiveTexture(TextureUnit unit)
    {
        ActiveTextureUnit = unit;
    }

    /// <inheritdoc />
    public void PixelStore(PixelStoreParameter param, int value)
    {
        RenderState.PixelStoreParameters[param] = value;
    }

    #endregion

    #region Shader Operations

    /// <inheritdoc />
    public uint CreateProgram()
    {
        var handle = nextHandle++;
        Programs[handle] = new MockProgram(handle);
        return handle;
    }

    /// <inheritdoc />
    public uint CreateShader(ShaderType type)
    {
        var handle = nextHandle++;
        Shaders[handle] = new MockShader(handle, type);
        return handle;
    }

    /// <inheritdoc />
    public void ShaderSource(uint shader, string source)
    {
        if (Shaders.TryGetValue(shader, out var s))
        {
            s.Source = source;
        }
    }

    /// <inheritdoc />
    public void CompileShader(uint shader)
    {
        if (Shaders.TryGetValue(shader, out var s))
        {
            s.IsCompiled = !ShouldFailShaderCompile;
        }
    }

    /// <inheritdoc />
    public bool GetShaderCompileStatus(uint shader)
    {
        return Shaders.TryGetValue(shader, out var s) && s.IsCompiled;
    }

    /// <inheritdoc />
    public string GetShaderInfoLog(uint shader)
    {
        if (ShouldFailShaderCompile)
        {
            return "Mock shader compilation failed (ShouldFailShaderCompile = true)";
        }

        return string.Empty;
    }

    /// <inheritdoc />
    public void AttachShader(uint program, uint shader)
    {
        if (Programs.TryGetValue(program, out var p))
        {
            p.AttachedShaders.Add(shader);
        }
    }

    /// <inheritdoc />
    public void DetachShader(uint program, uint shader)
    {
        if (Programs.TryGetValue(program, out var p))
        {
            p.AttachedShaders.Remove(shader);
        }
    }

    /// <inheritdoc />
    public void LinkProgram(uint program)
    {
        if (Programs.TryGetValue(program, out var p))
        {
            p.IsLinked = !ShouldFailProgramLink;
        }
    }

    /// <inheritdoc />
    public bool GetProgramLinkStatus(uint program)
    {
        return Programs.TryGetValue(program, out var p) && p.IsLinked;
    }

    /// <inheritdoc />
    public string GetProgramInfoLog(uint program)
    {
        if (ShouldFailProgramLink)
        {
            return "Mock program linking failed (ShouldFailProgramLink = true)";
        }

        return string.Empty;
    }

    /// <inheritdoc />
    public void DeleteShader(uint shader)
    {
        Shaders.Remove(shader);
    }

    /// <inheritdoc />
    public void DeleteProgram(uint program)
    {
        Programs.Remove(program);
        if (BoundProgram == program)
        {
            BoundProgram = null;
        }
    }

    /// <inheritdoc />
    public void UseProgram(uint program)
    {
        BoundProgram = program == 0 ? null : program;
    }

    /// <inheritdoc />
    public int GetUniformLocation(uint program, string name)
    {
        if (Programs.TryGetValue(program, out var p))
        {
            if (!p.UniformLocations.TryGetValue(name, out var location))
            {
                location = p.UniformLocations.Count;
                p.UniformLocations[name] = location;
            }

            return location;
        }

        return -1;
    }

    /// <inheritdoc />
    public void Uniform1(int location, float value)
    {
        RecordUniform(location, value);
    }

    /// <inheritdoc />
    public void Uniform1(int location, int value)
    {
        RecordUniform(location, value);
    }

    /// <inheritdoc />
    public void Uniform2(int location, float x, float y)
    {
        RecordUniform(location, new Vector2(x, y));
    }

    /// <inheritdoc />
    public void Uniform3(int location, float x, float y, float z)
    {
        RecordUniform(location, new Vector3(x, y, z));
    }

    /// <inheritdoc />
    public void Uniform4(int location, float x, float y, float z, float w)
    {
        RecordUniform(location, new Vector4(x, y, z, w));
    }

    /// <inheritdoc />
    public void UniformMatrix4(int location, in Matrix4x4 matrix)
    {
        RecordUniform(location, matrix);
    }

    private void RecordUniform(int location, object value)
    {
        if (BoundProgram.HasValue && Programs.TryGetValue(BoundProgram.Value, out var p))
        {
            p.UniformValues[location] = value;
        }
    }

    #endregion

    #region Rendering Operations

    /// <inheritdoc />
    public void ClearColor(float r, float g, float b, float a)
    {
        RenderState.ClearColor = new Vector4(r, g, b, a);
    }

    /// <inheritdoc />
    public void Clear(ClearMask mask)
    {
        RenderState.LastClearMask = mask;
        RenderState.ClearCount++;
    }

    /// <inheritdoc />
    public void Enable(RenderCapability cap)
    {
        RenderState.EnabledCapabilities.Add(cap);
    }

    /// <inheritdoc />
    public void Disable(RenderCapability cap)
    {
        RenderState.EnabledCapabilities.Remove(cap);
    }

    /// <inheritdoc />
    public void CullFace(CullFaceMode mode)
    {
        RenderState.CullFaceMode = mode;
    }

    /// <inheritdoc />
    public void Viewport(int x, int y, uint width, uint height)
    {
        RenderState.Viewport = (x, y, (int)width, (int)height);
    }

    /// <inheritdoc />
    public void Scissor(int x, int y, uint width, uint height)
    {
        RenderState.ScissorRect = (x, y, (int)width, (int)height);
    }

    /// <inheritdoc />
    public void BlendFunc(BlendFactor srcFactor, BlendFactor dstFactor)
    {
        RenderState.BlendSrcFactor = srcFactor;
        RenderState.BlendDstFactor = dstFactor;
    }

    /// <inheritdoc />
    public void DepthFunc(DepthFunction func)
    {
        RenderState.DepthFunction = func;
    }

    /// <inheritdoc />
    public void DrawElements(PrimitiveType mode, uint count, IndexType type)
    {
        DrawCalls.Add(new DrawCall(
            mode,
            (int)count,
            IsIndexed: true,
            BoundProgram,
            BoundVAO,
            BoundTextures.Values.ToList(),
            InstanceCount: 1));
    }

    /// <inheritdoc />
    public void DrawArrays(PrimitiveType mode, int first, uint count)
    {
        DrawCalls.Add(new DrawCall(
            mode,
            (int)count,
            IsIndexed: false,
            BoundProgram,
            BoundVAO,
            BoundTextures.Values.ToList(),
            InstanceCount: 1));
    }

    /// <inheritdoc />
    public void DrawElementsInstanced(PrimitiveType mode, uint count, IndexType type, uint instanceCount)
    {
        DrawCalls.Add(new DrawCall(
            mode,
            (int)count,
            IsIndexed: true,
            BoundProgram,
            BoundVAO,
            BoundTextures.Values.ToList(),
            instanceCount));
    }

    /// <inheritdoc />
    public void DrawArraysInstanced(PrimitiveType mode, int first, uint count, uint instanceCount)
    {
        DrawCalls.Add(new DrawCall(
            mode,
            (int)count,
            IsIndexed: false,
            BoundProgram,
            BoundVAO,
            BoundTextures.Values.ToList(),
            instanceCount));
    }

    /// <inheritdoc />
    public void LineWidth(float width)
    {
        RenderState.LineWidth = width;
    }

    /// <inheritdoc />
    public void PointSize(float size)
    {
        RenderState.PointSize = size;
    }

    #endregion

    #region Error Handling

    /// <inheritdoc />
    public int GetError()
    {
        var error = SimulatedErrorCode;
        SimulatedErrorCode = 0;
        return error;
    }

    #endregion

    #region Debug

    /// <inheritdoc />
    public void GetTexImage(TextureTarget target, int level, PixelFormat format, Span<byte> data)
    {
        if (BoundTextures.TryGetValue(ActiveTextureUnit, out var handle) &&
            Textures.TryGetValue(handle, out var texture) &&
            texture.Data is not null)
        {
            var copyLength = Math.Min(data.Length, texture.Data.Length);
            texture.Data.AsSpan(0, copyLength).CopyTo(data);
        }
    }

    /// <inheritdoc />
    public void ReadFramebuffer(int x, int y, int width, int height, PixelFormat format, Span<byte> data)
    {
        if (SimulatedFramebufferData is null)
        {
            return;
        }

        // Calculate bytes per pixel based on format
        var bytesPerPixel = format switch
        {
            PixelFormat.R => 1,
            PixelFormat.RG => 2,
            PixelFormat.RGB => 3,
            PixelFormat.RGBA => 4,
            _ => 4
        };

        // If the request is for the full framebuffer and dimensions match, just copy
        if (x == 0 && y == 0 &&
            width == SimulatedFramebufferWidth &&
            height == SimulatedFramebufferHeight)
        {
            var copyLength = Math.Min(data.Length, SimulatedFramebufferData.Length);
            SimulatedFramebufferData.AsSpan(0, copyLength).CopyTo(data);
            return;
        }

        // Otherwise, copy the requested region
        var srcRowStride = SimulatedFramebufferWidth * bytesPerPixel;
        var dstRowStride = width * bytesPerPixel;

        for (var row = 0; row < height; row++)
        {
            var srcY = y + row;
            if (srcY < 0 || srcY >= SimulatedFramebufferHeight)
            {
                continue;
            }

            var srcOffset = (srcY * srcRowStride) + (x * bytesPerPixel);
            var dstOffset = row * dstRowStride;
            var rowBytes = Math.Min(dstRowStride, srcRowStride - (x * bytesPerPixel));

            if (srcOffset >= 0 && srcOffset + rowBytes <= SimulatedFramebufferData.Length &&
                dstOffset + rowBytes <= data.Length)
            {
                SimulatedFramebufferData.AsSpan(srcOffset, rowBytes).CopyTo(data.Slice(dstOffset, rowBytes));
            }
        }
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Reset();
        }
    }
}

#region Supporting Types

/// <summary>
/// Represents a recorded draw call.
/// </summary>
/// <param name="PrimitiveType">The primitive type drawn.</param>
/// <param name="VertexCount">The number of vertices/indices.</param>
/// <param name="IsIndexed">Whether this was an indexed draw call.</param>
/// <param name="Program">The shader program used, if any.</param>
/// <param name="VAO">The VAO used, if any.</param>
/// <param name="Textures">The textures bound during the draw.</param>
/// <param name="InstanceCount">The number of instances drawn (1 for non-instanced).</param>
public sealed record DrawCall(
    PrimitiveType PrimitiveType,
    int VertexCount,
    bool IsIndexed,
    uint? Program,
    uint? VAO,
    List<uint> Textures,
    uint InstanceCount = 1);

/// <summary>
/// Tracks buffer state.
/// </summary>
public sealed class MockBuffer(uint handle)
{
    /// <summary>
    /// Gets the buffer handle.
    /// </summary>
    public uint Handle { get; } = handle;

    /// <summary>
    /// Gets or sets the buffer data.
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// Gets or sets the buffer usage hint.
    /// </summary>
    public BufferUsage Usage { get; set; }

    /// <summary>
    /// Gets or sets the buffer target.
    /// </summary>
    public BufferTarget Target { get; set; }
}

/// <summary>
/// Tracks texture state.
/// </summary>
public sealed class MockTexture(uint handle)
{
    /// <summary>
    /// Gets the texture handle.
    /// </summary>
    public uint Handle { get; } = handle;

    /// <summary>
    /// Gets or sets the texture width.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the texture height.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the pixel format.
    /// </summary>
    public PixelFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the compressed texture format, if this is a compressed texture.
    /// </summary>
    public CompressedTextureFormat? CompressedFormat { get; set; }

    /// <summary>
    /// Gets or sets the texture data.
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// Gets or sets the texture target.
    /// </summary>
    public TextureTarget Target { get; set; }

    /// <summary>
    /// Gets the texture parameters.
    /// </summary>
    public Dictionary<TextureParam, int> Parameters { get; } = [];

    /// <summary>
    /// Gets the mip levels that have been set.
    /// </summary>
    public Dictionary<int, bool> MipLevels { get; } = [];

    /// <summary>
    /// Gets or sets whether mipmaps have been generated.
    /// </summary>
    public bool HasGeneratedMipmaps { get; set; }

    /// <summary>
    /// Gets or sets the count of sub-image updates.
    /// </summary>
    public int SubImageUpdateCount { get; set; }
}

/// <summary>
/// Tracks shader state.
/// </summary>
public sealed class MockShader(uint handle, ShaderType type)
{
    /// <summary>
    /// Gets the shader handle.
    /// </summary>
    public uint Handle { get; } = handle;

    /// <summary>
    /// Gets the shader type.
    /// </summary>
    public ShaderType Type { get; } = type;

    /// <summary>
    /// Gets or sets the shader source code.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets whether the shader was compiled successfully.
    /// </summary>
    public bool IsCompiled { get; set; }
}

/// <summary>
/// Tracks shader program state.
/// </summary>
public sealed class MockProgram(uint handle)
{
    /// <summary>
    /// Gets the program handle.
    /// </summary>
    public uint Handle { get; } = handle;

    /// <summary>
    /// Gets the attached shader handles.
    /// </summary>
    public HashSet<uint> AttachedShaders { get; } = [];

    /// <summary>
    /// Gets or sets whether the program was linked successfully.
    /// </summary>
    public bool IsLinked { get; set; }

    /// <summary>
    /// Gets the uniform locations by name.
    /// </summary>
    public Dictionary<string, int> UniformLocations { get; } = [];

    /// <summary>
    /// Gets the uniform values by location.
    /// </summary>
    public Dictionary<int, object> UniformValues { get; } = [];
}

/// <summary>
/// Tracks VAO state.
/// </summary>
public sealed class MockVao(uint handle)
{
    /// <summary>
    /// Gets the VAO handle.
    /// </summary>
    public uint Handle { get; } = handle;

    /// <summary>
    /// Gets the enabled vertex attributes.
    /// </summary>
    public HashSet<uint> EnabledAttributes { get; } = [];

    /// <summary>
    /// Gets the vertex attribute configurations.
    /// </summary>
    public Dictionary<uint, VertexAttribute> Attributes { get; } = [];

    /// <summary>
    /// Gets the vertex attribute divisors for instanced rendering.
    /// </summary>
    /// <remarks>
    /// A divisor of 0 means per-vertex data (default).
    /// A divisor of 1 means per-instance data.
    /// A divisor of N means the attribute advances every N instances.
    /// </remarks>
    public Dictionary<uint, uint> AttributeDivisors { get; } = [];
}

/// <summary>
/// Represents a vertex attribute configuration.
/// </summary>
/// <param name="Index">The attribute index.</param>
/// <param name="Size">The number of components.</param>
/// <param name="Type">The data type.</param>
/// <param name="Normalized">Whether to normalize.</param>
/// <param name="Stride">The stride in bytes.</param>
/// <param name="Offset">The offset in bytes.</param>
public sealed record VertexAttribute(
    uint Index,
    int Size,
    VertexAttribType Type,
    bool Normalized,
    uint Stride,
    nuint Offset);

/// <summary>
/// Tracks render state.
/// </summary>
public sealed class MockRenderState
{
    /// <summary>
    /// Gets or sets the clear color.
    /// </summary>
    public Vector4 ClearColor { get; set; }

    /// <summary>
    /// Gets or sets the last clear mask used.
    /// </summary>
    public ClearMask LastClearMask { get; set; }

    /// <summary>
    /// Gets the number of clear operations.
    /// </summary>
    public int ClearCount { get; set; }

    /// <summary>
    /// Gets the enabled render capabilities.
    /// </summary>
    public HashSet<RenderCapability> EnabledCapabilities { get; } = [];

    /// <summary>
    /// Gets or sets the cull face mode.
    /// </summary>
    public CullFaceMode CullFaceMode { get; set; }

    /// <summary>
    /// Gets or sets the viewport (x, y, width, height).
    /// </summary>
    public (int X, int Y, int Width, int Height) Viewport { get; set; }

    /// <summary>
    /// Gets or sets the scissor rectangle (x, y, width, height).
    /// </summary>
    public (int X, int Y, int Width, int Height) ScissorRect { get; set; }

    /// <summary>
    /// Gets or sets the source blend factor.
    /// </summary>
    public BlendFactor BlendSrcFactor { get; set; }

    /// <summary>
    /// Gets or sets the destination blend factor.
    /// </summary>
    public BlendFactor BlendDstFactor { get; set; }

    /// <summary>
    /// Gets or sets the depth function.
    /// </summary>
    public DepthFunction DepthFunction { get; set; }

    /// <summary>
    /// Gets or sets the line width.
    /// </summary>
    public float LineWidth { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the point size.
    /// </summary>
    public float PointSize { get; set; } = 1f;

    /// <summary>
    /// Gets the pixel store parameters.
    /// </summary>
    public Dictionary<PixelStoreParameter, int> PixelStoreParameters { get; } = [];

    /// <summary>
    /// Resets the render state to defaults.
    /// </summary>
    public void Reset()
    {
        ClearColor = Vector4.Zero;
        LastClearMask = default;
        ClearCount = 0;
        EnabledCapabilities.Clear();
        CullFaceMode = default;
        Viewport = default;
        ScissorRect = default;
        BlendSrcFactor = default;
        BlendDstFactor = default;
        DepthFunction = default;
        LineWidth = 1f;
        PointSize = 1f;
        PixelStoreParameters.Clear();
    }
}

#endregion
