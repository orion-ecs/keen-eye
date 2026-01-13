namespace KeenEyes.TestBridge.Mutation;

/// <summary>
/// Result of an entity mutation operation.
/// </summary>
/// <remarks>
/// <para>
/// This record encapsulates the result of entity operations such as spawn, clone, or other
/// mutations that may create or modify entities. It includes success status, entity identifiers
/// on success, and error information on failure.
/// </para>
/// </remarks>
public sealed record EntityResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID if the operation succeeded, or null on failure.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the entity version if the operation succeeded, or null on failure.
    /// </summary>
    public int? EntityVersion { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed, or null on success.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful result for an entity operation.
    /// </summary>
    /// <param name="entityId">The ID of the created or modified entity.</param>
    /// <param name="version">The version of the entity.</param>
    /// <returns>A successful entity result.</returns>
    public static EntityResult Ok(int entityId, int version) => new()
    {
        Success = true,
        EntityId = entityId,
        EntityVersion = version
    };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message describing what went wrong.</param>
    /// <returns>A failed entity result.</returns>
    public static EntityResult Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}
