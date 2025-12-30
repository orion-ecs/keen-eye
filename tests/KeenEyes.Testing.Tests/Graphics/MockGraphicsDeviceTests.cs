using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockGraphicsDeviceTests
{
    #region Construction and Reset

    [Fact]
    public void Constructor_CreatesDeviceWithEmptyState()
    {
        using var device = new MockGraphicsDevice();

        Assert.Empty(device.DrawCalls);
        Assert.Empty(device.Buffers);
        Assert.Empty(device.Textures);
        Assert.Empty(device.Shaders);
        Assert.Empty(device.Programs);
        Assert.Empty(device.VAOs);
        Assert.Null(device.BoundVAO);
        Assert.Null(device.BoundProgram);
        Assert.Empty(device.BoundBuffers);
        Assert.Empty(device.BoundTextures);
        Assert.Equal(TextureUnit.Texture0, device.ActiveTextureUnit);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var device = new MockGraphicsDevice();

        // Create some resources
        device.GenBuffer();
        device.GenTexture();
        device.CreateShader(ShaderType.Vertex);
        device.CreateProgram();
        device.GenVertexArray();

        device.Reset();

        Assert.Empty(device.Buffers);
        Assert.Empty(device.Textures);
        Assert.Empty(device.Shaders);
        Assert.Empty(device.Programs);
        Assert.Empty(device.VAOs);
        Assert.Null(device.BoundVAO);
        Assert.Null(device.BoundProgram);
    }

    [Fact]
    public void ClearDrawCalls_ClearsOnlyDrawCalls()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);
        device.DrawArrays(PrimitiveType.Triangles, 0, 3);

        device.ClearDrawCalls();

        Assert.Empty(device.DrawCalls);
        Assert.NotEmpty(device.VAOs); // Other state preserved
    }

    #endregion

    #region Buffer Operations

    [Fact]
    public void GenBuffer_CreatesBuffer()
    {
        using var device = new MockGraphicsDevice();

        var handle = device.GenBuffer();

        Assert.True(handle > 0);
        Assert.Contains(handle, device.Buffers.Keys);
        Assert.Equal(1, device.CreateBufferCount);
    }

    [Fact]
    public void BindBuffer_BindsBuffer()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();

        device.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        Assert.Equal(buffer, device.BoundBuffers[BufferTarget.ArrayBuffer]);
    }

    [Fact]
    public void BindBuffer_WithZero_UnbindsBuffer()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();
        device.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        device.BindBuffer(BufferTarget.ArrayBuffer, 0);

        Assert.False(device.BoundBuffers.ContainsKey(BufferTarget.ArrayBuffer));
    }

    [Fact]
    public void BufferData_StoresDataInBuffer()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();
        device.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        var data = new byte[] { 1, 2, 3, 4 };

        device.BufferData(BufferTarget.ArrayBuffer, data, BufferUsage.StaticDraw);

        var mockBuffer = device.Buffers[buffer];
        Assert.Equal(data, mockBuffer.Data);
        Assert.Equal(BufferUsage.StaticDraw, mockBuffer.Usage);
        Assert.Equal(BufferTarget.ArrayBuffer, mockBuffer.Target);
    }

    [Fact]
    public void DeleteBuffer_RemovesBuffer()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();
        device.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        device.DeleteBuffer(buffer);

        Assert.DoesNotContain(buffer, device.Buffers.Keys);
        Assert.False(device.BoundBuffers.ContainsKey(BufferTarget.ArrayBuffer));
    }

    #endregion

    #region VAO Operations

    [Fact]
    public void GenVertexArray_CreatesVAO()
    {
        using var device = new MockGraphicsDevice();

        var handle = device.GenVertexArray();

        Assert.True(handle > 0);
        Assert.Contains(handle, device.VAOs.Keys);
    }

    [Fact]
    public void BindVertexArray_BindsVAO()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();

        device.BindVertexArray(vao);

        Assert.Equal(vao, device.BoundVAO);
    }

    [Fact]
    public void BindVertexArray_WithZero_UnbindsVAO()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.BindVertexArray(0);

        Assert.Null(device.BoundVAO);
    }

    [Fact]
    public void DeleteVertexArray_RemovesVAO()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.DeleteVertexArray(vao);

        Assert.DoesNotContain(vao, device.VAOs.Keys);
        Assert.Null(device.BoundVAO);
    }

    [Fact]
    public void EnableVertexAttribArray_EnablesAttribute()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.EnableVertexAttribArray(0);

        Assert.Contains(0u, device.VAOs[vao].EnabledAttributes);
    }

    [Fact]
    public void VertexAttribPointer_ConfiguresAttribute()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 12, 0);

        var attr = device.VAOs[vao].Attributes[0];
        Assert.Equal(0u, attr.Index);
        Assert.Equal(3, attr.Size);
        Assert.Equal(VertexAttribType.Float, attr.Type);
        Assert.False(attr.Normalized);
        Assert.Equal(12u, attr.Stride);
        Assert.Equal(0u, (uint)attr.Offset);
    }

    #endregion

    #region Texture Operations

    [Fact]
    public void GenTexture_CreatesTexture()
    {
        using var device = new MockGraphicsDevice();

        var handle = device.GenTexture();

        Assert.True(handle > 0);
        Assert.Contains(handle, device.Textures.Keys);
        Assert.Equal(1, device.CreateTextureCount);
    }

    [Fact]
    public void BindTexture_BindsToActiveUnit()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();

        device.BindTexture(TextureTarget.Texture2D, texture);

        Assert.Equal(texture, device.BoundTextures[TextureUnit.Texture0]);
        Assert.Equal(TextureTarget.Texture2D, device.Textures[texture].Target);
    }

    [Fact]
    public void BindTexture_WithZero_UnbindsTexture()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);

        device.BindTexture(TextureTarget.Texture2D, 0);

        Assert.False(device.BoundTextures.ContainsKey(TextureUnit.Texture0));
    }

    [Fact]
    public void ActiveTexture_ChangesActiveUnit()
    {
        using var device = new MockGraphicsDevice();

        device.ActiveTexture(TextureUnit.Texture1);

        Assert.Equal(TextureUnit.Texture1, device.ActiveTextureUnit);
    }

    [Fact]
    public void TexImage2D_StoresTextureData()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);
        var data = new byte[16];

        device.TexImage2D(TextureTarget.Texture2D, 0, 2, 2, PixelFormat.RGBA, data);

        var tex = device.Textures[texture];
        Assert.Equal(2, tex.Width);
        Assert.Equal(2, tex.Height);
        Assert.Equal(PixelFormat.RGBA, tex.Format);
        Assert.Equal(data, tex.Data);
        Assert.True(tex.MipLevels[0]);
    }

    [Fact]
    public void TexImage2D_WhenShouldFail_SetsError()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);
        device.ShouldFailTextureLoad = true;

        device.TexImage2D(TextureTarget.Texture2D, 0, 2, 2, PixelFormat.RGBA, new byte[16]);

        Assert.Equal(1, device.GetError());
    }

    [Fact]
    public void TexParameter_StoresParameter()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);

        device.TexParameter(TextureTarget.Texture2D, TextureParam.MinFilter, (int)TextureMinFilter.Linear);

        Assert.Equal((int)TextureMinFilter.Linear, device.Textures[texture].Parameters[TextureParam.MinFilter]);
    }

    [Fact]
    public void TexSubImage2D_IncrementsUpdateCount()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);
        device.TexImage2D(TextureTarget.Texture2D, 0, 4, 4, PixelFormat.RGBA, new byte[64]);

        device.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 2, 2, PixelFormat.RGBA, new byte[16]);

        Assert.Equal(1, device.Textures[texture].SubImageUpdateCount);
    }

    [Fact]
    public void GenerateMipmap_SetsMipmapFlag()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);
        device.TexImage2D(TextureTarget.Texture2D, 0, 4, 4, PixelFormat.RGBA, new byte[64]);

        device.GenerateMipmap(TextureTarget.Texture2D);

        Assert.True(device.Textures[texture].HasGeneratedMipmaps);
    }

    [Fact]
    public void DeleteTexture_RemovesTexture()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);

        device.DeleteTexture(texture);

        Assert.DoesNotContain(texture, device.Textures.Keys);
        Assert.False(device.BoundTextures.ContainsKey(TextureUnit.Texture0));
    }

    [Fact]
    public void PixelStore_StoresParameter()
    {
        using var device = new MockGraphicsDevice();

        device.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        Assert.Equal(1, device.RenderState.PixelStoreParameters[PixelStoreParameter.UnpackAlignment]);
    }

    [Fact]
    public void GetTexImage_RetrievesTextureData()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);
        var sourceData = new byte[] { 1, 2, 3, 4 };
        device.TexImage2D(TextureTarget.Texture2D, 0, 2, 1, PixelFormat.RGBA, sourceData);

        var destData = new byte[4];
        device.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.RGBA, destData);

        Assert.Equal(sourceData, destData);
    }

    #endregion

    #region Shader Operations

    [Fact]
    public void CreateShader_CreatesShader()
    {
        using var device = new MockGraphicsDevice();

        var handle = device.CreateShader(ShaderType.Vertex);

        Assert.True(handle > 0);
        Assert.Contains(handle, device.Shaders.Keys);
        Assert.Equal(ShaderType.Vertex, device.Shaders[handle].Type);
        Assert.Equal(1, device.CreateShaderCount);
    }

    [Fact]
    public void ShaderSource_StoresSource()
    {
        using var device = new MockGraphicsDevice();
        var shader = device.CreateShader(ShaderType.Vertex);

        device.ShaderSource(shader, "void main() {}");

        Assert.Equal("void main() {}", device.Shaders[shader].Source);
    }

    [Fact]
    public void CompileShader_MarksAsCompiled()
    {
        using var device = new MockGraphicsDevice();
        var shader = device.CreateShader(ShaderType.Vertex);
        device.ShaderSource(shader, "void main() {}");

        device.CompileShader(shader);

        Assert.True(device.Shaders[shader].IsCompiled);
        Assert.True(device.GetShaderCompileStatus(shader));
    }

    [Fact]
    public void CompileShader_WhenShouldFail_MarksAsNotCompiled()
    {
        using var device = new MockGraphicsDevice();
        device.ShouldFailShaderCompile = true;
        var shader = device.CreateShader(ShaderType.Vertex);

        device.CompileShader(shader);

        Assert.False(device.Shaders[shader].IsCompiled);
        Assert.False(device.GetShaderCompileStatus(shader));
    }

    [Fact]
    public void GetShaderInfoLog_ReturnsEmptyOnSuccess()
    {
        using var device = new MockGraphicsDevice();
        var shader = device.CreateShader(ShaderType.Vertex);
        device.CompileShader(shader);

        var log = device.GetShaderInfoLog(shader);

        Assert.Empty(log);
    }

    [Fact]
    public void GetShaderInfoLog_ReturnsErrorOnFailure()
    {
        using var device = new MockGraphicsDevice();
        device.ShouldFailShaderCompile = true;
        var shader = device.CreateShader(ShaderType.Vertex);

        var log = device.GetShaderInfoLog(shader);

        Assert.Contains("failed", log);
    }

    [Fact]
    public void DeleteShader_RemovesShader()
    {
        using var device = new MockGraphicsDevice();
        var shader = device.CreateShader(ShaderType.Vertex);

        device.DeleteShader(shader);

        Assert.DoesNotContain(shader, device.Shaders.Keys);
    }

    #endregion

    #region Program Operations

    [Fact]
    public void CreateProgram_CreatesProgram()
    {
        using var device = new MockGraphicsDevice();

        var handle = device.CreateProgram();

        Assert.True(handle > 0);
        Assert.Contains(handle, device.Programs.Keys);
        Assert.Equal(1, device.CreateProgramCount);
    }

    [Fact]
    public void AttachShader_AttachesShaderToProgram()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        var shader = device.CreateShader(ShaderType.Vertex);

        device.AttachShader(program, shader);

        Assert.Contains(shader, device.Programs[program].AttachedShaders);
    }

    [Fact]
    public void DetachShader_DetachesShaderFromProgram()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        var shader = device.CreateShader(ShaderType.Vertex);
        device.AttachShader(program, shader);

        device.DetachShader(program, shader);

        Assert.DoesNotContain(shader, device.Programs[program].AttachedShaders);
    }

    [Fact]
    public void LinkProgram_MarksAsLinked()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();

        device.LinkProgram(program);

        Assert.True(device.Programs[program].IsLinked);
        Assert.True(device.GetProgramLinkStatus(program));
    }

    [Fact]
    public void LinkProgram_WhenShouldFail_MarksAsNotLinked()
    {
        using var device = new MockGraphicsDevice();
        device.ShouldFailProgramLink = true;
        var program = device.CreateProgram();

        device.LinkProgram(program);

        Assert.False(device.Programs[program].IsLinked);
        Assert.False(device.GetProgramLinkStatus(program));
    }

    [Fact]
    public void GetProgramInfoLog_ReturnsErrorOnFailure()
    {
        using var device = new MockGraphicsDevice();
        device.ShouldFailProgramLink = true;

        var log = device.GetProgramInfoLog(0);

        Assert.Contains("failed", log);
    }

    [Fact]
    public void UseProgram_BindsProgram()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();

        device.UseProgram(program);

        Assert.Equal(program, device.BoundProgram);
    }

    [Fact]
    public void UseProgram_WithZero_UnbindsProgram()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);

        device.UseProgram(0);

        Assert.Null(device.BoundProgram);
    }

    [Fact]
    public void DeleteProgram_RemovesProgram()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);

        device.DeleteProgram(program);

        Assert.DoesNotContain(program, device.Programs.Keys);
        Assert.Null(device.BoundProgram);
    }

    [Fact]
    public void GetUniformLocation_ReturnsLocation()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();

        var location = device.GetUniformLocation(program, "uMatrix");

        Assert.True(location >= 0);
        Assert.Equal(location, device.Programs[program].UniformLocations["uMatrix"]);
    }

    [Fact]
    public void GetUniformLocation_ReturnsSameLocationForSameName()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();

        var loc1 = device.GetUniformLocation(program, "uMatrix");
        var loc2 = device.GetUniformLocation(program, "uMatrix");

        Assert.Equal(loc1, loc2);
    }

    [Fact]
    public void GetUniformLocation_ForInvalidProgram_ReturnsNegative()
    {
        using var device = new MockGraphicsDevice();

        var location = device.GetUniformLocation(999, "uMatrix");

        Assert.Equal(-1, location);
    }

    [Fact]
    public void Uniform1_Float_StoresValue()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);
        var loc = device.GetUniformLocation(program, "uTime");

        device.Uniform1(loc, 1.5f);

        Assert.Equal(1.5f, device.Programs[program].UniformValues[loc]);
    }

    [Fact]
    public void Uniform1_Int_StoresValue()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);
        var loc = device.GetUniformLocation(program, "uTexture");

        device.Uniform1(loc, 0);

        Assert.Equal(0, device.Programs[program].UniformValues[loc]);
    }

    [Fact]
    public void Uniform2_StoresVector()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);
        var loc = device.GetUniformLocation(program, "uResolution");

        device.Uniform2(loc, 800f, 600f);

        Assert.Equal(new Vector2(800f, 600f), device.Programs[program].UniformValues[loc]);
    }

    [Fact]
    public void Uniform3_StoresVector()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);
        var loc = device.GetUniformLocation(program, "uColor");

        device.Uniform3(loc, 1f, 0f, 0f);

        Assert.Equal(new Vector3(1f, 0f, 0f), device.Programs[program].UniformValues[loc]);
    }

    [Fact]
    public void Uniform4_StoresVector()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);
        var loc = device.GetUniformLocation(program, "uColor");

        device.Uniform4(loc, 1f, 0f, 0f, 1f);

        Assert.Equal(new Vector4(1f, 0f, 0f, 1f), device.Programs[program].UniformValues[loc]);
    }

    [Fact]
    public void UniformMatrix4_StoresMatrix()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);
        var loc = device.GetUniformLocation(program, "uModelView");
        var matrix = Matrix4x4.Identity;

        device.UniformMatrix4(loc, matrix);

        Assert.Equal(matrix, device.Programs[program].UniformValues[loc]);
    }

    #endregion

    #region Rendering Operations

    [Fact]
    public void ClearColor_SetsColor()
    {
        using var device = new MockGraphicsDevice();

        device.ClearColor(0.2f, 0.3f, 0.4f, 1f);

        Assert.Equal(new Vector4(0.2f, 0.3f, 0.4f, 1f), device.RenderState.ClearColor);
    }

    [Fact]
    public void Clear_UpdatesClearState()
    {
        using var device = new MockGraphicsDevice();

        device.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);

        Assert.Equal(ClearMask.ColorBuffer | ClearMask.DepthBuffer, device.RenderState.LastClearMask);
        Assert.Equal(1, device.RenderState.ClearCount);
    }

    [Fact]
    public void Enable_AddsCapability()
    {
        using var device = new MockGraphicsDevice();

        device.Enable(RenderCapability.DepthTest);

        Assert.Contains(RenderCapability.DepthTest, device.RenderState.EnabledCapabilities);
    }

    [Fact]
    public void Disable_RemovesCapability()
    {
        using var device = new MockGraphicsDevice();
        device.Enable(RenderCapability.DepthTest);

        device.Disable(RenderCapability.DepthTest);

        Assert.DoesNotContain(RenderCapability.DepthTest, device.RenderState.EnabledCapabilities);
    }

    [Fact]
    public void CullFace_SetsCullFaceMode()
    {
        using var device = new MockGraphicsDevice();

        device.CullFace(CullFaceMode.Back);

        Assert.Equal(CullFaceMode.Back, device.RenderState.CullFaceMode);
    }

    [Fact]
    public void Viewport_SetsViewport()
    {
        using var device = new MockGraphicsDevice();

        device.Viewport(0, 0, 800, 600);

        Assert.Equal((0, 0, 800, 600), device.RenderState.Viewport);
    }

    [Fact]
    public void Scissor_SetsScissorRect()
    {
        using var device = new MockGraphicsDevice();

        device.Scissor(10, 20, 100, 200);

        Assert.Equal((10, 20, 100, 200), device.RenderState.ScissorRect);
    }

    [Fact]
    public void BlendFunc_SetsBlendFactors()
    {
        using var device = new MockGraphicsDevice();

        device.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);

        Assert.Equal(BlendFactor.SrcAlpha, device.RenderState.BlendSrcFactor);
        Assert.Equal(BlendFactor.OneMinusSrcAlpha, device.RenderState.BlendDstFactor);
    }

    [Fact]
    public void DepthFunc_SetsDepthFunction()
    {
        using var device = new MockGraphicsDevice();

        device.DepthFunc(DepthFunction.LessOrEqual);

        Assert.Equal(DepthFunction.LessOrEqual, device.RenderState.DepthFunction);
    }

    [Fact]
    public void DrawElements_RecordsDrawCall()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        var program = device.CreateProgram();
        device.BindVertexArray(vao);
        device.UseProgram(program);

        device.DrawElements(PrimitiveType.Triangles, 36, IndexType.UnsignedInt);

        Assert.Single(device.DrawCalls);
        Assert.Equal(1, device.DrawCount);
        var call = device.DrawCalls[0];
        Assert.Equal(PrimitiveType.Triangles, call.PrimitiveType);
        Assert.Equal(36, call.VertexCount);
        Assert.True(call.IsIndexed);
        Assert.Equal(program, call.Program);
        Assert.Equal(vao, call.VAO);
    }

    [Fact]
    public void DrawArrays_RecordsDrawCall()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.DrawArrays(PrimitiveType.Lines, 0, 10);

        Assert.Single(device.DrawCalls);
        var call = device.DrawCalls[0];
        Assert.Equal(PrimitiveType.Lines, call.PrimitiveType);
        Assert.Equal(10, call.VertexCount);
        Assert.False(call.IsIndexed);
    }

    [Fact]
    public void LineWidth_SetsLineWidth()
    {
        using var device = new MockGraphicsDevice();

        device.LineWidth(2.5f);

        Assert.Equal(2.5f, device.RenderState.LineWidth);
    }

    [Fact]
    public void PointSize_SetsPointSize()
    {
        using var device = new MockGraphicsDevice();

        device.PointSize(3.0f);

        Assert.Equal(3.0f, device.RenderState.PointSize);
    }

    #endregion

    #region Error Handling

    [Fact]
    public void GetError_ReturnsAndClearsError()
    {
        using var device = new MockGraphicsDevice();
        device.SimulatedErrorCode = 42;

        var error = device.GetError();

        Assert.Equal(42, error);
        Assert.Equal(0, device.GetError()); // Should be cleared
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_ClearsAllState()
    {
        var device = new MockGraphicsDevice();
        device.GenBuffer();
        device.GenTexture();

        device.Dispose();

        Assert.Empty(device.Buffers);
        Assert.Empty(device.Textures);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var device = new MockGraphicsDevice();

        device.Dispose();
        device.Dispose();

        // Should not throw
    }

    #endregion
}

#region Supporting Type Tests

public class MockRenderStateTests
{
    [Fact]
    public void Reset_ClearsAllState()
    {
        var state = new MockRenderState
        {
            ClearColor = new Vector4(1, 0, 0, 1),
            ClearCount = 5,
            LineWidth = 3f,
            PointSize = 5f
        };
        state.EnabledCapabilities.Add(RenderCapability.DepthTest);
        state.PixelStoreParameters[PixelStoreParameter.UnpackAlignment] = 4;

        state.Reset();

        Assert.Equal(Vector4.Zero, state.ClearColor);
        Assert.Equal(0, state.ClearCount);
        Assert.Equal(1f, state.LineWidth);
        Assert.Equal(1f, state.PointSize);
        Assert.Empty(state.EnabledCapabilities);
        Assert.Empty(state.PixelStoreParameters);
    }
}

public class DrawCallTests
{
    [Fact]
    public void DrawCall_RecordEquality()
    {
        var textures = new List<uint> { 1, 2 };
        var call1 = new DrawCall(PrimitiveType.Triangles, 36, true, 1, 2, textures);
        var call2 = new DrawCall(PrimitiveType.Triangles, 36, true, 1, 2, textures);

        // Record equality compares by value
        Assert.Equal(call1.PrimitiveType, call2.PrimitiveType);
        Assert.Equal(call1.VertexCount, call2.VertexCount);
        Assert.Equal(call1.IsIndexed, call2.IsIndexed);
    }
}

public class MockBufferTests
{
    [Fact]
    public void MockBuffer_StoresProperties()
    {
        var buffer = new MockBuffer(1)
        {
            Data = [1, 2, 3],
            Usage = BufferUsage.DynamicDraw,
            Target = BufferTarget.ElementArrayBuffer
        };

        Assert.Equal(1u, buffer.Handle);
        Assert.Equal([1, 2, 3], buffer.Data);
        Assert.Equal(BufferUsage.DynamicDraw, buffer.Usage);
        Assert.Equal(BufferTarget.ElementArrayBuffer, buffer.Target);
    }
}

public class MockTextureTests
{
    [Fact]
    public void MockTexture_StoresProperties()
    {
        var texture = new MockTexture(1)
        {
            Width = 256,
            Height = 256,
            Format = PixelFormat.RGBA,
            Target = TextureTarget.Texture2D,
            HasGeneratedMipmaps = true
        };

        Assert.Equal(1u, texture.Handle);
        Assert.Equal(256, texture.Width);
        Assert.Equal(256, texture.Height);
        Assert.Equal(PixelFormat.RGBA, texture.Format);
        Assert.Equal(TextureTarget.Texture2D, texture.Target);
        Assert.True(texture.HasGeneratedMipmaps);
    }
}

public class MockShaderTests
{
    [Fact]
    public void MockShader_StoresProperties()
    {
        var shader = new MockShader(1, ShaderType.Fragment)
        {
            Source = "void main() {}",
            IsCompiled = true
        };

        Assert.Equal(1u, shader.Handle);
        Assert.Equal(ShaderType.Fragment, shader.Type);
        Assert.Equal("void main() {}", shader.Source);
        Assert.True(shader.IsCompiled);
    }
}

public class MockProgramTests
{
    [Fact]
    public void MockProgram_StoresProperties()
    {
        var program = new MockProgram(1)
        {
            IsLinked = true
        };
        program.AttachedShaders.Add(2);
        program.UniformLocations["uMatrix"] = 0;
        program.UniformValues[0] = Matrix4x4.Identity;

        Assert.Equal(1u, program.Handle);
        Assert.True(program.IsLinked);
        Assert.Contains(2u, program.AttachedShaders);
        Assert.Equal(0, program.UniformLocations["uMatrix"]);
        Assert.Equal(Matrix4x4.Identity, program.UniformValues[0]);
    }
}

public class MockVAOTests
{
    [Fact]
    public void MockVAO_StoresProperties()
    {
        var vao = new MockVAO(1);
        vao.EnabledAttributes.Add(0);
        vao.EnabledAttributes.Add(1);
        vao.Attributes[0] = new VertexAttribute(0, 3, VertexAttribType.Float, false, 12, 0);

        Assert.Equal(1u, vao.Handle);
        Assert.Contains(0u, vao.EnabledAttributes);
        Assert.Contains(1u, vao.EnabledAttributes);
        Assert.Equal(3, vao.Attributes[0].Size);
    }
}

public class VertexAttributeTests
{
    [Fact]
    public void VertexAttribute_RecordEquality()
    {
        var attr1 = new VertexAttribute(0, 3, VertexAttribType.Float, false, 12, 0);
        var attr2 = new VertexAttribute(0, 3, VertexAttribType.Float, false, 12, 0);

        Assert.Equal(attr1, attr2);
    }
}

#endregion
