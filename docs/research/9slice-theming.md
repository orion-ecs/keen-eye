# 9-Slice Theming Research

**Date:** 2024-12-20
**Status:** Research Complete
**Author:** Claude (AI Assistant)

## Overview

This document captures research findings for adding 9-slice rendering support and a theming system to the KeenEyes UI plugin.

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Border units | **Pixels** | Industry standard (Unity, CSS). Borders are fixed visual elements. |
| Texture dimensions | **Baked into TextureHandle** | No runtime queries, data is immutable after load |
| Edge/center fill | **Support both stretch and tile** | Tiling needed for repeating patterns |
| Theme storage | **World singleton** | Canvases are world-level, theme applies globally |
| Style inheritance | **Cascade from parent to child** | Like CSS, children inherit unless overridden |
| Theme hot-reload | **Immediate propagation** | Changes apply without explicit refresh |

## Current State Analysis

### Existing Infrastructure

| Component | Location | Status |
|-----------|----------|--------|
| `UIEdges` | `UI.Abstractions/Types/UIEdges.cs` | ✅ Ready for 9-slice borders |
| `ImageScaleMode.NineSlice` | `UI.Abstractions/Enums/ImageScaleMode.cs` | ✅ Defined (value 4), not implemented |
| `UIImage` component | `UI.Abstractions/Components/UIImage.cs` | Needs `SliceBorder` field |
| `UIRenderSystem` | `UI/Systems/UIRenderSystem.cs` | Needs 9-slice rendering logic |
| `TextureHandle` | `Graphics.Abstractions/Handles/TextureHandle.cs` | **Needs width/height** |
| Theme system | N/A | ❌ Does not exist |

### TextureHandle Change Required

Current:
```csharp
public readonly record struct TextureHandle(int Id)
{
    public static readonly TextureHandle Invalid = new(-1);
    public bool IsValid => Id >= 0;
}
```

Proposed:
```csharp
public readonly record struct TextureHandle(int Id, int Width, int Height)
{
    public static readonly TextureHandle Invalid = new(-1, 0, 0);
    public bool IsValid => Id >= 0;
}
```

**Impact:**
- `IGraphicsContext.CreateTexture()` / `LoadTexture()` already know dimensions
- All call sites that create handles need updating
- Handle size increases from 4 bytes to 12 bytes (acceptable for UI textures)
- Enables 9-slice border calculation without runtime texture queries

## Proposed Architecture

### Phase 1: Core 9-Slice Rendering

#### 1.1 Extend TextureHandle

```csharp
// Graphics.Abstractions/Handles/TextureHandle.cs
public readonly record struct TextureHandle(int Id, int Width, int Height)
{
    public static readonly TextureHandle Invalid = new(-1, 0, 0);
    public bool IsValid => Id >= 0;

    /// <summary>
    /// Gets the texture size as a vector.
    /// </summary>
    public Vector2 Size => new(Width, Height);
}
```

#### 1.2 Add SlicedFillMode Enum

```csharp
// UI.Abstractions/Enums/SlicedFillMode.cs
namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Defines how 9-slice edges and center regions are filled.
/// </summary>
public enum SlicedFillMode : byte
{
    /// <summary>
    /// Stretch regions to fill available space.
    /// </summary>
    Stretch = 0,

    /// <summary>
    /// Tile regions using original pixel size.
    /// </summary>
    Tile = 1
}
```

#### 1.3 Extend UIImage Component

```csharp
// UI.Abstractions/Components/UIImage.cs
public struct UIImage : IComponent
{
    public TextureHandle Texture;
    public Vector4 Tint;
    public ImageScaleMode ScaleMode;
    public Rectangle SourceRect;
    public bool PreserveAspect;

    // NEW: 9-slice support
    /// <summary>
    /// Border sizes for 9-slice rendering (in source texture pixels).
    /// Only used when <see cref="ScaleMode"/> is <see cref="ImageScaleMode.NineSlice"/>.
    /// </summary>
    public UIEdges SliceBorder;

    /// <summary>
    /// How to fill the center region when 9-slice rendering.
    /// </summary>
    public SlicedFillMode CenterFillMode;

    /// <summary>
    /// How to fill the edge regions when 9-slice rendering.
    /// </summary>
    public SlicedFillMode EdgeFillMode;

    // Factory method
    public static UIImage NineSlice(TextureHandle texture, UIEdges border) => new()
    {
        Texture = texture,
        Tint = Vector4.One,
        ScaleMode = ImageScaleMode.NineSlice,
        SourceRect = Rectangle.Empty,
        SliceBorder = border,
        CenterFillMode = SlicedFillMode.Stretch,
        EdgeFillMode = SlicedFillMode.Stretch
    };
}
```

#### 1.4 Implement 9-Slice Rendering

```csharp
// In UIRenderSystem.cs
private void RenderNineSlice(Rectangle bounds, in UIImage image)
{
    if (renderer2D is null || !image.Texture.IsValid)
        return;

    var tex = image.Texture;
    var border = image.SliceBorder;

    // Source regions (in pixels, convert to normalized UV)
    float texW = tex.Width;
    float texH = tex.Height;

    // Normalized border sizes
    float uLeft = border.Left / texW;
    float uRight = border.Right / texW;
    float vTop = border.Top / texH;
    float vBottom = border.Bottom / texH;

    // Destination border sizes (in screen pixels)
    float dLeft = border.Left;
    float dRight = border.Right;
    float dTop = border.Top;
    float dBottom = border.Bottom;

    // Clamp borders if destination is smaller than source borders
    float totalHorizontal = dLeft + dRight;
    float totalVertical = dTop + dBottom;

    if (totalHorizontal > bounds.Width)
    {
        float scale = bounds.Width / totalHorizontal;
        dLeft *= scale;
        dRight *= scale;
    }

    if (totalVertical > bounds.Height)
    {
        float scale = bounds.Height / totalVertical;
        dTop *= scale;
        dBottom *= scale;
    }

    // Calculate the 9 destination rectangles
    // Row 1: Top-left, Top-center, Top-right
    // Row 2: Middle-left, Center, Middle-right
    // Row 3: Bottom-left, Bottom-center, Bottom-right

    var destRects = new Rectangle[9];
    var srcRects = new Rectangle[9];

    // Calculate positions
    float x0 = bounds.X;
    float x1 = bounds.X + dLeft;
    float x2 = bounds.Right - dRight;
    float x3 = bounds.Right;

    float y0 = bounds.Y;
    float y1 = bounds.Y + dTop;
    float y2 = bounds.Bottom - dBottom;
    float y3 = bounds.Bottom;

    // UV coordinates
    float u0 = 0, u1 = uLeft, u2 = 1 - uRight, u3 = 1;
    float v0 = 0, v1 = vTop, v2 = 1 - vBottom, v3 = 1;

    // Define all 9 regions
    // Corners (fixed size)
    destRects[0] = new(x0, y0, dLeft, dTop);          // Top-left
    srcRects[0] = new(u0, v0, uLeft, vTop);

    destRects[2] = new(x2, y0, dRight, dTop);         // Top-right
    srcRects[2] = new(u2, v0, uRight, vTop);

    destRects[6] = new(x0, y2, dLeft, dBottom);       // Bottom-left
    srcRects[6] = new(u0, v2, uLeft, vBottom);

    destRects[8] = new(x2, y2, dRight, dBottom);      // Bottom-right
    srcRects[8] = new(u2, v2, uRight, vBottom);

    // Edges (stretch/tile one axis)
    destRects[1] = new(x1, y0, x2 - x1, dTop);        // Top
    srcRects[1] = new(u1, v0, u2 - u1, vTop);

    destRects[7] = new(x1, y2, x2 - x1, dBottom);     // Bottom
    srcRects[7] = new(u1, v2, u2 - u1, vBottom);

    destRects[3] = new(x0, y1, dLeft, y2 - y1);       // Left
    srcRects[3] = new(u0, v1, uLeft, v2 - v1);

    destRects[5] = new(x2, y1, dRight, y2 - y1);      // Right
    srcRects[5] = new(u2, v1, uRight, v2 - v1);

    // Center (stretch/tile both axes)
    destRects[4] = new(x1, y1, x2 - x1, y2 - y1);     // Center
    srcRects[4] = new(u1, v1, u2 - u1, v2 - v1);

    // Draw all 9 regions
    for (int i = 0; i < 9; i++)
    {
        if (destRects[i].Width > 0 && destRects[i].Height > 0)
        {
            // TODO: Handle tiling mode for edges (1,3,5,7) and center (4)
            renderer2D.DrawTextureRegion(image.Texture, destRects[i], srcRects[i], image.Tint);
        }
    }
}
```

### Phase 2: Theme System

#### 2.1 Theme Style Definition

```csharp
// UI.Abstractions/Theming/UIThemeStyle.cs
namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Defines visual properties for a named style in a theme.
/// </summary>
public sealed class UIThemeStyle
{
    public string Name { get; init; } = "";

    // Colors
    public Vector4? BackgroundColor { get; init; }
    public Vector4? BorderColor { get; init; }
    public Vector4? TextColor { get; init; }

    // Borders and corners
    public float? BorderWidth { get; init; }
    public float? CornerRadius { get; init; }
    public UIEdges? Padding { get; init; }

    // 9-slice background
    public TextureHandle? BackgroundTexture { get; init; }
    public UIEdges? SliceBorder { get; init; }
    public SlicedFillMode? CenterFillMode { get; init; }
    public SlicedFillMode? EdgeFillMode { get; init; }

    // State variants (hover, pressed, disabled, focused)
    public UIThemeStyle? Hovered { get; init; }
    public UIThemeStyle? Pressed { get; init; }
    public UIThemeStyle? Disabled { get; init; }
    public UIThemeStyle? Focused { get; init; }
}
```

#### 2.2 Theme Definition

```csharp
// UI.Abstractions/Theming/UITheme.cs
namespace KeenEyes.UI.Abstractions;

/// <summary>
/// A collection of named styles that define the visual appearance of UI elements.
/// </summary>
public sealed class UITheme
{
    private readonly Dictionary<string, UIThemeStyle> styles = new();

    public string Name { get; init; } = "Default";

    /// <summary>
    /// Registers a style in the theme.
    /// </summary>
    public UITheme WithStyle(string name, UIThemeStyle style)
    {
        styles[name] = style;
        return this;
    }

    /// <summary>
    /// Gets a style by name, or null if not found.
    /// </summary>
    public UIThemeStyle? GetStyle(string name)
        => styles.TryGetValue(name, out var style) ? style : null;

    /// <summary>
    /// Gets a style with variant (e.g., "button.primary").
    /// </summary>
    public UIThemeStyle? GetStyle(string name, string? variant)
    {
        if (!string.IsNullOrEmpty(variant))
        {
            var variantStyle = GetStyle($"{name}.{variant}");
            if (variantStyle is not null)
                return variantStyle;
        }
        return GetStyle(name);
    }
}
```

#### 2.3 Theme Class Component (Per-Element Override)

```csharp
// UI.Abstractions/Components/UIThemeClass.cs
namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that assigns a theme style class to a UI element.
/// </summary>
/// <remarks>
/// When present, the UIThemeSystem applies styles from the world's active theme.
/// If not present, the element inherits the style class from its parent.
/// </remarks>
public struct UIThemeClass : IComponent
{
    /// <summary>
    /// The style class name (e.g., "button", "panel", "input").
    /// </summary>
    public string ClassName;

    /// <summary>
    /// Optional variant modifier (e.g., "primary", "danger", "ghost").
    /// </summary>
    public string? Variant;

    /// <summary>
    /// If true, prevents style inheritance from parent elements.
    /// </summary>
    public bool BlockInheritance;
}
```

#### 2.4 Theme System (Applies Styles)

```csharp
// UI/Systems/UIThemeSystem.cs
namespace KeenEyes.UI;

/// <summary>
/// System that resolves and applies theme styles to UI elements.
/// </summary>
/// <remarks>
/// Runs before UIRenderSystem to ensure styles are applied before rendering.
/// Handles style inheritance from parent to child elements.
/// </remarks>
public sealed class UIThemeSystem : SystemBase
{
    private World? concreteWorld;
    private UITheme? activeTheme;

    protected override void OnInitialize()
    {
        concreteWorld = World as World;

        // Get theme from world singleton (or use default)
        if (World.TryGetSingleton<UITheme>(out var theme))
        {
            activeTheme = theme;
        }
    }

    public override void Update(float deltaTime)
    {
        if (concreteWorld is null || activeTheme is null)
            return;

        // Process all root canvases
        foreach (var root in World.Query<UIElement, UIRect, UIRootTag>())
        {
            ApplyThemeRecursive(root, inheritedClass: null, inheritedVariant: null);
        }
    }

    private void ApplyThemeRecursive(Entity entity, string? inheritedClass, string? inheritedVariant)
    {
        // Determine effective style class
        string? effectiveClass = inheritedClass;
        string? effectiveVariant = inheritedVariant;

        if (World.Has<UIThemeClass>(entity))
        {
            ref readonly var themeClass = ref World.Get<UIThemeClass>(entity);
            effectiveClass = themeClass.ClassName;
            effectiveVariant = themeClass.Variant;

            if (themeClass.BlockInheritance)
            {
                inheritedClass = null;
                inheritedVariant = null;
            }
        }

        // Apply style if we have a class
        if (!string.IsNullOrEmpty(effectiveClass))
        {
            var style = activeTheme!.GetStyle(effectiveClass, effectiveVariant);
            if (style is not null)
            {
                ApplyStyleToElement(entity, style);
            }
        }

        // Recurse to children (passing inheritance)
        var children = concreteWorld!.GetChildren(entity);
        foreach (var child in children)
        {
            if (World.Has<UIElement>(child))
            {
                ApplyThemeRecursive(child, effectiveClass, effectiveVariant);
            }
        }
    }

    private void ApplyStyleToElement(Entity entity, UIThemeStyle style)
    {
        // Only apply if element doesn't have explicit UIStyle override
        // (explicit UIStyle takes priority over theme)
        if (!World.Has<UIStyle>(entity))
        {
            // Create UIStyle from theme style
            var uiStyle = new UIStyle
            {
                BackgroundColor = style.BackgroundColor ?? Vector4.Zero,
                BorderColor = style.BorderColor ?? Vector4.Zero,
                BorderWidth = style.BorderWidth ?? 0,
                CornerRadius = style.CornerRadius ?? 0,
                Padding = style.Padding ?? UIEdges.Zero,
                BackgroundTexture = style.BackgroundTexture ?? TextureHandle.Invalid
            };
            World.Add(entity, uiStyle);
        }

        // Apply 9-slice image if specified
        if (style.BackgroundTexture.HasValue && style.SliceBorder.HasValue)
        {
            if (!World.Has<UIImage>(entity))
            {
                var image = UIImage.NineSlice(
                    style.BackgroundTexture.Value,
                    style.SliceBorder.Value);
                image.CenterFillMode = style.CenterFillMode ?? SlicedFillMode.Stretch;
                image.EdgeFillMode = style.EdgeFillMode ?? SlicedFillMode.Stretch;
                World.Add(entity, image);
            }
        }
    }
}
```

### Phase 3: Widget Factory Integration

```csharp
// Example: Updated ButtonConfig
public sealed class ButtonConfig
{
    // Existing color overrides (take priority over theme)
    public Vector4? BackgroundColor { get; init; }
    public Vector4? TextColor { get; init; }
    public Vector4? BorderColor { get; init; }

    // NEW: Theme class (defaults to "button")
    public string ThemeClass { get; init; } = "button";
    public string? ThemeVariant { get; init; }

    // ... rest of config
}

// In WidgetFactory.Button:
public static Entity Button(IWorld world, Entity parent, ButtonConfig? config = null)
{
    config ??= new ButtonConfig();

    var button = world.Spawn("Button")
        .WithParent(parent)
        .With(UIElement.Default)
        .With(UIRect.Default)
        .With(new UIThemeClass
        {
            ClassName = config.ThemeClass,
            Variant = config.ThemeVariant
        })
        .With(UIInteractable.Clickable())
        // ... rest of button setup
        .Build();

    // Apply explicit overrides AFTER theme (overrides take priority)
    if (config.BackgroundColor.HasValue)
    {
        world.Add(button, UIStyle.SolidColor(config.BackgroundColor.Value));
    }

    return button;
}
```

## Implementation Order

### Phase 1: Core 9-Slice (Foundation)
1. Extend `TextureHandle` with Width/Height
2. Update `IGraphicsContext.CreateTexture()` / `LoadTexture()` signatures
3. Update `SilkGraphicsContext` implementation
4. Update `MockGraphicsContext` for tests
5. Add `SlicedFillMode` enum
6. Extend `UIImage` with slice properties
7. Implement `RenderNineSlice()` in `UIRenderSystem`
8. Add unit tests for 9-slice rendering

### Phase 2: Theme System
1. Add `UIThemeStyle` class
2. Add `UITheme` class
3. Add `UIThemeClass` component
4. Implement `UIThemeSystem`
5. Register theme system in `UIPlugin`
6. Add theme as world singleton storage
7. Add unit tests for theme resolution

### Phase 3: Integration
1. Update `WidgetConfig` classes with theme class support
2. Update `WidgetFactory` methods to apply theme classes
3. Create default theme with sensible defaults
4. Add sample demonstrating themed UI
5. Documentation

## Open Questions (Resolved)

| Question | Resolution |
|----------|------------|
| Border units? | **Pixels** - industry standard |
| Store texture dimensions where? | **In TextureHandle** - baked at load time |
| Support tiling? | **Yes** - both stretch and tile modes |
| Theme storage level? | **World singleton** - canvases are world-level |
| Style inheritance? | **Cascade** - parent to child, child can override |
| Hot-reload? | **Yes** - theme changes propagate immediately |

## References

- [Unity 9-Slicing Manual](https://docs.unity3d.com/Manual/9SliceSprites.html)
- [9-Slice Scaling - Wikipedia](https://en.wikipedia.org/wiki/9-slice_scaling)
- [Unity UI Toolkit USS Properties](https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-USS-SupportedProperties.html)
