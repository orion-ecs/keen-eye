using KeenEyes.Graphics.Abstractions;
using Pfim;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for DDS (DirectDraw Surface) texture assets with GPU-compressed format support.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DdsTextureLoader"/> loads DDS files containing GPU-compressed textures
/// (BC1-BC7/DXT formats) using the Pfim library. These textures remain compressed
/// in VRAM, providing significant memory and bandwidth savings.
/// </para>
/// <para>
/// Supported formats:
/// <list type="bullet">
/// <item><description>BC1/DXT1: RGB with optional 1-bit alpha (4 bpp)</description></item>
/// <item><description>BC2/DXT3: RGBA with explicit 4-bit alpha (8 bpp)</description></item>
/// <item><description>BC3/DXT5: RGBA with interpolated alpha (8 bpp)</description></item>
/// <item><description>BC4: Single red channel (4 bpp)</description></item>
/// <item><description>BC5: Two channels (8 bpp)</description></item>
/// <item><description>BC7: High-quality RGBA (8 bpp)</description></item>
/// </list>
/// </para>
/// <para>
/// Non-compressed DDS files are decompressed and uploaded as standard RGBA textures.
/// </para>
/// </remarks>
public sealed class DdsTextureLoader : IAssetLoader<TextureAsset>
{
    private readonly IGraphicsContext graphics;

    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".dds"];

    /// <summary>
    /// Creates a new DDS texture loader.
    /// </summary>
    /// <param name="graphics">The graphics context for GPU texture creation.</param>
    /// <exception cref="ArgumentNullException">Graphics context is null.</exception>
    public DdsTextureLoader(IGraphicsContext graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        this.graphics = graphics;
    }

    /// <inheritdoc />
    public TextureAsset Load(Stream stream, AssetLoadContext context)
    {
        using var dds = Dds.Create(stream, new PfimConfig());

        // Check if this is a compressed format we can upload directly
        var compressedFormat = GetCompressedFormat(dds);
        if (compressedFormat.HasValue)
        {
            return LoadCompressed(dds, compressedFormat.Value);
        }

        // For non-compressed formats, convert to RGBA and upload
        return LoadUncompressed(dds, context);
    }

    private TextureAsset LoadCompressed(Dds dds, CompressedTextureFormat format)
    {
        // Collect mipmap data from the DDS file
        // Pfim stores all data in a single buffer with mipmaps stored sequentially
        List<ReadOnlyMemory<byte>> mipmaps =
        [
            // Base level (level 0) - the whole data buffer for level 0
            new ReadOnlyMemory<byte>(dds.Data, 0, dds.DataLen)
        ];

        // Additional mipmap levels if present
        if (dds.MipMaps != null)
        {
            foreach (var mipmap in dds.MipMaps)
            {
                // MipMapOffset contains DataOffset and DataLen but data is in main Data array
                mipmaps.Add(new ReadOnlyMemory<byte>(dds.Data, mipmap.DataOffset, mipmap.DataLen));
            }
        }

        var handle = graphics.CreateCompressedTexture(
            dds.Width,
            dds.Height,
            format,
            mipmaps.ToArray());

        var textureFormat = ToTextureFormat(format);

        return new TextureAsset(
            handle,
            dds.Width,
            dds.Height,
            textureFormat,
            graphics);
    }

    private TextureAsset LoadUncompressed(Dds dds, AssetLoadContext context)
    {
        // Convert to RGBA if needed
        byte[] pixels;
        if (dds.Format == ImageFormat.Rgba32)
        {
            pixels = dds.Data;
        }
        else if (dds.Format == ImageFormat.Rgb24)
        {
            // Convert RGB24 to RGBA32
            pixels = new byte[dds.Width * dds.Height * 4];
            for (int i = 0, j = 0; i < dds.Data.Length && j < pixels.Length; i += 3, j += 4)
            {
                pixels[j] = dds.Data[i];
                pixels[j + 1] = dds.Data[i + 1];
                pixels[j + 2] = dds.Data[i + 2];
                pixels[j + 3] = 255;
            }
        }
        else if (dds.Format == ImageFormat.Rgb8)
        {
            // Grayscale to RGBA
            pixels = new byte[dds.Width * dds.Height * 4];
            for (int i = 0, j = 0; i < dds.Data.Length && j < pixels.Length; i++, j += 4)
            {
                pixels[j] = dds.Data[i];
                pixels[j + 1] = dds.Data[i];
                pixels[j + 2] = dds.Data[i];
                pixels[j + 3] = 255;
            }
        }
        else
        {
            throw new AssetLoadException(
                context.Path,
                typeof(TextureAsset),
                $"Unsupported DDS format: {dds.Format}");
        }

        var handle = graphics.CreateTexture(dds.Width, dds.Height, pixels);

        return new TextureAsset(
            handle,
            dds.Width,
            dds.Height,
            TextureFormat.Rgba8,
            graphics);
    }

    private static CompressedTextureFormat? GetCompressedFormat(Dds dds)
    {
        return dds.Header.PixelFormat.FourCC switch
        {
            CompressionAlgorithm.D3DFMT_DXT1 => CompressedTextureFormat.Bc1,
            CompressionAlgorithm.D3DFMT_DXT3 => CompressedTextureFormat.Bc2,
            CompressionAlgorithm.D3DFMT_DXT5 => CompressedTextureFormat.Bc3,
            CompressionAlgorithm.BC4U or CompressionAlgorithm.BC4S or CompressionAlgorithm.ATI1 => CompressedTextureFormat.Bc4,
            CompressionAlgorithm.BC5U or CompressionAlgorithm.BC5S or CompressionAlgorithm.ATI2 => CompressedTextureFormat.Bc5,
            // DX10 header may have BC6H/BC7 formats - check extended header
            CompressionAlgorithm.DX10 => GetDx10Format(dds),
            _ => null
        };
    }

    private static CompressedTextureFormat? GetDx10Format(Dds dds)
    {
        // For DX10 format, the extended header contains the DXGI format
        // Pfim exposes this via the Header10 property if DX10 format is used
        var header10 = dds.Header10;
        if (header10 == null)
        {
            return null;
        }

        return header10.DxgiFormat switch
        {
            DxgiFormat.BC1_UNORM or DxgiFormat.BC1_UNORM_SRGB => CompressedTextureFormat.Bc1,
            DxgiFormat.BC2_UNORM or DxgiFormat.BC2_UNORM_SRGB => CompressedTextureFormat.Bc2,
            DxgiFormat.BC3_UNORM or DxgiFormat.BC3_UNORM_SRGB => CompressedTextureFormat.Bc3,
            DxgiFormat.BC4_UNORM or DxgiFormat.BC4_SNORM => CompressedTextureFormat.Bc4,
            DxgiFormat.BC5_UNORM or DxgiFormat.BC5_SNORM => CompressedTextureFormat.Bc5,
            DxgiFormat.BC6H_UF16 or DxgiFormat.BC6H_SF16 => CompressedTextureFormat.Bc6h,
            DxgiFormat.BC7_UNORM or DxgiFormat.BC7_UNORM_SRGB => CompressedTextureFormat.Bc7,
            _ => null
        };
    }

    private static TextureFormat ToTextureFormat(CompressedTextureFormat format) => format switch
    {
        CompressedTextureFormat.Bc1 => TextureFormat.Bc1,
        CompressedTextureFormat.Bc1Alpha => TextureFormat.Bc1Alpha,
        CompressedTextureFormat.Bc2 => TextureFormat.Bc2,
        CompressedTextureFormat.Bc3 => TextureFormat.Bc3,
        CompressedTextureFormat.Bc4 => TextureFormat.Bc4,
        CompressedTextureFormat.Bc5 => TextureFormat.Bc5,
        CompressedTextureFormat.Bc6h => TextureFormat.Bc6h,
        CompressedTextureFormat.Bc7 => TextureFormat.Bc7,
        _ => TextureFormat.Unknown
    };

    /// <inheritdoc />
    public async Task<TextureAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // Pfim doesn't have async methods, so run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(TextureAsset asset)
        => asset.SizeBytes;
}
