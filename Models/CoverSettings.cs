using System.IO;

namespace Antivisual.Models;

/// <summary>
/// Immutable input for a cover render. Use C# <c>with</c> expressions to create variations.
/// </summary>
public sealed record CoverSettings
{
    public static string DefaultAssetRoot { get; } = Path.Combine(AppContext.BaseDirectory, "Assets");

    public static string DefaultBackgroundPath { get; } = Path.Combine(
        DefaultAssetRoot, "Backgrounds", "megarex_signal.jpg");

    public string TrackTitle { get; init; } = "Nutshell Princess";

    public string TrackArtist { get; init; } = "Aurora Sky";

    public string BackgroundPath { get; init; } = DefaultBackgroundPath;

    public string CoverPath { get; init; } = string.Empty;

    public string LeftSidePath { get; init; } = Path.Combine(DefaultAssetRoot, "Side", "lacrymira.png");

    public string RightSidePath { get; init; } = Path.Combine(DefaultAssetRoot, "Side", "nell.png");

    public string TextBackgroundPath { get; init; } = Path.Combine(DefaultAssetRoot, "Text", "TextBG_6.png");

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
