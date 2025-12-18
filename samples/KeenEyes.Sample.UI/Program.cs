// KeenEyes UI Widget Gallery
// Comprehensive demonstration of all WidgetFactory widgets.
//
// This sample showcases:
// - All 12 WidgetFactory widgets (Button, Panel, Label, TextField, Checkbox,
//   Slider, ProgressBar, Toggle, Dropdown, TabView, Divider, ScrollView)
// - Multiple color schemes (primary, success, danger, warning, info)
// - Various font sizes (small, normal, large, title)
// - Different layout configurations
//
// Controls:
//   Tab / Shift+Tab  - Navigate between focusable elements
//   Enter / Space    - Activate focused element
//   Mouse            - Click and interact with widgets
//   Escape           - Exit
//
// NOTE: This sample requires a display to run.

using System.Numerics;
using KeenEyes;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Input.Abstractions;
using KeenEyes.Input.Silk;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

Console.WriteLine("KeenEyes UI Widget Gallery");
Console.WriteLine("==========================");
Console.WriteLine();
Console.WriteLine("This sample demonstrates all WidgetFactory widgets:");
Console.WriteLine("- Button, Panel, Label, TextField");
Console.WriteLine("- Checkbox, Slider, ProgressBar, Toggle");
Console.WriteLine("- Dropdown, TabView, Divider, ScrollView");
Console.WriteLine();
Console.WriteLine("Controls:");
Console.WriteLine("  Tab / Shift+Tab  - Navigate between elements");
Console.WriteLine("  Enter / Space    - Activate focused element");
Console.WriteLine("  Mouse            - Click and interact");
Console.WriteLine("  Escape           - Exit");
Console.WriteLine();

// Find a suitable font file
var fontPath = FindSystemFont();
if (fontPath is null)
{
    Console.WriteLine("Warning: No suitable system font found. Text will not be visible.");
}
else
{
    Console.WriteLine($"Using font: {fontPath}");
}

// Configure window
var windowConfig = new WindowConfig
{
    Title = "KeenEyes UI Widget Gallery",
    Width = 1280,
    Height = 800,
    VSync = true
};

// Configure graphics
var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.08f, 0.08f, 0.12f, 1f),
    EnableDepthTest = false,
    EnableCulling = false
};

// Configure input
var inputConfig = new SilkInputConfig
{
    EnableGamepads = false,
    CaptureMouseOnClick = false
};

using var world = new World();

// Install plugins
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));
world.InstallPlugin(new UIPlugin());

// Subscribe to UI events
SubscribeToUIEvents(world);

Console.WriteLine("Starting...");

try
{
    world.CreateRunner()
        .OnReady(() =>
        {
            Console.WriteLine("UI system initialized!");

            var ui = world.GetExtension<UIContext>();

            // Load the font
            FontHandle font = default;
            var fontManagerProvider = world.GetExtension<IFontManagerProvider>();
            var fontManager = fontManagerProvider?.GetFontManager();
            if (fontManager is not null && fontPath is not null)
            {
                try
                {
                    font = fontManager.LoadFont(fontPath, 16f);
                    Console.WriteLine("Font loaded successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load font: {ex.Message}");
                }
            }

            // Create the widget gallery
            CreateWidgetGallery(world, ui, font);

            // Handle escape key to exit
            var input = world.GetExtension<IInputContext>();
            input.Keyboard.OnKeyDown += args =>
            {
                if (args.Key == Key.Escape)
                {
                    Console.WriteLine("Escape pressed - closing...");
                    Environment.Exit(0);
                }
            };

            Console.WriteLine();
            Console.WriteLine("Widget gallery created!");
        })
        .OnResize((width, height) =>
        {
            Console.WriteLine($"Window resized to {width}x{height}");
            var layoutSystem = world.GetSystem<UILayoutSystem>();
            layoutSystem?.SetScreenSize(width, height);
        })
        .Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("This sample requires a display.");
}

Console.WriteLine("Sample complete!");

// ============================================================================
// Widget Gallery Creation
// ============================================================================

static void CreateWidgetGallery(World world, UIContext ui, FontHandle font)
{
    // Create root canvas
    var canvas = ui.CreateCanvas("WidgetGallery");

    // Create main container panel (centered, fixed size)
    var mainPanel = WidgetFactory.CreatePanel(world, canvas, "MainPanel", new PanelConfig(
        Width: 900,
        Height: 700,
        Direction: LayoutDirection.Vertical,
        MainAxisAlign: LayoutAlign.Start,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 0,
        BackgroundColor: Colors.DarkPanel,
        CornerRadius: 12,
        Padding: UIEdges.All(20)
    ));

    // Center the main panel
    ref var mainRect = ref world.Get<UIRect>(mainPanel);
    mainRect.AnchorMin = new Vector2(0.5f, 0.5f);
    mainRect.AnchorMax = new Vector2(0.5f, 0.5f);
    mainRect.Pivot = new Vector2(0.5f, 0.5f);

    // Title
    WidgetFactory.CreateLabel(world, mainPanel, "Title", "KeenEyes UI Widget Gallery", font, new LabelConfig(
        Width: 860,
        Height: 40,
        FontSize: 28,
        TextColor: Colors.TextWhite,
        HorizontalAlign: TextAlignH.Center
    ));

    // Subtitle
    WidgetFactory.CreateLabel(world, mainPanel, "Subtitle", "Showcasing all 12 WidgetFactory widgets", font, new LabelConfig(
        Width: 860,
        Height: 25,
        FontSize: 14,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Center
    ));

    // Spacing
    CreateSpacer(world, mainPanel, 15);

    // Create TabView with 3 tabs
    var tabs = new TabConfig[]
    {
        new("Controls", MinWidth: 120),
        new("Inputs", MinWidth: 120),
        new("Layout", MinWidth: 120)
    };

    var (tabView, contentPanels) = WidgetFactory.CreateTabView(
        world, mainPanel, "MainTabs", tabs, font,
        new TabViewConfig(
            Width: 860,
            Height: 560,
            TabBarHeight: 45,
            TabSpacing: 4,
            TabBarColor: new Vector4(0.10f, 0.10f, 0.13f, 1f),
            ContentColor: Colors.MediumPanel,
            TabColor: new Vector4(0.14f, 0.14f, 0.18f, 1f),
            ActiveTabColor: Colors.MediumPanel,
            FontSize: 15
        ));

    // Populate Tab 1: Controls
    PopulateControlsTab(world, contentPanels[0], font);

    // Populate Tab 2: Inputs
    PopulateInputsTab(world, contentPanels[1], font);

    // Populate Tab 3: Layout
    PopulateLayoutTab(world, contentPanels[2], font);

    Console.WriteLine("Created widget gallery with TabView and all widget types");
}

// ============================================================================
// Tab 1: Controls (Buttons, Progress Bars, Sliders)
// ============================================================================

static void PopulateControlsTab(World world, Entity panel, FontHandle font)
{
    // Configure panel layout
    ref var layout = ref world.Get<UILayout>(panel);
    layout.Spacing = 20;
    layout.CrossAxisAlign = LayoutAlign.Center;
    world.Add(panel, new UIStyle { Padding = UIEdges.All(20) });

    // --- Buttons Section ---
    var buttonsSection = CreateSection(world, panel, "Buttons", font);

    // Row 1: Different colors
    var colorRow = WidgetFactory.CreatePanel(world, buttonsSection, "ColorRow", new PanelConfig(
        Height: 50,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 15
    ));

    WidgetFactory.CreateButton(world, colorRow, "PrimaryBtn", "Primary", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Primary,
        BorderColor: Colors.PrimaryBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 1
    ));

    WidgetFactory.CreateButton(world, colorRow, "SuccessBtn", "Success", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Success,
        BorderColor: Colors.SuccessBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 2
    ));

    WidgetFactory.CreateButton(world, colorRow, "DangerBtn", "Danger", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Danger,
        BorderColor: Colors.DangerBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 3
    ));

    WidgetFactory.CreateButton(world, colorRow, "WarningBtn", "Warning", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Warning,
        BorderColor: Colors.WarningBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TextColor: Colors.TextDark,
        TabIndex: 4
    ));

    WidgetFactory.CreateButton(world, colorRow, "InfoBtn", "Info", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Info,
        BorderColor: Colors.InfoBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 5
    ));

    // Row 2: Different sizes
    var sizeRow = WidgetFactory.CreatePanel(world, buttonsSection, "SizeRow", new PanelConfig(
        Height: 55,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 15
    ));

    WidgetFactory.CreateButton(world, sizeRow, "SmallBtn", "Small", font, new ButtonConfig(
        Width: 70, Height: 28,
        BackgroundColor: Colors.LightPanel,
        CornerRadius: 4,
        FontSize: 12,
        TabIndex: 6
    ));

    WidgetFactory.CreateButton(world, sizeRow, "MediumBtn", "Medium", font, new ButtonConfig(
        Width: 100, Height: 38,
        BackgroundColor: Colors.LightPanel,
        CornerRadius: 6,
        FontSize: 14,
        TabIndex: 7
    ));

    WidgetFactory.CreateButton(world, sizeRow, "LargeBtn", "Large Button", font, new ButtonConfig(
        Width: 150, Height: 48,
        BackgroundColor: Colors.LightPanel,
        CornerRadius: 8,
        FontSize: 16,
        TabIndex: 8
    ));

    WidgetFactory.CreateButton(world, sizeRow, "XLBtn", "Extra Large", font, new ButtonConfig(
        Width: 180, Height: 55,
        BackgroundColor: Colors.Primary,
        BorderColor: Colors.PrimaryBorder,
        BorderWidth: 3,
        CornerRadius: 10,
        FontSize: 18,
        TabIndex: 9
    ));

    // --- Progress Bars Section ---
    var progressSection = CreateSection(world, panel, "Progress Bars", font);

    // Different values with labels
    CreateLabeledProgressBar(world, progressSection, "Loading...", 25f, Colors.AccentBlue, font);
    CreateLabeledProgressBar(world, progressSection, "Processing", 50f, Colors.AccentGreen, font);
    CreateLabeledProgressBar(world, progressSection, "Almost done", 75f, Colors.AccentOrange, font);
    CreateLabeledProgressBar(world, progressSection, "Complete!", 100f, Colors.Success, font);

    // --- Sliders Section ---
    var slidersSection = CreateSection(world, panel, "Sliders", font);

    CreateLabeledSlider(world, slidersSection, "Volume", 75f, Colors.AccentBlue, font, 10);
    CreateLabeledSlider(world, slidersSection, "Brightness", 50f, Colors.AccentOrange, font, 11);
    CreateLabeledSlider(world, slidersSection, "Sensitivity", 30f, Colors.AccentPurple, font, 12);
}

// ============================================================================
// Tab 2: Inputs (TextField, Checkbox, Toggle, Dropdown)
// ============================================================================

static void PopulateInputsTab(World world, Entity panel, FontHandle font)
{
    ref var layout = ref world.Get<UILayout>(panel);
    layout.Spacing = 25;
    layout.CrossAxisAlign = LayoutAlign.Center;
    world.Add(panel, new UIStyle { Padding = UIEdges.All(20) });

    // --- Text Fields Section ---
    var textSection = CreateSection(world, panel, "Text Fields", font);

    var textFieldRow = WidgetFactory.CreatePanel(world, textSection, "TextFieldRow", new PanelConfig(
        Height: 45,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 20
    ));

    WidgetFactory.CreateTextField(world, textFieldRow, "UsernameField", font, new TextFieldConfig(
        Width: 220,
        Height: 36,
        PlaceholderText: "Enter username...",
        BorderWidth: 1,
        TabIndex: 20
    ));

    WidgetFactory.CreateTextField(world, textFieldRow, "EmailField", font, new TextFieldConfig(
        Width: 280,
        Height: 36,
        PlaceholderText: "Enter email address...",
        BorderWidth: 1,
        TabIndex: 21
    ));

    // --- Checkboxes Section ---
    var checkboxSection = CreateSection(world, panel, "Checkboxes", font);

    var checkRow = WidgetFactory.CreatePanel(world, checkboxSection, "CheckRow", new PanelConfig(
        Height: 100,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Start,
        Spacing: 40
    ));

    // Left column
    var checkCol1 = WidgetFactory.CreatePanel(world, checkRow, "CheckCol1", new PanelConfig(
        Width: 200,
        Direction: LayoutDirection.Vertical,
        Spacing: 12
    ));

    WidgetFactory.CreateCheckbox(world, checkCol1, "EnableSound", "Enable Sound", font, new CheckboxConfig(
        IsChecked: true,
        CheckColor: Colors.Success,
        TabIndex: 22
    ));

    WidgetFactory.CreateCheckbox(world, checkCol1, "EnableMusic", "Enable Music", font, new CheckboxConfig(
        IsChecked: true,
        CheckColor: Colors.AccentBlue,
        TabIndex: 23
    ));

    WidgetFactory.CreateCheckbox(world, checkCol1, "ShowFPS", "Show FPS Counter", font, new CheckboxConfig(
        IsChecked: false,
        CheckColor: Colors.AccentOrange,
        TabIndex: 24
    ));

    // Right column
    var checkCol2 = WidgetFactory.CreatePanel(world, checkRow, "CheckCol2", new PanelConfig(
        Width: 200,
        Direction: LayoutDirection.Vertical,
        Spacing: 12
    ));

    WidgetFactory.CreateCheckbox(world, checkCol2, "Fullscreen", "Fullscreen Mode", font, new CheckboxConfig(
        IsChecked: false,
        CheckColor: Colors.AccentPurple,
        TabIndex: 25
    ));

    WidgetFactory.CreateCheckbox(world, checkCol2, "VSync", "Enable V-Sync", font, new CheckboxConfig(
        IsChecked: true,
        CheckColor: Colors.Success,
        TabIndex: 26
    ));

    WidgetFactory.CreateCheckbox(world, checkCol2, "AutoSave", "Auto-Save Game", font, new CheckboxConfig(
        IsChecked: true,
        CheckColor: Colors.AccentBlue,
        TabIndex: 27
    ));

    // --- Toggles Section ---
    var toggleSection = CreateSection(world, panel, "Toggles", font);

    var toggleRow = WidgetFactory.CreatePanel(world, toggleSection, "ToggleRow", new PanelConfig(
        Height: 80,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Start,
        Spacing: 50
    ));

    var toggleCol1 = WidgetFactory.CreatePanel(world, toggleRow, "ToggleCol1", new PanelConfig(
        Width: 180,
        Direction: LayoutDirection.Vertical,
        Spacing: 15
    ));

    WidgetFactory.CreateToggle(world, toggleCol1, "DarkMode", "Dark Mode", font, new ToggleConfig(
        IsOn: true,
        TrackOnColor: Colors.AccentBlue,
        TabIndex: 28
    ));

    WidgetFactory.CreateToggle(world, toggleCol1, "Notifications", "Notifications", font, new ToggleConfig(
        IsOn: false,
        TrackOnColor: Colors.Success,
        TabIndex: 29
    ));

    var toggleCol2 = WidgetFactory.CreatePanel(world, toggleRow, "ToggleCol2", new PanelConfig(
        Width: 180,
        Direction: LayoutDirection.Vertical,
        Spacing: 15
    ));

    WidgetFactory.CreateToggle(world, toggleCol2, "Analytics", "Send Analytics", font, new ToggleConfig(
        IsOn: false,
        TrackOnColor: Colors.AccentOrange,
        TabIndex: 30
    ));

    WidgetFactory.CreateToggle(world, toggleCol2, "Beta", "Beta Features", font, new ToggleConfig(
        IsOn: true,
        TrackOnColor: Colors.AccentPurple,
        TabIndex: 31
    ));

    // --- Dropdown Section ---
    var dropdownSection = CreateSection(world, panel, "Dropdowns", font);

    var dropdownRow = WidgetFactory.CreatePanel(world, dropdownSection, "DropdownRow", new PanelConfig(
        Height: 50,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 30
    ));

    WidgetFactory.CreateDropdown(world, dropdownRow, "ResolutionDropdown",
        ["1920 x 1080", "1600 x 900", "1280 x 720", "1024 x 768"],
        font, new DropdownConfig(
            Width: 180,
            SelectedIndex: 0,
            TabIndex: 32
        ));

    WidgetFactory.CreateDropdown(world, dropdownRow, "QualityDropdown",
        ["Ultra", "High", "Medium", "Low"],
        font, new DropdownConfig(
            Width: 140,
            SelectedIndex: 1,
            SelectedColor: Colors.Success,
            TabIndex: 33
        ));

    WidgetFactory.CreateDropdown(world, dropdownRow, "LanguageDropdown",
        ["English", "Spanish", "French", "German", "Japanese"],
        font, new DropdownConfig(
            Width: 150,
            SelectedIndex: 0,
            SelectedColor: Colors.AccentPurple,
            TabIndex: 34
        ));
}

// ============================================================================
// Tab 3: Layout (ScrollView, Dividers, Nested Panels)
// ============================================================================

static void PopulateLayoutTab(World world, Entity panel, FontHandle font)
{
    ref var layout = ref world.Get<UILayout>(panel);
    layout.Spacing = 20;
    layout.CrossAxisAlign = LayoutAlign.Center;
    world.Add(panel, new UIStyle { Padding = UIEdges.All(20) });

    // --- Dividers Section ---
    var dividerSection = CreateSection(world, panel, "Dividers", font);

    WidgetFactory.CreateDivider(world, dividerSection, "Divider1", new DividerConfig(
        Thickness: 1,
        Color: Colors.TextMuted,
        Margin: 5
    ));

    WidgetFactory.CreateLabel(world, dividerSection, "DividerLabel1", "Thin divider (1px)", font, new LabelConfig(
        Height: 20,
        FontSize: 12,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Center
    ));

    WidgetFactory.CreateDivider(world, dividerSection, "Divider2", new DividerConfig(
        Thickness: 2,
        Color: Colors.AccentBlue,
        Margin: 5
    ));

    WidgetFactory.CreateLabel(world, dividerSection, "DividerLabel2", "Colored divider (2px)", font, new LabelConfig(
        Height: 20,
        FontSize: 12,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Center
    ));

    WidgetFactory.CreateDivider(world, dividerSection, "Divider3", new DividerConfig(
        Thickness: 4,
        Color: Colors.Success,
        Margin: 5
    ));

    WidgetFactory.CreateLabel(world, dividerSection, "DividerLabel3", "Thick divider (4px)", font, new LabelConfig(
        Height: 20,
        FontSize: 12,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Center
    ));

    // --- Nested Panels Section ---
    var panelsSection = CreateSection(world, panel, "Nested Panels", font);

    var panelRow = WidgetFactory.CreatePanel(world, panelsSection, "PanelRow", new PanelConfig(
        Height: 120,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 20
    ));

    // Create colored panels
    CreateColoredPanel(world, panelRow, "Dark Panel", Colors.DarkPanel, font);
    CreateColoredPanel(world, panelRow, "Medium Panel", Colors.MediumPanel, font);
    CreateColoredPanel(world, panelRow, "Light Panel", Colors.LightPanel, font);
    CreateColoredPanel(world, panelRow, "Primary", Colors.Primary, font);

    // --- ScrollView Section ---
    var scrollSection = CreateSection(world, panel, "ScrollView", font);

    var (scrollView, scrollContent) = WidgetFactory.CreateScrollView(
        world, scrollSection, "DemoScrollView",
        new ScrollViewConfig(
            Width: 700,
            Height: 150,
            ContentHeight: 400,
            BackgroundColor: Colors.DarkPanel,
            ShowVerticalScrollbar: true
        ));

    // Add content to scroll view
    for (int i = 1; i <= 15; i++)
    {
        var colors = new[] { Colors.Primary, Colors.Success, Colors.Warning, Colors.Info, Colors.Danger };
        var color = colors[(i - 1) % colors.Length];

        WidgetFactory.CreateLabel(world, scrollContent, $"ScrollItem{i}",
            $"Scrollable Item {i} - Scroll down to see more content!", font,
            new LabelConfig(
                Width: 680,
                Height: 24,
                FontSize: 13,
                TextColor: color
            ));
    }
}

// ============================================================================
// Helper Methods
// ============================================================================

static Entity CreateSection(World world, Entity parent, string title, FontHandle font)
{
    var section = WidgetFactory.CreatePanel(world, parent, $"{title}Section", new PanelConfig(
        Direction: LayoutDirection.Vertical,
        MainAxisAlign: LayoutAlign.Start,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 10,
        BackgroundColor: Colors.LightPanel,
        CornerRadius: 8,
        Padding: UIEdges.All(15)
    ));

    ref var sectionRect = ref world.Get<UIRect>(section);
    sectionRect.Size = new Vector2(800, 0);
    sectionRect.WidthMode = UISizeMode.Fixed;
    sectionRect.HeightMode = UISizeMode.FitContent;

    // Section title
    WidgetFactory.CreateLabel(world, section, $"{title}Title", title, font, new LabelConfig(
        Width: 770,
        Height: 28,
        FontSize: 18,
        TextColor: Colors.TextWhite,
        HorizontalAlign: TextAlignH.Left
    ));

    WidgetFactory.CreateDivider(world, section, $"{title}Divider", new DividerConfig(
        Thickness: 1,
        Color: new Vector4(0.4f, 0.4f, 0.45f, 0.5f),
        Margin: 5
    ));

    return section;
}

static void CreateSpacer(World world, Entity parent, float height)
{
    var spacer = world.Spawn()
        .With(new UIElement { Visible = true, RaycastTarget = false })
        .With(new UIRect
        {
            AnchorMin = Vector2.Zero,
            AnchorMax = Vector2.One,
            Pivot = new Vector2(0.5f, 0.5f),
            Size = new Vector2(0, height),
            WidthMode = UISizeMode.Fill,
            HeightMode = UISizeMode.Fixed
        })
        .Build();

    world.SetParent(spacer, parent);
}

static void CreateLabeledProgressBar(World world, Entity parent, string label, float value, Vector4 fillColor, FontHandle font)
{
    var row = WidgetFactory.CreatePanel(world, parent, $"Progress{label}Row", new PanelConfig(
        Height: 30,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 15
    ));

    WidgetFactory.CreateLabel(world, row, $"Progress{label}Label", label, font, new LabelConfig(
        Width: 120,
        Height: 24,
        FontSize: 13,
        TextColor: Colors.TextLight,
        HorizontalAlign: TextAlignH.Right
    ));

    WidgetFactory.CreateProgressBar(world, row, $"Progress{label}Bar", font, new ProgressBarConfig(
        Width: 400,
        Height: 16,
        Value: value,
        FillColor: fillColor,
        ShowLabel: true,
        CornerRadius: 8
    ));
}

static void CreateLabeledSlider(World world, Entity parent, string label, float value, Vector4 fillColor, FontHandle font, int tabIndex)
{
    var row = WidgetFactory.CreatePanel(world, parent, $"Slider{label}Row", new PanelConfig(
        Height: 35,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 15
    ));

    WidgetFactory.CreateLabel(world, row, $"Slider{label}Label", label, font, new LabelConfig(
        Width: 120,
        Height: 24,
        FontSize: 13,
        TextColor: Colors.TextLight,
        HorizontalAlign: TextAlignH.Right
    ));

    WidgetFactory.CreateSlider(world, row, $"Slider{label}", new SliderConfig(
        Width: 350,
        Height: 24,
        Value: value,
        FillColor: fillColor,
        TabIndex: tabIndex
    ));

    WidgetFactory.CreateLabel(world, row, $"Slider{label}Value", $"{value:0}%", font, new LabelConfig(
        Width: 50,
        Height: 24,
        FontSize: 13,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Left
    ));
}

static void CreateColoredPanel(World world, Entity parent, string label, Vector4 color, FontHandle font)
{
    var colorPanel = WidgetFactory.CreatePanel(world, parent, $"ColorPanel{label}", new PanelConfig(
        Width: 140,
        Height: 100,
        Direction: LayoutDirection.Vertical,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        BackgroundColor: color,
        CornerRadius: 8
    ));

    WidgetFactory.CreateLabel(world, colorPanel, $"ColorPanelLabel{label}", label, font, new LabelConfig(
        FontSize: 12,
        TextColor: Colors.TextWhite,
        HorizontalAlign: TextAlignH.Center
    ));
}

// ============================================================================
// Event Handling
// ============================================================================

static void SubscribeToUIEvents(World world)
{
    world.Subscribe<UIClickEvent>(e =>
    {
        var name = world.GetName(e.Element) ?? e.Element.ToString();
        Console.WriteLine($"[Click] {name}");
    });

    world.Subscribe<UIFocusGainedEvent>(e =>
    {
        var name = world.GetName(e.Element) ?? e.Element.ToString();
        Console.WriteLine($"[Focus] {name}");
    });
}

// ============================================================================
// Font Discovery
// ============================================================================

static string? FindSystemFont()
{
    string[] candidates =
    [
        @"C:\Windows\Fonts\segoeui.ttf",
        @"C:\Windows\Fonts\arial.ttf",
        @"C:\Windows\Fonts\calibri.ttf",
        @"C:\Windows\Fonts\verdana.ttf",
        @"C:\Windows\Fonts\tahoma.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/TTF/DejaVuSans.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
        "/Library/Fonts/Arial.ttf"
    ];

    foreach (var path in candidates)
    {
        if (File.Exists(path))
        {
            return path;
        }
    }

    return null;
}

// ============================================================================
// Color Palette
// ============================================================================

internal static class Colors
{
    // Primary button colors
    public static Vector4 Primary => new(0.25f, 0.47f, 0.85f, 1f);
    public static Vector4 PrimaryBorder => new(0.35f, 0.57f, 0.95f, 1f);

    // Success (green)
    public static Vector4 Success => new(0.22f, 0.65f, 0.35f, 1f);
    public static Vector4 SuccessBorder => new(0.32f, 0.75f, 0.45f, 1f);

    // Danger (red)
    public static Vector4 Danger => new(0.75f, 0.22f, 0.22f, 1f);
    public static Vector4 DangerBorder => new(0.85f, 0.32f, 0.32f, 1f);

    // Warning (orange/yellow)
    public static Vector4 Warning => new(0.85f, 0.55f, 0.15f, 1f);
    public static Vector4 WarningBorder => new(0.95f, 0.65f, 0.25f, 1f);

    // Info (purple)
    public static Vector4 Info => new(0.55f, 0.35f, 0.75f, 1f);
    public static Vector4 InfoBorder => new(0.65f, 0.45f, 0.85f, 1f);

    // Panel backgrounds
    public static Vector4 DarkPanel => new(0.12f, 0.12f, 0.16f, 0.98f);
    public static Vector4 MediumPanel => new(0.16f, 0.16f, 0.20f, 0.95f);
    public static Vector4 LightPanel => new(0.20f, 0.20f, 0.25f, 0.90f);

    // Text colors
    public static Vector4 TextWhite => new(1f, 1f, 1f, 1f);
    public static Vector4 TextLight => new(0.85f, 0.85f, 0.90f, 1f);
    public static Vector4 TextMuted => new(0.6f, 0.6f, 0.65f, 1f);
    public static Vector4 TextDark => new(0.15f, 0.15f, 0.15f, 1f);

    // Accent colors for sliders/progress
    public static Vector4 AccentBlue => new(0.3f, 0.6f, 0.95f, 1f);
    public static Vector4 AccentGreen => new(0.3f, 0.8f, 0.45f, 1f);
    public static Vector4 AccentOrange => new(0.95f, 0.6f, 0.2f, 1f);
    public static Vector4 AccentPurple => new(0.7f, 0.4f, 0.9f, 1f);
}
