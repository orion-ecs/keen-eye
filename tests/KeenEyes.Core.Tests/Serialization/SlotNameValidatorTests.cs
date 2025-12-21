using KeenEyes.Serialization;

namespace KeenEyes.Tests.Serialization;

/// <summary>
/// Tests for the SlotNameValidator class which validates save slot names to prevent path traversal attacks.
/// </summary>
public class SlotNameValidatorTests
{
    #region Validate Tests - Valid Names

    [Fact]
    public void Validate_WithSimpleName_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("savegame"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithAlphanumericName_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("save123"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithUnderscores_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("my_save_game"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithHyphens_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("my-save-game"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithSpaces_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("my save game"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithDots_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("save.1"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithParentheses_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("save(1)"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithBrackets_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("save[1]"));
        Assert.Null(exception);
    }

    #endregion

    #region Validate Tests - Invalid Names

    [Fact]
    public void Validate_WithNull_ThrowsArgumentException()
    {
        // ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        Assert.Throws<ArgumentNullException>(() => SlotNameValidator.Validate(null!));
    }

    [Fact]
    public void Validate_WithEmptyString_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SlotNameValidator.Validate(""));
        Assert.NotNull(ex);
    }

    [Fact]
    public void Validate_WithWhitespaceOnly_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SlotNameValidator.Validate("   "));
        Assert.NotNull(ex);
    }

    [Fact]
    public void Validate_WithDirectorySeparator_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate($"save{Path.DirectorySeparatorChar}game"));

        Assert.Contains("directory separator", ex.Message);
    }

    [Fact]
    public void Validate_WithAltDirectorySeparator_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate($"save{Path.AltDirectorySeparatorChar}game"));

        Assert.Contains("alternate directory separator", ex.Message);
    }

    [Fact]
    public void Validate_WithForwardSlash_ThrowsArgumentException()
    {
        // On Windows, this is the alt separator; on Unix, it's the main separator
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save/game"));

        Assert.Contains("separator", ex.Message);
    }

    [Fact]
    public void Validate_WithBackslash_ThrowsArgumentException()
    {
        // On Windows, this is the main separator; on Unix, it's just invalid
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save\\game"));

        // Message will vary by platform
        Assert.True(ex.Message.Contains("separator") || ex.Message.Contains("invalid"));
    }

    [Fact]
    public void Validate_WithParentDirectoryReference_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("../save"));

        // The error could be about ".." or separator, depending on which is checked first
        Assert.True(ex.Message.Contains("..") || ex.Message.Contains("separator"));
    }

    [Fact]
    public void Validate_WithParentDirectoryInMiddle_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save..game"));

        Assert.Contains("..", ex.Message);
    }

    [Fact]
    public void Validate_WithParentDirectoryAtEnd_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save.."));

        Assert.Contains("..", ex.Message);
    }

    [Fact]
    public void Validate_WithNullCharacter_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save\0game"));

        Assert.Contains("invalid character", ex.Message);
    }

    [Fact]
    public void Validate_WithColon_ThrowsArgumentException()
    {
        // Colon is invalid on Windows (drive letter separator)
        if (Path.GetInvalidFileNameChars().Contains(':'))
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                SlotNameValidator.Validate("save:game"));

            Assert.Contains("invalid character", ex.Message);
        }
    }

    [Fact]
    public void Validate_WithQuestionMark_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save?game"));

        Assert.Contains("invalid character", ex.Message);
    }

    [Fact]
    public void Validate_WithAsterisk_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save*game"));

        Assert.Contains("invalid character", ex.Message);
    }

    [Fact]
    public void Validate_WithPipe_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save|game"));

        Assert.Contains("invalid character", ex.Message);
    }

    [Fact]
    public void Validate_WithLessThan_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save<game"));

        Assert.Contains("invalid character", ex.Message);
    }

    [Fact]
    public void Validate_WithGreaterThan_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save>game"));

        Assert.Contains("invalid character", ex.Message);
    }

    [Fact]
    public void Validate_WithQuotes_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SlotNameValidator.Validate("save\"game"));

        Assert.Contains("invalid character", ex.Message);
    }

    #endregion

    #region TryValidate Tests - Valid Names

    [Fact]
    public void TryValidate_WithSimpleName_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("savegame", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidate_WithAlphanumericName_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("save123", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidate_WithUnderscores_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("my_save_game", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidate_WithHyphens_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("my-save-game", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidate_WithSpaces_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("my save game", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidate_WithDots_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("save.1", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidate_WithMixedCharacters_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("Save_Game-1 (Auto)", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    #endregion

    #region TryValidate Tests - Invalid Names

    [Fact]
    public void TryValidate_WithNull_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate(null!, out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("null or whitespace", errorMessage);
    }

    [Fact]
    public void TryValidate_WithEmptyString_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("null or whitespace", errorMessage);
    }

    [Fact]
    public void TryValidate_WithWhitespaceOnly_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("   ", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("null or whitespace", errorMessage);
    }

    [Fact]
    public void TryValidate_WithDirectorySeparator_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate($"save{Path.DirectorySeparatorChar}game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("directory separator", errorMessage);
    }

    [Fact]
    public void TryValidate_WithAltDirectorySeparator_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate($"save{Path.AltDirectorySeparatorChar}game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("alternate directory separator", errorMessage);
    }

    [Fact]
    public void TryValidate_WithParentDirectoryReference_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("../save", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        // The error could be about ".." or separator, depending on which is checked first
        Assert.True(errorMessage.Contains("..") || errorMessage.Contains("separator"));
    }

    [Fact]
    public void TryValidate_WithParentDirectoryInMiddle_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save..game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("..", errorMessage);
    }

    [Fact]
    public void TryValidate_WithNullCharacter_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save\0game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid character", errorMessage);
    }

    [Fact]
    public void TryValidate_WithQuestionMark_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save?game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid character", errorMessage);
    }

    [Fact]
    public void TryValidate_WithAsterisk_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save*game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid character", errorMessage);
    }

    [Fact]
    public void TryValidate_WithPipe_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save|game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid character", errorMessage);
    }

    [Fact]
    public void TryValidate_WithLessThan_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save<game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid character", errorMessage);
    }

    [Fact]
    public void TryValidate_WithGreaterThan_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save>game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid character", errorMessage);
    }

    [Fact]
    public void TryValidate_WithQuotes_ReturnsFalse()
    {
        var result = SlotNameValidator.TryValidate("save\"game", out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid character", errorMessage);
    }

    #endregion

    #region Path Traversal Attack Tests

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("save/../../../secret")]
    [InlineData("..")]
    [InlineData("...")]
    public void Validate_WithPathTraversalAttempt_ThrowsArgumentException(string maliciousName)
    {
        var ex = Assert.Throws<ArgumentException>(() => SlotNameValidator.Validate(maliciousName));
        Assert.NotNull(ex);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("save/../../../secret")]
    [InlineData("..")]
    [InlineData("...")]
    public void TryValidate_WithPathTraversalAttempt_ReturnsFalse(string maliciousName)
    {
        var result = SlotNameValidator.TryValidate(maliciousName, out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\System32")]
    [InlineData("/var/log/system.log")]
    [InlineData("\\\\server\\share\\file")]
    public void Validate_WithAbsolutePath_ThrowsArgumentException(string absolutePath)
    {
        var ex = Assert.Throws<ArgumentException>(() => SlotNameValidator.Validate(absolutePath));
        Assert.NotNull(ex);
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\System32")]
    [InlineData("/var/log/system.log")]
    [InlineData("\\\\server\\share\\file")]
    public void TryValidate_WithAbsolutePath_ReturnsFalse(string absolutePath)
    {
        var result = SlotNameValidator.TryValidate(absolutePath, out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithVeryLongName_DoesNotThrow()
    {
        // Most file systems support at least 255 characters
        var longName = new string('a', 200);
        var exception = Record.Exception(() => SlotNameValidator.Validate(longName));
        Assert.Null(exception);
    }

    [Fact]
    public void TryValidate_WithVeryLongName_ReturnsTrue()
    {
        var longName = new string('a', 200);
        var result = SlotNameValidator.TryValidate(longName, out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Validate_WithUnicodeCharacters_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("保存ゲーム"));
        Assert.Null(exception);
    }

    [Fact]
    public void TryValidate_WithUnicodeCharacters_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("保存ゲーム", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Validate_WithEmoji_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("save_game_😀"));
        Assert.Null(exception);
    }

    [Fact]
    public void TryValidate_WithEmoji_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("save_game_😀", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Validate_WithSingleDot_DoesNotThrow()
    {
        // Single dot is valid (current directory reference in paths, but valid in filenames)
        var exception = Record.Exception(() => SlotNameValidator.Validate("."));
        Assert.Null(exception);
    }

    [Fact]
    public void TryValidate_WithSingleDot_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate(".", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    public void Validate_WithWindowsReservedNames_BehaviorDependsOnPlatform(string reservedName)
    {
        // On Windows, these may be invalid; on Unix, they're fine
        // We don't enforce this - the OS will handle it
        var exception = Record.Exception(() => SlotNameValidator.Validate(reservedName));
        // Just verify it doesn't crash
        Assert.True(exception == null || exception is ArgumentException);
    }

    [Fact]
    public void Validate_WithLeadingWhitespace_DoesNotThrow()
    {
        // Leading/trailing whitespace is technically valid in filenames
        var exception = Record.Exception(() => SlotNameValidator.Validate(" savegame"));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithTrailingWhitespace_DoesNotThrow()
    {
        var exception = Record.Exception(() => SlotNameValidator.Validate("savegame "));
        Assert.Null(exception);
    }

    [Fact]
    public void TryValidate_WithLeadingWhitespace_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate(" savegame", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidate_WithTrailingWhitespace_ReturnsTrue()
    {
        var result = SlotNameValidator.TryValidate("savegame ", out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    #endregion

    #region Consistency Tests

    [Fact]
    public void Validate_And_TryValidate_AreConsistent_ForValidNames()
    {
        var validNames = new[]
        {
            "savegame",
            "save_game",
            "save-game",
            "save game",
            "save.1",
            "save(1)",
            "save[1]"
        };

        foreach (var name in validNames)
        {
            // Validate should not throw
            var validateException = Record.Exception(() => SlotNameValidator.Validate(name));
            Assert.Null(validateException);

            // TryValidate should return true
            var tryResult = SlotNameValidator.TryValidate(name, out var errorMessage);
            Assert.True(tryResult);
            Assert.Null(errorMessage);
        }
    }

    [Fact]
    public void Validate_And_TryValidate_AreConsistent_ForInvalidNames()
    {
        var invalidNames = new[]
        {
            null!,
            "",
            "   ",
            "../save",
            "save..game",
            "save/game",
            "save\\game",
            "save?game",
            "save*game",
            "save|game",
            "save<game",
            "save>game",
            "save\"game",
            "save\0game"
        };

        foreach (var name in invalidNames)
        {
            // Validate should throw ArgumentException or a derived type (like ArgumentNullException)
            var validateException = Record.Exception(() => SlotNameValidator.Validate(name));
            Assert.NotNull(validateException);
            Assert.True(validateException is ArgumentException,
                $"Expected ArgumentException or derived type, got {validateException.GetType().Name}");

            // TryValidate should return false
            var tryResult = SlotNameValidator.TryValidate(name, out var errorMessage);
            Assert.False(tryResult);
            Assert.NotNull(errorMessage);
        }
    }

    #endregion
}
