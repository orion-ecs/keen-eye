namespace KeenEyes.Sample.Voxel;

// =============================================================================
// SIMPLEX NOISE IMPLEMENTATION
// =============================================================================
// A simple noise generator for terrain generation.
// Based on Ken Perlin's improved noise algorithm.
// =============================================================================

/// <summary>
/// Simplex noise generator for procedural terrain.
/// </summary>
public sealed class SimplexNoise
{
    private readonly int[] perm = new int[512];
    private readonly int[] permMod12 = new int[512];

    private static readonly int[] grad3 =
    [
        1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1, 0,
        1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, -1,
        0, 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1
    ];

    /// <summary>
    /// Creates a new noise generator with the specified seed.
    /// </summary>
    public SimplexNoise(int seed)
    {
        // Seeded random is required for deterministic procedural generation
#pragma warning disable KEEN030
        var random = new Random(seed);
#pragma warning restore KEEN030
        var p = new int[256];

        for (int i = 0; i < 256; i++)
        {
            p[i] = i;
        }

        // Fisher-Yates shuffle
        for (int i = 255; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }

        for (int i = 0; i < 512; i++)
        {
            perm[i] = p[i & 255];
            permMod12[i] = perm[i] % 12;
        }
    }

    /// <summary>
    /// 2D simplex noise.
    /// </summary>
    public float Noise2D(float x, float y)
    {
        const float F2 = 0.5f * (1.732050808f - 1.0f); // (sqrt(3) - 1) / 2
        const float G2 = (3.0f - 1.732050808f) / 6.0f; // (3 - sqrt(3)) / 6

        float s = (x + y) * F2;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);

        float t = (i + j) * G2;
        float x0 = x - (i - t);
        float y0 = y - (j - t);

        int i1, j1;
        if (x0 > y0) { i1 = 1; j1 = 0; }
        else { i1 = 0; j1 = 1; }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1.0f + 2.0f * G2;
        float y2 = y0 - 1.0f + 2.0f * G2;

        int ii = i & 255;
        int jj = j & 255;

        float n0 = CalculateCorner(x0, y0, permMod12[ii + perm[jj]]);
        float n1 = CalculateCorner(x1, y1, permMod12[ii + i1 + perm[jj + j1]]);
        float n2 = CalculateCorner(x2, y2, permMod12[ii + 1 + perm[jj + 1]]);

        return 70.0f * (n0 + n1 + n2);
    }

    /// <summary>
    /// Fractal Brownian motion - layered noise for more natural terrain.
    /// </summary>
    public float FBM(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise2D(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    private static float CalculateCorner(float x, float y, int gi)
    {
        float t = 0.5f - x * x - y * y;
        if (t < 0)
        {
            return 0;
        }

        t *= t;
        return t * t * Dot2D(grad3, gi * 3, x, y);
    }

    private static float Dot2D(int[] g, int offset, float x, float y)
    {
        return g[offset] * x + g[offset + 1] * y;
    }

    private static int FastFloor(float x)
    {
        int xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }
}
