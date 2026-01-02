namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for extending asset handling in the editor.
/// Allows plugins to register custom importers, thumbnail generators, and handlers.
/// </summary>
public interface IAssetCapability : IEditorCapability
{
    /// <summary>
    /// Registers an asset importer for specific file extensions.
    /// </summary>
    /// <param name="extensions">The file extensions to handle (e.g., ".png", ".obj").</param>
    /// <param name="importer">The importer instance.</param>
    void RegisterImporter(string[] extensions, IAssetImporter importer);

    /// <summary>
    /// Unregisters an asset importer.
    /// </summary>
    /// <param name="importerId">The importer ID to unregister.</param>
    /// <returns>True if the importer was found and unregistered.</returns>
    bool UnregisterImporter(string importerId);

    /// <summary>
    /// Gets the importer for a specific file extension.
    /// </summary>
    /// <param name="extension">The file extension (with leading dot).</param>
    /// <returns>The importer, or null if none registered.</returns>
    IAssetImporter? GetImporter(string extension);

    /// <summary>
    /// Gets all registered importers.
    /// </summary>
    /// <returns>The importers with their supported extensions.</returns>
    IEnumerable<(IAssetImporter Importer, string[] Extensions)> GetImporters();

    /// <summary>
    /// Registers a thumbnail generator for a specific asset type.
    /// </summary>
    /// <typeparam name="TAsset">The asset type.</typeparam>
    /// <param name="generator">The thumbnail generator.</param>
    void RegisterThumbnailGenerator<TAsset>(IThumbnailGenerator generator);

    /// <summary>
    /// Registers a thumbnail generator for a specific asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <param name="generator">The thumbnail generator.</param>
    void RegisterThumbnailGenerator(Type assetType, IThumbnailGenerator generator);

    /// <summary>
    /// Gets the thumbnail generator for an asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <returns>The thumbnail generator, or null if none registered.</returns>
    IThumbnailGenerator? GetThumbnailGenerator(Type assetType);

    /// <summary>
    /// Registers a drag-and-drop handler for a specific asset type.
    /// </summary>
    /// <typeparam name="TAsset">The asset type.</typeparam>
    /// <param name="handler">The drag-drop handler.</param>
    void RegisterDragDropHandler<TAsset>(IAssetDragDropHandler handler);

    /// <summary>
    /// Registers a drag-and-drop handler for a specific asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <param name="handler">The drag-drop handler.</param>
    void RegisterDragDropHandler(Type assetType, IAssetDragDropHandler handler);

    /// <summary>
    /// Gets the drag-drop handler for an asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <returns>The drag-drop handler, or null if none registered.</returns>
    IAssetDragDropHandler? GetDragDropHandler(Type assetType);

    /// <summary>
    /// Registers a double-click action for a specific asset type.
    /// </summary>
    /// <typeparam name="TAsset">The asset type.</typeparam>
    /// <param name="action">The action to execute on double-click.</param>
    void RegisterDoubleClickAction<TAsset>(Action<TAsset> action);

    /// <summary>
    /// Registers a double-click action for a specific asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <param name="action">The action to execute on double-click.</param>
    void RegisterDoubleClickAction(Type assetType, Action<object> action);

    /// <summary>
    /// Gets the double-click action for an asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <returns>The action, or null if none registered.</returns>
    Action<object>? GetDoubleClickAction(Type assetType);

    /// <summary>
    /// Registers a context menu provider for a specific asset type.
    /// </summary>
    /// <typeparam name="TAsset">The asset type.</typeparam>
    /// <param name="provider">The context menu provider.</param>
    void RegisterContextMenuProvider<TAsset>(IAssetContextMenuProvider provider);

    /// <summary>
    /// Registers a context menu provider for a specific asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <param name="provider">The context menu provider.</param>
    void RegisterContextMenuProvider(Type assetType, IAssetContextMenuProvider provider);

    /// <summary>
    /// Gets context menu providers for an asset type.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <returns>The context menu providers.</returns>
    IEnumerable<IAssetContextMenuProvider> GetContextMenuProviders(Type assetType);

    /// <summary>
    /// Event raised when an importer is registered.
    /// </summary>
    event Action<IAssetImporter, string[]>? ImporterRegistered;

    /// <summary>
    /// Event raised when an importer is unregistered.
    /// </summary>
    event Action<string>? ImporterUnregistered;
}

/// <summary>
/// Interface for importing assets from files.
/// </summary>
public interface IAssetImporter
{
    /// <summary>
    /// Gets the unique identifier for this importer.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name for this importer.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the priority of this importer. Higher values take precedence.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the supported file extensions.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Checks if this importer can import a file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if this importer can handle the file.</returns>
    bool CanImport(string filePath);

    /// <summary>
    /// Imports an asset from a file.
    /// </summary>
    /// <param name="context">The import context.</param>
    /// <returns>The import result.</returns>
    Task<AssetImportResult> ImportAsync(AssetImportContext context);

    /// <summary>
    /// Gets import settings UI for this importer.
    /// </summary>
    /// <returns>The settings UI, or null if no settings are available.</returns>
    IImportSettingsUI? GetSettingsUI();
}

/// <summary>
/// Context for importing assets.
/// </summary>
public sealed class AssetImportContext
{
    /// <summary>
    /// Gets the source file path.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Gets the destination path in the project.
    /// </summary>
    public required string DestinationPath { get; init; }

    /// <summary>
    /// Gets the project root directory.
    /// </summary>
    public required string ProjectRoot { get; init; }

    /// <summary>
    /// Gets the import settings.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Settings { get; init; }

    /// <summary>
    /// Gets the progress reporter.
    /// </summary>
    public IProgress<AssetImportProgress>? Progress { get; init; }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Progress information for asset import.
/// </summary>
public sealed class AssetImportProgress
{
    /// <summary>
    /// Gets the current operation name.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public float Percentage { get; init; }

    /// <summary>
    /// Gets optional status message.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Result of an asset import operation.
/// </summary>
public sealed class AssetImportResult
{
    /// <summary>
    /// Gets whether the import was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the imported asset path.
    /// </summary>
    public string? ImportedPath { get; init; }

    /// <summary>
    /// Gets any additional assets that were created.
    /// </summary>
    public IReadOnlyList<string>? AdditionalAssets { get; init; }

    /// <summary>
    /// Gets the error message if import failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets any warnings generated during import.
    /// </summary>
    public IReadOnlyList<string>? Warnings { get; init; }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static AssetImportResult Succeeded(string importedPath, IReadOnlyList<string>? additionalAssets = null) =>
        new()
        {
            Success = true,
            ImportedPath = importedPath,
            AdditionalAssets = additionalAssets
        };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static AssetImportResult Failed(string errorMessage) =>
        new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Interface for import settings UI.
/// </summary>
public interface IImportSettingsUI
{
    /// <summary>
    /// Creates the settings UI.
    /// </summary>
    /// <param name="context">The editor context.</param>
    /// <param name="parent">The parent entity.</param>
    /// <returns>The root UI entity.</returns>
    Entity CreateUI(IEditorContext context, Entity parent);

    /// <summary>
    /// Gets the current settings from the UI.
    /// </summary>
    /// <returns>The settings dictionary.</returns>
    IReadOnlyDictionary<string, object> GetSettings();

    /// <summary>
    /// Loads settings into the UI.
    /// </summary>
    /// <param name="settings">The settings to load.</param>
    void LoadSettings(IReadOnlyDictionary<string, object> settings);
}

/// <summary>
/// Interface for generating asset thumbnails.
/// </summary>
public interface IThumbnailGenerator
{
    /// <summary>
    /// Gets the unique identifier for this generator.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the priority of this generator. Higher values take precedence.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if this generator can generate a thumbnail for an asset.
    /// </summary>
    /// <param name="assetPath">The asset path.</param>
    /// <returns>True if this generator can handle the asset.</returns>
    bool CanGenerate(string assetPath);

    /// <summary>
    /// Generates a thumbnail for an asset.
    /// </summary>
    /// <param name="context">The thumbnail context.</param>
    /// <returns>The generated thumbnail.</returns>
    Task<ThumbnailResult> GenerateAsync(ThumbnailContext context);
}

/// <summary>
/// Context for generating thumbnails.
/// </summary>
public sealed class ThumbnailContext
{
    /// <summary>
    /// Gets the asset path.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Gets the requested thumbnail width.
    /// </summary>
    public int Width { get; init; } = 64;

    /// <summary>
    /// Gets the requested thumbnail height.
    /// </summary>
    public int Height { get; init; } = 64;

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Result of a thumbnail generation operation.
/// </summary>
public sealed class ThumbnailResult
{
    /// <summary>
    /// Gets whether generation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the thumbnail image data (RGBA).
    /// </summary>
    public byte[]? ImageData { get; init; }

    /// <summary>
    /// Gets the thumbnail width.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets the thumbnail height.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Gets the error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static ThumbnailResult Succeeded(byte[] imageData, int width, int height) =>
        new()
        {
            Success = true,
            ImageData = imageData,
            Width = width,
            Height = height
        };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static ThumbnailResult Failed(string errorMessage) =>
        new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Interface for handling asset drag-and-drop operations.
/// </summary>
public interface IAssetDragDropHandler
{
    /// <summary>
    /// Gets the unique identifier for this handler.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the priority of this handler. Higher values take precedence.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if this handler can handle a drag operation.
    /// </summary>
    /// <param name="context">The drag context.</param>
    /// <returns>The drag effect, or None if not handled.</returns>
    DragDropEffect CanDrag(AssetDragContext context);

    /// <summary>
    /// Checks if this handler can handle a drop operation.
    /// </summary>
    /// <param name="context">The drop context.</param>
    /// <returns>The drag effect, or None if not handled.</returns>
    DragDropEffect CanDrop(AssetDropContext context);

    /// <summary>
    /// Handles a drop operation.
    /// </summary>
    /// <param name="context">The drop context.</param>
    /// <returns>True if the drop was handled.</returns>
    bool OnDrop(AssetDropContext context);
}

/// <summary>
/// Context for asset drag operations.
/// </summary>
public sealed class AssetDragContext
{
    /// <summary>
    /// Gets the asset entry being dragged.
    /// </summary>
    public required AssetEntry Asset { get; init; }

    /// <summary>
    /// Gets the editor context.
    /// </summary>
    public required IEditorContext EditorContext { get; init; }
}

/// <summary>
/// Context for asset drop operations.
/// </summary>
public sealed class AssetDropContext
{
    /// <summary>
    /// Gets the asset entry being dropped.
    /// </summary>
    public required AssetEntry Asset { get; init; }

    /// <summary>
    /// Gets the editor context.
    /// </summary>
    public required IEditorContext EditorContext { get; init; }

    /// <summary>
    /// Gets the drop target type.
    /// </summary>
    public required DropTargetType TargetType { get; init; }

    /// <summary>
    /// Gets the target entity (if dropping on an entity).
    /// </summary>
    public Entity TargetEntity { get; init; }

    /// <summary>
    /// Gets the target path (if dropping in a file browser).
    /// </summary>
    public string? TargetPath { get; init; }

    /// <summary>
    /// Gets the drop position in world coordinates (if dropping in viewport).
    /// </summary>
    public System.Numerics.Vector3? WorldPosition { get; init; }
}

/// <summary>
/// Types of drop targets.
/// </summary>
public enum DropTargetType
{
    /// <summary>
    /// Unknown target.
    /// </summary>
    Unknown,

    /// <summary>
    /// Dropping on an entity in the hierarchy.
    /// </summary>
    Entity,

    /// <summary>
    /// Dropping in the viewport.
    /// </summary>
    Viewport,

    /// <summary>
    /// Dropping in the project browser.
    /// </summary>
    ProjectBrowser,

    /// <summary>
    /// Dropping on a property field.
    /// </summary>
    PropertyField
}

/// <summary>
/// Drag-and-drop effect types.
/// </summary>
public enum DragDropEffect
{
    /// <summary>
    /// No drag-drop allowed.
    /// </summary>
    None,

    /// <summary>
    /// Copy operation.
    /// </summary>
    Copy,

    /// <summary>
    /// Move operation.
    /// </summary>
    Move,

    /// <summary>
    /// Link/reference operation.
    /// </summary>
    Link
}

/// <summary>
/// Interface for providing context menu items for assets.
/// </summary>
public interface IAssetContextMenuProvider
{
    /// <summary>
    /// Gets the unique identifier for this provider.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the priority of this provider. Higher values appear first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets context menu items for an asset.
    /// </summary>
    /// <param name="context">The context menu context.</param>
    /// <returns>The menu items to add.</returns>
    IEnumerable<AssetMenuItem> GetMenuItems(AssetContextMenuContext context);
}

/// <summary>
/// Context for asset context menus.
/// </summary>
public sealed class AssetContextMenuContext
{
    /// <summary>
    /// Gets the asset entry.
    /// </summary>
    public required AssetEntry Asset { get; init; }

    /// <summary>
    /// Gets the editor context.
    /// </summary>
    public required IEditorContext EditorContext { get; init; }

    /// <summary>
    /// Gets whether multiple assets are selected.
    /// </summary>
    public bool MultipleSelected { get; init; }

    /// <summary>
    /// Gets all selected assets (when multiple are selected).
    /// </summary>
    public IReadOnlyList<AssetEntry>? SelectedAssets { get; init; }
}

/// <summary>
/// Represents a menu item for asset context menus.
/// </summary>
public sealed class AssetMenuItem
{
    /// <summary>
    /// Gets the display name of the menu item.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the action to execute when clicked.
    /// </summary>
    public required Action Execute { get; init; }

    /// <summary>
    /// Gets whether the menu item is currently enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the optional icon identifier.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the optional keyboard shortcut.
    /// </summary>
    public string? Shortcut { get; init; }

    /// <summary>
    /// Gets whether to show a separator before this item.
    /// </summary>
    public bool SeparatorBefore { get; init; }

    /// <summary>
    /// Gets child menu items for submenus.
    /// </summary>
    public IReadOnlyList<AssetMenuItem>? Children { get; init; }
}
