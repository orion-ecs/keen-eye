namespace KeenEyes.AI;

/// <summary>
/// Standard blackboard keys for common AI data.
/// </summary>
/// <remarks>
/// Using consistent keys ensures interoperability between different AI actions and conditions.
/// </remarks>
public static class BBKeys
{
    #region Time

    /// <summary>
    /// Total elapsed time in seconds (float).
    /// </summary>
    public const string Time = "Time";

    /// <summary>
    /// Time since last frame in seconds (float).
    /// </summary>
    public const string DeltaTime = "DeltaTime";

    #endregion

    #region Target

    /// <summary>
    /// The current target entity (Entity).
    /// </summary>
    public const string Target = "Target";

    /// <summary>
    /// The target's position (Vector3).
    /// </summary>
    public const string TargetPosition = "TargetPosition";

    /// <summary>
    /// The last known position of the target (Vector3).
    /// </summary>
    public const string TargetLastSeen = "TargetLastSeen";

    /// <summary>
    /// Time since the target was last seen in seconds (float).
    /// </summary>
    public const string TimeSinceTargetSeen = "TimeSinceTargetSeen";

    #endregion

    #region Self

    /// <summary>
    /// Current health value (float or int).
    /// </summary>
    public const string Health = "Health";

    /// <summary>
    /// Maximum health value (float or int).
    /// </summary>
    public const string MaxHealth = "MaxHealth";

    /// <summary>
    /// Current ammo count (int).
    /// </summary>
    public const string Ammo = "Ammo";

    /// <summary>
    /// Current alert level (float 0-1).
    /// </summary>
    public const string AlertLevel = "AlertLevel";

    #endregion

    #region Navigation

    /// <summary>
    /// The navigation destination position (Vector3).
    /// </summary>
    public const string Destination = "Destination";

    /// <summary>
    /// The current navigation path (NavPath).
    /// </summary>
    public const string CurrentPath = "CurrentPath";

    /// <summary>
    /// Current waypoint index in a patrol route (int).
    /// </summary>
    public const string PatrolIndex = "PatrolIndex";

    /// <summary>
    /// Array of patrol waypoints (Vector3[]).
    /// </summary>
    public const string PatrolWaypoints = "PatrolWaypoints";

    /// <summary>
    /// Minimum flee distance from threat (float).
    /// </summary>
    public const string FleeDistance = "FleeDistance";

    /// <summary>
    /// The entity or position to flee from (Entity or Vector3).
    /// </summary>
    public const string ThreatSource = "ThreatSource";

    /// <summary>
    /// The threat position (Vector3).
    /// </summary>
    public const string ThreatPosition = "ThreatPosition";

    #endregion

    #region Chase

    /// <summary>
    /// Time interval between path updates during chase (float).
    /// </summary>
    public const string ChaseUpdateInterval = "ChaseUpdateInterval";

    /// <summary>
    /// Time since last path update during chase (float).
    /// </summary>
    public const string ChaseTimeSinceUpdate = "ChaseTimeSinceUpdate";

    #endregion
}
