// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Viewport;

/// <summary>
/// Executes the viewport's plugin gizmo render pass over the renderers registered
/// with the viewport capability.
/// </summary>
/// <remarks>
/// Renderers run in ascending <see cref="IGizmoRenderer.Order"/> (ties keep registration
/// order). A renderer is skipped when it is disabled or when no entity in the scene world
/// satisfies its <see cref="IGizmoRenderer.ShouldRender"/> check.
/// </remarks>
internal static class GizmoRendererPass
{
    /// <summary>
    /// Renders all applicable gizmo renderers with the supplied context.
    /// </summary>
    /// <param name="renderers">The registered gizmo renderers.</param>
    /// <param name="context">The render context for the current frame.</param>
    internal static void Render(IEnumerable<IGizmoRenderer> renderers, GizmoRenderContext context)
    {
        foreach (var renderer in renderers.OrderBy(r => r.Order))
        {
            if (!renderer.IsEnabled)
            {
                continue;
            }

            if (HasRenderableContent(renderer, context.SceneWorld))
            {
                renderer.Render(context);
            }
        }
    }

    private static bool HasRenderableContent(IGizmoRenderer renderer, IWorld sceneWorld)
    {
        foreach (var entity in sceneWorld.GetAllEntities())
        {
            if (renderer.ShouldRender(entity, sceneWorld))
            {
                return true;
            }
        }

        return false;
    }
}
