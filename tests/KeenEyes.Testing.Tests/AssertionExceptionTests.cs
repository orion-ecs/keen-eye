namespace KeenEyes.Testing.Tests;

public class AssertionExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new AssertionException("test message");

        ex.Message.ShouldBe("test message");
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = new AssertionException("outer message", inner);

        ex.Message.ShouldBe("outer message");
        ex.InnerException.ShouldBe(inner);
    }
}
