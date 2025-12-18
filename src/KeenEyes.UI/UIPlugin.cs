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

        // Tag components
        context.RegisterComponent<UIRootTag>(isTag: true);
        context.RegisterComponent<UIDisabledTag>(isTag: true);
        context.RegisterComponent<UIHiddenTag>(isTag: true);
        context.RegisterComponent<UIFocusedTag>(isTag: true);
        context.RegisterComponent<UILayoutDirtyTag>(isTag: true);
        context.RegisterComponent<UIClipChildrenTag>(isTag: true);
    }
}
