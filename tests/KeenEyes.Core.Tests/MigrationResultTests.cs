using System.Text.Json;
using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the MigrationResult class.
/// </summary>
public class MigrationResultTests
{
    #region Succeeded Factory Tests

    [Fact]
    public void Succeeded_SetsSuccessToTrue()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });

        var result = MigrationResult.Succeeded(
            data, "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(100), []);

        Assert.True(result.Success);
    }

    [Fact]
    public void Succeeded_SetsAllProperties()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });
        var timings = new List<MigrationStepTiming>
        {
            new(new MigrationStep(1, 2), TimeSpan.FromMilliseconds(50)),
            new(new MigrationStep(2, 3), TimeSpan.FromMilliseconds(50))
        };

        var result = MigrationResult.Succeeded(
            data, "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(100), timings);

        Assert.Equal(data.GetRawText(), result.Data!.Value.GetRawText());
        Assert.Equal("TestComponent", result.ComponentTypeName);
        Assert.Equal(1, result.FromVersion);
        Assert.Equal(3, result.ToVersion);
        Assert.Equal(TimeSpan.FromMilliseconds(100), result.TotalElapsed);
        Assert.Equal(2, result.StepTimings.Count);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.FailedAtVersion);
    }

    #endregion

    #region Failed Factory Tests

    [Fact]
    public void Failed_SetsSuccessToFalse()
    {
        var result = MigrationResult.Failed(
            "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(50),
            "Migration not found");

        Assert.False(result.Success);
    }

    [Fact]
    public void Failed_SetsAllProperties()
    {
        var timings = new List<MigrationStepTiming>
        {
            new(new MigrationStep(1, 2), TimeSpan.FromMilliseconds(50))
        };

        var result = MigrationResult.Failed(
            "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(100),
            "Migration from v2 failed",
            failedAtVersion: 2,
            stepTimings: timings);

        Assert.Null(result.Data);
        Assert.Equal("TestComponent", result.ComponentTypeName);
        Assert.Equal(1, result.FromVersion);
        Assert.Equal(3, result.ToVersion);
        Assert.Equal(TimeSpan.FromMilliseconds(100), result.TotalElapsed);
        Assert.Equal("Migration from v2 failed", result.ErrorMessage);
        Assert.Equal(2, result.FailedAtVersion);
        Assert.Single(result.StepTimings);
    }

    [Fact]
    public void Failed_WithNullStepTimings_DefaultsToEmpty()
    {
        var result = MigrationResult.Failed(
            "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(50),
            "Error");

        Assert.Empty(result.StepTimings);
    }

    #endregion

    #region NoMigrationNeeded Factory Tests

    [Fact]
    public void NoMigrationNeeded_SetsSuccessToTrue()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });

        var result = MigrationResult.NoMigrationNeeded(data, "TestComponent", 3);

        Assert.True(result.Success);
    }

    [Fact]
    public void NoMigrationNeeded_SetsVersionsEqual()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });

        var result = MigrationResult.NoMigrationNeeded(data, "TestComponent", 3);

        Assert.Equal(3, result.FromVersion);
        Assert.Equal(3, result.ToVersion);
    }

    [Fact]
    public void NoMigrationNeeded_HasZeroElapsedTime()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });

        var result = MigrationResult.NoMigrationNeeded(data, "TestComponent", 3);

        Assert.Equal(TimeSpan.Zero, result.TotalElapsed);
    }

    [Fact]
    public void NoMigrationNeeded_HasEmptyStepTimings()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });

        var result = MigrationResult.NoMigrationNeeded(data, "TestComponent", 3);

        Assert.Empty(result.StepTimings);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesComponentName()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });
        var result = MigrationResult.Succeeded(
            data, "MyComponent", 1, 3,
            TimeSpan.FromMilliseconds(100), []);

        var str = result.ToString();

        Assert.Contains("MyComponent", str);
    }

    [Fact]
    public void ToString_IncludesStatus()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });
        var successResult = MigrationResult.Succeeded(
            data, "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(100), []);
        var failedResult = MigrationResult.Failed(
            "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(50), "Error");

        Assert.Contains("Success", successResult.ToString());
        Assert.Contains("Failed", failedResult.ToString());
    }

    [Fact]
    public void ToString_IncludesVersionRange()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });
        var result = MigrationResult.Succeeded(
            data, "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(100), []);

        var str = result.ToString();

        Assert.Contains("v1", str);
        Assert.Contains("v3", str);
    }

    [Fact]
    public void ToString_IncludesTotalTime()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });
        var result = MigrationResult.Succeeded(
            data, "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(123.45), []);

        var str = result.ToString();

        Assert.Contains("ms", str);
    }

    [Fact]
    public void ToString_IncludesStepTimings()
    {
        var data = JsonSerializer.SerializeToElement(new { value = 42 });
        var timings = new List<MigrationStepTiming>
        {
            new(new MigrationStep(1, 2), TimeSpan.FromMilliseconds(50))
        };
        var result = MigrationResult.Succeeded(
            data, "TestComponent", 1, 2,
            TimeSpan.FromMilliseconds(100), timings);

        var str = result.ToString();

        Assert.Contains("Steps", str);
    }

    [Fact]
    public void ToString_FailedResult_IncludesError()
    {
        var result = MigrationResult.Failed(
            "TestComponent", 1, 3,
            TimeSpan.FromMilliseconds(50),
            "No migration defined from v2 to v3",
            failedAtVersion: 2);

        var str = result.ToString();

        Assert.Contains("No migration defined", str);
        Assert.Contains("v2", str);
    }

    #endregion
}

/// <summary>
/// Tests for the MigrationStepTiming record struct.
/// </summary>
public class MigrationStepTimingTests
{
    [Fact]
    public void MigrationStepTiming_ToString_FormatsCorrectly()
    {
        var timing = new MigrationStepTiming(
            new MigrationStep(1, 2),
            TimeSpan.FromMilliseconds(50));

        var str = timing.ToString();

        Assert.Contains("v1", str);
        Assert.Contains("v2", str);
        Assert.Contains("ms", str);
    }

    [Fact]
    public void MigrationStepTiming_Equality_ComparesAll()
    {
        var timing1 = new MigrationStepTiming(
            new MigrationStep(1, 2),
            TimeSpan.FromMilliseconds(50));
        var timing2 = new MigrationStepTiming(
            new MigrationStep(1, 2),
            TimeSpan.FromMilliseconds(50));
        var timing3 = new MigrationStepTiming(
            new MigrationStep(2, 3),
            TimeSpan.FromMilliseconds(50));

        Assert.Equal(timing1, timing2);
        Assert.NotEqual(timing1, timing3);
    }
}
