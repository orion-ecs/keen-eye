// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.BuiltIn;

namespace KeenEyes.Editor.Tests.Plugins.BuiltIn;

/// <summary>
/// Tests for <see cref="PluginManagerPanelImpl"/> and related functionality.
/// </summary>
public sealed class PluginManagerPanelTests
{
    #region GetStateColor Tests

    [Fact]
    public void GetStateColor_ForEnabledState_ReturnsGreen()
    {
        // Act
        var color = PluginManagerPanelImpl.GetStateColor(PluginState.Enabled);

        // Assert - Green color
        Assert.True(color.X < color.Y); // Green component should be highest
        Assert.True(color.Y > 0.7f);    // High green value
    }

    [Fact]
    public void GetStateColor_ForDisabledState_ReturnsGray()
    {
        // Act
        var color = PluginManagerPanelImpl.GetStateColor(PluginState.Disabled);

        // Assert - Gray color (similar R, G, B values)
        Assert.True(Math.Abs(color.X - color.Y) < 0.1f);
        Assert.True(Math.Abs(color.Y - color.Z) < 0.1f);
    }

    [Fact]
    public void GetStateColor_ForFailedState_ReturnsRed()
    {
        // Act
        var color = PluginManagerPanelImpl.GetStateColor(PluginState.Failed);

        // Assert - Red color
        Assert.True(color.X > color.Y); // Red component should be highest
        Assert.True(color.X > 0.8f);    // High red value
    }

    [Fact]
    public void GetStateColor_ForLoadedState_ReturnsYellow()
    {
        // Act
        var color = PluginManagerPanelImpl.GetStateColor(PluginState.Loaded);

        // Assert - Yellow color (high red and green)
        Assert.True(color.X > 0.7f);
        Assert.True(color.Y > 0.5f);
        Assert.True(color.Z < 0.4f);
    }

    [Fact]
    public void GetStateColor_ForDiscoveredState_ReturnsBlueGray()
    {
        // Act
        var color = PluginManagerPanelImpl.GetStateColor(PluginState.Discovered);

        // Assert - Blue-gray color
        Assert.True(color.Z >= color.X); // Blue component >= red
        Assert.True(color.Z >= color.Y); // Blue component >= green
    }

    [Fact]
    public void GetStateColor_ForUnloadingState_ReturnsOrange()
    {
        // Act
        var color = PluginManagerPanelImpl.GetStateColor(PluginState.Unloading);

        // Assert - Orange color
        Assert.True(color.X > color.Z); // Red component > blue
        Assert.True(color.Y > color.Z); // Green component > blue
    }

    [Fact]
    public void GetStateColor_AllStates_HaveFullAlpha()
    {
        // All state colors should have full alpha (1.0)
        var states = Enum.GetValues<PluginState>();

        foreach (var state in states)
        {
            var color = PluginManagerPanelImpl.GetStateColor(state);
            Assert.Equal(1f, color.W);
        }
    }

    #endregion

    #region Component Tests

    [Fact]
    public void PluginManagerPanelState_DefaultValues_AreNull()
    {
        // Arrange
        var state = new PluginManagerPanelState();

        // Assert
        Assert.Null(state.SelectedPluginId);
    }

    [Fact]
    public void PluginListItemData_StoresPluginId()
    {
        // Arrange
        var itemData = new PluginListItemData
        {
            PluginId = "com.test.plugin"
        };

        // Assert
        Assert.Equal("com.test.plugin", itemData.PluginId);
    }

    [Fact]
    public void PluginControlButtonData_StoresPluginIdAndAction()
    {
        // Arrange
        var buttonData = new PluginControlButtonData
        {
            PluginId = "com.test.plugin",
            Action = PluginButtonAction.Enable
        };

        // Assert
        Assert.Equal("com.test.plugin", buttonData.PluginId);
        Assert.Equal(PluginButtonAction.Enable, buttonData.Action);
    }

    #endregion

    #region PluginButtonAction Tests

    [Fact]
    public void PluginButtonAction_HasExpectedValues()
    {
        // Assert - All expected actions exist
        Assert.Equal(0, (int)PluginButtonAction.Enable);
        Assert.Equal(1, (int)PluginButtonAction.Disable);
        Assert.Equal(2, (int)PluginButtonAction.Reload);
    }

    #endregion

    #region State Color Visibility Tests

    [Fact]
    public void GetStateColor_ColorsAreDistinct()
    {
        // Get all unique colors for different states
        var enabledColor = PluginManagerPanelImpl.GetStateColor(PluginState.Enabled);
        var disabledColor = PluginManagerPanelImpl.GetStateColor(PluginState.Disabled);
        var failedColor = PluginManagerPanelImpl.GetStateColor(PluginState.Failed);
        var loadedColor = PluginManagerPanelImpl.GetStateColor(PluginState.Loaded);
        var discoveredColor = PluginManagerPanelImpl.GetStateColor(PluginState.Discovered);

        // All main states should have distinct colors
        Assert.NotEqual(enabledColor, disabledColor);
        Assert.NotEqual(enabledColor, failedColor);
        Assert.NotEqual(enabledColor, loadedColor);
        Assert.NotEqual(enabledColor, discoveredColor);

        Assert.NotEqual(disabledColor, failedColor);
        Assert.NotEqual(disabledColor, loadedColor);
        Assert.NotEqual(disabledColor, discoveredColor);

        Assert.NotEqual(failedColor, loadedColor);
        Assert.NotEqual(failedColor, discoveredColor);

        Assert.NotEqual(loadedColor, discoveredColor);
    }

    [Theory]
    [InlineData(PluginState.Enabled)]
    [InlineData(PluginState.Disabled)]
    [InlineData(PluginState.Failed)]
    [InlineData(PluginState.Loaded)]
    [InlineData(PluginState.Discovered)]
    [InlineData(PluginState.Unloading)]
    public void GetStateColor_AllStates_ReturnValidColors(PluginState state)
    {
        // Act
        var color = PluginManagerPanelImpl.GetStateColor(state);

        // Assert - All color components are in valid range [0, 1]
        Assert.InRange(color.X, 0f, 1f);
        Assert.InRange(color.Y, 0f, 1f);
        Assert.InRange(color.Z, 0f, 1f);
        Assert.InRange(color.W, 0f, 1f);
    }

    #endregion
}
