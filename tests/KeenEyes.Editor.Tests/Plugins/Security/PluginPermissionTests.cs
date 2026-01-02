// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="PluginPermission"/> and <see cref="PluginPermissionExtensions"/>.
/// </summary>
public sealed class PluginPermissionTests
{
    #region HasAll Tests

    [Fact]
    public void HasAll_SinglePermission_ReturnsTrue()
    {
        // Arrange
        var granted = PluginPermission.FileSystemProject;

        // Act & Assert
        Assert.True(granted.HasAll(PluginPermission.FileSystemProject));
    }

    [Fact]
    public void HasAll_MultiplePermissions_AllPresent_ReturnsTrue()
    {
        // Arrange
        var granted = PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess;

        // Act & Assert
        Assert.True(granted.HasAll(PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess));
    }

    [Fact]
    public void HasAll_MultiplePermissions_SomeMissing_ReturnsFalse()
    {
        // Arrange
        var granted = PluginPermission.FileSystemProject;

        // Act & Assert
        Assert.False(granted.HasAll(PluginPermission.FileSystemProject | PluginPermission.ClipboardAccess));
    }

    [Fact]
    public void HasAll_None_ReturnsTrue()
    {
        // Arrange
        var granted = PluginPermission.FileSystemProject;

        // Act & Assert
        Assert.True(granted.HasAll(PluginPermission.None));
    }

    [Fact]
    public void HasAll_FullTrust_IncludesAll()
    {
        // Arrange
        var granted = PluginPermission.FullTrust;

        // Act & Assert
        Assert.True(granted.HasAll(PluginPermission.FileSystemProject));
        Assert.True(granted.HasAll(PluginPermission.NetworkClient));
        Assert.True(granted.HasAll(PluginPermission.ProcessExecution));
        Assert.True(granted.HasAll(PluginPermission.Reflection));
    }

    #endregion

    #region HasAny Tests

    [Fact]
    public void HasAny_OneMatches_ReturnsTrue()
    {
        // Arrange
        var granted = PluginPermission.FileSystemProject;

        // Act & Assert
        Assert.True(granted.HasAny(PluginPermission.FileSystemProject | PluginPermission.NetworkClient));
    }

    [Fact]
    public void HasAny_NoneMatch_ReturnsFalse()
    {
        // Arrange
        var granted = PluginPermission.FileSystemProject;

        // Act & Assert
        Assert.False(granted.HasAny(PluginPermission.NetworkClient | PluginPermission.ProcessExecution));
    }

    [Fact]
    public void HasAny_None_ReturnsFalse()
    {
        // Arrange
        var granted = PluginPermission.FileSystemProject;

        // Act & Assert
        Assert.False(granted.HasAny(PluginPermission.None));
    }

    #endregion

    #region GetDisplayName Tests

    [Theory]
    [InlineData(PluginPermission.FileSystemRead, "File System (Read)")]
    [InlineData(PluginPermission.FileSystemWrite, "File System (Write)")]
    [InlineData(PluginPermission.FileSystemProject, "Project Files")]
    [InlineData(PluginPermission.NetworkClient, "Network (Outgoing)")]
    [InlineData(PluginPermission.ProcessExecution, "Execute Processes")]
    [InlineData(PluginPermission.ClipboardAccess, "Clipboard")]
    [InlineData(PluginPermission.MenuAccess, "Menu Items")]
    [InlineData(PluginPermission.SelectionAccess, "Selection")]
    public void GetDisplayName_ReturnsReadableName(PluginPermission permission, string expected)
    {
        // Act
        var displayName = permission.GetDisplayName();

        // Assert
        Assert.Equal(expected, displayName);
    }

    #endregion

    #region GetDescription Tests

    [Theory]
    [InlineData(PluginPermission.FileSystemProject, "Read and write files within the project directory")]
    [InlineData(PluginPermission.NetworkClient, "Make outgoing network connections")]
    [InlineData(PluginPermission.ProcessExecution, "Start and interact with external processes")]
    public void GetDescription_ReturnsDescription(PluginPermission permission, string expected)
    {
        // Act
        var description = permission.GetDescription();

        // Assert
        Assert.Equal(expected, description);
    }

    #endregion

    #region TryParse Tests

    [Fact]
    public void TryParse_ValidName_ReturnsTrue()
    {
        // Act
        var result = PluginPermissionExtensions.TryParse("FileSystemProject", out var permission);

        // Assert
        Assert.True(result);
        Assert.Equal(PluginPermission.FileSystemProject, permission);
    }

    [Fact]
    public void TryParse_CaseInsensitive_ReturnsTrue()
    {
        // Act
        var result = PluginPermissionExtensions.TryParse("filesystemproject", out var permission);

        // Assert
        Assert.True(result);
        Assert.Equal(PluginPermission.FileSystemProject, permission);
    }

    [Fact]
    public void TryParse_InvalidName_ReturnsFalse()
    {
        // Act
        var result = PluginPermissionExtensions.TryParse("InvalidPermission", out var permission);

        // Assert
        Assert.False(result);
        Assert.Equal(PluginPermission.None, permission);
    }

    #endregion

    #region Composite Permission Tests

    [Fact]
    public void StandardEditor_IncludesExpectedPermissions()
    {
        // Arrange
        var standard = PluginPermission.StandardEditor;

        // Assert
        Assert.True(standard.HasAll(PluginPermission.FileSystemProject));
        Assert.True(standard.HasAll(PluginPermission.ClipboardAccess));
        Assert.True(standard.HasAll(PluginPermission.MenuAccess));
        Assert.True(standard.HasAll(PluginPermission.ShortcutAccess));
        Assert.True(standard.HasAll(PluginPermission.PanelAccess));
        Assert.True(standard.HasAll(PluginPermission.InspectorAccess));
        Assert.True(standard.HasAll(PluginPermission.UndoAccess));
        Assert.True(standard.HasAll(PluginPermission.SelectionAccess));
    }

    [Fact]
    public void FileSystemFull_IncludesReadAndWrite()
    {
        // Arrange
        var full = PluginPermission.FileSystemFull;

        // Assert
        Assert.True(full.HasAll(PluginPermission.FileSystemRead));
        Assert.True(full.HasAll(PluginPermission.FileSystemWrite));
    }

    [Fact]
    public void NetworkFull_IncludesClientAndServer()
    {
        // Arrange
        var full = PluginPermission.NetworkFull;

        // Assert
        Assert.True(full.HasAll(PluginPermission.NetworkClient));
        Assert.True(full.HasAll(PluginPermission.NetworkServer));
    }

    [Fact]
    public void EditorUI_IncludesAllUIPermissions()
    {
        // Arrange
        var ui = PluginPermission.EditorUI;

        // Assert
        Assert.True(ui.HasAll(PluginPermission.MenuAccess));
        Assert.True(ui.HasAll(PluginPermission.ShortcutAccess));
        Assert.True(ui.HasAll(PluginPermission.PanelAccess));
        Assert.True(ui.HasAll(PluginPermission.ViewportAccess));
        Assert.True(ui.HasAll(PluginPermission.InspectorAccess));
    }

    #endregion

    #region Permission Bit Tests

    [Fact]
    public void Permissions_AreDistinct()
    {
        // All single-bit permissions should be distinct
        var singleBitPermissions = new[]
        {
            PluginPermission.FileSystemRead,
            PluginPermission.FileSystemWrite,
            PluginPermission.FileSystemProject,
            PluginPermission.FileSystemConfig,
            PluginPermission.FileSystemTemp,
            PluginPermission.NetworkClient,
            PluginPermission.NetworkServer,
            PluginPermission.NetworkLocalhost,
            PluginPermission.ProcessExecution,
            PluginPermission.EnvironmentRead,
            PluginPermission.ClipboardAccess,
            PluginPermission.Notifications,
            PluginPermission.Reflection,
            PluginPermission.NativeCode,
            PluginPermission.AssemblyLoading,
            PluginPermission.UnsafeCode,
            PluginPermission.MenuAccess,
            PluginPermission.ShortcutAccess,
            PluginPermission.PanelAccess,
            PluginPermission.ViewportAccess,
            PluginPermission.InspectorAccess,
            PluginPermission.UndoAccess,
            PluginPermission.SelectionAccess,
            PluginPermission.AssetDatabaseAccess
        };

        // Each permission should be a power of 2 (single bit)
        foreach (var perm in singleBitPermissions)
        {
            var value = (long)perm;
            var isSingleBit = value != 0 && (value & (value - 1)) == 0;
            Assert.True(isSingleBit, $"Permission {perm} is not a single bit value");
        }

        // All permissions should be unique
        Assert.Equal(singleBitPermissions.Length, singleBitPermissions.Distinct().Count());
    }

    #endregion
}
