using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Events;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for off-mesh link building, pathfinding, serialization, and agent
/// traversal. The core fixture is two walkable slabs separated by a gap that
/// only an off-mesh connection can bridge.
/// </summary>
public class OffMeshLinkTests
{
    private const float TimeStep = 0.05f;

    // Two 10x10 slabs with a 4-unit gap between them along X.
    private static readonly Vector3 linkStart = new(5f, 0f, 5f);
    private static readonly Vector3 linkEnd = new(19f, 0f, 5f);
    private static readonly Vector3 pathStart = new(2f, 0f, 5f);
    private static readonly Vector3 pathEnd = new(22f, 0f, 5f);

    #region Helpers

    /// <summary>
    /// Builds combined box geometry (8 vertices, 12 triangles per box) so that
    /// multiple slabs can be voxelized in a single mesh build.
    /// </summary>
    private static (float[] Vertices, int[] Indices) BuildBoxesGeometry(params (Vector3 Min, Vector3 Max)[] boxes)
    {
        var vertices = new List<float>();
        var indices = new List<int>();

        foreach (var (min, max) in boxes)
        {
            int baseIndex = vertices.Count / 3;

            vertices.AddRange(
            [
                // Bottom face
                min.X, min.Y, min.Z,
                max.X, min.Y, min.Z,
                max.X, min.Y, max.Z,
                min.X, min.Y, max.Z,

                // Top face (walkable surface)
                min.X, max.Y, min.Z,
                max.X, max.Y, min.Z,
                max.X, max.Y, max.Z,
                min.X, max.Y, max.Z
            ]);

            int[] boxIndices =
            [
                // Bottom (normal down)
                0, 2, 1,
                0, 3, 2,

                // Top (normal up - walkable)
                4, 6, 5,
                4, 7, 6,

                // Sides
                0, 1, 5,
                0, 5, 4,
                2, 3, 7,
                2, 7, 6,
                0, 4, 7,
                0, 7, 3,
                1, 2, 6,
                1, 6, 5
            ];

            foreach (int index in boxIndices)
            {
                indices.Add(baseIndex + index);
            }
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    private static (float[] Vertices, int[] Indices) TwoSlabGeometry()
        => BuildBoxesGeometry(
            (new Vector3(0f, -1f, 0f), new Vector3(10f, 0f, 10f)),
            (new Vector3(14f, -1f, 0f), new Vector3(24f, 0f, 10f)));

    private static NavMeshData BuildTwoSlabMesh(IReadOnlyList<OffMeshLinkDefinition>? links, bool tiled = false)
    {
        var config = tiled ? TestHelper.CreateTiledTestConfig() : TestHelper.CreateTestConfig();
        var builder = new DotRecastMeshBuilder(config);
        var (vertices, indices) = TwoSlabGeometry();
        return builder.Build(vertices, indices, offMeshLinks: links);
    }

    private static OffMeshLinkDefinition TwoSlabLink(bool bidirectional = true)
        => new(linkStart, linkEnd, 1f, bidirectional, NavAreaType.OffMeshLink);

    private static bool ContainsOffMeshPoint(NavPath path)
        => path.Any(p => (p.Properties & NavPointProperties.OffMeshConnection) != 0);

    #endregion

    #region Component Tests

    [Fact]
    public void Create_WithEndpoints_ReturnsBidirectionalDefaults()
    {
        var link = OffMeshLink.Create(linkStart, linkEnd);

        Assert.Equal(linkStart, link.Start);
        Assert.Equal(linkEnd, link.End);
        Assert.True(link.Bidirectional);
        Assert.Equal(NavAreaType.OffMeshLink, link.AreaType);
        Assert.True(link.Radius > 0f);
        Assert.True(link.CostModifier.ApproximatelyEquals(1f));
    }

    #endregion

    #region Build Tests

    [Fact]
    public void Build_WithOffMeshLink_PathCrossesDisconnectedSlabs()
    {
        var mesh = BuildTwoSlabMesh([TwoSlabLink()]);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTestConfig());

        var path = provider.FindPath(pathStart, pathEnd, mesh.BuiltForAgent);

        Assert.True(path.IsValid);
        Assert.True(path.IsComplete);
        Assert.True(ContainsOffMeshPoint(path));
    }

    [Fact]
    public void Build_WithoutOffMeshLink_PathDoesNotReachAcrossGap()
    {
        var mesh = BuildTwoSlabMesh(null);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTestConfig());

        var path = provider.FindPath(pathStart, pathEnd, mesh.BuiltForAgent);

        // Without the link the best result is a partial path that stays on the
        // first slab and never reaches the destination.
        Assert.False(path.IsComplete);
        if (path.IsValid)
        {
            Assert.True(path.End.Position.X < 12f);
        }
    }

    [Fact]
    public void Build_WithOneWayLink_ReversePathDoesNotCrossGap()
    {
        var mesh = BuildTwoSlabMesh([TwoSlabLink(bidirectional: false)]);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTestConfig());

        var forward = provider.FindPath(pathStart, pathEnd, mesh.BuiltForAgent);
        var reverse = provider.FindPath(pathEnd, pathStart, mesh.BuiltForAgent);

        Assert.True(forward.IsComplete);
        Assert.True(ContainsOffMeshPoint(forward));

        Assert.False(reverse.IsComplete);
        if (reverse.IsValid)
        {
            Assert.True(reverse.End.Position.X > 12f);
        }
    }

    [Fact]
    public void Build_TiledWithOffMeshLink_PathCrossesDisconnectedSlabs()
    {
        var mesh = BuildTwoSlabMesh([TwoSlabLink()], tiled: true);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTiledTestConfig());

        var path = provider.FindPath(pathStart, pathEnd, mesh.BuiltForAgent);

        Assert.True(path.IsValid);
        Assert.True(path.IsComplete);
        Assert.True(ContainsOffMeshPoint(path));
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void Serialize_WithOffMeshLink_RoundTripPreservesConnection()
    {
        var mesh = BuildTwoSlabMesh([TwoSlabLink()]);

        var data = mesh.Serialize();
        var restored = NavMeshData.Deserialize(data);
        using var provider = new DotRecastProvider(restored, TestHelper.CreateTestConfig());

        var path = provider.FindPath(pathStart, pathEnd, restored.BuiltForAgent);

        Assert.True(path.IsValid);
        Assert.True(path.IsComplete);
        Assert.True(ContainsOffMeshPoint(path));
    }

    #endregion

    #region Area Cost Tests

    /// <summary>
    /// Builds a U-shaped walkable surface whose arms are also joined by an
    /// off-mesh link, so a walkable alternative to the link always exists.
    /// </summary>
    private static NavMeshData BuildUShapeMesh(out OffMeshLinkDefinition link)
    {
        link = new OffMeshLinkDefinition(
            new Vector3(5f, 0f, 35f),
            new Vector3(25f, 0f, 35f),
            1f,
            Bidirectional: true,
            NavAreaType.OffMeshLink);

        var (vertices, indices) = BuildBoxesGeometry(
            (new Vector3(0f, -1f, 0f), new Vector3(30f, 0f, 10f)),
            (new Vector3(0f, -1f, 8f), new Vector3(10f, 0f, 40f)),
            (new Vector3(20f, -1f, 8f), new Vector3(30f, 0f, 40f)));

        var builder = TestHelper.CreateTestBuilder();
        return builder.Build(vertices, indices, offMeshLinks: [link]);
    }

    // Query endpoints sit a little away from the link endpoints so the funnel
    // emits the link entry as its own (flagged) corner vertex.
    private static readonly Vector3 uShapeStart = new(5f, 0f, 38f);
    private static readonly Vector3 uShapeEnd = new(25f, 0f, 38f);

    [Fact]
    public void FindPath_DefaultOffMeshLinkCost_UsesLinkShortcut()
    {
        var mesh = BuildUShapeMesh(out _);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTestConfig());

        var path = provider.FindPath(uShapeStart, uShapeEnd, mesh.BuiltForAgent);

        Assert.True(path.IsComplete);
        Assert.True(ContainsOffMeshPoint(path));
    }

    [Fact]
    public void FindPath_HighOffMeshLinkAreaCost_PrefersWalkableRoute()
    {
        var mesh = BuildUShapeMesh(out var link);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTestConfig());

        provider.SetAreaCost(NavAreaType.OffMeshLink, 20f);
        var path = provider.FindPath(uShapeStart, uShapeEnd, mesh.BuiltForAgent);

        Assert.True(path.IsComplete);
        Assert.False(ContainsOffMeshPoint(path));

        // The walkable route goes around the U, so it is much longer than the
        // direct link distance.
        Assert.True(path.Length > Vector3.Distance(link.Start, link.End) * 2f);
    }

    #endregion

    #region Agent Traversal Tests

    [Fact]
    public void Update_AgentPathIncludesOffMeshLink_TraversesLinkAndFiresEvents()
    {
        var mesh = BuildTwoSlabMesh([TwoSlabLink()]);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTestConfig());

        using var world = new World();
        world.InstallPlugin(new NavigationPlugin(new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = provider
        }));

        var events = new List<string>();
        var started = new List<OffMeshLinkTraversalStarted>();
        var completed = new List<OffMeshLinkTraversalCompleted>();

        using var startedSub = world.Subscribe<OffMeshLinkTraversalStarted>(evt =>
        {
            started.Add(evt);
            events.Add("started");
        });
        using var completedSub = world.Subscribe<OffMeshLinkTraversalCompleted>(evt =>
        {
            completed.Add(evt);
            events.Add("completed");
        });

        // A matching link entity lets the traversal resolve its cost modifier.
        var linkComponent = OffMeshLink.Create(linkStart, linkEnd);
        linkComponent.Radius = 1f;
        world.Spawn().With(linkComponent).Build();

        var agentComponent = NavMeshAgent.Create();
        agentComponent.StoppingDistance = 0.5f;
        var agent = world.Spawn()
            .With(new Transform3D(new Vector3(3f, 0f, 5f), Quaternion.Identity, Vector3.One))
            .With(agentComponent)
            .Build();

        var context = world.GetExtension<NavigationContext>();
        context.SetDestination(agent, new Vector3(21f, 0f, 5f));

        bool sawTraversalState = false;
        for (int i = 0; i < 1000; i++)
        {
            world.Update(TimeStep);

            if (context.TryGetAgentState(agent, out var state) && state.IsTraversingOffMeshLink)
            {
                sawTraversalState = true;
            }

            if (world.Get<NavMeshAgent>(agent).IsStopped && !world.Get<NavMeshAgent>(agent).PathPending)
            {
                break;
            }
        }

        Assert.True(sawTraversalState, "Agent never entered the off-mesh traversal state");
        Assert.Single(started);
        Assert.Single(completed);
        Assert.Equal(["started", "completed"], events);

        Assert.Equal(agent, started[0].Entity);
        Assert.Equal(agent, completed[0].Entity);
        Assert.Equal(NavAreaType.OffMeshLink, started[0].AreaType);
        Assert.True(Vector3.Distance(started[0].Start, linkStart) < 1.5f);
        Assert.True(Vector3.Distance(started[0].End, linkEnd) < 1.5f);

        // The agent must have crossed the gap onto the far slab.
        var position = world.Get<Transform3D>(agent).Position;
        Assert.True(position.X > 14f, $"Agent did not reach the far slab (X = {position.X})");
    }

    [Fact]
    public void Update_CrowdAgentPathIncludesOffMeshLink_TraversesLinkAndFiresEvents()
    {
        var mesh = BuildTwoSlabMesh([TwoSlabLink()]);
        using var provider = new DotRecastProvider(mesh, TestHelper.CreateTestConfig());

        using var world = new World();
        world.InstallPlugin(new NavigationPlugin(new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = provider
        }));

        var events = new List<string>();
        var started = new List<OffMeshLinkTraversalStarted>();
        var completed = new List<OffMeshLinkTraversalCompleted>();

        using var startedSub = world.Subscribe<OffMeshLinkTraversalStarted>(evt =>
        {
            started.Add(evt);
            events.Add("started");
        });
        using var completedSub = world.Subscribe<OffMeshLinkTraversalCompleted>(evt =>
        {
            completed.Add(evt);
            events.Add("completed");
        });

        // A matching link entity lets the crowd traversal resolve its area type.
        var linkComponent = OffMeshLink.Create(linkStart, linkEnd);
        linkComponent.Radius = 1f;
        world.Spawn().With(linkComponent).Build();

        var agentComponent = NavMeshAgent.Create();
        agentComponent.StoppingDistance = 0.5f;
        var crowdComponent = CrowdAgent.Create();
        crowdComponent.Radius = 0.6f;
        var agent = world.Spawn()
            .With(new Transform3D(new Vector3(3f, 0f, 5f), Quaternion.Identity, Vector3.One))
            .With(agentComponent)
            .With(crowdComponent)
            .Build();

        var context = world.GetExtension<NavigationContext>();
        context.SetDestination(agent, new Vector3(21f, 0f, 5f));

        bool sawTraversalState = false;
        for (int i = 0; i < 1000; i++)
        {
            world.Update(TimeStep);

            if (provider.TryGetCrowdAgentState(agent, out var state) && state.IsTraversingOffMeshLink)
            {
                sawTraversalState = true;
            }

            if (world.Get<NavMeshAgent>(agent).IsStopped && !world.Get<NavMeshAgent>(agent).PathPending)
            {
                break;
            }
        }

        // The entity was steered by the crowd simulation, not plain steering.
        Assert.Equal(1, provider.CrowdAgentCount);

        Assert.True(sawTraversalState, "Crowd agent never entered the off-mesh traversal state");
        Assert.Single(started);
        Assert.Single(completed);
        Assert.Equal(["started", "completed"], events);

        Assert.Equal(agent, started[0].Entity);
        Assert.Equal(agent, completed[0].Entity);
        Assert.Equal(NavAreaType.OffMeshLink, started[0].AreaType);
        Assert.Equal(NavAreaType.OffMeshLink, completed[0].AreaType);
        Assert.Equal(started[0].Start, completed[0].Start);
        Assert.Equal(started[0].End, completed[0].End);
        Assert.True(Vector3.Distance(started[0].Start, linkStart) < 1.5f);
        Assert.True(Vector3.Distance(started[0].End, linkEnd) < 1.5f);

        // The agent must have crossed the gap onto the far slab.
        var position = world.Get<Transform3D>(agent).Position;
        Assert.True(position.X > 14f, $"Crowd agent did not reach the far slab (X = {position.X})");
    }

    #endregion
}
