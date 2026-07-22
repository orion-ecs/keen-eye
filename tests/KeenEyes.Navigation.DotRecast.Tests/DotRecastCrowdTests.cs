using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for crowd simulation via <see cref="DotRecastCrowdManager"/> exposed
/// through the <see cref="ICrowdNavigationProvider"/> seam on <see cref="DotRecastProvider"/>.
/// </summary>
public class DotRecastCrowdTests : IDisposable
{
    private const float SlabSize = 40f;
    private const float AgentRadius = 0.6f;
    private const float TimeStep = 0.05f;

    private readonly DotRecastProvider provider;

    public DotRecastCrowdTests()
    {
        provider = CreateSlabProvider();
    }

    public void Dispose()
    {
        provider.Dispose();
    }

    #region Helpers

    private static DotRecastProvider CreateSlabProvider()
    {
        var builder = TestHelper.CreateTestBuilder();
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var mesh = builder.Build(vertices, indices);
        return new DotRecastProvider(mesh, TestHelper.CreateTestConfig());
    }

    private static NavMeshAgent CreateNavAgent() => NavMeshAgent.Create();

    private static CrowdAgent CreateCrowdAgent()
    {
        var crowdAgent = CrowdAgent.Create();
        crowdAgent.Radius = AgentRadius;
        return crowdAgent;
    }

    private bool AddAgent(Entity entity, Vector3 position)
    {
        var agent = CreateNavAgent();
        var crowdAgent = CreateCrowdAgent();
        return provider.TryAddCrowdAgent(entity, position, in agent, in crowdAgent);
    }

    private Vector3 GetPosition(Entity entity)
    {
        provider.TryGetCrowdAgentState(entity, out var state).ShouldBeTrue();
        return state.Position;
    }

    #endregion

    #region Registration Tests

    [Fact]
    public void TryAddCrowdAgent_OnNavMesh_RegistersAgent()
    {
        var entity = new Entity(1, 0);

        AddAgent(entity, new Vector3(10f, 0f, 10f)).ShouldBeTrue();

        provider.CrowdAgentCount.ShouldBe(1);
        provider.TryGetCrowdAgentState(entity, out var state).ShouldBeTrue();
        Vector3.Distance(state.Position, new Vector3(10f, 0f, 10f)).ShouldBeLessThan(0.5f);
    }

    [Fact]
    public void TryAddCrowdAgent_OffNavMesh_ReturnsFalse()
    {
        var entity = new Entity(1, 0);

        AddAgent(entity, new Vector3(500f, 0f, 500f)).ShouldBeFalse();

        provider.CrowdAgentCount.ShouldBe(0);
        provider.TryGetCrowdAgentState(entity, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryAddCrowdAgent_AlreadyRegistered_ReturnsTrueWithoutDuplicate()
    {
        var entity = new Entity(1, 0);

        AddAgent(entity, new Vector3(10f, 0f, 10f)).ShouldBeTrue();
        AddAgent(entity, new Vector3(12f, 0f, 12f)).ShouldBeTrue();

        provider.CrowdAgentCount.ShouldBe(1);
    }

    [Fact]
    public void RemoveCrowdAgent_RegisteredAgent_RemovesFromCrowd()
    {
        var entity = new Entity(1, 0);
        AddAgent(entity, new Vector3(10f, 0f, 10f));

        provider.RemoveCrowdAgent(entity);

        provider.CrowdAgentCount.ShouldBe(0);
        provider.TryGetCrowdAgentState(entity, out _).ShouldBeFalse();
    }

    [Fact]
    public void RequestCrowdMoveTarget_UnknownEntity_ReturnsFalse()
    {
        provider.RequestCrowdMoveTarget(new Entity(99, 0), new Vector3(20f, 0f, 20f)).ShouldBeFalse();
    }

    [Fact]
    public void ResetCrowdMoveTarget_UnknownEntity_ReturnsFalse()
    {
        provider.ResetCrowdMoveTarget(new Entity(99, 0)).ShouldBeFalse();
    }

    #endregion

    #region Simulation Tests

    [Fact]
    public void UpdateCrowd_SingleAgent_ReachesTarget()
    {
        var entity = new Entity(1, 0);
        var target = new Vector3(32f, 0f, 32f);
        AddAgent(entity, new Vector3(8f, 0f, 8f)).ShouldBeTrue();
        provider.RequestCrowdMoveTarget(entity, target).ShouldBeTrue();

        float distance = float.MaxValue;
        for (int i = 0; i < 800; i++)
        {
            provider.UpdateCrowd(TimeStep);
            distance = Vector3.Distance(GetPosition(entity), target);
            if (distance <= 0.5f)
            {
                break;
            }
        }

        distance.ShouldBeLessThanOrEqualTo(0.5f);
    }

    [Fact]
    public void UpdateCrowd_TwoOpposingAgents_DeviateWithoutInterpenetrating()
    {
        var left = new Entity(1, 0);
        var right = new Entity(2, 0);
        var leftStart = new Vector3(10f, 0f, 20f);
        var rightStart = new Vector3(30f, 0f, 20.05f);

        AddAgent(left, leftStart).ShouldBeTrue();
        AddAgent(right, rightStart).ShouldBeTrue();
        provider.RequestCrowdMoveTarget(left, new Vector3(30f, 0f, 20f)).ShouldBeTrue();
        provider.RequestCrowdMoveTarget(right, new Vector3(10f, 0f, 20f)).ShouldBeTrue();

        float minSeparation = float.MaxValue;
        float maxDeviation = 0f;

        for (int i = 0; i < 400; i++)
        {
            provider.UpdateCrowd(TimeStep);

            var leftPos = GetPosition(left);
            var rightPos = GetPosition(right);
            minSeparation = MathF.Min(minSeparation, Vector3.Distance(leftPos, rightPos));
            maxDeviation = MathF.Max(maxDeviation, MathF.Abs(leftPos.Z - 20f));
            maxDeviation = MathF.Max(maxDeviation, MathF.Abs(rightPos.Z - 20f));
        }

        // Agents must never interpenetrate: separation stays at or above the
        // sum of their radii, with tolerance for soft collision resolution.
        minSeparation.ShouldBeGreaterThanOrEqualTo((AgentRadius + AgentRadius) * 0.8f);

        // Avoidance must have steered them off the straight corridor line
        maxDeviation.ShouldBeGreaterThan(0.1f);

        // Both agents must still have crossed to their respective targets
        Vector3.Distance(GetPosition(left), new Vector3(30f, 0f, 20f)).ShouldBeLessThan(2f);
        Vector3.Distance(GetPosition(right), new Vector3(10f, 0f, 20f)).ShouldBeLessThan(2f);
    }

    [Fact]
    public void UpdateCrowd_ManyAgentsToOneTarget_DoNotStack()
    {
        const int agentCount = 6;
        var center = new Vector3(20f, 0f, 20f);
        var entities = new Entity[agentCount];

        for (int i = 0; i < agentCount; i++)
        {
            float angle = i * MathF.Tau / agentCount;
            var start = center + new Vector3(MathF.Cos(angle) * 8f, 0f, MathF.Sin(angle) * 8f);

            entities[i] = new Entity(i + 1, 0);
            AddAgent(entities[i], start).ShouldBeTrue();
            provider.RequestCrowdMoveTarget(entities[i], center).ShouldBeTrue();
        }

        for (int step = 0; step < 400; step++)
        {
            provider.UpdateCrowd(TimeStep);
        }

        // Every agent should have made progress toward the shared target
        foreach (var entity in entities)
        {
            Vector3.Distance(GetPosition(entity), center).ShouldBeLessThan(5f);
        }

        // No two agents may occupy the same spot: pairwise separation stays
        // near the sum of radii even when crowding a single target.
        for (int i = 0; i < agentCount; i++)
        {
            for (int j = i + 1; j < agentCount; j++)
            {
                float separation = Vector3.Distance(GetPosition(entities[i]), GetPosition(entities[j]));
                separation.ShouldBeGreaterThanOrEqualTo((AgentRadius + AgentRadius) * 0.6f);
            }
        }
    }

    #endregion

    #region Mesh Replacement Tests

    [Fact]
    public void SetNavMesh_WithRegisteredAgents_PreservesAgentsAndTargets()
    {
        var entity = new Entity(1, 0);
        var target = new Vector3(30f, 0f, 20f);
        AddAgent(entity, new Vector3(10f, 0f, 20f)).ShouldBeTrue();
        provider.RequestCrowdMoveTarget(entity, target).ShouldBeTrue();

        // Let the agent start moving, then replace the mesh mid-run
        for (int i = 0; i < 40; i++)
        {
            provider.UpdateCrowd(TimeStep);
        }

        var builder = TestHelper.CreateTestBuilder();
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        provider.SetNavMesh(builder.Build(vertices, indices));

        provider.CrowdAgentCount.ShouldBe(1);
        provider.TryGetCrowdAgentState(entity, out _).ShouldBeTrue();

        // The move target must survive the mesh swap without a new request
        float distance = float.MaxValue;
        for (int i = 0; i < 600; i++)
        {
            provider.UpdateCrowd(TimeStep);
            distance = Vector3.Distance(GetPosition(entity), target);
            if (distance <= 0.5f)
            {
                break;
            }
        }

        distance.ShouldBeLessThanOrEqualTo(0.5f);
    }

    #endregion
}
