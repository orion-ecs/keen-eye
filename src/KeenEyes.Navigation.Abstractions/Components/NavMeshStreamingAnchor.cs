// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Navigation.Abstractions.Components;

/// <summary>
/// Component that marks an entity as a navigation mesh streaming anchor.
/// </summary>
/// <remarks>
/// <para>
/// Navigation providers that support tile streaming keep navmesh tiles resident
/// around the positions of anchor entities (typically players or cameras) and
/// unload tiles that fall out of range. Attach this component alongside a
/// transform to any entity whose surroundings must remain navigable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// world.Spawn("Player")
///     .With(new Transform3D(spawnPoint, Quaternion.Identity, Vector3.One))
///     .With(new NavMeshStreamingAnchor())
///     .Build();
/// </code>
/// </example>
public struct NavMeshStreamingAnchor : IComponent
{
}
