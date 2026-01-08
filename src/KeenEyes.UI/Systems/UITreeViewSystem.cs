using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles tree view interactions: expansion and selection.
/// </summary>
/// <remarks>
/// <para>
/// This system processes interactions with tree view widgets including:
/// <list type="bullet">
/// <item>Expand/collapse nodes via arrow clicks</item>
/// <item>Selection via node content clicks</item>
/// <item>Double-click events for actions</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UITreeViewSystem : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Handle expand arrow clicks
        ProcessExpandCollapseClicks();

        // Handle node selection clicks
        ProcessNodeSelectionClicks();
    }

    private void ProcessExpandCollapseClicks()
    {
        // Find expand arrows that were clicked
        foreach (var arrowEntity in World.Query<UITreeNodeExpandArrowTag, UIInteractable>())
        {
            ref readonly var interactable = ref World.Get<UIInteractable>(arrowEntity);

            if (!interactable.HasEvent(UIEventType.Click))
            {
                continue;
            }

            // Find the parent tree node
            var parent = World.GetParent(arrowEntity);
            while (parent.IsValid && !World.Has<UITreeNode>(parent))
            {
                parent = World.GetParent(parent);
            }

            if (!parent.IsValid || !World.Has<UITreeNode>(parent))
            {
                continue;
            }

            ref var node = ref World.Get<UITreeNode>(parent);
            if (!node.HasChildren)
            {
                continue;
            }

            // Toggle expansion
            node.IsExpanded = !node.IsExpanded;

            // Update visibility of child container
            if (node.ChildContainer.IsValid && World.Has<UIElement>(node.ChildContainer))
            {
                ref var childElement = ref World.Get<UIElement>(node.ChildContainer);
                childElement.Visible = node.IsExpanded;

                // Toggle UIHiddenTag so layout system properly reclaims space
                if (node.IsExpanded)
                {
                    if (World.Has<UIHiddenTag>(node.ChildContainer))
                    {
                        World.Remove<UIHiddenTag>(node.ChildContainer);
                    }
                }
                else
                {
                    if (!World.Has<UIHiddenTag>(node.ChildContainer))
                    {
                        World.Add(node.ChildContainer, new UIHiddenTag());
                    }
                }
            }

            // Mark tree view root dirty to force layout recalculation
            if (node.TreeView.IsValid && !World.Has<UILayoutDirtyTag>(node.TreeView))
            {
                World.Add(node.TreeView, new UILayoutDirtyTag());
            }

            // Update arrow rotation visual
            UpdateExpandArrowVisual(arrowEntity, node.IsExpanded);

            // Fire expand/collapse event
            if (node.TreeView.IsValid)
            {
                if (node.IsExpanded)
                {
                    World.Send(new UITreeNodeExpandedEvent(parent, node.TreeView));
                }
                else
                {
                    World.Send(new UITreeNodeCollapsedEvent(parent, node.TreeView));
                }
            }
        }
    }

    private void ProcessNodeSelectionClicks()
    {
        // Find tree nodes that were clicked (but not on expand arrow)
        foreach (var nodeEntity in World.Query<UITreeNode, UIInteractable>())
        {
            ref readonly var interactable = ref World.Get<UIInteractable>(nodeEntity);

            if (!interactable.HasEvent(UIEventType.Click))
            {
                continue;
            }

            ref var node = ref World.Get<UITreeNode>(nodeEntity);
            var treeView = node.TreeView;

            if (!treeView.IsValid || !World.Has<UITreeView>(treeView))
            {
                continue;
            }

            ref var treeViewData = ref World.Get<UITreeView>(treeView);

            // Deselect previous selection
            if (treeViewData.SelectedItem.IsValid &&
                treeViewData.SelectedItem != nodeEntity &&
                World.Has<UITreeNode>(treeViewData.SelectedItem))
            {
                ref var prevNode = ref World.Get<UITreeNode>(treeViewData.SelectedItem);
                prevNode.IsSelected = false;
                UpdateNodeSelectionVisual(treeViewData.SelectedItem, false);
            }

            // Select new node
            node.IsSelected = true;
            treeViewData.SelectedItem = nodeEntity;
            UpdateNodeSelectionVisual(nodeEntity, true);

            // Fire selection event
            World.Send(new UITreeNodeSelectedEvent(nodeEntity, treeView));

            // Check for double-click
            if (interactable.HasEvent(UIEventType.DoubleClick))
            {
                World.Send(new UITreeNodeDoubleClickedEvent(nodeEntity, treeView));
            }
        }
    }

    private void UpdateExpandArrowVisual(Entity arrowEntity, bool isExpanded)
    {
        // Update arrow text based on expansion state
        // Uses "v" and ">" for better font compatibility than Unicode arrows
        if (World.Has<UIText>(arrowEntity))
        {
            ref var text = ref World.Get<UIText>(arrowEntity);
            text.Content = isExpanded ? "v" : ">";
        }
    }

    private void UpdateNodeSelectionVisual(Entity nodeEntity, bool isSelected)
    {
        if (World.Has<UIStyle>(nodeEntity))
        {
            ref var style = ref World.Get<UIStyle>(nodeEntity);
            // Update background color based on selection state
            style.BackgroundColor = isSelected
                ? new Vector4(0.3f, 0.5f, 0.7f, 1f)
                : Vector4.Zero;
        }
    }
}
