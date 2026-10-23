using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using MediaFoundation.Controls;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class SourcePathTest
{
    internal static async Task RunAsync(string media, CancellationToken ct)
    {
        using var player = new MediaFoundation.Controls.MediaPlayer();
        var window = new Window { Content = new ContentControl { Content = player.VideoView }, Width = 640, Height = 480 };
        window.Show();
        string root = Path.Combine(AppContext.BaseDirectory, "path-probes", Guid.NewGuid().ToString("N"));
        string[] names = ["plain.mp4", "中文 空格 # % &.mp4", "literal%23%25%20.mp4", Path.Combine(new string('a', 110), new string('b', 110), "中文 # % &.mp4")];
        string[] cases = ["plain", "special", "literal-percent", "long"];
        int failures = 0;
        try
        {
            foreach (var mode in new[] { VideoRenderingMode.D3DImage, VideoRenderingMode.Hwnd })
            {
                player.Close();
                player.RenderingMode = mode;
                for (int i = 0; i < names.Length; i++)
                {
                    string target = Path.Combine(root, names[i]);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(media, target, true);
                    var source = new Uri(target);
                    PlaybackDiagnostics.RegisterSource(source);
                    Trace.WriteLine($"BEGIN source-path case={cases[i]} mode={mode}");
                    try
                    {
                        if (source.LocalPath != target) throw new InvalidOperationException("URI round-trip mismatch");
                        if (SourceDiagnostics.Describe(source).Split(' ')[0] != SourceDiagnostics.Describe(new Uri(source.AbsoluteUri)).Split(' ')[0])
                            throw new InvalidOperationException("Source correlation mismatch");
                        await OpenAsync(source);
                        player.Play();
                        await WaitAsync(() => player.Position > TimeSpan.FromMilliseconds(600));
                        await Task.Delay(1000, ct);
                        if (player.LastError != null) throw new InvalidOperationException("Playback failed", player.LastError);
                        Trace.WriteLine($"PASS source-path case={cases[i]} mode={mode} correlation=True {SourceDiagnostics.Describe(source)} position={player.Position}");
                    }
                    catch (Exception error) when (error is IOException or ExternalException or InvalidOperationException or TimeoutException)
                    {
                        failures++;
                        Trace.WriteLine($"FAIL source-path case={cases[i]} mode={mode} HRESULT=0x{error.HResult:X8} state={player.State}");
                    }
                    finally { player.Close(); }
                }
                var missing = new Uri(Path.Combine(root, "missing # % &.mp4"));
                PlaybackDiagnostics.RegisterSource(missing);
                var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                void OnFailed(object? sender, PlaybackFailedEventArgs e) => failure.TrySetResult(e.Error);
                player.Failed += OnFailed;
                try
                {
                    Trace.WriteLine($"BEGIN source-path case=missing mode={mode}");
                    Exception error;
                    try
                    {
                        player.Open(missing);
                        error = await failure.Task.WaitAsync(TimeSpan.FromSeconds(20), ct);
                    }
                    catch (Exception synchronousError) when (synchronousError is IOException or ExternalException)
                    {
                        error = synchronousError;
                    }
                    if (player.State != PlaybackState.Failed || error.HResult >= 0) throw new InvalidOperationException("Missing source did not fail");
                    await Task.Delay(1000, ct);
                    if (player.State != PlaybackState.Failed) throw new InvalidOperationException("Failed state changed after frame pump shutdown");
                    Trace.WriteLine($"PASS missing-source mode={mode} HRESULT=0x{error.HResult:X8} stableState={player.State}");
                }
                finally { player.Failed -= OnFailed; }
                await OpenAsync(new Uri(Path.Combine(root, names[0])));
                player.Play();
                await WaitAsync(() => player.Position > TimeSpan.FromMilliseconds(600));
                Trace.WriteLine($"PASS recovery mode={mode}");
            }
        }
        finally { player.Close(); window.Close(); }
        Trace.WriteLine($"SUMMARY source-path failures={failures}");
        if (failures != 0) throw new InvalidOperationException($"Source path cases failed: {failures}");

        async Task OpenAsync(Uri source)
        {
            var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnOpened(object? sender, EventArgs e) => opened.TrySetResult();
            void OnFailed(object? sender, PlaybackFailedEventArgs e) => opened.TrySetException(e.Error);
            player.MediaOpened += OnOpened;
            player.Failed += OnFailed;
            try { player.Open(source); await opened.Task.WaitAsync(TimeSpan.FromSeconds(20), ct); }
            finally { player.MediaOpened -= OnOpened; player.Failed -= OnFailed; }
        }
        async Task WaitAsync(Func<bool> predicate)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            while (!predicate())
            {
                if (player.LastError != null) throw new InvalidOperationException("Playback failed", player.LastError);
                await Task.Delay(100, timeout.Token);
            }
        }
    }
}
