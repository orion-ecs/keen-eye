using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetConfig record types.
/// </summary>
public sealed class WidgetConfigTests
{
    #region ButtonConfig Tests

    [Fact]
    public void ButtonConfig_Default_HasExpectedValues()
    {
        var config = ButtonConfig.Default;

        Assert.Equal(100f, config.Width);
        Assert.Equal(40f, config.Height);
        Assert.Null(config.BackgroundColor);
        Assert.Null(config.BorderColor);
        Assert.Equal(0f, config.BorderWidth);
        Assert.Equal(4f, config.CornerRadius);
        Assert.Null(config.TextColor);
        Assert.Equal(16f, config.FontSize);
        Assert.Equal(0, config.TabIndex);
    }

    [Fact]
    public void ButtonConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new ButtonConfig();

        var color = config.GetBackgroundColor();

        Assert.Equal(new Vector4(0.3f, 0.5f, 0.8f, 1f), color);
    }

    [Fact]
    public void ButtonConfig_GetBackgroundColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(1f, 0f, 0f, 1f);
        var config = new ButtonConfig(BackgroundColor: customColor);

        var color = config.GetBackgroundColor();

        Assert.Equal(customColor, color);
    }

    [Fact]
    public void ButtonConfig_GetBorderColor_ReturnsDefaultWhenNull()
    {
        var config = new ButtonConfig();

        var color = config.GetBorderColor();

        Assert.Equal(new Vector4(0.4f, 0.6f, 0.9f, 1f), color);
    }

    [Fact]
    public void ButtonConfig_GetBorderColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0f, 1f, 0f, 1f);
        var config = new ButtonConfig(BorderColor: customColor);

        var color = config.GetBorderColor();

        Assert.Equal(customColor, color);
    }

    [Fact]
    public void ButtonConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new ButtonConfig();

        var color = config.GetTextColor();

        Assert.Equal(new Vector4(1f, 1f, 1f, 1f), color);
    }

    [Fact]
    public void ButtonConfig_GetTextColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0f, 0f, 1f, 1f);
        var config = new ButtonConfig(TextColor: customColor);

        var color = config.GetTextColor();

        Assert.Equal(customColor, color);
    }

    [Fact]
    public void ButtonConfig_WithCustomValues_PreservesValues()
    {
        var config = new ButtonConfig(
            Width: 200,
            Height: 60,
            BorderWidth: 2,
            CornerRadius: 8,
            FontSize: 20,
            TabIndex: 5);

        Assert.Equal(200f, config.Width);
        Assert.Equal(60f, config.Height);
        Assert.Equal(2f, config.BorderWidth);
        Assert.Equal(8f, config.CornerRadius);
        Assert.Equal(20f, config.FontSize);
        Assert.Equal(5, config.TabIndex);
    }

    #endregion

    #region PanelConfig Tests

    [Fact]
    public void PanelConfig_Default_HasExpectedValues()
    {
        var config = PanelConfig.Default;

        Assert.Null(config.Width);
        Assert.Null(config.Height);
        Assert.Equal(LayoutDirection.Vertical, config.Direction);
        Assert.Equal(LayoutAlign.Start, config.MainAxisAlign);
        Assert.Equal(LayoutAlign.Start, config.CrossAxisAlign);
        Assert.Equal(8f, config.Spacing);
        Assert.Null(config.Padding);
        Assert.Null(config.BackgroundColor);
        Assert.Equal(0f, config.CornerRadius);
    }

    [Fact]
    public void PanelConfig_GetPadding_ReturnsZeroWhenNull()
    {
        var config = new PanelConfig();

        var padding = config.GetPadding();

        Assert.Equal(UIEdges.Zero, padding);
    }

    [Fact]
    public void PanelConfig_GetPadding_ReturnsCustomWhenSet()
    {
        var customPadding = new UIEdges { Left = 10, Right = 10, Top = 5, Bottom = 5 };
        var config = new PanelConfig(Padding: customPadding);

        var padding = config.GetPadding();

        Assert.Equal(customPadding, padding);
    }

    [Fact]
    public void PanelConfig_GetBackgroundColor_ReturnsTransparentWhenNull()
    {
        var config = new PanelConfig();

        var color = config.GetBackgroundColor();

        Assert.Equal(Vector4.Zero, color);
    }

    [Fact]
    public void PanelConfig_GetBackgroundColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var config = new PanelConfig(BackgroundColor: customColor);

        var color = config.GetBackgroundColor();

        Assert.Equal(customColor, color);
    }

    [Fact]
    public void PanelConfig_WithCustomValues_PreservesValues()
    {
        var config = new PanelConfig(
            Width: 300,
            Height: 400,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.Center,
            CrossAxisAlign: LayoutAlign.End,
            Spacing: 12,
            CornerRadius: 5);

        Assert.Equal(300f, config.Width);
        Assert.Equal(400f, config.Height);
        Assert.Equal(LayoutDirection.Horizontal, config.Direction);
        Assert.Equal(LayoutAlign.Center, config.MainAxisAlign);
        Assert.Equal(LayoutAlign.End, config.CrossAxisAlign);
        Assert.Equal(12f, config.Spacing);
        Assert.Equal(5f, config.CornerRadius);
    }

    #endregion

    #region LabelConfig Tests

    [Fact]
    public void LabelConfig_Default_HasExpectedValues()
    {
        var config = LabelConfig.Default;

        Assert.Null(config.Width);
        Assert.Null(config.Height);
        Assert.Null(config.TextColor);
        Assert.Equal(14f, config.FontSize);
        Assert.Equal(TextAlignH.Left, config.HorizontalAlign);
        Assert.Equal(TextAlignV.Middle, config.VerticalAlign);
    }

    [Fact]
    public void LabelConfig_GetTextColor_ReturnsWhiteWhenNull()
    {
        var config = new LabelConfig();

        var color = config.GetTextColor();

        Assert.Equal(new Vector4(1f, 1f, 1f, 1f), color);
    }

    [Fact]
    public void LabelConfig_GetTextColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.8f, 0.2f, 0.2f, 1f);
        var config = new LabelConfig(TextColor: customColor);

        var color = config.GetTextColor();

        Assert.Equal(customColor, color);
    }

    [Fact]
    public void LabelConfig_WithCustomValues_PreservesValues()
    {
        var config = new LabelConfig(
            Width: 150,
            Height: 30,
            FontSize: 18,
            HorizontalAlign: TextAlignH.Center,
            VerticalAlign: TextAlignV.Top);

        Assert.Equal(150f, config.Width);
        Assert.Equal(30f, config.Height);
        Assert.Equal(18f, config.FontSize);
        Assert.Equal(TextAlignH.Center, config.HorizontalAlign);
        Assert.Equal(TextAlignV.Top, config.VerticalAlign);
    }

    #endregion

    #region TextFieldConfig Tests

    [Fact]
    public void TextFieldConfig_WithCustomValues_PreservesValues()
    {
        var config = new TextFieldConfig(
            Width: 300,
            Height: 40,
            PlaceholderText: "Enter text here",
            MaxLength: 100,
            BorderWidth: 2,
            FontSize: 16,
            TabIndex: 3);

        Assert.Equal(300f, config.Width);
        Assert.Equal(40f, config.Height);
        Assert.Equal("Enter text here", config.PlaceholderText);
        Assert.Equal(100, config.MaxLength);
        Assert.Equal(2f, config.BorderWidth);
        Assert.Equal(16f, config.FontSize);
        Assert.Equal(3, config.TabIndex);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void ButtonConfig_EqualityWithSameValues_ReturnsTrue()
    {
        var config1 = new ButtonConfig(Width: 150, Height: 50, FontSize: 18);
        var config2 = new ButtonConfig(Width: 150, Height: 50, FontSize: 18);

        Assert.Equal(config1, config2);
    }

    [Fact]
    public void ButtonConfig_EqualityWithDifferentValues_ReturnsFalse()
    {
        var config1 = new ButtonConfig(Width: 150, Height: 50);
        var config2 = new ButtonConfig(Width: 200, Height: 50);

        Assert.NotEqual(config1, config2);
    }

    [Fact]
    public void PanelConfig_EqualityWithSameValues_ReturnsTrue()
    {
        var padding = new UIEdges { Left = 10, Right = 10, Top = 5, Bottom = 5 };
        var config1 = new PanelConfig(Width: 300, Padding: padding);
        var config2 = new PanelConfig(Width: 300, Padding: padding);

        Assert.Equal(config1, config2);
    }

    [Fact]
    public void PanelConfig_EqualityWithDifferentValues_ReturnsFalse()
    {
        var config1 = new PanelConfig(Direction: LayoutDirection.Horizontal);
        var config2 = new PanelConfig(Direction: LayoutDirection.Vertical);

        Assert.NotEqual(config1, config2);
    }

    [Fact]
    public void LabelConfig_EqualityWithSameValues_ReturnsTrue()
    {
        var config1 = new LabelConfig(FontSize: 16, HorizontalAlign: TextAlignH.Center);
        var config2 = new LabelConfig(FontSize: 16, HorizontalAlign: TextAlignH.Center);

        Assert.Equal(config1, config2);
    }

    [Fact]
    public void LabelConfig_EqualityWithDifferentValues_ReturnsFalse()
    {
        var config1 = new LabelConfig(HorizontalAlign: TextAlignH.Left);
        var config2 = new LabelConfig(HorizontalAlign: TextAlignH.Right);

        Assert.NotEqual(config1, config2);
    }

    #endregion

    #region With Expression Tests

    [Fact]
    public void ButtonConfig_WithExpression_CreatesNewInstance()
    {
        var original = new ButtonConfig(Width: 100, Height: 40);
        var modified = original with { Width = 200 };

        Assert.Equal(100f, original.Width);
        Assert.Equal(200f, modified.Width);
        Assert.Equal(40f, modified.Height);
    }

    [Fact]
    public void PanelConfig_WithExpression_CreatesNewInstance()
    {
        var original = new PanelConfig(Spacing: 8);
        var modified = original with { Spacing = 16 };

        Assert.Equal(8f, original.Spacing);
        Assert.Equal(16f, modified.Spacing);
    }

    [Fact]
    public void LabelConfig_WithExpression_CreatesNewInstance()
    {
        var original = new LabelConfig(FontSize: 14);
        var modified = original with { FontSize = 18 };

        Assert.Equal(14f, original.FontSize);
        Assert.Equal(18f, modified.FontSize);
    }

    #endregion

    #region Null Handling Tests

    [Fact]
    public void ButtonConfig_NullColors_ReturnsDefaultValues()
    {
        var config = new ButtonConfig(
            BackgroundColor: null,
            BorderColor: null,
            TextColor: null);

        // Should return default colors, not throw
        var bgColor = config.GetBackgroundColor();
        var borderColor = config.GetBorderColor();
        var textColor = config.GetTextColor();

        Assert.Equal(new Vector4(0.3f, 0.5f, 0.8f, 1f), bgColor);
        Assert.Equal(new Vector4(0.4f, 0.6f, 0.9f, 1f), borderColor);
        Assert.Equal(new Vector4(1f, 1f, 1f, 1f), textColor);
    }

    [Fact]
    public void PanelConfig_NullPaddingAndColor_ReturnsDefaultValues()
    {
        var config = new PanelConfig(
            Padding: null,
            BackgroundColor: null);

        // Should return default values, not throw
        var padding = config.GetPadding();
        var bgColor = config.GetBackgroundColor();

        Assert.Equal(UIEdges.Zero, padding);
        Assert.Equal(Vector4.Zero, bgColor);
    }

    [Fact]
    public void LabelConfig_NullColor_ReturnsDefaultValue()
    {
        var config = new LabelConfig(TextColor: null);

        // Should return default white color
        var textColor = config.GetTextColor();

        Assert.Equal(new Vector4(1f, 1f, 1f, 1f), textColor);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ButtonConfig_ZeroValues_Allowed()
    {
        var config = new ButtonConfig(
            Width: 0,
            Height: 0,
            BorderWidth: 0,
            CornerRadius: 0,
            FontSize: 0,
            TabIndex: 0);

        Assert.Equal(0f, config.Width);
        Assert.Equal(0f, config.Height);
        Assert.Equal(0f, config.BorderWidth);
        Assert.Equal(0f, config.CornerRadius);
        Assert.Equal(0f, config.FontSize);
        Assert.Equal(0, config.TabIndex);
    }

    [Fact]
    public void PanelConfig_ZeroSpacing_Allowed()
    {
        var config = new PanelConfig(Spacing: 0);

        Assert.Equal(0f, config.Spacing);
    }

    [Fact]
    public void ButtonConfig_NegativeTabIndex_Allowed()
    {
        var config = new ButtonConfig(TabIndex: -1);

        Assert.Equal(-1, config.TabIndex);
    }

    [Fact]
    public void TextFieldConfig_UnlimitedMaxLength_UsesZero()
    {
        var config = new TextFieldConfig(MaxLength: 0);

        Assert.Equal(0, config.MaxLength);
    }

    [Fact]
    public void PanelConfig_NullDimensions_MeanStretch()
    {
        var config = new PanelConfig(Width: null, Height: null);

        Assert.Null(config.Width);
        Assert.Null(config.Height);
    }

    [Fact]
    public void LabelConfig_NullDimensions_MeanAuto()
    {
        var config = new LabelConfig(Width: null, Height: null);

        Assert.Null(config.Width);
        Assert.Null(config.Height);
    }

    #endregion
}
