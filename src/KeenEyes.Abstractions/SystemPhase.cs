namespace KeenEyes;

/// <summary>
/// Defines when a system executes in the game loop.
/// </summary>
public enum SystemPhase
{
    /// <summary>Runs at the start of each frame before other updates.</summary>
    EarlyUpdate,

    /// <summary>Runs at a fixed timestep, ideal for physics.</summary>
    FixedUpdate,

    /// <summary>Main update phase, runs every frame.</summary>
    Update,

    /// <summary>Runs after Update, before rendering.</summary>
    LateUpdate,

    /// <summary>Runs during the render phase.</summary>
    Render,

    /// <summary>Runs after rendering completes.</summary>
    PostRender
}
