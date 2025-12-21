using System.Numerics;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for render command types.
/// </summary>
public class RenderCommandTests
{
    #region ClearCommand Tests

    [Fact]
    public void ClearCommand_Constructor_SetsProperties()
    {
        var color = new Vector4(0.2f, 0.3f, 0.4f, 1f);
        var command = new ClearCommand(
            ClearMask.ColorBuffer | ClearMask.DepthBuffer,
            color,
            0.8f,
            5);

        Assert.Equal(ClearMask.ColorBuffer | ClearMask.DepthBuffer, command.Mask);
        Assert.Equal(color, command.Color);
        Assert.Equal(0.8f, command.Depth);
        Assert.Equal(5, command.Stencil);
    }

    [Fact]
    public void ClearCommand_DefaultParameters_UsesDefaults()
    {
        var command = new ClearCommand(ClearMask.ColorBuffer);

        Assert.Equal(ClearMask.ColorBuffer, command.Mask);
        Assert.Equal(default(Vector4), command.Color);
        Assert.Equal(1f, command.Depth);
        Assert.Equal(0, command.Stencil);
    }

    [Fact]
    public void ClearCommand_SortKey_IsZero()
    {
        var command = new ClearCommand(ClearMask.ColorBuffer);

        Assert.Equal(0UL, command.SortKey);
    }

    [Fact]
    public void ClearCommand_ColorOnly_CreatesColorOnlyCommand()
    {
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var command = ClearCommand.ColorOnly(color);

        Assert.Equal(ClearMask.ColorBuffer, command.Mask);
        Assert.Equal(color, command.Color);
    }

    [Fact]
    public void ClearCommand_ColorAndDepth_CreatesColorAndDepthCommand()
    {
        var color = new Vector4(0.1f, 0.2f, 0.3f, 1f);
        var command = ClearCommand.ColorAndDepth(color, 0.5f);

        Assert.Equal(ClearMask.ColorBuffer | ClearMask.DepthBuffer, command.Mask);
        Assert.Equal(color, command.Color);
        Assert.Equal(0.5f, command.Depth);
    }

    [Fact]
    public void ClearCommand_ColorAndDepth_DefaultDepth_UsesOnePointZero()
    {
        var color = new Vector4(0.1f, 0.2f, 0.3f, 1f);
        var command = ClearCommand.ColorAndDepth(color);

        Assert.Equal(1f, command.Depth);
    }

    [Fact]
    public void ClearCommand_All_CreatesCommandForAllBuffers()
    {
        var color = new Vector4(0.1f, 0.2f, 0.3f, 1f);
        var command = ClearCommand.All(color, 0.9f, 7);

        Assert.Equal(
            ClearMask.ColorBuffer | ClearMask.DepthBuffer | ClearMask.StencilBuffer,
            command.Mask);
        Assert.Equal(color, command.Color);
        Assert.Equal(0.9f, command.Depth);
        Assert.Equal(7, command.Stencil);
    }

    [Fact]
    public void ClearCommand_All_DefaultParameters_UsesDefaults()
    {
        var color = new Vector4(0.1f, 0.2f, 0.3f, 1f);
        var command = ClearCommand.All(color);

        Assert.Equal(1f, command.Depth);
        Assert.Equal(0, command.Stencil);
    }

    [Fact]
    public void ClearCommand_ImplementsIRenderCommand()
    {
        var command = new ClearCommand(ClearMask.ColorBuffer);

        Assert.IsAssignableFrom<IRenderCommand>(command);
    }

    #endregion

    #region DrawMeshCommand Tests

    [Fact]
    public void DrawMeshCommand_Constructor_SetsProperties()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(2);
        var texture = new TextureHandle(3);
        var transform = Matrix4x4.CreateTranslation(1, 2, 3);
        var sortKey = 12345UL;

        var command = new DrawMeshCommand(mesh, shader, texture, transform, sortKey);

        Assert.Equal(mesh, command.Mesh);
        Assert.Equal(shader, command.Shader);
        Assert.Equal(texture, command.Texture);
        Assert.Equal(transform, command.Transform);
        Assert.Equal(sortKey, command.SortKey);
    }

    [Fact]
    public void DrawMeshCommand_Create_ComputesSortKey()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(5);
        var texture = new TextureHandle(10);
        var transform = Matrix4x4.Identity;
        var depth = 15.5f;

        var command = DrawMeshCommand.Create(mesh, shader, texture, transform, depth);

        Assert.Equal(mesh, command.Mesh);
        Assert.Equal(shader, command.Shader);
        Assert.Equal(texture, command.Texture);
        Assert.Equal(transform, command.Transform);
        // Sort key should be non-zero and computed from shader, texture, depth
        Assert.NotEqual(0UL, command.SortKey);
    }

    [Fact]
    public void DrawMeshCommand_Create_EncodesShaderInSortKey()
    {
        var mesh = new MeshHandle(1);
        var shader1 = new ShaderHandle(5);
        var shader2 = new ShaderHandle(6);
        var texture = new TextureHandle(10);
        var transform = Matrix4x4.Identity;
        var depth = 15.5f;

        var command1 = DrawMeshCommand.Create(mesh, shader1, texture, transform, depth);
        var command2 = DrawMeshCommand.Create(mesh, shader2, texture, transform, depth);

        // Different shaders should produce different sort keys
        Assert.NotEqual(command1.SortKey, command2.SortKey);
    }

    [Fact]
    public void DrawMeshCommand_Create_EncodesTextureInSortKey()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(5);
        var texture1 = new TextureHandle(10);
        var texture2 = new TextureHandle(11);
        var transform = Matrix4x4.Identity;
        var depth = 15.5f;

        var command1 = DrawMeshCommand.Create(mesh, shader, texture1, transform, depth);
        var command2 = DrawMeshCommand.Create(mesh, shader, texture2, transform, depth);

        // Different textures should produce different sort keys
        Assert.NotEqual(command1.SortKey, command2.SortKey);
    }

    [Fact]
    public void DrawMeshCommand_Create_EncodesDepthInSortKey()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(5);
        var texture = new TextureHandle(10);
        var transform = Matrix4x4.Identity;
        var depth1 = 10.0f;
        var depth2 = 20.0f;

        var command1 = DrawMeshCommand.Create(mesh, shader, texture, transform, depth1);
        var command2 = DrawMeshCommand.Create(mesh, shader, texture, transform, depth2);

        // Different depths should produce different sort keys
        Assert.NotEqual(command1.SortKey, command2.SortKey);
    }

    [Fact]
    public void DrawMeshCommand_CreateSimple_UsesIdentityTransform()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(5);
        var texture = new TextureHandle(10);

        var command = DrawMeshCommand.CreateSimple(mesh, shader, texture);

        Assert.Equal(Matrix4x4.Identity, command.Transform);
    }

    [Fact]
    public void DrawMeshCommand_CreateSimple_ComputesSortKeyFromShader()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(5);
        var texture = new TextureHandle(10);

        var command = DrawMeshCommand.CreateSimple(mesh, shader, texture);

        // Sort key should be based on shader ID shifted to high bits
        var expectedSortKey = (ulong)5 << 48;
        Assert.Equal(expectedSortKey, command.SortKey);
    }

    [Fact]
    public void DrawMeshCommand_ImplementsIRenderCommand()
    {
        var command = DrawMeshCommand.CreateSimple(
            new MeshHandle(1),
            new ShaderHandle(2),
            new TextureHandle(3));

        Assert.IsAssignableFrom<IRenderCommand>(command);
    }

    #endregion

    #region SetRenderStateCommand Tests

    [Fact]
    public void SetRenderStateCommand_Constructor_SetsProperties()
    {
        var command = new SetRenderStateCommand(
            DepthTest: true,
            DepthWrite: false,
            Blending: true,
            CullFace: false,
            CullMode: CullFaceMode.Front,
            BlendSrc: BlendFactor.SrcAlpha,
            BlendDst: BlendFactor.OneMinusSrcAlpha);

        Assert.True(command.DepthTest);
        Assert.False(command.DepthWrite);
        Assert.True(command.Blending);
        Assert.False(command.CullFace);
        Assert.Equal(CullFaceMode.Front, command.CullMode);
        Assert.Equal(BlendFactor.SrcAlpha, command.BlendSrc);
        Assert.Equal(BlendFactor.OneMinusSrcAlpha, command.BlendDst);
    }

    [Fact]
    public void SetRenderStateCommand_DefaultParameters_AreNull()
    {
        var command = new SetRenderStateCommand();

        Assert.Null(command.DepthTest);
        Assert.Null(command.DepthWrite);
        Assert.Null(command.Blending);
        Assert.Null(command.CullFace);
        Assert.Null(command.CullMode);
        Assert.Null(command.BlendSrc);
        Assert.Null(command.BlendDst);
    }

    [Fact]
    public void SetRenderStateCommand_SortKey_IsTwo()
    {
        var command = new SetRenderStateCommand();

        Assert.Equal(2UL, command.SortKey);
    }

    [Fact]
    public void SetRenderStateCommand_Opaque_HasCorrectSettings()
    {
        var command = SetRenderStateCommand.Opaque;

        Assert.True(command.DepthTest);
        Assert.True(command.DepthWrite);
        Assert.False(command.Blending);
        Assert.True(command.CullFace);
        Assert.Equal(CullFaceMode.Back, command.CullMode);
    }

    [Fact]
    public void SetRenderStateCommand_Transparent_HasCorrectSettings()
    {
        var command = SetRenderStateCommand.Transparent;

        Assert.True(command.DepthTest);
        Assert.False(command.DepthWrite);
        Assert.True(command.Blending);
        Assert.False(command.CullFace);
        Assert.Equal(BlendFactor.SrcAlpha, command.BlendSrc);
        Assert.Equal(BlendFactor.OneMinusSrcAlpha, command.BlendDst);
    }

    [Fact]
    public void SetRenderStateCommand_Additive_HasCorrectSettings()
    {
        var command = SetRenderStateCommand.Additive;

        Assert.True(command.DepthTest);
        Assert.False(command.DepthWrite);
        Assert.True(command.Blending);
        Assert.False(command.CullFace);
        Assert.Equal(BlendFactor.SrcAlpha, command.BlendSrc);
        Assert.Equal(BlendFactor.One, command.BlendDst);
    }

    [Fact]
    public void SetRenderStateCommand_EnableBlending_CreatesBlendingCommand()
    {
        var command = SetRenderStateCommand.EnableBlending(
            BlendFactor.One,
            BlendFactor.Zero);

        Assert.True(command.Blending);
        Assert.Equal(BlendFactor.One, command.BlendSrc);
        Assert.Equal(BlendFactor.Zero, command.BlendDst);
    }

    [Fact]
    public void SetRenderStateCommand_DisableBlending_CreatesDisableCommand()
    {
        var command = SetRenderStateCommand.DisableBlending();

        Assert.False(command.Blending);
    }

    [Fact]
    public void SetRenderStateCommand_PartialState_OnlySetsSpecifiedProperties()
    {
        var command = new SetRenderStateCommand(DepthTest: true, Blending: false);

        Assert.True(command.DepthTest);
        Assert.False(command.Blending);
        Assert.Null(command.DepthWrite);
        Assert.Null(command.CullFace);
    }

    [Fact]
    public void SetRenderStateCommand_ImplementsIRenderCommand()
    {
        var command = new SetRenderStateCommand();

        Assert.IsAssignableFrom<IRenderCommand>(command);
    }

    #endregion

    #region SetViewportCommand Tests

    [Fact]
    public void SetViewportCommand_Constructor_SetsProperties()
    {
        var command = new SetViewportCommand(10, 20, 800, 600);

        Assert.Equal(10, command.X);
        Assert.Equal(20, command.Y);
        Assert.Equal(800, command.Width);
        Assert.Equal(600, command.Height);
    }

    [Fact]
    public void SetViewportCommand_SortKey_IsOne()
    {
        var command = new SetViewportCommand(0, 0, 100, 100);

        Assert.Equal(1UL, command.SortKey);
    }

    [Fact]
    public void SetViewportCommand_FullWindow_CreatesFullWindowViewport()
    {
        var command = SetViewportCommand.FullWindow(1920, 1080);

        Assert.Equal(0, command.X);
        Assert.Equal(0, command.Y);
        Assert.Equal(1920, command.Width);
        Assert.Equal(1080, command.Height);
    }

    [Fact]
    public void SetViewportCommand_NegativeCoordinates_AreAllowed()
    {
        var command = new SetViewportCommand(-10, -20, 100, 100);

        Assert.Equal(-10, command.X);
        Assert.Equal(-20, command.Y);
    }

    [Fact]
    public void SetViewportCommand_ZeroDimensions_AreAllowed()
    {
        var command = new SetViewportCommand(0, 0, 0, 0);

        Assert.Equal(0, command.Width);
        Assert.Equal(0, command.Height);
    }

    [Fact]
    public void SetViewportCommand_ImplementsIRenderCommand()
    {
        var command = new SetViewportCommand(0, 0, 100, 100);

        Assert.IsAssignableFrom<IRenderCommand>(command);
    }

    #endregion

    #region Sort Key Ordering Tests

    [Fact]
    public void RenderCommands_SortKeyOrdering_ClearBeforeViewportBeforeState()
    {
        var clear = new ClearCommand(ClearMask.ColorBuffer);
        var viewport = new SetViewportCommand(0, 0, 100, 100);
        var state = new SetRenderStateCommand();

        Assert.True(clear.SortKey < viewport.SortKey);
        Assert.True(viewport.SortKey < state.SortKey);
    }

    [Fact]
    public void RenderCommands_DrawCommand_HasHigherSortKeyThanStateCommands()
    {
        var state = new SetRenderStateCommand();
        var draw = DrawMeshCommand.CreateSimple(
            new MeshHandle(1),
            new ShaderHandle(1),
            new TextureHandle(1));

        // Draw commands should have higher sort keys than state commands
        // (shader ID is in high bits, so minimum is 1 << 48)
        Assert.True(draw.SortKey > state.SortKey);
    }

    #endregion
}
