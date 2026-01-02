// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

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

        // TODO: Register transform tools (move, rotate, scale)
        // tools.RegisterTool("transform.select", new SelectTool());
        // tools.RegisterTool("transform.move", new MoveTool());
        // tools.RegisterTool("transform.rotate", new RotateTool());
        // tools.RegisterTool("transform.scale", new ScaleTool());

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
    private EventSubscription? sceneOpenedSubscription;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext panelContext)
    {
        context = panelContext.EditorContext;
        editorWorld = panelContext.EditorWorld;
        rootEntity = panelContext.Parent;

        // Subscribe to scene changes
        sceneOpenedSubscription = context.OnSceneOpened(OnSceneOpened);
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // TODO: Update viewport rendering, gizmos, etc.
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        sceneOpenedSubscription?.Dispose();

        if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
        {
            editorWorld.Despawn(rootEntity);
        }
    }

    private void OnSceneOpened(IWorld sceneWorld)
    {
        // TODO: Setup viewport for the new scene
    }
}
