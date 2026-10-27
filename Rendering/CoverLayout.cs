using Antivisual.Models;
using SkiaSharp;

namespace Antivisual.Rendering;

/// <summary>
/// Absolute PSD-derived layout coordinates. Renderers scale this coordinate system as a unit.
/// </summary>
public sealed record CoverLayout
{
    public const float TextBackground6SourceHeight = 1202f;
    public const float TextBackground6StrongBandTop = 334f;
    public const float TextBackground6StrongBandBottom = 762f;

    private CoverLayout(
        int width,
        int height,
        SKRect coverRect,
        SKRect titleRect,
        SKRect artistRect,
        SKRect textBackground6Rect)
    {
        Width = width;
        Height = height;
        CoverRect = coverRect;
        TitleRect = titleRect;
        ArtistRect = artistRect;
        TextBackground6Rect = textBackground6Rect;
    }

    public int Width { get; }

    public int Height { get; }

    public SKRect CoverRect { get; }

    public SKRect TitleRect { get; }

    public SKRect ArtistRect { get; }

    public SKRect TextBackground6Rect { get; }

    /// <summary>
    /// Vertical center of TextBG_6's Alpha &gt;= 128 band in layout coordinates.
    /// Other text backgrounds align their measured strong-alpha band to this value.
    /// </summary>
    public float TextBandCenterY =>
        TextBackground6Rect.Top +
        TextBackground6Rect.Height *
        ((TextBackground6StrongBandTop + TextBackground6StrongBandBottom) * 0.5f /
         TextBackground6SourceHeight);

    public static CoverLayout SixteenByNine { get; } = new(
        1920,
        1080,
        new SKRect(639, 31, 1281, 673),
        new SKRect(360, 724, 1560, 819),
        new SKRect(360, 933, 1560, 1007),
        new SKRect(3, 455, 1920, 1353));

    public static CoverLayout FourByThree { get; } = new(
        1920,
        1440,
        new SKRect(531, 60, 1389, 918),
        new SKRect(203, 997, 1717, 1116),
        new SKRect(203, 1261, 1717, 1354),
        new SKRect(0, 770, 1920, 1672));

    public static CoverLayout For(CoverAspect aspect) => aspect switch
    {
        CoverAspect.SixteenByNine => SixteenByNine,
        CoverAspect.FourByThree => FourByThree,
        _ => throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "Unsupported cover aspect ratio.")
    };
}
