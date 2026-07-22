using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// A marker to draw on the ASCII track view: the live car or a ghost.
/// </summary>
/// <param name="Symbol">The single character used to plot this marker.</param>
/// <param name="Label">A human-readable name shown in the legend.</param>
/// <param name="Position">The marker's world-space position.</param>
public readonly record struct TrackMarker(char Symbol, string Label, Vector3 Position);

/// <summary>
/// A fading motion trail to draw behind a ghost on the ASCII track view.
/// </summary>
/// <param name="Points">The trail points, ordered oldest first, newest last.</param>
/// <param name="FadeStart">The opacity at the oldest (tail) point; the head is fully opaque.</param>
/// <param name="Style">The requested trail style (see the per-style notes on the renderer's Render method).</param>
public readonly record struct TrackTrail(IReadOnlyList<Vector3> Points, float FadeStart, TrailStyle Style);

/// <summary>
/// Renders a top-down ASCII view of the circular track with the car and its ghosts.
/// </summary>
/// <remarks>
/// This is the sample's "presentation tier": a console visualization that runs
/// anywhere, with no window or GPU. It maps the track's XZ plane onto a character
/// grid so the relative positions of the car and its ghosts are visible at a glance.
/// </remarks>
public sealed class TrackRenderer
{
    private readonly Track track;
    private readonly int width;
    private readonly int height;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackRenderer"/> class.
    /// </summary>
    /// <param name="track">The track to draw.</param>
    /// <param name="width">Grid width in characters.</param>
    /// <param name="height">Grid height in characters.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="track"/> is null.</exception>
    public TrackRenderer(Track track, int width = 51, int height = 25)
    {
        ArgumentNullException.ThrowIfNull(track);
        this.track = track;
        this.width = width;
        this.height = height;
    }

    // Glyph ramp for trail age, faintest (oldest) to boldest (newest). Renderers on a
    // real GPU would fade an alpha value instead; on a character grid we quantize the
    // computed opacity onto these glyphs so the direction of travel reads at a glance.
    private static readonly char[] trailGlyphs = [':', '+', '*'];

    /// <summary>
    /// Builds the ASCII view for the given markers and ghost trails.
    /// </summary>
    /// <param name="markers">The markers to plot on the track.</param>
    /// <param name="trails">
    /// The ghost trails to draw beneath the markers. Pass an empty list for no trails.
    /// </param>
    /// <returns>A multi-line string containing the rendered view and legend.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <remarks>
    /// Trails are drawn on top of the racing line but beneath the car/ghost markers, so
    /// a head marker always wins its cell. Each trail point is quantized to a glyph by
    /// its age-based opacity (fading from <see cref="TrackTrail.FadeStart"/> at the tail
    /// to fully opaque at the head). <see cref="TrailStyle.Dots"/> draws every other
    /// point for a dashed look; <see cref="TrailStyle.Line"/>, <see cref="TrailStyle.Gradient"/>,
    /// and <see cref="TrailStyle.Ribbon"/> draw a continuous run of glyphs. This 2D view
    /// has no oriented 3D primitive, so <see cref="TrailStyle.Ribbon"/> falls back to the
    /// line rendering.
    /// </remarks>
    public string Render(IReadOnlyList<TrackMarker> markers, IReadOnlyList<TrackTrail> trails)
    {
        ArgumentNullException.ThrowIfNull(markers);
        ArgumentNullException.ThrowIfNull(trails);

        var grid = new char[height, width];
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                grid[row, col] = ' ';
            }
        }

        // Draw the racing line by sampling the track at many points.
        var samples = width * height;
        for (var i = 0; i < samples; i++)
        {
            var distance = track.Length * i / samples;
            Plot(grid, track.PositionAt(distance), '.');
        }

        // Draw ghost trails on top of the racing line but under the markers below.
        foreach (var trail in trails)
        {
            PlotTrail(grid, trail);
        }

        // Overlay the markers on top of the racing line, in the order given (the
        // live car first). When distance-synced ghosts share the car's exact track
        // position, later markers would land on an occupied cell; those are left
        // off the map (and the legend) so the car stays visible - the temporal gap
        // to each ghost is reported separately by the caller.
        var occupied = new HashSet<(int Row, int Col)>();
        var plotted = new List<TrackMarker>();
        foreach (var marker in markers)
        {
            if (TryPlot(grid, occupied, marker.Position, marker.Symbol))
            {
                plotted.Add(marker);
            }
        }

        var builder = new StringBuilder();
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                builder.Append(grid[row, col]);
            }
            builder.Append('\n');
        }

        builder.Append("  Legend: ");
        for (var i = 0; i < plotted.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("   ");
            }
            builder.Append(plotted[i].Symbol).Append('=').Append(plotted[i].Label);
        }

        return builder.ToString();
    }

    private bool TryPlot(char[,] grid, HashSet<(int Row, int Col)> occupied, Vector3 position, char symbol)
    {
        // Map world XZ (range roughly [-R, R]) onto the character grid.
        var normalizedX = (position.X + track.Radius) / (2f * track.Radius);
        var normalizedZ = (position.Z + track.Radius) / (2f * track.Radius);

        var col = (int)MathF.Round(normalizedX * (width - 1));
        var row = (int)MathF.Round(normalizedZ * (height - 1));

        col = Math.Clamp(col, 0, width - 1);
        row = Math.Clamp(row, 0, height - 1);

        if (!occupied.Add((row, col)))
        {
            return false;
        }

        grid[row, col] = symbol;
        return true;
    }

    private void PlotTrail(char[,] grid, TrackTrail trail)
    {
        var points = trail.Points;
        if (points.Count == 0)
        {
            return;
        }

        // Dots draw a dashed trail; every other line style draws a continuous run.
        var step = trail.Style == TrailStyle.Dots ? 2 : 1;

        for (var i = 0; i < points.Count; i += step)
        {
            // Opacity ramps from FadeStart at the tail to 1.0 at the head. With a
            // single point, treat it as the head (fully opaque).
            var age = points.Count > 1 ? (float)i / (points.Count - 1) : 1f;
            var opacity = trail.FadeStart + (1f - trail.FadeStart) * age;

            var glyph = trailGlyphs[Math.Clamp(
                (int)(opacity * trailGlyphs.Length),
                0,
                trailGlyphs.Length - 1)];

            Plot(grid, points[i], glyph);
        }
    }

    private void Plot(char[,] grid, Vector3 position, char symbol)
    {
        // Map world XZ (range roughly [-R, R]) onto the character grid.
        var normalizedX = (position.X + track.Radius) / (2f * track.Radius);
        var normalizedZ = (position.Z + track.Radius) / (2f * track.Radius);

        var col = (int)MathF.Round(normalizedX * (width - 1));
        var row = (int)MathF.Round(normalizedZ * (height - 1));

        col = Math.Clamp(col, 0, width - 1);
        row = Math.Clamp(row, 0, height - 1);

        grid[row, col] = symbol;
    }
}
