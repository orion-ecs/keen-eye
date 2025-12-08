using System.Numerics;
using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the MeshManager class.
/// </summary>
public class MeshManagerTests : IDisposable
{
    private readonly MockGraphicsDevice device;
    private readonly MeshManager manager;

    public MeshManagerTests()
    {
        device = new MockGraphicsDevice();
        manager = new MeshManager { Device = device };
    }

    public void Dispose()
    {
        manager.Dispose();
        device.Dispose();
    }

    #region CreateMesh Tests

    [Fact]
    public void CreateMesh_WithValidData_ReturnsPositiveId()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = manager.CreateMesh(vertices, indices);

        Assert.True(meshId > 0);
    }

    [Fact]
    public void CreateMesh_GeneratesVAO()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        manager.CreateMesh(vertices, indices);

        Assert.Single(device.GeneratedVAOs);
    }

    [Fact]
    public void CreateMesh_GeneratesTwoBuffers()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        manager.CreateMesh(vertices, indices);

        // One VBO for vertices, one EBO for indices
        Assert.Equal(2, device.GeneratedBuffers.Count);
    }

    [Fact]
    public void CreateMesh_BindsVAO()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        manager.CreateMesh(vertices, indices);

        Assert.Contains(device.Calls, c => c.StartsWith("BindVertexArray(") && !c.Contains("(0)"));
    }

    [Fact]
    public void CreateMesh_EnablesVertexAttributes()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        manager.CreateMesh(vertices, indices);

        // Should enable position, normal, texCoord attributes
        var enableCalls = device.Calls.Count(c => c.StartsWith("EnableVertexAttribArray"));
        Assert.True(enableCalls >= 3);
    }

    [Fact]
    public void CreateMesh_StoresIndexCount()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = manager.CreateMesh(vertices, indices);
        var meshData = manager.GetMesh(meshId);

        Assert.NotNull(meshData);
        Assert.Equal(3, meshData.IndexCount);
    }

    [Fact]
    public void CreateMesh_WithoutDevice_ThrowsInvalidOperationException()
    {
        var managerWithoutDevice = new MeshManager();
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        Assert.Throws<InvalidOperationException>(() =>
            managerWithoutDevice.CreateMesh(vertices, indices));
    }

    [Fact]
    public void CreateMesh_MultipleCalls_ReturnsUniqueIds()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int id1 = manager.CreateMesh(vertices, indices);
        int id2 = manager.CreateMesh(vertices, indices);

        Assert.NotEqual(id1, id2);
    }

    #endregion

    #region GetMesh Tests

    [Fact]
    public void GetMesh_WithValidId_ReturnsMeshData()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = manager.CreateMesh(vertices, indices);
        var meshData = manager.GetMesh(meshId);

        Assert.NotNull(meshData);
    }

    [Fact]
    public void GetMesh_WithInvalidId_ReturnsNull()
    {
        var meshData = manager.GetMesh(999);

        Assert.Null(meshData);
    }

    [Fact]
    public void GetMesh_WithZeroId_ReturnsNull()
    {
        var meshData = manager.GetMesh(0);

        Assert.Null(meshData);
    }

    #endregion

    #region DeleteMesh Tests

    [Fact]
    public void DeleteMesh_WithValidId_ReturnsTrue()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = manager.CreateMesh(vertices, indices);
        bool deleted = manager.DeleteMesh(meshId);

        Assert.True(deleted);
    }

    [Fact]
    public void DeleteMesh_WithInvalidId_ReturnsFalse()
    {
        bool deleted = manager.DeleteMesh(999);

        Assert.False(deleted);
    }

    [Fact]
    public void DeleteMesh_DeletesVAO()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = manager.CreateMesh(vertices, indices);
        manager.DeleteMesh(meshId);

        Assert.Single(device.DeletedVAOs);
    }

    [Fact]
    public void DeleteMesh_DeletesBuffers()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = manager.CreateMesh(vertices, indices);
        manager.DeleteMesh(meshId);

        // Should delete both VBO and EBO
        Assert.Equal(2, device.DeletedBuffers.Count);
    }

    [Fact]
    public void DeleteMesh_MakesMeshUnavailable()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = manager.CreateMesh(vertices, indices);
        manager.DeleteMesh(meshId);

        Assert.Null(manager.GetMesh(meshId));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DeletesAllMeshes()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        manager.CreateMesh(vertices, indices);
        manager.CreateMesh(vertices, indices);
        device.Reset();

        // Use a new manager instance for this test
        using var testManager = new MeshManager { Device = device };
        testManager.CreateMesh(vertices, indices);
        testManager.CreateMesh(vertices, indices);
        testManager.Dispose();

        Assert.Equal(2, device.DeletedVAOs.Count);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector3(0, 0, 0) },
            new() { Position = new Vector3(1, 0, 0) },
            new() { Position = new Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        using var testManager = new MeshManager { Device = device };
        testManager.CreateMesh(vertices, indices);

        // Should not throw
        testManager.Dispose();
        testManager.Dispose();
    }

    #endregion
}
