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
    private readonly D3DImage image = new();
    private readonly System.Windows.Threading.DispatcherTimer frameTimer = new(System.Windows.Threading.DispatcherPriority.Render)
    {
        Interval = TimeSpan.FromMilliseconds(16)
    };
    private readonly SharedVideoSurface graphics;
    private readonly Action<Exception> failed;
    private MediaEngineSession? session;
    private bool disposed, faulted;
    private long frames;
    private long lastProgress = Environment.TickCount64;
    internal Image View { get; }
    internal nint Device => graphics.Device;

    internal D3DImagePresenter(Action<Exception> failed)
    {
        this.failed = failed;
        graphics = new SharedVideoSurface();
        View = new Image { Source = image, Stretch = Stretch.Uniform };
        image.IsFrontBufferAvailableChanged += OnFrontBufferChanged;
    }
    internal void Start(MediaEngineSession current)
    {
        session = current;
        frameTimer.Tick += Render;
        frameTimer.Start();
    }
    private void OnFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        System.Diagnostics.Trace.WriteLine($"D3DImage front buffer available={image.IsFrontBufferAvailable}", "MediaFoundation.Controls");
        if (!image.IsFrontBufferAvailable || disposed || faulted || graphics.Surface == 0) return;
        try
        {
            image.Lock();
            try
            {
                image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, graphics.Surface, true);
                image.AddDirtyRect(new Int32Rect(0, 0, graphics.Width, graphics.Height));
            }
            finally { image.Unlock(); }
        }
        catch (ExternalException error) { Fail(error); }
        catch (InvalidOperationException error) { Fail(error); }
    }
    private void Render(object? sender, EventArgs e)
    {
        if (disposed || faulted || session == null) return;
        try
        {
            session.Refresh();
            if (Environment.TickCount64 - lastProgress >= 5000)
            {
                lastProgress = Environment.TickCount64;
                System.Diagnostics.Trace.WriteLine($"D3DImage progress frames={frames} state={session.State} position={session.Position} rate={session.PlaybackRate} frontBuffer={image.IsFrontBufferAvailable} video={session.VideoWidth}x{session.VideoHeight}", "MediaFoundation.Controls");
            }
            int width = session.VideoWidth, height = session.VideoHeight;
            if (width <= 0 || height <= 0) return;
            if (width != graphics.Width || height != graphics.Height)
            {
                image.Lock();
                try
                {
                    image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                    graphics.Resize(width, height);
                    image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, graphics.Surface, true);
                }
                finally { image.Unlock(); }
            }
            if (!session.TransferFrame(graphics.Texture, width, height)) return;
            graphics.Flush();
            if (++frames == 1)
            {
                graphics.DiagnoseFrame();
                System.Diagnostics.Trace.WriteLine($"D3DImage first frame {width}x{height}", "MediaFoundation.Controls");
            }
            bool writable = image.TryLock(new Duration(TimeSpan.Zero));
            // WPF increments its lock count even when TryLock returns false.
            try { if (writable) image.AddDirtyRect(new Int32Rect(0, 0, width, height)); }
            finally { image.Unlock(); }
        }
        catch (ExternalException error) { Fail(error); }
        catch (InvalidOperationException error) { Fail(error); }
    }
    private void Fail(Exception error)
    {
        System.Diagnostics.Trace.WriteLine($"D3DImage stage=Render HRESULT=0x{error.HResult:X8} {error}", "MediaFoundation.Controls");
        faulted = true;
        frameTimer.Stop();
        frameTimer.Tick -= Render;
        failed(error);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; session = null;
        frameTimer.Stop();
        frameTimer.Tick -= Render;
        image.IsFrontBufferAvailableChanged -= OnFrontBufferChanged;
        image.Lock();
        try { image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0); }
        finally { image.Unlock(); }
        View.Source = null;
        graphics.Dispose();
        System.Diagnostics.Trace.WriteLine($"D3DImage frame pump stopped timerEnabled={frameTimer.IsEnabled} handlerDetached=True presented frames={frames}", "MediaFoundation.Controls");
    }
}
