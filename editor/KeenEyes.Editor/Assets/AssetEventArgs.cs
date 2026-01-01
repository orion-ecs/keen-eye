namespace KeenEyes.Editor.Assets;

/// <summary>
/// Event arguments for asset database events.
/// </summary>
public sealed class AssetEventArgs : EventArgs
{
    /// <summary>
    /// Creates new asset event args.
    /// </summary>
    /// <param name="asset">The affected asset.</param>
    public AssetEventArgs(AssetEntry asset)
    {
        Asset = asset;
    }

    /// <summary>
    /// Gets the affected asset.
    /// </summary>
    public AssetEntry Asset { get; }
}
