# UI Theming

The `KeenEyes.UI.Themes` library adds OS-aware theme detection and automatic style application to the `KeenEyes.UI` system, using contracts defined in `KeenEyes.UI.Themes.Abstractions`.

## Overview

A theme (`ITheme`) bundles a semantic `ColorPalette` with a set of methods that produce a `UIStyle` for each kind of widget (buttons, panels, inputs, menus, modals, scrollbars, tooltips). The `ThemePlugin` registers two built-in themes, "Light" and "Dark", detects the operating system's color scheme on Windows, macOS, and Linux, and can automatically switch between them when the OS preference changes. Individual UI entities opt in to automatic styling by attaching a `UIThemed` component; a system then keeps their `UIStyle` in sync with the active theme.

## Quick Start

### Installation

```csharp
using KeenEyes.UI;
using KeenEyes.UI.Themes;
using KeenEyes.UI.Themes.Abstractions;

using var world = new World();

// UI before Themes: the theme applicator styles UIStyle components
// created by the UI system's widgets.
world.InstallPlugin(new UIPlugin());
world.InstallPlugin(new ThemePlugin());

// Access the theme context extension
var theme = world.GetExtension<IThemeContext>();
```

Installing `ThemePlugin` does the following:

- Detects a platform-appropriate `ISystemThemeProvider` (Windows, macOS, or Linux; a no-op fallback otherwise) and uses it to pick the initial "Light" or "Dark" theme.
- Registers the built-in `LightTheme` and `DarkTheme` under the names `"Light"` and `"Dark"`.
- Exposes a `ThemeContext` as the `IThemeContext` world extension.
- Registers the `UIThemed` component.
- Adds `ThemeApplicatorSystem` to the `SystemPhase.LateUpdate` phase at `order: -20`, so it runs before layout/render systems in that phase.

## Core Concepts

### IThemeContext

`IThemeContext` (implemented by `ThemeContext`) is the entry point for working with themes at runtime. Retrieve it with `world.GetExtension<IThemeContext>()`.

```csharp
var theme = world.GetExtension<IThemeContext>();

// Inspect the active theme
var isDark = theme.CurrentTheme.BaseTheme == SystemTheme.Dark;

// Follow the OS preference automatically
theme.FollowSystemTheme = true;

// Or switch manually by name (also sets FollowSystemTheme to false)
theme.SetTheme("Dark");

// Register a custom theme (can reuse "Light"/"Dark" to override the built-ins)
theme.RegisterTheme("HighContrast", new MyHighContrastTheme());

// React to changes
world.Subscribe<ThemeChangedEvent>(e =>
    Console.WriteLine($"Theme changed to {e.NewTheme.Name}"));
```

Key members:

| Member | Description |
|--------|-------------|
| `CurrentTheme` | The active `ITheme`. |
| `SystemTheme` | The OS's current preference (`SystemTheme.Unknown`/`Light`/`Dark`/`HighContrast`). |
| `FollowSystemTheme` | When `true`, theme switches automatically follow OS changes. Setting a theme manually via `SetTheme` sets this back to `false`. |
| `SetTheme(ITheme)` / `SetTheme(string)` | Activates a theme directly, or by registered name (returns `false` if the name is unknown). |
| `RegisterTheme(string, ITheme)` | Registers a theme under a name; built-in `"Light"`/`"Dark"` names can be overridden. |
| `GetTheme(string)` | Looks up a registered theme, or `null`. |
| `RegisteredThemes` | All registered theme names. |
| `OnThemeChanged` | `Action<ThemeChangedEvent>` raised on theme changes (also broadcast on the world via `world.Send`). |

### ITheme and ColorPalette

`ITheme` exposes a `Name`, a `BaseTheme` (`SystemTheme.Light` or `SystemTheme.Dark`, used to pick correct contrast), a `Colors` palette, and one style-producing method per `UIComponentType`: `GetButtonStyle`, `GetPanelStyle`, `GetInputStyle`, `GetMenuStyle`, `GetMenuItemStyle`, `GetModalStyle`, `GetScrollbarTrackStyle`, `GetScrollbarThumbStyle`, and `GetTooltipStyle`. The interactive variants (button, input, menu item, scrollbar thumb) accept a `UIInteractionState` so the returned `UIStyle` can react to hover/press/focus.

`ColorPalette` is a set of semantic `Vector4` (RGBA, 0-1) colors rather than raw hex values, so themes stay legible regardless of light/dark base: `Background`, `Surface`, `SurfaceElevated`, `Primary`, `PrimaryVariant`, `Secondary`, `Accent`, `TextPrimary`, `TextSecondary`, `TextDisabled`, `TextOnPrimary`, `Success`, `Warning`, `Error`, `Info`, `Border`, `BorderFocused`, `Divider`, `HoverOverlay`, `PressedOverlay`, and `DisabledOverlay`.

The built-in `LightTheme` and `DarkTheme` (in `KeenEyes.UI.Themes.Themes`) show the expected pattern for a custom theme - a `ColorPalette` plus per-component style logic:

```csharp
public UIStyle GetButtonStyle(UIInteractionState state)
{
    var baseColor = Colors.Primary;

    if (state.HasFlag(UIInteractionState.Pressed))
    {
        baseColor = Colors.PrimaryVariant;
    }
    else if (state.HasFlag(UIInteractionState.Hovered))
    {
        baseColor = BlendColor(Colors.Primary, Colors.HoverOverlay);
    }

    return new UIStyle
    {
        BackgroundColor = baseColor,
        BorderColor = Vector4.Zero,
        BorderWidth = 0,
        CornerRadius = 4,
        Padding = new UIEdges(12, 8, 12, 8)
    };
}
```

To ship a custom theme, implement `ITheme` and register it with `theme.RegisterTheme("MyTheme", new MyTheme())`.

### UIThemed and ThemeApplicatorSystem

`UIThemed` is the marker component that opts a UI entity into automatic styling. It carries a `UIComponentType` that tells `ThemeApplicatorSystem` which `ITheme` method to call. Use the static factories (`UIThemed.Button`, `UIThemed.Panel`, `UIThemed.Input`, `UIThemed.Menu`, `UIThemed.MenuItem`, `UIThemed.Modal`, or `UIThemed.For(UIComponentType)` for the remaining types) when building an entity:

```csharp
using System.Numerics;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Themes;

var button = world.Spawn()
    .With(new UIElement { Visible = true, RaycastTarget = true })
    .With(UIRect.Fixed(x: 0, y: 0, width: 120, height: 32))
    .With(theme.CurrentTheme.GetButtonStyle(UIInteractionState.None))
    .With(new UIInteractable { CanClick = true, CanFocus = true })
    .With(UIThemed.Button)
    .Build();
```

Every frame, `ThemeApplicatorSystem` queries entities with both `UIStyle` and `UIThemed`, reads the entity's `UIInteractionState` from its `UIInteractable` component (if present), and overwrites `UIStyle` with the result of the matching `ITheme` method. Entities with a `UIDisabledTag` are skipped so their current style is preserved. Because the system runs in `SystemPhase.LateUpdate` at `order: -20`, styles are refreshed before later layout/render systems read them, and every entity picks up a theme switch on the very next frame without any manual re-styling.

### System Theme Detection

`ISystemThemeProvider` abstracts OS-level theme queries: `IsAvailable`, `SupportsRuntimeNotification`, `GetCurrentTheme()`, and an `OnThemeChanged` event for platforms that support live notifications. `ThemePlugin` selects a concrete provider (`WindowsThemeProvider`, `MacOSThemeProvider`, `LinuxThemeProvider`, or `FallbackThemeProvider` when the platform isn't recognized) and forwards its `SystemThemeChangedEvent` onto the world's messaging system whenever `IThemeContext.FollowSystemTheme` is enabled.

## Performance

`ThemeApplicatorSystem` only visits entities that have both `UIStyle` and `UIThemed`; UI elements that don't opt in to theming are never touched. Style updates just replace the `UIStyle` struct - since `UIStyle` is a plain-data component, this is a cheap per-entity write, not a re-layout, and only occurs while the theme is actively changing (the query still runs every `LateUpdate`, so keep the themed-entity count proportional to what's on screen).

## Next Steps

- [UI Guide](ui.md) - the retained-mode UI system, widgets, and `WidgetFactory`
- [Plugins Guide](plugins.md) - how `IWorldPlugin` installation, extensions, and system registration work
- [Systems Guide](systems.md) - system phases and ordering
- [9-Slice Theming Design](research/9slice-theming.md) - original exploratory design document (note: the shipped `ThemePlugin`/`ITheme` API described above supersedes the theme-system sketch in that document)
