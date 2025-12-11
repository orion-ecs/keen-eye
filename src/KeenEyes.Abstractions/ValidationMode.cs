namespace KeenEyes;

/// <summary>
/// Specifies the validation mode for component constraints in a World.
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// Validation is always enabled. All component constraints are checked.
    /// This is the default mode.
    /// </summary>
    Enabled,

    /// <summary>
    /// Validation is disabled. No constraint checks are performed.
    /// Use this for maximum performance in production builds.
    /// </summary>
    Disabled,

    /// <summary>
    /// Validation is only enabled in debug builds (when DEBUG is defined).
    /// This provides safety during development without production overhead.
    /// </summary>
    DebugOnly
}
