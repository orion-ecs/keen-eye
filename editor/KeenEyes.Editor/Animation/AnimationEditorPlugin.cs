using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Animation;

/// <summary>
/// Editor plugin that registers the animation debug gizmo renderers in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// This plugin wires up the skeleton and IK chain visualizers so their debug gizmos appear in
/// the viewport for the loaded scene:
/// <list type="bullet">
///   <item><description><see cref="SkeletonGizmoRenderer"/> - bone hierarchy visualization.</description></item>
///   <item><description><see cref="IKGizmoRenderer"/> - IK chains, targets, and pole vectors.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AnimationEditorPlugin : EditorPluginBase
{
    private SkeletonGizmoRenderer? skeletonGizmo;
    private IKGizmoRenderer? ikGizmo;

    /// <inheritdoc/>
    public override string Name => "Animation";

    /// <inheritdoc/>
    public override string? Description => "Skeleton and IK chain visualization gizmos";

    /// <inheritdoc/>
    protected override void OnInitialize(IEditorContext context)
    {
        if (context.TryGetCapability<IViewportCapability>(out var viewport) && viewport != null)
        {
            skeletonGizmo = new SkeletonGizmoRenderer();
            viewport.AddGizmoRenderer(skeletonGizmo);

            ikGizmo = new IKGizmoRenderer();
            viewport.AddGizmoRenderer(ikGizmo);
        }
    }

    /// <inheritdoc/>
    protected override void OnShutdown()
    {
        if (Context != null && Context.TryGetCapability<IViewportCapability>(out var viewport) && viewport != null)
        {
            if (skeletonGizmo != null)
            {
                viewport.RemoveGizmoRenderer(skeletonGizmo);
            }

            if (ikGizmo != null)
            {
                viewport.RemoveGizmoRenderer(ikGizmo);
            }
        }
    }
}
