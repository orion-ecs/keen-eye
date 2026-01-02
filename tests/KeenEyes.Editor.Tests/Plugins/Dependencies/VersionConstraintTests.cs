// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Dependencies;

namespace KeenEyes.Editor.Tests.Plugins.Dependencies;

/// <summary>
/// Tests for <see cref="VersionConstraint"/>.
/// </summary>
public sealed class VersionConstraintTests
{
    #region Parse Tests

    [Fact]
    public void Parse_WithExactVersion_Succeeds()
    {
        // Arrange & Act
        var constraint = VersionConstraint.Parse("1.0.0");

        // Assert
        Assert.False(constraint.IsEmpty);
    }

    [Fact]
    public void Parse_WithGreaterThanOrEqual_Succeeds()
    {
        // Arrange & Act
        var constraint = VersionConstraint.Parse(">=1.0.0");

        // Assert
        Assert.False(constraint.IsEmpty);
    }

    [Fact]
    public void Parse_WithCaretConstraint_Succeeds()
    {
        // Arrange & Act
        var constraint = VersionConstraint.Parse("^1.0.0");

        // Assert
        Assert.False(constraint.IsEmpty);
    }

    [Fact]
    public void Parse_WithTildeConstraint_Succeeds()
    {
        // Arrange & Act
        var constraint = VersionConstraint.Parse("~1.2.0");

        // Assert
        Assert.False(constraint.IsEmpty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WithEmptyString_ThrowsArgumentException(string constraint)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => VersionConstraint.Parse(constraint));
    }

    [Fact]
    public void Parse_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => VersionConstraint.Parse(null!));
    }

    #endregion

    #region TryParse Tests

    [Fact]
    public void TryParse_WithValidConstraint_ReturnsTrue()
    {
        // Arrange & Act
        var result = VersionConstraint.TryParse(">=1.0.0", out var constraint);

        // Assert
        Assert.True(result);
        Assert.False(constraint.IsEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_WithInvalidConstraint_ReturnsFalse(string? input)
    {
        // Arrange & Act
        var result = VersionConstraint.TryParse(input, out var constraint);

        // Assert
        Assert.False(result);
        Assert.True(constraint.IsEmpty);
    }

    #endregion

    #region IsSatisfiedBy Tests - Greater Than Or Equal

    [Theory]
    [InlineData(">=1.0.0", "1.0.0", true)]
    [InlineData(">=1.0.0", "1.0.1", true)]
    [InlineData(">=1.0.0", "2.0.0", true)]
    [InlineData(">=1.0.0", "0.9.0", false)]
    [InlineData(">=1.0.0", "0.9.9", false)]
    public void IsSatisfiedBy_GreaterThanOrEqual_ReturnsExpected(
        string constraintStr,
        string version,
        bool expected)
    {
        // Arrange
        var constraint = VersionConstraint.Parse(constraintStr);

        // Act
        var result = constraint.IsSatisfiedBy(version);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsSatisfiedBy Tests - Caret (Compatible With)

    [Theory]
    [InlineData("^1.0.0", "1.0.0", true)]
    [InlineData("^1.0.0", "1.5.0", true)]
    [InlineData("^1.0.0", "1.9.9", true)]
    [InlineData("^1.0.0", "2.0.0", false)]
    [InlineData("^1.0.0", "0.9.0", false)]
    [InlineData("^0.1.0", "0.1.0", true)]
    [InlineData("^0.1.0", "0.1.5", true)]
    [InlineData("^0.1.0", "0.2.0", false)]
    [InlineData("^0.0.1", "0.0.1", true)]
    [InlineData("^0.0.1", "0.0.2", false)]
    public void IsSatisfiedBy_CaretConstraint_ReturnsExpected(
        string constraintStr,
        string version,
        bool expected)
    {
        // Arrange
        var constraint = VersionConstraint.Parse(constraintStr);

        // Act
        var result = constraint.IsSatisfiedBy(version);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsSatisfiedBy Tests - Tilde (Approximately)

    [Theory]
    [InlineData("~1.2.0", "1.2.0", true)]
    [InlineData("~1.2.0", "1.2.5", true)]
    [InlineData("~1.2.0", "1.2.99", true)]
    [InlineData("~1.2.0", "1.3.0", false)]
    [InlineData("~1.2.0", "1.1.0", false)]
    [InlineData("~1.2.0", "2.0.0", false)]
    public void IsSatisfiedBy_TildeConstraint_ReturnsExpected(
        string constraintStr,
        string version,
        bool expected)
    {
        // Arrange
        var constraint = VersionConstraint.Parse(constraintStr);

        // Act
        var result = constraint.IsSatisfiedBy(version);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsSatisfiedBy Tests - Other Operators

    [Theory]
    [InlineData(">1.0.0", "1.0.1", true)]
    [InlineData(">1.0.0", "1.0.0", false)]
    [InlineData("<2.0.0", "1.9.9", true)]
    [InlineData("<2.0.0", "2.0.0", false)]
    [InlineData("<=2.0.0", "2.0.0", true)]
    [InlineData("<=2.0.0", "2.0.1", false)]
    public void IsSatisfiedBy_OtherOperators_ReturnsExpected(
        string constraintStr,
        string version,
        bool expected)
    {
        // Arrange
        var constraint = VersionConstraint.Parse(constraintStr);

        // Act
        var result = constraint.IsSatisfiedBy(version);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsSatisfiedBy Tests - Invalid Version

    [Fact]
    public void IsSatisfiedBy_WithInvalidVersion_ReturnsFalse()
    {
        // Arrange
        var constraint = VersionConstraint.Parse(">=1.0.0");

        // Act
        var result = constraint.IsSatisfiedBy("not-a-version");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_WithEmptyConstraint_ReturnsFalse()
    {
        // Arrange
        var constraint = default(VersionConstraint);

        // Act
        var result = constraint.IsSatisfiedBy("1.0.0");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameConstraints_ReturnsTrue()
    {
        // Arrange
        var c1 = VersionConstraint.Parse(">=1.0.0");
        var c2 = VersionConstraint.Parse(">=1.0.0");

        // Act & Assert
        Assert.Equal(c1, c2);
        Assert.True(c1 == c2);
    }

    [Fact]
    public void Equals_DifferentConstraints_ReturnsFalse()
    {
        // Arrange
        var c1 = VersionConstraint.Parse(">=1.0.0");
        var c2 = VersionConstraint.Parse(">=2.0.0");

        // Act & Assert
        Assert.NotEqual(c1, c2);
        Assert.True(c1 != c2);
    }

    [Fact]
    public void Equals_EmptyConstraints_ReturnsTrue()
    {
        // Arrange
        var c1 = default(VersionConstraint);
        var c2 = default(VersionConstraint);

        // Act & Assert
        Assert.Equal(c1, c2);
    }

    [Fact]
    public void GetHashCode_SameConstraints_ReturnsSameValue()
    {
        // Arrange
        var c1 = VersionConstraint.Parse(">=1.0.0");
        var c2 = VersionConstraint.Parse(">=1.0.0");

        // Act & Assert
        Assert.Equal(c1.GetHashCode(), c2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_EmptyConstraint_ReturnsEmptyString()
    {
        // Arrange
        var constraint = default(VersionConstraint);

        // Act
        var result = constraint.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion
}
