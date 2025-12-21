namespace KeenEyes.Tests;

/// <summary>
/// Tests for ComponentValidationException class to ensure proper exception handling and properties.
/// </summary>
public class ComponentValidationExceptionTests
{
    private struct TestComponent : IComponent
    {
#pragma warning disable CS0649 // Field is never assigned - intentional for test component
        public int Value;
#pragma warning restore CS0649
    }

    [Fact]
    public void Constructor_WithMessageAndType_SetsProperties()
    {
        var exception = new ComponentValidationException("Test message", typeof(TestComponent));

        Assert.Equal("Test message", exception.Message);
        Assert.Equal(typeof(TestComponent), exception.ComponentType);
        Assert.Null(exception.Entity);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageTypeAndEntity_SetsAllProperties()
    {
        var entity = new Entity(42, 1);
        var exception = new ComponentValidationException("Test message", typeof(TestComponent), entity);

        Assert.Equal("Test message", exception.Message);
        Assert.Equal(typeof(TestComponent), exception.ComponentType);
        Assert.Equal(entity, exception.Entity);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageTypeAndInnerException_SetsProperties()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new ComponentValidationException("Test message", typeof(TestComponent), innerException);

        Assert.Equal("Test message", exception.Message);
        Assert.Equal(typeof(TestComponent), exception.ComponentType);
        Assert.Null(exception.Entity);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithNullType_DoesNotThrow()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        var exception = new ComponentValidationException("Test message", null!);
#pragma warning restore CS8625

        Assert.Equal("Test message", exception.Message);
        Assert.Null(exception.ComponentType);
    }

    [Fact]
    public void Exception_IsInvalidOperationException()
    {
        var exception = new ComponentValidationException("Test message", typeof(TestComponent));

        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }

    [Fact]
    public void Exception_CanBeCaught()
    {
        var ex = Assert.Throws<ComponentValidationException>(ThrowValidationException);
        Assert.Equal(typeof(TestComponent), ex.ComponentType);
    }

    [Fact]
    public void Exception_CanBeCaughtAsInvalidOperationException()
    {
        try
        {
            ThrowValidationException();
            Assert.Fail("Expected exception was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            Assert.IsType<ComponentValidationException>(ex);
        }
    }

    private static void ThrowValidationException()
    {
        throw new ComponentValidationException("Test message", typeof(TestComponent));
    }

    [Fact]
    public void Exception_WithEntity_PreservesEntityInfo()
    {
        var entity = new Entity(123, 5);

        try
        {
            throw new ComponentValidationException("Validation failed", typeof(TestComponent), entity);
        }
        catch (ComponentValidationException ex)
        {
            Assert.Equal(entity.Id, ex.Entity!.Value.Id);
            Assert.Equal(entity.Version, ex.Entity!.Value.Version);
        }
    }

    [Fact]
    public void Exception_WithInnerException_PreservesStackTrace()
    {
        var innerException = new InvalidOperationException("Inner error");

        try
        {
            throw new ComponentValidationException("Outer error", typeof(TestComponent), innerException);
        }
        catch (ComponentValidationException ex)
        {
            Assert.NotNull(ex.InnerException);
            Assert.Equal("Inner error", ex.InnerException.Message);
        }
    }

    [Fact]
    public void Exception_SupportsEmptyMessage()
    {
        var exception = new ComponentValidationException(string.Empty, typeof(TestComponent));

        Assert.Empty(exception.Message);
        Assert.Equal(typeof(TestComponent), exception.ComponentType);
    }

    [Fact]
    public void Exception_SupportsLongMessage()
    {
        var longMessage = new string('A', 10000);
        var exception = new ComponentValidationException(longMessage, typeof(TestComponent));

        Assert.Equal(longMessage, exception.Message);
        Assert.Equal(typeof(TestComponent), exception.ComponentType);
    }
}
