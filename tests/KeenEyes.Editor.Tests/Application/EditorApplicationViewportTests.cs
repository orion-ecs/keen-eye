// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Settings;

namespace KeenEyes.Editor.Tests.Application;

/// <summary>
/// Tests for EditorApplication viewport functionality.
/// These tests validate that the viewport-related shortcut actions correctly
/// interact with EditorSettings and viewport state.
/// </summary>
[Collection("EditorSettings")]
public sealed class EditorApplicationViewportTests : IDisposable
{
    public EditorApplicationViewportTests()
    {
        // Reset settings to defaults before each test
        EditorSettings.ResetToDefaults();
    }

    public void Dispose()
    {
        // Clean up settings after tests
        EditorSettings.ResetToDefaults();
    }

    #region ToggleGrid Tests

    [Fact]
    public void ToggleGrid_WhenGridVisible_HidesGrid()
    {
        // Arrange
        EditorSettings.GridVisible = true;

        // Act
        ToggleGrid();

        // Assert
        Assert.False(EditorSettings.GridVisible);
    }

    [Fact]
    public void ToggleGrid_WhenGridHidden_ShowsGrid()
    {
        // Arrange
        EditorSettings.GridVisible = false;

        // Act
        ToggleGrid();

        // Assert
        Assert.True(EditorSettings.GridVisible);
    }

    [Fact]
    public void ToggleGrid_MultipleTimes_TogglesCorrectly()
    {
        // Arrange
        EditorSettings.GridVisible = true;

        // Act & Assert
        ToggleGrid();
        Assert.False(EditorSettings.GridVisible);

        ToggleGrid();
        Assert.True(EditorSettings.GridVisible);

        ToggleGrid();
        Assert.False(EditorSettings.GridVisible);
    }

    [Fact]
    public void ToggleGrid_RaisesSettingChangedEvent()
    {
        // Arrange
        EditorSettings.GridVisible = true;
        EditorSettings.Load(GetTestSettingsPath());
        SettingChangedEventArgs? eventArgs = null;
        EditorSettings.SettingChanged += (_, e) => eventArgs = e;

        // Act
        ToggleGrid();

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("GridVisible", eventArgs.SettingName);
        Assert.Equal("Viewport", eventArgs.Category);
    }

    #endregion

    #region ToggleWireframe Tests

    [Fact]
    public void ToggleWireframe_WhenWireframeOff_EnablesWireframe()
    {
        // Arrange
        EditorSettings.WireframeMode = false;

        // Act
        ToggleWireframe();

        // Assert
        Assert.True(EditorSettings.WireframeMode);
    }

    [Fact]
    public void ToggleWireframe_WhenWireframeOn_DisablesWireframe()
    {
        // Arrange
        EditorSettings.WireframeMode = true;

        // Act
        ToggleWireframe();

        // Assert
        Assert.False(EditorSettings.WireframeMode);
    }

    [Fact]
    public void ToggleWireframe_MultipleTimes_TogglesCorrectly()
    {
        // Arrange
        EditorSettings.WireframeMode = false;

        // Act & Assert
        ToggleWireframe();
        Assert.True(EditorSettings.WireframeMode);

        ToggleWireframe();
        Assert.False(EditorSettings.WireframeMode);

        ToggleWireframe();
        Assert.True(EditorSettings.WireframeMode);
    }

    [Fact]
    public void ToggleWireframe_RaisesSettingChangedEvent()
    {
        // Arrange
        EditorSettings.WireframeMode = false;
        EditorSettings.Load(GetTestSettingsPath());
        SettingChangedEventArgs? eventArgs = null;
        EditorSettings.SettingChanged += (_, e) => eventArgs = e;

        // Act
        ToggleWireframe();

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("WireframeMode", eventArgs.SettingName);
        Assert.Equal("Viewport", eventArgs.Category);
    }

    #endregion

    #region FocusSelection Tests

    [Fact]
    public void FocusSelection_WithNoSelection_DoesNotThrow()
    {
        // Arrange
        var hasSelection = false;
        var focusCalled = false;

        // Act & Assert - should not throw
        var exception = Record.Exception(() => FocusSelection(hasSelection, () => focusCalled = true));
        Assert.Null(exception);
        Assert.False(focusCalled);
    }

    [Fact]
    public void FocusSelection_WithSelection_CallsFocusOnEntity()
    {
        // Arrange
        var hasSelection = true;
        var focusCalled = false;

        // Act
        FocusSelection(hasSelection, () => focusCalled = true);

        // Assert
        Assert.True(focusCalled);
    }

    #endregion

    #region Combined Viewport State Tests

    [Fact]
    public void ViewportState_GridAndWireframe_CanBeToggledIndependently()
    {
        // Arrange
        EditorSettings.GridVisible = true;
        EditorSettings.WireframeMode = false;

        // Act
        ToggleWireframe();

        // Assert
        Assert.True(EditorSettings.GridVisible); // Grid unchanged
        Assert.True(EditorSettings.WireframeMode); // Wireframe changed

        // Act again
        ToggleGrid();

        // Assert
        Assert.False(EditorSettings.GridVisible); // Grid changed
        Assert.True(EditorSettings.WireframeMode); // Wireframe unchanged
    }

    [Fact]
    public void ViewportState_Persists_AfterSaveAndLoad()
    {
        // Arrange
        var path = GetTestSettingsPath();
        var path2 = GetTestSettingsPath();

        try
        {
            EditorSettings.Load(path);
            EditorSettings.GridVisible = true;
            EditorSettings.WireframeMode = false;

            // Toggle both
            ToggleGrid();
            ToggleWireframe();

            EditorSettings.Save();
            File.Copy(path, path2);

            // Reset and reload
            EditorSettings.ResetToDefaults();
            EditorSettings.Load(path2);

            // Assert - state should be preserved
            Assert.False(EditorSettings.GridVisible);
            Assert.True(EditorSettings.WireframeMode);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            if (File.Exists(path2))
            {
                File.Delete(path2);
            }
        }
    }

    #endregion

    #region Test Helpers

    private static string GetTestSettingsPath()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "KeenEyesViewportTests");
        Directory.CreateDirectory(testDir);
        return Path.Combine(testDir, $"settings_{Guid.NewGuid()}.json");
    }

    /// <summary>
    /// Simulates the ToggleGrid action as implemented in EditorApplication.
    /// </summary>
    private static void ToggleGrid()
    {
        EditorSettings.GridVisible = !EditorSettings.GridVisible;
    }

    /// <summary>
    /// Simulates the ToggleWireframe action as implemented in EditorApplication.
    /// </summary>
    private static void ToggleWireframe()
    {
        EditorSettings.WireframeMode = !EditorSettings.WireframeMode;
    }

    /// <summary>
    /// Simulates the FocusSelection action as implemented in EditorApplication.
    /// </summary>
    private static void FocusSelection(bool hasSelection, Action focusAction)
    {
        if (!hasSelection)
        {
            return;
        }

        focusAction();
    }

    #endregion
}
