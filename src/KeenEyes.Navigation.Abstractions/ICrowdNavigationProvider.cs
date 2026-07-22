using System.Numerics;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Optional capability interface for navigation providers that support crowd
/// simulation with local avoidance.
/// </summary>
/// <remarks>
/// <para>
/// Providers that implement this interface can steer groups of agents while
/// avoiding inter-agent collisions. Entities are registered with
/// <see cref="TryAddCrowdAgent"/>, given targets via
/// <see cref="RequestCrowdMoveTarget"/>, and advanced once per frame with
/// <see cref="UpdateCrowd"/>. The simulated position and velocity of each agent
/// are read back via <see cref="TryGetCrowdAgentState"/>.
/// </para>
/// <para>
/// Providers without crowd support simply do not implement this interface;
/// callers should fall back to individual agent steering in that case.
/// </para>
/// </remarks>
public interface ICrowdNavigationProvider : INavigationProvider
{
    /// <summary>
    /// Gets the number of agents currently registered in the crowd.
    /// </summary>
    int CrowdAgentCount { get; }

    /// <summary>
    /// Registers an entity as a crowd-simulated agent.
    /// </summary>
    /// <param name="entity">The entity to register.</param>
    /// <param name="position">The agent's current world position.</param>
    /// <param name="agent">The agent's navigation parameters.</param>
    /// <param name="crowdAgent">The agent's crowd avoidance parameters.</param>
    /// <returns>
    /// True if the agent was added (or already registered); false if the
    /// position is not on the navigation mesh or the provider is not ready.
    /// </returns>
    bool TryAddCrowdAgent(Entity entity, Vector3 position, in NavMeshAgent agent, in CrowdAgent crowdAgent);

    /// <summary>
    /// Removes an entity from the crowd simulation.
    /// </summary>
    /// <param name="entity">The entity to remove. Unknown entities are ignored.</param>
    void RemoveCrowdAgent(Entity entity);

    /// <summary>
    /// Requests that a crowd agent move toward the given target position.
    /// </summary>
    /// <param name="entity">The registered crowd agent entity.</param>
    /// <param name="target">The destination in world space.</param>
    /// <returns>
    /// True if the request was accepted; false if the entity is not registered
    /// or the target could not be mapped to the navigation mesh.
    /// </returns>
    bool RequestCrowdMoveTarget(Entity entity, Vector3 target);

    /// <summary>
    /// Cancels a crowd agent's current move target, letting it come to rest.
    /// </summary>
    /// <param name="entity">The registered crowd agent entity.</param>
    /// <returns>True if the agent had its target reset; false if not registered.</returns>
    bool ResetCrowdMoveTarget(Entity entity);

    /// <summary>
    /// Advances the crowd simulation by one step.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update, in seconds.</param>
    void UpdateCrowd(float deltaTime);

    /// <summary>
    /// Gets the simulated state of a crowd agent.
    /// </summary>
    /// <param name="entity">The registered crowd agent entity.</param>
    /// <param name="state">The agent's simulated position and velocities.</param>
    /// <returns>True if the entity is registered in the crowd.</returns>
    bool TryGetCrowdAgentState(Entity entity, out CrowdAgentState state);
}

/// <summary>
/// The simulated state of a crowd agent as computed by the crowd simulation.
/// </summary>
/// <param name="Position">The agent's simulated position on the navigation mesh.</param>
/// <param name="Velocity">The agent's actual velocity after avoidance and collision resolution.</param>
/// <param name="DesiredVelocity">The agent's desired velocity before avoidance was applied.</param>
public readonly record struct CrowdAgentState(
    Vector3 Position,
    Vector3 Velocity,
    Vector3 DesiredVelocity);
