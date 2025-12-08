namespace KeenEyes.Testing;

/// <summary>
/// Exception thrown when a test assertion fails.
/// </summary>
/// <remarks>
/// <para>
/// AssertionException provides clear, descriptive error messages when
/// test assertions fail. It includes the specific condition that failed
/// and any additional context provided by the test.
/// </para>
/// </remarks>
public sealed class AssertionException : Exception
{
    /// <summary>
    /// Creates a new assertion exception with the specified message.
    /// </summary>
    /// <param name="message">The error message describing the assertion failure.</param>
    public AssertionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new assertion exception with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing the assertion failure.</param>
    /// <param name="innerException">The exception that caused this assertion to fail.</param>
    public AssertionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
