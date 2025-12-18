# UI

KeenEyes provides an ECS-based retained-mode UI system where UI elements are entities with components. This enables flexible, data-driven user interfaces that integrate seamlessly with the rest of your game.

## Architecture Overview

The UI system follows the same abstraction pattern as graphics and input:

| Package | Purpose |
|---------|---------|
| `KeenEyes.UI.Abstractions` | Backend-agnostic components, events, and enums |
| `KeenEyes.UI` | Systems, UIContext, and WidgetFactory |

Key benefits of ECS-based UI:
- **Composition**: Build complex widgets from simple components
- **Query-able**: Use ECS queries to find and manipulate UI elements
- **Data-driven**: All UI state is in components, making serialization trivial
- **No wrapper classes**: Widgets are just entity/component patterns

## Quick Start

```csharp
using KeenEyes;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

using var world = new World();

// Install the UI plugin
world.InstallPlugin(new UIPlugin());

// Get the UI context extension
var ui = world.GetExtension<UIContext>();

// Create a root canvas
var canvas = ui.CreateCanvas("MainUI");

// Create a button using the widget factory
var font = new FontHandle(1);  // Your loaded font
var button = WidgetFactory.CreateButton(world, canvas, "Click Me", font);

// Subscribe to click events
world.Subscribe<UIClickEvent>(e =>
{
    if (e.Element == button)
        Console.WriteLine("Button clicked!");
});
```

## Plugin Installation

The `UIPlugin` registers all UI systems and exposes the `UIContext` extension:

```csharp
using var world = new World();

// Install required plugins
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new UIPlugin());

// Access the UI context
var ui = world.GetExtension<UIContext>();
```

### System Execution Order

| Phase | Order | System | Responsibility |
|-------|-------|--------|----------------|
| EarlyUpdate | 0 | UIInputSystem | Hit testing, hover/press states, click events |
| EarlyUpdate | 10 | UIFocusSystem | Tab navigation, keyboard focus |
| LateUpdate | -10 | UILayoutSystem | Calculate ComputedBounds for all elements |
| Render | 100 | UIRenderSystem | Draw UI via I2DRenderer/ITextRenderer |

## Core Components

### UIElement

Base component for all UI entities. Controls visibility and input interaction.

```csharp
public struct UIElement : IComponent
{
    public bool Visible;        // Whether the element is rendered
    public bool RaycastTarget;  // Whether the element receives pointer events
}

// Static factories
var element = UIElement.Default;          // Visible = true, RaycastTarget = true
var nonInteractive = UIElement.NonInteractive;  // Visible = true, RaycastTarget = false
```

### UIRect

Defines position and size using an anchor-based layout system.

```csharp
public struct UIRect : IComponent
{
    public Vector2 AnchorMin;   // Min anchor (0,0 = top-left, 1,1 = bottom-right)
    public Vector2 AnchorMax;   // Max anchor
    public Vector2 Pivot;       // Pivot point for positioning
    public UIEdges Offset;      // Pixel offset from anchor
    public Vector2 Size;        // Element size (used with Fixed mode)
    public UISizeMode WidthMode;   // How width is calculated
    public UISizeMode HeightMode;  // How height is calculated
    public Rectangle ComputedBounds;  // Read-only, set by layout system
    public short LocalZIndex;   // Render order within same level
}
```

**Anchor System Diagram:**

```
Parent Bounds
┌──────────────────────────────┐
│ (0,0)                  (1,0) │
│                              │
│      AnchorMin●───────●      │
│              │ Element │      │
│              │        │      │
│              ●───────●AnchorMax
│                              │
│ (0,1)                  (1,1) │
└──────────────────────────────┘
```

**Static Factories:**

```csharp
// Fill parent entirely
var stretch = UIRect.Stretch();

// Fixed size, centered in parent
var centered = UIRect.Centered(200, 100);

// Fixed position and size from top-left
var fixed = UIRect.Fixed(x: 10, y: 20, width: 150, height: 75);
```

### UIStyle

Defines visual appearance.

```csharp
public struct UIStyle : IComponent
{
    public Vector4 BackgroundColor;
    public TextureHandle BackgroundTexture;
    public Vector4 BorderColor;
    public float BorderWidth;
    public float CornerRadius;
    public UIEdges Padding;
}

// Static factories
var solid = UIStyle.SolidColor(new Vector4(0.2f, 0.4f, 0.8f, 1f));
var transparent = UIStyle.Transparent;
var bordered = UIStyle.BorderOnly(new Vector4(1, 1, 1, 1), width: 2f);
```

### UIText

Renders text content.

```csharp
public struct UIText : IComponent
{
    public string Content;
    public FontHandle Font;
    public float FontSize;
    public Vector4 Color;
    public TextAlignH HorizontalAlign;  // Left, Center, Right
    public TextAlignV VerticalAlign;    // Top, Middle, Bottom
    public bool WordWrap;
    public TextOverflow Overflow;       // Visible, Hidden, Ellipsis
}

// Static factories
var text = UIText.Create("Hello", font, fontSize: 16);
var centered = UIText.Centered("Title", font, fontSize: 24);
```

### UIImage

Renders an image or sprite.

```csharp
public struct UIImage : IComponent
{
    public TextureHandle Texture;
    public Vector4 Tint;
    public ImageScaleMode ScaleMode;  // Stretch, ScaleToFit, ScaleToFill, Tile, NineSlice
    public Rectangle SourceRect;      // For sprite atlases
    public bool PreserveAspect;
}

// Static factories
var image = UIImage.Create(textureHandle);
var stretched = UIImage.Stretch(textureHandle);
var sprite = UIImage.FromAtlas(textureHandle, new Rectangle(0, 0, 64, 64));
```

### UIInteractable

Enables user interaction.

```csharp
public struct UIInteractable : IComponent
{
    public bool CanFocus;     // Can receive keyboard focus
    public bool CanClick;     // Can be clicked
    public bool CanDrag;      // Can be dragged
    public int TabIndex;      // Keyboard navigation order
    public UIInteractionState State;     // Flags: Hovered, Pressed, Focused, Dragging
    public UIEventFlags PendingEvents;   // Events to process this frame
}

// Helper properties
bool isHovered = interactable.IsHovered;
bool isPressed = interactable.IsPressed;
bool isFocused = interactable.IsFocused;
bool isDragging = interactable.IsDragging;

// Check for specific event
bool wasClicked = interactable.HasEvent(UIEventFlags.Click);

// Static factories
var clickable = UIInteractable.Clickable();      // CanClick only
var button = UIInteractable.Button(tabIndex: 1); // CanClick + CanFocus
var draggable = UIInteractable.Draggable();      // CanDrag only
```

### UILayout

Enables flexbox-style layout for child elements.

```csharp
public struct UILayout : IComponent
{
    public LayoutDirection Direction;  // Horizontal or Vertical
    public LayoutAlign MainAxisAlign;  // Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly
    public LayoutAlign CrossAxisAlign; // Start, Center, End
    public float Spacing;              // Gap between children
    public bool Wrap;                  // Wrap to next line when full
    public bool ReverseOrder;          // Reverse child order
}

// Static factories
var horizontal = UILayout.Horizontal(spacing: 10);
var vertical = UILayout.Vertical(spacing: 5);
var hCentered = UILayout.HorizontalCentered(spacing: 8);
var vCentered = UILayout.VerticalCentered(spacing: 12);
```

**Layout Alignment Diagram:**

```
MainAxisAlign (Horizontal direction)
┌──────────────────────────────┐
│ Start    [A][B][C]           │
│ Center       [A][B][C]       │
│ End              [A][B][C]   │
│ SpaceBetween [A]   [B]   [C] │
│ SpaceAround  [A]  [B]  [C]   │
│ SpaceEvenly  [A]  [B]  [C]   │
└──────────────────────────────┘
```

### UIScrollable

Enables scrolling for content larger than the viewport.

```csharp
public struct UIScrollable : IComponent
{
    public bool HorizontalScroll;    // Allow horizontal scrolling
    public bool VerticalScroll;      // Allow vertical scrolling
    public Vector2 ScrollPosition;   // Current scroll offset (0-1 normalized)
    public Vector2 ContentSize;      // Size of scrollable content
    public float ScrollSensitivity;  // Mouse wheel sensitivity
}

// Static factories
var vertical = UIScrollable.Vertical();
var horizontal = UIScrollable.Horizontal();
var both = UIScrollable.Both();
```

## Tag Components

Tags are marker components with no data, used for filtering and state tracking.

| Tag | Purpose |
|-----|---------|
| `UIRootTag` | Marks canvas/root elements |
| `UIDisabledTag` | Element is visible but non-interactive |
| `UIHiddenTag` | Element is completely hidden (no layout space) |
| `UIFocusedTag` | Currently focused element |
| `UILayoutDirtyTag` | Layout needs recalculation |
| `UIClipChildrenTag` | Clip children to element bounds |

```csharp
// Check if element is disabled
if (world.Has<UIDisabledTag>(entity))
    return;

// Hide an element
world.Add(entity, new UIHiddenTag());

// Show an element
world.Remove<UIHiddenTag>(entity);
```

## Events

UI events are sent via the world's messaging system. Subscribe to receive them.

### Event Types

| Event | When Fired | Properties |
|-------|------------|------------|
| `UIClickEvent` | Element clicked | Element, Position, Button |
| `UIPointerEnterEvent` | Pointer enters bounds | Element, Position |
| `UIPointerExitEvent` | Pointer exits bounds | Element |
| `UIFocusGainedEvent` | Element gains focus | Element, Previous |
| `UIFocusLostEvent` | Element loses focus | Element, Next |
| `UIDragStartEvent` | Drag begins | Element, StartPosition |
| `UIDragEvent` | During drag | Element, Position, Delta |
| `UIDragEndEvent` | Drag ends | Element, EndPosition |
| `UIValueChangedEvent` | Value changes | Element, OldValue, NewValue |
| `UISubmitEvent` | Submit action | Element |

### Event Handling

**Using World Messaging (Recommended):**

```csharp
// Subscribe to click events
world.Subscribe<UIClickEvent>(e =>
{
    Console.WriteLine($"Clicked: {e.Element} at {e.Position}");
});

// Subscribe to drag events
world.Subscribe<UIDragEvent>(e =>
{
    Console.WriteLine($"Dragging: delta = {e.Delta}");
});

// Subscribe to focus changes
world.Subscribe<UIFocusGainedEvent>(e =>
{
    Console.WriteLine($"Focused: {e.Element}, previous: {e.Previous}");
});
```

**Polling in Systems:**

```csharp
public class UIReactionSystem : ISystem
{
    public void Update(float dt)
    {
        foreach (var entity in world.Query<UIInteractable>())
        {
            ref readonly var interactable = ref world.Get<UIInteractable>(entity);

            if (interactable.HasEvent(UIEventFlags.Click))
            {
                HandleClick(entity);
            }

            if (interactable.IsHovered && !previouslyHovered.Contains(entity))
            {
                HandleHoverStart(entity);
            }
        }
    }
}
```

## UIContext Extension

The `UIContext` provides focus management and canvas creation.

```csharp
var ui = world.GetExtension<UIContext>();

// Focus management
ui.RequestFocus(buttonEntity);    // Set focus to an element
ui.ClearFocus();                  // Remove focus from current element
Entity focused = ui.FocusedEntity; // Get currently focused element
bool hasFocus = ui.HasFocus;       // Check if anything is focused

// Canvas creation
var canvas = ui.CreateCanvas();           // Create anonymous canvas
var namedCanvas = ui.CreateCanvas("HUD"); // Create named canvas

// Layout management
ui.SetLayoutDirty(element);  // Mark element for layout recalculation
```

## Widget Factory

The `WidgetFactory` creates complete, multi-entity widgets using pure ECS composition.

### Button

```csharp
var button = WidgetFactory.CreateButton(
    world, parent, "Click Me", font);

// With custom config
var config = new ButtonConfig
{
    Width = 200,
    Height = 50,
    FontSize = 18,
    BackgroundColor = new Vector4(0.3f, 0.5f, 0.9f, 1f)
};
var customButton = WidgetFactory.CreateButton(
    world, parent, "Submit", font, config);

// Named button
var namedButton = WidgetFactory.CreateButton(
    world, parent, "SubmitBtn", "Submit", font);
```

### Panel

Container with layout support.

```csharp
var panel = WidgetFactory.CreatePanel(world, parent);

// With layout configuration
var config = new PanelConfig
{
    Direction = LayoutDirection.Vertical,
    Spacing = 10,
    Width = 400,
    Height = 300,
    BackgroundColor = new Vector4(0.1f, 0.1f, 0.1f, 0.9f)
};
var configuredPanel = WidgetFactory.CreatePanel(world, parent, config);
```

### Label

Non-interactive text display.

```csharp
var label = WidgetFactory.CreateLabel(world, parent, "Hello World", font);

var config = new LabelConfig
{
    FontSize = 24,
    TextColor = new Vector4(1, 1, 0, 1),
    HorizontalAlign = TextAlignH.Center
};
var titleLabel = WidgetFactory.CreateLabel(world, parent, "Title", font, config);
```

### TextField

Text input field.

```csharp
var textField = WidgetFactory.CreateTextField(world, parent, font);

var config = new TextFieldConfig
{
    PlaceholderText = "Enter your name...",
    Width = 250
};
var nameField = WidgetFactory.CreateTextField(world, parent, font, config);
```

### Checkbox

Toggle with label.

```csharp
var checkbox = WidgetFactory.CreateCheckbox(
    world, parent, "Accept Terms", font);

var config = new CheckboxConfig { IsChecked = true };
var checkedBox = WidgetFactory.CreateCheckbox(
    world, parent, "Enable Sound", font, config);
```

### Slider

Horizontal value slider.

```csharp
var slider = WidgetFactory.CreateSlider(world, parent);

var config = new SliderConfig
{
    MinValue = 0,
    MaxValue = 100,
    Value = 50,
    Width = 200
};
var volumeSlider = WidgetFactory.CreateSlider(world, parent, config);
```

### ProgressBar

Visual progress indicator.

```csharp
var config = new ProgressBarConfig
{
    MinValue = 0,
    MaxValue = 100,
    Value = 75,
    ShowLabel = true,
    Width = 300
};
var healthBar = WidgetFactory.CreateProgressBar(world, parent, font, config);
```

### Toggle

On/off switch.

```csharp
var toggle = WidgetFactory.CreateToggle(world, parent, "Dark Mode", font);

var config = new ToggleConfig { IsOn = true };
var enabledToggle = WidgetFactory.CreateToggle(
    world, parent, "Notifications", font, config);
```

### Dropdown

Selection list.

```csharp
var items = new[] { "Low", "Medium", "High", "Ultra" };
var dropdown = WidgetFactory.CreateDropdown(world, parent, items, font);

var config = new DropdownConfig { SelectedIndex = 2 };
var qualityDropdown = WidgetFactory.CreateDropdown(
    world, parent, items, font, config);
```

### TabView

Tabbed content panels.

```csharp
var tabs = new[]
{
    new TabConfig("General"),
    new TabConfig("Audio"),
    new TabConfig("Video")
};

var (tabView, contentPanels) = WidgetFactory.CreateTabView(
    world, parent, tabs, font);

// Add content to each tab panel
WidgetFactory.CreateLabel(world, contentPanels[0], "General settings...", font);
WidgetFactory.CreateLabel(world, contentPanels[1], "Audio settings...", font);
WidgetFactory.CreateLabel(world, contentPanels[2], "Video settings...", font);
```

### Divider

Visual separator.

```csharp
var divider = WidgetFactory.CreateDivider(world, parent);

var config = new DividerConfig
{
    Orientation = LayoutDirection.Vertical,
    Thickness = 2
};
var verticalDivider = WidgetFactory.CreateDivider(world, parent, config);
```

### ScrollView

Scrollable container.

```csharp
var config = new ScrollViewConfig
{
    ContentWidth = 800,
    ContentHeight = 1200,
    ShowVerticalScrollbar = true
};
var (scrollView, contentPanel) = WidgetFactory.CreateScrollView(
    world, parent, config);

// Add content to the scroll view
for (int i = 0; i < 50; i++)
{
    WidgetFactory.CreateLabel(world, contentPanel, $"Item {i}", font);
}
```

## Layout System Deep Dive

### Size Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| `Fixed` | Exact pixel size | Buttons, icons, fixed UI |
| `Fill` | Expand to available space | Full-width headers, stretching |
| `FitContent` | Size to fit children | Auto-sizing panels, text labels |
| `Percentage` | Percentage of parent size | Responsive columns |

### Anchor-Based Positioning

Anchors define how an element attaches to its parent:

```csharp
// Full stretch (fill parent)
rect.AnchorMin = new Vector2(0, 0);  // Top-left
rect.AnchorMax = new Vector2(1, 1);  // Bottom-right

// Centered point anchor
rect.AnchorMin = new Vector2(0.5f, 0.5f);
rect.AnchorMax = new Vector2(0.5f, 0.5f);

// Top-edge strip
rect.AnchorMin = new Vector2(0, 0);  // Top-left
rect.AnchorMax = new Vector2(1, 0);  // Top-right (spans width)

// Right-edge strip
rect.AnchorMin = new Vector2(1, 0);  // Top-right
rect.AnchorMax = new Vector2(1, 1);  // Bottom-right (spans height)
```

### Flexbox Layout

When a parent has `UILayout`, children are arranged automatically:

```csharp
// Vertical layout with centered items
var panel = world.Spawn()
    .With(UIElement.Default)
    .With(UIRect.Centered(300, 400))
    .With(UILayout.VerticalCentered(spacing: 10))
    .Build();

// Children will stack vertically with 10px gaps
WidgetFactory.CreateButton(world, panel, "Option 1", font);
WidgetFactory.CreateButton(world, panel, "Option 2", font);
WidgetFactory.CreateButton(world, panel, "Option 3", font);
```

## Input + UI Integration

### Focus and Keyboard Navigation

The UI system handles Tab navigation automatically via `UIFocusSystem`. Set `TabIndex` on interactable elements to control order:

```csharp
var config = new ButtonConfig { TabIndex = 1 };
var firstButton = WidgetFactory.CreateButton(world, parent, "First", font, config);

config.TabIndex = 2;
var secondButton = WidgetFactory.CreateButton(world, parent, "Second", font, config);
```

### Blocking Gameplay Input When UI Has Focus

```csharp
public class GameInputSystem : ISystem
{
    public void Update(float dt)
    {
        var ui = world.TryGetExtension<UIContext>(out var ctx) ? ctx : null;

        // Skip gameplay input if UI has focus
        if (ui?.HasFocus == true)
            return;

        // Process gameplay input
        ProcessGameplayInput();
    }
}
```

## Complete Example

A settings menu with multiple widget types:

```csharp
using KeenEyes;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

public static class SettingsMenu
{
    public static void Create(IWorld world, FontHandle font)
    {
        var ui = world.GetExtension<UIContext>();
        var canvas = ui.CreateCanvas("Settings");

        // Main panel
        var panelConfig = new PanelConfig
        {
            Width = 400,
            Height = 500,
            Direction = LayoutDirection.Vertical,
            Spacing = 15,
            BackgroundColor = new Vector4(0.1f, 0.1f, 0.15f, 0.95f)
        };
        var panel = WidgetFactory.CreatePanel(world, canvas, "SettingsPanel", panelConfig);

        // Title
        var titleConfig = new LabelConfig { FontSize = 28, TextColor = Vector4.One };
        WidgetFactory.CreateLabel(world, panel, "Settings", font, titleConfig);

        // Divider
        WidgetFactory.CreateDivider(world, panel);

        // Volume slider
        WidgetFactory.CreateLabel(world, panel, "Volume", font);
        var volumeConfig = new SliderConfig { MinValue = 0, MaxValue = 100, Value = 75 };
        var volumeSlider = WidgetFactory.CreateSlider(world, panel, "VolumeSlider", volumeConfig);

        // Fullscreen toggle
        var fullscreenToggle = WidgetFactory.CreateToggle(
            world, panel, "FullscreenToggle", "Fullscreen", font);

        // Graphics quality dropdown
        WidgetFactory.CreateLabel(world, panel, "Quality", font);
        var qualities = new[] { "Low", "Medium", "High", "Ultra" };
        var qualityConfig = new DropdownConfig { SelectedIndex = 2 };
        var qualityDropdown = WidgetFactory.CreateDropdown(
            world, panel, qualities, font, qualityConfig);

        // Buttons row
        var buttonRow = WidgetFactory.CreatePanel(world, panel, new PanelConfig
        {
            Direction = LayoutDirection.Horizontal,
            Spacing = 10,
            Height = 50
        });

        var applyBtn = WidgetFactory.CreateButton(world, buttonRow, "Apply", font);
        var cancelBtn = WidgetFactory.CreateButton(world, buttonRow, "Cancel", font);

        // Handle events
        world.Subscribe<UIClickEvent>(e =>
        {
            if (e.Element == applyBtn)
            {
                Console.WriteLine("Settings applied!");
            }
            else if (e.Element == cancelBtn)
            {
                Console.WriteLine("Settings cancelled.");
            }
        });

        world.Subscribe<UIValueChangedEvent>(e =>
        {
            if (e.Element == volumeSlider)
            {
                Console.WriteLine($"Volume: {e.NewValue}");
            }
        });
    }
}
```

## Troubleshooting

### Element Not Visible

1. **Check `Visible` flag**: Ensure `UIElement.Visible = true`
2. **Check parent visibility**: Hidden parents hide children
3. **Check `UIHiddenTag`**: Remove if present
4. **Check computed bounds**: Element may be positioned off-screen

### Click Not Working

1. **Check `RaycastTarget`**: Must be `true` on `UIElement`
2. **Check `UIInteractable`**: Ensure component exists with `CanClick = true`
3. **Check `UIDisabledTag`**: Remove if present
4. **Check z-order**: Another element may be on top

### Layout Not Updating

1. **Call `SetLayoutDirty`**: `ui.SetLayoutDirty(element)`
2. **Check `UILayoutDirtyTag`**: Should be present after changes
3. **Verify parent has `UILayout`**: Required for flexbox layout

### Focus Not Working

1. **Check `CanFocus`**: Must be `true` on `UIInteractable`
2. **Check `TabIndex`**: Should be set for tab navigation
3. **Verify element is alive**: Dead entities cannot be focused

## Dependencies

- **KeenEyes.UI.Abstractions** - Components, events, and enums
- **KeenEyes.UI** - Systems and widget factory
- **KeenEyes.Input.Abstractions** - For mouse button types
- **KeenEyes.Graphics.Abstractions** - For rendering types (Rectangle, FontHandle, TextureHandle)
