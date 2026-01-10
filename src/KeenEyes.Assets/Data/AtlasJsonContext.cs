using System.Text.Json.Serialization;

namespace KeenEyes.Assets;

/// <summary>
/// Source-generated JSON serialization context for sprite atlas and animation formats.
/// </summary>
/// <remarks>
/// This context enables Native AOT-compatible JSON serialization for
/// TexturePacker, Aseprite atlas formats, and .keanim animation files.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// TexturePacker format
[JsonSerializable(typeof(TexturePackerAtlas))]
[JsonSerializable(typeof(TexturePackerFrame))]
[JsonSerializable(typeof(TexturePackerMeta))]
// Aseprite format
[JsonSerializable(typeof(AsepriteAtlas))]
[JsonSerializable(typeof(AsepriteFrame))]
[JsonSerializable(typeof(AsepriteMeta))]
[JsonSerializable(typeof(AsepriteFrameTag))]
[JsonSerializable(typeof(AsepriteLayer))]
[JsonSerializable(typeof(AsepriteSlice))]
[JsonSerializable(typeof(AsepriteSliceKey))]
// Shared primitives
[JsonSerializable(typeof(JsonRect))]
[JsonSerializable(typeof(JsonSize))]
[JsonSerializable(typeof(JsonPoint))]
// Collections
[JsonSerializable(typeof(Dictionary<string, TexturePackerFrame>))]
[JsonSerializable(typeof(Dictionary<string, AsepriteFrame>))]
[JsonSerializable(typeof(List<AsepriteFrameTag>))]
[JsonSerializable(typeof(List<AsepriteLayer>))]
[JsonSerializable(typeof(List<AsepriteSlice>))]
[JsonSerializable(typeof(List<AsepriteSliceKey>))]
// Animation format
[JsonSerializable(typeof(AnimationFileJson))]
[JsonSerializable(typeof(AnimationFrameJson))]
[JsonSerializable(typeof(AnimationEventJson))]
[JsonSerializable(typeof(List<AnimationFrameJson>))]
[JsonSerializable(typeof(List<AnimationEventJson>))]
internal partial class AtlasJsonContext : JsonSerializerContext
{
}
