using KeenEyes.Common;

namespace KeenEyesPlugin;

/// <summary>
/// Example system that uses common components from KeenEyes.Common.
/// </summary>
/// <remarks>
/// This file is only included when the --IncludeCommon option is specified.
/// KeenEyes.Common provides ready-to-use components like Transform2D, Velocity2D, etc.
/// </remarks>
[System(Phase = SystemPhase.Update, Order = 10)]
public partial class CommonComponentSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Process entities with Transform2D and Velocity2D from KeenEyes.Common
        foreach (var entity in World.Query<Transform2D, Velocity2D>())
        {
            ref var transform = ref World.Get<Transform2D>(entity);
            ref readonly var velocity = ref World.Get<Velocity2D>(entity);

            transform.Position.X += velocity.X * deltaTime;
            transform.Position.Y += velocity.Y * deltaTime;
        }
    }
}
