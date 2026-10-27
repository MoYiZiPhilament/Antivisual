using Antivisual.Models;
using Antivisual.Services;

namespace Antivisual.Tests;

public sealed class BundledAssetTests
{
    [Fact]
    public void BundledAssets_AreAvailableAndTextLayerIsFixed()
    {
        var settings = CoverSettings.Default;
        Assert.True(File.Exists(settings.BackgroundPath));
        Assert.True(File.Exists(settings.LeftSidePath));
        Assert.True(File.Exists(settings.RightSidePath));
        Assert.True(File.Exists(settings.TextBackgroundPath));
        Assert.Equal("TextBG_6.png", Path.GetFileName(settings.TextBackgroundPath));
        Assert.Single(Directory.GetFiles(AssetCatalog.TextBackgroundDirectory, "*.png"));
    }

    [Fact]
    public void LegacySettings_MoveToBundledAssetsAndPreserveCover()
    {
        var legacy = CoverSettings.Default with
        {
            BackgroundPath = AssetCatalog.LegacyDefaultBackgroundPath,
            CoverPath = @"D:\Custom\cover.png",
            LeftSidePath = AssetCatalog.LegacyAssetRoot + @"\Side\lacrymira.png",
            RightSidePath = AssetCatalog.LegacyAssetRoot + @"\Side\nell.png",
            TextBackgroundPath = AssetCatalog.LegacyAssetRoot + @"\Text\TextBG_1.png"
        };

        var migrated = SettingsService.MigrateToBundledAssets(legacy);
        var defaults = CoverSettings.Default;
        Assert.Equal(defaults.BackgroundPath, migrated.BackgroundPath);
        Assert.Equal(defaults.LeftSidePath, migrated.LeftSidePath);
        Assert.Equal(defaults.RightSidePath, migrated.RightSidePath);
        Assert.Equal(defaults.TextBackgroundPath, migrated.TextBackgroundPath);
        Assert.Equal(legacy.CoverPath, migrated.CoverPath);
    }
}
