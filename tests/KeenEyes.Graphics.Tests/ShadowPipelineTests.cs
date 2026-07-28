using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Shadows;
using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Regression tests for #1280: the shadow pipeline shipped with three defects that made
/// it unusable end-to-end — spot shadows bound texture units 16-19 that
/// <c>OpenGLDevice.ToGL</c> cannot represent (and GL 3.3 does not guarantee), the
/// point-light pass wrote only <c>gl_FragDepth</c> while the sampled cubemap is the
/// color attachment, and depth textures enabled <c>COMPARE_REF_TO_TEXTURE</c> while the
/// shaders sample them through plain <c>sampler2D</c> (undefined per the GL spec).
/// </summary>
public class ShadowPipelineTests
{
    private static (World World, SilkGraphicsContext Context, MockGraphicsDevice Device) CreateLoadedGraphics()
    {
        var world = new World();
        world.SetExtension<ISilkWindowProvider>(new MockSilkWindowProvider());
        world.InstallPlugin(new SilkGraphicsPlugin());

        var context = world.GetExtension<SilkGraphicsContext>();
        var device = new MockGraphicsDevice();
        context.InitializeResources(device);

        return (world, context, device);
    }

    #region Texture unit budget

    [Fact]
    public void ShadowTextureUnits_StayWithinGl33GuaranteedRange()
    {
        // OpenGL 3.3 guarantees exactly 16 fragment texture units (0-15), and the
        // engine's TextureUnit enum ends at Texture15. Every unit the render system
        // assigns must stay inside that range — unit 16 threw in OpenGLDevice.ToGL.
        Assert.InRange(
            RenderSystem.ShadowMapBaseTextureUnit + RenderSystem.MaxCascades - 1, 0, 15);
        Assert.InRange(
            RenderSystem.PointShadowMapBaseTextureUnit + RenderSystem.MaxPointShadows - 1, 0, 15);
        Assert.InRange(
            RenderSystem.SpotShadowMapBaseTextureUnit + RenderSystem.MaxSpotShadows - 1, 0, 15);
    }

    [Fact]
    public void ShadowTextureUnitRanges_DoNotOverlap()
    {
        var cascades = Enumerable.Range(RenderSystem.ShadowMapBaseTextureUnit, RenderSystem.MaxCascades);
        var points = Enumerable.Range(RenderSystem.PointShadowMapBaseTextureUnit, RenderSystem.MaxPointShadows);
        var spots = Enumerable.Range(RenderSystem.SpotShadowMapBaseTextureUnit, RenderSystem.MaxSpotShadows);

        var all = cascades.Concat(points).Concat(spots).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    #endregion

    #region Depth-compare mode

    [Fact]
    public void CreateRenderTarget_DepthTexture_DoesNotEnableDepthCompare()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            device.Calls.Clear();
            context.CreateRenderTarget(64, 64, RenderTargetFormat.Depth24);

            // The shadow shaders compare depths manually through plain sampler2D;
            // COMPARE_REF_TO_TEXTURE on such a texture is undefined per the GL spec.
            Assert.DoesNotContain(device.Calls, c => c.Contains("CompareMode"));
            Assert.DoesNotContain(device.Calls, c => c.Contains("CompareFunc"));
        }
    }

    #endregion

    #region Point shadow pass writes the sampled cubemap

    private static void SpawnPointShadowScene(World world)
    {
        world.Spawn("Camera")
            .With(new Camera { FieldOfView = 60f, NearPlane = 0.1f, FarPlane = 100f })
            .With(new Transform3D { Position = new Vector3(0, 0, 5) })
            .Build();

        var light = Light.Point(Vector3.One, 1f, 25f);
        light.CastShadows = true;
        world.Spawn("PointLight")
            .With(light)
            .With(new Transform3D { Position = new Vector3(0, 3, 0) })
            .Build();

        world.Spawn("Caster")
            .With(new Transform3D { Position = Vector3.Zero })
            .With(new Renderable(meshId: 1, materialId: 1) { CastShadows = true })
            .Build();
    }

    [Fact]
    public void PointShadowPass_ClearsAndWritesColorFaces()
    {
        var (world, _, device) = CreateLoadedGraphics();
        using (world)
        {
            SpawnPointShadowScene(world);

            var system = new ShadowRenderingSystem();
            system.Initialize(world);
            device.Calls.Clear();
            system.Update(0.016f);
            system.Dispose();

            // Faces must clear to white (1.0 = max distance = lit) including the COLOR
            // buffer; before the fix only the depth buffer was cleared and the sampled
            // color faces stayed unwritten forever.
            Assert.Contains("ClearColor(1, 1, 1, 1)", device.Calls);
            var colorClears = device.Calls.Count(c =>
                c.StartsWith("Clear(") && c.Contains("ColorBuffer") && c.Contains("DepthBuffer"));
            Assert.True(colorClears >= 6, $"expected 6 face clears with color, saw {colorClears}");
        }
    }

    [Fact]
    public void PointShadowPass_UsesDistanceWritingShader()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            SpawnPointShadowScene(world);

            var system = new ShadowRenderingSystem();
            system.Initialize(world);
            device.Calls.Clear();
            system.Update(0.016f);

            // The pass must parameterize the distance shader; a depth-only shader has
            // neither uniform and leaves the cubemap unwritten.
            Assert.Contains(device.Calls, c => c.StartsWith("GetUniformLocation(") && c.Contains("uLightPos"));
            Assert.Contains(device.Calls, c => c.StartsWith("GetUniformLocation(") && c.Contains("uFarPlane"));

            system.Dispose();
        }
    }

    #endregion
}
