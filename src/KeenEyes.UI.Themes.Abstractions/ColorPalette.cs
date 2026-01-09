using System.Numerics;

namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Defines a complete color palette for a theme.
/// </summary>
/// <remarks>
/// <para>
/// All colors use Vector4 in RGBA format with values from 0 to 1.
/// This matches the format used by <see cref="KeenEyes.UI.Abstractions.UIStyle"/>.
/// </para>
/// <para>
/// The palette uses semantic color names that describe purpose rather than appearance.
/// This allows themes to define colors appropriate for their light or dark base.
/// </para>
/// </remarks>
public sealed class ColorPalette
{
    // Semantic background colors

    /// <summary>
    /// Primary background color for the application.
    /// </summary>
    public required Vector4 Background { get; init; }

    /// <summary>
    /// Background color for elevated surfaces like panels and cards.
    /// </summary>
    public required Vector4 Surface { get; init; }

    /// <summary>
    /// Background color for surfaces that appear above other surfaces (e.g., modals).
    /// </summary>
    public required Vector4 SurfaceElevated { get; init; }

    // Brand colors

    /// <summary>
    /// Primary brand/accent color for important UI elements.
    /// </summary>
    public required Vector4 Primary { get; init; }

    /// <summary>
    /// Variant of primary color for hover or secondary emphasis.
    /// </summary>
    public required Vector4 PrimaryVariant { get; init; }

    /// <summary>
    /// Secondary brand color for less prominent accents.
    /// </summary>
    public required Vector4 Secondary { get; init; }

    /// <summary>
    /// Accent color for highlights and selections.
    /// </summary>
    public required Vector4 Accent { get; init; }

    // Text colors

    /// <summary>
    /// Primary text color for body text and most content.
    /// </summary>
    public required Vector4 TextPrimary { get; init; }

    /// <summary>
    /// Secondary text color for captions and less important text.
    /// </summary>
    public required Vector4 TextSecondary { get; init; }

    /// <summary>
    /// Text color for disabled or placeholder content.
    /// </summary>
    public required Vector4 TextDisabled { get; init; }

    /// <summary>
    /// Text color for content on primary-colored backgrounds.
    /// </summary>
    public required Vector4 TextOnPrimary { get; init; }

    // State colors

    /// <summary>
    /// Color indicating success or positive state.
    /// </summary>
    public required Vector4 Success { get; init; }

    /// <summary>
    /// Color indicating a warning or cautionary state.
    /// </summary>
    public required Vector4 Warning { get; init; }

    /// <summary>
    /// Color indicating an error or negative state.
    /// </summary>
    public required Vector4 Error { get; init; }

    /// <summary>
    /// Color indicating informational content.
    /// </summary>
    public required Vector4 Info { get; init; }

    // Border colors

    /// <summary>
    /// Default border color for UI elements.
    /// </summary>
    public required Vector4 Border { get; init; }

    /// <summary>
    /// Border color for focused elements.
    /// </summary>
    public required Vector4 BorderFocused { get; init; }

    /// <summary>
    /// Subtle divider/separator color.
    /// </summary>
    public required Vector4 Divider { get; init; }

    // Interaction state overlays

    /// <summary>
    /// Semi-transparent overlay for hover state.
    /// </summary>
    public required Vector4 HoverOverlay { get; init; }

    /// <summary>
    /// Semi-transparent overlay for pressed state.
    /// </summary>
    public required Vector4 PressedOverlay { get; init; }

    /// <summary>
    /// Semi-transparent overlay for disabled state.
    /// </summary>
    public required Vector4 DisabledOverlay { get; init; }
}
