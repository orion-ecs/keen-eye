using KeenEyes.Animation.Data;

namespace KeenEyes.Animation;

/// <summary>
/// Manages animation assets (clips, sprite sheets, controllers) for a world.
/// </summary>
/// <remarks>
/// <para>
/// AnimationManager stores and retrieves animation assets by ID. Components
/// reference assets by ID rather than holding direct references, keeping
/// components as pure data.
/// </para>
/// <para>
/// This manager is exposed as a world extension and can be accessed via
/// <c>world.GetExtension&lt;AnimationManager&gt;()</c>.
/// </para>
/// </remarks>
public sealed class AnimationManager : IDisposable
{
    private readonly Dictionary<int, AnimationClip> clips = [];
    private readonly Dictionary<int, SpriteSheet> spriteSheets = [];
    private readonly Dictionary<int, AnimatorController> controllers = [];
    private int nextClipId = 1;
    private int nextSheetId = 1;
    private int nextControllerId = 1;

    /// <summary>
    /// Gets the number of registered animation clips.
    /// </summary>
    public int ClipCount => clips.Count;

    /// <summary>
    /// Gets the number of registered sprite sheets.
    /// </summary>
    public int SpriteSheetCount => spriteSheets.Count;

    /// <summary>
    /// Gets the number of registered animator controllers.
    /// </summary>
    public int ControllerCount => controllers.Count;

    #region Animation Clips

    /// <summary>
    /// Registers an animation clip and returns its ID.
    /// </summary>
    /// <param name="clip">The animation clip to register.</param>
    /// <returns>The ID assigned to the clip.</returns>
    public int RegisterClip(AnimationClip clip)
    {
        var id = nextClipId++;
        clips[id] = clip;
        return id;
    }

    /// <summary>
    /// Gets an animation clip by ID.
    /// </summary>
    /// <param name="id">The clip ID.</param>
    /// <returns>The animation clip.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the clip is not found.</exception>
    public AnimationClip GetClip(int id)
    {
        return clips[id];
    }

    /// <summary>
    /// Tries to get an animation clip by ID.
    /// </summary>
    /// <param name="id">The clip ID.</param>
    /// <param name="clip">The animation clip, if found.</param>
    /// <returns>True if the clip was found.</returns>
    public bool TryGetClip(int id, out AnimationClip? clip)
    {
        return clips.TryGetValue(id, out clip);
    }

    /// <summary>
    /// Unregisters an animation clip.
    /// </summary>
    /// <param name="id">The clip ID.</param>
    /// <returns>True if the clip was removed.</returns>
    public bool UnregisterClip(int id)
    {
        return clips.Remove(id);
    }

    #endregion

    #region Sprite Sheets

    /// <summary>
    /// Registers a sprite sheet and returns its ID.
    /// </summary>
    /// <param name="sheet">The sprite sheet to register.</param>
    /// <returns>The ID assigned to the sheet.</returns>
    public int RegisterSpriteSheet(SpriteSheet sheet)
    {
        var id = nextSheetId++;
        spriteSheets[id] = sheet;
        return id;
    }

    /// <summary>
    /// Gets a sprite sheet by ID.
    /// </summary>
    /// <param name="id">The sheet ID.</param>
    /// <returns>The sprite sheet.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the sheet is not found.</exception>
    public SpriteSheet GetSpriteSheet(int id)
    {
        return spriteSheets[id];
    }

    /// <summary>
    /// Tries to get a sprite sheet by ID.
    /// </summary>
    /// <param name="id">The sheet ID.</param>
    /// <param name="sheet">The sprite sheet, if found.</param>
    /// <returns>True if the sheet was found.</returns>
    public bool TryGetSpriteSheet(int id, out SpriteSheet? sheet)
    {
        return spriteSheets.TryGetValue(id, out sheet);
    }

    /// <summary>
    /// Unregisters a sprite sheet.
    /// </summary>
    /// <param name="id">The sheet ID.</param>
    /// <returns>True if the sheet was removed.</returns>
    public bool UnregisterSpriteSheet(int id)
    {
        return spriteSheets.Remove(id);
    }

    #endregion

    #region Animator Controllers

    /// <summary>
    /// Registers an animator controller and returns its ID.
    /// </summary>
    /// <param name="controller">The controller to register.</param>
    /// <returns>The ID assigned to the controller.</returns>
    public int RegisterController(AnimatorController controller)
    {
        var id = nextControllerId++;
        controllers[id] = controller;
        return id;
    }

    /// <summary>
    /// Gets an animator controller by ID.
    /// </summary>
    /// <param name="id">The controller ID.</param>
    /// <returns>The animator controller.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the controller is not found.</exception>
    public AnimatorController GetController(int id)
    {
        return controllers[id];
    }

    /// <summary>
    /// Tries to get an animator controller by ID.
    /// </summary>
    /// <param name="id">The controller ID.</param>
    /// <param name="controller">The animator controller, if found.</param>
    /// <returns>True if the controller was found.</returns>
    public bool TryGetController(int id, out AnimatorController? controller)
    {
        return controllers.TryGetValue(id, out controller);
    }

    /// <summary>
    /// Unregisters an animator controller.
    /// </summary>
    /// <param name="id">The controller ID.</param>
    /// <returns>True if the controller was removed.</returns>
    public bool UnregisterController(int id)
    {
        return controllers.Remove(id);
    }

    #endregion

    /// <summary>
    /// Clears all registered assets.
    /// </summary>
    public void Clear()
    {
        clips.Clear();
        spriteSheets.Clear();
        controllers.Clear();
        nextClipId = 1;
        nextSheetId = 1;
        nextControllerId = 1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Clear();
    }
}
