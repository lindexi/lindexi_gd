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
    private readonly SharedVideoSurface graphics;
    private readonly Action<Exception> failed;
    private MediaEngineSession? session;
    private bool disposed, faulted;
    private long frames;
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
        CompositionTarget.Rendering += Render;
    }
    private void OnFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!image.IsFrontBufferAvailable || disposed || faulted) return;
        image.Lock();
        try { image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, graphics.Surface); }
        finally { image.Unlock(); }
    }
    private void Render(object? sender, EventArgs e)
    {
        if (disposed || faulted || session == null || !image.IsFrontBufferAvailable || !View.IsVisible) return;
        try
        {
            session.Refresh();
            int width = session.VideoWidth, height = session.VideoHeight;
            if (width <= 0 || height <= 0) return;
            if (!image.TryLock(new Duration(TimeSpan.Zero))) return;
            try
            {
                if (width != graphics.Width || height != graphics.Height)
                {
                    image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                    graphics.Resize(width, height);
                    image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, graphics.Surface);
                }
                if (session.TransferFrame(graphics.Texture, width, height))
                {
                    graphics.Flush();
                    image.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    if (++frames == 1) System.Diagnostics.Trace.WriteLine($"D3DImage first frame {width}x{height}", "MediaFoundation.Controls");
                }
            }
            finally { image.Unlock(); }
        }
        catch (ExternalException error) { Fail(error); }
        catch (InvalidOperationException error) { Fail(error); }
    }
    private void Fail(Exception error)
    {
        faulted = true;
        CompositionTarget.Rendering -= Render;
        failed(error);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; session = null;
        CompositionTarget.Rendering -= Render;
        image.IsFrontBufferAvailableChanged -= OnFrontBufferChanged;
        image.Lock();
        try { image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0); }
        finally { image.Unlock(); }
        View.Source = null;
        graphics.Dispose();
        System.Diagnostics.Trace.WriteLine($"D3DImage presented frames={frames}", "MediaFoundation.Controls");
    }
}
