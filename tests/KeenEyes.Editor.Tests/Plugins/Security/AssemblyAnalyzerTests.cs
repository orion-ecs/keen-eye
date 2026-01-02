// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="AssemblyAnalyzer"/> IL analysis.
/// </summary>
public sealed class AssemblyAnalyzerTests
{
    private readonly AssemblyAnalyzer analyzer;

    public AssemblyAnalyzerTests()
    {
        analyzer = new AssemblyAnalyzer();
    }

    #region Analyze - Basic Tests

    [Fact]
    public void Analyze_WithNonexistentFile_ReturnsError()
    {
        // Act
        var result = analyzer.Analyze("/nonexistent/path/assembly.dll");

        // Assert
        Assert.False(result.Passed);
        Assert.False(result.AnalysisCompleted);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage.ToLowerInvariant());
    }

    [Fact]
    public void Analyze_WithValidAssembly_ReturnsResult()
    {
        // Arrange - Use this test assembly
        var assemblyPath = typeof(AssemblyAnalyzerTests).Assembly.Location;

        // Act
        var result = analyzer.Analyze(assemblyPath);

        // Assert
        Assert.True(result.AnalysisCompleted);
        Assert.NotEmpty(result.AssemblyPath);
        Assert.NotEmpty(result.AssemblyHash);
        Assert.NotNull(result.AssemblyName);
    }

    [Fact]
    public void Analyze_SetsTimestamp()
    {
        // Arrange
        var assemblyPath = typeof(AssemblyAnalyzerTests).Assembly.Location;
        var beforeAnalysis = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = analyzer.Analyze(assemblyPath);

        // Assert
        Assert.True(result.AnalysisTimestamp > beforeAnalysis);
        Assert.True(result.AnalysisTimestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void Analyze_ComputesHash()
    {
        // Arrange
        var assemblyPath = typeof(AssemblyAnalyzerTests).Assembly.Location;

        // Act
        var result = analyzer.Analyze(assemblyPath);

        // Assert
        Assert.NotEmpty(result.AssemblyHash);
        Assert.Equal(64, result.AssemblyHash.Length); // SHA256 = 64 hex chars
        Assert.True(result.AssemblyHash.All(c => "0123456789abcdef".Contains(c)));
    }

    [Fact]
    public void Analyze_SameAssembly_ProducesSameHash()
    {
        // Arrange
        var assemblyPath = typeof(AssemblyAnalyzerTests).Assembly.Location;

        // Act
        var result1 = analyzer.Analyze(assemblyPath);
        var result2 = analyzer.Analyze(assemblyPath);

        // Assert
        Assert.Equal(result1.AssemblyHash, result2.AssemblyHash);
    }

    #endregion

    #region Analyze - Pattern Detection Tests

    [Fact]
    public void Analyze_DetectsFileSystemAccess()
    {
        // Arrange - Use the KeenEyes.Editor assembly which uses System.IO
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var result = analyzer.Analyze(assemblyPath);

        // Assert
        Assert.True(result.AnalysisCompleted);
        var fileFindings = result.GetFindingsByPattern(DetectionPattern.FileSystemAccess);
        Assert.NotEmpty(fileFindings);
    }

    [Fact]
    public void Analyze_WithConfiguration_RespectsEnabledPatterns()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            EnabledPatterns = new HashSet<DetectionPattern> { DetectionPattern.ProcessExecution }
        };
        var customAnalyzer = new AssemblyAnalyzer(configuration: config);
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var result = customAnalyzer.Analyze(assemblyPath);

        // Assert
        Assert.True(result.AnalysisCompleted);
        // Should not find FileSystemAccess since it's disabled
        var fileFindings = result.GetFindingsByPattern(DetectionPattern.FileSystemAccess);
        Assert.Empty(fileFindings);
    }

    #endregion

    #region DetectPatterns Tests

    [Fact]
    public void DetectPatterns_ReturnsFindings()
    {
        // Arrange
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var findings = analyzer.DetectPatterns(assemblyPath);

        // Assert
        Assert.NotNull(findings);
        // The assembly does file I/O so should have findings
        Assert.NotEmpty(findings);
    }

    [Fact]
    public void DetectPatterns_WithNonexistentFile_ReturnsEmpty()
    {
        // Act
        var findings = analyzer.DetectPatterns("/nonexistent/assembly.dll");

        // Assert
        Assert.Empty(findings);
    }

    #endregion

    #region Analysis Mode Tests

    [Fact]
    public void Analyze_WithWarnOnlyMode_PassesWithFindings()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Mode = AnalysisMode.WarnOnly
        };
        var customAnalyzer = new AssemblyAnalyzer(configuration: config);
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var result = customAnalyzer.Analyze(assemblyPath);

        // Assert
        Assert.True(result.AnalysisCompleted);
        // In WarnOnly mode, should pass even with findings
        Assert.True(result.Passed);
    }

    [Fact]
    public void Analyze_WithBlockMode_FailsOnCriticalFindings()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Mode = AnalysisMode.Block,
            BlockingSeverity = SecuritySeverity.Low // Very sensitive
        };
        var customAnalyzer = new AssemblyAnalyzer(configuration: config);
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var result = customAnalyzer.Analyze(assemblyPath);

        // Assert
        Assert.True(result.AnalysisCompleted);
        // Should fail because the assembly uses File I/O (Medium severity)
        // and we're blocking on Low and above
        if (result.Findings.Any(f => f.Severity >= SecuritySeverity.Low))
        {
            Assert.False(result.Passed);
        }
    }

    [Fact]
    public void Analyze_WithPromptUserMode_PassesForLaterPrompting()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Mode = AnalysisMode.PromptUser
        };
        var customAnalyzer = new AssemblyAnalyzer(configuration: config);
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var result = customAnalyzer.Analyze(assemblyPath);

        // Assert
        Assert.True(result.AnalysisCompleted);
        // In PromptUser mode, passes for now (user prompted later)
        Assert.True(result.Passed);
    }

    #endregion

    #region Finding Properties Tests

    [Fact]
    public void Finding_HasLocation()
    {
        // Arrange
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var result = analyzer.Analyze(assemblyPath);
        var finding = result.Findings.FirstOrDefault();

        // Assert
        if (finding != null)
        {
            Assert.NotEmpty(finding.Location);
        }
    }

    [Fact]
    public void Finding_HasDescription()
    {
        // Arrange
        var assemblyPath = typeof(PluginManifest).Assembly.Location;

        // Act
        var result = analyzer.Analyze(assemblyPath);
        var finding = result.Findings.FirstOrDefault();

        // Assert
        if (finding != null)
        {
            Assert.NotEmpty(finding.Description);
        }
    }

    #endregion

    #region Assembly Info Tests

    [Fact]
    public void Analyze_ExtractsAssemblyName()
    {
        // Arrange
        var assemblyPath = typeof(AssemblyAnalyzerTests).Assembly.Location;

        // Act
        var result = analyzer.Analyze(assemblyPath);

        // Assert
        Assert.NotNull(result.AssemblyName);
        Assert.Contains("KeenEyes", result.AssemblyName);
    }

    [Fact]
    public void Analyze_ExtractsAssemblyVersion()
    {
        // Arrange
        var assemblyPath = typeof(AssemblyAnalyzerTests).Assembly.Location;

        // Act
        var result = analyzer.Analyze(assemblyPath);

        // Assert
        Assert.NotNull(result.AssemblyVersion);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Analyze_WithInvalidFile_ReturnsError()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "This is not a valid assembly");

            // Act
            var result = analyzer.Analyze(tempFile);

            // Assert
            Assert.False(result.AnalysisCompleted);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Analyze_WithEmptyFile_ReturnsError()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Leave file empty

            // Act
            var result = analyzer.Analyze(tempFile);

            // Assert
            Assert.False(result.AnalysisCompleted);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion
}
