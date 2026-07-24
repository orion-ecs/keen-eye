using System.Numerics;
using KeenEyes.Persistence;

namespace KeenEyes.Sample.NovaFall;

// ============================================================================
// Persistence. The player profile (per-mode bests, Daily Inferno history, the
// selected cosmetic style) is stored as a tiny dedicated "profile world":
// entities carrying [Component(Serializable = true)] records, saved through
// KeenEyes.Persistence's slot API. The game world itself is never snapshotted —
// a run is transient; only what outlives runs goes to disk.
// ============================================================================

/// <summary>
/// Save-file schema header. Version 1: bump <see cref="SchemaVersion"/> (and the
/// component <c>Version</c> attributes) together with a migration when the
/// schema changes; an unknown newer version loads as fresh state rather than
/// guessing.
/// </summary>
[Component(Serializable = true, Version = 1)]
public partial struct ProfileHeaderRecord
{
    /// <summary>The profile schema version this file was written with.</summary>
    public int SchemaVersion;

    /// <summary>Index into <see cref="CosmeticStyles.All"/> of the selected style.</summary>
    public int SelectedStyle;
}

/// <summary>
/// Lifetime bests for one mode. One entity per mode in the profile world.
/// </summary>
[Component(Serializable = true, Version = 1)]
public partial struct ModeBestRecord
{
    /// <summary>The mode, stored as its <see cref="GameMode"/> integer value.</summary>
    public int Mode;

    /// <summary>Best final score.</summary>
    public double BestScore;

    /// <summary>Best final depth in meters.</summary>
    public float BestDepth;

    /// <summary>Best combo reached in any run.</summary>
    public int BestCombo;
}

/// <summary>
/// One day's Daily Inferno record: attempts used and the best medal earned.
/// One entity per played date in the profile world.
/// </summary>
[Component(Serializable = true, Version = 1)]
public partial struct DailyRecord
{
    /// <summary>The date key (yyyyMMdd) this record belongs to.</summary>
    public int DateKey;

    /// <summary>Attempts consumed on that date (max <see cref="Tuning.DailyAttemptsPerDay"/>).</summary>
    public int AttemptsUsed;

    /// <summary>Best medal earned that date (0 none, 1 bronze, 2 silver, 3 gold).</summary>
    public int Medal;
}

/// <summary>
/// The in-memory player profile: what the save file round-trips. Held by the
/// <see cref="ProfileState"/> singleton; <see cref="ProfileSystem"/> writes it
/// back to disk when it changes.
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>Current profile schema version.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Index into <see cref="CosmeticStyles.All"/> of the selected style.</summary>
    public int SelectedStyle { get; set; }

    /// <summary>Lifetime bests, indexed by <see cref="GameMode"/>.</summary>
    public ModeBestRecord[] ModeBests { get; } = CreateModeBests();

    /// <summary>Daily Inferno history, one entry per played date.</summary>
    public List<DailyRecord> DailyHistory { get; } = [];

    /// <summary>Best depth across all modes, for cosmetic unlock milestones.</summary>
    public float BestDepthOverall
    {
        get
        {
            var best = 0f;
            foreach (var record in ModeBests)
            {
                best = Math.Max(best, record.BestDepth);
            }

            return best;
        }
    }

    /// <summary>Best combo across all modes, for cosmetic unlock milestones.</summary>
    public int BestComboOverall
    {
        get
        {
            var best = 0;
            foreach (var record in ModeBests)
            {
                best = Math.Max(best, record.BestCombo);
            }

            return best;
        }
    }

    /// <summary>
    /// Gets (creating if absent) the Daily Inferno record for a date key.
    /// </summary>
    /// <param name="dateKey">The yyyyMMdd date key.</param>
    /// <returns>The index of the record in <see cref="DailyHistory"/>.</returns>
    public int DailyRecordIndexFor(int dateKey)
    {
        for (var i = 0; i < DailyHistory.Count; i++)
        {
            if (DailyHistory[i].DateKey == dateKey)
            {
                return i;
            }
        }

        DailyHistory.Add(new DailyRecord { DateKey = dateKey });
        return DailyHistory.Count - 1;
    }

    private static ModeBestRecord[] CreateModeBests()
    {
        var bests = new ModeBestRecord[ModeCatalog.All.Length];
        for (var i = 0; i < bests.Length; i++)
        {
            bests[i] = new ModeBestRecord { Mode = (int)ModeCatalog.All[i] };
        }

        return bests;
    }
}

/// <summary>
/// Profile availability singleton. In headless <c>--simulate</c> mode
/// <see cref="SaveEnabled"/> is false and <see cref="Profile"/> is a fresh
/// in-memory instance, so CI never touches the disk — the hermetic guard.
/// </summary>
public struct ProfileState
{
    /// <summary>The loaded (or fresh) profile, or null before Program wires it up.</summary>
    public PlayerProfile? Profile;

    /// <summary>When false, the profile is never written to disk.</summary>
    public bool SaveEnabled;

    /// <summary>Directory the save slot lives in (windowed mode only).</summary>
    public string? SaveDirectory;

    /// <summary>Set by any system that changes the profile; cleared by <see cref="ProfileSystem"/> after saving.</summary>
    public bool Dirty;

    /// <summary>Today's yyyyMMdd key, captured once at startup (wall clock never enters the simulation).</summary>
    public int TodayKey;
}

/// <summary>
/// One cosmetic style: a trail gradient variant plus a ball palette, unlocked
/// at a depth or combo milestone. Purely cosmetic — styles recolor the trail
/// and ball but never touch the simulation.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="UnlockHint">Ready-screen hint shown while locked.</param>
/// <param name="RequiredDepth">Lifetime best depth (meters) needed to unlock, or 0.</param>
/// <param name="RequiredCombo">Lifetime best combo needed to unlock, or 0.</param>
/// <param name="TrailOverride">Replacement trail color, or null to keep the tier palette's.</param>
/// <param name="BallOverride">Replacement ball color, or null to keep the tier palette's.</param>
public readonly record struct CosmeticStyle(
    string Name,
    string UnlockHint,
    float RequiredDepth,
    int RequiredCombo,
    Vector4? TrailOverride,
    Vector4? BallOverride);

/// <summary>
/// The cosmetic style catalog and its unlock rules. Unlocks are DERIVED from
/// the profile's lifetime bests rather than stored as flags — one source of
/// truth, nothing to migrate when the milestone list changes.
/// </summary>
public static class CosmeticStyles
{
    /// <summary>Every style, in Ready-screen cycle order. Index 0 is the default.</summary>
    public static readonly CosmeticStyle[] All =
    [
        new("TIER COLORS", "always yours", 0f, 0, null, null),
        new("ION WAKE", "reach 200m", 200f, 0,
            new Vector4(0.30f, 0.95f, 0.85f, 1f), new Vector4(0.75f, 1.00f, 0.95f, 1f)),
        new("EMBERLINE", "hit a 12 combo", 0f, 12,
            new Vector4(1.00f, 0.35f, 0.20f, 1f), new Vector4(1.00f, 0.80f, 0.45f, 1f)),
        new("PRISMATIC", "reach 500m", 500f, 0,
            new Vector4(0.95f, 0.55f, 1.00f, 1f), new Vector4(1.00f, 1.00f, 1.00f, 1f)),
    ];

    /// <summary>
    /// Checks whether a style is unlocked for a profile.
    /// </summary>
    /// <param name="styleIndex">Index into <see cref="All"/>.</param>
    /// <param name="profile">The player profile.</param>
    /// <returns>True if the style's milestone has been met.</returns>
    public static bool IsUnlocked(int styleIndex, PlayerProfile profile)
    {
        var style = All[styleIndex];
        return profile.BestDepthOverall >= style.RequiredDepth
            && profile.BestComboOverall >= style.RequiredCombo;
    }
}

/// <summary>
/// Loads and saves the <see cref="PlayerProfile"/> through KeenEyes.Persistence.
/// </summary>
/// <remarks>
/// The profile is serialized as a snapshot of a small dedicated world whose
/// entities carry the serializable record components above. The generated
/// <c>KeenEyes.Generated.ComponentSerializer</c> (from the
/// <c>[Component(Serializable = true)]</c> attributes) makes the round trip
/// reflection-free and Native AOT compatible.
/// </remarks>
public static class ProfilePersistence
{
    private const string SlotName = "profile";

    /// <summary>
    /// Loads the profile from disk, returning fresh state on first run or when
    /// the file is unreadable. Never throws: a save file is untrusted input, and
    /// the only acceptable failure mode is starting over — not crashing.
    /// </summary>
    /// <param name="saveDirectory">Directory the save slot lives in.</param>
    /// <returns>The loaded or fresh profile.</returns>
    public static PlayerProfile Load(string saveDirectory)
    {
        var profile = new PlayerProfile();

        try
        {
            using var profileWorld = new World();
            profileWorld.InstallPlugin(new PersistencePlugin(new PersistenceConfig
            {
                SaveDirectory = saveDirectory,
            }));

            var api = profileWorld.GetExtension<EncryptedPersistenceApi>();
            if (!api.SlotExists(SlotName))
            {
                return profile;
            }

            api.LoadFromSlot(SlotName, KeenEyes.Generated.ComponentSerializer.Instance);

            var schemaVersion = PlayerProfile.CurrentSchemaVersion;
            foreach (var entity in profileWorld.Query<ProfileHeaderRecord>())
            {
                ref readonly var header = ref profileWorld.Get<ProfileHeaderRecord>(entity);
                schemaVersion = header.SchemaVersion;
                profile.SelectedStyle = Math.Clamp(header.SelectedStyle, 0, CosmeticStyles.All.Length - 1);
            }

            if (schemaVersion > PlayerProfile.CurrentSchemaVersion)
            {
                // Written by a future build: refuse to guess at its shape.
                return new PlayerProfile();
            }

            foreach (var entity in profileWorld.Query<ModeBestRecord>())
            {
                ref readonly var record = ref profileWorld.Get<ModeBestRecord>(entity);
                if (record.Mode >= 0 && record.Mode < profile.ModeBests.Length)
                {
                    profile.ModeBests[record.Mode] = record;
                }
            }

            foreach (var entity in profileWorld.Query<DailyRecord>())
            {
                ref readonly var record = ref profileWorld.Get<DailyRecord>(entity);
                var index = profile.DailyRecordIndexFor(record.DateKey);
                profile.DailyHistory[index] = record;
            }
        }
        catch (Exception ex)
        {
            // Deliberately broad: ANY defect in the file (truncation, bad
            // checksum, malformed JSON, a hand-edited field) must mean fresh
            // state. Losing a corrupt profile beats crashing at launch.
            Console.WriteLine($"Profile unreadable, starting fresh: {ex.Message}");
            return new PlayerProfile();
        }

        return profile;
    }

    /// <summary>
    /// Saves the profile to disk. Failures are reported and swallowed — losing
    /// one save write must never take the game down.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="saveDirectory">Directory the save slot lives in.</param>
    public static void Save(PlayerProfile profile, string saveDirectory)
    {
        try
        {
            using var profileWorld = new World();
            profileWorld.InstallPlugin(new PersistencePlugin(new PersistenceConfig
            {
                SaveDirectory = saveDirectory,
            }));

            profileWorld.Spawn()
                .With(new ProfileHeaderRecord
                {
                    SchemaVersion = PlayerProfile.CurrentSchemaVersion,
                    SelectedStyle = profile.SelectedStyle,
                })
                .Build();

            foreach (var record in profile.ModeBests)
            {
                profileWorld.Spawn().With(record).Build();
            }

            foreach (var record in profile.DailyHistory)
            {
                profileWorld.Spawn().With(record).Build();
            }

            profileWorld.GetExtension<EncryptedPersistenceApi>()
                .SaveToSlot(SlotName, KeenEyes.Generated.ComponentSerializer.Instance);
        }
        catch (Exception ex)
        {
            // Same reasoning as Load: persistence is best-effort, play goes on.
            Console.WriteLine($"Profile save failed: {ex.Message}");
        }
    }
}
