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
            return JsonSerializer.Deserialize<CoverSettings>(json, _jsonOptions) ?? CoverSettings.Default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            TryBackupInvalidSettings();
            return CoverSettings.Default;
        }
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
