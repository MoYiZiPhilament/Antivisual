using System.Windows;
using Antivisual.Models;
using Antivisual.Rendering;
using Antivisual.ViewModels;
using SkiaSharp.Views.Desktop;

namespace Antivisual;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly CoverRenderer _renderer = new();

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.PreviewInvalidated += (_, _) => Dispatcher.BeginInvoke(PreviewCanvas.InvalidateVisual);
        Loaded += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            SettingsScroller.ScrollToTop();
            SettingsScroller.Focus();
        });
        Closed += (_, _) =>
        {
            _viewModel.SaveNow();
            _renderer.Dispose();
        };
    }

    private void PreviewCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        var aspect = _viewModel.IsWidePreview ? CoverAspect.SixteenByNine : CoverAspect.FourByThree;
        var layout = CoverLayout.For(aspect);
        var scale = Math.Min(e.Info.Width / (float)layout.Width, e.Info.Height / (float)layout.Height);
        var targetWidth = Math.Max(1, (int)(layout.Width * scale));
        var targetHeight = CoverRenderer.GetOutputSize(aspect, targetWidth).Height;
        var offsetX = (e.Info.Width - targetWidth) * 0.5f;
        var offsetY = (e.Info.Height - targetHeight) * 0.5f;
        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        _renderer.Render(canvas, _viewModel.CurrentSettings, aspect, targetWidth);
        canvas.Restore();
    }
}
