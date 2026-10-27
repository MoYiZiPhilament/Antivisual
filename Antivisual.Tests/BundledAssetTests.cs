using Antivisual.Models;
using Antivisual.Services;

namespace Antivisual.Tests;

public sealed class BundledAssetTests
{
    [Fact]
    public void BundledAssets_AreAvailableAndTextLayerIsFixed()
    {
        var settings = CoverSettings.Default;
        var textBackground = CoverSettings.DefaultTextBackgroundPath;
        Assert.True(File.Exists(settings.BackgroundPath));
        Assert.True(File.Exists(settings.LeftSidePath));
        Assert.True(File.Exists(settings.RightSidePath));
        Assert.True(File.Exists(textBackground));
        Assert.Equal("TextBG_6.png", Path.GetFileName(textBackground));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(textBackground)!, "*.png"));
    }

    [Fact]
    public void LegacySettings_MoveToBundledAssetsAndPreserveCover()
    {
        var legacy = CoverSettings.Default with
        {
            BackgroundPath = AssetCatalog.LegacyDefaultBackgroundPath,
            CoverPath = @"D:\Custom\cover.png",
            LeftSidePath = AssetCatalog.LegacyAssetRoot + @"\Side\lacrymira.png",
            RightSidePath = AssetCatalog.LegacyAssetRoot + @"\Side\nell.png"
        };

        var migrated = SettingsService.MigrateToBundledAssets(legacy);
        var defaults = CoverSettings.Default;
        Assert.Equal(defaults.BackgroundPath, migrated.BackgroundPath);
        Assert.Equal(defaults.LeftSidePath, migrated.LeftSidePath);
        Assert.Equal(defaults.RightSidePath, migrated.RightSidePath);
        Assert.Equal(legacy.CoverPath, migrated.CoverPath);
    }
}
