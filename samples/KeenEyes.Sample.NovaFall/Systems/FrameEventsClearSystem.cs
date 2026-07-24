namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Zeroes the <see cref="FrameEvents"/> singleton at the start of each frame.
/// </summary>
/// <remarks>
/// Events published during frame N's Update phase are consumed by simulation
/// systems the same frame and by EarlyUpdate systems (camera trauma, hitstop) at
/// the start of frame N+1. This system runs in EarlyUpdate AFTER those consumers,
/// so every event is visible to every consumer exactly once and never twice.
/// </remarks>
public sealed class FrameEventsClearSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        World.GetSingleton<FrameEvents>() = default;
    }
}
