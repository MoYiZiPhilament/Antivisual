namespace Antivisual.Models;

/// <summary>
/// Immutable input for a cover render. Use C# <c>with</c> expressions to create variations.
/// </summary>
public sealed record CoverSettings
{
    public const string DefaultBackgroundPath =
        @"E:\Arcade-Alpha_v1.11.3_b2026072500\Backgrounds\megarex_signal.jpg";

    public const string DefaultAssetRoot =
        @"C:\Users\1\Documents\TempContent\Antivisual";

    public string TrackTitle { get; init; } = "Nutshell Princess";

    public string TrackArtist { get; init; } = "Aurora Sky";

    public string BackgroundPath { get; init; } = DefaultBackgroundPath;

    public string CoverPath { get; init; } = string.Empty;

    public string LeftSidePath { get; init; } =
        $@"{DefaultAssetRoot}\Side\lacrymira.png";

    public string RightSidePath { get; init; } =
        $@"{DefaultAssetRoot}\Side\nell.png";

    public string TextBackgroundPath { get; init; } =
        $@"{DefaultAssetRoot}\Text\TextBG_1.png";

    public bool SideDecorationsEnabled { get; init; } = true;

    public string FontFamily { get; init; } = "Felix Titling";

    /// <summary>
    /// CSS-style hexadecimal RGB/RGBA color used by the text satin pass.
    /// Invalid values fall back to #2300A5.
    /// </summary>
    public string GlossColor { get; init; } = "#2300A5";

    public bool IsWidePreview { get; init; } = true;

    public static CoverSettings Default => new();
}
