namespace KeenEyes.Editor.Assets;

/// <summary>
/// Types of assets recognized by the editor.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Unknown asset type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Scene file (.kescene).
    /// </summary>
    Scene,

    /// <summary>
    /// Prefab file (.keprefab).
    /// </summary>
    Prefab,

    /// <summary>
    /// World configuration file (.keworld).
    /// </summary>
    WorldConfig,

    /// <summary>
    /// Texture/image file (.png, .jpg, etc.).
    /// </summary>
    Texture,

    /// <summary>
    /// Shader file (.kesl).
    /// </summary>
    Shader,

    /// <summary>
    /// Audio file (.wav, .ogg, .mp3).
    /// </summary>
    Audio,

    /// <summary>
    /// C# script file.
    /// </summary>
    Script,

    /// <summary>
    /// JSON data file.
    /// </summary>
    Data
}
