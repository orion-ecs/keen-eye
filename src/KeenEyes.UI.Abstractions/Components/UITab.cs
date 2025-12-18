namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that identifies a tab button within a tab view.
/// </summary>
/// <remarks>
/// Attached to each tab button in a TabView to enable tab switching.
/// The UITabSystem listens for click events on entities with this component.
/// </remarks>
/// <param name="tabIndex">The index of this tab (0-based).</param>
/// <param name="tabView">The tab view container entity.</param>
public struct UITabButton(int tabIndex, Entity tabView) : IComponent
{
    /// <summary>
    /// The index of this tab (0-based).
    /// </summary>
    public int TabIndex = tabIndex;

    /// <summary>
    /// Reference to the tab view container entity.
    /// </summary>
    public Entity TabView = tabView;
}

/// <summary>
/// Component that identifies a content panel within a tab view.
/// </summary>
/// <remarks>
/// Attached to each content panel in a TabView to enable visibility toggling
/// when tabs are switched.
/// </remarks>
/// <param name="tabIndex">The index of this panel (corresponds to tab index).</param>
/// <param name="tabView">The tab view container entity.</param>
public struct UITabPanel(int tabIndex, Entity tabView) : IComponent
{
    /// <summary>
    /// The index of this panel (corresponds to tab index).
    /// </summary>
    public int TabIndex = tabIndex;

    /// <summary>
    /// Reference to the tab view container entity.
    /// </summary>
    public Entity TabView = tabView;
}

/// <summary>
/// Component that stores the state of a tab view container.
/// </summary>
/// <remarks>
/// Attached to the tab view container entity to track the currently selected tab.
/// </remarks>
/// <param name="selectedIndex">The initially selected tab index.</param>
public struct UITabViewState(int selectedIndex) : IComponent
{
    /// <summary>
    /// The currently selected tab index.
    /// </summary>
    public int SelectedIndex = selectedIndex;
}
