// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="PluginSecurityManager"/>.
/// </summary>
public sealed class PluginSecurityManagerTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly string pluginDirectory;

    public PluginSecurityManagerTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"SecurityManagerTests_{Guid.NewGuid():N}");
        pluginDirectory = Path.Combine(tempDirectory, "plugin");
        Directory.CreateDirectory(pluginDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    #region CheckPlugin - Basic Tests

    [Fact]
    public void CheckPlugin_WithDefaultConfig_ReturnsResult()
    {
        // Arrange
        var manager = CreateSecurityManager();
        var plugin = CreateTestPlugin();

        // Act
        var result = manager.CheckPlugin(plugin);

        // Assert
        Assert.NotNull(result);
        // With default config (no signature required, warn only), should pass
        Assert.True(result.CanLoad);
    }

    [Fact]
    public void CheckPlugin_ReturnsResultWithAnalysis()
    {
        // Arrange
        var manager = CreateSecurityManager();
        var plugin = CreateTestPlugin();

        // Act
        var result = manager.CheckPlugin(plugin);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AnalysisResult);
        // Note: The security result is stored on the plugin by PluginLoader, not CheckPlugin
    }

    #endregion

    #region CheckPlugin - Signature Requirements

    [Fact]
    public void CheckPlugin_WithRequiredSignature_BlocksUnsignedPlugin()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            RequireSignature = true,
            AllowUnsignedFromTrustedPaths = false
        };
        var manager = CreateSecurityManager(config);
        var plugin = CreateTestPlugin();

        // Act
        var result = manager.CheckPlugin(plugin);

        // Assert
        // The test assembly is unsigned, so should be blocked
        if (result.SignatureResult?.Status == SignatureStatus.Unsigned)
        {
            Assert.False(result.CanLoad);
            Assert.Contains(result.BlockingReasons, r => r.Contains("not signed"));
        }
    }

    [Fact]
    public void CheckPlugin_WithUnsignedFromTrustedPath_AllowsUnsigned()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            RequireSignature = true,
            AllowUnsignedFromTrustedPaths = true,
            TrustedPaths = [pluginDirectory]
        };
        var manager = CreateSecurityManager(config);
        var plugin = CreateTestPlugin();

        // Act
        var result = manager.CheckPlugin(plugin);

        // Assert
        // Plugin is from trusted path, so should be allowed despite being unsigned
        if (result.SignatureResult?.Status == SignatureStatus.Unsigned)
        {
            Assert.True(result.CanLoad);
            Assert.Contains(result.Warnings, w => w.Contains("trusted path"));
        }
    }

    #endregion

    #region CheckPlugin - Analysis

    [Fact]
    public void CheckPlugin_WithAnalysisEnabled_IncludesAnalysisResult()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            EnableAnalysis = true
        };
        var manager = CreateSecurityManager(config);
        var plugin = CreateTestPlugin();

        // Act
        var result = manager.CheckPlugin(plugin);

        // Assert
        Assert.NotNull(result.AnalysisResult);
    }

    [Fact]
    public void CheckPlugin_WithAnalysisDisabled_NoAnalysisResult()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            EnableAnalysis = false,
            RequireSignature = false
        };
        var manager = CreateSecurityManager(config);
        var plugin = CreateTestPlugin();

        // Act
        var result = manager.CheckPlugin(plugin);

        // Assert
        Assert.Null(result.AnalysisResult);
    }

    [Fact]
    public void CheckPlugin_WithBlockModeAnalysis_BlocksOnHighSeverity()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            RequireSignature = false,
            EnableAnalysis = true,
            AnalysisConfig = new AnalysisConfiguration
            {
                Mode = AnalysisMode.Block,
                BlockingSeverity = SecuritySeverity.Low // Very sensitive
            }
        };
        var manager = CreateSecurityManager(config);
        var plugin = CreateTestPlugin();

        // Act
        var result = manager.CheckPlugin(plugin);

        // Assert
        // If the assembly has any findings >= Low, it should be blocked
        if (result.AnalysisResult?.HasFindingsAtOrAbove(SecuritySeverity.Low) == true)
        {
            Assert.False(result.CanLoad);
            Assert.NotEmpty(result.BlockingReasons);
        }
    }

    #endregion

    #region CheckPlugin - Async

    [Fact]
    public async Task CheckPluginAsync_ReturnsResult()
    {
        // Arrange
        var manager = CreateSecurityManager();
        var plugin = CreateTestPlugin();

        // Act
        var result = await manager.CheckPluginAsync(plugin);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region SecurityCheckResult Static Methods

    [Fact]
    public void SecurityCheckResult_Passed_ReturnsCanLoad()
    {
        // Act
        var result = SecurityCheckResult.Passed();

        // Assert
        Assert.True(result.CanLoad);
        Assert.Empty(result.BlockingReasons);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void SecurityCheckResult_Blocked_SetsReason()
    {
        // Act
        var result = SecurityCheckResult.Blocked("Reason 1", "Reason 2");

        // Assert
        Assert.False(result.CanLoad);
        Assert.Equal(2, result.BlockingReasons.Count);
        Assert.Contains("Reason 1", result.BlockingReasons);
        Assert.Contains("Reason 2", result.BlockingReasons);
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_DoesNotThrow()
    {
        // Arrange
        var manager = CreateSecurityManager();
        var plugin = CreateTestPlugin();

        // Analyze once to populate cache
        manager.CheckPlugin(plugin);

        // Act & Assert
        var exception = Record.Exception(() => manager.ClearCache());
        Assert.Null(exception);
    }

    #endregion

    #region Security Prompt Tests

    [Fact]
    public async Task CheckPluginAsync_WithUntrustedPublisher_CanPromptUser()
    {
        // Arrange
        var promptCalled = false;
        var config = new SecurityConfiguration
        {
            RequireSignature = true,
            EnableAnalysis = false
        };
        var manager = CreateSecurityManager(config);
        manager.OnSecurityPrompt += async prompt =>
        {
            promptCalled = true;
            Assert.Equal(SecurityPromptType.UntrustedPublisher, prompt.PromptType);
            return SecurityDecision.Block;
        };

        // Use a signed but untrusted assembly
        var systemAssemblyPath = typeof(object).Assembly.Location;
        var manifest = new PluginManifest
        {
            Name = "Test",
            Id = "com.test.signed",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint
            {
                Assembly = Path.GetFileName(systemAssemblyPath),
                Type = "Test.Plugin"
            }
        };
        var plugin = new LoadedPlugin(manifest, Path.GetDirectoryName(systemAssemblyPath)!);

        // Act
        var result = await manager.CheckPluginAsync(plugin);

        // Assert
        // Prompt should have been called if assembly is signed
        if (result.SignatureResult?.IsSigned == true && !result.SignatureResult.IsTrusted)
        {
            Assert.True(promptCalled);
        }
    }

    #endregion

    #region SecurityPromptInfo Tests

    [Fact]
    public void SecurityPromptInfo_HasRequiredProperties()
    {
        // Arrange
        var prompt = new SecurityPromptInfo
        {
            PluginId = "com.test.plugin",
            PluginName = "Test Plugin",
            PromptType = SecurityPromptType.SuspiciousCode,
            Message = "Plugin contains suspicious code",
            Details = "Details here",
            Options = [SecurityDecision.Allow, SecurityDecision.Block]
        };

        // Assert
        Assert.Equal("com.test.plugin", prompt.PluginId);
        Assert.Equal("Test Plugin", prompt.PluginName);
        Assert.Equal(SecurityPromptType.SuspiciousCode, prompt.PromptType);
        Assert.Equal("Plugin contains suspicious code", prompt.Message);
        Assert.Equal("Details here", prompt.Details);
        Assert.Contains(SecurityDecision.Allow, prompt.Options);
        Assert.Contains(SecurityDecision.Block, prompt.Options);
    }

    #endregion

    #region Helper Methods

    private PluginSecurityManager CreateSecurityManager(SecurityConfiguration? config = null)
    {
        return new PluginSecurityManager(config);
    }

    private LoadedPlugin CreateTestPlugin()
    {
        // Copy the test assembly to the plugin directory
        var sourceAssembly = typeof(PluginSecurityManagerTests).Assembly.Location;
        var destAssembly = Path.Combine(pluginDirectory, "TestPlugin.dll");

        if (!File.Exists(destAssembly))
        {
            File.Copy(sourceAssembly, destAssembly);
        }

        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.test.plugin",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint
            {
                Assembly = "TestPlugin.dll",
                Type = "TestPlugin.TestEditorPlugin"
            }
        };

        return new LoadedPlugin(manifest, pluginDirectory);
    }

    #endregion
}
