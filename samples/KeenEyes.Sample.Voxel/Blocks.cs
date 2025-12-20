namespace KeenEyes.Sample.Voxel;

// =============================================================================
// BLOCK DEFINITIONS
// =============================================================================

/// <summary>
/// Block type identifiers.
/// </summary>
public static class BlockId
{
    /// <summary>Empty block (no collision).</summary>
    public const byte Air = 0;

    /// <summary>Basic stone block.</summary>
    public const byte Stone = 1;

    /// <summary>Dirt block (subsurface layer).</summary>
    public const byte Dirt = 2;

    /// <summary>Grass-covered surface block.</summary>
    public const byte Grass = 3;

    /// <summary>Sand block (desert/beach).</summary>
    public const byte Sand = 4;

    /// <summary>Water block (liquid).</summary>
    public const byte Water = 5;

    /// <summary>Tree trunk block.</summary>
    public const byte Wood = 6;

    /// <summary>Tree foliage block.</summary>
    public const byte Leaves = 7;

    /// <summary>Snow-covered surface block.</summary>
    public const byte Snow = 8;

    /// <summary>Frozen water block.</summary>
    public const byte Ice = 9;

    /// <summary>Gravel block.</summary>
    public const byte Gravel = 10;

    /// <summary>Clay block (underwater).</summary>
    public const byte Clay = 11;

    /// <summary>Indestructible bedrock layer.</summary>
    public const byte Bedrock = 12;

    /// <summary>Cobblestone block.</summary>
    public const byte Cobblestone = 13;

    /// <summary>Desert cactus plant.</summary>
    public const byte Cactus = 14;

    /// <summary>Dead bush plant.</summary>
    public const byte DeadBush = 15;
}

/// <summary>
/// Properties of a block type.
/// </summary>
public readonly record struct BlockType(
    byte Id,
    string Name,
    char Symbol,
    ConsoleColor Color,
    bool IsSolid,
    bool IsTransparent,
    bool IsLiquid
);

/// <summary>
/// Registry of all block types and their properties.
/// </summary>
public sealed class BlockRegistry
{
    private readonly BlockType[] _blocks = new BlockType[256];

    /// <summary>
    /// Initializes the block registry with default block types.
    /// </summary>
    public BlockRegistry()
    {
        Register(new BlockType(BlockId.Air, "Air", ' ', ConsoleColor.Black, false, true, false));
        Register(new BlockType(BlockId.Stone, "Stone", '#', ConsoleColor.DarkGray, true, false, false));
        Register(new BlockType(BlockId.Dirt, "Dirt", '=', ConsoleColor.DarkYellow, true, false, false));
        Register(new BlockType(BlockId.Grass, "Grass", '"', ConsoleColor.Green, true, false, false));
        Register(new BlockType(BlockId.Sand, "Sand", '.', ConsoleColor.Yellow, true, false, false));
        Register(new BlockType(BlockId.Water, "Water", '~', ConsoleColor.Blue, false, true, true));
        Register(new BlockType(BlockId.Wood, "Wood", '|', ConsoleColor.DarkRed, true, false, false));
        Register(new BlockType(BlockId.Leaves, "Leaves", '*', ConsoleColor.DarkGreen, true, true, false));
        Register(new BlockType(BlockId.Snow, "Snow", 'o', ConsoleColor.White, true, false, false));
        Register(new BlockType(BlockId.Ice, "Ice", '-', ConsoleColor.Cyan, true, true, false));
        Register(new BlockType(BlockId.Gravel, "Gravel", ':', ConsoleColor.Gray, true, false, false));
        Register(new BlockType(BlockId.Clay, "Clay", '%', ConsoleColor.DarkCyan, true, false, false));
        Register(new BlockType(BlockId.Bedrock, "Bedrock", '@', ConsoleColor.DarkGray, true, false, false));
        Register(new BlockType(BlockId.Cobblestone, "Cobblestone", '+', ConsoleColor.Gray, true, false, false));
        Register(new BlockType(BlockId.Cactus, "Cactus", '!', ConsoleColor.Green, true, false, false));
        Register(new BlockType(BlockId.DeadBush, "Dead Bush", 'v', ConsoleColor.DarkYellow, false, true, false));
    }

    private void Register(BlockType block)
    {
        _blocks[block.Id] = block;
    }

    /// <summary>
    /// Gets the block type for the given ID.
    /// </summary>
    public ref readonly BlockType Get(byte id) => ref _blocks[id];

    /// <summary>
    /// Checks if a block is solid (blocks movement).
    /// </summary>
    public bool IsSolid(byte id) => _blocks[id].IsSolid;

    /// <summary>
    /// Checks if a block is transparent (can see through).
    /// </summary>
    public bool IsTransparent(byte id) => _blocks[id].IsTransparent;

    /// <summary>
    /// Checks if a block is a liquid.
    /// </summary>
    public bool IsLiquid(byte id) => _blocks[id].IsLiquid;

    /// <summary>
    /// Gets the display symbol for a block.
    /// </summary>
    public char GetSymbol(byte id) => _blocks[id].Symbol;

    /// <summary>
    /// Gets the display color for a block.
    /// </summary>
    public ConsoleColor GetColor(byte id) => _blocks[id].Color;
}
