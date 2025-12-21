namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for AssetLoadException factory methods.
/// </summary>
public class AssetLoadExceptionTests
{
    #region FileNotFound Tests

    [Fact]
    public void FileNotFound_ContainsPath()
    {
        var ex = AssetLoadException.FileNotFound("textures/player.png", typeof(TestAsset));

        Assert.Contains("textures/player.png", ex.Message);
    }

    [Fact]
    public void FileNotFound_SetsProperties()
    {
        var ex = AssetLoadException.FileNotFound("test.txt", typeof(TestAsset));

        Assert.Equal("test.txt", ex.AssetPath);
        Assert.Equal(typeof(TestAsset), ex.AssetType);
    }

    #endregion

    #region UnsupportedFormat Tests

    [Fact]
    public void UnsupportedFormat_ContainsExtension()
    {
        var ex = AssetLoadException.UnsupportedFormat("file.xyz", typeof(TestAsset), ".xyz");

        Assert.Contains(".xyz", ex.Message);
    }

    [Fact]
    public void UnsupportedFormat_ContainsTypeName()
    {
        var ex = AssetLoadException.UnsupportedFormat("file.xyz", typeof(TestAsset), ".xyz");

        Assert.Contains("TestAsset", ex.Message);
    }

    [Fact]
    public void UnsupportedFormat_SetsProperties()
    {
        var ex = AssetLoadException.UnsupportedFormat("file.xyz", typeof(TestAsset), ".xyz");

        Assert.Equal("file.xyz", ex.AssetPath);
        Assert.Equal(typeof(TestAsset), ex.AssetType);
    }

    #endregion

    #region ParseError Tests

    [Fact]
    public void ParseError_ContainsPath()
    {
        var inner = new InvalidDataException("Bad data");
        var ex = AssetLoadException.ParseError("corrupt.txt", typeof(TestAsset), inner);

        Assert.Contains("corrupt.txt", ex.Message);
    }

    [Fact]
    public void ParseError_ContainsInnerException()
    {
        var inner = new InvalidDataException("Bad data");
        var ex = AssetLoadException.ParseError("corrupt.txt", typeof(TestAsset), inner);

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ParseError_SetsProperties()
    {
        var inner = new InvalidDataException("Bad data");
        var ex = AssetLoadException.ParseError("corrupt.txt", typeof(TestAsset), inner);

        Assert.Equal("corrupt.txt", ex.AssetPath);
        Assert.Equal(typeof(TestAsset), ex.AssetType);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var inner = new Exception("Inner");
        // Constructor order: (assetPath, assetType, message, innerException)
        var ex = new AssetLoadException("path.txt", typeof(TestAsset), "Custom message", inner);

        Assert.Equal("Custom message", ex.Message);
        Assert.Equal("path.txt", ex.AssetPath);
        Assert.Equal(typeof(TestAsset), ex.AssetType);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void Constructor_WithNullInnerException_Works()
    {
        var ex = new AssetLoadException("path.txt", typeof(TestAsset), "Error message");

        Assert.Equal("path.txt", ex.AssetPath);
        Assert.Null(ex.InnerException);
    }

    #endregion
}
