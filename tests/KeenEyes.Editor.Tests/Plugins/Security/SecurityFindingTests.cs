// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="SecurityFinding"/> factory methods.
/// </summary>
public sealed class SecurityFindingTests
{
    #region Factory Method Tests

    [Fact]
    public void Reflection_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.Reflection("TestClass.TestMethod", "System.Type.GetMethod");

        // Assert
        Assert.Equal(DetectionPattern.Reflection, finding.Pattern);
        Assert.Equal(SecuritySeverity.Medium, finding.Severity);
        Assert.Equal("TestClass.TestMethod", finding.Location);
        Assert.Equal("System.Type.GetMethod", finding.MemberReference);
        Assert.Contains("reflection", finding.Description.ToLowerInvariant());
    }

    [Fact]
    public void Unsafe_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.Unsafe("TestNamespace.UnsafeClass.Method");

        // Assert
        Assert.Equal(DetectionPattern.UnsafeCode, finding.Pattern);
        Assert.Equal(SecuritySeverity.High, finding.Severity);
        Assert.Equal("TestNamespace.UnsafeClass.Method", finding.Location);
        Assert.Contains("unsafe", finding.Description.ToLowerInvariant());
    }

    [Fact]
    public void PInvoke_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.PInvoke("NativeMethods.MessageBox", "user32.dll", "MessageBoxW");

        // Assert
        Assert.Equal(DetectionPattern.PInvoke, finding.Pattern);
        Assert.Equal(SecuritySeverity.High, finding.Severity);
        Assert.Equal("NativeMethods.MessageBox", finding.Location);
        Assert.Equal("user32.dll::MessageBoxW", finding.MemberReference);
        Assert.Contains("P/Invoke", finding.Description);
        Assert.Contains("user32.dll", finding.Description);
    }

    [Fact]
    public void FileSystem_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.FileSystem("DataReader.Load", "System.IO.File.ReadAllText");

        // Assert
        Assert.Equal(DetectionPattern.FileSystemAccess, finding.Pattern);
        Assert.Equal(SecuritySeverity.Medium, finding.Severity);
        Assert.Equal("DataReader.Load", finding.Location);
        Assert.Equal("System.IO.File.ReadAllText", finding.MemberReference);
    }

    [Fact]
    public void Network_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.Network("ApiClient.Get", "System.Net.Http.HttpClient.GetAsync");

        // Assert
        Assert.Equal(DetectionPattern.NetworkAccess, finding.Pattern);
        Assert.Equal(SecuritySeverity.Medium, finding.Severity);
        Assert.Equal("ApiClient.Get", finding.Location);
        Assert.Equal("System.Net.Http.HttpClient.GetAsync", finding.MemberReference);
    }

    [Fact]
    public void ProcessExec_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.ProcessExec("Launcher.Run", "System.Diagnostics.Process.Start");

        // Assert
        Assert.Equal(DetectionPattern.ProcessExecution, finding.Pattern);
        Assert.Equal(SecuritySeverity.Critical, finding.Severity);
        Assert.Equal("Launcher.Run", finding.Location);
        Assert.Equal("System.Diagnostics.Process.Start", finding.MemberReference);
    }

    [Fact]
    public void Environment_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.Environment("Config.GetPath", "System.Environment.GetEnvironmentVariable");

        // Assert
        Assert.Equal(DetectionPattern.EnvironmentAccess, finding.Pattern);
        Assert.Equal(SecuritySeverity.Low, finding.Severity);
        Assert.Equal("Config.GetPath", finding.Location);
    }

    [Fact]
    public void AssemblyLoad_CreatesCorrectFinding()
    {
        // Act
        var finding = SecurityFinding.AssemblyLoad("PluginLoader.Load", "System.Reflection.Assembly.LoadFrom");

        // Assert
        Assert.Equal(DetectionPattern.AssemblyLoading, finding.Pattern);
        Assert.Equal(SecuritySeverity.High, finding.Severity);
        Assert.Equal("PluginLoader.Load", finding.Location);
        Assert.Contains("assembly", finding.Description.ToLowerInvariant());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesAllRelevantInfo()
    {
        // Arrange
        var finding = SecurityFinding.FileSystem("MyClass.ReadData", "System.IO.File.ReadAllText");

        // Act
        var str = finding.ToString();

        // Assert
        Assert.Contains("Medium", str);
        Assert.Contains("FileSystemAccess", str);
        Assert.Contains("MyClass.ReadData", str);
    }

    #endregion

    #region Severity Ordering Tests

    [Fact]
    public void Severity_OrdersCorrectly()
    {
        // Assert ordering: Info < Low < Medium < High < Critical
        Assert.True(SecuritySeverity.Info < SecuritySeverity.Low);
        Assert.True(SecuritySeverity.Low < SecuritySeverity.Medium);
        Assert.True(SecuritySeverity.Medium < SecuritySeverity.High);
        Assert.True(SecuritySeverity.High < SecuritySeverity.Critical);
    }

    [Fact]
    public void ProcessExec_HasHighestSeverity_Critical()
    {
        // Process execution should be the highest severity
        var finding = SecurityFinding.ProcessExec("Test", "Process.Start");
        Assert.Equal(SecuritySeverity.Critical, finding.Severity);
    }

    [Fact]
    public void Environment_HasLowestSeverity_Low()
    {
        // Environment access should be low severity
        var finding = SecurityFinding.Environment("Test", "Environment.GetVariable");
        Assert.Equal(SecuritySeverity.Low, finding.Severity);
    }

    #endregion
}
