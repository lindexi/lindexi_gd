using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class MfPlayPrototype
{
    internal static async Task RunAsync(string path, bool testing, Window? owner = null)
    {
        using var log = testing ? new TextWriterTraceListener(Path.Combine(AppContext.BaseDirectory, "mfplay-d3dimage.log")) : null;
        if (log != null) Trace.Listeners.Add(log);
        Trace.AutoFlush = true;
        var source = Uri.TryCreate(path, UriKind.Absolute, out var uri) ? uri : new Uri(Path.GetFullPath(path));
        PlaybackDiagnostics.RegisterSource(source);
        var window = new Window { Title = Text("PrototypeTitle"), Width = 960, Height = 640, Background = Brushes.Black };
        if (owner != null) window.Owner = owner;
        Trace.WriteLine($"PROTOTYPE stage=window-created testing={testing} log={PlaybackDiagnostics.LogPath}", "MFPlay.Prototype");
        var image = new D3DImage();
        var view = new Image { Source = image, Stretch = Stretch.Uniform };
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(view, 1); grid.Children.Add(view);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Background = Brushes.DarkSlateBlue };
        grid.Children.Add(panel); window.Content = grid;
        var status = new TextBlock { Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(12) };
        Grid.SetRow(status, 2); grid.Children.Add(status);
        MediaPlaybackSession? session = null;
        Exception? failure = null;
        bool opened = false, closed = false;
        nint attached = 0;
        bool suppressDrawing = false;
        long nextLockDiagnostic = 0;
        var timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(8) };
        void Detach()
        {
            image.Lock();
            try { image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0); attached = 0; }
            finally { image.Unlock(); }
        }
        void Open()
        {
            Detach(); session?.Dispose(); opened = false; failure = null;
            session = MediaPlaybackSession.CreateD3DImagePrototype(new WindowInteropHelper(window).Handle);
            session.PrototypeSurfaceReleasing += (_, _) => Detach();
            session.MediaOpened += (_, _) => { opened = true; session.Play(); };
            session.Failed += (_, args) => { failure = args.Error; status.Text = args.Error.ToString(); PlaybackDiagnostics.Write(args.Error, null); }; 
            Trace.WriteLine($"PROTOTYPE stage=open sourcePath={(source.IsFile ? source.LocalPath : source.AbsoluteUri)}", "MFPlay.Prototype");
            session!.Open(source);
        }
        void Button(string key, Action action)
        {
            var button = new Button { Content = Text(key), Padding = new Thickness(12), Margin = new Thickness(3) };
            button.Click += (_, _) =>
            {
                try { action(); }
                catch (Exception error) when (error is ExternalException or InvalidOperationException or ArgumentException)
                { failure = error; status.Text = error.ToString(); PlaybackDiagnostics.Write(error, null); }
            };
            panel.Children.Add(button);
        }
        Button("PrototypePlay", () => session!.Play());
        Button("PrototypePause", () => session!.Pause());
        Button("PrototypeSeek", () => session!.Seek(TimeSpan.FromSeconds(2)));
        Button("PrototypeClose", () => { Detach(); session?.Dispose(); session = null; });
        Button("PrototypeReopen", Open);
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => { closed = true; timer.Stop(); Detach(); session?.Dispose(); ended.TrySetResult(); };
        try
        {
            window.Show();
            timer.Tick += (_, _) =>
            {
                if (failure != null || closed || session == null) return;
                try
                {
                    session.Refresh();
                    if (suppressDrawing) return;
                    bool writable = image.TryLock(new Duration(TimeSpan.FromMilliseconds(2)));
                    try
                    {
                        // WPF increments its lock count even when TryLock returns false.
                        if (!writable)
                        {
                            if (Environment.TickCount64 >= nextLockDiagnostic)
                            {
                                nextLockDiagnostic = Environment.TickCount64 + 5000;
                                Trace.WriteLine($"PROTOTYPE D3DImage.TryLock=false frontBufferAvailable={image.IsFrontBufferAvailable} actualFrames={session.PrototypeFrames}");
                            }
                            return;
                        }
                        session.PresentPrototypeFrame((surface, width, height, before) =>
                        {
                            if (before && attached != surface)
                            {
                                image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, true); attached = surface;
                            }
                            if (!before) image.AddDirtyRect(new Int32Rect(0, 0, width, height));
                        });
                    }
                    finally { image.Unlock(); }
                    status.Text = $"MFPlay + custom EVR | {session.State} | {session.Position} | actualFrames={session.PrototypeFrames}";
                }
                catch (Exception error) when (error is ExternalException or InvalidOperationException or ArgumentException)
                { failure = error; status.Text = error.ToString(); PlaybackDiagnostics.Write(error, null); }
            };
            timer.Start(); Open();
            if (!testing) { await ended.Task; return; }
            var playbackSession = session ?? throw new InvalidOperationException();
            async Task WaitAsync(Func<bool> condition, string stage)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                while (!condition())
                {
                    if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
                    try { await Task.Delay(20, timeout.Token); }
                    catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                    {
                        var error = new TimeoutException($"Prototype stage timed out: {stage}");
                        Trace.WriteLine($"PROTOTYPE FAIL stage={stage} HRESULT=0x{error.HResult:X8} actualFrames={playbackSession.PrototypeFrames}");
                        throw error;
                    }
                }
                Trace.WriteLine($"PROTOTYPE PASS stage={stage} actualFrames={playbackSession.PrototypeFrames}");
            }
            await WaitAsync(() => opened && playbackSession.PrototypeFrames >= 30, "first-30-frames");
            await WaitAsync(() => playbackSession.State == PlaybackState.Playing && playbackSession.Position > TimeSpan.FromSeconds(4.5), "playing-visible-sample");
            Trace.WriteLine($"PROTOTYPE visible-check position={playbackSession.Position} image={view.ActualWidth}x{view.ActualHeight} parent={grid.RenderSize} stretch={view.Stretch} frontBuffer={image.IsFrontBufferAvailable}");
            var bitmap = new RenderTargetBitmap((int)grid.ActualWidth, (int)grid.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(grid);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Path.Combine(AppContext.BaseDirectory, "mfplay-d3dimage-wpf.png"))) png.Save(file);
            playbackSession.Pause(); await WaitAsync(() => playbackSession.State == PlaybackState.Paused, "pause");
            playbackSession.Refresh(); var paused = playbackSession.Position;
            await Task.Delay(500); playbackSession.Refresh();
            if ((playbackSession.Position - paused).Duration() > TimeSpan.FromMilliseconds(100)) throw new InvalidOperationException("pause clock drift");
            playbackSession.Seek(TimeSpan.FromSeconds(6));
            playbackSession.Seek(TimeSpan.FromSeconds(1));
            playbackSession.Seek(TimeSpan.FromSeconds(2));
            await Task.Delay(700);
            if (playbackSession.State != PlaybackState.Paused) throw new InvalidOperationException("seek changed paused intent");
            long frames = playbackSession.PrototypeFrames;
            playbackSession.Play();
            await WaitAsync(() => playbackSession.State == PlaybackState.Playing && playbackSession.PrototypeFrames >= frames + 10 && playbackSession.Position > TimeSpan.FromSeconds(2.3), "seek-resume");
            Trace.WriteLine($"PROTOTYPE seek evidence position={playbackSession.Position} actualFrames={playbackSession.PrototypeFrames}");
            suppressDrawing = true;
            frames = playbackSession.PrototypeFrames;
            await WaitAsync(() => playbackSession.PrototypeFrames >= frames + 15, "media-progress-without-ui-drawing");
            suppressDrawing = false;
            await Task.Delay(200);
            Open();
            playbackSession = session ?? throw new InvalidOperationException();
            await WaitAsync(() => opened && playbackSession.PrototypeFrames >= 10, "close-reopen");
            Trace.WriteLine("PROTOTYPE RESULT PASS");
        }
        catch (Exception error)
        {
            Trace.WriteLine($"PROTOTYPE RESULT FAIL {error}");
            throw;
        }
        finally
        {
            timer.Stop(); if (!closed) window.Close();
            if (log != null) Trace.Listeners.Remove(log);
            Trace.WriteLine("PROTOTYPE stage=window-closed session-disposed", "MFPlay.Prototype");
        }
    }
    private static string Text(string key) => (string)Application.Current.FindResource(key);
}
