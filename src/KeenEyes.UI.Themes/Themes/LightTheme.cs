using System.Numerics;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes.Themes;

/// <summary>
/// Built-in light theme with a clean, modern appearance.
/// </summary>
/// <remarks>
/// <para>
/// This theme uses a light color palette suitable for well-lit environments.
/// Text is dark on light backgrounds for optimal readability.
/// </para>
/// <para>
/// The color scheme is based on Material Design guidelines with some customizations
/// for game UI contexts.
/// </para>
/// </remarks>
public sealed class LightTheme : ITheme
{
    /// <inheritdoc />
    public string Name => "Light";

    /// <inheritdoc />
    public SystemTheme BaseTheme => SystemTheme.Light;

    /// <inheritdoc />
    public ColorPalette Colors { get; } = new()
    {
        // Backgrounds
        Background = new Vector4(0.96f, 0.96f, 0.96f, 1f),       // #F5F5F5
        Surface = new Vector4(1f, 1f, 1f, 1f),                    // #FFFFFF
        SurfaceElevated = new Vector4(1f, 1f, 1f, 1f),            // #FFFFFF

        // Brand colors
        Primary = new Vector4(0.13f, 0.59f, 0.95f, 1f),           // #2196F3
        PrimaryVariant = new Vector4(0.10f, 0.46f, 0.82f, 1f),    // #1976D2
        Secondary = new Vector4(0.30f, 0.69f, 0.31f, 1f),         // #4CAF50
        Accent = new Vector4(0.01f, 0.66f, 0.96f, 1f),            // #03A9F4

        // Text colors
        TextPrimary = new Vector4(0.13f, 0.13f, 0.13f, 1f),       // #212121
        TextSecondary = new Vector4(0.46f, 0.46f, 0.46f, 1f),     // #757575
        TextDisabled = new Vector4(0.62f, 0.62f, 0.62f, 1f),      // #9E9E9E
        TextOnPrimary = new Vector4(1f, 1f, 1f, 1f),              // #FFFFFF

        // State colors
        Success = new Vector4(0.30f, 0.69f, 0.31f, 1f),           // #4CAF50
        Warning = new Vector4(1f, 0.60f, 0f, 1f),                 // #FF9800
        Error = new Vector4(0.96f, 0.26f, 0.21f, 1f),             // #F44336
        Info = new Vector4(0.13f, 0.59f, 0.95f, 1f),              // #2196F3

        // Borders
        Border = new Vector4(0.88f, 0.88f, 0.88f, 1f),            // #E0E0E0
        BorderFocused = new Vector4(0.13f, 0.59f, 0.95f, 1f),     // #2196F3
        Divider = new Vector4(0.88f, 0.88f, 0.88f, 1f),           // #E0E0E0

        // Overlays
        HoverOverlay = new Vector4(0f, 0f, 0f, 0.04f),            // 4% black
        PressedOverlay = new Vector4(0f, 0f, 0f, 0.08f),          // 8% black
        DisabledOverlay = new Vector4(0f, 0f, 0f, 0.12f)          // 12% black
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
            BackgroundColor = Colors.Surface,
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
            BackgroundColor = Colors.Surface,
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
            BackgroundColor = new Vector4(0.9f, 0.9f, 0.9f, 1f),
            BorderColor = Vector4.Zero,
            BorderWidth = 0,
            CornerRadius = 4,
            Padding = UIEdges.Zero
        };
    }

    /// <inheritdoc />
    public UIStyle GetScrollbarThumbStyle(UIInteractionState state)
    {
        var color = new Vector4(0.7f, 0.7f, 0.7f, 1f);

        if (state.HasFlag(UIInteractionState.Pressed))
        {
            color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        }
        else if (state.HasFlag(UIInteractionState.Hovered))
        {
            color = new Vector4(0.6f, 0.6f, 0.6f, 1f);
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
            BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 0.9f),
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
