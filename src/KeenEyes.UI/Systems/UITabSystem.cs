using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles tab switching in TabView widgets.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for click events on tab buttons (entities with <see cref="UITabButton"/>)
/// and switches the visible content panel accordingly.
/// </para>
/// <para>
/// When a tab is clicked:
/// <list type="bullet">
/// <item>The previously active panel gets <see cref="UIHiddenTag"/> added and Visible set to false</item>
/// <item>The newly selected panel gets <see cref="UIHiddenTag"/> removed and Visible set to true</item>
/// <item>Tab button styles are updated to reflect active/inactive state</item>
/// <item>The <see cref="UITabViewState.SelectedIndex"/> is updated</item>
/// </list>
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase after
/// <see cref="UIInputSystem"/>.
/// </para>
/// </remarks>
public sealed class UITabSystem : SystemBase
{
    private EventSubscription? clickSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to click events on tab buttons
        clickSubscription = World.Subscribe<UIClickEvent>(OnTabClick);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        clickSubscription?.Dispose();
        clickSubscription = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Tab switching is event-driven, no per-frame work needed
    }

    private void OnTabClick(UIClickEvent e)
    {
        // Check if the clicked element is a tab button
        if (!World.Has<UITabButton>(e.Element))
        {
            return;
        }

        ref readonly var tabButton = ref World.Get<UITabButton>(e.Element);
        var tabView = tabButton.TabView;

        // Validate the tab view still exists and has state
        if (!World.IsAlive(tabView) || !World.Has<UITabViewState>(tabView))
        {
            return;
        }

        ref var state = ref World.Get<UITabViewState>(tabView);
        var oldIndex = state.SelectedIndex;
        var newIndex = tabButton.TabIndex;

        // If already selected, nothing to do
        if (oldIndex == newIndex)
        {
            return;
        }

        // Update state
        state.SelectedIndex = newIndex;

        // Update panels visibility
        UpdatePanels(tabView, oldIndex, newIndex);

        // Update tab button styles
        UpdateTabButtons(tabView, oldIndex, newIndex);
    }

    private void UpdatePanels(Entity tabView, int oldIndex, int newIndex)
    {
        foreach (var panel in World.Query<UITabPanel, UIElement>())
        {
            ref readonly var tabPanel = ref World.Get<UITabPanel>(panel);

            // Only process panels belonging to this tab view
            if (tabPanel.TabView != tabView)
            {
                continue;
            }

            ref var element = ref World.Get<UIElement>(panel);

            if (tabPanel.TabIndex == oldIndex)
            {
                // Hide the old panel
                element.Visible = false;
                if (!World.Has<UIHiddenTag>(panel))
                {
                    World.Add(panel, new UIHiddenTag());
                }
            }
            else if (tabPanel.TabIndex == newIndex)
            {
                // Show the new panel
                element.Visible = true;
                if (World.Has<UIHiddenTag>(panel))
                {
                    World.Remove<UIHiddenTag>(panel);
                }
            }
        }
    }

    private void UpdateTabButtons(Entity tabView, int oldIndex, int newIndex)
    {
        foreach (var button in World.Query<UITabButton, UIStyle, UIText>())
        {
            ref readonly var tabButton = ref World.Get<UITabButton>(button);

            // Only process buttons belonging to this tab view
            if (tabButton.TabView != tabView)
            {
                continue;
            }

            ref var style = ref World.Get<UIStyle>(button);
            ref var text = ref World.Get<UIText>(button);

            // Get the tab view config colors (use defaults if not available)
            var activeTabColor = new System.Numerics.Vector4(0.15f, 0.15f, 0.2f, 1f);
            var inactiveTabColor = new System.Numerics.Vector4(0.18f, 0.18f, 0.22f, 1f);
            var activeTextColor = new System.Numerics.Vector4(1f, 1f, 1f, 1f);
            var inactiveTextColor = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1f);

            if (tabButton.TabIndex == newIndex)
            {
                // Make active
                style.BackgroundColor = activeTabColor;
                text.Color = activeTextColor;
            }
            else if (tabButton.TabIndex == oldIndex)
            {
                // Make inactive
                style.BackgroundColor = inactiveTabColor;
                text.Color = inactiveTextColor;
            }
        }
    }
}
