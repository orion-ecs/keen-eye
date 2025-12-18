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
/// <param name="MinWidth">Minimum width when resizing.</param>
/// <param name="MinHeight">Minimum height when resizing.</param>
/// <param name="TitleBarColor">The title bar background color.</param>
/// <param name="ContentColor">The content area background color.</param>
/// <param name="TitleTextColor">The title text color.</param>
/// <param name="CloseButtonColor">The close button background color.</param>
/// <param name="CloseButtonHoverColor">The close button hover color.</param>
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
    float MinWidth = 150,
    float MinHeight = 100,
    Vector4? TitleBarColor = null,
    Vector4? ContentColor = null,
    Vector4? TitleTextColor = null,
    Vector4? CloseButtonColor = null,
    Vector4? CloseButtonHoverColor = null,
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
