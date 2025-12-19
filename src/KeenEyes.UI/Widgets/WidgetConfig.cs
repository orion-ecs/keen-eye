using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Configuration for creating button widgets.
/// </summary>
/// <param name="Width">The button width in pixels.</param>
/// <param name="Height">The button height in pixels.</param>
/// <param name="BackgroundColor">The default background color.</param>
/// <param name="BorderColor">The border color.</param>
/// <param name="BorderWidth">The border width in pixels.</param>
/// <param name="CornerRadius">The corner radius for rounded corners.</param>
/// <param name="TextColor">The button text color.</param>
/// <param name="FontSize">The text font size.</param>
/// <param name="TabIndex">The tab order for keyboard navigation.</param>
public sealed record ButtonConfig(
    float Width = 100,
    float Height = 40,
    Vector4? BackgroundColor = null,
    Vector4? BorderColor = null,
    float BorderWidth = 0,
    float CornerRadius = 4,
    Vector4? TextColor = null,
    float FontSize = 16,
    int TabIndex = 0)
{
    /// <summary>
    /// The default button configuration.
    /// </summary>
    public static ButtonConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.3f, 0.5f, 0.8f, 1f);

    /// <summary>
    /// Gets the border color or default.
    /// </summary>
    internal Vector4 GetBorderColor() =>
        BorderColor ?? new Vector4(0.4f, 0.6f, 0.9f, 1f);

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating panel widgets.
/// </summary>
/// <param name="Width">The panel width in pixels (null for stretch).</param>
/// <param name="Height">The panel height in pixels (null for stretch).</param>
/// <param name="Direction">The layout direction for children.</param>
/// <param name="MainAxisAlign">Alignment along the main axis.</param>
/// <param name="CrossAxisAlign">Alignment along the cross axis.</param>
/// <param name="Spacing">Space between children in pixels.</param>
/// <param name="Padding">Internal padding.</param>
/// <param name="BackgroundColor">The panel background color.</param>
/// <param name="CornerRadius">The corner radius for rounded corners.</param>
public sealed record PanelConfig(
    float? Width = null,
    float? Height = null,
    LayoutDirection Direction = LayoutDirection.Vertical,
    LayoutAlign MainAxisAlign = LayoutAlign.Start,
    LayoutAlign CrossAxisAlign = LayoutAlign.Start,
    float Spacing = 8,
    UIEdges? Padding = null,
    Vector4? BackgroundColor = null,
    float CornerRadius = 0)
{
    /// <summary>
    /// The default panel configuration.
    /// </summary>
    public static PanelConfig Default { get; } = new();

    /// <summary>
    /// Gets the padding or default.
    /// </summary>
    internal UIEdges GetPadding() =>
        Padding ?? UIEdges.Zero;

    /// <summary>
    /// Gets the background color or default (transparent).
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? Vector4.Zero;
}

/// <summary>
/// Configuration for creating label widgets.
/// </summary>
/// <param name="Width">The label width in pixels (null for auto).</param>
/// <param name="Height">The label height in pixels (null for auto).</param>
/// <param name="TextColor">The text color.</param>
/// <param name="FontSize">The text font size.</param>
/// <param name="HorizontalAlign">Horizontal text alignment.</param>
/// <param name="VerticalAlign">Vertical text alignment.</param>
public sealed record LabelConfig(
    float? Width = null,
    float? Height = null,
    Vector4? TextColor = null,
    float FontSize = 14,
    TextAlignH HorizontalAlign = TextAlignH.Left,
    TextAlignV VerticalAlign = TextAlignV.Middle)
{
    /// <summary>
    /// The default label configuration.
    /// </summary>
    public static LabelConfig Default { get; } = new();

    /// <summary>
    /// Gets the text color or default (white).
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating text field widgets.
/// </summary>
/// <param name="Width">The text field width in pixels.</param>
/// <param name="Height">The text field height in pixels.</param>
/// <param name="PlaceholderText">The placeholder text when empty.</param>
/// <param name="MaxLength">Maximum characters allowed (0 for unlimited).</param>
/// <param name="BackgroundColor">The background color.</param>
/// <param name="BorderColor">The border color.</param>
/// <param name="BorderWidth">The border width in pixels.</param>
/// <param name="TextColor">The input text color.</param>
/// <param name="PlaceholderColor">The placeholder text color.</param>
/// <param name="FontSize">The text font size.</param>
/// <param name="TabIndex">The tab order for keyboard navigation.</param>
public sealed record TextFieldConfig(
    float Width = 200,
    float Height = 32,
    string PlaceholderText = "",
    int MaxLength = 0,
    Vector4? BackgroundColor = null,
    Vector4? BorderColor = null,
    float BorderWidth = 1,
    Vector4? TextColor = null,
    Vector4? PlaceholderColor = null,
    float FontSize = 14,
    int TabIndex = 0)
{
    /// <summary>
    /// The default text field configuration.
    /// </summary>
    public static TextFieldConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.1f, 0.1f, 0.15f, 1f);

    /// <summary>
    /// Gets the border color or default.
    /// </summary>
    internal Vector4 GetBorderColor() =>
        BorderColor ?? new Vector4(0.3f, 0.3f, 0.4f, 1f);

    /// <summary>
    /// Gets the text color or default (white).
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);

    /// <summary>
    /// Gets the placeholder color or default (gray).
    /// </summary>
    internal Vector4 GetPlaceholderColor() =>
        PlaceholderColor ?? new Vector4(0.5f, 0.5f, 0.5f, 1f);
}

/// <summary>
/// Configuration for creating checkbox widgets.
/// </summary>
/// <param name="IsChecked">The initial checked state.</param>
/// <param name="Size">The checkbox size in pixels.</param>
/// <param name="Spacing">Space between checkbox and label.</param>
/// <param name="BackgroundColor">The checkbox background color.</param>
/// <param name="CheckColor">The check mark color.</param>
/// <param name="BorderColor">The border color.</param>
/// <param name="BorderWidth">The border width in pixels.</param>
/// <param name="TextColor">The label text color.</param>
/// <param name="FontSize">The label font size.</param>
/// <param name="TabIndex">The tab order for keyboard navigation.</param>
public sealed record CheckboxConfig(
    bool IsChecked = false,
    float Size = 20,
    float Spacing = 8,
    Vector4? BackgroundColor = null,
    Vector4? CheckColor = null,
    Vector4? BorderColor = null,
    float BorderWidth = 1,
    Vector4? TextColor = null,
    float FontSize = 14,
    int TabIndex = 0)
{
    /// <summary>
    /// The default checkbox configuration.
    /// </summary>
    public static CheckboxConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.15f, 0.15f, 0.2f, 1f);

    /// <summary>
    /// Gets the check mark color or default.
    /// </summary>
    internal Vector4 GetCheckColor() =>
        CheckColor ?? new Vector4(0.4f, 0.7f, 1f, 1f);

    /// <summary>
    /// Gets the border color or default.
    /// </summary>
    internal Vector4 GetBorderColor() =>
        BorderColor ?? new Vector4(0.4f, 0.4f, 0.5f, 1f);

    /// <summary>
    /// Gets the text color or default (white).
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating slider widgets.
/// </summary>
/// <param name="Width">The slider width in pixels.</param>
/// <param name="Height">The slider height in pixels.</param>
/// <param name="MinValue">The minimum value.</param>
/// <param name="MaxValue">The maximum value.</param>
/// <param name="Value">The initial value.</param>
/// <param name="TrackColor">The track background color.</param>
/// <param name="FillColor">The filled portion color.</param>
/// <param name="ThumbColor">The thumb/handle color.</param>
/// <param name="ThumbSize">The thumb size in pixels.</param>
/// <param name="TabIndex">The tab order for keyboard navigation.</param>
public sealed record SliderConfig(
    float Width = 200,
    float Height = 24,
    float MinValue = 0,
    float MaxValue = 100,
    float Value = 0,
    Vector4? TrackColor = null,
    Vector4? FillColor = null,
    Vector4? ThumbColor = null,
    float ThumbSize = 16,
    int TabIndex = 0)
{
    /// <summary>
    /// The default slider configuration.
    /// </summary>
    public static SliderConfig Default { get; } = new();

    /// <summary>
    /// Gets the track color or default.
    /// </summary>
    internal Vector4 GetTrackColor() =>
        TrackColor ?? new Vector4(0.2f, 0.2f, 0.25f, 1f);

    /// <summary>
    /// Gets the fill color or default.
    /// </summary>
    internal Vector4 GetFillColor() =>
        FillColor ?? new Vector4(0.3f, 0.5f, 0.8f, 1f);

    /// <summary>
    /// Gets the thumb color or default.
    /// </summary>
    internal Vector4 GetThumbColor() =>
        ThumbColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating progress bar widgets.
/// </summary>
/// <param name="Width">The progress bar width in pixels.</param>
/// <param name="Height">The progress bar height in pixels.</param>
/// <param name="MinValue">The minimum value.</param>
/// <param name="MaxValue">The maximum value.</param>
/// <param name="Value">The initial value.</param>
/// <param name="TrackColor">The track background color.</param>
/// <param name="FillColor">The filled portion color.</param>
/// <param name="CornerRadius">The corner radius for rounded corners.</param>
/// <param name="ShowLabel">Whether to show a percentage label.</param>
/// <param name="LabelColor">The label text color.</param>
/// <param name="FontSize">The label font size.</param>
public sealed record ProgressBarConfig(
    float Width = 200,
    float Height = 20,
    float MinValue = 0,
    float MaxValue = 100,
    float Value = 0,
    Vector4? TrackColor = null,
    Vector4? FillColor = null,
    float CornerRadius = 4,
    bool ShowLabel = false,
    Vector4? LabelColor = null,
    float FontSize = 12)
{
    /// <summary>
    /// The default progress bar configuration.
    /// </summary>
    public static ProgressBarConfig Default { get; } = new();

    /// <summary>
    /// Gets the track color or default.
    /// </summary>
    internal Vector4 GetTrackColor() =>
        TrackColor ?? new Vector4(0.15f, 0.15f, 0.2f, 1f);

    /// <summary>
    /// Gets the fill color or default.
    /// </summary>
    internal Vector4 GetFillColor() =>
        FillColor ?? new Vector4(0.3f, 0.7f, 0.4f, 1f);

    /// <summary>
    /// Gets the label color or default (white).
    /// </summary>
    internal Vector4 GetLabelColor() =>
        LabelColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating toggle/switch widgets.
/// </summary>
/// <param name="IsOn">The initial toggle state.</param>
/// <param name="Width">The toggle width in pixels.</param>
/// <param name="Height">The toggle height in pixels.</param>
/// <param name="Spacing">Space between toggle and label.</param>
/// <param name="TrackOffColor">The track color when off.</param>
/// <param name="TrackOnColor">The track color when on.</param>
/// <param name="ThumbColor">The thumb/knob color.</param>
/// <param name="TextColor">The label text color.</param>
/// <param name="FontSize">The label font size.</param>
/// <param name="TabIndex">The tab order for keyboard navigation.</param>
public sealed record ToggleConfig(
    bool IsOn = false,
    float Width = 48,
    float Height = 24,
    float Spacing = 8,
    Vector4? TrackOffColor = null,
    Vector4? TrackOnColor = null,
    Vector4? ThumbColor = null,
    Vector4? TextColor = null,
    float FontSize = 14,
    int TabIndex = 0)
{
    /// <summary>
    /// The default toggle configuration.
    /// </summary>
    public static ToggleConfig Default { get; } = new();

    /// <summary>
    /// Gets the track off color or default.
    /// </summary>
    internal Vector4 GetTrackOffColor() =>
        TrackOffColor ?? new Vector4(0.3f, 0.3f, 0.35f, 1f);

    /// <summary>
    /// Gets the track on color or default.
    /// </summary>
    internal Vector4 GetTrackOnColor() =>
        TrackOnColor ?? new Vector4(0.3f, 0.7f, 0.4f, 1f);

    /// <summary>
    /// Gets the thumb color or default.
    /// </summary>
    internal Vector4 GetThumbColor() =>
        ThumbColor ?? new Vector4(1f, 1f, 1f, 1f);

    /// <summary>
    /// Gets the text color or default (white).
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating dropdown widgets.
/// </summary>
/// <param name="Width">The dropdown width in pixels.</param>
/// <param name="Height">The dropdown header height in pixels.</param>
/// <param name="MaxDropdownHeight">Maximum height of the dropdown list.</param>
/// <param name="SelectedIndex">The initially selected item index.</param>
/// <param name="BackgroundColor">The header background color.</param>
/// <param name="DropdownColor">The dropdown list background color.</param>
/// <param name="SelectedColor">The selected item highlight color.</param>
/// <param name="HoverColor">The hovered item highlight color.</param>
/// <param name="BorderColor">The border color.</param>
/// <param name="BorderWidth">The border width in pixels.</param>
/// <param name="TextColor">The text color.</param>
/// <param name="FontSize">The text font size.</param>
/// <param name="TabIndex">The tab order for keyboard navigation.</param>
public sealed record DropdownConfig(
    float Width = 200,
    float Height = 32,
    float MaxDropdownHeight = 200,
    int SelectedIndex = 0,
    Vector4? BackgroundColor = null,
    Vector4? DropdownColor = null,
    Vector4? SelectedColor = null,
    Vector4? HoverColor = null,
    Vector4? BorderColor = null,
    float BorderWidth = 1,
    Vector4? TextColor = null,
    float FontSize = 14,
    int TabIndex = 0)
{
    /// <summary>
    /// The default dropdown configuration.
    /// </summary>
    public static DropdownConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.15f, 0.15f, 0.2f, 1f);

    /// <summary>
    /// Gets the dropdown color or default.
    /// </summary>
    internal Vector4 GetDropdownColor() =>
        DropdownColor ?? new Vector4(0.12f, 0.12f, 0.16f, 1f);

    /// <summary>
    /// Gets the selected color or default.
    /// </summary>
    internal Vector4 GetSelectedColor() =>
        SelectedColor ?? new Vector4(0.3f, 0.5f, 0.8f, 1f);

    /// <summary>
    /// Gets the hover color or default.
    /// </summary>
    internal Vector4 GetHoverColor() =>
        HoverColor ?? new Vector4(0.25f, 0.25f, 0.3f, 1f);

    /// <summary>
    /// Gets the border color or default.
    /// </summary>
    internal Vector4 GetBorderColor() =>
        BorderColor ?? new Vector4(0.3f, 0.3f, 0.4f, 1f);

    /// <summary>
    /// Gets the text color or default (white).
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating tab view widgets.
/// </summary>
/// <param name="Width">The tab view width (null for stretch).</param>
/// <param name="Height">The tab view height (null for stretch).</param>
/// <param name="TabBarHeight">The height of the tab bar.</param>
/// <param name="TabSpacing">Space between tabs.</param>
/// <param name="SelectedIndex">The initially selected tab index.</param>
/// <param name="TabBarColor">The tab bar background color.</param>
/// <param name="ContentColor">The content area background color.</param>
/// <param name="TabColor">The inactive tab color.</param>
/// <param name="ActiveTabColor">The active tab color.</param>
/// <param name="TabTextColor">The tab text color.</param>
/// <param name="ActiveTabTextColor">The active tab text color.</param>
/// <param name="FontSize">The tab text font size.</param>
public sealed record TabViewConfig(
    float? Width = null,
    float? Height = null,
    float TabBarHeight = 40,
    float TabSpacing = 2,
    int SelectedIndex = 0,
    Vector4? TabBarColor = null,
    Vector4? ContentColor = null,
    Vector4? TabColor = null,
    Vector4? ActiveTabColor = null,
    Vector4? TabTextColor = null,
    Vector4? ActiveTabTextColor = null,
    float FontSize = 14)
{
    /// <summary>
    /// The default tab view configuration.
    /// </summary>
    public static TabViewConfig Default { get; } = new();

    /// <summary>
    /// Gets the tab bar color or default.
    /// </summary>
    internal Vector4 GetTabBarColor() =>
        TabBarColor ?? new Vector4(0.12f, 0.12f, 0.15f, 1f);

    /// <summary>
    /// Gets the content color or default.
    /// </summary>
    internal Vector4 GetContentColor() =>
        ContentColor ?? new Vector4(0.15f, 0.15f, 0.2f, 1f);

    /// <summary>
    /// Gets the inactive tab color or default.
    /// </summary>
    internal Vector4 GetTabColor() =>
        TabColor ?? new Vector4(0.18f, 0.18f, 0.22f, 1f);

    /// <summary>
    /// Gets the active tab color or default.
    /// </summary>
    internal Vector4 GetActiveTabColor() =>
        ActiveTabColor ?? new Vector4(0.15f, 0.15f, 0.2f, 1f);

    /// <summary>
    /// Gets the tab text color or default.
    /// </summary>
    internal Vector4 GetTabTextColor() =>
        TabTextColor ?? new Vector4(0.7f, 0.7f, 0.7f, 1f);

    /// <summary>
    /// Gets the active tab text color or default.
    /// </summary>
    internal Vector4 GetActiveTabTextColor() =>
        ActiveTabTextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for individual tab items.
/// </summary>
/// <param name="Label">The tab label text.</param>
/// <param name="MinWidth">Minimum tab width in pixels.</param>
/// <param name="Padding">Horizontal padding inside the tab.</param>
public sealed record TabConfig(
    string Label,
    float MinWidth = 80,
    float Padding = 16);

/// <summary>
/// Configuration for creating divider/separator widgets.
/// </summary>
/// <param name="Orientation">Horizontal or vertical divider.</param>
/// <param name="Thickness">The divider thickness in pixels.</param>
/// <param name="Length">The divider length (null for stretch).</param>
/// <param name="Color">The divider color.</param>
/// <param name="Margin">Space around the divider.</param>
public sealed record DividerConfig(
    LayoutDirection Orientation = LayoutDirection.Horizontal,
    float Thickness = 1,
    float? Length = null,
    Vector4? Color = null,
    float Margin = 8)
{
    /// <summary>
    /// The default horizontal divider configuration.
    /// </summary>
    public static DividerConfig Horizontal { get; } = new(LayoutDirection.Horizontal);

    /// <summary>
    /// The default vertical divider configuration.
    /// </summary>
    public static DividerConfig Vertical { get; } = new(LayoutDirection.Vertical);

    /// <summary>
    /// Gets the divider color or default.
    /// </summary>
    internal Vector4 GetColor() =>
        Color ?? new Vector4(0.3f, 0.3f, 0.35f, 1f);
}

/// <summary>
/// Configuration for creating scroll view widgets.
/// </summary>
/// <param name="Width">The scroll view width (null for stretch).</param>
/// <param name="Height">The scroll view height (null for stretch).</param>
/// <param name="ContentWidth">The content width (null for auto).</param>
/// <param name="ContentHeight">The content height (null for auto).</param>
/// <param name="ScrollbarWidth">The scrollbar width in pixels.</param>
/// <param name="BackgroundColor">The viewport background color.</param>
/// <param name="ScrollbarTrackColor">The scrollbar track color.</param>
/// <param name="ScrollbarThumbColor">The scrollbar thumb color.</param>
/// <param name="ShowHorizontalScrollbar">Whether to show horizontal scrollbar.</param>
/// <param name="ShowVerticalScrollbar">Whether to show vertical scrollbar.</param>
public sealed record ScrollViewConfig(
    float? Width = null,
    float? Height = null,
    float? ContentWidth = null,
    float? ContentHeight = null,
    float ScrollbarWidth = 12,
    Vector4? BackgroundColor = null,
    Vector4? ScrollbarTrackColor = null,
    Vector4? ScrollbarThumbColor = null,
    bool ShowHorizontalScrollbar = false,
    bool ShowVerticalScrollbar = true)
{
    /// <summary>
    /// The default scroll view configuration.
    /// </summary>
    public static ScrollViewConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default (transparent).
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? Vector4.Zero;

    /// <summary>
    /// Gets the scrollbar track color or default.
    /// </summary>
    internal Vector4 GetScrollbarTrackColor() =>
        ScrollbarTrackColor ?? new Vector4(0.1f, 0.1f, 0.12f, 1f);

    /// <summary>
    /// Gets the scrollbar thumb color or default.
    /// </summary>
    internal Vector4 GetScrollbarThumbColor() =>
        ScrollbarThumbColor ?? new Vector4(0.35f, 0.35f, 0.4f, 1f);
}

/// <summary>
/// Configuration for creating floating UI window widgets.
/// </summary>
/// <param name="Width">The window width in pixels.</param>
/// <param name="Height">The window height in pixels.</param>
/// <param name="X">Initial X position from top-left.</param>
/// <param name="Y">Initial Y position from top-left.</param>
/// <param name="TitleBarHeight">The title bar height in pixels.</param>
/// <param name="CanDrag">Whether the window can be dragged.</param>
/// <param name="CanResize">Whether the window can be resized.</param>
/// <param name="CanClose">Whether the window has a close button.</param>
/// <param name="CanMinimize">Whether the window can be minimized.</param>
/// <param name="CanMaximize">Whether the window can be maximized.</param>
/// <param name="MinWidth">Minimum width when resizing.</param>
/// <param name="MinHeight">Minimum height when resizing.</param>
/// <param name="TitleBarColor">The title bar background color.</param>
/// <param name="ContentColor">The content area background color.</param>
/// <param name="TitleTextColor">The title text color.</param>
/// <param name="CloseButtonColor">The close button background color.</param>
/// <param name="CloseButtonHoverColor">The close button hover color.</param>
/// <param name="MinimizeButtonColor">The minimize button background color.</param>
/// <param name="MaximizeButtonColor">The maximize button background color.</param>
/// <param name="FontSize">The title text font size.</param>
/// <param name="CornerRadius">The window corner radius.</param>
public sealed record UIWindowConfig(
    float Width = 400,
    float Height = 300,
    float X = 100,
    float Y = 100,
    float TitleBarHeight = 32,
    bool CanDrag = true,
    bool CanResize = false,
    bool CanClose = true,
    bool CanMinimize = false,
    bool CanMaximize = false,
    float MinWidth = 150,
    float MinHeight = 100,
    Vector4? TitleBarColor = null,
    Vector4? ContentColor = null,
    Vector4? TitleTextColor = null,
    Vector4? CloseButtonColor = null,
    Vector4? CloseButtonHoverColor = null,
    Vector4? MinimizeButtonColor = null,
    Vector4? MaximizeButtonColor = null,
    float FontSize = 14,
    float CornerRadius = 6)
{
    /// <summary>
    /// The default window configuration.
    /// </summary>
    public static UIWindowConfig Default { get; } = new();

    /// <summary>
    /// Gets the title bar color or default.
    /// </summary>
    internal Vector4 GetTitleBarColor() =>
        TitleBarColor ?? new Vector4(0.18f, 0.18f, 0.22f, 1f);

    /// <summary>
    /// Gets the content color or default.
    /// </summary>
    internal Vector4 GetContentColor() =>
        ContentColor ?? new Vector4(0.12f, 0.12f, 0.15f, 0.98f);

    /// <summary>
    /// Gets the title text color or default.
    /// </summary>
    internal Vector4 GetTitleTextColor() =>
        TitleTextColor ?? new Vector4(1f, 1f, 1f, 1f);

    /// <summary>
    /// Gets the close button color or default.
    /// </summary>
    internal Vector4 GetCloseButtonColor() =>
        CloseButtonColor ?? new Vector4(0.5f, 0.2f, 0.2f, 0f);

    /// <summary>
    /// Gets the close button hover color or default.
    /// </summary>
    internal Vector4 GetCloseButtonHoverColor() =>
        CloseButtonHoverColor ?? new Vector4(0.8f, 0.3f, 0.3f, 1f);

    /// <summary>
    /// Gets the minimize button color or default.
    /// </summary>
    internal Vector4 GetMinimizeButtonColor() =>
        MinimizeButtonColor ?? new Vector4(0.3f, 0.4f, 0.5f, 0f);

    /// <summary>
    /// Gets the maximize button color or default.
    /// </summary>
    internal Vector4 GetMaximizeButtonColor() =>
        MaximizeButtonColor ?? new Vector4(0.3f, 0.5f, 0.3f, 0f);
}

/// <summary>
/// Configuration for creating a splitter widget.
/// </summary>
/// <param name="Orientation">The split direction. Horizontal means panes side by side.</param>
/// <param name="InitialRatio">Initial ratio for the first pane (0.0-1.0).</param>
/// <param name="HandleSize">Size of the draggable handle in pixels.</param>
/// <param name="MinFirstPane">Minimum size of the first pane in pixels.</param>
/// <param name="MinSecondPane">Minimum size of the second pane in pixels.</param>
/// <param name="HandleColor">Color of the splitter handle.</param>
/// <param name="HandleHoverColor">Color of the handle when hovered.</param>
/// <param name="FirstPaneColor">Background color of the first pane (null for transparent).</param>
/// <param name="SecondPaneColor">Background color of the second pane (null for transparent).</param>
public sealed record SplitterConfig(
    LayoutDirection Orientation = LayoutDirection.Horizontal,
    float InitialRatio = 0.5f,
    float HandleSize = 4,
    float MinFirstPane = 100,
    float MinSecondPane = 100,
    Vector4? HandleColor = null,
    Vector4? HandleHoverColor = null,
    Vector4? FirstPaneColor = null,
    Vector4? SecondPaneColor = null)
{
    /// <summary>
    /// The default splitter configuration.
    /// </summary>
    public static SplitterConfig Default { get; } = new();

    /// <summary>
    /// Gets the handle color or default.
    /// </summary>
    internal Vector4 GetHandleColor() =>
        HandleColor ?? new Vector4(0.3f, 0.3f, 0.35f, 1f);

    /// <summary>
    /// Gets the handle hover color or default.
    /// </summary>
    internal Vector4 GetHandleHoverColor() =>
        HandleHoverColor ?? new Vector4(0.4f, 0.4f, 0.5f, 1f);
}

/// <summary>
/// Configuration for a tooltip.
/// </summary>
/// <param name="Delay">Delay in seconds before showing (default 0.5s).</param>
/// <param name="MaxWidth">Maximum width for text wrapping.</param>
/// <param name="Position">Preferred tooltip position.</param>
/// <param name="FollowMouse">Whether tooltip follows cursor.</param>
/// <param name="BackgroundColor">Tooltip background color.</param>
/// <param name="TextColor">Tooltip text color.</param>
/// <param name="FontSize">Font size for tooltip text.</param>
/// <param name="CornerRadius">Corner radius for tooltip background.</param>
public sealed record TooltipConfig(
    float Delay = 0.5f,
    float MaxWidth = 300,
    TooltipPosition Position = TooltipPosition.Auto,
    bool FollowMouse = false,
    Vector4? BackgroundColor = null,
    Vector4? TextColor = null,
    float FontSize = 13,
    float CornerRadius = 4)
{
    /// <summary>
    /// The default tooltip configuration.
    /// </summary>
    public static TooltipConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.1f, 0.1f, 0.12f, 0.95f);

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(0.9f, 0.9f, 0.9f, 1f);
}

/// <summary>
/// Configuration for a popover.
/// </summary>
/// <param name="Trigger">What triggers the popover to open.</param>
/// <param name="Position">Preferred position relative to trigger.</param>
/// <param name="CloseOnClickOutside">Whether clicking outside closes it.</param>
/// <param name="Width">Popover width.</param>
/// <param name="Height">Popover height.</param>
/// <param name="BackgroundColor">Popover background color.</param>
/// <param name="CornerRadius">Corner radius.</param>
public sealed record PopoverConfig(
    PopoverTrigger Trigger = PopoverTrigger.Click,
    TooltipPosition Position = TooltipPosition.Below,
    bool CloseOnClickOutside = true,
    float Width = 250,
    float Height = 150,
    Vector4? BackgroundColor = null,
    float CornerRadius = 6)
{
    /// <summary>
    /// The default popover configuration.
    /// </summary>
    public static PopoverConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.15f, 0.15f, 0.18f, 0.98f);
}

/// <summary>
/// Configuration for a menu bar.
/// </summary>
/// <param name="Height">Height of the menu bar.</param>
/// <param name="BackgroundColor">Background color.</param>
/// <param name="ItemColor">Normal item background color.</param>
/// <param name="ItemHoverColor">Item background when hovered.</param>
/// <param name="TextColor">Text color.</param>
/// <param name="FontSize">Font size.</param>
/// <param name="ItemPadding">Horizontal padding for each item.</param>
public sealed record MenuBarConfig(
    float Height = 28,
    Vector4? BackgroundColor = null,
    Vector4? ItemColor = null,
    Vector4? ItemHoverColor = null,
    Vector4? TextColor = null,
    float FontSize = 14,
    float ItemPadding = 12)
{
    /// <summary>
    /// The default menu bar configuration.
    /// </summary>
    public static MenuBarConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.18f, 0.18f, 0.22f, 1f);

    /// <summary>
    /// Gets the item hover color or default.
    /// </summary>
    internal Vector4 GetItemHoverColor() =>
        ItemHoverColor ?? new Vector4(0.25f, 0.25f, 0.3f, 1f);

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(0.9f, 0.9f, 0.9f, 1f);
}

/// <summary>
/// Configuration for a dropdown or context menu.
/// </summary>
/// <param name="MinWidth">Minimum menu width.</param>
/// <param name="MaxWidth">Maximum menu width (0 for no limit).</param>
/// <param name="ItemHeight">Height of each menu item.</param>
/// <param name="BackgroundColor">Menu background color.</param>
/// <param name="ItemHoverColor">Item background when hovered.</param>
/// <param name="TextColor">Normal text color.</param>
/// <param name="DisabledTextColor">Disabled item text color.</param>
/// <param name="ShortcutColor">Keyboard shortcut text color.</param>
/// <param name="SeparatorColor">Separator line color.</param>
/// <param name="CheckmarkColor">Checkmark color for toggle items.</param>
/// <param name="FontSize">Font size.</param>
/// <param name="CornerRadius">Corner radius.</param>
public sealed record MenuConfig(
    float MinWidth = 180,
    float MaxWidth = 0,
    float ItemHeight = 26,
    Vector4? BackgroundColor = null,
    Vector4? ItemHoverColor = null,
    Vector4? TextColor = null,
    Vector4? DisabledTextColor = null,
    Vector4? ShortcutColor = null,
    Vector4? SeparatorColor = null,
    Vector4? CheckmarkColor = null,
    float FontSize = 13,
    float CornerRadius = 4)
{
    /// <summary>
    /// The default menu configuration.
    /// </summary>
    public static MenuConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.15f, 0.15f, 0.18f, 0.98f);

    /// <summary>
    /// Gets the item hover color or default.
    /// </summary>
    internal Vector4 GetItemHoverColor() =>
        ItemHoverColor ?? new Vector4(0.25f, 0.4f, 0.7f, 1f);

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>
    /// Gets the disabled text color or default.
    /// </summary>
    internal Vector4 GetDisabledTextColor() =>
        DisabledTextColor ?? new Vector4(0.5f, 0.5f, 0.5f, 1f);

    /// <summary>
    /// Gets the shortcut color or default.
    /// </summary>
    internal Vector4 GetShortcutColor() =>
        ShortcutColor ?? new Vector4(0.6f, 0.6f, 0.6f, 1f);

    /// <summary>
    /// Gets the separator color or default.
    /// </summary>
    internal Vector4 GetSeparatorColor() =>
        SeparatorColor ?? new Vector4(0.3f, 0.3f, 0.35f, 1f);
}

/// <summary>
/// Definition of a menu item for factory methods.
/// </summary>
/// <param name="Label">Display label for the item.</param>
/// <param name="ItemId">Unique identifier for event handling.</param>
/// <param name="Shortcut">Optional keyboard shortcut display text.</param>
/// <param name="IsEnabled">Whether the item is enabled.</param>
/// <param name="IsSeparator">Whether this is a separator line.</param>
/// <param name="IsToggle">Whether this is a toggle item.</param>
/// <param name="IsChecked">Initial checked state for toggle items.</param>
/// <param name="SubmenuItems">Items for a submenu (makes this a submenu parent).</param>
public sealed record MenuItemDef(
    string Label,
    string? ItemId = null,
    string? Shortcut = null,
    bool IsEnabled = true,
    bool IsSeparator = false,
    bool IsToggle = false,
    bool IsChecked = false,
    IEnumerable<MenuItemDef>? SubmenuItems = null)
{
    /// <summary>
    /// Creates a separator item.
    /// </summary>
    public static MenuItemDef Separator() => new("", IsSeparator: true);
}

/// <summary>
/// Configuration for a radial (pie) menu.
/// </summary>
/// <param name="InnerRadius">Inner radius (dead zone).</param>
/// <param name="OuterRadius">Outer radius of the menu.</param>
/// <param name="BackgroundColor">Background color of slices.</param>
/// <param name="SelectedColor">Color when slice is selected.</param>
/// <param name="DisabledColor">Color for disabled slices.</param>
/// <param name="TextColor">Text color for labels.</param>
/// <param name="IconSize">Size for slice icons.</param>
/// <param name="FontSize">Font size for labels.</param>
/// <param name="ShowLabels">Whether to show text labels on slices.</param>
/// <param name="StartAngle">Starting angle in radians (default is top).</param>
public sealed record RadialMenuConfig(
    float InnerRadius = 40f,
    float OuterRadius = 120f,
    Vector4? BackgroundColor = null,
    Vector4? SelectedColor = null,
    Vector4? DisabledColor = null,
    Vector4? TextColor = null,
    float IconSize = 32f,
    float FontSize = 12f,
    bool ShowLabels = true,
    float StartAngle = -MathF.PI / 2)
{
    /// <summary>
    /// The default radial menu configuration.
    /// </summary>
    public static RadialMenuConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.2f, 0.2f, 0.25f, 0.9f);

    /// <summary>
    /// Gets the selected color or default.
    /// </summary>
    internal Vector4 GetSelectedColor() =>
        SelectedColor ?? new Vector4(0.3f, 0.5f, 0.8f, 0.95f);

    /// <summary>
    /// Gets the disabled color or default.
    /// </summary>
    internal Vector4 GetDisabledColor() =>
        DisabledColor ?? new Vector4(0.15f, 0.15f, 0.15f, 0.7f);

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Definition of a radial menu slice for factory methods.
/// </summary>
/// <param name="Label">Display label for the slice.</param>
/// <param name="ItemId">Unique identifier for event handling.</param>
/// <param name="IsEnabled">Whether the slice is enabled.</param>
/// <param name="SubSlices">Slices for a submenu (makes this open a nested radial menu).</param>
public sealed record RadialSliceDef(
    string Label,
    string? ItemId = null,
    bool IsEnabled = true,
    IEnumerable<RadialSliceDef>? SubSlices = null);

/// <summary>
/// Configuration for a dock container.
/// </summary>
/// <param name="LeftZoneSize">Initial width of the left dock zone.</param>
/// <param name="RightZoneSize">Initial width of the right dock zone.</param>
/// <param name="TopZoneSize">Initial height of the top dock zone.</param>
/// <param name="BottomZoneSize">Initial height of the bottom dock zone.</param>
/// <param name="MinZoneSize">Minimum size for any dock zone.</param>
/// <param name="SplitterSize">Size of splitters between zones.</param>
/// <param name="ShowLeftZone">Whether the left zone is initially visible.</param>
/// <param name="ShowRightZone">Whether the right zone is initially visible.</param>
/// <param name="ShowTopZone">Whether the top zone is initially visible.</param>
/// <param name="ShowBottomZone">Whether the bottom zone is initially visible.</param>
/// <param name="ZoneBackgroundColor">Background color for dock zones.</param>
/// <param name="SplitterColor">Color of the zone splitters.</param>
/// <param name="PreviewColor">Color for the dock preview overlay.</param>
public sealed record DockContainerConfig(
    float LeftZoneSize = 200f,
    float RightZoneSize = 200f,
    float TopZoneSize = 150f,
    float BottomZoneSize = 150f,
    float MinZoneSize = 100f,
    float SplitterSize = 4f,
    bool ShowLeftZone = true,
    bool ShowRightZone = true,
    bool ShowTopZone = false,
    bool ShowBottomZone = false,
    Vector4? ZoneBackgroundColor = null,
    Vector4? SplitterColor = null,
    Vector4? PreviewColor = null)
{
    /// <summary>
    /// The default dock container configuration.
    /// </summary>
    public static DockContainerConfig Default { get; } = new();

    /// <summary>
    /// Gets the zone background color or default.
    /// </summary>
    internal Vector4 GetZoneBackgroundColor() =>
        ZoneBackgroundColor ?? new Vector4(0.12f, 0.12f, 0.15f, 1f);

    /// <summary>
    /// Gets the splitter color or default.
    /// </summary>
    internal Vector4 GetSplitterColor() =>
        SplitterColor ?? new Vector4(0.08f, 0.08f, 0.1f, 1f);

    /// <summary>
    /// Gets the preview color or default.
    /// </summary>
    internal Vector4 GetPreviewColor() =>
        PreviewColor ?? new Vector4(0.3f, 0.5f, 0.8f, 0.4f);
}

/// <summary>
/// Configuration for a dockable panel.
/// </summary>
/// <param name="Width">Initial floating width.</param>
/// <param name="Height">Initial floating height.</param>
/// <param name="CanClose">Whether the panel has a close button.</param>
/// <param name="CanFloat">Whether the panel can be undocked/floated.</param>
/// <param name="CanDock">Whether the panel can be docked.</param>
/// <param name="AllowedZones">Which dock zones this panel can dock to.</param>
/// <param name="TitleBarHeight">Height of the panel title bar.</param>
/// <param name="TitleBarColor">Title bar background color.</param>
/// <param name="ContentColor">Content area background color.</param>
/// <param name="TitleTextColor">Title text color.</param>
/// <param name="TabColor">Tab background color when in a tab group.</param>
/// <param name="ActiveTabColor">Active tab background color.</param>
/// <param name="FontSize">Font size for title text.</param>
public sealed record DockPanelConfig(
    float Width = 300f,
    float Height = 200f,
    bool CanClose = true,
    bool CanFloat = true,
    bool CanDock = true,
    DockZone AllowedZones = DockZone.All,
    float TitleBarHeight = 28f,
    Vector4? TitleBarColor = null,
    Vector4? ContentColor = null,
    Vector4? TitleTextColor = null,
    Vector4? TabColor = null,
    Vector4? ActiveTabColor = null,
    float FontSize = 13f)
{
    /// <summary>
    /// The default dock panel configuration.
    /// </summary>
    public static DockPanelConfig Default { get; } = new();

    /// <summary>
    /// Gets the title bar color or default.
    /// </summary>
    internal Vector4 GetTitleBarColor() =>
        TitleBarColor ?? new Vector4(0.18f, 0.18f, 0.22f, 1f);

    /// <summary>
    /// Gets the content color or default.
    /// </summary>
    internal Vector4 GetContentColor() =>
        ContentColor ?? new Vector4(0.15f, 0.15f, 0.18f, 1f);

    /// <summary>
    /// Gets the title text color or default.
    /// </summary>
    internal Vector4 GetTitleTextColor() =>
        TitleTextColor ?? new Vector4(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>
    /// Gets the tab color or default.
    /// </summary>
    internal Vector4 GetTabColor() =>
        TabColor ?? new Vector4(0.15f, 0.15f, 0.18f, 1f);

    /// <summary>
    /// Gets the active tab color or default.
    /// </summary>
    internal Vector4 GetActiveTabColor() =>
        ActiveTabColor ?? new Vector4(0.2f, 0.2f, 0.25f, 1f);
}

#region Phase 6: Toolbar and StatusBar

/// <summary>
/// Definition for a toolbar button.
/// </summary>
/// <param name="Icon">Optional icon texture for the button.</param>
/// <param name="Tooltip">Tooltip text shown on hover.</param>
/// <param name="IsToggle">Whether this button acts as a toggle.</param>
/// <param name="IsEnabled">Whether the button is enabled.</param>
/// <param name="GroupId">Optional group ID for radio-button behavior.</param>
public sealed record ToolbarButtonDef(
    TextureHandle Icon = default,
    string Tooltip = "",
    bool IsToggle = false,
    bool IsEnabled = true,
    string? GroupId = null);

/// <summary>
/// Definition for a toolbar separator.
/// </summary>
public sealed record ToolbarSeparatorDef;

/// <summary>
/// Union type for toolbar items (button or separator).
/// </summary>
public abstract record ToolbarItemDef
{
    /// <summary>
    /// Creates a button item.
    /// </summary>
    public sealed record Button(ToolbarButtonDef Definition) : ToolbarItemDef;

    /// <summary>
    /// Creates a separator item.
    /// </summary>
    public sealed record Separator() : ToolbarItemDef;
}

/// <summary>
/// Configuration for creating toolbar widgets.
/// </summary>
/// <param name="Orientation">The toolbar layout direction.</param>
/// <param name="ButtonSize">Size of toolbar buttons in pixels.</param>
/// <param name="Spacing">Space between buttons in pixels.</param>
/// <param name="Padding">Padding around the toolbar content.</param>
/// <param name="BackgroundColor">The toolbar background color.</param>
/// <param name="ButtonColor">Default button background color.</param>
/// <param name="ButtonHoverColor">Button color when hovered.</param>
/// <param name="ButtonPressedColor">Button color when pressed/toggled.</param>
/// <param name="SeparatorColor">Color of separator lines.</param>
/// <param name="SeparatorWidth">Width of separator lines in pixels.</param>
public sealed record ToolbarConfig(
    LayoutDirection Orientation = LayoutDirection.Horizontal,
    float ButtonSize = 32f,
    float Spacing = 2f,
    UIEdges? Padding = null,
    Vector4? BackgroundColor = null,
    Vector4? ButtonColor = null,
    Vector4? ButtonHoverColor = null,
    Vector4? ButtonPressedColor = null,
    Vector4? SeparatorColor = null,
    float SeparatorWidth = 1f)
{
    /// <summary>
    /// The default toolbar configuration.
    /// </summary>
    public static ToolbarConfig Default { get; } = new();

    /// <summary>
    /// Gets the padding or default.
    /// </summary>
    internal UIEdges GetPadding() =>
        Padding ?? new UIEdges(4, 4, 4, 4);

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.2f, 0.2f, 0.22f, 1f);

    /// <summary>
    /// Gets the button color or default.
    /// </summary>
    internal Vector4 GetButtonColor() =>
        ButtonColor ?? new Vector4(0.25f, 0.25f, 0.28f, 1f);

    /// <summary>
    /// Gets the button hover color or default.
    /// </summary>
    internal Vector4 GetButtonHoverColor() =>
        ButtonHoverColor ?? new Vector4(0.35f, 0.35f, 0.38f, 1f);

    /// <summary>
    /// Gets the button pressed color or default.
    /// </summary>
    internal Vector4 GetButtonPressedColor() =>
        ButtonPressedColor ?? new Vector4(0.4f, 0.5f, 0.7f, 1f);

    /// <summary>
    /// Gets the separator color or default.
    /// </summary>
    internal Vector4 GetSeparatorColor() =>
        SeparatorColor ?? new Vector4(0.4f, 0.4f, 0.45f, 0.5f);
}

/// <summary>
/// Definition for a status bar section.
/// </summary>
/// <param name="InitialText">Initial text content of the section.</param>
/// <param name="Width">Fixed width in pixels (0 for flexible).</param>
/// <param name="IsFlexible">Whether the section expands to fill space.</param>
/// <param name="MinWidth">Minimum width for flexible sections.</param>
/// <param name="TextAlign">Text alignment within the section.</param>
public sealed record StatusBarSectionDef(
    string InitialText = "",
    float Width = 0f,
    bool IsFlexible = false,
    float MinWidth = 50f,
    TextAlignH TextAlign = TextAlignH.Left);

/// <summary>
/// Configuration for creating status bar widgets.
/// </summary>
/// <param name="Height">Height of the status bar in pixels.</param>
/// <param name="Padding">Padding around the status bar content.</param>
/// <param name="BackgroundColor">The status bar background color.</param>
/// <param name="TextColor">Default text color for sections.</param>
/// <param name="SeparatorColor">Color of separator lines between sections.</param>
/// <param name="FontSize">Font size for status text.</param>
public sealed record StatusBarConfig(
    float Height = 24f,
    UIEdges? Padding = null,
    Vector4? BackgroundColor = null,
    Vector4? TextColor = null,
    Vector4? SeparatorColor = null,
    float FontSize = 12f)
{
    /// <summary>
    /// The default status bar configuration.
    /// </summary>
    public static StatusBarConfig Default { get; } = new();

    /// <summary>
    /// Gets the padding or default.
    /// </summary>
    internal UIEdges GetPadding() =>
        Padding ?? new UIEdges(4, 2, 4, 2);

    /// <summary>
    /// Gets the background color or default.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.15f, 0.15f, 0.18f, 1f);

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>
    /// Gets the separator color or default.
    /// </summary>
    internal Vector4 GetSeparatorColor() =>
        SeparatorColor ?? new Vector4(0.3f, 0.3f, 0.35f, 0.5f);
}

#endregion

#region Phase 7: TreeView

/// <summary>
/// Configuration for creating tree view widgets.
/// </summary>
/// <param name="Width">The tree view width (null for stretch).</param>
/// <param name="Height">The tree view height (null for stretch).</param>
/// <param name="IndentSize">Indentation per depth level in pixels.</param>
/// <param name="RowHeight">Height of each tree node row.</param>
/// <param name="ShowLines">Whether to show connecting lines between nodes.</param>
/// <param name="AllowMultiSelect">Whether multiple nodes can be selected.</param>
/// <param name="BackgroundColor">The tree view background color.</param>
/// <param name="SelectedColor">Background color for selected nodes.</param>
/// <param name="HoverColor">Background color for hovered nodes.</param>
/// <param name="TextColor">Color for node labels.</param>
/// <param name="LineColor">Color for connecting lines (if shown).</param>
/// <param name="ExpandArrowColor">Color for expand/collapse arrows.</param>
/// <param name="FontSize">Font size for node labels.</param>
public sealed record TreeViewConfig(
    float? Width = null,
    float? Height = null,
    float IndentSize = 20f,
    float RowHeight = 24f,
    bool ShowLines = false,
    bool AllowMultiSelect = false,
    Vector4? BackgroundColor = null,
    Vector4? SelectedColor = null,
    Vector4? HoverColor = null,
    Vector4? TextColor = null,
    Vector4? LineColor = null,
    Vector4? ExpandArrowColor = null,
    float FontSize = 13f)
{
    /// <summary>
    /// The default tree view configuration.
    /// </summary>
    public static TreeViewConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default (transparent).
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? Vector4.Zero;

    /// <summary>
    /// Gets the selected color or default.
    /// </summary>
    internal Vector4 GetSelectedColor() =>
        SelectedColor ?? new Vector4(0.3f, 0.5f, 0.7f, 1f);

    /// <summary>
    /// Gets the hover color or default.
    /// </summary>
    internal Vector4 GetHoverColor() =>
        HoverColor ?? new Vector4(0.25f, 0.25f, 0.3f, 0.5f);

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>
    /// Gets the line color or default.
    /// </summary>
    internal Vector4 GetLineColor() =>
        LineColor ?? new Vector4(0.4f, 0.4f, 0.45f, 0.5f);

    /// <summary>
    /// Gets the expand arrow color or default.
    /// </summary>
    internal Vector4 GetExpandArrowColor() =>
        ExpandArrowColor ?? new Vector4(0.7f, 0.7f, 0.7f, 1f);
}

/// <summary>
/// Configuration for individual tree nodes.
/// </summary>
/// <param name="IsExpanded">Initial expanded state for nodes with children.</param>
/// <param name="Icon">Optional icon to display next to the label.</param>
/// <param name="IconSize">Size of the icon in pixels.</param>
public sealed record TreeNodeConfig(
    bool IsExpanded = false,
    TextureHandle Icon = default,
    float IconSize = 16f);

/// <summary>
/// Definition for creating a tree node.
/// </summary>
/// <param name="Label">The display label for the node.</param>
/// <param name="Children">Optional child node definitions.</param>
/// <param name="IsExpanded">Initial expanded state.</param>
/// <param name="Icon">Optional icon for the node.</param>
/// <param name="UserData">Optional user data associated with the node.</param>
public sealed record TreeNodeDef(
    string Label,
    IEnumerable<TreeNodeDef>? Children = null,
    bool IsExpanded = false,
    TextureHandle Icon = default,
    object? UserData = null);

#endregion

#region Phase 8: PropertyGrid

/// <summary>
/// Configuration for creating property grid widgets.
/// </summary>
/// <param name="Width">The property grid width (null for stretch).</param>
/// <param name="Height">The property grid height (null for stretch).</param>
/// <param name="LabelWidthRatio">Ratio of width for property labels (0.0-1.0).</param>
/// <param name="RowHeight">Height of each property row.</param>
/// <param name="CategoryHeight">Height of category headers.</param>
/// <param name="ShowCategories">Whether to group properties into collapsible categories.</param>
/// <param name="BackgroundColor">The property grid background color.</param>
/// <param name="RowAlternateColor">Alternate row background color for striping.</param>
/// <param name="CategoryColor">Category header background color.</param>
/// <param name="LabelColor">Color for property labels.</param>
/// <param name="ValueColor">Color for property values.</param>
/// <param name="SeparatorColor">Color of separator lines.</param>
/// <param name="FontSize">Font size for labels and values.</param>
/// <param name="CategoryFontSize">Font size for category headers.</param>
public sealed record PropertyGridConfig(
    float? Width = null,
    float? Height = null,
    float LabelWidthRatio = 0.4f,
    float RowHeight = 24f,
    float CategoryHeight = 28f,
    bool ShowCategories = true,
    Vector4? BackgroundColor = null,
    Vector4? RowAlternateColor = null,
    Vector4? CategoryColor = null,
    Vector4? LabelColor = null,
    Vector4? ValueColor = null,
    Vector4? SeparatorColor = null,
    float FontSize = 13f,
    float CategoryFontSize = 13f)
{
    /// <summary>
    /// The default property grid configuration.
    /// </summary>
    public static PropertyGridConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default (transparent).
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? Vector4.Zero;

    /// <summary>
    /// Gets the row alternate color or default.
    /// </summary>
    internal Vector4 GetRowAlternateColor() =>
        RowAlternateColor ?? new Vector4(0.15f, 0.15f, 0.18f, 0.3f);

    /// <summary>
    /// Gets the category color or default.
    /// </summary>
    internal Vector4 GetCategoryColor() =>
        CategoryColor ?? new Vector4(0.2f, 0.2f, 0.24f, 1f);

    /// <summary>
    /// Gets the label color or default.
    /// </summary>
    internal Vector4 GetLabelColor() =>
        LabelColor ?? new Vector4(0.7f, 0.7f, 0.7f, 1f);

    /// <summary>
    /// Gets the value color or default.
    /// </summary>
    internal Vector4 GetValueColor() =>
        ValueColor ?? new Vector4(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>
    /// Gets the separator color or default.
    /// </summary>
    internal Vector4 GetSeparatorColor() =>
        SeparatorColor ?? new Vector4(0.3f, 0.3f, 0.35f, 0.5f);
}

/// <summary>
/// Definition for a property in a property grid.
/// </summary>
/// <param name="Name">The unique property identifier.</param>
/// <param name="Label">The display label (defaults to Name if null).</param>
/// <param name="Type">The type of property editor to use.</param>
/// <param name="Category">Optional category name for grouping.</param>
/// <param name="InitialValue">The initial value for the property.</param>
/// <param name="IsReadOnly">Whether the property is read-only.</param>
/// <param name="EnumValues">For Enum type, the available string values.</param>
/// <param name="MinValue">For numeric types, the minimum value.</param>
/// <param name="MaxValue">For numeric types, the maximum value.</param>
public sealed record PropertyDef(
    string Name,
    string? Label = null,
    PropertyType Type = PropertyType.String,
    string? Category = null,
    object? InitialValue = null,
    bool IsReadOnly = false,
    IEnumerable<string>? EnumValues = null,
    float? MinValue = null,
    float? MaxValue = null);

/// <summary>
/// Definition for a property category in a property grid.
/// </summary>
/// <param name="Name">The category display name.</param>
/// <param name="IsExpanded">Initial expanded state.</param>
/// <param name="Properties">Properties in this category.</param>
public sealed record PropertyCategoryDef(
    string Name,
    bool IsExpanded = true,
    IEnumerable<PropertyDef>? Properties = null);

#endregion

#region Phase 5: Image, Card, Badge, Avatar

/// <summary>
/// Configuration for creating image widgets.
/// </summary>
/// <param name="Width">The image width in pixels.</param>
/// <param name="Height">The image height in pixels.</param>
/// <param name="Tint">Color tint applied to the image.</param>
/// <param name="ScaleMode">How the image is scaled to fit.</param>
/// <param name="PreserveAspect">Whether to preserve aspect ratio.</param>
/// <param name="SourceRect">Source rectangle for sprite atlas (empty for full texture).</param>
public sealed record ImageConfig(
    float Width = 100,
    float Height = 100,
    Vector4? Tint = null,
    ImageScaleMode ScaleMode = ImageScaleMode.ScaleToFit,
    bool PreserveAspect = true,
    Rectangle? SourceRect = null)
{
    /// <summary>
    /// The default image configuration.
    /// </summary>
    public static ImageConfig Default { get; } = new();

    /// <summary>
    /// Gets the tint color or default (white, no tint).
    /// </summary>
    internal Vector4 GetTint() =>
        Tint ?? Vector4.One;

    /// <summary>
    /// Gets the source rectangle or default (empty, full texture).
    /// </summary>
    internal Rectangle GetSourceRect() =>
        SourceRect ?? Rectangle.Empty;
}

/// <summary>
/// Configuration for creating card widgets.
/// </summary>
/// <param name="Width">The card width in pixels.</param>
/// <param name="TitleHeight">Height of the title bar in pixels.</param>
/// <param name="TitleBarColor">The title bar background color.</param>
/// <param name="ContentColor">The content area background color.</param>
/// <param name="BorderColor">The card border color.</param>
/// <param name="BorderWidth">The border width in pixels.</param>
/// <param name="CornerRadius">The corner radius for rounded corners.</param>
public sealed record CardConfig(
    float Width = 300,
    float TitleHeight = 40,
    Vector4? TitleBarColor = null,
    Vector4? ContentColor = null,
    Vector4? BorderColor = null,
    float BorderWidth = 1,
    float CornerRadius = 8)
{
    /// <summary>
    /// The default card configuration.
    /// </summary>
    public static CardConfig Default { get; } = new();

    /// <summary>
    /// Gets the title bar color or default.
    /// </summary>
    internal Vector4 GetTitleBarColor() =>
        TitleBarColor ?? new Vector4(0.18f, 0.18f, 0.22f, 1f);

    /// <summary>
    /// Gets the content color or default.
    /// </summary>
    internal Vector4 GetContentColor() =>
        ContentColor ?? new Vector4(0.15f, 0.15f, 0.18f, 1f);

    /// <summary>
    /// Gets the border color or default.
    /// </summary>
    internal Vector4 GetBorderColor() =>
        BorderColor ?? new Vector4(0.3f, 0.3f, 0.35f, 1f);
}

/// <summary>
/// Configuration for creating badge widgets.
/// </summary>
/// <param name="Size">The badge size (diameter) in pixels.</param>
/// <param name="BackgroundColor">The badge background color.</param>
/// <param name="TextColor">The badge text color.</param>
/// <param name="FontSize">The text font size.</param>
/// <param name="MaxValue">Maximum value to display (shows "99+" if exceeded).</param>
public sealed record BadgeConfig(
    float Size = 24,
    Vector4? BackgroundColor = null,
    Vector4? TextColor = null,
    float FontSize = 12,
    int MaxValue = 99)
{
    /// <summary>
    /// The default badge configuration.
    /// </summary>
    public static BadgeConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default (red).
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? new Vector4(0.8f, 0.2f, 0.2f, 1f);

    /// <summary>
    /// Gets the text color or default (white).
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);
}

/// <summary>
/// Configuration for creating avatar widgets.
/// </summary>
/// <param name="Size">The avatar size (width and height) in pixels.</param>
/// <param name="Image">Optional image texture for the avatar.</param>
/// <param name="FallbackText">Text to show if no image (e.g., initials).</param>
/// <param name="FallbackBackgroundColor">Background color when showing fallback text.</param>
/// <param name="FallbackTextColor">Text color for fallback text.</param>
/// <param name="FallbackFontSize">Font size for fallback text.</param>
/// <param name="CornerRadius">Corner radius (0 for square, size/2 for circle).</param>
/// <param name="BorderColor">Optional border color.</param>
/// <param name="BorderWidth">Border width in pixels.</param>
public sealed record AvatarConfig(
    float Size = 64,
    TextureHandle Image = default,
    string FallbackText = "",
    Vector4? FallbackBackgroundColor = null,
    Vector4? FallbackTextColor = null,
    float FallbackFontSize = 24,
    float CornerRadius = 32,
    Vector4? BorderColor = null,
    float BorderWidth = 0)
{
    /// <summary>
    /// The default avatar configuration.
    /// </summary>
    public static AvatarConfig Default { get; } = new();

    /// <summary>
    /// Gets the fallback background color or default.
    /// </summary>
    internal Vector4 GetFallbackBackgroundColor() =>
        FallbackBackgroundColor ?? new Vector4(0.3f, 0.5f, 0.8f, 1f);

    /// <summary>
    /// Gets the fallback text color or default (white).
    /// </summary>
    internal Vector4 GetFallbackTextColor() =>
        FallbackTextColor ?? new Vector4(1f, 1f, 1f, 1f);

    /// <summary>
    /// Gets the border color or default (transparent).
    /// </summary>
    internal Vector4 GetBorderColor() =>
        BorderColor ?? Vector4.Zero;
}

#endregion

#region Phase 10: Modal Dialog

/// <summary>
/// Configuration for creating modal dialog widgets.
/// </summary>
/// <param name="Width">The modal width in pixels.</param>
/// <param name="Height">The modal height in pixels (null for auto height based on content).</param>
/// <param name="Title">The modal title displayed in the header.</param>
/// <param name="CloseOnBackdropClick">Whether clicking the backdrop closes the modal.</param>
/// <param name="CloseOnEscape">Whether pressing Escape closes the modal.</param>
/// <param name="ShowCloseButton">Whether to show a close button in the header.</param>
/// <param name="TitleBarHeight">Height of the title bar in pixels.</param>
/// <param name="BackdropColor">The backdrop overlay color.</param>
/// <param name="TitleBarColor">The title bar background color.</param>
/// <param name="ContentColor">The content area background color.</param>
/// <param name="TitleTextColor">The title text color.</param>
/// <param name="FontSize">Font size for the title.</param>
/// <param name="CornerRadius">Corner radius for the modal dialog.</param>
/// <param name="ButtonSpacing">Space between action buttons.</param>
/// <param name="ContentPadding">Padding around the content area.</param>
public sealed record ModalConfig(
    float Width = 400,
    float? Height = null,
    string Title = "",
    bool CloseOnBackdropClick = true,
    bool CloseOnEscape = true,
    bool ShowCloseButton = true,
    float TitleBarHeight = 40,
    Vector4? BackdropColor = null,
    Vector4? TitleBarColor = null,
    Vector4? ContentColor = null,
    Vector4? TitleTextColor = null,
    float FontSize = 16,
    float CornerRadius = 8,
    float ButtonSpacing = 8,
    UIEdges? ContentPadding = null)
{
    /// <summary>
    /// The default modal configuration.
    /// </summary>
    public static ModalConfig Default { get; } = new();

    /// <summary>
    /// Gets the backdrop color or default.
    /// </summary>
    internal Vector4 GetBackdropColor() =>
        BackdropColor ?? new Vector4(0f, 0f, 0f, 0.6f);

    /// <summary>
    /// Gets the title bar color or default.
    /// </summary>
    internal Vector4 GetTitleBarColor() =>
        TitleBarColor ?? new Vector4(0.18f, 0.18f, 0.22f, 1f);

    /// <summary>
    /// Gets the content color or default.
    /// </summary>
    internal Vector4 GetContentColor() =>
        ContentColor ?? new Vector4(0.15f, 0.15f, 0.18f, 1f);

    /// <summary>
    /// Gets the title text color or default.
    /// </summary>
    internal Vector4 GetTitleTextColor() =>
        TitleTextColor ?? new Vector4(1f, 1f, 1f, 1f);

    /// <summary>
    /// Gets the content padding or default.
    /// </summary>
    internal UIEdges GetContentPadding() =>
        ContentPadding ?? new UIEdges(16, 16, 16, 16);
}

/// <summary>
/// Configuration for an alert dialog (single OK button).
/// </summary>
/// <param name="Width">The alert width in pixels.</param>
/// <param name="Title">The alert title.</param>
/// <param name="OkButtonText">Text for the OK button.</param>
/// <param name="CloseOnBackdropClick">Whether clicking backdrop closes the alert.</param>
/// <param name="CloseOnEscape">Whether Escape closes the alert.</param>
public sealed record AlertConfig(
    float Width = 350,
    string Title = "Alert",
    string OkButtonText = "OK",
    bool CloseOnBackdropClick = true,
    bool CloseOnEscape = true);

/// <summary>
/// Configuration for a confirm dialog (OK/Cancel buttons).
/// </summary>
/// <param name="Width">The confirm dialog width in pixels.</param>
/// <param name="Title">The dialog title.</param>
/// <param name="OkButtonText">Text for the OK button.</param>
/// <param name="CancelButtonText">Text for the Cancel button.</param>
/// <param name="CloseOnBackdropClick">Whether clicking backdrop closes the dialog.</param>
/// <param name="CloseOnEscape">Whether Escape closes the dialog.</param>
public sealed record ConfirmConfig(
    float Width = 350,
    string Title = "Confirm",
    string OkButtonText = "OK",
    string CancelButtonText = "Cancel",
    bool CloseOnBackdropClick = false,
    bool CloseOnEscape = true);

/// <summary>
/// Configuration for a prompt dialog (text input with OK/Cancel).
/// </summary>
/// <param name="Width">The prompt dialog width in pixels.</param>
/// <param name="Title">The dialog title.</param>
/// <param name="Placeholder">Placeholder text for the input field.</param>
/// <param name="InitialValue">Initial value in the input field.</param>
/// <param name="OkButtonText">Text for the OK button.</param>
/// <param name="CancelButtonText">Text for the Cancel button.</param>
/// <param name="CloseOnBackdropClick">Whether clicking backdrop closes the dialog.</param>
/// <param name="CloseOnEscape">Whether Escape closes the dialog.</param>
public sealed record PromptConfig(
    float Width = 400,
    string Title = "Input",
    string Placeholder = "",
    string InitialValue = "",
    string OkButtonText = "OK",
    string CancelButtonText = "Cancel",
    bool CloseOnBackdropClick = false,
    bool CloseOnEscape = true);

/// <summary>
/// Definition for a modal action button.
/// </summary>
/// <param name="Text">The button text.</param>
/// <param name="Result">The result value when clicked.</param>
/// <param name="IsPrimary">Whether this is a primary (styled) button.</param>
/// <param name="Width">Button width (null for auto).</param>
public sealed record ModalButtonDef(
    string Text,
    ModalResult Result = ModalResult.OK,
    bool IsPrimary = false,
    float? Width = null);

#endregion

#region Phase 9: Accordion

/// <summary>
/// Configuration for creating accordion widgets.
/// </summary>
/// <param name="Width">The accordion width (null for stretch).</param>
/// <param name="Height">The accordion height (null for stretch).</param>
/// <param name="AllowMultipleExpanded">Whether multiple sections can be expanded at once.</param>
/// <param name="HeaderHeight">Height of section headers.</param>
/// <param name="Spacing">Space between sections.</param>
/// <param name="BackgroundColor">The accordion background color.</param>
/// <param name="HeaderColor">Section header background color.</param>
/// <param name="HeaderHoverColor">Section header color when hovered.</param>
/// <param name="ContentColor">Section content background color.</param>
/// <param name="HeaderTextColor">Text color for section headers.</param>
/// <param name="ArrowColor">Color for expand/collapse arrows.</param>
/// <param name="BorderColor">Border color between sections.</param>
/// <param name="FontSize">Font size for header text.</param>
/// <param name="CornerRadius">Corner radius for sections.</param>
public sealed record AccordionConfig(
    float? Width = null,
    float? Height = null,
    bool AllowMultipleExpanded = false,
    float HeaderHeight = 32f,
    float Spacing = 1f,
    Vector4? BackgroundColor = null,
    Vector4? HeaderColor = null,
    Vector4? HeaderHoverColor = null,
    Vector4? ContentColor = null,
    Vector4? HeaderTextColor = null,
    Vector4? ArrowColor = null,
    Vector4? BorderColor = null,
    float FontSize = 14f,
    float CornerRadius = 0f)
{
    /// <summary>
    /// The default accordion configuration.
    /// </summary>
    public static AccordionConfig Default { get; } = new();

    /// <summary>
    /// Gets the background color or default (transparent).
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? Vector4.Zero;

    /// <summary>
    /// Gets the header color or default.
    /// </summary>
    internal Vector4 GetHeaderColor() =>
        HeaderColor ?? new Vector4(0.2f, 0.2f, 0.24f, 1f);

    /// <summary>
    /// Gets the header hover color or default.
    /// </summary>
    internal Vector4 GetHeaderHoverColor() =>
        HeaderHoverColor ?? new Vector4(0.25f, 0.25f, 0.3f, 1f);

    /// <summary>
    /// Gets the content color or default.
    /// </summary>
    internal Vector4 GetContentColor() =>
        ContentColor ?? new Vector4(0.15f, 0.15f, 0.18f, 1f);

    /// <summary>
    /// Gets the header text color or default.
    /// </summary>
    internal Vector4 GetHeaderTextColor() =>
        HeaderTextColor ?? new Vector4(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>
    /// Gets the arrow color or default.
    /// </summary>
    internal Vector4 GetArrowColor() =>
        ArrowColor ?? new Vector4(0.7f, 0.7f, 0.7f, 1f);

    /// <summary>
    /// Gets the border color or default.
    /// </summary>
    internal Vector4 GetBorderColor() =>
        BorderColor ?? new Vector4(0.1f, 0.1f, 0.12f, 1f);
}

/// <summary>
/// Definition for an accordion section.
/// </summary>
/// <param name="Title">The section title displayed in the header.</param>
/// <param name="IsExpanded">Initial expanded state.</param>
public sealed record AccordionSectionDef(
    string Title,
    bool IsExpanded = false);

#endregion

#region Toast Configuration

/// <summary>
/// Configuration for creating toast notifications.
/// </summary>
/// <param name="Message">The toast message text.</param>
/// <param name="Title">Optional title displayed above the message.</param>
/// <param name="Type">The toast type (info, success, warning, error).</param>
/// <param name="Duration">How long the toast displays in seconds (0 = indefinite).</param>
/// <param name="CanDismiss">Whether the toast can be dismissed by clicking.</param>
/// <param name="ShowCloseButton">Whether to show a close button.</param>
/// <param name="Width">The toast width in pixels.</param>
/// <param name="BackgroundColor">Custom background color (defaults based on type).</param>
/// <param name="TextColor">Custom text color.</param>
/// <param name="FontSize">The message font size.</param>
/// <param name="TitleFontSize">The title font size.</param>
/// <param name="CornerRadius">The corner radius for rounded corners.</param>
/// <param name="Padding">Internal padding.</param>
public sealed record ToastConfig(
    string Message,
    string? Title = null,
    ToastType Type = ToastType.Info,
    float Duration = 3f,
    bool CanDismiss = true,
    bool ShowCloseButton = true,
    float Width = 300,
    Vector4? BackgroundColor = null,
    Vector4? TextColor = null,
    float FontSize = 14,
    float TitleFontSize = 16,
    float CornerRadius = 6,
    UIEdges? Padding = null)
{
    /// <summary>
    /// Creates an info toast configuration.
    /// </summary>
    public static ToastConfig Info(string message, string? title = null, float duration = 3f) =>
        new(message, title, ToastType.Info, duration);

    /// <summary>
    /// Creates a success toast configuration.
    /// </summary>
    public static ToastConfig Success(string message, string? title = null, float duration = 3f) =>
        new(message, title, ToastType.Success, duration);

    /// <summary>
    /// Creates a warning toast configuration.
    /// </summary>
    public static ToastConfig Warning(string message, string? title = null, float duration = 5f) =>
        new(message, title, ToastType.Warning, duration);

    /// <summary>
    /// Creates an error toast configuration.
    /// </summary>
    public static ToastConfig Error(string message, string? title = null, float duration = 0f) =>
        new(message, title, ToastType.Error, duration);

    /// <summary>
    /// Gets the background color based on toast type.
    /// </summary>
    internal Vector4 GetBackgroundColor() =>
        BackgroundColor ?? Type switch
        {
            ToastType.Success => new Vector4(0.15f, 0.5f, 0.2f, 0.95f),
            ToastType.Warning => new Vector4(0.6f, 0.45f, 0.1f, 0.95f),
            ToastType.Error => new Vector4(0.6f, 0.15f, 0.15f, 0.95f),
            _ => new Vector4(0.2f, 0.25f, 0.35f, 0.95f)  // Info
        };

    /// <summary>
    /// Gets the text color or default.
    /// </summary>
    internal Vector4 GetTextColor() =>
        TextColor ?? new Vector4(1f, 1f, 1f, 1f);

    /// <summary>
    /// Gets the padding or default.
    /// </summary>
    internal UIEdges GetPadding() =>
        Padding ?? new UIEdges(12, 16, 12, 16);
}

/// <summary>
/// Configuration for creating toast containers.
/// </summary>
/// <param name="Position">Where toasts appear on screen.</param>
/// <param name="MaxVisible">Maximum number of visible toasts.</param>
/// <param name="Spacing">Space between stacked toasts.</param>
/// <param name="Margin">Margin from container edges.</param>
public sealed record ToastContainerConfig(
    ToastPosition Position = ToastPosition.TopRight,
    int MaxVisible = 5,
    float Spacing = 10f,
    float Margin = 20f)
{
    /// <summary>
    /// The default toast container configuration.
    /// </summary>
    public static ToastContainerConfig Default { get; } = new();

    /// <summary>
    /// Creates a top-right positioned container.
    /// </summary>
    public static ToastContainerConfig TopRight(int maxVisible = 5) =>
        new(ToastPosition.TopRight, maxVisible);

    /// <summary>
    /// Creates a bottom-right positioned container.
    /// </summary>
    public static ToastContainerConfig BottomRight(int maxVisible = 5) =>
        new(ToastPosition.BottomRight, maxVisible);

    /// <summary>
    /// Creates a top-center positioned container.
    /// </summary>
    public static ToastContainerConfig TopCenter(int maxVisible = 3) =>
        new(ToastPosition.TopCenter, maxVisible);

    /// <summary>
    /// Creates a bottom-center positioned container.
    /// </summary>
    public static ToastContainerConfig BottomCenter(int maxVisible = 3) =>
        new(ToastPosition.BottomCenter, maxVisible);
}

/// <summary>
/// Configuration for creating spinner widgets.
/// </summary>
/// <param name="Style">The spinner animation style.</param>
/// <param name="Size">The size of the spinner in pixels.</param>
/// <param name="Speed">Rotation speed in radians per second.</param>
/// <param name="Color">The spinner color.</param>
/// <param name="Thickness">The stroke thickness.</param>
/// <param name="ArcLength">Arc length for circular spinners (0 to 1).</param>
/// <param name="ElementCount">Number of elements for dot spinners.</param>
public sealed record SpinnerConfig(
    SpinnerStyle Style = SpinnerStyle.Circular,
    float Size = 40f,
    float Speed = MathF.PI * 2,
    Vector4? Color = null,
    float Thickness = 3f,
    float ArcLength = 0.75f,
    int ElementCount = 8)
{
    /// <summary>
    /// The default spinner configuration.
    /// </summary>
    public static SpinnerConfig Default { get; } = new();

    /// <summary>
    /// Creates a small spinner (24px).
    /// </summary>
    public static SpinnerConfig Small(Vector4? color = null) =>
        new(Size: 24f, Color: color, Thickness: 2f);

    /// <summary>
    /// Creates a medium spinner (40px).
    /// </summary>
    public static SpinnerConfig Medium(Vector4? color = null) =>
        new(Size: 40f, Color: color);

    /// <summary>
    /// Creates a large spinner (64px).
    /// </summary>
    public static SpinnerConfig Large(Vector4? color = null) =>
        new(Size: 64f, Color: color, Thickness: 4f);

    /// <summary>
    /// Creates a dot-style spinner.
    /// </summary>
    public static SpinnerConfig Dots(float size = 40f, int dotCount = 8, Vector4? color = null) =>
        new(Style: SpinnerStyle.Dots, Size: size, ElementCount: dotCount, Color: color);

    /// <summary>
    /// Creates a bar-style spinner (indeterminate progress).
    /// </summary>
    public static SpinnerConfig Bar(float width = 200f, Vector4? color = null) =>
        new(Style: SpinnerStyle.Bar, Size: width, Color: color);

    /// <summary>
    /// Gets the color or default.
    /// </summary>
    internal Vector4 GetColor() =>
        Color ?? new Vector4(0.3f, 0.6f, 1f, 1f); // Blue
}

#endregion

#region Color Picker

/// <summary>
/// Configuration for creating color picker widgets.
/// </summary>
/// <param name="InitialColor">The initial color in RGBA format (0-1 range).</param>
/// <param name="Mode">The color picker mode (HSV, RGB, or Both).</param>
/// <param name="ShowAlpha">Whether to show the alpha slider.</param>
/// <param name="ShowHexInput">Whether to show the hex input field.</param>
/// <param name="ShowPreview">Whether to show the color preview panel.</param>
/// <param name="Width">The width of the color picker.</param>
/// <param name="Height">The height of the color picker.</param>
/// <param name="CornerRadius">The corner radius for rounded corners.</param>
public sealed record ColorPickerConfig(
    Vector4? InitialColor = null,
    ColorPickerMode Mode = ColorPickerMode.HSV,
    bool ShowAlpha = true,
    bool ShowHexInput = true,
    bool ShowPreview = true,
    float Width = 250f,
    float Height = 300f,
    float CornerRadius = 4f)
{
    /// <summary>
    /// The default color picker configuration.
    /// </summary>
    public static ColorPickerConfig Default { get; } = new();

    /// <summary>
    /// Creates an HSV color picker configuration.
    /// </summary>
    public static ColorPickerConfig HSV(Vector4? initialColor = null, bool showAlpha = true) =>
        new(InitialColor: initialColor, Mode: ColorPickerMode.HSV, ShowAlpha: showAlpha);

    /// <summary>
    /// Creates an RGB color picker configuration.
    /// </summary>
    public static ColorPickerConfig RGB(Vector4? initialColor = null, bool showAlpha = true) =>
        new(InitialColor: initialColor, Mode: ColorPickerMode.RGB, ShowAlpha: showAlpha);

    /// <summary>
    /// Creates a compact color picker (no hex input, smaller size).
    /// </summary>
    public static ColorPickerConfig Compact(Vector4? initialColor = null) =>
        new(InitialColor: initialColor, ShowHexInput: false, Width: 200f, Height: 220f);

    /// <summary>
    /// Creates a color picker without alpha slider.
    /// </summary>
    public static ColorPickerConfig Opaque(Vector4? initialColor = null) =>
        new(InitialColor: initialColor, ShowAlpha: false);

    /// <summary>
    /// Gets the initial color or default (red).
    /// </summary>
    internal Vector4 GetInitialColor() =>
        InitialColor ?? new Vector4(1f, 0f, 0f, 1f);
}

#endregion

#region Date/Time Picker

/// <summary>
/// Configuration for creating date picker widgets.
/// </summary>
/// <param name="InitialValue">The initial date/time value.</param>
/// <param name="Mode">The picker mode (Date, Time, or DateTime).</param>
/// <param name="TimeFormat">The time format (12-hour or 24-hour).</param>
/// <param name="ShowSeconds">Whether to show seconds in time mode.</param>
/// <param name="MinDate">The minimum selectable date (null for no minimum).</param>
/// <param name="MaxDate">The maximum selectable date (null for no maximum).</param>
/// <param name="FirstDayOfWeek">The first day of the week for calendar display.</param>
/// <param name="Width">The width of the date picker.</param>
/// <param name="Height">The height of the date picker.</param>
/// <param name="CornerRadius">The corner radius for rounded corners.</param>
public sealed record DatePickerConfig(
    DateTime? InitialValue = null,
    DatePickerMode Mode = DatePickerMode.Date,
    TimeFormat TimeFormat = TimeFormat.Hour24,
    bool ShowSeconds = false,
    DateTime? MinDate = null,
    DateTime? MaxDate = null,
    DayOfWeek FirstDayOfWeek = DayOfWeek.Sunday,
    float Width = 280f,
    float Height = 320f,
    float CornerRadius = 4f)
{
    /// <summary>
    /// The default date picker configuration.
    /// </summary>
    public static DatePickerConfig Default { get; } = new();

    /// <summary>
    /// Creates a date-only picker configuration.
    /// </summary>
    public static DatePickerConfig DateOnly(DateTime? initialValue = null) =>
        new(InitialValue: initialValue, Mode: DatePickerMode.Date);

    /// <summary>
    /// Creates a time-only picker configuration.
    /// </summary>
    public static DatePickerConfig TimeOnly(DateTime? initialValue = null, TimeFormat format = TimeFormat.Hour24) =>
        new(InitialValue: initialValue, Mode: DatePickerMode.Time, TimeFormat: format, Height: 100f);

    /// <summary>
    /// Creates a date and time picker configuration.
    /// </summary>
    public static DatePickerConfig DateAndTime(DateTime? initialValue = null, TimeFormat format = TimeFormat.Hour24) =>
        new(InitialValue: initialValue, Mode: DatePickerMode.DateTime, TimeFormat: format, Height: 380f);

    /// <summary>
    /// Creates a date picker with a date range constraint.
    /// </summary>
    public static DatePickerConfig WithRange(DateTime minDate, DateTime maxDate, DateTime? initialValue = null) =>
        new(InitialValue: initialValue, MinDate: minDate, MaxDate: maxDate);

    /// <summary>
    /// Creates a date picker that only allows future dates.
    /// </summary>
    public static DatePickerConfig FutureOnly(DateTime? initialValue = null) =>
        new(InitialValue: initialValue ?? DateTime.Today, MinDate: DateTime.Today);

    /// <summary>
    /// Creates a date picker that only allows past dates.
    /// </summary>
    public static DatePickerConfig PastOnly(DateTime? initialValue = null) =>
        new(InitialValue: initialValue ?? DateTime.Today, MaxDate: DateTime.Today);

    /// <summary>
    /// Gets the initial value or default (now).
    /// </summary>
    internal DateTime GetInitialValue() =>
        InitialValue ?? DateTime.Now;
}

#endregion

#region DataGrid Config

/// <summary>
/// Configuration for a data grid column definition.
/// </summary>
/// <param name="Header">The column header text.</param>
/// <param name="Width">The column width in pixels.</param>
/// <param name="MinWidth">The minimum width when resizing.</param>
/// <param name="IsSortable">Whether this column is sortable.</param>
/// <param name="IsResizable">Whether this column is resizable.</param>
public sealed record DataGridColumnDef(
    string Header,
    float Width = 100f,
    float MinWidth = 50f,
    bool IsSortable = true,
    bool IsResizable = true);

/// <summary>
/// Configuration for creating a data grid.
/// </summary>
/// <param name="Columns">The column definitions.</param>
/// <param name="SelectionMode">The row selection mode.</param>
/// <param name="AllowColumnResize">Whether columns can be resized.</param>
/// <param name="AllowSorting">Whether columns can be sorted.</param>
/// <param name="RowHeight">The height of data rows.</param>
/// <param name="HeaderHeight">The height of the header row.</param>
/// <param name="AlternatingRowColors">Whether to show alternating row colors.</param>
/// <param name="Size">The overall grid size.</param>
public sealed record DataGridConfig(
    DataGridColumnDef[] Columns,
    GridSelectionMode SelectionMode = GridSelectionMode.Single,
    bool AllowColumnResize = true,
    bool AllowSorting = true,
    float RowHeight = 30f,
    float HeaderHeight = 35f,
    bool AlternatingRowColors = true,
    Vector2? Size = null)
{
    /// <summary>
    /// Creates a default data grid configuration with specified columns.
    /// </summary>
    public static DataGridConfig WithColumns(params DataGridColumnDef[] columns) =>
        new(Columns: columns);

    /// <summary>
    /// Creates a data grid with simple string column headers.
    /// </summary>
    public static DataGridConfig WithHeaders(params string[] headers) =>
        new(Columns: headers.Select(h => new DataGridColumnDef(h)).ToArray());

    /// <summary>
    /// Creates a read-only data grid (no selection, no sorting, no resize).
    /// </summary>
    public static DataGridConfig ReadOnly(params DataGridColumnDef[] columns) =>
        new(
            Columns: columns,
            SelectionMode: GridSelectionMode.None,
            AllowColumnResize: false,
            AllowSorting: false);

    /// <summary>
    /// Creates a data grid that allows multiple row selection.
    /// </summary>
    public static DataGridConfig MultiSelect(params DataGridColumnDef[] columns) =>
        new(
            Columns: columns,
            SelectionMode: GridSelectionMode.Multiple);
}

#endregion
