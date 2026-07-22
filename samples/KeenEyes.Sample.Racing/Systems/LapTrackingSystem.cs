using System;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// Advances each car's lap timer and detects lap completion.
/// </summary>
/// <remarks>
/// A lap is complete once the car's <see cref="TrackPosition.Distance"/> reaches
/// the track length. The final lap time is latched into
/// <see cref="LapTimer.FinishedSeconds"/> so the driver loop can stop the lap and
/// compare it against ghosts.
/// </remarks>
public sealed class LapTrackingSystem : SystemBase
{
    private readonly float lapLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="LapTrackingSystem"/> class.
    /// </summary>
    /// <param name="lapLength">The length of one lap, in world units.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when lap length is not positive.</exception>
    public LapTrackingSystem(float lapLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lapLength);
        this.lapLength = lapLength;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<TrackPosition, LapTimer>())
        {
            ref var trackPosition = ref World.Get<TrackPosition>(entity);
            ref var lapTimer = ref World.Get<LapTimer>(entity);

            if (lapTimer.Finished)
            {
                continue;
            }

            lapTimer.ElapsedSeconds += deltaTime;

            if (trackPosition.Distance >= lapLength)
            {
                lapTimer.Finished = true;
                lapTimer.FinishedSeconds = lapTimer.ElapsedSeconds;
            }
        }
    }
}
