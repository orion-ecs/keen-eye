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
    /// Salt XORed into the run seed for the personality stream, so kinds draw
    /// from a random stream independent of the gap stream: adding a draw to one
    /// can never shift the other.
    /// </summary>
    private const ulong KindStreamSalt = 0x9E3B_51F0_C0DE_5EEDUL;

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

    /// <summary>
    /// Computes the personality of a given floor of a run. Personalities phase
    /// in by depth — standard-only early, then Brittle, Bumper, and Pulse join —
    /// and their combined chance is capped (~25%) so they stay minority spice.
    /// </summary>
    /// <param name="seed">The run seed from <see cref="RunConfig"/>.</param>
    /// <param name="floorIndex">The sequential index of the floor (0 = first floor).</param>
    /// <returns>The floor's kind.</returns>
    public static FloorKind KindForFloor(ulong seed, int floorIndex)
    {
        if (floorIndex < Tuning.BrittleMinFloorIndex)
        {
            return FloorKind.Standard;
        }

        var rng = SeededGenerator.ForFloor(seed ^ KindStreamSalt, floorIndex);
        var roll = rng.NextFloat();

        // Each unlocked kind claims one PersonalityChancePerKind-wide band of
        // the roll; everything past the bands is Standard. Because the bands
        // only ever EXTEND as depth unlocks more kinds, a floor that was
        // Brittle at index 12 stays Brittle at index 60 for the same roll.
        if (roll < Tuning.PersonalityChancePerKind)
        {
            return FloorKind.Brittle;
        }

        if (floorIndex >= Tuning.BumperMinFloorIndex && roll < 2f * Tuning.PersonalityChancePerKind)
        {
            return FloorKind.Bumper;
        }

        if (floorIndex >= Tuning.PulseMinFloorIndex && roll < 3f * Tuning.PersonalityChancePerKind)
        {
            return FloorKind.Pulse;
        }

        return FloorKind.Standard;
    }

    /// <summary>
    /// Computes how open a Pulse gap is at a music-clock time, in [0, 1]:
    /// 1 fully open, 0 fully closed, and in between while telegraphing the
    /// close (shrinking edges) or snapping back open.
    /// </summary>
    /// <remarks>
    /// The cycle is a pure function of the music clock — half a music loop per
    /// cycle — so every Pulse floor in the shaft breathes on the same beat and
    /// the headless simulation replays it exactly. The close is telegraphed for
    /// <see cref="Tuning.PulseCloseTelegraphSeconds"/> (&gt;= 0.6 s, the telegraph
    /// contract) before the gap is fully shut.
    /// </remarks>
    /// <param name="musicSeconds">The music clock from <see cref="MusicClock"/>.</param>
    /// <returns>The gap openness in [0, 1].</returns>
    public static float PulseOpenness(float musicSeconds)
    {
        var t = musicSeconds % Tuning.PulsePeriodSeconds;
        if (t < 0f)
        {
            t += Tuning.PulsePeriodSeconds;
        }

        // Cycle layout: reopen snap, full open, shrinking-edges telegraph, closed.
        var openEnd = Tuning.PulsePeriodSeconds - Tuning.PulseClosedSeconds - Tuning.PulseCloseTelegraphSeconds;

        if (t < Tuning.PulseReopenSeconds)
        {
            return t / Tuning.PulseReopenSeconds;
        }

        if (t < openEnd)
        {
            return 1f;
        }

        if (t < openEnd + Tuning.PulseCloseTelegraphSeconds)
        {
            return 1f - (t - openEnd) / Tuning.PulseCloseTelegraphSeconds;
        }

        return 0f;
    }

    /// <summary>
    /// Gets a floor's effective gap width at a music-clock time: the full width
    /// for every kind except Pulse, whose gap scales with <see cref="PulseOpenness"/>.
    /// Collision, graze detection, and rendering all share this one function, so
    /// what the player sees is exactly what the physics does.
    /// </summary>
    /// <param name="floor">The floor.</param>
    /// <param name="musicSeconds">The music clock from <see cref="MusicClock"/>.</param>
    /// <returns>The effective gap width in design units.</returns>
    public static float EffectiveGapWidth(in Floor floor, float musicSeconds)
        => floor.Kind == FloorKind.Pulse
            ? floor.GapWidth * PulseOpenness(musicSeconds)
            : floor.GapWidth;
}
