using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory advanced input widgets: ColorPicker, DatePicker.
/// </summary>
public class WidgetFactoryInputAdvancedTests
{
    private static readonly FontHandle testFont = new(1);

    #region ColorPicker Tests

    [Fact]
    public void CreateColorPicker_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateColorPicker(world, parent);

        Assert.True(world.Has<UIElement>(picker));
        Assert.True(world.Has<UIRect>(picker));
        Assert.True(world.Has<UIStyle>(picker));
        Assert.True(world.Has<UILayout>(picker));
        Assert.True(world.Has<UIColorPicker>(picker));
    }

    [Fact]
    public void CreateColorPicker_HasVerticalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateColorPicker(world, parent);

        ref readonly var layout = ref world.Get<UILayout>(picker);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateColorPicker_AppliesDefaultSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateColorPicker(world, parent);

        ref readonly var rect = ref world.Get<UIRect>(picker);
        Assert.Equal(250f, rect.Size.X);
        Assert.Equal(300f, rect.Size.Y);
    }

    [Fact]
    public void CreateColorPicker_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(Width: 400, Height: 500, CornerRadius: 8f);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(picker);
        Assert.Equal(400f, rect.Size.X);
        Assert.Equal(500f, rect.Size.Y);
    }

    [Fact]
    public void CreateColorPicker_WithInitialColor_SetsColor()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var blueColor = new Vector4(0f, 0f, 1f, 1f);
        var config = new ColorPickerConfig(InitialColor: blueColor);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.Equal(blueColor, pickerData.Color);
    }

    [Fact]
    public void CreateColorPicker_DefaultColor_IsRed()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateColorPicker(world, parent);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.Equal(new Vector4(1f, 0f, 0f, 1f), pickerData.Color);
    }

    [Fact]
    public void CreateColorPicker_HSVMode_HasSatValArea()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(Mode: ColorPickerMode.HSV);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(world.IsAlive(pickerData.SatValAreaEntity));
        Assert.True(world.Has<UIColorSatValArea>(pickerData.SatValAreaEntity));
    }

    [Fact]
    public void CreateColorPicker_HasHueSlider()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(Mode: ColorPickerMode.HSV);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(world.IsAlive(pickerData.HueSliderEntity));
        Assert.True(world.Has<UIColorSlider>(pickerData.HueSliderEntity));
    }

    [Fact]
    public void CreateColorPicker_ShowAlpha_HasAlphaSlider()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(ShowAlpha: true);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(world.IsAlive(pickerData.AlphaSliderEntity));
    }

    [Fact]
    public void CreateColorPicker_HideAlpha_NoAlphaSlider()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(ShowAlpha: false);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.Equal(Entity.Null, pickerData.AlphaSliderEntity);
    }

    [Fact]
    public void CreateColorPicker_ShowPreview_HasPreviewEntity()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(ShowPreview: true);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(world.IsAlive(pickerData.PreviewEntity));
    }

    [Fact]
    public void CreateColorPicker_HidePreview_NoPreviewEntity()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(ShowPreview: false);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.Equal(Entity.Null, pickerData.PreviewEntity);
    }

    [Fact]
    public void CreateColorPicker_SatValArea_IsInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(Mode: ColorPickerMode.HSV);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(world.Has<UIInteractable>(pickerData.SatValAreaEntity));
        ref readonly var interactable = ref world.Get<UIInteractable>(pickerData.SatValAreaEntity);
        Assert.True(interactable.CanClick);
        Assert.True(interactable.CanDrag);
    }

    [Fact]
    public void CreateColorPicker_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateColorPicker(world, parent, "MyColorPicker");

        Assert.Equal("MyColorPicker", world.GetName(picker));
    }

    [Fact]
    public void CreateColorPicker_StoresMode()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(Mode: ColorPickerMode.RGB);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.Equal(ColorPickerMode.RGB, pickerData.Mode);
    }

    [Fact]
    public void CreateColorPicker_StoresShowAlpha()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(ShowAlpha: false);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.False(pickerData.ShowAlpha);
    }

    [Fact]
    public void CreateColorPicker_StoresShowHexInput()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig(ShowHexInput: false);

        var picker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.False(pickerData.ShowHexInput);
    }

    #endregion

    #region DatePicker Tests

    [Fact]
    public void CreateDatePicker_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont);

        Assert.True(world.Has<UIElement>(picker));
        Assert.True(world.Has<UIRect>(picker));
        Assert.True(world.Has<UIStyle>(picker));
        Assert.True(world.Has<UILayout>(picker));
        Assert.True(world.Has<UIDatePicker>(picker));
    }

    [Fact]
    public void CreateDatePicker_HasVerticalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont);

        ref readonly var layout = ref world.Get<UILayout>(picker);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateDatePicker_AppliesDefaultSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont);

        ref readonly var rect = ref world.Get<UIRect>(picker);
        Assert.Equal(280f, rect.Size.X);
        Assert.Equal(320f, rect.Size.Y);
    }

    [Fact]
    public void CreateDatePicker_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(Width: 400, Height: 500);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(picker);
        Assert.Equal(400f, rect.Size.X);
        Assert.Equal(500f, rect.Size.Y);
    }

    [Fact]
    public void CreateDatePicker_WithInitialValue_SetsValue()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var initialDate = new DateTime(2024, 12, 25, 10, 30, 0);
        var config = new DatePickerConfig(InitialValue: initialDate);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(initialDate, pickerData.Value);
    }

    [Fact]
    public void CreateDatePicker_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, "MyDatePicker");

        Assert.Equal("MyDatePicker", world.GetName(picker));
    }

    [Fact]
    public void CreateDatePicker_DateMode_HasCalendarChildren()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(Mode: DatePickerMode.Date);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        // Date mode should have calendar children (header and grid)
        var children = world.GetChildren(picker).ToList();
        Assert.True(children.Count >= 1);
    }

    [Fact]
    public void CreateDatePicker_StoresMode()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(Mode: DatePickerMode.DateTime);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(DatePickerMode.DateTime, pickerData.Mode);
    }

    [Fact]
    public void CreateDatePicker_StoresTimeFormat()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(TimeFormat: TimeFormat.Hour12);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(TimeFormat.Hour12, pickerData.TimeFormat);
    }

    [Fact]
    public void CreateDatePicker_StoresShowSeconds()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(ShowSeconds: true);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.True(pickerData.ShowSeconds);
    }

    [Fact]
    public void CreateDatePicker_StoresMinMaxDate()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var minDate = new DateTime(2020, 1, 1);
        var maxDate = new DateTime(2030, 12, 31);
        var config = new DatePickerConfig(MinDate: minDate, MaxDate: maxDate);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(minDate, pickerData.MinDate);
        Assert.Equal(maxDate, pickerData.MaxDate);
    }

    [Fact]
    public void CreateDatePicker_StoresFirstDayOfWeek()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(FirstDayOfWeek: DayOfWeek.Monday);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(DayOfWeek.Monday, pickerData.FirstDayOfWeek);
    }

    [Fact]
    public void CreateDatePicker_DateMode_HasNavigationButtons()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(Mode: DatePickerMode.Date);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, "DatePicker", config);

        // Should have prev and next buttons in the header
        // Find by searching for entities with name pattern
        var hasPrev = false;
        var hasNext = false;
        foreach (var entity in world.Query<UIElement>())
        {
            var name = world.GetName(entity);
            if (name == "DatePicker_Prev")
            {
                hasPrev = true;
            }
            if (name == "DatePicker_Next")
            {
                hasNext = true;
            }
        }
        Assert.True(hasPrev);
        Assert.True(hasNext);
    }

    [Fact]
    public void CreateDatePicker_NavigationButtons_AreClickable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig(Mode: DatePickerMode.Date);

        var picker = WidgetFactory.CreateDatePicker(world, parent, testFont, "DatePicker", config);

        // Find prev button and verify it's clickable
        Entity prevButton = Entity.Null;
        foreach (var entity in world.Query<UIElement>())
        {
            if (world.GetName(entity) == "DatePicker_Prev")
            {
                prevButton = entity;
                break;
            }
        }
        Assert.NotEqual(Entity.Null, prevButton);
        Assert.True(world.Has<UIInteractable>(prevButton));
        ref readonly var interactable = ref world.Get<UIInteractable>(prevButton);
        Assert.True(interactable.CanClick);
    }

    #endregion

    #region ColorPickerConfig Factory Methods Tests

    [Fact]
    public void ColorPickerConfig_HSV_SetsCorrectMode()
    {
        var config = ColorPickerConfig.HSV();

        Assert.Equal(ColorPickerMode.HSV, config.Mode);
    }

    [Fact]
    public void ColorPickerConfig_RGB_SetsCorrectMode()
    {
        var config = ColorPickerConfig.RGB();

        Assert.Equal(ColorPickerMode.RGB, config.Mode);
    }

    [Fact]
    public void ColorPickerConfig_Compact_SetsReducedSize()
    {
        var config = ColorPickerConfig.Compact();

        Assert.Equal(200f, config.Width);
        Assert.Equal(220f, config.Height);
        Assert.False(config.ShowHexInput);
    }

    [Fact]
    public void ColorPickerConfig_Opaque_HidesAlpha()
    {
        var config = ColorPickerConfig.Opaque();

        Assert.False(config.ShowAlpha);
    }

    #endregion

    #region DatePickerConfig Factory Methods Tests

    [Fact]
    public void DatePickerConfig_DateOnly_SetsDateMode()
    {
        var config = DatePickerConfig.DateOnly();

        Assert.Equal(DatePickerMode.Date, config.Mode);
    }

    [Fact]
    public void DatePickerConfig_TimeOnly_SetsTimeMode()
    {
        var config = DatePickerConfig.TimeOnly();

        Assert.Equal(DatePickerMode.Time, config.Mode);
        Assert.Equal(100f, config.Height); // Reduced height for time-only
    }

    [Fact]
    public void DatePickerConfig_TimeOnly_CanSpecifyFormat()
    {
        var config = DatePickerConfig.TimeOnly(format: TimeFormat.Hour12);

        Assert.Equal(TimeFormat.Hour12, config.TimeFormat);
    }

    #endregion

    #region Helper Methods

    private static Entity CreateRootEntity(World world)
    {
        var root = world.Spawn("Root")
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var layout = new UILayoutSystem();
        world.AddSystem(layout);
        layout.Initialize(world);
        layout.Update(0);

        return root;
    }

    #endregion
}
