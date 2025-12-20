using KeenEyes.Logging;
using KeenEyes.Testing.Logging;

namespace KeenEyes.Testing.Tests.Logging;

public class LogAssertionsTests
{
    #region ShouldHaveLogged

    [Fact]
    public void ShouldHaveLogged_WithMatchingLevel_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Error, "Cat", "Error", null);

        Should.NotThrow(() => provider.ShouldHaveLogged(LogLevel.Error));
    }

    [Fact]
    public void ShouldHaveLogged_WithNoMatchingLevel_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLogged(LogLevel.Error));
    }

    #endregion

    #region ShouldHaveLoggedError

    [Fact]
    public void ShouldHaveLoggedError_WithError_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Error, "Cat", "Error", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedError());
    }

    [Fact]
    public void ShouldHaveLoggedError_WithNoError_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedError());
    }

    #endregion

    #region ShouldHaveLoggedWarning

    [Fact]
    public void ShouldHaveLoggedWarning_WithWarning_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Warning, "Cat", "Warning", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedWarning());
    }

    [Fact]
    public void ShouldHaveLoggedWarning_WithNoWarning_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedWarning());
    }

    #endregion

    #region ShouldHaveLoggedContaining

    [Fact]
    public void ShouldHaveLoggedContaining_WithMatchingText_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "System initialized successfully", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedContaining("initialized"));
    }

    [Fact]
    public void ShouldHaveLoggedContaining_CaseInsensitive_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "SYSTEM INITIALIZED", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedContaining("system"));
    }

    [Fact]
    public void ShouldHaveLoggedContaining_WithNoMatch_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Hello world", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedContaining("goodbye"));
    }

    #endregion

    #region ShouldHaveLoggedMatching

    [Fact]
    public void ShouldHaveLoggedMatching_WithMatch_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Error, "Database", "Connection failed", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedMatching(
            e => e.Level == LogLevel.Error && e.Category == "Database"));
    }

    [Fact]
    public void ShouldHaveLoggedMatching_WithNoMatch_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "App", "Started", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedMatching(
            e => e.Level == LogLevel.Fatal));
    }

    #endregion

    #region ShouldNotHaveLoggedErrors

    [Fact]
    public void ShouldNotHaveLoggedErrors_WithNoErrors_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);
        provider.Log(LogLevel.Warning, "Cat", "Warning", null);

        Should.NotThrow(() => provider.ShouldNotHaveLoggedErrors());
    }

    [Fact]
    public void ShouldNotHaveLoggedErrors_WithErrors_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Error, "Cat", "Error occurred", null);

        Should.Throw<AssertionException>(() => provider.ShouldNotHaveLoggedErrors());
    }

    #endregion

    #region ShouldNotHaveLoggedWarnings

    [Fact]
    public void ShouldNotHaveLoggedWarnings_WithNoWarnings_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        Should.NotThrow(() => provider.ShouldNotHaveLoggedWarnings());
    }

    [Fact]
    public void ShouldNotHaveLoggedWarnings_WithWarnings_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Warning, "Cat", "Warning occurred", null);

        Should.Throw<AssertionException>(() => provider.ShouldNotHaveLoggedWarnings());
    }

    #endregion

    #region ShouldNotHaveLoggedFatals

    [Fact]
    public void ShouldNotHaveLoggedFatals_WithNoFatals_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Error, "Cat", "Error", null);

        Should.NotThrow(() => provider.ShouldNotHaveLoggedFatals());
    }

    [Fact]
    public void ShouldNotHaveLoggedFatals_WithFatals_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Fatal, "Cat", "Fatal error", null);

        Should.Throw<AssertionException>(() => provider.ShouldNotHaveLoggedFatals());
    }

    #endregion

    #region ShouldHaveLoggedExactly

    [Fact]
    public void ShouldHaveLoggedExactly_WithCorrectCount_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info1", null);
        provider.Log(LogLevel.Info, "Cat", "Info2", null);
        provider.Log(LogLevel.Warning, "Cat", "Warning", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedExactly(LogLevel.Info, 2));
    }

    [Fact]
    public void ShouldHaveLoggedExactly_WithWrongCount_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedExactly(LogLevel.Info, 2));
    }

    #endregion

    #region ShouldHaveLoggedAtLeast

    [Fact]
    public void ShouldHaveLoggedAtLeast_WithEnoughEntries_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info1", null);
        provider.Log(LogLevel.Info, "Cat", "Info2", null);
        provider.Log(LogLevel.Info, "Cat", "Info3", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedAtLeast(2));
    }

    [Fact]
    public void ShouldHaveLoggedAtLeast_WithFewerEntries_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedAtLeast(5));
    }

    #endregion

    #region ShouldHaveLoggedForCategory

    [Fact]
    public void ShouldHaveLoggedForCategory_WithMatch_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Database", "Connected", null);

        Should.NotThrow(() => provider.ShouldHaveLoggedForCategory("Database"));
    }

    [Fact]
    public void ShouldHaveLoggedForCategory_WithNoMatch_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "App", "Started", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedForCategory("Network"));
    }

    #endregion

    #region ShouldBeEmpty

    [Fact]
    public void ShouldBeEmpty_WhenEmpty_Succeeds()
    {
        using var provider = new MockLogProvider();

        Should.NotThrow(() => provider.ShouldBeEmpty());
    }

    [Fact]
    public void ShouldBeEmpty_WhenNotEmpty_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Message", null);

        Should.Throw<AssertionException>(() => provider.ShouldBeEmpty());
    }

    #endregion

    #region ShouldHaveLoggedWithProperty

    [Fact]
    public void ShouldHaveLoggedWithProperty_WithProperty_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Message", new Dictionary<string, object?> { ["UserId"] = 123 });

        Should.NotThrow(() => provider.ShouldHaveLoggedWithProperty("UserId"));
    }

    [Fact]
    public void ShouldHaveLoggedWithProperty_WithPropertyAndValue_Succeeds()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Message", new Dictionary<string, object?> { ["UserId"] = 123 });

        Should.NotThrow(() => provider.ShouldHaveLoggedWithProperty("UserId", 123));
    }

    [Fact]
    public void ShouldHaveLoggedWithProperty_WithWrongValue_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Message", new Dictionary<string, object?> { ["UserId"] = 123 });

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedWithProperty("UserId", 456));
    }

    [Fact]
    public void ShouldHaveLoggedWithProperty_WithNoProperty_Throws()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Message", null);

        Should.Throw<AssertionException>(() => provider.ShouldHaveLoggedWithProperty("UserId"));
    }

    #endregion

    #region Chaining

    [Fact]
    public void Assertions_CanBeChained()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "App", "System initialized", null);
        provider.Log(LogLevel.Info, "App", "Ready", null);

        Should.NotThrow(() =>
            provider
                .ShouldHaveLogged(LogLevel.Info)
                .ShouldHaveLoggedContaining("initialized")
                .ShouldHaveLoggedForCategory("App")
                .ShouldNotHaveLoggedErrors()
                .ShouldNotHaveLoggedWarnings());
    }

    #endregion
}
