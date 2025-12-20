using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockTextRendererTests
{
    #region Batch Control

    [Fact]
    public void Begin_SetsIsInBatch()
    {
        using var renderer = new MockTextRenderer();

        renderer.Begin();

        renderer.IsInBatch.ShouldBeTrue();
    }

    [Fact]
    public void Begin_IncrementsBeginCount()
    {
        using var renderer = new MockTextRenderer();

        renderer.Begin();

        renderer.BeginCount.ShouldBe(1);
    }

    [Fact]
    public void Begin_WithProjection_SetsProjection()
    {
        using var renderer = new MockTextRenderer();
        var projection = Matrix4x4.CreateOrthographic(800, 600, 0.1f, 100f);

        renderer.Begin(projection);

        renderer.CurrentProjection.ShouldBe(projection);
    }

    [Fact]
    public void Begin_WhenAlreadyInBatch_Throws()
    {
        using var renderer = new MockTextRenderer();
        renderer.Begin();

        Should.Throw<InvalidOperationException>(() => renderer.Begin());
    }

    [Fact]
    public void End_ClearsIsInBatch()
    {
        using var renderer = new MockTextRenderer();
        renderer.Begin();

        renderer.End();

        renderer.IsInBatch.ShouldBeFalse();
    }

    [Fact]
    public void End_IncrementsEndCount()
    {
        using var renderer = new MockTextRenderer();
        renderer.Begin();

        renderer.End();

        renderer.EndCount.ShouldBe(1);
    }

    [Fact]
    public void End_WhenNotInBatch_Throws()
    {
        using var renderer = new MockTextRenderer();

        Should.Throw<InvalidOperationException>(() => renderer.End());
    }

    [Fact]
    public void Flush_IncrementsFlushCount()
    {
        using var renderer = new MockTextRenderer();

        renderer.Flush();

        renderer.FlushCount.ShouldBe(1);
    }

    #endregion

    #region DrawText Commands

    [Fact]
    public void DrawText_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawText(font, "Hello", 100, 200, new Vector4(1, 1, 1, 1));

        renderer.Commands.Count.ShouldBe(1);
        var command = renderer.Commands[0].ShouldBeOfType<DrawTextCommand>();
        command.Text.ShouldBe("Hello");
        command.Position.ShouldBe(new Vector2(100, 200));
        command.Font.ShouldBe(font);
    }

    [Fact]
    public void DrawText_WithScale_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawText(font, "Hello", 100, 200, new Vector4(1, 1, 1, 1), 2f);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextCommand>();
        command.Scale.ShouldBe(2f);
    }

    [Fact]
    public void DrawText_WithAlignment_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawText(font, "Hello", 100, 200, new Vector4(1, 1, 1, 1), 1f, TextAlignH.Center, TextAlignV.Middle);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextCommand>();
        command.AlignH.ShouldBe(TextAlignH.Center);
        command.AlignV.ShouldBe(TextAlignV.Middle);
    }

    #endregion

    #region DrawTextRotated Commands

    [Fact]
    public void DrawTextRotated_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawTextRotated(font, "Rotated", 100, 100, new Vector4(1, 1, 1, 1), MathF.PI / 4);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextRotatedCommand>();
        command.Text.ShouldBe("Rotated");
        command.Rotation.ShouldBe(MathF.PI / 4, tolerance: 0.0001f);
    }

    [Fact]
    public void DrawTextRotated_WithAlignment_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawTextRotated(font, "Rotated", 100, 100, new Vector4(1, 1, 1, 1), MathF.PI / 4, TextAlignH.Right, TextAlignV.Bottom);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextRotatedCommand>();
        command.AlignH.ShouldBe(TextAlignH.Right);
        command.AlignV.ShouldBe(TextAlignV.Bottom);
    }

    #endregion

    #region DrawTextWrapped Commands

    [Fact]
    public void DrawTextWrapped_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var bounds = new Rectangle(10, 10, 200, 100);

        renderer.DrawTextWrapped(font, "This is wrapped text", bounds, new Vector4(1, 1, 1, 1));

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextWrappedCommand>();
        command.Text.ShouldBe("This is wrapped text");
        command.Bounds.ShouldBe(bounds);
    }

    [Fact]
    public void DrawTextWrapped_WithAlignment_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var bounds = new Rectangle(10, 10, 200, 100);

        renderer.DrawTextWrapped(font, "Wrapped", bounds, new Vector4(1, 1, 1, 1), TextAlignH.Center, TextAlignV.Middle);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextWrappedCommand>();
        command.AlignH.ShouldBe(TextAlignH.Center);
        command.AlignV.ShouldBe(TextAlignV.Middle);
    }

    #endregion

    #region DrawTextOutlined Commands

    [Fact]
    public void DrawTextOutlined_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var textColor = new Vector4(1, 1, 1, 1);
        var outlineColor = new Vector4(0, 0, 0, 1);

        renderer.DrawTextOutlined(font, "Outlined", 100, 100, textColor, outlineColor, 2f);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextOutlinedCommand>();
        command.Text.ShouldBe("Outlined");
        command.TextColor.ShouldBe(textColor);
        command.OutlineColor.ShouldBe(outlineColor);
        command.OutlineWidth.ShouldBe(2f);
    }

    [Fact]
    public void DrawTextOutlined_WithAlignment_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawTextOutlined(font, "Outlined", 100, 100, Vector4.One, Vector4.Zero, 1f, TextAlignH.Center, TextAlignV.Top);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextOutlinedCommand>();
        command.AlignH.ShouldBe(TextAlignH.Center);
        command.AlignV.ShouldBe(TextAlignV.Top);
    }

    #endregion

    #region DrawTextShadowed Commands

    [Fact]
    public void DrawTextShadowed_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        var textColor = new Vector4(1, 1, 1, 1);
        var shadowColor = new Vector4(0, 0, 0, 0.5f);
        var shadowOffset = new Vector2(2, 2);

        renderer.DrawTextShadowed(font, "Shadowed", 100, 100, textColor, shadowColor, shadowOffset);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextShadowedCommand>();
        command.Text.ShouldBe("Shadowed");
        command.TextColor.ShouldBe(textColor);
        command.ShadowColor.ShouldBe(shadowColor);
        command.ShadowOffset.ShouldBe(shadowOffset);
    }

    [Fact]
    public void DrawTextShadowed_WithAlignment_RecordsCommand()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);

        renderer.DrawTextShadowed(font, "Shadowed", 100, 100, Vector4.One, Vector4.Zero, new Vector2(2, 2), TextAlignH.Right, TextAlignV.Bottom);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextShadowedCommand>();
        command.AlignH.ShouldBe(TextAlignH.Right);
        command.AlignV.ShouldBe(TextAlignV.Bottom);
    }

    #endregion

    #region CurrentBatchSize

    [Fact]
    public void CurrentBatchSize_TracksCommandsInBatch()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();

        renderer.DrawText(font, "One", 0, 0, Vector4.One);
        renderer.DrawText(font, "Two", 0, 20, Vector4.One);

        renderer.CurrentBatchSize.ShouldBe(2);
    }

    [Fact]
    public void Begin_ResetsBatchSize()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();
        renderer.DrawText(font, "Text", 0, 0, Vector4.One);
        renderer.End();

        renderer.Begin();

        renderer.CurrentBatchSize.ShouldBe(0);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();
        renderer.DrawText(font, "Text", 0, 0, Vector4.One);
        renderer.End();

        renderer.Reset();

        renderer.Commands.ShouldBeEmpty();
        renderer.BeginCount.ShouldBe(0);
        renderer.EndCount.ShouldBe(0);
        renderer.FlushCount.ShouldBe(0);
        renderer.IsInBatch.ShouldBeFalse();
    }

    [Fact]
    public void ClearCommands_ClearsOnlyCommands()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();
        renderer.DrawText(font, "Text", 0, 0, Vector4.One);

        renderer.ClearCommands();

        renderer.Commands.ShouldBeEmpty();
        renderer.BeginCount.ShouldBe(1);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var renderer = new MockTextRenderer();

        Should.NotThrow(() =>
        {
            renderer.Dispose();
            renderer.Dispose();
        });
    }

    [Fact]
    public void Dispose_ResetsState()
    {
        var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.Begin();
        renderer.DrawText(font, "Text", 0, 0, Vector4.One);

        renderer.Dispose();

        renderer.Commands.ShouldBeEmpty();
    }

    #endregion
}
