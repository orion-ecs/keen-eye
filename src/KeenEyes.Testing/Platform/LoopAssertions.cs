namespace KeenEyes.Testing.Platform;

/// <summary>
/// Fluent assertion extensions for <see cref="MockLoopProvider"/>.
/// </summary>
/// <example>
/// <code>
/// var loop = new MockLoopProvider();
/// loop.Initialize();
/// loop.StepFrames(5);
///
/// loop.ShouldBeInitialized()
///     .ShouldHaveUpdatedTimes(5)
///     .ShouldHaveRenderedTimes(5);
/// </code>
/// </example>
public static class LoopAssertions
{
    /// <summary>
    /// Asserts that the loop provider has been initialized.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if not initialized.</exception>
    public static MockLoopProvider ShouldBeInitialized(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (!loop.IsInitialized)
        {
            throw new AssertionException("Expected loop provider to be initialized, but it was not.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop provider has not been initialized.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if initialized.</exception>
    public static MockLoopProvider ShouldNotBeInitialized(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.IsInitialized)
        {
            throw new AssertionException("Expected loop provider to not be initialized, but it was.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop provider is currently running.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if not running.</exception>
    public static MockLoopProvider ShouldBeRunning(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (!loop.IsRunning)
        {
            throw new AssertionException("Expected loop provider to be running, but it was not.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop provider is not currently running.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if running.</exception>
    public static MockLoopProvider ShouldNotBeRunning(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.IsRunning)
        {
            throw new AssertionException("Expected loop provider to not be running, but it was.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop has received at least one update.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if no updates have occurred.</exception>
    public static MockLoopProvider ShouldHaveUpdated(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.UpdateCount == 0)
        {
            throw new AssertionException("Expected at least one update, but none occurred.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop has received no updates.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if any updates have occurred.</exception>
    public static MockLoopProvider ShouldNotHaveUpdated(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.UpdateCount != 0)
        {
            throw new AssertionException($"Expected no updates, but {loop.UpdateCount} occurred.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop has received exactly the specified number of updates.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <param name="expectedCount">The expected number of updates.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if the update count doesn't match.</exception>
    public static MockLoopProvider ShouldHaveUpdatedTimes(this MockLoopProvider loop, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.UpdateCount != expectedCount)
        {
            throw new AssertionException(
                $"Expected {expectedCount} updates, but {loop.UpdateCount} occurred.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop has received at least one render.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if no renders have occurred.</exception>
    public static MockLoopProvider ShouldHaveRendered(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.RenderCount == 0)
        {
            throw new AssertionException("Expected at least one render, but none occurred.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop has received no renders.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if any renders have occurred.</exception>
    public static MockLoopProvider ShouldNotHaveRendered(this MockLoopProvider loop)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.RenderCount != 0)
        {
            throw new AssertionException($"Expected no renders, but {loop.RenderCount} occurred.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop has received exactly the specified number of renders.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <param name="expectedCount">The expected number of renders.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if the render count doesn't match.</exception>
    public static MockLoopProvider ShouldHaveRenderedTimes(this MockLoopProvider loop, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.RenderCount != expectedCount)
        {
            throw new AssertionException(
                $"Expected {expectedCount} renders, but {loop.RenderCount} occurred.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the total accumulated time matches the expected value within a tolerance.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <param name="expectedTime">The expected total time in seconds.</param>
    /// <param name="tolerance">The allowed tolerance. Defaults to 0.0001.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if the total time doesn't match within tolerance.</exception>
    public static MockLoopProvider ShouldHaveTotalTime(
        this MockLoopProvider loop,
        float expectedTime,
        float tolerance = 0.0001f)
    {
        ArgumentNullException.ThrowIfNull(loop);

        var difference = Math.Abs(loop.TotalTime - expectedTime);
        if (difference > tolerance)
        {
            throw new AssertionException(
                $"Expected total time of {expectedTime:F4}s (±{tolerance:F4}), but was {loop.TotalTime:F4}s.");
        }

        return loop;
    }

    /// <summary>
    /// Asserts that the loop has the expected dimensions.
    /// </summary>
    /// <param name="loop">The loop provider to check.</param>
    /// <param name="expectedWidth">The expected width.</param>
    /// <param name="expectedHeight">The expected height.</param>
    /// <returns>The loop provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown if the dimensions don't match.</exception>
    public static MockLoopProvider ShouldHaveSize(
        this MockLoopProvider loop,
        int expectedWidth,
        int expectedHeight)
    {
        ArgumentNullException.ThrowIfNull(loop);

        if (loop.Width != expectedWidth || loop.Height != expectedHeight)
        {
            throw new AssertionException(
                $"Expected size {expectedWidth}x{expectedHeight}, but was {loop.Width}x{loop.Height}.");
        }

        return loop;
    }
}
