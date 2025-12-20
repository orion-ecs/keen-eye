using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class Mock2DRendererTests
{
    #region Batch Control

    [Fact]
    public void Begin_SetsIsInBatch()
    {
        using var renderer = new Mock2DRenderer();

        renderer.Begin();

        renderer.IsInBatch.ShouldBeTrue();
    }

    [Fact]
    public void Begin_IncrementsBeginCount()
    {
        using var renderer = new Mock2DRenderer();

        renderer.Begin();

        renderer.BeginCount.ShouldBe(1);
    }

    [Fact]
    public void Begin_WithProjection_SetsProjection()
    {
        using var renderer = new Mock2DRenderer();
        var projection = Matrix4x4.CreateOrthographic(800, 600, 0.1f, 100f);

        renderer.Begin(projection);

        renderer.CurrentProjection.ShouldBe(projection);
    }

    [Fact]
    public void Begin_WhenAlreadyInBatch_Throws()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        Should.Throw<InvalidOperationException>(() => renderer.Begin());
    }

    [Fact]
    public void End_ClearsIsInBatch()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        renderer.End();

        renderer.IsInBatch.ShouldBeFalse();
    }

    [Fact]
    public void End_IncrementsEndCount()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        renderer.End();

        renderer.EndCount.ShouldBe(1);
    }

    [Fact]
    public void End_WhenNotInBatch_Throws()
    {
        using var renderer = new Mock2DRenderer();

        Should.Throw<InvalidOperationException>(() => renderer.End());
    }

    [Fact]
    public void Flush_IncrementsFlushCount()
    {
        using var renderer = new Mock2DRenderer();

        renderer.Flush();
        renderer.Flush();

        renderer.FlushCount.ShouldBe(2);
    }

    #endregion

    #region Rectangle Commands

    [Fact]
    public void FillRect_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var color = new Vector4(1, 0, 0, 1);

        renderer.FillRect(10, 20, 100, 50, color);

        renderer.Commands.Count.ShouldBe(1);
        var command = renderer.Commands[0].ShouldBeOfType<FillRectCommand>();
        command.Rect.X.ShouldBe(10);
        command.Rect.Y.ShouldBe(20);
        command.Rect.Width.ShouldBe(100);
        command.Rect.Height.ShouldBe(50);
        command.Color.ShouldBe(color);
    }

    [Fact]
    public void FillRect_WithRectangle_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var rect = new Rectangle(10, 20, 100, 50);
        var color = new Vector4(0, 1, 0, 1);

        renderer.FillRect(rect, color);

        var command = renderer.Commands[0].ShouldBeOfType<FillRectCommand>();
        command.Rect.ShouldBe(rect);
    }

    [Fact]
    public void DrawRect_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawRect(10, 20, 100, 50, new Vector4(1, 1, 1, 1), 2f);

        var command = renderer.Commands[0].ShouldBeOfType<DrawRectCommand>();
        command.Thickness.ShouldBe(2f);
    }

    [Fact]
    public void FillRoundedRect_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.FillRoundedRect(10, 20, 100, 50, 5f, new Vector4(1, 0, 0, 1));

        var command = renderer.Commands[0].ShouldBeOfType<FillRoundedRectCommand>();
        command.Radius.ShouldBe(5f);
    }

    [Fact]
    public void DrawRoundedRect_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawRoundedRect(10, 20, 100, 50, 5f, new Vector4(1, 0, 0, 1), 2f);

        var command = renderer.Commands[0].ShouldBeOfType<DrawRoundedRectCommand>();
        command.Radius.ShouldBe(5f);
        command.Thickness.ShouldBe(2f);
    }

    #endregion

    #region Line Commands

    [Fact]
    public void DrawLine_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawLine(0, 0, 100, 100, new Vector4(1, 1, 1, 1), 2f);

        var command = renderer.Commands[0].ShouldBeOfType<DrawLineCommand>();
        command.Start.ShouldBe(new Vector2(0, 0));
        command.End.ShouldBe(new Vector2(100, 100));
        command.Thickness.ShouldBe(2f);
    }

    [Fact]
    public void DrawLine_WithVectors_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawLine(new Vector2(10, 20), new Vector2(30, 40), new Vector4(1, 0, 0, 1));

        var command = renderer.Commands[0].ShouldBeOfType<DrawLineCommand>();
        command.Start.ShouldBe(new Vector2(10, 20));
        command.End.ShouldBe(new Vector2(30, 40));
    }

    [Fact]
    public void DrawLineStrip_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var points = new Vector2[] { new(0, 0), new(10, 10), new(20, 0) };

        renderer.DrawLineStrip(points, new Vector4(1, 1, 1, 1));

        var command = renderer.Commands[0].ShouldBeOfType<DrawLineStripCommand>();
        command.Points.Length.ShouldBe(3);
    }

    [Fact]
    public void DrawPolygon_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var points = new Vector2[] { new(0, 0), new(10, 0), new(5, 10) };

        renderer.DrawPolygon(points, new Vector4(1, 1, 1, 1), 2f);

        var command = renderer.Commands[0].ShouldBeOfType<DrawPolygonCommand>();
        command.Points.Length.ShouldBe(3);
        command.Thickness.ShouldBe(2f);
    }

    #endregion

    #region Circle and Ellipse Commands

    [Fact]
    public void FillCircle_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.FillCircle(100, 100, 50, new Vector4(1, 0, 0, 1), 64);

        var command = renderer.Commands[0].ShouldBeOfType<FillCircleCommand>();
        command.Center.ShouldBe(new Vector2(100, 100));
        command.Radius.ShouldBe(50);
        command.Segments.ShouldBe(64);
    }

    [Fact]
    public void DrawCircle_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawCircle(100, 100, 50, new Vector4(1, 1, 1, 1), 2f, 32);

        var command = renderer.Commands[0].ShouldBeOfType<DrawCircleCommand>();
        command.Thickness.ShouldBe(2f);
        command.Segments.ShouldBe(32);
    }

    [Fact]
    public void FillEllipse_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.FillEllipse(100, 100, 60, 40, new Vector4(0, 1, 0, 1));

        var command = renderer.Commands[0].ShouldBeOfType<FillEllipseCommand>();
        command.RadiusX.ShouldBe(60);
        command.RadiusY.ShouldBe(40);
    }

    [Fact]
    public void DrawEllipse_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawEllipse(100, 100, 60, 40, new Vector4(0, 0, 1, 1), 2f);

        var command = renderer.Commands[0].ShouldBeOfType<DrawEllipseCommand>();
        command.Thickness.ShouldBe(2f);
    }

    #endregion

    #region Texture Commands

    [Fact]
    public void DrawTexture_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);

        renderer.DrawTexture(texture, 10, 20);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextureCommand>();
        command.Texture.ShouldBe(texture);
        command.DestRect.X.ShouldBe(10);
        command.DestRect.Y.ShouldBe(20);
    }

    [Fact]
    public void DrawTexture_WithSize_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);

        renderer.DrawTexture(texture, 10, 20, 100, 50, new Vector4(1, 1, 1, 0.5f));

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextureCommand>();
        command.DestRect.Width.ShouldBe(100);
        command.DestRect.Height.ShouldBe(50);
        command.Tint.W.ShouldBe(0.5f);
    }

    [Fact]
    public void DrawTextureRegion_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);
        var dest = new Rectangle(0, 0, 100, 100);
        var source = new Rectangle(0, 0, 64, 64);

        renderer.DrawTextureRegion(texture, dest, source);

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextureRegionCommand>();
        command.DestRect.ShouldBe(dest);
        command.SourceRect.ShouldBe(source);
    }

    [Fact]
    public void DrawTextureRotated_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);
        var dest = new Rectangle(100, 100, 50, 50);

        renderer.DrawTextureRotated(texture, dest, MathF.PI / 4, new Vector2(25, 25));

        var command = renderer.Commands[0].ShouldBeOfType<DrawTextureRotatedCommand>();
        command.Rotation.ShouldBe(MathF.PI / 4, tolerance: 0.0001f);
        command.Origin.ShouldBe(new Vector2(25, 25));
    }

    #endregion

    #region Clipping

    [Fact]
    public void PushClip_SetsCurrentClip()
    {
        using var renderer = new Mock2DRenderer();
        var clip = new Rectangle(0, 0, 100, 100);

        renderer.PushClip(clip);

        renderer.CurrentClip.ShouldBe(clip);
        renderer.ClipStackDepth.ShouldBe(1);
    }

    [Fact]
    public void PushClip_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var clip = new Rectangle(0, 0, 100, 100);

        renderer.PushClip(clip);

        var command = renderer.Commands[0].ShouldBeOfType<PushClipCommand>();
        command.RequestedClip.ShouldBe(clip);
    }

    [Fact]
    public void PushClip_Nested_IntersectsClips()
    {
        using var renderer = new Mock2DRenderer();
        var outer = new Rectangle(0, 0, 100, 100);
        var inner = new Rectangle(50, 50, 100, 100);

        renderer.PushClip(outer);
        renderer.PushClip(inner);

        renderer.ClipStackDepth.ShouldBe(2);
        // Effective clip should be intersection
        var command = renderer.Commands[1].ShouldBeOfType<PushClipCommand>();
        command.EffectiveClip.X.ShouldBe(50);
        command.EffectiveClip.Y.ShouldBe(50);
    }

    [Fact]
    public void PopClip_RemovesClip()
    {
        using var renderer = new Mock2DRenderer();
        renderer.PushClip(new Rectangle(0, 0, 100, 100));

        renderer.PopClip();

        renderer.CurrentClip.ShouldBeNull();
        renderer.ClipStackDepth.ShouldBe(0);
    }

    [Fact]
    public void PopClip_WhenEmpty_Throws()
    {
        using var renderer = new Mock2DRenderer();

        Should.Throw<InvalidOperationException>(() => renderer.PopClip());
    }

    [Fact]
    public void ClearClip_ClearsStack()
    {
        using var renderer = new Mock2DRenderer();
        renderer.PushClip(new Rectangle(0, 0, 100, 100));
        renderer.PushClip(new Rectangle(10, 10, 50, 50));

        renderer.ClearClip();

        renderer.ClipStackDepth.ShouldBe(0);
    }

    #endregion

    #region Batch Hints

    [Fact]
    public void SetBatchHint_SetsBatchHint()
    {
        using var renderer = new Mock2DRenderer();

        renderer.SetBatchHint(100);

        renderer.BatchHint.ShouldBe(100);
    }

    #endregion

    #region CurrentBatchSize

    [Fact]
    public void CurrentBatchSize_TracksCommandsInBatch()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        renderer.FillRect(0, 0, 10, 10, Vector4.One);
        renderer.FillRect(0, 0, 10, 10, Vector4.One);

        renderer.CurrentBatchSize.ShouldBe(2);
    }

    [Fact]
    public void Begin_ResetsBatchSize()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();
        renderer.FillRect(0, 0, 10, 10, Vector4.One);
        renderer.End();

        renderer.Begin();

        renderer.CurrentBatchSize.ShouldBe(0);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();
        renderer.FillRect(0, 0, 10, 10, Vector4.One);
        renderer.PushClip(new Rectangle(0, 0, 100, 100));
        renderer.End();

        renderer.Reset();

        renderer.Commands.ShouldBeEmpty();
        renderer.ClipStackDepth.ShouldBe(0);
        renderer.BeginCount.ShouldBe(0);
        renderer.EndCount.ShouldBe(0);
        renderer.IsInBatch.ShouldBeFalse();
    }

    [Fact]
    public void ClearCommands_ClearsOnlyCommands()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();
        renderer.FillRect(0, 0, 10, 10, Vector4.One);

        renderer.ClearCommands();

        renderer.Commands.ShouldBeEmpty();
        renderer.BeginCount.ShouldBe(1);
    }

    #endregion
}
