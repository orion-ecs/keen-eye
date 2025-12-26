namespace KeenEyes.Replay;

/// <summary>
/// Internal system that finalizes frame recording at the end of each update cycle.
/// </summary>
/// <remarks>
/// This system runs at <see cref="int.MaxValue"/> order to ensure it executes
/// after all other systems in the Update phase, allowing it to properly capture
/// the frame boundary.
/// </remarks>
internal sealed class ReplayFrameEndSystem(ReplayRecorder recorder, ReplayPlugin plugin) : ISystem
{
    public bool Enabled { get; set; } = true;

    public void Initialize(IWorld world)
    {
        // No initialization needed
    }

    public void Update(float deltaTime)
    {
        recorder.EndFrame(deltaTime);
        plugin.ResetFrameTracking();
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
