using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class Mock2DRendererTests
{
    #region Construction and State

    [Fact]
    public void Constructor_CreatesRendererWithEmptyState()
    {
        using var renderer = new Mock2DRenderer();

        Assert.Empty(renderer.Commands);
        Assert.False(renderer.IsInBatch);
        Assert.Equal(0, renderer.BeginCount);
        Assert.Equal(0, renderer.EndCount);
        Assert.Equal(0, renderer.FlushCount);
        Assert.Equal(0, renderer.CurrentBatchSize);
        Assert.Null(renderer.CurrentClip);
        Assert.Equal(0, renderer.ClipStackDepth);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();
        renderer.FillRect(0, 0, 100, 100, Vector4.One);
        renderer.End();

        renderer.Reset();

        Assert.Empty(renderer.Commands);
        Assert.False(renderer.IsInBatch);
        Assert.Equal(0, renderer.BeginCount);
        Assert.Equal(0, renderer.EndCount);
        Assert.Equal(0, renderer.CurrentBatchSize);
    }

    [Fact]
    public void ClearCommands_ClearsOnlyCommands()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();
        renderer.FillRect(0, 0, 100, 100, Vector4.One);

        renderer.ClearCommands();

        Assert.Empty(renderer.Commands);
        Assert.True(renderer.IsInBatch);
        Assert.Equal(1, renderer.BeginCount);
    }

    #endregion

    #region Batch Control

    [Fact]
    public void Begin_StartsNewBatch()
    {
        using var renderer = new Mock2DRenderer();

        renderer.Begin();

        Assert.True(renderer.IsInBatch);
        Assert.Equal(1, renderer.BeginCount);
        Assert.Equal(Matrix4x4.Identity, renderer.CurrentProjection);
    }

    [Fact]
    public void Begin_WithProjection_SetsProjection()
    {
        using var renderer = new Mock2DRenderer();
        var projection = Matrix4x4.CreateOrthographic(800, 600, 0.1f, 100f);

        renderer.Begin(projection);

        Assert.Equal(projection, renderer.CurrentProjection);
    }

    [Fact]
    public void Begin_WhenAlreadyInBatch_ThrowsInvalidOperationException()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        Assert.Throws<InvalidOperationException>(() => renderer.Begin());
    }

    [Fact]
    public void End_EndsBatch()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        renderer.End();

        Assert.False(renderer.IsInBatch);
        Assert.Equal(1, renderer.EndCount);
    }

    [Fact]
    public void End_WhenNotInBatch_ThrowsInvalidOperationException()
    {
        using var renderer = new Mock2DRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.End());
    }

    [Fact]
    public void Flush_IncrementsFlushCount()
    {
        using var renderer = new Mock2DRenderer();

        renderer.Flush();
        renderer.Flush();

        Assert.Equal(2, renderer.FlushCount);
    }

    [Fact]
    public void SetBatchHint_SetsHint()
    {
        using var renderer = new Mock2DRenderer();

        renderer.SetBatchHint(100);

        Assert.Equal(100, renderer.BatchHint);
    }

    #endregion

    #region Rectangles

    [Fact]
    public void FillRect_WithCoordinates_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var color = new Vector4(1, 0, 0, 1);

        renderer.FillRect(10, 20, 100, 50, color);

        Assert.Single(renderer.Commands);
        var cmd = Assert.IsType<FillRectCommand>(renderer.Commands[0]);
        Assert.Equal(10, cmd.Rect.X);
        Assert.Equal(20, cmd.Rect.Y);
        Assert.Equal(100, cmd.Rect.Width);
        Assert.Equal(50, cmd.Rect.Height);
        Assert.Equal(color, cmd.Color);
    }

    [Fact]
    public void FillRect_WithRectangle_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var rect = new Rectangle(10, 20, 100, 50);
        var color = new Vector4(0, 1, 0, 1);

        renderer.FillRect(rect, color);

        var cmd = Assert.IsType<FillRectCommand>(renderer.Commands[0]);
        Assert.Equal(rect, cmd.Rect);
    }

    [Fact]
    public void DrawRect_WithCoordinates_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var color = new Vector4(0, 0, 1, 1);

        renderer.DrawRect(10, 20, 100, 50, color, 2f);

        var cmd = Assert.IsType<DrawRectCommand>(renderer.Commands[0]);
        Assert.Equal(2f, cmd.Thickness);
    }

    [Fact]
    public void DrawRect_WithRectangle_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var rect = new Rectangle(10, 20, 100, 50);

        renderer.DrawRect(rect, Vector4.One, 3f);

        var cmd = Assert.IsType<DrawRectCommand>(renderer.Commands[0]);
        Assert.Equal(3f, cmd.Thickness);
    }

    [Fact]
    public void FillRoundedRect_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var color = new Vector4(1, 1, 0, 1);

        renderer.FillRoundedRect(10, 20, 100, 50, 5f, color);

        var cmd = Assert.IsType<FillRoundedRectCommand>(renderer.Commands[0]);
        Assert.Equal(5f, cmd.Radius);
        Assert.Equal(color, cmd.Color);
    }

    [Fact]
    public void DrawRoundedRect_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawRoundedRect(10, 20, 100, 50, 8f, Vector4.One, 2f);

        var cmd = Assert.IsType<DrawRoundedRectCommand>(renderer.Commands[0]);
        Assert.Equal(8f, cmd.Radius);
        Assert.Equal(2f, cmd.Thickness);
    }

    #endregion

    #region Lines

    [Fact]
    public void DrawLine_WithCoordinates_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var color = new Vector4(1, 0, 1, 1);

        renderer.DrawLine(0, 0, 100, 100, color, 2f);

        var cmd = Assert.IsType<DrawLineCommand>(renderer.Commands[0]);
        Assert.Equal(new Vector2(0, 0), cmd.Start);
        Assert.Equal(new Vector2(100, 100), cmd.End);
        Assert.Equal(color, cmd.Color);
        Assert.Equal(2f, cmd.Thickness);
    }

    [Fact]
    public void DrawLine_WithVectors_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var start = new Vector2(10, 20);
        var end = new Vector2(50, 60);

        renderer.DrawLine(start, end, Vector4.One);

        var cmd = Assert.IsType<DrawLineCommand>(renderer.Commands[0]);
        Assert.Equal(start, cmd.Start);
        Assert.Equal(end, cmd.End);
    }

    [Fact]
    public void DrawLineStrip_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var points = new[] { new Vector2(0, 0), new Vector2(10, 10), new Vector2(20, 0) };

        renderer.DrawLineStrip(points, Vector4.One, 1.5f);

        var cmd = Assert.IsType<DrawLineStripCommand>(renderer.Commands[0]);
        Assert.Equal(3, cmd.Points.Length);
        Assert.Equal(1.5f, cmd.Thickness);
    }

    [Fact]
    public void DrawPolygon_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var points = new[] { new Vector2(0, 0), new Vector2(100, 0), new Vector2(50, 100) };

        renderer.DrawPolygon(points, Vector4.One, 2f);

        var cmd = Assert.IsType<DrawPolygonCommand>(renderer.Commands[0]);
        Assert.Equal(3, cmd.Points.Length);
        Assert.Equal(2f, cmd.Thickness);
    }

    #endregion

    #region Circles and Ellipses

    [Fact]
    public void FillCircle_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var color = new Vector4(0, 1, 1, 1);

        renderer.FillCircle(50, 50, 25, color, 16);

        var cmd = Assert.IsType<FillCircleCommand>(renderer.Commands[0]);
        Assert.Equal(new Vector2(50, 50), cmd.Center);
        Assert.Equal(25f, cmd.Radius);
        Assert.Equal(color, cmd.Color);
        Assert.Equal(16, cmd.Segments);
    }

    [Fact]
    public void DrawCircle_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawCircle(50, 50, 25, Vector4.One, 2f, 24);

        var cmd = Assert.IsType<DrawCircleCommand>(renderer.Commands[0]);
        Assert.Equal(25f, cmd.Radius);
        Assert.Equal(2f, cmd.Thickness);
        Assert.Equal(24, cmd.Segments);
    }

    [Fact]
    public void FillEllipse_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.FillEllipse(100, 100, 40, 20, Vector4.One, 32);

        var cmd = Assert.IsType<FillEllipseCommand>(renderer.Commands[0]);
        Assert.Equal(new Vector2(100, 100), cmd.Center);
        Assert.Equal(40f, cmd.RadiusX);
        Assert.Equal(20f, cmd.RadiusY);
        Assert.Equal(32, cmd.Segments);
    }

    [Fact]
    public void DrawEllipse_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();

        renderer.DrawEllipse(100, 100, 40, 20, Vector4.One, 1.5f, 48);

        var cmd = Assert.IsType<DrawEllipseCommand>(renderer.Commands[0]);
        Assert.Equal(40f, cmd.RadiusX);
        Assert.Equal(20f, cmd.RadiusY);
        Assert.Equal(1.5f, cmd.Thickness);
        Assert.Equal(48, cmd.Segments);
    }

    #endregion

    #region Textures

    [Fact]
    public void DrawTexture_WithPosition_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);

        renderer.DrawTexture(texture, 10, 20);

        var cmd = Assert.IsType<DrawTextureCommand>(renderer.Commands[0]);
        Assert.Equal(texture, cmd.Texture);
        Assert.Equal(10, cmd.DestRect.X);
        Assert.Equal(20, cmd.DestRect.Y);
        Assert.Equal(Vector4.One, cmd.Tint);
    }

    [Fact]
    public void DrawTexture_WithSizeAndTint_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);
        var tint = new Vector4(1, 0.5f, 0.5f, 1);

        renderer.DrawTexture(texture, 10, 20, 100, 100, tint);

        var cmd = Assert.IsType<DrawTextureCommand>(renderer.Commands[0]);
        Assert.Equal(100, cmd.DestRect.Width);
        Assert.Equal(100, cmd.DestRect.Height);
        Assert.Equal(tint, cmd.Tint);
    }

    [Fact]
    public void DrawTextureRegion_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);
        var dest = new Rectangle(0, 0, 64, 64);
        var source = new Rectangle(0, 0, 32, 32);

        renderer.DrawTextureRegion(texture, dest, source);

        var cmd = Assert.IsType<DrawTextureRegionCommand>(renderer.Commands[0]);
        Assert.Equal(dest, cmd.DestRect);
        Assert.Equal(source, cmd.SourceRect);
    }

    [Fact]
    public void DrawTextureRotated_RecordsCommand()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);
        var dest = new Rectangle(100, 100, 64, 64);
        var origin = new Vector2(32, 32);

        renderer.DrawTextureRotated(texture, dest, 1.57f, origin);

        var cmd = Assert.IsType<DrawTextureRotatedCommand>(renderer.Commands[0]);
        Assert.Equal(dest, cmd.DestRect);
        Assert.Equal(1.57f, cmd.Rotation, 0.01f);
        Assert.Equal(origin, cmd.Origin);
    }

    #endregion

    #region Clipping

    [Fact]
    public void PushClip_RecordsCommandAndUpdatesState()
    {
        using var renderer = new Mock2DRenderer();
        var clip = new Rectangle(10, 10, 100, 100);

        renderer.PushClip(clip);

        Assert.Single(renderer.Commands);
        var cmd = Assert.IsType<PushClipCommand>(renderer.Commands[0]);
        Assert.Equal(clip, cmd.RequestedClip);
        Assert.Equal(clip, cmd.EffectiveClip);
        Assert.Equal(clip, renderer.CurrentClip);
        Assert.Equal(1, renderer.ClipStackDepth);
    }

    [Fact]
    public void PushClip_NestedClips_IntersectsClipRectangles()
    {
        using var renderer = new Mock2DRenderer();
        var outer = new Rectangle(0, 0, 100, 100);
        var inner = new Rectangle(50, 50, 100, 100);

        renderer.PushClip(outer);
        renderer.PushClip(inner);

        var cmd = Assert.IsType<PushClipCommand>(renderer.Commands[1]);
        Assert.Equal(inner, cmd.RequestedClip);
        // Effective should be intersection: (50,50,50,50)
        var effective = cmd.EffectiveClip;
        Assert.Equal(50, effective.X);
        Assert.Equal(50, effective.Y);
        Assert.Equal(50, effective.Width);
        Assert.Equal(50, effective.Height);
        Assert.Equal(2, renderer.ClipStackDepth);
    }

    [Fact]
    public void PopClip_RecordsCommandAndUpdatesState()
    {
        using var renderer = new Mock2DRenderer();
        renderer.PushClip(new Rectangle(0, 0, 100, 100));

        renderer.PopClip();

        Assert.Equal(2, renderer.Commands.Count);
        Assert.IsType<PopClipCommand>(renderer.Commands[1]);
        Assert.Null(renderer.CurrentClip);
        Assert.Equal(0, renderer.ClipStackDepth);
    }

    [Fact]
    public void PopClip_WithNoClip_ThrowsInvalidOperationException()
    {
        using var renderer = new Mock2DRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.PopClip());
    }

    [Fact]
    public void ClearClip_RecordsCommandAndClearsStack()
    {
        using var renderer = new Mock2DRenderer();
        renderer.PushClip(new Rectangle(0, 0, 100, 100));
        renderer.PushClip(new Rectangle(10, 10, 50, 50));

        renderer.ClearClip();

        Assert.IsType<ClearClipCommand>(renderer.Commands[^1]);
        Assert.Null(renderer.CurrentClip);
        Assert.Equal(0, renderer.ClipStackDepth);
    }

    #endregion

    #region Batch Size Tracking

    [Fact]
    public void CurrentBatchSize_TracksCommandsInBatch()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        renderer.FillRect(0, 0, 10, 10, Vector4.One);
        Assert.Equal(1, renderer.CurrentBatchSize);

        renderer.DrawLine(0, 0, 10, 10, Vector4.One);
        Assert.Equal(2, renderer.CurrentBatchSize);

        renderer.End();

        // Starting new batch resets size
        renderer.Begin();
        Assert.Equal(0, renderer.CurrentBatchSize);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_ResetsState()
    {
        var renderer = new Mock2DRenderer();
        renderer.Begin();
        renderer.FillRect(0, 0, 10, 10, Vector4.One);

        renderer.Dispose();

        Assert.Empty(renderer.Commands);
        Assert.False(renderer.IsInBatch);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var renderer = new Mock2DRenderer();

        renderer.Dispose();
        renderer.Dispose();

        // Should not throw
    }

    #endregion
}

#region Command Type Tests

public class Draw2DCommandTests
{
    [Fact]
    public void FillRectCommand_RecordEquality()
    {
        var cmd1 = new FillRectCommand(new Rectangle(0, 0, 100, 100), Vector4.One);
        var cmd2 = new FillRectCommand(new Rectangle(0, 0, 100, 100), Vector4.One);

        Assert.Equal(cmd1, cmd2);
    }

    [Fact]
    public void DrawRectCommand_StoresThickness()
    {
        var cmd = new DrawRectCommand(new Rectangle(0, 0, 50, 50), Vector4.One, 3f);

        Assert.Equal(3f, cmd.Thickness);
    }

    [Fact]
    public void FillRoundedRectCommand_StoresRadius()
    {
        var cmd = new FillRoundedRectCommand(new Rectangle(0, 0, 100, 100), 10f, Vector4.One);

        Assert.Equal(10f, cmd.Radius);
    }

    [Fact]
    public void DrawLineCommand_StoresAllProperties()
    {
        var cmd = new DrawLineCommand(new Vector2(0, 0), new Vector2(100, 100), Vector4.One, 2f);

        Assert.Equal(new Vector2(0, 0), cmd.Start);
        Assert.Equal(new Vector2(100, 100), cmd.End);
        Assert.Equal(2f, cmd.Thickness);
    }

    [Fact]
    public void DrawLineStripCommand_CopiesPoints()
    {
        var points = new[] { new Vector2(0, 0), new Vector2(10, 10) };
        var cmd = new DrawLineStripCommand(points, Vector4.One, 1f);

        Assert.Equal(2, cmd.Points.Length);
    }

    [Fact]
    public void DrawPolygonCommand_CopiesPoints()
    {
        var points = new[] { new Vector2(0, 0), new Vector2(50, 100), new Vector2(100, 0) };
        var cmd = new DrawPolygonCommand(points, Vector4.One, 1f);

        Assert.Equal(3, cmd.Points.Length);
    }

    [Fact]
    public void CircleCommands_StoreSegments()
    {
        var fillCmd = new FillCircleCommand(Vector2.Zero, 50, Vector4.One, 64);
        var drawCmd = new DrawCircleCommand(Vector2.Zero, 50, Vector4.One, 1f, 48);

        Assert.Equal(64, fillCmd.Segments);
        Assert.Equal(48, drawCmd.Segments);
    }

    [Fact]
    public void EllipseCommands_StoreRadii()
    {
        var fillCmd = new FillEllipseCommand(Vector2.Zero, 40, 20, Vector4.One, 32);
        var drawCmd = new DrawEllipseCommand(Vector2.Zero, 60, 30, Vector4.One, 2f, 32);

        Assert.Equal(40f, fillCmd.RadiusX);
        Assert.Equal(20f, fillCmd.RadiusY);
        Assert.Equal(60f, drawCmd.RadiusX);
        Assert.Equal(30f, drawCmd.RadiusY);
    }

    [Fact]
    public void TextureCommands_StoreTextureHandle()
    {
        var texture = new TextureHandle(42);
        var cmd = new DrawTextureCommand(texture, new Rectangle(0, 0, 64, 64), null, Vector4.One);

        Assert.Equal(texture, cmd.Texture);
    }

    [Fact]
    public void DrawTextureRotatedCommand_StoresRotation()
    {
        var cmd = new DrawTextureRotatedCommand(
            new TextureHandle(1),
            new Rectangle(0, 0, 64, 64),
            3.14f,
            new Vector2(32, 32),
            Vector4.One);

        Assert.Equal(3.14f, cmd.Rotation);
        Assert.Equal(new Vector2(32, 32), cmd.Origin);
    }

    [Fact]
    public void PushClipCommand_StoresBothClips()
    {
        var requested = new Rectangle(0, 0, 100, 100);
        var effective = new Rectangle(0, 0, 50, 50);
        var cmd = new PushClipCommand(requested, effective);

        Assert.Equal(requested, cmd.RequestedClip);
        Assert.Equal(effective, cmd.EffectiveClip);
    }

    [Fact]
    public void PopClipCommand_RecordEquality()
    {
        var cmd1 = new PopClipCommand();
        var cmd2 = new PopClipCommand();

        Assert.Equal(cmd1, cmd2);
        Assert.Equal(cmd1.GetHashCode(), cmd2.GetHashCode());
        Assert.True(cmd1.Equals(cmd2));
    }

    [Fact]
    public void PopClipCommand_IsDrawCommand()
    {
        var cmd = new PopClipCommand();

        Assert.IsAssignableFrom<Draw2DCommand>(cmd);
    }

    [Fact]
    public void ClearClipCommand_RecordEquality()
    {
        var cmd1 = new ClearClipCommand();
        var cmd2 = new ClearClipCommand();

        Assert.Equal(cmd1, cmd2);
        Assert.Equal(cmd1.GetHashCode(), cmd2.GetHashCode());
        Assert.True(cmd1.Equals(cmd2));
    }

    [Fact]
    public void ClearClipCommand_IsDrawCommand()
    {
        var cmd = new ClearClipCommand();

        Assert.IsAssignableFrom<Draw2DCommand>(cmd);
    }

    [Fact]
    public void Draw2DCommand_TypesAreDistinct()
    {
        Draw2DCommand pop = new PopClipCommand();
        Draw2DCommand clear = new ClearClipCommand();
        Draw2DCommand fillRect = new FillRectCommand(new Rectangle(0, 0, 10, 10), Vector4.One);

        Assert.NotEqual(pop, clear);
        Assert.NotEqual(pop, fillRect);
        Assert.NotEqual(clear, fillRect);
    }

    [Fact]
    public void DrawTextureRegionCommand_StoresSourceRect()
    {
        var texture = new TextureHandle(1);
        var dest = new Rectangle(0, 0, 128, 128);
        var source = new Rectangle(0, 0, 64, 64);
        var tint = new Vector4(1, 1, 1, 0.5f);

        var cmd = new DrawTextureRegionCommand(texture, dest, source, tint);

        Assert.Equal(texture, cmd.Texture);
        Assert.Equal(dest, cmd.DestRect);
        Assert.Equal(source, cmd.SourceRect);
        Assert.Equal(tint, cmd.Tint);
    }

    [Fact]
    public void DrawRoundedRectCommand_StoresAllProperties()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        var color = new Vector4(1, 0, 0, 1);

        var cmd = new DrawRoundedRectCommand(rect, 8f, color, 2.5f);

        Assert.Equal(rect, cmd.Rect);
        Assert.Equal(8f, cmd.Radius);
        Assert.Equal(color, cmd.Color);
        Assert.Equal(2.5f, cmd.Thickness);
    }
}

#endregion
