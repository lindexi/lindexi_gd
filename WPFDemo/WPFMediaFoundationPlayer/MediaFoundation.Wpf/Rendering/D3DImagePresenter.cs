using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using MediaFoundation.Core;

namespace MediaFoundation.Controls;

[System.Runtime.Versioning.SupportedOSPlatform("windows8.0")]
internal sealed class D3DImagePresenter : IDisposable
{
    private readonly D3DImage _image = new();

    private readonly System.Windows.Threading.DispatcherTimer _frameTimer = new
        (System.Windows.Threading.DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

    private readonly SharedVideoSurface _graphics;
    private readonly Action<Exception> _failed;
    private MediaEngineSession? _session;
    private bool _disposed;
    private bool _faulted;
    private long _frames;
    private long _lastProgress = Environment.TickCount64;
    internal Image View { get; }
    internal nint Device => _graphics.Device;

    internal D3DImagePresenter(Action<Exception> failed)
    {
        _failed = failed;
        _graphics = new SharedVideoSurface();
        View = new Image { Source = _image, Stretch = Stretch.Uniform };
        _image.IsFrontBufferAvailableChanged += OnFrontBufferChanged;
    }

    internal void Start(MediaEngineSession current)
    {
        _session = current;
        _frameTimer.Tick += Render;
        _frameTimer.Start();
    }

    private void OnFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        System.Diagnostics.Trace.WriteLine
            ($"D3DImage front buffer available={_image.IsFrontBufferAvailable}", "MediaFoundation.Controls");
        if (!_image.IsFrontBufferAvailable || _disposed || _faulted || _graphics.Surface == 0) return;
        try
        {
            _image.Lock();
            try
            {
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _graphics.Surface, true);
                _image.AddDirtyRect(new Int32Rect(0, 0, _graphics.Width, _graphics.Height));
            }
            finally
            {
                _image.Unlock();
            }
        }
        catch (ExternalException error)
        {
            Fail(error);
        }
        catch (InvalidOperationException error)
        {
            Fail(error);
        }
    }

    private void Render(object? sender, EventArgs e)
    {
        if (_disposed || _faulted || _session == null) return;
        try
        {
            _session.Refresh();
            if (Environment.TickCount64 - _lastProgress >= 5000)
            {
                _lastProgress = Environment.TickCount64;
                System.Diagnostics.Trace.WriteLine
                (
                    $"D3DImage progress frames={_frames} state={_session.State} position={_session.Position} rate={_session.PlaybackRate} frontBuffer={_image.IsFrontBufferAvailable} video={_session.VideoWidth}x{_session.VideoHeight}",
                    "MediaFoundation.Controls"
                );
            }

            int width = _session.VideoWidth, height = _session.VideoHeight;
            if (width <= 0 || height <= 0) return;
            if (width != _graphics.Width || height != _graphics.Height)
            {
                _image.Lock();
                try
                {
                    _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                    _graphics.Resize(width, height);
                    _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _graphics.Surface, true);
                }
                finally
                {
                    _image.Unlock();
                }
            }

            if (!_session.TransferFrame(_graphics.Texture, width, height)) return;
            _graphics.Flush();
            if (++_frames == 1)
            {
                _graphics.DiagnoseFrame();
                System.Diagnostics.Trace.WriteLine
                    ($"D3DImage first frame {width}x{height}", "MediaFoundation.Controls");
            }

            bool writable = _image.TryLock(new Duration(TimeSpan.Zero));
            // WPF increments its lock count even when TryLock returns false.
            try
            {
                if (writable) _image.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                _image.Unlock();
            }
        }
        catch (ExternalException error)
        {
            Fail(error);
        }
        catch (InvalidOperationException error)
        {
            Fail(error);
        }
    }

    private void Fail(Exception error)
    {
        System.Diagnostics.Trace.WriteLine
            ($"D3DImage stage=Render HRESULT=0x{error.HResult:X8} {error}", "MediaFoundation.Controls");
        _faulted = true;
        _frameTimer.Stop();
        _frameTimer.Tick -= Render;
        _failed(error);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session = null;
        _frameTimer.Stop();
        _frameTimer.Tick -= Render;
        _image.IsFrontBufferAvailableChanged -= OnFrontBufferChanged;
        _image.Lock();
        try
        {
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
        }
        finally
        {
            _image.Unlock();
        }

        View.Source = null;
        _graphics.Dispose();
        System.Diagnostics.Trace.WriteLine
        (
            $"D3DImage frame pump stopped timerEnabled={_frameTimer.IsEnabled} handlerDetached=True presented frames={_frames}",
            "MediaFoundation.Controls"
        );
    }
}
