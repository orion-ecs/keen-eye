using System;
using KeenEyes.Mcp.TestBridge;
using Xunit;

namespace KeenEyes.Mcp.TestBridge.Tests;

/// <summary>
/// Tests for <see cref="ConfigurationParser"/> transport-mode selection and configuration parsing.
/// </summary>
/// <remarks>
/// All tests live in a single class so xUnit runs them sequentially, keeping the process-global
/// environment-variable mutations from racing one another.
/// </remarks>
public sealed class ConfigurationParserTests
{
    #region ParseClientTransport Tests

    [Fact]
    public void ParseClientTransport_WithNull_ReturnsStdio()
    {
        Assert.Equal(McpClientTransport.Stdio, ConfigurationParser.ParseClientTransport(null));
    }

    [Fact]
    public void ParseClientTransport_WithEmpty_ReturnsStdio()
    {
        Assert.Equal(McpClientTransport.Stdio, ConfigurationParser.ParseClientTransport("  "));
    }

    [Theory]
    [InlineData("http")]
    [InlineData("HTTP")]
    [InlineData(" Http ")]
    public void ParseClientTransport_WithHttp_ReturnsHttp(string value)
    {
        Assert.Equal(McpClientTransport.Http, ConfigurationParser.ParseClientTransport(value));
    }

    [Fact]
    public void ParseClientTransport_WithStdio_ReturnsStdio()
    {
        Assert.Equal(McpClientTransport.Stdio, ConfigurationParser.ParseClientTransport("stdio"));
    }

    [Fact]
    public void ParseClientTransport_WithUnknownValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ConfigurationParser.ParseClientTransport("websocket"));
    }

    #endregion

    #region Parse Default Tests

    [Fact]
    public void Parse_WithNoArgsOrEnv_DefaultsToStdio()
    {
        WithoutMcpEnvironment(() =>
        {
            var config = ConfigurationParser.Parse([]);

            Assert.Equal(McpClientTransport.Stdio, config.ClientTransport);
            Assert.Equal(ConfigurationParser.DefaultHttpUrl, config.HttpUrl);
            Assert.Null(config.AuthToken);
        });
    }

    #endregion

    #region Parse Flag Override Tests

    [Fact]
    public void Parse_WithMcpTransportFlag_SelectsHttp()
    {
        WithoutMcpEnvironment(() =>
        {
            var config = ConfigurationParser.Parse(["--mcp-transport", "http"]);

            Assert.Equal(McpClientTransport.Http, config.ClientTransport);
        });
    }

    [Fact]
    public void Parse_WithMcpUrlFlag_OverridesDefaultUrl()
    {
        WithoutMcpEnvironment(() =>
        {
            var config = ConfigurationParser.Parse(["--mcp-transport", "http", "--mcp-url", "http://192.168.1.5:9000/"]);

            Assert.Equal("http://192.168.1.5:9000/", config.HttpUrl);
        });
    }

    #endregion

    #region Parse Environment Tests

    [Fact]
    public void Parse_WithTransportEnvironmentVariable_SelectsHttp()
    {
        WithoutMcpEnvironment(() =>
        {
            Environment.SetEnvironmentVariable("KEENEYES_MCP_TRANSPORT", "http");

            var config = ConfigurationParser.Parse([]);

            Assert.Equal(McpClientTransport.Http, config.ClientTransport);
        });
    }

    [Fact]
    public void Parse_WithTokenEnvironmentVariable_PopulatesAuthToken()
    {
        WithoutMcpEnvironment(() =>
        {
            Environment.SetEnvironmentVariable("KEENEYES_MCP_TOKEN", "s3cret");

            var config = ConfigurationParser.Parse([]);

            Assert.Equal("s3cret", config.AuthToken);
        });
    }

    [Fact]
    public void Parse_WithBlankTokenEnvironmentVariable_LeavesAuthTokenNull()
    {
        WithoutMcpEnvironment(() =>
        {
            Environment.SetEnvironmentVariable("KEENEYES_MCP_TOKEN", "   ");

            var config = ConfigurationParser.Parse([]);

            Assert.Null(config.AuthToken);
        });
    }

    [Fact]
    public void Parse_FlagOverridesTransportEnvironmentVariable()
    {
        WithoutMcpEnvironment(() =>
        {
            Environment.SetEnvironmentVariable("KEENEYES_MCP_TRANSPORT", "stdio");

            var config = ConfigurationParser.Parse(["--mcp-transport", "http"]);

            Assert.Equal(McpClientTransport.Http, config.ClientTransport);
        });
    }

    #endregion

    private static void WithoutMcpEnvironment(Action action)
    {
        var transport = Environment.GetEnvironmentVariable("KEENEYES_MCP_TRANSPORT");
        var url = Environment.GetEnvironmentVariable("KEENEYES_MCP_URL");
        var token = Environment.GetEnvironmentVariable("KEENEYES_MCP_TOKEN");

        Environment.SetEnvironmentVariable("KEENEYES_MCP_TRANSPORT", null);
        Environment.SetEnvironmentVariable("KEENEYES_MCP_URL", null);
        Environment.SetEnvironmentVariable("KEENEYES_MCP_TOKEN", null);

        try
        {
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("KEENEYES_MCP_TRANSPORT", transport);
            Environment.SetEnvironmentVariable("KEENEYES_MCP_URL", url);
            Environment.SetEnvironmentVariable("KEENEYES_MCP_TOKEN", token);
        }
    }
}
