using System.Diagnostics;
using System.IO;
using System.Windows;
using MediaFoundation.Controls;

namespace MediaFoundation.PlayerDemo;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool testing = e.Args.Contains("--self-test");
        if (testing) System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        string? path = e.Args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
        if (testing && string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine((string)FindResource("SelfTestMediaRequiredText"));
            Shutdown(2);
            return;
        }
        MediaPlayerControl? control = null;
        Window? window = null;
        Window CreateWindow()
        {
            control = new MediaPlayerControl { AutoPlay = !testing };
            window = new Window { Title = (string)FindResource("DemoTitle"), Width = 1100, Height = 720, MinWidth = 640, MinHeight = 360, Content = control };
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
        using var log = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "self-test.log"), false) { AutoFlush = true };
        using var listener = new TextWriterTraceListener(log);
        Trace.Listeners.Add(listener);
        Trace.AutoFlush = true;
        void Write(string message) { string line = $"{DateTimeOffset.Now:O} {message}"; Console.WriteLine(line); log.WriteLine(line); }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(200));
        // The independent deadline still terminates a test if the UI thread stops pumping.
        using var watchdogCancellation = new CancellationTokenSource();
        var watchdog = WatchDeadlineAsync(watchdogCancellation.Token);
        async Task WatchDeadlineAsync(CancellationToken token)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(210), token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            Write("FAIL 全局超时：Dispatcher 无响应");
            Environment.Exit(124);
        }
        int code = 1;
        try
        {
            CreateWindow().Show();
            await SelfTest.RunAsync(window!, control!, new Uri(Path.GetFullPath(path!)), Write, timeout.Token);
            code = 0;
        }
        catch (Exception error)
        {
            Write($"FAIL {error}");
        }
        finally
        {
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
