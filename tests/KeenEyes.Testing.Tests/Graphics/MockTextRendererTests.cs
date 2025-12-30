using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockTextRendererTests
{
    #region Batch Control

    [Fact]
    public void Begin_SetsIsInBatchTrue()
    {
        using var renderer = new MockTextRenderer();

        renderer.Begin();

        Assert.True(renderer.IsInBatch);
    }

    [Fact]
    public void Begin_IncrementsBeginCount()
    {
        using var renderer = new MockTextRenderer();

        renderer.Begin();
        renderer.End();
        renderer.Begin();

        Assert.Equal(2, renderer.BeginCount);
    }

    [Fact]
    public void Begin_WithProjection_SetsCurrentProjection()
    {
        using var renderer = new MockTextRenderer();
        var projection = Matrix4x4.CreateOrthographic(800, 600, 0.1f, 100f);

        renderer.Begin(projection);

        Assert.Equal(projection, renderer.CurrentProjection);
    }

    [Fact]
    public void Begin_WhenAlreadyInBatch_ThrowsException()
    {
        using var renderer = new MockTextRenderer();
        renderer.Begin();

        Assert.Throws<InvalidOperationException>(() => renderer.Begin());
    }

    [Fact]
    public void End_SetsIsInBatchFalse()
    {
        using var renderer = new MockTextRenderer();
        renderer.Begin();

        renderer.End();

        Assert.False(renderer.IsInBatch);
    }

    [Fact]
    public void End_IncrementsEndCount()
    {
        using var renderer = new MockTextRenderer();
        renderer.Begin();
        renderer.End();
        renderer.Begin();
        renderer.End();

        Assert.Equal(2, renderer.EndCount);
    }

    [Fact]
    public void End_WhenNotInBatch_ThrowsException()
    {
        using var renderer = new MockTextRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.End());
    }

    [Fact]
    public void Flush_IncrementsFlushCount()
    {
        using var renderer = new MockTextRenderer();

        renderer.Flush();
        renderer.Flush();

        Assert.Equal(2, renderer.FlushCount);
    }

    #endregion

    #region DrawText

    [Fact]
    public void DrawText_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawText(font, "Hello", 100, 200, new Vector4(1, 1, 1, 1));

        Assert.Single(renderer.Commands);
        var cmd = Assert.IsType<DrawTextCommand>(renderer.Commands[0]);
        Assert.Equal("Hello", cmd.Text);
        Assert.Equal(new Vector2(100, 200), cmd.Position);
    }

    [Fact]
    public void DrawText_WithScale_RecordsCommandWithScale()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawText(font, "Text", 50, 60, new Vector4(1, 1, 1, 1), 2.0f);

        var cmd = Assert.IsType<DrawTextCommand>(renderer.Commands[0]);
        Assert.Equal(2.0f, cmd.Scale);
    }

    [Fact]
    public void DrawText_WithAlignment_RecordsAlignment()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawText(font, "Centered", 0, 0, new Vector4(1, 1, 1, 1), TextAlignH.Center, TextAlignV.Middle);

        var cmd = Assert.IsType<DrawTextCommand>(renderer.Commands[0]);
        Assert.Equal(TextAlignH.Center, cmd.AlignH);
        Assert.Equal(TextAlignV.Middle, cmd.AlignV);
    }

    [Fact]
    public void DrawText_IncrementsCurrentBatchSize()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();

        renderer.DrawText(font, "One", 0, 0, new Vector4(1, 1, 1, 1));
        renderer.DrawText(font, "Two", 0, 20, new Vector4(1, 1, 1, 1));

        Assert.Equal(2, renderer.CurrentBatchSize);
    }

    #endregion

    #region DrawTextRotated

    [Fact]
    public void DrawTextRotated_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawTextRotated(font, "Rotated", 100, 100, new Vector4(1, 0, 0, 1), MathF.PI / 4);

        Assert.Single(renderer.Commands);
        var cmd = Assert.IsType<DrawTextRotatedCommand>(renderer.Commands[0]);
        Assert.Equal("Rotated", cmd.Text);
        Assert.Equal(MathF.PI / 4, cmd.Rotation);
    }

    [Fact]
    public void DrawTextRotated_RecordsAllProperties()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);

        renderer.DrawTextRotated(font, "Test", 50, 75, color, 1.5f, TextAlignH.Right, TextAlignV.Bottom);

        var cmd = Assert.IsType<DrawTextRotatedCommand>(renderer.Commands[0]);
        Assert.Equal(font, cmd.Font);
        Assert.Equal(new Vector2(50, 75), cmd.Position);
        Assert.Equal(color, cmd.Color);
        Assert.Equal(1.5f, cmd.Rotation);
        Assert.Equal(TextAlignH.Right, cmd.AlignH);
        Assert.Equal(TextAlignV.Bottom, cmd.AlignV);
    }

    #endregion

    #region DrawTextWrapped

    [Fact]
    public void DrawTextWrapped_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var bounds = new Rectangle(10, 20, 200, 100);

        renderer.DrawTextWrapped(font, "Wrapped text that might span multiple lines", bounds, new Vector4(1, 1, 1, 1));

        Assert.Single(renderer.Commands);
        var cmd = Assert.IsType<DrawTextWrappedCommand>(renderer.Commands[0]);
        Assert.Equal(bounds, cmd.Bounds);
    }

    [Fact]
    public void DrawTextWrapped_RecordsAlignment()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var bounds = new Rectangle(0, 0, 100, 50);

        renderer.DrawTextWrapped(font, "Test", bounds, new Vector4(1, 1, 1, 1), TextAlignH.Center, TextAlignV.Middle);

        var cmd = Assert.IsType<DrawTextWrappedCommand>(renderer.Commands[0]);
        Assert.Equal(TextAlignH.Center, cmd.AlignH);
        Assert.Equal(TextAlignV.Middle, cmd.AlignV);
    }

    #endregion

    #region DrawTextOutlined

    [Fact]
    public void DrawTextOutlined_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var textColor = new Vector4(1, 1, 1, 1);
        var outlineColor = new Vector4(0, 0, 0, 1);

        renderer.DrawTextOutlined(font, "Outlined", 100, 50, textColor, outlineColor, 2f);

        Assert.Single(renderer.Commands);
        var cmd = Assert.IsType<DrawTextOutlinedCommand>(renderer.Commands[0]);
        Assert.Equal("Outlined", cmd.Text);
        Assert.Equal(textColor, cmd.TextColor);
        Assert.Equal(outlineColor, cmd.OutlineColor);
        Assert.Equal(2f, cmd.OutlineWidth);
    }

    [Fact]
    public void DrawTextOutlined_RecordsAllProperties()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawTextOutlined(font, "Title", 200, 100, new Vector4(1, 1, 0, 1), new Vector4(0, 0, 0, 1), 3f, TextAlignH.Center, TextAlignV.Top);

        var cmd = Assert.IsType<DrawTextOutlinedCommand>(renderer.Commands[0]);
        Assert.Equal(font, cmd.Font);
        Assert.Equal(new Vector2(200, 100), cmd.Position);
        Assert.Equal(TextAlignH.Center, cmd.AlignH);
        Assert.Equal(TextAlignV.Top, cmd.AlignV);
    }

    #endregion

    #region DrawTextShadowed

    [Fact]
    public void DrawTextShadowed_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var textColor = new Vector4(1, 1, 1, 1);
        var shadowColor = new Vector4(0, 0, 0, 0.5f);
        var shadowOffset = new Vector2(2, 2);

        renderer.DrawTextShadowed(font, "Shadowed", 100, 50, textColor, shadowColor, shadowOffset);

        Assert.Single(renderer.Commands);
        var cmd = Assert.IsType<DrawTextShadowedCommand>(renderer.Commands[0]);
        Assert.Equal("Shadowed", cmd.Text);
        Assert.Equal(textColor, cmd.TextColor);
        Assert.Equal(shadowColor, cmd.ShadowColor);
        Assert.Equal(shadowOffset, cmd.ShadowOffset);
    }

    [Fact]
    public void DrawTextShadowed_RecordsAllProperties()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawTextShadowed(font, "Drop Shadow", 50, 75, new Vector4(1, 1, 1, 1), new Vector4(0, 0, 0, 1), new Vector2(3, 3), TextAlignH.Right, TextAlignV.Bottom);

        var cmd = Assert.IsType<DrawTextShadowedCommand>(renderer.Commands[0]);
        Assert.Equal(font, cmd.Font);
        Assert.Equal(new Vector2(50, 75), cmd.Position);
        Assert.Equal(TextAlignH.Right, cmd.AlignH);
        Assert.Equal(TextAlignV.Bottom, cmd.AlignV);
    }

    #endregion

    #region Test Control

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();
        renderer.DrawText(font, "Test", 0, 0, new Vector4(1, 1, 1, 1));
        renderer.End();
        renderer.Flush();

        renderer.Reset();

        Assert.Empty(renderer.Commands);
        Assert.False(renderer.IsInBatch);
        Assert.Equal(0, renderer.BeginCount);
        Assert.Equal(0, renderer.EndCount);
        Assert.Equal(0, renderer.FlushCount);
        Assert.Equal(0, renderer.CurrentBatchSize);
        Assert.Equal(Matrix4x4.Identity, renderer.CurrentProjection);
    }

    [Fact]
    public void ClearCommands_ClearsOnlyCommands()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();
        renderer.DrawText(font, "Test", 0, 0, new Vector4(1, 1, 1, 1));

        renderer.ClearCommands();

        Assert.Empty(renderer.Commands);
        Assert.Equal(0, renderer.CurrentBatchSize);
        Assert.Equal(1, renderer.BeginCount); // Preserved
        Assert.True(renderer.IsInBatch); // Preserved
    }

    [Fact]
    public void Dispose_ResetsState()
    {
        var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawText(font, "Test", 0, 0, new Vector4(1, 1, 1, 1));

        renderer.Dispose();

        Assert.Empty(renderer.Commands);
    }

    #endregion
}

#region Command Type Tests

public class DrawTextCommandTests
{
    [Fact]
    public void DrawTextCommand_StoresAllProperties()
    {
        var font = new FontHandle(1);
        var position = new Vector2(100, 200);
        var color = new Vector4(1, 0, 0, 1);

        var cmd = new DrawTextCommand(font, "Hello", position, color, 1.5f, TextAlignH.Center, TextAlignV.Middle);

        Assert.Equal(font, cmd.Font);
        Assert.Equal("Hello", cmd.Text);
        Assert.Equal(position, cmd.Position);
        Assert.Equal(color, cmd.Color);
        Assert.Equal(1.5f, cmd.Scale);
        Assert.Equal(TextAlignH.Center, cmd.AlignH);
        Assert.Equal(TextAlignV.Middle, cmd.AlignV);
    }
}

public class DrawTextRotatedCommandTests
{
    [Fact]
    public void DrawTextRotatedCommand_StoresAllProperties()
    {
        var font = new FontHandle(2);
        var position = new Vector2(50, 75);
        var color = new Vector4(0, 1, 0, 1);

        var cmd = new DrawTextRotatedCommand(font, "Rotated", position, color, 1.57f, TextAlignH.Right, TextAlignV.Bottom);

        Assert.Equal(font, cmd.Font);
        Assert.Equal("Rotated", cmd.Text);
        Assert.Equal(position, cmd.Position);
        Assert.Equal(color, cmd.Color);
        Assert.Equal(1.57f, cmd.Rotation);
        Assert.Equal(TextAlignH.Right, cmd.AlignH);
        Assert.Equal(TextAlignV.Bottom, cmd.AlignV);
    }
}

public class DrawTextWrappedCommandTests
{
    [Fact]
    public void DrawTextWrappedCommand_StoresAllProperties()
    {
        var font = new FontHandle(3);
        var bounds = new Rectangle(10, 20, 300, 200);
        var color = new Vector4(0, 0, 1, 1);

        var cmd = new DrawTextWrappedCommand(font, "Wrapped text", bounds, color, TextAlignH.Left, TextAlignV.Top);

        Assert.Equal(font, cmd.Font);
        Assert.Equal("Wrapped text", cmd.Text);
        Assert.Equal(bounds, cmd.Bounds);
        Assert.Equal(color, cmd.Color);
        Assert.Equal(TextAlignH.Left, cmd.AlignH);
        Assert.Equal(TextAlignV.Top, cmd.AlignV);
    }
}

public class DrawTextOutlinedCommandTests
{
    [Fact]
    public void DrawTextOutlinedCommand_StoresAllProperties()
    {
        var font = new FontHandle(4);
        var position = new Vector2(100, 100);
        var textColor = new Vector4(1, 1, 1, 1);
        var outlineColor = new Vector4(0, 0, 0, 1);

        var cmd = new DrawTextOutlinedCommand(font, "Outlined", position, textColor, outlineColor, 2f, TextAlignH.Center, TextAlignV.Middle);

        Assert.Equal(font, cmd.Font);
        Assert.Equal("Outlined", cmd.Text);
        Assert.Equal(position, cmd.Position);
        Assert.Equal(textColor, cmd.TextColor);
        Assert.Equal(outlineColor, cmd.OutlineColor);
        Assert.Equal(2f, cmd.OutlineWidth);
        Assert.Equal(TextAlignH.Center, cmd.AlignH);
        Assert.Equal(TextAlignV.Middle, cmd.AlignV);
    }
}

public class DrawTextShadowedCommandTests
{
    [Fact]
    public void DrawTextShadowedCommand_StoresAllProperties()
    {
        var font = new FontHandle(5);
        var position = new Vector2(200, 150);
        var textColor = new Vector4(1, 1, 0, 1);
        var shadowColor = new Vector4(0, 0, 0, 0.5f);
        var shadowOffset = new Vector2(3, 3);

        var cmd = new DrawTextShadowedCommand(font, "Shadowed", position, textColor, shadowColor, shadowOffset, TextAlignH.Right, TextAlignV.Top);

        Assert.Equal(font, cmd.Font);
        Assert.Equal("Shadowed", cmd.Text);
        Assert.Equal(position, cmd.Position);
        Assert.Equal(textColor, cmd.TextColor);
        Assert.Equal(shadowColor, cmd.ShadowColor);
        Assert.Equal(shadowOffset, cmd.ShadowOffset);
        Assert.Equal(TextAlignH.Right, cmd.AlignH);
        Assert.Equal(TextAlignV.Top, cmd.AlignV);
    }
}

#endregion
