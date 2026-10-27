using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Antivisual.Models;
using Antivisual.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Antivisual.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly string DefaultBackground = AssetCatalog.DefaultBackgroundPath;
    private readonly SettingsService _settingsService;
    private readonly ExportService _exportService;
    private CancellationTokenSource? _saveCancellation;
    private bool _initialized;

    [ObservableProperty] private string trackTitle = "Nutshell Princess";
    [ObservableProperty] private string trackArtist = "Aurora Sky";
    [ObservableProperty] private string backgroundPath = DefaultBackground;
    [ObservableProperty] private string coverPath = string.Empty;
    [ObservableProperty] private string leftSidePath = string.Empty;
    [ObservableProperty] private string rightSidePath = string.Empty;
    [ObservableProperty] private string textBackgroundPath = string.Empty;
    [ObservableProperty] private bool sideDecorationsEnabled = true;
    [ObservableProperty] private string fontFamily = "Felix Titling";
    [ObservableProperty] private string glossColor = "#2300A5";
    [ObservableProperty] private bool isWidePreview = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = "就绪 · 修改会实时应用到预览";

    public MainViewModel() : this(new SettingsService(), new ExportService()) { }

    public MainViewModel(SettingsService settingsService, ExportService exportService)
    {
        _settingsService = settingsService;
        _exportService = exportService;
        FontFamilies = new ObservableCollection<string>(
            Fonts.SystemFontFamilies.Select(font => font.Source).OrderBy(name => name, StringComparer.CurrentCulture));
        SideOptions = new ObservableCollection<AssetItem>();
        TextBackgroundOptions = new ObservableCollection<AssetItem>();
        OutputDirectory = ExportService.GetOutputDirectory();

        var saved = _settingsService.Load();
        trackTitle = saved.TrackTitle;
        trackArtist = saved.TrackArtist;
        backgroundPath = saved.BackgroundPath;
        coverPath = saved.CoverPath;
        leftSidePath = saved.LeftSidePath;
        rightSidePath = saved.RightSidePath;
        textBackgroundPath = saved.TextBackgroundPath;
        sideDecorationsEnabled = saved.SideDecorationsEnabled;
        fontFamily = saved.FontFamily;
        glossColor = saved.GlossColor;
        isWidePreview = saved.IsWidePreview;
        RefreshAssets();
        _initialized = true;
    }

    public event EventHandler? PreviewInvalidated;

    public ObservableCollection<string> FontFamilies { get; }
    public ObservableCollection<AssetItem> SideOptions { get; }
    public ObservableCollection<AssetItem> TextBackgroundOptions { get; }
    public string OutputDirectory { get; }
    public string BackgroundDisplayName => AssetCatalog.DisplayName(BackgroundPath, "未选择背景");
    public string CoverDisplayName => AssetCatalog.DisplayName(CoverPath, "未上传封面");
    public string PreviewResolution => IsWidePreview ? "1920 × 1080 · PNG" : "1920 × 1440 · PNG";

    public bool IsStandardPreview
    {
        get => !IsWidePreview;
        set { if (value) IsWidePreview = false; }
    }

    public CoverSettings CurrentSettings => CoverSettings.Default with
    {
        TrackTitle = TrackTitle,
        TrackArtist = TrackArtist,
        BackgroundPath = BackgroundPath,
        CoverPath = CoverPath,
        LeftSidePath = LeftSidePath,
        RightSidePath = RightSidePath,
        TextBackgroundPath = TextBackgroundPath,
        SideDecorationsEnabled = SideDecorationsEnabled,
        FontFamily = FontFamily,
        GlossColor = GlossColor,
        IsWidePreview = IsWidePreview
    };

    [RelayCommand]
    private void BrowseBackground()
    {
        var path = SelectImage(Path.GetDirectoryName(BackgroundPath));
        if (path is not null) BackgroundPath = path;
    }

    [RelayCommand]
    private void ResetBackground() => BackgroundPath = DefaultBackground;

    [RelayCommand]
    private void BrowseCover()
    {
        var path = SelectImage(Path.GetDirectoryName(CoverPath));
        if (path is not null) CoverPath = path;
    }

    [RelayCommand]
    private void RefreshAssets()
    {
        ReplaceItems(SideOptions, AssetCatalog.Enumerate(AssetCatalog.SideDirectory));
        ReplaceItems(TextBackgroundOptions, AssetCatalog.Enumerate(AssetCatalog.TextBackgroundDirectory));

        if (SideOptions.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(LeftSidePath)) LeftSidePath = SideOptions[0].Path;
            if (string.IsNullOrWhiteSpace(RightSidePath)) RightSidePath = SideOptions[^1].Path;
        }
        if (TextBackgroundOptions.Count > 0 && string.IsNullOrWhiteSpace(TextBackgroundPath))
            TextBackgroundPath = (TextBackgroundOptions.FirstOrDefault(item => item.Name.Equals("TextBG_6.png", StringComparison.OrdinalIgnoreCase)) ?? TextBackgroundOptions[0]).Path;

        if (!_initialized) return;
        StatusMessage = SideOptions.Count + TextBackgroundOptions.Count > 0
            ? $"已载入 {SideOptions.Count} 个侧饰与 {TextBackgroundOptions.Count} 个文字底图"
            : "未找到装饰素材，可在素材目录添加 PNG 后重新扫描";
        Touch();
    }

    [RelayCommand]
    private void SetPreviewAspect(string aspect) => IsWidePreview = aspect.Equals("wide", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private void SelectColor(string color) => GlossColor = color;

    [RelayCommand]
    private void ChooseColor()
    {
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
        try { dialog.Color = System.Drawing.ColorTranslator.FromHtml(GlossColor); }
        catch (ArgumentException) { }
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            GlossColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        IsBusy = true;
        StatusMessage = "正在生成两种比例的 PNG…";
        try
        {
            SaveNow();
            var files = await _exportService.ExportBothAsync(CurrentSettings);
            StatusMessage = $"导出完成：{Path.GetFileName(files[0])}、{Path.GetFileName(files[1])}";
        }
        catch (Exception ex)
        {
            StatusMessage = "导出失败：" + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExport() => !IsBusy;

    [RelayCommand]
    private void OpenOutputFolder()
    {
        Directory.CreateDirectory(OutputDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", OutputDirectory) { UseShellExecute = true });
    }

    public void SaveNow()
    {
        _saveCancellation?.Cancel();
        if (!_initialized) return;
        _settingsService.Save(CurrentSettings);
    }

    private static string? SelectImage(string? initialDirectory)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.webp;*.bmp|所有文件|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (Directory.Exists(initialDirectory)) dialog.InitialDirectory = initialDirectory;
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void ReplaceItems(ObservableCollection<AssetItem> target, IEnumerable<AssetItem> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }

    private void Touch()
    {
        if (!_initialized) return;
        OnPropertyChanged(nameof(CurrentSettings));
        PreviewInvalidated?.Invoke(this, EventArgs.Empty);
        _saveCancellation?.Cancel();
        _saveCancellation = new CancellationTokenSource();
        _ = SaveAfterDelayAsync(_saveCancellation.Token);
    }

    private async Task SaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(600, token);
            await Task.Run(() => _settingsService.Save(CurrentSettings), token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusMessage = "设置保存失败：" + ex.Message;
        }
    }

    partial void OnTrackTitleChanged(string value) => Touch();
    partial void OnTrackArtistChanged(string value) => Touch();
    partial void OnCoverPathChanged(string value) { OnPropertyChanged(nameof(CoverDisplayName)); Touch(); }
    partial void OnBackgroundPathChanged(string value) { OnPropertyChanged(nameof(BackgroundDisplayName)); Touch(); }
    partial void OnLeftSidePathChanged(string value) => Touch();
    partial void OnRightSidePathChanged(string value) => Touch();
    partial void OnTextBackgroundPathChanged(string value) => Touch();
    partial void OnSideDecorationsEnabledChanged(bool value) => Touch();
    partial void OnFontFamilyChanged(string value) => Touch();
    partial void OnGlossColorChanged(string value) => Touch();
    partial void OnIsBusyChanged(bool value) => ExportCommand.NotifyCanExecuteChanged();
    partial void OnIsWidePreviewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStandardPreview));
        OnPropertyChanged(nameof(PreviewResolution));
        Touch();
    }
}
