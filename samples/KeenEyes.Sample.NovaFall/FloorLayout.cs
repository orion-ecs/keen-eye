namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Pure functions describing the procedural floor layout of a run.
/// </summary>
/// <remarks>
/// Both <see cref="FloorScrollSystem"/> (spawning floors during play) and the
/// headless <c>--simulate</c> mode (verifying determinism) call the same function,
/// guaranteeing that what the test asserts is exactly what the game spawns.
/// </remarks>
public static class FloorLayout
{
    /// <summary>
    /// Computes the gap for a given floor of a run.
    /// </summary>
    /// <param name="seed">The run seed from <see cref="RunConfig"/>.</param>
    /// <param name="floorIndex">The sequential index of the floor (0 = first floor).</param>
    /// <returns>The gap center X and gap width in design units.</returns>
    public static (float GapCenterX, float GapWidth) GapForFloor(ulong seed, int floorIndex)
    {
        var rng = SeededGenerator.ForFloor(seed, floorIndex);

        var gapWidth = rng.NextRange(Tuning.GapWidthMin, Tuning.GapWidthMax);

        // Keep the whole gap away from the walls so every gap is reachable.
        var halfGap = gapWidth / 2f;
        var minCenter = Tuning.GapWallMargin + halfGap;
        var maxCenter = Tuning.ShaftWidth - Tuning.GapWallMargin - halfGap;
        var gapCenterX = rng.NextRange(minCenter, maxCenter);

        return (gapCenterX, gapWidth);
    }
}
