using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockGraphicsDeviceTests
{
    #region Buffer Operations

    [Fact]
    public void GenBuffer_CreatesBuffer()
    {
        using var device = new MockGraphicsDevice();

        var buffer = device.GenBuffer();

        buffer.ShouldNotBe(0u);
        device.Buffers.ShouldContainKey(buffer);
    }

    [Fact]
    public void GenBuffer_ReturnsUniqueHandles()
    {
        using var device = new MockGraphicsDevice();

        var buffer1 = device.GenBuffer();
        var buffer2 = device.GenBuffer();

        buffer1.ShouldNotBe(buffer2);
    }

    [Fact]
    public void BindBuffer_SetsBoundBuffer()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();

        device.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        device.BoundBuffers[BufferTarget.ArrayBuffer].ShouldBe(buffer);
    }

    [Fact]
    public void BindBuffer_ZeroUnbindsBuffer()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();
        device.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        device.BindBuffer(BufferTarget.ArrayBuffer, 0);

        device.BoundBuffers.ShouldNotContainKey(BufferTarget.ArrayBuffer);
    }

    [Fact]
    public void BufferData_StoresData()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();
        device.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        var data = new byte[] { 1, 2, 3, 4 };

        device.BufferData(BufferTarget.ArrayBuffer, data, BufferUsage.StaticDraw);

        device.Buffers[buffer].Data.ShouldBe(data);
        device.Buffers[buffer].Usage.ShouldBe(BufferUsage.StaticDraw);
    }

    [Fact]
    public void DeleteBuffer_RemovesBuffer()
    {
        using var device = new MockGraphicsDevice();
        var buffer = device.GenBuffer();

        device.DeleteBuffer(buffer);

        device.Buffers.ShouldNotContainKey(buffer);
    }

    #endregion

    #region VAO Operations

    [Fact]
    public void GenVertexArray_CreatesVAO()
    {
        using var device = new MockGraphicsDevice();

        var vao = device.GenVertexArray();

        vao.ShouldNotBe(0u);
        device.VAOs.ShouldContainKey(vao);
    }

    [Fact]
    public void BindVertexArray_SetsBoundVAO()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();

        device.BindVertexArray(vao);

        device.BoundVAO.ShouldBe(vao);
    }

    [Fact]
    public void BindVertexArray_ZeroUnbinds()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.BindVertexArray(0);

        device.BoundVAO.ShouldBeNull();
    }

    [Fact]
    public void EnableVertexAttribArray_TracksEnabledAttributes()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.EnableVertexAttribArray(0);
        device.EnableVertexAttribArray(1);

        device.VAOs[vao].EnabledAttributes.ShouldContain(0u);
        device.VAOs[vao].EnabledAttributes.ShouldContain(1u);
    }

    [Fact]
    public void VertexAttribPointer_TracksConfiguration()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);

        device.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 12, 0);

        var attr = device.VAOs[vao].Attributes[0];
        attr.Size.ShouldBe(3);
        attr.Type.ShouldBe(VertexAttribType.Float);
        attr.Normalized.ShouldBeFalse();
        attr.Stride.ShouldBe(12u);
    }

    #endregion

    #region Texture Operations

    [Fact]
    public void GenTexture_CreatesTexture()
    {
        using var device = new MockGraphicsDevice();

        var texture = device.GenTexture();

        texture.ShouldNotBe(0u);
        device.Textures.ShouldContainKey(texture);
    }

    [Fact]
    public void BindTexture_SetsBoundTexture()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();

        device.BindTexture(TextureTarget.Texture2D, texture);

        device.BoundTextures[TextureUnit.Texture0].ShouldBe(texture);
    }

    [Fact]
    public void ActiveTexture_ChangesActiveUnit()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();

        device.ActiveTexture(TextureUnit.Texture1);
        device.BindTexture(TextureTarget.Texture2D, texture);

        device.ActiveTextureUnit.ShouldBe(TextureUnit.Texture1);
        device.BoundTextures[TextureUnit.Texture1].ShouldBe(texture);
    }

    [Fact]
    public void TexImage2D_StoresTextureData()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);
        var pixels = new byte[16];

        device.TexImage2D(TextureTarget.Texture2D, 0, 4, 4, PixelFormat.RGBA, pixels);

        device.Textures[texture].Width.ShouldBe(4);
        device.Textures[texture].Height.ShouldBe(4);
        device.Textures[texture].Format.ShouldBe(PixelFormat.RGBA);
    }

    [Fact]
    public void TexImage2D_WhenShouldFail_SetsErrorCode()
    {
        using var device = new MockGraphicsDevice();
        device.ShouldFailTextureLoad = true;
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);

        device.TexImage2D(TextureTarget.Texture2D, 0, 4, 4, PixelFormat.RGBA, new byte[16]);

        device.GetError().ShouldNotBe(0);
    }

    [Fact]
    public void GenerateMipmap_SetsFlag()
    {
        using var device = new MockGraphicsDevice();
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);

        device.GenerateMipmap(TextureTarget.Texture2D);

        device.Textures[texture].HasGeneratedMipmaps.ShouldBeTrue();
    }

    #endregion

    #region Shader Operations

    [Fact]
    public void CreateShader_CreatesShader()
    {
        using var device = new MockGraphicsDevice();

        var shader = device.CreateShader(ShaderType.Vertex);

        shader.ShouldNotBe(0u);
        device.Shaders.ShouldContainKey(shader);
        device.Shaders[shader].Type.ShouldBe(ShaderType.Vertex);
    }

    [Fact]
    public void ShaderSource_StoresSource()
    {
        using var device = new MockGraphicsDevice();
        var shader = device.CreateShader(ShaderType.Vertex);
        var source = "void main() {}";

        device.ShaderSource(shader, source);

        device.Shaders[shader].Source.ShouldBe(source);
    }

    [Fact]
    public void CompileShader_SetsCompiled()
    {
        using var device = new MockGraphicsDevice();
        var shader = device.CreateShader(ShaderType.Vertex);

        device.CompileShader(shader);

        device.Shaders[shader].IsCompiled.ShouldBeTrue();
    }

    [Fact]
    public void CompileShader_WhenShouldFail_DoesNotSetCompiled()
    {
        using var device = new MockGraphicsDevice();
        device.ShouldFailShaderCompile = true;
        var shader = device.CreateShader(ShaderType.Vertex);

        device.CompileShader(shader);

        device.Shaders[shader].IsCompiled.ShouldBeFalse();
    }

    [Fact]
    public void GetShaderCompileStatus_ReturnsCompileStatus()
    {
        using var device = new MockGraphicsDevice();
        var shader = device.CreateShader(ShaderType.Vertex);
        device.CompileShader(shader);

        device.GetShaderCompileStatus(shader).ShouldBeTrue();
    }

    #endregion

    #region Program Operations

    [Fact]
    public void CreateProgram_CreatesProgram()
    {
        using var device = new MockGraphicsDevice();

        var program = device.CreateProgram();

        program.ShouldNotBe(0u);
        device.Programs.ShouldContainKey(program);
    }

    [Fact]
    public void AttachShader_AttachesShader()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        var shader = device.CreateShader(ShaderType.Vertex);

        device.AttachShader(program, shader);

        device.Programs[program].AttachedShaders.ShouldContain(shader);
    }

    [Fact]
    public void LinkProgram_SetsLinked()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();

        device.LinkProgram(program);

        device.Programs[program].IsLinked.ShouldBeTrue();
    }

    [Fact]
    public void UseProgram_SetsBoundProgram()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();

        device.UseProgram(program);

        device.BoundProgram.ShouldBe(program);
    }

    [Fact]
    public void GetUniformLocation_ReturnsLocation()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();

        var location = device.GetUniformLocation(program, "uColor");

        location.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Uniform_StoresValue()
    {
        using var device = new MockGraphicsDevice();
        var program = device.CreateProgram();
        device.UseProgram(program);
        var location = device.GetUniformLocation(program, "uColor");

        device.Uniform4(location, 1f, 0f, 0f, 1f);

        device.Programs[program].UniformValues[location].ShouldBe(new Vector4(1f, 0f, 0f, 1f));
    }

    #endregion

    #region Draw Operations

    [Fact]
    public void DrawElements_RecordsDrawCall()
    {
        using var device = new MockGraphicsDevice();
        var vao = device.GenVertexArray();
        device.BindVertexArray(vao);
        var program = device.CreateProgram();
        device.UseProgram(program);

        device.DrawElements(PrimitiveType.Triangles, 36, IndexType.UnsignedInt);

        device.DrawCalls.Count.ShouldBe(1);
        device.DrawCalls[0].PrimitiveType.ShouldBe(PrimitiveType.Triangles);
        device.DrawCalls[0].VertexCount.ShouldBe(36);
        device.DrawCalls[0].IsIndexed.ShouldBeTrue();
    }

    [Fact]
    public void DrawArrays_RecordsDrawCall()
    {
        using var device = new MockGraphicsDevice();

        device.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        device.DrawCalls.Count.ShouldBe(1);
        device.DrawCalls[0].PrimitiveType.ShouldBe(PrimitiveType.TriangleStrip);
        device.DrawCalls[0].IsIndexed.ShouldBeFalse();
    }

    [Fact]
    public void DrawCount_ReturnsNumberOfDrawCalls()
    {
        using var device = new MockGraphicsDevice();

        device.DrawArrays(PrimitiveType.Triangles, 0, 3);
        device.DrawArrays(PrimitiveType.Triangles, 0, 6);

        device.DrawCount.ShouldBe(2);
    }

    #endregion

    #region Render State

    [Fact]
    public void ClearColor_SetsColor()
    {
        using var device = new MockGraphicsDevice();

        device.ClearColor(1f, 0f, 0f, 1f);

        device.RenderState.ClearColor.ShouldBe(new Vector4(1f, 0f, 0f, 1f));
    }

    [Fact]
    public void Clear_IncrementsCount()
    {
        using var device = new MockGraphicsDevice();

        device.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);

        device.RenderState.ClearCount.ShouldBe(1);
        device.RenderState.LastClearMask.ShouldBe(ClearMask.ColorBuffer | ClearMask.DepthBuffer);
    }

    [Fact]
    public void Enable_AddsCapability()
    {
        using var device = new MockGraphicsDevice();

        device.Enable(RenderCapability.DepthTest);
        device.Enable(RenderCapability.Blend);

        device.RenderState.EnabledCapabilities.ShouldContain(RenderCapability.DepthTest);
        device.RenderState.EnabledCapabilities.ShouldContain(RenderCapability.Blend);
    }

    [Fact]
    public void Disable_RemovesCapability()
    {
        using var device = new MockGraphicsDevice();
        device.Enable(RenderCapability.DepthTest);

        device.Disable(RenderCapability.DepthTest);

        device.RenderState.EnabledCapabilities.ShouldNotContain(RenderCapability.DepthTest);
    }

    [Fact]
    public void Viewport_SetsViewport()
    {
        using var device = new MockGraphicsDevice();

        device.Viewport(0, 0, 1920, 1080);

        device.RenderState.Viewport.ShouldBe((0, 0, 1920, 1080));
    }

    [Fact]
    public void BlendFunc_SetsBlendFactors()
    {
        using var device = new MockGraphicsDevice();

        device.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);

        device.RenderState.BlendSrcFactor.ShouldBe(BlendFactor.SrcAlpha);
        device.RenderState.BlendDstFactor.ShouldBe(BlendFactor.OneMinusSrcAlpha);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var device = new MockGraphicsDevice();
        device.GenBuffer();
        device.GenTexture();
        device.CreateProgram();
        device.DrawArrays(PrimitiveType.Triangles, 0, 3);

        device.Reset();

        device.Buffers.ShouldBeEmpty();
        device.Textures.ShouldBeEmpty();
        device.Programs.ShouldBeEmpty();
        device.DrawCalls.ShouldBeEmpty();
    }

    [Fact]
    public void ClearDrawCalls_ClearsOnlyDrawCalls()
    {
        using var device = new MockGraphicsDevice();
        device.GenBuffer();
        device.DrawArrays(PrimitiveType.Triangles, 0, 3);

        device.ClearDrawCalls();

        device.DrawCalls.ShouldBeEmpty();
        device.Buffers.ShouldNotBeEmpty();
    }

    #endregion

    #region Error Handling

    [Fact]
    public void GetError_ReturnsAndClearsError()
    {
        using var device = new MockGraphicsDevice();
        device.SimulatedErrorCode = 1281;

        var error = device.GetError();

        error.ShouldBe(1281);
        device.GetError().ShouldBe(0);
    }

    #endregion
}
