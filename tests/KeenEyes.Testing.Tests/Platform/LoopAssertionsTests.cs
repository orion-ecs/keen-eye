using KeenEyes.Testing.Platform;

namespace KeenEyes.Testing.Tests.Platform;

public class LoopAssertionsTests
{
    #region ShouldBeInitialized

    [Fact]
    public void ShouldBeInitialized_WhenInitialized_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        var result = loop.ShouldBeInitialized();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldBeInitialized_WhenNotInitialized_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldBeInitialized());
        Assert.Equal("Expected loop provider to be initialized, but it was not.", ex.Message);
    }

    [Fact]
    public void ShouldBeInitialized_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldBeInitialized());
    }

    #endregion

    #region ShouldNotBeInitialized

    [Fact]
    public void ShouldNotBeInitialized_WhenNotInitialized_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();

        var result = loop.ShouldNotBeInitialized();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldNotBeInitialized_WhenInitialized_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldNotBeInitialized());
        Assert.Equal("Expected loop provider to not be initialized, but it was.", ex.Message);
    }

    [Fact]
    public void ShouldNotBeInitialized_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldNotBeInitialized());
    }

    #endregion

    #region ShouldBeRunning

    [Fact]
    public void ShouldBeRunning_WhenRunning_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();

        var result = loop.ShouldBeRunning();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldBeRunning_WhenNotRunning_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldBeRunning());
        Assert.Equal("Expected loop provider to be running, but it was not.", ex.Message);
    }

    [Fact]
    public void ShouldBeRunning_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldBeRunning());
    }

    #endregion

    #region ShouldNotBeRunning

    [Fact]
    public void ShouldNotBeRunning_WhenNotRunning_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        var result = loop.ShouldNotBeRunning();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldNotBeRunning_WhenRunning_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldNotBeRunning());
        Assert.Equal("Expected loop provider to not be running, but it was.", ex.Message);
    }

    [Fact]
    public void ShouldNotBeRunning_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldNotBeRunning());
    }

    #endregion

    #region ShouldHaveUpdated

    [Fact]
    public void ShouldHaveUpdated_WhenUpdated_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerUpdate(0.016f);

        var result = loop.ShouldHaveUpdated();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveUpdated_WhenNotUpdated_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldHaveUpdated());
        Assert.Equal("Expected at least one update, but none occurred.", ex.Message);
    }

    [Fact]
    public void ShouldHaveUpdated_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldHaveUpdated());
    }

    #endregion

    #region ShouldNotHaveUpdated

    [Fact]
    public void ShouldNotHaveUpdated_WhenNotUpdated_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();

        var result = loop.ShouldNotHaveUpdated();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldNotHaveUpdated_WhenUpdated_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerUpdate(0.016f);

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldNotHaveUpdated());
        Assert.Equal("Expected no updates, but 1 occurred.", ex.Message);
    }

    [Fact]
    public void ShouldNotHaveUpdated_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldNotHaveUpdated());
    }

    #endregion

    #region ShouldHaveUpdatedTimes

    [Fact]
    public void ShouldHaveUpdatedTimes_WhenCountMatches_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(5);

        var result = loop.ShouldHaveUpdatedTimes(5);

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveUpdatedTimes_WhenCountDoesNotMatch_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(3);

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldHaveUpdatedTimes(5));
        Assert.Equal("Expected 5 updates, but 3 occurred.", ex.Message);
    }

    [Fact]
    public void ShouldHaveUpdatedTimes_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldHaveUpdatedTimes(5));
    }

    #endregion

    #region ShouldHaveRendered

    [Fact]
    public void ShouldHaveRendered_WhenRendered_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerRender(0.016f);

        var result = loop.ShouldHaveRendered();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveRendered_WhenNotRendered_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldHaveRendered());
        Assert.Equal("Expected at least one render, but none occurred.", ex.Message);
    }

    [Fact]
    public void ShouldHaveRendered_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldHaveRendered());
    }

    #endregion

    #region ShouldNotHaveRendered

    [Fact]
    public void ShouldNotHaveRendered_WhenNotRendered_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();

        var result = loop.ShouldNotHaveRendered();

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldNotHaveRendered_WhenRendered_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerRender(0.016f);

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldNotHaveRendered());
        Assert.Equal("Expected no renders, but 1 occurred.", ex.Message);
    }

    [Fact]
    public void ShouldNotHaveRendered_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldNotHaveRendered());
    }

    #endregion

    #region ShouldHaveRenderedTimes

    [Fact]
    public void ShouldHaveRenderedTimes_WhenCountMatches_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(5);

        var result = loop.ShouldHaveRenderedTimes(5);

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveRenderedTimes_WhenCountDoesNotMatch_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(3);

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldHaveRenderedTimes(5));
        Assert.Equal("Expected 5 renders, but 3 occurred.", ex.Message);
    }

    [Fact]
    public void ShouldHaveRenderedTimes_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldHaveRenderedTimes(5));
    }

    #endregion

    #region ShouldHaveTotalTime

    [Fact]
    public void ShouldHaveTotalTime_WhenTimeMatchesWithinTolerance_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(10, 0.016f);

        var result = loop.ShouldHaveTotalTime(0.16f, 0.0001f);

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveTotalTime_WithDefaultTolerance_UsesSmallEpsilon()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(10, 0.016f);

        var result = loop.ShouldHaveTotalTime(0.16f);

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveTotalTime_WhenTimeDoesNotMatchWithinTolerance_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(5, 0.016f);

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldHaveTotalTime(1.0f, 0.0001f));
        Assert.Contains("Expected total time of 1.0000s", ex.Message);
        Assert.Contains("but was 0.0800s", ex.Message);
    }

    [Fact]
    public void ShouldHaveTotalTime_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldHaveTotalTime(1.0f));
    }

    #endregion

    #region ShouldHaveSize

    [Fact]
    public void ShouldHaveSize_WhenSizeMatches_ReturnsLoopForChaining()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerResize(1920, 1080);

        var result = loop.ShouldHaveSize(1920, 1080);

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveSize_WithDefaultSize_Succeeds()
    {
        using var loop = new MockLoopProvider();

        var result = loop.ShouldHaveSize(800, 600);

        Assert.Same(loop, result);
    }

    [Fact]
    public void ShouldHaveSize_WhenWidthDoesNotMatch_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerResize(1920, 1080);

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldHaveSize(1024, 1080));
        Assert.Equal("Expected size 1024x1080, but was 1920x1080.", ex.Message);
    }

    [Fact]
    public void ShouldHaveSize_WhenHeightDoesNotMatch_ThrowsAssertionException()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerResize(1920, 1080);

        var ex = Assert.Throws<AssertionException>(() => loop.ShouldHaveSize(1920, 768));
        Assert.Equal("Expected size 1920x768, but was 1920x1080.", ex.Message);
    }

    [Fact]
    public void ShouldHaveSize_WithNullLoop_ThrowsArgumentNullException()
    {
        MockLoopProvider? loop = null;

        Assert.Throws<ArgumentNullException>(() => loop!.ShouldHaveSize(800, 600));
    }

    #endregion

    #region Fluent Chaining

    [Fact]
    public void Assertions_CanBeChained()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.StepFrames(5);

        loop.ShouldBeInitialized()
            .ShouldHaveUpdatedTimes(5)
            .ShouldHaveRenderedTimes(5)
            .ShouldHaveSize(800, 600);
    }

    #endregion
}
