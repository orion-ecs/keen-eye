namespace KeenEyes.Testing.Systems;

/// <summary>
/// Fluent assertion extensions for verifying system execution in tests.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide readable assertions for verifying that systems
/// were executed as expected during test runs.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var recorder = testWorld.SystemRecorder!;
/// recorder.ShouldHaveCalledSystem&lt;MovementSystem&gt;();
/// recorder.ShouldHaveCalledSystemTimes&lt;MovementSystem&gt;(3);
/// recorder.ShouldNotHaveCalledSystem&lt;DisabledSystem&gt;();
/// </code>
/// </example>
public static class SystemRecorderAssertions
{
    /// <summary>
    /// Asserts that the specified system was called at least once.
    /// </summary>
    /// <typeparam name="TSystem">The system type to check.</typeparam>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the system was never called.</exception>
    public static SystemRecorder ShouldHaveCalledSystem<TSystem>(
        this SystemRecorder recorder,
        string? because = null)
        where TSystem : ISystem
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (!recorder.WasCalled<TSystem>())
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected system {typeof(TSystem).Name} to have been called{reason}, but it was not.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the specified system was called a specific number of times.
    /// </summary>
    /// <typeparam name="TSystem">The system type to check.</typeparam>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="expectedCount">The expected number of calls.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the call count does not match.</exception>
    public static SystemRecorder ShouldHaveCalledSystemTimes<TSystem>(
        this SystemRecorder recorder,
        int expectedCount,
        string? because = null)
        where TSystem : ISystem
    {
        ArgumentNullException.ThrowIfNull(recorder);

        var actualCount = recorder.GetCallCount<TSystem>();
        if (actualCount != expectedCount)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected system {typeof(TSystem).Name} to have been called {expectedCount} time(s){reason}, " +
                $"but it was called {actualCount} time(s).");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the specified system was called at least a minimum number of times.
    /// </summary>
    /// <typeparam name="TSystem">The system type to check.</typeparam>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="minimumCount">The minimum expected number of calls.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the call count is below minimum.</exception>
    public static SystemRecorder ShouldHaveCalledSystemAtLeast<TSystem>(
        this SystemRecorder recorder,
        int minimumCount,
        string? because = null)
        where TSystem : ISystem
    {
        ArgumentNullException.ThrowIfNull(recorder);

        var actualCount = recorder.GetCallCount<TSystem>();
        if (actualCount < minimumCount)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected system {typeof(TSystem).Name} to have been called at least {minimumCount} time(s){reason}, " +
                $"but it was called only {actualCount} time(s).");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the specified system was not called.
    /// </summary>
    /// <typeparam name="TSystem">The system type to check.</typeparam>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the system was called.</exception>
    public static SystemRecorder ShouldNotHaveCalledSystem<TSystem>(
        this SystemRecorder recorder,
        string? because = null)
        where TSystem : ISystem
    {
        ArgumentNullException.ThrowIfNull(recorder);

        var callCount = recorder.GetCallCount<TSystem>();
        if (callCount > 0)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected system {typeof(TSystem).Name} to not have been called{reason}, " +
                $"but it was called {callCount} time(s).");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the system was called with a specific system name.
    /// </summary>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="systemName">The name of the system to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the system was never called.</exception>
    public static SystemRecorder ShouldHaveCalledSystem(
        this SystemRecorder recorder,
        string systemName,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(systemName);

        if (!recorder.WasCalled(systemName))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected system '{systemName}' to have been called{reason}, but it was not.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the total number of system calls matches the expected count.
    /// </summary>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="expectedCount">The expected total call count.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the total call count does not match.</exception>
    public static SystemRecorder ShouldHaveTotalCallCount(
        this SystemRecorder recorder,
        int expectedCount,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (recorder.TotalCallCount != expectedCount)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected total of {expectedCount} system call(s){reason}, " +
                $"but found {recorder.TotalCallCount} call(s).");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that no systems were called.
    /// </summary>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when any system was called.</exception>
    public static SystemRecorder ShouldHaveNoCalls(
        this SystemRecorder recorder,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (recorder.TotalCallCount > 0)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            var systemNames = string.Join(", ", recorder.Calls.Select(c => c.SystemName).Distinct());
            throw new AssertionException(
                $"Expected no system calls{reason}, but found {recorder.TotalCallCount} call(s) to: {systemNames}.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the specified system accumulated at least the given total delta time.
    /// </summary>
    /// <typeparam name="TSystem">The system type to check.</typeparam>
    /// <param name="recorder">The system recorder.</param>
    /// <param name="minimumDeltaTime">The minimum expected accumulated delta time.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the accumulated time is below minimum.</exception>
    public static SystemRecorder ShouldHaveAccumulatedDeltaTime<TSystem>(
        this SystemRecorder recorder,
        float minimumDeltaTime,
        string? because = null)
        where TSystem : ISystem
    {
        ArgumentNullException.ThrowIfNull(recorder);

        var actualTime = recorder.GetTotalDeltaTime<TSystem>();
        if (actualTime < minimumDeltaTime)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected system {typeof(TSystem).Name} to have accumulated at least {minimumDeltaTime:F4}s{reason}, " +
                $"but it only accumulated {actualTime:F4}s.");
        }

        return recorder;
    }
}
