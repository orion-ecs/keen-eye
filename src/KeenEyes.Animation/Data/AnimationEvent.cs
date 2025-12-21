namespace KeenEyes.Animation.Data;

/// <summary>
/// An event that fires at a specific time during an animation.
/// </summary>
/// <param name="Time">The time in seconds when the event fires.</param>
/// <param name="EventName">The name identifier for this event.</param>
/// <param name="Parameter">Optional string parameter for the event.</param>
public readonly record struct AnimationEvent(float Time, string EventName, string? Parameter = null);

/// <summary>
/// A track of animation events within a clip.
/// </summary>
public sealed class AnimationEventTrack
{
    private readonly List<AnimationEvent> events = [];

    /// <summary>
    /// Gets the events in this track, sorted by time.
    /// </summary>
    public IReadOnlyList<AnimationEvent> Events => events;

    /// <summary>
    /// Adds an event to the track.
    /// </summary>
    /// <param name="time">The time when the event fires.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="parameter">Optional parameter.</param>
    public void AddEvent(float time, string eventName, string? parameter = null)
    {
        events.Add(new AnimationEvent(time, eventName, parameter));
        // Keep sorted by time
        events.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    /// <summary>
    /// Gets events that occur within a time range (for detecting events during a frame).
    /// </summary>
    /// <param name="previousTime">The previous frame's time.</param>
    /// <param name="currentTime">The current frame's time.</param>
    /// <param name="result">The list to populate with triggered events.</param>
    public void GetEventsInRange(float previousTime, float currentTime, List<AnimationEvent> result)
    {
        // Handle normal forward playback
        if (currentTime >= previousTime)
        {
            foreach (var evt in events)
            {
                if (evt.Time > previousTime && evt.Time <= currentTime)
                {
                    result.Add(evt);
                }
            }
        }
        else
        {
            // Wrapped around (looping) - get events from previous to end, then 0 to current
            foreach (var evt in events)
            {
                if (evt.Time > previousTime || evt.Time <= currentTime)
                {
                    result.Add(evt);
                }
            }
        }
    }
}
