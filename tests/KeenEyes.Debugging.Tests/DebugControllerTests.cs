namespace KeenEyes.Debugging.Tests;

/// <summary>
/// Unit tests for the <see cref="DebugController"/> class.
/// </summary>
public class DebugControllerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultParameter_DebugModeIsFalse()
    {
        // Arrange & Act
        var controller = new DebugController();

        // Assert
        Assert.False(controller.IsDebugMode);
    }

    [Fact]
    public void Constructor_WithTrueParameter_DebugModeIsTrue()
    {
        // Arrange & Act
        var controller = new DebugController(true);

        // Assert
        Assert.True(controller.IsDebugMode);
    }

    [Fact]
    public void Constructor_WithFalseParameter_DebugModeIsFalse()
    {
        // Arrange & Act
        var controller = new DebugController(false);

        // Assert
        Assert.False(controller.IsDebugMode);
    }

    #endregion

    #region IsDebugMode Property Tests

    [Fact]
    public void IsDebugMode_Set_UpdatesValue()
    {
        // Arrange - start with false, then set to true
        var controller = new DebugController(initialDebugMode: false);
        Assert.False(controller.IsDebugMode);

        // Act
        controller.IsDebugMode = true;

        // Assert
        Assert.True(controller.IsDebugMode);
    }

    [Fact]
    public void IsDebugMode_SetSameValue_DoesNotRaiseEvent()
    {
        // Arrange
        var controller = new DebugController(true);
        var eventRaised = false;
        controller.DebugModeChanged += (_, _) => eventRaised = true;

        // Act
        controller.IsDebugMode = true; // Same value

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void IsDebugMode_SetDifferentValue_RaisesEvent()
    {
        // Arrange
        var controller = new DebugController(false);
        var eventRaised = false;
        bool? newValue = null;
        controller.DebugModeChanged += (_, value) =>
        {
            eventRaised = true;
            newValue = value;
        };

        // Act
        controller.IsDebugMode = true;

        // Assert
        Assert.True(eventRaised);
        Assert.True(newValue);
    }

    #endregion

    #region Toggle Tests

    [Fact]
    public void Toggle_WhenFalse_BecomesTrue()
    {
        // Arrange
        var controller = new DebugController(false);

        // Act
        var result = controller.Toggle();

        // Assert
        Assert.True(result);
        Assert.True(controller.IsDebugMode);
    }

    [Fact]
    public void Toggle_WhenTrue_BecomesFalse()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        var result = controller.Toggle();

        // Assert
        Assert.False(result);
        Assert.False(controller.IsDebugMode);
    }

    [Fact]
    public void Toggle_IncrementsToggleCount()
    {
        // Arrange
        var controller = new DebugController();
        Assert.Equal(0, controller.ToggleCount);

        // Act
        controller.Toggle();

        // Assert
        Assert.Equal(1, controller.ToggleCount);
    }

    [Fact]
    public void Toggle_MultipleTimes_IncrementsCorrectly()
    {
        // Arrange
        var controller = new DebugController();

        // Act
        controller.Toggle();
        controller.Toggle();
        controller.Toggle();

        // Assert
        Assert.Equal(3, controller.ToggleCount);
        Assert.True(controller.IsDebugMode); // false -> true -> false -> true
    }

    [Fact]
    public void Toggle_UpdatesLastToggleTime()
    {
        // Arrange
        var controller = new DebugController();
        Assert.Null(controller.LastToggleTime);
        var beforeToggle = DateTime.UtcNow;

        // Act
        controller.Toggle();
        var afterToggle = DateTime.UtcNow;

        // Assert
        Assert.NotNull(controller.LastToggleTime);
        Assert.InRange(controller.LastToggleTime.Value, beforeToggle, afterToggle);
    }

    #endregion

    #region Enable Tests

    [Fact]
    public void Enable_WhenFalse_BecomesTrue()
    {
        // Arrange
        var controller = new DebugController(false);

        // Act
        controller.Enable();

        // Assert
        Assert.True(controller.IsDebugMode);
    }

    [Fact]
    public void Enable_WhenTrue_StaysTrue()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        controller.Enable();

        // Assert
        Assert.True(controller.IsDebugMode);
    }

    [Fact]
    public void Enable_WhenFalse_IncrementsToggleCount()
    {
        // Arrange
        var controller = new DebugController(false);

        // Act
        controller.Enable();

        // Assert
        Assert.Equal(1, controller.ToggleCount);
    }

    [Fact]
    public void Enable_WhenTrue_DoesNotIncrementToggleCount()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        controller.Enable();

        // Assert
        Assert.Equal(0, controller.ToggleCount);
    }

    #endregion

    #region Disable Tests

    [Fact]
    public void Disable_WhenTrue_BecomesFalse()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        controller.Disable();

        // Assert
        Assert.False(controller.IsDebugMode);
    }

    [Fact]
    public void Disable_WhenFalse_StaysFalse()
    {
        // Arrange
        var controller = new DebugController(false);

        // Act
        controller.Disable();

        // Assert
        Assert.False(controller.IsDebugMode);
    }

    [Fact]
    public void Disable_WhenTrue_IncrementsToggleCount()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        controller.Disable();

        // Assert
        Assert.Equal(1, controller.ToggleCount);
    }

    [Fact]
    public void Disable_WhenFalse_DoesNotIncrementToggleCount()
    {
        // Arrange
        var controller = new DebugController(false);

        // Act
        controller.Disable();

        // Assert
        Assert.Equal(0, controller.ToggleCount);
    }

    #endregion

    #region WhenDebug Tests

    [Fact]
    public void WhenDebug_DebugModeEnabled_ExecutesAction()
    {
        // Arrange
        var controller = new DebugController(true);
        var actionExecuted = false;

        // Act
        controller.WhenDebug(() => actionExecuted = true);

        // Assert
        Assert.True(actionExecuted);
    }

    [Fact]
    public void WhenDebug_DebugModeDisabled_DoesNotExecuteAction()
    {
        // Arrange
        var controller = new DebugController(false);
        var actionExecuted = false;

        // Act
        controller.WhenDebug(() => actionExecuted = true);

        // Assert
        Assert.False(actionExecuted);
    }

    [Fact]
    public void WhenDebug_AfterToggle_ExecutesAction()
    {
        // Arrange
        var controller = new DebugController(false);
        var actionExecuted = false;
        controller.Toggle();

        // Act
        controller.WhenDebug(() => actionExecuted = true);

        // Assert
        Assert.True(actionExecuted);
    }

    #endregion

    #region Select Tests

    [Fact]
    public void Select_DebugModeEnabled_ReturnsDebugValue()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        var result = controller.Select("debug", "release");

        // Assert
        Assert.Equal("debug", result);
    }

    [Fact]
    public void Select_DebugModeDisabled_ReturnsReleaseValue()
    {
        // Arrange
        var controller = new DebugController(false);

        // Act
        var result = controller.Select("debug", "release");

        // Assert
        Assert.Equal("release", result);
    }

    [Fact]
    public void Select_WithIntValues_ReturnsCorrectValue()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        var result = controller.Select(100, 10);

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void Select_AfterToggle_ReturnsCorrectValue()
    {
        // Arrange
        var controller = new DebugController(true);

        // Act
        controller.Toggle(); // Now false
        var result = controller.Select("debug", "release");

        // Assert
        Assert.Equal("release", result);
    }

    #endregion

    #region DebugModeChanged Event Tests

    [Fact]
    public void DebugModeChanged_RaisedWithCorrectSender()
    {
        // Arrange
        var controller = new DebugController();
        object? capturedSender = null;
        controller.DebugModeChanged += (sender, _) => capturedSender = sender;

        // Act
        controller.IsDebugMode = true;

        // Assert
        Assert.Same(controller, capturedSender);
    }

    [Fact]
    public void DebugModeChanged_RaisedOnEveryChange()
    {
        // Arrange
        var controller = new DebugController();
        var eventCount = 0;
        controller.DebugModeChanged += (_, _) => eventCount++;

        // Act
        controller.IsDebugMode = true;
        controller.IsDebugMode = false;
        controller.IsDebugMode = true;

        // Assert
        Assert.Equal(3, eventCount);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void DebugController_InstallViaDebugPlugin_IsAvailable()
    {
        // Arrange
        using var world = new World();
        world.InstallPlugin(new DebugPlugin());

        // Act
        var controller = world.GetExtension<DebugController>();

        // Assert
        Assert.NotNull(controller);
        Assert.False(controller.IsDebugMode);
    }

    [Fact]
    public void DebugController_InstallWithInitialDebugMode_IsEnabled()
    {
        // Arrange
        using var world = new World();
        world.InstallPlugin(new DebugPlugin(new DebugOptions { InitialDebugMode = true }));

        // Act
        var controller = world.GetExtension<DebugController>();

        // Assert
        Assert.NotNull(controller);
        Assert.True(controller.IsDebugMode);
    }

    [Fact]
    public void DebugController_AfterUninstall_NotAvailable()
    {
        // Arrange
        using var world = new World();
        world.InstallPlugin(new DebugPlugin());

        // Act
        world.UninstallPlugin("Debug");

        // Assert
        Assert.Throws<InvalidOperationException>(() => world.GetExtension<DebugController>());
    }

    #endregion
}
