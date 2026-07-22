using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// A marker to draw on the ASCII track view: the live car or a ghost.
/// </summary>
/// <param name="Symbol">The single character used to plot this marker.</param>
/// <param name="Label">A human-readable name shown in the legend.</param>
/// <param name="Position">The marker's world-space position.</param>
public readonly record struct TrackMarker(char Symbol, string Label, Vector3 Position);

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

    /// <summary>
    /// Builds the ASCII view for the given markers.
    /// </summary>
    /// <param name="markers">The markers to plot on the track.</param>
    /// <returns>A multi-line string containing the rendered view and legend.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="markers"/> is null.</exception>
    public string Render(IReadOnlyList<TrackMarker> markers)
    {
        ArgumentNullException.ThrowIfNull(markers);

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
