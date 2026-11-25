using System.Diagnostics;
using System.IO;
using System.Windows;
using MediaFoundation.Controls;

namespace MediaFoundation.PlayerDemo;

/// <summary>提供播放器演示和诊断自测的应用程序入口。</summary>
public partial class App : Application
{
    private sealed class SanitizedTestListener(TextWriter writer) : TextWriterTraceListener(writer)
    {
        /// <inheritdoc />
        public override void Write(string? message) => base.Write(PlaybackDiagnostics.Sanitize(message ?? ""));
        /// <inheritdoc />
        public override void WriteLine(string? message) => base.WriteLine(PlaybackDiagnostics.Sanitize(message ?? ""));
    }

    private void OnStorageFailed()
    {
        Dispatcher.BeginInvoke
        (
            new Action
            (() => MessageBox.Show
                (
                    Lang.Current.DemoLogFailureText.ToString
                    (
                        PlaybackDiagnostics.LogPath, PlaybackDiagnostics.Failure ?? string.Empty
                    ), Lang.Current.DemoTitle, MessageBoxButton.OK, MessageBoxImage.Warning
                )
            )
        );
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        PlaybackDiagnostics.StorageFailed -= OnStorageFailed;
        PlaybackDiagnostics.Stop();
        if (Environment.GetCommandLineArgs().Any
                (arg => arg is "--self-test" or "--scaling-test" or "--source-path-test"))
            File.Copy
                (PlaybackDiagnostics.LogPath, Path.Combine(AppContext.BaseDirectory, "playback-session.log"), true);
        base.OnExit(e);
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        PlaybackDiagnostics.StorageFailed += OnStorageFailed;
        PlaybackDiagnostics.Start();
        DispatcherUnhandledException += (_, args) => PlaybackDiagnostics.Write(args.Exception, null);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception error) PlaybackDiagnostics.Write(error, null);
        };
        base.OnStartup(e);
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool testing = e.Args.Contains("--self-test") || e.Args.Contains("--source-path-test") || e.Args.Contains
            ("--scaling-test");
        bool scalingTest = e.Args.Contains("--scaling-test");
        bool d3dImage = !e.Args.Contains("--hwnd");
        if (!d3dImage && e.Args.Contains("--d3dimage"))
            Trace.WriteLine("Both --hwnd and --d3dimage specified; --hwnd takes precedence.", "Demo.Diagnostics");
        if (testing && (scalingTest || d3dImage || e.Args.Contains("--mfplay-d3dimage")))
            Environment.SetEnvironmentVariable("MF_DIAGNOSE_FRAME", "1");
        if (testing && e.Args.Contains("--software-wpf"))
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        Trace.WriteLine
        (
            $"WPF tier={System.Windows.Media.RenderCapability.Tier >> 16} processMode={System.Windows.Media.RenderOptions.ProcessRenderMode}",
            "Demo.Diagnostics"
        );
        string? path = e.Args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
        if (testing && string.IsNullOrWhiteSpace(path) && File.Exists
                (Path.Combine(AppContext.BaseDirectory, "self-test.mp4")))
            path = Path.Combine(AppContext.BaseDirectory, "self-test.mp4");
        if (!string.IsNullOrWhiteSpace(path))
        {
            path = Path.GetFullPath(path, testing ? AppContext.BaseDirectory : Environment.CurrentDirectory);
            PlaybackDiagnostics.RegisterSource(new Uri(path));
        }

        if (e.Args.Contains("--mfplay-d3dimage"))
        {
            try
            {
                if (string.IsNullOrWhiteSpace
                        (path)) throw new ArgumentException(Lang.Current.PrototypeMediaRequired);
                await MFPlayPrototype.RunAsync
                    (Path.GetFullPath(path, AppContext.BaseDirectory), e.Args.Contains("--self-test"));
                Shutdown();
            }
            catch (Exception error)
            {
                PlaybackDiagnostics.Write(error, null);
                Console.Error.WriteLine(error);
                Shutdown(1);
            }

            return;
        }

        if (e.Args.Contains("--media-probe-test"))
        {
            try
            {
                await MediaProbeTest.RunAsync(path ?? Path.Combine(AppContext.BaseDirectory, "self-test.mp4"));
                Shutdown();
            }
            catch (Exception error) when (error is System.Runtime.InteropServices.ExternalException or IOException
                                              or InvalidOperationException or ArgumentException)
            {
                PlaybackDiagnostics.Write(error, null);
                await File.WriteAllTextAsync
                (
                    Path.Combine(AppContext.BaseDirectory, "media-probe-result.log"),
                    PlaybackDiagnostics.Sanitize(error.ToString())
                );
                Shutdown(1);
            }

            return;
        }

        if (e.Args.Contains("--subtitle-test"))
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            try
            {
                await SubtitleTest.RunAsync(cancellation.Token);
                Shutdown(0);
            }
            catch (Exception error)
            {
                PlaybackDiagnostics.Write(error, null);
                Shutdown(1);
            }
            return;
        }

        if (e.Args.Contains("--diagnostics-test"))
        {
            try
            {
                await SelfTest.RunDiagnosticsAsync();
                Shutdown();
            }
            catch (Exception error)
            {
                PlaybackDiagnostics.Write(error, null);
                await File.WriteAllTextAsync
                (
                    Path.Combine(AppContext.BaseDirectory, "diagnostics-result.log"),
                    error + Environment.NewLine + PlaybackDiagnostics.Summary
                );
                Shutdown(1);
            }

            return;
        }

        if (testing && string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine(Lang.Current.SelfTestMediaRequiredText);
            Shutdown(2);
            return;
        }

        MediaPlayerControl? control = null;
        Window? window = null;
        RenderingModeMenu? renderingMenu = null;
        SubtitleMenu? subtitleMenu = null;

        Window CreateWindow()
        {
            control = new MediaPlayerControl
            {
                AutoPlay = !testing, RenderingMode = d3dImage ? VideoRenderingMode.D3DImage : VideoRenderingMode.Hwnd,
                ScalingMode = e.Args.Contains("--presentation-surface") && !e.Args.Contains("--mixer-output")
                    ? MediaFoundation.Core.VideoScalingMode.PresentationSurface
                    : MediaFoundation.Core.VideoScalingMode.MixerOutput
            };
            var layout = new System.Windows.Controls.DockPanel();
            renderingMenu = new RenderingModeMenu(control, layout);
            subtitleMenu = new SubtitleMenu(control);
            window = new Window
            {
                Title = Lang.Current.DemoTitle, Width = 1100, Height = 720, MinWidth = 640, MinHeight = 360,
                Content = layout
            };
            window.Closed += (_, _) =>
            {
                subtitleMenu.Dispose();
                renderingMenu.Dispose();
                control.Dispose();
            };
            MainWindow = window;
            return window;
        }

        if (!testing)
        {
            CreateWindow().Closed += (_, _) => Shutdown();
            window!.Show();
            if (!string.IsNullOrWhiteSpace(path))
            {
                control!.Source = new Uri(Path.GetFullPath(path));
            }

            return;
        }

        using var log = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "self-test.log"), false)
            { AutoFlush = true };
        using var listener = new SanitizedTestListener(log);
        Trace.Listeners.Add(listener);
        Trace.AutoFlush = true;

        void Write(string message)
        {
            string line = $"{DateTime.UtcNow:O} {PlaybackDiagnostics.Sanitize(message)}";
            Console.WriteLine(line);
            log.WriteLine(line);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(200));
        // The independent deadline still terminates a test if the UI thread stops pumping.
        using var watchdogCancellation = new CancellationTokenSource();
        var watchdog = WatchDeadlineAsync(watchdogCancellation.Token);

        async Task WatchDeadlineAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(210), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }

            Write("FAIL 全局超时：Dispatcher 无响应");
            Environment.Exit(124);
        }

        int code = 1;
        try
        {
            if (e.Args.Contains("--source-path-test"))
            {
                Environment.SetEnvironmentVariable("MF_DIAGNOSE_FRAME", "1");
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                await SourcePathTest.RunAsync(Path.GetFullPath(path!, AppContext.BaseDirectory), timeout.Token);
                code = 0;
                return;
            }

            CreateWindow().Show();
            if (scalingTest)
            {
                await ScalingTest.RunAsync
                (
                    window!, control!, new Uri(Path.GetFullPath(path!, AppContext.BaseDirectory)), Write, timeout.Token
                );
                code = 0;
                return;
            }

            await VisualTest.RunEmptyAsync(window!, control!, Write, timeout.Token);
            await SelfTest.RunRenderingMenuAsync
            (
                control!, renderingMenu!, new Uri(Path.GetFullPath(path!, AppContext.BaseDirectory)), Write,
                timeout.Token
            );
            await SelfTest.RunAsync
                (window!, control!, new Uri(Path.GetFullPath(path!, AppContext.BaseDirectory)), Write, timeout.Token);
            code = 0;
        }
        catch (Exception error)
        {
            PlaybackDiagnostics.Write(error, control?.Player.Source);
            Write($"FAIL {PlaybackDiagnostics.Sanitize(error.ToString())}; log={PlaybackDiagnostics.LogPath}");
        }
        finally
        {
            subtitleMenu?.Dispose();
            renderingMenu?.Dispose();
            control?.Dispose();
            window?.Close();
            watchdogCancellation.Cancel();
            await watchdog;
            Write($"EXIT {code}");
            Trace.Listeners.Remove(listener);
            Shutdown(code);
        }
    }
}