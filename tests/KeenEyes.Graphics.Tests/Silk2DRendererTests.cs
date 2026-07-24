using System.Numerics;
using System.Runtime.InteropServices;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Rendering2D;
using KeenEyes.Graphics.Silk.Resources;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Headless tests for <see cref="Silk2DRenderer"/> geometry and projection handling.
/// </summary>
/// <remarks>
/// The renderer is normally GPU-bound, but <see cref="MockGraphicsDevice"/> records every
/// buffer upload, draw call, and uniform assignment, which lets these tests verify the
/// vertex/index math and projection state without a real OpenGL context.
/// </remarks>
public class Silk2DRendererTests : IDisposable
{
    private const float ScreenWidth = 800f;
    private const float ScreenHeight = 600f;

    // Vertex2D layout: Position (Vector2, 8 bytes) + TexCoord (Vector2, 8 bytes) + Color (Vector4, 16 bytes).
    private const int FloatsPerVertex = 8;

    private readonly MockGraphicsDevice device = new();
    private readonly TextureManager textureManager;
    private readonly Silk2DRenderer renderer;

    public Silk2DRendererTests()
    {
        textureManager = new TextureManager { Device = device };
        renderer = new Silk2DRenderer(device, textureManager, ScreenWidth, ScreenHeight);
    }

    public void Dispose()
    {
        renderer.Dispose();
        textureManager.Dispose();
    }

    #region #1183 - FillEllipse / FillCircle index packing

    /// <summary>
    /// Regression test for #1183: filled circles must tessellate into a clean triangle fan.
    /// </summary>
    /// <remarks>
    /// The batch shares one quad-patterned index buffer. Pre-fix, <c>FillEllipse</c> emitted
    /// three vertices per segment while the index buffer advanced four vertices per quad, so
    /// the drawn triangles indexed fan vertices as if they were quads. That produced
    /// "garbage" triangles built entirely from perimeter points (never touching the center),
    /// corrupting the shape. A correct fan has every visible triangle anchored at the center.
    /// </remarks>
    [Fact]
    public void FillCircle_DrawnTriangles_AreAllCenterAnchoredWedges()
    {
        const int segments = 8;
        var center = new Vector2(100f, 120f);

        renderer.Begin();
        renderer.FillCircle(center.X, center.Y, 50f, new Vector4(1f, 1f, 1f, 1f), segments);
        renderer.End();

        var triangles = ReconstructDrawnTriangles();

        int nonDegenerate = 0;
        int garbageWithoutCenter = 0;
        foreach (var (a, b, c) in triangles)
        {
            if (IsDegenerate(a, b, c))
            {
                continue;
            }

            nonDegenerate++;

            bool touchesCenter =
                ApproximatelyEqual(a, center) ||
                ApproximatelyEqual(b, center) ||
                ApproximatelyEqual(c, center);

            if (!touchesCenter)
            {
                garbageWithoutCenter++;
            }
        }

        // Pre-fix code produces at least one perimeter-only triangle; the fix produces none.
        Assert.Equal(0, garbageWithoutCenter);

        // A correct fan yields exactly one visible wedge per segment.
        Assert.Equal(segments, nonDegenerate);
    }

    #endregion

    #region #1187 - Begin(projection) honored through Flush

    /// <summary>
    /// Regression test for #1187: a custom projection supplied to <c>Begin</c> must remain
    /// bound through the <c>Flush</c> that issues the draw call, instead of being overwritten
    /// by the internal screen projection.
    /// </summary>
    [Fact]
    public void Begin_WithCustomProjection_KeepsItBoundAtDrawTime()
    {
        // A projection clearly different from the 800x600 screen ortho.
        var custom = Matrix4x4.CreateOrthographicOffCenter(0f, 320f, 240f, 0f, -1f, 1f);
        var screen = Matrix4x4.CreateOrthographicOffCenter(0f, ScreenWidth, ScreenHeight, 0f, -1f, 1f);

        renderer.Begin(custom);
        renderer.FillRect(10f, 10f, 50f, 50f, new Vector4(1f, 0f, 0f, 1f));
        renderer.End();

        var boundProjection = GetBoundProjection();

        Assert.Equal(custom, boundProjection);
        Assert.NotEqual(screen, boundProjection);
    }

    /// <summary>
    /// The default <c>Begin()</c> overload must draw using the screen projection.
    /// </summary>
    [Fact]
    public void Begin_Default_UsesScreenProjection()
    {
        var screen = Matrix4x4.CreateOrthographicOffCenter(0f, ScreenWidth, ScreenHeight, 0f, -1f, 1f);

        renderer.Begin();
        renderer.FillRect(10f, 10f, 50f, 50f, new Vector4(1f, 0f, 0f, 1f));
        renderer.End();

        Assert.Equal(screen, GetBoundProjection());
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Reconstructs the triangles drawn by the last triangle draw call, applying the shared
    /// quad-patterned index layout (v+0, v+1, v+2, v+2, v+3, v+0) to the uploaded vertices.
    /// </summary>
    private List<(Vector2 A, Vector2 B, Vector2 C)> ReconstructDrawnTriangles()
    {
        var draw = device.DrawCalls.Last(d => d.PrimitiveType == PrimitiveType.Triangles && d.IsIndexed);
        int indexCount = draw.VertexCount;

        // The 2D vertex buffer is the array buffer holding only the freshly uploaded vertices
        // (the rounded-rect buffer keeps its far larger initial allocation).
        var vboData = device.Buffers.Values
            .Where(b => b.Target == BufferTarget.ArrayBuffer && b.Data is not null)
            .OrderBy(b => b.Data!.Length)
            .First()
            .Data!;

        var floats = MemoryMarshal.Cast<byte, float>(vboData);

        var triangles = new List<(Vector2, Vector2, Vector2)>();
        for (int baseIndex = 0, v = 0; baseIndex + 6 <= indexCount; baseIndex += 6, v += 4)
        {
            // Quad pattern: (v+0, v+1, v+2) and (v+2, v+3, v+0).
            triangles.Add((ReadPosition(floats, v + 0), ReadPosition(floats, v + 1), ReadPosition(floats, v + 2)));
            triangles.Add((ReadPosition(floats, v + 2), ReadPosition(floats, v + 3), ReadPosition(floats, v + 0)));
        }

        return triangles;
    }

    private static Vector2 ReadPosition(ReadOnlySpan<float> floats, int vertexIndex)
    {
        int offset = vertexIndex * FloatsPerVertex;
        return new Vector2(floats[offset], floats[offset + 1]);
    }

    private static bool IsDegenerate(Vector2 a, Vector2 b, Vector2 c)
    {
        float cross = ((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X));
        return MathF.Abs(cross) < 1e-3f;
    }

    private static bool ApproximatelyEqual(Vector2 a, Vector2 b)
    {
        return Vector2.DistanceSquared(a, b) < 1e-6f;
    }

    /// <summary>
    /// Reads the projection matrix bound to the main 2D shader program (identified by its
    /// <c>uTexture</c> uniform, which the rounded-rect shader lacks).
    /// </summary>
    private Matrix4x4 GetBoundProjection()
    {
        var program = device.Programs.Values.Single(p => p.UniformLocations.ContainsKey("uTexture"));
        int location = program.UniformLocations["uProjection"];
        return (Matrix4x4)program.UniformValues[location];
    }

    #endregion
}
