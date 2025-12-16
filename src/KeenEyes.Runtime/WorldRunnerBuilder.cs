namespace KeenEyes.Runtime;

/// <summary>
/// Fluent builder for configuring and running the world's main loop.
/// </summary>
/// <remarks>
/// <para>
/// This builder provides a backend-agnostic way to configure the main loop lifecycle.
/// It works with any plugin that implements <see cref="ILoopProvider"/>, enabling
/// backends to be swapped without changing application code.
/// </para>
/// <para>
/// By default, if no <see cref="OnUpdate(Action{float})"/> callback is provided,
/// the world's <see cref="KeenEyes.IWorld.Update(float)"/> method is called automatically
/// each frame.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple usage with auto-update
/// world.CreateRunner()
///     .OnReady(() =&gt; CreateScene(world))
///     .OnResize((w, h) =&gt; Console.WriteLine($"Resized: {w}x{h}"))
///     .Run();
///
/// // Explicit update control
/// world.CreateRunner()
///     .OnReady(() =&gt; CreateScene(world))
///     .OnUpdate((dt) =&gt;
///     {
///         HandleInput();
///         world.Update(dt);
///         PostUpdate();
///     })
///     .Run();
/// </code>
/// </example>
public sealed class WorldRunnerBuilder
{
    private readonly IWorld world;
    private readonly ILoopProvider loopProvider;

    private Action? onReady;
    private Action<float>? onUpdate;
    private Action<float>? onRender;
    private Action<int, int>? onResize;
    private Action? onClosing;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldRunnerBuilder"/> class.
    /// </summary>
    /// <param name="world">The world to run.</param>
    /// <param name="loopProvider">The loop provider to use.</param>
    internal WorldRunnerBuilder(IWorld world, ILoopProvider loopProvider)
    {
        this.world = world;
        this.loopProvider = loopProvider;
    }

    /// <summary>
    /// Sets a callback to be invoked once when the loop is ready.
    /// </summary>
    /// <param name="callback">The callback to invoke when ready.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Use this callback to set up your scene, create entities, and load resources.
    /// At this point, the window is created and graphics context is available.
    /// </remarks>
    public WorldRunnerBuilder OnReady(Action callback)
    {
        onReady = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked each frame for update logic.
    /// </summary>
    /// <param name="callback">The callback to invoke each frame.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// If not set, <see cref="KeenEyes.IWorld.Update(float)"/> is called automatically.
    /// If set, you are responsible for calling <c>world.Update(dt)</c> yourself.
    /// </remarks>
    public WorldRunnerBuilder OnUpdate(Action<float> callback)
    {
        onUpdate = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked each frame for rendering.
    /// </summary>
    /// <param name="callback">The callback to invoke each frame after update.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Most rendering should be handled by systems in the Render phase.
    /// This callback is for additional rendering outside the system pipeline.
    /// </remarks>
    public WorldRunnerBuilder OnRender(Action<float> callback)
    {
        onRender = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked when the window is resized.
    /// </summary>
    /// <param name="callback">The callback to invoke with new width and height.</param>
    /// <returns>This builder for chaining.</returns>
    public WorldRunnerBuilder OnResize(Action<int, int> callback)
    {
        onResize = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked when the loop is closing.
    /// </summary>
    /// <param name="callback">The callback to invoke on close.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Use this for cleanup that must happen before the window closes.
    /// Note that <see cref="IWorld"/> disposal should be handled via <c>using</c> statements.
    /// </remarks>
    public WorldRunnerBuilder OnClosing(Action callback)
    {
        onClosing = callback;
        return this;
    }

    /// <summary>
    /// Initializes and runs the main loop. Blocks until closed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method wires up all configured callbacks to the loop provider,
    /// initializes the loop (creating the window), and runs until closed.
    /// </para>
    /// <para>
    /// If no <see cref="OnUpdate(Action{float})"/> callback was provided,
    /// <see cref="IWorld.Update(float)"/> is called automatically each frame.
    /// </para>
    /// </remarks>
    public void Run()
    {
        // Wire up callbacks
        if (onReady is not null)
        {
            loopProvider.OnReady += onReady;
        }

        // Update handler - auto-update world if no explicit callback provided
        Action<float> updateHandler = onUpdate ?? ((dt) => world.Update(dt));
        loopProvider.OnUpdate += updateHandler;

        if (onRender is not null)
        {
            loopProvider.OnRender += onRender;
        }

        if (onResize is not null)
        {
            loopProvider.OnResize += onResize;
        }

        if (onClosing is not null)
        {
            loopProvider.OnClosing += onClosing;
        }

        try
        {
            // Initialize and run the loop
            loopProvider.Initialize();
            loopProvider.Run();
        }
        finally
        {
            // Unhook callbacks to avoid memory leaks
            if (onReady is not null)
            {
                loopProvider.OnReady -= onReady;
            }

            loopProvider.OnUpdate -= updateHandler;

            if (onRender is not null)
            {
                loopProvider.OnRender -= onRender;
            }

            if (onResize is not null)
            {
                loopProvider.OnResize -= onResize;
            }

            if (onClosing is not null)
            {
                loopProvider.OnClosing -= onClosing;
            }
        }
    }
}
