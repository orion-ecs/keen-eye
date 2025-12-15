# UI System Architecture

This document outlines the architecture for a retained-mode UI system in KeenEyes, where UI elements are ECS entities with specialized components.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Design Philosophy](#design-philosophy)
3. [Architecture Overview](#architecture-overview)
4. [Core Components](#core-components)
5. [Layout System](#layout-system)
6. [Event Handling](#event-handling)
7. [Rendering Integration](#rendering-integration)
8. [Implementation Plan](#implementation-plan)

---

## Executive Summary

KeenEyes UI will be a **retained-mode** system where UI elements are entities with components. This approach:
- Leverages existing ECS infrastructure (hierarchy, events, queries)
- Enables data-driven UI construction
- Supports serialization/prefabs for UI templates
- Provides familiar patterns for KeenEyes users

**Key Decision:** UI does NOT need a separate abstraction layer. It builds on `KeenEyes.Graphics.Abstractions` for rendering primitives (sprites, text, shapes).

---

## Design Philosophy

### Retained Mode vs Immediate Mode

| Aspect | Retained Mode (chosen) | Immediate Mode |
|--------|----------------------|----------------|
| State | UI elements persist as entities | Rebuilt every frame |
| Memory | Higher baseline, predictable | Lower baseline, spiky |
| Complexity | More upfront, less per-frame | Less upfront, more per-frame |
| ECS Fit | Natural fit | Awkward fit |
| Animation | Easy (component interpolation) | Manual tracking |
| Serialization | Built-in (entity persistence) | Custom solution |

### UI as Entities

```csharp
// A button is just an entity with UI components
var button = world.Spawn()
    .WithUIElement(new UIElement { Type = UIElementType.Button })
    .WithUITransform(new UITransform { ... })
    .WithUIText(new UIText { Content = "Click Me" })
    .WithUIStyle(new UIStyle { ... })
    .WithUIInteractable(new UIInteractable { ... })
    .Build();

// Parent-child via existing hierarchy
world.SetParent(buttonLabel, button);
```

---

## Architecture Overview

### Project Structure

```
KeenEyes.UI/
├── KeenEyes.UI.csproj
├── UIPlugin.cs                    # IWorldPlugin entry point
│
├── Components/
│   ├── UIElement.cs              # Base UI component (type, enabled, visible)
│   ├── UITransform.cs            # Position, size, anchors, pivot
│   ├── UIText.cs                 # Text content and formatting
│   ├── UIImage.cs                # Image/sprite display
│   ├── UIStyle.cs                # Colors, borders, backgrounds
│   ├── UIInteractable.cs         # Click, hover, focus state
│   ├── UILayout.cs               # Layout container settings
│   └── UIScrollable.cs           # Scroll view settings
│
├── Layout/
│   ├── ILayoutStrategy.cs        # Layout algorithm interface
│   ├── StackLayout.cs            # Vertical/horizontal stacking
│   ├── GridLayout.cs             # Grid-based layout
│   ├── FlexLayout.cs             # Flexbox-style layout
│   └── LayoutSystem.cs           # Processes layout each frame
│
├── Systems/
│   ├── UIInputSystem.cs          # Routes input to UI elements
│   ├── UILayoutSystem.cs         # Computes layout bounds
│   ├── UIRenderSystem.cs         # Submits UI to renderer
│   ├── UIAnimationSystem.cs      # Handles UI transitions
│   └── UIFocusSystem.cs          # Manages keyboard focus
│
├── Rendering/
│   ├── UIRenderer.cs             # Specialized UI batch renderer
│   ├── UIPrimitives.cs           # Rect, RoundedRect, Circle, Line
│   └── TextRenderer.cs           # Font rendering integration
│
└── Builders/
    ├── UIBuilder.cs              # Fluent API for creating UI
    └── StyleBuilder.cs           # Fluent API for styles
```

### Dependencies

```
KeenEyes.Abstractions
         ↑
KeenEyes.Graphics.Abstractions
         ↑
KeenEyes.Input.Abstractions
         ↑
    KeenEyes.UI
```

---

## Core Components

### UIElement - Base Component

```csharp
[Component]
public partial struct UIElement
{
    public UIElementType Type;
    public bool Enabled;
    public bool Visible;
    public int ZIndex;           // Render order within parent
    public string? Name;         // For debugging/lookup
}

public enum UIElementType
{
    Container,      // Invisible grouping
    Panel,          // Visible background
    Button,         // Clickable
    Label,          // Text display
    Image,          // Sprite display
    TextInput,      // Editable text
    Checkbox,       // Toggle
    Slider,         // Range input
    ScrollView,     // Scrollable container
    Dropdown,       // Selection list
    ProgressBar,    // Progress display
}
```

### UITransform - Position & Size

```csharp
[Component]
public partial struct UITransform
{
    // Position relative to anchor
    public Vector2 Position;

    // Size in pixels (or percentage if SizeMode is Relative)
    public Vector2 Size;
    public UISizeMode WidthMode;
    public UISizeMode HeightMode;

    // Anchor point on parent (0,0 = top-left, 1,1 = bottom-right)
    public Vector2 AnchorMin;
    public Vector2 AnchorMax;

    // Pivot point for rotation/scaling (0,0 = top-left, 0.5,0.5 = center)
    public Vector2 Pivot;

    // Margins from anchor edges
    public UIEdges Margin;
    public UIEdges Padding;

    // Computed by layout system
    public Rectangle ComputedBounds;
}

public enum UISizeMode
{
    Fixed,      // Absolute pixels
    Relative,   // Percentage of parent
    FitContent, // Size to content
    Fill,       // Fill available space
}

public readonly record struct UIEdges(float Top, float Right, float Bottom, float Left)
{
    public static UIEdges All(float value) => new(value, value, value, value);
    public static UIEdges Symmetric(float vertical, float horizontal)
        => new(vertical, horizontal, vertical, horizontal);
}
```

### UIText - Text Display

```csharp
[Component]
public partial struct UIText
{
    public string Content;           // Direct text OR
    public string? LocalizationKey;  // Key for localization lookup

    public string FontFamily;
    public float FontSize;
    public FontStyle Style;
    public Color Color;

    public TextAlignment HorizontalAlign;
    public TextAlignment VerticalAlign;

    public bool WordWrap;
    public TextOverflow Overflow;
}

public enum TextAlignment { Start, Center, End }
public enum TextOverflow { Visible, Hidden, Ellipsis }

[Flags]
public enum FontStyle
{
    Normal = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikethrough = 8
}
```

### UIStyle - Visual Appearance

```csharp
[Component]
public partial struct UIStyle
{
    // Background
    public Color BackgroundColor;
    public ITexture? BackgroundImage;
    public ImageScaleMode BackgroundScaleMode;

    // Border
    public Color BorderColor;
    public float BorderWidth;
    public float BorderRadius;       // Corner rounding

    // Effects
    public Color ShadowColor;
    public Vector2 ShadowOffset;
    public float ShadowBlur;

    // State variations (computed from base + state)
    public UIStateStyle? HoverStyle;
    public UIStateStyle? PressedStyle;
    public UIStateStyle? DisabledStyle;
    public UIStateStyle? FocusedStyle;
}

public record struct UIStateStyle
{
    public Color? BackgroundColor;
    public Color? BorderColor;
    public Color? TextColor;
    public float? Scale;
}
```

### UIInteractable - Input Handling

```csharp
[Component]
public partial struct UIInteractable
{
    public bool CanFocus;
    public bool CanClick;
    public bool CanDrag;

    // Current state (set by UIInputSystem)
    public UIInteractionState State;

    // Event flags (cleared each frame after processing)
    public UIEventFlags Events;
}

[Flags]
public enum UIInteractionState
{
    Normal = 0,
    Hovered = 1,
    Pressed = 2,
    Focused = 4,
    Disabled = 8,
    Dragging = 16
}

[Flags]
public enum UIEventFlags : uint
{
    None = 0,
    Clicked = 1 << 0,
    DoubleClicked = 1 << 1,
    RightClicked = 1 << 2,
    DragStarted = 1 << 3,
    DragEnded = 1 << 4,
    FocusGained = 1 << 5,
    FocusLost = 1 << 6,
    ValueChanged = 1 << 7,
    Submitted = 1 << 8,
}
```

---

## Layout System

### Layout Strategy Pattern

```csharp
public interface ILayoutStrategy
{
    void Calculate(Entity container, ReadOnlySpan<Entity> children, IWorld world);
}

[Component]
public partial struct UILayout
{
    public LayoutType Type;
    public LayoutDirection Direction;
    public float Spacing;
    public UIAlignment MainAxisAlign;
    public UIAlignment CrossAxisAlign;
    public bool WrapContent;
}

public enum LayoutType
{
    None,       // Manual positioning
    Stack,      // Linear stack
    Grid,       // Grid cells
    Flex,       // Flexbox
}

public enum LayoutDirection
{
    Horizontal,
    Vertical
}

public enum UIAlignment
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}
```

### Stack Layout Example

```csharp
public class StackLayoutStrategy : ILayoutStrategy
{
    public void Calculate(Entity container, ReadOnlySpan<Entity> children, IWorld world)
    {
        ref readonly var layout = ref world.Get<UILayout>(container);
        ref readonly var transform = ref world.Get<UITransform>(container);

        var bounds = transform.ComputedBounds;
        var padding = transform.Padding;
        var availableSpace = new Vector2(
            bounds.Width - padding.Left - padding.Right,
            bounds.Height - padding.Top - padding.Bottom
        );

        float offset = 0;
        bool isVertical = layout.Direction == LayoutDirection.Vertical;

        foreach (var child in children)
        {
            ref var childTransform = ref world.Get<UITransform>(child);

            var childSize = CalculateChildSize(childTransform, availableSpace);

            if (isVertical)
            {
                childTransform.ComputedBounds = new Rectangle(
                    bounds.X + padding.Left,
                    bounds.Y + padding.Top + offset,
                    childSize.X,
                    childSize.Y
                );
                offset += childSize.Y + layout.Spacing;
            }
            else
            {
                childTransform.ComputedBounds = new Rectangle(
                    bounds.X + padding.Left + offset,
                    bounds.Y + padding.Top,
                    childSize.X,
                    childSize.Y
                );
                offset += childSize.X + layout.Spacing;
            }
        }
    }
}
```

---

## Event Handling

### Hybrid Approach: Flags + ECS Messaging

UI events use two complementary patterns:

#### 1. Component Flags (Polling)

```csharp
// In a system, check for clicks
foreach (var entity in world.Query<UIInteractable>())
{
    ref var interactable = ref world.Get<UIInteractable>(entity);

    if (interactable.Events.HasFlag(UIEventFlags.Clicked))
    {
        // Handle click
        HandleButtonClick(entity);
    }
}
```

#### 2. ECS Messages (Event-Driven)

```csharp
// UI system sends typed messages
public readonly record struct UIClickedMessage(Entity Element, Vector2 Position);
public readonly record struct UIValueChangedMessage(Entity Element, object OldValue, object NewValue);
public readonly record struct UIFocusChangedMessage(Entity Element, bool HasFocus);

// Subscribe in your system
world.Subscribe<UIClickedMessage>(msg =>
{
    if (msg.Element == myButton)
    {
        StartGame();
    }
});
```

### Input Routing

```csharp
public class UIInputSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var input = World.GetExtension<IInputManager>();
        var mousePos = input.Source.MousePosition;

        // Find topmost element under cursor
        Entity? hoveredElement = FindElementAtPosition(mousePos);

        // Update hover states
        foreach (var entity in World.Query<UIInteractable>())
        {
            ref var interactable = ref World.Get<UIInteractable>(entity);

            bool wasHovered = interactable.State.HasFlag(UIInteractionState.Hovered);
            bool isHovered = entity == hoveredElement;

            if (isHovered && !wasHovered)
                interactable.State |= UIInteractionState.Hovered;
            else if (!isHovered && wasHovered)
                interactable.State &= ~UIInteractionState.Hovered;

            // Handle clicks
            if (isHovered && input.Source.IsMouseButtonPressed(MouseButton.Left))
            {
                interactable.State |= UIInteractionState.Pressed;
            }

            if (interactable.State.HasFlag(UIInteractionState.Pressed)
                && input.Source.IsMouseButtonReleased(MouseButton.Left))
            {
                interactable.State &= ~UIInteractionState.Pressed;

                if (isHovered)
                {
                    interactable.Events |= UIEventFlags.Clicked;
                    World.Send(new UIClickedMessage(entity, mousePos));
                }
            }
        }
    }

    private Entity? FindElementAtPosition(Vector2 position)
    {
        Entity? topmost = null;
        int highestZ = int.MinValue;

        foreach (var entity in World.Query<UIElement, UITransform, UIInteractable>())
        {
            ref readonly var element = ref World.Get<UIElement>(entity);
            ref readonly var transform = ref World.Get<UITransform>(entity);

            if (!element.Visible || !element.Enabled)
                continue;

            if (transform.ComputedBounds.Contains(position))
            {
                int z = CalculateGlobalZ(entity);
                if (z > highestZ)
                {
                    highestZ = z;
                    topmost = entity;
                }
            }
        }

        return topmost;
    }
}
```

---

## Rendering Integration

### UI Render System

```csharp
public class UIRenderSystem : SystemBase
{
    private readonly UIRenderer uiRenderer;

    public override void Update(float deltaTime)
    {
        var graphics = World.GetExtension<IGraphicsContext>();

        uiRenderer.Begin();

        // Render in Z-order
        foreach (var entity in GetSortedUIEntities())
        {
            ref readonly var element = ref World.Get<UIElement>(entity);
            if (!element.Visible) continue;

            ref readonly var transform = ref World.Get<UITransform>(entity);
            var bounds = transform.ComputedBounds;

            // Render background/border
            if (World.Has<UIStyle>(entity))
            {
                ref readonly var style = ref World.Get<UIStyle>(entity);
                RenderBackground(bounds, style);
            }

            // Render specific content
            switch (element.Type)
            {
                case UIElementType.Label:
                    RenderText(entity, bounds);
                    break;
                case UIElementType.Image:
                    RenderImage(entity, bounds);
                    break;
                case UIElementType.Button:
                    RenderButton(entity, bounds);
                    break;
                // ... etc
            }
        }

        uiRenderer.End();
    }
}
```

### 2D Primitives (Added to Graphics.Abstractions)

```csharp
// These primitives are needed by UI but useful for any 2D rendering

public interface IPrimitiveRenderer
{
    void DrawRect(Rectangle bounds, Color color);
    void DrawRoundedRect(Rectangle bounds, float radius, Color color);
    void DrawBorder(Rectangle bounds, float width, Color color);
    void DrawRoundedBorder(Rectangle bounds, float radius, float width, Color color);
    void DrawCircle(Vector2 center, float radius, Color color);
    void DrawLine(Vector2 start, Vector2 end, float width, Color color);
    void DrawText(string text, Vector2 position, Font font, float size, Color color);
}
```

---

## Implementation Plan

### Phase 1: Core Components & Layout

1. Create `KeenEyes.UI` project
2. Implement core components (UIElement, UITransform, UIStyle)
3. Implement UILayoutSystem with Stack layout
4. Basic rendering (rectangles, borders)

### Phase 2: Interaction & Events

1. Implement UIInputSystem
2. Add UIInteractable component
3. Implement focus management
4. Wire up click/hover events

### Phase 3: Widgets

1. Button with states
2. Label with text rendering
3. Image display
4. TextInput (basic)
5. Checkbox and Slider

### Phase 4: Advanced Features

1. ScrollView
2. Dropdown
3. Grid layout
4. Flex layout
5. UI animations/transitions

### Phase 5: Polish & Tooling

1. UIBuilder fluent API
2. Style inheritance/cascading
3. Theming system
4. Debug visualization

---

## Open Questions

1. **Text Rendering** - Use existing font library or integrate new one?
2. **9-Slice Sprites** - Support for scalable UI sprites?
3. **Localization Integration** - How tightly coupled with Localization system?
4. **Accessibility** - Screen reader support, keyboard navigation?
5. **UI Prefabs** - Special prefab handling for UI templates?
6. **Data Binding** - Automatic component ↔ UI synchronization?

---

## Related Issues

- Milestone #15: UI System
- Issue #416: Create KeenEyes.UI project with core components
- Issue #417: Implement UI layout system
- Issue #418: Implement UI input and event system
