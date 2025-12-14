using KeenEyes;

namespace KeenEyesPlugin;

/// <summary>
/// Example system that processes entities with ExampleComponent.
/// </summary>
/// <remarks>
/// Systems contain logic and process entities that match their queries.
/// Use the [System] attribute to specify phase and ordering.
/// </remarks>
[System(Phase = SystemPhase.Update, Order = 0)]
public partial class ExampleSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<ExampleComponent>().With<ExampleTag>())
        {
            ref var component = ref World.Get<ExampleComponent>(entity);
            component.Value++;
        }
    }
}
