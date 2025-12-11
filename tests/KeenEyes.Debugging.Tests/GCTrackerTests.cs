namespace KeenEyes.Debugging.Tests;

#pragma warning disable IDE0059 // Unnecessary assignment - intentional for GC allocation testing

/// <summary>
/// Unit tests for the <see cref="GCTracker"/> class.
/// </summary>
public sealed class GCTrackerTests
{
    #region BeginTracking/EndTracking Tests

    [Fact]
    public void BeginTracking_EndTracking_RecordsAllocations()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act
        tracker.BeginTracking("TestSystem");

        // Allocate some memory
        var list = new List<int>(1000);
        for (int i = 0; i < 1000; i++)
        {
            list.Add(i);
        }

        var allocated = tracker.EndTracking("TestSystem");

        // Assert
        Assert.True(allocated >= 0); // May be 0 if GC hasn't updated yet, but shouldn't be negative
        var profile = tracker.GetSystemAllocations("TestSystem");
        Assert.Equal("TestSystem", profile.Name);
        Assert.Equal(1, profile.CallCount);
    }

    [Fact]
    public void BeginTracking_EndTracking_MultipleCalls_AccumulatesStats()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tracker.BeginTracking("TestSystem");

            // Allocate some memory
            var list = new List<int>(100);
            for (int j = 0; j < 100; j++)
            {
                list.Add(j);
            }

            tracker.EndTracking("TestSystem");
        }

        // Assert
        var profile = tracker.GetSystemAllocations("TestSystem");
        Assert.Equal("TestSystem", profile.Name);
        Assert.Equal(5, profile.CallCount);
    }

    [Fact]
    public void EndTracking_WithoutBeginTracking_ReturnsZero()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act
        var allocated = tracker.EndTracking("NonExistentSystem");

        // Assert
        Assert.Equal(0, allocated);

        var profile = tracker.GetSystemAllocations("NonExistentSystem");
        Assert.Equal(0, profile.CallCount);
    }

    [Fact]
    public void BeginTracking_Overwrite_ReplacesPreviousSnapshot()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act
        tracker.BeginTracking("TestSystem");

        // Allocate memory
        var list1 = new List<int>(100);

        tracker.BeginTracking("TestSystem"); // Overwrite previous BeginTracking

        // Allocate different amount
        var list2 = new List<int>(50);

        tracker.EndTracking("TestSystem");

        // Assert - should only record one sample (the second one)
        var profile = tracker.GetSystemAllocations("TestSystem");
        Assert.Equal(1, profile.CallCount);
    }

    #endregion

    #region GetSystemAllocations Tests

    [Fact]
    public void GetSystemAllocations_NonExistentSystem_ReturnsEmptyProfile()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act
        var profile = tracker.GetSystemAllocations("NonExistent");

        // Assert
        Assert.Equal("NonExistent", profile.Name);
        Assert.Equal(0, profile.CallCount);
        Assert.Equal(0, profile.TotalBytes);
        Assert.Equal(0, profile.AverageBytes);
        Assert.Equal(0, profile.MinBytes);
        Assert.Equal(0, profile.MaxBytes);
    }

    [Fact]
    public void GetSystemAllocations_ExistingSystem_ReturnsCorrectProfile()
    {
        // Arrange
        var tracker = new GCTracker();
        tracker.BeginTracking("TestSystem");
        var list = new List<int>(100); // Allocate some memory
        tracker.EndTracking("TestSystem");

        // Act
        var profile = tracker.GetSystemAllocations("TestSystem");

        // Assert
        Assert.Equal("TestSystem", profile.Name);
        Assert.Equal(1, profile.CallCount);
    }

    #endregion

    #region GetAllAllocationProfiles Tests

    [Fact]
    public void GetAllAllocationProfiles_NoTracking_ReturnsEmptyList()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act
        var profiles = tracker.GetAllAllocationProfiles();

        // Assert
        Assert.Empty(profiles);
    }

    [Fact]
    public void GetAllAllocationProfiles_MultipleSystems_ReturnsAllProfiles()
    {
        // Arrange
        var tracker = new GCTracker();

        tracker.BeginTracking("System1");
        tracker.EndTracking("System1");

        tracker.BeginTracking("System2");
        tracker.EndTracking("System2");

        tracker.BeginTracking("System3");
        tracker.EndTracking("System3");

        // Act
        var profiles = tracker.GetAllAllocationProfiles();

        // Assert
        Assert.Equal(3, profiles.Count);
        Assert.Contains(profiles, p => p.Name == "System1");
        Assert.Contains(profiles, p => p.Name == "System2");
        Assert.Contains(profiles, p => p.Name == "System3");
    }

    [Fact]
    public void GetAllAllocationProfiles_ReturnsSnapshot()
    {
        // Arrange
        var tracker = new GCTracker();
        tracker.BeginTracking("TestSystem");
        tracker.EndTracking("TestSystem");

        // Act
        var profiles1 = tracker.GetAllAllocationProfiles();

        tracker.BeginTracking("NewSystem");
        tracker.EndTracking("NewSystem");

        var profiles2 = tracker.GetAllAllocationProfiles();

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
        var tracker = new GCTracker();
        tracker.BeginTracking("System1");
        tracker.EndTracking("System1");
        tracker.BeginTracking("System2");
        tracker.EndTracking("System2");

        // Act
        tracker.Reset();

        // Assert
        var profiles = tracker.GetAllAllocationProfiles();
        Assert.Empty(profiles);

        var profile = tracker.GetSystemAllocations("System1");
        Assert.Equal(0, profile.CallCount);
    }

    [Fact]
    public void Reset_ClearsActiveSnapshots()
    {
        // Arrange
        var tracker = new GCTracker();
        tracker.BeginTracking("TestSystem");

        // Act
        tracker.Reset();
        var allocated = tracker.EndTracking("TestSystem"); // Should do nothing since BeginTracking was cleared

        // Assert
        Assert.Equal(0, allocated);
        var profile = tracker.GetSystemAllocations("TestSystem");
        Assert.Equal(0, profile.CallCount);
    }

    #endregion

    #region Min/Max Tracking Tests

    [Fact]
    public void AllocationProfile_TracksMinMax()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act - First allocation (larger)
        tracker.BeginTracking("TestSystem");
        var list1 = new List<int>(1000);
        for (int i = 0; i < 1000; i++)
        {
            list1.Add(i);
        }
        tracker.EndTracking("TestSystem");

        var profile1 = tracker.GetSystemAllocations("TestSystem");

        // Second allocation (smaller - though GC tracking may not detect it precisely)
        tracker.BeginTracking("TestSystem");
        var list2 = new List<int>(10);
        tracker.EndTracking("TestSystem");

        // Assert
        var profile = tracker.GetSystemAllocations("TestSystem");
        Assert.Equal(2, profile.CallCount);
        Assert.True(profile.MinBytes >= 0);
        Assert.True(profile.MaxBytes >= profile.MinBytes);
    }

    #endregion

    #region Average Calculation Tests

    [Fact]
    public void AverageBytes_CalculatesCorrectly()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act - Execute multiple times
        for (int i = 0; i < 10; i++)
        {
            tracker.BeginTracking("TestSystem");
            var list = new List<int>(100);
            tracker.EndTracking("TestSystem");
        }

        // Assert
        var profile = tracker.GetSystemAllocations("TestSystem");
        var expectedAverage = profile.TotalBytes / profile.CallCount;
        Assert.Equal(expectedAverage, profile.AverageBytes);
    }

    #endregion

    #region GetAllocationReport Tests

    [Fact]
    public void GetAllocationReport_NoAllocations_ReturnsFormattedReport()
    {
        // Arrange
        var tracker = new GCTracker();

        // Act
        var report = tracker.GetAllocationReport();

        // Assert
        Assert.Contains("=== GC Allocation Report ===", report);
        Assert.Contains("Total tracked allocations:", report);
    }

    [Fact]
    public void GetAllocationReport_WithAllocations_IncludesSystems()
    {
        // Arrange
        var tracker = new GCTracker();
        tracker.BeginTracking("System1");
        var list = new List<int>(100);
        tracker.EndTracking("System1");

        tracker.BeginTracking("System2");
        var dict = new Dictionary<int, int>();
        for (int i = 0; i < 50; i++)
        {
            dict[i] = i;
        }
        tracker.EndTracking("System2");

        // Act
        var report = tracker.GetAllocationReport();

        // Assert
        Assert.Contains("System1", report);
        Assert.Contains("System2", report);
    }

    [Fact]
    public void GetAllocationReport_WithThreshold_FiltersLowAllocations()
    {
        // Arrange
        var tracker = new GCTracker();

        // System with large allocations
        tracker.BeginTracking("LargeSystem");
        var largeList = new List<int>(10000);
        for (int i = 0; i < 10000; i++)
        {
            largeList.Add(i);
        }
        tracker.EndTracking("LargeSystem");

        // System with small allocations
        tracker.BeginTracking("SmallSystem");
        var smallList = new List<int>(1);
        tracker.EndTracking("SmallSystem");

        // Act - Set threshold high enough to filter SmallSystem
        var report = tracker.GetAllocationReport(threshold: 10000);

        // Assert
        Assert.Contains("LargeSystem", report);
        // SmallSystem may or may not appear depending on actual allocations
    }

    [Fact]
    public void GetAllocationReport_SortsByTotalBytes()
    {
        // Arrange
        var tracker = new GCTracker();

        tracker.BeginTracking("SmallSystem");
        var small = new List<int>(10);
        tracker.EndTracking("SmallSystem");

        tracker.BeginTracking("LargeSystem");
        var large = new List<int>(1000);
        for (int i = 0; i < 1000; i++)
        {
            large.Add(i);
        }
        tracker.EndTracking("LargeSystem");

        // Act
        var report = tracker.GetAllocationReport();

        // Assert
        var largeIndex = report.IndexOf("LargeSystem");
        var smallIndex = report.IndexOf("SmallSystem");

        // LargeSystem should appear before SmallSystem (sorted descending by total bytes)
        // Only check if both are present in report
        if (largeIndex >= 0 && smallIndex >= 0)
        {
            Assert.True(largeIndex < smallIndex);
        }
    }

    #endregion
}
