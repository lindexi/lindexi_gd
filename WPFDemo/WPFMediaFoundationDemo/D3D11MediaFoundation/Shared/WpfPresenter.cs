#if WPF_SAMPLE
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Win32.Foundation;

namespace MediaSamples;

internal unsafe sealed class WpfPresenter : IDisposable
{
    private readonly Window _window;
    private readonly D3DImage _image = new();
    private MediaSession? _session;
    private readonly bool _selfTest = Environment.GetCommandLineArgs().Contains("--self-test");
    private readonly Stopwatch _clock = new();
    private readonly DispatcherTimer _timeout;
    private bool _completed;
    private bool _resizeRequested;
    private int _surfaceRebuilds;
    private long _framesAtResize;
    private readonly SampleKind _kind;
    internal MediaSession? Session => _session;

    internal WpfPresenter(Window window, Image image, SampleKind kind)
    {
        _window = window;
        _kind = kind;
        image.Source = _image;
        _timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _timeout.Tick += (_, _) => Finish();
        window.Closed += (_, _) => Dispose();
    }

    internal void Initialize()
    {
        try
        {
            string path = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal))
                ?? Path.Combine(AppContext.BaseDirectory, "Video", "视频.mp4");
            if (!File.Exists(path)) throw new FileNotFoundException("Video source not found.", path);
            _session = new MediaSession(new HWND(new WindowInteropHelper(_window).EnsureHandle()), _kind, path, true);
            _image.Lock();
            try { _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, (nint)_session.Graphics.Surface9); }
            finally { _image.Unlock(); }
            CompositionTarget.Rendering += Render;
            _clock.Start();
            if (_selfTest) _timeout.Start();
        }
        catch (Exception error) { Fail(error); }
    }

    private void Render(object? sender, EventArgs args)
    {
        try
        {
            if (_session == null || _completed) return;
            if (_selfTest && !_resizeRequested && _clock.Elapsed >= TimeSpan.FromSeconds(4))
            {
                _window.Width = 960;
                _window.Height = 640;
                _resizeRequested = true;
            }
            lock (_session.Gate)
            {
                if (_session.Graphics.SharedOutputSizeChanged())
                {
                    _image.Lock();
                    try
                    {
                        _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                        _session.Graphics.ResizeSharedOutput();
                        _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, (nint)_session.Graphics.Surface9);
                    }
                    finally { _image.Unlock(); }
                    _surfaceRebuilds++;
                    _framesAtResize = _session.Frames;
                }
                if (!_session.Tick()) return;
            }
            _image.Lock();
            try { _image.AddDirtyRect(new Int32Rect(0, 0, _session.Graphics.Width, _session.Graphics.Height)); }
            finally { _image.Unlock(); }
            if (_session.Frames == 1) Console.WriteLine("WPF shared D3D11/D3D9 texture presented through D3DImage.");
        }
        catch (Exception error) { Fail(error); }
    }

    private void Finish()
    {
        _timeout.Stop();
        Console.WriteLine($"RESULT kind={_kind} frames={_session?.Frames ?? 0} elapsed={_clock.Elapsed} selfTest={_selfTest}");
        if ((_session?.Frames ?? 0) < 3) { Fail(new InvalidOperationException("Fewer than three frames were actually presented.")); return; }
        Console.WriteLine($"RESIZE requested={_resizeRequested} rebuilds={_surfaceRebuilds} framesAfterResize={_session!.Frames - _framesAtResize} output={_session.Graphics.Width}x{_session.Graphics.Height}");
        if (_selfTest && (!_resizeRequested || _surfaceRebuilds == 0 || _session.Frames - _framesAtResize < 3))
        {
            Fail(new InvalidOperationException("Shared surface resize did not present at least three subsequent frames."));
            return;
        }
        _completed = true;
        _window.Close();
    }

    private void Fail(Exception error)
    {
        Console.Error.WriteLine($"FAIL kind={_kind}: {error}");
        Environment.ExitCode = 1;
        _completed = true;
        _timeout.Stop();
        _window.Close();
        Application.Current.Shutdown(1);
    }

    public void Dispose()
    {
        _timeout.Stop();
        CompositionTarget.Rendering -= Render;
        _image.Lock();
        try { _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0); }
        finally { _image.Unlock(); }
        _session?.Dispose();
        _session = null;
    }
}
#endif
