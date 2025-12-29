using KeenEyes.Network.Prediction;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="InputBuffer{T}"/> class.
/// </summary>
public class InputBufferTests
{
    private struct TestInput : INetworkInput
    {
        public uint Tick { get; set; }
        public float Value { get; set; }
    }

    [Fact]
    public void Count_DefaultsToZero()
    {
        var buffer = new InputBuffer<TestInput>();

        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Add_SingleInput_IncreasesCount()
    {
        var buffer = new InputBuffer<TestInput>();

        buffer.Add(new TestInput { Tick = 1, Value = 1.0f });

        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void Add_SingleInput_SetsOldestAndNewestTick()
    {
        var buffer = new InputBuffer<TestInput>();

        buffer.Add(new TestInput { Tick = 5, Value = 1.0f });

        Assert.Equal(5u, buffer.OldestTick);
        Assert.Equal(5u, buffer.NewestTick);
    }

    [Fact]
    public void Add_MultipleInputs_UpdatesNewestTick()
    {
        var buffer = new InputBuffer<TestInput>();

        buffer.Add(new TestInput { Tick = 1, Value = 1.0f });
        buffer.Add(new TestInput { Tick = 2, Value = 2.0f });
        buffer.Add(new TestInput { Tick = 3, Value = 3.0f });

        Assert.Equal(1u, buffer.OldestTick);
        Assert.Equal(3u, buffer.NewestTick);
        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void TryGet_ExistingInput_ReturnsTrue()
    {
        var buffer = new InputBuffer<TestInput>();
        buffer.Add(new TestInput { Tick = 1, Value = 42.0f });

        var result = buffer.TryGet(1, out var input);

        Assert.True(result);
        Assert.Equal(1u, input.Tick);
        Assert.Equal(42.0f, input.Value);
    }

    [Fact]
    public void TryGet_NonExistingTick_ReturnsFalse()
    {
        var buffer = new InputBuffer<TestInput>();
        buffer.Add(new TestInput { Tick = 1, Value = 42.0f });

        var result = buffer.TryGet(999, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGet_OldTick_ReturnsFalseAfterWrap()
    {
        var buffer = new InputBuffer<TestInput>(capacity: 4);

        // Fill the buffer and wrap around
        for (uint i = 1; i <= 6; i++)
        {
            buffer.Add(new TestInput { Tick = i, Value = i });
        }

        // Old ticks should not be accessible
        Assert.False(buffer.TryGet(1, out _));
        Assert.False(buffer.TryGet(2, out _));

        // Recent ticks should be accessible
        Assert.True(buffer.TryGet(5, out _));
        Assert.True(buffer.TryGet(6, out _));
    }

    [Fact]
    public void RemoveOlderThan_RemovesOldInputs()
    {
        var buffer = new InputBuffer<TestInput>();
        buffer.Add(new TestInput { Tick = 1, Value = 1.0f });
        buffer.Add(new TestInput { Tick = 2, Value = 2.0f });
        buffer.Add(new TestInput { Tick = 3, Value = 3.0f });

        buffer.RemoveOlderThan(3);

        Assert.Equal(3u, buffer.OldestTick);
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void Clear_ResetsBuffer()
    {
        var buffer = new InputBuffer<TestInput>();
        buffer.Add(new TestInput { Tick = 1, Value = 1.0f });
        buffer.Add(new TestInput { Tick = 2, Value = 2.0f });

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.Equal(0u, buffer.OldestTick);
        Assert.Equal(0u, buffer.NewestTick);
    }

    [Fact]
    public void GetInputsFrom_ReturnsInputsFromStartTick()
    {
        var buffer = new InputBuffer<TestInput>();
        buffer.Add(new TestInput { Tick = 1, Value = 1.0f });
        buffer.Add(new TestInput { Tick = 2, Value = 2.0f });
        buffer.Add(new TestInput { Tick = 3, Value = 3.0f });
        buffer.Add(new TestInput { Tick = 4, Value = 4.0f });

        var inputs = buffer.GetInputsFrom(2).ToList();

        Assert.Equal(3, inputs.Count);
        Assert.Equal(2u, inputs[0].Tick);
        Assert.Equal(3u, inputs[1].Tick);
        Assert.Equal(4u, inputs[2].Tick);
    }

    [Fact]
    public void GetInputsFromBoxed_ReturnsBoxedInputs()
    {
        var buffer = new InputBuffer<TestInput>();
        buffer.Add(new TestInput { Tick = 1, Value = 1.0f });
        buffer.Add(new TestInput { Tick = 2, Value = 2.0f });

        var inputs = ((IInputBuffer)buffer).GetInputsFromBoxed(1).ToList();

        Assert.Equal(2, inputs.Count);
        Assert.IsType<TestInput>(inputs[0]);
        Assert.IsType<TestInput>(inputs[1]);
    }

    [Fact]
    public void IInputBuffer_Properties_ExposeCorrectValues()
    {
        var buffer = new InputBuffer<TestInput>();
        buffer.Add(new TestInput { Tick = 5, Value = 1.0f });
        buffer.Add(new TestInput { Tick = 10, Value = 2.0f });

        IInputBuffer iBuffer = buffer;

        Assert.Equal(5u, iBuffer.OldestTick);
        Assert.Equal(10u, iBuffer.NewestTick);
        Assert.Equal(2, iBuffer.Count);
    }
}
