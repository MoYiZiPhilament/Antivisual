using Antivisual.Models;
using Antivisual.Rendering;
using Antivisual.Services;

namespace Antivisual.Tests;

public sealed class LayoutAndExportTests
{
    [Theory]
    [InlineData(CoverAspect.SixteenByNine, 1920, 1080, 639, 31, 642)]
    [InlineData(CoverAspect.FourByThree, 1920, 1440, 531, 60, 858)]
    public void Layout_MatchesPsd(CoverAspect aspect, int width, int height, float x, float y, float coverSize)
    {
        var layout = CoverLayout.For(aspect);
        Assert.Equal(width, layout.Width);
        Assert.Equal(height, layout.Height);
        Assert.Equal(x, layout.CoverRect.Left);
        Assert.Equal(y, layout.CoverRect.Top);
        Assert.Equal(coverSize, layout.CoverRect.Width);
        Assert.Equal(coverSize, layout.CoverRect.Height);
    }

    [Fact]
    public void TextBandCenter_IsDerivedFromTextBackground6()
    {
        Assert.Equal(864.4f, CoverLayout.SixteenByNine.TextBandCenterY, 1);
        Assert.Equal(1181.2f, CoverLayout.FourByThree.TextBandCenterY, 1);
    }

    [Theory]
    [InlineData("Nutshell Princess", "Nutshell Princess")]
    [InlineData("A/B:C*D?E", "A_B_C_D_E")]
    [InlineData("   ", "Antivisual")]
    [InlineData("name.", "name")]
    public void SafeFileName_CleansInvalidCharacters(string input, string expected)
        => Assert.Equal(expected, ExportService.SafeFileName(input));
}
