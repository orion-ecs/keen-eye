using System.Text.Json;
using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.Ipc.Protocol;

namespace KeenEyes.TestBridge.Tests.Ipc;

public class IpcJsonContextTests
{
    #region IpcRequest

    [Fact]
    public void IpcRequest_Serialization_RoundTrips()
    {
        var request = new IpcRequest
        {
            Id = 42,
            Command = "test.command",
            Args = JsonSerializer.SerializeToElement(new { name = "test" })
        };

        var json = JsonSerializer.Serialize(request, IpcJsonContext.Default.IpcRequest);
        var deserialized = JsonSerializer.Deserialize(json, IpcJsonContext.Default.IpcRequest);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(42);
        deserialized.Command.ShouldBe("test.command");
        deserialized.Args.ShouldNotBeNull();
    }

    [Fact]
    public void IpcRequest_WithNullArgs_Serializes()
    {
        var request = new IpcRequest
        {
            Id = 1,
            Command = "test",
            Args = null
        };

        var json = JsonSerializer.Serialize(request, IpcJsonContext.Default.IpcRequest);

        // null args are serialized explicitly (needed for AOT compatibility with required properties)
        json.ShouldContain("\"args\":null");
    }

    [Fact]
    public void IpcRequest_UsesCamelCase()
    {
        var request = new IpcRequest
        {
            Id = 1,
            Command = "test",
            Args = null
        };

        var json = JsonSerializer.Serialize(request, IpcJsonContext.Default.IpcRequest);

        json.ShouldContain("\"id\"");
        json.ShouldContain("\"command\"");
    }

    #endregion

    #region IpcResponse

    [Fact]
    public void IpcResponse_SuccessWithData_RoundTrips()
    {
        var response = new IpcResponse
        {
            Id = 42,
            Success = true,
            Data = JsonSerializer.SerializeToElement(123),
            Error = null
        };

        var json = JsonSerializer.Serialize(response, IpcJsonContext.Default.IpcResponse);
        var deserialized = JsonSerializer.Deserialize(json, IpcJsonContext.Default.IpcResponse);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(42);
        deserialized.Success.ShouldBeTrue();
        deserialized.Data.ShouldNotBeNull();
        deserialized.Data!.Value.GetInt32().ShouldBe(123);
        deserialized.Error.ShouldBeNull();
    }

    [Fact]
    public void IpcResponse_ErrorWithMessage_RoundTrips()
    {
        var response = new IpcResponse
        {
            Id = 42,
            Success = false,
            Data = null,
            Error = "Something went wrong"
        };

        var json = JsonSerializer.Serialize(response, IpcJsonContext.Default.IpcResponse);
        var deserialized = JsonSerializer.Deserialize(json, IpcJsonContext.Default.IpcResponse);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(42);
        deserialized.Success.ShouldBeFalse();
        deserialized.Data.ShouldBeNull();
        deserialized.Error.ShouldBe("Something went wrong");
    }

    #endregion

    #region Enums

    [Fact]
    public void Key_SerializesAsString()
    {
        var key = Key.Space;

        var json = JsonSerializer.Serialize(key, IpcJsonContext.Default.Key);

        json.ShouldBe("\"Space\"");
    }

    [Fact]
    public void Key_DeserializesFromString()
    {
        var json = "\"Enter\"";

        var key = JsonSerializer.Deserialize(json, IpcJsonContext.Default.Key);

        key.ShouldBe(Key.Enter);
    }

    [Fact]
    public void MouseButton_SerializesAsString()
    {
        var button = MouseButton.Right;

        var json = JsonSerializer.Serialize(button, IpcJsonContext.Default.MouseButton);

        json.ShouldBe("\"Right\"");
    }

    [Fact]
    public void KeyModifiers_SerializesAsString()
    {
        var modifiers = KeyModifiers.Control | KeyModifiers.Shift;

        var json = JsonSerializer.Serialize(modifiers, IpcJsonContext.Default.KeyModifiers);

        // Flags enum serialization
        json.ShouldContain("Control");
        json.ShouldContain("Shift");
    }

    #endregion

    #region Result Types

    [Fact]
    public void FrameSizeResult_RoundTrips()
    {
        var result = new FrameSizeResult { Width = 1920, Height = 1080 };

        var json = JsonSerializer.Serialize(result, IpcJsonContext.Default.FrameSizeResult);
        var deserialized = JsonSerializer.Deserialize(json, IpcJsonContext.Default.FrameSizeResult);

        deserialized.ShouldNotBeNull();
        deserialized.Width.ShouldBe(1920);
        deserialized.Height.ShouldBe(1080);
    }

    [Fact]
    public void MousePositionResult_RoundTrips()
    {
        var result = new MousePositionResult { X = 100.5f, Y = 200.5f };

        var json = JsonSerializer.Serialize(result, IpcJsonContext.Default.MousePositionResult);
        var deserialized = JsonSerializer.Deserialize(json, IpcJsonContext.Default.MousePositionResult);

        deserialized.ShouldNotBeNull();
        deserialized.X.ShouldBe(100.5f);
        deserialized.Y.ShouldBe(200.5f);
    }

    #endregion

    #region Primitives

    [Fact]
    public void Int_RoundTrips()
    {
        var json = JsonSerializer.Serialize(42, IpcJsonContext.Default.Int32);
        var value = JsonSerializer.Deserialize(json, IpcJsonContext.Default.Int32);

        value.ShouldBe(42);
    }

    [Fact]
    public void Bool_RoundTrips()
    {
        var json = JsonSerializer.Serialize(true, IpcJsonContext.Default.Boolean);
        var value = JsonSerializer.Deserialize(json, IpcJsonContext.Default.Boolean);

        value.ShouldBeTrue();
    }

    [Fact]
    public void String_RoundTrips()
    {
        var json = JsonSerializer.Serialize("hello", IpcJsonContext.Default.String);
        var value = JsonSerializer.Deserialize(json, IpcJsonContext.Default.String);

        value.ShouldBe("hello");
    }

    #endregion
}
