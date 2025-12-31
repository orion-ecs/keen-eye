// Stub types for testing the shader generator
// These would be provided by KeenEyes.Shaders in a real project

namespace KeenEyes
{
    public class World
    {
        public IEnumerable<int> Query<T1, T2>() where T1 : struct where T2 : struct => [];
        public ref T Get<T>(int entity) where T : struct => throw new NotImplementedException();
        public IEnumerable<Archetype> QueryArchetypes(Shaders.QueryDescriptor descriptor) => [];
    }

    public class Archetype
    {
        public int Count => 0;
        public int EntityCount => 0;
        public T[] GetComponentArray<T>() where T : struct => [];
        public Span<T> GetComponentSpan<T>() where T : struct => [];
        public void SetComponentArray<T>(T[] data) where T : struct { }
    }
}

namespace KeenEyes.Shaders
{
    public interface IGpuComputeSystem
    {
        void Execute(KeenEyes.World world, float deltaTime);
    }

    public interface IGpuDevice
    {
        CompiledShader CompileComputeShader(string source);
        GpuBuffer<T> CreateBuffer<T>(int size) where T : unmanaged;
        GpuCommandBuffer CreateCommandBuffer();
    }

    public class MockGpuDevice : IGpuDevice
    {
        public CompiledShader CompileComputeShader(string source) => new();
        public GpuBuffer<T> CreateBuffer<T>(int size) where T : unmanaged => new();
        public GpuCommandBuffer CreateCommandBuffer() => new();
    }

    public class GpuCommandBuffer
    {
        public void Submit() { }
        public void BindComputeShader(CompiledShader shader) { }
        public void BindBuffer(int binding, object buffer) { }
        public void SetUniform(string name, float value) { }
        public void SetUniform(string name, uint value) { }
        public void Dispatch(int x, int y, int z) { }
        public void Execute() { }
    }

    public class CompiledShader : IDisposable
    {
        public void SetUniform(string name, float value) { }
        public void SetUniform(string name, uint value) { }
        public void BindBuffer(int binding, object buffer) { }
        public void Dispatch(int x, int y, int z) { }
        public void Dispose() { }
    }

    public class GpuBuffer<T> : IDisposable where T : unmanaged
    {
        public int Count { get; set; }
        public void Upload(ReadOnlySpan<T> data) { }
        public void Download(Span<T> data) { }
        public void Dispose() { }
    }

    public class QueryDescriptor
    {
        public static QueryDescriptor Create() => new();
        public QueryDescriptor With<T>() => this;
        public QueryDescriptor WithWrite<T>() => this;
        public QueryDescriptor WithRead<T>() => this;
        public QueryDescriptor Without<T>() => this;
    }

    public static class EmbeddedShaders
    {
        public static string UpdatePhysics => "";
    }
}

// Component stubs matching the shader query
namespace KeenEyes.Shaders.Generator.IntegrationTests
{
    public struct Position
    {
        public float X;
        public float Y;
        public float Z;
    }

    public struct Velocity
    {
        public float X;
        public float Y;
        public float Z;
    }

    public struct Frozen;
}
