namespace KeenEyes;

public partial class World
{
    private SceneManager? sceneManager;

    /// <summary>
    /// Gets the scene manager for spawning and managing scenes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scene manager provides the runtime API for the unified scene model.
    /// Scenes are entity hierarchies that can be spawned, unloaded, and managed.
    /// </para>
    /// <para>
    /// The manager is lazily initialized on first access.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Spawn a scene
    /// var level = world.Scenes.Spawn("ForestLevel");
    ///
    /// // Create an NPC in the scene
    /// var npc = world.Spawn().Build();
    /// world.Scenes.AddToScene(npc, level);
    ///
    /// // Later, unload the scene
    /// world.Scenes.Unload(level);
    /// </code>
    /// </example>
    public SceneManager Scenes => sceneManager ??= new SceneManager(this);
}
