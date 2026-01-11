using KeenEyes.Shaders.Compiler.Diagnostics;

namespace KeenEyes.Shaders.Compiler.Tests.Diagnostics;

/// <summary>
/// Tests for the SuggestionEngine class.
/// </summary>
public class SuggestionEngineTests
{
    #region LevenshteinDistance Tests

    [Fact]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        var distance = SuggestionEngine.LevenshteinDistance("hello", "hello");

        Assert.Equal(0, distance);
    }

    [Fact]
    public void LevenshteinDistance_EmptyStrings_ReturnsZero()
    {
        var distance = SuggestionEngine.LevenshteinDistance("", "");

        Assert.Equal(0, distance);
    }

    [Fact]
    public void LevenshteinDistance_OneEmptyString_ReturnsOtherLength()
    {
        var distance1 = SuggestionEngine.LevenshteinDistance("", "hello");
        var distance2 = SuggestionEngine.LevenshteinDistance("hello", "");

        Assert.Equal(5, distance1);
        Assert.Equal(5, distance2);
    }

    [Fact]
    public void LevenshteinDistance_OneCharDifference_ReturnsOne()
    {
        // Substitution
        var distance = SuggestionEngine.LevenshteinDistance("Position", "Positon");

        Assert.Equal(1, distance);
    }

    [Fact]
    public void LevenshteinDistance_Insertion_ReturnsOne()
    {
        var distance = SuggestionEngine.LevenshteinDistance("Position", "Positionn");

        Assert.Equal(1, distance);
    }

    [Fact]
    public void LevenshteinDistance_Deletion_ReturnsOne()
    {
        var distance = SuggestionEngine.LevenshteinDistance("Position", "Positin");

        Assert.Equal(1, distance);
    }

    [Fact]
    public void LevenshteinDistance_MultipleEdits_ReturnsCorrectCount()
    {
        // "kitten" -> "sitting" requires 3 edits
        var distance = SuggestionEngine.LevenshteinDistance("kitten", "sitting");

        Assert.Equal(3, distance);
    }

    [Fact]
    public void LevenshteinDistance_CaseMatters()
    {
        var distance = SuggestionEngine.LevenshteinDistance("Position", "position");

        Assert.Equal(1, distance); // One character different (P vs p)
    }

    #endregion

    #region GetSuggestions Tests

    [Fact]
    public void GetSuggestions_ExactMatch_ReturnsMatch()
    {
        var candidates = new[] { "Position", "Velocity", "Rotation" };
        var suggestions = SuggestionEngine.GetSuggestions("Position", candidates);

        Assert.Contains("Position", suggestions);
    }

    [Fact]
    public void GetSuggestions_TypoWithinDistance_ReturnsSuggestion()
    {
        var candidates = new[] { "Position", "Velocity", "Rotation" };
        var suggestions = SuggestionEngine.GetSuggestions("Positon", candidates);

        Assert.Contains("Position", suggestions);
    }

    [Fact]
    public void GetSuggestions_TypoTooFar_ReturnsEmpty()
    {
        var candidates = new[] { "Position", "Velocity", "Rotation" };
        var suggestions = SuggestionEngine.GetSuggestions("XYZ", candidates, maxDistance: 2);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_CaseInsensitive()
    {
        var candidates = new[] { "Position", "Velocity", "Rotation" };
        var suggestions = SuggestionEngine.GetSuggestions("positon", candidates);

        Assert.Contains("Position", suggestions);
    }

    [Fact]
    public void GetSuggestions_SortedByDistance()
    {
        var candidates = new[] { "Position", "Positionn", "Pos" };
        var suggestions = SuggestionEngine.GetSuggestions("Positon", candidates);

        // "Position" (distance 1) should come before others
        Assert.True(suggestions.Count > 0);
        Assert.Equal("Position", suggestions[0]);
    }

    [Fact]
    public void GetSuggestions_RespectsMaxSuggestions()
    {
        var candidates = new[] { "Positon", "Positin", "Positin", "Positn" };
        var suggestions = SuggestionEngine.GetSuggestions(
            "Position",
            candidates,
            maxDistance: 3,
            maxSuggestions: 2);

        Assert.True(suggestions.Count <= 2);
    }

    [Fact]
    public void GetSuggestions_EmptyInput_ReturnsEmpty()
    {
        var candidates = new[] { "Position", "Velocity" };
        var suggestions = SuggestionEngine.GetSuggestions("", candidates);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_EmptyCandidates_ReturnsEmpty()
    {
        var suggestions = SuggestionEngine.GetSuggestions("Position", Array.Empty<string>());

        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_PrefixMatch_ReturnsSuggestion()
    {
        var candidates = new[] { "PositionComponent", "VelocityComponent" };
        var suggestions = SuggestionEngine.GetSuggestions("Position", candidates);

        // Should find PositionComponent as a prefix match
        Assert.Contains("PositionComponent", suggestions);
    }

    #endregion

    #region GetKeywordSuggestions Tests

    [Fact]
    public void GetKeywordSuggestions_Typo_ReturnsSuggestion()
    {
        var suggestions = SuggestionEngine.GetKeywordSuggestions("compnent");

        Assert.Contains("component", suggestions);
    }

    [Fact]
    public void GetKeywordSuggestions_TypoInCompute_ReturnsSuggestion()
    {
        var suggestions = SuggestionEngine.GetKeywordSuggestions("compue");

        Assert.Contains("compute", suggestions);
    }

    [Fact]
    public void GetKeywordSuggestions_TypoInFloat_ReturnsSuggestion()
    {
        var suggestions = SuggestionEngine.GetKeywordSuggestions("flot");

        Assert.Contains("float", suggestions);
    }

    [Fact]
    public void GetKeywordSuggestions_UnrecognizedWord_ReturnsEmpty()
    {
        var suggestions = SuggestionEngine.GetKeywordSuggestions("xyzabc");

        Assert.Empty(suggestions);
    }

    #endregion

    #region GetFunctionSuggestions Tests

    [Fact]
    public void GetFunctionSuggestions_Typo_ReturnsSuggestion()
    {
        var suggestions = SuggestionEngine.GetFunctionSuggestions("normilize");

        Assert.Contains("normalize", suggestions);
    }

    [Fact]
    public void GetFunctionSuggestions_TypoInLength_ReturnsSuggestion()
    {
        var suggestions = SuggestionEngine.GetFunctionSuggestions("lenght");

        Assert.Contains("length", suggestions);
    }

    [Fact]
    public void GetFunctionSuggestions_TypoInClamp_ReturnsSuggestion()
    {
        var suggestions = SuggestionEngine.GetFunctionSuggestions("clam");

        Assert.Contains("clamp", suggestions);
    }

    #endregion

    #region GetComponentSuggestions Tests

    [Fact]
    public void GetComponentSuggestions_AllowsHigherDistance()
    {
        var candidates = new[] { "PositionComponent", "VelocityComponent", "RotationComponent" };

        // "PosiComponent" has distance 4 from "PositionComponent", but should still suggest
        // because GetComponentSuggestions uses maxDistance: 3
        var suggestions = SuggestionEngine.GetComponentSuggestions("PositonComponent", candidates);

        Assert.Contains("PositionComponent", suggestions);
    }

    #endregion
}
