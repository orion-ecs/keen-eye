// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="AnalysisResult"/> model and helper methods.
/// </summary>
public sealed class AnalysisResultTests
{
    #region Factory Method Tests

    [Fact]
    public void Clean_ReturnsPassedResultWithNoFindings()
    {
        // Act
        var result = AnalysisResult.Clean("/path/to/assembly.dll", "abc123hash");

        // Assert
        Assert.True(result.Passed);
        Assert.Empty(result.Findings);
        Assert.Equal("/path/to/assembly.dll", result.AssemblyPath);
        Assert.Equal("abc123hash", result.AssemblyHash);
        Assert.True(result.AnalysisCompleted);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Error_ReturnsFailedResultWithErrorMessage()
    {
        // Act
        var result = AnalysisResult.Error("/path/to/bad.dll", "File not found");

        // Assert
        Assert.False(result.Passed);
        Assert.Empty(result.Findings);
        Assert.Equal("/path/to/bad.dll", result.AssemblyPath);
        Assert.Equal("File not found", result.ErrorMessage);
        Assert.False(result.AnalysisCompleted);
    }

    #endregion

    #region GetFindingsBySeverity Tests

    [Fact]
    public void GetFindingsBySeverity_FiltersCorrectly()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            SecurityFinding.Environment("Test1", "Env.Get"),      // Low
            SecurityFinding.FileSystem("Test2", "File.Read"),     // Medium
            SecurityFinding.PInvoke("Test3", "dll", "func"),      // High
            SecurityFinding.ProcessExec("Test4", "Process.Start") // Critical
        };

        var result = new AnalysisResult
        {
            AssemblyPath = "/test.dll",
            Passed = true,
            Findings = findings,
            AnalysisTimestamp = DateTime.UtcNow,
            AssemblyHash = "hash123"
        };

        // Act
        var highAndAbove = result.GetFindingsBySeverity(SecuritySeverity.High).ToList();
        var mediumAndAbove = result.GetFindingsBySeverity(SecuritySeverity.Medium).ToList();

        // Assert
        Assert.Equal(2, highAndAbove.Count); // High + Critical
        Assert.Equal(3, mediumAndAbove.Count); // Medium + High + Critical
    }

    [Fact]
    public void GetFindingsBySeverity_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            SecurityFinding.Environment("Test1", "Env.Get") // Low severity
        };

        var result = new AnalysisResult
        {
            AssemblyPath = "/test.dll",
            Passed = true,
            Findings = findings,
            AnalysisTimestamp = DateTime.UtcNow,
            AssemblyHash = "hash123"
        };

        // Act
        var criticalFindings = result.GetFindingsBySeverity(SecuritySeverity.Critical).ToList();

        // Assert
        Assert.Empty(criticalFindings);
    }

    #endregion

    #region GetFindingsByPattern Tests

    [Fact]
    public void GetFindingsByPattern_FiltersCorrectly()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            SecurityFinding.FileSystem("Test1", "File.Read"),
            SecurityFinding.FileSystem("Test2", "File.Write"),
            SecurityFinding.Network("Test3", "HttpClient.Get")
        };

        var result = new AnalysisResult
        {
            AssemblyPath = "/test.dll",
            Passed = true,
            Findings = findings,
            AnalysisTimestamp = DateTime.UtcNow,
            AssemblyHash = "hash123"
        };

        // Act
        var fileSystemFindings = result.GetFindingsByPattern(DetectionPattern.FileSystemAccess).ToList();
        var networkFindings = result.GetFindingsByPattern(DetectionPattern.NetworkAccess).ToList();
        var reflectionFindings = result.GetFindingsByPattern(DetectionPattern.Reflection).ToList();

        // Assert
        Assert.Equal(2, fileSystemFindings.Count);
        Assert.Single(networkFindings);
        Assert.Empty(reflectionFindings);
    }

    #endregion

    #region HasFindingsAtOrAbove Tests

    [Fact]
    public void HasFindingsAtOrAbove_ReturnsTrueWhenFindingsExist()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            SecurityFinding.FileSystem("Test", "File.Read") // Medium severity
        };

        var result = new AnalysisResult
        {
            AssemblyPath = "/test.dll",
            Passed = true,
            Findings = findings,
            AnalysisTimestamp = DateTime.UtcNow,
            AssemblyHash = "hash123"
        };

        // Assert
        Assert.True(result.HasFindingsAtOrAbove(SecuritySeverity.Low));
        Assert.True(result.HasFindingsAtOrAbove(SecuritySeverity.Medium));
        Assert.False(result.HasFindingsAtOrAbove(SecuritySeverity.High));
        Assert.False(result.HasFindingsAtOrAbove(SecuritySeverity.Critical));
    }

    [Fact]
    public void HasFindingsAtOrAbove_WithNoFindings_ReturnsFalse()
    {
        // Arrange
        var result = AnalysisResult.Clean("/test.dll", "hash123");

        // Assert
        Assert.False(result.HasFindingsAtOrAbove(SecuritySeverity.Info));
    }

    #endregion

    #region GetFindingSummary Tests

    [Fact]
    public void GetFindingSummary_GroupsByPattern()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            SecurityFinding.FileSystem("Test1", "File.Read"),
            SecurityFinding.FileSystem("Test2", "File.Write"),
            SecurityFinding.Network("Test3", "HttpClient.Get"),
            SecurityFinding.Reflection("Test4", "Type.GetMethod")
        };

        var result = new AnalysisResult
        {
            AssemblyPath = "/test.dll",
            Passed = true,
            Findings = findings,
            AnalysisTimestamp = DateTime.UtcNow,
            AssemblyHash = "hash123"
        };

        // Act
        var summary = result.GetFindingSummary();

        // Assert
        Assert.Equal(3, summary.Count);
        Assert.Equal(2, summary[DetectionPattern.FileSystemAccess]);
        Assert.Equal(1, summary[DetectionPattern.NetworkAccess]);
        Assert.Equal(1, summary[DetectionPattern.Reflection]);
    }

    [Fact]
    public void GetFindingSummary_WithNoFindings_ReturnsEmptyDictionary()
    {
        // Arrange
        var result = AnalysisResult.Clean("/test.dll", "hash123");

        // Act
        var summary = result.GetFindingSummary();

        // Assert
        Assert.Empty(summary);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithPassedResult_ShowsNoFindings()
    {
        // Arrange
        var result = AnalysisResult.Clean("/path/to/clean.dll", "hash123");

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("Passed", str);
        Assert.Contains("no findings", str);
    }

    [Fact]
    public void ToString_WithFindings_ShowsFindingCount()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            SecurityFinding.FileSystem("Test1", "File.Read"),
            SecurityFinding.Network("Test2", "HttpClient.Get")
        };

        var result = new AnalysisResult
        {
            AssemblyPath = "/test.dll",
            Passed = false,
            Findings = findings,
            AnalysisTimestamp = DateTime.UtcNow,
            AssemblyHash = "hash123"
        };

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("2 findings", str);
    }

    [Fact]
    public void ToString_WithError_ShowsErrorMessage()
    {
        // Arrange
        var result = AnalysisResult.Error("/bad.dll", "Assembly corrupted");

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("failed", str.ToLowerInvariant());
        Assert.Contains("Assembly corrupted", str);
    }

    #endregion

    #region AnalysisCompleted Tests

    [Fact]
    public void AnalysisCompleted_TrueWhenNoError()
    {
        // Arrange
        var result = new AnalysisResult
        {
            AssemblyPath = "/test.dll",
            Passed = true,
            Findings = [],
            AnalysisTimestamp = DateTime.UtcNow,
            AssemblyHash = "hash123"
        };

        // Assert
        Assert.True(result.AnalysisCompleted);
    }

    [Fact]
    public void AnalysisCompleted_FalseWhenError()
    {
        // Arrange
        var result = AnalysisResult.Error("/bad.dll", "Some error");

        // Assert
        Assert.False(result.AnalysisCompleted);
    }

    #endregion
}
