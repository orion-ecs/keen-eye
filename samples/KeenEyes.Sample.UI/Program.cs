// KeenEyes UI Widget Gallery
// Comprehensive demonstration of all WidgetFactory widgets.
//
// This sample showcases:
// - Basic widgets: Button, Panel, Label, TextField, Checkbox, Slider,
//   ProgressBar, Toggle, Dropdown, TabView, Divider, ScrollView
// - Advanced widgets: Splitter, Toolbar, StatusBar, TreeView, Accordion, PropertyGrid
// - Menu system: MenuBar with dropdowns
// - Tooltips on interactive elements
// - Multiple color schemes (primary, success, danger, warning, info)
// - Various font sizes and layout configurations
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
Console.WriteLine("Basic:    Button, Panel, Label, TextField, Checkbox, Slider");
Console.WriteLine("          ProgressBar, Toggle, Dropdown, TabView, Divider, ScrollView");
Console.WriteLine("Advanced: Splitter, Toolbar, StatusBar, TreeView, Accordion, PropertyGrid");
Console.WriteLine("Menus:    MenuBar with dropdowns, tooltips on hover");
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
        Width: 1100,
        Height: 750,
        Direction: LayoutDirection.Vertical,
        MainAxisAlign: LayoutAlign.Start,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 0,
        BackgroundColor: Colors.DarkPanel,
        CornerRadius: 12,
        Padding: UIEdges.All(10)
    ));

    // Center the main panel
    ref var mainRect = ref world.Get<UIRect>(mainPanel);
    mainRect.AnchorMin = new Vector2(0.5f, 0.5f);
    mainRect.AnchorMax = new Vector2(0.5f, 0.5f);
    mainRect.Pivot = new Vector2(0.5f, 0.5f);

    // Create MenuBar at the top
    CreateMenuBar(world, mainPanel, font);

    // Title
    WidgetFactory.CreateLabel(world, mainPanel, "Title", "KeenEyes UI Widget Gallery", font, new LabelConfig(
        Width: 1060,
        Height: 35,
        FontSize: 24,
        TextColor: Colors.TextWhite,
        HorizontalAlign: TextAlignH.Center
    ));

    // Subtitle
    WidgetFactory.CreateLabel(world, mainPanel, "Subtitle", "Showcasing all WidgetFactory widgets including advanced controls", font, new LabelConfig(
        Width: 1060,
        Height: 20,
        FontSize: 12,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Center
    ));

    // Spacing
    CreateSpacer(world, mainPanel, 8);

    // Create TabView with 6 tabs
    var tabs = new TabConfig[]
    {
        new("Controls", MinWidth: 90),
        new("Inputs", MinWidth: 90),
        new("Layout", MinWidth: 90),
        new("Splitter", MinWidth: 90),
        new("TreeView", MinWidth: 90),
        new("Inspector", MinWidth: 90)
    };

    var (tabView, contentPanels) = WidgetFactory.CreateTabView(
        world, mainPanel, "MainTabs", tabs, font,
        new TabViewConfig(
            Width: 1060,
            Height: 600,
            TabBarHeight: 40,
            TabSpacing: 4,
            TabBarColor: new Vector4(0.10f, 0.10f, 0.13f, 1f),
            ContentColor: Colors.MediumPanel,
            TabColor: new Vector4(0.14f, 0.14f, 0.18f, 1f),
            ActiveTabColor: Colors.MediumPanel,
            FontSize: 13
        ));

    // Populate Tab 1: Controls
    PopulateControlsTab(world, contentPanels[0], font);

    // Populate Tab 2: Inputs
    PopulateInputsTab(world, contentPanels[1], font);

    // Populate Tab 3: Layout
    PopulateLayoutTab(world, contentPanels[2], font);

    // Populate Tab 4: Splitter & Toolbar
    PopulateSplitterTab(world, contentPanels[3], font);

    // Populate Tab 5: TreeView & Accordion
    PopulateTreeViewTab(world, contentPanels[4], font);

    // Populate Tab 6: PropertyGrid
    PopulatePropertyGridTab(world, contentPanels[5], font);

    Console.WriteLine("Created widget gallery with TabView and all widget types");
}

// ============================================================================
// Tab 1: Controls (Buttons, Progress Bars, Sliders)
// ============================================================================

static void PopulateControlsTab(World world, Entity panel, FontHandle font)
{
    // Configure panel layout
    ref var layout = ref world.Get<UILayout>(panel);
    layout.Spacing = 15;
    layout.CrossAxisAlign = LayoutAlign.Center;
    world.Add(panel, new UIStyle { Padding = UIEdges.All(15) });

    // --- Buttons Section ---
    var buttonsSection = CreateSection(world, panel, "Buttons (with Tooltips)", font);

    // Row 1: Different colors with tooltips
    var colorRow = WidgetFactory.CreatePanel(world, buttonsSection, "ColorRow", new PanelConfig(
        Height: 50,
        Direction: LayoutDirection.Horizontal,
        MainAxisAlign: LayoutAlign.Center,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 15
    ));

    var primaryBtn = WidgetFactory.CreateButton(world, colorRow, "PrimaryBtn", "Primary", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Primary,
        BorderColor: Colors.PrimaryBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 1
    ));
    WidgetFactory.AddTooltip(world, primaryBtn, "Primary action button");

    var successBtn = WidgetFactory.CreateButton(world, colorRow, "SuccessBtn", "Success", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Success,
        BorderColor: Colors.SuccessBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 2
    ));
    WidgetFactory.AddTooltip(world, successBtn, "Confirms successful action");

    var dangerBtn = WidgetFactory.CreateButton(world, colorRow, "DangerBtn", "Danger", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Danger,
        BorderColor: Colors.DangerBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 3
    ));
    WidgetFactory.AddTooltip(world, dangerBtn, "Destructive action - use with caution!");

    var warningBtn = WidgetFactory.CreateButton(world, colorRow, "WarningBtn", "Warning", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Warning,
        BorderColor: Colors.WarningBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TextColor: Colors.TextDark,
        TabIndex: 4
    ));
    WidgetFactory.AddTooltip(world, warningBtn, "Requires attention before proceeding");

    var infoBtn = WidgetFactory.CreateButton(world, colorRow, "InfoBtn", "Info", font, new ButtonConfig(
        Width: 100, Height: 40,
        BackgroundColor: Colors.Info,
        BorderColor: Colors.InfoBorder,
        BorderWidth: 2,
        CornerRadius: 6,
        TabIndex: 5
    ));
    WidgetFactory.AddTooltip(world, infoBtn, "Shows additional information");

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

static Entity CreateSection(World world, Entity parent, string title, FontHandle font, float width = 1000)
{
    var section = WidgetFactory.CreatePanel(world, parent, $"{title}Section", new PanelConfig(
        Direction: LayoutDirection.Vertical,
        MainAxisAlign: LayoutAlign.Start,
        CrossAxisAlign: LayoutAlign.Center,
        Spacing: 8,
        BackgroundColor: Colors.LightPanel,
        CornerRadius: 8,
        Padding: UIEdges.All(12)
    ));

    ref var sectionRect = ref world.Get<UIRect>(section);
    sectionRect.Size = new Vector2(width, 0);
    sectionRect.WidthMode = UISizeMode.Fixed;
    sectionRect.HeightMode = UISizeMode.FitContent;

    // Section title
    WidgetFactory.CreateLabel(world, section, $"{title}Title", title, font, new LabelConfig(
        Width: width - 30,
        Height: 24,
        FontSize: 16,
        TextColor: Colors.TextWhite,
        HorizontalAlign: TextAlignH.Left
    ));

    WidgetFactory.CreateDivider(world, section, $"{title}Divider", new DividerConfig(
        Thickness: 1,
        Color: new Vector4(0.4f, 0.4f, 0.45f, 0.5f),
        Margin: 3
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
// MenuBar Creation
// ============================================================================

static void CreateMenuBar(World world, Entity parent, FontHandle font)
{
    var menuDefs = new (string Label, MenuItemDef[] Items)[]
    {
        ("File", [
            new MenuItemDef("New", "new", Shortcut: "Ctrl+N"),
            new MenuItemDef("Open", "open", Shortcut: "Ctrl+O"),
            new MenuItemDef("Save", "save", Shortcut: "Ctrl+S"),
            new MenuItemDef("---", "sep1", IsSeparator: true),
            new MenuItemDef("Exit", "exit", Shortcut: "Alt+F4")
        ]),
        ("Edit", [
            new MenuItemDef("Undo", "undo", Shortcut: "Ctrl+Z"),
            new MenuItemDef("Redo", "redo", Shortcut: "Ctrl+Y"),
            new MenuItemDef("---", "sep2", IsSeparator: true),
            new MenuItemDef("Cut", "cut", Shortcut: "Ctrl+X"),
            new MenuItemDef("Copy", "copy", Shortcut: "Ctrl+C"),
            new MenuItemDef("Paste", "paste", Shortcut: "Ctrl+V")
        ]),
        ("View", [
            new MenuItemDef("Zoom In", "zoom_in", Shortcut: "Ctrl++"),
            new MenuItemDef("Zoom Out", "zoom_out", Shortcut: "Ctrl+-"),
            new MenuItemDef("Reset Zoom", "zoom_reset", Shortcut: "Ctrl+0"),
            new MenuItemDef("---", "sep3", IsSeparator: true),
            new MenuItemDef("Fullscreen", "fullscreen", Shortcut: "F11")
        ]),
        ("Help", [
            new MenuItemDef("Documentation", "docs", Shortcut: "F1"),
            new MenuItemDef("About", "about")
        ])
    };

    WidgetFactory.CreateMenuBar(world, parent, font,
        menuDefs.Select(m => (m.Label, (IEnumerable<MenuItemDef>)m.Items)),
        new MenuBarConfig(
        Height: 28,
        BackgroundColor: new Vector4(0.15f, 0.15f, 0.18f, 1f),
        ItemColor: new Vector4(0.15f, 0.15f, 0.18f, 1f),
        ItemHoverColor: new Vector4(0.25f, 0.25f, 0.30f, 1f),
        TextColor: Colors.TextLight,
        FontSize: 13
    ));
}

// ============================================================================
// Tab 4: Splitter & Toolbar
// ============================================================================

static void PopulateSplitterTab(World world, Entity panel, FontHandle font)
{
    ref var layout = ref world.Get<UILayout>(panel);
    layout.Spacing = 15;
    layout.CrossAxisAlign = LayoutAlign.Center;
    world.Add(panel, new UIStyle { Padding = UIEdges.All(15) });

    // --- Toolbar Section ---
    var toolbarSection = CreateSection(world, panel, "Toolbar (Icon buttons)", font);

    // Toolbar uses ToolbarItemDef.Button and ToolbarItemDef.Separator
    var toolbarItems = new ToolbarItemDef[]
    {
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "New file")),
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Open file")),
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Save file")),
        new ToolbarItemDef.Separator(),
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Cut")),
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Copy")),
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Paste")),
        new ToolbarItemDef.Separator(),
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Undo")),
        new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Redo")),
        new ToolbarItemDef.Separator(),
        new ToolbarItemDef.Button(new ToolbarButtonDef(IsToggle: true, Tooltip: "Bold")),
        new ToolbarItemDef.Button(new ToolbarButtonDef(IsToggle: true, Tooltip: "Italic"))
    };

    WidgetFactory.CreateToolbar(world, toolbarSection, toolbarItems, new ToolbarConfig(
        BackgroundColor: new Vector4(0.18f, 0.18f, 0.22f, 1f),
        ButtonColor: new Vector4(0.22f, 0.22f, 0.26f, 1f),
        ButtonHoverColor: new Vector4(0.28f, 0.28f, 0.32f, 1f),
        ButtonSize: 32
    ));

    // --- Splitter Section ---
    var splitterSection = CreateSection(world, panel, "Splitter (Drag the divider)", font);

    // Create a container for the splitter with fixed height
    var splitterContainer = WidgetFactory.CreatePanel(world, splitterSection, "SplitterContainer", new PanelConfig(
        Width: 950,
        Height: 180,
        BackgroundColor: Colors.DarkPanel
    ));

    var (_, leftPane, rightPane) = WidgetFactory.CreateSplitter(
        world, splitterContainer,
        new SplitterConfig(
            Orientation: LayoutDirection.Horizontal,
            InitialRatio: 0.4f,
            HandleSize: 6,
            MinFirstPane: 100,
            MinSecondPane: 100,
            HandleColor: new Vector4(0.35f, 0.35f, 0.40f, 1f),
            HandleHoverColor: Colors.AccentBlue
        ));

    // Configure left pane layout
    ref var leftLayout = ref world.Get<UILayout>(leftPane);
    leftLayout.Direction = LayoutDirection.Vertical;
    leftLayout.MainAxisAlign = LayoutAlign.Center;
    leftLayout.CrossAxisAlign = LayoutAlign.Center;

    // Add content to left pane
    WidgetFactory.CreateLabel(world, leftPane, "LeftPaneLabel", "Left Pane", font, new LabelConfig(
        FontSize: 14,
        TextColor: Colors.TextWhite,
        HorizontalAlign: TextAlignH.Center
    ));
    WidgetFactory.CreateLabel(world, leftPane, "LeftPaneDesc", "Drag the handle to resize", font, new LabelConfig(
        FontSize: 11,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Center
    ));

    // Configure right pane layout
    ref var rightLayout = ref world.Get<UILayout>(rightPane);
    rightLayout.Direction = LayoutDirection.Vertical;
    rightLayout.MainAxisAlign = LayoutAlign.Center;
    rightLayout.CrossAxisAlign = LayoutAlign.Center;

    // Add content to right pane
    WidgetFactory.CreateLabel(world, rightPane, "RightPaneLabel", "Right Pane", font, new LabelConfig(
        FontSize: 14,
        TextColor: Colors.TextWhite,
        HorizontalAlign: TextAlignH.Center
    ));
    WidgetFactory.CreateLabel(world, rightPane, "RightPaneDesc", "Content adjusts automatically", font, new LabelConfig(
        FontSize: 11,
        TextColor: Colors.TextMuted,
        HorizontalAlign: TextAlignH.Center
    ));

    // --- StatusBar Section ---
    var statusSection = CreateSection(world, panel, "StatusBar", font);

    var statusSections = new StatusBarSectionDef[]
    {
        new("Ready", IsFlexible: true),
        new("Line: 42, Col: 15", Width: 120),
        new("UTF-8", Width: 60),
        new("LF", Width: 40),
        new("Spaces: 4", Width: 70)
    };

    WidgetFactory.CreateStatusBar(world, statusSection, font, statusSections, new StatusBarConfig(
        Height: 24,
        BackgroundColor: new Vector4(0.12f, 0.12f, 0.15f, 1f),
        TextColor: Colors.TextMuted,
        SeparatorColor: new Vector4(0.3f, 0.3f, 0.35f, 1f),
        FontSize: 11
    ));
}

// ============================================================================
// Tab 5: TreeView & Accordion
// ============================================================================

static void PopulateTreeViewTab(World world, Entity panel, FontHandle font)
{
    ref var layout = ref world.Get<UILayout>(panel);
    layout.Spacing = 15;
    layout.Direction = LayoutDirection.Horizontal;
    layout.CrossAxisAlign = LayoutAlign.Start;
    world.Add(panel, new UIStyle { Padding = UIEdges.All(15) });

    // Left side: TreeView
    var treeSection = CreateSection(world, panel, "TreeView (File Browser)", font, 480);

    var treeNodes = new TreeNodeDef[]
    {
        new("Project", IsExpanded: true, Children: [
            new("src", IsExpanded: true, Children: [
                new("Components", Children: [
                    new("Button.cs"),
                    new("Panel.cs"),
                    new("Label.cs")
                ]),
                new("Systems", Children: [
                    new("InputSystem.cs"),
                    new("RenderSystem.cs")
                ]),
                new("Program.cs")
            ]),
            new("assets", Children: [
                new("textures", Children: [
                    new("icon.png"),
                    new("background.png")
                ]),
                new("fonts", Children: [
                    new("default.ttf")
                ])
            ]),
            new("README.md"),
            new("project.csproj")
        ])
    };

    WidgetFactory.CreateTreeViewWithNodes(world, treeSection, font, treeNodes, new TreeViewConfig(
        Width: 450,
        Height: 420,
        IndentSize: 18,
        RowHeight: 24,
        BackgroundColor: Colors.DarkPanel,
        SelectedColor: Colors.Primary,
        HoverColor: new Vector4(0.25f, 0.25f, 0.30f, 1f),
        FontSize: 12
    ));

    // Right side: Accordion
    var accordionSection = CreateSection(world, panel, "Accordion (Collapsible)", font, 480);

    var accordionSections = new AccordionSectionDef[]
    {
        new("General Settings", IsExpanded: true),
        new("Display Options"),
        new("Audio Settings"),
        new("Controls")
    };

    var (accordion, sectionEntities) = WidgetFactory.CreateAccordionWithSections(
        world, accordionSection, font, accordionSections,
        new AccordionConfig(
            Width: 450,
            Height: 420,
            AllowMultipleExpanded: false,
            HeaderHeight: 36,
            Spacing: 2,
            BackgroundColor: Colors.DarkPanel,
            HeaderColor: new Vector4(0.22f, 0.22f, 0.26f, 1f),
            HeaderHoverColor: new Vector4(0.28f, 0.28f, 0.32f, 1f),
            ContentColor: new Vector4(0.16f, 0.16f, 0.20f, 1f),
            FontSize: 13
        ));

    // Add content to each accordion section
    if (sectionEntities.Length > 0)
    {
        ref var section0 = ref world.Get<UIAccordionSection>(sectionEntities[0]);
        if (section0.ContentContainer.IsValid)
        {
            WidgetFactory.CreateLabel(world, section0.ContentContainer, "GeneralLabel",
                "Application Name: KeenEyes Demo", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
            WidgetFactory.CreateLabel(world, section0.ContentContainer, "VersionLabel",
                "Version: 1.0.0", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
        }
    }

    if (sectionEntities.Length > 1)
    {
        ref var section1 = ref world.Get<UIAccordionSection>(sectionEntities[1]);
        if (section1.ContentContainer.IsValid)
        {
            WidgetFactory.CreateLabel(world, section1.ContentContainer, "DisplayLabel",
                "Resolution: 1920x1080", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
            WidgetFactory.CreateLabel(world, section1.ContentContainer, "RefreshLabel",
                "Refresh Rate: 60 Hz", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
        }
    }

    if (sectionEntities.Length > 2)
    {
        ref var section2 = ref world.Get<UIAccordionSection>(sectionEntities[2]);
        if (section2.ContentContainer.IsValid)
        {
            WidgetFactory.CreateLabel(world, section2.ContentContainer, "AudioLabel",
                "Master Volume: 80%", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
            WidgetFactory.CreateLabel(world, section2.ContentContainer, "MusicLabel",
                "Music Volume: 60%", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
        }
    }

    if (sectionEntities.Length > 3)
    {
        ref var section3 = ref world.Get<UIAccordionSection>(sectionEntities[3]);
        if (section3.ContentContainer.IsValid)
        {
            WidgetFactory.CreateLabel(world, section3.ContentContainer, "ControlsLabel",
                "Mouse Sensitivity: 5.0", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
            WidgetFactory.CreateLabel(world, section3.ContentContainer, "InvertLabel",
                "Invert Y-Axis: No", font, new LabelConfig(FontSize: 12, TextColor: Colors.TextLight));
        }
    }
}

// ============================================================================
// Tab 6: PropertyGrid
// ============================================================================

static void PopulatePropertyGridTab(World world, Entity panel, FontHandle font)
{
    ref var layout = ref world.Get<UILayout>(panel);
    layout.Spacing = 15;
    layout.CrossAxisAlign = LayoutAlign.Center;
    world.Add(panel, new UIStyle { Padding = UIEdges.All(15) });

    // --- PropertyGrid Section ---
    var propertySection = CreateSection(world, panel, "PropertyGrid (Inspector)", font);

    var categories = new PropertyCategoryDef[]
    {
        new("Transform", Properties: [
            new PropertyDef("Position X", Type: PropertyType.Float, InitialValue: "0.0"),
            new PropertyDef("Position Y", Type: PropertyType.Float, InitialValue: "0.0"),
            new PropertyDef("Position Z", Type: PropertyType.Float, InitialValue: "0.0"),
            new PropertyDef("Rotation", Type: PropertyType.Float, InitialValue: "0.0"),
            new PropertyDef("Scale", Type: PropertyType.Float, InitialValue: "1.0")
        ]),
        new("Appearance", Properties: [
            new PropertyDef("Visible", Type: PropertyType.Bool, InitialValue: "true"),
            new PropertyDef("Opacity", Type: PropertyType.Float, InitialValue: "1.0"),
            new PropertyDef("Tint Color", Type: PropertyType.String, InitialValue: "#FFFFFF"),
            new PropertyDef("Sprite", Type: PropertyType.String, InitialValue: "default.png")
        ]),
        new("Physics", Properties: [
            new PropertyDef("Is Static", Type: PropertyType.Bool, InitialValue: "false"),
            new PropertyDef("Mass", Type: PropertyType.Float, InitialValue: "1.0"),
            new PropertyDef("Friction", Type: PropertyType.Float, InitialValue: "0.5"),
            new PropertyDef("Bounciness", Type: PropertyType.Float, InitialValue: "0.0")
        ]),
        new("Script", Properties: [
            new PropertyDef("Script Name", Type: PropertyType.String, InitialValue: "PlayerController"),
            new PropertyDef("Update Rate", Type: PropertyType.Int, InitialValue: "60"),
            new PropertyDef("Enabled", Type: PropertyType.Bool, InitialValue: "true")
        ])
    };

    WidgetFactory.CreatePropertyGridWithCategories(world, propertySection, font, categories,
        new PropertyGridConfig(
            Width: 950,
            Height: 450,
            LabelWidthRatio: 0.35f,
            RowHeight: 28,
            CategoryHeight: 32,
            BackgroundColor: Colors.DarkPanel,
            CategoryColor: new Vector4(0.20f, 0.20f, 0.24f, 1f),
            RowAlternateColor: new Vector4(0.16f, 0.16f, 0.20f, 1f),
            LabelColor: Colors.TextLight,
            ValueColor: Colors.TextWhite,
            FontSize: 12
        ));
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
