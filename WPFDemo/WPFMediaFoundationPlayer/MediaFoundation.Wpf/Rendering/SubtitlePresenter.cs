using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MediaFoundation.Core;

namespace MediaFoundation.Controls;

internal sealed class SubtitlePresenter : IDisposable
{
    private readonly MediaPlayer _player;
    private readonly Grid _view;
    private readonly Border _caption;
    private readonly Grid _overlayContent = new() { ClipToBounds = true };
    private readonly TextBlock _text;
    private readonly DispatcherTimer _timer;
    private Window? _overlay;
    private Window? _owner;
    private string _currentText = "";
    private TimeSpan? _lastPosition;
    private SubtitleDocument? _lastDocument;
    private TimeSpan _lastDelay;
    private bool _lastReady;
    private bool _disposed;

    internal SubtitlePresenter(MediaPlayer player, Grid view)
    {
        _player = player;
        _view = view;
        _text = new TextBlock
        {
            Foreground = Brushes.White, TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, FontSize = 24, FontFamily = new FontFamily("Segoe UI"),
            Padding = new Thickness(10, 4, 10, 4)
        };
        _caption = new Border
        {
            Child = _text, Background = new SolidColorBrush(Color.FromArgb(185, 0, 0, 0)),
            CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        Panel.SetZIndex(_caption, 1);
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, OnTick, view.Dispatcher);
        _timer.Stop();
        view.Loaded += OnLoaded;
        view.Unloaded += OnUnloaded;
    }

    internal void Update()
    {
        if (_disposed) return;
        if (_player.Subtitle == null || !_view.IsLoaded)
        {
            _timer.Stop();
            _currentText = "";
            _lastPosition = null;
            _lastDocument = null;
            _caption.Visibility = Visibility.Collapsed;
            CloseOverlay();
            _view.Children.Remove(_caption);
            return;
        }
        _timer.Start();
        OnTick(null, EventArgs.Empty);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Update();
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        CloseOverlay();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_player.Subtitle is not { } document) return;
        try
        {
            TimeSpan position = _player.RefreshSubtitlePosition();
            if (_lastPosition != position || !ReferenceEquals(_lastDocument, document) || _lastDelay != _player.SubtitleDelay || _lastReady != (_player.HasVideo && _player.IsReady))
            {
                _lastPosition = position;
                _lastDocument = document;
                _lastDelay = _player.SubtitleDelay;
                _lastReady = _player.HasVideo && _player.IsReady;
                // Saturating subtraction keeps offsets valid at TimeSpan's endpoints.
                long ticks = position.Ticks;
                long delay = _player.SubtitleDelay.Ticks;
                long adjusted = delay > 0 && ticks < long.MinValue + delay ? long.MinValue :
                    delay < 0 && ticks > long.MaxValue + delay ? long.MaxValue : ticks - delay;
                string text = _player.HasVideo && _player.IsReady ? document.GetText(TimeSpan.FromTicks(adjusted)) : "";
                if (_currentText != text)
                {
                    _currentText = text;
                    _text.Text = text;
                }
            }
            Present();
        }
        catch (ExternalException error)
        {
            Disable(error);
        }
        catch (InvalidOperationException error)
        {
            Disable(error);
        }
    }

    private void Disable(Exception error)
    {
        _timer.Stop();
        CloseOverlay();
        _caption.Visibility = Visibility.Collapsed;
        _player.ReportSubtitleFailure(error);
    }

    private void Present()
    {
        if (!_view.IsVisible || _view.ActualWidth <= 0 || _view.ActualHeight <= 0 || string.IsNullOrEmpty(_currentText))
        {
            _caption.Visibility = Visibility.Collapsed;
            _overlay?.Hide();
            return;
        }
        double width = _view.ActualWidth, height = _view.ActualHeight;
        double videoWidth = width, videoHeight = height;
        if (_player.VideoWidth > 0 && _player.VideoHeight > 0)
        {
            double scale = Math.Min(width / _player.VideoWidth, height / _player.VideoHeight);
            videoWidth = _player.VideoWidth * scale;
            videoHeight = _player.VideoHeight * scale;
        }
        _caption.MaxWidth = videoWidth * .94;
        _caption.Margin = new Thickness(0, 0, 0, (height - videoHeight) / 2 + Math.Max(12, videoHeight * .04));
        _text.FontSize = Math.Clamp(videoHeight / 22, 16, 40);
        _caption.Visibility = Visibility.Visible;
        if (_player.RenderingMode == VideoRenderingMode.D3DImage)
        {
            CloseOverlay();
            if (!_view.Children.Contains(_caption)) _view.Children.Add(_caption);
            return;
        }

        Window? owner = Window.GetWindow(_view);
        if (owner == null || owner.WindowState == WindowState.Minimized || !owner.IsVisible)
        {
            _overlay?.Hide();
            return;
        }
        if (_owner != owner) CloseOverlay();
        if (_overlay == null)
        {
            _view.Children.Remove(_caption);
            _owner = owner;
            _overlay = new Window
            {
                WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent,
                ShowInTaskbar = false, ShowActivated = false, ResizeMode = ResizeMode.NoResize,
                Focusable = false, IsHitTestVisible = false, Content = _overlayContent, Width = width, Height = height
            };
            _overlayContent.Children.Add(_caption);
            // WindowInteropHelper.Owner establishes the native GWLP_HWNDPARENT owner relationship.
            _overlay.Owner = owner;
            new WindowInteropHelper(_overlay).Owner = new WindowInteropHelper(owner).EnsureHandle();
            _overlay.SourceInitialized += OnOverlayInitialized;
            owner.Closed += OnOwnerClosed;
        }
        var visible = new Rect(0, 0, width, height);
        DependencyObject? ancestor = _view;
        while ((ancestor = VisualTreeHelper.GetParent(ancestor)) != null)
        {
            if (ancestor is FrameworkElement element && (element.ClipToBounds || element is Window))
            {
                Rect bounds = element.TransformToVisual(_view).TransformBounds(new Rect(element.RenderSize));
                visible.Intersect(bounds);
            }
        }
        if (visible.IsEmpty)
        {
            _overlay.Hide();
            return;
        }
        _overlayContent.Clip = new RectangleGeometry(visible);
        Point origin = _view.PointToScreen(new Point());
        Point end = _view.PointToScreen(new Point(width, height));
        // WPF bounds use DIPs; native positioning keeps mixed-DPI screen coordinates unambiguous.
        if (!_overlay.IsVisible) _overlay.Show();
        nint handle = new WindowInteropHelper(_overlay).Handle;
        if (!SetWindowPos(handle, 0, (int)Math.Round(origin.X), (int)Math.Round(origin.Y),
            Math.Max(1, (int)Math.Round(end.X - origin.X)), Math.Max(1, (int)Math.Round(end.Y - origin.Y)), 0x0014))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private void OnOverlayInitialized(object? sender, EventArgs e)
    {
        nint hwnd = new WindowInteropHelper(_overlay!).Handle;
        int style = GetWindowLong(hwnd, -20);
        Marshal.SetLastPInvokeError(0);
        int previous = SetWindowLong(hwnd, -20, style | 0x08000000 | 0x00000020 | 0x00000080);
        if (previous == 0 && Marshal.GetLastWin32Error() != 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        HwndSource.FromHwnd(hwnd)?.AddHook(WindowHook);
    }

    private static nint WindowHook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == 0x0084) { handled = true; return -1; }
        if (message == 0x0021) { handled = true; return 3; }
        return 0;
    }

    private void OnOwnerClosed(object? sender, EventArgs e) => CloseOverlay();

    private void CloseOverlay()
    {
        if (_owner != null) _owner.Closed -= OnOwnerClosed;
        _owner = null;
        if (_overlay == null) return;
        var window = _overlay;
        _overlay = null;
        window.Content = null;
        _overlayContent.Children.Clear();
        window.Close();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _view.Loaded -= OnLoaded;
        _view.Unloaded -= OnUnloaded;
        CloseOverlay();
        _view.Children.Remove(_caption);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(nint hwnd, int index, int value);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
}
