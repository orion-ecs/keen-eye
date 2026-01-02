// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="AnalysisConfiguration"/> and <see cref="PatternException"/>.
/// </summary>
public sealed class AnalysisConfigurationTests
{
    #region Default Configuration Tests

    [Fact]
    public void Default_HasWarnOnlyMode()
    {
        // Assert
        Assert.Equal(AnalysisMode.WarnOnly, AnalysisConfiguration.Default.Mode);
    }

    [Fact]
    public void Default_HasCriticalBlockingSeverity()
    {
        // Assert
        Assert.Equal(SecuritySeverity.Critical, AnalysisConfiguration.Default.BlockingSeverity);
    }

    [Fact]
    public void Default_EnablesAllPatterns()
    {
        // Act
        var config = AnalysisConfiguration.Default;

        // Assert
        foreach (var pattern in Enum.GetValues<DetectionPattern>())
        {
            Assert.True(config.IsPatternEnabled(pattern), $"Pattern {pattern} should be enabled by default");
        }
    }

    [Fact]
    public void Default_HasNoExceptions()
    {
        // Assert
        Assert.Empty(AnalysisConfiguration.Default.Exceptions);
    }

    #endregion

    #region Strict Configuration Tests

    [Fact]
    public void Strict_HasBlockMode()
    {
        // Assert
        Assert.Equal(AnalysisMode.Block, AnalysisConfiguration.Strict.Mode);
    }

    [Fact]
    public void Strict_HasHighBlockingSeverity()
    {
        // Assert
        Assert.Equal(SecuritySeverity.High, AnalysisConfiguration.Strict.BlockingSeverity);
    }

    #endregion

    #region Permissive Configuration Tests

    [Fact]
    public void Permissive_HasWarnOnlyMode()
    {
        // Assert
        Assert.Equal(AnalysisMode.WarnOnly, AnalysisConfiguration.Permissive.Mode);
    }

    [Fact]
    public void Permissive_HasCriticalBlockingSeverity()
    {
        // Assert
        Assert.Equal(SecuritySeverity.Critical, AnalysisConfiguration.Permissive.BlockingSeverity);
    }

    #endregion

    #region IsPatternEnabled Tests

    [Fact]
    public void IsPatternEnabled_WithEnabledPattern_ReturnsTrue()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            EnabledPatterns = new HashSet<DetectionPattern> { DetectionPattern.Reflection, DetectionPattern.PInvoke }
        };

        // Assert
        Assert.True(config.IsPatternEnabled(DetectionPattern.Reflection));
        Assert.True(config.IsPatternEnabled(DetectionPattern.PInvoke));
    }

    [Fact]
    public void IsPatternEnabled_WithDisabledPattern_ReturnsFalse()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            EnabledPatterns = new HashSet<DetectionPattern> { DetectionPattern.Reflection }
        };

        // Assert
        Assert.False(config.IsPatternEnabled(DetectionPattern.PInvoke));
        Assert.False(config.IsPatternEnabled(DetectionPattern.FileSystemAccess));
    }

    [Fact]
    public void IsPatternEnabled_WithEmptySet_ReturnsFalse()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            EnabledPatterns = new HashSet<DetectionPattern>()
        };

        // Assert
        Assert.False(config.IsPatternEnabled(DetectionPattern.Reflection));
    }

    #endregion

    #region IsExcepted Tests

    [Fact]
    public void IsExcepted_WithMatchingPattern_ReturnsTrue()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Exceptions =
            [
                new PatternException { Pattern = DetectionPattern.Reflection }
            ]
        };
        var finding = SecurityFinding.Reflection("Test", "System.Type.GetMethod");

        // Act & Assert
        Assert.True(config.IsExcepted(finding));
    }

    [Fact]
    public void IsExcepted_WithNonMatchingPattern_ReturnsFalse()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Exceptions =
            [
                new PatternException { Pattern = DetectionPattern.Reflection }
            ]
        };
        var finding = SecurityFinding.FileSystem("Test", "System.IO.File.Read");

        // Act & Assert
        Assert.False(config.IsExcepted(finding));
    }

    [Fact]
    public void IsExcepted_WithMatchingMemberPattern_ReturnsTrue()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Exceptions =
            [
                new PatternException
                {
                    Pattern = DetectionPattern.FileSystemAccess,
                    MemberPattern = "System.IO.File.Exists"
                }
            ]
        };
        var finding = SecurityFinding.FileSystem("Test", "System.IO.File.Exists");

        // Act & Assert
        Assert.True(config.IsExcepted(finding));
    }

    [Fact]
    public void IsExcepted_WithWildcardMemberPattern_MatchesPrefix()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Exceptions =
            [
                new PatternException
                {
                    Pattern = DetectionPattern.FileSystemAccess,
                    MemberPattern = "System.IO.Path.*"
                }
            ]
        };
        var finding1 = SecurityFinding.FileSystem("Test", "System.IO.Path.GetFileName");
        var finding2 = SecurityFinding.FileSystem("Test", "System.IO.Path.Combine");
        var finding3 = SecurityFinding.FileSystem("Test", "System.IO.File.Read");

        // Act & Assert
        Assert.True(config.IsExcepted(finding1));
        Assert.True(config.IsExcepted(finding2));
        Assert.False(config.IsExcepted(finding3));
    }

    [Fact]
    public void IsExcepted_WithNoExceptions_ReturnsFalse()
    {
        // Arrange
        var config = new AnalysisConfiguration { Exceptions = [] };
        var finding = SecurityFinding.Reflection("Test", "Type.GetMethod");

        // Act & Assert
        Assert.False(config.IsExcepted(finding));
    }

    #endregion

    #region PatternException.Matches Tests

    [Fact]
    public void PatternException_Matches_WithNullPattern_MatchesAnyPattern()
    {
        // Arrange
        var exception = new PatternException { Pattern = null };
        var finding1 = SecurityFinding.Reflection("Test", "Type.GetMethod");
        var finding2 = SecurityFinding.FileSystem("Test", "File.Read");

        // Assert
        Assert.True(exception.Matches(finding1));
        Assert.True(exception.Matches(finding2));
    }

    [Fact]
    public void PatternException_Matches_WithNullMemberPattern_MatchesAnyMember()
    {
        // Arrange
        var exception = new PatternException
        {
            Pattern = DetectionPattern.FileSystemAccess,
            MemberPattern = null
        };
        var finding1 = SecurityFinding.FileSystem("Test", "System.IO.File.Read");
        var finding2 = SecurityFinding.FileSystem("Test", "System.IO.Directory.Create");

        // Assert
        Assert.True(exception.Matches(finding1));
        Assert.True(exception.Matches(finding2));
    }

    [Fact]
    public void PatternException_Matches_ExactMemberMatch()
    {
        // Arrange
        var exception = new PatternException
        {
            Pattern = DetectionPattern.Reflection,
            MemberPattern = "System.Type.GetMethod"
        };
        var exactMatch = SecurityFinding.Reflection("Test", "System.Type.GetMethod");
        var noMatch = SecurityFinding.Reflection("Test", "System.Type.GetMethods");

        // Assert
        Assert.True(exception.Matches(exactMatch));
        Assert.False(exception.Matches(noMatch));
    }

    #endregion

    #region Custom Configuration Tests

    [Fact]
    public void CustomConfiguration_CanDisableSpecificPatterns()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            EnabledPatterns = new HashSet<DetectionPattern>
            {
                DetectionPattern.ProcessExecution,
                DetectionPattern.PInvoke
            }
        };

        // Assert
        Assert.True(config.IsPatternEnabled(DetectionPattern.ProcessExecution));
        Assert.True(config.IsPatternEnabled(DetectionPattern.PInvoke));
        Assert.False(config.IsPatternEnabled(DetectionPattern.Reflection));
        Assert.False(config.IsPatternEnabled(DetectionPattern.FileSystemAccess));
    }

    [Fact]
    public void CustomConfiguration_CanSetCustomBlockingSeverity()
    {
        // Arrange
        var config = new AnalysisConfiguration
        {
            Mode = AnalysisMode.Block,
            BlockingSeverity = SecuritySeverity.Medium
        };

        // Assert
        Assert.Equal(SecuritySeverity.Medium, config.BlockingSeverity);
    }

    #endregion
}
