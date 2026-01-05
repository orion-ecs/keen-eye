namespace KeenEyes.Localization.Tests;

public class IcuFormatterTests
{
    private readonly IcuFormatter formatter = IcuFormatter.Instance;

    #region Pluralization

    [Theory]
    [InlineData(0, "No items")]
    [InlineData(1, "One item")]
    [InlineData(2, "2 items")]
    [InlineData(5, "5 items")]
    [InlineData(100, "100 items")]
    public void Format_PluralPattern_ReturnsCorrectForm(int count, string expected)
    {
        var pattern = "{count, plural, =0 {No items} =1 {One item} other {# items}}";

        var result = formatter.Format(pattern, new { count }, Locale.EnglishUS);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, "Less than a minute remaining")]
    [InlineData(1, "1 minute remaining")]
    [InlineData(5, "5 minutes remaining")]
    public void Format_PluralWithContext_ReturnsCorrectForm(int minutes, string expected)
    {
        var pattern = "{minutes, plural, =0 {Less than a minute remaining} =1 {1 minute remaining} other {# minutes remaining}}";

        var result = formatter.Format(pattern, new { minutes }, Locale.EnglishUS);

        result.ShouldBe(expected);
    }

    #endregion

    #region Gender/Select

    [Theory]
    [InlineData("male", "He found treasure!")]
    [InlineData("female", "She found treasure!")]
    [InlineData("other", "They found treasure!")]
    [InlineData("unknown", "They found treasure!")]
    public void Format_GenderSelect_ReturnsCorrectForm(string gender, string expected)
    {
        var pattern = "{gender, select, male {He found treasure!} female {She found treasure!} other {They found treasure!}}";

        var result = formatter.Format(pattern, new { gender }, Locale.EnglishUS);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("admin", "Administrator")]
    [InlineData("user", "User")]
    [InlineData("guest", "Guest")]
    [InlineData("anything", "Guest")]
    public void Format_SelectExpression_ReturnsCorrectForm(string role, string expected)
    {
        var pattern = "{role, select, admin {Administrator} user {User} other {Guest}}";

        var result = formatter.Format(pattern, new { role }, Locale.EnglishUS);

        result.ShouldBe(expected);
    }

    #endregion

    #region Combined Patterns

    [Fact]
    public void Format_CombinedGenderAndPlural_ReturnsCorrectForm()
    {
        var pattern = "{gender, select, male {He has {count, plural, =1 {# coin} other {# coins}}} female {She has {count, plural, =1 {# coin} other {# coins}}} other {They have {count, plural, =1 {# coin} other {# coins}}}}";

        var result = formatter.Format(pattern, new { gender = "male", count = 5 }, Locale.EnglishUS);

        result.ShouldBe("He has 5 coins");
    }

    [Fact]
    public void Format_NestedPlurals_ReturnsCorrectForm()
    {
        var pattern = "{gender, select, male {He has {count, plural, =1 {# coin} other {# coins}}} female {She has {count, plural, =1 {# coin} other {# coins}}} other {They have {count, plural, =1 {# coin} other {# coins}}}}";

        var result = formatter.Format(pattern, new { gender = "female", count = 1 }, Locale.EnglishUS);

        result.ShouldBe("She has 1 coin");
    }

    #endregion

    #region Simple Substitution

    [Fact]
    public void Format_SimpleNamedPlaceholder_SubstitutesValue()
    {
        var result = formatter.Format("Hello, {name}!", new { name = "World" }, Locale.EnglishUS);

        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void Format_MultipleNamedPlaceholders_SubstitutesAllValues()
    {
        var result = formatter.Format(
            "{player} scored {score} points!",
            new { player = "Alice", score = 1500 },
            Locale.EnglishUS);

        result.ShouldBe("Alice scored 1500 points!");
    }

    #endregion

    #region Dictionary Arguments

    [Fact]
    public void Format_WithDictionary_SubstitutesValues()
    {
        var pattern = "{count, plural, =0 {No items} =1 {One item} other {# items}}";
        var args = new Dictionary<string, object?> { ["count"] = 5 };

        var result = formatter.Format(pattern, args, Locale.EnglishUS);

        result.ShouldBe("5 items");
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
    public void Format_InvalidPattern_ReturnsFallback()
    {
        // Invalid ICU pattern - unclosed brace
        var result = formatter.Format("{count, plural, =0 {No items}", new { count = 5 }, Locale.EnglishUS);

        // Should return original template on failure
        result.ShouldBe("{count, plural, =0 {No items}");
    }

    #endregion

    #region TryFormat

    [Fact]
    public void TryFormat_ValidPattern_ReturnsTrue()
    {
        var pattern = "{count, plural, =0 {No items} =1 {One item} other {# items}}";

        var success = formatter.TryFormat(pattern, new { count = 5 }, Locale.EnglishUS, out var result);

        success.ShouldBeTrue();
        result.ShouldBe("5 items");
    }

    [Fact]
    public void TryFormat_InvalidPattern_ReturnsFalse()
    {
        var success = formatter.TryFormat("{count, plural, =0 {No items}", new { count = 5 }, Locale.EnglishUS, out var result);

        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    #endregion

    #region Singleton Instance

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = IcuFormatter.Instance;
        var instance2 = IcuFormatter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    #endregion
}
