namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for the <see cref="ReplayDesyncException"/> class.
/// </summary>
public class ReplayDesyncExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesExceptionWithDefaultMessage()
    {
        // Arrange & Act
        var exception = new ReplayDesyncException();

        // Assert
        Assert.Equal("Replay desync detected.", exception.Message);
        Assert.Equal(0, exception.Frame);
        Assert.Equal(0u, exception.ExpectedChecksum);
        Assert.Equal(0u, exception.ActualChecksum);
    }

    [Fact]
    public void Constructor_WithMessage_CreatesExceptionWithMessage()
    {
        // Arrange
        var message = "Custom desync message";

        // Act
        var exception = new ReplayDesyncException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_CreatesExceptionWithBoth()
    {
        // Arrange
        var message = "Outer message";
        var innerException = new InvalidOperationException("Inner message");

        // Act
        var exception = new ReplayDesyncException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithFrameAndChecksums_StoresAllProperties()
    {
        // Arrange
        var frame = 42;
        var expectedChecksum = 0xDEADBEEFu;
        var actualChecksum = 0xCAFEBABEu;

        // Act
        var exception = new ReplayDesyncException(frame, expectedChecksum, actualChecksum);

        // Assert
        Assert.Equal(frame, exception.Frame);
        Assert.Equal(expectedChecksum, exception.ExpectedChecksum);
        Assert.Equal(actualChecksum, exception.ActualChecksum);
    }

    [Fact]
    public void Constructor_WithFrameAndChecksums_FormatsMessageCorrectly()
    {
        // Arrange
        var frame = 100;
        var expectedChecksum = 0x12345678u;
        var actualChecksum = 0x87654321u;

        // Act
        var exception = new ReplayDesyncException(frame, expectedChecksum, actualChecksum);

        // Assert
        Assert.Contains("frame 100", exception.Message);
        Assert.Contains("12345678", exception.Message.ToUpperInvariant());
        Assert.Contains("87654321", exception.Message.ToUpperInvariant());
    }

    [Fact]
    public void Constructor_WithCustomMessageAndChecksums_UsesCustomMessage()
    {
        // Arrange
        var message = "Custom error message";
        var frame = 50;
        var expectedChecksum = 0xAAAAAAAAu;
        var actualChecksum = 0xBBBBBBBBu;

        // Act
        var exception = new ReplayDesyncException(message, frame, expectedChecksum, actualChecksum);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(frame, exception.Frame);
        Assert.Equal(expectedChecksum, exception.ExpectedChecksum);
        Assert.Equal(actualChecksum, exception.ActualChecksum);
    }

    [Fact]
    public void IsReplayException_InheritsFromReplayException()
    {
        // Arrange & Act
        var exception = new ReplayDesyncException();

        // Assert
        Assert.IsAssignableFrom<ReplayException>(exception);
    }

    [Fact]
    public void Frame_WithZeroFrame_ReturnsZero()
    {
        // Arrange & Act
        var exception = new ReplayDesyncException(0, 0x1234u, 0x5678u);

        // Assert
        Assert.Equal(0, exception.Frame);
    }

    [Fact]
    public void Frame_WithNegativeFrame_StoresNegativeValue()
    {
        // Note: Negative frames shouldn't normally occur, but the type allows it
        // Arrange & Act
        var exception = new ReplayDesyncException(-1, 0x1234u, 0x5678u);

        // Assert
        Assert.Equal(-1, exception.Frame);
    }

    [Fact]
    public void Checksums_WithMatchingValues_StoresSameValue()
    {
        // Arrange
        var checksum = 0xABCDEF12u;

        // Act
        var exception = new ReplayDesyncException(10, checksum, checksum);

        // Assert
        Assert.Equal(checksum, exception.ExpectedChecksum);
        Assert.Equal(checksum, exception.ActualChecksum);
    }
}
