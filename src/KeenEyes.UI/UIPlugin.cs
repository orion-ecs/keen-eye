using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// Plugin that adds ECS-based UI capabilities to the world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides a retained-mode UI system where UI elements are entities with components.
/// It registers systems for input handling, focus management, layout calculation, and rendering.
/// </para>
/// <para>
/// The plugin exposes a <see cref="UIContext"/> extension that can be accessed via
/// <c>world.GetExtension&lt;UIContext&gt;()</c> for focus management and canvas creation.
/// </para>
/// <para>
/// <b>System Execution Order:</b>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Phase</term>
/// <term>Order</term>
/// <term>System</term>
/// <term>Responsibility</term>
/// </listheader>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>0</term>
/// <term><see cref="UIInputSystem"/></term>
/// <term>Hit testing, hover/press states, click events</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>10</term>
/// <term><see cref="UIFocusSystem"/></term>
/// <term>Tab navigation, keyboard focus</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>20</term>
/// <term><see cref="UITabSystem"/></term>
/// <term>Tab view switching on click</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>30</term>
/// <term><see cref="UIWindowSystem"/></term>
/// <term>Window dragging, closing, z-order</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>40</term>
/// <term><see cref="UISplitterSystem"/></term>
/// <term>Splitter pane resizing</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>45</term>
/// <term><see cref="UITooltipSystem"/></term>
/// <term>Tooltips and popovers</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>50</term>
/// <term><see cref="UIMenuSystem"/></term>
/// <term>Menu bars, dropdowns, context menus</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>55</term>
/// <term><see cref="UIRadialMenuSystem"/></term>
/// <term>Radial pie menus for gamepad</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>60</term>
/// <term><see cref="UIDockSystem"/></term>
/// <term>Panel docking, tabs, drag-drop</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>70</term>
/// <term><see cref="UITreeViewSystem"/></term>
/// <term>Tree view expand/collapse, selection, navigation</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>75</term>
/// <term><see cref="UIPropertyGridSystem"/></term>
/// <term>Property grid category expand/collapse</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>80</term>
/// <term><see cref="UIAccordionSystem"/></term>
/// <term>Accordion section expand/collapse</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>85</term>
/// <term><see cref="UICheckboxSystem"/></term>
/// <term>Checkbox and toggle click behavior</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>86</term>
/// <term><see cref="UISliderSystem"/></term>
/// <term>Slider drag behavior</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>87</term>
/// <term><see cref="UIScrollbarSystem"/></term>
/// <term>Scrollbar thumb drag behavior</term>
/// </item>
/// <item>
/// <term>EarlyUpdate</term>
/// <term>88</term>
/// <term><see cref="UITextInputSystem"/></term>
/// <term>Text field keyboard input handling</term>
/// </item>
/// <item>
/// <term>LateUpdate</term>
/// <term>-10</term>
/// <term><see cref="UILayoutSystem"/></term>
/// <term>Calculate ComputedBounds for all elements</term>
/// </item>
/// <item>
/// <term>Render</term>
/// <term>100</term>
/// <term><see cref="UIRenderSystem"/></term>
/// <term>Draw UI via I2DRenderer/ITextRenderer</term>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install the UI plugin
/// world.InstallPlugin(new UIPlugin());
///
/// // Access the UI context
/// var ui = world.GetExtension&lt;UIContext&gt;();
///
/// // Create a root canvas
/// var canvas = ui.CreateCanvas("MainUI");
///
/// // Create a button entity
/// var button = world.Spawn()
///     .With(new UIElement { Visible = true, RaycastTarget = true })
///     .With(new UIRect
///     {
///         AnchorMin = new Vector2(0.5f, 0.5f),
///         AnchorMax = new Vector2(0.5f, 0.5f),
///         Size = new Vector2(200, 50),
///         Pivot = new Vector2(0.5f, 0.5f)
///     })
///     .With(new UIStyle { BackgroundColor = new Vector4(0.2f, 0.4f, 0.8f, 1f) })
///     .With(new UIText { Content = "Click Me", Color = Vector4.One })
///     .With(new UIInteractable { CanClick = true, CanFocus = true })
///     .Build();
///
/// // Set the button as a child of the canvas
/// world.SetParent(button, canvas);
/// </code>
/// </example>
public sealed class UIPlugin : IWorldPlugin
{
    private UIContext? uiContext;

    /// <inheritdoc/>
    public string Name => "UI";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register component types so they can be used dynamically
        RegisterComponents(context);

        // Create and expose the UI context
        uiContext = new UIContext(context.World);
        context.SetExtension(uiContext);

        // Register UI systems in proper execution order
        // Input system processes mouse events first
        context.AddSystem<UIInputSystem>(SystemPhase.EarlyUpdate, order: 0);

        // Focus system handles keyboard navigation after input
        context.AddSystem<UIFocusSystem>(SystemPhase.EarlyUpdate, order: 10);

        // Tab system handles tab switching after input and focus
        context.AddSystem<UITabSystem>(SystemPhase.EarlyUpdate, order: 20);

        // Window system handles window dragging, closing, and z-order
        context.AddSystem<UIWindowSystem>(SystemPhase.EarlyUpdate, order: 30);

        // Splitter system handles splitter drag operations
        context.AddSystem<UISplitterSystem>(SystemPhase.EarlyUpdate, order: 40);

        // Tooltip system handles tooltip display and popover behavior
        context.AddSystem<UITooltipSystem>(SystemPhase.EarlyUpdate, order: 45);

        // Menu system handles menu bars, dropdowns, and context menus
        context.AddSystem<UIMenuSystem>(SystemPhase.EarlyUpdate, order: 50);

        // Radial menu system handles pie menus for gamepad navigation
        context.AddSystem<UIRadialMenuSystem>(SystemPhase.EarlyUpdate, order: 55);

        // Dock system handles panel docking, tabs, and drag-drop
        context.AddSystem<UIDockSystem>(SystemPhase.EarlyUpdate, order: 60);

        // Tree view system handles expand/collapse, selection, and navigation
        context.AddSystem<UITreeViewSystem>(SystemPhase.EarlyUpdate, order: 70);

        // Property grid system handles category expand/collapse
        context.AddSystem<UIPropertyGridSystem>(SystemPhase.EarlyUpdate, order: 75);

        // Accordion system handles section expand/collapse
        context.AddSystem<UIAccordionSystem>(SystemPhase.EarlyUpdate, order: 80);

        // Checkbox system handles checkbox and toggle click behavior
        context.AddSystem<UICheckboxSystem>(SystemPhase.EarlyUpdate, order: 85);

        // Slider system handles slider drag behavior
        context.AddSystem<UISliderSystem>(SystemPhase.EarlyUpdate, order: 86);

        // Scrollbar system handles scrollbar thumb drag behavior
        context.AddSystem<UIScrollbarSystem>(SystemPhase.EarlyUpdate, order: 87);

        // Text input system handles keyboard input for text fields
        context.AddSystem<UITextInputSystem>(SystemPhase.EarlyUpdate, order: 88);

        // Modal system handles modal dialogs, backdrop clicks, and Escape key
        context.AddSystem<UIModalSystem>(SystemPhase.EarlyUpdate, order: 35);

        // Toast system handles toast notification timers and dismissal
        context.AddSystem<UIToastSystem>(SystemPhase.EarlyUpdate, order: 90);

        // Spinner system handles spinner rotation and progress bar animation
        context.AddSystem<UISpinnerSystem>(SystemPhase.EarlyUpdate, order: 91);

        // Color picker system handles color selection interactions
        context.AddSystem<UIColorPickerSystem>(SystemPhase.EarlyUpdate, order: 92);

        // Date picker system handles calendar and time selection
        context.AddSystem<UIDatePickerSystem>(SystemPhase.EarlyUpdate, order: 93);

        // Data grid system handles sorting, selection, and column resizing
        context.AddSystem<UIDataGridSystem>(SystemPhase.EarlyUpdate, order: 94);

        // Layout calculates bounds before rendering
        context.AddSystem<UILayoutSystem>(SystemPhase.LateUpdate, order: -10);

        // Render system draws UI on top of everything else
        context.AddSystem<UIRenderSystem>(SystemPhase.Render, order: 100);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Remove the extension
        context.RemoveExtension<UIContext>();
        uiContext = null;

        // Systems are automatically cleaned up by PluginManager
    }

    private static void RegisterComponents(IPluginContext context)
    {
        // Core components
        context.RegisterComponent<UIElement>();
        context.RegisterComponent<UIRect>();
        context.RegisterComponent<UIStyle>();
        context.RegisterComponent<UIText>();
        context.RegisterComponent<UIImage>();
        context.RegisterComponent<UIInteractable>();
        context.RegisterComponent<UILayout>();
        context.RegisterComponent<UIScrollable>();

        // Tab components
        context.RegisterComponent<UITabButton>();
        context.RegisterComponent<UITabPanel>();
        context.RegisterComponent<UITabViewState>();

        // Window components
        context.RegisterComponent<UIWindow>();
        context.RegisterComponent<UIWindowTitleBar>();
        context.RegisterComponent<UIWindowCloseButton>();
        context.RegisterComponent<UIWindowResizeHandle>();

        // Splitter components
        context.RegisterComponent<UISplitter>();
        context.RegisterComponent<UISplitterHandle>();
        context.RegisterComponent<UISplitterFirstPane>();
        context.RegisterComponent<UISplitterSecondPane>();

        // Tooltip components
        context.RegisterComponent<UITooltip>();
        context.RegisterComponent<UIPopover>();
        context.RegisterComponent<UITooltipHoverState>();

        // Menu components
        context.RegisterComponent<UIMenuBar>();
        context.RegisterComponent<UIMenu>();
        context.RegisterComponent<UIMenuItem>();
        context.RegisterComponent<UIMenuShortcut>();
        context.RegisterComponent<UIMenuBarItem>();

        // Radial menu components
        context.RegisterComponent<UIRadialMenu>();
        context.RegisterComponent<UIRadialSlice>();
        context.RegisterComponent<UIRadialMenuInputState>();

        // Dock components
        context.RegisterComponent<UIDockContainer>();
        context.RegisterComponent<UIDockZone>();
        context.RegisterComponent<UIDockPanel>();
        context.RegisterComponent<UIDockTabGroup>();
        context.RegisterComponent<UIDockTab>();

        // Toolbar components
        context.RegisterComponent<UIToolbar>();
        context.RegisterComponent<UIToolbarButton>();
        context.RegisterComponent<UIToolbarSeparator>();
        context.RegisterComponent<UIStatusBar>();
        context.RegisterComponent<UIStatusBarSection>();

        // Tree view components
        context.RegisterComponent<UITreeView>();
        context.RegisterComponent<UITreeNode>();

        // Property grid components
        context.RegisterComponent<UIPropertyGrid>();
        context.RegisterComponent<UIPropertyCategory>();
        context.RegisterComponent<UIPropertyRow>();

        // Accordion components
        context.RegisterComponent<UIAccordion>();
        context.RegisterComponent<UIAccordionSection>();

        // Modal components
        context.RegisterComponent<UIModal>();
        context.RegisterComponent<UIModalBackdrop>();
        context.RegisterComponent<UIModalCloseButton>();
        context.RegisterComponent<UIModalButton>();

        // Toast components
        context.RegisterComponent<UIToast>();
        context.RegisterComponent<UIToastContainer>();
        context.RegisterComponent<UIToastCloseButton>();

        // Input widget components
        context.RegisterComponent<UICheckbox>();
        context.RegisterComponent<UIToggle>();
        context.RegisterComponent<UISlider>();
        context.RegisterComponent<UIScrollbarThumb>();
        context.RegisterComponent<UITextInput>();

        // Spinner and progress bar components
        context.RegisterComponent<UISpinner>();
        context.RegisterComponent<UIProgressBar>();

        // Color picker components
        context.RegisterComponent<UIColorPicker>();
        context.RegisterComponent<UIColorSlider>();
        context.RegisterComponent<UIColorSatValArea>();

        // Date picker components
        context.RegisterComponent<UIDatePicker>();
        context.RegisterComponent<UICalendarDay>();
        context.RegisterComponent<UITimeSpinner>();

        // Data grid components
        context.RegisterComponent<UIDataGrid>();
        context.RegisterComponent<UIDataGridColumn>();
        context.RegisterComponent<UIDataGridRow>();
        context.RegisterComponent<UIDataGridCell>();
        context.RegisterComponent<UIDataGridResizeHandle>();

        // Tag components
        context.RegisterComponent<UIRootTag>(isTag: true);
        context.RegisterComponent<UIDisabledTag>(isTag: true);
        context.RegisterComponent<UIHiddenTag>(isTag: true);
        context.RegisterComponent<UIFocusedTag>(isTag: true);
        context.RegisterComponent<UILayoutDirtyTag>(isTag: true);
        context.RegisterComponent<UIClipChildrenTag>(isTag: true);
        context.RegisterComponent<UITooltipVisibleTag>(isTag: true);
        context.RegisterComponent<UIMenuToggleTag>(isTag: true);
        context.RegisterComponent<UIMenuSubmenuTag>(isTag: true);
        context.RegisterComponent<UIRadialMenuOpenTag>(isTag: true);
        context.RegisterComponent<UIRadialSliceSelectedTag>(isTag: true);
        context.RegisterComponent<UIDockPreviewTag>(isTag: true);
        context.RegisterComponent<UIDockDraggingTag>(isTag: true);
        context.RegisterComponent<UIToolbarButtonGroupTag>(isTag: true);
        context.RegisterComponent<UITreeNodeDraggingTag>(isTag: true);
        context.RegisterComponent<UITreeNodeExpandArrowTag>(isTag: true);
        context.RegisterComponent<UIPropertyCategoryHeaderTag>(isTag: true);
        context.RegisterComponent<UIPropertyCategoryArrowTag>(isTag: true);
        context.RegisterComponent<UIAccordionHeaderTag>(isTag: true);
        context.RegisterComponent<UIAccordionArrowTag>(isTag: true);
        context.RegisterComponent<UIAccordionContentTag>(isTag: true);
        context.RegisterComponent<UIDataGridRowSelectedTag>(isTag: true);
    }
}
