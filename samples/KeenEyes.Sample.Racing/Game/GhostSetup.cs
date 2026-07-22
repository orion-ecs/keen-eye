using System;
using System.Numerics;
using KeenEyes.Replay;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// Helpers for turning a recorded lap into a ghost and for the visual styling of
/// the ghosts shown during a race.
/// </summary>
/// <remarks>
/// The visual configs are the payload a real renderer would consume: a tint, an
/// opacity and a label per ghost. The ghost system itself never renders anything -
/// it only carries these hints so the game (here, the console view) can present
/// each ghost distinctly.
/// </remarks>
public static class GhostSetup
{
    /// <summary>The entity name recorded for the player's car, used for extraction.</summary>
    public const string CarEntityName = "RaceCar";

    /// <summary>Gold, mostly transparent styling for the fastest recorded lap.</summary>
    /// <remarks>
    /// Enables a gradient racing-line trail so the fastest lap's path is easy to trace.
    /// </remarks>
    public static GhostVisualConfig BestLap { get; } = new()
    {
        TintColor = new Vector4(1f, 0.84f, 0f, 1f),
        Opacity = 0.4f,
        Label = "Best Lap",
        ShowTrail = true,
        TrailLength = 40,
        TrailFadeStart = 0.25f,
        TrailStyle = TrailStyle.Gradient,
    };

    /// <summary>Cyan, semi-transparent styling for the immediately preceding lap.</summary>
    /// <remarks>
    /// Enables a shorter dotted trail to distinguish it from the best lap's line.
    /// </remarks>
    public static GhostVisualConfig PreviousLap { get; } = new()
    {
        TintColor = new Vector4(0f, 0.7f, 1f, 1f),
        Opacity = 0.55f,
        Label = "Previous Lap",
        ShowTrail = true,
        TrailLength = 24,
        TrailFadeStart = 0.4f,
        TrailStyle = TrailStyle.Dots,
    };

    /// <summary>
    /// Extracts the car's ghost from a recorded lap and writes it to a
    /// <c>.keghost</c> file.
    /// </summary>
    /// <param name="replay">The recorded lap.</param>
    /// <param name="path">Destination path for the <c>.keghost</c> file.</param>
    /// <returns>The extracted ghost data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the car could not be extracted (for example, if no snapshots
    /// captured its <see cref="KeenEyes.Common.Transform3D"/>).
    /// </exception>
    public static GhostData ExtractAndSave(ReplayData replay, string path)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(path);

        var extractor = new GhostExtractor();
        var ghost = extractor.ExtractGhost(replay, CarEntityName)
            ?? throw new InvalidOperationException(
                $"Could not extract a ghost for '{CarEntityName}'. Ensure the replay " +
                "captured keyframe snapshots containing the car's Transform3D.");

        GhostFileFormat.WriteToFile(path, ghost);
        return ghost;
    }
}
