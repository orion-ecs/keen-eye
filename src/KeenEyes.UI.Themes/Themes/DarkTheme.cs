using System.Numerics;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes.Themes;

/// <summary>
/// Built-in dark theme optimized for low-light environments.
/// </summary>
/// <remarks>
/// <para>
/// This theme uses a dark color palette that reduces eye strain in dim environments
/// and is suitable for extended use. Text is light on dark backgrounds.
/// </para>
/// <para>
/// The color scheme follows dark mode best practices with appropriate contrast ratios
/// for accessibility (WCAG 2.1 AA compliant).
/// </para>
/// </remarks>
public sealed class DarkTheme : ITheme
{
    /// <inheritdoc />
    public string Name => "Dark";

    /// <inheritdoc />
    public SystemTheme BaseTheme => SystemTheme.Dark;

    /// <inheritdoc />
    public ColorPalette Colors { get; } = new()
    {
        // Backgrounds
        Background = new Vector4(0.12f, 0.12f, 0.12f, 1f),        // #1E1E1E
        Surface = new Vector4(0.18f, 0.18f, 0.18f, 1f),           // #2D2D2D
        SurfaceElevated = new Vector4(0.22f, 0.22f, 0.22f, 1f),   // #383838

        // Brand colors (lighter variants for dark backgrounds)
        Primary = new Vector4(0.39f, 0.71f, 1f, 1f),              // #64B5F6
        PrimaryVariant = new Vector4(0.56f, 0.79f, 1f, 1f),       // #90CAF9
        Secondary = new Vector4(0.50f, 0.80f, 0.50f, 1f),         // #81C784
        Accent = new Vector4(0.31f, 0.76f, 0.97f, 1f),            // #4FC3F7

        // Text colors
        TextPrimary = new Vector4(0.93f, 0.93f, 0.93f, 1f),       // #EDEDED
        TextSecondary = new Vector4(0.70f, 0.70f, 0.70f, 1f),     // #B3B3B3
        TextDisabled = new Vector4(0.50f, 0.50f, 0.50f, 1f),      // #808080
        TextOnPrimary = new Vector4(0.13f, 0.13f, 0.13f, 1f),     // #212121

        // State colors (adjusted for dark backgrounds)
        Success = new Vector4(0.50f, 0.80f, 0.50f, 1f),           // #81C784
        Warning = new Vector4(1f, 0.76f, 0.28f, 1f),              // #FFC247
        Error = new Vector4(0.94f, 0.50f, 0.45f, 1f),             // #EF8073
        Info = new Vector4(0.39f, 0.71f, 1f, 1f),                 // #64B5F6

        // Borders
        Border = new Vector4(0.30f, 0.30f, 0.30f, 1f),            // #4D4D4D
        BorderFocused = new Vector4(0.39f, 0.71f, 1f, 1f),        // #64B5F6
        Divider = new Vector4(0.25f, 0.25f, 0.25f, 1f),           // #404040

        // Overlays
        HoverOverlay = new Vector4(1f, 1f, 1f, 0.05f),            // 5% white
        PressedOverlay = new Vector4(1f, 1f, 1f, 0.10f),          // 10% white
        DisabledOverlay = new Vector4(1f, 1f, 1f, 0.12f)          // 12% white
    };

    /// <inheritdoc />
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

    /// <inheritdoc />
    public UIStyle GetPanelStyle()
    {
        return new UIStyle
        {
            BackgroundColor = Colors.Surface,
            BorderColor = Colors.Border,
            BorderWidth = 1,
            CornerRadius = 4,
            Padding = new UIEdges(8, 8, 8, 8)
        };
    }

    /// <inheritdoc />
    public UIStyle GetInputStyle(UIInteractionState state)
    {
        var borderColor = state.HasFlag(UIInteractionState.Focused)
            ? Colors.BorderFocused
            : Colors.Border;

        return new UIStyle
        {
            BackgroundColor = Colors.Background,
            BorderColor = borderColor,
            BorderWidth = 1,
            CornerRadius = 4,
            Padding = new UIEdges(8, 6, 8, 6)
        };
    }

    /// <inheritdoc />
    public UIStyle GetMenuStyle()
    {
        return new UIStyle
        {
            BackgroundColor = Colors.SurfaceElevated,
            BorderColor = Colors.Border,
            BorderWidth = 1,
            CornerRadius = 4,
            Padding = new UIEdges(4, 4, 4, 4)
        };
    }

    /// <inheritdoc />
    public UIStyle GetMenuItemStyle(UIInteractionState state)
    {
        var bgColor = Vector4.Zero;

        if (state.HasFlag(UIInteractionState.Pressed))
        {
            bgColor = Colors.PressedOverlay;
        }
        else if (state.HasFlag(UIInteractionState.Hovered))
        {
            bgColor = Colors.HoverOverlay;
        }

        return new UIStyle
        {
            BackgroundColor = bgColor,
            BorderColor = Vector4.Zero,
            BorderWidth = 0,
            CornerRadius = 2,
            Padding = new UIEdges(12, 6, 12, 6)
        };
    }

    /// <inheritdoc />
    public UIStyle GetModalStyle()
    {
        return new UIStyle
        {
            BackgroundColor = Colors.SurfaceElevated,
            BorderColor = Colors.Border,
            BorderWidth = 1,
            CornerRadius = 8,
            Padding = new UIEdges(16, 16, 16, 16)
        };
    }

    /// <inheritdoc />
    public UIStyle GetScrollbarTrackStyle()
    {
        return new UIStyle
        {
            BackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1f),
            BorderColor = Vector4.Zero,
            BorderWidth = 0,
            CornerRadius = 4,
            Padding = UIEdges.Zero
        };
    }

    /// <inheritdoc />
    public UIStyle GetScrollbarThumbStyle(UIInteractionState state)
    {
        var color = new Vector4(0.4f, 0.4f, 0.4f, 1f);

        if (state.HasFlag(UIInteractionState.Pressed))
        {
            color = new Vector4(0.55f, 0.55f, 0.55f, 1f);
        }
        else if (state.HasFlag(UIInteractionState.Hovered))
        {
            color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        }

        return new UIStyle
        {
            BackgroundColor = color,
            BorderColor = Vector4.Zero,
            BorderWidth = 0,
            CornerRadius = 4,
            Padding = UIEdges.Zero
        };
    }

    /// <inheritdoc />
    public UIStyle GetTooltipStyle()
    {
        return new UIStyle
        {
            BackgroundColor = new Vector4(0.9f, 0.9f, 0.9f, 0.95f),
            BorderColor = Vector4.Zero,
            BorderWidth = 0,
            CornerRadius = 4,
            Padding = new UIEdges(8, 4, 8, 4)
        };
    }

    private static Vector4 BlendColor(Vector4 baseColor, Vector4 overlay)
    {
        return new Vector4(
            baseColor.X + (overlay.X - baseColor.X) * overlay.W,
            baseColor.Y + (overlay.Y - baseColor.Y) * overlay.W,
            baseColor.Z + (overlay.Z - baseColor.Z) * overlay.W,
            baseColor.W);
    }
}
