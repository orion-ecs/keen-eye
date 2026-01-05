using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// Plugin that adds graph node editor capabilities to the world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides a visual graph editor system for creating and manipulating
/// node-based graphs. It registers systems for input handling, layout calculation,
/// and rendering.
/// </para>
/// <para>
/// The plugin exposes a <see cref="GraphContext"/> extension that can be accessed via
/// <c>world.GetExtension&lt;GraphContext&gt;()</c> for graph manipulation.
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
/// <term><see cref="GraphInputSystem"/></term>
/// <term>Pan, zoom, select, drag nodes</term>
/// </item>
/// <item>
/// <term>LateUpdate</term>
/// <term>-5</term>
/// <term><see cref="GraphLayoutSystem"/></term>
/// <term>Calculate node bounds, port positions</term>
/// </item>
/// <item>
/// <term>Render</term>
/// <term>90</term>
/// <term><see cref="GraphRenderSystem"/></term>
/// <term>Draw grid, nodes, connections</term>
/// </item>
/// </list>
/// </remarks>
public sealed class GraphPlugin : IWorldPlugin
{
    private GraphContext? graphContext;
    private PortRegistry? portRegistry;
    private PortPositionCache? portCache;

    /// <inheritdoc/>
    public string Name => "Graph";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register component types
        RegisterComponents(context);

        // Create registries and context
        portRegistry = new PortRegistry();
        graphContext = new GraphContext(context.World, portRegistry);
        portCache = new PortPositionCache();

        // Wire undo manager if available
        if (context.World.TryGetExtension<IUndoRedoManager>(out var undoManager) && undoManager is not null)
        {
            graphContext.SetUndoManager(undoManager);
        }

        // Expose extensions
        context.SetExtension(graphContext);
        context.SetExtension(portRegistry);
        context.SetExtension(portCache);

        // Register systems
        context.AddSystem<GraphInputSystem>(SystemPhase.EarlyUpdate, order: 0);
        context.AddSystem<GraphContextMenuSystem>(SystemPhase.EarlyUpdate, order: 5);
        context.AddSystem<GraphViewAnimationSystem>(SystemPhase.Update, order: 0);
        context.AddSystem<GraphLayoutSystem>(SystemPhase.LateUpdate, order: -5);
        context.AddSystem<GraphRenderSystem>(SystemPhase.Render, order: 90);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Remove extensions
        context.RemoveExtension<GraphContext>();
        context.RemoveExtension<PortRegistry>();
        context.RemoveExtension<PortPositionCache>();

        graphContext = null;
        portRegistry = null;
        portCache = null;

        // Systems are automatically cleaned up by PluginManager
    }

    private static void RegisterComponents(IPluginContext context)
    {
        // Core components
        context.RegisterComponent<GraphCanvas>();
        context.RegisterComponent<GraphNode>();
        context.RegisterComponent<GraphConnection>();

        // Interaction state components (added to canvas entity)
        context.RegisterComponent<PendingConnection>();
        context.RegisterComponent<HoveredPort>();
        context.RegisterComponent<GraphContextMenu>();
        context.RegisterComponent<GraphViewAnimation>();

        // Tag components
        context.RegisterComponent<GraphCanvasTag>(isTag: true);
        context.RegisterComponent<GraphNodeSelectedTag>(isTag: true);
        context.RegisterComponent<GraphConnectionSelectedTag>(isTag: true);
        context.RegisterComponent<GraphNodeDraggingTag>(isTag: true);
        context.RegisterComponent<GraphNodeGhostTag>(isTag: true);
        context.RegisterComponent<GraphSpacePanningTag>(isTag: true);
    }
}
