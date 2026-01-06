using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.FSM;
using KeenEyes.AI.Utility;

namespace KeenEyes.AI;

/// <summary>
/// Plugin that adds AI capabilities to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides comprehensive AI support including:
/// </para>
/// <list type="bullet">
/// <item><description>Finite State Machines via <see cref="StateMachineSystem"/></description></item>
/// <item><description>Behavior Trees via <see cref="BehaviorTreeSystem"/></description></item>
/// <item><description>Utility AI via <see cref="UtilitySystem"/></description></item>
/// </list>
/// <para>
/// The plugin exposes an <see cref="AIContext"/> extension that can be accessed
/// via <c>world.GetExtension&lt;AIContext&gt;()</c> or <c>world.AI</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install with default configuration
/// world.InstallPlugin(new AIPlugin());
///
/// // Create an entity with a state machine
/// var enemy = world.Spawn()
///     .With(StateMachineComponent.Create(enemyStateMachine))
///     .Build();
///
/// // Create an entity with a behavior tree
/// var guard = world.Spawn()
///     .With(BehaviorTreeComponent.Create(guardBehavior))
///     .Build();
///
/// // Create an entity with utility AI
/// var npc = world.Spawn()
///     .With(UtilityComponent.Create(npcUtility))
///     .Build();
///
/// // Access AI context for debugging
/// var ai = world.GetExtension&lt;AIContext&gt;();
/// var stats = ai.GetStatistics();
/// </code>
/// </example>
public sealed class AIPlugin : IWorldPlugin
{
    private AIContext? aiContext;

    /// <inheritdoc/>
    public string Name => "AI";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register AI components
        context.RegisterComponent<StateMachineComponent>();
        context.RegisterComponent<BehaviorTreeComponent>();
        context.RegisterComponent<UtilityComponent>();

        // Create and expose the AI context API
        aiContext = new AIContext(context.World);
        context.SetExtension(aiContext);

        // Register systems in order:
        // - FSM (100): Evaluate state transitions first
        // - Behavior Tree (110): Execute behavior logic
        // - Utility AI (120): Score and select actions
        context.AddSystem<StateMachineSystem>(SystemPhase.Update, order: 100);
        context.AddSystem<BehaviorTreeSystem>(SystemPhase.Update, order: 110);
        context.AddSystem<UtilitySystem>(SystemPhase.Update, order: 120);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Remove extensions
        context.RemoveExtension<AIContext>();

        // Clear reference
        aiContext = null;

        // Systems are automatically cleaned up by PluginManager
    }
}
