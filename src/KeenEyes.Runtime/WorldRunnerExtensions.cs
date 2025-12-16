namespace KeenEyes.Runtime;

/// <summary>
/// Extension methods for running worlds with main loops.
/// </summary>
public static class WorldRunnerExtensions
{
    /// <summary>
    /// Creates a runner builder for configuring and running the world's main loop.
    /// </summary>
    /// <param name="world">The world to run.</param>
    /// <returns>A builder for configuring the main loop.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="ILoopProvider"/> is registered. Install a plugin that
    /// provides a main loop (e.g., SilkGraphicsPlugin) before calling this method.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method requires a plugin that implements <see cref="ILoopProvider"/> to be
    /// installed on the world. Graphics plugins typically provide this interface.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var world = new World();
    /// world.InstallPlugin(new SilkGraphicsPlugin(config));
    /// world.AddSystem&lt;CameraSystem&gt;(SystemPhase.EarlyUpdate);
    /// world.AddSystem&lt;RenderSystem&gt;(SystemPhase.Render);
    ///
    /// world.CreateRunner()
    ///     .OnReady(() =&gt; CreateScene(world))
    ///     .Run();
    /// </code>
    /// </example>
    public static WorldRunnerBuilder CreateRunner(this IWorld world)
    {
        if (!world.TryGetExtension<ILoopProvider>(out var loopProvider))
        {
            throw new InvalidOperationException(
                "No ILoopProvider found. Install a plugin that provides a main loop " +
                "(e.g., SilkGraphicsPlugin) before calling CreateRunner().");
        }

        return new WorldRunnerBuilder(world, loopProvider!);
    }
}
