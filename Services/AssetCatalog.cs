using System.IO;
using Antivisual.Models;

namespace Antivisual.Services;

public static class AssetCatalog
{
    public const string LegacyAssetRoot = @"C:\Users\1\Documents\TempContent\Antivisual";
    public const string LegacyDefaultBackgroundPath = @"E:\Arcade-Alpha_v1.11.3_b2026072500\Backgrounds\megarex_signal.jpg";

    public static string SideDirectory { get; } = Path.Combine(CoverSettings.DefaultAssetRoot, "Side");
    public static string DefaultBackgroundPath => CoverSettings.DefaultBackgroundPath;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp"
    };

    public static IReadOnlyList<AssetItem> Enumerate(string directory)
    {
        if (!Directory.Exists(directory)) return Array.Empty<AssetItem>();

        return Directory.EnumerateFiles(directory)
            .Where(path => ImageExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .Select(path => new AssetItem(Path.GetFileName(path), path))
            .ToArray();
    }

    public static string DisplayName(string? path, string emptyText)
        => string.IsNullOrWhiteSpace(path) ? emptyText : Path.GetFileName(path);
}
