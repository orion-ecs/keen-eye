using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes.Systems;

/// <summary>
/// System that applies theme styles to UI components marked with <see cref="UIThemed"/>.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the LateUpdate phase before layout and rendering systems.
/// It updates <see cref="UIStyle"/> components based on the current theme and
/// interaction state of each UI element.
/// </para>
/// <para>
/// Only entities with both <see cref="UIStyle"/> and <see cref="UIThemed"/> components
/// are processed. The <see cref="UIThemed.ComponentType"/> determines which style
/// method is called on the theme.
/// </para>
/// </remarks>
public sealed class ThemeApplicatorSystem : SystemBase
{
    private IThemeContext? themeContext;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization to avoid issues during plugin install
        themeContext ??= World.GetExtension<IThemeContext>();

        var theme = themeContext.CurrentTheme;

        // Query entities that need theme styling
        foreach (var entity in World.Query<UIStyle, UIThemed>())
        {
            ref var style = ref World.Get<UIStyle>(entity);
            ref readonly var themed = ref World.Get<UIThemed>(entity);

            // Get interaction state if available
            var interactionState = UIInteractionState.None;
            if (World.Has<UIInteractable>(entity))
            {
                ref readonly var interactable = ref World.Get<UIInteractable>(entity);
                interactionState = interactable.State;
            }

            // Check if disabled
            if (World.Has<UIDisabledTag>(entity))
            {
                // Keep existing style but could apply disabled overlay in future
                continue;
            }

            // Apply theme style based on component type
            style = themed.ComponentType switch
            {
                UIComponentType.Button => theme.GetButtonStyle(interactionState),
                UIComponentType.Panel => theme.GetPanelStyle(),
                UIComponentType.Input => theme.GetInputStyle(interactionState),
                UIComponentType.Menu => theme.GetMenuStyle(),
                UIComponentType.MenuItem => theme.GetMenuItemStyle(interactionState),
                UIComponentType.Modal => theme.GetModalStyle(),
                UIComponentType.ScrollbarTrack => theme.GetScrollbarTrackStyle(),
                UIComponentType.ScrollbarThumb => theme.GetScrollbarThumbStyle(interactionState),
                UIComponentType.Tooltip => theme.GetTooltipStyle(),
                _ => style // Keep existing style for unknown types
            };
        }
    }
}
