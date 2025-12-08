namespace KeenEyes.Testing.Tests;

public class TestClockTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultFps_IsSixty()
    {
        var clock = new TestClock();

        clock.Fps.ShouldBe(60f);
    }

    [Fact]
    public void Constructor_CustomFps_SetsCorrectly()
    {
        var clock = new TestClock(120f);

        clock.Fps.ShouldBe(120f);
    }

    [Fact]
    public void Constructor_InitializesToZero()
    {
        var clock = new TestClock();

        clock.CurrentTime.ShouldBe(0f);
        clock.DeltaTime.ShouldBe(0f);
        clock.FrameCount.ShouldBe(0);
        clock.IsPaused.ShouldBeFalse();
    }

    #endregion

    #region Step Tests

    [Fact]
    public void Step_SingleFrame_AdvancesCorrectTime()
    {
        var clock = new TestClock(60f);

        clock.Step();

        // At 60 FPS, one frame = 1000/60 ms ≈ 16.67 ms
        clock.DeltaTime.ShouldBe(1000f / 60f, tolerance: 0.01f);
        clock.CurrentTime.ShouldBe(1000f / 60f, tolerance: 0.01f);
        clock.FrameCount.ShouldBe(1);
    }

    [Fact]
    public void Step_MultipleFrames_AdvancesCorrectTime()
    {
        var clock = new TestClock(60f);

        clock.Step(5);

        clock.DeltaTime.ShouldBe(5 * 1000f / 60f, tolerance: 0.01f);
        clock.CurrentTime.ShouldBe(5 * 1000f / 60f, tolerance: 0.01f);
        clock.FrameCount.ShouldBe(5);
    }

    [Fact]
    public void Step_ReturnsDeltaSeconds()
    {
        var clock = new TestClock(60f);

        var result = clock.Step();

        result.ShouldBe(1f / 60f, tolerance: 0.0001f);
    }

    [Fact]
    public void Step_WhenPaused_DoesNotAdvance()
    {
        var clock = new TestClock();
        clock.Pause();

        clock.Step();

        clock.CurrentTime.ShouldBe(0f);
        clock.DeltaTime.ShouldBe(0f);
        clock.FrameCount.ShouldBe(0);
    }

    [Fact]
    public void Step_ZeroFrames_DoesNotAdvance()
    {
        var clock = new TestClock();

        clock.Step(0);

        clock.CurrentTime.ShouldBe(0f);
        clock.DeltaTime.ShouldBe(0f);
    }

    [Fact]
    public void Step_NegativeFrames_DoesNotAdvance()
    {
        var clock = new TestClock();

        clock.Step(-1);

        clock.CurrentTime.ShouldBe(0f);
        clock.DeltaTime.ShouldBe(0f);
    }

    #endregion

    #region StepByTime Tests

    [Fact]
    public void StepByTime_AdvancesCorrectTime()
    {
        var clock = new TestClock();

        clock.StepByTime(100f);

        clock.CurrentTime.ShouldBe(100f);
        clock.DeltaTime.ShouldBe(100f);
        clock.FrameCount.ShouldBe(1);
    }

    [Fact]
    public void StepByTime_ReturnsDeltaSeconds()
    {
        var clock = new TestClock();

        var result = clock.StepByTime(100f);

        result.ShouldBe(0.1f);
    }

    [Fact]
    public void StepByTime_WhenPaused_DoesNotAdvance()
    {
        var clock = new TestClock();
        clock.Pause();

        clock.StepByTime(100f);

        clock.CurrentTime.ShouldBe(0f);
    }

    #endregion

    #region SetTime Tests

    [Fact]
    public void SetTime_SetsAbsoluteTime()
    {
        var clock = new TestClock();

        clock.SetTime(500f);

        clock.CurrentTime.ShouldBe(500f);
        clock.DeltaTime.ShouldBe(500f);
    }

    [Fact]
    public void SetTime_CalculatesDeltaFromPrevious()
    {
        var clock = new TestClock();
        clock.SetTime(100f);

        clock.SetTime(250f);

        clock.DeltaTime.ShouldBe(150f);
    }

    [Fact]
    public void SetTime_NegativeValue_ClampsToZero()
    {
        var clock = new TestClock();
        clock.SetTime(100f);

        clock.SetTime(-50f);

        clock.CurrentTime.ShouldBe(0f);
    }

    #endregion

    #region Pause/Resume Tests

    [Fact]
    public void Pause_SetsIsPaused()
    {
        var clock = new TestClock();

        clock.Pause();

        clock.IsPaused.ShouldBeTrue();
    }

    [Fact]
    public void Resume_ClearsIsPaused()
    {
        var clock = new TestClock();
        clock.Pause();

        clock.Resume();

        clock.IsPaused.ShouldBeFalse();
    }

    #endregion

    #region SetFps Tests

    [Fact]
    public void SetFps_ChangesFps()
    {
        var clock = new TestClock(60f);

        clock.SetFps(120f);

        clock.Fps.ShouldBe(120f);
    }

    [Fact]
    public void SetFps_AffectsStepDuration()
    {
        var clock = new TestClock(60f);
        clock.SetFps(30f);

        clock.Step();

        // At 30 FPS, one frame = 1000/30 ms ≈ 33.33 ms
        clock.DeltaTime.ShouldBe(1000f / 30f, tolerance: 0.01f);
    }

    [Fact]
    public void SetFps_ZeroOrNegative_Throws()
    {
        var clock = new TestClock();

        Should.Throw<ArgumentOutOfRangeException>(() => clock.SetFps(0f));
        Should.Throw<ArgumentOutOfRangeException>(() => clock.SetFps(-1f));
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        var clock = new TestClock();
        clock.Step(10);
        clock.Pause();

        clock.Reset();

        clock.CurrentTime.ShouldBe(0f);
        clock.DeltaTime.ShouldBe(0f);
        clock.FrameCount.ShouldBe(0);
        clock.IsPaused.ShouldBeFalse();
    }

    #endregion

    #region DeltaSeconds Tests

    [Fact]
    public void DeltaSeconds_ConvertsMsToSeconds()
    {
        var clock = new TestClock();
        clock.StepByTime(1000f);

        clock.DeltaSeconds.ShouldBe(1f);
    }

    #endregion
}
