using System.IO;
using Antivisual.Models;

namespace Antivisual.Services;

public static class AssetCatalog
{
    public const string SideDirectory = @"C:\Users\1\Documents\TempContent\Antivisual\Side";
    public const string TextBackgroundDirectory = @"C:\Users\1\Documents\TempContent\Antivisual\Text";
    public const string DefaultBackgroundPath = @"E:\Arcade-Alpha_v1.11.3_b2026072500\Backgrounds\megarex_signal.jpg";

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
