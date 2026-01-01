namespace KeenEyes;

public sealed partial class World
{
    #region Systems

    /// <summary>
    /// Adds a system to this world with specified execution phase and order.
    /// </summary>
    /// <typeparam name="T">The system type to add.</typeparam>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Systems are automatically sorted by phase then by order when <see cref="Update"/> is called.
    /// Systems in earlier phases (e.g., <see cref="SystemPhase.EarlyUpdate"/>) execute before
    /// systems in later phases (e.g., <see cref="SystemPhase.LateUpdate"/>).
    /// </para>
    /// <para>
    /// Within the same phase, systems with lower order values execute first.
    /// Systems with the same phase and order maintain stable relative ordering.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.AddSystem&lt;InputSystem&gt;(SystemPhase.EarlyUpdate, order: -10)
    ///      .AddSystem&lt;PhysicsSystem&gt;(SystemPhase.FixedUpdate)
    ///      .AddSystem&lt;MovementSystem&gt;(SystemPhase.Update, order: 0)
    ///      .AddSystem&lt;RenderSystem&gt;(SystemPhase.Render);
    /// </code>
    /// </example>
    public World AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0) where T : ISystem, new()
    {
        systemManager.AddSystem<T>(phase, order, runsBefore: [], runsAfter: []);
        return this;
    }

    /// <summary>
    /// Adds a system to this world with specified execution phase, order, and dependency constraints.
    /// </summary>
    /// <typeparam name="T">The system type to add.</typeparam>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="runsBefore"/> and <paramref name="runsAfter"/> constraints define
    /// explicit ordering dependencies between systems within the same phase. Systems are sorted
    /// using topological sorting to respect these constraints.
    /// </para>
    /// <para>
    /// If constraints create a cycle (e.g., A runs before B, B runs before A), an
    /// <see cref="InvalidOperationException"/> is thrown during the first <see cref="Update"/> call.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // MovementSystem must run after InputSystem and before RenderSystem
    /// world.AddSystem&lt;MovementSystem&gt;(
    ///     SystemPhase.Update,
    ///     order: 0,
    ///     runsBefore: [typeof(RenderSystem)],
    ///     runsAfter: [typeof(InputSystem)]);
    /// </code>
    /// </example>
    public World AddSystem<T>(
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter) where T : ISystem, new()
    {
        systemManager.AddSystem<T>(phase, order, runsBefore, runsAfter);
        return this;
    }

    /// <summary>
    /// Adds a system instance to this world with specified execution phase and order.
    /// </summary>
    /// <param name="system">The system instance to add.</param>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured system instance or a system
    /// that requires constructor parameters.
    /// </para>
    /// <para>
    /// Systems are automatically sorted by phase then by order when <see cref="Update"/> is called.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customSystem = new CustomSystem("config");
    /// world.AddSystem(customSystem, SystemPhase.Update, order: 10);
    /// </code>
    /// </example>
    public World AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        systemManager.AddSystem(system, phase, order, runsBefore: [], runsAfter: []);
        return this;
    }

    /// <summary>
    /// Adds a system instance to this world with specified execution phase, order, and dependency constraints.
    /// </summary>
    /// <param name="system">The system instance to add.</param>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured system instance with explicit
    /// ordering constraints.
    /// </para>
    /// <para>
    /// The <paramref name="runsBefore"/> and <paramref name="runsAfter"/> constraints define
    /// explicit ordering dependencies between systems within the same phase. Systems are sorted
    /// using topological sorting to respect these constraints.
    /// </para>
    /// <para>
    /// If constraints create a cycle (e.g., A runs before B, B runs before A), an
    /// <see cref="InvalidOperationException"/> is thrown during the first <see cref="Update"/> call.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customSystem = new CustomSystem("config");
    /// world.AddSystem(
    ///     customSystem,
    ///     SystemPhase.Update,
    ///     order: 0,
    ///     runsBefore: [typeof(RenderSystem)],
    ///     runsAfter: [typeof(InputSystem)]);
    /// </code>
    /// </example>
    public World AddSystem(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
    {
        systemManager.AddSystem(system, phase, order, runsBefore, runsAfter);
        return this;
    }

    /// <summary>
    /// Updates all enabled systems with the given delta time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <remarks>
    /// <para>
    /// Systems are executed in order of their phase (earliest first), then by their order
    /// value within each phase (lowest first). Disabled systems are skipped.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the following lifecycle methods
    /// are called in order: <c>OnBeforeUpdate</c>, <c>Update</c>, <c>OnAfterUpdate</c>.
    /// </para>
    /// </remarks>
    public void Update(float deltaTime)
        => systemManager.Update(deltaTime);

    /// <summary>
    /// Updates only systems in the <see cref="SystemPhase.FixedUpdate"/> phase.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed timestep interval.</param>
    /// <remarks>
    /// <para>
    /// This method is intended for fixed-timestep physics and simulation updates that
    /// should run at a consistent rate regardless of frame rate. Call this method from
    /// your game loop's fixed update tick.
    /// </para>
    /// <para>
    /// Only systems registered with <see cref="SystemPhase.FixedUpdate"/> will be executed.
    /// Systems in other phases are not affected by this method.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the following lifecycle methods
    /// are called in order: <c>OnBeforeUpdate</c>, <c>Update</c>, <c>OnAfterUpdate</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Typical game loop with fixed timestep
    /// float accumulator = 0f;
    /// const float fixedDeltaTime = 1f / 60f;
    ///
    /// while (running)
    /// {
    ///     float frameTime = GetFrameTime();
    ///     accumulator += frameTime;
    ///
    ///     while (accumulator >= fixedDeltaTime)
    ///     {
    ///         world.FixedUpdate(fixedDeltaTime);
    ///         accumulator -= fixedDeltaTime;
    ///     }
    ///
    ///     world.Update(frameTime);
    /// }
    /// </code>
    /// </example>
    public void FixedUpdate(float fixedDeltaTime)
        => systemManager.FixedUpdate(fixedDeltaTime);

    /// <summary>
    /// Gets a system of the specified type from this world.
    /// </summary>
    /// <typeparam name="T">The type of system to retrieve.</typeparam>
    /// <returns>The system instance, or null if not found.</returns>
    /// <remarks>
    /// This method searches for systems by type, including systems nested within
    /// <see cref="SystemGroup"/> instances.
    /// </remarks>
    /// <example>
    /// <code>
    /// var physics = world.GetSystem&lt;PhysicsSystem&gt;();
    /// if (physics is not null)
    /// {
    ///     physics.Enabled = false; // Pause physics
    /// }
    /// </code>
    /// </example>
    public T? GetSystem<T>() where T : class, ISystem
        => systemManager.GetSystem<T>();

    /// <summary>
    /// Enables a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of system to enable.</typeparam>
    /// <returns>True if the system was found and enabled; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// If the system was already enabled, this method has no effect but still returns true.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the <c>OnEnabled</c> callback
    /// is invoked when transitioning from disabled to enabled state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Resume physics simulation
    /// world.EnableSystem&lt;PhysicsSystem&gt;();
    /// </code>
    /// </example>
    public bool EnableSystem<T>() where T : class, ISystem
        => systemManager.EnableSystem<T>();

    /// <summary>
    /// Disables a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of system to disable.</typeparam>
    /// <returns>True if the system was found and disabled; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// Disabled systems are skipped during <see cref="Update"/> calls.
    /// </para>
    /// <para>
    /// If the system was already disabled, this method has no effect but still returns true.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the <c>OnDisabled</c> callback
    /// is invoked when transitioning from enabled to disabled state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Pause physics simulation
    /// world.DisableSystem&lt;PhysicsSystem&gt;();
    /// </code>
    /// </example>
    public bool DisableSystem<T>() where T : class, ISystem
        => systemManager.DisableSystem<T>();

    /// <summary>
    /// Removes a system instance from this world.
    /// </summary>
    /// <param name="system">The system instance to remove.</param>
    /// <returns>True if the system was found and removed; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// The system will no longer receive update calls after removal.
    /// Any resources held by the system should be disposed by the caller if needed.
    /// </para>
    /// <para>
    /// This method is primarily intended for hot reload scenarios where systems
    /// need to be unregistered before loading new versions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var physics = world.GetSystem&lt;PhysicsSystem&gt;();
    /// if (physics is not null)
    /// {
    ///     world.RemoveSystem(physics);
    /// }
    /// </code>
    /// </example>
    public bool RemoveSystem(ISystem system)
        => systemManager.RemoveSystem(system);

    /// <summary>
    /// Removes a system of the specified type from this world.
    /// </summary>
    /// <typeparam name="T">The type of system to remove.</typeparam>
    /// <returns>True if the system was found and removed; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// If multiple systems of the same type exist, only the first match is removed.
    /// </para>
    /// <para>
    /// The system will no longer receive update calls after removal.
    /// Any resources held by the system should be disposed by the caller if needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.RemoveSystem&lt;PhysicsSystem&gt;();
    /// </code>
    /// </example>
    public bool RemoveSystem<T>() where T : class, ISystem
    {
        var system = GetSystem<T>();
        return system != null && RemoveSystem(system);
    }

    #endregion
}
