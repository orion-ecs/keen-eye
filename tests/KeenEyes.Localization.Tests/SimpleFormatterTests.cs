namespace KeenEyes.Localization.Tests;

public class SimpleFormatterTests
{
    private readonly SimpleFormatter formatter = SimpleFormatter.Instance;

    #region Positional Placeholders

    [Fact]
    public void Format_PositionalPlaceholder_SubstitutesValue()
    {
        var result = formatter.Format("Hello, {0}!", new object[] { "World" }, Locale.EnglishUS);

        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void Format_MultiplePositionalPlaceholders_SubstitutesAllValues()
    {
        var result = formatter.Format("Score: {0} / {1}", new object[] { 150, 1000 }, Locale.EnglishUS);

        result.ShouldBe("Score: 150 / 1000");
    }

    [Fact]
    public void Format_PositionalWithFormatSpecifier_FormatsCorrectly()
    {
        var result = formatter.Format("Price: {0:C2}", new object[] { 19.99m }, new Locale("en-US"));

        result.ShouldContain("19.99");
    }

    #endregion

    #region Named Placeholders with Dictionary

    [Fact]
    public void Format_NamedPlaceholderWithDictionary_SubstitutesValue()
    {
        var args = new Dictionary<string, object?> { ["name"] = "World" };
        var result = formatter.Format("Hello, {name}!", args, Locale.EnglishUS);

        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void Format_MultipleNamedPlaceholdersWithDictionary_SubstitutesAllValues()
    {
        var args = new Dictionary<string, object?>
        {
            ["player"] = "Alice",
            ["score"] = 1500
        };
        var result = formatter.Format("{player} scored {score} points!", args, Locale.EnglishUS);

        result.ShouldBe("Alice scored 1500 points!");
    }

    [Fact]
    public void Format_MissingNamedPlaceholderInDictionary_KeepsPlaceholder()
    {
        var args = new Dictionary<string, object?> { ["name"] = "World" };
        var result = formatter.Format("Hello, {name}! Your score is {score}.", args, Locale.EnglishUS);

        result.ShouldBe("Hello, World! Your score is {score}.");
    }

    #endregion

    #region Named Placeholders with Anonymous Objects

    [Fact]
    public void Format_NamedPlaceholderWithAnonymousObject_SubstitutesValue()
    {
        var result = formatter.Format("Hello, {name}!", new { name = "World" }, Locale.EnglishUS);

        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void Format_MultipleNamedPlaceholdersWithAnonymousObject_SubstitutesAllValues()
    {
        var result = formatter.Format(
            "{player} scored {score} points!",
            new { player = "Alice", score = 1500 },
            Locale.EnglishUS);

        result.ShouldBe("Alice scored 1500 points!");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Format_NullArgs_ReturnsTemplateUnchanged()
    {
        var result = formatter.Format("Hello, World!", null, Locale.EnglishUS);

        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void Format_EmptyTemplate_ReturnsEmpty()
    {
        var result = formatter.Format("", new { name = "World" }, Locale.EnglishUS);

        result.ShouldBe("");
    }

    [Fact]
    public void Format_NullValueInDictionary_ReturnsEmptyString()
    {
        var args = new Dictionary<string, object?> { ["name"] = null };
        var result = formatter.Format("Hello, {name}!", args, Locale.EnglishUS);

        result.ShouldBe("Hello, !");
    }

    [Fact]
    public void Format_InvalidPositionalFormat_ReturnsFallback()
    {
        var success = formatter.TryFormat("Value: {0} {1}", new object[] { 42 }, Locale.EnglishUS, out var result);

        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    #endregion

    #region TryFormat

    [Fact]
    public void TryFormat_ValidTemplate_ReturnsTrue()
    {
        var success = formatter.TryFormat("Hello, {name}!", new { name = "World" }, Locale.EnglishUS, out var result);

        success.ShouldBeTrue();
        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void TryFormat_InvalidPositionalTemplate_ReturnsFalse()
    {
        var success = formatter.TryFormat("{0} {1} {2}", new object[] { 1, 2 }, Locale.EnglishUS, out var result);

        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    #endregion

    #region Locale Formatting

    [Fact]
    public void Format_FormattableValue_UsesLocale()
    {
        var args = new Dictionary<string, object?> { ["value"] = 1234.56 };

        var usResult = formatter.Format("Value: {value}", args, new Locale("en-US"));
        var deResult = formatter.Format("Value: {value}", args, new Locale("de-DE"));

        // Different locales may format numbers differently
        usResult.ShouldNotBeEmpty();
        deResult.ShouldNotBeEmpty();
    }

    #endregion

    #region Singleton Instance

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = SimpleFormatter.Instance;
        var instance2 = SimpleFormatter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    #endregion
}
