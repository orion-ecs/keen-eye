namespace KeenEyes.Debugging;

/// <summary>
/// Central controller for debug mode and debugging features.
/// </summary>
/// <remarks>
/// <para>
/// The DebugController provides a central toggle for debug mode that other
/// debugging components can query. When debug mode is enabled, expensive
/// diagnostic operations become available and verbose output may be enabled.
/// </para>
/// <para>
/// This controller integrates with other debugging extensions through the
/// <see cref="DebugModeChanged"/> event, allowing them to respond to debug
/// mode changes dynamically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var controller = world.GetExtension&lt;DebugController&gt;();
///
/// // Subscribe to debug mode changes
/// controller.DebugModeChanged += (sender, enabled) =>
/// {
///     if (enabled)
///     {
///         // Enable verbose logging, expensive checks, etc.
///         logManager.MinimumLevel = LogLevel.Debug;
///     }
///     else
///     {
///         // Restore normal logging level
///         logManager.MinimumLevel = LogLevel.Info;
///     }
/// };
///
/// // Toggle debug mode
/// controller.IsDebugMode = true;
///
/// // Check if debug mode is active
/// if (controller.IsDebugMode)
/// {
///     // Perform expensive diagnostic operation
/// }
/// </code>
/// </example>
/// <param name="initialDebugMode">Initial debug mode state. Defaults to false.</param>
public sealed class DebugController(bool initialDebugMode = false)
{
    private bool isDebugMode = initialDebugMode;

    /// <summary>
    /// Event raised when the debug mode state changes.
    /// </summary>
    /// <remarks>
    /// Subscribers can use this event to adjust logging levels, enable additional
    /// diagnostics, or toggle expensive validation checks.
    /// </remarks>
    public event EventHandler<bool>? DebugModeChanged;

    /// <summary>
    /// Gets or sets whether debug mode is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When debug mode is enabled, debugging components may perform additional
    /// diagnostics, capture more detailed information, or enable verbose logging.
    /// </para>
    /// <para>
    /// Setting this property raises the <see cref="DebugModeChanged"/> event
    /// if the value changes.
    /// </para>
    /// </remarks>
    public bool IsDebugMode
    {
        get => isDebugMode;
        set
        {
            if (isDebugMode != value)
            {
                isDebugMode = value;
                DebugModeChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Gets the number of times debug mode has been toggled.
    /// </summary>
    /// <remarks>
    /// Useful for diagnostics to track how often debug mode is changed.
    /// </remarks>
    public int ToggleCount { get; private set; }

    /// <summary>
    /// Gets the last time debug mode was changed.
    /// </summary>
    /// <remarks>
    /// Returns null if debug mode has never been changed from its initial state.
    /// </remarks>
    public DateTime? LastToggleTime { get; private set; }

    /// <summary>
    /// Toggles the debug mode state.
    /// </summary>
    /// <returns>The new debug mode state after toggling.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to setting <see cref="IsDebugMode"/>
    /// to its opposite value.
    /// </remarks>
    public bool Toggle()
    {
        IsDebugMode = !IsDebugMode;
        ToggleCount++;
        LastToggleTime = DateTime.UtcNow;
        return IsDebugMode;
    }

    /// <summary>
    /// Enables debug mode.
    /// </summary>
    /// <remarks>
    /// Equivalent to setting <see cref="IsDebugMode"/> to true.
    /// </remarks>
    public void Enable()
    {
        if (!IsDebugMode)
        {
            ToggleCount++;
            LastToggleTime = DateTime.UtcNow;
        }

        IsDebugMode = true;
    }

    /// <summary>
    /// Disables debug mode.
    /// </summary>
    /// <remarks>
    /// Equivalent to setting <see cref="IsDebugMode"/> to false.
    /// </remarks>
    public void Disable()
    {
        if (IsDebugMode)
        {
            ToggleCount++;
            LastToggleTime = DateTime.UtcNow;
        }

        IsDebugMode = false;
    }

    /// <summary>
    /// Executes an action only if debug mode is enabled.
    /// </summary>
    /// <param name="action">The action to execute when debug mode is enabled.</param>
    /// <remarks>
    /// <para>
    /// Use this method to conditionally execute expensive diagnostic operations
    /// that should only run in debug mode.
    /// </para>
    /// <para>
    /// The action parameter should be lightweight to create (e.g., a lambda that
    /// captures minimal state) since it will be evaluated on every call.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// controller.WhenDebug(() =>
    /// {
    ///     // This only runs when debug mode is enabled
    ///     ValidateAllEntityReferences();
    ///     LogDetailedSystemMetrics();
    /// });
    /// </code>
    /// </example>
    public void WhenDebug(Action action)
    {
        if (IsDebugMode)
        {
            action();
        }
    }

    /// <summary>
    /// Returns a value depending on whether debug mode is enabled.
    /// </summary>
    /// <typeparam name="T">The type of value to return.</typeparam>
    /// <param name="debugValue">The value to return when debug mode is enabled.</param>
    /// <param name="releaseValue">The value to return when debug mode is disabled.</param>
    /// <returns>The appropriate value based on debug mode state.</returns>
    /// <remarks>
    /// Use this method to select between debug and release configurations
    /// without conditional statements in calling code.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use more detailed timing in debug mode
    /// var resolution = controller.Select(
    ///     debugValue: TimeSpan.FromMicroseconds(1),
    ///     releaseValue: TimeSpan.FromMilliseconds(1));
    /// </code>
    /// </example>
    public T Select<T>(T debugValue, T releaseValue)
    {
        return IsDebugMode ? debugValue : releaseValue;
    }
}
