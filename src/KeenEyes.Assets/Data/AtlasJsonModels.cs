using System.Text.Json.Serialization;

namespace KeenEyes.Assets;

#region TexturePacker Format

/// <summary>
/// Root model for TexturePacker JSON atlas format.
/// </summary>
internal sealed class TexturePackerAtlas
{
    [JsonPropertyName("frames")]
    public Dictionary<string, TexturePackerFrame>? Frames { get; set; }

    [JsonPropertyName("meta")]
    public TexturePackerMeta? Meta { get; set; }
}

/// <summary>
/// Frame data in TexturePacker format.
/// </summary>
internal sealed class TexturePackerFrame
{
    [JsonPropertyName("frame")]
    public JsonRect? Frame { get; set; }

    [JsonPropertyName("rotated")]
    public bool Rotated { get; set; }

    [JsonPropertyName("trimmed")]
    public bool Trimmed { get; set; }

    [JsonPropertyName("spriteSourceSize")]
    public JsonRect? SpriteSourceSize { get; set; }

    [JsonPropertyName("sourceSize")]
    public JsonSize? SourceSize { get; set; }

    [JsonPropertyName("pivot")]
    public JsonPoint? Pivot { get; set; }
}

/// <summary>
/// Metadata in TexturePacker format.
/// </summary>
internal sealed class TexturePackerMeta
{
    [JsonPropertyName("app")]
    public string? App { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("size")]
    public JsonSize? Size { get; set; }

    [JsonPropertyName("scale")]
    public string? Scale { get; set; }
}

#endregion

#region Aseprite Format

/// <summary>
/// Root model for Aseprite JSON atlas format.
/// </summary>
internal sealed class AsepriteAtlas
{
    [JsonPropertyName("frames")]
    public Dictionary<string, AsepriteFrame>? Frames { get; set; }

    [JsonPropertyName("meta")]
    public AsepriteMeta? Meta { get; set; }
}

/// <summary>
/// Frame data in Aseprite format.
/// </summary>
internal sealed class AsepriteFrame
{
    [JsonPropertyName("frame")]
    public JsonRect? Frame { get; set; }

    [JsonPropertyName("rotated")]
    public bool Rotated { get; set; }

    [JsonPropertyName("trimmed")]
    public bool Trimmed { get; set; }

    [JsonPropertyName("spriteSourceSize")]
    public JsonRect? SpriteSourceSize { get; set; }

    [JsonPropertyName("sourceSize")]
    public JsonSize? SourceSize { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; } = 100;
}

/// <summary>
/// Metadata in Aseprite format.
/// </summary>
internal sealed class AsepriteMeta
{
    [JsonPropertyName("app")]
    public string? App { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("size")]
    public JsonSize? Size { get; set; }

    [JsonPropertyName("scale")]
    public string? Scale { get; set; }

    [JsonPropertyName("frameTags")]
    public List<AsepriteFrameTag>? FrameTags { get; set; }

    [JsonPropertyName("layers")]
    public List<AsepriteLayer>? Layers { get; set; }

    [JsonPropertyName("slices")]
    public List<AsepriteSlice>? Slices { get; set; }
}

/// <summary>
/// Frame tag (animation) in Aseprite format.
/// </summary>
internal sealed class AsepriteFrameTag
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("from")]
    public int From { get; set; }

    [JsonPropertyName("to")]
    public int To { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

/// <summary>
/// Layer data in Aseprite format.
/// </summary>
internal sealed class AsepriteLayer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("opacity")]
    public int Opacity { get; set; } = 255;

    [JsonPropertyName("blendMode")]
    public string? BlendMode { get; set; }
}

/// <summary>
/// Slice data in Aseprite format.
/// </summary>
internal sealed class AsepriteSlice
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("keys")]
    public List<AsepriteSliceKey>? Keys { get; set; }
}

/// <summary>
/// Slice key frame in Aseprite format.
/// </summary>
internal sealed class AsepriteSliceKey
{
    [JsonPropertyName("frame")]
    public int Frame { get; set; }

    [JsonPropertyName("bounds")]
    public JsonRect? Bounds { get; set; }

    [JsonPropertyName("center")]
    public JsonRect? Center { get; set; }

    [JsonPropertyName("pivot")]
    public JsonPoint? Pivot { get; set; }
}

#endregion

#region Shared JSON Primitives

/// <summary>
/// JSON rectangle primitive with integer coordinates.
/// </summary>
internal sealed class JsonRect
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("w")]
    public int W { get; set; }

    [JsonPropertyName("h")]
    public int H { get; set; }
}

/// <summary>
/// JSON size primitive.
/// </summary>
internal sealed class JsonSize
{
    [JsonPropertyName("w")]
    public int W { get; set; }

    [JsonPropertyName("h")]
    public int H { get; set; }
}

/// <summary>
/// JSON point primitive with float coordinates.
/// </summary>
internal sealed class JsonPoint
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}

#endregion
