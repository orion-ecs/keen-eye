using System.Numerics;
using KeenEyes.Graph;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Tests;

public class PortPositionCacheTests
{
    #region SetPortPosition and GetPortPosition Tests

    [Fact]
    public void SetPortPosition_AndGetPortPosition_ReturnsSameValue()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var position = new Vector2(100, 200);

        cache.SetPortPosition(node, PortDirection.Input, 0, position);
        var result = cache.GetPortPosition(node, PortDirection.Input, 0);

        Assert.Equal(position.X, result.X);
        Assert.Equal(position.Y, result.Y);
    }

    [Fact]
    public void GetPortPosition_WithNonExistent_ReturnsZero()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);

        var result = cache.GetPortPosition(node, PortDirection.Input, 0);

        Assert.Equal(Vector2.Zero, result);
    }

    [Fact]
    public void TryGetPortPosition_WithExisting_ReturnsTrue()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var position = new Vector2(100, 200);
        cache.SetPortPosition(node, PortDirection.Input, 0, position);

        var found = cache.TryGetPortPosition(node, PortDirection.Input, 0, out var result);

        Assert.True(found);
        Assert.Equal(position, result);
    }

    [Fact]
    public void TryGetPortPosition_WithNonExistent_ReturnsFalse()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);

        var found = cache.TryGetPortPosition(node, PortDirection.Input, 0, out var result);

        Assert.False(found);
        Assert.Equal(Vector2.Zero, result);
    }

    [Fact]
    public void SetPortPosition_DifferentDirections_StoresIndependently()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var inputPos = new Vector2(0, 100);
        var outputPos = new Vector2(200, 100);

        cache.SetPortPosition(node, PortDirection.Input, 0, inputPos);
        cache.SetPortPosition(node, PortDirection.Output, 0, outputPos);

        Assert.Equal(inputPos, cache.GetPortPosition(node, PortDirection.Input, 0));
        Assert.Equal(outputPos, cache.GetPortPosition(node, PortDirection.Output, 0));
    }

    [Fact]
    public void SetPortPosition_DifferentIndices_StoresIndependently()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var pos0 = new Vector2(0, 50);
        var pos1 = new Vector2(0, 100);

        cache.SetPortPosition(node, PortDirection.Input, 0, pos0);
        cache.SetPortPosition(node, PortDirection.Input, 1, pos1);

        Assert.Equal(pos0, cache.GetPortPosition(node, PortDirection.Input, 0));
        Assert.Equal(pos1, cache.GetPortPosition(node, PortDirection.Input, 1));
    }

    #endregion

    #region Clear and Count Tests

    [Fact]
    public void Count_InitiallyZero()
    {
        var cache = new PortPositionCache();

        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Count_IncreasesAfterAdd()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);

        cache.SetPortPosition(node, PortDirection.Input, 0, Vector2.Zero);
        cache.SetPortPosition(node, PortDirection.Input, 1, Vector2.Zero);
        cache.SetPortPosition(node, PortDirection.Output, 0, Vector2.Zero);

        Assert.Equal(3, cache.Count);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        cache.SetPortPosition(node, PortDirection.Input, 0, Vector2.Zero);
        cache.SetPortPosition(node, PortDirection.Output, 0, Vector2.Zero);

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGetPortPosition(node, PortDirection.Input, 0, out _));
    }

    #endregion

    #region HitTestPort Tests

    [Fact]
    public void HitTestPort_WithinRadius_ReturnsTrue()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var portPos = new Vector2(100, 100);
        cache.SetPortPosition(node, PortDirection.Input, 0, portPos);

        // Screen pos exactly at port position (no pan, zoom 1, no origin offset)
        var screenPos = new Vector2(100, 100);
        var hit = cache.HitTestPort(
            screenPos,
            pan: Vector2.Zero,
            zoom: 1f,
            origin: Vector2.Zero,
            out var hitNode,
            out var hitDirection,
            out var hitIndex);

        Assert.True(hit);
        Assert.Equal(node, hitNode);
        Assert.Equal(PortDirection.Input, hitDirection);
        Assert.Equal(0, hitIndex);
    }

    [Fact]
    public void HitTestPort_OutsideRadius_ReturnsFalse()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var portPos = new Vector2(100, 100);
        cache.SetPortPosition(node, PortDirection.Input, 0, portPos);

        // Screen pos far from port position
        var screenPos = new Vector2(200, 200);
        var hit = cache.HitTestPort(
            screenPos,
            pan: Vector2.Zero,
            zoom: 1f,
            origin: Vector2.Zero,
            out var hitNode,
            out _,
            out var hitIndex);

        Assert.False(hit);
        Assert.Equal(Entity.Null, hitNode);
        Assert.Equal(-1, hitIndex);
    }

    [Fact]
    public void HitTestPort_JustWithinRadius_ReturnsTrue()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var portPos = new Vector2(100, 100);
        cache.SetPortPosition(node, PortDirection.Input, 0, portPos);

        // Screen pos just inside hit radius (12 pixels at zoom 1)
        var screenPos = new Vector2(100 + 10f, 100);
        var hit = cache.HitTestPort(
            screenPos,
            pan: Vector2.Zero,
            zoom: 1f,
            origin: Vector2.Zero,
            out _,
            out _,
            out _);

        Assert.True(hit);
    }

    [Fact]
    public void HitTestPort_JustOutsideRadius_ReturnsFalse()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var portPos = new Vector2(100, 100);
        cache.SetPortPosition(node, PortDirection.Input, 0, portPos);

        // Screen pos just outside hit radius (12 pixels at zoom 1)
        var screenPos = new Vector2(100 + 15f, 100);
        var hit = cache.HitTestPort(
            screenPos,
            pan: Vector2.Zero,
            zoom: 1f,
            origin: Vector2.Zero,
            out _,
            out _,
            out _);

        Assert.False(hit);
    }

    [Fact]
    public void HitTestPort_WithZoom_ScalesHitRadius()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        var portPos = new Vector2(100, 100);
        cache.SetPortPosition(node, PortDirection.Input, 0, portPos);

        // At zoom 2, port appears at screen position 200,200
        // Hit radius also scales by zoom (12 * 2 = 24 pixels)
        var screenPos = new Vector2(200 + 20f, 200);
        var hit = cache.HitTestPort(
            screenPos,
            pan: Vector2.Zero,
            zoom: 2f,
            origin: Vector2.Zero,
            out _,
            out _,
            out _);

        Assert.True(hit);
    }

    [Fact]
    public void HitTestPort_EmptyCache_ReturnsFalse()
    {
        var cache = new PortPositionCache();

        var hit = cache.HitTestPort(
            new Vector2(100, 100),
            pan: Vector2.Zero,
            zoom: 1f,
            origin: Vector2.Zero,
            out _,
            out _,
            out _);

        Assert.False(hit);
    }

    [Fact]
    public void HitTestPort_WithMultiplePorts_ReturnsClosest()
    {
        var cache = new PortPositionCache();
        var node1 = new Entity(1, 0);
        var node2 = new Entity(2, 0);
        cache.SetPortPosition(node1, PortDirection.Input, 0, new Vector2(100, 100));
        cache.SetPortPosition(node2, PortDirection.Output, 0, new Vector2(110, 100));

        // Position closer to node1's port
        var screenPos = new Vector2(100, 100);
        var hit = cache.HitTestPort(
            screenPos,
            pan: Vector2.Zero,
            zoom: 1f,
            origin: Vector2.Zero,
            out var hitNode,
            out var hitDirection,
            out _);

        Assert.True(hit);
        Assert.Equal(node1, hitNode);
        Assert.Equal(PortDirection.Input, hitDirection);
    }

    #endregion

    #region GetAllPorts Tests

    [Fact]
    public void GetAllPorts_ReturnsAllEntries()
    {
        var cache = new PortPositionCache();
        var node = new Entity(1, 0);
        cache.SetPortPosition(node, PortDirection.Input, 0, new Vector2(0, 50));
        cache.SetPortPosition(node, PortDirection.Input, 1, new Vector2(0, 100));
        cache.SetPortPosition(node, PortDirection.Output, 0, new Vector2(200, 75));

        var allPorts = cache.GetAllPorts().ToList();

        Assert.Equal(3, allPorts.Count);
    }

    [Fact]
    public void GetAllPorts_EmptyCache_ReturnsEmpty()
    {
        var cache = new PortPositionCache();

        var allPorts = cache.GetAllPorts().ToList();

        Assert.Empty(allPorts);
    }

    #endregion
}
