// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Common.Serialization;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Core editor plugin that provides essential commands and shortcuts.
/// </summary>
/// <remarks>
/// <para>
/// This plugin registers fundamental editor operations including:
/// </para>
/// <list type="bullet">
/// <item><description>Undo/Redo commands (Ctrl+Z, Ctrl+Y)</description></item>
/// <item><description>Selection shortcuts (Ctrl+A, Escape to deselect)</description></item>
/// <item><description>Delete selected entities (Delete key)</description></item>
/// <item><description>Duplicate selected entities (Ctrl+D)</description></item>
/// </list>
/// </remarks>
internal sealed class CoreEditorPlugin : EditorPluginBase
{
    /// <inheritdoc />
    public override string Name => "Core Editor";

    /// <inheritdoc />
    public override string? Description => "Essential editor commands and shortcuts";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        RegisterShortcuts(context);
    }

    private static void RegisterShortcuts(IEditorContext context)
    {
        if (!context.TryGetCapability<IShortcutCapability>(out var shortcuts) || shortcuts is null)
        {
            return;
        }

        // Undo/Redo
        shortcuts.RegisterShortcut(
            "core.undo",
            "Undo",
            ShortcutCategories.Edit,
            "Ctrl+Z",
            () => context.UndoRedo.Undo(),
            () => context.UndoRedo.CanUndo);

        shortcuts.RegisterShortcut(
            "core.redo",
            "Redo",
            ShortcutCategories.Edit,
            "Ctrl+Y",
            () => context.UndoRedo.Redo(),
            () => context.UndoRedo.CanRedo);

        // Also support Ctrl+Shift+Z for redo
        shortcuts.RegisterShortcut(
            "core.redo-alt",
            "Redo (Alt)",
            ShortcutCategories.Edit,
            "Ctrl+Shift+Z",
            () => context.UndoRedo.Redo(),
            () => context.UndoRedo.CanRedo);

        // Selection
        shortcuts.RegisterShortcut(
            "core.select-all",
            "Select All",
            ShortcutCategories.Selection,
            "Ctrl+A",
            () => SelectAll(context));

        shortcuts.RegisterShortcut(
            "core.deselect",
            "Deselect All",
            ShortcutCategories.Selection,
            "Escape",
            () => context.Selection.ClearSelection());

        // Entity operations
        shortcuts.RegisterShortcut(
            "core.delete",
            "Delete",
            ShortcutCategories.Edit,
            "Delete",
            () => DeleteSelected(context),
            () => context.Selection.SelectedEntities.Count > 0);

        shortcuts.RegisterShortcut(
            "core.duplicate",
            "Duplicate",
            ShortcutCategories.Edit,
            "Ctrl+D",
            () => DuplicateSelected(context),
            () => context.Selection.SelectedEntities.Count > 0);
    }

    private static void SelectAll(IEditorContext context)
    {
        var sceneWorld = context.Worlds.CurrentSceneWorld;
        if (sceneWorld is null)
        {
            return;
        }

        context.Selection.SelectMultiple(sceneWorld.GetAllEntities());
    }

    private static void DeleteSelected(IEditorContext context)
    {
        var sceneWorld = context.Worlds.CurrentSceneWorld;
        if (sceneWorld is null)
        {
            return;
        }

        var selected = context.Selection.SelectedEntities.ToList();
        if (selected.Count == 0)
        {
            return;
        }

        context.UndoRedo.Execute(new DeleteEntitiesCommand(context, selected));
        context.Selection.ClearSelection();
    }

    private static void DuplicateSelected(IEditorContext context)
    {
        var sceneWorld = context.Worlds.CurrentSceneWorld;
        if (sceneWorld is null)
        {
            return;
        }

        var selected = context.Selection.SelectedEntities.ToList();
        if (selected.Count == 0)
        {
            // Nothing to duplicate
            return;
        }

        // Capture the selected entities
        var snapshots = EntitySerializer.CaptureEntities(sceneWorld, selected, includeChildren: true);

        // For single entity, duplicate with same parent; for multiple, place at root
        Entity? parent = null;
        if (selected.Count == 1 && sceneWorld is KeenEyes.Capabilities.IHierarchyCapability hierarchy)
        {
            parent = hierarchy.GetParent(selected[0]);
        }

        // Restore as new entities
        var duplicated = EntitySerializer.RestoreEntities(sceneWorld, snapshots, parent);

        // Select the duplicated entities
        context.Selection.ClearSelection();
        foreach (var entity in duplicated)
        {
            context.Selection.AddToSelection(entity);
        }
    }

    /// <summary>
    /// Command to delete entities from the scene with full undo support.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="EntitySerializer"/> to capture entity state before deletion,
    /// enabling full restoration on undo including component data and hierarchy.
    /// </remarks>
    private sealed class DeleteEntitiesCommand : IEditorCommand
    {
        private readonly IEditorContext context;
        private readonly List<Entity> entities;
        private IReadOnlyList<EntitySnapshot>? snapshots;

        public DeleteEntitiesCommand(IEditorContext context, List<Entity> entities)
        {
            this.context = context;
            this.entities = entities;
        }

        public string Description => $"Delete {entities.Count} entit{(entities.Count == 1 ? "y" : "ies")}";

        public void Execute()
        {
            var sceneWorld = context.Worlds.CurrentSceneWorld;
            if (sceneWorld is null)
            {
                return;
            }

            // Capture entity state before deletion for undo
            snapshots = EntitySerializer.CaptureEntities(sceneWorld, entities, includeChildren: true);

            // Delete the entities (children are included in snapshots)
            foreach (var entity in entities)
            {
                if (entity.IsValid && sceneWorld.IsAlive(entity))
                {
                    if (sceneWorld is KeenEyes.Capabilities.IHierarchyCapability hierarchy)
                    {
                        hierarchy.DespawnRecursive(entity);
                    }
                    else
                    {
                        sceneWorld.Despawn(entity);
                    }
                }
            }
        }

        public void Undo()
        {
            if (snapshots is null || snapshots.Count == 0)
            {
                return;
            }

            var sceneWorld = context.Worlds.CurrentSceneWorld;
            if (sceneWorld is null)
            {
                return;
            }

            // Restore entities from captured snapshots
            _ = EntitySerializer.RestoreEntities(sceneWorld, snapshots, parent: null);
        }

        public bool TryMerge(IEditorCommand other)
        {
            // Delete commands don't merge
            return false;
        }
    }
}
