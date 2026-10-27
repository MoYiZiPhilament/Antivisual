using System.IO;
using Antivisual.Models;
using Antivisual.Rendering;

namespace Antivisual.Services;

public sealed class ExportService
{
    public static string GetOutputDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Antivisual");

    public Task<IReadOnlyList<string>> ExportBothAsync(
        CoverSettings settings,
        CancellationToken cancellationToken = default)
        => ExportBothAsync(settings, GetOutputDirectory(), cancellationToken);

    public async Task<IReadOnlyList<string>> ExportBothAsync(
        CoverSettings settings,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var snapshot = settings with { };
        Directory.CreateDirectory(outputDirectory);
        using var renderer = new CoverRenderer();

        var wideTask = Task.Run(() => RenderToFile(renderer, snapshot, CoverAspect.SixteenByNine, outputDirectory), cancellationToken);
        var standardTask = Task.Run(() => RenderToFile(renderer, snapshot, CoverAspect.FourByThree, outputDirectory), cancellationToken);
        return await Task.WhenAll(wideTask, standardTask);
    }

    private static string RenderToFile(CoverRenderer renderer, CoverSettings settings, CoverAspect aspect, string outputDirectory)
    {
        var png = renderer.RenderPng(settings, aspect);
        var suffix = aspect == CoverAspect.SixteenByNine ? "16x9" : "4x3";
        var baseName = $"{SafeFileName(settings.TrackTitle)}_{suffix}";
        var path = GetAvailablePath(outputDirectory, baseName);
        var temporaryPath = path + ".tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, png);
            File.Move(temporaryPath, path, false);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
        return path;
    }

    private static string GetAvailablePath(string directory, string baseName)
    {
        var candidate = Path.Combine(directory, baseName + ".png");
        if (!File.Exists(candidate)) return candidate;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        for (var index = 0; ; index++)
        {
            var suffix = index == 0 ? string.Empty : $"_{index + 1}";
            candidate = Path.Combine(directory, $"{baseName}_{timestamp}{suffix}.png");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    public static string SafeFileName(string? value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var characters = (value ?? string.Empty).Trim()
            .Select(character => invalid.Contains(character) ? '_' : character)
            .ToArray();
        var result = new string(characters).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(result) ? "Antivisual" : result;
    }
}
