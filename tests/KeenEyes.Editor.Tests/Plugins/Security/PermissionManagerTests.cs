// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="PermissionManager"/>.
/// </summary>
public sealed class PermissionManagerTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly string storePath;

    public PermissionManagerTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"PermissionManagerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        storePath = Path.Combine(tempDirectory, "plugin-permissions.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    #region Basic Permission Operations

    [Fact]
    public void GetGrantedPermissions_NoGrants_ReturnsNone()
    {
        // Arrange
        var manager = new PermissionManager(storePath);

        // Act
        var permissions = manager.GetGrantedPermissions("com.test.plugin");

        // Assert
        Assert.Equal(PluginPermission.None, permissions);
    }

    [Fact]
    public void GrantPermissions_SinglePermission_IsGranted()
    {
        // Arrange
        var manager = new PermissionManager(storePath);

        // Act
        manager.GrantPermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Assert
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.FileSystemProject));
    }

    [Fact]
    public void GrantPermissions_MultiplePermissions_AllGranted()
    {
        // Arrange
        var manager = new PermissionManager(storePath);

        // Act
        manager.GrantPermissions("com.test.plugin",
            PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess);

        // Assert
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.FileSystemProject));
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.ClipboardAccess));
    }

    [Fact]
    public void GrantPermissions_Cumulative_AddsToExisting()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.GrantPermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Act
        manager.GrantPermissions("com.test.plugin", PluginPermission.NetworkClient);

        // Assert
        var granted = manager.GetGrantedPermissions("com.test.plugin");
        Assert.True(granted.HasAll(PluginPermission.FileSystemProject));
        Assert.True(granted.HasAll(PluginPermission.NetworkClient));
    }

    [Fact]
    public void RevokePermissions_RemovesGranted()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.GrantPermissions("com.test.plugin",
            PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess);

        // Act
        manager.RevokePermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Assert
        Assert.False(manager.HasPermission("com.test.plugin", PluginPermission.FileSystemProject));
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.ClipboardAccess));
    }

    [Fact]
    public void ClearGrants_RemovesAllPermissions()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.GrantPermissions("com.test.plugin",
            PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess);

        // Act
        manager.ClearGrants("com.test.plugin");

        // Assert
        Assert.Equal(PluginPermission.None, manager.GetGrantedPermissions("com.test.plugin"));
    }

    #endregion

    #region DemandPermission Tests

    [Fact]
    public void DemandPermission_WithGrantedPermission_DoesNotThrow()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.GrantPermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Act & Assert
        var exception = Record.Exception(
            () => manager.DemandPermission("com.test.plugin", PluginPermission.FileSystemProject));
        Assert.Null(exception);
    }

    [Fact]
    public void DemandPermission_WithoutPermission_ThrowsPermissionDeniedException()
    {
        // Arrange
        var manager = new PermissionManager(storePath);

        // Act & Assert
        var exception = Assert.Throws<PermissionDeniedException>(
            () => manager.DemandPermission("com.test.plugin", PluginPermission.FileSystemProject));

        Assert.Equal("com.test.plugin", exception.PluginId);
        Assert.Equal(PluginPermission.FileSystemProject, exception.RequiredPermission);
    }

    [Fact]
    public void DemandPermission_WithCapabilityType_IncludesInException()
    {
        // Arrange
        var manager = new PermissionManager(storePath);

        // Act & Assert
        var exception = Assert.Throws<PermissionDeniedException>(
            () => manager.DemandPermission("com.test.plugin", PluginPermission.MenuAccess, typeof(IEditorCapability)));

        Assert.Equal(typeof(IEditorCapability), exception.CapabilityType);
    }

    #endregion

    #region Plugin Registration Tests

    [Fact]
    public void RegisterPlugin_SetsRequiredPermissions()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.test.plugin",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Permissions = new PluginPermissions
            {
                Required = ["FileSystemProject", "ClipboardAccess"]
            }
        };

        // Act
        manager.RegisterPlugin("com.test.plugin", manifest);

        // Assert
        var required = manager.GetRequiredPermissions("com.test.plugin");
        Assert.True(required.HasAll(PluginPermission.FileSystemProject));
        Assert.True(required.HasAll(PluginPermission.ClipboardAccess));
    }

    [Fact]
    public void RegisterPlugin_SetsOptionalPermissions()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.test.plugin",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Permissions = new PluginPermissions
            {
                Optional = ["NetworkClient"]
            }
        };

        // Act
        manager.RegisterPlugin("com.test.plugin", manifest);

        // Assert
        var optional = manager.GetOptionalPermissions("com.test.plugin");
        Assert.True(optional.HasAll(PluginPermission.NetworkClient));
    }

    [Fact]
    public void UnregisterPlugin_ClearsPluginData()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.test.plugin",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Permissions = new PluginPermissions
            {
                Required = ["FileSystemProject"]
            }
        };
        manager.RegisterPlugin("com.test.plugin", manifest);

        // Act
        manager.UnregisterPlugin("com.test.plugin");

        // Assert
        Assert.Equal(PluginPermission.None, manager.GetRequiredPermissions("com.test.plugin"));
        Assert.Equal(PluginPermission.None, manager.GetOptionalPermissions("com.test.plugin"));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidatePlugin_AllRequiredGranted_IsValid()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.test.plugin",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Permissions = new PluginPermissions
            {
                Required = ["FileSystemProject", "ClipboardAccess"]
            }
        };
        manager.RegisterPlugin("com.test.plugin", manifest);
        manager.GrantPermissions("com.test.plugin",
            PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess);

        // Act
        var result = manager.ValidatePlugin("com.test.plugin");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(PluginPermission.None, result.MissingPermissions);
    }

    [Fact]
    public void ValidatePlugin_MissingRequired_IsNotValid()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.test.plugin",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Permissions = new PluginPermissions
            {
                Required = ["FileSystemProject", "ClipboardAccess"]
            }
        };
        manager.RegisterPlugin("com.test.plugin", manifest);
        manager.GrantPermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Act
        var result = manager.ValidatePlugin("com.test.plugin");

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.MissingPermissions.HasAll(PluginPermission.ClipboardAccess));
    }

    [Fact]
    public void ValidatePlugin_NoRequirements_IsValid()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.test.plugin",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" }
        };
        manager.RegisterPlugin("com.test.plugin", manifest);

        // Act
        var result = manager.ValidatePlugin("com.test.plugin");

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        // Arrange
        var manager1 = new PermissionManager(storePath);
        manager1.GrantPermissions("com.test.plugin",
            PluginPermission.FileSystemProject | PluginPermission.NetworkClient);
        manager1.Save();

        // Act
        var manager2 = new PermissionManager(storePath);
        manager2.Load();

        // Assert
        var permissions = manager2.GetGrantedPermissions("com.test.plugin");
        Assert.True(permissions.HasAll(PluginPermission.FileSystemProject));
        Assert.True(permissions.HasAll(PluginPermission.NetworkClient));
    }

    [Fact]
    public void Load_NonexistentFile_DoesNotThrow()
    {
        // Arrange
        var manager = new PermissionManager(storePath);

        // Act & Assert
        var exception = Record.Exception(() => manager.Load());
        Assert.Null(exception);
    }

    [Fact]
    public void Save_CreatesDirectory()
    {
        // Arrange
        var subDir = Path.Combine(tempDirectory, "subdir", "permissions.json");
        var manager = new PermissionManager(subDir);
        manager.GrantPermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Act
        manager.Save();

        // Assert
        Assert.True(File.Exists(subDir));
    }

    #endregion

    #region Permission Request Tests

    [Fact]
    public async Task RequestPermissionAsync_AlreadyGranted_ReturnsTrue()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.GrantPermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Act
        var result = await manager.RequestPermissionAsync("com.test.plugin", PluginPermission.FileSystemProject);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RequestPermissionAsync_NoHandler_ReturnsFalse()
    {
        // Arrange
        var manager = new PermissionManager(storePath);

        // Act
        var result = await manager.RequestPermissionAsync("com.test.plugin", PluginPermission.FileSystemProject);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RequestPermissionAsync_HandlerApproves_GrantsPermission()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.OnPermissionRequest += request =>
        {
            return Task.FromResult(new PermissionResponse { Granted = true, Remember = false });
        };

        // Act
        var result = await manager.RequestPermissionAsync("com.test.plugin", PluginPermission.FileSystemProject);

        // Assert
        Assert.True(result);
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.FileSystemProject));
    }

    [Fact]
    public async Task RequestPermissionAsync_HandlerDenies_DoesNotGrant()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.OnPermissionRequest += request =>
        {
            return Task.FromResult(new PermissionResponse { Granted = false });
        };

        // Act
        var result = await manager.RequestPermissionAsync("com.test.plugin", PluginPermission.FileSystemProject);

        // Assert
        Assert.False(result);
        Assert.False(manager.HasPermission("com.test.plugin", PluginPermission.FileSystemProject));
    }

    [Fact]
    public async Task RequestPermissionAsync_WithRemember_SavesGrant()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.OnPermissionRequest += request =>
        {
            return Task.FromResult(new PermissionResponse { Granted = true, Remember = true });
        };

        // Act
        await manager.RequestPermissionAsync("com.test.plugin", PluginPermission.FileSystemProject);

        // Assert - Should have saved to disk
        Assert.True(File.Exists(storePath));
    }

    #endregion

    #region HasPermission Tests

    [Fact]
    public void HasPermission_CompositePermission_RequiresAll()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.GrantPermissions("com.test.plugin", PluginPermission.FileSystemProject);

        // Act & Assert - Asking for both, but only one is granted
        var compositePermission = PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess;
        Assert.False(manager.HasPermission("com.test.plugin", compositePermission));
    }

    [Fact]
    public void HasPermission_FullTrust_IncludesAll()
    {
        // Arrange
        var manager = new PermissionManager(storePath);
        manager.GrantPermissions("com.test.plugin", PluginPermission.FullTrust);

        // Act & Assert
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.FileSystemProject));
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.NetworkClient));
        Assert.True(manager.HasPermission("com.test.plugin", PluginPermission.ProcessExecution));
    }

    #endregion
}
