// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Cli.Commands;

namespace KeenEyes.Cli.Tests.Commands;

public sealed class CommandResultTests
{
    [Fact]
    public void Success_ReturnsSuccessResult()
    {
        var result = CommandResult.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Success_WithMessage_ReturnsSuccessWithMessage()
    {
        var result = CommandResult.Success("Done!");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("Done!", result.Message);
    }

    [Fact]
    public void Failure_ReturnsFailureResult()
    {
        var result = CommandResult.Failure("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("Something went wrong", result.Message);
    }

    [Fact]
    public void InvalidArguments_ReturnsInvalidArgsResult()
    {
        var result = CommandResult.InvalidArguments("Unknown option: --foo");

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("Unknown option: --foo", result.Message);
    }

    [Fact]
    public void Cancelled_ReturnsCancelledResult()
    {
        var result = CommandResult.Cancelled();

        Assert.False(result.IsSuccess);
        Assert.Equal(130, result.ExitCode);
    }
}
