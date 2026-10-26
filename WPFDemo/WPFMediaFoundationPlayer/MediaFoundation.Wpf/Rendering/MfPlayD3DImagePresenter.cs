using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MediaFoundation.Core;

namespace MediaFoundation.Controls;

internal sealed class MFPlayD3DImagePresenter : IDisposable
{
    private readonly DiagnosticImage _image = new();
    private readonly Image _content;
    private int _targetWidth;
    private int _targetHeight;
    private long _lastResize;
    private (int Width, int Height) _pendingTarget;
    private bool _diagnosed;

    private sealed class DiagnosticImage : D3DImage
    {
        internal void Diagnose()
        {
            var bitmap = CopyBackBuffer();
            if (bitmap == null)
            {
                Trace.WriteLine("backend=MFPlay D3DImage.CopyBackBuffer=null", "MediaFoundation.Controls");
                return;
            }

            var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap
                (bitmap, PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth, height = converted.PixelHeight;
            var pixels = new byte[width * height * 4];
            converted.CopyPixels(pixels, width * 4, 0);
            var colors = new HashSet<int>();
            int minAlpha = 255, maxAlpha = 0;
            for (int y = height / 4; y < height * 3 / 4; y += 3)
            {
                for (int x = width / 4; x < width * 3 / 4; x += 3)
                {
                    int i = (y * width + x) * 4;
                    colors.Add(pixels[i] | pixels[i + 1] << 8 | pixels[i + 2] << 16);
                    minAlpha = Math.Min(minAlpha, pixels[i + 3]);
                    maxAlpha = Math.Max(maxAlpha, pixels[i + 3]);
                }
            }

            Trace.WriteLine
            (
                $"backend=MFPlay D3DImage.CopyBackBuffer size={width}x{height} rgbColors={colors.Count} alpha={minAlpha}..{maxAlpha}",
                "MediaFoundation.Controls"
            );
        }
    }

    private readonly MediaPlaybackSession _session;
    private readonly Action<Exception> _reportError;
    private readonly DispatcherTimer _timer;
    private nint _attached;
    private bool _disposed;
    private bool _force = true;
    private bool _firstFrame;
    private long _nextDiagnostic;

    internal MFPlayD3DImagePresenter(MediaPlaybackSession session, Action<Exception> reportError)
    {
        _session = session;
        _reportError = reportError;
        _content = new Image { Source = _image, Stretch = Stretch.Fill, SnapsToDevicePixels = true };
        View = new Canvas { ClipToBounds = true, UseLayoutRounding = true };
        View.Children.Add(_content);
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(8) };
        _timer.Tick += OnTick;
        _image.IsFrontBufferAvailableChanged += OnFrontBufferChanged;
        _session.SurfaceReleasing += OnSurfaceReleasing;
        _timer.Start();
        Trace.WriteLine
        (
            "backend=MFPlay mode=D3DImage frame pump started; staging scheduler independent of UI",
            "MediaFoundation.Controls"
        );
    }

    internal Canvas View { get; }

    private void UpdateViewport()
    {
        var info = _session.VideoSurfaceInfo;
        var source = PresentationSource.FromVisual(View);
        if (info == null || source?.CompositionTarget == null || !View.IsVisible ||
            Window.GetWindow(View)?.WindowState == WindowState.Minimized) return;
        var dpi = VisualTreeHelper.GetDpi(View);
        double width = View.ActualWidth * dpi.DpiScaleX, height = View.ActualHeight * dpi.DpiScaleY;
        if (width < 1 || height < 1 || info.DisplayAspectRatio <= 0) return;
        double scale = Math.Min(width / info.DisplayAspectRatio, height);
        int pixelsWide = Math.Max(1, (int)Math.Floor(scale * info.DisplayAspectRatio));
        int pixelsHigh = Math.Max(1, (int)Math.Floor(scale));
        var root = (Visual)source.RootVisual;
        var origin = View.TransformToAncestor(root).Transform(new Point());
        double x = (Math.Round(origin.X * dpi.DpiScaleX + (width - pixelsWide) / 2) - origin.X * dpi.DpiScaleX) /
                   dpi.DpiScaleX;
        double y = (Math.Round(origin.Y * dpi.DpiScaleY + (height - pixelsHigh) / 2) - origin.Y * dpi.DpiScaleY) /
                   dpi.DpiScaleY;
        Canvas.SetLeft(_content, x);
        Canvas.SetTop(_content, y);
        _content.Width = pixelsWide / dpi.DpiScaleX;
        _content.Height = pixelsHigh / dpi.DpiScaleY;
        if (_pendingTarget != (pixelsWide, pixelsHigh))
        {
            _pendingTarget = (pixelsWide, pixelsHigh);
            _lastResize = Environment.TickCount64;
        }

        // Debounce interactive layout/DPI bursts without allocating intermediate surfaces.
        if ((_targetWidth, _targetHeight) == _pendingTarget || Environment.TickCount64 - _lastResize < 80) return;
        _session.RequestVideoTarget(pixelsWide, pixelsHigh);
        _targetWidth = pixelsWide;
        _targetHeight = pixelsHigh;
        _force = true;
        Trace.WriteLine
        (
            $"WPF scaling requested={info.ScalingMode} viewportDip={View.ActualWidth:F2}x{View.ActualHeight:F2} dpi={dpi.DpiScaleX:F2}x{dpi.DpiScaleY:F2} target={pixelsWide}x{pixelsHigh} originDip={x:F2},{y:F2}",
            "MediaFoundation.Controls"
        );
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_disposed) return;
        try
        {
            // Check scheduler failures even when WPF cannot acquire its display surface.
            _session.Refresh();
            if (_disposed) return;
            UpdateViewport();
            if (View.ActualWidth < 1 || View.ActualHeight < 1 ||
                Window.GetWindow(View)?.WindowState == WindowState.Minimized) return;
            if (!_diagnosed && _session.State == PlaybackState.Playing && _session.Position > TimeSpan.FromSeconds(4) &&
                Environment.GetEnvironmentVariable("MF_DIAGNOSE_FRAME") == "1")
            {
                _diagnosed = true;
                Trace.WriteLine
                (
                    $"backend=MFPlay pre-lock visible-check state={_session.State} position={_session.Position} frames={_session.PrototypeFrames} image={View.ActualWidth}x{View.ActualHeight} parent={(View.Parent as FrameworkElement)?.RenderSize} stretch={_content.Stretch} frontBuffer={_image.IsFrontBufferAvailable}",
                    "MediaFoundation.Controls"
                );
                _image.Diagnose();
            }

            bool writable = _image.TryLock(new Duration(TimeSpan.FromMilliseconds(2)));
            try
            {
                // WPF increments its lock count even when TryLock returns false.
                if (!writable)
                {
                    if (Environment.TickCount64 >= _nextDiagnostic)
                    {
                        _nextDiagnostic = Environment.TickCount64 + 5000;
                        Trace.WriteLine
                        (
                            $"backend=MFPlay mode=D3DImage lock=false frontBufferAvailable={_image.IsFrontBufferAvailable} scheduledFrames={_session.PrototypeFrames}",
                            "MediaFoundation.Controls"
                        );
                    }

                    return;
                }

                if (_session.CopyLatestFrame
                    (
                        (surface, width, height, before) =>
                        {
                            if (before && (_attached != surface || _force))
                            {
                                if (surface != _attached) Marshal.AddRef(surface);
                                try
                                {
                                    _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, true);
                                }
                                catch
                                {
                                    if (surface != _attached) Marshal.Release(surface);
                                    throw;
                                }

                                if (_attached != 0 && _attached != surface) Marshal.Release(_attached);
                                _attached = surface;
                            }

                            if (!before) _image.AddDirtyRect(new Int32Rect(0, 0, width, height));
                        }, _force
                    ))
                {
                    _force = false;
                    if (!_firstFrame)
                    {
                        _firstFrame = true;
                        Trace.WriteLine
                        (
                            $"backend=MFPlay mode=D3DImage first-frame copied GPU-complete size={_image.PixelWidth}x{_image.PixelHeight}",
                            "MediaFoundation.Controls"
                        );
                    }
                }
            }
            finally
            {
                _image.Unlock();
            }
        }
        catch (ExternalException error)
        {
            _reportError(error);
        }
        catch (InvalidOperationException error)
        {
            _reportError(error);
        }
        catch (ArgumentException error)
        {
            _reportError(error);
        }
    }

    private void OnFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _force = true;
        Trace.WriteLine
        (
            $"backend=MFPlay mode=D3DImage frontBufferAvailable={_image.IsFrontBufferAvailable}; rebind pending (not device recovery)",
            "MediaFoundation.Controls"
        );
    }

    private void OnSurfaceReleasing(object? sender, EventArgs e)
    {
        _timer.Stop();
        Detach();
    }

    private void Detach()
    {
        _image.Lock();
        try
        {
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
            if (_attached != 0) Marshal.Release(_attached);
            _attached = 0;
            _force = true;
        }
        finally
        {
            _image.Unlock();
        }

        Trace.WriteLine
            ("backend=MFPlay mode=D3DImage back-buffer detached before EVR release", "MediaFoundation.Controls");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _image.IsFrontBufferAvailableChanged -= OnFrontBufferChanged;
        _session.SurfaceReleasing -= OnSurfaceReleasing;
        Detach();
        _content.Source = null;
        Trace.WriteLine
            ("backend=MFPlay mode=D3DImage frame pump stopped; handlers detached", "MediaFoundation.Controls");
    }
}
