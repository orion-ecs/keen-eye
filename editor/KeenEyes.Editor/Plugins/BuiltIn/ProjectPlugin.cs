// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the project panel for asset browsing.
/// </summary>
/// <remarks>
/// <para>
/// The project panel displays the project's asset folder structure and allows
/// browsing, importing, and managing assets. Assets can be dragged from the
/// project panel into the scene.
/// </para>
/// </remarks>
internal sealed class ProjectPlugin : EditorPluginBase
{
    private const string PanelId = "project";

    /// <inheritdoc />
    public override string Name => "Project";

    /// <inheritdoc />
    public override string? Description => "Project panel for asset browsing and management";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the project panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Project",
                Icon = "folder",
                DefaultLocation = PanelDockLocation.Bottom,
                OpenByDefault = true,
                MinWidth = 300,
                MinHeight = 150,
                DefaultWidth = 600,
                DefaultHeight = 250,
                Category = "Assets",
                ToggleShortcut = "Ctrl+Shift+P"
            },
            () => new ProjectPanelImpl());

        // Register shortcut for toggling the project panel
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "project.toggle",
                "Toggle Project",
                ShortcutCategories.View,
                "Ctrl+Shift+P",
                () =>
                {
                    if (context.TryGetCapability<IPanelCapability>(out var p) && p is not null)
                    {
                        if (p.IsPanelOpen(PanelId))
                        {
                            p.ClosePanel(PanelId);
                        }
                        else
                        {
                            p.OpenPanel(PanelId);
                        }
                    }
                });

            shortcuts.RegisterShortcut(
                "project.refresh",
                "Refresh Project",
                ShortcutCategories.File,
                "Ctrl+R",
                () =>
                {
                    // TODO: Refresh asset database
                });
        }
    }
}

/// <summary>
/// Implementation of the project panel.
/// </summary>
internal sealed class ProjectPanelImpl : IEditorPanel
{
    private Entity rootEntity;
    private IWorld? editorWorld;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext context)
    {
        editorWorld = context.EditorWorld;
        rootEntity = context.Parent;

        // TODO: Create project browser UI with folder tree and file grid
        // Access context.EditorContext.Assets for asset database
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Project panel uses UI events, no per-frame logic needed
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
        {
            editorWorld.Despawn(rootEntity);
        }
    }
}
