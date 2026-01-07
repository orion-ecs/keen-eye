using KeenEyes.Graphics.Abstractions;
using KeenEyes.TestBridge.Capture;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.TestBridge.Tests.Capture;

public class CaptureControllerImplTests
{
    #region IsAvailable

    [Fact]
    public void IsAvailable_WithNoGraphicsContext_ReturnsFalse()
    {
        var controller = new CaptureControllerImpl();

        controller.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void IsAvailable_WithUninitializedContext_ReturnsFalse()
    {
        var context = new MockGraphicsContext { IsInitialized = false };
        var controller = new CaptureControllerImpl(context);

        controller.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void IsAvailable_WithNullDevice_ReturnsFalse()
    {
        var context = new MockGraphicsContext { Device = null };
        var controller = new CaptureControllerImpl(context);

        controller.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void IsAvailable_WithInitializedContextAndDevice_ReturnsTrue()
    {
        var device = new MockGraphicsDevice();
        var context = new MockGraphicsContext
        {
            IsInitialized = true,
            Device = device
        };
        var controller = new CaptureControllerImpl(context);

        controller.IsAvailable.ShouldBeTrue();
    }

    #endregion

    #region GetFrameSizeAsync

    [Fact]
    public async Task GetFrameSizeAsync_WhenNotAvailable_ReturnsZero()
    {
        var controller = new CaptureControllerImpl();

        var (width, height) = await controller.GetFrameSizeAsync();

        width.ShouldBe(0);
        height.ShouldBe(0);
    }

    [Fact]
    public async Task GetFrameSizeAsync_WhenAvailable_ReturnsContextDimensions()
    {
        var device = new MockGraphicsDevice();
        var context = new MockGraphicsContext
        {
            IsInitialized = true,
            Device = device,
            Width = 1920,
            Height = 1080
        };
        var controller = new CaptureControllerImpl(context);

        var (width, height) = await controller.GetFrameSizeAsync();

        width.ShouldBe(1920);
        height.ShouldBe(1080);
    }

    #endregion

    #region CaptureFrameAsync

    [Fact]
    public async Task CaptureFrameAsync_WhenNotAvailable_ThrowsInvalidOperation()
    {
        var controller = new CaptureControllerImpl();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await controller.CaptureFrameAsync());
    }

    [Fact]
    public async Task CaptureFrameAsync_WhenAvailable_ReturnsFrameWithCorrectDimensions()
    {
        var (controller, _) = CreateInitializedController(100, 100);

        var frame = await controller.CaptureFrameAsync();

        frame.Width.ShouldBe(100);
        frame.Height.ShouldBe(100);
    }

    [Fact]
    public async Task CaptureFrameAsync_WhenAvailable_ReturnsPixelsOfCorrectSize()
    {
        var (controller, _) = CreateInitializedController(50, 50);

        var frame = await controller.CaptureFrameAsync();

        frame.Pixels.Length.ShouldBe(50 * 50 * 4);
    }

    [Fact]
    public async Task CaptureFrameAsync_IncrementsFrameNumber_OnEachCapture()
    {
        var (controller, _) = CreateInitializedController(10, 10);

        var frame1 = await controller.CaptureFrameAsync();
        controller.OnFrameComplete();
        var frame2 = await controller.CaptureFrameAsync();

        frame2.FrameNumber.ShouldBeGreaterThan(frame1.FrameNumber);
    }

    [Fact]
    public async Task CaptureFrameAsync_FlipsPixelsVertically()
    {
        var device = new MockGraphicsDevice();

        // Create a framebuffer with a known pattern:
        // Row 0 (bottom in OpenGL): Red
        // Row 1 (top in OpenGL): Blue
        const int width = 2;
        const int height = 2;
        var framebufferData = new byte[width * height * 4];

        // Row 0 - Red (will be flipped to bottom)
        framebufferData[0] = 255;  // R
        framebufferData[1] = 0;    // G
        framebufferData[2] = 0;    // B
        framebufferData[3] = 255;  // A
        framebufferData[4] = 255;
        framebufferData[5] = 0;
        framebufferData[6] = 0;
        framebufferData[7] = 255;

        // Row 1 - Blue (will be flipped to top)
        framebufferData[8] = 0;
        framebufferData[9] = 0;
        framebufferData[10] = 255;
        framebufferData[11] = 255;
        framebufferData[12] = 0;
        framebufferData[13] = 0;
        framebufferData[14] = 255;
        framebufferData[15] = 255;

        device.SimulatedFramebufferData = framebufferData;
        device.SimulatedFramebufferWidth = width;
        device.SimulatedFramebufferHeight = height;

        var context = new MockGraphicsContext
        {
            IsInitialized = true,
            Device = device,
            Width = width,
            Height = height
        };
        var controller = new CaptureControllerImpl(context);

        var frame = await controller.CaptureFrameAsync();

        // After flipping, row 0 in output should be blue (was row 1 in framebuffer)
        // Top-left pixel should be blue
        var (r, g, b, _) = frame.GetPixel(0, 0);
        r.ShouldBe((byte)0);
        g.ShouldBe((byte)0);
        b.ShouldBe((byte)255);

        // Bottom-left pixel should be red
        var (r2, g2, b2, _) = frame.GetPixel(0, 1);
        r2.ShouldBe((byte)255);
        g2.ShouldBe((byte)0);
        b2.ShouldBe((byte)0);
    }

    #endregion

    #region GetScreenshotBytesAsync

    [Fact]
    public async Task GetScreenshotBytesAsync_WhenNotAvailable_ThrowsInvalidOperation()
    {
        var controller = new CaptureControllerImpl();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await controller.GetScreenshotBytesAsync());
    }

    [Fact]
    public async Task GetScreenshotBytesAsync_DefaultFormat_ReturnsPng()
    {
        var (controller, _) = CreateInitializedController(10, 10);

        var bytes = await controller.GetScreenshotBytesAsync();

        // PNG magic bytes
        bytes[0].ShouldBe((byte)0x89);
        bytes[1].ShouldBe((byte)'P');
    }

    [Fact]
    public async Task GetScreenshotBytesAsync_JpegFormat_ReturnsJpeg()
    {
        var (controller, _) = CreateInitializedController(10, 10);

        var bytes = await controller.GetScreenshotBytesAsync(ImageFormat.Jpeg);

        // JPEG magic bytes
        bytes[0].ShouldBe((byte)0xFF);
        bytes[1].ShouldBe((byte)0xD8);
    }

    [Fact]
    public async Task GetScreenshotBytesAsync_BmpFormat_ReturnsBmp()
    {
        var (controller, _) = CreateInitializedController(10, 10);

        var bytes = await controller.GetScreenshotBytesAsync(ImageFormat.Bmp);

        // BMP magic bytes
        bytes[0].ShouldBe((byte)'B');
        bytes[1].ShouldBe((byte)'M');
    }

    #endregion

    #region SaveScreenshotAsync

    [Fact]
    public async Task SaveScreenshotAsync_WhenNotAvailable_ThrowsInvalidOperation()
    {
        var controller = new CaptureControllerImpl();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await controller.SaveScreenshotAsync("test.png"));
    }

    [Fact]
    public async Task SaveScreenshotAsync_WritesFile_ToSpecifiedPath()
    {
        var (controller, _) = CreateInitializedController(10, 10);
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");

        try
        {
            var resultPath = await controller.SaveScreenshotAsync(tempPath);

            File.Exists(resultPath).ShouldBeTrue();
            resultPath.ShouldBe(Path.GetFullPath(tempPath));

#pragma warning disable xUnit1051 // Test verifies file content, cancellation not needed
            var bytes = await File.ReadAllBytesAsync(resultPath);
#pragma warning restore xUnit1051
            bytes[0].ShouldBe((byte)0x89); // PNG magic byte
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task SaveScreenshotAsync_CreatesDirectory_IfNotExists()
    {
        var (controller, _) = CreateInitializedController(10, 10);
        var tempDir = Path.Combine(Path.GetTempPath(), $"screenshot_test_{Guid.NewGuid()}");
        var tempPath = Path.Combine(tempDir, "screenshot.png");

        try
        {
            await controller.SaveScreenshotAsync(tempPath);

            Directory.Exists(tempDir).ShouldBeTrue();
            File.Exists(tempPath).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region Recording

    [Fact]
    public void IsRecording_Initially_ReturnsFalse()
    {
        var controller = new CaptureControllerImpl();

        controller.IsRecording.ShouldBeFalse();
    }

    [Fact]
    public async Task StartRecordingAsync_SetsIsRecordingToTrue()
    {
        var controller = new CaptureControllerImpl();

        await controller.StartRecordingAsync();

        controller.IsRecording.ShouldBeTrue();
    }

    [Fact]
    public async Task StartRecordingAsync_WhenAlreadyRecording_ThrowsInvalidOperation()
    {
        var controller = new CaptureControllerImpl();
        await controller.StartRecordingAsync();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await controller.StartRecordingAsync());
    }

    [Fact]
    public async Task StopRecordingAsync_SetsIsRecordingToFalse()
    {
        var controller = new CaptureControllerImpl();
        await controller.StartRecordingAsync();

        await controller.StopRecordingAsync();

        controller.IsRecording.ShouldBeFalse();
    }

    [Fact]
    public async Task StopRecordingAsync_WhenNotRecording_ThrowsInvalidOperation()
    {
        var controller = new CaptureControllerImpl();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await controller.StopRecordingAsync());
    }

    [Fact]
    public void RecordedFrameCount_Initially_ReturnsZero()
    {
        var controller = new CaptureControllerImpl();

        controller.RecordedFrameCount.ShouldBe(0);
    }

    [Fact]
    public async Task CaptureFrameAsync_WhileRecording_IncrementsRecordedFrameCount()
    {
        var (controller, _) = CreateInitializedController(10, 10);
        await controller.StartRecordingAsync();

        await controller.CaptureFrameAsync();
        await controller.CaptureFrameAsync();

        controller.RecordedFrameCount.ShouldBe(2);
    }

    [Fact]
    public async Task StopRecordingAsync_ReturnsAllRecordedFrames()
    {
        var (controller, _) = CreateInitializedController(10, 10);
        await controller.StartRecordingAsync();

        await controller.CaptureFrameAsync();
        controller.OnFrameComplete();
        await controller.CaptureFrameAsync();
        controller.OnFrameComplete();
        await controller.CaptureFrameAsync();

        var frames = await controller.StopRecordingAsync();

        frames.Count.ShouldBe(3);
    }

    [Fact]
    public async Task StopRecordingAsync_ClearsRecordedFrames()
    {
        var (controller, _) = CreateInitializedController(10, 10);
        await controller.StartRecordingAsync();
        await controller.CaptureFrameAsync();
        await controller.StopRecordingAsync();

        controller.RecordedFrameCount.ShouldBe(0);
    }

    [Fact]
    public async Task StartRecordingAsync_WithMaxFrames_TrimsOlderFrames()
    {
        var (controller, _) = CreateInitializedController(10, 10);
        await controller.StartRecordingAsync(maxFrames: 2);

        await controller.CaptureFrameAsync();
        controller.OnFrameComplete();
        await controller.CaptureFrameAsync();
        controller.OnFrameComplete();
        await controller.CaptureFrameAsync();

        controller.RecordedFrameCount.ShouldBe(2);
    }

    #endregion

    #region Helper Methods

    private static (CaptureControllerImpl Controller, MockGraphicsDevice Device) CreateInitializedController(int width, int height)
    {
        var device = new MockGraphicsDevice();

        // Create solid color framebuffer data
        var framebufferData = new byte[width * height * 4];
        for (var i = 0; i < framebufferData.Length; i += 4)
        {
            framebufferData[i] = 128;     // R
            framebufferData[i + 1] = 64;  // G
            framebufferData[i + 2] = 32;  // B
            framebufferData[i + 3] = 255; // A
        }
        device.SimulatedFramebufferData = framebufferData;
        device.SimulatedFramebufferWidth = width;
        device.SimulatedFramebufferHeight = height;

        var context = new MockGraphicsContext
        {
            IsInitialized = true,
            Device = device,
            Width = width,
            Height = height
        };

        return (new CaptureControllerImpl(context), device);
    }

    #endregion
}
