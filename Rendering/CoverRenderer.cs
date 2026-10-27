using System.IO;
using Antivisual.Models;
using SkiaSharp;

namespace Antivisual.Rendering;

/// <summary>
/// Renders all five cover layers in one coordinate system for both interactive previews and PNG export.
/// Successfully decoded images and text-background measurements are cached for the renderer lifetime.
/// </summary>
public sealed class CoverRenderer : IDisposable
{
    private const byte BackgroundOpacity = 148;
    private const float BackgroundBlur = 28f;
    private static readonly SKSamplingOptions HighQualitySampling =
        new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private readonly object _sync = new();
    private readonly Dictionary<string, SKImage> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> _strongBandCenters = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Gets the pixel dimensions for an arbitrary output width. Height is rounded to the nearest pixel.
    /// </summary>
    public static SKSizeI GetOutputSize(CoverAspect aspect, int targetWidth)
    {
        if (targetWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWidth), "Target width must be positive.");
        }

        var layout = CoverLayout.For(aspect);
        var height = (int)Math.Round(layout.Height * (targetWidth / (double)layout.Width));
        return new SKSizeI(targetWidth, height);
    }

    /// <summary>
    /// Draws a complete cover onto an existing canvas, for example an SKCanvasView PaintSurface canvas.
    /// The caller owns the canvas and should size its viewport with <see cref="GetOutputSize"/>.
    /// </summary>
    public void Render(
        SKCanvas canvas,
        CoverSettings settings,
        CoverAspect aspect,
        int targetWidth)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(settings);

        var layout = CoverLayout.For(aspect);
        var outputSize = GetOutputSize(aspect, targetWidth);
        var scale = targetWidth / (float)layout.Width;

        canvas.Save();
        try
        {
            canvas.ClipRect(new SKRect(0, 0, outputSize.Width, outputSize.Height));
            canvas.Clear(SKColors.Black);
            canvas.Scale(scale);

            DrawBackground(canvas, settings.BackgroundPath, layout);

            if (settings.SideDecorationsEnabled)
            {
                DrawSide(canvas, settings.LeftSidePath, layout, true);
                DrawSide(canvas, settings.RightSidePath, layout, false);
            }

            DrawTextBackground(canvas, settings.TextBackgroundPath, layout);
            DrawText(canvas, settings, layout);
            DrawCover(canvas, settings.CoverPath, layout);
        }
        finally
        {
            canvas.Restore();
        }
    }

    /// <summary>
    /// Renders into a new bitmap. A target width of zero uses the native PSD width of 1920.
    /// The caller owns the returned bitmap.
    /// </summary>
    public SKBitmap RenderBitmap(
        CoverSettings settings,
        CoverAspect aspect,
        int targetWidth = 0)
    {
        var layout = CoverLayout.For(aspect);
        var width = targetWidth == 0 ? layout.Width : targetWidth;
        var size = GetOutputSize(aspect, width);
        var bitmap = new SKBitmap(size.Width, size.Height, true);

        try
        {
            using var canvas = new SKCanvas(bitmap);
            Render(canvas, settings, aspect, width);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Renders and encodes a PNG. A target width of zero uses the native PSD width of 1920.
    /// </summary>
    public byte[] RenderPng(
        CoverSettings settings,
        CoverAspect aspect,
        int targetWidth = 0)
    {
        using var bitmap = RenderBitmap(settings, aspect, targetWidth);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray() ?? throw new InvalidOperationException("SkiaSharp could not encode the cover as PNG.");
    }

    /// <summary>
    /// Renders and writes a PNG to an existing writable stream. A target width of zero uses 1920.
    /// </summary>
    public void RenderPng(
        Stream output,
        CoverSettings settings,
        CoverAspect aspect,
        int targetWidth = 0)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", nameof(output));
        }

        using var bitmap = RenderBitmap(settings, aspect, targetWidth);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        if (data is null)
        {
            throw new InvalidOperationException("SkiaSharp could not encode the cover as PNG.");
        }

        data.SaveTo(output);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var image in _images.Values)
            {
                image.Dispose();
            }

            _images.Clear();
            _strongBandCenters.Clear();
        }
    }

    private void DrawBackground(SKCanvas canvas, string? path, CoverLayout layout)
    {
        var image = LoadImage(path);
        if (image is null)
        {
            return;
        }

        var canvasRect = new SKRect(0, 0, layout.Width, layout.Height);
        var scale = Math.Max(layout.Width / (float)image.Width, layout.Height / (float)image.Height);
        var width = image.Width * scale;
        var height = image.Height * scale;
        var destination = new SKRect(
            (layout.Width - width) * 0.5f,
            (layout.Height - height) * 0.5f,
            (layout.Width + width) * 0.5f,
            (layout.Height + height) * 0.5f);

        using var blur = SKImageFilter.CreateBlur(BackgroundBlur * 0.5f, BackgroundBlur * 0.5f);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 255, 255, BackgroundOpacity),
            ImageFilter = blur
        };
        canvas.Save();
        canvas.ClipRect(canvasRect);
        canvas.DrawImage(image, destination, HighQualitySampling, paint);
        canvas.Restore();
    }

    private void DrawSide(SKCanvas canvas, string? path, CoverLayout layout, bool left)
    {
        var image = LoadImage(path);
        if (image is null || image.Height <= 0)
        {
            return;
        }

        var width = image.Width * (layout.Height / (float)image.Height);
        var destination = left
            ? new SKRect(0, 0, width, layout.Height)
            : new SKRect(layout.Width - width, 0, layout.Width, layout.Height);
        canvas.DrawImage(image, destination, HighQualitySampling);
    }

    private void DrawTextBackground(SKCanvas canvas, string? path, CoverLayout layout)
    {
        var image = LoadImage(path);
        if (image is null || image.Width <= 0 || path is null)
        {
            return;
        }

        SKRect destination;
        if (string.Equals(Path.GetFileName(path), "TextBG_6.png", StringComparison.OrdinalIgnoreCase))
        {
            destination = layout.TextBackground6Rect;
        }
        else
        {
            var scale = layout.Width / (float)image.Width;
            var height = image.Height * scale;
            var sourceBandCenter = GetStrongBandCenter(path, image);
            var top = layout.TextBandCenterY - sourceBandCenter * scale;
            destination = new SKRect(0, top, layout.Width, top + height);
        }

        canvas.DrawImage(image, destination, HighQualitySampling);
    }

    private static void DrawText(SKCanvas canvas, CoverSettings settings, CoverLayout layout)
    {
        var family = string.IsNullOrWhiteSpace(settings.FontFamily) ? "Segoe UI" : settings.FontFamily;
        var typeface = SKFontManager.Default.MatchFamily(family) ??
                       SKFontManager.Default.MatchFamily("Segoe UI");

        try
        {
            using var font = new SKFont(typeface ?? SKTypeface.Default)
            {
                Edging = SKFontEdging.Antialias,
                Hinting = SKFontHinting.Normal,
                Subpixel = true
            };
            var gloss = ParseColor(settings.GlossColor);
            var textBase = Lighten(gloss, 0.72f);

            DrawTextLine(canvas, settings.TrackTitle, layout.TitleRect, font, gloss, textBase);
            DrawTextLine(canvas, settings.TrackArtist, layout.ArtistRect, font, gloss, textBase);
        }
        finally
        {
            typeface?.Dispose();
        }
    }

    private static void DrawTextLine(
        SKCanvas canvas,
        string? text,
        SKRect bounds,
        SKFont font,
        SKColor gloss,
        SKColor baseColor)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        font.Size = 100f;
        font.MeasureText(text, out var textBounds);
        if (textBounds.Width <= 0 || textBounds.Height <= 0)
        {
            return;
        }

        var fit = Math.Min(bounds.Width / textBounds.Width, bounds.Height / textBounds.Height);
        font.Size = Math.Max(1f, 100f * fit);
        font.MeasureText(text, out textBounds);

        var x = bounds.MidX - textBounds.MidX;
        var y = bounds.MidY - textBounds.MidY;

        using (var shadow = SKImageFilter.CreateDropShadow(
                   0,
                   12,
                   9,
                   9,
                   new SKColor(0, 0, 0, 128)))
        using (var paint = CreateTextPaint(SKColors.White))
        {
            paint.ImageFilter = shadow;
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        }

        using (var outerBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 23f))
        using (var paint = CreateTextPaint(new SKColor(255, 255, 255, 20)))
        {
            paint.BlendMode = SKBlendMode.Screen;
            paint.MaskFilter = outerBlur;
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        }

        using (var paint = CreateTextPaint(baseColor))
        {
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        }

        using (var satinShader = SKShader.CreateLinearGradient(
                   new SKPoint(bounds.Left, bounds.Top),
                   new SKPoint(bounds.Right, bounds.Bottom),
                   new[]
                   {
                       gloss.WithAlpha(0),
                       gloss.WithAlpha(255),
                       SKColors.White,
                       gloss.WithAlpha(255),
                       gloss.WithAlpha(0)
                   },
                   new[] { 0f, 0.35f, 0.5f, 0.65f, 1f },
                   SKShaderTileMode.Clamp))
        using (var paint = CreateTextPaint(new SKColor(255, 255, 255, 89)))
        {
            paint.BlendMode = SKBlendMode.HardLight;
            paint.Shader = satinShader;
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        }

        using (var innerBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Inner, 3.5f))
        using (var paint = CreateTextPaint(new SKColor(255, 255, 255, 89)))
        {
            paint.BlendMode = SKBlendMode.Screen;
            paint.MaskFilter = innerBlur;
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        }
    }

    private void DrawCover(SKCanvas canvas, string? path, CoverLayout layout)
    {
        var image = LoadImage(path);
        if (image is null)
        {
            return;
        }

        var rect = layout.CoverRect;

        using (var shadowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 62.5f))
        using (var paint = new SKPaint
               {
                   IsAntialias = true,
                   Color = new SKColor(0, 0, 0, 128),
                   MaskFilter = shadowBlur
               })
        {
            var shadowRect = rect;
            shadowRect.Offset(0, 31);
            canvas.DrawRect(shadowRect, paint);
        }

        using (var glowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 23f))
        using (var paint = new SKPaint
               {
                   IsAntialias = true,
                   Color = new SKColor(255, 255, 255, 74),
                   BlendMode = SKBlendMode.Screen,
                   MaskFilter = glowBlur
               })
        {
            canvas.DrawRect(rect, paint);
        }

        // Intentionally stretch both axes: product behavior requires direct normalization to 1:1.
        canvas.DrawImage(image, rect, HighQualitySampling);

        canvas.Save();
        try
        {
            canvas.ClipRect(rect);
            using var strokeShader = SKShader.CreateLinearGradient(
                new SKPoint(rect.Left, rect.Top),
                new SKPoint(rect.Left, rect.Bottom),
                new[] { SKColors.Black, SKColors.White },
                null,
                SKShaderTileMode.Clamp);
            using var stroke = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                // Clipping removes the outer half, leaving the PSD's 18px inner stroke.
                StrokeWidth = 36f,
                BlendMode = SKBlendMode.Difference,
                Shader = strokeShader
            };
            canvas.DrawRect(rect, stroke);
        }
        finally
        {
            canvas.Restore();
        }
    }

    private SKImage? LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_images.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }
        }

        SKImage? loaded = null;
        try
        {
            if (File.Exists(fullPath))
            {
                loaded = SKImage.FromEncodedData(fullPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }

        if (loaded is null)
        {
            // Missing/decode-failed files are deliberately not cached, so a later file replacement can succeed.
            return null;
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_images.TryGetValue(fullPath, out var existing))
            {
                loaded.Dispose();
                return existing;
            }

            _images.Add(fullPath, loaded);
            return loaded;
        }
    }

    private float GetStrongBandCenter(string path, SKImage image)
    {
        var key = Path.GetFullPath(path);
        lock (_sync)
        {
            if (_strongBandCenters.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var center = MeasureStrongBandCenter(image);
        lock (_sync)
        {
            _strongBandCenters[key] = center;
        }

        return center;
    }

    private static float MeasureStrongBandCenter(SKImage image)
    {
        using var bitmap = SKBitmap.FromImage(image);
        var first = -1;
        var last = -1;
        var xStep = Math.Max(1, image.Width / 512);

        for (var y = 0; y < image.Height; y++)
        {
            var hasStrongPixel = false;
            for (var x = 0; x < image.Width; x += xStep)
            {
                if (bitmap.GetPixel(x, y).Alpha < 128)
                {
                    continue;
                }

                hasStrongPixel = true;
                break;
            }

            if (!hasStrongPixel)
            {
                continue;
            }

            if (first < 0)
            {
                first = y;
            }

            last = y;
        }

        return first < 0 ? image.Height * 0.5f : (first + last + 1) * 0.5f;
    }

    private static SKPaint CreateTextPaint(SKColor color) => new()
    {
        IsAntialias = true,
        Color = color
    };

    private static SKColor Lighten(SKColor color, float amount) => new(
        (byte)(color.Red + (255 - color.Red) * amount),
        (byte)(color.Green + (255 - color.Green) * amount),
        (byte)(color.Blue + (255 - color.Blue) * amount));

    private static SKColor ParseColor(string? value)
    {
        return SKColor.TryParse(value, out var color)
            ? color
            : new SKColor(0x23, 0x00, 0xA5);
    }
}
