namespace KeenEyes.Debugging.Tests;

/// <summary>
/// Unit tests for the <see cref="Profiler"/> class.
/// </summary>
public sealed class ProfilerTests
{
    #region BeginSample/EndSample Tests

    [Fact]
    public void BeginSample_EndSample_RecordsProfile()
    {
        // Arrange
        var profiler = new Profiler();

        // Act
        profiler.BeginSample("TestSystem");
        Thread.Sleep(1); // Small delay to ensure measurable time
        profiler.EndSample("TestSystem");

        // Assert
        var profile = profiler.GetSystemProfile("TestSystem");
        Assert.Equal("TestSystem", profile.Name);
        Assert.Equal(1, profile.CallCount);
        Assert.True(profile.TotalTime > TimeSpan.Zero);
        Assert.True(profile.AverageTime > TimeSpan.Zero);
        Assert.Equal(profile.TotalTime, profile.AverageTime); // First call, so avg == total
    }

    [Fact]
    public void BeginSample_EndSample_MultipleCalls_AccumulatesStats()
    {
        // Arrange
        var profiler = new Profiler();

        // Act
        for (int i = 0; i < 5; i++)
        {
            profiler.BeginSample("TestSystem");
            Thread.Sleep(1);
            profiler.EndSample("TestSystem");
        }

        // Assert
        var profile = profiler.GetSystemProfile("TestSystem");
        Assert.Equal("TestSystem", profile.Name);
        Assert.Equal(5, profile.CallCount);
        Assert.True(profile.TotalTime > TimeSpan.Zero);
        Assert.True(profile.AverageTime > TimeSpan.Zero);
        Assert.True(profile.MinTime > TimeSpan.Zero);
        Assert.True(profile.MaxTime >= profile.MinTime);
    }

    [Fact]
    public void EndSample_WithoutBeginSample_DoesNotThrow()
    {
        // Arrange
        var profiler = new Profiler();

        // Act & Assert - should not throw
        profiler.EndSample("NonExistentSystem");

        var profile = profiler.GetSystemProfile("NonExistentSystem");
        Assert.Equal(0, profile.CallCount);
    }

    [Fact]
    public void BeginSample_Overwrite_ReplacesPreviousSample()
    {
        // Arrange
        var profiler = new Profiler();

        // Act
        profiler.BeginSample("TestSystem");
        Thread.Sleep(1);
        profiler.BeginSample("TestSystem"); // Overwrite previous BeginSample
        Thread.Sleep(1);
        profiler.EndSample("TestSystem");

        // Assert - should only record one sample (the second one)
        var profile = profiler.GetSystemProfile("TestSystem");
        Assert.Equal(1, profile.CallCount);
    }

    #endregion

    #region GetSystemProfile Tests

    [Fact]
    public void GetSystemProfile_NonExistentSystem_ReturnsEmptyProfile()
    {
        // Arrange
        var profiler = new Profiler();

        // Act
        var profile = profiler.GetSystemProfile("NonExistent");

        // Assert
        Assert.Equal("NonExistent", profile.Name);
        Assert.Equal(0, profile.CallCount);
        Assert.Equal(TimeSpan.Zero, profile.TotalTime);
        Assert.Equal(TimeSpan.Zero, profile.AverageTime);
        Assert.Equal(TimeSpan.Zero, profile.MinTime);
        Assert.Equal(TimeSpan.Zero, profile.MaxTime);
    }

    [Fact]
    public void GetSystemProfile_ExistingSystem_ReturnsCorrectProfile()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.BeginSample("TestSystem");
        Thread.Sleep(1);
        profiler.EndSample("TestSystem");

        // Act
        var profile = profiler.GetSystemProfile("TestSystem");

        // Assert
        Assert.Equal("TestSystem", profile.Name);
        Assert.Equal(1, profile.CallCount);
        Assert.True(profile.TotalTime > TimeSpan.Zero);
    }

    #endregion

    #region GetAllSystemProfiles Tests

    [Fact]
    public void GetAllSystemProfiles_NoSamples_ReturnsEmptyList()
    {
        // Arrange
        var profiler = new Profiler();

        // Act
        var profiles = profiler.GetAllSystemProfiles();

        // Assert
        Assert.Empty(profiles);
    }

    [Fact]
    public void GetAllSystemProfiles_MultipleSystems_ReturnsAllProfiles()
    {
        // Arrange
        var profiler = new Profiler();

        profiler.BeginSample("System1");
        profiler.EndSample("System1");

        profiler.BeginSample("System2");
        profiler.EndSample("System2");

        profiler.BeginSample("System3");
        profiler.EndSample("System3");

        // Act
        var profiles = profiler.GetAllSystemProfiles();

        // Assert
        Assert.Equal(3, profiles.Count);
        Assert.Contains(profiles, p => p.Name == "System1");
        Assert.Contains(profiles, p => p.Name == "System2");
        Assert.Contains(profiles, p => p.Name == "System3");
    }

    [Fact]
    public void GetAllSystemProfiles_ReturnsSnapshot()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.BeginSample("TestSystem");
        profiler.EndSample("TestSystem");

        // Act
        var profiles1 = profiler.GetAllSystemProfiles();

        profiler.BeginSample("NewSystem");
        profiler.EndSample("NewSystem");

        var profiles2 = profiler.GetAllSystemProfiles();

        // Assert - first snapshot should not include NewSystem
        Assert.Single(profiles1);
        Assert.Equal(2, profiles2.Count);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllProfiles()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.BeginSample("System1");
        profiler.EndSample("System1");
        profiler.BeginSample("System2");
        profiler.EndSample("System2");

        // Act
        profiler.Reset();

        // Assert
        var profiles = profiler.GetAllSystemProfiles();
        Assert.Empty(profiles);

        var profile = profiler.GetSystemProfile("System1");
        Assert.Equal(0, profile.CallCount);
    }

    [Fact]
    public void Reset_ClearsActiveSamples()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.BeginSample("TestSystem");

        // Act
        profiler.Reset();
        profiler.EndSample("TestSystem"); // Should do nothing since BeginSample was cleared

        // Assert
        var profile = profiler.GetSystemProfile("TestSystem");
        Assert.Equal(0, profile.CallCount);
    }

    #endregion

    #region Min/Max Tracking Tests

    [Fact]
    public void MinTime_TracksSmallestExecution()
    {
        // Arrange
        var profiler = new Profiler();

        // Act - First call with longer sleep
        profiler.BeginSample("TestSystem");
        Thread.Sleep(10);
        profiler.EndSample("TestSystem");

        var firstMin = profiler.GetSystemProfile("TestSystem").MinTime;

        // Second call with no sleep (should be faster)
        profiler.BeginSample("TestSystem");
        profiler.EndSample("TestSystem");

        // Assert
        var profile = profiler.GetSystemProfile("TestSystem");
        Assert.True(profile.MinTime <= firstMin, $"MinTime ({profile.MinTime.TotalMilliseconds}ms) should be <= firstMin ({firstMin.TotalMilliseconds}ms)");
    }

    [Fact]
    public void MaxTime_TracksLargestExecution()
    {
        // Arrange
        var profiler = new Profiler();

        // Act - First call with shorter sleep
        profiler.BeginSample("TestSystem");
        Thread.Sleep(1);
        profiler.EndSample("TestSystem");

        var firstMax = profiler.GetSystemProfile("TestSystem").MaxTime;

        // Second call with longer sleep
        profiler.BeginSample("TestSystem");
        Thread.Sleep(5);
        profiler.EndSample("TestSystem");

        // Assert
        var profile = profiler.GetSystemProfile("TestSystem");
        Assert.True(profile.MaxTime > firstMax);
    }

    #endregion

    #region Average Calculation Tests

    [Fact]
    public void AverageTime_CalculatesCorrectly()
    {
        // Arrange
        var profiler = new Profiler();

        // Act - Execute multiple times
        for (int i = 0; i < 10; i++)
        {
            profiler.BeginSample("TestSystem");
            Thread.Sleep(1);
            profiler.EndSample("TestSystem");
        }

        // Assert
        var profile = profiler.GetSystemProfile("TestSystem");
        var expectedAverage = TimeSpan.FromTicks(profile.TotalTime.Ticks / profile.CallCount);
        Assert.Equal(expectedAverage, profile.AverageTime);
    }

    #endregion
}
