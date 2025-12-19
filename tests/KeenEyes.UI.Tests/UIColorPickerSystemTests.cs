using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIColorPickerSystem color conversion and interaction handling.
/// </summary>
public class UIColorPickerSystemTests
{
    #region HSV to RGB Conversion Tests

    [Fact]
    public void HsvToRgb_RedHue_ReturnsRed()
    {
        var result = UIColorPickerSystem.HsvToRgb(0f, 1f, 1f);

        Assert.True(result.X.ApproximatelyEquals(1f)); // Red
        Assert.True(result.Y.ApproximatelyEquals(0f)); // Green
        Assert.True(result.Z.ApproximatelyEquals(0f)); // Blue
        Assert.True(result.W.ApproximatelyEquals(1f)); // Alpha
    }

    [Fact]
    public void HsvToRgb_GreenHue_ReturnsGreen()
    {
        var result = UIColorPickerSystem.HsvToRgb(120f, 1f, 1f);

        Assert.True(result.X.ApproximatelyEquals(0f)); // Red
        Assert.True(result.Y.ApproximatelyEquals(1f)); // Green
        Assert.True(result.Z.ApproximatelyEquals(0f)); // Blue
    }

    [Fact]
    public void HsvToRgb_BlueHue_ReturnsBlue()
    {
        var result = UIColorPickerSystem.HsvToRgb(240f, 1f, 1f);

        Assert.True(result.X.ApproximatelyEquals(0f)); // Red
        Assert.True(result.Y.ApproximatelyEquals(0f)); // Green
        Assert.True(result.Z.ApproximatelyEquals(1f)); // Blue
    }

    [Fact]
    public void HsvToRgb_ZeroSaturation_ReturnsGray()
    {
        var result = UIColorPickerSystem.HsvToRgb(0f, 0f, 0.5f);

        Assert.True(result.X.ApproximatelyEquals(0.5f));
        Assert.True(result.Y.ApproximatelyEquals(0.5f));
        Assert.True(result.Z.ApproximatelyEquals(0.5f));
    }

    [Fact]
    public void HsvToRgb_ZeroValue_ReturnsBlack()
    {
        var result = UIColorPickerSystem.HsvToRgb(180f, 1f, 0f);

        Assert.True(result.X.ApproximatelyEquals(0f));
        Assert.True(result.Y.ApproximatelyEquals(0f));
        Assert.True(result.Z.ApproximatelyEquals(0f));
    }

    [Fact]
    public void HsvToRgb_YellowHue_ReturnsYellow()
    {
        var result = UIColorPickerSystem.HsvToRgb(60f, 1f, 1f);

        Assert.True(result.X.ApproximatelyEquals(1f)); // Red
        Assert.True(result.Y.ApproximatelyEquals(1f)); // Green
        Assert.True(result.Z.ApproximatelyEquals(0f)); // Blue
    }

    [Fact]
    public void HsvToRgb_CyanHue_ReturnsCyan()
    {
        var result = UIColorPickerSystem.HsvToRgb(180f, 1f, 1f);

        Assert.True(result.X.ApproximatelyEquals(0f)); // Red
        Assert.True(result.Y.ApproximatelyEquals(1f)); // Green
        Assert.True(result.Z.ApproximatelyEquals(1f)); // Blue
    }

    [Fact]
    public void HsvToRgb_MagentaHue_ReturnsMagenta()
    {
        var result = UIColorPickerSystem.HsvToRgb(300f, 1f, 1f);

        Assert.True(result.X.ApproximatelyEquals(1f)); // Red
        Assert.True(result.Y.ApproximatelyEquals(0f)); // Green
        Assert.True(result.Z.ApproximatelyEquals(1f)); // Blue
    }

    [Fact]
    public void HsvToRgb_WithCustomAlpha_PreservesAlpha()
    {
        var result = UIColorPickerSystem.HsvToRgb(0f, 1f, 1f, 0.5f);

        Assert.True(result.W.ApproximatelyEquals(0.5f));
    }

    #endregion

    #region RGB to HSV Conversion Tests

    [Fact]
    public void RgbToHue_Red_ReturnsZero()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);
        var hue = UIColorPickerSystem.RgbToHue(color);

        Assert.True(hue.ApproximatelyEquals(0f));
    }

    [Fact]
    public void RgbToHue_Green_Returns120()
    {
        var color = new Vector4(0f, 1f, 0f, 1f);
        var hue = UIColorPickerSystem.RgbToHue(color);

        Assert.True(hue.ApproximatelyEquals(120f));
    }

    [Fact]
    public void RgbToHue_Blue_Returns240()
    {
        var color = new Vector4(0f, 0f, 1f, 1f);
        var hue = UIColorPickerSystem.RgbToHue(color);

        Assert.True(hue.ApproximatelyEquals(240f));
    }

    [Fact]
    public void RgbToHue_Gray_ReturnsZero()
    {
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var hue = UIColorPickerSystem.RgbToHue(color);

        // Gray has no hue, should return 0
        Assert.True(hue.ApproximatelyEquals(0f));
    }

    [Fact]
    public void RgbToSaturation_FullySaturated_ReturnsOne()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);
        var saturation = UIColorPickerSystem.RgbToSaturation(color);

        Assert.True(saturation.ApproximatelyEquals(1f));
    }

    [Fact]
    public void RgbToSaturation_Gray_ReturnsZero()
    {
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var saturation = UIColorPickerSystem.RgbToSaturation(color);

        Assert.True(saturation.ApproximatelyEquals(0f));
    }

    [Fact]
    public void RgbToSaturation_Black_ReturnsZero()
    {
        var color = new Vector4(0f, 0f, 0f, 1f);
        var saturation = UIColorPickerSystem.RgbToSaturation(color);

        Assert.True(saturation.ApproximatelyEquals(0f));
    }

    [Fact]
    public void RgbToValue_White_ReturnsOne()
    {
        var color = new Vector4(1f, 1f, 1f, 1f);
        var value = UIColorPickerSystem.RgbToValue(color);

        Assert.True(value.ApproximatelyEquals(1f));
    }

    [Fact]
    public void RgbToValue_Black_ReturnsZero()
    {
        var color = new Vector4(0f, 0f, 0f, 1f);
        var value = UIColorPickerSystem.RgbToValue(color);

        Assert.True(value.ApproximatelyEquals(0f));
    }

    [Fact]
    public void RgbToValue_HalfBrightRed_ReturnsHalf()
    {
        var color = new Vector4(0.5f, 0f, 0f, 1f);
        var value = UIColorPickerSystem.RgbToValue(color);

        Assert.True(value.ApproximatelyEquals(0.5f));
    }

    #endregion

    #region Hex Conversion Tests

    [Fact]
    public void ColorToHex_Red_ReturnsFF0000()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);
        var hex = UIColorPickerSystem.ColorToHex(color);

        Assert.Equal("#FF0000", hex);
    }

    [Fact]
    public void ColorToHex_Green_Returns00FF00()
    {
        var color = new Vector4(0f, 1f, 0f, 1f);
        var hex = UIColorPickerSystem.ColorToHex(color);

        Assert.Equal("#00FF00", hex);
    }

    [Fact]
    public void ColorToHex_Blue_Returns0000FF()
    {
        var color = new Vector4(0f, 0f, 1f, 1f);
        var hex = UIColorPickerSystem.ColorToHex(color);

        Assert.Equal("#0000FF", hex);
    }

    [Fact]
    public void ColorToHex_WithAlpha_IncludesAlpha()
    {
        var color = new Vector4(1f, 0f, 0f, 0.5f);
        var hex = UIColorPickerSystem.ColorToHex(color, includeAlpha: true);

        Assert.Equal("#FF00007F", hex);
    }

    [Fact]
    public void ColorToHex_Black_Returns000000()
    {
        var color = new Vector4(0f, 0f, 0f, 1f);
        var hex = UIColorPickerSystem.ColorToHex(color);

        Assert.Equal("#000000", hex);
    }

    [Fact]
    public void ColorToHex_White_ReturnsFFFFFF()
    {
        var color = new Vector4(1f, 1f, 1f, 1f);
        var hex = UIColorPickerSystem.ColorToHex(color);

        Assert.Equal("#FFFFFF", hex);
    }

    [Fact]
    public void TryParseHex_ValidRed_Succeeds()
    {
        var success = UIColorPickerSystem.TryParseHex("#FF0000", out var color);

        Assert.True(success);
        Assert.True(color.X.ApproximatelyEquals(1f));
        Assert.True(color.Y.ApproximatelyEquals(0f));
        Assert.True(color.Z.ApproximatelyEquals(0f));
        Assert.True(color.W.ApproximatelyEquals(1f));
    }

    [Fact]
    public void TryParseHex_ValidWithAlpha_ParsesAlpha()
    {
        var success = UIColorPickerSystem.TryParseHex("#FF00007F", out var color);

        Assert.True(success);
        Assert.True(color.X.ApproximatelyEquals(1f));
        Assert.True(color.W.ApproximatelyEquals(0.498f, 0.01f));
    }

    [Fact]
    public void TryParseHex_WithoutHash_Succeeds()
    {
        var success = UIColorPickerSystem.TryParseHex("00FF00", out var color);

        Assert.True(success);
        Assert.True(color.Y.ApproximatelyEquals(1f));
    }

    [Fact]
    public void TryParseHex_Empty_Fails()
    {
        var success = UIColorPickerSystem.TryParseHex("", out var color);

        Assert.False(success);
        Assert.Equal(Vector4.Zero, color);
    }

    [Fact]
    public void TryParseHex_Null_Fails()
    {
        var success = UIColorPickerSystem.TryParseHex(null!, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParseHex_InvalidLength_Fails()
    {
        var success = UIColorPickerSystem.TryParseHex("#FFF", out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParseHex_InvalidChars_Fails()
    {
        var success = UIColorPickerSystem.TryParseHex("#GGGGGG", out _);

        Assert.False(success);
    }

    #endregion

    #region Sat-Val Area Click Tests

    [Fact]
    public void SatValArea_Click_UpdatesSaturationAndValue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        // Get sat-val area entity
        var satValEntity = world.Get<UIColorPicker>(picker).SatValAreaEntity;

        // Click at center (50% sat, 50% val)
        SimulateClick(world, satValEntity, new Vector2(128, 128));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Saturation.ApproximatelyEquals(0.5f));
        Assert.True(pickerData.Value.ApproximatelyEquals(0.5f));
    }

    [Fact]
    public void SatValArea_ClickTopRight_FullSatFullValue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var satValEntity = world.Get<UIColorPicker>(picker).SatValAreaEntity;

        // Click at top-right (full sat, full val)
        SimulateClick(world, satValEntity, new Vector2(256, 0));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Saturation.ApproximatelyEquals(1f));
        Assert.True(pickerData.Value.ApproximatelyEquals(1f));
    }

    [Fact]
    public void SatValArea_ClickBottomLeft_ZeroSatZeroValue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var satValEntity = world.Get<UIColorPicker>(picker).SatValAreaEntity;

        // Click at bottom-left (zero sat, zero val)
        SimulateClick(world, satValEntity, new Vector2(0, 256));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Saturation.ApproximatelyEquals(0f));
        Assert.True(pickerData.Value.ApproximatelyEquals(0f));
    }

    [Fact]
    public void SatValArea_Click_UpdatesRGBColor()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        // Start with red hue
        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var satValEntity = world.Get<UIColorPicker>(picker).SatValAreaEntity;

        // Click at top-right (full sat, full val) - should be pure red
        SimulateClick(world, satValEntity, new Vector2(256, 0));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Color.X.ApproximatelyEquals(1f)); // Red
        Assert.True(pickerData.Color.Y.ApproximatelyEquals(0f)); // Green
        Assert.True(pickerData.Color.Z.ApproximatelyEquals(0f)); // Blue
    }

    #endregion

    #region Sat-Val Area Drag Tests

    [Fact]
    public void SatValArea_Drag_UpdatesSaturationAndValue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var satValEntity = world.Get<UIColorPicker>(picker).SatValAreaEntity;

        // Drag to 75% position
        SimulateDrag(world, satValEntity, new Vector2(192, 64));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Saturation.ApproximatelyEquals(0.75f));
        Assert.True(pickerData.Value.ApproximatelyEquals(0.75f));
    }

    #endregion

    #region Hue Slider Tests

    [Fact]
    public void HueSlider_Click_UpdatesHue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var hueSliderEntity = world.Get<UIColorPicker>(picker).HueSliderEntity;

        // Click at middle (180 degrees / 360 = 0.5)
        SimulateClick(world, hueSliderEntity, new Vector2(128, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Hue.ApproximatelyEquals(180f));
    }

    [Fact]
    public void HueSlider_ClickAtStart_SetsZeroHue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(0f, 1f, 1f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var hueSliderEntity = world.Get<UIColorPicker>(picker).HueSliderEntity;

        SimulateClick(world, hueSliderEntity, new Vector2(0, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Hue.ApproximatelyEquals(0f));
    }

    [Fact]
    public void HueSlider_ClickAtEnd_Sets360Hue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var hueSliderEntity = world.Get<UIColorPicker>(picker).HueSliderEntity;

        SimulateClick(world, hueSliderEntity, new Vector2(256, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Hue.ApproximatelyEquals(360f));
    }

    #endregion

    #region Alpha Slider Tests

    [Fact]
    public void AlphaSlider_Click_UpdatesAlpha()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256, showAlpha: true);
        layout.Update(0);

        var alphaSliderEntity = world.Get<UIColorPicker>(picker).AlphaSliderEntity;
        if (!world.IsAlive(alphaSliderEntity))
        {
            // Skip if alpha slider not created
            return;
        }

        SimulateClick(world, alphaSliderEntity, new Vector2(128, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Color.W.ApproximatelyEquals(0.5f));
    }

    #endregion

    #region RGB Slider Tests

    [Fact]
    public void RedSlider_Click_UpdatesRed()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPickerRgbMode(world, new Vector4(0f, 0f, 1f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var redSliderEntity = world.Get<UIColorPicker>(picker).RedSliderEntity;
        if (!world.IsAlive(redSliderEntity))
        {
            return;
        }

        SimulateClick(world, redSliderEntity, new Vector2(128, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Color.X.ApproximatelyEquals(0.5f));
    }

    [Fact]
    public void GreenSlider_Click_UpdatesGreen()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPickerRgbMode(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var greenSliderEntity = world.Get<UIColorPicker>(picker).GreenSliderEntity;
        if (!world.IsAlive(greenSliderEntity))
        {
            return;
        }

        SimulateClick(world, greenSliderEntity, new Vector2(192, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Color.Y.ApproximatelyEquals(0.75f));
    }

    [Fact]
    public void BlueSlider_Click_UpdatesBlue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPickerRgbMode(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var blueSliderEntity = world.Get<UIColorPicker>(picker).BlueSliderEntity;
        if (!world.IsAlive(blueSliderEntity))
        {
            return;
        }

        SimulateClick(world, blueSliderEntity, new Vector2(256, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Color.Z.ApproximatelyEquals(1f));
    }

    [Fact]
    public void RgbSlider_Click_UpdatesHsvValues()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPickerRgbMode(world, new Vector4(0f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        var redSliderEntity = world.Get<UIColorPicker>(picker).RedSliderEntity;
        if (!world.IsAlive(redSliderEntity))
        {
            return;
        }

        // Set red to full - should update HSV accordingly
        SimulateClick(world, redSliderEntity, new Vector2(256, 10));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        // Pure red has hue 0, sat 1, val 1
        Assert.True(pickerData.Hue.ApproximatelyEquals(0f));
        Assert.True(pickerData.Saturation.ApproximatelyEquals(1f));
        Assert.True(pickerData.Value.ApproximatelyEquals(1f));
    }

    #endregion

    #region API Tests

    [Fact]
    public void SetColor_ValidEntity_UpdatesColor()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        system.SetColor(picker, new Vector4(0f, 1f, 0f, 0.5f));

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Color.X.ApproximatelyEquals(0f));
        Assert.True(pickerData.Color.Y.ApproximatelyEquals(1f));
        Assert.True(pickerData.Color.Z.ApproximatelyEquals(0f));
        Assert.True(pickerData.Color.W.ApproximatelyEquals(0.5f));
    }

    [Fact]
    public void SetColor_UpdatesHsvValues()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        // Set to green
        system.SetColor(picker, new Vector4(0f, 1f, 0f, 1f));

        ref readonly var pickerData = ref world.Get<UIColorPicker>(picker);
        Assert.True(pickerData.Hue.ApproximatelyEquals(120f)); // Green hue
        Assert.True(pickerData.Saturation.ApproximatelyEquals(1f));
        Assert.True(pickerData.Value.ApproximatelyEquals(1f));
    }

    [Fact]
    public void SetColor_InvalidEntity_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        // Should not throw
        system.SetColor(Entity.Null, new Vector4(1f, 0f, 0f, 1f));
    }

    [Fact]
    public void GetColor_ValidEntity_ReturnsColor()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0.5f, 0.25f, 0.75f), 0, 0, 256, 256);
        layout.Update(0);

        var color = system.GetColor(picker);

        Assert.True(color.X.ApproximatelyEquals(1f));
        Assert.True(color.Y.ApproximatelyEquals(0.5f));
        Assert.True(color.Z.ApproximatelyEquals(0.25f));
        Assert.True(color.W.ApproximatelyEquals(0.75f));
    }

    [Fact]
    public void GetColor_InvalidEntity_ReturnsZero()
    {
        using var world = new World();
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var color = system.GetColor(Entity.Null);

        Assert.Equal(Vector4.Zero, color);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void SatValArea_Click_RaisesColorChangedEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        UIColorChangedEvent? receivedEvent = null;
        world.Subscribe<UIColorChangedEvent>(evt => receivedEvent = evt);

        var satValEntity = world.Get<UIColorPicker>(picker).SatValAreaEntity;
        SimulateClick(world, satValEntity, new Vector2(128, 128));
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(picker, receivedEvent.Value.Entity);
    }

    [Fact]
    public void SetColor_RaisesColorChangedEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var picker = CreateColorPicker(world, new Vector4(1f, 0f, 0f, 1f), 0, 0, 256, 256);
        layout.Update(0);

        UIColorChangedEvent? receivedEvent = null;
        world.Subscribe<UIColorChangedEvent>(evt => receivedEvent = evt);

        system.SetColor(picker, new Vector4(0f, 1f, 0f, 1f));

        Assert.NotNull(receivedEvent);
        Assert.Equal(picker, receivedEvent.Value.Entity);
        Assert.True(receivedEvent.Value.OldColor.X.ApproximatelyEquals(1f)); // Was red
        Assert.True(receivedEvent.Value.NewColor.Y.ApproximatelyEquals(1f)); // Now green
    }

    [Fact]
    public void SetColor_SameColor_DoesNotRaiseEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIColorPickerSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var initialColor = new Vector4(1f, 0f, 0f, 1f);
        var picker = CreateColorPicker(world, initialColor, 0, 0, 256, 256);
        layout.Update(0);

        UIColorChangedEvent? receivedEvent = null;
        world.Subscribe<UIColorChangedEvent>(evt => receivedEvent = evt);

        // Set to same color
        system.SetColor(picker, initialColor);

        Assert.Null(receivedEvent);
    }

    #endregion

    #region Component Tests

    [Fact]
    public void UIColorPicker_Initialization_SetsHsvFromColor()
    {
        var picker = new UIColorPicker(new Vector4(0f, 1f, 0f, 1f)); // Green

        Assert.True(picker.Hue.ApproximatelyEquals(120f));
        Assert.True(picker.Saturation.ApproximatelyEquals(1f));
        Assert.True(picker.Value.ApproximatelyEquals(1f));
    }

    [Fact]
    public void UIColorPicker_DefaultMode_IsHSV()
    {
        var picker = new UIColorPicker(new Vector4(1f, 0f, 0f, 1f));

        Assert.Equal(ColorPickerMode.HSV, picker.Mode);
    }

    [Fact]
    public void UIColorPicker_DefaultShowAlpha_IsTrue()
    {
        var picker = new UIColorPicker(new Vector4(1f, 0f, 0f, 1f));

        Assert.True(picker.ShowAlpha);
    }

    [Fact]
    public void UIColorPicker_DefaultShowHexInput_IsTrue()
    {
        var picker = new UIColorPicker(new Vector4(1f, 0f, 0f, 1f));

        Assert.True(picker.ShowHexInput);
    }

    [Fact]
    public void UIColorSlider_Initialization_SetsChannel()
    {
        var slider = new UIColorSlider(Entity.Null, ColorChannel.Hue);

        Assert.Equal(ColorChannel.Hue, slider.Channel);
    }

    [Fact]
    public void UIColorSatValArea_Initialization_SetsColorPicker()
    {
        var testEntity = new Entity(1, 1);
        var area = new UIColorSatValArea(testEntity);

        Assert.Equal(testEntity, area.ColorPicker);
    }

    #endregion

    #region Helper Methods

    private static UILayoutSystem SetupLayout(World world)
    {
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.Initialize(world);
        layoutSystem.SetScreenSize(800, 600);
        return layoutSystem;
    }

    private static Entity CreateColorPicker(World world, Vector4 initialColor,
        float x, float y, float width, float height, bool showAlpha = true)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create preview entity
        var preview = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(40, 40) })
            .With(new UIStyle { BackgroundColor = initialColor })
            .Build();

        // Create sat-val area entity
        var satValArea = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, width, height))
            .With(new UIInteractable { CanClick = true, CanDrag = true })
            .Build();

        // Create hue slider entity
        var hueSlider = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, height + 10, width, 20))
            .With(new UIInteractable { CanClick = true, CanDrag = true })
            .Build();

        // Create alpha slider if needed
        Entity alphaSlider = Entity.Null;
        if (showAlpha)
        {
            alphaSlider = world.Spawn()
                .With(UIElement.Default)
                .With(UIRect.Fixed(0, height + 40, width, 20))
                .With(new UIInteractable { CanClick = true, CanDrag = true })
                .Build();
        }

        // Create color picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(x, y, width, height + (showAlpha ? 70 : 40)))
            .With(new UIColorPicker(initialColor)
            {
                Mode = ColorPickerMode.HSV,
                ShowAlpha = showAlpha,
                PreviewEntity = preview,
                SatValAreaEntity = satValArea,
                HueSliderEntity = hueSlider,
                AlphaSliderEntity = alphaSlider
            })
            .Build();

        // Add UIColorSatValArea component to sat-val area
        world.Add(satValArea, new UIColorSatValArea(picker));

        // Add UIColorSlider components to sliders
        world.Add(hueSlider, new UIColorSlider(picker, ColorChannel.Hue));
        if (showAlpha)
        {
            world.Add(alphaSlider, new UIColorSlider(picker, ColorChannel.Alpha));
        }

        // Set up hierarchy
        world.SetParent(picker, canvas);
        world.SetParent(preview, picker);
        world.SetParent(satValArea, picker);
        world.SetParent(hueSlider, picker);
        if (showAlpha)
        {
            world.SetParent(alphaSlider, picker);
        }

        return picker;
    }

    private static Entity CreateColorPickerRgbMode(World world, Vector4 initialColor,
        float x, float y, float width, float height)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create preview entity
        var preview = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(40, 40) })
            .With(new UIStyle { BackgroundColor = initialColor })
            .Build();

        // Create RGB sliders
        var redSlider = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, width, 20))
            .With(new UIInteractable { CanClick = true, CanDrag = true })
            .Build();

        var greenSlider = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 30, width, 20))
            .With(new UIInteractable { CanClick = true, CanDrag = true })
            .Build();

        var blueSlider = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 60, width, 20))
            .With(new UIInteractable { CanClick = true, CanDrag = true })
            .Build();

        // Create color picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(x, y, width, 100))
            .With(new UIColorPicker(initialColor)
            {
                Mode = ColorPickerMode.RGB,
                ShowAlpha = false,
                PreviewEntity = preview,
                RedSliderEntity = redSlider,
                GreenSliderEntity = greenSlider,
                BlueSliderEntity = blueSlider
            })
            .Build();

        // Add UIColorSlider components
        world.Add(redSlider, new UIColorSlider(picker, ColorChannel.Red));
        world.Add(greenSlider, new UIColorSlider(picker, ColorChannel.Green));
        world.Add(blueSlider, new UIColorSlider(picker, ColorChannel.Blue));

        // Set up hierarchy
        world.SetParent(picker, canvas);
        world.SetParent(preview, picker);
        world.SetParent(redSlider, picker);
        world.SetParent(greenSlider, picker);
        world.SetParent(blueSlider, picker);

        return picker;
    }

    private static void SimulateClick(World world, Entity entity, Vector2 position)
    {
        var clickEvent = new UIClickEvent(entity, position, MouseButton.Left);
        world.Send(clickEvent);
    }

    private static void SimulateDrag(World world, Entity entity, Vector2 position)
    {
        var dragEvent = new UIDragEvent(entity, position, Vector2.Zero);
        world.Send(dragEvent);
    }

    #endregion
}
