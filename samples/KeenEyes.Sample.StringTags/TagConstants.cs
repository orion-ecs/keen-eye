namespace KeenEyes.Sample.StringTags;

/// <summary>
/// Common tag constants for consistency and IDE support.
/// </summary>
public static class Tags
{
    /// <summary>Marks entity as an enemy.</summary>
    public const string Enemy = "Enemy";
    /// <summary>Marks entity as friendly.</summary>
    public const string Friendly = "Friendly";
    /// <summary>Marks entity as a boss.</summary>
    public const string Boss = "Boss";
    /// <summary>Marks entity as hostile.</summary>
    public const string Hostile = "Hostile";
    /// <summary>Marks entity as able to trade.</summary>
    public const string CanTrade = "CanTrade";

    /// <summary>Elemental type tags.</summary>
    public static class Elements
    {
        /// <summary>Fire element.</summary>
        public const string Fire = "Fire";
        /// <summary>Ice element.</summary>
        public const string Ice = "Ice";
        /// <summary>Lightning element.</summary>
        public const string Lightning = "Lightning";
    }

    /// <summary>Resistance tags.</summary>
    public static class Resistances
    {
        /// <summary>Immune to fire damage.</summary>
        public const string ImmuneToFire = "ImmuneToFire";
        /// <summary>Immune to ice damage.</summary>
        public const string ImmuneToIce = "ImmuneToIce";
        /// <summary>Resistant to physical damage.</summary>
        public const string ResistPhysical = "ResistPhysical";
    }
}
