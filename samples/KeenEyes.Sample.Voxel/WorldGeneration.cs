namespace KeenEyes.Sample.Voxel;

// =============================================================================
// WORLD GENERATION
// =============================================================================
// Procedural terrain generation with biomes using layered noise.
// =============================================================================

/// <summary>
/// Biome properties for terrain generation.
/// </summary>
public readonly record struct BiomeProperties(
    BiomeType Type,
    int MinHeight,
    int MaxHeight,
    byte SurfaceBlock,
    byte SubsurfaceBlock,
    float TreeDensity,
    float PlantDensity
);

/// <summary>
/// World generator using simplex noise for terrain and biome distribution.
/// </summary>
/// <param name="seed">World seed for reproducible generation.</param>
public sealed class WorldGenerator(int seed)
{
    private readonly SimplexNoise _heightNoise = new(seed);
    private readonly SimplexNoise _temperatureNoise = new(seed + 2);
    private readonly SimplexNoise _moistureNoise = new(seed + 3);
    private readonly SimplexNoise _detailNoise = new(seed + 4);

    /// <summary>World seed for reproducible generation.</summary>
    public int Seed { get; } = seed;

    /// <summary>Sea level height.</summary>
    public int SeaLevel { get; } = 32;

    /// <summary>Bedrock layer height.</summary>
    public int BedrockHeight { get; } = 1;

    private static readonly BiomeProperties[] BiomeData =
    [
        new(BiomeType.Plains, 28, 40, BlockId.Grass, BlockId.Dirt, 0.02f, 0.15f),
        new(BiomeType.Forest, 30, 45, BlockId.Grass, BlockId.Dirt, 0.25f, 0.3f),
        new(BiomeType.Desert, 26, 38, BlockId.Sand, BlockId.Sand, 0.005f, 0.02f),
        new(BiomeType.Mountains, 40, 80, BlockId.Stone, BlockId.Stone, 0.01f, 0.05f),
        new(BiomeType.Tundra, 28, 42, BlockId.Snow, BlockId.Dirt, 0.01f, 0.02f),
        new(BiomeType.Ocean, 10, 30, BlockId.Sand, BlockId.Clay, 0f, 0f)
    ];

    /// <summary>
    /// Generates chunk voxel data at the given chunk coordinates.
    /// </summary>
    public VoxelData GenerateChunk(int chunkX, int chunkY, int chunkZ, out BiomeType biome, out HeightMap heightMap)
    {
        var voxels = new VoxelData { Blocks = new byte[VoxelData.Volume] };
        heightMap = new HeightMap { Heights = new byte[VoxelData.Size * VoxelData.Size] };

        // Determine biome at chunk center
        int centerX = chunkX * VoxelData.Size + VoxelData.Size / 2;
        int centerZ = chunkZ * VoxelData.Size + VoxelData.Size / 2;
        biome = DetermineBiome(centerX, centerZ);
        var biomeProps = BiomeData[(int)biome];

        // Generate terrain for each column
        for (int lz = 0; lz < VoxelData.Size; lz++)
        {
            for (int lx = 0; lx < VoxelData.Size; lx++)
            {
                int worldX = chunkX * VoxelData.Size + lx;
                int worldZ = chunkZ * VoxelData.Size + lz;

                // Get local biome (may differ from chunk center)
                var localBiome = DetermineBiome(worldX, worldZ);
                var localBiomeProps = BiomeData[(int)localBiome];

                // Calculate terrain height
                int terrainHeight = CalculateTerrainHeight(worldX, worldZ, localBiomeProps);
                heightMap.Heights[lx + lz * VoxelData.Size] = (byte)Math.Clamp(terrainHeight, 0, 255);

                // Fill blocks for this column within chunk bounds
                int worldBaseY = chunkY * VoxelData.Size;

                for (int ly = 0; ly < VoxelData.Size; ly++)
                {
                    int worldY = worldBaseY + ly;
                    byte blockId = GetBlockForPosition(worldX, worldY, worldZ, terrainHeight, localBiome, localBiomeProps);
                    voxels.SetBlock(lx, ly, lz, blockId);
                }
            }
        }

        // Add features (trees, plants) - only in surface chunks
        if (chunkY >= 0 && chunkY <= 5)
        {
            AddFeatures(ref voxels, chunkX, chunkY, chunkZ, biome, biomeProps, heightMap);
        }

        return voxels;
    }

    /// <summary>
    /// Determines the biome at the given world coordinates.
    /// </summary>
    public BiomeType DetermineBiome(int worldX, int worldZ)
    {
        float scale = 0.005f;
        float temperature = _temperatureNoise.Noise2D(worldX * scale, worldZ * scale);
        float moisture = _moistureNoise.Noise2D(worldX * scale * 1.5f, worldZ * scale * 1.5f);

        // Normalize to 0-1 range
        temperature = (temperature + 1) * 0.5f;
        moisture = (moisture + 1) * 0.5f;

        // Check for ocean first (low elevation areas)
        float elevation = _heightNoise.FBM(worldX * 0.01f, worldZ * 0.01f, 3, 0.5f, 2.0f);
        if (elevation < -0.3f)
        {
            return BiomeType.Ocean;
        }

        // Temperature-moisture based biome selection
        if (temperature < 0.25f)
        {
            return BiomeType.Tundra;
        }
        else if (temperature > 0.75f && moisture < 0.35f)
        {
            return BiomeType.Desert;
        }
        else if (elevation > 0.4f)
        {
            return BiomeType.Mountains;
        }
        else if (moisture > 0.55f)
        {
            return BiomeType.Forest;
        }
        else
        {
            return BiomeType.Plains;
        }
    }

    private int CalculateTerrainHeight(int worldX, int worldZ, BiomeProperties biome)
    {
        // Base terrain using FBM
        float baseHeight = _heightNoise.FBM(worldX * 0.02f, worldZ * 0.02f, 4, 0.5f, 2.0f);

        // Detail noise for local variation
        float detail = _detailNoise.Noise2D(worldX * 0.1f, worldZ * 0.1f) * 0.2f;

        // Combine and scale to biome height range
        float normalized = (baseHeight + detail + 1) * 0.5f;
        int height = (int)(biome.MinHeight + normalized * (biome.MaxHeight - biome.MinHeight));

        return Math.Clamp(height, 1, 127);
    }

    private byte GetBlockForPosition(int worldX, int worldY, int worldZ, int terrainHeight, BiomeType biome, BiomeProperties biomeProps)
    {
        // Bedrock layer
        if (worldY <= BedrockHeight)
        {
            return BlockId.Bedrock;
        }

        // Underground
        if (worldY < terrainHeight - 4)
        {
            return BlockId.Stone;
        }

        // Subsurface layer (3-4 blocks below surface)
        if (worldY < terrainHeight)
        {
            return biomeProps.SubsurfaceBlock;
        }

        // Surface layer
        if (worldY == terrainHeight)
        {
            // Water covers low areas
            if (terrainHeight < SeaLevel && biome != BiomeType.Desert)
            {
                return biomeProps.SubsurfaceBlock; // Sand/clay under water
            }

            return biomeProps.SurfaceBlock;
        }

        // Water filling
        if (worldY <= SeaLevel && worldY > terrainHeight)
        {
            if (biome == BiomeType.Tundra && worldY == SeaLevel)
            {
                return BlockId.Ice;
            }

            return BlockId.Water;
        }

        // Air above
        return BlockId.Air;
    }

    private void AddFeatures(ref VoxelData voxels, int chunkX, int chunkY, int chunkZ, BiomeType biome, BiomeProperties biomeProps, HeightMap heightMap)
    {
        // Use deterministic random based on chunk position via noise
        int worldBaseY = chunkY * VoxelData.Size;

        for (int lz = 1; lz < VoxelData.Size - 1; lz++)
        {
            for (int lx = 1; lx < VoxelData.Size - 1; lx++)
            {
                int terrainHeight = heightMap.GetHeight(lx, lz);
                int localY = terrainHeight - worldBaseY;

                // Skip if surface is not in this chunk
                if (localY < 0 || localY >= VoxelData.Size - 4)
                {
                    continue;
                }

                // Skip underwater areas
                if (terrainHeight < 32) // Sea level
                {
                    continue;
                }

                // Use noise for deterministic feature placement
                int worldX = chunkX * VoxelData.Size + lx;
                int worldZ = chunkZ * VoxelData.Size + lz;
                float roll = (_detailNoise.Noise2D(worldX * 0.5f, worldZ * 0.5f) + 1) * 0.5f;

                // Trees
                if (roll < biomeProps.TreeDensity && localY < VoxelData.Size - 6)
                {
                    PlaceTree(ref voxels, lx, localY + 1, lz, biome, worldX, worldZ);
                }
                // Plants
                else if (roll < biomeProps.TreeDensity + biomeProps.PlantDensity)
                {
                    PlacePlant(ref voxels, lx, localY + 1, lz, biome);
                }
            }
        }
    }

    private void PlaceTree(ref VoxelData voxels, int x, int y, int z, BiomeType biome, int worldX, int worldZ)
    {
        if (biome == BiomeType.Desert)
        {
            // Cactus in desert - use noise for height variation
            int height = 2 + (int)((_detailNoise.Noise2D(worldX * 0.7f, worldZ * 0.7f) + 1) * 1.5f);
            height = Math.Clamp(height, 2, 4);
            for (int i = 0; i < height && y + i < VoxelData.Size; i++)
            {
                voxels.SetBlock(x, y + i, z, BlockId.Cactus);
            }
        }
        else
        {
            // Regular tree - use noise for trunk height
            int trunkHeight = 4 + (int)((_detailNoise.Noise2D(worldX * 0.3f, worldZ * 0.3f) + 1) * 1.5f);
            trunkHeight = Math.Clamp(trunkHeight, 4, 7);

            // Trunk
            for (int i = 0; i < trunkHeight && y + i < VoxelData.Size; i++)
            {
                voxels.SetBlock(x, y + i, z, BlockId.Wood);
            }

            // Leaves (simple sphere-ish shape)
            int leafStart = trunkHeight - 2;
            for (int ly = leafStart; ly < trunkHeight + 2; ly++)
            {
                int radius = ly < trunkHeight ? 2 : 1;
                for (int lx = -radius; lx <= radius; lx++)
                {
                    for (int lz = -radius; lz <= radius; lz++)
                    {
                        if (lx == 0 && lz == 0 && ly < trunkHeight)
                        {
                            continue; // Skip trunk
                        }

                        int px = x + lx;
                        int py = y + ly;
                        int pz = z + lz;

                        if (px >= 0 && px < VoxelData.Size &&
                            py >= 0 && py < VoxelData.Size &&
                            pz >= 0 && pz < VoxelData.Size &&
                            voxels.GetBlock(px, py, pz) == BlockId.Air)
                        {
                            voxels.SetBlock(px, py, pz, BlockId.Leaves);
                        }
                    }
                }
            }
        }
    }

    private static void PlacePlant(ref VoxelData voxels, int x, int y, int z, BiomeType biome)
    {
        if (y >= VoxelData.Size)
        {
            return;
        }

        byte plantBlock = biome switch
        {
            BiomeType.Desert => BlockId.DeadBush,
            _ => BlockId.Air // Just grass block is enough for plains/forest
        };

        if (plantBlock != BlockId.Air)
        {
            voxels.SetBlock(x, y, z, plantBlock);
        }
    }
}
