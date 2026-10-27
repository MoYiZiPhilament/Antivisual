using System.IO;
using System.Text.Json;
using Antivisual.Models;

namespace Antivisual.Services;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Antivisual", "settings.json");

    public CoverSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return CoverSettings.Default;
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<CoverSettings>(json, _jsonOptions) ?? CoverSettings.Default;
            return MigrateToBundledAssets(settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            TryBackupInvalidSettings();
            return CoverSettings.Default;
        }
    }

    public static CoverSettings MigrateToBundledAssets(CoverSettings settings)
    {
        var defaults = CoverSettings.Default;
        return settings with
        {
            BackgroundPath = settings.BackgroundPath.Equals(AssetCatalog.LegacyDefaultBackgroundPath, StringComparison.OrdinalIgnoreCase)
                ? defaults.BackgroundPath
                : settings.BackgroundPath,
            LeftSidePath = MigrateSidePath(settings.LeftSidePath),
            RightSidePath = MigrateSidePath(settings.RightSidePath)
        };
    }

    private static string MigrateSidePath(string path)
    {
        var legacySide = Path.Combine(AssetCatalog.LegacyAssetRoot, "Side");
        return path.StartsWith(legacySide + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(CoverSettings.DefaultAssetRoot, "Side", Path.GetFileName(path))
            : path;
    }

    public void Save(CoverSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, _jsonOptions));
        File.Move(temporaryPath, SettingsPath, true);
    }

    private void TryBackupInvalidSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var backup = SettingsPath + ".invalid-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            File.Copy(SettingsPath, backup, false);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
