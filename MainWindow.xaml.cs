using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Antivisual.Models;
using Antivisual.Rendering;
using Antivisual.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace Antivisual;

public partial class MainWindow : Window
{
    private static readonly SKSamplingOptions ResizeSampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private readonly MainViewModel _viewModel;
    private readonly CoverRenderer _renderer = new();
    private readonly DispatcherTimer _resizeTimer;
    private SKImage? _cachedPreview;
    private Border? _resizeBorder;
    private Point _resizeStartPointer;
    private double _resizeStartLeft;
    private double _resizeStartTop;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private int _resizeHitTest;
    private bool _isResizing;
    private bool _hasRendered;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _resizeTimer.Tick += (_, _) =>
        {
            _resizeTimer.Stop();
            _isResizing = false;
            PreviewCanvas.InvalidateVisual();
        };

        _viewModel.PreviewInvalidated += (_, _) => Dispatcher.BeginInvoke(PreviewCanvas.InvalidateVisual);
        PreviewCanvas.SizeChanged += (_, _) => BeginResize();
        Loaded += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            SettingsScroller.ScrollToTop();
            SettingsScroller.Focus();
            UpdateWindowFrame();
        });
        StateChanged += (_, _) => UpdateWindowFrame();
        Closed += (_, _) =>
        {
            _resizeTimer.Stop();
            _viewModel.SaveNow();
            _cachedPreview?.Dispose();
            _renderer.Dispose();
        };
    }

    private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed && WindowState != WindowState.Maximized)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleWindowState();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleWindowState()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            return;
        }

        MaxWidth = SystemParameters.WorkArea.Width;
        MaxHeight = SystemParameters.WorkArea.Height;
        WindowState = WindowState.Maximized;
    }

    private void WindowFrame_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateWindowFrame();

    private void UpdateWindowFrame()
    {
        var maximized = WindowState == WindowState.Maximized;
        var radius = maximized ? 0 : 12;
        WindowFrame.CornerRadius = new CornerRadius(radius);
        WindowOutline.CornerRadius = new CornerRadius(radius);
        WindowOutline.BorderThickness = new Thickness(maximized ? 0 : 1);
        WindowClip.RadiusX = radius;
        WindowClip.RadiusY = radius;
        WindowClip.Rect = new Rect(0, 0, WindowFrame.ActualWidth, WindowFrame.ActualHeight);
    }

    private void ResizeBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || WindowState == WindowState.Maximized ||
            sender is not Border border || !int.TryParse(border.Tag?.ToString(), out var hitTest))
            return;

        e.Handled = true;
        _resizeBorder = border;
        _resizeHitTest = hitTest;
        _resizeStartPointer = GetLogicalCursorPosition();
        _resizeStartLeft = Left;
        _resizeStartTop = Top;
        _resizeStartWidth = Width;
        _resizeStartHeight = Height;
        border.CaptureMouse();
        BeginResize();
    }

    private void WindowRoot_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_resizeBorder is null || e.LeftButton != MouseButtonState.Pressed) return;

        var pointer = GetLogicalCursorPosition();
        var deltaX = pointer.X - _resizeStartPointer.X;
        var deltaY = pointer.Y - _resizeStartPointer.Y;
        var fromLeft = _resizeHitTest is 10 or 13 or 16;
        var fromRight = _resizeHitTest is 11 or 14 or 17;
        var fromTop = _resizeHitTest is 12 or 13 or 14;
        var fromBottom = _resizeHitTest is 15 or 16 or 17;

        if (fromLeft || fromRight)
        {
            var width = Math.Max(MinWidth, _resizeStartWidth + (fromLeft ? -deltaX : deltaX));
            if (fromLeft) Left = _resizeStartLeft + _resizeStartWidth - width;
            Width = width;
        }

        if (fromTop || fromBottom)
        {
            var height = Math.Max(MinHeight, _resizeStartHeight + (fromTop ? -deltaY : deltaY));
            if (fromTop) Top = _resizeStartTop + _resizeStartHeight - height;
            Height = height;
        }

        e.Handled = true;
    }

    private void WindowRoot_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_resizeBorder is null) return;
        _resizeBorder.ReleaseMouseCapture();
        _resizeBorder = null;
        _resizeTimer.Stop();
        _resizeTimer.Start();
        e.Handled = true;
    }

    private Point GetLogicalCursorPosition()
    {
        var position = System.Windows.Forms.Cursor.Position;
        var devicePoint = new Point(position.X, position.Y);
        return PresentationSource.FromVisual(this)?.CompositionTarget is { } target
            ? target.TransformFromDevice.Transform(devicePoint)
            : devicePoint;
    }

    private void BeginResize()
    {
        if (!_hasRendered) return;
        _isResizing = true;
        _resizeTimer.Stop();
        _resizeTimer.Start();
    }

    private void PreviewCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_isResizing && _cachedPreview is not null)
        {
            canvas.DrawImage(_cachedPreview, new SKRect(0, 0, e.Info.Width, e.Info.Height), ResizeSampling);
            return;
        }

        var aspect = _viewModel.IsWidePreview ? CoverAspect.SixteenByNine : CoverAspect.FourByThree;
        var layout = CoverLayout.For(aspect);
        var scale = Math.Min(e.Info.Width / (float)layout.Width, e.Info.Height / (float)layout.Height);
        var targetWidth = Math.Max(1, (int)(layout.Width * scale));
        var targetHeight = CoverRenderer.GetOutputSize(aspect, targetWidth).Height;
        var offsetX = (e.Info.Width - targetWidth) * 0.5f;
        var offsetY = (e.Info.Height - targetHeight) * 0.5f;

        canvas.Save();
        try
        {
            canvas.Translate(offsetX, offsetY);
            _renderer.Render(canvas, _viewModel.CurrentSettings, aspect, targetWidth);
        }
        finally
        {
            canvas.Restore();
        }

        var snapshot = e.Surface.Snapshot();
        _cachedPreview?.Dispose();
        _cachedPreview = snapshot;
        _hasRendered = true;
    }

}
