// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Tools;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the viewport panel for 3D scene visualization.
/// </summary>
/// <remarks>
/// <para>
/// The viewport panel renders the current scene with editor gizmos, selection
/// highlighting, and transform tools. It handles mouse input for entity selection
/// and tool interaction.
/// </para>
/// </remarks>
internal sealed class ViewportPlugin : EditorPluginBase
{
    private const string PanelId = "viewport";

    /// <inheritdoc />
    public override string Name => "Viewport";

    /// <inheritdoc />
    public override string? Description => "3D scene viewport for visualization and interaction";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the viewport panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Scene",
                Icon = "viewport",
                DefaultLocation = PanelDockLocation.Center,
                OpenByDefault = true,
                MinWidth = 400,
                MinHeight = 300,
                DefaultWidth = 800,
                DefaultHeight = 600,
                Category = "Scene"
            },
            () => new ViewportPanelImpl());

        // Register transform tools
        RegisterTransformTools(context);
    }

    private static void RegisterTransformTools(IEditorContext context)
    {
        if (!context.TryGetCapability<IToolCapability>(out var tools) || tools is null)
        {
            return;
        }

        tools.RegisterTool("transform.select", new SelectTool());
        tools.RegisterTool("transform.move", new MoveTool());
        tools.RegisterTool("transform.rotate", new RotateTool());
        tools.RegisterTool("transform.scale", new ScaleTool());

        // Register shortcuts for tool activation
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "tools.select",
                "Select Tool",
                ShortcutCategories.Tools,
                "Q",
                () => { if (tools is not null) tools.ActivateTool("transform.select"); });

            shortcuts.RegisterShortcut(
                "tools.move",
                "Move Tool",
                ShortcutCategories.Tools,
                "W",
                () => { if (tools is not null) tools.ActivateTool("transform.move"); });

            shortcuts.RegisterShortcut(
                "tools.rotate",
                "Rotate Tool",
                ShortcutCategories.Tools,
                "E",
                () => { if (tools is not null) tools.ActivateTool("transform.rotate"); });

            shortcuts.RegisterShortcut(
                "tools.scale",
                "Scale Tool",
                ShortcutCategories.Tools,
                "R",
                () => { if (tools is not null) tools.ActivateTool("transform.scale"); });
        }
    }
}

/// <summary>
/// Implementation of the viewport panel.
/// </summary>
internal sealed class ViewportPanelImpl : IEditorPanel
{
    private Entity rootEntity;
    private IEditorContext? context;
    private IWorld? editorWorld;
    private IWorld? sceneWorld;
    private IToolCapability? toolCapability;
    private EventSubscription? sceneOpenedSubscription;
    private EventSubscription? sceneClosedSubscription;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <summary>
    /// Gets the currently loaded scene world, or null if no scene is open.
    /// </summary>
    public IWorld? SceneWorld => sceneWorld;

    /// <inheritdoc />
    public void Initialize(PanelContext context)
    {
        this.context = context.EditorContext;
        editorWorld = context.EditorWorld;
        rootEntity = context.Parent;

        // Cache capabilities for use in Update
        this.context.TryGetCapability(out toolCapability);

        // Subscribe to scene changes
        sceneOpenedSubscription = this.context.OnSceneOpened(OnSceneOpened);
        sceneClosedSubscription = this.context.OnSceneClosed(OnSceneClosed);
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Update the active tool if one is active
        if (toolCapability?.ActiveTool is not null && sceneWorld is not null && context is not null)
        {
            var selectedEntities = context.Selection.SelectedEntities.ToArray();
            var toolContext = CreateToolContext(selectedEntities);
            toolCapability.ActiveTool.Update(toolContext, deltaTime);
        }
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        sceneOpenedSubscription?.Dispose();
        sceneClosedSubscription?.Dispose();
        sceneWorld = null;

        if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
        {
            editorWorld.Despawn(rootEntity);
        }
    }

    private void OnSceneOpened(IWorld world)
    {
        // Store reference to the new scene world
        sceneWorld = world;

        // Deactivate any active tool when scene changes
        toolCapability?.DeactivateTool();
    }

    private void OnSceneClosed()
    {
        // Clear scene reference
        sceneWorld = null;

        // Deactivate any active tool
        toolCapability?.DeactivateTool();
    }

    private ToolContext CreateToolContext(IReadOnlyList<Entity> selectedEntities)
    {
        // Create a tool context with default values
        // In a full implementation, these would come from the camera system
        return new ToolContext
        {
            EditorContext = context!,
            SceneWorld = sceneWorld,
            SelectedEntities = selectedEntities,
            ViewportBounds = new ViewportBounds { X = 0, Y = 0, Width = 800, Height = 600 },
            ViewMatrix = System.Numerics.Matrix4x4.Identity,
            ProjectionMatrix = System.Numerics.Matrix4x4.Identity,
            CameraPosition = System.Numerics.Vector3.Zero,
            CameraForward = -System.Numerics.Vector3.UnitZ
        };
    }
}
