namespace KeenEyes.Assets;

/// <summary>
/// Represents the current state of an asset in the loading lifecycle.
/// </summary>
public enum AssetState
{
    /// <summary>The asset handle is invalid or has been disposed.</summary>
    Invalid = 0,

    /// <summary>The asset is queued for loading but has not started.</summary>
    Pending,

    /// <summary>The asset is currently being loaded.</summary>
    Loading,

    /// <summary>The asset has been loaded successfully and is ready for use.</summary>
    Loaded,

    /// <summary>The asset failed to load.</summary>
    Failed,

    /// <summary>The asset has been unloaded from memory.</summary>
    Unloaded
}
