using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Systems;

/// <summary>
/// System that steps the physics simulation at a fixed timestep.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the <see cref="SystemPhase.FixedUpdate"/> phase and maintains
/// a consistent physics tick rate regardless of frame rate variation. It uses an
/// accumulator pattern to handle the mismatch between frame times and physics timesteps.
/// </para>
/// <para>
/// The system synchronizes ECS component data to BepuPhysics before each step and
/// reads back the results afterward. For smooth rendering, use the interpolation
/// alpha from <see cref="PhysicsWorld.InterpolationAlpha"/> in your rendering system.
/// </para>
/// </remarks>
public sealed class PhysicsStepSystem : SystemBase
{
    private PhysicsWorld? physicsWorld;

    /// <summary>
    /// Gets the number of physics steps taken in the last frame.
    /// </summary>
    public int StepsTaken { get; private set; }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (World.TryGetExtension<PhysicsWorld>(out var pw))
        {
            physicsWorld = pw;
        }
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        var pw = physicsWorld;
        if (pw == null)
        {
            if (!World.TryGetExtension(out pw))
            {
                return;
            }

            physicsWorld = pw;
        }

        StepsTaken = pw!.Step(deltaTime);
    }
}
