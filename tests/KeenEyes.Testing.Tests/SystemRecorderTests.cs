using KeenEyes.Testing.Systems;

namespace KeenEyes.Testing.Tests;

/// <summary>
/// Unit tests for the <see cref="SystemRecorder"/> and <see cref="SystemRecorderAssertions"/> classes.
/// </summary>
public class SystemRecorderTests : IDisposable
{
    private readonly SystemRecorder recorder = new();

    public void Dispose()
    {
        recorder.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Test Systems

    private sealed class TestSystem : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    private sealed class AnotherTestSystem : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    private sealed class UnusedSystem : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void SystemRecorder_InitialState_HasNoCalls()
    {
        Assert.Empty(recorder.Calls);
        Assert.Equal(0, recorder.TotalCallCount);
    }

    #endregion

    #region RecordCall Tests

    [Fact]
    public void RecordCall_Generic_AddsToCalls()
    {
        // Act
        recorder.RecordCall<TestSystem>(0.016f);

        // Assert
        Assert.Single(recorder.Calls);
        Assert.Equal(typeof(TestSystem), recorder.Calls[0].SystemType);
        Assert.Equal("TestSystem", recorder.Calls[0].SystemName);
        Assert.Equal(0.016f, recorder.Calls[0].DeltaTime);
    }

    [Fact]
    public void RecordCall_MultipleCalls_TracksAll()
    {
        // Act
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<AnotherTestSystem>(0.032f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Assert
        Assert.Equal(3, recorder.TotalCallCount);
    }

    [Fact]
    public void RecordCall_RecordsTimestamp()
    {
        // Arrange
        var beforeRecord = DateTime.UtcNow;

        // Act
        recorder.RecordCall<TestSystem>(0.016f);

        // Assert
        var afterRecord = DateTime.UtcNow;
        Assert.InRange(recorder.Calls[0].Timestamp, beforeRecord, afterRecord);
    }

    #endregion

    #region WasCalled Tests

    [Fact]
    public void WasCalled_WhenSystemWasCalled_ReturnsTrue()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        Assert.True(recorder.WasCalled<TestSystem>());
    }

    [Fact]
    public void WasCalled_WhenSystemWasNotCalled_ReturnsFalse()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        Assert.False(recorder.WasCalled<AnotherTestSystem>());
    }

    [Fact]
    public void WasCalled_ByName_WhenSystemWasCalled_ReturnsTrue()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        Assert.True(recorder.WasCalled("TestSystem"));
    }

    [Fact]
    public void WasCalled_ByName_WhenSystemWasNotCalled_ReturnsFalse()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        Assert.False(recorder.WasCalled("AnotherTestSystem"));
    }

    #endregion

    #region GetCallCount Tests

    [Fact]
    public void GetCallCount_WhenNeverCalled_ReturnsZero()
    {
        Assert.Equal(0, recorder.GetCallCount<TestSystem>());
    }

    [Fact]
    public void GetCallCount_WhenCalledMultipleTimes_ReturnsCorrectCount()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        Assert.Equal(3, recorder.GetCallCount<TestSystem>());
    }

    [Fact]
    public void GetCallCount_ByName_ReturnsCorrectCount()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        Assert.Equal(2, recorder.GetCallCount("TestSystem"));
    }

    #endregion

    #region GetCalls Tests

    [Fact]
    public void GetCalls_ReturnsOnlyCallsForSpecifiedSystem()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<AnotherTestSystem>(0.032f);
        recorder.RecordCall<TestSystem>(0.048f);

        // Act
        var calls = recorder.GetCalls<TestSystem>();

        // Assert
        Assert.Equal(2, calls.Count);
        Assert.All(calls, c => Assert.Equal(typeof(TestSystem), c.SystemType));
    }

    [Fact]
    public void GetCalls_WhenNeverCalled_ReturnsEmptyList()
    {
        var calls = recorder.GetCalls<UnusedSystem>();
        Assert.Empty(calls);
    }

    #endregion

    #region GetLastCall Tests

    [Fact]
    public void GetLastCall_ReturnsLastCallForSystem()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.032f);
        recorder.RecordCall<TestSystem>(0.048f);

        // Act
        var lastCall = recorder.GetLastCall<TestSystem>();

        // Assert
        Assert.NotNull(lastCall);
        Assert.Equal(0.048f, lastCall.Value.DeltaTime);
    }

    [Fact]
    public void GetLastCall_WhenNeverCalled_ReturnsNull()
    {
        var lastCall = recorder.GetLastCall<UnusedSystem>();
        Assert.Null(lastCall);
    }

    #endregion

    #region GetTotalDeltaTime Tests

    [Fact]
    public void GetTotalDeltaTime_SumsAllDeltaTimes()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act
        var totalTime = recorder.GetTotalDeltaTime<TestSystem>();

        // Assert
        Assert.Equal(0.048f, totalTime, 5);
    }

    [Fact]
    public void GetTotalDeltaTime_WhenNeverCalled_ReturnsZero()
    {
        Assert.Equal(0f, recorder.GetTotalDeltaTime<UnusedSystem>());
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllCalls()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<AnotherTestSystem>(0.032f);

        // Act
        recorder.Clear();

        // Assert
        Assert.Empty(recorder.Calls);
        Assert.Equal(0, recorder.TotalCallCount);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task RecordCall_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int callsPerThread = 100;
        var tasks = new Task[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < callsPerThread; j++)
                {
                    recorder.RecordCall<TestSystem>(0.016f);
                }
            }, TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(threadCount * callsPerThread, recorder.TotalCallCount);
    }

    #endregion

    #region Assertion Tests - ShouldHaveCalledSystem

    [Fact]
    public void ShouldHaveCalledSystem_WhenCalled_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw
        recorder.ShouldHaveCalledSystem<TestSystem>();
    }

    [Fact]
    public void ShouldHaveCalledSystem_WhenNotCalled_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveCalledSystem<TestSystem>());
        Assert.Contains("TestSystem", ex.Message);
        Assert.Contains("was not", ex.Message);
    }

    [Fact]
    public void ShouldHaveCalledSystem_WithReason_IncludesReasonInMessage()
    {
        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveCalledSystem<TestSystem>("movement should update"));
        Assert.Contains("because movement should update", ex.Message);
    }

    [Fact]
    public void ShouldHaveCalledSystem_ByName_WhenCalled_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw
        recorder.ShouldHaveCalledSystem("TestSystem");
    }

    [Fact]
    public void ShouldHaveCalledSystem_ByName_WhenNotCalled_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveCalledSystem("UnknownSystem"));
        Assert.Contains("UnknownSystem", ex.Message);
    }

    #endregion

    #region Assertion Tests - ShouldHaveCalledSystemTimes

    [Fact]
    public void ShouldHaveCalledSystemTimes_WhenCountMatches_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw
        recorder.ShouldHaveCalledSystemTimes<TestSystem>(3);
    }

    [Fact]
    public void ShouldHaveCalledSystemTimes_WhenCountDoesNotMatch_Throws()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveCalledSystemTimes<TestSystem>(5));
        Assert.Contains("5 time(s)", ex.Message);
        Assert.Contains("2 time(s)", ex.Message);
    }

    #endregion

    #region Assertion Tests - ShouldHaveCalledSystemAtLeast

    [Fact]
    public void ShouldHaveCalledSystemAtLeast_WhenCountMeetsMinimum_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw
        recorder.ShouldHaveCalledSystemAtLeast<TestSystem>(2);
    }

    [Fact]
    public void ShouldHaveCalledSystemAtLeast_WhenCountExactlyMeetsMinimum_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw
        recorder.ShouldHaveCalledSystemAtLeast<TestSystem>(2);
    }

    [Fact]
    public void ShouldHaveCalledSystemAtLeast_WhenCountBelowMinimum_Throws()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveCalledSystemAtLeast<TestSystem>(5));
        Assert.Contains("at least 5 time(s)", ex.Message);
        Assert.Contains("only 1 time(s)", ex.Message);
    }

    #endregion

    #region Assertion Tests - ShouldNotHaveCalledSystem

    [Fact]
    public void ShouldNotHaveCalledSystem_WhenNotCalled_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw
        recorder.ShouldNotHaveCalledSystem<UnusedSystem>();
    }

    [Fact]
    public void ShouldNotHaveCalledSystem_WhenCalled_Throws()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldNotHaveCalledSystem<TestSystem>());
        Assert.Contains("TestSystem", ex.Message);
        Assert.Contains("to not have been called", ex.Message);
        Assert.Contains("2 time(s)", ex.Message);
    }

    #endregion

    #region Assertion Tests - ShouldHaveTotalCallCount

    [Fact]
    public void ShouldHaveTotalCallCount_WhenCountMatches_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<AnotherTestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw
        recorder.ShouldHaveTotalCallCount(3);
    }

    [Fact]
    public void ShouldHaveTotalCallCount_WhenCountDoesNotMatch_Throws()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveTotalCallCount(5));
        Assert.Contains("5 system call(s)", ex.Message);
        Assert.Contains("1 call(s)", ex.Message);
    }

    #endregion

    #region Assertion Tests - ShouldHaveNoCalls

    [Fact]
    public void ShouldHaveNoCalls_WhenEmpty_DoesNotThrow()
    {
        // Act & Assert - should not throw
        recorder.ShouldHaveNoCalls();
    }

    [Fact]
    public void ShouldHaveNoCalls_WhenHasCalls_Throws()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<AnotherTestSystem>(0.016f);

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveNoCalls());
        Assert.Contains("no system calls", ex.Message);
        Assert.Contains("2 call(s)", ex.Message);
        Assert.Contains("TestSystem", ex.Message);
        Assert.Contains("AnotherTestSystem", ex.Message);
    }

    #endregion

    #region Assertion Tests - ShouldHaveAccumulatedDeltaTime

    [Fact]
    public void ShouldHaveAccumulatedDeltaTime_WhenTimeMeetsMinimum_DoesNotThrow()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert - should not throw (0.048 >= 0.04)
        recorder.ShouldHaveAccumulatedDeltaTime<TestSystem>(0.04f);
    }

    [Fact]
    public void ShouldHaveAccumulatedDeltaTime_WhenTimeBelowMinimum_Throws()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            recorder.ShouldHaveAccumulatedDeltaTime<TestSystem>(1.0f));
        Assert.Contains("at least 1.0000s", ex.Message);
        Assert.Contains("0.0160s", ex.Message);
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Assertions_CanBeChained()
    {
        // Arrange
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<TestSystem>(0.016f);
        recorder.RecordCall<AnotherTestSystem>(0.032f);

        // Act & Assert - all assertions should pass and chain
        recorder
            .ShouldHaveCalledSystem<TestSystem>()
            .ShouldHaveCalledSystemTimes<TestSystem>(2)
            .ShouldHaveCalledSystemAtLeast<TestSystem>(1)
            .ShouldNotHaveCalledSystem<UnusedSystem>()
            .ShouldHaveTotalCallCount(3);
    }

    #endregion

    #region Integration Tests with World

    [Fact]
    public void SystemRecorder_WithSystemHooks_RecordsCalls()
    {
        // Arrange
        using var world = new World();
        var system = new TestSystem();
        world.AddSystem(system);

        using var recorder = new SystemRecorder();
        recorder.AttachTo(world);

        // Act
        world.Update(0.016f);

        // Assert
        recorder.ShouldHaveCalledSystem<TestSystem>();
        Assert.Equal(0.016f, recorder.Calls[0].DeltaTime);
    }

    [Fact]
    public void SystemRecorder_WithMultipleSteps_RecordsAll()
    {
        // Arrange
        using var world = new World();
        var system = new TestSystem();
        world.AddSystem(system);

        using var recorder = new SystemRecorder();
        recorder.AttachTo(world);

        // Act
        world.Update(0.016f);
        world.Update(0.016f);
        world.Update(0.016f);

        // Assert
        recorder.ShouldHaveCalledSystemTimes<TestSystem>(3);
    }

    [Fact]
    public void SystemRecorder_DisposesHookSubscription()
    {
        // Arrange
        using var world = new World();
        var system = new TestSystem();
        world.AddSystem(system);

        var recorder = new SystemRecorder();
        recorder.AttachTo(world);

        // Act - dispose recorder
        recorder.Dispose();
        world.Update(0.016f);

        // Assert - no calls recorded after dispose
        Assert.Equal(0, recorder.TotalCallCount);
    }

    #endregion

    #region TestWorldBuilder Integration Tests

    [Fact]
    public void TestWorldBuilder_WithSystemRecording_CreatesRecorder()
    {
        // Arrange & Act
        using var testWorld = new TestWorldBuilder()
            .WithSystemRecording()
            .Build();

        // Assert
        Assert.NotNull(testWorld.SystemRecorder);
        Assert.True(testWorld.HasSystemRecording);
    }

    [Fact]
    public void TestWorldBuilder_WithoutSystemRecording_NoRecorder()
    {
        // Arrange & Act
        using var testWorld = new TestWorldBuilder()
            .Build();

        // Assert
        Assert.Null(testWorld.SystemRecorder);
        Assert.False(testWorld.HasSystemRecording);
    }

    [Fact]
    public void TestWorldBuilder_WithSystemRecording_AutoAttachesToWorld()
    {
        // Arrange
        using var testWorld = new TestWorldBuilder()
            .WithSystemRecording()
            .WithSystem<TestSystem>()
            .WithManualTime()
            .Build();

        // Act
        testWorld.Step();

        // Assert
        testWorld.SystemRecorder!.ShouldHaveCalledSystem<TestSystem>();
    }

    [Fact]
    public void TestWorldBuilder_WithSystemRecording_FullIntegration()
    {
        // Arrange
        using var testWorld = new TestWorldBuilder()
            .WithSystemRecording()
            .WithSystem<TestSystem>()
            .WithSystem<AnotherTestSystem>()
            .WithManualTime()
            .Build();

        // Act
        testWorld.Step();
        testWorld.Step();
        testWorld.Step();

        // Assert
        testWorld.SystemRecorder!
            .ShouldHaveCalledSystem<TestSystem>()
            .ShouldHaveCalledSystemTimes<TestSystem>(3)
            .ShouldHaveCalledSystem<AnotherTestSystem>()
            .ShouldHaveCalledSystemTimes<AnotherTestSystem>(3)
            .ShouldNotHaveCalledSystem<UnusedSystem>()
            .ShouldHaveTotalCallCount(6);
    }

    [Fact]
    public void TestWorldBuilder_WithSystemRecording_DisposesOnTestWorldDispose()
    {
        // Arrange
        var testWorld = new TestWorldBuilder()
            .WithSystemRecording()
            .WithSystem<TestSystem>()
            .WithManualTime()
            .Build();

        var recorder = testWorld.SystemRecorder!;
        testWorld.Step();
        Assert.Equal(1, recorder.TotalCallCount);

        // Act
        testWorld.Dispose();

        // Assert - recorder is disposed but existing calls are preserved
        Assert.Equal(1, recorder.TotalCallCount);
    }

    #endregion
}
