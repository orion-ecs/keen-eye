using System.Numerics;
using KeenEyes;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// Utility class for UI hit testing (finding elements at a screen position).
/// </summary>
/// <remarks>
/// <para>
/// Hit testing is used to determine which UI element is under the mouse cursor or touch point.
/// Elements are tested in reverse render order (topmost first) and must have
/// <see cref="UIElement.RaycastTarget"/> set to true to be considered.
/// </para>
/// </remarks>
/// <param name="world">The world to perform hit tests on.</param>
public sealed class UIHitTester(IWorld world)
{

    /// <summary>
    /// Finds the topmost UI element at the specified screen position.
    /// </summary>
    /// <param name="screenPosition">The position to test in screen coordinates.</param>
    /// <returns>The entity at the position, or <see cref="Entity.Null"/> if none found.</returns>
    /// <remarks>
    /// <para>
    /// This method only considers elements with <see cref="UIElement.RaycastTarget"/> set to true
    /// and that are not hidden or disabled.
    /// </para>
    /// <para>
    /// Elements are sorted by depth (hierarchy depth × 10000 + LocalZIndex) and the topmost
    /// element is returned.
    /// </para>
    /// </remarks>
    public Entity HitTest(Vector2 screenPosition)
    {
        Entity topHit = Entity.Null;
        int topDepth = int.MinValue;

        // Find all root canvases
        foreach (var rootEntity in world.Query<UIElement, UIRect, UIRootTag>())
        {
            ref readonly var element = ref world.Get<UIElement>(rootEntity);
            if (!element.Visible)
            {
                continue;
            }

            if (world.Has<UIHiddenTag>(rootEntity))
            {
                continue;
            }

            // Test this root and its descendants
            var hit = HitTestRecursive(rootEntity, screenPosition, 0, ref topDepth);
            if (hit.IsValid)
            {
                topHit = hit;
            }
        }

        return topHit;
    }

    /// <summary>
    /// Finds all UI elements at the specified screen position.
    /// </summary>
    /// <param name="screenPosition">The position to test in screen coordinates.</param>
    /// <returns>A list of entities at the position, sorted by depth (topmost first).</returns>
    public List<Entity> HitTestAll(Vector2 screenPosition)
    {
        var hits = new List<(Entity Entity, int Depth)>();

        // Find all root canvases
        foreach (var rootEntity in world.Query<UIElement, UIRect, UIRootTag>())
        {
            ref readonly var element = ref world.Get<UIElement>(rootEntity);
            if (!element.Visible)
            {
                continue;
            }

            if (world.Has<UIHiddenTag>(rootEntity))
            {
                continue;
            }

            // Test this root and its descendants
            HitTestRecursiveAll(rootEntity, screenPosition, 0, hits);
        }

        // Sort by depth (highest first) and return entities
        return hits
            .OrderByDescending(h => h.Depth)
            .Select(h => h.Entity)
            .ToList();
    }

    private Entity HitTestRecursive(Entity entity, Vector2 position, int depth, ref int topDepth)
    {
        Entity topHit = Entity.Null;

        // Check children first (they render on top) - using IWorld.GetChildren
        foreach (var child in world.GetChildren(entity))
        {
            if (!world.Has<UIElement>(child) || !world.Has<UIRect>(child))
            {
                continue;
            }

            if (world.Has<UIHiddenTag>(child))
            {
                continue;
            }

            ref readonly var childElement = ref world.Get<UIElement>(child);
            if (!childElement.Visible)
            {
                continue;
            }

            var hit = HitTestRecursive(child, position, depth + 1, ref topDepth);
            if (hit.IsValid)
            {
                topHit = hit;
            }
        }

        // If a child was hit, return it (children are on top)
        if (topHit.IsValid)
        {
            return topHit;
        }

        // Check this entity
        ref readonly var element = ref world.Get<UIElement>(entity);
        if (!element.RaycastTarget)
        {
            return Entity.Null;
        }

        ref readonly var rect = ref world.Get<UIRect>(entity);
        if (!rect.ComputedBounds.Contains(position))
        {
            return Entity.Null;
        }

        // Calculate depth score
        int depthScore = depth * 10000 + rect.LocalZIndex;
        if (depthScore > topDepth)
        {
            topDepth = depthScore;
            return entity;
        }

        return Entity.Null;
    }

    private void HitTestRecursiveAll(Entity entity, Vector2 position, int depth, List<(Entity, int)> hits)
    {
        // Check this entity
        ref readonly var element = ref world.Get<UIElement>(entity);
        ref readonly var rect = ref world.Get<UIRect>(entity);

        if (element.RaycastTarget && rect.ComputedBounds.Contains(position))
        {
            int depthScore = depth * 10000 + rect.LocalZIndex;
            hits.Add((entity, depthScore));
        }

        // Check children - using IWorld.GetChildren
        foreach (var child in world.GetChildren(entity))
        {
            if (!world.Has<UIElement>(child) || !world.Has<UIRect>(child))
            {
                continue;
            }

            if (world.Has<UIHiddenTag>(child))
            {
                continue;
            }

            ref readonly var childElement = ref world.Get<UIElement>(child);
            if (!childElement.Visible)
            {
                continue;
            }

            HitTestRecursiveAll(child, position, depth + 1, hits);
        }
    }
}
