using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// System that drives navigation mesh tile streaming from anchor entities.
/// </summary>
/// <remarks>
/// <para>
/// Each update, entities carrying <see cref="NavMeshStreamingAnchor"/> and
/// <see cref="Transform3D"/> are mirrored into the world's
/// <see cref="NavMeshStreamingManager"/> as streaming anchors, anchors for
/// despawned entities are removed, and the manager applies its budgeted tile
/// residency pass.
/// </para>
/// <para>
/// Registered by <see cref="DotRecastNavigationPlugin"/> when constructed with
/// a streaming manager. Runs early in the update phase so tiles are resident
/// before path requests and agent steering execute.
/// </para>
/// </remarks>
internal sealed class NavMeshStreamingSystem : SystemBase
{
    private NavMeshStreamingManager? manager;
    private HashSet<int> currentAnchors = [];
    private HashSet<int> previousAnchors = [];

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (!World.TryGetExtension<NavMeshStreamingManager>(out var streamingManager) || streamingManager is null)
        {
            throw new InvalidOperationException("NavMeshStreamingSystem requires the NavMeshStreamingManager extension.");
        }

        manager = streamingManager;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (manager == null)
        {
            return;
        }

        currentAnchors.Clear();

        foreach (var entity in World.Query<NavMeshStreamingAnchor, Transform3D>())
        {
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            manager.SetAnchor(entity.Id, transform.Position);
            currentAnchors.Add(entity.Id);
        }

        foreach (int anchorId in previousAnchors)
        {
            if (!currentAnchors.Contains(anchorId))
            {
                manager.RemoveAnchor(anchorId);
            }
        }

        (previousAnchors, currentAnchors) = (currentAnchors, previousAnchors);

        manager.Update();
    }
}
