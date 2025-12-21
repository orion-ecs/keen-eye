namespace KeenEyes.Assets;

/// <summary>
/// Context information passed to asset loaders during loading operations.
/// </summary>
/// <param name="Path">The path of the asset being loaded.</param>
/// <param name="Manager">The asset manager performing the load.</param>
/// <param name="Services">Optional service provider for dependency injection.</param>
public readonly record struct AssetLoadContext(
    string Path,
    AssetManager Manager,
    IServiceProvider? Services = null
);
