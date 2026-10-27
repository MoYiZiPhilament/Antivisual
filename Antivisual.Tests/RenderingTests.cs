using Antivisual.Models;
using Antivisual.Rendering;
using Antivisual.Services;
using SkiaSharp;

namespace Antivisual.Tests;

public sealed class RenderingTests
{
    private static CoverSettings EmptyAssets => CoverSettings.Default with
    {
        BackgroundPath = string.Empty,
        CoverPath = string.Empty,
        LeftSidePath = string.Empty,
        RightSidePath = string.Empty,
        TextBackgroundPath = string.Empty
    };

    [Theory]
    [InlineData(CoverAspect.SixteenByNine, 1920, 1080)]
    [InlineData(CoverAspect.FourByThree, 1920, 1440)]
    public void RenderPng_ProducesExpectedDimensions(CoverAspect aspect, int width, int height)
    {
        using var renderer = new CoverRenderer();
        var png = renderer.RenderPng(EmptyAssets, aspect);
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);
        Assert.Equal(width, bitmap.Width);
        Assert.Equal(height, bitmap.Height);
        Assert.Equal(new byte[] { 137, 80, 78, 71 }, png.Take(4).ToArray());
    }

    [Fact]
    public void GlossColor_ChangesRenderedPixels()
    {
        using var renderer = new CoverRenderer();
        var purple = renderer.RenderPng(EmptyAssets with { GlossColor = "#2300A5" }, CoverAspect.SixteenByNine, 480);
        var cyan = renderer.RenderPng(EmptyAssets with { GlossColor = "#00E5FF" }, CoverAspect.SixteenByNine, 480);
        Assert.NotEqual(purple, cyan);
    }

    [Fact]
    public async Task Export_CreatesBothPngFilesWithoutOverwriting()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Antivisual.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var service = new ExportService();
            var settings = EmptyAssets with { TrackTitle = "Test/Track" };
            var files = await service.ExportBothAsync(settings, directory);
            var secondFiles = await service.ExportBothAsync(settings, directory);

            Assert.Equal(2, files.Count);
            Assert.All(files, path => Assert.True(File.Exists(path)));
            Assert.Contains(files, path => path.EndsWith("Test_Track_16x9.png", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(files, path => path.EndsWith("Test_Track_4x3.png", StringComparison.OrdinalIgnoreCase));
            Assert.All(secondFiles, path => Assert.True(File.Exists(path)));
            Assert.Equal(4, Directory.GetFiles(directory, "*.png").Length);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
