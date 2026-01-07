namespace KeenEyes.TestBridge.Commands;

/// <summary>
/// Base interface for all test bridge commands.
/// </summary>
/// <remarks>
/// Commands are the low-level API for the test bridge. They can be serialized
/// for IPC communication and provide a uniform execution model.
/// </remarks>
public interface ITestCommand
{
    /// <summary>
    /// Gets the unique command identifier for serialization.
    /// </summary>
    /// <remarks>
    /// Command types follow a dotted notation (e.g., "input.keyDown", "capture.screenshot").
    /// </remarks>
    string CommandType { get; }
}

/// <summary>
/// Result from executing a command.
/// </summary>
/// <remarks>
/// All command executions return a result, even void operations (which return null data on success).
/// </remarks>
public readonly record struct CommandResult
{
    /// <summary>
    /// Gets whether the command executed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the command failed.
    /// </summary>
    public required string? Error { get; init; }

    /// <summary>
    /// Gets the result data if the command succeeded.
    /// </summary>
    /// <remarks>
    /// The type of data depends on the command. Commands that don't return data
    /// will have null here even on success.
    /// </remarks>
    public required object? Data { get; init; }

    /// <summary>
    /// Creates a successful result with optional data.
    /// </summary>
    /// <param name="data">The result data, if any.</param>
    /// <returns>A successful command result.</returns>
    public static CommandResult Ok(object? data = null) => new()
    {
        Success = true,
        Error = null,
        Data = data
    };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed command result.</returns>
    public static CommandResult Fail(string error) => new()
    {
        Success = false,
        Error = error,
        Data = null
    };

    /// <summary>
    /// Gets the data as a specific type, throwing if the command failed or data is wrong type.
    /// </summary>
    /// <typeparam name="T">The expected data type.</typeparam>
    /// <returns>The typed data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the command failed or data is wrong type.</exception>
    public T GetData<T>()
    {
        if (!Success)
        {
            throw new InvalidOperationException($"Command failed: {Error}");
        }

        if (Data is not T typed)
        {
            throw new InvalidOperationException(
                $"Expected data of type {typeof(T).Name} but got {Data?.GetType().Name ?? "null"}");
        }

        return typed;
    }

    /// <summary>
    /// Tries to get the data as a specific type.
    /// </summary>
    /// <typeparam name="T">The expected data type.</typeparam>
    /// <param name="data">When this method returns, contains the data if successful.</param>
    /// <returns>True if the command succeeded and data is of the expected type.</returns>
    public bool TryGetData<T>(out T? data)
    {
        if (Success && Data is T typed)
        {
            data = typed;
            return true;
        }

        data = default;
        return false;
    }
}
