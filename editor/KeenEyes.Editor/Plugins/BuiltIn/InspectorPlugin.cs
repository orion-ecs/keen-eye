// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the inspector panel for editing entity properties.
/// </summary>
/// <remarks>
/// <para>
/// The inspector panel displays the components attached to the selected entity
/// and allows editing their properties. Property drawers can be registered
/// to customize the editing experience for specific component types.
/// </para>
/// </remarks>
internal sealed class InspectorPlugin : EditorPluginBase
{
    private const string PanelId = "inspector";

    /// <inheritdoc />
    public override string Name => "Inspector";

    /// <inheritdoc />
    public override string? Description => "Entity inspector panel for editing component properties";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the inspector panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Inspector",
                Icon = "inspector",
                DefaultLocation = PanelDockLocation.Right,
                OpenByDefault = true,
                MinWidth = 250,
                MinHeight = 200,
                DefaultWidth = 350,
                DefaultHeight = 500,
                Category = "Scene",
                ToggleShortcut = "Ctrl+Shift+I"
            },
            () => new InspectorPanelImpl());

        // Register shortcut for toggling the inspector panel
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "inspector.toggle",
                "Toggle Inspector",
                ShortcutCategories.View,
                "Ctrl+Shift+I",
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
        }
    }
}

/// <summary>
/// Implementation of the inspector panel.
/// </summary>
internal sealed class InspectorPanelImpl : IEditorPanel
{
    private Entity rootEntity;
    private IEditorContext? context;
    private IWorld? editorWorld;
    private EventSubscription? selectionChangedSubscription;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext context)
    {
        this.context = context.EditorContext;
        editorWorld = context.EditorWorld;
        rootEntity = context.Parent;

        // Subscribe to selection changes to update the inspector
        selectionChangedSubscription = this.context.OnSelectionChanged(OnSelectionChanged);
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Inspector uses UI events, no per-frame logic needed
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        selectionChangedSubscription?.Dispose();

        if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
        {
            editorWorld.Despawn(rootEntity);
        }
    }

    private void OnSelectionChanged(IReadOnlyList<Entity> selectedEntities)
    {
        // TODO: Rebuild the inspector UI for the newly selected entity
        // This would enumerate components and create property editors
    }
}
