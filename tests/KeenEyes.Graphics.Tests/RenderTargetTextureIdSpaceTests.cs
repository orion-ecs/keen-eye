using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Regression tests for #1279: render-target attachment handles must live in the
/// texture manager's ID space so <see cref="IGraphicsContext.BindTexture"/> binds the
/// actual attachment. Before the fix they carried raw GL ids, so binding resolved to
/// whatever unrelated texture occupied that manager slot (or nothing).
/// </summary>
public class RenderTargetTextureIdSpaceTests
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

    [Fact]
    public void GetRenderTargetColorTexture_HandleResolvesToAttachmentGlTexture()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            var target = context.CreateRenderTarget(64, 64, RenderTargetFormat.RGBA8Depth24);
            var handle = context.GetRenderTargetColorTexture(target);

            Assert.True(handle.IsValid);

            // The handle must resolve in the texture manager's ID space...
            var data = context.TextureManager.GetTexture(handle.Id);
            Assert.NotNull(data);

            // ...to exactly the GL texture the render target manager attached.
            var glId = context.RenderTargets!.GetColorTextureId(target);
            Assert.Equal(glId, data.Handle);

            // And BindTexture must bind that GL texture (the observable symptom before
            // the fix: no bind, or a bind of an unrelated texture).
            device.Calls.Clear();
            context.BindTexture(handle, unit: 3);
            Assert.Contains($"BindTexture(Texture2D, {glId})", device.Calls);
        }
    }

    [Fact]
    public void GetRenderTargetDepthTexture_HandleResolvesToAttachmentGlTexture()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            var target = context.CreateRenderTarget(32, 32, RenderTargetFormat.RGBA8Depth24);
            var handle = context.GetRenderTargetDepthTexture(target);

            Assert.True(handle.IsValid);

            var data = context.TextureManager.GetTexture(handle.Id);
            Assert.NotNull(data);
            Assert.Equal(context.RenderTargets!.GetDepthTextureId(target), data.Handle);

            device.Calls.Clear();
            context.BindTexture(handle, unit: 0);
            Assert.Contains($"BindTexture(Texture2D, {data.Handle})", device.Calls);
        }
    }

    [Fact]
    public void GetRenderTargetColorTexture_CalledRepeatedly_ReturnsSameHandle()
    {
        var (world, context, _) = CreateLoadedGraphics();
        using (world)
        {
            var target = context.CreateRenderTarget(16, 16, RenderTargetFormat.RGBA8Depth24);

            var first = context.GetRenderTargetColorTexture(target);
            var second = context.GetRenderTargetColorTexture(target);

            // Per-frame lookups must not mint a new registration each call.
            Assert.Equal(first.Id, second.Id);
        }
    }

    [Fact]
    public void DeleteRenderTarget_ReleasesRegistration_AndDeletesGlTextureExactlyOnce()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            var target = context.CreateRenderTarget(16, 16, RenderTargetFormat.RGBA8Depth24);
            var handle = context.GetRenderTargetColorTexture(target);
            var glId = context.TextureManager.GetTexture(handle.Id)!.Handle;

            context.DeleteRenderTarget(target);

            // Registration gone; the render target manager deleted the GL texture once
            // (a double delete would corrupt an unrelated texture reusing the id).
            Assert.Null(context.TextureManager.GetTexture(handle.Id));
            Assert.Equal(1, device.Calls.Count(c => c == $"DeleteTexture({glId})"));

            // Manager teardown must not delete it a second time.
            device.Calls.Clear();
            world.UninstallPlugin<SilkGraphicsPlugin>();
            Assert.DoesNotContain($"DeleteTexture({glId})", device.Calls);
        }
    }

    [Fact]
    public void DeleteRenderTargetKeepTexture_ColorHandleSurvives_AndManagerTakesOwnership()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            var target = context.CreateRenderTarget(16, 16, RenderTargetFormat.RGBA8Depth24);
            var handle = context.GetRenderTargetColorTexture(target);
            var glId = context.TextureManager.GetTexture(handle.Id)!.Handle;

            context.DeleteRenderTargetKeepTexture(target);

            // The kept color texture still resolves and binds (the IBL BRDF-LUT flow).
            var data = context.TextureManager.GetTexture(handle.Id);
            Assert.NotNull(data);
            Assert.Equal(glId, data.Handle);
            Assert.DoesNotContain($"DeleteTexture({glId})", device.Calls);

            // Ownership transferred: deleting the handle now deletes the GL texture.
            context.DeleteTexture(handle);
            Assert.Equal(1, device.Calls.Count(c => c == $"DeleteTexture({glId})"));
        }
    }

    [Fact]
    public void GetRenderTargetColorTexture_DoesNotAliasUnrelatedManagerTextures()
    {
        var (world, context, _) = CreateLoadedGraphics();
        using (world)
        {
            // Populate the texture manager so low-numbered manager ids are taken; raw GL
            // ids from the render target would collide with them before the fix.
            byte[] pixel = [255, 0, 0, 255];
            var unrelated = new List<TextureHandle>();
            for (var i = 0; i < 8; i++)
            {
                unrelated.Add(context.CreateTexture(1, 1, pixel));
            }

            var target = context.CreateRenderTarget(16, 16, RenderTargetFormat.RGBA8Depth24);
            var handle = context.GetRenderTargetColorTexture(target);

            // The render-target handle must not equal any unrelated texture's handle,
            // and must resolve to the attachment rather than one of them.
            Assert.DoesNotContain(handle.Id, unrelated.Select(u => u.Id));
            Assert.Equal(
                context.RenderTargets!.GetColorTextureId(target),
                context.TextureManager.GetTexture(handle.Id)!.Handle);
        }
    }
}
