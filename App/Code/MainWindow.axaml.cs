using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageViewer.Models;
using ImageViewer.Services;

namespace ImageViewer;

public partial class MainWindow : Window
{
    private const double WheelZoomStep = 1.10;

    private readonly ImageDirectoryService _directoryService = new();
    private readonly ImageLayoutService _layoutService = new();
    private readonly ImageZoomService _zoomService = new();
    private readonly ImageViewerState _state = new();
    private readonly DispatcherTimer _zoomIndicatorTimer;
    private readonly DispatcherTimer _fullscreenChromeTimer;
    private Bitmap? _currentBitmap;
    private bool _isFullscreen;

    public MainWindow()
        : this(null)
    {
    }

    public MainWindow(string[]? args)
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        _zoomIndicatorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _zoomIndicatorTimer.Tick += (_, _) =>
        {
            _zoomIndicatorTimer.Stop();
            ZoomIndicator.IsVisible = false;
        };

        _fullscreenChromeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _fullscreenChromeTimer.Tick += (_, _) => HideChromeIfFullscreen();

        OpenButton.Click += OpenButtonClick;
        PreviousButton.Click += (_, _) => NavigateBy(-1);
        NextButton.Click += (_, _) => NavigateBy(1);
        FitButton.Click += (_, _) => SetFitMode();
        ActualSizeButton.Click += (_, _) => SetActualSizeMode();
        RotateLeftButton.Click += (_, _) => RotateBy(-90);
        RotateRightButton.Click += (_, _) => RotateBy(90);
        FullscreenButton.Click += (_, _) => ToggleFullscreen();

        ViewerHost.SizeChanged += (_, _) =>
        {
            if (_state.IsFitMode)
            {
                SetFitMode(showIndicator: false);
            }
            else
            {
                ApplyImageLayout();
            }
        };
        ViewerHost.PointerWheelChanged += ViewerHostPointerWheelChanged;
        ViewerHost.PointerPressed += ViewerHostPointerPressed;
        ViewerHost.PointerMoved += ViewerHostPointerMoved;
        ViewerHost.PointerReleased += ViewerHostPointerReleased;
        ViewerHost.PointerCaptureLost += (_, _) => EndPan();
        ViewerHost.DoubleTapped += (_, _) => ToggleActualSize();
        ViewerHost.PointerMoved += (_, _) => ShowChromeIfFullscreen();
        KeyDown += MainWindowKeyDown;

        UpdateEmptyState();

        var startupPath = args?.FirstOrDefault(argument => !string.IsNullOrWhiteSpace(argument));
        if (!string.IsNullOrWhiteSpace(startupPath))
        {
            _ = LoadFromPathAsync(startupPath);
        }
    }

    public void OpenFromExternalInstance(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Activate();
            return;
        }

        Dispatcher.UIThread.Post(async () =>
        {
            Activate();
            await LoadFromPathAsync(filePath);
        });
    }

    private async void OpenButtonClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "打开图片",
            FileTypeFilter =
            [
                new FilePickerFileType("图片文件")
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp", "*.tif", "*.tiff"]
                },
                FilePickerFileTypes.All
            ]
        });

        var filePath = files.Count > 0 ? files[0].Path.LocalPath : null;
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            await LoadFromPathAsync(filePath);
        }
    }

    private Task LoadFromPathAsync(string filePath)
    {
        LoadFromPath(filePath);
        return Task.CompletedTask;
    }

    private void LoadFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            ShowError("不支持的图片格式", isCorrupt: false);
            return;
        }

        if (!_directoryService.IsSupportedImagePath(filePath))
        {
            ShowError("不支持的图片格式", isCorrupt: false);
            return;
        }

        _state.ImagePaths = _directoryService.GetImagesInSameDirectory(filePath);
        var currentIndex = _state.ImagePaths
            .Select((path, index) => new { path, index })
            .FirstOrDefault(item => string.Equals(Path.GetFullPath(item.path), Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
            ?.index ?? 0;

        LoadImageAt(currentIndex);
    }

    private void LoadImageAt(int index)
    {
        if (index < 0 || index >= _state.ImagePaths.Count)
        {
            return;
        }

        var filePath = _state.ImagePaths[index];
        try
        {
            var bitmap = new Bitmap(filePath);
            _currentBitmap?.Dispose();
            _currentBitmap = bitmap;
            _state.CurrentIndex = index;

            var fileInfo = new FileInfo(filePath);
            _state.CurrentInfo = new ImageFileInfo(
                filePath,
                Path.GetFileName(filePath),
                fileInfo.Length,
                bitmap.PixelSize.Width,
                bitmap.PixelSize.Height,
                _directoryService.GetFormatName(filePath));

            ViewerImage.Source = bitmap;
            ViewerImage.IsVisible = true;
            EmptyStatePanel.IsVisible = false;
            ErrorStatePanel.IsVisible = false;
            _state.RotationDegrees = 0;
            _state.PanOffset = default;
            SetFitMode(showIndicator: false);
            UpdateTitleAndStatus();
        }
        catch (NotSupportedException)
        {
            ShowError("不支持的图片格式", isCorrupt: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ShowError("文件可能已损坏", isCorrupt: true);
        }
    }

    private void UpdateEmptyState()
    {
        ClearImageState();
        Title = "图片查看器";
        ViewerImage.IsVisible = false;
        EmptyStatePanel.IsVisible = true;
        ErrorStatePanel.IsVisible = false;
        StatusText.Text = "未打开图片";
        ZoomStatusText.Text = string.Empty;
        ToolbarFileNameText.Text = string.Empty;
        UpdateNavigationButtons();
    }

    private void ShowError(string message, bool isCorrupt)
    {
        ClearImageState();
        ViewerImage.IsVisible = false;
        EmptyStatePanel.IsVisible = false;
        ErrorStatePanel.IsVisible = true;
        ErrorMessageText.Text = message;
        ErrorIconText.Foreground = SolidColorBrush.Parse(isCorrupt ? "#D4A040" : "#E05555");
        StatusText.Text = message;
        ZoomStatusText.Text = string.Empty;
        ToolbarFileNameText.Text = string.Empty;
        Title = "无法打开此文件 - 图片查看器";
        UpdateNavigationButtons();
    }

    private void ClearImageState()
    {
        _currentBitmap?.Dispose();
        _currentBitmap = null;
        _state.ResetImageState();
        EndPan();
        ViewerImage.Source = null;
        ViewerImage.Width = double.NaN;
        ViewerImage.Height = double.NaN;
        ViewerImage.RenderTransform = null;
        Canvas.SetLeft(ViewerImage, 0);
        Canvas.SetTop(ViewerImage, 0);
        ZoomIndicator.IsVisible = false;
        _zoomIndicatorTimer.Stop();
    }

    private void UpdateTitleAndStatus()
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        Title = $"{_state.CurrentInfo.FileName} - 图片查看器";
        ToolbarFileNameText.Text = _state.CurrentInfo.FileName;

        var indexText = _state.ImagePaths.Count > 1 ? $"第 {_state.CurrentIndex + 1}/{_state.ImagePaths.Count} 张 │ " : string.Empty;
        StatusText.Text = string.Create(
            CultureInfo.CurrentCulture,
            $"{indexText}{_state.CurrentInfo.FileName} │ {_state.CurrentInfo.PixelWidth} × {_state.CurrentInfo.PixelHeight} │ {FormatFileSize(_state.CurrentInfo.FileSizeBytes)} │ {_state.CurrentInfo.Format}");

        var rotationText = _state.RotationDegrees == 0 ? string.Empty : $" ↻{_state.RotationDegrees}°";
        ZoomStatusText.Text = _state.IsFitMode
            ? $"适应窗口{rotationText}"
            : $"{_state.Zoom:P0}{rotationText}";

        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        var hasImage = _state.CurrentInfo is not null;
        PreviousButton.IsEnabled = hasImage && _state.CurrentIndex > 0;
        NextButton.IsEnabled = hasImage && _state.CurrentIndex >= 0 && _state.CurrentIndex < _state.ImagePaths.Count - 1;
        FitButton.IsEnabled = hasImage;
        ActualSizeButton.IsEnabled = hasImage;
        RotateLeftButton.IsEnabled = hasImage;
        RotateRightButton.IsEnabled = hasImage;
        FullscreenButton.IsEnabled = true;
    }

    private void NavigateBy(int direction)
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        var nextIndex = _state.CurrentIndex + direction;
        if (nextIndex < 0 || nextIndex >= _state.ImagePaths.Count)
        {
            return;
        }

        LoadImageAt(nextIndex);
    }

    private void NavigateToBoundary(bool first)
    {
        if (_state.CurrentInfo is null || _state.ImagePaths.Count == 0)
        {
            return;
        }

        var targetIndex = first ? 0 : _state.ImagePaths.Count - 1;
        if (targetIndex != _state.CurrentIndex)
        {
            LoadImageAt(targetIndex);
        }
    }

    private void SetFitMode(bool showIndicator = true)
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        _state.FitZoom = CalculateFitZoom();
        _state.Zoom = _state.FitZoom;
        _state.IsFitMode = true;
        _state.PanOffset = default;
        ApplyImageLayout();
        UpdateTitleAndStatus();

        if (showIndicator)
        {
            ShowZoomIndicator("适应窗口");
        }
    }

    private void SetActualSizeMode()
    {
        SetZoom(1, keepCenter: true);
        _state.IsFitMode = false;
        ShowZoomIndicator("100%");
    }

    private void ToggleActualSize()
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        if (_state.IsFitMode)
        {
            SetActualSizeMode();
        }
        else
        {
            SetFitMode();
        }
    }

    private void RotateBy(int degrees)
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        _state.RotationDegrees = ((_state.RotationDegrees + degrees) % 360 + 360) % 360;
        if (_state.IsFitMode)
        {
            SetFitMode(showIndicator: false);
        }
        else
        {
            ApplyImageLayout();
        }

        UpdateTitleAndStatus();
    }

    private void SetZoom(double zoom, bool keepCenter)
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        var oldSize = GetRotatedBoundsSize(_state.Zoom);
        var newZoom = _zoomService.ClampZoom(zoom);
        var newSize = GetRotatedBoundsSize(newZoom);

        if (keepCenter && oldSize.Width > 0 && oldSize.Height > 0)
        {
            var viewportCenter = new Point(ViewerHost.Bounds.Width / 2, ViewerHost.Bounds.Height / 2);
            var imageCenter = new Point(ViewerHost.Bounds.Width / 2 + _state.PanOffset.X, ViewerHost.Bounds.Height / 2 + _state.PanOffset.Y);
            var relativeX = (viewportCenter.X - imageCenter.X) / oldSize.Width;
            var relativeY = (viewportCenter.Y - imageCenter.Y) / oldSize.Height;
            _state.PanOffset = new Point(
                viewportCenter.X - ViewerHost.Bounds.Width / 2 - relativeX * newSize.Width,
                viewportCenter.Y - ViewerHost.Bounds.Height / 2 - relativeY * newSize.Height);
        }

        _state.Zoom = newZoom;
        _state.IsFitMode = false;
        ApplyImageLayout();
        UpdateTitleAndStatus();
    }

    private void ZoomAt(double factor, Point pointerPosition)
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        var oldSize = GetRotatedBoundsSize(_state.Zoom);
        if (oldSize.Width <= 0 || oldSize.Height <= 0)
        {
            return;
        }

        var imageCenter = new Point(ViewerHost.Bounds.Width / 2 + _state.PanOffset.X, ViewerHost.Bounds.Height / 2 + _state.PanOffset.Y);
        var relativeX = (pointerPosition.X - imageCenter.X) / oldSize.Width;
        var relativeY = (pointerPosition.Y - imageCenter.Y) / oldSize.Height;
        _state.Zoom = _zoomService.ClampZoom(_state.Zoom * factor);
        _state.IsFitMode = false;

        var newSize = GetRotatedBoundsSize(_state.Zoom);
        _state.PanOffset = new Point(
            pointerPosition.X - ViewerHost.Bounds.Width / 2 - relativeX * newSize.Width,
            pointerPosition.Y - ViewerHost.Bounds.Height / 2 - relativeY * newSize.Height);

        ApplyImageLayout();
        UpdateTitleAndStatus();
        ShowZoomIndicator(_state.Zoom.ToString("P0", CultureInfo.CurrentCulture));
    }

    private void ApplyImageLayout()
    {
        if (_state.CurrentInfo is null || _currentBitmap is null)
        {
            return;
        }

        ConstrainPan();
        var layout = _layoutService.CalculateLayout(
            ViewerHost.Bounds.Size,
            GetImagePixelSize(),
            _state.Zoom,
            _state.RotationDegrees,
            _state.PanOffset);

        ViewerImage.Width = layout.ImageSize.Width;
        ViewerImage.Height = layout.ImageSize.Height;
        ViewerImage.RenderTransform = new RotateTransform(_state.RotationDegrees);
        Canvas.SetLeft(ViewerImage, layout.CanvasPosition.X);
        Canvas.SetTop(ViewerImage, layout.CanvasPosition.Y);
    }

    private double CalculateFitZoom()
    {
        if (_state.CurrentInfo is null)
        {
            return 1;
        }

        return _zoomService.CalculateFitZoom(ViewerHost.Bounds.Size, GetImagePixelSize(), _state.RotationDegrees);
    }

    private Size GetUnrotatedImageSize(double zoom)
    {
        return _zoomService.GetUnrotatedImageSize(GetImagePixelSize(), zoom);
    }

    private Size GetRotatedBoundsSize(double zoom)
    {
        return _zoomService.GetRotatedBoundsSize(GetImagePixelSize(), zoom, _state.RotationDegrees);
    }

    private Size GetImagePixelSize()
    {
        return _state.CurrentInfo is null
            ? default
            : new Size(_state.CurrentInfo.PixelWidth, _state.CurrentInfo.PixelHeight);
    }

    private void ShowZoomIndicator(string text)
    {
        ZoomIndicatorText.Text = text;
        ZoomIndicator.IsVisible = true;
        _zoomIndicatorTimer.Stop();
        _zoomIndicatorTimer.Start();
    }

    private void ViewerHostPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_state.CurrentInfo is null)
        {
            return;
        }

        var keyModifiers = e.KeyModifiers;
        if ((keyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
        {
            ZoomAt(e.Delta.Y > 0 ? WheelZoomStep : 1 / WheelZoomStep, e.GetPosition(ViewerHost));
            e.Handled = true;
            return;
        }

        NavigateBy(e.Delta.Y < 0 ? 1 : -1);
        e.Handled = true;
    }

    private void ToggleFullscreen()
    {
        _isFullscreen = !_isFullscreen;
        WindowState = _isFullscreen ? WindowState.FullScreen : WindowState.Normal;
        FullscreenButton.Content = _isFullscreen ? "退出全屏" : "全屏";
        ViewerHost.Background = SolidColorBrush.Parse(_isFullscreen ? "#000000" : "#1A1A1A");
        ImageCanvas.Background = Brushes.Transparent;

        if (_isFullscreen)
        {
            ShowChromeIfFullscreen();
        }
        else
        {
            _fullscreenChromeTimer.Stop();
            TopToolbar.IsVisible = true;
            StatusBar.IsVisible = true;
        }

        if (_state.IsFitMode)
        {
            SetFitMode(showIndicator: false);
        }
    }

    private void ShowChromeIfFullscreen()
    {
        if (!_isFullscreen)
        {
            return;
        }

        TopToolbar.IsVisible = true;
        StatusBar.IsVisible = true;
        _fullscreenChromeTimer.Stop();
        _fullscreenChromeTimer.Start();
    }

    private void HideChromeIfFullscreen()
    {
        _fullscreenChromeTimer.Stop();
        if (!_isFullscreen)
        {
            return;
        }

        TopToolbar.IsVisible = false;
        StatusBar.IsVisible = false;
    }

    private void ViewerHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_state.CurrentInfo is null || !CanPan())
        {
            return;
        }

        var point = e.GetCurrentPoint(ViewerHost);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _state.LastPanPoint = point.Position;
        ViewerHost.Cursor = new Cursor(StandardCursorType.Hand);
        e.Pointer.Capture(ViewerHost);
        e.Handled = true;
    }

    private void ViewerHostPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_state.LastPanPoint is null)
        {
            return;
        }

        var currentPoint = e.GetCurrentPoint(ViewerHost);
        if (!currentPoint.Properties.IsLeftButtonPressed)
        {
            EndPan();
            return;
        }

        var currentPosition = currentPoint.Position;
        var delta = currentPosition - _state.LastPanPoint.Value;
        _state.LastPanPoint = currentPosition;
        _state.PanOffset = new Point(_state.PanOffset.X + delta.X, _state.PanOffset.Y + delta.Y);
        ApplyImageLayout();
        e.Handled = true;
    }

    private void ViewerHostPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndPan();
        e.Pointer.Capture(null);
    }

    private void EndPan()
    {
        _state.LastPanPoint = null;
        ViewerHost.Cursor = null;
    }

    private bool CanPan()
    {
        var size = GetRotatedBoundsSize(_state.Zoom);
        return size.Width > ViewerHost.Bounds.Width || size.Height > ViewerHost.Bounds.Height;
    }

    private void ConstrainPan()
    {
        var size = GetRotatedBoundsSize(_state.Zoom);
        var maxX = Math.Max(0, (size.Width - ViewerHost.Bounds.Width) / 2);
        var maxY = Math.Max(0, (size.Height - ViewerHost.Bounds.Height) / 2);
        _state.PanOffset = new Point(
            Math.Clamp(_state.PanOffset.X, -maxX, maxX),
            Math.Clamp(_state.PanOffset.Y, -maxY, maxY));
    }

    private void MainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.O:
                    OpenButtonClick(OpenButton, e);
                    e.Handled = true;
                    return;
                case Key.D0:
                case Key.NumPad0:
                    SetFitMode();
                    e.Handled = true;
                    return;
                case Key.D1:
                case Key.NumPad1:
                case Key.D9:
                case Key.NumPad9:
                    SetActualSizeMode();
                    e.Handled = true;
                    return;
                case Key.Add:
                case Key.OemPlus:
                    if (_state.CurrentInfo is not null)
                    {
                        SetZoom(_state.Zoom * WheelZoomStep, keepCenter: true);
                        ShowZoomIndicator(_state.Zoom.ToString("P0", CultureInfo.CurrentCulture));
                    }

                    e.Handled = true;
                    return;
                case Key.Subtract:
                case Key.OemMinus:
                    if (_state.CurrentInfo is not null)
                    {
                        SetZoom(_state.Zoom / WheelZoomStep, keepCenter: true);
                        ShowZoomIndicator(_state.Zoom.ToString("P0", CultureInfo.CurrentCulture));
                    }

                    e.Handled = true;
                    return;
                case Key.OemComma:
                    RotateBy(-90);
                    e.Handled = true;
                    return;
                case Key.OemPeriod:
                    RotateBy(90);
                    e.Handled = true;
                    return;
                case Key.Q:
                    Close();
                    e.Handled = true;
                    return;
            }
        }

        switch (e.Key)
        {
            case Key.F11:
            case Key.F:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.Escape when _isFullscreen:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.Left:
                NavigateBy(-1);
                e.Handled = true;
                break;
            case Key.Right:
                NavigateBy(1);
                e.Handled = true;
                break;
            case Key.Home:
                NavigateToBoundary(first: true);
                e.Handled = true;
                break;
            case Key.End:
                NavigateToBoundary(first: false);
                e.Handled = true;
                break;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
    }
}