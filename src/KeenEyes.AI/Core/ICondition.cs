namespace KeenEyes.AI;

/// <summary>
/// Interface for conditions that can be evaluated in behavior trees.
/// </summary>
/// <remarks>
/// <para>
/// Conditions are used to check the state of the world or an entity.
/// They are typically used in condition nodes to guard action execution.
/// </para>
/// <para>
/// Navigation conditions like <see cref="Conditions.HasPathCondition"/> implement this interface
/// to check navigation state in behavior trees.
/// </para>
/// </remarks>
public interface ICondition
{
    /// <summary>
    /// Evaluates the condition for the given entity.
    /// </summary>
    /// <param name="entity">The entity to evaluate the condition for.</param>
    /// <param name="blackboard">The blackboard for reading shared state.</param>
    /// <param name="world">The ECS world containing the entity.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    bool Evaluate(Entity entity, Blackboard blackboard, IWorld world);
}
