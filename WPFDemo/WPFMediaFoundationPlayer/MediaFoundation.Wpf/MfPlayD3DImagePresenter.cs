using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MediaFoundation.Core;

namespace MediaFoundation.Controls;

internal sealed class MfPlayD3DImagePresenter : IDisposable
{
    private readonly DiagnosticImage image = new();
    private readonly Image content;
    private int targetWidth, targetHeight;
    private long lastResize;
    private (int Width, int Height) pendingTarget;
    private bool diagnosed;

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
            var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth, height = converted.PixelHeight;
            var pixels = new byte[width * height * 4];
            converted.CopyPixels(pixels, width * 4, 0);
            var colors = new HashSet<int>();
            int minAlpha = 255, maxAlpha = 0;
            for (int y = height / 4; y < height * 3 / 4; y += 3)
                for (int x = width / 4; x < width * 3 / 4; x += 3)
                {
                    int i = (y * width + x) * 4;
                    colors.Add(pixels[i] | pixels[i + 1] << 8 | pixels[i + 2] << 16);
                    minAlpha = Math.Min(minAlpha, pixels[i + 3]); maxAlpha = Math.Max(maxAlpha, pixels[i + 3]);
                }
            Trace.WriteLine($"backend=MFPlay D3DImage.CopyBackBuffer size={width}x{height} rgbColors={colors.Count} alpha={minAlpha}..{maxAlpha}", "MediaFoundation.Controls");
        }
    }
    private readonly MediaPlaybackSession session;
    private readonly Action<Exception> reportError;
    private readonly DispatcherTimer timer;
    private nint attached;
    private bool disposed, force = true, firstFrame;
    private long nextDiagnostic;

    internal MfPlayD3DImagePresenter(MediaPlaybackSession session, Action<Exception> reportError)
    {
        this.session = session;
        this.reportError = reportError;
        content = new Image { Source = image, Stretch = Stretch.Fill, SnapsToDevicePixels = true };
        View = new Canvas { ClipToBounds = true, UseLayoutRounding = true };
        View.Children.Add(content);
        timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(8) };
        timer.Tick += OnTick;
        image.IsFrontBufferAvailableChanged += OnFrontBufferChanged;
        session.SurfaceReleasing += OnSurfaceReleasing;
        timer.Start();
        Trace.WriteLine("backend=MFPlay mode=D3DImage frame pump started; staging scheduler independent of UI", "MediaFoundation.Controls");
    }

    internal Canvas View { get; }

    private void UpdateViewport()
    {
        var info = session.VideoSurfaceInfo;
        var source = PresentationSource.FromVisual(View);
        if (info == null || source?.CompositionTarget == null || !View.IsVisible || Window.GetWindow(View)?.WindowState == WindowState.Minimized) return;
        var dpi = VisualTreeHelper.GetDpi(View);
        double width = View.ActualWidth * dpi.DpiScaleX, height = View.ActualHeight * dpi.DpiScaleY;
        if (width < 1 || height < 1 || info.DisplayAspectRatio <= 0) return;
        double scale = Math.Min(width / info.DisplayAspectRatio, height);
        int pixelsWide = Math.Max(1, (int)Math.Floor(scale * info.DisplayAspectRatio));
        int pixelsHigh = Math.Max(1, (int)Math.Floor(scale));
        var root = (Visual)source.RootVisual;
        var origin = View.TransformToAncestor(root).Transform(new Point());
        double x = (Math.Round(origin.X * dpi.DpiScaleX + (width - pixelsWide) / 2) - origin.X * dpi.DpiScaleX) / dpi.DpiScaleX;
        double y = (Math.Round(origin.Y * dpi.DpiScaleY + (height - pixelsHigh) / 2) - origin.Y * dpi.DpiScaleY) / dpi.DpiScaleY;
        Canvas.SetLeft(content, x); Canvas.SetTop(content, y);
        content.Width = pixelsWide / dpi.DpiScaleX; content.Height = pixelsHigh / dpi.DpiScaleY;
        if (pendingTarget != (pixelsWide, pixelsHigh))
        {
            pendingTarget = (pixelsWide, pixelsHigh);
            lastResize = Environment.TickCount64;
        }
        // Debounce interactive layout/DPI bursts without allocating intermediate surfaces.
        if ((targetWidth, targetHeight) == pendingTarget || Environment.TickCount64 - lastResize < 80) return;
        session.RequestVideoTarget(pixelsWide, pixelsHigh);
        targetWidth = pixelsWide; targetHeight = pixelsHigh;
        force = true;
        Trace.WriteLine($"WPF scaling requested={info.ScalingMode} viewportDip={View.ActualWidth:F2}x{View.ActualHeight:F2} dpi={dpi.DpiScaleX:F2}x{dpi.DpiScaleY:F2} target={pixelsWide}x{pixelsHigh} originDip={x:F2},{y:F2}", "MediaFoundation.Controls");
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (disposed) return;
        try
        {
            // Check scheduler failures even when WPF cannot acquire its display surface.
            session.Refresh();
            if (disposed) return;
            UpdateViewport();
            if (View.ActualWidth < 1 || View.ActualHeight < 1 || Window.GetWindow(View)?.WindowState == WindowState.Minimized) return;
            if (!diagnosed && session.State == PlaybackState.Playing && session.Position > TimeSpan.FromSeconds(4) && Environment.GetEnvironmentVariable("MF_DIAGNOSE_FRAME") == "1")
            {
                diagnosed = true;
                Trace.WriteLine($"backend=MFPlay pre-lock visible-check state={session.State} position={session.Position} frames={session.PrototypeFrames} image={View.ActualWidth}x{View.ActualHeight} parent={(View.Parent as FrameworkElement)?.RenderSize} stretch={content.Stretch} frontBuffer={image.IsFrontBufferAvailable}", "MediaFoundation.Controls");
                image.Diagnose();
            }
            bool writable = image.TryLock(new Duration(TimeSpan.FromMilliseconds(2)));
            try
            {
                // WPF increments its lock count even when TryLock returns false.
                if (!writable)
                {
                    if (Environment.TickCount64 >= nextDiagnostic)
                    {
                        nextDiagnostic = Environment.TickCount64 + 5000;
                        Trace.WriteLine($"backend=MFPlay mode=D3DImage lock=false frontBufferAvailable={image.IsFrontBufferAvailable} scheduledFrames={session.PrototypeFrames}", "MediaFoundation.Controls");
                    }
                    return;
                }
                if (session.CopyLatestFrame((surface, width, height, before) =>
                {
                    if (before && (attached != surface || force))
                    {
                        if (surface != attached) Marshal.AddRef(surface);
                        try { image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, true); }
                        catch { if (surface != attached) Marshal.Release(surface); throw; }
                        if (attached != 0 && attached != surface) Marshal.Release(attached);
                        attached = surface;
                    }
                    if (!before) image.AddDirtyRect(new Int32Rect(0, 0, width, height));
                }, force))
                {
                    force = false;
                    if (!firstFrame)
                    {
                        firstFrame = true;
                        Trace.WriteLine($"backend=MFPlay mode=D3DImage first-frame copied GPU-complete size={image.PixelWidth}x{image.PixelHeight}", "MediaFoundation.Controls");
                    }
                }
            }
            finally { image.Unlock(); }
        }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or ArgumentException)
        {
            reportError(error);
        }
    }

    private void OnFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        force = true;
        Trace.WriteLine($"backend=MFPlay mode=D3DImage frontBufferAvailable={image.IsFrontBufferAvailable}; rebind pending (not device recovery)", "MediaFoundation.Controls");
    }

    private void OnSurfaceReleasing(object? sender, EventArgs e)
    {
        timer.Stop();
        Detach();
    }

    private void Detach()
    {
        image.Lock();
        try
        {
            image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
            if (attached != 0) Marshal.Release(attached);
            attached = 0;
            force = true;
        }
        finally { image.Unlock(); }
        Trace.WriteLine("backend=MFPlay mode=D3DImage back-buffer detached before EVR release", "MediaFoundation.Controls");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        timer.Tick -= OnTick;
        image.IsFrontBufferAvailableChanged -= OnFrontBufferChanged;
        session.SurfaceReleasing -= OnSurfaceReleasing;
        Detach();
        content.Source = null;
        Trace.WriteLine("backend=MFPlay mode=D3DImage frame pump stopped; handlers detached", "MediaFoundation.Controls");
    }
}
