using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="NavMeshAgent"/>.
/// </summary>
public class NavMeshAgentTests
{
    [Fact]
    public void Create_ReturnsDefaultAgent()
    {
        var agent = NavMeshAgent.Create();

        Assert.Equal(AgentSettings.Default, agent.Settings);
        Assert.Equal(NavAreaMask.All, agent.AreaMask);
        Assert.Equal(3.5f, agent.Speed);
        Assert.Equal(8f, agent.Acceleration);
        Assert.Equal(120f, agent.AngularSpeed);
        Assert.Equal(0.1f, agent.StoppingDistance);
        Assert.True(agent.AutoBraking);
        Assert.True(agent.IsStopped);
    }

    [Fact]
    public void Create_WithSettings_UsesProvidedSettings()
    {
        var settings = AgentSettings.Large;
        var agent = NavMeshAgent.Create(settings, 5.0f);

        Assert.Equal(settings, agent.Settings);
        Assert.Equal(5.0f, agent.Speed);
    }

    [Fact]
    public void SetDestination_UpdatesDestinationAndState()
    {
        var agent = NavMeshAgent.Create();
        var destination = new Vector3(10f, 0f, 10f);

        agent.SetDestination(destination);

        Assert.Equal(destination, agent.Destination);
        Assert.True(agent.PathPending);
        Assert.False(agent.IsStopped);
    }

    [Fact]
    public void Stop_ClearsPathAndStopsAgent()
    {
        var agent = NavMeshAgent.Create();
        agent.SetDestination(new Vector3(10f, 0f, 10f));
        agent.HasPath = true;
        agent.DesiredVelocity = new Vector3(1f, 0f, 0f);
        agent.RemainingDistance = 5f;

        agent.Stop();

        Assert.False(agent.HasPath);
        Assert.False(agent.PathPending);
        Assert.True(agent.IsStopped);
        Assert.Equal(Vector3.Zero, agent.DesiredVelocity);
        Assert.Equal(0f, agent.RemainingDistance);
    }

    [Fact]
    public void Resume_UnstopsAgentWithPath()
    {
        var agent = NavMeshAgent.Create();
        agent.HasPath = true;
        agent.IsStopped = true;

        agent.Resume();

        Assert.False(agent.IsStopped);
    }

    [Fact]
    public void Resume_WithNoPath_StaysStoppedStopped()
    {
        var agent = NavMeshAgent.Create();
        agent.HasPath = false;
        agent.IsStopped = true;

        agent.Resume();

        Assert.True(agent.IsStopped);
    }

    [Fact]
    public void InitialState_HasNoPath()
    {
        var agent = NavMeshAgent.Create();

        Assert.False(agent.HasPath);
        Assert.False(agent.PathPending);
        Assert.False(agent.IsOnNavMesh);
    }
}
