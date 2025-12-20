using KeenEyes.Animation.Data;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Animation.Tests;

public class SpriteSheetTests
{
    #region Frame Management

    [Fact]
    public void AddFrame_IncreasesTotalDuration()
    {
        var sheet = new SpriteSheet { Name = "Test" };

        sheet.AddFrame(new Rectangle(0, 0, 1, 1), 0.1f);
        sheet.TotalDuration.ShouldBe(0.1f);

        sheet.AddFrame(new Rectangle(0, 0, 1, 1), 0.2f);
        sheet.TotalDuration.ShouldBe(0.3f);
    }

    [Fact]
    public void AddGridFrames_CreatesCorrectFrames()
    {
        var sheet = new SpriteSheet { Name = "Test" };

        sheet.AddGridFrames(columns: 4, rows: 4, frameCount: 8, frameDuration: 0.1f);

        sheet.Frames.Count.ShouldBe(8);
        sheet.TotalDuration.ShouldBe(0.8f, 0.001f); // Use tolerance for float comparison

        // First frame should be top-left
        sheet.Frames[0].SourceRect.X.ShouldBe(0f);
        sheet.Frames[0].SourceRect.Y.ShouldBe(0f);
        sheet.Frames[0].SourceRect.Width.ShouldBe(0.25f);
        sheet.Frames[0].SourceRect.Height.ShouldBe(0.25f);

        // Second frame should be next column
        sheet.Frames[1].SourceRect.X.ShouldBe(0.25f);
        sheet.Frames[1].SourceRect.Y.ShouldBe(0f);
    }

    #endregion

    #region GetFrameAtTime Tests

    [Fact]
    public void GetFrameAtTime_WithNoFrames_ReturnsDefault()
    {
        var sheet = new SpriteSheet { Name = "Test" };

        var (index, frame) = sheet.GetFrameAtTime(0f);

        index.ShouldBe(0);
        frame.ShouldBe(default(SpriteFrame));
    }

    [Fact]
    public void GetFrameAtTime_ReturnsCorrectFrame()
    {
        var sheet = new SpriteSheet { Name = "Test" };
        sheet.AddFrame(new Rectangle(0, 0, 0.5f, 1), 0.2f);
        sheet.AddFrame(new Rectangle(0.5f, 0, 0.5f, 1), 0.2f);

        // At time 0, should be frame 0
        var (index0, _) = sheet.GetFrameAtTime(0f);
        index0.ShouldBe(0);

        // At time 0.1, still frame 0
        var (index1, _) = sheet.GetFrameAtTime(0.1f);
        index1.ShouldBe(0);

        // At time 0.2, should be frame 1
        var (index2, _) = sheet.GetFrameAtTime(0.2f);
        index2.ShouldBe(1);

        // At time 0.3, still frame 1
        var (index3, _) = sheet.GetFrameAtTime(0.3f);
        index3.ShouldBe(1);
    }

    [Fact]
    public void GetFrameAtTime_WithLoop_WrapsCorrectly()
    {
        var sheet = new SpriteSheet { Name = "Test", WrapMode = WrapMode.Loop };
        sheet.AddFrame(new Rectangle(0, 0, 0.5f, 1), 0.2f);
        sheet.AddFrame(new Rectangle(0.5f, 0, 0.5f, 1), 0.2f);

        // After full cycle (0.4f wraps to 0.0f), should be frame 0
        var (index, _) = sheet.GetFrameAtTime(0.4f);
        index.ShouldBe(0);

        // At time 0.5f (wraps to 0.1f), should still be frame 0
        var (index2, _) = sheet.GetFrameAtTime(0.5f);
        index2.ShouldBe(0);

        // At time 0.6f (wraps to 0.2f), at boundary of frame 1
        var (index3, _) = sheet.GetFrameAtTime(0.6f);
        index3.ShouldBe(1);
    }

    [Fact]
    public void GetFrameAtTime_WithOnce_ClampsToLast()
    {
        var sheet = new SpriteSheet { Name = "Test", WrapMode = WrapMode.Once };
        sheet.AddFrame(new Rectangle(0, 0, 0.5f, 1), 0.2f);
        sheet.AddFrame(new Rectangle(0.5f, 0, 0.5f, 1), 0.2f);

        // Past duration should return last frame
        var (index, _) = sheet.GetFrameAtTime(1f);
        index.ShouldBe(1);
    }

    #endregion
}
